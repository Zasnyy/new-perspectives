
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class EnemyMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform twoDPlane;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float turnSpeed = 10f;
    [SerializeField] private float gravity = -20f;

    [Header("2D Movement")]
    [SerializeField] private float depthCorrectionSpeed = 2.5f;
    [SerializeField] private float depthTolerance = 0.15f;

    private CharacterController controller;

    private PerspectiveMode currentPerspective =
        PerspectiveMode.ThreeD;

    private float verticalVelocity;
    private bool movementEnabled = true;

    public PerspectiveMode CurrentPerspective => currentPerspective;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        ApplyGravity();
    }

    public void SetPerspective(PerspectiveMode mode)
    {
        currentPerspective = mode;
    }

    public void MoveTowards(Transform target)
    {
        if (!movementEnabled || target == null)
            return;

        Vector3 direction;

        if (currentPerspective == PerspectiveMode.TwoD)
        {
            direction = Get2DMovementDirection(target);
        }
        else
        {
            direction = Get3DMovementDirection(target);
        }

        direction = Vector3.ClampMagnitude(direction, 1f);

        Vector3 velocity = direction * moveSpeed;


        velocity.y = 0f;

        controller.Move(velocity * Time.deltaTime);

        RotateTowards(direction);
    }

    private Vector3 Get2DMovementDirection(Transform target)
    {
        if (twoDPlane == null)
        {
            Debug.LogError("EnemyMovement: 2D Plane not assigned.", this);
            return Vector3.zero;
        }

        Vector3 planeNormal = twoDPlane.forward.normalized;

        Vector3 planeRight = Vector3.ProjectOnPlane(
            twoDPlane.right,
            Vector3.up).normalized;

        // Calculate the player's position projected onto the 2D plane.
        Vector3 targetOffset = target.position - twoDPlane.position;

        Vector3 projectedTarget = twoDPlane.position +
            Vector3.ProjectOnPlane(targetOffset, planeNormal);

        // Only move along the 2D horizontal axis.
        Vector3 planarDifference =
            projectedTarget - transform.position;

        float horizontalDirection = Vector3.Dot(
            planarDifference,
            planeRight);

        Vector3 movement = planeRight *
            Mathf.Sign(horizontalDirection);

        // Move toward the plane if the enemy is too far away from it.
        float currentDepth = Vector3.Dot(
            transform.position - twoDPlane.position,
            planeNormal);

        if (Mathf.Abs(currentDepth) > depthTolerance)
        {
            movement += -planeNormal *
                Mathf.Sign(currentDepth) *
                depthCorrectionSpeed / moveSpeed;
        }

        return movement;
    }

    private Vector3 Get3DMovementDirection(Transform target)
    {
        Vector3 direction = target.position - transform.position;

        // Keep horizontal movement on the ground.
        direction.y = 0f;

        return direction.normalized;
    }

    private void ApplyGravity()
    {
        if (!controller.enabled)
            return;

        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        verticalVelocity += gravity * Time.deltaTime;

        // Gravity is applied separately from horizontal movement.
        controller.Move(
            Vector3.up * verticalVelocity * Time.deltaTime);
    }

    private void RotateTowards(Vector3 direction)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            turnSpeed * Time.deltaTime);
    }

    public void FaceTarget(Transform target)
    {
        if (target == null)
            return;

        Vector3 direction = target.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        transform.rotation = Quaternion.LookRotation(direction);
    }
    public float Get2DPlanarDistance(Transform target)
{
    if (twoDPlane == null || target == null)
        return Mathf.Infinity;

    Vector3 normal = twoDPlane.forward.normalized;

    Vector3 difference = target.position - transform.position;

    return Vector3.ProjectOnPlane(difference, normal).magnitude;
}

    public void SetMovementEnabled(bool enabled)
    {
        movementEnabled = enabled;
    }

    public float GetDepthDifference(Transform target)
    {
        if (twoDPlane == null || target == null)
            return 0f;

        Vector3 normal = twoDPlane.forward.normalized;

        float enemyDepth = Vector3.Dot(
            transform.position - twoDPlane.position,
            normal);

        float targetDepth = Vector3.Dot(
            target.position - twoDPlane.position,
            normal);

        return Mathf.Abs(enemyDepth - targetDepth);
    }
}
