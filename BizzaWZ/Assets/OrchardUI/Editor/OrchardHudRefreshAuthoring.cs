#if BIZZA_REAL_WITHDRAW
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Prefab authoring and disposable static previews; never enters Play mode or builds a player.
public static class OrchardHudRefreshAuthoring
{
    const string Widget = "Assets/BizzaWZ/Final/Real/UI/GamePanel/Resources/Resources/GameUiWidget.prefab";
    const string Core = "Assets/FruitsHarvest/Resources/Original/res/local/coreplay/CorePlayUI.prefab";
    const string Atlas = "Assets/OrchardUI/Art/HudRefreshAtlas.png";
    const string Output = "Design/HudRefresh-20261008/";

    public static void Apply()
    {
        ImportAtlas();
        var root = PrefabUtility.LoadPrefabContents(Widget);
        try
        {
            var bar = root.GetComponentInChildren<CurrencyBar>(true);
            var originalCoin = bar.coinBtn; var originalDollar = bar.dollarBtn; var originalSetting = bar.settingBtn;
            var top = At(root,"CurrencyBar"); Rect(top,0,-153,1240,200,.5f,1);
            var level = At(root,"CurrencyBar/Image"); Rect(level,-506,0,206,200); Paint(level,"HudLevel");
            // Center the number on the circular plate, excluding the asymmetric leaves.
            Rect(bar.curLevelTxt.transform,14,-17,117,83); Text(bar.curLevelTxt,61,42,false);
            var settings = At(root,"CurrencyBar/PauseButton"); Rect(settings,516,0,194,175); Paint(settings,"HudSettings");
            At(root,"CurrencyBar/PauseButton/Icon").GetComponent<Image>().enabled=false;
            bar.settingBtn.targetGraphic=settings.GetComponent<Image>();
            var group = At(root,"CurrencyBar/CurrentGroup"); Rect(group,0,0,786,166);
            var layout=group.GetComponent<HorizontalLayoutGroup>(); layout.enabled=true; layout.spacing=30;
            layout.padding=new RectOffset(); layout.childAlignment=TextAnchor.MiddleCenter;
            layout.childControlWidth=layout.childControlHeight=false; layout.childForceExpandWidth=layout.childForceExpandHeight=false;
            ConfigureCounter(root,"GoldGroup","RealBtn","CoinBox","BG (1)",bar.coinTxt,bar.coinImg,true);
            ConfigureCounter(root,"DollarGroup","FakeBtn","DollarBox","BG",bar.dollarTxt,bar.dollarImg,false);
            Rect(bar.addCoinTxt.transform,55,-105,260,48); Rect(bar.addDollarTxt.transform,55,-105,260,48);
            var gainOutline=AssetDatabase.LoadAssetAtPath<Material>("Assets/OrchardUI/Generated/7cc23ba99c7035347900a2e939f7ab60-Title.mat");
            if(gainOutline==null) throw new InvalidOperationException("Missing currency gain text outline material.");
            foreach(var gain in new[]{bar.addCoinTxt,bar.addDollarTxt})
            {
                Text(gain,42,26,true);
                gain.fontSharedMaterial=gainOutline;
            }
            if(bar.coinBtn!=originalCoin || bar.dollarBtn!=originalDollar || bar.settingBtn!=originalSetting) throw new InvalidOperationException("HUD Button bindings changed.");

            var slot=At(root,"SlotEnter"); Rect(slot,0,222,224,220,.125f,0);
            Rect(At(root,"SlotEnter/Content/Icon"),0,11,224,179);
            var slotButton=slot.GetComponent<Button>(); slotButton.targetGraphic=At(root,"SlotEnter/Content/Icon").GetComponent<Image>();
            slotButton.targetGraphic.raycastTarget=true;
            var gift=root.transform.Find("DailyMissionItem") ?? At(root,"OrchardGiftFit/DailyMissionItem");
            var fit=root.transform.Find("OrchardGiftFit");
            if(fit==null) { fit=new GameObject("OrchardGiftFit",typeof(RectTransform)).transform; fit.SetParent(root.transform,false); }
            Rect(fit,0,222,224,220,.875f,0); gift.SetParent(fit,false); Rect(gift,0,0,224,220);
            var badge=gift.GetComponentInChildren<BadgeShow>(true);
            Rect(badge.transform,0,0,224,220); Rect(badge.badgeImg.transform,0,8,220,220);
            if(badge.badges.Count!=7) throw new InvalidOperationException("Milestone badge count changed.");
            for(int i=0;i<badge.badges.Count;i++)
            {
                string path=$"Assets/OrchardUI/Art/MilestoneBadges/OrchardBadge{i+1:00}.png";
                badge.badges[i]=AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if(badge.badges[i]==null) throw new InvalidOperationException("Missing milestone badge: "+path);
            }
            badge.badgeImg.sprite=badge.badges[0]; badge.badgeImg.type=Image.Type.Simple;
            badge.badgeImg.preserveAspect=true; badge.badgeImg.color=Color.white; badge.badgeImg.enabled=true;
            var badgePlate=At(root,"OrchardGiftFit/DailyMissionItem/Badge/Image (3)");
            Rect(badgePlate,0,0,224,220); badgePlate.SetAsFirstSibling();
            var badgePlateImage=badgePlate.GetComponent<Image>();
            badgePlateImage.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/OrchardUI/Art/HudMilestoneEntryPlate.png");
            if(badgePlateImage.sprite==null) throw new InvalidOperationException("Missing badge button plate.");
            badgePlateImage.type=Image.Type.Simple; badgePlateImage.preserveAspect=true;
            badgePlateImage.color=Color.white; badgePlateImage.raycastTarget=true; badgePlateImage.enabled=true;
            var amountBox=badge.transform.Find("BadgeAmountBox");
            if(amountBox==null)
            {
                var go=new GameObject("BadgeAmountBox",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
                go.layer=5; go.transform.SetParent(badge.transform,false); amountBox=go.transform;
            }
            Rect(amountBox,0,-108,216,72);
            var amountImage=amountBox.GetComponent<Image>();
            amountImage.sprite=Sprite("HudBalance"); amountImage.type=Image.Type.Simple;
            amountImage.preserveAspect=false; amountImage.pixelsPerUnitMultiplier=1;
            amountImage.color=Color.white; amountImage.raycastTarget=false; amountImage.enabled=true;
            amountBox.SetAsLastSibling(); badge.priceTxt.transform.SetAsLastSibling();
            Rect(badge.priceTxt.transform,0,-108,200,56); Text(badge.priceTxt,52,40,false);
            var amountFont=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/OrchardUI/Fonts/Fidelity/Baloo2 SemiBold SDF.asset");
            if(amountFont==null) throw new InvalidOperationException("Missing badge amount font.");
            badge.priceTxt.font=amountFont; badge.priceTxt.fontSharedMaterial=amountFont.material;
            badge.priceTxt.fontStyle=FontStyles.Normal;
            badge.GetComponent<Button>().targetGraphic=badgePlateImage; badge.badgeImg.raycastTarget=false;
            var daily=At(root,"OrchardGiftFit/DailyMissionItem/DailyMission"); Rect(daily,0,0,224,220);
            var dailyImage=At(root,"OrchardGiftFit/DailyMissionItem/DailyMission/Image (1)");
            Paint(dailyImage,"HudGift"); Rect(dailyImage,0,0,232,208); daily.GetComponent<Button>().targetGraphic=dailyImage.GetComponent<Image>();
            At(root,"OrchardGiftFit/DailyMissionItem/DailyMission/Image (2)").GetComponent<Image>().enabled=false;
            // This regional entry has no amount in its original business flow.
            // Keep its distinct label instead of inventing a $300 daily-mission reward.
            var dailyLabel=daily.Find("OrchardDailyLabel");
            if(dailyLabel==null) { var go=new GameObject("OrchardDailyLabel",typeof(RectTransform)); go.transform.SetParent(daily,false); dailyLabel=go.transform; var tx=go.AddComponent<TextMeshProUGUI>(); tx.font=badge.priceTxt.font; tx.fontSharedMaterial=badge.priceTxt.fontSharedMaterial; }
            Rect(dailyLabel,0,-75,184,43); var dailyText=dailyLabel.GetComponent<TMP_Text>(); Text(dailyText,31,23,true); dailyText.text="Daily";
            var localized=dailyLabel.GetComponent<OrchardLocalizedLabel>() ?? dailyLabel.gameObject.AddComponent<OrchardLocalizedLabel>();
            var copy=new SerializedObject(localized);copy.FindProperty("target").objectReferenceValue=dailyText;copy.FindProperty("english").stringValue="Daily";copy.FindProperty("portuguese").stringValue="Diário";copy.ApplyModifiedPropertiesWithoutUndo();
            ConfigureFit(root,new[]{(RectTransform)top,(RectTransform)slot,(RectTransform)fit});
            PrefabUtility.SaveAsPrefabAsset(root,Widget);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        root=PrefabUtility.LoadPrefabContents(Core);
        try
        {
            var items=new RectTransform[3]; var names=new[]{"Undo","Magic","Shuffle"};
            for(int i=0;i<3;i++) { var t=At(root,"UI/Bottom/"+names[i]); Rect(t,0,222,212,219,.3125f+i*.1875f,0); items[i]=(RectTransform)t; }
            ConfigureFit(root,items);
            PrefabUtility.SaveAsPrefabAsset(root,Core);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
        RenderBatch();
    }

    static void ConfigureCounter(GameObject root,string groupName,string btnName,string boxName,string bgName,TMP_Text amount,Image icon,bool coin)
    {
        string p="CurrencyBar/CurrentGroup/"+groupName;
        var g=At(root,p); Rect(g,0,0,378,166);
        var box=At(root,p+"/"+btnName+"/"+boxName); Rect(box,0,0,378,166);
        var bg=At(root,p+"/"+btnName+"/"+boxName+"/"+bgName); Rect(bg,0,0,378,166); Paint(bg,"HudBalance");
        var image=bg.GetComponent<Image>(); image.type=Image.Type.Sliced; image.pixelsPerUnitMultiplier=1.3f; image.preserveAspect=false;
        Rect(icon.transform,-122,0,120,120); icon.preserveAspect=true;
        Rect(amount.transform,56,35,224,62); Text(amount,49,28,false);
        var action=At(root,p+"/"+btnName+"/ButtonView"); Rect(action,55,-36,220,75); Paint(action,"HudAction");
        var btn=At(root,p+"/"+btnName).GetComponent<Button>(); btn.targetGraphic=action.GetComponent<Image>();
        var label=action.Find("Text (TMP)").GetComponent<TMP_Text>();
        Rect(label.transform,coin?18:0,0,coin?158:200,57); Text(label,coin?32:34,22,true);
        if(coin) { var approx=action.Find("Text (TMP) (1)"); Rect(approx,-62,0,32,57); Text(approx.GetComponent<TMP_Text>(),32,24,true); }
        image.raycastTarget=true;
    }

    static void ConfigureFit(GameObject root,RectTransform[] items)
    {
        var layout=root.GetComponent<OrchardReferenceLayout>() ?? root.AddComponent<OrchardReferenceLayout>();
        var so=new SerializedObject(layout); so.FindProperty("referenceSize").vector2Value=new Vector2(1327.5f,2360);
        var list=so.FindProperty("elements"); list.arraySize=items.Length;
        for(int i=0;i<items.Length;i++) { var e=list.GetArrayElementAtIndex(i); e.FindPropertyRelative("rect").objectReferenceValue=items[i]; e.FindPropertyRelative("position").vector2Value=items[i].anchoredPosition; e.FindPropertyRelative("scale").vector3Value=items[i].localScale; }
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    static Transform At(GameObject root,string path) { return root.transform.Find(path) ?? throw new InvalidOperationException("Missing "+path); }
    static void Rect(Transform t,float x,float y,float w,float h,float ax=.5f,float ay=.5f)
    { var r=(RectTransform)t; r.anchorMin=r.anchorMax=new Vector2(ax,ay);r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);r.localScale=Vector3.one; }
    static Sprite Sprite(string name)
    { foreach(var a in AssetDatabase.LoadAllAssetsAtPath(Atlas)) if(a is Sprite s && s.name==name)return s; throw new InvalidOperationException(name); }
    static void Paint(Transform t,string name)
    { var image=t.GetComponent<Image>();image.sprite=Sprite(name);image.color=Color.white;image.enabled=true;image.type=Image.Type.Simple;image.preserveAspect=true;image.raycastTarget=true; }
    static void Text(TMP_Text t,float max,float min,bool white)
    { t.fontSize=max;t.enableAutoSizing=true;t.fontSizeMax=max;t.fontSizeMin=min;t.alignment=TextAlignmentOptions.Center;t.enableWordWrapping=false;t.overflowMode=TextOverflowModes.Ellipsis;t.margin=Vector4.zero;t.color=white?Color.white:new Color32(95,33,9,255);t.raycastTarget=false; }

    public static void RenderBatch()
    {
        Render("preview-16x9",1080,1920);
        Render("preview-20x9",1080,2400);
        File.WriteAllText(Output+"replacement-result.txt","Prefab visual replacement completed. Button components, data bindings and runtime listeners preserved. Static previews only; no gameplay, network, build or device test.");
    }

    public static void Render(string name,int width,int height,string output=Output,Action<GameObject> configure=null,Action<GameObject> inspect=null)
    {
        Directory.CreateDirectory(output);
        var result=OrchardSkinValidation.PreviewPrefab(Core,output+name+".png",core=>
        {
            // Match the production CanvasScaler's height reference rather than
            // stretching the basket into a screenshot-sized reference canvas.
            var cr=(RectTransform)core.transform;cr.anchorMin=cr.anchorMax=new Vector2(.5f,.5f);
            cr.sizeDelta=new Vector2(width*2360f/height,2360);cr.localScale=Vector3.one*(height/2360f);
            foreach(var child in core.GetComponentsInChildren<Transform>(true))
                if(child.name=="Game" || child.name=="Top" || child.name=="EffRoot") child.gameObject.SetActive(false);
            var widget=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Widget),core.scene);
            widget.SetActive(false);widget.transform.SetParent(core.transform,false);
            var wr=(RectTransform)widget.transform;wr.anchorMin=Vector2.zero;wr.anchorMax=Vector2.one;wr.offsetMin=wr.offsetMax=Vector2.zero;
            DisableBusiness(widget);
            var bar=widget.GetComponentInChildren<CurrencyBar>(true);bar.curLevelTxt.text="3";bar.coinTxt.text="0.00";bar.dollarTxt.text="89.13";bar.clashTxt.text="$0.00";
            bar.addCoinTxt.gameObject.SetActive(false);bar.addDollarTxt.gameObject.SetActive(false);
            var badge=widget.GetComponentInChildren<BadgeShow>(true);badge.priceTxt.text="$300";
            At(widget,"OrchardGiftFit/DailyMissionItem/DailyMission").gameObject.SetActive(false);
            var slot=widget.GetComponentInChildren<SlotEnter>(true);slot.progressTxt.text="2/5";slot.progressImag.fillAmount=.4f;
            foreach(var glow in slot.GetComponentsInChildren<Transform>(true)) if(glow.name=="VFX_ShanGuang")glow.gameObject.SetActive(false);
            At(widget,"UIBizzaAAA").gameObject.SetActive(false);
            var canvas=core.GetComponentInParent<Canvas>();
            foreach(var c in widget.GetComponentsInChildren<Canvas>(true)) { c.renderMode=RenderMode.WorldSpace;c.worldCamera=canvas.worldCamera;c.overrideSorting=true;c.sortingOrder=100; }
            widget.SetActive(true);
            foreach(var r in new[]{core,widget}) r.GetComponent<OrchardReferenceLayout>().RefreshLayout();
            foreach(string prop in new[]{"Undo","Magic","Shuffle"})
            {
                var status=At(core,"UI/Bottom/"+prop+"/AniLayer/ItemStatus");
                foreach(Transform child in status)child.gameObject.SetActive(prop=="Magic"?child.name=="Lock":child.name=="UseDirectly");
            }
            At(core,"UI/Bottom/Box_Root/DangerousTip").gameObject.SetActive(false);
            var tray=At(core,"UI/Bottom/Box_Root/AddOne");
            foreach(Transform state in tray.Find("AniLayer/ItemStatus"))state.gameObject.SetActive(state.name=="Lock");
            var trayCanvas=tray.GetComponent<Canvas>(); if(trayCanvas!=null){trayCanvas.overrideSorting=true;trayCanvas.sortingOrder=110;trayCanvas.worldCamera=canvas.worldCamera;}
            foreach(var child in core.GetComponentsInChildren<Transform>(true))if(child.name.StartsWith("Effct_"))child.gameObject.SetActive(false);
            // Resolve authored basket Canvas offsets in this disposable scene.
            foreach(var c in core.GetComponentsInChildren<Canvas>(true))
            {
                if(c.transform.IsChildOf(widget.transform))continue;
                c.overrideSorting=true;c.sortingOrder=0;c.worldCamera=canvas.worldCamera;
                var dynamicLayer=c.GetComponent<Orange.DynamicCanvasLayer>();
                if(dynamicLayer!=null)c.sortingOrder=new SerializedObject(dynamicLayer).FindProperty("offset").intValue;
            }
            configure?.Invoke(core);
        },width,height,inspectPreview:root=>
        {
            inspect?.Invoke(root);
            ValidateGeometryAndInput(root,output+name+"-checks.txt");
        });
        if(!string.IsNullOrEmpty(result.error))throw new InvalidOperationException(result.error);
        File.WriteAllText(output+name+".json",JsonUtility.ToJson(result,true));
    }
    static void DisableBusiness(GameObject root)
    {
        foreach(var b in root.GetComponentsInChildren<MonoBehaviour>(true))
            if(!(b is Graphic) && !(b is Selectable) && !(b is LayoutGroup) && !(b is ContentSizeFitter) && !(b is AspectRatioFitter) && !(b is GraphicRaycaster))b.enabled=false;
        foreach(var a in root.GetComponentsInChildren<Animation>(true))a.enabled=false;
        foreach(var a in root.GetComponentsInChildren<Animator>(true))a.enabled=false;
    }

    static void ValidateGeometryAndInput(GameObject core,string output)
    {
        var report=new StringBuilder("Static prefab layout and standard UI raycast checks; no click dispatch or business flow execution.\n");
        var buttons=new List<Button>();
        var widget=core.GetComponentInChildren<CurrencyBar>(true).transform.parent.gameObject;
        var bar=widget.GetComponentInChildren<CurrencyBar>(true);
        buttons.Add(bar.coinBtn);buttons.Add(bar.dollarBtn);buttons.Add(bar.settingBtn);
        buttons.Add(At(widget,"SlotEnter").GetComponent<Button>());
        foreach(string prop in new[]{"Undo","Magic","Shuffle"})buttons.Add(At(core,"UI/Bottom/"+prop).GetComponent<Button>());
        buttons.Add(widget.GetComponentInChildren<BadgeShow>(true).GetComponent<Button>());
        var eventObject=new GameObject("Disposable HUD raycast check"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(eventObject,core.scene);
        var previous=EventSystem.current;var events=eventObject.AddComponent<EventSystem>();events.enabled=false;
        var registered=new List<BaseRaycaster>();var errors=new List<string>();
        try
        {
            var host=core.transform.parent;
            foreach(var c in host.GetComponentsInChildren<Canvas>(true))
            {
                var raycaster=c.GetComponent<GraphicRaycaster>() ?? c.gameObject.AddComponent<GraphicRaycaster>();
                raycaster.enabled=true;
                if(!RaycasterManager.GetRaycasters().Contains(raycaster)){RaycasterManager.GetRaycasters().Add(raycaster);registered.Add(raycaster);}
            }
            Canvas.ForceUpdateCanvases();
            var camera=host.GetComponent<Canvas>().worldCamera;
            // Populate CanvasRenderer depth before asking GraphicRaycaster to hit it.
            camera.Render();Canvas.ForceUpdateCanvases();
            foreach(var button in buttons)
            {
                if(button==null || button.targetGraphic==null)throw new InvalidOperationException("Missing bound Button graphic.");
                var visual=button.targetGraphic;
                var r=visual.rectTransform;
                var corners=new Vector3[4];r.GetWorldCorners(corners);
                var screenMin=RectTransformUtility.WorldToScreenPoint(camera,corners[0]);var screenMax=RectTransformUtility.WorldToScreenPoint(camera,corners[2]);
                if(screenMin.x<0 || screenMin.y<0 || screenMax.x>camera.targetTexture.width || screenMax.y>camera.targetTexture.height) errors.Add(button.name+" is outside the viewport.");
                foreach(float x in new[]{.25f,.5f,.75f})
                {
                    var point=new Vector3(Mathf.Lerp(r.rect.xMin,r.rect.xMax,x),r.rect.center.y,0);
                    var pointer=new PointerEventData(events){position=RectTransformUtility.WorldToScreenPoint(camera,r.TransformPoint(point))};
                    var hits=new List<RaycastResult>();events.RaycastAll(pointer,hits);
                    GameObject handler=null;
                    foreach(var hit in hits)if(hit.gameObject.scene==core.scene){handler=ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit.gameObject);break;}
                    if(handler==null || handler.GetComponent<Button>()!=button)errors.Add(button.name+" point "+x+" routed to "+(handler==null?"nothing":handler.name));
                }
                report.AppendLine(button.name+": bounds "+screenMin+".."+screenMax+"; original Button and graphic present");
            }
            foreach(var tx in bar.GetComponentsInChildren<TMP_Text>(true))
            {
                if(!tx.gameObject.activeInHierarchy)continue;
                tx.ForceMeshUpdate(true,true);if(tx.isTextOverflowing)errors.Add(tx.name+" text overflow.");
            }
            report.AppendLine(errors.Count==0?"PASS: 8 Buttons / 24 sample points; all visible controls within viewport; no top label overflow.":"FAILED: "+string.Join("; ",errors));
            File.WriteAllText(output,report.ToString());
            if(errors.Count>0)throw new InvalidOperationException(string.Join("; ",errors));
        }
        finally { foreach(var r in registered)RaycasterManager.GetRaycasters().Remove(r);UnityEngine.Object.DestroyImmediate(eventObject);if(previous!=null)EventSystem.current=previous; }
    }

    public static void Inventory()
    {
        ImportAtlas();
        var report = new StringBuilder();
        foreach (var path in new[] { Widget, Core })
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                report.AppendLine(path);
                foreach (var r in root.GetComponentsInChildren<RectTransform>(true))
                {
                    string p = AnimationUtility.CalculateTransformPath(r, root.transform);
                    if (path == Core && !p.StartsWith("UI/Bottom")) continue;
                    if (p.Contains("NewParticle") || p.Contains("Effct")) continue;
                    report.Append(p).Append(" active=").Append(r.gameObject.activeSelf).Append(" a=").Append(r.anchorMin).Append("..").Append(r.anchorMax).Append(" pos=").Append(r.anchoredPosition).Append(" size=").Append(r.sizeDelta).Append(" scale=").Append(r.localScale);
                    foreach (var c in r.GetComponents<Component>())
                    {
                        if (c == null) { report.Append(" MISSING"); continue; }
                        if (c is RectTransform || c is CanvasRenderer) continue;
                        report.Append(" | ").Append(c.GetType().Name);
                        if (c is Image im) report.Append(" sprite=").Append(im.sprite != null ? AssetDatabase.GetAssetPath(im.sprite)+":"+im.sprite.name : "null").Append(" color=").Append(im.color).Append(" enabled=").Append(im.enabled);
                        if (c is TMP_Text tx) report.Append(" text=").Append(tx.text).Append(" font=").Append(tx.fontSize);
                    }
                    report.AppendLine();
                }
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        File.WriteAllText(Output + "inventory.txt", report.ToString());
    }

    static void ImportAtlas()
    {
        AssetDatabase.ImportAsset(Atlas, ImportAssetOptions.ForceSynchronousImport);
        var ti = (TextureImporter)AssetImporter.GetAtPath(Atlas);
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Multiple;
        ti.alphaIsTransparency = true;
        ti.mipmapEnabled = false;
        ti.isReadable = false;
        ti.maxTextureSize = 2048;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.filterMode = FilterMode.Bilinear;
        ti.wrapMode = TextureWrapMode.Clamp;
        ti.spritePixelsPerUnit = 100;
        var settings = new TextureImporterSettings(); ti.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect; ti.SetTextureSettings(settings);
        ti.spritesheet = new[] {
            Slice("HudLevel",61,1083,387,375), Slice("HudSettings",585,1073,382,344),
            Slice("HudBalance",18,702,495,196,new Vector4(72,72,72,72)), Slice("HudAction",541,720,464,159), Slice("HudGift",33,143,463,415)
        };
        ti.SetPlatformTextureSettings(new TextureImporterPlatformSettings { name="Android", overridden=true, maxTextureSize=2048, format=TextureImporterFormat.ASTC_4x4, compressionQuality=100 });
        ti.SaveAndReimport();
    }
    static SpriteMetaData Slice(string name, float x, float y, float w, float h,Vector4 border=default)
    { return new SpriteMetaData { name=name, rect=new Rect(x,y,w,h), pivot=new Vector2(.5f,.5f), alignment=9,border=border }; }
}
#endif
