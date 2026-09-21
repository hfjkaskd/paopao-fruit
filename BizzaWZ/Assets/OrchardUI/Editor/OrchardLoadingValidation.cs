using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>
/// Explicit, disposable loading-prefab previews and read-only startup observation.
/// This editor tool never saves assets, enters Play mode, publishes loading events,
/// or changes initialization, accounts, saved data, or runtime resource loading.
/// </summary>
[InitializeOnLoad]
public static class OrchardLoadingValidation
{
    private const string PrefabPath = "Assets/BizzaWZ/Common/MenuSystem/Common/LoadingPanel/LoadingPanel.prefab";
    private const string WatchKey = "OrchardLoadingValidation.WatchStartup";
    private static readonly System.Text.UTF8Encoding Utf8 = new System.Text.UTF8Encoding(false);
    private static string OutputDirectory => Path.GetFullPath(Path.Combine(Application.dataPath, "../Design/OrchardLoading"));

    [Serializable]
    private sealed class Report
    {
        public string generatedUtc = DateTime.UtcNow.ToString("o");
        public string kind;
        public string status;
        public string prefab = PrefabPath;
        public List<string> errors = new List<string>();
        public List<string> notes = new List<string>();
        public List<Frame> frames = new List<Frame>();
    }

    [Serializable]
    private sealed class Frame
    {
        public string capturedUtc;
        public string image;
        public string detailImage;
        public int width;
        public int height;
        public float progress;
        public float visibleAlpha;
        public bool resourcesBound;
        public int activeGraphics;
        public int disabledBusinessBehaviours;
        public int nonUniformSamples;
        public string activeScene;
        public string error;
    }

    [Serializable]
    private sealed class BridgeState
    {
        public string updatedUtc;
        public bool playing;
        public bool compiling;
        public bool previewing;
        public bool watchingStartup;
        public int completedFrames;
        public string lastCommand;
        public string lastOutput;
        public string lastError;
    }

    private static readonly BridgeState State = new BridgeState();
    private static Queue<Frame> previewQueue;
    private static Report previewReport;
    private static Report startupReport;
    private static bool sawLoading;
    private static double watchStarted;
    private static double lastLoadingSeen;
    private static double nextObservation;
    private static double nextCommandCheck;
    private static double nextStateWrite;
    private static string pendingScreenshot;
    private static float lastCapturedProgress = -1f;
    private static bool capturedBound;

    static OrchardLoadingValidation()
    {
        EditorApplication.update += Tick;
        AssemblyReloadEvents.beforeAssemblyReload += BeforeReload;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnPlayModeChanged(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.ExitingPlayMode && startupReport != null && watchStarted > 0 && SessionState.GetBool(WatchKey, false))
            FinishStartup("Play mode ended");
    }

    private static void BeforeReload()
    {
        if (previewQueue != null)
        {
            previewReport.status = "Interrupted by script reload; rerun preview after compilation.";
            WriteReport("preview-report.json", previewReport);
        }
        if (startupReport != null) WriteReport("startup-report.json", startupReport);
    }

    private static void Tick()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        double now = EditorApplication.timeSinceStartup;
        try
        {
            if (previewQueue != null) TickPreview();
            else if (now >= nextCommandCheck)
            {
                nextCommandCheck = now + 0.25;
                ReadCommand();
            }
            if (SessionState.GetBool(WatchKey, false) && now >= nextObservation)
            {
                nextObservation = now + 0.1;
                ObserveStartup();
            }
        }
        catch (Exception exception)
        {
            State.lastError = exception.ToString();
            if (previewReport != null && previewQueue != null)
            {
                previewReport.errors.Add(exception.Message);
                FinishPreview("Failed");
            }
            if (startupReport != null && SessionState.GetBool(WatchKey, false))
            {
                startupReport.errors.Add(exception.Message);
                FinishStartup("Observation failed");
            }
            Debug.LogException(exception);
        }
        if (now >= nextStateWrite)
        {
            nextStateWrite = now + 1;
            WriteState();
        }
    }

    private static void ReadCommand()
    {
        string path = Path.Combine(OutputDirectory, "validation.command");
        if (!File.Exists(path)) return;
        string command;
        try { command = File.ReadAllText(path).Trim(); File.Delete(path); }
        catch (IOException) { return; }
        State.lastCommand = command;
        State.lastError = string.Empty;
        if (command == "preview") Preview();
        else if (command == "watch-startup") WatchStartup();
        else throw new ArgumentException("Expected preview or watch-startup, received: " + command);
        WriteState();
    }

    [MenuItem("Tools/Orchard UI/Preview Loading Artwork")]
    public static void Preview()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Disposable prefab previews require Edit mode.");
        if (previewQueue != null) throw new InvalidOperationException("A loading preview is already running.");
        previewReport = new Report
        {
            kind = "STATIC PREFAB PREVIEW: isolated clones; business scripts disabled; serialized Resources sprite bindings applied only to clones.",
            status = "Rendering"
        };
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null) throw new FileNotFoundException("Loading prefab unavailable", PrefabPath);
        AuditPrefab(prefab, previewReport);
        previewQueue = new Queue<Frame>();
        int[,] sizes = { { 941, 1672 }, { 1080, 2400 }, { 1536, 2048 } };
        float[] progressSteps = { 0f, 0.01f, 0.35f, 0.5f, 1f };
        for (int size = 0; size < sizes.GetLength(0); size++)
            for (int step = 0; step < progressSteps.Length; step++)
                previewQueue.Enqueue(new Frame
                {
                    width = sizes[size, 0], height = sizes[size, 1], progress = progressSteps[step],
                    image = Path.Combine(OutputDirectory, "Previews", sizes[size, 0] + "x" + sizes[size, 1] + "-progress-" + Mathf.RoundToInt(progressSteps[step] * 100f).ToString("D3") + ".png")
                });
        State.completedFrames = 0;
        State.lastOutput = "Queued 15 isolated rounded loading previews.";
    }

    private static void AuditPrefab(GameObject prefab, Report report)
    {
        var panel = prefab.GetComponent<LoadingPanel>();
        if (panel == null) report.errors.Add("Root has no LoadingPanel component.");
        else if (panel.progressBar == null) report.errors.Add("LoadingPanel.progressBar is unassigned.");
        else if (panel.progressBar.type != Image.Type.Sliced || panel.loadingArtwork == null)
            report.errors.Add("LoadingPanel.progressBar must use a Sliced Image and the rounded loading artwork controller.");
        else if (!ActiveInPrefab(panel.progressBar.transform)) report.errors.Add("LoadingPanel.progressBar is beneath an inactive object.");

        if (prefab.GetComponentsInChildren<OrchardLoadingArt>(true).Length != 1)
            report.errors.Add("Expected exactly one OrchardLoadingArt component.");
        foreach (Transform transform in prefab.GetComponentsInChildren<Transform>(true))
        {
            if (!ActiveInPrefab(transform)) continue;
            if (string.Equals(transform.name, "Logo", StringComparison.OrdinalIgnoreCase))
                report.errors.Add("Legacy Logo object is still active: " + ObjectPath(transform));
            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) != 0)
                report.errors.Add("Missing script on active object: " + ObjectPath(transform));
            foreach (Component component in transform.GetComponents<Component>())
            {
                if (component == null) continue;
                using (var serialized = new SerializedObject(component))
                {
                    var property = serialized.GetIterator();
                    while (property.Next(true))
                        if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null && property.objectReferenceInstanceIDValue != 0)
                            report.errors.Add("Missing active reference: " + ObjectPath(transform) + "/" + property.propertyPath);
                }
            }
        }
        report.notes.Add("Only genuinely missing references (nonzero serialized ID resolving null) count as missing; optional unassigned fields do not.");
        report.notes.Add("0%, 1%, 35%, 50%, and 100% are visual test states on disposable clones, not injected runtime loading events.");
    }

    private static void TickPreview()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) { FinishPreview("Cancelled: Play mode started"); return; }
        if (previewQueue.Count == 0) { FinishPreview(previewReport.errors.Count == 0 ? "Complete" : "Complete with validation errors"); return; }
        Frame frame = previewQueue.Dequeue();
        try { RenderFrame(frame); }
        catch (Exception exception)
        {
            frame.error = exception.ToString();
            previewReport.errors.Add(frame.width + "x" + frame.height + ": " + exception.Message);
        }
        previewReport.frames.Add(frame);
        State.completedFrames = previewReport.frames.Count;
        State.lastOutput = frame.image;
        WriteReport("preview-report.json", previewReport);
    }

    private static void FinishPreview(string status)
    {
        previewReport.status = status;
        WriteReport("preview-report.json", previewReport);
        previewQueue = null;
        State.lastOutput = Path.Combine(OutputDirectory, "preview-report.json");
    }

    private static void RenderFrame(Frame frame)
    {
        Scene scene = EditorSceneManager.NewPreviewScene();
        RenderTexture target = null;
        Texture2D image = null;
        RenderTexture oldActive = RenderTexture.active;
        try
        {
            var cameraObject = NewObject("Loading Preview Camera", scene, false);
            var camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.scene = scene;
            camera.cameraType = CameraType.Game;
            camera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(scene);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.magenta;
            camera.orthographic = true;
            camera.orthographicSize = frame.height * 0.5f;
            camera.aspect = (float)frame.width / frame.height;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 2000f;
            camera.transform.position = new Vector3(0, 0, -1000);
            camera.allowHDR = false;
            camera.allowMSAA = false;
            target = new RenderTexture(frame.width, frame.height, 24, RenderTextureFormat.ARGB32);
            target.Create();
            camera.targetTexture = target;

            var canvasObject = NewObject("Disposable Loading Preview", scene, true);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1000f;
            var canvasRect = (RectTransform)canvasObject.transform;
            canvasRect.sizeDelta = new Vector2(frame.width, frame.height);

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), scene);
            instance.SetActive(false);
            instance.hideFlags = HideFlags.HideAndDontSave;
            instance.transform.SetParent(canvasObject.transform, false);
            instance.transform.localScale = Vector3.one;
            instance.transform.localRotation = Quaternion.identity;
            var root = (RectTransform)instance.transform;
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
            root.anchoredPosition3D = Vector3.zero;
            foreach (var behaviour in instance.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour == null || IsVisual(behaviour)) continue;
                if (behaviour.enabled) frame.disabledBusinessBehaviours++;
                behaviour.enabled = false;
            }
            foreach (var animation in instance.GetComponentsInChildren<Animation>(true)) { animation.Stop(); animation.enabled = false; }
            foreach (var animator in instance.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
            foreach (var audio in instance.GetComponentsInChildren<AudioSource>(true)) audio.enabled = false;
            foreach (var particle in instance.GetComponentsInChildren<ParticleSystem>(true)) particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            foreach (var childCamera in instance.GetComponentsInChildren<Camera>(true)) childCamera.enabled = false;
            foreach (var childCanvas in instance.GetComponentsInChildren<Canvas>(true))
            {
                childCanvas.renderMode = RenderMode.WorldSpace;
                childCanvas.worldCamera = camera;
            }
            BindPreviewArtwork(instance);
            var panel = instance.GetComponent<LoadingPanel>();
            if (panel == null || panel.progressBar == null) throw new InvalidOperationException("No authored progressBar to render.");
            panel.progressBar.fillAmount = frame.progress;
            panel.loadingArtwork.SetProgress(frame.progress);
            instance.SetActive(true);
            Canvas.ForceUpdateCanvases();
            foreach (var rect in instance.GetComponentsInChildren<RectTransform>(true))
                if (rect.gameObject.activeInHierarchy && rect.GetComponent<LayoutGroup>() != null) LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
            foreach (var text in instance.GetComponentsInChildren<TMP_Text>(true))
                if (text.gameObject.activeInHierarchy) text.ForceMeshUpdate(true, true);
            foreach (var graphic in instance.GetComponentsInChildren<Graphic>(true))
            {
                if (!graphic.isActiveAndEnabled) continue;
                frame.activeGraphics++;
                graphic.SetAllDirty();
                graphic.Rebuild(CanvasUpdate.PreRender);
            }
            Canvas.ForceUpdateCanvases();
            panel.loadingArtwork.SetProgress(frame.progress);
            Canvas.ForceUpdateCanvases();
            frame.resourcesBound = InspectBindings(instance, out frame.visibleAlpha);
            camera.Render();
            RenderTexture.active = target;
            image = new Texture2D(frame.width, frame.height, TextureFormat.RGBA32, false);
            image.ReadPixels(new Rect(0, 0, frame.width, frame.height), 0, 0);
            image.Apply(false, false);
            var pixels = image.GetRawTextureData<Color32>();
            Color32 first = pixels[0];
            for (int index = 0; index < pixels.Length; index += 503)
                if (Math.Abs(pixels[index].r - first.r) > 2 || Math.Abs(pixels[index].g - first.g) > 2 || Math.Abs(pixels[index].b - first.b) > 2) frame.nonUniformSamples++;
            if (frame.nonUniformSamples == 0) throw new InvalidOperationException("Uniform render; preview cannot establish visual correctness.");
            Directory.CreateDirectory(Path.GetDirectoryName(frame.image));
            File.WriteAllBytes(frame.image, image.EncodeToPNG());
            CaptureProgressDetail(panel.loadingArtwork, camera, target, frame);
            frame.capturedUtc = DateTime.UtcNow.ToString("o");
        }
        finally
        {
            RenderTexture.active = oldActive;
            if (image != null) Object.DestroyImmediate(image);
            if (target != null) { target.Release(); Object.DestroyImmediate(target); }
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    private static void CaptureProgressDetail(OrchardLoadingArt art, Camera camera, RenderTexture target, Frame frame)
    {
        RectTransform track;
        using (var serialized = new SerializedObject(art))
            track = serialized.FindProperty("progressTrack").objectReferenceValue as RectTransform;
        if (track == null) throw new InvalidOperationException("Artwork progressTrack is unassigned.");

        var corners = new Vector3[4];
        track.GetWorldCorners(corners);
        Vector2 min = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
        Vector2 max = min;
        for (int index = 1; index < corners.Length; index++)
        {
            Vector2 point = RectTransformUtility.WorldToScreenPoint(camera, corners[index]);
            min = Vector2.Min(min, point);
            max = Vector2.Max(max, point);
        }
        float height = max.y - min.y;
        Rect pixels = camera.pixelRect;
        int left = Mathf.Clamp(Mathf.FloorToInt(min.x - height * 0.8f), Mathf.CeilToInt(pixels.xMin), Mathf.Min(target.width, Mathf.FloorToInt(pixels.xMax)));
        int right = Mathf.Clamp(Mathf.CeilToInt(max.x + height * 0.8f), left, Mathf.Min(target.width, Mathf.FloorToInt(pixels.xMax)));
        int bottom = Mathf.Clamp(Mathf.FloorToInt(min.y - height * 2.2f), Mathf.CeilToInt(pixels.yMin), Mathf.Min(target.height, Mathf.FloorToInt(pixels.yMax)));
        int top = Mathf.Clamp(Mathf.CeilToInt(max.y + height * 0.7f), bottom, Mathf.Min(target.height, Mathf.FloorToInt(pixels.yMax)));
        if (right <= left || top <= bottom) throw new InvalidOperationException("Progress detail falls outside the preview render target.");

        Texture2D detail = null;
        RenderTexture oldActive = RenderTexture.active;
        try
        {
            RenderTexture.active = target;
            detail = new Texture2D(right - left, top - bottom, TextureFormat.RGBA32, false);
            detail.ReadPixels(new Rect(left, bottom, right - left, top - bottom), 0, 0);
            detail.Apply(false, false);
            frame.detailImage = Path.Combine(Path.GetDirectoryName(frame.image), Path.GetFileNameWithoutExtension(frame.image) + "-detail.png");
            File.WriteAllBytes(frame.detailImage, detail.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = oldActive;
            if (detail != null) Object.DestroyImmediate(detail);
        }
    }

    private static void BindPreviewArtwork(GameObject instance)
    {
        foreach (var art in instance.GetComponentsInChildren<OrchardLoadingArt>(true))
        {
            using (var serialized = new SerializedObject(art))
            {
                string resourcePath = serialized.FindProperty("resourcePath").stringValue;
                Sprite[] sprites = Resources.LoadAll<Sprite>(resourcePath);
                var bindings = serialized.FindProperty("bindings");
                if (bindings.arraySize == 0) throw new InvalidOperationException("Artwork has no sprite bindings.");
                for (int index = 0; index < bindings.arraySize; index++)
                {
                    var binding = bindings.GetArrayElementAtIndex(index);
                    var target = binding.FindPropertyRelative("target").objectReferenceValue as Image;
                    string name = binding.FindPropertyRelative("spriteName").stringValue;
                    Sprite resolved = null;
                    foreach (var sprite in sprites) if (sprite.name == name) { resolved = sprite; break; }
                    if (target == null || resolved == null) throw new InvalidOperationException("Cannot resolve artwork binding: " + resourcePath + "/" + name);
                    target.sprite = resolved;
                }
                var group = serialized.FindProperty("visualGroup").objectReferenceValue as CanvasGroup;
                if (group == null) throw new InvalidOperationException("Artwork visualGroup is unassigned.");
                group.alpha = 1;
            }
        }
    }

    private static bool InspectBindings(GameObject instance, out float alpha)
    {
        alpha = 0;
        var art = instance.GetComponentInChildren<OrchardLoadingArt>(true);
        if (art == null) return false;
        using (var serialized = new SerializedObject(art))
        {
            var group = serialized.FindProperty("visualGroup").objectReferenceValue as CanvasGroup;
            alpha = group != null ? group.alpha : 0;
            var bindings = serialized.FindProperty("bindings");
            if (bindings.arraySize == 0) return false;
            for (int index = 0; index < bindings.arraySize; index++)
            {
                var binding = bindings.GetArrayElementAtIndex(index);
                var target = binding.FindPropertyRelative("target").objectReferenceValue as Image;
                string name = binding.FindPropertyRelative("spriteName").stringValue;
                if (target == null || target.sprite == null || target.sprite.name != name) return false;
            }
        }
        return true;
    }

    [MenuItem("Tools/Orchard UI/Observe Next Loading Startup")]
    public static void WatchStartup()
    {
        if (previewQueue != null) throw new InvalidOperationException("Wait until previews finish before observing startup.");
        SessionState.SetBool(WatchKey, true);
        ResetObservation();
        State.lastOutput = "Armed read-only observation. Start the formal InitWZ scene normally; this tool does not enter Play mode.";
    }

    private static void ResetObservation()
    {
        startupReport = new Report
        {
            kind = "READ-ONLY STARTUP OBSERVATION: actual active LoadingPanel progress; screenshots requested at next frame end; no loading events or account state changes.",
            status = "Waiting for normal Play mode startup"
        };
        sawLoading = false;
        watchStarted = 0;
        lastLoadingSeen = 0;
        lastCapturedProgress = -1;
        capturedBound = false;
        pendingScreenshot = null;
        WriteReport("startup-report.json", startupReport);
    }

    private static void ObserveStartup()
    {
        if (startupReport == null) ResetObservation();
        if (!EditorApplication.isPlaying)
        {
            if (watchStarted > 0) FinishStartup("Play mode ended");
            return;
        }
        double now = EditorApplication.timeSinceStartup;
        if (watchStarted == 0) { watchStarted = now; startupReport.status = "Observing formal startup"; }
        LoadingPanel activePanel = null;
        foreach (var panel in Resources.FindObjectsOfTypeAll<LoadingPanel>())
        {
            if (!panel.gameObject.scene.IsValid() || !panel.gameObject.activeInHierarchy || EditorUtility.IsPersistent(panel)) continue;
            activePanel = panel;
            break;
        }
        if (activePanel != null)
        {
            sawLoading = true;
            lastLoadingSeen = now;
            bool bound = InspectBindings(activePanel.gameObject, out float alpha);
            float progress = activePanel.progressBar != null ? activePanel.progressBar.fillAmount : -1;
            bool captureDue = startupReport.frames.Count == 0 || bound && !capturedBound || Mathf.Abs(progress - lastCapturedProgress) >= 0.1f || progress >= 0.999f && lastCapturedProgress < 0.999f;
            if (captureDue && startupReport.frames.Count < 12 && (pendingScreenshot == null || File.Exists(pendingScreenshot)))
            {
                string folder = Path.Combine(OutputDirectory, "Startup");
                Directory.CreateDirectory(folder);
                pendingScreenshot = Path.Combine(folder, "loading-" + startupReport.frames.Count.ToString("D2") + "-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + ".png");
                var frame = new Frame
                {
                    capturedUtc = DateTime.UtcNow.ToString("o"), image = pendingScreenshot,
                    width = Screen.width, height = Screen.height, progress = progress,
                    visibleAlpha = alpha, resourcesBound = bound, activeScene = SceneManager.GetActiveScene().path
                };
                foreach (var graphic in activePanel.GetComponentsInChildren<Graphic>())
                    if (graphic.isActiveAndEnabled) frame.activeGraphics++;
                startupReport.frames.Add(frame);
                lastCapturedProgress = progress;
                capturedBound |= bound;
                ScreenCapture.CaptureScreenshot(pendingScreenshot);
                WriteReport("startup-report.json", startupReport);
                State.lastOutput = pendingScreenshot;
            }
        }
        if (sawLoading && activePanel == null && now - lastLoadingSeen > 2) FinishStartup("LoadingPanel closed normally");
        else if (now - watchStarted > 120) FinishStartup(sawLoading ? "Observation duration reached" : "No active LoadingPanel observed within 120 seconds");
    }

    private static void FinishStartup(string status)
    {
        startupReport.status = status;
        if (!sawLoading) startupReport.errors.Add("No active LoadingPanel was observed; this does not verify startup.");
        if (!capturedBound) startupReport.errors.Add("No captured observation had all artwork sprite bindings resolved.");
        float firstVisibleProgress = -1;
        bool multipleVisibleProgressValues = false;
        foreach (var frame in startupReport.frames)
        {
            if (!File.Exists(frame.image)) startupReport.errors.Add("Requested screenshot did not arrive: " + frame.image);
            if (!frame.resourcesBound || frame.visibleAlpha <= 0) continue;
            if (firstVisibleProgress < 0) firstVisibleProgress = frame.progress;
            else if (Mathf.Abs(firstVisibleProgress - frame.progress) > 0.01f) multipleVisibleProgressValues = true;
        }
        if (!multipleVisibleProgressValues) startupReport.notes.Add("Multiple distinct visible runtime progress values were not captured; use the separately labeled static 0/50/100% previews for fill geometry verification.");
        startupReport.notes.Add("Progress samples are observations made before the next-frame screenshot. Startup may advance between observation and image capture.");
        WriteReport("startup-report.json", startupReport);
        SessionState.SetBool(WatchKey, false);
        State.lastOutput = Path.Combine(OutputDirectory, "startup-report.json");
    }

    private static bool IsVisual(MonoBehaviour behaviour)
    {
        return behaviour is Graphic || behaviour is BaseMeshEffect || behaviour is LayoutGroup || behaviour is ContentSizeFitter ||
               behaviour is AspectRatioFitter || behaviour is CanvasScaler || behaviour is Mask || behaviour is RectMask2D ||
               behaviour is ScrollRect || behaviour is Selectable || behaviour is TMP_SubMeshUI;
    }

    private static bool ActiveInPrefab(Transform transform)
    {
        while (transform != null) { if (!transform.gameObject.activeSelf) return false; transform = transform.parent; }
        return true;
    }

    private static string ObjectPath(Transform transform)
    {
        string path = transform.name;
        while (transform.parent != null) { transform = transform.parent; path = transform.name + "/" + path; }
        return path;
    }

    private static GameObject NewObject(string name, Scene scene, bool rectTransform)
    {
        var gameObject = rectTransform ? new GameObject(name, typeof(RectTransform)) : new GameObject(name);
        gameObject.hideFlags = HideFlags.HideAndDontSave;
        SceneManager.MoveGameObjectToScene(gameObject, scene);
        return gameObject;
    }

    private static void WriteReport(string filename, object report)
    {
        Directory.CreateDirectory(OutputDirectory);
        File.WriteAllText(Path.Combine(OutputDirectory, filename), JsonUtility.ToJson(report, true), Utf8);
    }

    private static void WriteState()
    {
        State.updatedUtc = DateTime.UtcNow.ToString("o");
        State.playing = EditorApplication.isPlaying;
        State.compiling = EditorApplication.isCompiling;
        State.previewing = previewQueue != null;
        State.watchingStartup = SessionState.GetBool(WatchKey, false);
        try { WriteReport("state.json", State); }
        catch (IOException) { /* An external report reader may briefly hold the file. */ }
    }
}
