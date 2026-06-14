using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerRigidbodyMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float runSpeed = 6f;
    [SerializeField] private float acceleration = 16f;
    [SerializeField] private float rotationSpeed = 12f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 6f;
    [SerializeField] private GroundSensor groundSensor;

    [Header("Input")]
    [SerializeField] private KeyCode runKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;

    private Rigidbody rb;

    private Vector2 moveInput;
    private bool wantsToRun;
    private bool wantsToJump;

    public float CurrentSpeed { get; private set; }
    public bool IsGrounded => groundSensor != null && groundSensor.IsGrounded;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
    }

    private void Update()
    {
        ReadInput();
    }

    private void FixedUpdate()
    {
        Move();

        if (wantsToJump && IsGrounded)
        {
            Jump();
        }

        wantsToJump = false;
    }

    private void ReadInput()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        moveInput = new Vector2(horizontal, vertical);
        moveInput = Vector2.ClampMagnitude(moveInput, 1f);

        wantsToRun = Input.GetKey(runKey);

        if (Input.GetKeyDown(jumpKey))
        {
            wantsToJump = true;
        }
    }

    private void Move()
    {
        float targetSpeed = wantsToRun ? runSpeed : walkSpeed;

        Vector3 moveDirection = new Vector3(
            moveInput.x,
            0f,
            moveInput.y
        );

        if (moveDirection.sqrMagnitude > 0.01f)
        {
            moveDirection.Normalize();

            Quaternion targetRotation = Quaternion.LookRotation(
                moveDirection,
                Vector3.up
            );

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.fixedDeltaTime
            );
        }

        Vector3 currentVelocity = rb.linearVelocity;

        Vector3 currentHorizontalVelocity = new Vector3(
            currentVelocity.x,
            0f,
            currentVelocity.z
        );

        Vector3 targetHorizontalVelocity = moveDirection * targetSpeed;

        Vector3 newHorizontalVelocity = Vector3.MoveTowards(
            currentHorizontalVelocity,
            targetHorizontalVelocity,
            acceleration * Time.fixedDeltaTime
        );

        rb.linearVelocity = new Vector3(
            newHorizontalVelocity.x,
            currentVelocity.y,
            newHorizontalVelocity.z
        );

        CurrentSpeed = newHorizontalVelocity.magnitude;
    }

    private void Jump()
    {
        Vector3 velocity = rb.linearVelocity;
        velocity.y = 0f;
        rb.linearVelocity = velocity;

        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

        if (groundSensor != null)
        {
            groundSensor.ResetContacts();
        }
    }

    private void OnGUI()
    {
        if (!showDebugInfo)
        {
            return;
        }

        GUI.Label(new Rect(20, 20, 300, 25), "Speed: " + CurrentSpeed.ToString("F2"));
        GUI.Label(new Rect(20, 45, 300, 25), "Grounded: " + IsGrounded);
        GUI.Label(new Rect(20, 70, 300, 25), "Running: " + wantsToRun);
    }
}