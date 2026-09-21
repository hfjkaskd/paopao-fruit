using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using SnakeEscape.Recovered;

/// <summary>
/// Prefab authoring only. The caller loads and saves each prefab once; this pass
/// changes serialized presentation and never changes runtime initialization or events.
/// </summary>
public static class OrchardCommonPass
{
    public static void Apply(GameObject root, string assetPath)
    {
        if (root == null || string.IsNullOrEmpty(assetPath)) return;
        string path = assetPath.Replace('\\', '/');
        if (!path.StartsWith("Assets/BizzaWZ/", StringComparison.Ordinal)) return;

        switch (Path.GetFileNameWithoutExtension(path))
        {
            case "LoadingPanel": ApplyLoading(root); break;
            case "PausePanel": ApplyPause(root); break;
            case "LosePanel": ApplyLose(root); break;
            case "WhiteWinPanel": ApplyVictory(root); break;
            case "AddPropPanel": ApplyAddProp(root); break;
            case "RealGamePanel": ApplyGamePanel(root); break;
            case "GameUiWidget": ApplyWidget(root); break;
            case "UIPropEntry": ApplyPropEntry(root); break;
            case "UIItem": ApplyItem(root); break;
            case "CurrencyBar": ApplyCurrency(root); break;
            case "BroadCastBar": ApplyBroadcast(root); break;
            case "CommonConfirmTipsPanel": ApplyConfirm(root); break;
            case "BG":
                if (path.Contains("/MenuSystem/Common/UICommons/")) ApplyDialogFrame(root.transform);
                break;
            case "UITeachTipsPage":
                Paint(root, "Panel", "Card");
                Body(root, "Panel/Text");
                break;
            case "UITeachMaskFocusPage":
                PaintNamedImage(root, "Frame", "SelectionRing");
                break;
            case "UITeachMaskPage":
            case "UITeachFingerMovePage":
                // Finger graphics and mask internals are functional tutorial assets.
                break;
            case "PageMask":
            case "Mask":
                // The transparency, stencil masks and finger graphics carry tutorial
                // behavior. Only caption materials are part of the theme here.
                foreach (var text in root.GetComponentsInChildren<TMP_Text>(true)) OrchardSkinAuthoring.SetTitle(text);
                break;
        }
    }

    private static void ApplyGamePanel(GameObject root)
    {
        Paint(root, "BG/Image (2)", "Title");
        var background = Find(root, "BG");
        if (background == null) return;
        // This decoration overlaps the HUD; it must never consume its clicks.
        foreach (var graphic in background.GetComponentsInChildren<Graphic>(true))
            graphic.raycastTarget = false;
    }

    private static void ApplyLoading(GameObject root)
    {
        OrchardSkinAuthoring.EnsureBackdrop(root);
        DisableGraphic(root, "BG");
        DisableGraphic(root, "HarvestLoadingVisual/bg");
        // Keep CameraObj, video components, progress scripts, privacy controls,
        // and their references alive. Only the obsolete rendered decoration is hidden.
        DisableNamedGraphic(root, "LoadingAnim_Raw");
        Paint(root, "LoadingBar/Image", "ProgressTrack");
        PaintProgress(root, "LoadingBar/Image/Fill");
        Paint(root, "HarvestLoadingVisual/MidContent/ProgressBg", "ProgressTrack");
        PaintProgress(root, "HarvestLoadingVisual/MidContent/ProgressBg/ProgressFg");
        Paint(root, "HarvestLoadingVisual/MidContent/PrivacyPolicy/PlayBtn/Bg", "ButtonGreen");
        DisableGraphic(root, "HarvestLoadingVisual/MidContent/PrivacyPolicy/PlayBtn/Image");
        var play = Find(root, "HarvestLoadingVisual/MidContent/PrivacyPolicy/PlayBtn");
        var playBackground = Find(root, "HarvestLoadingVisual/MidContent/PrivacyPolicy/PlayBtn/Bg");
        if (play != null && playBackground != null && play.TryGetComponent<Button>(out var playButton) &&
            playBackground.TryGetComponent<Image>(out var playImage))
        {
            playButton.targetGraphic = playImage;
            playImage.raycastTarget = true;
        }
        StyleBodyLabels(root);
        Title(root, "HarvestLoadingVisual/MidContent/PrivacyPolicy/PlayBtn/Image/LevelTextShadow/LevelText");
        var shadow = Find(root, "HarvestLoadingVisual/MidContent/PrivacyPolicy/PlayBtn/Image/LevelTextShadow");
        if (shadow != null && shadow.TryGetComponent<TMP_Text>(out var shadowText))
        {
            OrchardSkinAuthoring.SetBody(shadowText);
            shadowText.color = new Color32(22, 103, 30, 255);
        }
        // This is the original game's brand illustration, deliberately retained.
        // No text, privacy links, level flow, or account initialization is rewritten.
    }

    private static void ApplyPause(GameObject root)
    {
        Paint(root, "", "Panel");
        StyleBodyLabels(root);
        ThemeDirectDialogFrames(root);
        foreach (var image in root.GetComponentsInChildren<Image>(true))
        {
            string name = image.name;
            if (name == "MusicGroup" || name == "SoundGroup" || name == "LibGroup")
                SetImage(image, "ButtonRoundCream");
            if (name == "MusicOn" || name == "SoundOn" || name == "LibOn")
                SetStatePlatePreservingGlyph(image, "ButtonRoundBlue");
            if (name == "MusicOff" || name == "SoundOff" || name == "LIboff")
                SetStatePlatePreservingGlyph(image, "ButtonDisabled");
        }
        foreach (var button in root.GetComponentsInChildren<Button>(true))
        {
            if (button.name == "ContinueBtn") SetButton(button, "ButtonGreen");
            else if (button.name == "BackButton") SetButton(button, "ButtonBlue");
            else if (button.name == "CloseButton") SetIconButton(button, "ButtonRoundBlue");
            else if (button.transform.parent != null &&
                     (button.transform.parent.name == "MusicGroup" || button.transform.parent.name == "SoundGroup" || button.transform.parent.name == "LibGroup"))
                SetIconButton(button, "ButtonRoundBlue");
        }
        PaintNamedImage(root, "Dropdown", "Input");
        foreach (var image in root.GetComponentsInChildren<Image>(true))
        {
            if (image.name == "Image (2)" && image.transform.parent != null && image.transform.parent.name == "MainPauseGroup")
                SetImage(image, "Inset");
        }
        foreach (var dropdown in root.GetComponentsInChildren<TMP_Dropdown>(true))
        {
            if (dropdown.template != null && dropdown.template.TryGetComponent<Image>(out var background))
                SetImage(background, "Card");
        }
    }

    private static void ApplyLose(GameObject root)
    {
        Paint(root, "bg", "Panel");
        Paint(root, "bg/Image", "Title");
        StyleBodyLabels(root);
        Title(root, "bg/Image/Title");
        foreach (var button in root.GetComponentsInChildren<Button>(true))
        {
            if (button.name == "ReviveBtn") SetButton(button, "ButtonGreen");
            else if (button.name == "LoseBtn") SetButton(button, "ButtonBlue");
        }
    }

    private static void ApplyVictory(GameObject root)
    {
        // This legacy page uses RawImage and reloads a ScriptableObject skin in
        // OnAwake. A themed prefab alone would be overwritten immediately.
        var panel = root.GetComponent<RecoveredVictoryPanel>();
        if (panel == null) return;
        var serializedPanel = new SerializedObject(panel);
        var skinProperty = serializedPanel.FindProperty("skin");
        var source = skinProperty != null ? skinProperty.objectReferenceValue as RecoveredVictoryPanelSkin : null;
        if (skinProperty == null) throw new InvalidOperationException("Victory panel skin field is missing.");
        const string destination = "Assets/OrchardUI/Generated/OrchardVictoryPanelSkin.asset";
        var skin = AssetDatabase.LoadAssetAtPath<RecoveredVictoryPanelSkin>(destination);
        if (skin == null)
        {
            EnsureAssetFolder("Assets/OrchardUI/Generated");
            skin = source != null ? UnityEngine.Object.Instantiate(source) : ScriptableObject.CreateInstance<RecoveredVictoryPanelSkin>();
            skin.name = "OrchardVictoryPanelSkin";
            AssetDatabase.CreateAsset(skin, destination);
        }
        Sprite panelSprite = OrchardSkinAuthoring.SpriteFor("Panel");
        Sprite buttonSprite = OrchardSkinAuthoring.SpriteFor("ButtonGreen");
        if (panelSprite != null) skin.panel = panelSprite.texture;
        if (buttonSprite != null) skin.nextButton = buttonSprite.texture;
        skin.bodyTextColor = new Color32(23, 79, 125, 255);
        skin.titleTextColor = new Color32(23, 79, 125, 255);
        skin.nextTextColor = Color.white;
        skin.outlineColor = new Color32(105, 56, 22, 255);
        EditorUtility.SetDirty(skin);
        skinProperty.objectReferenceValue = skin;
        serializedPanel.ApplyModifiedPropertiesWithoutUndo();
        // Runtime only assigns RawImage.texture and leaves the authored UV rect
        // intact, so atlas regions work without changing the runtime component.
        SetRawSprite(root, "Card", panelSprite);
        SetRawSprite(root, "Card/NextButton", buttonSprite);
        var hero = Find(root, "VictoryHero");
        if (hero != null && hero.TryGetComponent<RawImage>(out var heroImage))
        {
            if (skin.hero == null && heroImage.texture is Texture2D existingHero) skin.hero = existingHero;
            if (skin.hero == null)
            {
                // The old decorative hero texture has a dead asset GUID. Preserve
                // its object/reference for the runtime page, but do not draw a white
                // fallback rectangle or invent a gameplay character.
                heroImage.enabled = false;
                var serializedHero = new SerializedObject(heroImage);
                serializedHero.FindProperty("m_Texture").objectReferenceValue = null;
                serializedHero.ApplyModifiedPropertiesWithoutUndo();
                skin.heroResource = string.Empty;
            }
            else
            {
                heroImage.texture = skin.hero;
                heroImage.enabled = true;
            }
        }
        var titleLabel = Find(root, "Card/Title");
        var bodyLabel = Find(root, "Card/Body");
        var nextLabel = Find(root, "Card/NextButton/Label");
        if (titleLabel != null && titleLabel.TryGetComponent<TMP_Text>(out var actualTitle)) skin.titleFontSize = actualTitle.fontSize;
        if (bodyLabel != null && bodyLabel.TryGetComponent<TMP_Text>(out var actualBody)) skin.bodyFontSize = actualBody.fontSize;
        if (nextLabel != null && nextLabel.TryGetComponent<TMP_Text>(out var actualNext)) skin.nextFontSize = actualNext.fontSize;
        EditorUtility.SetDirty(skin);
        Body(root, "Card/Body");
        // The title lives inside the light panel rather than on a wood title strip.
        Body(root, "Card/Title");
        Title(root, "Card/NextButton/Label");
    }

    private static void ApplyAddProp(GameObject root)
    {
        StyleBodyLabels(root);
        ThemeDirectDialogFrames(root);
        Paint(root, "Content/BtnGroup/ButtonAnim/AdBtn", "ButtonGreen");
        var close = Find(root, "Content/CloseBtn");
        if (close != null && close.TryGetComponent<Button>(out var closeButton)) SetIconButton(closeButton, "ButtonRoundBlue");
        Title(root, "Content/BtnGroup/ButtonAnim/AdBtn/Text (TMP)");
        var icon = Find(root, "Content/PropIcon");
        if (icon != null && icon.TryGetComponent<Image>(out var propImage)) propImage.preserveAspect = true;

        // Author the existing button parts together; runtime text and reward binding stay intact.
        var group = Find(root, "Content/BtnGroup") as RectTransform;
        if (group != null) group.anchoredPosition = new Vector2(0f, -382f);
        var animationRoot = Find(root, "Content/BtnGroup/ButtonAnim") as RectTransform;
        if (animationRoot != null) animationRoot.anchoredPosition = Vector2.zero;
        var adButton = Find(root, "Content/BtnGroup/ButtonAnim/AdBtn") as RectTransform;
        if (adButton != null)
        {
            adButton.anchoredPosition = Vector2.zero;
            adButton.sizeDelta = new Vector2(460f, 150f);
        }
        var free = Find(root, "Content/BtnGroup/ButtonAnim/AdBtn/Text (TMP)");
        if (free != null && free.TryGetComponent<TMP_Text>(out var freeLabel))
        {
            var rect = freeLabel.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2(49f, 0f);
            rect.sizeDelta = new Vector2(218f, 90f);
            freeLabel.fontSize = 56f;
            freeLabel.enableAutoSizing = true;
            freeLabel.fontSizeMin = 28f;
            freeLabel.fontSizeMax = 56f;
            freeLabel.enableWordWrapping = false;
            freeLabel.alignment = TextAlignmentOptions.Center;
        }
        var adIcon = Find(root, "Content/BtnGroup/ButtonAnim/AdBtn/Image (3)");
        if (adIcon != null && adIcon.TryGetComponent<Image>(out var adImage))
        {
            var rect = adImage.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2(-104f, 0f);
            rect.sizeDelta = new Vector2(84f, 86f);
            adImage.preserveAspect = true;
        }
        var rewardBadge = Find(root, "Content/BtnGroup/ButtonAnim/Pop") as RectTransform;
        if (rewardBadge != null)
        {
            rewardBadge.anchoredPosition = new Vector2(221f, 68f);
            rewardBadge.localScale = new Vector3(.48f, .48f, 1f);
        }
        var limit = Find(root, "Content/Limination");
        if (limit != null && limit.TryGetComponent<TMP_Text>(out var limitLabel))
        {
            var rect = limitLabel.rectTransform;
            rect.sizeDelta = new Vector2(650f, 42f);
            rect.anchoredPosition = new Vector2(0f, -524f);
            limitLabel.fontSize = 32f;
            limitLabel.enableAutoSizing = true;
            limitLabel.fontSizeMin = 24f;
            limitLabel.fontSizeMax = 32f;
            limitLabel.enableWordWrapping = false;
            limitLabel.alignment = TextAlignmentOptions.Center;
        }
        // PropIcon and the reward-ad graphic remain semantic, dynamic sprites.
    }

    private static void ApplyWidget(GameObject root)
    {
        var currency = Find(root, "CurrencyBar");
        if (currency != null) ApplyCurrency(currency.gameObject);
        Paint(root, "TaskButton", "ButtonRoundBlue");
        Paint(root, "DailyMissionItem/DailyMission/Image (1)", "ButtonRoundBlue");
        Paint(root, "DailyMissionItem/Badge/Image (3)", "Badge");
        Title(root, "DailyMissionItem/Badge/Text (TMP)");
        // Never add a full-screen background to this overlay on the game world.
    }

    private static void ApplyItem(GameObject root)
    {
        // The missing legacy background and font are presentation assets. The
        // distinct ItemIcon is business data and is not replaced with an invented icon.
        Paint(root, "BG", "Card");
        StyleBodyLabels(root);
    }

    private static void ApplyPropEntry(GameObject root)
    {
        var buttonTransform = Find(root, "Button");
        var background = Find(root, "BG");
        if (buttonTransform != null && buttonTransform.TryGetComponent<Button>(out var button))
        {
            // Preserve the legacy BG/PropIcon and BG/LockIcon paths and all serialized
            // references. The actual visible hit surface is the original Button's
            // Image; the old frame is hidden and semantic glyphs do not block it.
            if (background != null)
            {
                if (background.TryGetComponent<Image>(out var legacyImage)) legacyImage.enabled = false;
                foreach (var graphic in background.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
                buttonTransform.SetAsFirstSibling();
            }
            SetButton(button, "ButtonRoundBlue");
        }
        Paint(root, "HaveTips", "Badge");
        Paint(root, "AddTips", "ButtonGreen");
        Title(root, "HaveTips/HaveCount");
        Title(root, "AddTips/AddCount");
        var lockText = Find(root, "BG/LockIcon/UnlockLevelText");
        if (lockText != null && lockText.TryGetComponent<TMP_Text>(out var label)) OrchardSkinAuthoring.SetBody(label);
    }

    private static void ApplyCurrency(GameObject root)
    {
        Paint(root, "CurrentGroup/GoldGroup/RealBtn/CoinBox/BG (1)", "Inset");
        Paint(root, "CurrentGroup/DollarGroup/FakeBtn/DollarBox/BG", "Inset");
        Paint(root, "CurrentGroup/GoldGroup/RealBtn/ButtonView", "ButtonGreen");
        Paint(root, "CurrentGroup/DollarGroup/FakeBtn/ButtonView", "ButtonGreen");
        var pause = Find(root, "PauseButton");
        if (pause != null && pause.TryGetComponent<Button>(out var pauseButton)) SetIconButton(pauseButton, "ButtonRoundBlue");
        Paint(root, "Image", "Badge");
        Paint(root, "CurrentGroup/DollarGroup/Panel", "Card");
        PaintPreservingAlpha(root, "CurrentGroup/DollarGroup/FakeBtn/DollarBox", "Inset");
        Body(root, "CurrentGroup/GoldGroup/RealBtn/CoinBox/GoldText");
        Body(root, "CurrentGroup/DollarGroup/FakeBtn/DollarBox/DollarText");
        FitCurrencyAmount(root, "CurrentGroup/GoldGroup/RealBtn/CoinBox/GoldText");
        FitCurrencyAmount(root, "CurrentGroup/DollarGroup/FakeBtn/DollarBox/DollarText");
        SimpleSprite(root, "CurrentGroup/GoldGroup/RealBtn/ButtonView");
        SimpleSprite(root, "CurrentGroup/DollarGroup/FakeBtn/ButtonView");
        Body(root, "CurrentGroup/DollarGroup/Panel/Text (TMP)");
        Title(root, "CurrentGroup/GoldGroup/RealBtn/ButtonView/Text (TMP)");
        Title(root, "CurrentGroup/GoldGroup/RealBtn/ButtonView/Text (TMP) (1)");
        Title(root, "CurrentGroup/DollarGroup/FakeBtn/ButtonView/Text (TMP)");
        Title(root, "Image/Text (TMP)");
        // Currency pictures, values, animated deltas and account routing stay intact.
    }

    private static void ApplyBroadcast(GameObject root)
    {
        Paint(root, "", "Inset");
        Paint(root, "TextItem", "Card");
        Paint(root, "EntryItem", "Panel");
        StyleBodyLabels(root);
    }

    private static void ApplyConfirm(GameObject root)
    {
        ThemeDirectDialogFrames(root);
        StyleBodyLabels(root);
        Paint(root, "Content/ConfirmBtn", "ButtonGreen");
        Title(root, "Content/ConfirmBtn/Text (TMP)");
        // Two legacy children both have the name Image (2). Use their actual size
        // to distinguish the title plaque from the inner description panel.
        var frame = Find(root, "BG (1)");
        if (frame != null)
        {
            foreach (var image in frame.GetComponentsInChildren<Image>(true))
            {
                if (image.transform == frame || image.name != "Image (2)") continue;
                SetImage(image, image.rectTransform.sizeDelta.y < 250f ? "Title" : "Inset");
            }
            var text = frame.Find("Text (TMP)");
            if (text != null && text.TryGetComponent<TMP_Text>(out var title))
            {
                OrchardSkinAuthoring.SetTitle(title);
                EnsureTitlePlaque(title);
            }
        }
    }

    private static void EnsureTitlePlaque(TMP_Text title)
    {
        var parent = title.transform.parent;
        if (parent == null) return;
        var existing = parent.Find("OrchardTitlePlaque");
        var plaque = existing != null ? existing.GetComponent<Image>() : null;
        if (plaque == null)
        {
            var go = new GameObject("OrchardTitlePlaque", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = title.gameObject.layer;
            go.transform.SetParent(parent, false);
            plaque = go.GetComponent<Image>();
        }
        var source = title.rectTransform;
        var rect = plaque.rectTransform;
        rect.anchorMin = source.anchorMin;
        rect.anchorMax = source.anchorMax;
        rect.pivot = source.pivot;
        rect.anchoredPosition = source.anchoredPosition;
        rect.sizeDelta = new Vector2(760f, 190f);
        rect.localScale = Vector3.one;
        plaque.transform.SetAsLastSibling();
        plaque.transform.SetSiblingIndex(title.transform.GetSiblingIndex());
        SetImage(plaque, "Title");
        plaque.enabled = true;
        plaque.raycastTarget = false;
    }

    private static void SimpleSprite(GameObject root, string path)
    {
        var target = Find(root, path);
        if (target != null && target.TryGetComponent<Image>(out var image)) image.type = Image.Type.Simple;
    }

    private static void FitCurrencyAmount(GameObject root, string path)
    {
        var target = Find(root, path);
        if (target == null || !target.TryGetComponent<TMP_Text>(out var text)) return;
        text.enableAutoSizing = true;
        text.fontSizeMin = 20f;
        text.fontSizeMax = 40f;
        text.enableWordWrapping = false;
    }

    private static void ThemeDirectDialogFrames(GameObject root)
    {
        for (int i = 0; i < root.transform.childCount; i++)
        {
            var child = root.transform.GetChild(i);
            if (child.name == "BG" || child.name == "BG (1)") ApplyDialogFrame(child);
        }
    }

    private static void ApplyDialogFrame(Transform frame)
    {
        if (frame.TryGetComponent<Image>(out var panel)) SetImage(panel, "Panel");
        for (int i = 0; i < frame.childCount; i++)
        {
            var child = frame.GetChild(i);
            if (child.name == "Image (2)" && child.TryGetComponent<Image>(out var image))
                SetImage(image, image.rectTransform.sizeDelta.y < 250f ? "Title" : "Inset");
            if (child.name == "Text (TMP)" && child.TryGetComponent<TMP_Text>(out var title))
                OrchardSkinAuthoring.SetTitle(title);
        }
    }

    private static void SetButton(Button button, string role)
    {
        if (button == null) return;
        var image = button.GetComponent<Image>();
        if (image == null) image = button.targetGraphic as Image;
        if (image != null)
        {
            SetImage(image, role);
            image.enabled = true;
            image.raycastTarget = true;
            button.targetGraphic = image;
        }
        foreach (var text in button.GetComponentsInChildren<TMP_Text>(true)) OrchardSkinAuthoring.SetTitle(text);
    }

    private static void SetIconButton(Button button, string role)
    {
        if (button == null) return;
        var image = button.GetComponent<Image>();
        if (image == null)
        {
            // Add only a serialized visual to an existing standard Button. Its
            // runtime object, component, listeners and script references stay intact.
            image = button.gameObject.AddComponent<Image>();
        }
        PreserveOwnGlyphWhenNeeded(image);
        SetImage(image, role);
        image.enabled = true;
        image.raycastTarget = true;
        button.targetGraphic = image;
    }

    private static void SetStatePlatePreservingGlyph(Image plate, string role)
    {
        PreserveOwnGlyphWhenNeeded(plate);
        SetImage(plate, role);
        // The existing MusicOn/Off, SoundOn/Off and LibOn/Off GameObjects keep
        // their nested glyphs and their original script-controlled active state.
    }

    private static void PreserveOwnGlyphWhenNeeded(Image image)
    {
        if (image == null || image.sprite == null) return;
        // An existing glyph child is the authored semantic image. Preserve it;
        // the parent is just the plate. All current Pause switches use this form.
        foreach (var child in image.GetComponentsInChildren<Image>(true))
        {
            if (child == image || child.sprite == null) continue;
            string childPath = AssetDatabase.GetAssetPath(child.sprite).Replace('\\', '/');
            if (child.name == "OrchardGlyph" || !childPath.StartsWith("Assets/OrchardUI/", StringComparison.Ordinal)) return;
        }
        string path = AssetDatabase.GetAssetPath(image.sprite).Replace('\\', '/');
        // The main pass already replaces known empty plates with Orchard sprites.
        // They have no glyph to preserve and must not be copied as an icon.
        if (path.StartsWith("Assets/OrchardUI/", StringComparison.Ordinal)) return;
        var existing = image.transform.Find("OrchardGlyph");
        if (existing != null) return;
        var glyphObject = new GameObject("OrchardGlyph", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        glyphObject.layer = image.gameObject.layer;
        glyphObject.transform.SetParent(image.transform, false);
        var rect = (RectTransform)glyphObject.transform;
        rect.anchorMin = new Vector2(.18f, .18f);
        rect.anchorMax = new Vector2(.82f, .82f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        var glyph = glyphObject.GetComponent<Image>();
        glyph.sprite = image.sprite;
        glyph.color = image.color;
        glyph.type = Image.Type.Simple;
        glyph.preserveAspect = true;
        glyph.raycastTarget = false;
        glyph.maskable = image.maskable;
    }

    private static void Paint(GameObject root, string path, string role)
    {
        var target = Find(root, path);
        if (target != null && target.TryGetComponent<Image>(out var image)) SetImage(image, role);
    }

    private static void PaintProgress(GameObject root, string path)
    {
        var target = Find(root, path);
        if (target == null || !target.TryGetComponent<Image>(out var image)) return;
        Image.Type imageType = image.type;
        float amount = image.fillAmount;
        Image.FillMethod method = image.fillMethod;
        int origin = image.fillOrigin;
        SetImage(image, "ProgressFill");
        image.type = imageType;
        image.fillAmount = amount;
        image.fillMethod = method;
        image.fillOrigin = origin;
    }

    private static void PaintPreservingAlpha(GameObject root, string path, string role)
    {
        var target = Find(root, path);
        if (target == null || !target.TryGetComponent<Image>(out var image)) return;
        float alpha = image.color.a;
        SetImage(image, role);
        image.color = new Color(1f, 1f, 1f, alpha);
    }

    private static void BindExistingButtonGraphic(GameObject root, string path)
    {
        var target = Find(root, path);
        if (target != null && target.TryGetComponent<Button>(out var button) && target.TryGetComponent<Image>(out var image))
            button.targetGraphic = image;
    }

    private static void SetImage(Image image, string role)
    {
        OrchardSkinAuthoring.ApplySprite(image, role);
        image.color = Color.white;
    }

    private static void PaintNamedImage(GameObject root, string name, string role)
    {
        foreach (var image in root.GetComponentsInChildren<Image>(true))
            if (image.name == name) SetImage(image, role);
    }

    private static void Body(GameObject root, string path)
    {
        var target = Find(root, path);
        if (target != null && target.TryGetComponent<TMP_Text>(out var text)) OrchardSkinAuthoring.SetBody(text);
    }

    private static void Title(GameObject root, string path)
    {
        var target = Find(root, path);
        if (target != null && target.TryGetComponent<TMP_Text>(out var text)) OrchardSkinAuthoring.SetTitle(text);
    }

    private static void StyleBodyLabels(GameObject root)
    {
        var labels = root.GetComponentsInChildren<TMP_Text>(true);
        TMP_FontAsset existingFont = null;
        foreach (var text in labels)
        {
            if (text.font == null) continue;
            existingFont = text.font;
            break;
        }
        foreach (var text in labels)
        {
            // Reuse the page's own valid font when an old serialized font GUID is
            // missing (UIItem/Level). This does not change the chosen language/font
            // for any label whose original font is still present.
            if (text.font == null && existingFont != null) text.font = existingFont;
            OrchardSkinAuthoring.SetBody(text);
        }
    }

    private static Transform Find(GameObject root, string path)
    {
        return string.IsNullOrEmpty(path) ? root.transform : root.transform.Find(path);
    }

    private static void DisableGraphic(GameObject root, string path)
    {
        var target = Find(root, path);
        if (target != null && target.TryGetComponent<Graphic>(out var graphic)) graphic.enabled = false;
    }

    private static void DisableNamedGraphic(GameObject root, string name)
    {
        foreach (var graphic in root.GetComponentsInChildren<Graphic>(true))
            if (graphic.name == name) graphic.enabled = false;
    }

    private static void SetRawSprite(GameObject root, string path, Sprite sprite)
    {
        var target = Find(root, path);
        if (sprite != null && target != null && target.TryGetComponent<RawImage>(out var image))
        {
            image.texture = sprite.texture;
            Rect rect = sprite.rect;
            image.uvRect = new Rect(rect.x / sprite.texture.width, rect.y / sprite.texture.height,
                rect.width / sprite.texture.width, rect.height / sprite.texture.height);
            image.color = Color.white;
        }
    }

    private static void EnsureAssetFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureAssetFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
