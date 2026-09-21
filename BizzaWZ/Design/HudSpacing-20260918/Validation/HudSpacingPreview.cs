using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>
/// Temporary, isolated HUD asset validation. Install in an Editor folder only.
/// No account, save, runtime business method, UI action, scene selection, or asset save.
/// All visible content comes from disposable production-prefab instances.
/// </summary>
[InitializeOnLoad]
public static class HudSpacingPreview
{
    private const string CanvasPath = "Assets/BizzaWZ/Common/Framework/GameCanvas.prefab";
    private const string PanelPath = "Assets/BizzaWZ/Common/UI/GamePanel/RealGamePanel.prefab";
    private const string WidgetPath = "Assets/BizzaWZ/Final/Real/UI/GamePanel/Resources/Resources/GameUiWidget.prefab";
    private const string BarPath = "Assets/BizzaWZ/Final/MenuSystem/Common/CurrencyBar/CurrencyBar.prefab";
    private const int Width = 1080;
    private const int CropPixelHeight = 320;
    private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);
    private static string DirectoryPath => Path.GetFullPath(Path.Combine(Application.dataPath, "../Design/HudSpacing-20260918"));
    private static readonly BridgeState State = new BridgeState();
    private static Queue<Case> queue;
    private static BatchReport batch;
    private static double nextStateTime;

    [Serializable] private sealed class BridgeState
    {
        public string kind = "STATIC PREFAB PREVIEW bridge; never a runtime controller";
        public string updatedUtc;
        public bool playing;
        public bool compiling;
        public bool waitingForEditMode;
        public bool busy;
        public string lastCommand;
        public string lastOutput;
        public string lastError;
        public int completed;
        public int total;
    }

    [Serializable] private sealed class Case
    {
        public string name;
        public int height;
        public bool longValues;
        public float currencyScale;
    }

    [Serializable] public sealed class RectInfo
    {
        public Vector2 anchoredPosition;
        public Vector2 sizeDelta;
        public Vector2 size;
        public Vector2 anchorMin;
        public Vector2 anchorMax;
        public Vector2 pivot;
        public Vector3 localScale;
        public Rect screenRect;
        public Rect pngRectTopLeft;
    }

    [Serializable] public sealed class ImageInfo
    {
        public string path;
        public string sourceGuid;
        public long sourceFileID;
        public string spriteAsset;
        public Color color;
        public bool active;
        public bool enabled;
        public bool raycast;
        public RectInfo rect;
    }

    [Serializable] public sealed class TextInfo
    {
        public string path;
        public string text;
        public string sourceGuid;
        public long sourceFileID;
        public bool active;
        public bool enabled;
        public bool isTextOverflowing;
        public bool glyphOverflowsRect;
        public Vector4 glyphOverflowPixels;
        public bool autoSize;
        public float actualFontSize;
        public float fontSizeMin;
        public float fontSizeMax;
        public int lineCount;
        public int firstOverflowCharacterIndex;
        public RectInfo rect;
        public Rect renderedGlyphBounds;
    }

    [Serializable] public sealed class ButtonInfo
    {
        public string path;
        public string sourceGuid;
        public long sourceFileID;
        public string targetGraphicPath;
        public bool active;
        public bool enabled;
        public bool interactable;
        public bool ancestorsAllowRaycasts;
        public bool estimatedClickable;
        public RectInfo rect;
        public List<Rect> activeRaycastRects = new List<Rect>();
        public List<Vector4> raycastPadding = new List<Vector4>();
    }

    [Serializable] public sealed class OverlapInfo
    {
        public string first;
        public string second;
        public Rect intersection;
        public float areaPixels;
    }

    [Serializable] public sealed class ClusterInfo
    {
        public string path;
        public bool hasVisibleGraphics;
        public Rect bounds;
        public bool outsideScreen;
    }

    [Serializable] public sealed class LayoutInfo
    {
        public string path;
        public bool enabled;
        public float spacing;
        public bool childScaleWidth;
        public bool childScaleHeight;
        public bool childControlWidth;
        public bool childForceExpandWidth;
    }

    [Serializable] public sealed class AssetFingerprint
    {
        public string path;
        public string sha256Before;
        public string sha256After;
        public bool unchanged;
    }

    [Serializable] public sealed class Report
    {
        public string kind = "PREFAB STATIC PREVIEW WITH EXPLICIT SAMPLE TEXT — NOT UNITY GAMEPLAY OR ACCOUNT DATA";
        public string generatedUtc;
        public string name;
        public string screenshot;
        public string error;
        public string interpretation = "Production GameCanvas, RealGamePanel/Content and GameUiWidget instances in a disposable PreviewScene. Business behaviours, animation and audio are disabled before activation. Canvas coordinates reproduce the production MatchHeight scaler; no current Game view, account or save is read. Geometry checks are conservative rectangular estimates, not input-event tests or pixel silhouettes. Samples and currency animation extremes exist only on disposable clones.";
        public int screenWidth;
        public int screenHeight;
        public int pngWidth;
        public int pngHeight;
        public float canvasPixelScale;
        public Vector2 productionReferenceResolution;
        public float productionMatchWidthOrHeight;
        public Vector2 canvasLogicalSize;
        public float currencyScale;
        public bool longValues;
        public int disabledBusinessBehaviours;
        public int activeTextOverflowCount;
        public int activeGlyphOverflowCount;
        public int activeButtonOverlapCount;
        public int clusterOverlapCount;
        public int nonUniformPixelSamples;
        public List<string> sampleChanges = new List<string>();
        public List<ImageInfo> images = new List<ImageInfo>();
        public List<TextInfo> texts = new List<TextInfo>();
        public List<ButtonInfo> buttons = new List<ButtonInfo>();
        public List<LayoutInfo> layouts = new List<LayoutInfo>();
        public List<OverlapInfo> buttonOverlaps = new List<OverlapInfo>();
        public List<ClusterInfo> clusters = new List<ClusterInfo>();
        public List<OverlapInfo> clusterOverlaps = new List<OverlapInfo>();
        public List<AssetFingerprint> sourceAssets = new List<AssetFingerprint>();
    }

    [Serializable] private sealed class BatchReport
    {
        public string kind = "PREFAB STATIC PREVIEW BATCH — NOT RUNTIME";
        public string generatedUtc;
        public string folder;
        public List<Report> cases = new List<Report>();
    }

    static HudSpacingPreview()
    {
        EditorApplication.update += Tick;
        AssemblyReloadEvents.beforeAssemblyReload += BeforeReload;
    }

    private static void BeforeReload()
    {
        if (queue != null)
        {
            State.lastError = "Static preview interrupted by assembly reload; submit a new command after compilation.";
            SaveBatch();
        }
        WriteState();
    }

    private static void Tick()
    {
        State.playing = EditorApplication.isPlayingOrWillChangePlaymode;
        State.compiling = EditorApplication.isCompiling || EditorApplication.isUpdating;
        State.waitingForEditMode = State.playing && (queue != null || File.Exists(CommandPath));
        if (!State.playing && !State.compiling)
        {
            if (queue != null) RenderNext();
            else ReadCommand();
        }
        if (EditorApplication.timeSinceStartup >= nextStateTime)
        {
            nextStateTime = EditorApplication.timeSinceStartup + 2;
            WriteState();
        }
    }

    private static string CommandPath => Path.Combine(DirectoryPath, "preview.command");

    private static void ReadCommand()
    {
        if (!File.Exists(CommandPath)) return;
        string command;
        try { command = File.ReadAllText(CommandPath).Trim(); File.Delete(CommandPath); }
        catch (IOException) { return; }
        State.lastCommand = command;
        State.lastError = string.Empty;
        try
        {
            bool extended = command == "preview-all" || command.StartsWith("preview-all:", StringComparison.Ordinal);
            if (!extended && command != "preview" && !command.StartsWith("preview:", StringComparison.Ordinal))
                throw new ArgumentException("Allowed command: preview[:label] (two 1080x1920 samples) or preview-all[:label] (eight aspect/animation samples).");
            int separator = command.IndexOf(':');
            string label = separator < 0 ? DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") : SafeName(command.Substring(separator + 1));
            batch = new BatchReport { generatedUtc = DateTime.UtcNow.ToString("o"), folder = Path.Combine(DirectoryPath, "Previews", label) };
            Directory.CreateDirectory(batch.folder);
            queue = new Queue<Case>();
            foreach (int height in extended ? new[] { 1920, 2340 } : new[] { 1920 })
                foreach (bool longValues in new[] { false, true })
                    foreach (float scale in extended ? new[] { 1f, 1.25f } : new[] { 1f })
                        queue.Enqueue(new Case
                        {
                            name = "static-hud-1080x" + height + "-" + (longValues ? "long" : "sample") + "-" + (scale > 1f ? "scale125" : "rest"),
                            height = height, longValues = longValues, currencyScale = scale
                        });
            State.completed = 0;
            State.total = queue.Count;
            State.busy = true;
        }
        catch (Exception exception) { State.lastError = exception.ToString(); }
        WriteState();
    }

    private static void RenderNext()
    {
        if (queue.Count == 0)
        {
            SaveBatch();
            State.lastOutput = Path.Combine(batch.folder, "report.json");
            queue = null;
            State.busy = false;
            WriteState();
            return;
        }
        Case item = queue.Dequeue();
        Report report;
        try { report = Render(item, batch.folder); }
        catch (Exception exception)
        {
            report = new Report { name = item.name, error = exception.ToString(), generatedUtc = DateTime.UtcNow.ToString("o") };
            State.lastError = exception.ToString();
        }
        batch.cases.Add(report);
        WriteJson(Path.Combine(batch.folder, item.name + ".json"), report);
        State.completed++;
        State.lastOutput = report.screenshot;
        SaveBatch();
        WriteState();
    }

    private static Report Render(Case item, string folder)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit mode only.");
        var report = new Report
        {
            generatedUtc = DateTime.UtcNow.ToString("o"), name = item.name,
            screenWidth = Width, screenHeight = item.height, pngWidth = Width,
            currencyScale = item.currencyScale, longValues = item.longValues,
            screenshot = Path.Combine(folder, item.name + ".png")
        };
        foreach (string path in new[] { CanvasPath, PanelPath, WidgetPath, BarPath })
            report.sourceAssets.Add(new AssetFingerprint { path = path, sha256Before = Hash(path) });
        Scene scene = EditorSceneManager.NewPreviewScene();
        RenderTexture target = null;
        Texture2D image = null;
        RenderTexture priorTarget = RenderTexture.active;
        try
        {
            GameObject holder = NewObject("HUD static preview inactive staging", scene);
            holder.SetActive(false);
            GameObject canvasObject = Instantiate(CanvasPath, holder.transform, report);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            if (canvas == null || scaler == null) throw new InvalidDataException("Production Canvas or CanvasScaler missing.");
            report.productionReferenceResolution = scaler.referenceResolution;
            report.productionMatchWidthOrHeight = scaler.matchWidthOrHeight;
            if (scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize ||
                scaler.screenMatchMode != CanvasScaler.ScreenMatchMode.MatchWidthOrHeight ||
                !Mathf.Approximately(scaler.matchWidthOrHeight, 1f))
                throw new InvalidDataException("Production CanvasScaler changed; update preview assumptions before rendering.");
            report.canvasPixelScale = item.height / scaler.referenceResolution.y;
            report.canvasLogicalSize = new Vector2(Width / report.canvasPixelScale, scaler.referenceResolution.y);
            report.pngHeight = Mathf.Min(item.height, CropPixelHeight);
            scaler.enabled = false; // World-space camera below reproduces the scaler mathematically.
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.pixelPerfect = false;
            canvas.referencePixelsPerUnit = scaler.referencePixelsPerUnit;
            RectTransform canvasRect = (RectTransform)canvas.transform;
            canvasRect.localScale = Vector3.one;
            canvasRect.localRotation = Quaternion.identity;
            canvasRect.sizeDelta = report.canvasLogicalSize;
            canvasRect.localPosition = new Vector3((canvasRect.pivot.x - .5f) * report.canvasLogicalSize.x,
                (canvasRect.pivot.y - .5f) * report.canvasLogicalSize.y, 0f);
            Transform content = Require(canvas.transform, "Content");
            KeepChild(canvas.transform, content);
            Transform baseLayer = Require(content, "BaseLayer");
            KeepChild(content, baseLayer);
            DisableChildren(baseLayer);
            GameObject panel = Instantiate(PanelPath, baseLayer, report);
            Transform panelContent = Require(panel.transform, "Content");
            KeepChild(panel.transform, panelContent);
            DisableChildren(panelContent);
            GameObject widget = Instantiate(WidgetPath, panelContent, report);
            Transform bar = Require(widget.transform, "CurrencyBar");
            KeepChild(widget.transform, bar);
            ApplySamples(bar, item, report);

            GameObject cameraObject = NewObject("HUD static render camera", scene);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.scene = scene;
            camera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(scene);
            camera.cameraType = CameraType.Game;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.64f, .84f, .92f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = report.canvasLogicalSize.y * .5f;
            camera.aspect = (float)Width / item.height;
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 2000f;
            camera.transform.position = new Vector3(0, 0, -1000);
            camera.allowHDR = false;
            camera.allowMSAA = false;
            target = new RenderTexture(Width, item.height, 24, RenderTextureFormat.ARGB32);
            target.Create();
            camera.targetTexture = target;
            foreach (Canvas childCanvas in canvasObject.GetComponentsInChildren<Canvas>(true))
            {
                childCanvas.renderMode = RenderMode.WorldSpace;
                childCanvas.worldCamera = camera;
            }
            holder.SetActive(true);
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)widget.transform);
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)Require(bar, "CurrentGroup"));
            Canvas.ForceUpdateCanvases();
            foreach (TMP_Text text in bar.GetComponentsInChildren<TMP_Text>(true))
                if (text.gameObject.activeInHierarchy && text.enabled) text.ForceMeshUpdate();
            Canvas.ForceUpdateCanvases();
            camera.Render();
            Collect(bar, camera, report);
            RenderTexture.active = target;
            image = new Texture2D(Width, report.pngHeight, TextureFormat.RGBA32, false);
            image.ReadPixels(new Rect(0, item.height - report.pngHeight, Width, report.pngHeight), 0, 0);
            image.Apply(false, false);
            var pixels = image.GetRawTextureData<Color32>();
            Color32 first = pixels[0];
            for (int index = 0; index < pixels.Length; index += 127)
            {
                Color32 pixel = pixels[index];
                if (Math.Abs(pixel.r - first.r) > 2 || Math.Abs(pixel.g - first.g) > 2 || Math.Abs(pixel.b - first.b) > 2)
                    report.nonUniformPixelSamples++;
            }
            if (report.nonUniformPixelSamples == 0) report.error = "Uniform render: this image is not valid visual verification.";
            File.WriteAllBytes(report.screenshot, image.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = priorTarget;
            if (image != null) Object.DestroyImmediate(image);
            if (target != null) { target.Release(); Object.DestroyImmediate(target); }
            EditorSceneManager.ClosePreviewScene(scene);
            foreach (AssetFingerprint fingerprint in report.sourceAssets)
            {
                fingerprint.sha256After = Hash(fingerprint.path);
                fingerprint.unchanged = fingerprint.sha256Before == fingerprint.sha256After;
                if (!fingerprint.unchanged) report.error = "An input prefab changed during rendering; discard this mixed-version preview and rerun.";
            }
        }
        return report;
    }

    private static GameObject Instantiate(string path, Transform inactiveParent, Report report)
    {
        if (inactiveParent.gameObject.activeInHierarchy) throw new InvalidOperationException("Preview parent must remain inactive during staging.");
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (asset == null) throw new FileNotFoundException("Production prefab missing", path);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, inactiveParent);
        instance.hideFlags = HideFlags.HideAndDontSave;
        foreach (MonoBehaviour behaviour in instance.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (behaviour == null || IsVisual(behaviour)) continue;
            if (behaviour.enabled) report.disabledBusinessBehaviours++;
            behaviour.enabled = false;
        }
        foreach (Animator animator in instance.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
        foreach (Animation animation in instance.GetComponentsInChildren<Animation>(true)) { animation.Stop(); animation.enabled = false; }
        foreach (ParticleSystem particle in instance.GetComponentsInChildren<ParticleSystem>(true)) particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        foreach (AudioSource audio in instance.GetComponentsInChildren<AudioSource>(true)) audio.enabled = false;
        foreach (Camera camera in instance.GetComponentsInChildren<Camera>(true)) camera.enabled = false;
        return instance;
    }

    private static bool IsVisual(MonoBehaviour component)
    {
        return component is Graphic || component is BaseMeshEffect || component is LayoutGroup ||
            component is LayoutElement || component is ContentSizeFitter || component is AspectRatioFitter ||
            component is Mask || component is RectMask2D || component is TMP_SubMeshUI || component is Button;
    }

    private static void ApplySamples(Transform bar, Case item, Report report)
    {
        SetText(bar, "Image/Text (TMP)", item.longValues ? "9999" : "1", report);
        SetText(bar, "CurrentGroup/GoldGroup/CoinBox/GoldText", item.longValues ? "99.999,99" : "0,08", report);
        SetText(bar, "CurrentGroup/DollarGroup/DollarBox/DollarText", item.longValues ? "R$9.999,99" : "200.76", report);
        SetText(bar, "CurrentGroup/GoldGroup/ButtonView/Text (TMP)", item.longValues ? "R$9.999,99" : "R$0,00", report);
        SetText(bar, "CurrentGroup/GoldGroup/ButtonView/Text (TMP) (1)", "≈", report);
        SetText(bar, "CurrentGroup/DollarGroup/ButtonView/Text (TMP)", "Retirar", report);
        // Explicit resting sample for the two transient reward labels; no tween or business method runs.
        SetText(bar, "CurrentGroup/GoldGroup/DollarAddText (1)", string.Empty, report);
        SetText(bar, "CurrentGroup/DollarGroup/DollarAddText", string.Empty, report);
        Require(bar, "CurrentGroup/GoldGroup").localScale = Vector3.one * item.currencyScale;
        Require(bar, "CurrentGroup/DollarGroup").localScale = Vector3.one * item.currencyScale;
        report.sampleChanges.Add("Both production currency animation targets use scale " + item.currencyScale.ToString(System.Globalization.CultureInfo.InvariantCulture) + "; the existing HorizontalLayoutGroup is kept enabled and rebuilt.");
    }

    private static void SetText(Transform bar, string path, string value, Report report)
    {
        TMP_Text text = Require(bar, path).GetComponent<TMP_Text>();
        if (text == null) throw new InvalidDataException("Expected production TMP text at " + path);
        text.text = value;
        report.sampleChanges.Add(path + " = " + value);
    }

    private static void Collect(Transform bar, Camera camera, Report report)
    {
        foreach (Image image in bar.GetComponentsInChildren<Image>(true))
        {
            Source(image, out string guid, out long fileID);
            report.images.Add(new ImageInfo
            {
                path = PathOf(image.transform, bar), sourceGuid = guid, sourceFileID = fileID,
                spriteAsset = image.overrideSprite == null ? string.Empty : AssetDatabase.GetAssetPath(image.overrideSprite),
                color = image.color, active = image.gameObject.activeInHierarchy, enabled = image.enabled,
                raycast = image.raycastTarget, rect = Describe(image.rectTransform, camera, report)
            });
        }
        foreach (TMP_Text text in bar.GetComponentsInChildren<TMP_Text>(true))
        {
            Source(text, out string guid, out long fileID);
            bool active = text.gameObject.activeInHierarchy && text.enabled;
            RectInfo textRect = Describe(text.rectTransform, camera, report);
            Rect glyphs = GlyphBounds(text, camera);
            Vector4 overflow = glyphs.width <= 0 || glyphs.height <= 0 ? Vector4.zero : new Vector4(
                Mathf.Max(0, textRect.screenRect.xMin - glyphs.xMin),
                Mathf.Max(0, textRect.screenRect.yMin - glyphs.yMin),
                Mathf.Max(0, glyphs.xMax - textRect.screenRect.xMax),
                Mathf.Max(0, glyphs.yMax - textRect.screenRect.yMax));
            bool geometricOverflow = Mathf.Max(Mathf.Max(overflow.x, overflow.y), Mathf.Max(overflow.z, overflow.w)) > 1f;
            report.texts.Add(new TextInfo
            {
                path = PathOf(text.transform, bar), text = text.text, sourceGuid = guid, sourceFileID = fileID,
                active = text.gameObject.activeInHierarchy, enabled = text.enabled,
                isTextOverflowing = text.isTextOverflowing, autoSize = text.enableAutoSizing,
                glyphOverflowsRect = geometricOverflow, glyphOverflowPixels = overflow,
                actualFontSize = text.fontSize, fontSizeMin = text.fontSizeMin, fontSizeMax = text.fontSizeMax,
                lineCount = text.textInfo == null ? 0 : text.textInfo.lineCount,
                firstOverflowCharacterIndex = text.firstOverflowCharacterIndex,
                rect = textRect, renderedGlyphBounds = glyphs
            });
            if (active && text.isTextOverflowing && !string.IsNullOrEmpty(text.text)) report.activeTextOverflowCount++;
            if (active && geometricOverflow && !string.IsNullOrEmpty(text.text)) report.activeGlyphOverflowCount++;
        }
        foreach (Button button in bar.GetComponentsInChildren<Button>(true))
        {
            Source(button, out string guid, out long fileID);
            var entry = new ButtonInfo
            {
                path = PathOf(button.transform, bar), sourceGuid = guid, sourceFileID = fileID,
                targetGraphicPath = button.targetGraphic == null ? string.Empty : PathOf(button.targetGraphic.transform, bar),
                active = button.gameObject.activeInHierarchy, enabled = button.enabled,
                interactable = button.IsInteractable(), ancestorsAllowRaycasts = AllowsRaycasts(button.transform),
                rect = Describe(button.transform as RectTransform, camera, report)
            };
            foreach (Graphic graphic in button.GetComponentsInChildren<Graphic>(true))
            {
                if (!graphic.isActiveAndEnabled || !graphic.raycastTarget || graphic.GetComponentInParent<Button>() != button) continue;
                entry.activeRaycastRects.Add(ScreenRect(graphic.rectTransform, camera));
                entry.raycastPadding.Add(graphic.raycastPadding);
            }
            entry.estimatedClickable = entry.active && entry.enabled && entry.interactable && entry.ancestorsAllowRaycasts && entry.activeRaycastRects.Count > 0;
            report.buttons.Add(entry);
        }
        for (int first = 0; first < report.buttons.Count; first++)
            for (int second = first + 1; second < report.buttons.Count; second++)
            {
                ButtonInfo a = report.buttons[first], b = report.buttons[second];
                if (!a.estimatedClickable || !b.estimatedClickable) continue;
                foreach (Rect ar in a.activeRaycastRects)
                    foreach (Rect br in b.activeRaycastRects)
                        AddOverlap(report.buttonOverlaps, a.path, b.path, ar, br);
            }
        report.activeButtonOverlapCount = report.buttonOverlaps.Count;
        foreach (HorizontalLayoutGroup layout in bar.GetComponentsInChildren<HorizontalLayoutGroup>(true))
            report.layouts.Add(new LayoutInfo
            {
                path = PathOf(layout.transform, bar), enabled = layout.enabled, spacing = layout.spacing,
                childScaleWidth = layout.childScaleWidth, childScaleHeight = layout.childScaleHeight,
                childControlWidth = layout.childControlWidth, childForceExpandWidth = layout.childForceExpandWidth
            });
        foreach (string path in new[] { "Image", "CurrentGroup/GoldGroup", "CurrentGroup/DollarGroup", "PauseButton" })
        {
            Transform cluster = Require(bar, path);
            var entry = new ClusterInfo { path = PathOf(cluster, bar) };
            foreach (Graphic graphic in cluster.GetComponentsInChildren<Graphic>(true))
            {
                if (!graphic.isActiveAndEnabled || graphic.color.a < .01f || graphic.canvasRenderer.GetInheritedAlpha() < .01f) continue;
                Rect bounds;
                if (graphic is TMP_Text text)
                {
                    if (string.IsNullOrEmpty(text.text)) continue;
                    bounds = GlyphBounds(text, camera);
                    if (bounds.width <= 0 || bounds.height <= 0) continue;
                }
                else bounds = ScreenRect(graphic.rectTransform, camera);
                entry.bounds = entry.hasVisibleGraphics ? Union(entry.bounds, bounds) : bounds;
                entry.hasVisibleGraphics = true;
            }
            entry.outsideScreen = entry.hasVisibleGraphics && (entry.bounds.xMin < 0 || entry.bounds.yMin < 0 || entry.bounds.xMax > report.screenWidth || entry.bounds.yMax > report.screenHeight);
            report.clusters.Add(entry);
        }
        for (int first = 0; first < report.clusters.Count; first++)
            for (int second = first + 1; second < report.clusters.Count; second++)
            {
                ClusterInfo a = report.clusters[first], b = report.clusters[second];
                if (a.hasVisibleGraphics && b.hasVisibleGraphics) AddOverlap(report.clusterOverlaps, a.path, b.path, a.bounds, b.bounds);
            }
        report.clusterOverlapCount = report.clusterOverlaps.Count;
    }

    private static RectInfo Describe(RectTransform transform, Camera camera, Report report)
    {
        if (transform == null) return null;
        Rect rect = ScreenRect(transform, camera);
        return new RectInfo
        {
            anchoredPosition = transform.anchoredPosition, sizeDelta = transform.sizeDelta, size = transform.rect.size,
            anchorMin = transform.anchorMin, anchorMax = transform.anchorMax, pivot = transform.pivot, localScale = transform.localScale,
            screenRect = rect, pngRectTopLeft = new Rect(rect.x, report.screenHeight - rect.yMax, rect.width, rect.height)
        };
    }

    private static Rect ScreenRect(RectTransform transform, Camera camera)
    {
        var corners = new Vector3[4];
        transform.GetWorldCorners(corners);
        Vector2 min = camera.WorldToScreenPoint(corners[0]), max = min;
        for (int index = 1; index < corners.Length; index++)
        {
            Vector2 point = camera.WorldToScreenPoint(corners[index]);
            min = Vector2.Min(min, point); max = Vector2.Max(max, point);
        }
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    private static Rect GlyphBounds(TMP_Text text, Camera camera)
    {
        if (text == null || text.textInfo == null || text.textInfo.characterInfo == null) return Rect.zero;
        bool found = false;
        Vector2 min = Vector2.zero, max = Vector2.zero;
        for (int index = 0; index < Mathf.Min(text.textInfo.characterCount, text.textInfo.characterInfo.Length); index++)
        {
            TMP_CharacterInfo character = text.textInfo.characterInfo[index];
            if (!character.isVisible) continue;
            Vector2 a = camera.WorldToScreenPoint(text.transform.TransformPoint(character.bottomLeft));
            Vector2 b = camera.WorldToScreenPoint(text.transform.TransformPoint(character.topRight));
            if (!found) { min = Vector2.Min(a, b); max = Vector2.Max(a, b); found = true; }
            else { min = Vector2.Min(min, Vector2.Min(a, b)); max = Vector2.Max(max, Vector2.Max(a, b)); }
        }
        return found ? Rect.MinMaxRect(min.x, min.y, max.x, max.y) : Rect.zero;
    }

    private static void AddOverlap(List<OverlapInfo> list, string first, string second, Rect a, Rect b)
    {
        float left = Mathf.Max(a.xMin, b.xMin), right = Mathf.Min(a.xMax, b.xMax);
        float bottom = Mathf.Max(a.yMin, b.yMin), top = Mathf.Min(a.yMax, b.yMax);
        if (right - left <= .25f || top - bottom <= .25f) return;
        list.Add(new OverlapInfo { first = first, second = second, intersection = Rect.MinMaxRect(left, bottom, right, top), areaPixels = (right - left) * (top - bottom) });
    }

    private static Rect Union(Rect a, Rect b) => Rect.MinMaxRect(Mathf.Min(a.xMin, b.xMin), Mathf.Min(a.yMin, b.yMin), Mathf.Max(a.xMax, b.xMax), Mathf.Max(a.yMax, b.yMax));

    private static bool AllowsRaycasts(Transform transform)
    {
        while (transform != null)
        {
            bool stop = false;
            foreach (CanvasGroup group in transform.GetComponents<CanvasGroup>())
            {
                if (!group.enabled) continue;
                if (!group.blocksRaycasts) return false;
                if (group.ignoreParentGroups) stop = true;
            }
            if (stop) return true;
            transform = transform.parent;
        }
        return true;
    }

    private static void Source(Object instance, out string guid, out long fileID)
    {
        Object source = PrefabUtility.GetCorrespondingObjectFromOriginalSource(instance);
        guid = string.Empty; fileID = 0;
        if (source != null) AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source, out guid, out fileID);
    }

    private static Transform Require(Transform root, string path)
    {
        Transform value = root.Find(path);
        if (value == null) throw new InvalidDataException("Production hierarchy changed: missing " + root.name + "/" + path);
        return value;
    }

    private static void KeepChild(Transform parent, Transform keep)
    {
        for (int index = 0; index < parent.childCount; index++)
        {
            Transform child = parent.GetChild(index);
            child.gameObject.SetActive(child == keep);
        }
    }

    private static void DisableChildren(Transform parent)
    {
        for (int index = 0; index < parent.childCount; index++) parent.GetChild(index).gameObject.SetActive(false);
    }

    private static GameObject NewObject(string name, Scene scene)
    {
        var result = new GameObject(name) { hideFlags = HideFlags.HideAndDontSave };
        SceneManager.MoveGameObjectToScene(result, scene);
        return result;
    }

    private static string PathOf(Transform value, Transform root)
    {
        string path = value.name;
        while (value != root && value.parent != null) { value = value.parent; path = value.name + "/" + path; }
        return path;
    }

    private static string Hash(string assetPath)
    {
        string path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
        using (SHA256 sha = SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", string.Empty).ToLowerInvariant();
    }

    private static string SafeName(string value)
    {
        var result = new StringBuilder();
        foreach (char character in value) result.Append(char.IsLetterOrDigit(character) || character == '-' || character == '_' ? character : '-');
        return result.Length == 0 ? "current" : result.ToString();
    }

    private static void SaveBatch()
    {
        if (batch != null) WriteJson(Path.Combine(batch.folder, "report.json"), batch);
    }

    private static void WriteJson(string path, object value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, JsonUtility.ToJson(value, true), Utf8);
    }

    private static void WriteState()
    {
        try { State.updatedUtc = DateTime.UtcNow.ToString("o"); WriteJson(Path.Combine(DirectoryPath, "preview-state.json"), State); }
        catch (IOException) { }
    }
}
