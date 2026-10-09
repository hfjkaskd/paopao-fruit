#if BIZZA_REAL_WITHDRAW
using System;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static partial class OrchardApprovedPass
{
    const string WithdrawalProgressPrefab = "Assets/BizzaWZ/Final/Real/UI/RealWithdrawalPanl/RealWithdrawPanel.prefab";
    const string WithdrawalProgressOutput = "Design/WithdrawalProgress-20261008/";

    public static void ApplyWithdrawalProgress()
    {
        var root = PrefabUtility.LoadPrefabContents(WithdrawalProgressPrefab);
        try
        {
            StyleWithdrawalProgress(root);
            PrefabUtility.SaveAsPrefabAsset(root, WithdrawalProgressPrefab);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
        PreviewWithdrawalProgress();
    }

    private static void StyleWithdrawalProgress(GameObject root)
    {
        var page = root.GetComponent<RealWithdrawPanel>();
        var track = page.progressObj.GetComponent<Image>();
        var fill = page.progressBar;
        track.sprite = OrchardSkinAuthoring.SpriteFor("RewardProgressTrack");
        track.type = Image.Type.Sliced;
        track.preserveAspect = false;
        track.color = Color.white;
        // Retain the ScrollRect's drag surface over the bar.
        track.raycastTarget = true;
        track.rectTransform.sizeDelta = new Vector2(653, 55);
        track.pixelsPerUnitMultiplier = track.sprite.rect.height / track.pixelsPerUnit / 55;

        fill.sprite = NamedSprite("Assets/OrchardUI/Art/WithdrawProgressSoftFill.png", "RewardProgressSoftFill");
        fill.overrideSprite = null;
        fill.type = Image.Type.Sliced;
        fill.preserveAspect = false;
        fill.color = Color.white;
        fill.raycastTarget = false;
        fill.pixelsPerUnitMultiplier = fill.sprite.rect.height / fill.pixelsPerUnit / 37;
        // Keep the page controller's fillAmount and give both caps their authored shape.
        var rounded = fill.GetComponent<OrchardRoundedFill>() ?? fill.gameObject.AddComponent<OrchardRoundedFill>();
        var settings = new SerializedObject(rounded);
        settings.FindProperty("image").objectReferenceValue = fill;
        settings.FindProperty("fullSize").vector2Value = new Vector2(635, 37);
        settings.FindProperty("topLeft").vector2Value = new Vector2(9, -9);
        settings.ApplyModifiedPropertiesWithoutUndo();
        rounded.Refresh();

        var label = page.progressTxt;
        label.font = page.passLevelText.font;
        label.fontSharedMaterial = page.passLevelText.fontSharedMaterial;
        label.color = new Color32(95, 33, 9, 255);
        label.enableVertexGradient = false;
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = label.fontSizeMax = 32;
        label.fontSizeMin = 24;
        label.enableAutoSizing = true;
        label.enableWordWrapping = false;
        label.margin = Vector4.zero;
        label.raycastTarget = false;
        var rect = label.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(280, 47);
        rect.localScale = Vector3.one;
        rect.SetAsLastSibling();
    }

    public static void PreviewWithdrawalProgress()
    {
        Directory.CreateDirectory(WithdrawalProgressOutput);
        var report = new StringBuilder("Disposable prefab previews only. No account state, gameplay or APK build.\n");
        foreach (int height in new[] { 1600, 1280 })
        {
            var result = OrchardSkinValidation.PreviewPrefab(WithdrawalProgressPrefab,
                WithdrawalProgressOutput + "withdrawal-720x" + height + ".png",
                root =>
                {
                    ConfigureReferencePreview(root, "withdraw-main");
                    var page = root.GetComponent<RealWithdrawPanel>();
                    page.balanceTxt.text = "0,00";
                    page.clashTxt.text = "≈R$0,00";
                    page.rateTxt.text = "100≈R$2,50";
                    page.passLevelText.text = "Nível 3";
                    page.hintTxt.text = "<color=#007DA3>PagBank:</color> O valor mínimo de saque é <color=#005D24>R$0,01.</color> Você ainda precisa de mais <color=#005D24>R$0,01.</color>";
                    foreach (var card in page.withdrawLevelRoot.GetComponentsInChildren<WithdrawLevelItem>(true)) card.balanceText.text = "R$0,00";
                    page.progressObj.SetActive(true);
                    page.progressHintTxt.gameObject.SetActive(true);
                    page.completeHintTxt.gameObject.SetActive(false);
                    var footer = page.progressObj.transform.parent.GetComponent<LayoutElement>();
                    footer.preferredHeight = 146; footer.preferredWidth = 788;
                    page.progressHintTxt.text = "Passe de nível 50 para sacar R$0,00.\nFaltam 47 níveis";
                    ShowProgressFixture(page, 3, 50);
                }, 720, height, root =>
                {
                    var page = root.GetComponent<RealWithdrawPanel>();
                    var fill = page.progressBar;
                    var scroll = root.transform.Find("Content/Scroll View").GetComponent<ScrollRect>();
                    if (height == 1280) { scroll.verticalNormalizedPosition = 0; Canvas.ForceUpdateCanvases(); }
                    if (!page.progressObj.GetComponent<Image>().raycastTarget || !page.progressObj.transform.IsChildOf(scroll.content))
                        throw new InvalidOperationException("Progress bar must retain its ScrollRect drag surface.");
                    var rounded = fill.GetComponent<OrchardRoundedFill>();
                    if (rounded == null || fill.type != Image.Type.Sliced) throw new InvalidOperationException("Rounded fill configuration missing.");
                    foreach (int value in new[] { 0, 1, 3, 25, 49, 50 })
                    {
                        ShowProgressFixture(page, value, 50);
                        float expected = value / 50f;
                        float measured = fill.rectTransform.rect.width / 635;
                        if (Mathf.Abs(measured - expected) > .00001f) throw new InvalidOperationException("Fill width does not match progress.");
                        page.progressTxt.ForceMeshUpdate(true, true);
                        if (page.progressTxt.isTextOverflowing) throw new InvalidOperationException("Progress label overflow.");
                        var track = page.progressObj.GetComponent<RectTransform>();
                        var corners = new Vector3[4]; fill.rectTransform.GetWorldCorners(corners);
                        foreach (Vector3 corner in corners)
                            if (!track.rect.Contains(track.InverseTransformPoint(corner))) throw new InvalidOperationException("Fill outside track.");
                        if (value == 0)
                        {
                            fill.Rebuild(CanvasUpdate.PreRender);
                            var mesh = fill.canvasRenderer.GetMesh();
                            if (mesh != null && mesh.vertexCount != 0) throw new InvalidOperationException("Empty progress still renders a fill.");
                        }
                        report.AppendLine("720x" + height + " " + value + "/50: fill=" + measured.ToString("F2") + "; contained, label fits.");
                    }
                    ShowProgressFixture(page, 3, 50);
                    fill.SetAllDirty(); fill.Rebuild(CanvasUpdate.PreRender);
                });
            if (!string.IsNullOrEmpty(result.error)) throw new InvalidOperationException(result.error);
        }
        report.AppendLine("PASS: fill geometry at 0%, 2%, 6%, 50%, 98%, 100%; zero fill hidden; no text overflow.");
        File.WriteAllText(WithdrawalProgressOutput + "progress-checks.txt", report.ToString());
        Debug.Log("Withdrawal progress validation passed: " + WithdrawalProgressOutput);
    }

    private static void ShowProgressFixture(RealWithdrawPanel page, int value, int target)
    {
        page.progressBar.fillAmount = (float)value / target;
        page.progressTxt.text = value + "/" + target;
        page.progressBar.GetComponent<OrchardRoundedFill>().Refresh();
    }
}
#endif
