using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerRigidbodyMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float runSpeed = 6f;
    [SerializeField] private float acceleration = 16f;
    [SerializeField] private float rotationSpeed = 12f;

    [Header("Camera Relative Movement")]
    [SerializeField] private Transform cameraTransform;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 6f;
    [SerializeField] private GroundSensor groundSensor;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;

    private Rigidbody rb;
    private PlayerInput playerInput;
    private InputAction sprintAction;

    private Vector2 moveInput;
    private bool wantsToRun;
    private bool wantsToJump;

    private float timedSpeedMultiplier = 1f;
    private float zoneSpeedMultiplier = 1f;
    private float timedEffectEndTime;

    public float CurrentSpeed { get; private set; }
    public bool IsGrounded => groundSensor != null && groundSensor.IsGrounded;
    public bool IsRunning => wantsToRun && CurrentSpeed > 0.1f;
    public bool HasMovementInput => moveInput.sqrMagnitude > 0.01f;
    public float EffectiveSpeedMultiplier => timedSpeedMultiplier * zoneSpeedMultiplier;
    public float TimedSpeedRemaining => timedSpeedMultiplier != 1f ? Mathf.Max(0f, timedEffectEndTime - Time.time) : 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        playerInput = GetComponent<PlayerInput>();
        sprintAction = playerInput.actions != null
            ? playerInput.actions.FindAction("Sprint", false)
            : null;

        if (sprintAction != null)
        {
            sprintAction.started += OnSprintStarted;
            sprintAction.canceled += OnSprintCanceled;
        }
    }

    private void OnDestroy()
    {
        if (sprintAction == null) return;

        sprintAction.started -= OnSprintStarted;
        sprintAction.canceled -= OnSprintCanceled;
    }

    private void OnSprintStarted(InputAction.CallbackContext context) => wantsToRun = true;
    private void OnSprintCanceled(InputAction.CallbackContext context) => wantsToRun = false;

    // ─── Input Callbacks (called by PlayerInput component) ───────────────────

    private void OnMove(InputValue value)
    {
        moveInput = Vector2.ClampMagnitude(value.Get<Vector2>(), 1f);
    }

    private void OnSprint(InputValue value)
    {
        wantsToRun = value.isPressed;
    }

    private void OnJump(InputValue value)
    {
        if (value.isPressed)
            wantsToJump = true;
    }

    // ─── Physics ──────────────────────────────────────────────────────────────

    private void FixedUpdate()
    {
        Move();

        if (wantsToJump && IsGrounded)
            Jump();

        wantsToJump = false;
    }

    private void Move()
    {
        float effectiveMultiplier = timedSpeedMultiplier * zoneSpeedMultiplier;
        float targetSpeed = (wantsToRun ? runSpeed : walkSpeed) * effectiveMultiplier;

        Vector3 moveDirection = GetCameraRelativeMoveDirection();

        RotatePlayerTowardsMovement(moveDirection);

        Vector3 currentVelocity = rb.linearVelocity;

        Vector3 currentHorizontalVelocity = new Vector3(currentVelocity.x, 0f, currentVelocity.z);
        Vector3 targetHorizontalVelocity = moveDirection * targetSpeed;

        Vector3 newHorizontalVelocity = Vector3.MoveTowards(
            currentHorizontalVelocity,
            targetHorizontalVelocity,
            acceleration * Time.fixedDeltaTime
        );

        rb.linearVelocity = new Vector3(newHorizontalVelocity.x, currentVelocity.y, newHorizontalVelocity.z);

        CurrentSpeed = newHorizontalVelocity.magnitude;
    }

    private Vector3 GetCameraRelativeMoveDirection()
    {
        if (moveInput.sqrMagnitude < 0.01f)
            return Vector3.zero;

        if (cameraTransform == null)
            return new Vector3(moveInput.x, 0f, moveInput.y).normalized;

        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;

        cameraForward.Normalize();
        cameraRight.Normalize();

        return (cameraRight * moveInput.x + cameraForward * moveInput.y).normalized;
    }

    private void RotatePlayerTowardsMovement(Vector3 moveDirection)
    {
        if (moveDirection.sqrMagnitude < 0.01f) return;

        Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
    }

    private void Jump()
    {
        Vector3 velocity = rb.linearVelocity;
        velocity.y = 0f;
        rb.linearVelocity = velocity;

        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

        if (groundSensor != null)
            groundSensor.ResetContacts();
    }

    // ─── Speed Effects ────────────────────────────────────────────────────────

    public void ApplyTimedSpeedEffect(float multiplier, float duration)
    {
        StopCoroutine(nameof(TimedSpeedRoutine));
        StartCoroutine(TimedSpeedRoutine(multiplier, duration));
    }

    private System.Collections.IEnumerator TimedSpeedRoutine(float multiplier, float duration)
    {
        timedSpeedMultiplier = multiplier;
        timedEffectEndTime = Time.time + duration;
        yield return new WaitForSeconds(duration);
        timedSpeedMultiplier = 1f;
    }

    public void SetZoneSpeedEffect(float multiplier)
    {
        zoneSpeedMultiplier = multiplier;
    }

    // ─── Debug ────────────────────────────────────────────────────────────────

    private void OnGUI()
    {
        if (!showDebugInfo) return;

        GUI.Label(new Rect(20, 20, 300, 25), "Speed: " + CurrentSpeed.ToString("F2"));
        GUI.Label(new Rect(20, 45, 300, 25), "Grounded: " + IsGrounded);
        GUI.Label(new Rect(20, 70, 300, 25), "Running: " + wantsToRun);
    }
}
