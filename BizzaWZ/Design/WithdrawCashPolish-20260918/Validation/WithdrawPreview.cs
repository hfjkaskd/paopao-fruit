using System;
using System.Collections.Generic;
using System.Globalization;
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

/// <summary>Disposable production-prefab renders. No account, wallet, ad, save, withdrawal, input or gameplay methods.</summary>
[InitializeOnLoad]
public static class WithdrawPreview
{
    private const string CanvasPath = "Assets/BizzaWZ/Common/Framework/GameCanvas.prefab";
    private const string PagePath = "Assets/BizzaWZ/Final/Real/UI/FakeWithdrawPanel/FakeWithdrawPanel.prefab";
    private const string ItemPath = "Assets/BizzaWZ/Final/Real/UI/FakeWithdrawPanel/WithdrawAmountItem.prefab";
    private const string BackdropPath = "Assets/OrchardUI/Resources/OrchardUI/Backdrop.png";
    private const string GridPath = "Content/WithdrawAmount/Scroll View/Viewport/Content";
    private const string ProgressPath = "Content/WithdrawProgress/Progress";
    private const int Width = 1080, Height = 1920;
    private static string Output => Path.GetFullPath(Path.Combine(Application.dataPath, "../Design/WithdrawCashPolish-20260918"));
    private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);
    private static readonly State state = new State();
    private static Queue<float> pending;
    private static Batch batch;
    private static double nextStatus;

    [Serializable] private sealed class State
    {
        public string kind = "STATIC PREFAB PREVIEW bridge; never controls gameplay";
        public string updatedUtc, lastCommand, lastOutput, lastError;
        public bool playing, compiling, waitingForEditMode, busy;
        public int completed, total;
    }
    [Serializable] public sealed class ImageRecord
    {
        public string path, spriteAsset;
        public bool active, enabled;
        public int type, fillMethod, fillOrigin, meshVertexCount;
        public float fillAmount, pixelsPerUnitMultiplier;
        public Vector4 spriteBorder;
        public Rect spriteRect, screenRect, renderedMeshBounds;
        public Vector2 size;
    }
    [Serializable] public sealed class TextRecord
    {
        public string path, text;
        public bool active, enabled, tmpOverflow, glyphOverflow;
        public float actualFontSize;
        public Rect screenRect, glyphBounds;
        public Vector4 overflowLeftBottomRightTop;
    }
    [Serializable] public sealed class Fingerprint
    {
        public string path, before, after;
        public bool unchanged;
    }
    [Serializable] public sealed class Report
    {
        public string kind = "PREFAB STATIC PREVIEW WITH SAMPLE VALUES — NOT UNITY RUNTIME OR REAL ACCOUNT DATA";
        public string generatedUtc, screenshot, error;
        public string interpretation = "Actual production GameCanvas and FakeWithdrawPanel, including six existing WithdrawAmountItem instances, rendered in an isolated PreviewScene. Business/localization/animations/audio are disabled before activation. Portuguese text and data are explicit samples on clones. OrchardBackdrop uses its real Sprite through a documented static binding. No account, wallet, ad, withdrawal, save, current UI, Play mode or user scene is touched. Progress mesh bounds describe Unity's native Image geometry, not a hand-drawn simulation.";
        public int width = Width, height = Height, disabledBusinessBehaviours, amountItemCount, visibleGlyphOverflowCount, nonUniformPixelSamples;
        public float sampleProgress, canvasPixelScale;
        public Vector2 referenceResolution, logicalCanvasSize;
        public List<string> sampleChanges = new List<string>();
        public List<ImageRecord> progressImages = new List<ImageRecord>();
        public List<TextRecord> texts = new List<TextRecord>();
        public List<Fingerprint> sourceAssets = new List<Fingerprint>();
    }
    [Serializable] private sealed class Batch
    {
        public string kind = "STATIC PREFAB PREVIEW BATCH — NOT RUNTIME";
        public string generatedUtc, folder;
        public List<Report> cases = new List<Report>();
    }

    static WithdrawPreview() { EditorApplication.update += Tick; }

    private static void Tick()
    {
        state.playing = EditorApplication.isPlayingOrWillChangePlaymode;
        state.compiling = EditorApplication.isCompiling || EditorApplication.isUpdating;
        string commandPath = Path.Combine(Output, "preview.command");
        state.waitingForEditMode = state.playing && (pending != null || File.Exists(commandPath));
        if (!state.playing && !state.compiling)
        {
            if (pending != null) RunNext();
            else if (File.Exists(commandPath))
            {
                try
                {
                    string command = File.ReadAllText(commandPath).Trim(); File.Delete(commandPath);
                    state.lastCommand = command; state.lastError = string.Empty;
                    if (command != "preview" && !command.StartsWith("preview:", StringComparison.Ordinal)) throw new ArgumentException("Expected preview[:label].");
                    string label = command == "preview" ? DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") : SafeName(command.Substring(8));
                    batch = new Batch { generatedUtc = DateTime.UtcNow.ToString("o"), folder = Path.Combine(Output, "Previews", label) };
                    Directory.CreateDirectory(batch.folder);
                    pending = new Queue<float>(new[] { 1f, .35f, 0f });
                    state.completed = 0; state.total = 3; state.busy = true;
                }
                catch (Exception exception) { state.lastError = exception.ToString(); }
            }
        }
        if (EditorApplication.timeSinceStartup >= nextStatus)
        {
            nextStatus = EditorApplication.timeSinceStartup + 2;
            state.updatedUtc = DateTime.UtcNow.ToString("o");
            try { Json(Path.Combine(Output, "preview-state.json"), state); } catch (IOException) { }
        }
    }

    private static void RunNext()
    {
        if (pending.Count == 0)
        {
            pending = null; state.busy = false; state.lastOutput = Path.Combine(batch.folder, "report.json"); return;
        }
        float progress = pending.Dequeue();
        string name = "static-withdraw-" + Mathf.RoundToInt(progress * 100).ToString("D3", CultureInfo.InvariantCulture);
        Report report;
        try { report = Render(progress, Path.Combine(batch.folder, name + ".png")); }
        catch (Exception exception) { report = new Report { sampleProgress = progress, error = exception.ToString() }; state.lastError = report.error; }
        batch.cases.Add(report); state.completed++; state.lastOutput = report.screenshot;
        Json(Path.Combine(batch.folder, name + ".json"), report);
        Json(Path.Combine(batch.folder, "report.json"), batch);
    }

    private static Report Render(float progress, string outputPath)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit mode only; Play is never stopped by this tool.");
        var report = new Report { generatedUtc = DateTime.UtcNow.ToString("o"), sampleProgress = progress, screenshot = outputPath };
        foreach (string path in new[] { CanvasPath, PagePath, ItemPath }) report.sourceAssets.Add(new Fingerprint { path = path, before = Hash(path) });
        Scene scene = EditorSceneManager.NewPreviewScene();
        RenderTexture target = null, previousTarget = RenderTexture.active;
        Texture2D image = null;
        try
        {
            var holder = NewObject("Inactive withdrawal preview staging", scene); holder.SetActive(false);
            GameObject canvasObject = Instantiate(CanvasPath, holder.transform, report);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            if (canvas == null || scaler == null || scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize ||
                scaler.screenMatchMode != CanvasScaler.ScreenMatchMode.MatchWidthOrHeight || !Mathf.Approximately(scaler.matchWidthOrHeight, 1f))
                throw new InvalidDataException("Production scaler assumptions changed; update validator.");
            report.referenceResolution = scaler.referenceResolution;
            report.canvasPixelScale = Height / scaler.referenceResolution.y;
            report.logicalCanvasSize = new Vector2(Width / report.canvasPixelScale, scaler.referenceResolution.y);
            scaler.enabled = false;
            canvas.renderMode = RenderMode.WorldSpace; canvas.pixelPerfect = false;
            canvas.referencePixelsPerUnit = scaler.referencePixelsPerUnit;
            RectTransform canvasRect = (RectTransform)canvas.transform;
            canvasRect.localScale = Vector3.one; canvasRect.localRotation = Quaternion.identity;
            canvasRect.sizeDelta = report.logicalCanvasSize;
            canvasRect.localPosition = new Vector3((canvasRect.pivot.x - .5f) * report.logicalCanvasSize.x, (canvasRect.pivot.y - .5f) * report.logicalCanvasSize.y, 0);
            Transform content = Find(canvas.transform, "Content"); KeepOnly(canvas.transform, content);
            Transform baseLayer = Find(content, "BaseLayer"); KeepOnly(content, baseLayer); KeepOnly(baseLayer, null);
            GameObject page = Instantiate(PagePath, baseLayer, report);
            ApplySamples(page.transform, progress, report);
            Image backdrop = Find(page.transform, "OrchardBackdrop").GetComponent<Image>();
            Sprite backdropSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackdropPath);
            if (backdrop == null || backdropSprite == null) throw new InvalidDataException("Real OrchardBackdrop image or Sprite missing.");
            backdrop.sprite = backdropSprite; backdrop.color = Color.white; backdrop.enabled = true;
            report.sampleChanges.Add("STATIC ONLY: bind " + BackdropPath + " to existing OrchardBackdrop Image; runtime Resources loading is not exercised.");
            var camera = NewObject("Withdrawal static camera", scene).AddComponent<Camera>();
            camera.enabled = false; camera.scene = scene;
            camera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(scene);
            camera.cameraType = CameraType.Game; camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.65f, .85f, .93f); camera.orthographic = true;
            camera.orthographicSize = report.logicalCanvasSize.y * .5f; camera.aspect = (float)Width / Height;
            camera.nearClipPlane = .1f; camera.farClipPlane = 2000f; camera.transform.position = new Vector3(0, 0, -1000);
            camera.allowHDR = false; camera.allowMSAA = false;
            target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32); target.Create(); camera.targetTexture = target;
            foreach (Canvas nested in canvasObject.GetComponentsInChildren<Canvas>(true)) { nested.renderMode = RenderMode.WorldSpace; nested.worldCamera = camera; }
            holder.SetActive(true);
            Canvas.ForceUpdateCanvases(); LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)page.transform);
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)Find(page.transform, GridPath)); Canvas.ForceUpdateCanvases();
            foreach (TMP_Text text in page.GetComponentsInChildren<TMP_Text>(true)) if (text.isActiveAndEnabled) text.ForceMeshUpdate();
            Canvas.ForceUpdateCanvases(); camera.Render();
            Collect(page.transform, camera, report);
            RenderTexture.active = target;
            image = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0); image.Apply(false, false);
            var pixels = image.GetRawTextureData<Color32>(); Color32 first = pixels[0];
            for (int index = 0; index < pixels.Length; index += 503)
                if (Math.Abs(pixels[index].r - first.r) > 2 || Math.Abs(pixels[index].g - first.g) > 2 || Math.Abs(pixels[index].b - first.b) > 2) report.nonUniformPixelSamples++;
            if (report.nonUniformPixelSamples == 0) report.error = "Blank render: not valid visual verification.";
            File.WriteAllBytes(outputPath, image.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previousTarget;
            if (image != null) Object.DestroyImmediate(image);
            if (target != null) { target.Release(); Object.DestroyImmediate(target); }
            EditorSceneManager.ClosePreviewScene(scene);
            foreach (Fingerprint source in report.sourceAssets)
            {
                source.after = Hash(source.path); source.unchanged = source.before == source.after;
                if (!source.unchanged) report.error = "Source prefab changed during rendering; rerun instead of accepting a mixed-version capture.";
            }
        }
        return report;
    }

    private static void ApplySamples(Transform page, float progress, Report report)
    {
        // Existing pt-BR strings verified read-only in tbllanguage.bytes; this does not run localization code.
        SetText(page, "ButtomGroup/Title", "Sacar", report);
        SetText(page, "Content/CashBalance/Title", "Saldo em Dinheiro", report);
        SetText(page, "Content/CashBalance/Balance/Cash", "R$200,76", report);
        SetText(page, "Content/WithdrawAmount/Title", "Selecionar Valor", report);
        SetText(page, "Content/WithdrawProgress/Title", "Progresso", report);
        SetText(page, "Content/WithdrawBtn/Text", "Retirar", report);
        SetText(page, "Content/HintText", "Parabéns, todas as condições foram atendidas", report);
        SetText(page, ProgressPath + "/real/Text (TMP)", (progress * 100).ToString("F2", CultureInfo.InvariantCulture) + "%", report);
        Find(page, ProgressPath + "/real").GetComponent<Image>().fillAmount = progress;
        string[] amounts = { "0.01", "800", "1000", "2000", "3000", "5000" };
        Transform grid = Find(page, GridPath);
        for (int index = 0; index < grid.childCount; index++)
        {
            Transform item = grid.GetChild(index);
            if (item.Find("Amount") == null) continue;
            int sample = report.amountItemCount++;
            if (sample >= amounts.Length) throw new InvalidDataException("More than six production amount instances; review sample plan.");
            item.gameObject.SetActive(true);
            SetText(item, "Amount", amounts[sample], report);
            SetText(item, "GetTag/text", "Recompensa para Novos Usuários", report);
            SetText(item, "GetedTag/text", "Recompensa para Novos Usuários", report);
            Find(item, "GetTag").gameObject.SetActive(sample == 0);
            Find(item, "GetedTag").gameObject.SetActive(false);
            Find(item, "Select").gameObject.SetActive(sample == 0);
        }
        if (report.amountItemCount != 6) throw new InvalidDataException("Expected six existing amount instances; tool never creates missing UI.");
        report.sampleChanges.Add("Existing six items only: first sample selected/new-user tag; other claim/selected tags hidden on clones. No production hierarchy added or removed.");
        Transform finger = page.Find("ButtomGroup/HistoryBtn/FingerHint");
        if (finger != null) { finger.gameObject.SetActive(false); report.sampleChanges.Add("Static sample hides the existing animated tutorial finger."); }
    }

    private static void Collect(Transform page, Camera camera, Report report)
    {
        foreach (Image graphic in Find(page, ProgressPath).GetComponentsInChildren<Image>(true))
        {
            Sprite sprite = graphic.overrideSprite;
            var record = new ImageRecord
            {
                path = PathOf(graphic.transform, page), active = graphic.gameObject.activeInHierarchy, enabled = graphic.enabled,
                spriteAsset = sprite == null ? string.Empty : AssetDatabase.GetAssetPath(sprite),
                type = (int)graphic.type, fillMethod = (int)graphic.fillMethod, fillOrigin = graphic.fillOrigin, fillAmount = graphic.fillAmount,
                pixelsPerUnitMultiplier = graphic.pixelsPerUnitMultiplier, spriteBorder = sprite == null ? Vector4.zero : sprite.border,
                spriteRect = sprite == null ? Rect.zero : sprite.rect, size = graphic.rectTransform.rect.size,
                screenRect = RectOf(graphic.rectTransform, camera)
            };
            Mesh mesh = graphic.canvasRenderer.GetMesh();
            if (mesh != null)
            {
                record.meshVertexCount = mesh.vertexCount;
                record.renderedMeshBounds = PointsBounds(mesh.vertices, graphic.transform, camera);
            }
            report.progressImages.Add(record);
        }
        foreach (TMP_Text text in page.GetComponentsInChildren<TMP_Text>(true))
        {
            Rect rect = RectOf(text.rectTransform, camera), glyph = GlyphBounds(text, camera);
            Vector4 overflow = glyph.width <= 0 || glyph.height <= 0 ? Vector4.zero : new Vector4(Mathf.Max(0, rect.xMin - glyph.xMin), Mathf.Max(0, rect.yMin - glyph.yMin), Mathf.Max(0, glyph.xMax - rect.xMax), Mathf.Max(0, glyph.yMax - rect.yMax));
            bool exceeds = Mathf.Max(Mathf.Max(overflow.x, overflow.y), Mathf.Max(overflow.z, overflow.w)) > 1f;
            report.texts.Add(new TextRecord { path = PathOf(text.transform, page), text = text.text, active = text.gameObject.activeInHierarchy,
                enabled = text.enabled, tmpOverflow = text.isTextOverflowing, glyphOverflow = exceeds, actualFontSize = text.fontSize,
                screenRect = rect, glyphBounds = glyph, overflowLeftBottomRightTop = overflow });
            if (text.isActiveAndEnabled && !string.IsNullOrEmpty(text.text) && exceeds) report.visibleGlyphOverflowCount++;
        }
    }

    private static GameObject Instantiate(string path, Transform parent, Report report)
    {
        if (parent.gameObject.activeInHierarchy) throw new InvalidOperationException("Staging parent must be inactive.");
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (asset == null) throw new FileNotFoundException("Production prefab missing", path);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent); instance.hideFlags = HideFlags.HideAndDontSave;
        foreach (MonoBehaviour script in instance.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (script == null || script is Graphic || script is BaseMeshEffect || script is LayoutGroup || script is LayoutElement ||
                script is ContentSizeFitter || script is AspectRatioFitter || script is Mask || script is RectMask2D || script is ScrollRect || script is TMP_SubMeshUI || script is Button) continue;
            if (script.enabled) report.disabledBusinessBehaviours++; script.enabled = false;
        }
        foreach (Animator animator in instance.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
        foreach (Animation animation in instance.GetComponentsInChildren<Animation>(true)) { animation.Stop(); animation.enabled = false; }
        foreach (ParticleSystem particle in instance.GetComponentsInChildren<ParticleSystem>(true)) particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        foreach (AudioSource audio in instance.GetComponentsInChildren<AudioSource>(true)) audio.enabled = false;
        foreach (Camera camera in instance.GetComponentsInChildren<Camera>(true)) camera.enabled = false;
        return instance;
    }

    private static Rect RectOf(RectTransform transform, Camera camera)
    {
        var corners = new Vector3[4]; transform.GetWorldCorners(corners);
        return PointsBounds(corners, null, camera);
    }
    private static Rect GlyphBounds(TMP_Text text, Camera camera)
    {
        if (text.textInfo == null || text.textInfo.characterInfo == null) return Rect.zero;
        var points = new List<Vector3>();
        for (int index = 0; index < Mathf.Min(text.textInfo.characterCount, text.textInfo.characterInfo.Length); index++)
        {
            TMP_CharacterInfo c = text.textInfo.characterInfo[index]; if (!c.isVisible) continue;
            points.Add(c.bottomLeft); points.Add(c.topRight);
        }
        return PointsBounds(points.ToArray(), text.transform, camera);
    }
    private static Rect PointsBounds(Vector3[] points, Transform localTransform, Camera camera)
    {
        if (points == null || points.Length == 0) return Rect.zero;
        Vector2 min = camera.WorldToScreenPoint(localTransform == null ? points[0] : localTransform.TransformPoint(points[0])), max = min;
        for (int index = 1; index < points.Length; index++)
        {
            Vector2 p = camera.WorldToScreenPoint(localTransform == null ? points[index] : localTransform.TransformPoint(points[index]));
            min = Vector2.Min(min, p); max = Vector2.Max(max, p);
        }
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }
    private static void SetText(Transform root, string path, string value, Report report)
    {
        TMP_Text text = Find(root, path).GetComponent<TMP_Text>();
        if (text == null) throw new InvalidDataException("Expected TMP at " + path);
        text.text = value; report.sampleChanges.Add(root.name + "/" + path + " = " + value);
    }
    private static Transform Find(Transform root, string path) => root.Find(path) ?? throw new InvalidDataException("Missing production node " + root.name + "/" + path);
    private static void KeepOnly(Transform root, Transform keep) { for (int index = 0; index < root.childCount; index++) root.GetChild(index).gameObject.SetActive(root.GetChild(index) == keep); }
    private static string PathOf(Transform value, Transform root) { string path = value.name; while (value != root && value.parent != null) { value = value.parent; path = value.name + "/" + path; } return path; }
    private static GameObject NewObject(string name, Scene scene) { var value = new GameObject(name) { hideFlags = HideFlags.HideAndDontSave }; SceneManager.MoveGameObjectToScene(value, scene); return value; }
    private static string Hash(string path) { using (SHA256 sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(Path.GetFullPath(Path.Combine(Application.dataPath, "..", path))))).Replace("-", string.Empty).ToLowerInvariant(); }
    private static string SafeName(string value) { var name = new StringBuilder(); foreach (char c in value) name.Append(char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '-'); return name.Length == 0 ? "current" : name.ToString(); }
    private static void Json(string path, object value) { Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllText(path, JsonUtility.ToJson(value, true), Utf8); }
}
