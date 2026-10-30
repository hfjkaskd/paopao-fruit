
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Sirenix.OdinInspector;

public class UITeachTipsPage : UIPageBase<UITeachTipsPage.InitParam>, IPointerClickHandler
{
    public struct InitParam
    {
        public string content;
        public int posIdx;
        public bool block;
        public float alpha;
        public float heightValue;
        public string targetPath;
    }

    public PageId LegacyPageType => UIPageIds.UI_TeachTip;

    public Image bg = null;
    public Image panel = null;
    public TMP_Text content = null;
    public RectTransform[] panels;

    [SerializeField, Min(0)] private float targetGap = 48;
    [SerializeField, Min(0)] private float edgePadding = 24;
    private RectTransform placementTarget;
    private Button placementButton;
    private UIPageBase placementPage;
    private bool followingTarget;
    private readonly Vector3[] placementCorners = new Vector3[4];

    private float clickCD = 0;

    protected override void OnOpen(InitParam param)
    {
        StopAllCoroutines();
        ReleaseTarget();
        panel.gameObject.SetActive(true);
        content.text = LanguageUtils.GetText(param.content);
        if (param.posIdx >= 0)
        {
            panel.rectTransform.position = panels[param.posIdx].position;
        }
        else
        {
            var parentRect = panel.transform.parent as RectTransform;
            var rect = parentRect.rect;
            float yPos = Mathf.Lerp(rect.yMin, rect.yMax, param.heightValue);
            panel.transform.position = parentRect.TransformPoint(new Vector3(0, yPos));
        }

        bg.raycastTarget = param.block;
        Color color = bg.color;
        color.a = param.alpha / 255;
        bg.color = color;

        if (!string.IsNullOrEmpty(param.targetPath))
        {
            // Do not flash the legacy fixed-height position while the target page is loading.
            panel.gameObject.SetActive(false);
            StartCoroutine(GameObjUitl.FindGameObject(param.targetPath, BindTarget, DismissTargetTip));
        }
    }

    private void BindTarget(GameObject target)
    {
        placementTarget = target.transform as RectTransform;
        placementPage = target.GetComponentInParent<UIPageBase>();
        if (placementTarget == null || !target.activeInHierarchy ||
            (placementPage != null && placementPage.IsClosing))
        {
            DismissTargetTip();
            return;
        }

        placementButton = target.GetComponent<Button>();
        if (placementButton != null) placementButton.onClick.AddListener(DismissTargetTip);
        followingTarget = true;
        panel.gameObject.SetActive(true);
        PositionAboveTarget(placementTarget);
    }

    private void LateUpdate()
    {
        if (!followingTarget) return;
        // A guide belongs to its target, not to the gameplay screen behind the page.
        if (placementTarget == null || !placementTarget.gameObject.activeInHierarchy ||
            (placementPage != null && placementPage.IsClosing))
        {
            DismissTargetTip();
            return;
        }
        // Follow the cached button through page entrance animations and canvas resizes.
        // Four cached corners only: no hierarchy searches or layout rebuilds per frame.
        PositionAboveTarget(placementTarget);
    }

    private void DismissTargetTip()
    {
        // Hide immediately, including while a page close animation is running.
        panel.gameObject.SetActive(false);
        bg.raycastTarget = false;
        StopAllCoroutines();
        ReleaseTarget();
        CloseSelf();
    }

    private void ReleaseTarget()
    {
        if (placementButton != null) placementButton.onClick.RemoveListener(DismissTargetTip);
        placementButton = null;
        placementPage = null;
        placementTarget = null;
        followingTarget = false;
    }

    public void PositionAboveTarget(RectTransform target)
    {
        if (target == null || !(panel.transform.parent is RectTransform parentRect)) return;
        Rect targetRect = BoundsInParent(target, parentRect);
        Rect panelRect = BoundsInParent(panel.rectTransform, parentRect);
        Rect available = parentRect.rect;
        float bottom = targetRect.yMax + targetGap;
        if (bottom + panelRect.height > available.yMax - edgePadding)
            bottom = targetRect.yMin - targetGap - panelRect.height;
        bottom = Mathf.Clamp(bottom, available.yMin + edgePadding,
            available.yMax - edgePadding - panelRect.height);
        float left = Mathf.Clamp(targetRect.center.x - panelRect.width * .5f,
            available.xMin + edgePadding, available.xMax - edgePadding - panelRect.width);
        Vector3 offset = new Vector3(left - panelRect.xMin, bottom - panelRect.yMin, 0);
        if (offset.sqrMagnitude > .0001f) panel.rectTransform.localPosition += offset;
    }

    private Rect BoundsInParent(RectTransform rect, RectTransform parentRect)
    {
        rect.GetWorldCorners(placementCorners);
        Vector2 min = parentRect.InverseTransformPoint(placementCorners[0]);
        Vector2 max = min;
        for (int i = 1; i < placementCorners.Length; i++)
        {
            Vector2 point = parentRect.InverseTransformPoint(placementCorners[i]);
            min = Vector2.Min(min, point);
            max = Vector2.Max(max, point);
        }
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    private void Update()
    {
        clickCD -= Time.unscaledDeltaTime;
    }

    protected override void OnShow()
    {
    }

    protected override void OnHide()
    {
        StopAllCoroutines();
        ReleaseTarget();
        panel.gameObject.SetActive(false);
        bg.raycastTarget = false;
    }

    protected override void OnClose()
    {
        StopAllCoroutines();
        ReleaseTarget();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        ReleaseTarget();
    }

    public void OnClick()
    {
        if (clickCD > 0)
        {
            return;
        }

        CloseSelf();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnClick();
    }

}

public static partial class UIPageIds
{
    public static readonly PageId UI_TeachTip = "UI_TeachTip";
}
