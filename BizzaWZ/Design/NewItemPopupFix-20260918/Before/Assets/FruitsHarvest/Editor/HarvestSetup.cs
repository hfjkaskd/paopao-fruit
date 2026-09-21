using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>One-time asset authoring, never used as an Editor-only gameplay fallback.</summary>
[InitializeOnLoad]
public static class HarvestSetup
{
    private const string Root = "Assets/FruitsHarvest/";
    private const string Original = Root + "Resources/Original/";
    static HarvestSetup() { EditorApplication.delayCall += RunRequested; }
    private static void RunRequested()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
        if (!File.Exists("../Tools/Migration/setup.request")) return;
        try
        {
            if (File.ReadAllText("../Tools/Migration/setup.request").Trim() == "restore-original-props")
                RestoreOriginalPropPresentation();
            else Build();
            File.Delete("../Tools/Migration/setup.request");
        }
        catch (Exception e) { Debug.LogException(e); }
    }

    [MenuItem("Tools/Fruits Harvest/Configure Integration Assets")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first");
        ConfigureGameplay();
        ConfigureFrameworkHost();
        var scene = EditorSceneManager.OpenScene(Root + "Editor/ReferenceGame.unity", OpenSceneMode.Additive);
        var host = new GameObject("HarvestRoot", typeof(RectTransform));
        SceneManager.MoveGameObjectToScene(host, scene);
        var rect = (RectTransform)host.transform;
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.sizeDelta = Vector2.zero;
        var canvas = scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<Canvas>(true)).First(x=>x.name=="Canvas");
        canvas.transform.SetParent(host.transform,false);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = false;
        canvas.worldCamera = null;
        canvas.transform.localScale=Vector3.one;
        var canvasRect=(RectTransform)canvas.transform;
        canvasRect.anchorMin=Vector2.zero; canvasRect.anchorMax=Vector2.one;
        canvasRect.offsetMin=Vector2.zero; canvasRect.offsetMax=Vector2.zero;
        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null) UnityEngine.Object.DestroyImmediate(scaler);
        foreach(var node in host.GetComponentsInChildren<Transform>(true)) GameObjectUtility.RemoveMonoBehavioursWithMissingScript(node.gameObject);
        var manager = host.GetComponentInChildren<MgrUI>(true);
        if(manager==null)
        {
            manager=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<MgrUI>(true)).FirstOrDefault();
            if(manager!=null) manager.transform.SetParent(canvas.transform,false);
            else manager=host.GetComponentsInChildren<Transform>(true).First(x=>x.name=="MgrUI").gameObject.AddComponent<MgrUI>();
        }
        var global = host.GetComponentInChildren<MgrGlobalUI>(true);
        // A nested layout inherits the framework root Canvas. A standalone root Canvas
        // recalculates a zero editor scale while its prefab has no render target.
        var raycaster=canvas.GetComponent<GraphicRaycaster>();
        if(raycaster!=null) UnityEngine.Object.DestroyImmediate(raycaster);
        UnityEngine.Object.DestroyImmediate(canvas);
        canvasRect.localScale=Vector3.one;
        canvasRect.anchorMin=Vector2.zero; canvasRect.anchorMax=Vector2.one;
        canvasRect.offsetMin=Vector2.zero; canvasRect.offsetMax=Vector2.zero;
        var tween=scene.GetRootGameObjects().FirstOrDefault(x=>x.name=="MyTween");
        if(tween!=null) tween.transform.SetParent(host.transform,false);
        foreach(var c in host.GetComponentsInChildren<Camera>(true)) UnityEngine.Object.DestroyImmediate(c.gameObject);
        foreach(var c in host.GetComponentsInChildren<AudioListener>(true)) UnityEngine.Object.DestroyImmediate(c);
        foreach(var c in host.GetComponentsInChildren<Canvas>(true))
        {
            if(c.name=="InputMask") c.sortingOrder=-400;
            else if(c.overrideSorting) c.sortingOrder=-500;
        }
        var component = host.AddComponent<HarvestRoot>();
        var gameplayInput=host.AddComponent<CanvasGroup>();
        gameplayInput.interactable=false;
        var so = new SerializedObject(component);
        so.FindProperty("manager").objectReferenceValue=manager;
        so.FindProperty("global").objectReferenceValue=global;
        so.FindProperty("gameplayInput").objectReferenceValue=gameplayInput;
        so.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAsset(host,Root+"Resources/HarvestRoot.prefab");
        EditorSceneManager.CloseScene(scene,true);
        ConfigureProps();
        ConfigureLoading();
        ConfigureLosePanel();
        ConfigureNetworkPrompt();
        AssetDatabase.SaveAssets();
        Debug.Log("[HarvestSetup] Integration prefabs and prop configuration saved");
    }

    private static void ConfigureGameplay()
    {
        string path=Original+"res/local/coreplay/CorePlayUI.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        var ui=root.GetComponent<CorePlay.CorePlayUI>();
        var so=new SerializedObject(ui);
        foreach(var name in new[]{"m_CommonCoinBtn","m_SettingBtn"})
        {
            var field=so.FindProperty(name);
            var value=field.objectReferenceValue;
            GameObject go=value as GameObject;
            if(value is Component c) go=c.gameObject;
            field.objectReferenceValue=null;
            if(go!=null) UnityEngine.Object.DestroyImmediate(go);
        }
        var levelLabels=so.FindProperty("m_LevelTextGos");
        for(int i=0;i<levelLabels.arraySize;i++)
        {
            var go=levelLabels.GetArrayElementAtIndex(i).objectReferenceValue as GameObject;
            if(go!=null) UnityEngine.Object.DestroyImmediate(go);
        }
        levelLabels.arraySize=0;
        so.FindProperty("m_LevelTexts").arraySize=0;
        so.ApplyModifiedPropertiesWithoutUndo();
        var coin=root.GetComponentsInChildren<Transform>(true).FirstOrDefault(x=>x.name=="CommonCoinBtn");
        if(coin!=null) UnityEngine.Object.DestroyImmediate(coin.gameObject);
        ConfigureOriginalPropButtons(root);
        EnsureButtons(root);
        PrefabUtility.SaveAsPrefabAsset(root,path);
        PrefabUtility.UnloadPrefabContents(root);
        foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{Original}))
        {
            string other=AssetDatabase.GUIDToAssetPath(guid);
            if(other==path) continue;
            var prefab=PrefabUtility.LoadPrefabContents(other);
            if(EnsureButtons(prefab)) PrefabUtility.SaveAsPrefabAsset(prefab,other);
            PrefabUtility.UnloadPrefabContents(prefab);
        }
    }

    private static void ConfigureFrameworkHost()
    {
        const string path="Assets/BizzaWZ/Common/UI/GamePanel/RealGamePanel.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        var page=root.GetComponent<RealGamePanel>();
        var canvas=root.GetComponent<Canvas>();
        if(canvas==null) canvas=root.AddComponent<Canvas>();
        canvas.overrideSorting=true; canvas.sortingOrder=-1100;
        if(root.GetComponent<GraphicRaycaster>()==null) root.AddComponent<GraphicRaycaster>();
        page.propsRoot.gameObject.SetActive(false);
        var props=page.propsRoot.GetComponent<Canvas>();
        if(props==null) props=page.propsRoot.gameObject.AddComponent<Canvas>();
        props.overrideSorting=true; props.sortingOrder=-700;
        if(props.GetComponent<GraphicRaycaster>()==null) props.gameObject.AddComponent<GraphicRaycaster>();
        PrefabUtility.SaveAsPrefabAsset(root,path);
        PrefabUtility.UnloadPrefabContents(root);
        const string widgetPath="Assets/BizzaWZ/Final/Real/UI/GamePanel/Resources/Resources/GameUiWidget.prefab";
        var widget=PrefabUtility.LoadPrefabContents(widgetPath);
        var widgetCanvas=widget.GetComponent<Canvas>();
        if(widgetCanvas==null) widgetCanvas=widget.AddComponent<Canvas>();
        widgetCanvas.overrideSorting=true; widgetCanvas.sortingOrder=-200;
        if(widget.GetComponent<GraphicRaycaster>()==null) widget.AddComponent<GraphicRaycaster>();
        PrefabUtility.SaveAsPrefabAsset(widget,widgetPath);
        PrefabUtility.UnloadPrefabContents(widget);
    }

    [MenuItem("Tools/Fruits Harvest/Restore Original Prop Presentation")]
    public static void RestoreOriginalPropPresentation()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first");
        const string gameplayPath = Original + "res/local/coreplay/CorePlayUI.prefab";
        var gameplay = PrefabUtility.LoadPrefabContents(gameplayPath);
        ConfigureOriginalPropButtons(gameplay);
        PrefabUtility.SaveAsPrefabAsset(gameplay, gameplayPath);
        PrefabUtility.UnloadPrefabContents(gameplay);
        const string panelPath = "Assets/BizzaWZ/Common/UI/GamePanel/RealGamePanel.prefab";
        var panel = PrefabUtility.LoadPrefabContents(panelPath);
        panel.GetComponent<RealGamePanel>().propsRoot.gameObject.SetActive(false);
        PrefabUtility.SaveAsPrefabAsset(panel, panelPath);
        PrefabUtility.UnloadPrefabContents(panel);
        AssetDatabase.SaveAssets();
        Debug.Log("[HarvestSetup] Original prop presentation restored");
    }

    private static void ConfigureOriginalPropButtons(GameObject root)
    {
        foreach (var view in root.GetComponentsInChildren<CorePlayItemBtn>(true))
        {
            var entry = view.GetComponent<UIPropEntry>();
            if (entry == null) entry = view.gameObject.AddComponent<UIPropEntry>();
            var input = view.GetComponent<InputMono>();
            if (input == null) input = view.gameObject.AddComponent<InputMono>();
            var viewSo = new SerializedObject(view);
            var entrySo = new SerializedObject(entry);
            entrySo.FindProperty("originalPresentation").objectReferenceValue = view;
            entrySo.FindProperty("itemNumTxt").objectReferenceValue = viewSo.FindProperty("m_LeftItemNum").objectReferenceValue;
            entrySo.FindProperty("propIcon").objectReferenceValue = viewSo.FindProperty("m_ItemIcon").objectReferenceValue;
            entrySo.ApplyModifiedPropertiesWithoutUndo();
            viewSo.FindProperty("entry").objectReferenceValue = entry;
            viewSo.FindProperty("input").objectReferenceValue = input;
            var background = viewSo.FindProperty("m_ItemBG").objectReferenceValue as Image;
            var frame = background != null && background.transform.parent != null
                ? background.transform.parent.Find("BG") : null;
            viewSo.FindProperty("outerFrame").objectReferenceValue = frame != null ? frame.GetComponent<Image>() : null;
            viewSo.ApplyModifiedPropertiesWithoutUndo();
            input.targetGraphic = background;
            if (viewSo.FindProperty("m_BtnAnim").objectReferenceValue != null)
            {
                // The expansion button is above the tray's authored +10 canvas layer.
                if (view.GetComponent<Canvas>() == null) view.gameObject.AddComponent<Canvas>();
                var layer = view.GetComponent<Orange.DynamicCanvasLayer>();
                if (layer == null) layer = view.gameObject.AddComponent<Orange.DynamicCanvasLayer>();
                var layerSo = new SerializedObject(layer);
                layerSo.FindProperty("offset").intValue = 11;
                layerSo.FindProperty("addGraphicRaycaster").boolValue = true;
                layerSo.ApplyModifiedPropertiesWithoutUndo();
            }
            if (view.GetComponent<Canvas>() != null && view.GetComponent<GraphicRaycaster>() == null)
                view.gameObject.AddComponent<GraphicRaycaster>();
        }
    }

    private static bool EnsureButtons(GameObject root)
    {
        bool changed=false;
        foreach(var item in root.GetComponentsInChildren<CollectItem>(true))
        {
            // Normal fruit visuals must share hierarchy sorting with the occlusion priority.
            var visual = item.CollectImageRect;
            if (visual != null)
            {
                var visualLayer = visual.GetComponent<Orange.DynamicCanvasLayer>();
                if (visualLayer != null) { UnityEngine.Object.DestroyImmediate(visualLayer); changed = true; }
                var visualRaycaster = visual.GetComponent<GraphicRaycaster>();
                if (visualRaycaster != null) { UnityEngine.Object.DestroyImmediate(visualRaycaster); changed = true; }
                var visualCanvas = visual.GetComponent<Canvas>();
                if (visualCanvas != null) { UnityEngine.Object.DestroyImmediate(visualCanvas); changed = true; }
            }
            if(item.GetComponent<InputMono>()==null) { item.gameObject.AddComponent<InputMono>(); changed=true; }
            if(item.GetComponent<Canvas>()==null) { item.gameObject.AddComponent<Canvas>(); changed=true; }
            if(item.GetComponent<GraphicRaycaster>()==null) { item.gameObject.AddComponent<GraphicRaycaster>(); changed=true; }
            foreach(var layer in item.GetComponentsInChildren<Orange.DynamicCanvasLayer>(true))
            {
                if(layer.GetComponent<Canvas>()==null) { layer.gameObject.AddComponent<Canvas>(); changed=true; }
                if(layer.GetComponent<GraphicRaycaster>()==null) { layer.gameObject.AddComponent<GraphicRaycaster>(); changed=true; }
            }
        }
        foreach(var input in root.GetComponentsInChildren<InputMono>(true))
            if(input.GetComponent<Button>()==null) { input.gameObject.AddComponent<Button>(); changed=true; }
        foreach(var input in root.GetComponentsInChildren<InputMonoNoDrag>(true))
            if(input.GetComponent<Button>()==null) { input.gameObject.AddComponent<Button>(); changed=true; }
        return changed;
    }

    private static void ConfigureProps()
    {
        var cfg=AssetDatabase.LoadAssetAtPath<PropConfigSO>("Assets/BizzaWZ/Common/Resources/Configs/PropConfig.asset");
        var so=new SerializedObject(cfg); var entries=so.FindProperty("propCfgInfos");
        entries.arraySize=4;
        var icons=new[]{"Undo_Normal","Shuffle_Normal","Magic_Normal","Extra_Normal"};
        for(int i=0;i<4;i++)
        {
            var p=entries.GetArrayElementAtIndex(i);
            p.FindPropertyRelative("propType").intValue=24+i;
            p.FindPropertyRelative("unlockFunction").boolValue=true;
            p.FindPropertyRelative("unlockCondition").FindPropertyRelative("unlockLevel").intValue=i+2;
            p.FindPropertyRelative("cancelFunction").boolValue=false;
            // Preserve the framework's configured per-level allowance and newcomer quantity.
            p.FindPropertyRelative("preLimitNum").intValue=i==3 ? 1 : 3;
            p.FindPropertyRelative("newPlayerPropCount").intValue=1;
            var sprite=AssetDatabase.FindAssets(icons[i]+" t:Sprite",new[]{Original}).Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<Sprite>).FirstOrDefault(x=>x!=null);
            if(sprite==null) sprite=AssetDatabase.FindAssets("Icon"+new[]{"Undo","Shuffle","Magic","Extra"}[i]+" t:Sprite",new[]{Original}).Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<Sprite>).FirstOrDefault(x=>x!=null);
            if(sprite==null) throw new InvalidOperationException("Missing original prop icon: "+icons[i]);
            p.FindPropertyRelative("propIcon").objectReferenceValue=sprite;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureLoading()
    {
        // Keep the framework page ID/script and take visual children from the original loading prefab.
        string target="Assets/BizzaWZ/Common/MenuSystem/Common/LoadingPanel/LoadingPanel.prefab";
        string source=AssetDatabase.FindAssets("GameLoading t:Prefab",new[]{Original+"res/local/gameloading"}).Select(AssetDatabase.GUIDToAssetPath).First();
        var page=PrefabUtility.LoadPrefabContents(target);
        foreach(Transform child in page.transform.Cast<Transform>().ToArray())
            if(child.name.StartsWith("GameLoading") || child.name=="HarvestLoadingVisual") UnityEngine.Object.DestroyImmediate(child.gameObject);
        // The source framework uses the built-in pipeline; its camera retains an orphaned URP component.
        foreach(var t in page.GetComponentsInChildren<Transform>(true)) GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
        var visual=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(source),page.transform,false);
        visual.name="HarvestLoadingVisual";
        foreach(var t in visual.GetComponentsInChildren<Transform>(true)) GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
        foreach(var t in visual.GetComponentsInChildren<Transform>(true))
            if(t.name.IndexOf("Start",StringComparison.OrdinalIgnoreCase)>=0 && t.GetComponent<Button>()!=null || t.name.IndexOf("Privacy",StringComparison.OrdinalIgnoreCase)>=0) t.gameObject.SetActive(false);
        var panel=page.GetComponent<LoadingPanel>();
        var progress=visual.GetComponentInChildren<CommonProgressBar>(true);
        if(progress==null) throw new InvalidOperationException("Original loading progress component missing");
        progress.gameObject.SetActive(true);
        foreach(Transform child in page.transform) if(child!=visual.transform && child.gameObject!=panel.CameraObj) child.gameObject.SetActive(false);
        panel.harvestProgress=progress; panel.progressBar=null; panel.busRect=null;
        var text=visual.GetComponentsInChildren<TMPro.TMP_Text>(true).FirstOrDefault(x=>x.text.Contains("%"));
        if(text!=null) panel.progressTxt=text;
        PrefabUtility.SaveAsPrefabAsset(page,target);
        PrefabUtility.UnloadPrefabContents(page);
    }

    private static void ConfigureNetworkPrompt()
    {
        const string path="Assets/BizzaWZ/Final/FunctionTools/CommonPublic/UI/CommonConfirmTipsPanel.prefab";
        const string art="Assets/BizzaWZ/Common/BizzaGame/Z_ReplaceAssets/UI_Frame/Common/";
        var page=PrefabUtility.LoadPrefabContents(path);
        var font=AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(AssetDatabase.GUIDToAssetPath("7cc23ba99c7035347900a2e939f7ab60"));
        foreach(var text in page.GetComponentsInChildren<TMPro.TMP_Text>(true))
        {
            if(text.font==null) text.font=font;
            text.raycastTarget=false;
            text.color=text.GetComponentInParent<BizzaButton>()!=null ? Color.white : new Color(0.22f,0.12f,0.38f);
        }
        foreach(var img in page.GetComponentsInChildren<Image>(true))
        {
            if(img.name=="Image (2)") { img.enabled=false; continue; }
            if(img.name=="PageMask") continue;
            img.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(art+(img.name=="ConfirmBtn" ? "Btn_Green.png" : "Common_Frame.png"));
            img.type=Image.Type.Simple;
        }
        PrefabUtility.SaveAsPrefabAsset(page,path,out bool saved);
        PrefabUtility.UnloadPrefabContents(page);
        if(!saved) throw new InvalidOperationException("Could not save network prompt");
    }

    private static void ConfigureLosePanel()
    {
        const string path="Assets/BizzaWZ/Common/BizzaGame/LosePanel/LosePanel.prefab";
        const string art="Assets/BizzaWZ/Common/BizzaGame/Z_ReplaceAssets/UI_Frame/Common/";
        var page=PrefabUtility.LoadPrefabContents(path);
        foreach(var text in page.GetComponentsInChildren<TMPro.TMP_Text>(true))
            if(text.GetComponentInParent<BizzaButton>()==null) text.color=new Color(0.22f,0.12f,0.38f);
        foreach(var img in page.GetComponentsInChildren<Image>(true))
        {
            if(img.name=="bg")
            {
                img.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(art+"Common_Frame.png");
                img.type=Image.Type.Sliced;
            }
            else if(img.name=="Image")
            {
                img.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(art+"Common_Title.png");
                img.type=Image.Type.Sliced;
                img.rectTransform.anchoredPosition=new Vector2(0,370);
                img.rectTransform.sizeDelta=new Vector2(820,180);
                var title=img.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if(title==null)
                {
                    var source=page.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true).First(x=>x.name=="PanelLoseHint");
                    title=UnityEngine.Object.Instantiate(source,img.transform,false);
                    title.name="Title";
                }
                title.gameObject.SetActive(true);
                title.rectTransform.anchorMin=Vector2.zero; title.rectTransform.anchorMax=Vector2.one;
                title.rectTransform.offsetMin=new Vector2(20,10); title.rectTransform.offsetMax=new Vector2(-20,-10);
                title.fontSize=60; title.enableAutoSizing=true; title.fontSizeMin=30; title.fontSizeMax=60;
                title.color=new Color(0.22f,0.12f,0.38f); title.raycastTarget=false;
                title.GetComponent<UILanguageLabel>().key="Harvest_Lose_Title";
            }
        }
        PrefabUtility.SaveAsPrefabAsset(page,path,out bool saved);
        PrefabUtility.UnloadPrefabContents(page);
        if(!saved) throw new InvalidOperationException("Could not save LosePanel");
    }
}
