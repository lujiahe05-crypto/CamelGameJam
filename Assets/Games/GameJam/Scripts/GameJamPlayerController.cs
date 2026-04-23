using UnityEngine;

public class GameJamPlayerController : MonoBehaviour
{
    public float moveSpeed = 6f;
    public float jumpHeight = 1.2f;
    public float gravity = -20f;
    public float turnSmoothTime = 0.1f;

    CharacterController cc;
    Animator animator;
    float verticalVelocity;
    float turnSmoothVelocity;

    public Animator Animator => animator;

    void Start()
    {
        cc = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        var input = new Vector3(h, 0, v).normalized;
        float planarSpeed = input.magnitude * moveSpeed;
        bool jumpPressed = cc.isGrounded && Input.GetButtonDown("Jump");

        if (cc.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        if (jumpPressed)
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

        verticalVelocity += gravity * Time.deltaTime;

        var move = input * moveSpeed;
        move.y = verticalVelocity;
        cc.Move(move * Time.deltaTime);
        bool isGrounded = cc.isGrounded;
        bool animOnGround = isGrounded && !jumpPressed && verticalVelocity <= 0.05f;

        if (input.magnitude > 0.01f)
        {
            float targetAngle = Mathf.Atan2(input.x, input.z) * Mathf.Rad2Deg;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0, angle, 0);
        }

        if (animator != null)
        {
            // This controller's locomotion blend tree expects world-speed-like values (2/6/8),
            // not a normalized 0..1 input magnitude.
            animator.SetFloat("Speed", planarSpeed);
            // The Oaks controller uses left/right "rotate" clips here rather than strafe locomotion.
            // Since we already rotate the character toward movement input in code, keep locomotion
            // centered on the forward run/walk clip for all move directions.
            animator.SetFloat("Direction", 0f);
            animator.SetBool("OnGround", animOnGround);
            animator.SetFloat("VY", verticalVelocity);
            if (jumpPressed)
            {
                animator.ResetTrigger("Jump");
                animator.SetTrigger("Jump");
            }
        }
    }
}
