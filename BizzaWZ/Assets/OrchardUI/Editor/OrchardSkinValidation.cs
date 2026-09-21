using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>
/// Explicit, editor-only asset checks and isolated visual previews. This tool never
/// starts gameplay, changes save data, saves a prefab, or replaces runtime loading.
/// Preview PNGs contain serialized defaults, not a simulated account or game state.
/// </summary>
[InitializeOnLoad]
public static class OrchardSkinValidation
{
    public const int PreviewWidth = 1080;
    public const int PreviewHeight = 1920;
    public static string OutputDirectory => Path.GetFullPath(Path.Combine(Application.dataPath, "../Design/OrchardUI"));
    public static string ManifestPath => Path.Combine(OutputDirectory, "prefab-manifest.json");

    [Serializable]
    public sealed class PrefabManifest
    {
        public int version = 1;
        public string[] prefabs;
    }

    [Serializable]
    public sealed class AuditIssue
    {
        public string key;
        public string prefab;
        public string objectPath;
        public string category;
        public string severity;
        public string detail;
        public bool existedInBaseline;
        public bool hasPrefabActivity;
        public bool activeInPrefab;
    }

    [Serializable]
    public sealed class AuditReport
    {
        public string generatedUtc;
        public string kind = "Read-only prefab asset audit";
        public string baselinePath;
        public bool baselineAvailable;
        public int prefabCount;
        public int buttonCount;
        public int errors;
        public int warnings;
        public int existingIssueCount;
        public int newIssueCount;
        public int resolvedIssueCount;
        public int existingErrorCount;
        public int newErrorCount;
        public int missingGuidOccurrences;
        public int distinctMissingAssetGuids;
        public int missingReferenceSlots;
        public int missingScriptObjects;
        public int missingLocalReferences;
        public int errorsOnActivePrefabObjects;
        public int errorsOnInactivePrefabObjects;
        public string interpretation = "Counts are evidence records, not independent runtime failures: a missing asset GUID can also appear in several unresolved component slots. activeInPrefab means serialized activeSelf along the ancestor chain, not proof of runtime reachability. Optional null references with instance ID zero are not errors. Baseline differences preserve all genuine missing references, including inactive branches.";
        public List<AuditIssue> issues = new List<AuditIssue>();
    }

    [Serializable]
    public sealed class PreviewEntry
    {
        public string prefab;
        public string image;
        public string error;
        public int disabledBusinessBehaviours;
        public int activeGraphics;
        public int nonUniformPixelSamples;
        public List<string> revealedContainers = new List<string>();
        public List<string> notes = new List<string>();
    }

    [Serializable]
    public sealed class PreviewReport
    {
        public string generatedUtc;
        public string kind = "STATIC PREFAB PREVIEW — serialized defaults only; business scripts and animations disabled. Not a runtime or account-flow verification.";
        public int width = PreviewWidth;
        public int height = PreviewHeight;
        public List<PreviewEntry> pages = new List<PreviewEntry>();
    }

    [Serializable]
    public sealed class RuntimeRect
    {
        public Vector3 localPosition;
        public Vector2 anchoredPosition;
        public Vector2 size;
        public Vector2 sizeDelta;
        public Vector2 anchorMin;
        public Vector2 anchorMax;
        public Vector2 pivot;
        public Rect screenRect;
    }

    [Serializable]
    public sealed class RuntimeImage
    {
        public string path;
        public string canvasPath;
        public string spriteName;
        public string assetPath;
        public string sourceSpriteName;
        public Color color;
        public RuntimeRect rect;
        public bool raycast;
        public bool enabled;
        public bool rendererCulled;
        public float rendererAlpha;
    }

    [Serializable]
    public sealed class RuntimeText
    {
        public string path;
        public string text;
        public string fontName;
        public string fontAssetPath;
        public Color color;
        public float fontSize;
        public bool enabled;
        public bool raycast;
        public RuntimeRect rect;
    }

    [Serializable]
    public sealed class RuntimeButton
    {
        public string path;
        public string targetGraphicPath;
        public bool enabled;
        public bool interactable;
        public bool clickable;
        public int activeRaycastGraphicCount;
        public RuntimeRect rect;
    }

    [Serializable]
    public sealed class RuntimeSnapshot
    {
        public string generatedUtc;
        public string screenshot;
        public string activeScene;
        public int screenWidth;
        public int screenHeight;
        public string interpretation = "Read-only snapshot of active GameObjects under a Canvas at screenshot request time. Disabled Image/TMP components are included and marked. clickable is a component/CanvasGroup/raycast-graphic estimate; no clicks or overlay occlusion raycasts are performed. The PNG is captured at Unity's next frame end.";
        public List<RuntimeImage> images = new List<RuntimeImage>();
        public List<RuntimeText> texts = new List<RuntimeText>();
        public List<RuntimeButton> buttons = new List<RuntimeButton>();
    }

    [Serializable]
    private sealed class BridgeState
    {
        public string updatedUtc;
        public bool playing;
        public bool compiling;
        public bool updating;
        public bool busy;
        public string lastCommand;
        public string lastOutput;
        public string lastError;
        public int compilationErrors;
        public int previewCompleted;
        public int previewTotal;
        public bool runtimeCapturePending;
        public bool runtimePageOpening;
    }

    private static readonly Regex LocalDefinition = new Regex(@"^--- !u!\d+ &(-?\d+)", RegexOptions.Multiline);
    private static readonly Regex LocalReference = new Regex(@"\{fileID:\s*(-?\d+)\s*\}");
    private static readonly Regex AssetReference = new Regex(@"\{fileID:\s*-?\d+,\s*guid:\s*([0-9a-fA-F]{32}),\s*type:\s*\d+\s*\}");
    private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);
    private static readonly BridgeState State = new BridgeState();
    private static readonly HashSet<string> IssueKeys = new HashSet<string>(StringComparer.Ordinal);
    private static Queue<string> previewQueue;
    private static PreviewReport previewReport;
    private static double nextStateWrite;
    private static string pendingCapture;
    private static double captureDeadline;

    static OrchardSkinValidation()
    {
        EditorApplication.update += Tick;
        AssemblyReloadEvents.beforeAssemblyReload += BeforeAssemblyReload;
        CompilationPipeline.compilationStarted += OnCompilationStarted;
        CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompilationFinished;
    }

    private static void OnCompilationStarted(object context)
    {
        State.compilationErrors = 0;
        WriteState();
    }

    private static void OnAssemblyCompilationFinished(string assembly, CompilerMessage[] messages)
    {
        for (int i = 0; i < messages.Length; i++)
            if (messages[i].type == CompilerMessageType.Error) State.compilationErrors++;
        WriteState();
    }

    private static void BeforeAssemblyReload()
    {
        if (previewQueue != null)
        {
            State.lastError = "Preview queue interrupted by script reload. Run preview again after compilation.";
            SavePartialPreviewReport();
        }
        WriteState();
    }

    private static void Tick()
    {
        if (!EditorApplication.isCompiling && !EditorApplication.isUpdating)
        {
            if (previewQueue != null)
                TickPreview();
            else if (!State.runtimePageOpening)
                ReadCommand();

            if (!string.IsNullOrEmpty(pendingCapture))
            {
                if (File.Exists(pendingCapture))
                {
                    State.lastOutput = pendingCapture;
                    pendingCapture = null;
                }
                else if (EditorApplication.timeSinceStartup > captureDeadline)
                {
                    State.lastError = "Runtime screenshot was requested but no PNG arrived within 30 seconds.";
                    pendingCapture = null;
                }
            }
        }

        if (EditorApplication.timeSinceStartup >= nextStateWrite)
        {
            nextStateWrite = EditorApplication.timeSinceStartup + 2;
            WriteState();
        }
    }

    private static void ReadCommand()
    {
        string path = Path.Combine(OutputDirectory, "validation.command");
        if (!File.Exists(path)) return;
        string command;
        try
        {
            command = File.ReadAllText(path).Trim();
            File.Delete(path);
        }
        catch (IOException) { return; }

        State.lastCommand = command;
        State.lastError = string.Empty;
        State.busy = true;
        WriteState();
        try
        {
            if (command == "refresh") Refresh();
            else if (command == "audit") Audit("current");
            else if (command.StartsWith("audit:", StringComparison.Ordinal)) Audit(command.Substring(6));
            else if (command == "preview") PreviewAll();
            else if (command.StartsWith("preview:", StringComparison.Ordinal)) PreviewNamed(command.Substring(8));
            else if (command == "capture-runtime") CaptureRuntime();
            else if (command.StartsWith("open:", StringComparison.Ordinal)) OpenRuntimePage(command.Substring(5)).Forget(LogRuntimeOpenFailure);
            else if (command.StartsWith("close:", StringComparison.Ordinal)) CloseRuntimePage(command.Substring(6));
            else throw new ArgumentException("Unknown validation command: " + command);
        }
        catch (Exception exception)
        {
            State.lastError = exception.ToString();
            Debug.LogException(exception);
        }
        finally
        {
            State.busy = previewQueue != null || State.runtimePageOpening;
            WriteState();
        }
    }

    public static void Refresh()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        State.lastOutput = "AssetDatabase.Refresh completed; observe state and Editor.log for any ensuing compilation.";
    }

    /// <summary>Use suffix "baseline" before changes, then "current" afterward.</summary>
    public static AuditReport Audit(string outputSuffix = "current")
    {
        string suffix = SafeName(string.IsNullOrWhiteSpace(outputSuffix) ? "current" : outputSuffix);
        var manifest = ReadManifest();
        string baselinePath = Path.Combine(OutputDirectory, "audit-baseline.json");
        bool isBaseline = string.Equals(suffix, "baseline", StringComparison.Ordinal);
        var baselineKeys = new HashSet<string>(StringComparer.Ordinal);
        bool baselineAvailable = !isBaseline && File.Exists(baselinePath);
        if (baselineAvailable)
        {
            var baseline = JsonUtility.FromJson<AuditReport>(File.ReadAllText(baselinePath));
            if (baseline != null && baseline.issues != null)
                foreach (var issue in baseline.issues) baselineKeys.Add(issue.key);
        }

        var report = new AuditReport
        {
            generatedUtc = UtcNow(),
            baselinePath = baselinePath,
            baselineAvailable = baselineAvailable,
            prefabCount = manifest.prefabs.Length
        };
        IssueKeys.Clear();
        foreach (string path in manifest.prefabs)
        {
            try { AuditPrefab(path, report); }
            catch (Exception exception)
            {
                AddIssue(report, path, string.Empty, "audit-exception", "error", exception.Message);
            }
        }

        var missingGuids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var issue in report.issues)
        {
            issue.existedInBaseline = baselineKeys.Contains(issue.key);
            if (issue.severity == "error")
            {
                report.errors++;
                if (issue.existedInBaseline) report.existingErrorCount++; else report.newErrorCount++;
                if (issue.hasPrefabActivity)
                {
                    if (issue.activeInPrefab) report.errorsOnActivePrefabObjects++;
                    else report.errorsOnInactivePrefabObjects++;
                }
            }
            else report.warnings++;
            if (issue.existedInBaseline) report.existingIssueCount++; else report.newIssueCount++;
            if (issue.category == "missing-asset-guid")
            {
                report.missingGuidOccurrences++;
                missingGuids.Add(issue.objectPath);
            }
            else if (issue.category.StartsWith("missing-object-reference:", StringComparison.Ordinal)) report.missingReferenceSlots++;
            else if (issue.category == "missing-script") report.missingScriptObjects++;
            else if (issue.category == "missing-local-reference") report.missingLocalReferences++;
        }
        report.distinctMissingAssetGuids = missingGuids.Count;
        if (baselineAvailable)
            foreach (string key in baselineKeys)
                if (!IssueKeys.Contains(key)) report.resolvedIssueCount++;

        string reportPath = Path.Combine(OutputDirectory, "audit-" + suffix + ".json");
        WriteJson(reportPath, report);
        State.lastOutput = reportPath;
        Debug.Log("[OrchardUI] Audit " + suffix + ": " + report.prefabCount + " prefabs, " + report.errors + " errors, " + report.warnings + " warnings; baseline=" + baselineAvailable + ". " + reportPath);
        return report;
    }

    private static void AuditPrefab(string path, AuditReport report)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (asset == null)
        {
            AddIssue(report, path, string.Empty, "missing-prefab", "error", "Prefab could not be loaded.");
            return;
        }

        AuditYamlReferences(path, report);
        foreach (Transform transform in asset.GetComponentsInChildren<Transform>(true))
        {
            string objectPath = HierarchyPath(transform, asset.transform);
            string transformKey = LocalObjectKey(transform);
            int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject);
            if (missing > 0)
                AddIssue(report, path, objectPath, "missing-script", "error", missing + " missing MonoBehaviour script(s).", transformKey, transform.gameObject);

            var components = transform.GetComponents<Component>();
            for (int index = 0; index < components.Length; index++)
            {
                Component component = components[index];
                if (component == null) continue;
                using (var serialized = new SerializedObject(component))
                {
                    var property = serialized.GetIterator();
                    while (property.Next(true))
                    {
                        if (property.propertyType == SerializedPropertyType.ObjectReference &&
                            property.objectReferenceValue == null && property.objectReferenceInstanceIDValue != 0)
                        {
                            AddIssue(report, path, objectPath, "missing-object-reference:" + property.propertyPath,
                                "error", "Unity reports null object with nonzero serialized instance ID at " + property.propertyPath + "; this is a missing reference, not an optional unassigned field.", LocalObjectKey(component), component.gameObject);
                        }
                    }
                }
            }

            var button = transform.GetComponent<Button>();
            if (button == null) continue;
            string buttonKey = LocalObjectKey(button);
            report.buttonCount++;
            if (button.targetGraphic == null)
                AddIssue(report, path, objectPath, "button-no-target-graphic", "warning", "Standard Button has no targetGraphic. This can still receive clicks but cannot apply its configured graphic transition.", buttonKey, button.gameObject);
            else if (button.targetGraphic.transform != transform && !button.targetGraphic.transform.IsChildOf(transform))
                AddIssue(report, path, objectPath, "button-external-target-graphic", "warning", "Button targetGraphic is outside its own hierarchy.", buttonKey, button.gameObject);
            if (button.onClick.GetPersistentEventCount() > 0)
                AddIssue(report, path, objectPath, "button-persistent-listener", "warning", "Button has Inspector-bound persistent listeners; project convention requires code binding.", buttonKey, button.gameObject);
        }
    }

    private static void AuditYamlReferences(string path, AuditReport report)
    {
        string text = File.ReadAllText(ProjectAbsolutePath(path));
        var defined = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match match in LocalDefinition.Matches(text)) defined.Add(match.Groups[1].Value);
        if (defined.Count == 0)
        {
            AddIssue(report, path, string.Empty, "non-text-prefab", "warning", "Local fileID check requires text-serialized YAML.");
            return;
        }
        foreach (Match match in LocalReference.Matches(text))
        {
            string id = match.Groups[1].Value;
            if (id != "0" && !defined.Contains(id))
                AddIssue(report, path, "fileID:" + id, "missing-local-reference", "error", "YAML references local fileID " + id + " without a matching object definition.");
        }
        var checkedGuids = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match match in AssetReference.Matches(text))
        {
            string guid = match.Groups[1].Value;
            if (!checkedGuids.Add(guid) || guid.StartsWith("0000000000000000", StringComparison.Ordinal)) continue;
            if (string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(guid)))
                AddIssue(report, path, "guid:" + guid, "missing-asset-guid", "error", "Referenced asset GUID cannot be resolved: " + guid);
        }
    }

    private static void AddIssue(AuditReport report, string prefab, string objectPath, string category, string severity, string detail, string stableObjectKey = null, GameObject activityObject = null)
    {
        string key = prefab + "|" + (stableObjectKey ?? objectPath) + "|" + category;
        if (!IssueKeys.Add(key)) return;
        report.issues.Add(new AuditIssue
        {
            key = key, prefab = prefab, objectPath = objectPath, category = category, severity = severity, detail = detail,
            hasPrefabActivity = activityObject != null,
            activeInPrefab = activityObject != null && ActiveInPrefab(activityObject.transform)
        });
    }

    private static bool ActiveInPrefab(Transform transform)
    {
        // Prefab assets are not scene instances, so activeInHierarchy is unsuitable.
        while (transform != null)
        {
            if (!transform.gameObject.activeSelf) return false;
            transform = transform.parent;
        }
        return true;
    }

    private static string LocalObjectKey(Object value)
    {
        if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value, out string guid, out long localId))
            return "object:" + guid + ":" + localId;
        return value.name;
    }

    /// <summary>Queues one isolated render per editor update, so large galleries remain responsive.</summary>
    public static void PreviewAll()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Static previews require Edit mode. Runtime is captured only by capture-runtime.");
        if (previewQueue != null) throw new InvalidOperationException("A preview queue is already active.");
        var manifest = ReadManifest();
        Directory.CreateDirectory(Path.Combine(OutputDirectory, "Previews"));
        previewQueue = new Queue<string>(manifest.prefabs);
        previewReport = new PreviewReport { generatedUtc = UtcNow() };
        State.previewTotal = manifest.prefabs.Length;
        State.previewCompleted = 0;
        State.busy = true;
        State.lastError = string.Empty;
        WriteState();
    }

    public static PreviewEntry PreviewNamed(string prefabName)
    {
        var manifest = ReadManifest();
        foreach (string path in manifest.prefabs)
        {
            if (!string.Equals(Path.GetFileNameWithoutExtension(path), prefabName, StringComparison.Ordinal)) continue;
            string outputPath = Path.Combine(OutputDirectory, "PreviewChecks", SafeName(prefabName) + ".png");
            PreviewEntry entry = PreviewPrefab(path, outputPath);
            WriteJson(Path.Combine(OutputDirectory, "PreviewChecks", SafeName(prefabName) + ".json"), entry);
            State.lastOutput = outputPath;
            return entry;
        }
        throw new ArgumentException("No manifest prefab named " + prefabName);
    }

    private static void TickPreview()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            State.lastError = "Static preview queue cancelled because Play mode started.";
            FinishPreviews();
            return;
        }
        if (previewQueue.Count == 0)
        {
            FinishPreviews();
            return;
        }

        string prefabPath = previewQueue.Dequeue();
        string fileName = (State.previewCompleted + 1).ToString("D2") + "-" + SafeName(Path.GetFileNameWithoutExtension(prefabPath)) + ".png";
        string outputPath = Path.Combine(OutputDirectory, "Previews", fileName);
        PreviewEntry result;
        try { result = PreviewPrefab(prefabPath, outputPath); }
        catch (Exception exception)
        {
            result = new PreviewEntry { prefab = prefabPath, image = outputPath, error = exception.ToString() };
        }
        previewReport.pages.Add(result);
        State.previewCompleted++;
        State.lastOutput = outputPath;
        if (!string.IsNullOrEmpty(result.error)) State.lastError = result.error;
        WriteState();
    }

    private static void FinishPreviews()
    {
        SavePartialPreviewReport();
        previewQueue = null;
        State.busy = false;
        State.lastOutput = Path.Combine(OutputDirectory, "preview-report.json");
        WriteState();
    }

    private static void SavePartialPreviewReport()
    {
        if (previewReport != null) WriteJson(Path.Combine(OutputDirectory, "preview-report.json"), previewReport);
    }

    /// <summary>Renders a disposable prefab instance; never saves or edits its source asset.</summary>
    public static PreviewEntry PreviewPrefab(string prefabPath, string outputPath)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Static preview requires Edit mode.");
        ValidatePrefabPath(prefabPath);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null) throw new FileNotFoundException("Prefab not found", prefabPath);
        var result = new PreviewEntry { prefab = prefabPath, image = outputPath };
        Scene scene = EditorSceneManager.NewPreviewScene();
        RenderTexture target = null;
        Texture2D image = null;
        RenderTexture oldActive = RenderTexture.active;
        try
        {
            var cameraObject = NewPreviewObject("Orchard UI Preview Camera", scene, false);
            var camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.scene = scene;
            camera.cameraType = CameraType.Game;
            camera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(scene);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.71f, 0.89f, 0.95f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = PreviewHeight * 0.5f;
            camera.aspect = (float)PreviewWidth / PreviewHeight;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 2000f;
            camera.transform.position = new Vector3(0, 0, -1000);
            camera.allowHDR = false;
            camera.allowMSAA = false;
            target = new RenderTexture(PreviewWidth, PreviewHeight, 24, RenderTextureFormat.ARGB32) { name = "Orchard UI Preview" };
            target.Create();
            camera.targetTexture = target;

            var canvasObject = NewPreviewObject("Orchard UI Static Preview", scene, true);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1000f;
            canvas.pixelPerfect = false;
            var canvasRect = (RectTransform)canvasObject.transform;
            canvasRect.position = Vector3.zero;
            canvasRect.localScale = Vector3.one;
            canvasRect.sizeDelta = new Vector2(PreviewWidth, PreviewHeight);
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.SetActive(false);
            instance.hideFlags = HideFlags.HideAndDontSave;
            instance.transform.SetParent(canvasObject.transform, false);
            instance.transform.localScale = Vector3.one;
            instance.transform.localRotation = Quaternion.identity;
            if (instance.transform is RectTransform rootRect)
            {
                rootRect.anchorMin = Vector2.zero;
                rootRect.anchorMax = Vector2.one;
                rootRect.offsetMin = Vector2.zero;
                rootRect.offsetMax = Vector2.zero;
                rootRect.anchoredPosition3D = Vector3.zero;
            }

            PreparePreviewInstance(instance, camera, result);
            instance.SetActive(true);
            Canvas.ForceUpdateCanvases();
            foreach (var rect in instance.GetComponentsInChildren<RectTransform>(true))
                if (rect.gameObject.activeInHierarchy && rect.GetComponent<LayoutGroup>() != null)
                    LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
            foreach (var scroll in instance.GetComponentsInChildren<ScrollRect>(true))
                if (scroll.isActiveAndEnabled && scroll.vertical) scroll.verticalNormalizedPosition = 1f;
            foreach (var text in instance.GetComponentsInChildren<TMP_Text>(true))
                if (text.gameObject.activeInHierarchy) text.ForceMeshUpdate(true, true);
            int activeGraphics = 0;
            foreach (var graphic in instance.GetComponentsInChildren<Graphic>(true))
            {
                if (!graphic.isActiveAndEnabled) continue;
                activeGraphics++;
                graphic.SetAllDirty();
                graphic.Rebuild(CanvasUpdate.PreRender);
            }
            Canvas.ForceUpdateCanvases();
            result.notes.Add("Render diagnostics: activeGraphics=" + activeGraphics + ", canvasRect=" + canvasRect.rect + ", cameraScene=" + camera.scene.name + ", cullingMask=" + camera.overrideSceneCullingMask);
            camera.Render();
            RenderTexture.active = target;
            image = new Texture2D(PreviewWidth, PreviewHeight, TextureFormat.RGBA32, false);
            image.ReadPixels(new Rect(0, 0, PreviewWidth, PreviewHeight), 0, 0);
            image.Apply(false, false);
            result.activeGraphics = activeGraphics;
            var pixels = image.GetRawTextureData<Color32>();
            Color32 firstPixel = pixels[0];
            for (int pixelIndex = 0; pixelIndex < pixels.Length; pixelIndex += 503)
            {
                Color32 pixel = pixels[pixelIndex];
                if (Math.Abs(pixel.r - firstPixel.r) > 2 || Math.Abs(pixel.g - firstPixel.g) > 2 || Math.Abs(pixel.b - firstPixel.b) > 2)
                    result.nonUniformPixelSamples++;
            }
            if (activeGraphics > 2 && result.nonUniformPixelSamples == 0)
                result.error = "Static render is uniformly colored despite multiple active Graphics. Treat this as a failed preview, not visual verification.";
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath)));
            File.WriteAllBytes(outputPath, image.EncodeToPNG());
            result.notes.Add("Serialized text and imagery only. Runtime data binding, localization changes, gameplay state and page transitions are not exercised.");
            return result;
        }
        finally
        {
            RenderTexture.active = oldActive;
            if (image != null) Object.DestroyImmediate(image);
            if (target != null)
            {
                target.Release();
                Object.DestroyImmediate(target);
            }
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    private static void PreparePreviewInstance(GameObject instance, Camera camera, PreviewEntry result)
    {
        foreach (var behaviour in instance.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (behaviour == null || IsVisualBehaviour(behaviour)) continue;
            if (behaviour.enabled) result.disabledBusinessBehaviours++;
            behaviour.enabled = false;
        }
        // The production component loads this Sprite asynchronously through Resources.
        // Business behaviours are disabled in static previews, so bind the same asset
        // only on this disposable preview instance. No asset or runtime logic is changed.
        foreach (var backdrop in instance.GetComponentsInChildren<OrchardBackdrop>(true))
        {
            var targetImage = backdrop.GetComponent<Image>();
            var backdropSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/OrchardUI/Resources/OrchardUI/Backdrop.png");
            if (targetImage == null || backdropSprite == null)
            {
                result.notes.Add("Preview-only backdrop binding unavailable: " + HierarchyPath(backdrop.transform, instance.transform));
                continue;
            }
            targetImage.sprite = backdropSprite;
            targetImage.color = Color.white;
            targetImage.enabled = true;
            result.notes.Add("PREVIEW ONLY: bound Backdrop.png to disabled OrchardBackdrop's Image on the disposable clone. Runtime Resources loading is not exercised.");
        }
        foreach (var animation in instance.GetComponentsInChildren<Animation>(true)) { animation.Stop(); animation.enabled = false; }
        foreach (var animator in instance.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
        foreach (var particle in instance.GetComponentsInChildren<ParticleSystem>(true)) particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        foreach (var audio in instance.GetComponentsInChildren<AudioSource>(true)) audio.enabled = false;
        foreach (var childCamera in instance.GetComponentsInChildren<Camera>(true)) childCamera.enabled = false;
        foreach (var canvas in instance.GetComponentsInChildren<Canvas>(true))
        {
            canvas.worldCamera = camera;
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.planeDistance = 1000f;
        }
        foreach (var group in instance.GetComponentsInChildren<CanvasGroup>(true))
        {
            if (group.alpha <= 0.001f)
            {
                group.alpha = 1f;
                result.revealedContainers.Add(HierarchyPath(group.transform, instance.transform) + " (CanvasGroup alpha)");
            }
        }
        foreach (Transform transform in instance.GetComponentsInChildren<Transform>(true))
        {
            if (transform == instance.transform || transform.gameObject.activeSelf) continue;
            if (Depth(transform, instance.transform) > 3 || !IsContentContainer(transform.name)) continue;
            transform.gameObject.SetActive(true);
            result.revealedContainers.Add(HierarchyPath(transform, instance.transform) + " (inactive content container)");
        }
    }

    private static bool IsVisualBehaviour(MonoBehaviour behaviour)
    {
        return behaviour is Graphic || behaviour is BaseMeshEffect || behaviour is LayoutGroup ||
               behaviour is ContentSizeFitter || behaviour is AspectRatioFitter || behaviour is CanvasScaler ||
               behaviour is Mask || behaviour is RectMask2D || behaviour is ScrollRect ||
               behaviour is Selectable || behaviour is TMP_SubMeshUI;
    }

    private static bool IsContentContainer(string name)
    {
        return string.Equals(name, "Content", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(name, "Root", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(name, "Main", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(name, "Panel", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(name, "Window", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(name, "Body", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(name, "PageRoot", StringComparison.OrdinalIgnoreCase);
    }

    private static GameObject NewPreviewObject(string name, Scene scene, bool rectTransform)
    {
        var gameObject = rectTransform ? new GameObject(name, typeof(RectTransform)) : new GameObject(name);
        gameObject.hideFlags = HideFlags.HideAndDontSave;
        SceneManager.MoveGameObjectToScene(gameObject, scene);
        return gameObject;
    }

    public static string CaptureRuntime()
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("capture-runtime requires an already running game. This tool does not start Play mode.");
        if (!string.IsNullOrEmpty(pendingCapture)) throw new InvalidOperationException("A runtime screenshot is still pending.");
        string folder = Path.Combine(OutputDirectory, "RuntimeCaptures");
        Directory.CreateDirectory(folder);
        string capturePath = Path.Combine(folder, "runtime-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + ".png");
        CaptureRuntimeSnapshot(Path.ChangeExtension(capturePath, ".json"), capturePath);
        pendingCapture = capturePath;
        captureDeadline = EditorApplication.timeSinceStartup + 30;
        ScreenCapture.CaptureScreenshot(pendingCapture);
        State.lastOutput = "Requested current Game view capture: " + pendingCapture;
        WriteState();
        return pendingCapture;
    }

    public static RuntimeSnapshot CaptureRuntimeSnapshot(string reportPath, string screenshotPath = "")
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Runtime snapshots require an already running game.");
        var report = new RuntimeSnapshot
        {
            generatedUtc = UtcNow(), screenshot = screenshotPath,
            activeScene = SceneManager.GetActiveScene().path,
            screenWidth = Screen.width, screenHeight = Screen.height
        };
        foreach (var image in Object.FindObjectsOfType<Image>(true))
        {
            if (!image.gameObject.activeInHierarchy) continue;
            Canvas canvas = image.GetComponentInParent<Canvas>();
            if (canvas == null) continue;
            Sprite sprite = image.overrideSprite;
            report.images.Add(new RuntimeImage
            {
                path = HierarchyPath(image.transform, null), canvasPath = HierarchyPath(canvas.transform, null),
                spriteName = sprite != null ? sprite.name : string.Empty,
                assetPath = sprite != null ? AssetDatabase.GetAssetPath(sprite) : string.Empty,
                sourceSpriteName = image.sprite != null ? image.sprite.name : string.Empty,
                color = image.color, rect = DescribeRuntimeRect(image.rectTransform, canvas),
                raycast = image.raycastTarget, enabled = image.enabled,
                rendererCulled = image.canvasRenderer.cull, rendererAlpha = image.canvasRenderer.GetAlpha()
            });
        }
        foreach (var text in Object.FindObjectsOfType<TMP_Text>(true))
        {
            if (!text.gameObject.activeInHierarchy) continue;
            Canvas canvas = text.GetComponentInParent<Canvas>();
            if (canvas == null) continue;
            report.texts.Add(new RuntimeText
            {
                path = HierarchyPath(text.transform, null), text = text.text,
                fontName = text.font != null ? text.font.name : string.Empty,
                fontAssetPath = text.font != null ? AssetDatabase.GetAssetPath(text.font) : string.Empty,
                color = text.color, fontSize = text.fontSize, enabled = text.enabled,
                raycast = text.raycastTarget, rect = DescribeRuntimeRect(text.rectTransform, canvas)
            });
        }
        foreach (var button in Object.FindObjectsOfType<Button>(true))
        {
            if (!button.gameObject.activeInHierarchy) continue;
            Canvas canvas = button.GetComponentInParent<Canvas>();
            if (canvas == null) continue;
            int raycastGraphics = 0;
            foreach (var graphic in button.GetComponentsInChildren<Graphic>())
                if (graphic.isActiveAndEnabled && graphic.raycastTarget) raycastGraphics++;
            report.buttons.Add(new RuntimeButton
            {
                path = HierarchyPath(button.transform, null),
                targetGraphicPath = button.targetGraphic != null ? HierarchyPath(button.targetGraphic.transform, null) : string.Empty,
                enabled = button.enabled, interactable = button.IsInteractable(), activeRaycastGraphicCount = raycastGraphics,
                clickable = button.isActiveAndEnabled && button.IsInteractable() && raycastGraphics > 0 && AncestorsAllowRaycasts(button.transform),
                rect = DescribeRuntimeRect(button.transform as RectTransform, canvas)
            });
        }
        report.images.Sort((left, right) => string.CompareOrdinal(left.path, right.path));
        report.texts.Sort((left, right) => string.CompareOrdinal(left.path, right.path));
        report.buttons.Sort((left, right) => string.CompareOrdinal(left.path, right.path));
        WriteJson(reportPath, report);
        return report;
    }

    private static RuntimeRect DescribeRuntimeRect(RectTransform transform, Canvas canvas)
    {
        if (transform == null) return null;
        var corners = new Vector3[4];
        transform.GetWorldCorners(corners);
        Canvas rootCanvas = canvas.rootCanvas;
        Camera camera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
        Vector2 min = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
        Vector2 max = min;
        for (int index = 1; index < corners.Length; index++)
        {
            Vector2 point = RectTransformUtility.WorldToScreenPoint(camera, corners[index]);
            min = Vector2.Min(min, point);
            max = Vector2.Max(max, point);
        }
        return new RuntimeRect
        {
            localPosition = transform.localPosition, anchoredPosition = transform.anchoredPosition,
            size = transform.rect.size, sizeDelta = transform.sizeDelta,
            anchorMin = transform.anchorMin, anchorMax = transform.anchorMax, pivot = transform.pivot,
            screenRect = Rect.MinMaxRect(min.x, min.y, max.x, max.y)
        };
    }

    private static bool AncestorsAllowRaycasts(Transform transform)
    {
        while (transform != null)
        {
            bool stopAtThisLevel = false;
            foreach (var group in transform.GetComponents<CanvasGroup>())
            {
                if (!group.enabled) continue;
                if (!group.blocksRaycasts) return false;
                if (group.ignoreParentGroups) stopAtThisLevel = true;
            }
            if (stopAtThisLevel) return true;
            transform = transform.parent;
        }
        return true;
    }

    public static void CloseRuntimePage(string pageId)
    {
        if (!IsAllowedRuntimePage(pageId)) throw new ArgumentException("Runtime page is not allowlisted: " + pageId);
        if (!EditorApplication.isPlaying || !HarvestBridge.Ready || UIModule.Instance == null)
            throw new InvalidOperationException("Runtime page closing requires the initialized formal InitWZ runtime.");
        UIModule.Instance.ClosePage(new PageId(pageId));
        State.lastOutput = "Requested normal UIModule.ClosePage: " + pageId + ". Wait for its normal close animation before capture-runtime.";
        WriteState();
    }

    /// <summary>
    /// Opens only an explicit allowlisted page through the initialized production UI
    /// module. It never clicks a withdrawal/ad/send action or edits the saved profile.
    /// Normal page initialization and its normal read requests remain unchanged.
    /// </summary>
    public static async UniTask<UIPageBase> OpenRuntimePage(string pageId)
    {
        if (!IsAllowedRuntimePage(pageId)) throw new ArgumentException("Runtime page is not allowlisted: " + pageId);
        if (!EditorApplication.isPlaying || !HarvestBridge.Ready)
            throw new InvalidOperationException("Runtime page opening requires Play mode and HarvestBridge.Ready from the formal InitWZ flow.");
        if (UIModule.Instance == null) throw new InvalidOperationException("Production UIModule is not initialized.");
        if (State.runtimePageOpening) throw new InvalidOperationException("A runtime page is already opening.");
        State.runtimePageOpening = true;
        State.busy = true;
        State.lastError = string.Empty;
        WriteState();
        try
        {
            UIPageBase page = await UIModule.Instance.OpenPage(new PageId(pageId));
            if (page == null) throw new InvalidOperationException("Production UIModule could not open " + pageId);
            State.lastOutput = "Opened runtime page through UIModule: " + pageId + ". Wait for normal loading/animation before capture-runtime.";
            return page;
        }
        finally
        {
            State.runtimePageOpening = false;
            State.busy = previewQueue != null;
            WriteState();
        }
    }

    private static bool IsAllowedRuntimePage(string pageId)
    {
        return pageId == "RealWithdrawPanel" || pageId == "FakeWithdrawPanel" ||
               pageId == "PausePanel" || pageId == "FAQPanel" ||
               pageId == "WithdrawHistory" || pageId == "ServicePanel";
    }

    private static void LogRuntimeOpenFailure(Exception exception)
    {
        State.lastError = exception.ToString();
        WriteState();
        Debug.LogException(exception);
    }

    private static PrefabManifest ReadManifest()
    {
        if (!File.Exists(ManifestPath)) throw new FileNotFoundException("Create prefab-manifest.json with {\"version\":1,\"prefabs\":[\"Assets/...prefab\"]} first.", ManifestPath);
        var manifest = JsonUtility.FromJson<PrefabManifest>(File.ReadAllText(ManifestPath));
        if (manifest == null || manifest.prefabs == null || manifest.prefabs.Length == 0)
            throw new InvalidDataException("Prefab manifest contains no prefabs.");
        var unique = new HashSet<string>(StringComparer.Ordinal);
        foreach (string path in manifest.prefabs)
        {
            ValidatePrefabPath(path);
            if (!unique.Add(path)) throw new InvalidDataException("Duplicate prefab manifest entry: " + path);
        }
        return manifest;
    }

    private static void ValidatePrefabPath(string path)
    {
        if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/", StringComparison.Ordinal) ||
            !path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) || path.Contains("..") || path.Contains("\\"))
            throw new InvalidDataException("Expected a project-local Assets/...prefab path: " + path);
    }

    private static string ProjectAbsolutePath(string path)
    {
        return Path.GetFullPath(Path.Combine(Application.dataPath, "..", path));
    }

    private static int Depth(Transform transform, Transform root)
    {
        int depth = 0;
        while (transform != null && transform != root) { depth++; transform = transform.parent; }
        return depth;
    }

    private static string HierarchyPath(Transform transform, Transform root)
    {
        string result = transform.name + "[" + transform.GetSiblingIndex() + "]";
        while (transform != root && transform.parent != null)
        {
            transform = transform.parent;
            result = transform.name + "[" + transform.GetSiblingIndex() + "]/" + result;
        }
        return result;
    }

    private static string SafeName(string value)
    {
        var builder = new StringBuilder();
        foreach (char character in value)
            builder.Append(char.IsLetterOrDigit(character) || character == '-' || character == '_' ? character : '-');
        return builder.Length == 0 ? "current" : builder.ToString();
    }

    private static string UtcNow() => DateTime.UtcNow.ToString("o");

    private static void WriteJson(string path, object value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, JsonUtility.ToJson(value, true), Utf8);
    }

    private static void WriteState()
    {
        try
        {
            State.updatedUtc = UtcNow();
            State.playing = EditorApplication.isPlaying;
            State.compiling = EditorApplication.isCompiling;
            State.updating = EditorApplication.isUpdating;
            State.runtimeCapturePending = !string.IsNullOrEmpty(pendingCapture);
            WriteJson(Path.Combine(OutputDirectory, "state.json"), State);
        }
        catch (IOException) { /* A reader may hold the report briefly; retry next tick. */ }
    }
}
