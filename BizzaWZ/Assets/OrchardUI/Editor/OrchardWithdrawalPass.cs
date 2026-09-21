using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Applies the approved orchard visual language to the existing withdrawal prefabs.
/// Called by the skin authoring pipeline only; never attached to a runtime object.
/// </summary>
public static class OrchardWithdrawalPass
{
    private const string RealContent = "Content/Scroll View/Viewport/Content/";
    private static readonly Color Ink = new Color32(23, 79, 125, 255);
    private static readonly Color Muted = new Color32(105, 137, 139, 255);
    private static readonly Color Cash = new Color32(13, 155, 35, 255);
    private static readonly Color Accent = new Color32(0, 150, 122, 255);
    private static readonly Color Error = new Color32(184, 76, 54, 255);

    public static void Apply(GameObject root, string assetPath)
    {
        if (root == null) return;
        switch (Path.GetFileNameWithoutExtension(assetPath))
        {
            case "FakeWithdrawPanel": ApplyFake(root); break;
            case "WithdrawAmountItem": ApplyAmount(root); break;
            case "RealWithdrawPanel": ApplyReal(root); break;
            case "WithdrawInfo": ApplyInfo(root); break;
            case "WithdrawWay": ApplyWay(root); break;
            case "WithdrawLevel": ApplyLevelList(root); break;
            case "WithdrawLevelItem": ApplyLevelItem(root); break;
            case "DailyBonus": ApplyDailyBonus(root); break;
            case "DailyBonusItem": ApplyDailyBonusItem(root); break;
            case "BonusRate":
                Paint(root, "bg", "Badge");
                Body(root, "Num", Ink);
                break;
            case "WithdrawFillPanel": ApplyFill(root); break;
            case "UIWithdrawalConfirmPanel": ApplyConfirm(root); break;
            case "UIWithdrawalPendingPanel": ApplyPending(root); break;
            case "WithdrawHistory": ApplyHistory(root); break;
            case "WithdrawHistoryItem": ApplyHistoryItem(root); break;
            case "WithdrawDanPanel": ApplyDan(root); break;
            case "WithdrawDanItem": ApplyDanItem(root); break;
        }
    }

    private static void ApplyFake(GameObject root)
    {
        FullScreen(root);
        Paint(root, "Content/Frame", "Panel");
        Header(root, "ButtomGroup/bg", "ButtomGroup/Title");
        Paint(root, "Content/CashBalance/Balance/bg", "Inset");
        Body(root, "Content/CashBalance/Title");
        Body(root, "Content/CashBalance/Balance/Cash", Cash);
        Body(root, "Content/WithdrawAmount/Title");
        Body(root, "Content/WithdrawProgress/Title");
        Body(root, "Content/HintText");
        Paint(root, "Content/WithdrawBtn/bg", "ButtonGreen");
        Title(root, "Content/WithdrawBtn/Text");
        Paint(root, "Content/WithdrawProgress/Progress/bg", "ProgressTrack");
        Fill(root, "Content/WithdrawProgress/Progress/real");
        Title(root, "Content/WithdrawProgress/Progress/real/Text (TMP)");
        HidePaint(root, "Content/WithdrawAmount/Scroll View");
        Transform amountRoot = Find(root, "Content/WithdrawAmount");
        if (amountRoot != null)
        {
            // The page's generic pass writes overrides into its six nested items.
            // Reapply the item presentation here after that pass, including inactive states.
            foreach (Transform item in amountRoot.GetComponentsInChildren<Transform>(true))
                if (item.Find("Amount") != null && item.Find("GetTag") != null && item.Find("GetedTag") != null)
                    ApplyAmount(item.gameObject);
        }
        // Teach_01 addresses Content/WithdrawBtn by its original path: retain it exactly.
    }

    private static void ApplyAmount(GameObject root)
    {
        Paint(root, "bg", "Card");
        Paint(root, "GetTag", "SelectedCard");
        Paint(root, "GetedTag", "DisabledCard");
        Body(root, "Amount");
        Body(root, "GetTag/text", Accent);
        Body(root, "GetedTag/text", Muted);
        TMP_Text starterLabel = Text(root, "GetTag/text");
        if (starterLabel != null)
        {
            RectTransform labelRect = starterLabel.rectTransform;
            labelRect.anchorMin = labelRect.anchorMax = new Vector2(0.5f, 1f);
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.anchoredPosition = new Vector2(0f, -38f);
            labelRect.sizeDelta = new Vector2(236f, 44f);
            starterLabel.alignment = TextAlignmentOptions.Center;
            starterLabel.enableWordWrapping = true;
        }
        // State cards used to cover the amount because they followed it in draw order.
        // Keep every object and its active state; only put the value above its backgrounds.
        Transform amount = Find(root, "Amount");
        if (amount != null) amount.SetAsLastSibling();
        foreach (string state in new[] { "GetTag", "GetedTag" })
        {
            // Suppress only decorations created by the earlier green-button skin.
            // These surfaces are now light cards, while the business state objects remain intact.
            foreach (string leaf in new[] { "OrchardNavLeavesLeft", "OrchardNavLeavesRight" })
            {
                Transform decoration = Find(root, state + "/" + leaf);
                if (decoration != null && decoration.TryGetComponent<Image>(out var image)) image.enabled = false;
            }
        }
        // GetTag, GetedTag and Select are independently controlled business states.
    }

    private static void ApplyReal(GameObject root)
    {
        FullScreen(root);
        Header(root, "Title/Image (1)", "Title/Title");
        string info = RealContent + "WithdrawInfo/";
        Paint(root, info + "bg", "Panel");
        Paint(root, info + "CoinInfo/RealCurrent", "Inset");
        Body(root, info + "CoinInfo/RealCurrent/Text (TMP)", Cash);
        Body(root, info + "CoinInfo/CurrentCount/MyBalance");
        Body(root, info + "CoinInfo/CurrentCount/Balance_Text");
        Body(root, info + "CoinInfo/RateCount/RateNum");
        Body(root, info + "CoinInfo/PassLevelHint");
        Body(root, info + "WithdrawMode/WithdrawWayTitle");
        Body(root, info + "BlanaceHint");
        Body(root, info + "MoreWithdraw_Hint");
        Paint(root, info + "WithdrawBtn/Btn", "ButtonGreen");
        Title(root, info + "WithdrawBtn/Btn/Text (TMP)");
        SetSize(root, info + "WithdrawBtn/Btn", new Vector2(600f, 168f));
        TMP_Text balanceHint = Text(root, info + "BlanaceHint");
        if (balanceHint != null)
        {
            balanceHint.lineSpacing = 4f;
            balanceHint.enableWordWrapping = true;
        }

        Transform paymentRoot = Find(root, info + "WithdrawMode/Content");
        if (paymentRoot != null && paymentRoot.TryGetComponent<GridLayoutGroup>(out var paymentGrid))
        {
            // Two equally sized cards follow the approved reference, with room for the logos.
            paymentGrid.spacing = new Vector2(42f, paymentGrid.spacing.y);
            paymentGrid.childAlignment = TextAnchor.MiddleCenter;
        }
        string levels = RealContent + "WithdrawLevel/";
        HidePaint(root, RealContent + "WithdrawLevel");
        Paint(root, levels + "ProgressInfo/Progress", "ProgressTrack");
        Fill(root, levels + "ProgressInfo/Progress/activeProgress");
        Title(root, levels + "ProgressInfo/Progress/progressValue");
        Body(root, levels + "ProgressInfo/progressHint");
        Body(root, levels + "ProgressInfo/CompleteHint");
        AddPlate(Find(root, levels + "ProgressInfo"), "Title");

        Transform cardGridRoot = Find(root, levels + "Content");
        if (cardGridRoot != null && cardGridRoot.TryGetComponent<GridLayoutGroup>(out var cardGrid))
        {
            cardGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            cardGrid.constraintCount = 2;
            cardGrid.spacing = new Vector2(18f, 18f);
            cardGrid.childAlignment = TextAnchor.UpperCenter;
        }
        if (cardGridRoot != null)
            foreach (Transform item in cardGridRoot)
                ApplyLevelFrameWidths(item.gameObject);

        Paint(root, "WithdrawHint/BG (2)", "Panel");
        Paint(root, "WithdrawHint/BG (2)/Image (2)", "Title");
        Title(root, "WithdrawHint/BG (2)/Text (TMP)");
        Body(root, "WithdrawHint/Content (1)/Hint");
        Paint(root, "WithdrawHint/Content (1)/BtnGroup/GoState", "ButtonGreen");
        Title(root, "WithdrawHint/Content (1)/BtnGroup/GoState/Text (TMP)");
        ApplyRealRuntimePalette(root);
        // The scroll extent and ProgressInfo anchor remain intact. OnOpen positions that
        // anchor at 320 or 470 for country-specific content; art must not override the flow.
    }

    private static void ApplyInfo(GameObject root)
    {
        Paint(root, "bg", "Panel");
        Paint(root, "CoinInfo/RealCurrent/Image", "Inset");
        Body(root, "CoinInfo/RealCurrent/Text (TMP)", Cash);
        Body(root, "CoinInfo/CurrentCount/Text (TMP)");
        Body(root, "CoinInfo/RateCount/Text (TMP)");
        Body(root, "CoinInfo/RateCount/Text (TMP) (1)", Accent);
        Body(root, "WithdrawMode/Title");
        Body(root, "Hint");
        Paint(root, "WithdrawBtn/Btn", "ButtonGreen");
        Title(root, "WithdrawBtn/Btn/Text (TMP)");
        HidePaint(root, "WithdrawMode/Scroll View");
    }

    private static void ApplyWay(GameObject root)
    {
        // Frame is the dynamically replaced PAYMENT LOGO, despite its name. Never skin it.
        Image background = AddPlate(root.transform, "Input");
        if (background != null && root.TryGetComponent<Button>(out var button))
        {
            button.targetGraphic = background;
            background.raycastTarget = true;
        }
        Transform frame = Find(root, "Frame");
        if (frame != null && frame.TryGetComponent<Image>(out var logo))
        {
            logo.color = Color.white;
            logo.preserveAspect = true;
            logo.type = Image.Type.Simple;
        }
        // The legacy payment artwork includes a purple outer border. Cover only its
        // edge with the new ivory frame; retain the runtime logo Image and reference.
        Image logoFrame = Decoration(root.transform, "OrchardLogoFrame");
        OrchardSkinAuthoring.ApplySprite(logoFrame, "Input");
        logoFrame.fillCenter = false;
        Stretch(logoFrame.rectTransform, Vector2.zero, Vector2.zero);
        logoFrame.transform.SetSiblingIndex(frame != null ? frame.GetSiblingIndex() + 1 : 0);
        SelectionBorder(root, "Select");
    }

    private static void ApplyLevelList(GameObject root)
    {
        Paint(root, "bg", "Panel");
        Paint(root, "WithdrawMode/Title/bg", "Title");
        Title(root, "WithdrawMode/Title/Level");
        Title(root, "WithdrawMode/Title/Rate");
    }

    private static void ApplyLevelItem(GameObject root)
    {
        Paint(root, "bg", "Card");
        NormalizePlate(root, "bg");
        Paint(root, "Rate", "Badge");
        Title(root, "Rate/RateText");
        Body(root, "BalanceText");
        Body(root, "Level/Level");

        // Place the compact level caption above the amount, as in the approved card.
        RectTransform level = Rect(root, "Level");
        if (level != null)
        {
            level.anchorMin = level.anchorMax = new Vector2(0, 1);
            level.pivot = new Vector2(0, 1);
            level.anchoredPosition = new Vector2(24, -24);
            level.sizeDelta = new Vector2(234, 52);
        }
        RectTransform levelText = Rect(root, "Level/Level");
        if (levelText != null) Stretch(levelText, new Vector2(0, 0), new Vector2(-8, 0));
        TMP_Text caption = Text(root, "Level/Level");
        if (caption != null)
        {
            caption.alignment = TextAlignmentOptions.MidlineLeft;
            caption.enableAutoSizing = true;
            caption.fontSizeMin = 23;
            caption.fontSizeMax = 30;
            caption.enableWordWrapping = false;
        }
        // The old icon sits left of a long level string; retaining it visually would overlap.
        HidePaint(root, "Level/LevelImage");
        RectTransform amount = Rect(root, "BalanceText");
        if (amount != null)
        {
            amount.anchorMin = new Vector2(0, 0);
            amount.anchorMax = new Vector2(1, 1);
            amount.offsetMin = new Vector2(24, 25);
            amount.offsetMax = new Vector2(-74, -72);
        }

        SoftState(root, "Shadow", 0.33f);
        SoftState(root, "SelectShadow", 0.23f);
        ApplyLevelFrameWidths(root);
        Transform shadow = Find(root, "Shadow");
        if (shadow != null) AddLock(shadow);
        foreach (MonoBehaviour component in root.GetComponents<MonoBehaviour>())
        {
            if (component == null) continue;
            var serialized = new SerializedObject(component);
            // Completed cards use the serialized sage overlay, not a grey material that
            // would desaturate the shared gold multiplier and wood surface at runtime.
            if (serialized.FindProperty("shadowObj") != null && serialized.FindProperty("selectedObj") != null)
            {
                SerializedProperty material = serialized.FindProperty("material");
                if (material != null && material.propertyType == SerializedPropertyType.ObjectReference)
                    material.objectReferenceValue = null;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void ApplyLevelFrameWidths(GameObject root)
    {
        // Keep the card bounds intact while making its sliced wood border about 29% thinner.
        foreach (string path in new[] { "bg", "Shadow", "SelectShadow" })
        {
            Transform target = Find(root, path);
            if (target != null && target.TryGetComponent<Image>(out var image))
                image.pixelsPerUnitMultiplier = 1.4f;
        }
    }

    private static void ApplyDailyBonus(GameObject root)
    {
        Paint(root, "bg", "Panel");
        Body(root, "Title/DailyText");
        Body(root, "Title/Refresh", Muted);
        HidePaint(root, "WithdrawMode/Scroll View");
    }

    private static void ApplyDailyBonusItem(GameObject root)
    {
        Paint(root, "bg", "Card");
        Paint(root, "BonusInfo/progress/bg", "ProgressTrack");
        Fill(root, "BonusInfo/progress/progressImage");
        Body(root, "BonusInfo/IncreadseInfo", Accent);
        Body(root, "BonusInfo/videoInfo");
        Body(root, "BonusInfo/progressCount");
        // Icon_CanClaim / Icon_Claimed contain semantic symbols and are retained.
        Body(root, "GOBtn/Text (TMP)", Accent);
        Body(root, "ClaimedBtn/Text (TMP)", Muted);
        Tint(root, "Image", new Color32(199, 219, 179, 38));
    }

    private static void ApplyFill(GameObject root)
    {
        Paint(root, "Root/FillRoot/BG", "Panel");
        Body(root, "Root/FillRoot/Title");
        Paint(root, "Root/FillRoot/pageContent/BtnWithdrawal", "ButtonGreen");
        Title(root, "Root/FillRoot/pageContent/BtnWithdrawal/Text (TMP)");
        foreach (Image image in root.GetComponentsInChildren<Image>(true))
        {
            string path = AnimationUtility.CalculateTransformPath(image.transform, root.transform);
            if (path.EndsWith("/Background", StringComparison.Ordinal) && path.Contains("/InfoContent/"))
                OrchardSkinAuthoring.ApplySprite(image, "Input");
            else if (path.Contains("/SelectChannel /") && image.transform.parent.name == "SelectChannel ")
                OrchardSkinAuthoring.ApplySprite(image, "Card");
        }
        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            string path = AnimationUtility.CalculateTransformPath(text.transform, root.transform);
            if (!path.Contains("/InfoContent/")) continue;
            OrchardSkinAuthoring.SetBody(text);
            text.color = path.Contains("ErrorHint/") ? Error : text.name == "Placeholder" ? Muted : Ink;
        }
        // Sync only design fields; the AdvancedInputField and country-dependent roots stay intact.
        InputPalette(root);
    }

    private static void ApplyConfirm(GameObject root)
    {
        Paint(root, "BG", "Panel");
        Body(root, "Title/Text (TMP)");
        Body(root, "WithdrawValue", Cash);
        Body(root, "Text (TMP)");
        Paint(root, "BtnOk", "ButtonGreen");
        Title(root, "BtnOk/Text (TMP)");
        Paint(root, "txtContext/Name/Name_input", "Input");
        Paint(root, "txtContext/CPF/account_input", "Input");
        Paint(root, "txtContext/Account/AccountText", "Input");
        Transform details = Find(root, "txtContext");
        if (details != null)
            foreach (TMP_Text text in details.GetComponentsInChildren<TMP_Text>(true))
                OrchardSkinAuthoring.SetBody(text);
    }

    private static void ApplyPending(GameObject root)
    {
        Paint(root, "BG", "Panel");
        Body(root, "Title");
        Body(root, "WithdrawValue", Cash);
        Body(root, "Hint");
        Paint(root, "Progress/ProgressBg", "ProgressTrack");
        Fill(root, "Progress/ProgressFill");
        Title(root, "Progress/ProgressValue");
        Paint(root, "BtnConfirm", "ButtonGreen");
        Title(root, "BtnConfirm/Text");
        // successResultSprite and failResultSprite remain distinct semantic icons; never
        // map them to a decorative plate. Runtime alpha still denotes pending controls.
    }

    private static void ApplyHistory(GameObject root)
    {
        Paint(root, "BG (2)", "Panel");
        Paint(root, "BG (2)/Image (2)", "Title");
        Title(root, "BG (2)/Text (TMP)");
        Body(root, "Content/Text (TMP)");
    }

    private static void ApplyHistoryItem(GameObject root)
    {
        Paint(root, "bg", "Card");
        Body(root, "Infos/Current", Cash);
        Body(root, "Infos/Time", Muted);
        Body(root, "Infos/Name");
        Body(root, "Infos/CPF/CNP");
        Body(root, "Infos/Email");
        Body(root, "Infos/due", Error);
        // These labels sit on retained green/amber/red status badges, not on cream.
        Title(root, "SuccessTag/text");
        Title(root, "ProcessTag/text");
        Title(root, "FailTag/text");
        // Runtime selects the three status tags and the 230/300 row height. Preserve both.
    }

    private static void ApplyDan(GameObject root)
    {
        FullScreen(root);
        Header(root, "Title/bg (1)", "Title/Text");
        string info = RealContent + "WithdrawInfo/";
        Paint(root, info + "bg", "Panel");
        Paint(root, info + "CoinInfo/RealCurrent/Image", "Inset");
        Body(root, info + "CoinInfo/RealCurrent/Text (TMP)", Cash);
        Body(root, info + "Title");
        Paint(root, info + "WithdrawBtn/Btn", "ButtonGreen");
        Title(root, info + "WithdrawBtn/Btn/Text (TMP)");
        Paint(root, RealContent + "WithdrawDan/bg", "Panel");
        HidePaint(root, RealContent + "WithdrawDan/bg/WithdrawMode/Scroll View");
        string instruction = RealContent + "WithdrawalInstruction/";
        Paint(root, instruction + "bg", "Card");
        Paint(root, instruction + "Progress/bg", "ProgressTrack");
        Fill(root, instruction + "Progress/real");
        Title(root, instruction + "Progress/progressText");
        Body(root, instruction + "Hint");
        Body(root, instruction + "Title/DailyText");
    }

    private static void ApplyDanItem(GameObject root)
    {
        Paint(root, "bg", "Card");
        Paint(root, "Icon/Image (2)", "Badge");
        Body(root, "Hint");
        Body(root, "Num");
        Title(root, "Icon/Text (TMP)");
        Paint(root, "progress/bg", "ProgressTrack");
        Fill(root, "progress/progress");
        Paint(root, "ClaimBtn/Image", "ButtonGreen");
        Paint(root, "prepareBtn/Image", "ButtonBlue");
        Paint(root, "ClaimedBtn/Image", "ButtonDisabled");
        Title(root, "ClaimBtn/Text (TMP)");
        Title(root, "prepareBtn/Text (TMP)");
        Body(root, "ClaimedBtn/Text (TMP)", Muted);
    }

    private static void ApplyRealRuntimePalette(GameObject root)
    {
        foreach (MonoBehaviour component in root.GetComponents<MonoBehaviour>())
        {
            if (component == null) continue;
            var serialized = new SerializedObject(component);
            SetSprite(serialized, "normalSprite", "ButtonGreen");
            SetSprite(serialized, "canWithdrawSprite", "ButtonGreen");
            SetString(serialized, "withdrawValueKeyColor", "#00967AFF");
            SetString(serialized, "withdrawChannelKeyColor", "#007B91FF");
            SerializedProperty replacements = serialized.FindProperty("_colorReplaceList");
            if (replacements != null && replacements.isArray)
            {
                for (int i = 0; i < replacements.arraySize; i++)
                {
                    SerializedProperty entry = replacements.GetArrayElementAtIndex(i);
                    SerializedProperty key = entry.FindPropertyRelative("replaceKey");
                    SerializedProperty value = entry.FindPropertyRelative("replaceValue");
                    if (key == null || value == null) continue;
                    switch (key.stringValue)
                    {
                        case "progressLevelColor": value.stringValue = "#174F7DFF"; break;
                        case "progressClashColor": value.stringValue = "#00967AFF"; break;
                        case "progressRatioColor": value.stringValue = "#AF741EFF"; break;
                    }
                }
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void InputPalette(GameObject root)
    {
        foreach (MonoBehaviour component in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (component == null) continue;
            var serialized = new SerializedObject(component);
            SerializedProperty caret = serialized.FindProperty("caretColor");
            if (caret != null && caret.propertyType == SerializedPropertyType.Color) caret.colorValue = Ink;
            SerializedProperty selection = serialized.FindProperty("selectionColor");
            if (selection != null && selection.propertyType == SerializedPropertyType.Color)
                selection.colorValue = new Color32(129, 209, 239, 140);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void SelectionBorder(GameObject root, string path)
    {
        Transform target = Find(root, path);
        if (target == null || !target.TryGetComponent<Image>(out var image)) return;
        Transform existingCheck = target.Find("OrchardCheck");
        Sprite checkSprite = existingCheck != null && existingCheck.TryGetComponent<Image>(out var existingImage)
            ? existingImage.sprite : image.sprite;
        OrchardSkinAuthoring.ApplySprite(image, "SelectionRing");
        image.raycastTarget = false;
        Stretch((RectTransform)target, Vector2.zero, Vector2.zero);
        Image check = Decoration(target, "OrchardCheck");
        check.sprite = checkSprite;
        check.type = Image.Type.Simple;
        check.preserveAspect = true;
        check.color = Color.white;
        RectTransform rect = check.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(1, 0);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(-36, 36);
        rect.sizeDelta = new Vector2(56, 56);
    }

    private static void AddLock(Transform parent)
    {
        Sprite lockSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/BizzaWZ/Common/BizzaGame/Z_ReplaceAssets/UI_Frame/GamePanel/booster_right_lock.png");
        if (lockSprite == null) return;
        Image image = Decoration(parent, "OrchardLock");
        image.sprite = lockSprite;
        image.color = new Color32(114, 148, 142, 255);
        image.preserveAspect = true;
        RectTransform rect = image.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(1, 0);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(-42, 48);
        rect.sizeDelta = new Vector2(36, 42);
    }

    private static Image Decoration(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        GameObject target;
        if (existing != null) target = existing.gameObject;
        else
        {
            target = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            target.layer = parent.gameObject.layer;
            target.transform.SetParent(parent, false);
        }
        Image image = target.GetComponent<Image>();
        if (image == null) image = target.AddComponent<Image>();
        image.raycastTarget = false;
        return image;
    }

    private static Image AddPlate(Transform target, string role)
    {
        if (target == null) return null;
        Image image = target.GetComponent<Image>();
        if (image == null) image = target.gameObject.AddComponent<Image>();
        OrchardSkinAuthoring.ApplySprite(image, role);
        image.raycastTarget = false;
        return image;
    }

    private static void SoftState(GameObject root, string path, float alpha)
    {
        Paint(root, path, "DisabledCard");
        NormalizePlate(root, path);
        Tint(root, path, new Color(0.88f, 0.96f, 0.79f, alpha));
    }

    private static void NormalizePlate(GameObject root, string path)
    {
        RectTransform rect = Rect(root, path);
        if (rect != null) Stretch(rect, Vector2.zero, Vector2.zero);
    }

    private static void Header(GameObject root, string platePath, string titlePath)
    {
        Paint(root, platePath, "Title");
        Title(root, titlePath);
        RectTransform plate = Rect(root, platePath);
        RectTransform title = Rect(root, titlePath);
        if (plate == null || title == null) return;
        title.sizeDelta = new Vector2(490, 96);
        plate.anchorMin = title.anchorMin;
        plate.anchorMax = title.anchorMax;
        plate.pivot = title.pivot;
        plate.anchoredPosition = title.anchoredPosition;
        plate.localScale = Vector3.one;
        plate.sizeDelta = new Vector2(570, 154);
    }

    private static void FullScreen(GameObject root)
    {
        OrchardSkinAuthoring.EnsureBackdrop(root);
        HidePaint(root, "BG");
    }

    private static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.localScale = Vector3.one;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = min;
        rect.offsetMax = max;
    }

    private static Transform Find(GameObject root, string path) => root.transform.Find(path);
    private static RectTransform Rect(GameObject root, string path) => Find(root, path) as RectTransform;
    private static TMP_Text Text(GameObject root, string path)
    {
        Transform target = Find(root, path);
        return target != null ? target.GetComponent<TMP_Text>() : null;
    }

    private static void SetSize(GameObject root, string path, Vector2 size)
    {
        RectTransform rect = Rect(root, path);
        if (rect != null) rect.sizeDelta = size;
    }

    private static void Paint(GameObject root, string path, string role)
    {
        Transform target = Find(root, path);
        if (target != null && target.TryGetComponent<Image>(out var image))
            OrchardSkinAuthoring.ApplySprite(image, role);
    }

    private static void Fill(GameObject root, string path)
    {
        Transform target = Find(root, path);
        if (target == null || !target.TryGetComponent<Image>(out var image)) return;
        Image.Type type = image.type;
        OrchardSkinAuthoring.ApplySprite(image, "ProgressFill");
        if (type == Image.Type.Filled) image.type = type;
    }

    private static void Body(GameObject root, string path) => Body(root, path, Ink);
    private static void Body(GameObject root, string path, Color color)
    {
        TMP_Text text = Text(root, path);
        if (text == null) return;
        OrchardSkinAuthoring.SetBody(text);
        text.color = color;
    }

    private static void Title(GameObject root, string path)
    {
        TMP_Text text = Text(root, path);
        if (text != null) OrchardSkinAuthoring.SetTitle(text);
    }

    private static void Tint(GameObject root, string path, Color color)
    {
        Transform target = Find(root, path);
        if (target != null && target.TryGetComponent<Image>(out var image)) image.color = color;
    }

    private static void HidePaint(GameObject root, string path) => Tint(root, path, new Color(1, 1, 1, 0));

    private static void SetString(SerializedObject target, string field, string value)
    {
        SerializedProperty property = target.FindProperty(field);
        if (property != null && property.propertyType == SerializedPropertyType.String) property.stringValue = value;
    }

    private static void SetSprite(SerializedObject target, string field, string role)
    {
        SerializedProperty property = target.FindProperty(field);
        if (property == null || property.propertyType != SerializedPropertyType.ObjectReference) return;
        Sprite sprite = OrchardSkinAuthoring.SpriteFor(role);
        if (sprite != null) property.objectReferenceValue = sprite;
    }
}
