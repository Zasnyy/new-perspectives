using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Manages the active gameplay perspective and its camera.
/// </summary>
public class PerspectiveManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInputReader inputReader;
    [SerializeField] private PlayerMovementController playerMovement;
    [SerializeField] private CameraOrbitController cameraOrbitController;
    [SerializeField] private CameraFollow cameraFollow2D;

    [Header("Cameras")]
    [SerializeField] private GameObject camera3D;
    [SerializeField] private GameObject camera2D;

    [Header("Transition")]
    [SerializeField] private CanvasGroup transitionOverlay;
    [SerializeField] private float fadeDuration = 0.15f;

    [Header("Settings")]
    [SerializeField]
    private PerspectiveMode startingPerspective = PerspectiveMode.TwoD;

    [Header("Cursor")]
    [SerializeField] private bool lockCursorDuringGameplay = true;

    public event Action<PerspectiveMode> PerspectiveChanged;

    public PerspectiveMode CurrentPerspective { get; private set; }

    private bool isTransitioning;

    private void Awake()
    {
        if (cameraFollow2D == null && camera2D != null)
        {
            cameraFollow2D = camera2D.GetComponent<CameraFollow>();
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        if (playerMovement == null)
        {
            Debug.LogError("Player Movement has not been assigned.", this);
            enabled = false;
            return;
        }

        // Initialize the requested starting perspective
        if (!playerMovement.TrySetPerspective(startingPerspective))
        {
            Debug.LogWarning(
                "Starting perspective is blocked. Defaulting to 2D.",
                this);

            startingPerspective = PerspectiveMode.TwoD;
        }

        CurrentPerspective = startingPerspective;

        ApplyCameraState(CurrentPerspective);

        // Ensure the starting camera is positioned before rendering
        if (cameraFollow2D != null && 
            CurrentPerspective == PerspectiveMode.TwoD)
        {
            cameraFollow2D.SnapToTarget();
        }

        SetCursorLocked(lockCursorDuringGameplay);
    }

    // Update is called once per frame
    private void Update()
    {
        if (isTransitioning || inputReader == null)
        {
            return;
        }

        if (inputReader.SwitchPressedThisFrame)
        {
            SwitchPerspective();
        }
    }

    /// <summary>
    /// Requests a switch to the opposite perspective.
    /// </summary>
    public void SwitchPerspective()
    {
        if (isTransitioning)
        {
            return;
        }

        PerspectiveMode nextPerspective =
            CurrentPerspective == PerspectiveMode.ThreeD
                ? PerspectiveMode.TwoD
                : PerspectiveMode.ThreeD;

        StartCoroutine(SwitchToPerspective(nextPerspective));
    }

    /// <summary>
    /// Handles a perspective change and its visual transition.
    /// </summary>
    /// <param name="mode">The perspective to switch to.</param>
    /// <returns></returns>
    private IEnumerator SwitchToPerspective(PerspectiveMode mode)
    {
        // Reject the switch if the destination is blocked
        if (!playerMovement.CanSetPerspective(mode))
        {
            Debug.Log("Perspective switch blocked by geometry.");
            yield break;
        }

        isTransitioning = true;

        playerMovement.SetMovementLocked(true);

        if (cameraOrbitController != null)
        {
            cameraOrbitController.SetLookEnabled(false);
        }

        // Fade the screen out
        yield return FadeTo(1f);

        // Change the player position and perspective
        if (playerMovement.TrySetPerspective(mode))
        {
            CurrentPerspective = mode;

            ApplyCameraState(mode);

            PerspectiveChanged?.Invoke(mode);
        }
        else
        {
            Debug.LogWarning("Perspective switch failed during transition.");

            ApplyCameraState(CurrentPerspective);
        }

        // Fade the screen back in
        yield return FadeTo(0f);

        playerMovement.SetMovementLocked(false);

        isTransitioning = false;
    }

    /// <summary>
    /// Activates the camera associated with the given perspective.
    /// </summary>
    /// <param name="mode">The perspective being activated.</param>
    private void ApplyCameraState(PerspectiveMode mode)
    {
        bool use3D = mode == PerspectiveMode.ThreeD;

        if (camera3D != null)
        {
            camera3D.SetActive(use3D);
        }

        if (camera2D != null)
        {
            camera2D.SetActive(!use3D);
        }

        if (cameraOrbitController != null)
        {
            cameraOrbitController.SetLookEnabled(use3D);
        }

        if (!use3D && cameraFollow2D != null)
        {
            cameraFollow2D.SnapToTarget();
        }
    }

    /// <summary>
    /// Fades the transition overlay to the requested opacity.
    /// </summary>
    /// <param name="targetAlpha">The desired overlay opacity.</param>
    /// <returns></returns>
    private IEnumerator FadeTo(float targetAlpha)
    {
        if (transitionOverlay == null)
        {
            yield break;
        }

        float startAlpha = transitionOverlay.alpha;

        if (fadeDuration <= 0f)
        {
            transitionOverlay.alpha = targetAlpha;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(elapsed / fadeDuration);

            transitionOverlay.alpha = Mathf.Lerp(
                startAlpha,
                targetAlpha,
                progress);

            yield return null;
        }

        transitionOverlay.alpha = targetAlpha;
    }

    /// <summary>
    /// Updates the cursor's visibility and lock state.
    /// </summary>
    /// <param name="locked">Whether the cursor should be locked.</param>
    public void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked
            ? CursorLockMode.Locked
            : CursorLockMode.None;

        Cursor.visible = !locked;
    }

    /// <summary>
    /// Updates cursor state when the application gains or loses focus.
    /// </summary>
    /// <param name="focus">Whether the application currently has focus.</param>
    private void OnApplicationFocus(bool focus)
    {
        if (lockCursorDuringGameplay)
        {
            SetCursorLocked(focus);
        }
    }
}
