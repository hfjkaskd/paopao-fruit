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
/// One-off, explicit-request static inspection. Uses only disposable preview-scene
/// clones. Never enters Play, opens game UI, changes account data, or saves assets.
/// </summary>
[InitializeOnLoad]
public static class UnlockPopupValidation
{
    const string PrefabPath = "Assets/FruitsHarvest/Resources/Original/res/local/pops/newitempop/NewItemPop.prefab";
    const string WidgetPath = "Assets/BizzaWZ/Final/Real/UI/GamePanel/Resources/Resources/GameUiWidget.prefab";
    const string SpriteDirectory = "Assets/FruitsHarvest/Resources/Original/res/local/pops/newitempop/sprite/";
    const string AnimationPath = "Assets/FruitsHarvest/Resources/Original/res/local/pops/newitempop/anim/anim_NewItemPop_in.anim";
    const int PopupOrder = -600;
    static string OutputDirectory => Path.GetFullPath(Path.Combine(Application.dataPath, "../Design/NewItemPopupFix-20260918/Validation"));
    static string CommandPath => Path.GetFullPath(Path.Combine(OutputDirectory, "../validate.command"));
    static double nextPoll;
    static bool busy;

    [Serializable] public class Entry { public string key; public string pt; }
    [Serializable] public class Localization { public Entry[] entries; }
    [Serializable] public class TextMetric
    {
        public string path, text;
        public float fontSize;
        public bool overflowing, truncated, meshOutsideRect;
        public Rect rect, meshBounds;
        public Vector3 worldPosition;
    }
    [Serializable] public class GraphicMetric
    {
        public string path, sprite;
        public bool raycastTarget;
        public int sortingOrder;
        public Rect screenRect;
    }
    [Serializable] public class ButtonMetric
    {
        public string path, targetGraphic;
        public bool interactable;
        public Rect screenRect;
    }
    [Serializable] public class PreviewMetric
    {
        public string tool, image, animationSample;
        public int width, height, disabledBusinessBehaviours;
        public List<TextMetric> texts = new List<TextMetric>();
        public List<GraphicMetric> graphics = new List<GraphicMetric>();
        public List<ButtonMetric> buttons = new List<ButtonMetric>();
        public List<string> warnings = new List<string>();
    }
    [Serializable] public class Report
    {
        public string generatedUtc;
        public string kind = "STATIC PREFAB PREVIEW. Portuguese strings and sprites bound to disposable clones; opening animation sampled at its end. Business scripts, particles and animation playback disabled. Not a runtime screenshot or gameplay verification.";
        public string error;
        public int popupOrder = PopupOrder;
        public int widgetOrder;
        public bool widgetOverridesSorting, widgetBelowPopup, shadowBlocksRaycasts;
        public string sortingCheck = "Static inspection of serialized GameUiWidget Canvas and full-screen Shadow Image, compared with BaseUI/MgrUI popup Top order. Does not execute live UI, raycasts or account flows.";
        public List<string> widgetCanvasOrders = new List<string>();
        public List<PreviewMetric> previews = new List<PreviewMetric>();
    }

    static UnlockPopupValidation() { EditorApplication.update += Tick; }

    static void Tick()
    {
        if (busy || EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.timeSinceStartup < nextPoll) return;
        nextPoll = EditorApplication.timeSinceStartup + 1;
        if (!File.Exists(CommandPath)) return;
        string command = File.ReadAllText(CommandPath).Trim();
        File.Delete(CommandPath);
        if (command != "validate") return;
        busy = true;
        var report = new Report { generatedUtc = DateTime.UtcNow.ToString("o") };
        try
        {
            Directory.CreateDirectory(OutputDirectory);
            var localization = JsonUtility.FromJson<Localization>("{\"entries\":" + File.ReadAllText("Assets/FruitsHarvest/Resources/Original/res/local/configs/Text.json") + "}");
            var strings = new Dictionary<string, string>();
            foreach (Entry row in localization.entries) strings[row.key] = row.pt;
            InspectSorting(report);
            string[] tools = { "Undo", "Shuffle", "Magic", "Extra" };
            string[] keys = { "guide_undo", "guide_shuffle", "guide_magicwand", "guide_extraslot" };
            for (int i = 0; i < tools.Length; i++) report.previews.Add(Render(tools[i], keys[i], 1920, strings));
            report.previews.Add(Render("Undo", keys[0], 2160, strings));
        }
        catch (Exception exception) { report.error = exception.ToString(); }
        finally
        {
            File.WriteAllText(Path.Combine(OutputDirectory, "static-validation-report.json"), JsonUtility.ToJson(report, true));
            busy = false;
        }
    }

    static void InspectSorting(Report report)
    {
        var widget = AssetDatabase.LoadAssetAtPath<GameObject>(WidgetPath);
        if (widget == null) throw new FileNotFoundException("Widget prefab unavailable", WidgetPath);
        var rootCanvas = widget.GetComponent<Canvas>();
        if (rootCanvas == null) throw new InvalidOperationException("Widget has no root Canvas");
        report.widgetOrder = rootCanvas.sortingOrder;
        report.widgetOverridesSorting = rootCanvas.overrideSorting;
        report.widgetBelowPopup = rootCanvas.overrideSorting && rootCanvas.sortingOrder < PopupOrder;
        foreach (var canvas in widget.GetComponentsInChildren<Canvas>(true))
        {
            report.widgetCanvasOrders.Add(HierarchyPath(canvas.transform, widget.transform) + ": override=" + canvas.overrideSorting + ", serializedOrder=" + canvas.sortingOrder);
            if (canvas.overrideSorting && canvas.sortingOrder >= PopupOrder) report.widgetBelowPopup = false;
        }
        var popup = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        var shadow = popup.transform.Find("Shadow").GetComponent<Image>();
        report.shadowBlocksRaycasts = shadow != null && shadow.enabled && shadow.raycastTarget && shadow.color.a > 0;
    }

    static PreviewMetric Render(string tool, string key, int height, Dictionary<string, string> strings)
    {
        var metric = new PreviewMetric { tool = tool, width = 1080, height = height };
        metric.image = Path.Combine(OutputDirectory, "static-" + tool.ToLowerInvariant() + "-1080x" + height + ".png");
        Scene scene = EditorSceneManager.NewPreviewScene();
        RenderTexture target = null;
        Texture2D pixels = null;
        RenderTexture previous = RenderTexture.active;
        try
        {
            var cameraObject = NewObject("Static validation camera", scene, false);
            var camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.scene = scene;
            camera.cameraType = CameraType.Game;
            camera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(scene);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.05f, 0.13f, 0.17f, 1);
            camera.orthographic = true;
            camera.orthographicSize = height * 0.5f;
            camera.aspect = 1080f / height;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 2000;
            camera.transform.position = new Vector3(0, 0, -1000);
            camera.allowHDR = false;
            camera.allowMSAA = false;
            target = new RenderTexture(1080, height, 24, RenderTextureFormat.ARGB32);
            target.Create();
            camera.targetTexture = target;

            // Inactive staging parent ensures prefab business OnEnable never runs.
            var stage = NewObject("Disposable static canvas", scene, true);
            stage.SetActive(false);
            var canvas = stage.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;
            canvas.sortingOrder = -1100;
            ((RectTransform)stage.transform).sizeDelta = new Vector2(1080, height);
            var backdropObject = NewObject("Static backdrop", scene, true);
            backdropObject.transform.SetParent(stage.transform, false);
            Stretch((RectTransform)backdropObject.transform);
            var backdrop = backdropObject.AddComponent<Image>();
            backdrop.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/OrchardUI/Resources/OrchardUI/Backdrop.png");
            backdrop.color = new Color(0.55f, 0.61f, 0.60f, 1);
            backdrop.raycastTarget = false;

            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var instance = Object.Instantiate(asset, stage.transform, false);
            instance.name = asset.name;
            instance.hideFlags = HideFlags.HideAndDontSave;
            Stretch((RectTransform)instance.transform);
            DisableNonvisual(instance, metric);
            var popupCanvas = instance.GetComponent<Canvas>();
            if (popupCanvas == null) popupCanvas = instance.AddComponent<Canvas>();
            popupCanvas.overrideSorting = true;
            popupCanvas.sortingOrder = PopupOrder;
            foreach (var layer in instance.GetComponentsInChildren<Orange.DynamicCanvasLayer>(true))
            {
                var serializedLayer = new SerializedObject(layer);
                var childCanvas = layer.GetComponent<Canvas>();
                if (childCanvas == null) childCanvas = layer.gameObject.AddComponent<Canvas>();
                childCanvas.overrideSorting = true;
                childCanvas.sortingOrder = PopupOrder + serializedLayer.FindProperty("offset").intValue;
            }
            foreach (var childCanvas in instance.GetComponentsInChildren<Canvas>(true))
            {
                childCanvas.worldCamera = camera;
                childCanvas.renderMode = RenderMode.WorldSpace;
            }

            foreach (var text in instance.GetComponentsInChildren<TMP_Text>(true))
            {
                string path = HierarchyPath(text.transform, instance.transform);
                if (text.name == "ItemName") text.text = strings[key];
                else if (text.name == "Discription") text.text = strings[key + "_text"];
                else if (path.Contains("/ClaimBtn/")) text.text = strings["common_continue"];
                else if (path.Contains("/Title")) text.text = strings["guide_newtool_title"];
            }
            var itemTransform = instance.transform.Find("MainContent/Body/Item");
            if (itemTransform == null) throw new InvalidOperationException("Expected original Body/Item transform");
            var item = itemTransform.GetComponent<Image>();
            item.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDirectory + "Icon" + tool + ".asset");
            if (item.sprite == null) throw new FileNotFoundException("Tool sprite missing", SpriteDirectory + "Icon" + tool + ".asset");
            item.SetNativeSize();
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(AnimationPath);
            if (clip == null) throw new FileNotFoundException("Opening animation missing", AnimationPath);
            clip.SampleAnimation(instance, clip.length);
            metric.animationSample = clip.name + " at " + clip.length + " seconds; isolated clone only, no events/playback";
            // Sampling may re-enable animation-driven components; keep business disabled.
            DisableNonvisual(instance, metric);
            instance.SetActive(true);
            stage.SetActive(true);
            Canvas.ForceUpdateCanvases();
            foreach (var text in instance.GetComponentsInChildren<TMP_Text>(true))
                if (text.isActiveAndEnabled) text.ForceMeshUpdate(true, true);
            foreach (var graphic in instance.GetComponentsInChildren<Graphic>(true))
            {
                if (!graphic.isActiveAndEnabled) continue;
                graphic.SetAllDirty();
                graphic.Rebuild(CanvasUpdate.PreRender);
            }
            Canvas.ForceUpdateCanvases();
            CollectMetrics(instance, camera, metric);
            camera.Render();
            RenderTexture.active = target;
            pixels = new Texture2D(1080, height, TextureFormat.RGBA32, false);
            pixels.ReadPixels(new Rect(0, 0, 1080, height), 0, 0);
            pixels.Apply(false, false);
            File.WriteAllBytes(metric.image, pixels.EncodeToPNG());
            return metric;
        }
        finally
        {
            RenderTexture.active = previous;
            if (pixels != null) Object.DestroyImmediate(pixels);
            if (target != null) { target.Release(); Object.DestroyImmediate(target); }
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    static void DisableNonvisual(GameObject instance, PreviewMetric metric)
    {
        foreach (var behaviour in instance.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (behaviour == null || behaviour is Graphic || behaviour is BaseMeshEffect || behaviour is LayoutGroup ||
                behaviour is ContentSizeFitter || behaviour is AspectRatioFitter || behaviour is CanvasScaler ||
                behaviour is Mask || behaviour is RectMask2D || behaviour is Selectable || behaviour is TMP_SubMeshUI) continue;
            if (behaviour.enabled) metric.disabledBusinessBehaviours++;
            behaviour.enabled = false;
        }
        foreach (var animation in instance.GetComponentsInChildren<Animation>(true)) { animation.Stop(); animation.enabled = false; }
        foreach (var animator in instance.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
        foreach (var particle in instance.GetComponentsInChildren<ParticleSystem>(true)) particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        foreach (var renderer in instance.GetComponentsInChildren<ParticleSystemRenderer>(true)) renderer.enabled = false;
        foreach (var audio in instance.GetComponentsInChildren<AudioSource>(true)) audio.enabled = false;
        foreach (var camera in instance.GetComponentsInChildren<Camera>(true)) camera.enabled = false;
    }

    static void CollectMetrics(GameObject instance, Camera camera, PreviewMetric metric)
    {
        foreach (var text in instance.GetComponentsInChildren<TMP_Text>(true))
        {
            if (!text.isActiveAndEnabled) continue;
            Rect rect = text.rectTransform.rect;
            Bounds bounds = text.textBounds;
            Rect mesh = new Rect(bounds.min.x, bounds.min.y, bounds.size.x, bounds.size.y);
            bool outside = mesh.xMin < rect.xMin - 2 || mesh.xMax > rect.xMax + 2 || mesh.yMin < rect.yMin - 2 || mesh.yMax > rect.yMax + 2;
            metric.texts.Add(new TextMetric { path = HierarchyPath(text.transform, instance.transform), text = text.text,
                fontSize = text.fontSize, rect = rect, meshBounds = mesh, worldPosition = text.transform.position,
                overflowing = text.isTextOverflowing, truncated = text.isTextTruncated, meshOutsideRect = outside });
            if (outside || text.isTextOverflowing || text.isTextTruncated) metric.warnings.Add("Text overflow: " + HierarchyPath(text.transform, instance.transform));
        }
        foreach (var graphic in instance.GetComponentsInChildren<Graphic>(true))
        {
            if (!graphic.isActiveAndEnabled) continue;
            var image = graphic as Image;
            var graphicCanvas = graphic.canvas;
            metric.graphics.Add(new GraphicMetric { path = HierarchyPath(graphic.transform, instance.transform),
                sprite = image != null && image.sprite != null ? image.sprite.name : "", raycastTarget = graphic.raycastTarget,
                sortingOrder = graphicCanvas != null ? graphicCanvas.sortingOrder : 0, screenRect = ScreenRect(graphic.rectTransform, camera) });
        }
        foreach (var button in instance.GetComponentsInChildren<Button>(true))
            if (button.gameObject.activeInHierarchy) metric.buttons.Add(new ButtonMetric { path = HierarchyPath(button.transform, instance.transform),
                targetGraphic = button.targetGraphic != null ? HierarchyPath(button.targetGraphic.transform, instance.transform) : "MISSING",
                interactable = button.interactable, screenRect = ScreenRect((RectTransform)button.transform, camera) });
        if (metric.texts.Count < 4) metric.warnings.Add("Expected title, tool name, description and button text; found fewer than four visible text components.");
    }

    static Rect ScreenRect(RectTransform rect, Camera camera)
    {
        var corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        Vector3 min = camera.WorldToScreenPoint(corners[0]);
        Vector3 max = camera.WorldToScreenPoint(corners[2]);
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }
    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        rect.anchoredPosition3D = Vector3.zero; rect.localScale = Vector3.one;
    }
    static GameObject NewObject(string name, Scene scene, bool rect)
    {
        var result = rect ? new GameObject(name, typeof(RectTransform)) : new GameObject(name);
        result.hideFlags = HideFlags.HideAndDontSave;
        SceneManager.MoveGameObjectToScene(result, scene);
        return result;
    }
    static string HierarchyPath(Transform current, Transform root)
    {
        string path = current.name;
        while (current != root && current.parent != null) { current = current.parent; path = current.name + "/" + path; }
        return path;
    }
}
