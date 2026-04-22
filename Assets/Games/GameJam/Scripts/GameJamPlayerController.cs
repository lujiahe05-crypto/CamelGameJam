using UnityEngine;

public class GameJamPlayerController : MonoBehaviour
{
    public float moveSpeed = 6f;
    public float jumpHeight = 1.2f;
    public float gravity = -20f;
    public float turnSmoothTime = 0.1f;

    CharacterController cc;
    float verticalVelocity;
    float turnSmoothVelocity;

    void Start()
    {
        cc = GetComponent<CharacterController>();
    }

    void Update()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        var input = new Vector3(h, 0, v).normalized;

        if (cc.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        if (cc.isGrounded && Input.GetButtonDown("Jump"))
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

        verticalVelocity += gravity * Time.deltaTime;

        var move = input * moveSpeed;
        move.y = verticalVelocity;
        cc.Move(move * Time.deltaTime);

        if (input.magnitude > 0.01f)
        {
            float targetAngle = Mathf.Atan2(input.x, input.z) * Mathf.Rad2Deg;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0, angle, 0);
        }
    }
}
