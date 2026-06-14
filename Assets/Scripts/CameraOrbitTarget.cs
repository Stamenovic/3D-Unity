using UnityEngine;

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

    private void LateUpdate()
    {
        if (player == null)
        {
            return;
        }

        FollowPlayerPosition();
        RotateWithMouse();
    }

    private void FollowPlayerPosition()
    {
        transform.position = player.position + targetOffset;
    }

    private void RotateWithMouse()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        yaw += mouseX * mouseSensitivity;
        pitch -= mouseY * mouseSensitivity;

        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }
}