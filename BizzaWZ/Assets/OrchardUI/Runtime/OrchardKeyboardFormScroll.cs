using AdvancedInputFieldPlugin;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>Resizes the authored form viewport; the page backdrop and navigation stay fixed.</summary>
[DisallowMultipleComponent]
public sealed class OrchardKeyboardFormScroll : MonoBehaviour
{
    [SerializeField] private ScrollRect scroll;
    [SerializeField] private RectTransform contentEnd;
    [SerializeField] private AdvancedInputField[] inputs;
    [SerializeField] private float viewportHeight = 1375;
    [SerializeField] private float keyboardGap = 24;
    [SerializeField] private float focusPadding = 24;
    [SerializeField] private float bottomPadding = 16;
    [SerializeField] private float minimumViewportHeight = 100;

    private readonly Vector3[] corners = new Vector3[4];
    private UnityAction<bool>[] selectionHandlers;
    private Canvas canvas;
    private RectTransform selected;
    private int keyboardHeight;
    private bool refreshing;
    private bool dimensionsDirty;

    private Camera UiCamera => canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

    private void OnEnable()
    {
        if (scroll == null) return;
        canvas = GetComponentInParent<Canvas>();
        if (selectionHandlers == null)
        {
            selectionHandlers = new UnityAction<bool>[inputs.Length];
            for (int i = 0; i < inputs.Length; i++)
            {
                var field = inputs[i];
                selectionHandlers[i] = value => { if (value) RevealInput((RectTransform)field.transform); };
            }
        }
        for (int i = 0; i < inputs.Length; i++) inputs[i].OnSelectionChanged.AddListener(selectionHandlers[i]);
        NativeKeyboardManager.AddKeyboardHeightChangedListener(ApplyKeyboardHeight);
        ApplyKeyboardHeight(0);
        dimensionsDirty = true;
    }

    private void OnDisable()
    {
        NativeKeyboardManager.RemoveKeyboardHeightChangedListener(ApplyKeyboardHeight);
        if (selectionHandlers != null)
            for (int i = 0; i < inputs.Length; i++) inputs[i].OnSelectionChanged.RemoveListener(selectionHandlers[i]);
        if (scroll != null) ApplyKeyboardHeight(0);
        selected = null;
    }

    private void OnRectTransformDimensionsChange() { dimensionsDirty = true; }
    private void LateUpdate()
    {
        if (!dimensionsDirty) return;
        dimensionsDirty = false;
        ApplyKeyboardHeight(keyboardHeight);
    }

    // Native keyboard height is in screen pixels on every supported platform.
    public void ApplyKeyboardHeight(int height)
    {
        if (scroll == null || refreshing) return;
        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;
        refreshing = true;
        keyboardHeight = Mathf.Max(0, height);
        float available = viewportHeight;
        if (keyboardHeight > 0 && RectTransformUtility.ScreenPointToLocalPointInRectangle(
            scroll.viewport, new Vector2(Screen.width * .5f, keyboardHeight), UiCamera, out var keyboardTop))
        {
            available = Mathf.Clamp(scroll.viewport.rect.yMax - keyboardTop.y - keyboardGap,
                minimumViewportHeight, viewportHeight);
        }
        scroll.StopMovement();
        scroll.viewport.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, available);
        RefreshContent();
        if (keyboardHeight == 0) SetOffset(0);
        else if (selected != null && selected.gameObject.activeInHierarchy) RevealInput(selected);
        refreshing = false;
    }

    public void RefreshContent()
    {
        if (scroll == null || contentEnd == null) return;
        contentEnd.GetWorldCorners(corners);
        float bottom = scroll.content.InverseTransformPoint(corners[0]).y;
        scroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
            Mathf.Max(viewportHeight, -bottom + bottomPadding));
        SetOffset(scroll.content.anchoredPosition.y);
        if (keyboardHeight > 0 && selected != null && selected.gameObject.activeInHierarchy) RevealInput(selected);
    }

    public void RevealInput(RectTransform input)
    {
        if (scroll == null || input == null || !input.IsChildOf(scroll.content)) return;
        selected = input;
        if (keyboardHeight <= 0) return;
        input.GetWorldCorners(corners);
        float bottom = scroll.viewport.InverseTransformPoint(corners[0]).y;
        float top = scroll.viewport.InverseTransformPoint(corners[1]).y;
        var view = scroll.viewport.rect;
        float offset = scroll.content.anchoredPosition.y;
        if (bottom < view.yMin + focusPadding) offset += view.yMin + focusPadding - bottom;
        else if (top > view.yMax - focusPadding) offset -= top - (view.yMax - focusPadding);
        scroll.StopMovement();
        SetOffset(offset);
    }

    private void SetOffset(float offset)
    {
        var position = scroll.content.anchoredPosition;
        position.y = Mathf.Clamp(offset, 0, Mathf.Max(0, scroll.content.rect.height - scroll.viewport.rect.height));
        scroll.content.anchoredPosition = position;
    }
}
