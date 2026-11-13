using UnityEngine;

/// <summary>
/// Controls player movement in both 2D and 3D perspectives.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerMovementController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInputReader inputReader;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform twoDPlane;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float jumpHeight = 1.25f;
    [SerializeField] private float turnSpeed = 12f;

    [Header("Perspective Switching")]
    [SerializeField] private LayerMask perspectiveObstacleMask;
    [SerializeField] private float switchClearance = 0.01f;

    private CharacterController characterController;
    private PerspectiveMode currentPerspective = PerspectiveMode.ThreeD;

    private float verticalVelocity;
    private float saved3DDepth;
    private bool movementLocked;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        if (inputReader == null)
        {
            inputReader = GetComponent<PlayerInputReader>();
        }

        // Preserve the initial depth in case the game starts in 2D
        if (twoDPlane != null)
        {
            saved3DDepth = GetDepth(transform.position);
        }
    }

    // Update is called once per frame
    private void Update()
    {
        if (inputReader == null || movementLocked)
        {
            return;
        }

        Vector2 input = inputReader.MoveInput;

        // Get movement based on the active perspective
        Vector3 movement = currentPerspective == PerspectiveMode.TwoD
            ? GetTwoDMovement(input)
            : GetThreeDMovement(input);

        movement = Vector3.ClampMagnitude(movement, 1f);

        // Maintain a small downward velocity while grounded
        bool isGrounded = characterController.isGrounded;

        if (isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        // Apply jumping in either perspective
        if (isGrounded && inputReader.JumpPressedThisFrame)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // Apply gravity manually
        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = movement * moveSpeed;
        velocity.y = verticalVelocity;

        characterController.Move(velocity * Time.deltaTime);

        // Rotate the character toward the direction of movement
        RotateTowardsMovement(movement);
    }

    /// <summary>
    /// Checks whether the player can safely enter a perspective.
    /// </summary>
    /// <param name="mode">The perspective to check.</param>
    /// <returns>True if the player can safely enter the perspective.</returns>
    public bool CanSetPerspective(PerspectiveMode mode)
    {
        if (mode == currentPerspective)
        {
            return true;
        }

        if (twoDPlane == null)
        {
            Debug.LogError("The 2D movement plane has not be assigned.", this);
            return false;
        }

        Vector3 targetPosition = GetPerspectivePosition(mode);

        return IsCapsuleClearAt(targetPosition);
    }

    /// <summary>
    /// Attempts to change the player's active perspective.
    /// </summary>
    /// <param name="mode">The perspective to switch to.</param>
    /// <returns>True if the perspective change succeeds.</returns>
    public bool TrySetPerspective(PerspectiveMode mode)
    {
        if (mode == currentPerspective)
        {
            return true;
        }

        if (!CanSetPerspective(mode))
        {
            return false;
        }

        Vector3 targetPosition = GetPerspectivePosition(mode);

        // Save the current depth before projecting onto the 2D plane
        if (mode == PerspectiveMode.TwoD)
        {
            saved3DDepth = GetDepth(transform.position);
        }

        SetPlayerPosition(targetPosition);

        currentPerspective = mode;

        return true;
    }

    /// <summary>
    /// Calculates the player's destination for a perspective.
    /// </summary>
    /// <param name="mode">The perspective being entered.</param>
    /// <returns>The destination position in world space.</returns>
    private Vector3 GetPerspectivePosition(PerspectiveMode mode)
    {
        Vector3 planeNormal = twoDPlane.forward.normalized;

        Vector3 offset = transform.position - twoDPlane.position;

        Vector3 positionOnPlane = Vector3.ProjectOnPlane(
            offset,
            planeNormal);

        if (mode == PerspectiveMode.TwoD)
        {
            return twoDPlane.position + positionOnPlane;
        }

        // Restore the saved depth while keeping the current X and Y
        return twoDPlane.position + positionOnPlane
            + planeNormal * saved3DDepth;
    }

    /// <summary>
    /// Gets the player's signed distance from the 2D plane.
    /// </summary>
    /// <param name="position">The world position to measure.</param>
    /// <returns>The signed depth relative to the 2D plane.</returns>
    private float GetDepth(Vector3 position)
    {
        Vector3 planeNormal = twoDPlane.forward.normalized;

        return Vector3.Dot(
            position - twoDPlane.position,
            planeNormal);
    }

    /// <summary>
    /// Checks whether the player's capsule fits at a specified position.
    /// </summary>
    /// <param name="position">The root position being tested.</param>
    /// <returns>True if no blocking geometry overlaps the capsule.</returns>
    private bool IsCapsuleClearAt(Vector3 position)
    {
        if (perspectiveObstacleMask.value == 0)
        {
            Debug.LogError("Perspective Obstacle Mask is empty.", this);
            return false;
        }

        Vector3 center = position + transform.rotation *
            characterController.center;

        float radius = characterController.radius;

        float halfSegment = Mathf.Max(
            0f,
            characterController.height * 0.5f - radius);

        Vector3 point1 = center + transform.up * halfSegment;
        Vector3 point2 = center - transform.up * halfSegment;

        float checkRadius = Mathf.Max(0.01f, radius - switchClearance);

        Physics.SyncTransforms();

        Collider[] overlaps = Physics.OverlapCapsule(
            point1,
            point2,
            checkRadius,
            perspectiveObstacleMask,
            QueryTriggerInteraction.Ignore);

        foreach (Collider overlap in overlaps)
        {
            // Ignore the player and any of its child colliders
            if (overlap.transform == transform ||
                overlap.transform.IsChildOf(transform))
            {
                continue;
            }

            Debug.Log(
                $"Perspective switch blocked by: {overlap.name}",
                overlap);

            return false;
        }

        return true;
    }

    /// <summary>
    /// Moves the player to a validated destination position.
    /// </summary>
    /// <param name="position">The destination world position.</param>
    private void SetPlayerPosition(Vector3 position)
    {
        bool wasEnabled = characterController.enabled;

        characterController.enabled = false;
        transform.position = position;
        characterController.enabled = wasEnabled;

        Physics.SyncTransforms();
    }

    /// <summary>
    /// Calculates movement along the horizontal axis of the 2D plane.
    /// </summary>
    /// <param name="input">The player's movement input.</param>
    /// <returns>A world-space movement vector constrained to the 2D plane.</returns>
    private Vector3 GetTwoDMovement(Vector2 input)
    {
        Vector3 planeRight = Vector3.ProjectOnPlane(
            twoDPlane.right,
            Vector3.up).normalized;

        return planeRight * input.x;
    }

    /// <summary>
    /// Calculates movement relative to the 3D camera's orientation.
    /// </summary>
    /// <param name="input">The player's movement input.</param>
    /// <returns>A camera-relative movement vector in world space.</returns>
    private Vector3 GetThreeDMovement(Vector2 input)
    {
        Vector3 cameraForward = cameraTransform != null
            ? cameraTransform.forward
            : transform.forward;

        Vector3 forward = Vector3.ProjectOnPlane(
            cameraForward,
            Vector3.up).normalized;

        if (forward.sqrMagnitude < 0.001f)
        {
            forward = Vector3.forward;
        }

        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

        return (right * input.x) + (forward * input.y);
    }

    /// <summary>
    /// Rotates the player toward the current movement direction.
    /// </summary>
    /// <param name="movement">The current movement direction.</param>
    private void RotateTowardsMovement(Vector3 movement)
    {
        movement.y = 0f;

        if (movement.sqrMagnitude < 0.001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(
            movement,
            Vector3.up);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            turnSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Enables or disables player movement input.
    /// </summary>
    /// <param name="locked">Whether movement should be locked.</param>
    public void SetMovementLocked(bool locked)
    {
        movementLocked = locked;
    }
}
