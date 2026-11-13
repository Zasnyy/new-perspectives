using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Reads player input and exposes it to other player systems.
/// </summary>
public class PlayerInputReader : MonoBehaviour
{
    [Header("Input Actions")]
    [SerializeField] private InputActionAsset inputActions;

    [SerializeField] private string actionMapName = "Player";
    [SerializeField] private string moveActionName = "Move";
    [SerializeField] private string lookActionName = "Look";
    [SerializeField] private string jumpActionName = "Jump";
    [SerializeField] private string switchActionName = "SwitchPerspective";

    private InputActionMap playerMap;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction switchAction;

    /// <summary>
    /// Gets the current movement input.
    /// </summary>
    public Vector2 MoveInput =>
        moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;

    /// <summary>
    /// Gets the current camera look input.
    /// </summary>
    public Vector2 LookInput =>
        lookAction != null ? lookAction.ReadValue<Vector2>() : Vector2.zero;

    /// <summary>
    /// Gets whether jump was pressed during this frame.
    /// </summary>
    public bool JumpPressedThisFrame =>
        jumpAction != null && jumpAction.WasPressedThisFrame();

    /// <summary>
    /// Gets whether perspective switching was pressed this frame.
    /// </summary>
    public bool SwitchPressedThisFrame =>
        switchAction != null && switchAction.WasPressedThisFrame();

    private void Awake()
    {
        if (inputActions == null)
        {
            Debug.LogError("Player Input Actions have not been assigned.", this);
            enabled = false;
            return;
        }

        playerMap = inputActions.FindActionMap(actionMapName, true);

        moveAction = playerMap.FindAction(moveActionName, true);
        lookAction = playerMap.FindAction(lookActionName, true);
        jumpAction = playerMap.FindAction(jumpActionName, true);
        switchAction = playerMap.FindAction(switchActionName, true);
    }

    private void OnEnable()
    {
        playerMap?.Enable();
    }

    private void OnDisable()
    {
        playerMap?.Disable();
    }
}
