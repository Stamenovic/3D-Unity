using System.Collections;
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
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private GroundSensor groundSensor;
    [SerializeField] private float fallbackGroundCheckRadius = 0.22f;
    [SerializeField] private float fallbackGroundCheckHeight = 0.18f;
    [SerializeField] private float extraFallGravity = 18f;
    [SerializeField] private float maxUpwardVelocity = 6f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private float jumpUpAnimationTime = 0.35f;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int SpeedMultiplierHash = Animator.StringToHash("SpeedMultiplier");
    private static readonly int IsRunningHash = Animator.StringToHash("IsRunning");
    private static readonly int IdleStateHash = Animator.StringToHash("Idle");
    private static readonly int WalkStateHash = Animator.StringToHash("Walk");
    private static readonly int RunStateHash = Animator.StringToHash("Run");
    private static readonly int JumpUpStateHash = Animator.StringToHash("JumpUp");
    private static readonly int JumpDownStateHash = Animator.StringToHash("JumpDown");
    private static readonly int FallDownStateHash = Animator.StringToHash("FallDown");
    private static readonly int GetUpStateHash = Animator.StringToHash("GetUp");

    [Header("Movement Audio")]
    [SerializeField] private float movementAudioVolume = 0.55f;
    [SerializeField] private float movementAudioMinSpeed = 0.15f;

    [Header("Obstacle Impact")]
    [SerializeField] private bool enableObstacleImpactRecovery = true;
    [SerializeField] private float stumbleImpactSpeed = 2.4f;
    [SerializeField] private float fallImpactSpeed = 2.8f;
    [SerializeField] private float stumbleRecoveryTime = 0.22f;
    [SerializeField] private float fallDownAnimationTime = 0.85f;
    [SerializeField] private float fallDownAnimatorSpeed = 1.25f;
    [SerializeField] private float getUpAnimationTime = 1.45f;
    [SerializeField] private float fallDownBlendTime = 0.08f;
    [SerializeField] private float getUpBlendTime = 0.3f;
    [SerializeField] private float impactExitBlendTime = 0.22f;
    [SerializeField] private float impactRootMotionScale = 0.0f;
    [SerializeField] private float impactVisualHipsCompensation = 1f;
    [SerializeField] private float impactRecoveryCooldown = 0.9f;
    [SerializeField] private float knockbackForce = 0f;
    [SerializeField] private float heavyObjectMass = 5f;
    [SerializeField] private string stumbleTrigger = "Stumble";
    [SerializeField] private string fallTrigger = "Fall";

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo;

    private Rigidbody rb;
    private PlayerInput playerInput;
    private InputAction sprintAction;
    private AudioSource movementAudioSource;
    private AudioClip walkAudioClip;
    private AudioClip runAudioClip;

    private Vector2 moveInput;
    private bool actionWantsToRun;
    private bool keyboardWantsToRun;
    private bool wantsToRun;
    private bool wantsToJump;

    private float timedSpeedMultiplier = 1f;
    private float timedJumpMultiplier = 1f;
    private float timedSprintSpeedMultiplier = 1f;
    private float zoneSpeedMultiplier = 1f;
    private float timedEffectEndTime;
    private bool recoveringFromImpact;
    private bool applyingImpactRootMotion;
    private bool compensatingImpactVisualOffset;
    private Transform impactVisualRoot;
    private Transform impactHips;
    private Vector3 impactVisualRootBaseLocalPosition;
    private Vector3 impactHipsAnchorPosition;
    private float lastImpactTime;
    private bool wasGroundedLastFrame = true;
    private bool playingJumpUp;
    private bool playingJumpDown;
    private float ignoreGroundForJumpAnimationUntil;
    private float jumpUpAnimationUntil;

    public float CurrentSpeed { get; private set; }
    public bool IsGrounded => (groundSensor != null && groundSensor.IsGrounded) || IsGroundBelow();
    public bool IsRunning => wantsToRun && CurrentSpeed > 0.1f;
    public bool HasMovementInput => moveInput.sqrMagnitude > 0.01f;
    public float EffectiveSpeedMultiplier => timedSpeedMultiplier * zoneSpeedMultiplier;
    private float EffectiveAnimationSpeedMultiplier => EffectiveSpeedMultiplier * (wantsToRun ? timedSprintSpeedMultiplier : 1f);
    public float TimedSpeedRemaining => timedSpeedMultiplier != 1f ? Mathf.Max(0f, timedEffectEndTime - Time.time) : 0f;
    public bool IsRecoveringFromImpact => recoveringFromImpact;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        if (animator != null)
            animator.applyRootMotion = false;

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

        CreateMovementAudio();
    }

    private void OnDestroy()
    {
        if (sprintAction == null) return;

        sprintAction.started -= OnSprintStarted;
        sprintAction.canceled -= OnSprintCanceled;
    }

    private void Update()
    {
        UpdateKeyboardFallbackInput();
        UpdateAnimator();
        UpdateMovementAudio();
    }

    private void LateUpdate()
    {
        CompensateImpactVisualOffset();
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;
        animator.SetFloat(SpeedHash, CurrentSpeed);
        animator.SetFloat(SpeedMultiplierHash, EffectiveAnimationSpeedMultiplier);
        animator.SetBool(IsRunningHash, IsRunning);
        UpdateJumpAnimation();
    }

    private void OnSprintStarted(InputAction.CallbackContext context)
    {
        actionWantsToRun = true;
        UpdateRunIntent();
    }

    private void OnSprintCanceled(InputAction.CallbackContext context)
    {
        actionWantsToRun = false;
        UpdateRunIntent();
    }

    // ─── Input Callbacks ───────────────────

    private void OnMove(InputValue value)
    {
        moveInput = Vector2.ClampMagnitude(value.Get<Vector2>(), 1f);
    }

    private void OnSprint(InputValue value)
    {
        actionWantsToRun = value.isPressed;
        UpdateRunIntent();
    }

    private void OnJump(InputValue value)
    {
        if (value.isPressed)
            wantsToJump = true;
    }

    private void UpdateKeyboardFallbackInput()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        keyboardWantsToRun =
            Keyboard.current.leftShiftKey.isPressed ||
            Keyboard.current.rightShiftKey.isPressed;

        UpdateRunIntent();

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            wantsToJump = true;
        }
    }

    private void UpdateRunIntent()
    {
        wantsToRun = actionWantsToRun || keyboardWantsToRun;
    }

    private bool IsGroundBelow()
    {
        int groundLayer = LayerMask.NameToLayer("Ground");
        int groundMask = groundLayer >= 0 ? 1 << groundLayer : Physics.DefaultRaycastLayers;
        Vector3 checkPosition = transform.position + Vector3.up * fallbackGroundCheckHeight;

        return Physics.CheckSphere(
            checkPosition,
            fallbackGroundCheckRadius,
            groundMask,
            QueryTriggerInteraction.Ignore
        );
    }

    // ─── Physics ──────────────────────────────────────────────────────────────

    private void FixedUpdate()
    {
        if (recoveringFromImpact)
        {
            StopHorizontalRecoveryVelocity();
            wantsToJump = false;
            return;
        }

        Move();
        ApplyExtraGravity();

        if (wantsToJump && IsGrounded)
            Jump();

        wantsToJump = false;
    }

    private void Move()
    {
        float effectiveMultiplier = timedSpeedMultiplier * zoneSpeedMultiplier;
        float baseSpeed = wantsToRun ? runSpeed * timedSprintSpeedMultiplier : walkSpeed;
        float targetSpeed = baseSpeed * effectiveMultiplier;

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

    private void ApplyExtraGravity()
    {
        Vector3 velocity = rb.linearVelocity;
        if (!IsGrounded)
        {
            rb.AddForce(Vector3.down * extraFallGravity, ForceMode.Acceleration);
        }

        float currentMaxUpwardVelocity = maxUpwardVelocity * timedJumpMultiplier;
        if (velocity.y > currentMaxUpwardVelocity)
        {
            rb.linearVelocity = new Vector3(velocity.x, currentMaxUpwardVelocity, velocity.z);
        }
    }

    private void DampMovementDuringRecovery()
    {
        Vector3 velocity = rb.linearVelocity;
        Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);
        horizontal = Vector3.MoveTowards(horizontal, Vector3.zero, acceleration * 0.5f * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector3(horizontal.x, velocity.y, horizontal.z);
        CurrentSpeed = horizontal.magnitude;
    }

    private void StopHorizontalRecoveryVelocity()
    {
        Vector3 velocity = rb.linearVelocity;
        rb.linearVelocity = new Vector3(0f, velocity.y, 0f);
        CurrentSpeed = 0f;
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

        rb.AddForce(Vector3.up * jumpForce * timedJumpMultiplier, ForceMode.Impulse);
        ignoreGroundForJumpAnimationUntil = Time.time + 0.12f;
        jumpUpAnimationUntil = Time.time + jumpUpAnimationTime;
        PlayAnimatorState(JumpUpStateHash, 0.08f);
        playingJumpUp = true;
        playingJumpDown = false;

        if (groundSensor != null)
            groundSensor.ResetContacts();
    }

    private void UpdateJumpAnimation()
    {
        bool grounded = IsGrounded && Time.time >= ignoreGroundForJumpAnimationUntil;
        float verticalSpeed = rb != null ? rb.linearVelocity.y : 0f;

        if (!grounded)
        {
            if (verticalSpeed <= 0.05f && !playingJumpDown)
            {
                PlayAnimatorState(JumpDownStateHash, 0.08f);
                playingJumpDown = true;
                playingJumpUp = false;
            }
            else if (playingJumpUp && Time.time >= jumpUpAnimationUntil)
            {
                playingJumpUp = false;
                PlayAnimatorState(GetLocomotionStateHash(), 0.12f);
            }
        }
        else if (!wasGroundedLastFrame || playingJumpUp || playingJumpDown)
        {
            playingJumpUp = false;
            playingJumpDown = false;
            PlayAnimatorState(GetLocomotionStateHash(), 0.12f);
        }

        wasGroundedLastFrame = grounded;
    }

    private int GetLocomotionStateHash()
    {
        if (!HasMovementInput)
        {
            return IdleStateHash;
        }

        return IsRunning ? RunStateHash : WalkStateHash;
    }

    private void PlayAnimatorState(int stateHash, float transitionDuration)
    {
        if (animator != null && animator.HasState(0, stateHash))
        {
            animator.CrossFadeInFixedTime(stateHash, transitionDuration);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!enableObstacleImpactRecovery)
        {
            return;
        }

        if (recoveringFromImpact || Time.time - lastImpactTime < impactRecoveryCooldown)
        {
            return;
        }

        if (collision.contactCount == 0 || !IsHardImpactCollider(collision.collider))
        {
            return;
        }

        float impactSpeed = Mathf.Max(CurrentSpeed, collision.relativeVelocity.magnitude);
        if (!IsHighSpeedFallImpact(impactSpeed))
        {
            return;
        }

        Vector3 moveDirection = GetCameraRelativeMoveDirection();
        Vector3 obstacleDirection = (collision.GetContact(0).point - transform.position).normalized;
        float approach = Vector3.Dot(new Vector3(moveDirection.x, 0f, moveDirection.z), new Vector3(obstacleDirection.x, 0f, obstacleDirection.z));
        if (approach < 0.35f)
        {
            return;
        }

        Vector3 hitDirection = (transform.position - collision.GetContact(0).point).normalized;
        if (hitDirection.sqrMagnitude < 0.01f)
        {
            hitDirection = -transform.forward;
        }

        StartCoroutine(RecoverFromObstacleImpact(hitDirection, impactSpeed));
    }

    private bool IsHardImpactCollider(Collider hitCollider)
    {
        if (hitCollider.GetComponentInParent<PhysicsObstaclePiece>() != null)
        {
            return true;
        }

        Rigidbody hitBody = hitCollider.attachedRigidbody;
        if (hitBody == null)
        {
            return !hitCollider.isTrigger && !hitCollider.CompareTag("Player");
        }

        return hitBody.mass >= heavyObjectMass || hitBody.isKinematic;
    }

    private bool IsHighSpeedFallImpact(float impactSpeed)
    {
        if (impactSpeed < fallImpactSpeed)
        {
            return false;
        }

        return wantsToRun || timedSpeedMultiplier > 1.01f || timedSprintSpeedMultiplier > 1.01f;
    }

    private IEnumerator RecoverFromObstacleImpact(Vector3 hitDirection, float impactSpeed)
    {
        recoveringFromImpact = true;
        lastImpactTime = Time.time;
        wantsToJump = false;

        Vector3 velocity = rb.linearVelocity;
        velocity.x = 0f;
        velocity.z = 0f;
        rb.linearVelocity = velocity;

        Vector3 knockback = new Vector3(hitDirection.x, 0f, hitDirection.z).normalized;
        if (knockbackForce > 0f && knockback.sqrMagnitude > 0.01f)
        {
            rb.AddForce(knockback * knockbackForce * Mathf.Clamp(impactSpeed, 1f, 3f), ForceMode.Impulse);
        }

        applyingImpactRootMotion = true;
        if (animator != null)
            animator.applyRootMotion = true;

        BeginImpactVisualCompensation();
        PlayFallAnimation();
        yield return WaitForImpactAnimation(fallDownAnimationTime);

        if (animator != null)
            animator.speed = 1f;

        PlayAnimatorState(GetUpStateHash, getUpBlendTime);
        yield return WaitForImpactAnimation(getUpAnimationTime);

        applyingImpactRootMotion = false;
        if (animator != null)
            animator.applyRootMotion = false;

        EndImpactVisualCompensation();
        recoveringFromImpact = false;
        PlayAnimatorState(GetLocomotionStateHash(), impactExitBlendTime);
    }

    private IEnumerator WaitForImpactAnimation(float duration)
    {
        float timer = 0f;
        while (timer < duration)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            timer += Time.deltaTime;
            yield return null;
        }
    }

    private void OnAnimatorMove()
    {
        if (animator == null || rb == null || !applyingImpactRootMotion)
        {
            return;
        }

        Vector3 rootDelta = animator.deltaPosition * impactRootMotionScale;
        Vector3 horizontalDelta = new Vector3(rootDelta.x, 0f, rootDelta.z);
        rb.MovePosition(rb.position + horizontalDelta);
    }

    private void BeginImpactVisualCompensation()
    {
        if (animator == null || impactVisualHipsCompensation <= 0f)
        {
            compensatingImpactVisualOffset = false;
            return;
        }

        impactVisualRoot = animator.transform;
        impactHips = animator.GetBoneTransform(HumanBodyBones.Hips);
        if (impactVisualRoot == null || impactHips == null)
        {
            compensatingImpactVisualOffset = false;
            return;
        }

        impactVisualRootBaseLocalPosition = impactVisualRoot.localPosition;
        impactHipsAnchorPosition = impactHips.position;
        compensatingImpactVisualOffset = true;
    }

    private void CompensateImpactVisualOffset()
    {
        if (!compensatingImpactVisualOffset || impactVisualRoot == null || impactHips == null)
        {
            return;
        }

        impactVisualRoot.localPosition = impactVisualRootBaseLocalPosition;

        Vector3 correction = impactHipsAnchorPosition - impactHips.position;
        correction.y = 0f;
        impactVisualRoot.position += correction * impactVisualHipsCompensation;
    }

    private void EndImpactVisualCompensation()
    {
        if (impactVisualRoot != null)
        {
            impactVisualRoot.localPosition = impactVisualRootBaseLocalPosition;
        }

        compensatingImpactVisualOffset = false;
        impactVisualRoot = null;
        impactHips = null;
    }

    private void PlayFallAnimation()
    {
        if (animator != null && animator.HasState(0, FallDownStateHash))
        {
            animator.speed = fallDownAnimatorSpeed;
            PlayAnimatorState(FallDownStateHash, fallDownBlendTime);
            return;
        }

        if (animator != null)
            animator.speed = 1f;

        TriggerImpactAnimation(true);
    }

    private void TriggerImpactAnimation(bool fall)
    {
        if (animator == null)
        {
            return;
        }

        string triggerName = fall ? fallTrigger : stumbleTrigger;
        if (HasAnimatorParameter(triggerName, AnimatorControllerParameterType.Trigger))
        {
            animator.SetTrigger(triggerName);
        }
    }

    private bool HasAnimatorParameter(string parameterName, AnimatorControllerParameterType parameterType)
    {
        if (string.IsNullOrEmpty(parameterName))
        {
            return false;
        }

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.name == parameterName && parameter.type == parameterType)
            {
                return true;
            }
        }

        return false;
    }

    private void CreateMovementAudio()
    {
        walkAudioClip = Resources.Load<AudioClip>("Audio/FootstepsSoundEffect");
        runAudioClip = Resources.Load<AudioClip>("Audio/RunningSoundEffect");

        movementAudioSource = gameObject.AddComponent<AudioSource>();
        movementAudioSource.loop = true;
        movementAudioSource.playOnAwake = false;
        movementAudioSource.spatialBlend = 0f;
        movementAudioSource.volume = movementAudioVolume;
    }

    private void UpdateMovementAudio()
    {
        if (movementAudioSource == null)
        {
            return;
        }

        bool shouldPlay = Time.timeScale > 0f && GameUIBootstrap.EffectsEnabled && IsGrounded && CurrentSpeed > movementAudioMinSpeed && HasMovementInput;
        AudioClip targetClip = wantsToRun ? runAudioClip : walkAudioClip;

        if (!shouldPlay || targetClip == null)
        {
            if (movementAudioSource.isPlaying)
            {
                movementAudioSource.Stop();
            }

            return;
        }

        if (movementAudioSource.clip != targetClip)
        {
            movementAudioSource.clip = targetClip;
            movementAudioSource.Play();
        }
        else if (!movementAudioSource.isPlaying)
        {
            movementAudioSource.Play();
        }

        movementAudioSource.volume = movementAudioVolume * GameUIBootstrap.EffectsVolume;
        movementAudioSource.mute = !GameUIBootstrap.EffectsEnabled;
    }

    // ─── Speed Effects ────────────────────────────────────────────────────────

    public void ApplyTimedSpeedEffect(float multiplier, float duration)
    {
        StopCoroutine(nameof(TimedSpeedRoutine));
        StartCoroutine(TimedSpeedRoutine(multiplier, duration));
    }

    public void ApplyTimedJumpEffect(float multiplier, float duration)
    {
        StopCoroutine(nameof(TimedJumpRoutine));
        StartCoroutine(TimedJumpRoutine(multiplier, duration));
    }

    public void ApplyTimedSprintSpeedEffect(float multiplier, float duration)
    {
        StopCoroutine(nameof(TimedSprintSpeedRoutine));
        StartCoroutine(TimedSprintSpeedRoutine(multiplier, duration));
    }

    private System.Collections.IEnumerator TimedSpeedRoutine(float multiplier, float duration)
    {
        timedSpeedMultiplier = multiplier;
        timedEffectEndTime = Time.time + duration;
        yield return new WaitForSeconds(duration);
        timedSpeedMultiplier = 1f;
    }

    private System.Collections.IEnumerator TimedJumpRoutine(float multiplier, float duration)
    {
        timedJumpMultiplier = multiplier;
        yield return new WaitForSeconds(duration);
        timedJumpMultiplier = 1f;
    }

    private System.Collections.IEnumerator TimedSprintSpeedRoutine(float multiplier, float duration)
    {
        timedSprintSpeedMultiplier = multiplier;
        yield return new WaitForSeconds(duration);
        timedSprintSpeedMultiplier = 1f;
    }

    public void SetZoneSpeedEffect(float multiplier)
    {
        zoneSpeedMultiplier = multiplier;
    }
}
