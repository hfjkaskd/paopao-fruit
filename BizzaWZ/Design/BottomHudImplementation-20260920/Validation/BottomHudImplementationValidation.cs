using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Temporary isolated production-prefab renderer. No game method, account, Play
// toggle, scene switch, event dispatch, or asset saving is performed here.
// Install in Assets/OrchardUI/Editor only after reviewing this source.
[InitializeOnLoad]
public static class BottomHudImplementationValidation
{
    const string Output = "Design/BottomHudImplementation-20260920/Validation";
    const string CanvasPath = "Assets/BizzaWZ/Common/Framework/GameCanvas.prefab";
    const string PanelPath = "Assets/BizzaWZ/Common/UI/GamePanel/RealGamePanel.prefab";
    const string HarvestPath = "Assets/FruitsHarvest/Resources/HarvestRoot.prefab";
    const string CorePath = "Assets/FruitsHarvest/Resources/Original/res/local/coreplay/CorePlayUI.prefab";
    const string WidgetPath = "Assets/BizzaWZ/Final/Real/UI/GamePanel/Resources/Resources/GameUiWidget.prefab";
    const string SlotPath = "Assets/BizzaWZ/Final/Real/UI/SlotsPanel/SlotEnter/SlotEnter.prefab";
    const string PathsPath = "Assets/FruitsHarvest/Resources/HarvestPaths.txt";
    const int Width = 1080, Height = 1920, CropHeight = 330;
    static double nextPoll;
    static readonly Dictionary<string, string> resourcePaths = new Dictionary<string, string>();

    [Serializable] class Batch
    {
        public string kind = "UNITY PREFAB STATIC PREVIEW, explicit visual states only; not runtime or account-flow verification";
        public string generatedUtc, error;
        public bool editorWasPlaying;
        public List<CaseReport> cases = new List<CaseReport>();
        public List<Fingerprint> sourceAssets = new List<Fingerprint>();
        public List<ResourceLoadRecord> resourceLoadChecks = new List<ResourceLoadRecord>();
    }
    [Serializable] class ResourceLoadRecord
    {
        public string resourcePath, expectedAssetPath, loadedAssetPath, spriteName, loadedGuid;
        public long loadedFileID;
        public bool loadSucceeded, expectedAssetMatches;
        public int subSpriteCount;
        public Rect spriteRect;
    }
    [Serializable] class Fingerprint { public string path, before, after; public bool unchanged; }
    [Serializable] class CaseReport
    {
        public string name, image, error;
        public Vector2 logicalCanvasSize, referenceResolution;
        public float productionMatchWidthOrHeight, pixelScale, fill;
        public int strippedComponents, remainingBusinessComponents, textOverflowCount, activeGraphics;
        public bool horizontalFill, sourceBindingsReadOnly = true;
        public List<string> overflowTexts = new List<string>();
        public List<ResourceRecord> resources = new List<ResourceRecord>();
        public List<ButtonRecord> sourceButtons = new List<ButtonRecord>();
    }
    [Serializable] class ResourceRecord { public string owner, role, path, sprite, texturePath; public Rect spriteRect; public bool active; }
    [Serializable] class ButtonRecord { public string path, targetGraphic; public int persistentCalls; }
    class PropVisual
    {
        public string name;
        public Transform root;
        public Image bg, icon, frame;
        public GameObject onlyUnlock, locked, useCoin, watchAd, useDirectly, effect;
        public TMP_Text count;
    }
    class SlotVisual { public Transform root; public Image fill; public TMP_Text text; public GameObject hint; }

    static BottomHudImplementationValidation() { EditorApplication.update += Tick; }
    static void Tick()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.timeSinceStartup < nextPoll) return;
        nextPoll = EditorApplication.timeSinceStartup + 1;
        string path = Output + "/preview.command";
        if (!File.Exists(path)) return;
        string command = File.ReadAllText(path).Trim();
        // Inactive parents + destruction of all business components before any
        // activation make this disposable PreviewScene safe during existing Play.
        // We still refuse the transient starting/stopping state.
        if (EditorApplication.isPlaying != EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete(path);
        if (command != "preview") return;
        var batch = new Batch { generatedUtc = DateTime.UtcNow.ToString("o"), editorWasPlaying = EditorApplication.isPlaying };
        Directory.CreateDirectory(Output);
        try
        {
            resourcePaths.Clear();
            foreach (string line in File.ReadAllLines(PathsPath))
            {
                int tab = line.IndexOf('\t');
                if (tab > 0) resourcePaths[line.Substring(0, tab)] = line.Substring(tab + 1).Trim();
            }
            foreach (string asset in new[] { CanvasPath, PanelPath, HarvestPath, CorePath, WidgetPath, SlotPath, PathsPath })
                batch.sourceAssets.Add(new Fingerprint { path = asset, before = Hash(asset) });
            foreach (string name in new[] { "PropButtonNormal", "PropButtonLocked", "PropButtonFrame" })
            {
                string asset = "Assets/OrchardUI/Resources/OrchardUI/" + name + ".png";
                batch.sourceAssets.Add(new Fingerprint { path = asset, before = Hash(asset) });
                batch.sourceAssets.Add(new Fingerprint { path = asset + ".meta", before = Hash(asset + ".meta") });
                batch.resourceLoadChecks.Add(CheckResource(name));
            }
            foreach (int progress in new[] { 0, 1, 4, 5 }) batch.cases.Add(Render(progress, "mixed"));
            batch.cases.Add(Render(1, "all-unlocked"));
            batch.cases.Add(Render(1, "all-locked"));
        }
        catch (Exception e) { batch.error = e.ToString(); }
        finally
        {
            foreach (var f in batch.sourceAssets)
            {
                f.after = Hash(f.path); f.unchanged = f.before == f.after;
                if (!f.unchanged) batch.error = "Production source changed while rendering. Discard mixed-version previews and rerun.";
            }
            File.WriteAllText(Output + "/static-validation.json", JsonUtility.ToJson(batch, true));
        }
    }

    static ResourceLoadRecord CheckResource(string name)
    {
        var check = new ResourceLoadRecord
        {
            resourcePath = "OrchardUI/" + name,
            expectedAssetPath = "Assets/OrchardUI/Resources/OrchardUI/" + name + ".png"
        };
        // This is the exact production resource API used by GameResCatalog.Load.
        // No catalog singleton, game state or gameplay method is initialized.
        Sprite loaded = Resources.Load<Sprite>(check.resourcePath);
        check.loadSucceeded = loaded != null;
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(check.expectedAssetPath)) if (asset is Sprite) check.subSpriteCount++;
        if (loaded != null)
        {
            check.loadedAssetPath = AssetDatabase.GetAssetPath(loaded);
            check.spriteName = loaded.name;
            check.spriteRect = loaded.rect;
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(loaded, out string guid, out long fileID);
            check.loadedGuid = guid; check.loadedFileID = fileID;
            check.expectedAssetMatches = check.loadedAssetPath == check.expectedAssetPath && fileID == 21300000 && loaded.name == name;
        }
        if (!check.loadSucceeded || !check.expectedAssetMatches || check.subSpriteCount != 1)
            throw new InvalidDataException("Resources.Load<Sprite> must resolve the one expected production sprite: " + JsonUtility.ToJson(check));
        return check;
    }

    static CaseReport Render(int progress, string state)
    {
        var report = new CaseReport { name = state + "-" + progress + "of5" };
        report.image = Path.GetFullPath(Output + "/static-" + report.name + ".png");
        var scene = EditorSceneManager.NewPreviewScene();
        RenderTexture target = null;
        Texture2D pixels = null;
        RenderTexture previous = RenderTexture.active;
        try
        {
            var holder = new GameObject("Inactive isolated static-preview holder");
            SceneManager.MoveGameObjectToScene(holder, scene); holder.SetActive(false);
            var canvasObject = Clone(CanvasPath, holder.transform);
            var canvas = canvasObject.GetComponent<Canvas>();
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            if (canvas == null || scaler == null || scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize ||
                scaler.screenMatchMode != CanvasScaler.ScreenMatchMode.MatchWidthOrHeight || !Mathf.Approximately(scaler.matchWidthOrHeight, 1))
                throw new InvalidDataException("Production scaler changed; revise static renderer before use.");
            report.referenceResolution = scaler.referenceResolution;
            report.productionMatchWidthOrHeight = scaler.matchWidthOrHeight;
            report.pixelScale = Height / scaler.referenceResolution.y;
            report.logicalCanvasSize = new Vector2(Width / report.pixelScale, scaler.referenceResolution.y);
            scaler.enabled = false; // Preserve config; reproduce its output mathematically offscreen.
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.referencePixelsPerUnit = scaler.referencePixelsPerUnit;
            var canvasRect = (RectTransform)canvas.transform;
            canvasRect.localScale = Vector3.one; canvasRect.localRotation = Quaternion.identity;
            canvasRect.sizeDelta = report.logicalCanvasSize;
            canvasRect.localPosition = new Vector3((canvasRect.pivot.x - .5f) * report.logicalCanvasSize.x,
                (canvasRect.pivot.y - .5f) * report.logicalCanvasSize.y, 0);
            var baseLayer = Need(canvas.transform, "Content/BaseLayer");
            DisableChildren(baseLayer);
            var panel = Clone(PanelPath, baseLayer);
            var content = Need(panel.transform, "Content"); DisableChildren(content);
            var harvest = Clone(HarvestPath, content);
            var core = Clone(CorePath, Need(harvest.transform, "Canvas/MgrUI/Bottom"));
            var widget = Clone(WidgetPath, content);
            var props = CaptureProps(core);
            var slot = CaptureSlot(widget);
            var targets = new List<Transform> { slot.root };
            foreach (var prop in props) targets.Add(prop.root);
            RecordButtons(core, report); RecordButtons(widget, report);
            KeepRelevant(canvasObject.transform, targets);
            // Destroy rather than disable: Awake runs even on disabled behaviours
            // when an object is activated. No business component may remain.
            Strip(holder, report);
            AssertPure(holder, report);
            foreach (var prop in props)
            {
                bool unlocked = state != "all-locked" && (state == "all-unlocked" || prop.name != "Magic");
                int count = prop.name == "Shuffle" ? 1 : 0;
                ApplyVisualState(prop, unlocked, count, report);
            }
            slot.fill.fillAmount = progress / 5f;
            slot.text.text = progress + "/5";
            if (slot.hint != null) slot.hint.SetActive(false); // Particle readiness hint intentionally absent in static preview.
            report.fill = slot.fill.fillAmount;
            report.horizontalFill = slot.fill.type == Image.Type.Filled && slot.fill.fillMethod == Image.FillMethod.Horizontal && slot.fill.fillOrigin == 0;
            if (!report.horizontalFill) throw new InvalidDataException("Production SlotEnter fill is not horizontal/left-origin.");
            foreach (var img in slot.root.GetComponentsInChildren<Image>(true)) RecordImage(img, "SlotEnter", "prefab image", report);
            var camObject = new GameObject("Static offscreen camera", typeof(Camera));
            SceneManager.MoveGameObjectToScene(camObject, scene);
            var camera = camObject.GetComponent<Camera>(); camera.enabled = false;
            camera.scene = scene; camera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(scene);
            camera.orthographic = true; camera.orthographicSize = report.logicalCanvasSize.y * .5f;
            camera.aspect = (float)Width / Height; camera.nearClipPlane = .1f; camera.farClipPlane = 2000;
            camera.transform.position = new Vector3(0, 0, -1000);
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.12f, .24f, .18f, 1);
            camera.allowHDR = false; camera.allowMSAA = false;
            target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32); target.Create(); camera.targetTexture = target;
            foreach (var c in canvasObject.GetComponentsInChildren<Canvas>(true)) { c.renderMode = RenderMode.WorldSpace; c.worldCamera = camera; }
            holder.SetActive(true);
            Canvas.ForceUpdateCanvases();
            foreach (var t in targets) LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)t);
            Canvas.ForceUpdateCanvases();
            foreach (var text in holder.GetComponentsInChildren<TMP_Text>(true))
            {
                if (!text.isActiveAndEnabled) continue;
                text.ForceMeshUpdate(true, true);
                if (text.isTextOverflowing || text.isTextTruncated) { report.textOverflowCount++; report.overflowTexts.Add(PathOf(text.transform) + ": " + text.text); }
            }
            foreach (var g in holder.GetComponentsInChildren<Graphic>(true))
                if (g.isActiveAndEnabled) { g.SetAllDirty(); g.Rebuild(CanvasUpdate.PreRender); report.activeGraphics++; }
            Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = target;
            pixels = new Texture2D(Width, CropHeight, TextureFormat.RGBA32, false);
            pixels.ReadPixels(new Rect(0, 0, Width, CropHeight), 0, 0); pixels.Apply(false, false);
            File.WriteAllBytes(report.image, pixels.EncodeToPNG());
        }
        catch (Exception e) { report.error = e.ToString(); }
        finally
        {
            RenderTexture.active = previous;
            if (pixels != null) Object.DestroyImmediate(pixels);
            if (target != null) { target.Release(); Object.DestroyImmediate(target); }
            EditorSceneManager.ClosePreviewScene(scene);
        }
        return report;
    }

    static GameObject Clone(string path, Transform parent)
    {
        if (parent.gameObject.activeInHierarchy) throw new InvalidOperationException("Inactive clone parent required.");
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (source == null) throw new FileNotFoundException(path);
        var clone = Object.Instantiate(source, parent, false); clone.name = source.name;
        if (clone.activeInHierarchy) throw new InvalidOperationException("Unsafe active clone.");
        return clone;
    }
    static List<PropVisual> CaptureProps(GameObject core)
    {
        var result = new List<PropVisual>();
        foreach (var view in core.GetComponentsInChildren<CorePlayItemBtn>(true))
        {
            if (view.name != "Undo" && view.name != "Magic" && view.name != "Shuffle") continue;
            var so = new SerializedObject(view);
            result.Add(new PropVisual { name = view.name, root = view.transform, bg = Ref<Image>(so, "m_ItemBG"),
                icon = Ref<Image>(so, "m_ItemIcon"), frame = Ref<Image>(so, "outerFrame"), onlyUnlock = Ref<GameObject>(so, "m_OnlyUnlockIcon"),
                locked = Ref<GameObject>(so, "m_LockIcon"), useCoin = Ref<GameObject>(so, "m_UseCoin"), watchAd = Ref<GameObject>(so, "m_WatchAD"),
                useDirectly = Ref<GameObject>(so, "m_UseDirectly"), count = Ref<TMP_Text>(so, "m_LeftItemNum"), effect = Ref<GameObject>(so, "m_UseEff") });
        }
        if (result.Count != 3) throw new InvalidDataException("Expected exactly 3 bottom props.");
        return result;
    }
    static SlotVisual CaptureSlot(GameObject widget)
    {
        var slot = widget.GetComponentInChildren<SlotEnter>(true);
        if (slot == null) throw new InvalidDataException("Widget SlotEnter missing.");
        var so = new SerializedObject(slot);
        return new SlotVisual { root = slot.transform, fill = Ref<Image>(so, "progressImag"), text = Ref<TMP_Text>(so, "progressTxt"), hint = Ref<GameObject>(so, "slotCanHintObj") };
    }
    static T Ref<T>(SerializedObject so, string field) where T : Object { return so.FindProperty(field).objectReferenceValue as T; }
    static void ApplyVisualState(PropVisual p, bool unlocked, int count, CaseReport report)
    {
        SetActive(p.locked, !unlocked); SetActive(p.onlyUnlock, unlocked); SetActive(p.useDirectly, unlocked && count > 0);
        SetActive(p.useCoin, false); SetActive(p.watchAd, unlocked && count <= 0); SetActive(p.effect, false);
        if (p.count != null) p.count.text = count.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (p.frame != null) { p.frame.enabled = unlocked; p.frame.sprite = MappedSprite("ItemBg_Gray"); RecordImage(p.frame, p.name, "frame", report); }
        p.bg.sprite = MappedSprite(unlocked ? "ItemBg_Normal" : "ItemBg_Lock"); p.bg.enabled = true;
        p.icon.sprite = MappedSprite(p.name + (unlocked ? "_Normal" : "_Lock")); p.icon.SetNativeSize();
        RecordImage(p.bg, p.name, "state background", report); RecordImage(p.icon, p.name, "preserved original glyph", report);
        if (p.locked != null) foreach (var i in p.locked.GetComponentsInChildren<Image>(true)) RecordImage(i, p.name, "preserved lock glyph", report);
        if (p.watchAd != null) foreach (var i in p.watchAd.GetComponentsInChildren<Image>(true)) RecordImage(i, p.name, "AD badge", report);
    }
    static Sprite MappedSprite(string name)
    {
        string key = "res/local/coreplay/sprite/item/" + name.ToLowerInvariant();
        if (!resourcePaths.TryGetValue(key, out var path)) throw new InvalidDataException("Missing production mapping: " + key);
        foreach (string root in new[] { "Assets/FruitsHarvest/Resources/", "Assets/OrchardUI/Resources/" })
            foreach (string extension in new[] { ".asset", ".png" })
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(root + path + extension);
                if (sprite != null) return sprite;
            }
        throw new FileNotFoundException("Mapped sprite was not found: " + path);
    }
    static bool Pure(Component c)
    {
        // Exact built-in types only; the project's TextMeshProCustom is verified
        // to be an empty TextMeshProUGUI subclass with no lifecycle overrides.
        Type t = c.GetType();
        return t == typeof(Transform) || t == typeof(RectTransform) || t == typeof(Canvas) || t == typeof(CanvasRenderer) ||
            t == typeof(CanvasGroup) || t == typeof(Image) || t == typeof(RawImage) || t == typeof(TextMeshProUGUI) ||
            t == typeof(TextMeshProCustom) || t == typeof(TMP_SubMeshUI) || t == typeof(CanvasScaler) || t == typeof(Mask) ||
            t == typeof(RectMask2D) || t == typeof(HorizontalLayoutGroup) || t == typeof(VerticalLayoutGroup) ||
            t == typeof(GridLayoutGroup) || t == typeof(LayoutElement) || t == typeof(ContentSizeFitter) ||
            t == typeof(AspectRatioFitter) || t == typeof(Shadow) || t == typeof(Outline);
    }
    static void Strip(GameObject holder, CaseReport report)
    {
        // Scripts first, renderer/particle components afterward to satisfy RequireComponent relationships.
        foreach (var component in holder.GetComponentsInChildren<MonoBehaviour>(true))
            if (component != null && !Pure(component)) { Object.DestroyImmediate(component); report.strippedComponents++; }
        foreach (var component in holder.GetComponentsInChildren<Renderer>(true))
            if (component != null) { Object.DestroyImmediate(component); report.strippedComponents++; }
        foreach (var component in holder.GetComponentsInChildren<Component>(true))
            if (component != null && !Pure(component)) { Object.DestroyImmediate(component); report.strippedComponents++; }
        foreach (var t in holder.GetComponentsInChildren<Transform>(true)) GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
    }
    static void AssertPure(GameObject holder, CaseReport report)
    {
        foreach (var c in holder.GetComponentsInChildren<Component>(true)) if (c != null && !Pure(c)) report.remainingBusinessComponents++;
        if (report.remainingBusinessComponents != 0) throw new InvalidOperationException("Business component remains; refusing preview activation.");
    }
    static void RecordButtons(GameObject source, CaseReport report)
    {
        foreach (var b in source.GetComponentsInChildren<Button>(true))
            report.sourceButtons.Add(new ButtonRecord { path = PathOf(b.transform), targetGraphic = b.targetGraphic != null ? PathOf(b.targetGraphic.transform) : null,
                persistentCalls = b.onClick.GetPersistentEventCount() });
    }
    static void RecordImage(Image image, string owner, string role, CaseReport report)
    {
        if (image == null) return;
        Sprite sprite = image.sprite;
        report.resources.Add(new ResourceRecord { owner = owner, role = role, path = AssetDatabase.GetAssetPath(sprite), sprite = sprite != null ? sprite.name : "null",
            texturePath = sprite != null ? AssetDatabase.GetAssetPath(sprite.texture) : "", spriteRect = sprite != null ? sprite.rect : new Rect(), active = image.enabled && image.gameObject.activeSelf });
    }
    static bool KeepRelevant(Transform node, List<Transform> roots)
    {
        foreach (var root in roots) if (node == root) { node.gameObject.SetActive(true); return true; }
        bool keep = false;
        foreach (Transform child in node) { bool relevant = KeepRelevant(child, roots); child.gameObject.SetActive(relevant); keep |= relevant; }
        return keep;
    }
    static void DisableChildren(Transform parent) { foreach (Transform child in parent) child.gameObject.SetActive(false); }
    static void SetActive(GameObject obj, bool active) { if (obj != null) obj.SetActive(active); }
    static Transform Need(Transform root, string path) { var t = root.Find(path); if (t == null) throw new InvalidDataException("Missing hierarchy: " + path); return t; }
    static string PathOf(Transform node) { return node.parent == null ? node.name : PathOf(node.parent) + "/" + node.name; }
    static string Hash(string path) { using (var stream = File.OpenRead(path)) using (var hash = SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", "").ToLowerInvariant(); }
}
