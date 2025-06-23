using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>
/// Attaches to any GameObject to convert its mesh renderers (and optional CanvasGroup)
/// into a ghost (transparent) version that can be faded at runtime.
/// </summary>
[DisallowMultipleComponent]
public class GhostHelper : MonoBehaviour
{
    [Header("Ghost Settings")]
    [Tooltip("Starting opacity of the ghost (0 = invisible, 1 = fully opaque).")]
    public float initialOpacity = 0f;

    [Header("Zone Fade Settings")]
    [Tooltip("Opacity to apply when the player enters the zone.")]
    public float enterOpacity = 0.5f;
    [Tooltip("Opacity to apply when the player exits the zone.")]
    public float exitOpacity = 0f;

    private Renderer[] _meshRenderers;
    private Material[][] _clonedMaterials;
    // Store the original shared materials so we can restore them later
    private Material[][] _originalMaterials;
    // Optional CanvasGroup for UI fading
    private CanvasGroup _canvasGroup;

    void Awake()
    {
        // Clone and configure all mesh materials for transparency
        _meshRenderers = GetComponentsInChildren<Renderer>(true);
        _clonedMaterials = new Material[_meshRenderers.Length][];
        _originalMaterials = new Material[_meshRenderers.Length][];

        for (int i = 0; i < _meshRenderers.Length; i++)
        {
            var rend = _meshRenderers[i];
            var originalMats = rend.materials;
            // Cache the original materials
            _originalMaterials[i] = originalMats;
            var newMats = new Material[originalMats.Length];

            for (int j = 0; j < originalMats.Length; j++)
            {
                Material matCopy = new Material(originalMats[j]);
                SetupMaterialWithTransparency(matCopy);
                newMats[j] = matCopy;
            }

            _clonedMaterials[i] = newMats;
            rend.materials = newMats;
        }

        // Find a CanvasGroup in children for UI fading (optional)
        _canvasGroup = GetComponentInChildren<CanvasGroup>(true);
        if (_canvasGroup != null)
            _canvasGroup.alpha = 0f;

        // Apply initial opacity
        SetOpacity(initialOpacity);
    }

    /// <summary>
    /// Sets up a material to support transparency blending.
    /// </summary>
    private void SetupMaterialWithTransparency(Material mat)
    {
        mat.SetFloat("_Mode", 3); // Transparent
        mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = (int)RenderQueue.Transparent;
        mat.SetOverrideTag("RenderType", "Transparent");

        // URP/HDRP Lit support
        if (mat.HasProperty("_Surface"))
        {
            mat.SetFloat("_Surface", 1f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.DisableKeyword("_SURFACE_TYPE_OPAQUE");
        }
    }

    /// <summary>
    /// Set the opacity of the ghost clone materials and optional UI.
    /// </summary>
    public void SetOpacity(float alpha)
    {
        alpha = Mathf.Clamp01(alpha);

        // Fade mesh materials
        foreach (var rend in _meshRenderers)
        {
            // If fully transparent, disable renderer; otherwise enable
            rend.enabled = alpha > 0f;

            foreach (var mat in rend.materials)
            {
                // Standard/Legacy shaders
                if (mat.HasProperty("_Color"))
                {
                    Color c = mat.color;
                    c.a = alpha;
                    mat.color = c;
                }
                // URP/HDRP Lit shaders
                if (mat.HasProperty("_BaseColor"))
                {
                    Color c = mat.GetColor("_BaseColor");
                    c.a = alpha;
                    mat.SetColor("_BaseColor", c);
                }
            }
        }

        // Fade UI canvas group if present
        if (_canvasGroup != null)
            _canvasGroup.alpha = alpha;
    }

    /// <summary>
    /// Fade the ghost to enterOpacity when the player enters a zone.
    /// </summary>
    public void OnPlayerEnterZone()
    {
        SetOpacity(enterOpacity);
    }

    /// <summary>
    /// Fade the ghost to exitOpacity when the player leaves a zone.
    /// </summary>
    public void OnPlayerExitZone()
    {
        SetOpacity(exitOpacity);
    }

    /// <summary>
    /// Instantly sets the ghost to fully opaque (no transparency).
    /// </summary>
    public void SetOpaque()
    {
        SetOpacity(1f);
    }

    /// <summary>
    /// Restores the object's original materials and fully opaque UI.
    /// </summary>
    public void RestoreOriginalMaterials()
    {
        SetOpaque();
        // Restore mesh materials
        for (int i = 0; i < _meshRenderers.Length; i++)
        {
            _meshRenderers[i].materials = _originalMaterials[i];
        }
        // Reset UI alpha if present
        if (_canvasGroup != null)
            _canvasGroup.alpha = 1f;
    }
}
