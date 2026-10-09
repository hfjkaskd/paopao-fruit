#if BIZZA_REAL_WITHDRAW
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// All presentation changes are confined to disposable preview instances.
[InitializeOnLoad]
public static class OrchardHudContactValidation
{
    private const string CorePrefab = "Assets/FruitsHarvest/Resources/Original/res/local/coreplay/CorePlayUI.prefab";
    private const string PanelPrefab = "Assets/BizzaWZ/Final/Real/UI/WithdrawDanPanel/WithdrawDanPanel.prefab";
    private const string ItemPrefab = "Assets/BizzaWZ/Final/Real/UI/WithdrawDanPanel/WithdrawDanItem.prefab";
    private static readonly string DesignDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Design/OrchardUI"));
    private static readonly string CommandPath = Path.Combine(DesignDirectory, "hud-contact.command");
    private static readonly string OutputDirectory = Path.Combine(DesignDirectory, "HudContactChecks");
    private static double nextPoll;

    [Serializable]
    private sealed class ImageState
    {
        public string path, sprite, assetPath;
        public Rect rect;
        public Vector2 position, anchorMin, anchorMax, offsetMin, offsetMax;
        public Vector4 border;
        public bool enabled;
        public int imageType;
    }

    [Serializable]
    private sealed class ContactCase
    {
        public string image;
        public float rootY;
        public Rect trayBounds, buttonBounds, lockBounds;
        public Vector2 backgroundPosition, lockPosition;
        public int authoredCanvasOffset, previewCanvasOrder;
        public bool backgroundActive, lockActive;
    }

    [Serializable]
    private sealed class TierCase
    {
        public string image, label, progressText;
        public float expectedRatio, measuredRatio, fillAreaWidth, fillWidth;
        public bool hiddenAtZero, labelFits;
        public Vector2 labelPreferredSize;
        public Rect labelRect, labelGlyphBounds;
        public Bounds labelTextBounds, labelMeshBounds;
        public float renderedFontSize;
        public bool labelAutoSizing, labelReportsOverflow;
        public ImageState fill;
    }

    [Serializable]
    private sealed class TierState
    {
        public string hierarchy, label, progressText;
        public ImageState background, icon, progress;
        public List<string> differencesFromSource = new List<string>();
    }

    [Serializable]
    private sealed class Report
    {
        public string checkedAtUtc, mode;
        public bool passed, editorWasPlaying, sourcePrefabsUnchanged;
        public TierState sourceTier;
        public List<TierState> runtimeTiers = new List<TierState>();
        public List<ContactCase> contact = new List<ContactCase>();
        public List<TierCase> tiers = new List<TierCase>();
        public List<string> notes = new List<string>();
        public List<string> errors = new List<string>();
    }

    static OrchardHudContactValidation() { EditorApplication.update += Poll; }

    private static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.timeSinceStartup < nextPoll) return;
        nextPoll = EditorApplication.timeSinceStartup + 1;
        if (!File.Exists(CommandPath)) return;
        string command = File.ReadAllText(CommandPath).Trim();
        File.Delete(CommandPath);
        Run(command);
    }

    [MenuItem("Tools/Orchard UI/Preview HUD Contact and Tier Boundaries")]
    public static void Render() { Run("validate"); }

    private static void Run(string command)
    {
        Directory.CreateDirectory(OutputDirectory);
        var report = new Report { checkedAtUtc = DateTime.UtcNow.ToString("O"), mode = command, editorWasPlaying = EditorApplication.isPlaying };
        string[] paths = { CorePrefab, PanelPrefab, ItemPrefab };
        var hashes = new string[paths.Length];
        try
        {
            for (int i = 0; i < paths.Length; i++) hashes[i] = Hash(paths[i]);
            ReadRuntimeState(report);
            if (command == "runtime" || command == "snapshot-runtime")
                report.notes.Add("Read-only snapshot; this command never starts or stops Play mode.");
            else if (command == "validate" || command == "preview")
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    throw new InvalidOperationException("Runtime snapshot recorded. Stop Play mode before requesting disposable static previews.");
                RenderContact(report, false);
                RenderContact(report, true);
                if (!Mathf.Approximately(report.contact[0].rootY, 25f)) report.errors.Add("The source AddOne root Y is not the expected 25.");
                if (!Mathf.Approximately(report.contact[1].buttonBounds.y - report.contact[0].buttonBounds.y, 20f))
                    report.errors.Add("Before/after button displacement is not 20 UI units.");
                foreach (int current in new[] { 4, 1, 0, 5 }) RenderTier(report, current);
                report.notes.Add("The four complete panel previews contain one disposable tier prefab under the authored list root; no business Init, account data or save APIs are invoked.");
            }
            else throw new ArgumentException("Expected validate, preview, runtime or snapshot-runtime; received: " + command);
        }
        catch (Exception exception) { report.errors.Add(exception.ToString()); }
        finally
        {
            report.sourcePrefabsUnchanged = true;
            for (int i = 0; i < paths.Length; i++)
                if (hashes[i] == null || hashes[i] != Hash(paths[i])) report.sourcePrefabsUnchanged = false;
            if (!report.sourcePrefabsUnchanged) report.errors.Add("A source prefab changed during the preview operation.");
            report.passed = report.errors.Count == 0;
            string name = command == "runtime" || command == "snapshot-runtime" ? "runtime" : "result";
            File.WriteAllText(Path.Combine(OutputDirectory, name + ".json"), JsonUtility.ToJson(report, true));
            File.WriteAllText(Path.Combine(OutputDirectory, name + ".txt"),
                (report.passed ? "PASS" : "FAILED") + " " + report.checkedAtUtc + "\n" +
                "Contact previews: " + report.contact.Count + "; tier boundaries: " + report.tiers.Count +
                "; runtime items: " + report.runtimeTiers.Count + "; source prefabs unchanged: " + report.sourcePrefabsUnchanged +
                "\n" + string.Join("\n", report.errors));
            if (report.passed) Debug.Log("[OrchardHudContactValidation] PASS: " + OutputDirectory);
            else Debug.LogError("[OrchardHudContactValidation] FAILED: " + string.Join("\n", report.errors));
        }
    }

    private static void RenderContact(Report report, bool before)
    {
        var sample = new ContactCase { image = Path.Combine(OutputDirectory, before ? "add-one-before.png" : "add-one-after.png") };
        var preview = OrchardSkinValidation.PreviewPrefab(CorePrefab, sample.image, root =>
        {
            foreach (string path in new[] { "Game", "UI/Top", "UI/Magic", "UI/EffRoot", "UI/Bottom/Undo", "UI/Bottom/Magic", "UI/Bottom/Shuffle", "UI/Bottom/Box_Root/DangerousTip" })
                Find(root.transform, path).gameObject.SetActive(false);
            Transform trayRoot = Find(root.transform, "UI/Bottom/Box_Root");
            var button = (RectTransform)Find(trayRoot, "AddOne");
            if (before) button.anchoredPosition = new Vector2(button.anchoredPosition.x, 45f);
            Find(button, "AniLayer/Effct_UnLock_Blue").gameObject.SetActive(false);
            Find(button, "AniLayer/Icon").gameObject.SetActive(false);
            Transform status = Find(button, "AniLayer/ItemStatus");
            foreach (Transform state in status) state.gameObject.SetActive(state.name == "Lock");
            // The shared helper disables DynamicCanvasLayer, so apply its authored
            // offset only to this disposable clone, as the runtime setup would.
            var layer = button.GetComponent<Orange.DynamicCanvasLayer>();
            var buttonCanvas = button.GetComponent<Canvas>();
            var rootCanvas = root.GetComponentInParent<Canvas>();
            if (layer == null || buttonCanvas == null || rootCanvas == null)
                throw new InvalidOperationException("AddOne preview is missing its authored Canvas layer bindings.");
            sample.authoredCanvasOffset = new SerializedObject(layer).FindProperty("offset").intValue;
            buttonCanvas.overrideSorting = true;
            buttonCanvas.sortingLayerID = rootCanvas.sortingLayerID;
            buttonCanvas.sortingOrder = rootCanvas.sortingOrder + sample.authoredCanvasOffset;
            sample.previewCanvasOrder = buttonCanvas.sortingOrder;
            root.SetActive(true);
            RefreshLayout(root);
            var background = (RectTransform)Find(button, "AniLayer/Bg");
            var lockIcon = (RectTransform)Find(button, "AniLayer/ItemStatus/Lock/Image");
            sample.rootY = button.anchoredPosition.y;
            sample.trayBounds = BoundsIn((RectTransform)Find(trayRoot, "BastetUp"), trayRoot);
            sample.buttonBounds = BoundsIn(background, trayRoot);
            sample.lockBounds = BoundsIn(lockIcon, trayRoot);
            sample.backgroundPosition = background.anchoredPosition;
            sample.lockPosition = lockIcon.anchoredPosition;
            sample.backgroundActive = background.GetComponent<Image>().isActiveAndEnabled;
            sample.lockActive = lockIcon.GetComponent<Image>().isActiveAndEnabled;
            if (!sample.backgroundActive || !sample.lockActive) report.errors.Add("AddOne preview background or lock graphic is inactive.");
        });
        report.contact.Add(sample);
        CheckPreview(preview, report);
    }

    private static void RenderTier(Report report, int current)
    {
        var sample = new TierCase { image = Path.Combine(OutputDirectory, "withdraw-tier-" + current + "-of-5.png"), expectedRatio = current / 5f };
        var preview = OrchardSkinValidation.PreviewPrefab(PanelPrefab, sample.image, root =>
        {
            var panel = root.GetComponent<WithdrawDanPanel>();
            if (panel == null || panel.item == null || panel.root == null) throw new InvalidOperationException("Tier panel prefab bindings are missing.");
            foreach (Transform child in panel.root) child.gameObject.SetActive(false);
            var row = (GameObject)PrefabUtility.InstantiatePrefab(panel.item.gameObject, root.scene);
            row.SetActive(false);
            row.transform.SetParent(panel.root, false);
            row.hideFlags = HideFlags.HideAndDontSave;
            DisableBusinessComponents(row);
            var item = row.GetComponent<WithdrawDanItem>();
            if (item == null) throw new InvalidOperationException("Tier item component missing on the disposable row.");
            item.danText.text = "Bronze";
            item.hintText.text = "Passe 5 fases no total";
            item.progressText.text = current + "/5";
            foreach (TMP_Text money in new[] { item.moneyText1, item.moneyText2, item.moneyText3 }) money.text = "R$0,20";
            if (panel.danSprites.Count > 0) item.icon.sprite = panel.danSprites[0];
            item.prepareStateObj.SetActive(current < 5);
            item.claimStateObj.SetActive(current >= 5);
            item.claimedStateObj.SetActive(false);
            var fill = Find(row.transform, "progress/FillArea/progress").GetComponent<Image>();
            if (fill != item.progressImage) throw new InvalidOperationException("The nested fill does not match the item's progressImage binding.");
            float ratio = Mathf.Clamp01(sample.expectedRatio);
            Vector2 anchor = fill.rectTransform.anchorMax;
            anchor.x = ratio;
            fill.rectTransform.anchorMax = anchor;
            fill.enabled = ratio > 0f;
            row.SetActive(true);
            root.SetActive(true);
            RefreshLayout(root);
            item.danText.ForceMeshUpdate(true, true);
            var area = (RectTransform)fill.transform.parent;
            sample.fillAreaWidth = area.rect.width;
            sample.fillWidth = fill.rectTransform.rect.width;
            sample.measuredRatio = sample.fillAreaWidth > 0f ? sample.fillWidth / sample.fillAreaWidth : -1f;
            sample.hiddenAtZero = ratio > 0f || !fill.enabled;
            sample.label = item.danText.text;
            sample.progressText = item.progressText.text;
            sample.renderedFontSize = item.danText.fontSize;
            sample.labelAutoSizing = item.danText.enableAutoSizing;
            sample.labelReportsOverflow = item.danText.isTextOverflowing;
            sample.labelRect = item.danText.rectTransform.rect;
            sample.labelTextBounds = item.danText.textBounds;
            sample.labelMeshBounds = item.danText.mesh.bounds;
            sample.labelGlyphBounds = VisibleGlyphBounds(item.danText);
            // Preferred size can describe the maximum auto-size candidate rather
            // than the fitted mesh. Validate the visible glyph positions instead.
            sample.labelPreferredSize = new Vector2(item.danText.preferredWidth, item.danText.preferredHeight);
            sample.labelFits = Contains(sample.labelRect, sample.labelGlyphBounds, 0.5f);
            sample.fill = ReadImage(fill, row.transform);
            if (Mathf.Abs(sample.measuredRatio - ratio) > 0.001f) report.errors.Add(current + "/5: fill width does not match the requested ratio.");
            if (!sample.hiddenAtZero) report.errors.Add("0/5: fill is still enabled.");
            if (!sample.labelFits) report.errors.Add(current + "/5: rendered Bronze glyph bounds " + sample.labelGlyphBounds + " exceed text rectangle " + sample.labelRect + ".");
        });
        report.tiers.Add(sample);
        CheckPreview(preview, report);
    }

    private static void ReadRuntimeState(Report report)
    {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(ItemPrefab).GetComponent<WithdrawDanItem>();
        report.sourceTier = ReadTier(source);
        if (!EditorApplication.isPlaying) { report.notes.Add("Editor was not in Play mode; no live tier snapshot was attempted."); return; }
        foreach (WithdrawDanItem item in Resources.FindObjectsOfTypeAll<WithdrawDanItem>())
        {
            if (EditorUtility.IsPersistent(item) || !item.gameObject.scene.IsValid() || !item.gameObject.scene.isLoaded) continue;
            TierState live = ReadTier(item);
            CompareImage("bg", report.sourceTier.background, live.background, live.differencesFromSource);
            CompareImage("Icon", report.sourceTier.icon, live.icon, live.differencesFromSource);
            CompareImage("progress", report.sourceTier.progress, live.progress, live.differencesFromSource);
            report.runtimeTiers.Add(live);
        }
        if (report.runtimeTiers.Count == 0) report.notes.Add("No loaded WithdrawDanItem instance was found. The diagnostic did not open a page or change runtime state.");
    }

    private static TierState ReadTier(WithdrawDanItem item)
    {
        return new TierState {
            hierarchy = AnimationUtility.CalculateTransformPath(item.transform, null), label = item.danText.text, progressText = item.progressText.text,
            background = ReadImage(Find(item.transform, "bg").GetComponent<Image>(), item.transform),
            icon = ReadImage(item.icon, item.transform), progress = ReadImage(item.progressImage, item.transform)
        };
    }

    private static ImageState ReadImage(Image image, Transform root)
    {
        RectTransform rect = image.rectTransform;
        return new ImageState { path = AnimationUtility.CalculateTransformPath(image.transform, root),
            sprite = image.sprite != null ? image.sprite.name : "<null>", assetPath = AssetDatabase.GetAssetPath(image.sprite),
            rect = rect.rect, position = rect.anchoredPosition, anchorMin = rect.anchorMin, anchorMax = rect.anchorMax,
            offsetMin = rect.offsetMin, offsetMax = rect.offsetMax, border = image.sprite != null ? image.sprite.border : Vector4.zero,
            enabled = image.enabled, imageType = (int)image.type };
    }

    private static void CompareImage(string name, ImageState source, ImageState live, List<string> differences)
    {
        if (source.sprite != live.sprite || source.assetPath != live.assetPath) differences.Add(name + " sprite differs: source=" + source.sprite + " @ " + source.assetPath + "; live=" + live.sprite + " @ " + live.assetPath);
        if (source.path != live.path) differences.Add(name + " hierarchy differs: source=" + source.path + "; live=" + live.path);
        if (source.rect != live.rect || source.position != live.position || source.anchorMin != live.anchorMin || source.anchorMax != live.anchorMax)
            differences.Add(name + " geometry differs; see sourceTier and runtimeTiers values (progress ratio differences may be normal runtime data).");
    }

    private static void DisableBusinessComponents(GameObject root)
    {
        foreach (MonoBehaviour component in root.GetComponentsInChildren<MonoBehaviour>(true))
            if (component != null && !(component is Graphic) && !(component is BaseMeshEffect) && !(component is LayoutGroup) &&
                !(component is ContentSizeFitter) && !(component is AspectRatioFitter) && !(component is Mask) &&
                !(component is RectMask2D) && !(component is Selectable) && !(component is TMP_SubMeshUI)) component.enabled = false;
    }

    private static void RefreshLayout(GameObject root)
    {
        Canvas.ForceUpdateCanvases();
        foreach (RectTransform rect in root.GetComponentsInChildren<RectTransform>(true))
            if (rect.gameObject.activeInHierarchy && rect.GetComponent<LayoutGroup>() != null) LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        Canvas.ForceUpdateCanvases();
    }

    private static Rect BoundsIn(RectTransform rect, Transform relativeTo)
    {
        var corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        Vector2 minimum = relativeTo.InverseTransformPoint(corners[0]), maximum = minimum;
        foreach (Vector3 corner in corners)
        {
            Vector2 point = relativeTo.InverseTransformPoint(corner);
            minimum = Vector2.Min(minimum, point);
            maximum = Vector2.Max(maximum, point);
        }
        return Rect.MinMaxRect(minimum.x, minimum.y, maximum.x, maximum.y);
    }

    private static Rect VisibleGlyphBounds(TMP_Text text)
    {
        bool found = false;
        Vector2 minimum = Vector2.zero, maximum = Vector2.zero;
        for (int index = 0; index < text.textInfo.characterCount; index++)
        {
            TMP_CharacterInfo character = text.textInfo.characterInfo[index];
            if (!character.isVisible) continue;
            Vector2 lower = character.bottomLeft, upper = character.topRight;
            if (!found) { minimum = lower; maximum = upper; found = true; }
            else { minimum = Vector2.Min(minimum, lower); maximum = Vector2.Max(maximum, upper); }
        }
        if (!found) throw new InvalidOperationException("Bronze preview has no visible glyphs.");
        return Rect.MinMaxRect(minimum.x, minimum.y, maximum.x, maximum.y);
    }

    private static bool Contains(Rect outer, Rect inner, float tolerance)
    {
        return inner.xMin >= outer.xMin - tolerance && inner.xMax <= outer.xMax + tolerance &&
            inner.yMin >= outer.yMin - tolerance && inner.yMax <= outer.yMax + tolerance;
    }

    private static Transform Find(Transform root, string path)
    {
        Transform value = root.Find(path);
        if (value == null) throw new InvalidOperationException("Missing preview element: " + path);
        return value;
    }

    private static void CheckPreview(OrchardSkinValidation.PreviewEntry preview, Report report)
    {
        if (!string.IsNullOrEmpty(preview.error)) report.errors.Add(preview.image + ": " + preview.error);
    }

    private static string Hash(string path)
    {
        using (SHA256 sha = SHA256.Create()) return Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(path)));
    }
}
#endif
