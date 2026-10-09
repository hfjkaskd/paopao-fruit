using UnityEngine;
using UnityEngine.UI;

/// <summary>Displays an existing Image.fillAmount value using a rounded, sliced sprite.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public sealed class OrchardRoundedFill : MonoBehaviour
{
    [SerializeField] private Image image;
    [SerializeField] private Vector2 fullSize;
    [SerializeField] private Vector2 topLeft;
    private RectTransform rect;
    private float displayed = -1;

    private void Awake() { Cache(); }
    private void OnEnable() { Cache(); Refresh(); }
    private void Cache()
    {
        if (image == null) image = GetComponent<Image>();
        if (rect == null) rect = image.rectTransform;
    }
    // Existing page controllers own fillAmount. This only synchronizes its visual width.
    private void LateUpdate() { if (!Mathf.Approximately(displayed, image.fillAmount)) Refresh(); }
    public void Refresh()
    {
        Cache();
        displayed = Mathf.Clamp01(image.fillAmount);
        image.type = Image.Type.Sliced;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0,1);
        rect.anchoredPosition = topLeft;
        rect.sizeDelta = new Vector2(fullSize.x*displayed,fullSize.y);
    }
}
