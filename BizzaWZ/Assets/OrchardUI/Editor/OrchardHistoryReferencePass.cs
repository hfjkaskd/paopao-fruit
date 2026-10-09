using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static partial class OrchardApprovedPass
{
    private const string HistoryControls="Assets/OrchardUI/Resources/OrchardUI/HistoryReferenceControls.png";
    private const string HistoryCardPrefab="Assets/BizzaWZ/Final/Real/UI/WithdrawHistory/WithdrawHistoryItem.prefab";
    private static void ImportHistoryReference()
    {
        AssetDatabase.Refresh();
        ImportFidelitySheet(HistoryControls,new[]{"Card","Pending","Success","Failed","Help"},
            new[]{new Rect(27,102,970,346),new Rect(157,504,710,182),new Rect(157,738,710,182),new Rect(157,972,710,184),new Rect(35,1225,955,210)},
            new[]{new Vector4(115,115,115,115),Vector4.zero,Vector4.zero,Vector4.zero,Vector4.zero});
        ExtraSprites.Clear();
        var root=PrefabUtility.LoadPrefabContents(HistoryCardPrefab);
        try { StyleHistoryCard(root);PrefabUtility.SaveAsPrefabAsset(root,HistoryCardPrefab); }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
    private static void BindHistoryArtwork(GameObject root,Image[] images,string[] names)
    {
        var visual=root.GetComponent<OrchardHistoryVisual>()??root.AddComponent<OrchardHistoryVisual>();
        var so=new SerializedObject(visual);so.FindProperty("resourcePath").stringValue="OrchardUI/HistoryReferenceControls";
        var bindings=so.FindProperty("bindings");bindings.arraySize=images.Length;
        for(int i=0;i<images.Length;i++)
        {var binding=bindings.GetArrayElementAtIndex(i);binding.FindPropertyRelative("image").objectReferenceValue=images[i];binding.FindPropertyRelative("sprite").stringValue=names[i];}
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void HistoryBody(TMP_Text text,float x,float y,float width,float height,float size)
    {
        LocalText(text.transform,x,y,width,height,size);ReferenceBody(text);text.alignment=TextAlignmentOptions.MidlineLeft;
        text.enableWordWrapping=false;text.overflowMode=TextOverflowModes.Ellipsis;
    }
    private static void StyleHistoryReferenceCard(GameObject root)
    {
#if BIZZA_REAL_WITHDRAW
        var row=root.GetComponent<WithdrawHistoryItem>();((RectTransform)root.transform).sizeDelta=new Vector2(730,280);
        var bg=Need(root,"bg").GetComponent<Image>();CashImage(bg);bg.type=Image.Type.Sliced;bg.pixelsPerUnitMultiplier=2;
        StretchRect(bg.transform);bg.rectTransform.offsetMin=new Vector2(-2,-3);bg.rectTransform.offsetMax=new Vector2(2,3);
        LocalRect(row.withdrawImg.transform,27,29,322,114);row.withdrawImg.type=Image.Type.Simple;row.withdrawImg.preserveAspect=true;
        LocalText(row.amountTxt.transform,449,42,252,92,92);row.amountTxt.color=new Color32(0,86,45,255);
        row.amountTxt.alignment=TextAlignmentOptions.Center;row.amountTxt.enableWordWrapping=false;
        row.amountTxt.rectTransform.localScale=new Vector3(.91f,1.22f,1);
        HistoryBody(row.timeTxt,39,148,348,47,38);
        HistoryBody(row.emailTxt,39,197,348,46,37);
        HistoryBody(row.nameTxt,39,197,348,46,37);HistoryBody(row.cpfTxt,39,197,348,46,37);
        row.timeTxt.rectTransform.localScale=new Vector3(.99f,1.09f,1);row.timeTxt.rectTransform.anchoredPosition+=Vector2.up;
        row.emailTxt.rectTransform.localScale=new Vector3(.94f,1,1);row.emailTxt.rectTransform.anchoredPosition+=Vector2.left*11;
        HistoryBody(row.dueText,400,226,306,47,30);row.dueText.enableWordWrapping=true;row.dueText.fontSizeMin=23;row.dueText.rectTransform.localScale=new Vector3(.96f,1.2f,1);
        var so=new SerializedObject(row);so.FindProperty("normalRowHeight").floatValue=280;so.FindProperty("failedRowHeight").floatValue=300;so.ApplyModifiedPropertiesWithoutUndo();
        var states=new[]{row.processingObj,row.successObj,row.failObj};var images=new List<Image>{bg};
        for(int i=0;i<states.Length;i++)
        {
            var state=states[i];LocalRect(state.transform,i==2?390:402,152,i==2?307:300,80);
            foreach(var old in state.GetComponentsInChildren<Image>(true))old.enabled=false;
            var image=state.GetComponent<Image>()??state.AddComponent<Image>();CashImage(image);images.Add(image);
            foreach(var text in state.GetComponentsInChildren<TMP_Text>(true))
            {
                var legacy=text.GetComponent<UILanguageLabel>();if(legacy!=null)legacy.enabled=false;
                HistoryBody(text,92,8,i==2?196:190,63,i==2?34:38);
                text.color=i==1?new Color32(18,71,26,255):ApprovedInk;
                text.rectTransform.localScale=new Vector3(.94f,.985f,1);
                BindCopy(text,new[]{"In review","Completed","Not completed"}[i],new[]{"Em análise","Concluído","Não concluído"}[i]);
            }
        }
        BindHistoryArtwork(root,images.ToArray(),new[]{"Card","Pending","Success","Failed"});
#endif
    }
    private static void FinalizeHistoryReference(GameObject root)
    {
        Place(root,Need(root,"BG (2)"),23,241,806,1324);
        Place(root,Need(root,"BG (2)/Image (2)"),218,100,418,116);
        var title=Need(root,"BG (2)/Text (TMP)").GetComponent<TMP_Text>();Place(root,title.transform,251,115,348,79);SizeText(title,75);
        title.rectTransform.localScale=new Vector3(.91f,1.06f,1);
        Place(root,Need(root,"CloseBtn"),39,70,99,99);Place(root,Need(root,"ApprovedHelp"),710,70,99,99);
        var section=Need(root,"ApprovedSection").GetComponent<TMP_Text>();Place(root,section.transform,105,286,642,75);SizeText(section,64);section.rectTransform.localScale=new Vector3(.96f,1.16f,1);
        var scroll=Need(root,"Content/Scroll View").GetComponent<ScrollRect>();Place(root,scroll.transform,57,391,740,917);StretchRect(scroll.viewport);
        var oldMask=scroll.viewport.GetComponent<Mask>();if(oldMask!=null)Object.DestroyImmediate(oldMask);
        var mask=scroll.viewport.GetComponent<RectMask2D>()??scroll.viewport.gameObject.AddComponent<RectMask2D>();mask.padding=Vector4.zero;
        var vi=scroll.viewport.GetComponent<Image>();if(vi!=null)vi.enabled=false;
        var group=scroll.content.GetComponent<VerticalLayoutGroup>();group.spacing=24;group.padding=new RectOffset(5,5,4,4);
        scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;
        var support=Need(root,"ApprovedSupport").GetComponent<Image>();Place(root,support.transform,137,1377,588,124);CashImage(support);
        support.raycastTarget=true;support.GetComponent<Button>().targetGraphic=support;
        var label=Need(root,"ApprovedSupport/Label").GetComponent<TMP_Text>();Place(root,label.transform,327,1410,353,64);ReferenceBody(label);SizeText(label,48);
        label.alignment=TextAlignmentOptions.MidlineLeft;label.enableWordWrapping=false;label.color=new Color32(0,70,131,255);
        label.rectTransform.localScale=new Vector3(.88f,1,1);label.rectTransform.anchoredPosition+=new Vector2(-20,0);
        BindHistoryArtwork(root,new[]{support},new[]{"Help"});
    }
    private static void PreviewHistoryReference(GameObject root)
    {
        var sprites=new List<Sprite>();foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(HistoryControls))if(asset is Sprite sprite)sprites.Add(sprite);
        foreach(var visual in root.GetComponentsInChildren<OrchardHistoryVisual>(true))visual.ApplyArtwork(sprites.ToArray());
    }
}
