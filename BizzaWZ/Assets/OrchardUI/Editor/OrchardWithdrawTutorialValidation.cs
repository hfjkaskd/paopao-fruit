using System;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static partial class OrchardApprovedPass
{
    private const string WithdrawTutorialOutput = "Design/WithdrawTutorial-20261008/";
    private const string TeachTipsPrefab = "Assets/BizzaWZ/Final/Framework/Runtime/Module/Teach/UITeach/UITeachTipsPage.prefab";

    // Disposable prefab previews only. No page lifecycle, accounts, payments or saves are run.
    public static void ValidateWithdrawTutorial()
    {
#if BIZZA_REAL_WITHDRAW
        Directory.CreateDirectory(WithdrawTutorialOutput);
        var report = new StringBuilder();
        var graph = AssetDatabase.LoadAssetAtPath<GraphSO>("Assets/BizzaWZ/Final/BizzaGame/Resources/Actions/Teach_01.asset");
        int count = CheckWithdrawTipNodes(graph.graph.root);
        if (count != 1) throw new InvalidOperationException("Expected one target-anchored cash withdrawal tip, found " + count);
        report.AppendLine("PASS: Teach_01 cash-withdraw tip uses the unchanged Content/WithdrawBtn target path; optional path survives node cloning.");
        foreach (int height in new[] { 1280, 1600, 1800 })
        {
            var preview = OrchardSkinValidation.PreviewPrefab(
                "Assets/BizzaWZ/Final/Real/UI/FakeWithdrawPanel/FakeWithdrawPanel.prefab",
                WithdrawTutorialOutput + "tip-720x" + height + ".png",
                root => PrepareWithdrawTutorialPreview(root, height), 720, height,
                root => CheckWithdrawTutorialPosition(root, height, report));
            if (!string.IsNullOrEmpty(preview.error)) throw new InvalidOperationException(preview.error);
        }
        report.AppendLine("Static prefab/layout validation only; Android touch flow is not exercised. No APK built.");
        File.WriteAllText(WithdrawTutorialOutput + "result.txt", report.ToString());
        Debug.Log(report.ToString());
#endif
    }

    private static int CheckWithdrawTipNodes(NodeBase node)
    {
        if (node == null) return 0;
        int count = 0;
        if (node is Action_Teach_ShowTip tip && tip.targetPath?.internalValue is Variable_String_Direct path &&
            !string.IsNullOrEmpty(path.directValue))
        {
            const string expected = "[GameInstance]/GameCanvas/Content/PopupLayer/FakeWithdrawPanel(Clone)/Content/WithdrawBtn";
            var clone = (Action_Teach_ShowTip)tip.Clone();
            if (path.directValue != expected || ((Variable_String_Direct)clone.targetPath.internalValue).directValue != expected ||
                ((Variable_Bool_Direct)tip.block.internalValue).directValue)
                throw new InvalidOperationException("Tutorial target/cloning/non-blocking configuration changed.");
            count++;
        }
        foreach (var child in node.children) count += CheckWithdrawTipNodes(child);
        return count;
    }

#if BIZZA_REAL_WITHDRAW
    private static void PrepareWithdrawTutorialPreview(GameObject root, int height)
    {
        ConfigureReferencePreview(root, "cash-withdraw");
        var page = root.GetComponent<FakeWithdrawPanel>();
        page.balanceTxt.text = "R$61,77";
        page.progressImg.fillAmount = 1;
        page.progressTxt.text = "100%";
        page.hintTxt.text = "Parabéns, todas as condições foram atendidas";
        var visual = root.GetComponent<OrchardCashVisual>();
        if (visual != null) visual.SetAvailable(true);
        var cards = root.GetComponentsInChildren<WithdrawAmountItem>(true);
        string[] amounts = { "R$0,01", "R$800", "R$1000", "R$2000", "R$3000", "R$5000" };
        for (int i = 0; i < cards.Length && i < amounts.Length; i++)
        { cards[i].gameObject.SetActive(true); cards[i].amountTxt.text = amounts[i]; }

        var overlay = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(TeachTipsPrefab), root.transform, false);
        overlay.name = "TutorialPreview";
        foreach (var behaviour in overlay.GetComponentsInChildren<MonoBehaviour>(true))
            if (!(behaviour is Graphic)) behaviour.enabled = false;
        foreach (var animation in overlay.GetComponentsInChildren<Animation>(true)) animation.enabled = false;
        // Match the actual GameCanvas (height match, reference height 2360).
        float scale = height / 2360f;
        var rect = (RectTransform)overlay.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition3D = Vector3.zero;
        rect.sizeDelta = new Vector2(720 / scale, 2360);
        rect.localScale = Vector3.one * scale;
        var tips = overlay.GetComponent<UITeachTipsPage>();
        tips.bg.color = new Color(0, 0, 0, .39f);
        tips.bg.raycastTarget = false;
        tips.panel.gameObject.SetActive(true);
        tips.content.text = "Clique aqui para sacar";
    }

    private static void CheckWithdrawTutorialPosition(GameObject root, int height, StringBuilder report)
    {
        var tips = root.GetComponentInChildren<UITeachTipsPage>(true);
        var target = (RectTransform)root.transform.Find("Content/WithdrawBtn");
        if (target.GetComponent<Button>() == null) throw new InvalidOperationException("Withdrawal target is no longer a standard Button.");
        Vector3 originalPosition = target.localPosition;
        foreach (float movement in new[] { 0f, 60f, -60f })
        {
            target.localPosition = originalPosition + Vector3.up * movement;
            tips.PositionAboveTarget(target);
            Rect buttonRect = TutorialBounds(target, (RectTransform)root.transform);
            Rect tipRect = TutorialBounds(tips.panel.rectTransform, (RectTransform)root.transform);
            Rect screen = ((RectTransform)root.transform).rect;
            if (tipRect.Overlaps(buttonRect) || tipRect.yMin < buttonRect.yMax + 1 ||
                tipRect.yMin < screen.yMin || tipRect.yMax > screen.yMax ||
                tipRect.xMin < screen.xMin || tipRect.xMax > screen.xMax)
                throw new InvalidOperationException("Tip overlaps its button or screen edge at 720x" + height);
            if (movement == 0)
                report.AppendLine("PASS 720x" + height + ": button-to-tip gap " + (tipRect.yMin - buttonRect.yMax).ToString("F1") + " px; tip completely on screen.");
        }
        target.localPosition = originalPosition;
        tips.PositionAboveTarget(target);
        if (tips.bg.raycastTarget || tips.panel.raycastTarget || tips.content.raycastTarget)
            throw new InvalidOperationException("Non-blocking tutorial tip intercepts touches.");
        tips.content.ForceMeshUpdate(true, true);
        if (tips.content.isTextOverflowing) throw new InvalidOperationException("Portuguese tutorial text is clipped.");
        Canvas.ForceUpdateCanvases();
        report.AppendLine("PASS: moved target +/-60 units; no overlap; tip graphics do not intercept touches; Portuguese text fits.");
    }

    private static Rect TutorialBounds(RectTransform rect, RectTransform relative)
    {
        var corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        Vector2 min = relative.InverseTransformPoint(corners[0]), max = min;
        for (int i = 1; i < corners.Length; i++)
        { Vector2 p = relative.InverseTransformPoint(corners[i]); min = Vector2.Min(min, p); max = Vector2.Max(max, p); }
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }
#endif
}
