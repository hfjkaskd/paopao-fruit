using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One-time prefab authoring for service, rewards and the slot interface.
/// This editor tool writes existing visual components; no runtime theme or event wiring is added.
/// </summary>
public static class OrchardServiceRewardPass
{
    private static readonly Color Ink = new Color32(23, 79, 125, 255);
    private static readonly Color MutedInk = new Color32(98, 131, 134, 255);
    private static readonly Color Green = new Color32(18, 153, 59, 255);
    private static readonly Color Cream = new Color32(255, 249, 231, 255);
    private static readonly Color Sage = new Color32(218, 230, 195, 255);

    public static void Apply(GameObject root, string assetPath)
    {
        if (root == null) return;
        switch (Path.GetFileNameWithoutExtension(assetPath))
        {
            case "ServicePanel": ApplyService(root); break;
            case "ServiceSelectPanel": ApplyQuickReply(root); break;
            case "ServiceBtn":
                // The root sprite includes the speech bubble; its child is only a red dot.
                // Retain this complete blue icon rather than making the action unlabelled.
                if (root.TryGetComponent<Image>(out var serviceIcon)) serviceIcon.color = Color.white;
                break;
            case "ChatElement": ApplyChat(root); break;
            case "FAQPanel": ApplyFaq(root); break;
            case "DailyMissionPanel": ApplyDailyMission(root); break;
            case "DailyWithdrawPanel": ApplyDailyWithdraw(root); break;
            case "ExchangeRatePanel": ApplyExchange(root); break;
            case "NewbieGiftPage": ApplyNewbie(root); break;
            case "GetRewardPanel": ApplyReward(root); break;
            case "Real_WithdrawProgress": ApplyProgress(root); break;
            case "StarRatingPopup": ApplyRating(root); break;
            case "UIDailyTaskPage": ApplyTaskPage(root); break;
            case "UIDailyTaskElement": ApplyTaskItem(root); break;
            case "UIActivityTaskElement": ApplyActivityItem(root); break;
            case "ItemForCountry": ApplyCountryEntry(root); break;
            case "SlotPanel": ApplySlot(root); break;
            case "SlotFQAPanel": ApplySlotFaq(root); break;
            case "SlotEnter": ApplySlotEntry(root); break;
        }
    }

    private static void ApplyService(GameObject root)
    {
        OrchardSkinAuthoring.EnsureBackdrop(root);
        HidePaint(root, "BG");
        Skin(root, "Title/bg", "Title");
        Title(root, "Title/Title");
        CompactServiceHeader(root);
        Skin(root, "Content/ChatContent", "Panel");
        Skin(root, "Content/InputNode/bg", "Input");
        Skin(root, "Content/InputNode/SelectQuestionBtn ", "ButtonBlue");
        Title(root, "Content/InputNode/SelectQuestionBtn /Text (TMP)");
        // These two sprites contain a send glyph; do not replace them with an empty plate.
        Tint(root, "Content/InputNode/CanSendBtn", Color.white);
        Tint(root, "Content/InputNode/NotCanSendBtn", Sage);
        Body(root, "Content/InputNode/InputField/TextArea/Content/Text");
        Body(root, "Content/InputNode/InputField/TextArea/Content/Processed");
        Body(root, "Content/InputNode/InputField/TextArea/Content/Placeholder", MutedInk);
        foreach (MonoBehaviour component in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (component == null) continue;
            var serialized = new SerializedObject(component);
            SetColor(serialized, "caretColor", Ink);
            SetColor(serialized, "selectionColor", new Color32(130, 206, 239, 150));
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void ApplyQuickReply(GameObject root)
    {
        Skin(root, "Content", "Panel");
        Skin(root, "Content/bg_Content", "Inset");
        Body(root, "Content/bg_Content/Text (TMP)");
        Transform questions = Find(root, "Content/bg_Content/Questions");
        if (questions == null) return;
        for (int i = 0; i < questions.childCount; i++)
        {
            Transform row = questions.GetChild(i);
            Skin(row, "Card");
            foreach (TMP_Text text in row.GetComponentsInChildren<TMP_Text>(true))
            {
                OrchardSkinAuthoring.SetBody(text);
                text.enableWordWrapping = true;
                // The existing 80-unit rows must hold two-line localized questions.
                text.enableAutoSizing = true;
                text.fontSizeMin = 24f;
                text.fontSizeMax = 32f;
                text.margin = new Vector4(24, 4, 24, 4);
            }
        }
    }

    private static void ApplyChat(GameObject root)
    {
        Skin(root, "ChatInfo/Issue_bg", "Card");
        Skin(root, "ChatInfo/Player_bg", "Inset");
        Body(root, "ChatInfo/Text");
        Body(root, "Time", MutedInk);
        foreach (MonoBehaviour component in root.GetComponents<MonoBehaviour>())
        {
            if (component == null) continue;
            var serialized = new SerializedObject(component);
            SetColor(serialized, "issueBubbleColor", Cream);
            SetColor(serialized, "playerBubbleColor", Sage);
            SetColor(serialized, "issueTextColor", Ink);
            SetColor(serialized, "playerTextColor", Ink);
            SetColor(serialized, "timeTextColor", MutedInk);
            // Use the existing runtime layout's serialized design parameters.
            SetFloat(serialized, "outerHorizontalPadding", 28f);
            SetFloat(serialized, "bubbleHorizontalPadding", 30f);
            SetFloat(serialized, "bubbleVerticalPadding", 22f);
            SetFloat(serialized, "bubbleTimeSpacing", 10f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void ApplyFaq(GameObject root)
    {
        Skin(root, "Content (1)/Image", "Inset");
        HidePaint(root, "Content (1)/Scroll View");
        Transform content = Find(root, "Content (1)/Scroll View/Viewport/Content");
        if (content != null)
        {
            foreach (TMP_Text text in content.GetComponentsInChildren<TMP_Text>(true))
            {
                OrchardSkinAuthoring.SetBody(text);
                text.enableWordWrapping = true;
                text.lineSpacing = 7f;
                // The stored editor preview uses literal colors; runtime FAQDesc uses tokens below.
                text.text = text.text.Replace("#C26F50", "#174F7D")
                    .Replace("#B86445", "#174F7D").Replace("#8F5E4A", "#174F7D");
            }
        }
        foreach (MonoBehaviour component in root.GetComponents<MonoBehaviour>())
        {
            if (component == null) continue;
            var serialized = new SerializedObject(component);
            SetString(serialized, "titleColor.replaceValue", "#174F7D");
            SetString(serialized, "contentColor.replaceValue", "#174F7D");
            SetString(serialized, "highlightColor.replaceValue", "#11973B");
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void ApplyDailyMission(GameObject root)
    {
        Skin(root, "Content/bg", "Panel");
        Button(root, "Content/WithdrawBtn", "ButtonGreen");
        Button(root, "Content/GoBtn", "ButtonGreen");
        Button(root, "Content/ClaimedBtn", "ButtonDisabled");
        Body(root, "Content/Hint");
        Body(root, "Content/ClaimHint", MutedInk);
        Body(root, "Title/refreshTimeTxt", Ink);
        // This is the secondary sentence inside the cream body, not the wood heading.
        Body(root, "Title/title");
    }

    private static void ApplyDailyWithdraw(GameObject root)
    {
        Button(root, "Content (1)/WithdrawBtn", "ButtonGreen");
        Skin(root, "Content (1)/Image/Bg", "Inset");
        Body(root, "Content (1)/HintInfo/Hint");
        Body(root, "Content (1)/HintInfo/Num", Green);
        Body(root, "Content (1)/CoinHint (1)/CoinNum/balanceTxt");
        Body(root, "Content (1)/CoinHint (1)/CoinNum/withdrawalTxt", Green);
        Body(root, "Content (1)/CoinHint (1)/CoinNum/=");
    }

    private static void ApplyExchange(GameObject root)
    {
        Button(root, "Content (1)/WithdrawBtn", "ButtonGreen");
        Skin(root, "Content (1)/BeforeState/bg", "DisabledCard");
        Skin(root, "Content (1)/NowState/bg", "SelectedCard");
        Body(root, "Content (1)/BeforeState/Title", MutedInk);
        Body(root, "Content (1)/NowState/Title");
        Body(root, "Content (1)/BeforeState/GameObject/beclash", MutedInk);
        Body(root, "Content (1)/NowState/GameObject/nowclash", Green);
    }

    private static void ApplyNewbie(GameObject root)
    {
        Skin(root, "Panel/BG (2)", "Panel");
        Skin(root, "Panel/MoneyTile", "Inset");
        Button(root, "Panel/Button", "ButtonGreen");
        Body(root, "Panel/Os_UsdText", Green);
        Body(root, "Panel/BG (2)/Text (TMP)");
    }

    private static void ApplyReward(GameObject root)
    {
        // Preserve payment logos, coin artwork, progress transforms and reward fly targets.
        foreach (Image image in root.GetComponentsInChildren<Image>(true))
        {
            string path = AnimationUtility.CalculateTransformPath(image.transform, root.transform);
            if (path.EndsWith("WathAdProgress/bg", StringComparison.Ordinal))
                OrchardSkinAuthoring.ApplySprite(image, "Card");
            else if (path.EndsWith("/progress/bg", StringComparison.Ordinal))
                OrchardSkinAuthoring.ApplySprite(image, "ProgressTrack");
            else if (path.EndsWith("/FillArea/Fill", StringComparison.Ordinal))
                Progress(image, "ProgressFill");
            else if (image.name == "Image_Title_1" || image.name == "Image_Title_2")
                OrchardSkinAuthoring.ApplySprite(image, "Badge");
        }
        Button(root, "NextBtn", "ButtonGreen");
        ButtonSuffix(root, "Content/ButtonAnim/Button", "ButtonGreen");
        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.name == "ProgressHint" || text.name == "ProgressValue" ||
                text.name == "EValue" || text.name == "SValue")
            {
                OrchardSkinAuthoring.SetBody(text);
                string path = AnimationUtility.CalculateTransformPath(text.transform, root.transform);
                // Only WathAdProgress has a cream card; the other progress hints
                // are directly on the dark overlay and need a light foreground.
                if (text.name == "ProgressHint" && !path.Contains("WathAdProgress/"))
                    text.color = Cream;
            }
        }
    }

    private static void ApplyProgress(GameObject root)
    {
        Skin(root, "progress/bg", "ProgressTrack");
        Transform fill = Find(root, "progress/Image");
        if (fill != null) Progress(fill.GetComponent<Image>(), "ProgressFill");
        Body(root, "ProgressHint");
    }

    private static void ApplyRating(GameObject root)
    {
        Skin(root, "Content", "Panel");
        Button(root, "Content/GoBtn", "ButtonGreen");
        Body(root, "Content/Texts");
        Body(root, "Content/Title");
        // The starOn/starOff graphics and their show/hide state remain meaningful.
        foreach (Image image in root.GetComponentsInChildren<Image>(true))
        {
            if (image.transform.parent != null && image.transform.parent.name == "Darks")
                image.color = Sage;
        }
    }

    private static void ApplyTaskPage(GameObject root)
    {
        Skin(root, "Content", "Panel");
        Skin(root, "BG/BG", "Title");
        Title(root, "BG/BG/Text (TMP)");
        Skin(root, "Content/DailyTask/Top", "Inset");
        Skin(root, "Content/DailyTask/Top/BarBG", "ProgressTrack");
        Transform bar = Find(root, "Content/DailyTask/Top/BarBG/Bar");
        if (bar != null) Progress(bar.GetComponent<Image>(), "ProgressFill");
        foreach (string tabName in new[] { "TabDaily", "TabWeek", "TabPlayTime" })
        {
            string path = "TabGroup/" + tabName;
            Skin(root, path, "Card");
            Skin(root, path + "/Select", "ButtonGreen");
            Body(root, path + "/Text (TMP)");
            Title(root, path + "/Select/Text (TMP) (1)");
        }
        Body(root, "Content/DailyTask/Top/refreshTime", MutedInk);
    }

    private static void ApplyTaskItem(GameObject root)
    {
        Skin(root.transform, "Card");
        Button(root, "GotoBtn", "ButtonBlue");
        Button(root, "RewardBtn", "ButtonGreen");
        Button(root, "AdBtn", "ButtonGreen");
        Skin(root, "BarBG", "ProgressTrack");
        Transform bar = Find(root, "BarBG/Bar");
        if (bar != null) Progress(bar.GetComponent<Image>(), "ProgressFill");
        Body(root, "txtDesc");
        Title(root, "BarBG/Text (TMP)");
        Tint(root, "mask", new Color32(87, 118, 108, 62));
    }

    private static void ApplyActivityItem(GameObject root)
    {
        // normal/opened/finished are semantic reward-chest images, not background cards.
        Skin(root, "RewardObj/Content", "Card");
        Body(root, "txt");
    }

    private static void ApplyCountryEntry(GameObject root)
    {
        // Badge and daily mission illustrations identify separate gameplay actions.
        Title(root, "Bottom/Badge/Text (TMP)");
    }

    private static void ApplySlot(GameObject root)
    {
        OrchardSkinAuthoring.EnsureBackdrop(root);
        HidePaint(root, "bg");
        Skin(root, "Content/GetRewadPanel/bg", "Panel");
        Button(root, "Content/GetRewadPanel/Btn", "ButtonGreen");
        Body(root, "Content/GetRewadPanel/Title");
        Body(root, "Content/GetRewadPanel/MoneyRoot/Coin/CoinValue", Green);
        Body(root, "Content/GetRewadPanel/MoneyRoot/Dollar/CoinValue", Green);
        Body(root, "Top/DollarInfo/DollarTxt");
        Body(root, "Top/IconInfo/IconTxt");
        Body(root, "Content/Hint", Cream);
        foreach (UnityEngine.UI.Button button in root.GetComponentsInChildren<UnityEngine.UI.Button>(true))
            foreach (TMP_Text label in button.GetComponentsInChildren<TMP_Text>(true))
                OrchardSkinAuthoring.SetTitle(label);
        // Nested reels, machine frame holes and Spine animation are authored game artwork.
    }

    private static void ApplySlotFaq(GameObject root)
    {
        OrchardSkinAuthoring.EnsureBackdrop(root);
        HidePaint(root, "bg");
        Skin(root, "Content/bg2", "Panel");
        Skin(root, "Content/bg2 (1)", "Inset");
        Button(root, "Content/Btn", "ButtonBlue");
        Body(root, "Content/des", Cream);
        Transform resultRoot = Find(root, "Content/bg2 (1)/root");
        if (resultRoot == null) return;
        foreach (TMP_Text text in resultRoot.GetComponentsInChildren<TMP_Text>(true))
            OrchardSkinAuthoring.SetBody(text);
    }

    private static void ApplySlotEntry(GameObject root)
    {
        // Preserve the 777 icon and its reward animation while unifying the meter.
        foreach (Image image in root.GetComponentsInChildren<Image>(true))
        {
            if (image.name == "bg") OrchardSkinAuthoring.ApplySprite(image, "ProgressTrack");
            else if (image.name == "progress") Progress(image, "ProgressFill");
        }
        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
            OrchardSkinAuthoring.SetTitle(text);
    }

    private static Transform Find(GameObject root, string path) => root.transform.Find(path);

    private static void CompactServiceHeader(GameObject root)
    {
        RectTransform plate = Find(root, "Title/bg") as RectTransform;
        RectTransform title = Find(root, "Title/Title") as RectTransform;
        if (plate == null || title == null) return;
        title.sizeDelta = new Vector2(490, 96);
        plate.anchorMin = title.anchorMin;
        plate.anchorMax = title.anchorMax;
        plate.pivot = title.pivot;
        plate.anchoredPosition = title.anchoredPosition;
        plate.localScale = Vector3.one;
        plate.sizeDelta = new Vector2(570, 154);
    }

    private static void Skin(GameObject root, string path, string role) => Skin(Find(root, path), role);

    private static void Skin(Transform transform, string role)
    {
        if (transform == null) return;
        Image image = transform.GetComponent<Image>();
        if (image != null) OrchardSkinAuthoring.ApplySprite(image, role);
    }

    private static void Progress(Image image, string role)
    {
        if (image == null) return;
        Image.Type type = image.type;
        OrchardSkinAuthoring.ApplySprite(image, role);
        if (type == Image.Type.Filled) image.type = type;
    }

    private static void Button(GameObject root, string path, string role)
    {
        Transform target = Find(root, path);
        if (target != null) Button(target, role);
    }

    private static void ButtonSuffix(GameObject root, string path, string role)
    {
        foreach (Transform target in root.GetComponentsInChildren<Transform>(true))
        {
            string current = AnimationUtility.CalculateTransformPath(target, root.transform);
            if (current.EndsWith(path, StringComparison.Ordinal)) Button(target, role);
        }
    }

    private static void Button(Transform target, string role)
    {
        Image own = target.GetComponent<Image>();
        if (own != null && own.color.a > 0.01f) OrchardSkinAuthoring.ApplySprite(own, role);
        foreach (Image image in target.GetComponentsInChildren<Image>(true))
        {
            if (image == own || image.sprite == null) continue;
            // Preserve ad/play glyphs; only the large backing plate is a skin surface.
            string spriteName = image.sprite.name;
            if (spriteName.StartsWith("Btn_", StringComparison.OrdinalIgnoreCase) ||
                spriteName.StartsWith("Button", StringComparison.OrdinalIgnoreCase))
                OrchardSkinAuthoring.ApplySprite(image, role);
        }
        foreach (TMP_Text text in target.GetComponentsInChildren<TMP_Text>(true))
            OrchardSkinAuthoring.SetTitle(text);
    }

    private static void Body(GameObject root, string path) => Body(root, path, Ink);

    private static void Body(GameObject root, string path, Color color)
    {
        Transform target = Find(root, path);
        if (target == null) return;
        TMP_Text text = target.GetComponent<TMP_Text>();
        if (text == null) return;
        OrchardSkinAuthoring.SetBody(text);
        text.color = color;
    }

    private static void Title(GameObject root, string path)
    {
        Transform target = Find(root, path);
        if (target == null) return;
        TMP_Text text = target.GetComponent<TMP_Text>();
        if (text != null) OrchardSkinAuthoring.SetTitle(text);
    }

    private static void HidePaint(GameObject root, string path)
    {
        Transform target = Find(root, path);
        if (target == null) return;
        Image image = target.GetComponent<Image>();
        if (image != null) image.color = new Color(1, 1, 1, 0);
    }

    private static void Tint(GameObject root, string path, Color color)
    {
        Transform target = Find(root, path);
        if (target == null) return;
        Image image = target.GetComponent<Image>();
        if (image != null) image.color = color;
    }

    private static void SetColor(SerializedObject target, string field, Color value)
    {
        SerializedProperty property = target.FindProperty(field);
        if (property != null && property.propertyType == SerializedPropertyType.Color) property.colorValue = value;
    }

    private static void SetFloat(SerializedObject target, string field, float value)
    {
        SerializedProperty property = target.FindProperty(field);
        if (property != null && property.propertyType == SerializedPropertyType.Float) property.floatValue = value;
    }

    private static void SetString(SerializedObject target, string field, string value)
    {
        SerializedProperty property = target.FindProperty(field);
        if (property != null && property.propertyType == SerializedPropertyType.String) property.stringValue = value;
    }
}
