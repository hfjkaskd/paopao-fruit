using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static partial class OrchardApprovedPass
{
    [Serializable] public sealed class Layouts { public Layout[] pages; }
    [Serializable] public sealed class Layout
    {
        public string name;
        public string prefab;
        public bool backdrop = true;
        public bool fit = true;
        public bool preserve = false;
        public string backdropResource;
        public Vector2 referenceSize;
        public Box[] boxes;
        public string[] disableLayouts;
        public string[] hideImages;
        public string[] hideNodes;
        public Decoration[] decorations;
        public Caption[] captions;
    }
    [Serializable] public sealed class Box
    {
        public string path;
        public float x, y, w, h, font;
        public string role, align;
    }
    [Serializable] public sealed class Decoration
    {
        public string path, role;
        public float x, y, w, h;
        public bool behind = true;
    }
    [Serializable] public sealed class Caption
    {
        public string path, en, pt, key;
        public float x, y, w, h, font;
        public bool title;
    }
    private static readonly Color ApprovedInk = new Color32(94, 32, 12, 255);
    private static readonly Color ApprovedGreen = new Color32(0, 91, 29, 255);
    private static readonly Dictionary<string, Sprite> ExtraSprites = new Dictionary<string, Sprite>();
    private static readonly Dictionary<Transform, Rect> AuthoredBounds = new Dictionary<Transform, Rect>();
    private static Layout[] ReadLayouts() { return JsonUtility.FromJson<Layouts>(File.ReadAllText(Output + "layouts.json")).pages; }
    private static Vector2 LayoutReferenceSize(Layout spec)
    { return spec.referenceSize.x>0 && spec.referenceSize.y>0 ? spec.referenceSize : new Vector2(852,1846); }

    public static void ApplyLayouts(string name)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play before authoring.");
        foreach (var spec in ReadLayouts())
        {
            if (name != "all" && spec.name != name) continue;
            GameObject root = PrefabUtility.LoadPrefabContents(spec.prefab);
            try
            {
                string backup = Output + "BeforeAdditional/" + spec.prefab;
                if (!File.Exists(backup)) { Directory.CreateDirectory(Path.GetDirectoryName(backup)); File.Copy(spec.prefab, backup); }
                ApplyLayout(root, spec);
                PrefabUtility.SaveAsPrefabAsset(root, spec.prefab, out bool saved);
                if (!saved) throw new InvalidOperationException("Failed saving " + spec.prefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        AssetDatabase.SaveAssets();
    }

    private static void ApplyLayout(GameObject root, Layout spec)
    {
        if (spec.preserve) { ApplyPreservedPage(root,spec.name); return; }
        if (spec.name=="withdraw-account") RestoreAccountAuthoringRoots(root);
        Vector2 referenceSize = LayoutReferenceSize(spec);
        AuthoredBounds.Clear(); AuthoredBounds[root.transform] = new Rect(Vector2.zero, referenceSize);
        var suspendedLayouts = new List<Behaviour>();
        foreach (var layout in root.GetComponentsInChildren<LayoutGroup>(true)) if (layout.enabled) { suspendedLayouts.Add(layout); layout.enabled = false; }
        foreach (var fitter in root.GetComponentsInChildren<ContentSizeFitter>(true)) if (fitter.enabled) { suspendedLayouts.Add(fitter); fitter.enabled = false; }
        var keepDisabled = new HashSet<Behaviour>();
        var rootRect = (RectTransform)root.transform;
        rootRect.anchorMin = rootRect.anchorMax = rootRect.pivot = new Vector2(.5f, .5f);
        rootRect.sizeDelta = referenceSize;
        rootRect.localScale = Vector3.one;
        if (spec.name == "settings")
        {
            var oldDropdown = root.transform.Find("BG (1)/MainPauseGroup/Dropdown");
            if (oldDropdown != null) oldDropdown.SetParent(root.transform.Find("BG (1)"), false);
        }
        if (root.TryGetComponent<Image>(out var rootImage)) rootImage.enabled = false;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name.StartsWith("OrchardNavLeaves", StringComparison.Ordinal)) t.gameObject.SetActive(false);
        foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            Color originalColor = text.color;
            bool heading = text.fontSharedMaterial != null && text.fontSharedMaterial.name.Contains("Title");
            if (ApprovedFont != null) { text.font = ApprovedFont; if (heading) OrchardSkinAuthoring.SetTitle(text); else OrchardSkinAuthoring.SetBody(text); }
            if (originalColor.g > originalColor.r * 1.4f && originalColor.g > originalColor.b * 1.1f) text.color = ApprovedGreen;
            else if (!(text.color.r > .9f && text.color.g > .9f && text.color.b > .9f)) text.color = ApprovedInk;
            text.raycastTarget = false;
        }
        var previousBackdrop=root.transform.Find("OrchardBackdrop");if(previousBackdrop!=null)previousBackdrop.gameObject.SetActive(spec.backdrop);
        if (spec.backdrop)
        {
            OrchardSkinAuthoring.EnsureBackdrop(root);
            root.transform.Find("OrchardBackdrop").GetComponent<AspectRatioFitter>().aspectRatio = 853f / 1844f;
            var backdropData = new SerializedObject(root.transform.Find("OrchardBackdrop").GetComponent<OrchardBackdrop>());
            backdropData.FindProperty("resourcePath").stringValue = string.IsNullOrEmpty(spec.backdropResource) ? "OrchardUI/Backdrop" : spec.backdropResource;
            backdropData.ApplyModifiedPropertiesWithoutUndo();
            foreach (var im in root.GetComponentsInChildren<Image>(true))
                if (im.name.StartsWith("PageMask", StringComparison.Ordinal) || im.name == "Mask") im.color = Color.clear;
        }
        if (spec.disableLayouts != null)
            foreach (string path in spec.disableLayouts)
            {
                Transform t = Need(root, path);
                foreach (var layout in t.GetComponents<LayoutGroup>()) { layout.enabled = false; keepDisabled.Add(layout); }
                foreach (var fitter in t.GetComponents<ContentSizeFitter>()) { fitter.enabled = false; keepDisabled.Add(fitter); }
            }
        if (spec.hideImages != null) foreach (string path in spec.hideImages)
            { var t = Need(root, path); if (t.TryGetComponent<Graphic>(out var graphic)) graphic.enabled = false; }
        if (spec.hideNodes != null) foreach (string path in spec.hideNodes) Need(root, path).gameObject.SetActive(false);
        if (spec.boxes != null) foreach (var b in spec.boxes)
        {
            Transform t = Need(root, b.path);
            Place(root, t, b.x, b.y, b.w, b.h);
            if (!string.IsNullOrEmpty(b.role))
            {
                if (t.TryGetComponent<Graphic>(out var previousGraphic) && !(previousGraphic is Image))
                {
                    if (previousGraphic is TMP_Text) throw new InvalidOperationException("Cannot paint a text node: " + b.path);
                    Object.DestroyImmediate(previousGraphic);
                }
                var im = t.GetComponent<Image>() ?? t.gameObject.AddComponent<Image>();
                Paint(im, b.role);
                if (t.TryGetComponent<Button>(out var button)) { button.targetGraphic = im; im.raycastTarget = true; }
            }
            if (t.TryGetComponent<TMP_Text>(out var tx))
            {
                if (b.font > 0) { tx.fontSize = b.font; tx.enableAutoSizing = true; tx.fontSizeMax = b.font; tx.fontSizeMin = b.font * .7f; }
                if (b.align == "left") tx.alignment = TextAlignmentOptions.MidlineLeft;
                else if (b.align == "center") tx.alignment = TextAlignmentOptions.Center;
                else if (b.align == "right") tx.alignment = TextAlignmentOptions.MidlineRight;
                tx.margin = Vector4.zero;
            }
        }
        foreach (var layout in suspendedLayouts) if (!keepDisabled.Contains(layout)) layout.enabled = true;
        ApplySpecial(root, spec.name);
        if (spec.decorations != null) foreach (var d in spec.decorations)
        {
            var target = Ensure(root, d.path);
            var image = target.GetComponent<Image>() ?? target.gameObject.AddComponent<Image>();
            Paint(image, d.role); image.raycastTarget = false;
            Place(root, target, d.x, d.y, d.w, d.h);
            if (d.behind)
            {
                target.SetAsFirstSibling();
                foreach (Transform candidate in target.parent)
                        if ((candidate.name == "BG" || candidate.name == "bg" || candidate.name == "BG (1)" || candidate.name == "BG (2)") && candidate.TryGetComponent<Image>(out var bg) && bg.enabled)
                        { target.SetSiblingIndex(candidate.GetSiblingIndex() + 1); break; }
            }
        }
        if (spec.captions != null) foreach (var c in spec.captions)
        {
            var t = Ensure(root, c.path);
            var tx = t.GetComponent<TMP_Text>() ?? t.gameObject.AddComponent<TextMeshProUGUI>();
            tx.font = ApprovedFont != null ? ApprovedFont : root.GetComponentInChildren<TMP_Text>(true).font;
            if (t.TryGetComponent<UILanguageLabel>(out var previous)) previous.enabled = false;
            var label = t.GetComponent<OrchardLocalizedLabel>() ?? t.gameObject.AddComponent<OrchardLocalizedLabel>();
            var so = new SerializedObject(label);
            so.FindProperty("target").objectReferenceValue = tx;
            so.FindProperty("english").stringValue = c.en;
            so.FindProperty("portuguese").stringValue = c.pt;
            so.FindProperty("existingKey").stringValue = c.key ?? "";
            so.ApplyModifiedPropertiesWithoutUndo();
            tx.text = c.pt;
            tx.fontSize = c.font; tx.enableAutoSizing = true; tx.fontSizeMin = c.font * .7f; tx.fontSizeMax = c.font;
            tx.alignment = TextAlignmentOptions.Center; tx.enableWordWrapping = !c.title;
            tx.margin = Vector4.zero; tx.characterSpacing = 0; tx.wordSpacing = 0; tx.lineSpacing = 0;
            if (c.title) OrchardSkinAuthoring.SetTitle(tx); else { OrchardSkinAuthoring.SetBody(tx); tx.color = ApprovedInk; }
            Place(root, t, c.x, c.y, c.w, c.h);
            t.SetAsLastSibling();
        }
        FinalizeApprovedPage(root, spec.name);
        FinalizeReferenceDetails(root, spec.name);
        BindMissingButtonVisuals(root);
        if (spec.backdrop) root.transform.Find("OrchardBackdrop").SetAsFirstSibling();
        if (spec.fit)
        {
            CaptureReferenceLayout(root);
            rootRect.anchorMin=Vector2.zero;rootRect.anchorMax=Vector2.one;rootRect.sizeDelta=Vector2.zero;rootRect.anchoredPosition=Vector2.zero;
        }
    }

    private static Transform Need(GameObject root, string path)
    {
        Transform t = string.IsNullOrEmpty(path) ? root.transform : root.transform.Find(path);
        if (t == null) throw new InvalidOperationException(root.name + " missing required path " + path);
        return t;
    }
    private static void BindMissingButtonVisuals(GameObject root)
    {
        foreach(var button in root.GetComponentsInChildren<Button>(true))
        {
            if(button.targetGraphic!=null)continue;
            var image=button.GetComponent<Image>();
            if(image==null)foreach(var child in button.GetComponentsInChildren<Image>(true))if(child.enabled&&child.sprite!=null){image=child;break;}
            if(image!=null){button.targetGraphic=image;image.raycastTarget=true;}
        }
    }
    private static Transform Ensure(GameObject root, string path)
    {
        Transform found = root.transform.Find(path); if (found != null) return found;
        int slash = path.LastIndexOf('/');
        Transform parent = slash < 0 ? root.transform : Need(root, path.Substring(0, slash));
        string name = slash < 0 ? path : path.Substring(slash + 1);
        var go = new GameObject(name, typeof(RectTransform)); go.layer = root.layer;
        go.transform.SetParent(parent, false); return go.transform;
    }
    private static void Place(GameObject root, Transform target, float x, float y, float w, float h)
    {
        var r = (RectTransform)target;
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(.5f, .5f);
        r.localScale = Vector3.one; r.sizeDelta = new Vector2(w, h);
        if (AuthoredBounds.TryGetValue(target.parent, out Rect parentBounds))
            r.anchoredPosition = new Vector2(x + w * .5f - parentBounds.center.x, parentBounds.center.y - y - h * .5f);
        else
        {
            Vector2 center=AuthoredBounds[root.transform].center;
            r.position = root.transform.TransformPoint(new Vector3(x + w * .5f - center.x, center.y - y - h * .5f, 0));
        }
        AuthoredBounds[target] = new Rect(x, y, w, h);
    }
    private static Sprite NamedSprite(string path, string name)
    {
        string key = path + "#" + name;
        if (ExtraSprites.TryGetValue(key, out var sprite)) return sprite;
        foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path))
            if (obj is Sprite s && s.name == name) { ExtraSprites[key] = s; return s; }
        throw new InvalidOperationException("Missing sprite " + key);
    }
    private static void Paint(Image image, string role)
    {
        if (role.StartsWith("detail:", StringComparison.Ordinal))
        {
            PaintReferenceDetail(image, role.Substring(7));
            return;
        }
        if(TryFidelitySurface(image,role))return;
        if (role.StartsWith("reward:", StringComparison.Ordinal))
        {
            image.sprite=NamedSprite("Assets/OrchardUI/Art/RewardHeroes.png",role.Substring(7));image.type=Image.Type.Simple;image.preserveAspect=true;
        }
        else if (role.StartsWith("asset:", StringComparison.Ordinal))
        {
            image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(role.Substring(6));image.type=Image.Type.Simple;image.preserveAspect=true;
        }
        else if (role.StartsWith("nav:", StringComparison.Ordinal))
        {
            image.sprite = NamedSprite(OrchardNavigationPass.AtlasPath, role.Substring(4));
            image.type = Image.Type.Simple; image.preserveAspect = true;
        }
        else if (role.StartsWith("system:", StringComparison.Ordinal))
        {
            image.sprite = NamedSprite("Assets/OrchardUI/Art/SystemIcons.png", role.Substring(7));
            image.type = Image.Type.Simple; image.preserveAspect = true;
        }
        else if (role.StartsWith("hero:", StringComparison.Ordinal))
        {
            image.sprite = NamedSprite("Assets/OrchardUI/Art/ServiceHeroes.png", role.Substring(5));
            image.type = Image.Type.Simple; image.preserveAspect = true;
        }
        else { OrchardSkinAuthoring.ApplySprite(image, role); image.pixelsPerUnitMultiplier = 1f; }
        image.overrideSprite = null; image.color = Color.white; image.enabled = true;
    }
    private static void CaptureReferenceLayout(GameObject root)
    {
        var component = root.GetComponent<OrchardReferenceLayout>() ?? root.AddComponent<OrchardReferenceLayout>();
        var targets = new List<RectTransform>();
        foreach (Transform t in root.transform)
        {
            if (!(t is RectTransform r) || t.name == "OrchardBackdrop" || t.name.StartsWith("PageMask", StringComparison.Ordinal) || t.name == "Mask") continue;
            // The unlock overlay covers the canvas independently of the fitted popup artwork.
            if (t.name == "Shadow" && root.GetComponent<NewItemPop>() != null) continue;
            Vector3 center = root.transform.InverseTransformPoint(r.TransformPoint(r.rect.center));
            Vector2 size = r.rect.size;
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(.5f, .5f);
            r.sizeDelta = size; r.localPosition = center; targets.Add(r);
        }
        var so = new SerializedObject(component);
        so.FindProperty("referenceSize").vector2Value = AuthoredBounds[root.transform].size;
#if BIZZA_REAL_WITHDRAW
        if(root.GetComponent<RealWithdrawPanel>()!=null)
        {
            so.FindProperty("fitWidthAndScroll").boolValue=true;
            var scroll=(RectTransform)Need(root,"Content/Scroll View");
            Vector3 top=scroll.TransformPoint(new Vector3(0,scroll.rect.yMax,0));scroll.pivot=new Vector2(.5f,1);scroll.position=top;
            so.FindProperty("scrollWindow").objectReferenceValue=scroll;
            so.FindProperty("scrollTop").floatValue=189;so.FindProperty("scrollBottom").floatValue=40;
            StretchRect(Need(root,"Content/Scroll View/Viewport"));
        }
#endif
        var elements = so.FindProperty("elements"); elements.arraySize = targets.Count;
        for (int i = 0; i < targets.Count; i++)
        {
            var element = elements.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("rect").objectReferenceValue = targets[i];
            element.FindPropertyRelative("position").vector2Value = targets[i].anchoredPosition;
            element.FindPropertyRelative("scale").vector3Value = targets[i].localScale;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void ApplySpecial(GameObject root, string name)
    {
        if (name == "settings") ApplyApprovedSettings(root);
        if (name == "withdraw-main") SetupWithdrawalLayout(root);
        if (name == "withdraw-account") SetupAccountLayout(root);
        SetupApprovedService(root, name);
        SetupApprovedRewards(root, name);
    }
    private static void FinalizeApprovedPage(GameObject root, string name)
    {
        if(name=="withdraw-main") FinalizeFidelityWithdrawal(root);
        if(name=="new-booster")
        {Need(root,"MainContent/ApprovedPlaque").SetAsLastSibling();Need(root,"MainContent/Title").SetAsLastSibling();}
        if(name=="service")
        {
            var question=Need(root,"Content/InputNode/SelectQuestionBtn /Text (TMP)").GetComponent<TMP_Text>();question.enableWordWrapping=true;question.overflowMode=TextOverflowModes.Overflow;
        }
        if(name=="lucky-spin")
        {
            Need(root,"Content/SlotMachineGroup/ApprovedReelPaper").SetAsFirstSibling();
            Need(root,"Content/SlotMachineGroup/ApprovedCabinet").SetAsLastSibling();
            Need(root,"Content/SlotMachineGroup/Btn").SetAsLastSibling();
        }
        var page = root.GetComponent<UIPageBase>();
        if (page != null)
        {
            var actions = root.GetComponent<OrchardPageActions>() ?? root.AddComponent<OrchardPageActions>();
            var so = new SerializedObject(actions); so.FindProperty("page").objectReferenceValue = page;
            foreach (var binding in new[] { new[] { "ApprovedHelp", "helpButton" }, new[] { "ApprovedBack", "backButton" }, new[] { "ApprovedEdit", "editButton" }, new[] { "ApprovedSupport", "supportButton" } })
            {
                Transform t = root.transform.Find(binding[0]); if (t == null) continue;
                var button = t.GetComponent<Button>() ?? t.gameObject.AddComponent<Button>();
                var image = t.GetComponent<Image>(); button.targetGraphic = image; image.raycastTarget = true;
                so.FindProperty(binding[1]).objectReferenceValue = button;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
#if BIZZA_REAL_WITHDRAW
        if(name=="lucky-help")
        { Need(root,"Content/ApprovedPlaque").SetAsLastSibling(); Need(root,"Content/ApprovedTitle").SetAsLastSibling(); }
        if(name=="withdraw-confirm")
        { var confirm=root.GetComponent<UIWithdrawalConfirmPanel>();confirm.PaymentValueText.transform.SetAsLastSibling();confirm.paymentImage.transform.SetAsLastSibling(); }
        if (name == "withdraw-pending")
        {
            var pending = root.GetComponent<UIWithdrawalPendingPanel>(); var so = new SerializedObject(pending);
            pending.paymentImage.transform.SetAsLastSibling();pending.amountText.transform.SetAsLastSibling();pending.progressText.gameObject.SetActive(true);
            var pendingNames = new[] { "ApprovedClock", "ApprovedStatus", "ApprovedSubtitle", "ApprovedPendingTitle", "BtnConfirm/ApprovedPendingLabel" };
            var pp = so.FindProperty("pendingPresentation"); pp.arraySize = pendingNames.Length;
            for (int i = 0; i < pendingNames.Length; i++) pp.GetArrayElementAtIndex(i).objectReferenceValue = Need(root, pendingNames[i]).gameObject;
            var cp = so.FindProperty("completedPresentation"); cp.arraySize = 2;
            cp.GetArrayElementAtIndex(0).objectReferenceValue = pending.titleText.gameObject;
            cp.GetArrayElementAtIndex(1).objectReferenceValue = pending.confirmText.gameObject;
            so.FindProperty("pendingButtonSprite").objectReferenceValue = OrchardSkinAuthoring.SpriteFor("ButtonDisabled");
            so.FindProperty("completedButtonSprite").objectReferenceValue = OrchardSkinAuthoring.SpriteFor("ButtonGreen");
            so.ApplyModifiedPropertiesWithoutUndo();
            if (pending.titleText.TryGetComponent<OrchardLocalizedLabel>(out var obsolete)) obsolete.enabled = false;
            if (pending.titleText.TryGetComponent<UILanguageLabel>(out var localization)) localization.enabled = true;
            pending.titleText.gameObject.SetActive(false); pending.confirmText.gameObject.SetActive(false);
            Paint(pending.confirmText.transform.parent.GetComponent<Image>(), "ButtonDisabled");
        }
        if (name == "rating")
        {
            var rating = root.GetComponent<StarRatingPopup>();
            for (int i = 0; i < rating.starImages.Length; i++)
            {
                var star = rating.starImages[i];
                var button = star.GetComponent<BizzaButton>() ?? star.gameObject.AddComponent<BizzaButton>();
                button.targetGraphic = star; star.raycastTarget = true;
                rating.starButtons[i] = button;
                Place(root, star.transform, 89 + i * 139, 985, 118, 118);
            }
            Need(root, "Content/Stars/Darks").gameObject.SetActive(false);
        }
        if (name == "faq")
        {
            var faq = root.GetComponent<FAQPanel>();
            faq.titleColor.replaceValue = "#5E200C"; faq.contentColor.replaceValue = "#5E200C"; faq.highlightColor.replaceValue = "#005B1D";
            var content = Need(root, "Content (1)/Scroll View/Viewport/Content");
            foreach (var tx in content.GetComponentsInChildren<TMP_Text>(true))
            {
                tx.fontSize = 31; tx.enableAutoSizing = false; tx.color = ApprovedInk;
                tx.text = tx.text.Replace("#174F7D", "#5E200C"); tx.lineSpacing = 9;
            }
        }
#endif
    }
    private static void ApplyApprovedSettings(GameObject root)
    {
        var panel = root.GetComponent<PausePanel>();
        // Keep the original state-controlled images and button references, with complete visual targets.
        var buttons = new[] { panel.musicSwitchButton, panel.soundSwitchButton, panel.libSwitchButton };
        var on = new[] { panel.musicOnIm, panel.soundOnIm, panel.LibOnIm };
        var off = new[] { panel.musicOffIm, panel.soundOffIm, panel.LibOffIm };
        for (int i = 0; i < buttons.Length; i++)
        {
            var parent = buttons[i].transform.parent;
            string iconPath = "BG (1)/" + parent.name + "/ApprovedIcon";
            var icon = Ensure(root, iconPath).GetComponent<Image>();
            if (icon == null) icon = Need(root, iconPath).gameObject.AddComponent<Image>();
            var originalGlyph = on[i].transform.Find("Image (2)").GetComponent<Image>();
            Paint(icon, "system:" + new[] { "Music", "Sound", "Vibration" }[i]); icon.raycastTarget = false;
            Place(root, icon.transform, 100, 520 + i * 198, 100, 110);
            Paint(parent.GetComponent<Image>(), "Input");
            var background = buttons[i].GetComponent<Image>(); Paint(background, "ButtonDisabled");
            buttons[i].targetGraphic = background; background.raycastTarget = true;
            Paint(on[i], "ButtonGreen"); Paint(off[i], "ButtonDisabled");
            foreach (var state in new[] { on[i], off[i] })
            {
                var r = state.rectTransform; r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
                r.offsetMin = r.offsetMax = Vector2.zero; r.localScale = Vector3.one;
                foreach (var glyph in state.GetComponentsInChildren<Image>(true))
                    if (glyph != state && glyph.name != "ApprovedKnob") glyph.enabled = false;
            }
        }
        // The language selector was removed from the player-facing settings.
        panel.languageDropdown.transform.SetParent(Need(root, "BG (1)"), true);
        panel.languageDropdown.gameObject.SetActive(false);
        Place(root, panel.languageDropdown.transform, 416, 1112, 350, 102);
        foreach (Transform t in Need(root, "BG (1)"))
            if (t.name == "Image (2)" && t.TryGetComponent<Image>(out var im) && im.sprite != null && im.sprite.name == "Inset") im.enabled = false;
        panel.CloseButton.transform.SetAsLastSibling();
    }
}
