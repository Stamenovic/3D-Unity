using UnityEngine;
using UnityEngine.InputSystem;

public class CameraOrbitTarget : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform player;
    [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1.3f, 0f);

    [Header("Mouse Look")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float minPitch = -25f;
    [SerializeField] private float maxPitch = 60f;

    [Header("Cursor")]
    [SerializeField] private bool lockCursor = true;

    private float yaw;
    private float pitch;
    private InputAction lookAction;

    private void Awake()
    {
        var playerInput = FindFirstObjectByType<PlayerInput>();
        if (playerInput != null)
            lookAction = playerInput.actions["Look"];
    }

    private void Start()
    {
        if (player != null)
        {
            transform.position = player.position + targetOffset;
            yaw = transform.eulerAngles.y;
        }

        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    // ─── Camera ───────────────────────────────────────────────────────────────

    private void LateUpdate()
    {
        if (player == null) return;

        if (LevelManager.Instance != null && LevelManager.Instance.IsPaused)
            return;

        FollowPlayerPosition();
        RotateWithLookInput();
    }

    private void FollowPlayerPosition()
    {
        transform.position = player.position + targetOffset;
    }

    private void RotateWithLookInput()
    {
        Vector2 lookInput = lookAction?.ReadValue<Vector2>() ?? Vector2.zero;

        yaw += lookInput.x * mouseSensitivity;
        pitch -= lookInput.y * mouseSensitivity;

        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }
}