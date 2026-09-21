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

// Explicit one-off asset preview; never opens UI, starts Play or touches game data.
[InitializeOnLoad]
public static class AdBadgeValidation
{
    const string DirectoryPath = "Design/AdBadgeFix-20260918";
    const string PrefabPath = "Assets/FruitsHarvest/Resources/Original/res/local/coreplay/CorePlayUI.prefab";
    static double nextPoll;
    [Serializable] class Result { public string kind = "Unity static prefab badge preview; no gameplay or ad flow executed"; public string error; public List<Entry> badges = new List<Entry>(); }
    [Serializable] class Entry { public string owner, image; public int activeImages, activeTexts; public bool textOverflow; }
    static AdBadgeValidation() { EditorApplication.update += Tick; }
    static void Tick()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.timeSinceStartup < nextPoll) return;
        nextPoll = EditorApplication.timeSinceStartup + 1;
        string request = DirectoryPath + "/preview.command";
        if (!File.Exists(request)) return;
        string suffix = File.ReadAllText(request).Trim();
        File.Delete(request);
        if (suffix != "before" && suffix != "after") return;
        Directory.CreateDirectory(DirectoryPath);
        var result = new Result();
        try
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            foreach (var prop in source.GetComponentsInChildren<CorePlayItemBtn>(true))
            {
                var serialized = new SerializedObject(prop);
                var badge = serialized.FindProperty("m_WatchAD").objectReferenceValue as GameObject;
                if (badge != null) result.badges.Add(Render(badge, prop.name, suffix));
            }
        }
        catch (Exception e) { result.error = e.ToString(); }
        File.WriteAllText(DirectoryPath + "/" + suffix + "-validation.json", JsonUtility.ToJson(result, true));
    }
    static Entry Render(GameObject source, string owner, string suffix)
    {
        var result = new Entry { owner = owner, image = Path.GetFullPath(DirectoryPath + "/" + suffix + "-" + owner + ".png") };
        var scene = EditorSceneManager.NewPreviewScene();
        RenderTexture render = null;
        Texture2D pixels = null;
        var old = RenderTexture.active;
        try
        {
            var stage = new GameObject("Static badge preview", typeof(RectTransform), typeof(Canvas));
            SceneManager.MoveGameObjectToScene(stage, scene);
            stage.SetActive(false);
            var canvas = stage.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            ((RectTransform)stage.transform).sizeDelta = new Vector2(256, 128);
            var instance = Object.Instantiate(source, stage.transform, false);
            instance.SetActive(true);
            foreach (var b in instance.GetComponentsInChildren<MonoBehaviour>(true))
                if (b != null && !(b is Graphic) && !(b is BaseMeshEffect)) b.enabled = false;
            foreach (var a in instance.GetComponentsInChildren<Animation>(true)) a.enabled = false;
            foreach (var p in instance.GetComponentsInChildren<ParticleSystem>(true)) p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var boundsImage = instance.GetComponentInChildren<Image>(true);
            if (boundsImage == null) throw new InvalidOperationException("Badge has no background Image");
            var cameraObject = new GameObject("Static badge camera", typeof(Camera));
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var camera = cameraObject.GetComponent<Camera>();
            camera.enabled = false; camera.scene = scene;
            camera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(scene);
            camera.orthographic = true; camera.orthographicSize = 64; camera.aspect = 2;
            camera.nearClipPlane = .1f; camera.farClipPlane = 2000;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.91f, .86f, .72f, 1);
            var center = boundsImage.transform.position;
            camera.transform.position = new Vector3(center.x, center.y, -1000);
            camera.allowHDR = false; camera.allowMSAA = false;
            canvas.worldCamera = camera;
            render = new RenderTexture(768, 384, 24, RenderTextureFormat.ARGB32); render.Create(); camera.targetTexture = render;
            stage.SetActive(true);
            Canvas.ForceUpdateCanvases();
            foreach (var text in instance.GetComponentsInChildren<TMP_Text>(true))
                if (text.isActiveAndEnabled) { text.ForceMeshUpdate(true, true); result.activeTexts++; result.textOverflow |= text.isTextOverflowing || text.isTextTruncated; }
            foreach (var graphic in instance.GetComponentsInChildren<Graphic>(true))
                if (graphic.isActiveAndEnabled) { graphic.SetAllDirty(); graphic.Rebuild(CanvasUpdate.PreRender); if (graphic is Image) result.activeImages++; }
            Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = render;
            pixels = new Texture2D(768, 384, TextureFormat.RGBA32, false);
            pixels.ReadPixels(new Rect(0, 0, 768, 384), 0, 0); pixels.Apply(false, false);
            File.WriteAllBytes(result.image, pixels.EncodeToPNG());
            return result;
        }
        finally
        {
            RenderTexture.active = old;
            if (pixels != null) Object.DestroyImmediate(pixels);
            if (render != null) { render.Release(); Object.DestroyImmediate(render); }
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }
}
