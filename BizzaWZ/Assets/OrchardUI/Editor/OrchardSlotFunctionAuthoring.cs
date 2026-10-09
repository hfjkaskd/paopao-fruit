#if BIZZA_REAL_WITHDRAW
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class OrchardSlotFunctionAuthoring
{
    private const string SlotPath = "Assets/BizzaWZ/Final/Real/UI/SlotsPanel/SlotPanel/SlotPanel.prefab";
    private const string HelpPath = "Assets/BizzaWZ/Final/Real/UI/SlotsPanel/SlotFQAPanel/SlotFQAPanel.prefab";

    public static void Apply()
    {
        var root = PrefabUtility.LoadPrefabContents(SlotPath);
        try
        {
            Configure(root);
            PrefabUtility.SaveAsPrefabAsset(root, SlotPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        root = PrefabUtility.LoadPrefabContents(HelpPath);
        try
        {
            ConfigureHelp(root);
            PrefabUtility.SaveAsPrefabAsset(root, HelpPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
        OrchardSlotFunctionValidation.Validate();
    }

    public static void Configure(GameObject root)
    {
        var page = root.GetComponent<SlotPanel>();
        // An old outer-prefab override of listener[0].target can recreate a ghost
        // listener even after the nested Button's array size is set to zero.
        var instanceRoot = PrefabUtility.GetNearestPrefabInstanceRoot(page.slotBtn.gameObject);
        var sourceButton = PrefabUtility.GetCorrespondingObjectFromSource(page.slotBtn);
        var modifications = instanceRoot == null ? null : PrefabUtility.GetPropertyModifications(instanceRoot);
        if (modifications != null)
        {
            var kept = new List<PropertyModification>();
            foreach (var modification in modifications)
            {
                if (modification.target == sourceButton &&
                    (modification.propertyPath.StartsWith("onClick.m_PersistentCalls.m_Calls.Array.data[") ||
                     modification.propertyPath.StartsWith("m_OnClick.m_PersistentCalls.m_Calls.Array.data["))) continue;
                kept.Add(modification);
            }
            PrefabUtility.SetPropertyModifications(instanceRoot, kept.ToArray());
        }
        foreach (var button in new Button[] { page.slotBtn, page.closeBtn, page.faqBtn, page.slotRewardPanel.btnObj.GetComponent<Button>(), root.transform.Find("Content/ApprovedAdSpin").GetComponent<Button>() })
        {
            // Runtime actions are bound by SlotPanel / SlotRewardPanel in code.
            var data = new SerializedObject(button);
            foreach (string path in new[] { "m_OnClick.m_PersistentCalls.m_Calls", "onClick.m_PersistentCalls.m_Calls" })
            {
                var listeners = data.FindProperty(path);
                if (listeners != null) listeners.arraySize = 0;
            }
            data.ApplyModifiedPropertiesWithoutUndo();
        }
        var adState = page.notCanClickObj;
        // The unavailable-looking cover was introduced by the visual restyle.
        // Both free and ad states share the same working green Button underneath.
        adState.GetComponent<Image>().enabled = false;
        var label = adState.GetComponentInChildren<TMP_Text>(true);
        Copy(label, "Watch & Spin", "Ver vídeo e girar");
        label.fontSize = 58;
        label.enableAutoSizing = true;
        label.fontSizeMin = 34;
        label.fontSizeMax = 58;
        label.enableWordWrapping = false;
        label.alignment = TextAlignmentOptions.Center;
        Rect(label.rectTransform, new Vector2(42, 1), new Vector2(367, 109));
        var icon = adState.transform.Find("AdVideo");
        if (icon == null)
        {
            icon = new GameObject("AdVideo", typeof(RectTransform), typeof(Image)).transform;
            icon.SetParent(adState.transform, false);
        }
        Rect((RectTransform)icon, new Vector2(-180, 0), new Vector2(74, 74));
        var image = icon.GetComponent<Image>();
        image.sprite = root.transform.Find("Content/ApprovedAdSpin/Film").GetComponent<Image>().sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        page.slotBtn.interactable = true;
        page.slotBtn.targetGraphic = page.slotBtn.GetComponent<Image>();
        page.slotBtn.targetGraphic.raycastTarget = true;

        // The original page has one spin action, not a second ad/progress panel.
        // Retain inactive source artwork for older authoring passes, with no runtime binding.
        foreach (string path in RemovedFooterPaths)
            root.transform.Find(path).gameObject.SetActive(false);
        root.transform.Find("Content/ApprovedAdSpin").GetComponent<Button>().interactable = false;
        // Preserve the original state hint, immediately below the machine.
        Rect(page.slotHintTxt.rectTransform, new Vector2(0, -424), new Vector2(698, 54));
        page.slotHintTxt.fontSize = 30;
        page.slotHintTxt.enableAutoSizing = true;
        page.slotHintTxt.fontSizeMin = 22;
        page.slotHintTxt.fontSizeMax = 30;
        OrchardSkinAuthoring.SetTitle(page.slotHintTxt);
        ConfigureMachineSpacing(root, page);
        ConfigureCoinArt(root, page);

        // This modal must cover the later-authored free count.
        root.transform.Find("Content").SetAsLastSibling();
        var reward = page.slotRewardPanel;
        reward.transform.SetAsLastSibling();
        reward.gameObject.SetActive(false);
        var mask = reward.transform.Find("PageMask");
        mask.GetComponent<Image>().color = new Color(0, 0, 0, .52f);
        mask.GetComponent<Image>().raycastTarget = true;
        var emptyButton = mask.GetComponent<Button>();
        if (emptyButton != null) Object.DestroyImmediate(emptyButton);
        var panel = reward.transform.Find("bg").GetComponent<Image>();
        panel.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/OrchardUI/Art/SlotIvoryPanel.png");
        panel.type = Image.Type.Sliced;
        panel.pixelsPerUnitMultiplier = 4;
        var claim = reward.btnObj.GetComponent<Image>();
        claim.sprite = page.slotBtn.GetComponent<Image>().sprite;
        claim.type = Image.Type.Simple;
        claim.preserveAspect = false;
    }

    internal static readonly string[] RemovedFooterPaths =
    {
        "Content/ApprovedFooter", "Content/ApprovedCheckLine", "Content/ApprovedSlotProgress",
        "Content/ApprovedCheck0", "Content/ApprovedCheck1", "Content/ApprovedCheck2",
        "Content/ApprovedCheck3", "Content/ApprovedCheck4", "Content/ApprovedAdSpin"
    };

    private static void ConfigureMachineSpacing(GameObject root, SlotPanel page)
    {
        var cabinet = (RectTransform)root.transform.Find("Content/SlotMachineGroup/ApprovedCabinet");
        var balance = (RectTransform)root.transform.Find("Top/IconInfo");
        var corners = new Vector3[4];
        balance.GetWorldCorners(corners);
        float balanceBottom = root.transform.InverseTransformPoint(corners[0]).y;
        cabinet.GetWorldCorners(corners);
        float cabinetTop = root.transform.InverseTransformPoint(corners[1]).y;
        // A fixed authored gap keeps the apple crest clear of both balance counters.
        float offset = balanceBottom - 40 - cabinetTop;
        var movement = root.transform.TransformVector(new Vector3(0, offset, 0));
        root.transform.Find("Content/SlotMachineGroup").position += movement;
        root.transform.Find("Content/ApprovedFreeCount").position += movement;
        // Derive the hint from the moved cabinet, so repeated authoring is idempotent.
        cabinet.GetWorldCorners(corners);
        var hint = page.slotHintTxt.rectTransform;
        var position = hint.position;
        position.y = corners[0].y - root.transform.TransformVector(new Vector3(0, 45, 0)).y;
        hint.position = position;
    }

    private static void ConfigureCoinArt(GameObject root, SlotPanel page)
    {
        var coin = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/BizzaWZ/Final/Real/GameAssets/WzTexture_Money/GoldCoin_US.png");
        if (coin == null) throw new System.InvalidOperationException("Missing leaf-free coin sprite.");
        var balance = root.transform.Find("Top/IconInfo/Image").GetComponent<Image>();
        balance.sprite = coin;
        balance.overrideSprite = null;
        balance.type = Image.Type.Simple;
        balance.preserveAspect = true;
        foreach (var image in page.slotMachineManager.GetComponentsInChildren<Image>(true))
        {
            if (image.sprite == null || image.sprite.name != "Coin") continue;
            image.sprite = coin;
            image.overrideSprite = null;
            image.preserveAspect = true;
        }
        // Persist the same artwork in the reel symbol table used during actual spins.
        var data = new SerializedObject(page.slotMachineManager);
        var symbols = data.FindProperty("slotEntryss");
        for (int i = 0; i < symbols.arraySize; i++)
        {
            var sprite = symbols.GetArrayElementAtIndex(i).FindPropertyRelative("sprite");
            if (sprite.objectReferenceValue is Sprite original && original.name == "Coin")
                sprite.objectReferenceValue = coin;
        }
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void ConfigureHelp(GameObject root)
    {
        // Restore the original ad-reward explanation in the existing two-line guide.
        Copy(root.transform.Find("Content/ReferenceVideoHelp").GetComponent<TMP_Text>(),
            "Watch a video to spin\nfor extra rewards.", "Veja um vídeo e gire\npor mais prêmios.");
    }

    private static void Rect(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }

    private static void Copy(TMP_Text text, string english, string portuguese)
    {
        var legacy = text.GetComponent<UILanguageLabel>();
        if (legacy != null) legacy.enabled = false;
        var localized = text.GetComponent<OrchardLocalizedLabel>() ?? text.gameObject.AddComponent<OrchardLocalizedLabel>();
        var data = new SerializedObject(localized);
        data.FindProperty("target").objectReferenceValue = text;
        data.FindProperty("english").stringValue = english;
        data.FindProperty("portuguese").stringValue = portuguese;
        data.FindProperty("existingKey").stringValue = string.Empty;
        data.ApplyModifiedPropertiesWithoutUndo();
        text.text = english;
    }
}
#endif
