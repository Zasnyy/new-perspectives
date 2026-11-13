using UnityEngine;

/// <summary>
/// Controls a third-person camera that orbits around the player.
/// </summary>
public class CameraOrbitController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform target;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private PlayerInputReader inputReader;

    [Header("Camera Settings")]
    [SerializeField] private float distance = 6f;
    [SerializeField] private float initialPitch = 15f;
    [SerializeField] private float minPitch = -10f;
    [SerializeField] private float maxPitch = 65f;

    [Header("Sensitivity")]
    [SerializeField] private float mouseSensitivity = 0.12f;
    [SerializeField] private bool invertY = false;

    private float yaw;
    private float pitch;

    private bool lookEnabled = true;

    private void Awake()
    {
        yaw = transform.eulerAngles.y;
        pitch = Mathf.Clamp(initialPitch, minPitch, maxPitch);
    }

    private void LateUpdate()
    {
        if (target == null || cameraTransform == null)
        {
            return;
        }

        // Keep the camera rig positioned at the player's camera target
        transform.position = target.position;

        HandleCameraRotation();
        HandleCameraPosition();
    }

    /// <summary>
    /// Processes mouse input to rotate the camera.
    /// </summary>
    private void HandleCameraRotation()
    {
        if (!lookEnabled || inputReader == null)
        {
            return;
        }

        Vector2 lookInput = inputReader.LookInput;

        yaw += lookInput.x * mouseSensitivity;

        float verticalInput = invertY
            ? lookInput.y
            : -lookInput.y;

        pitch += verticalInput * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    /// <summary>
    /// Positions the camera behind the camera rig.
    /// </summary>
    private void HandleCameraPosition()
    {
        cameraTransform.localPosition = new Vector3(0f, 0f, -distance);
        cameraTransform.localRotation = Quaternion.identity;
    }

    /// <summary>
    /// Enables or disables camera rotation input.
    /// </summary>
    /// <param name="enabled">Whether camera rotation is enabled.</param>
    public void SetLookEnabled(bool enabled)
    {
        lookEnabled = enabled;
    }
}
