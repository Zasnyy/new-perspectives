using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Fades environment renderers between the 3D camera and the player's focus point.
/// </summary>
[DefaultExecutionOrder(100)]
public class CameraOcclusionFader : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CameraFollow cameraFollow;
    [SerializeField] private Camera viewCamera;

    [Header("Occlusion Detection")]
    [SerializeField] private LayerMask occlusionMask;
    [SerializeField] private float castRadius = 0.2f;
    [SerializeField]
    private QueryTriggerInteraction triggerInteraction =
        QueryTriggerInteraction.Ignore;

    [Header("Fade Settings")]
    [Range(0.05f, 0.9f)]
    [SerializeField] private float fadedOpacity = 0.3f;
    [SerializeField] private float fadeInSpeed = 8f;
    [SerializeField] private float fadeOutSpeed = 6f;

    private const int MaxHitCount = 32;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private readonly RaycastHit[] hitBuffer = new RaycastHit[MaxHitCount];
    private readonly Dictionary<Renderer, RendererFadeState> fadeStates =
        new Dictionary<Renderer, RendererFadeState>();
    private readonly List<Renderer> renderersToCheck = new List<Renderer>(8);
    private readonly List<Renderer> statesToRemove = new List<Renderer>();
    private readonly HashSet<Renderer> warnedRenderers = new HashSet<Renderer>();

    private MaterialPropertyBlock propertyBlock;

    /// <summary>
    /// Stores a renderer's original material colors and current fade state.
    /// </summary>
    private sealed class RendererFadeState
    {
        public Renderer Renderer;
        public int[] ColorPropertyIds;
        public Color[] OriginalColors;
        public float FadeAmount;
        public bool IsObstructing;
    }

    private void Awake()
    {
        if (cameraFollow == null)
        {
            cameraFollow = GetComponent<CameraFollow>();
        }

        if (viewCamera == null)
        {
            viewCamera = GetComponentInChildren<Camera>(true);
        }

        propertyBlock = new MaterialPropertyBlock();

        if (cameraFollow == null || viewCamera == null)
        {
            Debug.LogError(
                "CameraOcclusionFader needs CameraFollow and MainCamera3D references.",
                this);
            enabled = false;
        }
    }

    private void LateUpdate()
    {
        // The 3D camera is disabled while the game is using the 2D perspective
        if (viewCamera == null || !viewCamera.isActiveAndEnabled)
        {
            UpdateFades(false);
            return;
        }

        if (!cameraFollow.TryGetFocusPosition(out Vector3 focusPosition))
        {
            UpdateFades(false);
            return;
        }

        Vector3 cameraPosition = viewCamera.transform.position;
        Vector3 direction = cameraPosition - focusPosition;
        float distance = direction.magnitude;

        // Clear previous obstruction flags before scanning this frame
        foreach (RendererFadeState state in fadeStates.Values)
        {
            state.IsObstructing = false;
        }

        if (distance > 0.01f)
        {
            direction /= distance;

            int hitCount = Physics.SphereCastNonAlloc(
                focusPosition,
                castRadius,
                direction,
                hitBuffer,
                distance,
                occlusionMask,
                triggerInteraction);

            for (int i = 0; i < hitCount; i++)
            {
                Collider hitCollider = hitBuffer[i].collider;

                if (hitCollider == null || IsPlayerCollider(hitCollider))
                {
                    continue;
                }

                TrackHitRenderers(hitCollider);
            }
        }

        UpdateFades(true);
    }

    /// <summary>
    /// Checks whether a hit belongs to the player being followed.
    /// </summary>
    /// <param name="hitCollider">The collider hit by the sphere cast.</param>
    /// <returns>True when the collider belongs to the player hierarchy.</returns>
    private bool IsPlayerCollider(Collider hitCollider)
    {
        Transform player = cameraFollow.FollowTarget;

        if (player == null)
        {
            return false;
        }

        Transform hitTransform = hitCollider.transform;
        return hitTransform == player || hitTransform.IsChildOf(player);
    }

    /// <summary>
    /// Tracks renderers on the hit object, its parents, and its child objects.
    /// </summary>
    /// <param name="hitCollider">The collider that obstructs the camera.</param>
    private void TrackHitRenderers(Collider hitCollider)
    {
        Renderer parentRenderer = hitCollider.GetComponent<Renderer>();

        if (parentRenderer == null)
        {
            parentRenderer = hitCollider.GetComponentInParent<Renderer>();
        }

        if (parentRenderer != null)
        {
            TrackRenderer(parentRenderer);
        }

        renderersToCheck.Clear();
        hitCollider.GetComponentsInChildren(true, renderersToCheck);

        foreach (Renderer targetRenderer in renderersToCheck)
        {
            TrackRenderer(targetRenderer);
        }
    }

    /// <summary>
    /// Adds a renderer to the current frame's set of camera obstructions.
    /// </summary>
    /// <param name="targetRenderer">The renderer to fade.</param>
    private void TrackRenderer(Renderer targetRenderer)
    {
        if (targetRenderer == null || !targetRenderer.enabled)
        {
            return;
        }

        if (!fadeStates.TryGetValue(targetRenderer, out RendererFadeState state))
        {
            state = CreateFadeState(targetRenderer);

            if (state == null)
            {
                return;
            }

            fadeStates.Add(targetRenderer, state);
        }

        state.IsObstructing = true;
    }

    /// <summary>
    /// Stores the original material colors before fading a renderer.
    /// </summary>
    /// <param name="targetRenderer">The renderer that may be faded.</param>
    /// <returns>A new fade state, or null if no compatible color exists.</returns>
    private RendererFadeState CreateFadeState(Renderer targetRenderer)
    {
        Material[] materials = targetRenderer.sharedMaterials;
        int[] colorPropertyIds = new int[materials.Length];
        Color[] originalColors = new Color[materials.Length];
        bool foundFadeableMaterial = false;

        for (int i = 0; i < materials.Length; i++)
        {
            Material material = materials[i];

            if (material == null)
            {
                continue;
            }

            if (material.HasProperty(BaseColorId))
            {
                colorPropertyIds[i] = BaseColorId;
                originalColors[i] = material.GetColor(BaseColorId);
                foundFadeableMaterial = true;
            }
            else if (material.HasProperty(ColorId))
            {
                colorPropertyIds[i] = ColorId;
                originalColors[i] = material.GetColor(ColorId);
                foundFadeableMaterial = true;
            }
        }

        if (!foundFadeableMaterial)
        {
            if (warnedRenderers.Add(targetRenderer))
            {
                Debug.LogWarning(
                    $"'{targetRenderer.name}' has no _BaseColor or _Color property. " +
                    "Use a fade-compatible material for camera occlusion fading.",
                    targetRenderer);
            }

            return null;
        }

        return new RendererFadeState
        {
            Renderer = targetRenderer,
            ColorPropertyIds = colorPropertyIds,
            OriginalColors = originalColors,
            FadeAmount = 0f,
            IsObstructing = false
        };
    }

    /// <summary>
    /// Fades obstructing renderers and restores renderers no longer in the way.
    /// </summary>
    /// <param name="scanWasPerformed">Whether the current frame scanned for hits.</param>
    private void UpdateFades(bool scanWasPerformed)
    {
        if (!scanWasPerformed)
        {
            foreach (RendererFadeState state in fadeStates.Values)
            {
                state.IsObstructing = false;
            }
        }

        statesToRemove.Clear();

        foreach (KeyValuePair<Renderer, RendererFadeState> entry in fadeStates)
        {
            RendererFadeState state = entry.Value;

            if (state.Renderer == null)
            {
                statesToRemove.Add(entry.Key);
                continue;
            }

            float targetFade = state.IsObstructing ? 1f : 0f;
            float fadeSpeed = state.IsObstructing ? fadeInSpeed : fadeOutSpeed;

            state.FadeAmount = Mathf.MoveTowards(
                state.FadeAmount,
                targetFade,
                fadeSpeed * Time.deltaTime);

            ApplyFade(state);

            if (!state.IsObstructing && state.FadeAmount <= 0f)
            {
                statesToRemove.Add(entry.Key);
            }
        }

        foreach (Renderer renderer in statesToRemove)
        {
            fadeStates.Remove(renderer);
        }
    }

    /// <summary>
    /// Applies the current opacity through per-renderer material properties.
    /// </summary>
    /// <param name="state">The saved renderer fade state.</param>
    private void ApplyFade(RendererFadeState state)
    {
        for (int i = 0; i < state.ColorPropertyIds.Length; i++)
        {
            int propertyId = state.ColorPropertyIds[i];

            if (propertyId == 0)
            {
                continue;
            }

            Color color = state.OriginalColors[i];
            color.a *= Mathf.Lerp(1f, fadedOpacity, state.FadeAmount);

            propertyBlock.Clear();
            state.Renderer.GetPropertyBlock(propertyBlock, i);
            propertyBlock.SetColor(propertyId, color);
            state.Renderer.SetPropertyBlock(propertyBlock, i);
        }
    }

    /// <summary>
    /// Restores all tracked renderers when this component is disabled.
    /// </summary>
    private void OnDisable()
    {
        foreach (RendererFadeState state in fadeStates.Values)
        {
            if (state.Renderer == null)
            {
                continue;
            }

            state.FadeAmount = 0f;
            ApplyFade(state);
        }

        fadeStates.Clear();
    }

    private void OnValidate()
    {
        castRadius = Mathf.Max(0.01f, castRadius);
        fadeInSpeed = Mathf.Max(0f, fadeInSpeed);
        fadeOutSpeed = Mathf.Max(0f, fadeOutSpeed);
        fadedOpacity = Mathf.Clamp(fadedOpacity, 0.05f, 0.9f);
    }
}
