using UnityEngine;

/// <summary>
/// Makes a camera follow a target using a world-space offset.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform target;

    [Header("Settings")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 0f, -10f);
    [SerializeField] private float followSharpness = 10f;
    [SerializeField] private bool lookAtTarget = true;
    [SerializeField] private Vector3 lookAtOffset = new Vector3(0f, 1f, 0f);

    /// <summary>
    /// Gets the transform followed by this camera.
    /// </summary>
    public Transform FollowTarget => target;

    private void Awake()
    {
        SnapToTarget();
    }

    private void OnEnable()
    {
        SnapToTarget();
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 desiredPosition = target.position + worldOffset;

        float blend = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);

        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            blend);

        UpdateLookRotation();
    }

    /// <summary>
    /// Immediately positions the camera at its target.
    /// </summary>
    public void SnapToTarget()
    {
        if (target == null)
        {
            return;
        }

        transform.position = target.position + worldOffset;

        UpdateLookRotation();
    }

    /// <summary>
    /// Updates the camera's rotation if it should look at its target.
    /// </summary>
    private void UpdateLookRotation()
    {
        if (!lookAtTarget || !TryGetFocusPosition(out Vector3 focusPosition))
        {
            return;
        }

        Vector3 lookDirection = focusPosition - transform.position;

        if (lookDirection.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(
                lookDirection,
                Vector3.up);
        }
    }

    /// <summary>
    /// Gets the point used to aim the camera, including its look-at offset.
    /// </summary>
    /// <param name="focusPosition">The world-space focus point.</param>
    /// <returns>True when a target has been assigned.</returns>
    public bool TryGetFocusPosition(out Vector3 focusPosition)
    {
        if (target == null)
        {
            focusPosition = Vector3.zero;
            return false;
        }

        focusPosition = target.position + lookAtOffset;
        return true;
    }

    /// <summary>
    /// Assigns the target followed by the camera and immediately snaps to it.
    /// </summary>
    /// <param name="newTarget">The player transform to follow.</param>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        SnapToTarget();
    }
}
