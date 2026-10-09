using System;
using UnityEngine;

/// <summary>Fits prefab-authored artwork to the available canvas without changing its hierarchy.</summary>
[DisallowMultipleComponent]
public sealed class OrchardReferenceLayout : MonoBehaviour
{
    [Serializable]
    public struct Element
    {
        public RectTransform rect;
        public Vector2 position;
        public Vector3 scale;
    }
    [SerializeField] private Vector2 referenceSize = new Vector2(852, 1846);
    [SerializeField] private Element[] elements = Array.Empty<Element>();
    [SerializeField] private bool fitWidthAndScroll;
    [SerializeField] private RectTransform scrollWindow;
    [SerializeField] private float scrollTop = 189;
    [SerializeField] private float scrollBottom = 40;
    private RectTransform cachedRect;
    private Vector2 lastSize = new Vector2(-1, -1);
    private bool applying;

    private void Awake() { cachedRect = (RectTransform)transform; }
    private void OnEnable() { lastSize = new Vector2(-1, -1); RefreshLayout(); }
    private void OnRectTransformDimensionsChange() { if (isActiveAndEnabled) RefreshLayout(); }

    public void RefreshLayout()
    {
        if (applying || referenceSize.x <= 0 || referenceSize.y <= 0) return;
        if (cachedRect == null) cachedRect = (RectTransform)transform;
        Vector2 available = cachedRect.rect.size;
        if (available == lastSize || available.x <= 0 || available.y <= 0) return;
        applying = true;
        float factor = fitWidthAndScroll ? available.x / referenceSize.x : Mathf.Min(available.x / referenceSize.x, available.y / referenceSize.y);
        float topOffset = fitWidthAndScroll ? (available.y-referenceSize.y*factor)*.5f : 0;
        for (int i = 0; i < elements.Length; i++)
        {
            var element = elements[i];
            if (element.rect == null) continue;
            element.rect.anchoredPosition = element.position * factor + new Vector2(0,topOffset);
            element.rect.localScale = element.scale * factor;
        }
        if(fitWidthAndScroll && scrollWindow!=null)
            scrollWindow.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,Mathf.Max(1,available.y/factor-scrollTop-scrollBottom));
        lastSize = available;
        applying = false;
    }
}
