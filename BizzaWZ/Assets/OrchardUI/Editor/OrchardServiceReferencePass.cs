using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static partial class OrchardApprovedPass
{
    private const string ServiceControls="Assets/OrchardUI/Resources/OrchardUI/ServiceReferenceControls.png";
    private const string ServiceChatPrefab="Assets/BizzaWZ/Final/Real/UI/ServicePanel/ChatElement.prefab";
    private static void ImportServiceReference()
    {
        AssetDatabase.Refresh();
        ImportFidelitySheet(ServiceControls,new[]{"Issue","Player","Quick","Input","Today","Avatar","Question","Send"},
            new[]{new Rect(64,70,895,247),new Rect(67,369,912,184),new Rect(128,618,770,172),new Rect(36,838,953,182),new Rect(372,1078,281,130),new Rect(115,1245,245,244),new Rect(391,1245,242,244),new Rect(665,1245,243,244)},
            new[]{new Vector4(88,144,78,94),new Vector4(90,83,100,80),Vector4.zero,new Vector4(140,72,140,72),Vector4.zero,Vector4.zero,Vector4.zero,Vector4.zero});
        ExtraSprites.Clear();
        var root=PrefabUtility.LoadPrefabContents(ServiceChatPrefab);
        try { StyleServiceReferenceChat(root);PrefabUtility.SaveAsPrefabAsset(root,ServiceChatPrefab); }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
    private static void BindServiceArtwork(GameObject root,Image[] images,string[] names)
    {
        var visual=root.GetComponent<OrchardServiceVisual>()??root.AddComponent<OrchardServiceVisual>();var so=new SerializedObject(visual);
        so.FindProperty("resourcePath").stringValue="OrchardUI/ServiceReferenceControls";var bindings=so.FindProperty("bindings");bindings.arraySize=images.Length;
        for(int i=0;i<images.Length;i++){var b=bindings.GetArrayElementAtIndex(i);b.FindPropertyRelative("image").objectReferenceValue=images[i];b.FindPropertyRelative("sprite").stringValue=names[i];}
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void StyleServiceReferenceChat(GameObject root)
    {
#if BIZZA_REAL_WITHDRAW
        var row=root.GetComponent<ChatElement>();TextStyle(row.chatTxt,36);ReferenceBody(row.chatTxt);row.chatTxt.enableAutoSizing=false;
        row.chatTxt.characterSpacing=-.55f;
        row.chatTxtRect.localScale=new Vector3(.96f,1.05f,1);
        var so=new SerializedObject(row);so.FindProperty("maxBubbleWidth").floatValue=544;so.FindProperty("multilineBubbleWidth").floatValue=470;so.FindProperty("minimumBubbleHeight").floatValue=104;
        so.FindProperty("outerHorizontalPadding").floatValue=0;so.FindProperty("issueAvatarSpace").floatValue=98;so.FindProperty("issueHeaderHeight").floatValue=42;
        so.FindProperty("bubbleHorizontalPadding").floatValue=45;so.FindProperty("playerHorizontalPadding").floatValue=28;so.FindProperty("playerBottomSpacing").floatValue=12;so.FindProperty("bubbleVerticalPadding").floatValue=24;so.ApplyModifiedPropertiesWithoutUndo();
        var issue=Need(root,"ChatInfo/Issue_bg").GetComponent<Image>();var player=Need(root,"ChatInfo/Player_bg").GetComponent<Image>();
        foreach(var image in new[]{issue,player}){CashImage(image);image.type=Image.Type.Sliced;image.pixelsPerUnitMultiplier=1.8f;StretchRect(image.transform);}
        var avatar=Need(root,"ChatInfo/Issue_bg/ApprovedAvatar").GetComponent<Image>();CashImage(avatar);avatar.preserveAspect=true;LocalRect(avatar.transform,-90,5,94,94);
        var speaker=Need(root,"ChatInfo/Issue_bg/ApprovedSpeaker").GetComponent<TMP_Text>();LocalText(speaker.transform,45,14,300,45,34);ReferenceBody(speaker);speaker.color=new Color32(0,111,177,255);speaker.alignment=TextAlignmentOptions.MidlineLeft;ConfirmTextShape(speaker,.87f,1.1f,true);
        BindServiceArtwork(root,new[]{issue,player,avatar},new[]{"Issue","Player","Avatar"});
        ConfigureServiceMessageText(row.chatTxt);
#endif
    }
    private static Image ServiceLine(GameObject root,string path,float x,float y,float width,Color color)
    {
        var t=Ensure(root,path);var image=t.GetComponent<Image>()??t.gameObject.AddComponent<Image>();image.sprite=null;image.color=color;image.raycastTarget=false;Place(root,t,x,y,width,2);return image;
    }
    private static void FinalizeServiceReference(GameObject root)
    {
#if BIZZA_REAL_WITHDRAW
        Place(root,Need(root,"Content/ChatContent"),27,193,798,1414);
        Place(root,Need(root,"Title/bg"),232,61,387,115);
        var title=Need(root,"Title/Title").GetComponent<TMP_Text>();Place(root,title.transform,261,79,330,86);SizeText(title,70);title.rectTransform.localScale=new Vector3(.96f,1.06f,1);
        var page=root.GetComponent<ServicePanel>();var so=new SerializedObject(page);so.FindProperty("keepQuestionPickerVisible").boolValue=true;so.ApplyModifiedPropertiesWithoutUndo();
        Place(root,page.scrollRect.viewport,65,326,732,720);
        var content=(RectTransform)page.contentRoot;TopContent(content);var layout=content.GetComponent<VerticalLayoutGroup>();layout.padding=new RectOffset(0,0,12,26);layout.spacing=34;layout.childControlHeight=false;layout.childControlWidth=true;layout.childForceExpandHeight=false;layout.childForceExpandWidth=true;
        var mask=page.scrollRect.viewport.GetComponent<Mask>();if(mask!=null)Object.DestroyImmediate(mask);
        if(page.scrollRect.viewport.GetComponent<RectMask2D>()==null)page.scrollRect.viewport.gameObject.AddComponent<RectMask2D>();
        var viewportImage=page.scrollRect.viewport.GetComponent<Image>();if(viewportImage!=null)viewportImage.color=Color.clear;
        page.viewportResizer.bottomPadding=0;
        ServiceLine(root,"Content/ReferenceTodayLeft",79,272,267,new Color32(234,213,178,255));ServiceLine(root,"Content/ReferenceTodayRight",506,272,267,new Color32(234,213,178,255));
        ServiceLine(root,"Content/ReferenceInputLine",67,1412,718,new Color32(255,187,48,255));
        Place(root,Need(root,"Content/ApprovedTodayPlate"),365,242,122,63);
        var today=Need(root,"Content/ApprovedToday").GetComponent<TMP_Text>();Place(root,today.transform,380,245,91,56);ReferenceBody(today);SizeText(today,35);today.alignment=TextAlignmentOptions.Center;
        const string input="Content/InputNode/";
        Place(root,Need(root,input+"SelectQuestionBtn "),202,1058,449,103);Place(root,Need(root,input+"SelectQuestionBtn /ApprovedQuestion"),255,1080,59,59);
        var quick=Need(root,input+"SelectQuestionBtn /Text (TMP)").GetComponent<TMP_Text>();Place(root,quick.transform,331,1078,283,65);ReferenceBody(quick);SizeText(quick,36);quick.color=new Color32(0,109,177,255);quick.rectTransform.localScale=new Vector3(1,1.08f,1);
        BindCopy(quick,"Quick questions","Perguntas rápidas");
        Place(root,Need(root,input+"bg"),62,1438,596,119);
        Place(root,Need(root,input+"InputField"),77,1452,559,82);
        var textArea=Need(root,input+"InputField/TextArea").GetComponent<RectTransform>();textArea.offsetMin=new Vector2(31,4);textArea.offsetMax=new Vector2(-42,-4);
        foreach(var text in page.inputText.GetComponentsInChildren<TMP_Text>(true)){ReferenceBody(text);SizeText(text,36);text.enableWordWrapping=false;}
        var placeholder=page.inputText.PlaceholderTextRenderer.GetComponent<TMP_Text>();BindCopy(placeholder,"Write your message...","Escreva sua mensagem...");placeholder.color=new Color32(135,135,135,255);
        Place(root,Need(root,input+"ClearBtn"),599,1470,42,42);Need(root,input+"ClearBtn").gameObject.SetActive(false);
        foreach(string button in new[]{"CanSendBtn","NotCanSendBtn"})Place(root,Need(root,input+button),660,1436,125,124);
        var paths=new[]{"Content/ApprovedTodayPlate",input+"bg",input+"SelectQuestionBtn ",input+"SelectQuestionBtn /ApprovedQuestion",input+"CanSendBtn",input+"NotCanSendBtn"};
        var names=new[]{"Today","Input","Quick","Question","Send","Send"};var images=new List<Image>();
        foreach(var path in paths){var image=Need(root,path).GetComponent<Image>();CashImage(image);if(path==input+"bg"){image.type=Image.Type.Sliced;image.pixelsPerUnitMultiplier=1.45f;}if(image.TryGetComponent<Button>(out var button)){image.raycastTarget=true;button.targetGraphic=image;}images.Add(image);}
        BindServiceArtwork(root,images.ToArray(),names);
        ConfigureServiceInput(root);
#endif
    }
    private static void PreviewServiceReference(GameObject root)
    {
        var sprites=new List<Sprite>();foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(ServiceControls))if(asset is Sprite sprite)sprites.Add(sprite);
        foreach(var visual in root.GetComponentsInChildren<OrchardServiceVisual>(true))visual.ApplyArtwork(sprites.ToArray());
    }
}
