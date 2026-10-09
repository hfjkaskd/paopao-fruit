using UnityEngine;

/// <summary>Moves only the authored composer above the native keyboard.</summary>
[DisallowMultipleComponent]
public sealed class OrchardServiceKeyboardLayout : MonoBehaviour
{
    [SerializeField] private RectTransform inputBounds;
    [SerializeField] private RectTransform[] movingRects;
    [SerializeField] private RectTransform chatViewport;
    [SerializeField] private GameObject quickQuestions;
    [SerializeField, Min(0)] private float keyboardGap = 24;
    [SerializeField, Min(0)] private float chatGap = 24;
    [SerializeField, Min(1)] private float minimumChatHeight = 80;
    private Vector2[] positions;
    private Vector2 viewportMin, viewportMax;
    private Canvas canvas;
    private readonly Vector3[] corners = new Vector3[4];
    private bool captured;

    private Camera UiCamera => canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

    private void Capture()
    {
        if (captured) return;
        canvas = GetComponentInParent<Canvas>();
        positions = new Vector2[movingRects.Length];
        for (int i = 0; i < movingRects.Length; i++) positions[i] = movingRects[i].anchoredPosition;
        viewportMin = chatViewport.offsetMin;
        viewportMax = chatViewport.offsetMax;
        captured = true;
    }

    public void ApplyKeyboardHeight(int height)
    {
        if (inputBounds == null || chatViewport == null) return;
        Capture();
        if (canvas == null) return;
        for (int i = 0; i < movingRects.Length; i++) movingRects[i].anchoredPosition = positions[i];
        chatViewport.offsetMin = viewportMin;
        chatViewport.offsetMax = viewportMax;
        quickQuestions.SetActive(height <= 0);
        if (height <= 0) return;

        inputBounds.GetWorldCorners(corners);
        Vector2 bottom = RectTransformUtility.WorldToScreenPoint(UiCamera, corners[0]);
        Vector2 top = RectTransformUtility.WorldToScreenPoint(UiCamera, corners[1]);
        float inputScale = (top.y - bottom.y) / inputBounds.rect.height;
        float shift = Mathf.Max(0, height + keyboardGap * inputScale - bottom.y);
        // The divider and composer controls have different parents; convert the same pixel shift for each.
        for (int i = 0; i < movingRects.Length; i++)
        {
            var parent = (RectTransform)movingRects[i].parent;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, bottom, UiCamera, out var start);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, bottom + Vector2.up * shift, UiCamera, out var end);
            movingRects[i].anchoredPosition = positions[i] + end - start;
        }
        RectTransformUtility.ScreenPointToLocalPointInRectangle(chatViewport,
            new Vector2(top.x, top.y + shift + chatGap * inputScale), UiCamera, out var composerTop);
        float inset = Mathf.Clamp(composerTop.y - chatViewport.rect.yMin, 0,
            Mathf.Max(0, chatViewport.rect.height - minimumChatHeight));
        chatViewport.offsetMin = viewportMin + Vector2.up * inset;
    }

    private void OnDisable()
    {
        if (captured) ApplyKeyboardHeight(0);
    }
}
