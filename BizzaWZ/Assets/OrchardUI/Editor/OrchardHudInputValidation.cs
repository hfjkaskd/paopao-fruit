using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>
/// Read-only, resolved-prefab input regression. No Play mode, click dispatch,
/// account initialization, scene saves, or prefab saves are performed.
/// Batch entry: -executeMethod OrchardHudInputValidation.Validate -quit
/// </summary>
public static class OrchardHudInputValidation
{
    private const string PrefabPath = "Assets/BizzaWZ/Final/Real/UI/GamePanel/Resources/Resources/GameUiWidget.prefab";
    private const int Width = 1080;
    private const int Height = 1920;

    [Serializable]
    private sealed class Report
    {
        public string generatedUtc = DateTime.UtcNow.ToString("o");
        public string prefab = PrefabPath;
        public string scope = "Resolved GameUiWidget prefab in an isolated preview scene. Business behaviours disabled; only normal UI rendering and raycasts run. Does not verify runtime listener execution or account flows.";
        public int width = Width;
        public int height = Height;
        public int disabledBusinessBehaviours;
        public int activeGameViewDisplay;
        public int registeredRaycasters;
        public int registeredGraphics;
        public bool positiveControlPassed;
        public bool passed;
        public List<ButtonResult> buttons = new List<ButtonResult>();
        public List<string> errors = new List<string>();
    }

    [Serializable]
    private sealed class ButtonResult
    {
        public string field;
        public string button;
        public string visual;
        public string targetGraphic;
        public float targetGraphicAlpha;
        public bool targetGraphicRaycastTarget;
        public bool targetGraphicCulled;
        public int targetGraphicDepth;
        public bool active;
        public bool interactable;
        public bool visualOwnedByButton;
        public List<SampleResult> samples = new List<SampleResult>();
    }

    [Serializable]
    private sealed class SampleResult
    {
        public Vector2 normalizedPoint;
        public Vector2 screenPoint;
        public string topGraphic;
        public string clickHandler;
        public bool routesToExpectedButton;
    }

    private static readonly Vector2[] SamplePoints =
    {
        new Vector2(0.5f, 0.5f),
        new Vector2(0.2f, 0.5f),
        new Vector2(0.8f, 0.5f),
        new Vector2(0.5f, 0.2f),
        new Vector2(0.5f, 0.8f)
    };

    [MenuItem("Tools/Orchard UI/Validate HUD Input")]
    public static void Validate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("HUD prefab validation requires Edit mode.");

        var report = new Report();
        try
        {
            ValidatePreview(report);
        }
        catch (Exception exception)
        {
            report.errors.Add(exception.ToString());
        }

        report.passed = report.errors.Count == 0;
        string reportPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../Design/OrchardUI/hud-input-validation.json"));
        Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
        File.WriteAllText(reportPath, JsonUtility.ToJson(report, true), new UTF8Encoding(false));
        if (!report.passed)
            throw new InvalidOperationException("[OrchardUI] HUD input validation failed. " + reportPath + "\n" + string.Join("\n", report.errors));
        Debug.Log("[OrchardUI] HUD input validation passed: 3 buttons, 15 visible-area raycasts. " + reportPath);
    }

    private static void ValidatePreview(Report report)
    {
#if BIZZA_REAL_WITHDRAW
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (asset == null) throw new InvalidOperationException("Cannot load " + PrefabPath);

        Scene scene = EditorSceneManager.NewPreviewScene();
        EventSystem previousEventSystem = EventSystem.current;
        RenderTexture target = null;
        var addedRaycasters = new List<BaseRaycaster>();
        try
        {
            // Parent is inactive before instantiation, including for nested prefab overrides.
            var host = NewObject("Disposable HUD Input Validation", scene, true);
            host.SetActive(false);
            var hostRect = (RectTransform)host.transform;
            hostRect.sizeDelta = new Vector2(Width, Height);
            var hostCanvas = host.AddComponent<Canvas>();
            hostCanvas.renderMode = RenderMode.WorldSpace;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, host.transform);
            foreach (var behaviour in instance.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour == null || IsUiBehaviour(behaviour)) continue;
                if (behaviour.enabled) report.disabledBusinessBehaviours++;
                behaviour.enabled = false;
            }
            foreach (var animator in instance.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
            foreach (var animation in instance.GetComponentsInChildren<Animation>(true)) animation.enabled = false;
            foreach (var audio in instance.GetComponentsInChildren<AudioSource>(true)) audio.enabled = false;
            foreach (var camera in instance.GetComponentsInChildren<Camera>(true)) camera.enabled = false;
            foreach (var particles in instance.GetComponentsInChildren<ParticleSystem>(true))
                particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var root = instance.transform as RectTransform;
            if (root == null) throw new InvalidOperationException("HUD root is not a RectTransform.");
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
            root.anchoredPosition3D = Vector3.zero;
            root.localScale = Vector3.one;

            var cameraObject = NewObject("HUD Validation Camera", scene, false);
            var previewCamera = cameraObject.AddComponent<Camera>();
            previewCamera.enabled = false;
            previewCamera.scene = scene;
            previewCamera.cameraType = CameraType.Game;
            previewCamera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(scene);
            previewCamera.orthographic = true;
            previewCamera.orthographicSize = Height * 0.5f;
            previewCamera.aspect = (float)Width / Height;
            previewCamera.nearClipPlane = 0.1f;
            previewCamera.farClipPlane = 2000f;
            previewCamera.transform.position = new Vector3(0f, 0f, -1000f);
            target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            target.Create();
            previewCamera.targetTexture = target;
            foreach (var canvas in host.GetComponentsInChildren<Canvas>(true))
            {
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = previewCamera;
            }

            var eventObject = NewObject("HUD Validation EventSystem", scene, false);
            var eventSystem = eventObject.AddComponent<EventSystem>();
            eventSystem.enabled = false; // RaycastAll needs no input module or automatic input processing.
            // A disposable standard Button proves this harness can receive normal UI hits.
            var control = NewObject("Validation Positive Control", scene, true);
            control.transform.SetParent(instance.transform, false);
            var controlRect = (RectTransform)control.transform;
            controlRect.anchorMin = controlRect.anchorMax = new Vector2(0.5f, 0.5f);
            controlRect.sizeDelta = new Vector2(64f, 64f);
            controlRect.anchoredPosition = new Vector2(0f, -400f);
            var controlImage = control.AddComponent<Image>();
            var controlButton = control.AddComponent<Button>();
            controlButton.targetGraphic = controlImage;
            host.SetActive(true);
            // BaseRaycaster has no ExecuteAlways, so Edit mode does not run its
            // registration callback. Register only this fixture's authored raycasters.
            foreach (var raycaster in instance.GetComponentsInChildren<GraphicRaycaster>())
            {
                if (!raycaster.isActiveAndEnabled || RaycasterManager.GetRaycasters().Contains(raycaster)) continue;
                RaycasterManager.GetRaycasters().Add(raycaster);
                addedRaycasters.Add(raycaster);
            }
            Canvas.ForceUpdateCanvases();
            foreach (var rect in instance.GetComponentsInChildren<RectTransform>())
                if (rect.GetComponent<LayoutGroup>() != null) LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
            foreach (var text in instance.GetComponentsInChildren<TMP_Text>()) text.ForceMeshUpdate(true, true);
            foreach (var graphic in instance.GetComponentsInChildren<Graphic>())
            {
                if (!graphic.isActiveAndEnabled) continue;
                graphic.SetAllDirty();
                graphic.Rebuild(CanvasUpdate.PreRender);
            }
            Canvas.ForceUpdateCanvases();
            previewCamera.Render();
            Canvas.ForceUpdateCanvases();
            report.activeGameViewDisplay = Display.activeEditorGameViewTarget;
            report.registeredRaycasters = RaycasterManager.GetRaycasters().Count;
            report.registeredGraphics = GraphicRegistry.GetRaycastableGraphicsForCanvas(instance.GetComponent<Canvas>()).Count;
            var controlPointer = new PointerEventData(eventSystem)
            {
                position = RectTransformUtility.WorldToScreenPoint(previewCamera, controlRect.position)
            };
            var controlHits = new List<RaycastResult>();
            eventSystem.RaycastAll(controlPointer, controlHits);
            foreach (var hit in controlHits)
            {
                if (hit.gameObject == null || hit.gameObject.scene != scene) continue;
                report.positiveControlPassed = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit.gameObject) == control;
                break;
            }
            if (!report.positiveControlPassed)
                throw new InvalidOperationException("Validation fixture cannot hit its known-good standard Button; no HUD raycast conclusion can be drawn.");
            Object.DestroyImmediate(control);
            Canvas.ForceUpdateCanvases();

            var bar = instance.GetComponentInChildren<CurrencyBar>(true);
            if (bar == null) throw new InvalidOperationException("Resolved HUD has no CurrencyBar.");
            Transform goldGroup = bar.transform.Find("CurrentGroup/GoldGroup");
            Transform dollarGroup = bar.transform.Find("CurrentGroup/DollarGroup");
            InspectButton(report, scene, eventSystem, previewCamera, "coinBtn", bar.coinBtn, FindVisual(goldGroup, "ButtonView"));
            InspectButton(report, scene, eventSystem, previewCamera, "dollarBtn", bar.dollarBtn, FindVisual(dollarGroup, "ButtonView"));
            InspectButton(report, scene, eventSystem, previewCamera, "settingBtn", bar.settingBtn,
                bar.settingBtn != null ? bar.settingBtn.targetGraphic : null);
        }
        finally
        {
            foreach (var raycaster in addedRaycasters) RaycasterManager.GetRaycasters().Remove(raycaster);
            EditorSceneManager.ClosePreviewScene(scene);
            EventSystem.current = previousEventSystem;
            if (target != null) { target.Release(); Object.DestroyImmediate(target); }
        }
#else
        throw new InvalidOperationException("HUD validation requires this project's BIZZA_REAL_WITHDRAW configuration.");
#endif
    }

    private static void InspectButton(Report report, Scene scene, EventSystem eventSystem, Camera camera,
        string field, Button button, Graphic visual)
    {
        var result = new ButtonResult { field = field };
        report.buttons.Add(result);
        if (button == null || visual == null)
        {
            report.errors.Add(field + ": missing Button reference or authored visible graphic.");
            return;
        }

        result.button = ObjectPath(button.transform);
        result.visual = ObjectPath(visual.transform);
        result.targetGraphic = button.targetGraphic != null ? ObjectPath(button.targetGraphic.transform) : string.Empty;
        result.targetGraphicAlpha = button.targetGraphic != null ? button.targetGraphic.color.a : 0f;
        result.targetGraphicRaycastTarget = button.targetGraphic != null && button.targetGraphic.raycastTarget;
        result.targetGraphicCulled = button.targetGraphic != null && button.targetGraphic.canvasRenderer.cull;
        result.targetGraphicDepth = button.targetGraphic != null ? button.targetGraphic.depth : -1;
        result.active = button.isActiveAndEnabled;
        result.interactable = button.IsInteractable();
        result.visualOwnedByButton = visual.transform == button.transform || visual.transform.IsChildOf(button.transform);
        if (!result.active || !result.interactable) report.errors.Add(field + ": Button is inactive or not interactable.");
        if (button is BizzaButton bizzaButton && !bizzaButton.interactable) report.errors.Add(field + ": BizzaButton interactable is false.");
        if (!visual.isActiveAndEnabled || visual.color.a <= 0.01f) report.errors.Add(field + ": authored visual is not visible.");
        if (!result.visualOwnedByButton) report.errors.Add(field + ": visible graphic is outside the Button hierarchy.");

        var hits = new List<RaycastResult>();
        var pointer = new PointerEventData(eventSystem);
        Rect rect = visual.rectTransform.rect;
        foreach (Vector2 samplePoint in SamplePoints)
        {
            Vector3 localPoint = new Vector3(Mathf.Lerp(rect.xMin, rect.xMax, samplePoint.x), Mathf.Lerp(rect.yMin, rect.yMax, samplePoint.y), 0f);
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(camera, visual.rectTransform.TransformPoint(localPoint));
            pointer.position = screenPoint;
            hits.Clear();
            eventSystem.RaycastAll(pointer, hits);
            GameObject hit = null;
            foreach (var candidate in hits)
            {
                if (candidate.gameObject == null || candidate.gameObject.scene != scene) continue;
                hit = candidate.gameObject;
                break;
            }
            GameObject handler = hit != null ? ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit) : null;
            bool routesCorrectly = handler != null && handler.GetComponent<Button>() == button;
            result.samples.Add(new SampleResult
            {
                normalizedPoint = samplePoint,
                screenPoint = screenPoint,
                topGraphic = hit != null ? ObjectPath(hit.transform) : string.Empty,
                clickHandler = handler != null ? ObjectPath(handler.transform) : string.Empty,
                routesToExpectedButton = routesCorrectly
            });
            if (!routesCorrectly) report.errors.Add(field + ": visible point " + samplePoint + " routes to " + (handler != null ? ObjectPath(handler.transform) : "no Button") + ".");
        }
    }

    private static Graphic FindVisual(Transform container, string name)
    {
        if (container == null) return null;
        foreach (var graphic in container.GetComponentsInChildren<Graphic>(true))
            if (graphic.name == name) return graphic;
        return null;
    }

    private static bool IsUiBehaviour(MonoBehaviour behaviour)
    {
        return behaviour is Graphic || behaviour is BaseMeshEffect || behaviour is LayoutGroup ||
               behaviour is ContentSizeFitter || behaviour is AspectRatioFitter || behaviour is CanvasScaler ||
               behaviour is Mask || behaviour is RectMask2D || behaviour is ScrollRect ||
               behaviour is Selectable || behaviour is TMP_SubMeshUI || behaviour is GraphicRaycaster;
    }

    private static GameObject NewObject(string name, Scene scene, bool rectTransform)
    {
        var instance = rectTransform ? new GameObject(name, typeof(RectTransform)) : new GameObject(name);
        instance.hideFlags = HideFlags.HideAndDontSave;
        SceneManager.MoveGameObjectToScene(instance, scene);
        return instance;
    }

    private static string ObjectPath(Transform target)
    {
        string path = target.name;
        while (target.parent != null) { target = target.parent; path = target.name + "/" + path; }
        return path;
    }
}
