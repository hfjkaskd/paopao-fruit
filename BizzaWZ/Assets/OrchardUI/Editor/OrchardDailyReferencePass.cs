using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static partial class OrchardApprovedPass
{
    private const string DailyControls="Assets/OrchardUI/Resources/OrchardUI/DailyReferenceControls.png";
    private static void ImportDailyReference()
    {
        AssetDatabase.Refresh();
        ImportFidelitySheet(DailyControls,new[]{"Title","Gift","Amount","Progress","Action","Video","Clock","Close"},
            new[]{new Rect(51,12,927,226),new Rect(211,238,620,421),new Rect(43,658,943,212),new Rect(43,875,943,203),new Rect(24,1082,980,215),new Rect(158,1315,209,187),new Rect(411,1308,205,198),new Rect(657,1302,214,204)},
            new[]{Vector4.zero,Vector4.zero,Vector4.zero,Vector4.zero,Vector4.zero,Vector4.zero,Vector4.zero,Vector4.zero});
        ExtraSprites.Clear();
    }
    private static TMP_Text DailyText(GameObject root,string path,float x,float y,float w,float h,float size,bool body=true)
    {
        var text=Need(root,path).GetComponent<TMP_Text>();Place(root,text.transform,x,y,w,h);TextStyle(text,size);
        if(body)ReferenceBody(text);text.alignment=TextAlignmentOptions.Center;text.enableWordWrapping=false;text.overflowMode=TextOverflowModes.Ellipsis;return text;
    }
    private static void FinalizeDailyReference(GameObject root)
    {
#if BIZZA_REAL_WITHDRAW
        foreach(var image in root.GetComponentsInChildren<Image>(true))if(image.name.StartsWith("PageMask")||image.name=="Mask")image.color=new Color(0,0,0,.3f);
        foreach(var rect in root.GetComponentsInChildren<RectTransform>(true))if(rect.name.StartsWith("OrchardNavLeaves"))rect.gameObject.SetActive(false);
        Place(root,Need(root,"BG (2)"),33,345,786,1158);
        Place(root,Need(root,"BG (2)/Image (2)"),104,257,638,166);
        var title=DailyText(root,"BG (2)/Text (TMP)",201,293,450,111,88,false);SetReferenceTitle(title,ApprovedInk);title.fontSizeMin=40;title.rectTransform.localScale=new Vector3(.98f,1.14f,1);
        var arch=title.GetComponent<OrchardArchedText>()??title.gameObject.AddComponent<OrchardArchedText>();var archSo=new SerializedObject(arch);archSo.FindProperty("archHeight").floatValue=10;archSo.ApplyModifiedPropertiesWithoutUndo();
        Place(root,Need(root,"Title/CloseBtn"),718,330,110,108);
        var subtitle=DailyText(root,"Title/title",112,432,628,57,40,false);subtitle.rectTransform.localScale=new Vector3(.96f,1.08f,1);
        Place(root,Need(root,"Content/ApprovedGift"),207,497,458,335);
        Need(root,"Content/ApprovedGiftVideo").gameObject.SetActive(false);
        Place(root,Need(root,"Content/ApprovedAmount"),77,840,698,190);
        var requirement=DailyText(root,"Content/Hint",106,851,640,67,40,false);requirement.rectTransform.localScale=new Vector3(.96f,1.08f,1);
        var amount=DailyText(root,"Content/ApprovedRewardValue",111,913,630,102,125,false);amount.color=new Color32(0,108,28,255);amount.rectTransform.localScale=new Vector3(1.04f,1.19f,1);
        var track=Need(root,"Content/ApprovedProgressTrack").GetComponent<Image>();Place(root,track.transform,77,1047,699,159);
        var fill=Need(root,"Content/ApprovedProgressTrack/Fill").GetComponent<Image>();LocalRect(fill.transform,105,51,568,62);RoundFill(fill,true);fill.pixelsPerUnitMultiplier=2.1f;
        var rounded=fill.GetComponent<OrchardRoundedFill>();var fillSo=new SerializedObject(rounded);fillSo.FindProperty("fullSize").vector2Value=new Vector2(568,62);fillSo.FindProperty("topLeft").vector2Value=new Vector2(105,-62);fillSo.ApplyModifiedPropertiesWithoutUndo();rounded.Refresh();
        var caption=DailyText(root,"Content/ApprovedProgressCaption",106,1054,558,50,32,false);caption.alignment=TextAlignmentOptions.MidlineLeft;caption.rectTransform.localScale=new Vector3(.94f,1.12f,1);
        var progress=DailyText(root,"Content/ApprovedProgressValue",189,1103,563,79,49,false);SetReferenceTitle(progress,ApprovedInk);progress.rectTransform.localScale=new Vector3(.88f,1.09f,1);
        Place(root,Need(root,"Content/ApprovedVideoIcon"),101,1105,93,77);
        foreach(string path in new[]{"Content/GoBtn","Content/WithdrawBtn","Content/ClaimedBtn"})
        {
            Place(root,Need(root,path),77,1227,698,167);
            var label=DailyText(root,path+"/Text (TMP)",313,1261,359,93,69,false);SetReferenceTitle(label,new Color32(0,78,26,255));label.rectTransform.localScale=new Vector3(.94f,1.13f,1);
            var legacy=label.GetComponent<UILanguageLabel>();if(legacy!=null)legacy.enabled=false;
            if(path=="Content/GoBtn")
            {
                Place(root,Need(root,path+"/ApprovedFilm"),203,1265,105,91);BindCopy(label,"Watch Video","Assistir vídeo");
                if(legacy!=null){var copySo=new SerializedObject(label.GetComponent<OrchardLocalizedLabel>());copySo.FindProperty("existingKey").stringValue=legacy.key;copySo.ApplyModifiedPropertiesWithoutUndo();}
            }
            else
            {
                Place(root,label.transform,187,1265,478,93);
                BindCopy(label,path=="Content/WithdrawBtn"?"Withdraw":"Claimed",path=="Content/WithdrawBtn"?"Retirar":"Recebido");
                var film=root.transform.Find(path+"/ApprovedFilm");if(film!=null)film.gameObject.SetActive(false);
            }
        }
        DailyText(root,"Content/ClaimHint",109,1202,634,28,25);
        var clock=Ensure(root,"Title/ReferenceClock");if(clock.GetComponent<Image>()==null)clock.gameObject.AddComponent<Image>();Place(root,clock,253,1408,57,56);
        var timer=DailyText(root,"Title/refreshTimeTxt",323,1407,414,61,31,false);timer.alignment=TextAlignmentOptions.MidlineLeft;timer.rectTransform.localScale=new Vector3(.94f,1.12f,1);
        var page=root.GetComponent<DailyMissionPanel>();var pageSo=new SerializedObject(page);pageSo.FindProperty("requirementEnglish").stringValue="Watch {0} videos to receive";pageSo.FindProperty("requirementPortuguese").stringValue="Assista a {0} vídeos para receber";pageSo.FindProperty("countdownEnglish").stringValue="Refreshes in {0}";pageSo.FindProperty("countdownPortuguese").stringValue="Atualiza em {0}";pageSo.ApplyModifiedPropertiesWithoutUndo();
        var paths=new[]{"BG (2)/Image (2)","Content/ApprovedGift","Content/ApprovedAmount","Content/ApprovedProgressTrack","Content/GoBtn","Content/WithdrawBtn","Content/ClaimedBtn","Content/GoBtn/ApprovedFilm","Content/WithdrawBtn/ApprovedFilm","Content/ApprovedVideoIcon","Title/ReferenceClock","Title/CloseBtn"};
        var names=new[]{"Title","Gift","Amount","Progress","Action","Action","Action","Video","Video","Video","Clock","Close"};
        var visual=root.GetComponent<OrchardDailyVisual>()??root.AddComponent<OrchardDailyVisual>();var so=new SerializedObject(visual);so.FindProperty("resourcePath").stringValue="OrchardUI/DailyReferenceControls";var bindings=so.FindProperty("bindings");bindings.arraySize=paths.Length;
        for(int i=0;i<paths.Length;i++){var image=Need(root,paths[i]).GetComponent<Image>();CashImage(image);image.preserveAspect=false;if(image.TryGetComponent<Button>(out var button)){image.raycastTarget=true;button.targetGraphic=image;}var b=bindings.GetArrayElementAtIndex(i);b.FindPropertyRelative("image").objectReferenceValue=image;b.FindPropertyRelative("sprite").stringValue=names[i];}
        so.ApplyModifiedPropertiesWithoutUndo();
#endif
    }
    private static void PreviewDailyReference(GameObject root)
    {
        var sprites=new List<Sprite>();foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(DailyControls))if(asset is Sprite sprite)sprites.Add(sprite);
        root.GetComponent<OrchardDailyVisual>().ApplyArtwork(sprites.ToArray());
        root.transform.Find("Content/ApprovedProgressTrack/Fill").GetComponent<OrchardRoundedFill>().Refresh();
    }
}
