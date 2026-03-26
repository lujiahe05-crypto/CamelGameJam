using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // Movement settings
    const float MoveSpeed = 5f;
    const float SwimSpeed = 3f;
    const float JumpForce = 6f;
    const float MouseSensitivity = 2f;
    const float Gravity = -15f;
    const float SwimGravity = -2f;
    const float WaterDrag = 3f;

    // Components
    CharacterController cc;
    Transform camPivot;
    Camera cam;

    // State
    Vector3 velocity;
    float cameraPitch;
    bool isInWater;
    bool isGrounded;

    void Start()
    {
        // Character controller
        cc = gameObject.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.3f;
        cc.center = new Vector3(0, 0.9f, 0);
        cc.slopeLimit = 45;
        cc.stepOffset = 0.4f;

        // Camera pivot
        camPivot = new GameObject("CamPivot").transform;
        camPivot.SetParent(transform);
        camPivot.localPosition = new Vector3(0, 1.6f, 0);

        cam = Camera.main;
        cam.transform.SetParent(camPivot);
        cam.transform.localPosition = Vector3.zero;
        cam.transform.localRotation = Quaternion.identity;
    }

    void Update()
    {
        HandleMouseLook();
        HandleMovement();
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * MouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * MouseSensitivity;

        // Horizontal rotation on player body
        transform.Rotate(Vector3.up, mouseX);

        // Vertical rotation on camera pivot
        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, -85f, 85f);
        camPivot.localRotation = Quaternion.Euler(cameraPitch, 0, 0);
    }

    void HandleMovement()
    {
        isGrounded = cc.isGrounded;
        isInWater = transform.position.y + 0.5f < RaftGame.WaterLevel;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 moveDir = transform.right * h + transform.forward * v;
        if (moveDir.magnitude > 1f) moveDir.Normalize();

        if (isInWater)
        {
            // Swimming
            moveDir *= SwimSpeed;

            // Vertical swim
            if (Input.GetKey(KeyCode.Space))
                velocity.y = SwimSpeed;
            else
                velocity.y = Mathf.MoveTowards(velocity.y, SwimGravity, WaterDrag * Time.deltaTime);

            // Dampen horizontal velocity in water
            velocity.x = moveDir.x;
            velocity.z = moveDir.z;
        }
        else
        {
            // Ground / Air
            if (isGrounded && velocity.y < 0)
                velocity.y = -2f; // Small downward to stay grounded

            velocity.x = moveDir.x * MoveSpeed;
            velocity.z = moveDir.z * MoveSpeed;

            if (isGrounded && Input.GetKeyDown(KeyCode.Space))
                velocity.y = JumpForce;
            else
                velocity.y += Gravity * Time.deltaTime;
        }

        cc.Move(velocity * Time.deltaTime);

        // Prevent falling too far below water
        if (transform.position.y < RaftGame.WaterLevel - 5f)
        {
            transform.position = new Vector3(transform.position.x, RaftGame.WaterLevel - 5f, transform.position.z);
            velocity.y = 0;
        }
    }

    public bool IsInWater() => isInWater;
    public Camera GetCamera() => cam;
}
