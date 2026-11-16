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
        if (!lookAtTarget)
        {
            return;
        }

        Vector3 lookDirection = target.position + lookAtOffset - transform.position;

        if (lookDirection.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(lookDirection, Vector3.up);
        }
    }
}
