using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Temporary, offscreen prefab renderer. Review before copying to an Editor folder.
// It never activates the UIBizzaAAA page, invokes game methods, toggles Play,
// switches scenes, captures the desktop, dispatches clicks or saves assets.
[InitializeOnLoad]
public static class BubbleChestValidation
{
    const string Output = "Design/BubbleChestRestyle-20260921/Validation";
    const string PrefabPath = "Assets/BizzaWZ/Final/Real/UI/GamePanel/UIBizzaAAA.prefab";
    const string LogicPath = "Assets/BizzaWZ/Final/Real/UI/GamePanel/UIBizzaAAA.cs";
    const string BubblePath = "Assets/BizzaWZ/Common/BizzaGame/Z_ReplaceAssets/UI_Frame/GamePanel/Bg_Bubble.png";
    const string ChestPath = "Assets/BizzaWZ/Common/BizzaGame/Z_ReplaceAssets/UI_Frame/GamePanel/Icon_Bubble.png";
    const string NodePath = "Safe Zone/BtnFlowTreature";
    const int LogicalSize = 320;
    static double nextPoll;

    [Serializable] class Batch
    {
        public string kind = "UNITY PREFAB STATIC PREVIEW; actual prefab Images only; not runtime/gameplay verification";
        public string generatedUtc, error;
        public bool editorWasPlaying, editorStillPlaying, sourceAssetsUnchanged;
        public List<Fingerprint> fingerprints = new List<Fingerprint>();
        public List<CaseReport> cases = new List<CaseReport>();
    }
    [Serializable] class Fingerprint { public string path, before, after; public bool unchanged; }
    [Serializable] class CaseReport
    {
        public string image, error;
        public int pixelWidth, pixelHeight, strippedComponents, remainingBusinessComponents, activeImages, sourceButtonPersistentCalls;
        public float logicalPixelScale;
        public Vector2 logicalCanvasSize, originalButtonSize, originalChestSize;
        public bool originalRectsPreserved, sourceButtonInteractable;
        public string sourceButtonTargetGraphic;
        public List<ImageRecord> images = new List<ImageRecord>();
    }
    [Serializable] class ImageRecord
    {
        public string node, spritePath, spriteName, spriteGuid, texturePath;
        public long spriteFileID;
        public Rect spriteRect, uiRect;
        public Vector2 anchoredPosition, sizeDelta;
        public Vector3 localScale;
        public Color color;
        public bool enabled, active, preserveAspect;
        public int imageType, textureWidth, textureHeight;
    }

    static BubbleChestValidation() { EditorApplication.update += Tick; }

    static void Tick()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.timeSinceStartup < nextPoll) return;
        nextPoll = EditorApplication.timeSinceStartup + 1;
        string commandPath = Output + "/preview.command";
        if (!File.Exists(commandPath)) return;
        if (EditorApplication.isPlaying != EditorApplication.isPlayingOrWillChangePlaymode) return;
        string command = File.ReadAllText(commandPath).Trim();
        File.Delete(commandPath);
        if (command != "preview") return;
        Directory.CreateDirectory(Output);
        var batch = new Batch { generatedUtc = DateTime.UtcNow.ToString("o"), editorWasPlaying = EditorApplication.isPlaying };
        try
        {
            foreach (string path in new[] { PrefabPath, PrefabPath + ".meta", LogicPath, LogicPath + ".meta", BubblePath,
                BubblePath + ".meta", ChestPath, ChestPath + ".meta" }) AddFingerprint(batch, path);
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (source == null) throw new FileNotFoundException(PrefabPath);
            var target = source.transform.Find(NodePath);
            if (target == null) throw new InvalidDataException("Required prefab subtree missing: " + NodePath);
            if (target.GetComponentsInChildren<Image>(true).Length != 3)
                throw new InvalidDataException("Expected exactly one chest Image and two existing bubble Images. Review changed prefab before rendering.");
            // Fingerprint actual referenced graphics too, whether the chest was
            // replaced in place or assigned as a separate imported sprite.
            foreach (var image in target.GetComponentsInChildren<Image>(true))
            {
                if (image.sprite == null) throw new InvalidDataException("Source sprite missing: " + image.name);
                string assetPath = AssetDatabase.GetAssetPath(image.sprite);
                AddFingerprint(batch, assetPath); AddFingerprint(batch, assetPath + ".meta");
            }
            batch.cases.Add(Render(target.gameObject, 1));
            batch.cases.Add(Render(target.gameObject, 2));
        }
        catch (Exception e) { batch.error = e.ToString(); }
        finally
        {
            batch.sourceAssetsUnchanged = true;
            foreach (var fingerprint in batch.fingerprints)
            {
                fingerprint.after = Hash(fingerprint.path);
                fingerprint.unchanged = fingerprint.before == fingerprint.after;
                batch.sourceAssetsUnchanged &= fingerprint.unchanged;
            }
            if (!batch.sourceAssetsUnchanged) batch.error = "Source files changed during preview; discard mixed-version preview and rerun.";
            batch.editorStillPlaying = EditorApplication.isPlaying;
            File.WriteAllText(Output + "/static-validation.json", JsonUtility.ToJson(batch, true));
        }
    }

    static CaseReport Render(GameObject source, int scale)
    {
        var report = new CaseReport
        {
            image = Path.GetFullPath(Output + "/static-bubble-chest-" + scale + "x.png"),
            pixelWidth = LogicalSize * scale, pixelHeight = LogicalSize * scale,
            logicalCanvasSize = new Vector2(LogicalSize, LogicalSize), logicalPixelScale = scale
        };
        var scene = EditorSceneManager.NewPreviewScene();
        RenderTexture target = null;
        Texture2D pixels = null;
        RenderTexture previous = RenderTexture.active;
        try
        {
            var sourceRect = source.GetComponent<RectTransform>();
            var sourceChest = source.transform.Find("Image").GetComponent<RectTransform>();
            report.originalButtonSize = sourceRect.sizeDelta; report.originalChestSize = sourceChest.sizeDelta;
            if (report.originalButtonSize != new Vector2(200, 200) || report.originalChestSize != new Vector2(133, 117))
                throw new InvalidDataException("Production button or chest dimensions changed; inspect rather than normalize them in this renderer.");
            var sourceButton = source.GetComponent<Button>();
            if (sourceButton == null) throw new InvalidDataException("Production Button missing.");
            report.sourceButtonPersistentCalls = sourceButton.onClick.GetPersistentEventCount();
            report.sourceButtonInteractable = sourceButton.interactable;
            report.sourceButtonTargetGraphic = sourceButton.targetGraphic != null ? sourceButton.targetGraphic.name : "null";

            var holder = new GameObject("Inactive isolated chest preview holder");
            SceneManager.MoveGameObjectToScene(holder, scene); holder.SetActive(false);
            var canvasObject = new GameObject("Offscreen static canvas", typeof(RectTransform), typeof(Canvas));
            canvasObject.transform.SetParent(holder.transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace; canvas.referencePixelsPerUnit = 100;
            var canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = report.logicalCanvasSize; canvasRect.localPosition = Vector3.zero;
            if (canvasObject.activeInHierarchy) throw new InvalidOperationException("Clone parent must be inactive.");
            // Clone only the button subtree, never the business page parent.
            var clone = Object.Instantiate(source, canvasObject.transform, false); clone.name = source.name;
            if (clone.activeInHierarchy) throw new InvalidOperationException("Unsafe active clone.");
            foreach (var c in clone.GetComponentsInChildren<MonoBehaviour>(true))
                if (c != null && !Pure(c)) { Object.DestroyImmediate(c); report.strippedComponents++; }
            foreach (var c in clone.GetComponentsInChildren<Component>(true))
                if (c != null && !Pure(c)) { Object.DestroyImmediate(c); report.strippedComponents++; }
            foreach (var t in clone.GetComponentsInChildren<Transform>(true)) GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
            foreach (var c in clone.GetComponentsInChildren<Component>(true))
                if (c == null || !Pure(c)) report.remainingBusinessComponents++;
            if (report.remainingBusinessComponents != 0) throw new InvalidOperationException("Business component remains; refusing activation.");
            // All RectTransforms, Image settings, original Image order, and both
            // bubble layers remain exactly as serialized in the production prefab.
            report.originalRectsPreserved = SameRect(sourceRect, clone.GetComponent<RectTransform>()) &&
                SameRect(sourceChest, clone.transform.Find("Image").GetComponent<RectTransform>());
            if (!report.originalRectsPreserved) throw new InvalidOperationException("Unexpected layout change in cloned subtree.");

            var cameraObject = new GameObject("Isolated static camera", typeof(Camera));
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var camera = cameraObject.GetComponent<Camera>(); camera.enabled = false;
            camera.scene = scene; camera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(scene);
            camera.orthographic = true; camera.orthographicSize = LogicalSize * .5f; camera.aspect = 1;
            camera.nearClipPlane = .1f; camera.farClipPlane = 1000;
            camera.transform.position = new Vector3(0, 0, -500);
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.12f, .24f, .18f, 1);
            camera.allowHDR = false; camera.allowMSAA = false; canvas.worldCamera = camera;
            target = new RenderTexture(report.pixelWidth, report.pixelHeight, 24, RenderTextureFormat.ARGB32);
            target.Create(); camera.targetTexture = target;
            holder.SetActive(true); Canvas.ForceUpdateCanvases();
            foreach (var image in clone.GetComponentsInChildren<Image>(true))
            {
                report.images.Add(Record(image));
                if (image.isActiveAndEnabled)
                {
                    image.SetAllDirty(); image.Rebuild(CanvasUpdate.PreRender); report.activeImages++;
                }
            }
            if (report.activeImages != 3) throw new InvalidOperationException("All three production Image layers must be visible.");
            Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = target;
            pixels = new Texture2D(report.pixelWidth, report.pixelHeight, TextureFormat.RGBA32, false);
            pixels.ReadPixels(new Rect(0, 0, report.pixelWidth, report.pixelHeight), 0, 0); pixels.Apply(false, false);
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

    static bool Pure(Component c)
    {
        Type type = c.GetType();
        return type == typeof(Transform) || type == typeof(RectTransform) || type == typeof(Image) || type == typeof(CanvasRenderer);
    }
    static bool SameRect(RectTransform a, RectTransform b)
    {
        return a.anchorMin == b.anchorMin && a.anchorMax == b.anchorMax && a.anchoredPosition == b.anchoredPosition &&
            a.sizeDelta == b.sizeDelta && a.pivot == b.pivot && a.localScale == b.localScale && a.localRotation == b.localRotation;
    }
    static ImageRecord Record(Image image)
    {
        var sprite = image.sprite;
        if (sprite == null) throw new InvalidDataException("Missing sprite: " + image.name);
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite, out string guid, out long fileID);
        return new ImageRecord
        {
            node = image.name, spritePath = AssetDatabase.GetAssetPath(sprite), spriteName = sprite.name,
            spriteGuid = guid, spriteFileID = fileID, spriteRect = sprite.rect, texturePath = AssetDatabase.GetAssetPath(sprite.texture),
            textureWidth = sprite.texture.width, textureHeight = sprite.texture.height,
            uiRect = image.rectTransform.rect, anchoredPosition = image.rectTransform.anchoredPosition,
            sizeDelta = image.rectTransform.sizeDelta, localScale = image.transform.localScale,
            color = image.color, enabled = image.enabled, active = image.gameObject.activeSelf,
            preserveAspect = image.preserveAspect, imageType = (int)image.type
        };
    }
    static void AddFingerprint(Batch batch, string path)
    {
        foreach (var fingerprint in batch.fingerprints) if (fingerprint.path == path) return;
        batch.fingerprints.Add(new Fingerprint { path = path, before = Hash(path) });
    }
    static string Hash(string path)
    {
        if (!File.Exists(path)) return "MISSING";
        using (var stream = File.OpenRead(path)) using (var hash = SHA256.Create())
            return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
    }
}
