using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static partial class OrchardApprovedPass
{
    private const string RealRoot = "Assets/BizzaWZ/Final/Real/UI/RealWithdrawalPanl/";
    private static void AuthorComponents()
    {
        foreach (string name in new[] { "WithdrawLevelItem", "WithdrawWay" })
        {
            string path = RealRoot + name + ".prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (name == "WithdrawLevelItem") StyleLevelCard(root); else StylePaymentCard(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        AssetDatabase.SaveAssets();
        AuthorServiceComponents();
    }
    private static void LocalRect(Transform t, float x, float y, float w, float h)
    {
        var r = (RectTransform)t; r.anchorMin = r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(.5f, .5f);
        r.sizeDelta = new Vector2(w, h); r.anchoredPosition = new Vector2(x + w * .5f, -y - h * .5f); r.localScale = Vector3.one;
    }
    private static void StretchRect(Transform t)
    {
        var r = (RectTransform)t; r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero; r.localScale = Vector3.one;
    }
    private static void TextStyle(TMP_Text tx, float size, bool white = false)
    {
        if (ApprovedFont != null) tx.font = ApprovedFont;
        if (white) OrchardSkinAuthoring.SetTitle(tx); else { OrchardSkinAuthoring.SetBody(tx); tx.color = ApprovedInk; }
        tx.margin = Vector4.zero; tx.characterSpacing = 0; tx.wordSpacing = 0;
        tx.fontStyle = FontStyles.Normal;
        tx.fontSize = size; tx.enableAutoSizing = true; tx.fontSizeMax = size; tx.fontSizeMin = size * .68f;
        tx.alignment = TextAlignmentOptions.MidlineLeft; tx.raycastTarget = false;
    }
    private static void StyleLevelCard(GameObject root)
    {
        ((RectTransform)root.transform).sizeDelta = new Vector2(390, 176);
        var bg = Need(root, "bg").GetComponent<Image>(); Paint(bg, "Card"); StretchRect(bg.transform);
        var button = root.GetComponent<Button>(); button.targetGraphic = bg; bg.raycastTarget = true;
        LocalRect(Need(root, "Level"), 27, 18, 225, 43);
        StretchRect(Need(root, "Level/Level")); TextStyle(Need(root, "Level/Level").GetComponent<TMP_Text>(), 31);
        Need(root, "Level/LevelImage").GetComponent<Image>().enabled = false;
        LocalRect(Need(root, "Rate"), 248, 20, 112, 45); Paint(Need(root, "Rate").GetComponent<Image>(), "Badge");
        StretchRect(Need(root, "Rate/RateText")); var rate = Need(root, "Rate/RateText").GetComponent<TMP_Text>(); TextStyle(rate, 29); rate.alignment = TextAlignmentOptions.Center;
        LocalRect(Need(root, "BalanceText"), 150, 83, 171, 62);
        var value = Need(root, "BalanceText").GetComponent<TMP_Text>(); TextStyle(value, 48); value.color = new Color32(0,83,59,255);
        var cash = Ensure(root, "ApprovedCash"); var cashImage = cash.GetComponent<Image>() ?? cash.gameObject.AddComponent<Image>();
        cashImage.sprite = NamedSprite(FidelityAtlas,"Cash");
        cashImage.preserveAspect = true; cashImage.raycastTarget = false; LocalRect(cash, 24, 79, 114, 84);
        foreach (string n in new[] { "Shadow", "SelectShadow" })
        {
            var image = Need(root, n).GetComponent<Image>(); Paint(image, "DisabledCard"); StretchRect(image.transform);
            image.color = new Color(1, 1, 1, .18f); image.raycastTarget = false;
        }
        var lockImage = root.transform.Find("Shadow/OrchardLock"); if (lockImage != null) LocalRect(lockImage, 323, 109, 30, 34);
        var select = Need(root, "SelectObj"); LocalRect(select, 319, 111, 48, 48);
        var ring = Ensure(root, "ApprovedSelectionBorder");
        var ringImage = ring.GetComponent<Image>() ?? ring.gameObject.AddComponent<Image>();ringImage.sprite=NamedSprite(FidelityPayments,"Selected");ringImage.type=Image.Type.Sliced;ringImage.pixelsPerUnitMultiplier=1.9f;ringImage.raycastTarget = false;
        ring.gameObject.SetActive(false); StretchRect(ring);
        // The existing selected GameObject already owns the data-driven check mark.
        var selection = select.GetComponent<OrchardSelectionVisual>() ?? select.gameObject.AddComponent<OrchardSelectionVisual>();
        var so = new SerializedObject(selection);
        so.FindProperty("border").objectReferenceValue = ring.gameObject; so.ApplyModifiedPropertiesWithoutUndo();
        var itemSo=new SerializedObject(root.GetComponent<WithdrawLevelItem>());itemSo.FindProperty("compactLevelLabel").boolValue=true;itemSo.ApplyModifiedPropertiesWithoutUndo();
        cash.SetAsLastSibling();Need(root, "BalanceText").SetAsLastSibling(); Need(root, "Rate").SetAsLastSibling(); Need(root, "Level").SetAsLastSibling(); select.SetAsLastSibling();
        select.GetComponent<Image>().sprite=NamedSprite(FidelityAtlas,"Check");
    }
    private static void StylePaymentCard(GameObject root)
    {
        ((RectTransform)root.transform).sizeDelta = new Vector2(350, 144);
        var own = root.GetComponent<Image>() ?? root.AddComponent<Image>();own.sprite=NamedSprite(FidelityPayments,"Unselected");own.type=Image.Type.Sliced;own.pixelsPerUnitMultiplier=2f;own.color=Color.white;own.raycastTarget = true;
        root.GetComponent<Button>().targetGraphic = own;
        var logo = Need(root, "Frame").GetComponent<Image>(); logo.preserveAspect = true; logo.color = Color.white; logo.raycastTarget = false;
        LocalRect(logo.transform, 28, 31, 246, 83);
        if (root.transform.Find("OrchardLogoFrame") is Transform old) old.GetComponent<Image>().enabled = false;
        var selected = Need(root, "Select");var surface=selected.GetComponent<Image>();surface.sprite=NamedSprite(FidelityPayments,"Selected");surface.color=Color.white;surface.type=Image.Type.Sliced;surface.pixelsPerUnitMultiplier=2f;StretchRect(selected);surface.raycastTarget = false;selected.SetAsFirstSibling();
        var check = selected.Find("OrchardCheck"); if (check != null) {check.GetComponent<Image>().sprite=NamedSprite(FidelityAtlas,"Check");check.GetComponent<Image>().enabled = true; LocalRect(check, 281, 53, 55, 55);}
    }
    private static void SetupWithdrawalLayout(GameObject root)
    {
#if BIZZA_REAL_WITHDRAW
        const string contentPath = "Content/Scroll View/Viewport/Content";
        var content = Need(root, contentPath).GetComponent<RectTransform>();
        content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(.5f, 1);
        content.anchoredPosition = Vector2.zero; content.sizeDelta = Vector2.zero;
        var list = content.GetComponent<VerticalLayoutGroup>();
        list.enabled = true; list.padding = new RectOffset(0, 0, 0, 20); list.spacing = 16;
        list.childControlHeight = true; list.childControlWidth = true; list.childForceExpandHeight = false; list.childForceExpandWidth = true;
        var fit = content.GetComponent<ContentSizeFitter>(); fit.enabled = true; fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var info = Need(root, contentPath + "/WithdrawInfo");
        var infoLayout = info.GetComponent<LayoutElement>() ?? info.gameObject.AddComponent<LayoutElement>(); infoLayout.preferredHeight = 858;
        var levels = Need(root, contentPath + "/WithdrawLevel");
        var levelLayout = levels.GetComponent<VerticalLayoutGroup>() ?? levels.gameObject.AddComponent<VerticalLayoutGroup>();
        levelLayout.padding = new RectOffset(5,5,0,0); levelLayout.spacing = 26; levelLayout.childControlHeight = true; levelLayout.childControlWidth = true; levelLayout.childForceExpandHeight = false;levelLayout.childForceExpandWidth=false;levelLayout.childAlignment=TextAnchor.UpperCenter;
        var grid = Need(root, contentPath + "/WithdrawLevel/Content").GetComponent<GridLayoutGroup>();
        var gridWidth=grid.GetComponent<LayoutElement>()??grid.gameObject.AddComponent<LayoutElement>();gridWidth.preferredWidth=790;
        grid.cellSize = new Vector2(390,176); grid.spacing = new Vector2(10,16); grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 2;
        grid.padding = new RectOffset(); grid.childAlignment = TextAnchor.UpperCenter;
        var gridFitter = grid.GetComponent<ContentSizeFitter>(); if (gridFitter != null) gridFitter.enabled = false;
        var progress = Need(root, contentPath + "/WithdrawLevel/ProgressInfo");
        var progressLayout = progress.GetComponent<LayoutElement>() ?? progress.gameObject.AddComponent<LayoutElement>(); progressLayout.preferredHeight = 146;
        foreach (var card in root.GetComponentsInChildren<WithdrawWay>(true)) StylePaymentCard(card.gameObject);
        foreach (var card in root.GetComponentsInChildren<WithdrawLevelItem>(true)) StyleLevelCard(card.gameObject);
        var payments = Need(root, contentPath + "/WithdrawInfo/WithdrawMode/Content").GetComponent<GridLayoutGroup>();
        payments.cellSize = new Vector2(350,144); payments.spacing = new Vector2(16,16); payments.constraint = GridLayoutGroup.Constraint.FixedColumnCount; payments.constraintCount = 2; payments.childAlignment = TextAnchor.UpperCenter;
        payments.padding=new RectOffset();
        var page = root.GetComponent<RealWithdrawPanel>(); var so = new SerializedObject(page);
        StretchRect(page.completeHintTxt.transform);((RectTransform)page.completeHintTxt.transform).offsetMin=new Vector2(24,5);((RectTransform)page.completeHintTxt.transform).offsetMax=new Vector2(-24,-5);
        so.FindProperty("layoutControlsProgressPosition").boolValue = true;
        so.FindProperty("approvedProgressLayout").objectReferenceValue = progressLayout;
        so.FindProperty("approvedCompleteHeight").floatValue=70;
        so.FindProperty("approvedCompleteWidth").floatValue=428;
        so.FindProperty("approvedProgressWidth").floatValue=788;
        so.FindProperty("compactLevelLabels").boolValue=true;
        so.FindProperty("completeFormatPortuguese").stringValue="Valor a receber: <color=#005629>{0}</color>";
        so.FindProperty("completeFormatEnglish").stringValue="Amount to receive: <color=#005629>{0}</color>";
        so.ApplyModifiedPropertiesWithoutUndo();
#endif
    }
    private static void SetupAccountLayout(GameObject root)
    {
#if BIZZA_REAL_WITHDRAW
        var page = root.GetComponent<UIWithdrawalPanel>();
        const string prefix = "Root/FillRoot/pageContent/InfoContent/";
        var rows = Need(root, "Root/FillRoot/pageContent/InfoContent");
        int order = 0;
        foreach (string rowName in new[] { "Name", "NameErrorHint", "CPF", "CPFErrorHint", "document", "documentErrorHint", "EmailInfo", "EmailInfoErrorHint", "OVO_New", "OVOErrorHint" })
        {
            var row = Need(root, prefix + rowName); row.SetSiblingIndex(order++);
            var le = row.GetComponent<LayoutElement>() ?? row.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = rowName.EndsWith("ErrorHint", StringComparison.Ordinal) ? 44 : rowName == "document" ? 276 : 176;
        }
        foreach (var layout in new[] { page.InputRoot.GetComponent<VerticalLayoutGroup>(), rows.GetComponent<VerticalLayoutGroup>() })
        {
            layout.enabled = true; layout.padding = new RectOffset(); layout.childControlWidth = true; layout.childControlHeight = true;
            layout.childForceExpandHeight = false; layout.childForceExpandWidth = true; layout.spacing = layout.transform == rows ? 0 : 93;
        }
        foreach (var fitter in new[] { page.InputRoot.GetComponent<ContentSizeFitter>(), rows.GetComponent<ContentSizeFitter>() })
            if (fitter != null) fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        foreach (var input in new[] { page.accountNameInput, page.CPFNumberInput, page.paypalMailInput, page.accPhoneMailInput, page.accountIdentificationInput })
        {
            var r = (RectTransform)input.transform;
            LocalRect(r, 0, input == page.accountIdentificationInput ? 7 : 43, 708, 101);
            foreach (var tx in input.GetComponentsInChildren<TMP_Text>(true)) { TextStyle(tx, 36); tx.enableAutoSizing = false; }
            var bg = input.transform.Find("Background"); if (bg != null) { StretchRect(bg); Paint(bg.GetComponent<Image>(), "Input"); }
        }
        foreach (string pair in new[] { "Name/Text (TMP)", "CPF/Text (TMP)", "EmailInfo/Title", "OVO_New/Text (TMP)", "document/Text (TMP)" })
        {
            var label = Need(root, prefix + pair); LocalRect(label, 0, 0, 708, 40); TextStyle(label.GetComponent<TMP_Text>(), 36);
        }
        var button = Need(root, "Root/FillRoot/pageContent/BtnWithdrawal");
        var buttonLayout = button.GetComponent<LayoutElement>() ?? button.gameObject.AddComponent<LayoutElement>(); buttonLayout.preferredHeight = 142;
        page.defaultHeight = 658;
        Vector3 inputTop = page.InputRoot.TransformPoint(new Vector3(0, page.InputRoot.rect.yMax, 0));
        page.InputRoot.pivot = new Vector2(.5f, 1); page.InputRoot.position = inputTop;
        root.transform.Find("BG").GetComponent<Image>().color = Color.clear;
        var amountTransform = Ensure(root, "Root/FillRoot/ApprovedAmount");
        var amount = amountTransform.GetComponent<TMP_Text>() ?? amountTransform.gameObject.AddComponent<TextMeshProUGUI>();
        TextStyle(amount, 98); amount.alignment = TextAlignmentOptions.Center; amount.color = ApprovedGreen;
        Place(root, amountTransform, 97, 526, 658, 124);
        var so = new SerializedObject(page); so.FindProperty("approvedAmountLabel").objectReferenceValue = amount; so.ApplyModifiedPropertiesWithoutUndo();
#endif
    }
}
