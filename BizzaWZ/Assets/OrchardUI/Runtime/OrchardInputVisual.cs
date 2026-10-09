using AdvancedInputFieldPlugin;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Reflects the real input selection in its prefab-owned background.</summary>
[DisallowMultipleComponent]
public sealed class OrchardInputVisual : MonoBehaviour
{
    [SerializeField] private AdvancedInputField input;
    [SerializeField] private Image background;
    private Sprite normal, focused;
    private bool selected;

    private void OnEnable() { input.OnSelectionChanged.AddListener(SetSelected); }
    private void OnDisable() { input.OnSelectionChanged.RemoveListener(SetSelected); SetSelected(false); }
    public void SetArtwork(Sprite normalSprite, Sprite focusedSprite)
    {
        normal = normalSprite; focused = focusedSprite;
        Refresh();
    }
    public void SetSelected(bool value) { selected = value; Refresh(); }
    private void Refresh()
    {
        var sprite = selected ? focused : normal;
        if (sprite == null) return;
        background.sprite = sprite;
        background.enabled = true;
    }
}
