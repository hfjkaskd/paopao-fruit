using TMPro;
using UnityEngine;

/// <summary>Applies a prefab-authored text style using the atlas and metrics of the loaded font.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(TMP_Text))]
public sealed class OrchardFontMaterialBinding : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private Material preset;
    private Material selectedPreset;
    private Material runtimeMaterial;
    private TMP_FontAsset boundFont;
    private Texture boundAtlas;
    private Material appliedPreset;

    private void Awake()
    {
        if (label == null) label = GetComponent<TMP_Text>();
    }

    private void OnEnable() { SetPreset(selectedPreset != null ? selectedPreset : preset); }

    public void SetPreset(Material style)
    {
        selectedPreset = style;
        if (label == null) label = GetComponent<TMP_Text>();
        var font = label.font;
        if (style == null || font == null || font.material == null) return;
        var atlas = font.atlasTexture;
        if (atlas == null) return;

        if (runtimeMaterial == null || boundFont != font || boundAtlas != atlas)
        {
            ReleaseMaterial();
            runtimeMaterial = new Material(font.material) { name = style.name + " (font bound)", hideFlags = HideFlags.DontSave };
            boundFont = font;
            boundAtlas = atlas;
            appliedPreset = null;
        }
        bool changed = false;
        if (appliedPreset != style)
        {
            // TMP copies the authored outline/underlay style while retaining the destination font's atlas metrics.
            TMP_MaterialManager.CopyMaterialPresetProperties(style, runtimeMaterial);
            runtimeMaterial.mainTexture = atlas;
            appliedPreset = style;
            changed = true;
        }
        if (label.fontSharedMaterial != runtimeMaterial)
        {
            label.fontSharedMaterial = runtimeMaterial;
            changed = true;
        }
        if (changed) label.UpdateMeshPadding();
    }

    private void OnDestroy() { ReleaseMaterial(); }

    private void ReleaseMaterial()
    {
        if (runtimeMaterial == null) return;
        Destroy(runtimeMaterial);
        runtimeMaterial = null;
    }
}
