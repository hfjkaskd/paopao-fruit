using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static partial class OrchardApprovedPass
{
    private const string RateControls="Assets/OrchardUI/Resources/OrchardUI/RateReferenceControls.png";
    private const string RateRadiance="Assets/OrchardUI/Resources/OrchardUI/RateReferenceRadiance.png";
    private static void ImportRateReference()
    {
        AssetDatabase.Refresh();
        ImportFidelitySheet(RateControls,new[]{"Title","Before","Connector","Now","Check","Close"},
            new[]{new Rect(172,22,641,196),new Rect(35,530,915,299),new Rect(424,832,136,90),new Rect(34,919,917,352),new Rect(154,1284,676,154),new Rect(417,1440,150,146)},
            new[]{Vector4.zero,Vector4.zero,Vector4.zero,Vector4.zero,Vector4.zero,Vector4.zero});
        ImportFidelitySheet(RateRadiance,new[]{"Hero"},new[]{new Rect(185,205,650,327)},new[]{Vector4.zero});
        ExtraSprites.Clear();
    }
    private static TMP_Text RateText(GameObject root,string path,float x,float y,float width,float height,float size,Color color)
    {
        var text=Need(root,path).GetComponent<TMP_Text>();Place(root,text.transform,x,y,width,height);
        TextStyle(text,size);text.color=color;text.alignment=TextAlignmentOptions.MidlineLeft;text.enableWordWrapping=false;
        text.overflowMode=TextOverflowModes.Ellipsis;text.fontSizeMin=size*.5f;return text;
    }
    private static void FinalizeRateReference(GameObject root)
    {
        const string c="Content (1)/";
        foreach(var image in root.GetComponentsInChildren<Image>(true))if(image.name.StartsWith("PageMask")||image.name=="Mask")image.color=new Color(0,0,0,.18f);
        foreach(var rect in root.GetComponentsInChildren<RectTransform>(true))if(rect.name.StartsWith("OrchardNavLeaves"))rect.gameObject.SetActive(false);
        Place(root,Need(root,"BG (2)"),34,385,784,1117);
        Place(root,Need(root,"BG (2)/Image (2)"),142,293,568,172);
        var title=Need(root,"BG (2)/Text (TMP)").GetComponent<TMP_Text>();Place(root,title.transform,231,339,389,92);SizeText(title,93);title.rectTransform.localScale=new Vector3(.96f,1.14f,1);
        title.fontSizeMin=37;title.enableWordWrapping=false;title.overflowMode=TextOverflowModes.Ellipsis;
        var arch=title.GetComponent<OrchardArchedText>()??title.gameObject.AddComponent<OrchardArchedText>();var archData=new SerializedObject(arch);archData.FindProperty("archHeight").floatValue=12;archData.ApplyModifiedPropertiesWithoutUndo();
        Place(root,Need(root,c+"ApprovedRate"),154,431,544,274);
        var subtitle=RateText(root,c+"ApprovedSubtitle",115,672,622,68,49,ApprovedInk);subtitle.alignment=TextAlignmentOptions.Center;subtitle.rectTransform.localScale=new Vector3(.974f,1.23f,1);
        Place(root,Need(root,c+"BeforeState"),81,754,690,228);
        Place(root,Need(root,c+"BeforeState/bg"),77,749,698,239);
        Place(root,Need(root,c+"BeforeState/GameObject"),81,754,690,228);
        Place(root,Need(root,c+"NowState"),81,1019,690,276);
        Place(root,Need(root,c+"NowState/bg"),76,1012,700,290);
        Place(root,Need(root,c+"NowState/GameObject"),81,1019,690,276);
        foreach(string state in new[]{"BeforeState","NowState"})Need(root,c+state+"/Icon").GetComponent<Image>().enabled=false;
        var before=RateText(root,c+"BeforeState/Title",278,771,296,49,46,ApprovedInk);before.alignment=TextAlignmentOptions.Center;
        var now=RateText(root,c+"NowState/Title",278,1047,296,50,46,ApprovedInk);now.alignment=TextAlignmentOptions.Center;
        foreach(var text in new[]{before,now}){var legacy=text.GetComponent<UILanguageLabel>();if(legacy!=null)legacy.enabled=false;}
        BindCopy(before,"Before","Antes");BindCopy(now,"Now","Agora");
        before.rectTransform.localScale=new Vector3(.82f,.9f,1);now.rectTransform.localScale=new Vector3(.867f,1,1);
        ConfirmTextShape(RateText(root,c+"BeforeState/GameObject/beblance",355,829,330,67,52,ApprovedInk),.88f,.976f,true);
        ConfirmTextShape(RateText(root,c+"NowState/GameObject/nowblance",361,1115,332,68,51,ApprovedInk),.868f,.976f,true);
        var green=new Color32(0,100,28,255);
        var beforePrefix=RateText(root,c+"BeforeState/GameObject/=",359,886,65,79,74,green);beforePrefix.text="≈";ConfirmTextShape(beforePrefix,1.394f,1.296f,true,1);
        var nowPrefix=RateText(root,c+"NowState/GameObject/=",354,1171,70,99,91,green);nowPrefix.text="≈";ConfirmTextShape(nowPrefix,1.238f,1.265f,true,2.5f);
        ConfirmTextShape(RateText(root,c+"BeforeState/GameObject/beclash",420,883,253,85,88,green),.858f,.985f,true,2);
        ConfirmTextShape(RateText(root,c+"NowState/GameObject/nowclash",420,1160,296,119,124,green),.916f,1.022f,true,2);
        var connector=Ensure(root,c+"ReferenceConnector");var connectorImage=connector.GetComponent<Image>()??connector.gameObject.AddComponent<Image>();Place(root,connector,387,975,78,55);connector.SetAsLastSibling();
        Place(root,Need(root,c+"WithdrawBtn"),123,1313,608,154);
        var buttonLabel=Need(root,c+"WithdrawBtn/Text (TMP)").GetComponent<TMP_Text>();Place(root,buttonLabel.transform,173,1337,506,100);SizeText(buttonLabel,87);buttonLabel.rectTransform.localScale=new Vector3(.94f,1.07f,1);
        Place(root,Need(root,c+"CloseBtn"),699,378,112,108);
        var paths=new[]{"BG (2)/Image (2)",c+"BeforeState/bg",c+"ReferenceConnector",c+"NowState/bg",c+"WithdrawBtn",c+"CloseBtn"};
        var names=new[]{"Title","Before","Connector","Now","Check","Close"};
        var visual=root.GetComponent<OrchardRateVisual>()??root.AddComponent<OrchardRateVisual>();var so=new SerializedObject(visual);
        so.FindProperty("resourcePath").stringValue="OrchardUI/RateReferenceControls";so.FindProperty("heroResourcePath").stringValue="OrchardUI/RateReferenceRadiance";
        var hero=Need(root,c+"ApprovedRate").GetComponent<Image>();CashImage(hero);hero.preserveAspect=true;so.FindProperty("heroImage").objectReferenceValue=hero;
        var bindings=so.FindProperty("bindings");bindings.arraySize=paths.Length;
        for(int i=0;i<paths.Length;i++)
        {
            var image=Need(root,paths[i]).GetComponent<Image>();CashImage(image);
            if(image.TryGetComponent<Button>(out var button)){image.raycastTarget=true;button.targetGraphic=image;}
            var binding=bindings.GetArrayElementAtIndex(i);binding.FindPropertyRelative("image").objectReferenceValue=image;binding.FindPropertyRelative("sprite").stringValue=names[i];
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void PreviewRateReference(GameObject root)
    {
        var sprites=new List<Sprite>();foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(RateControls))if(asset is Sprite sprite)sprites.Add(sprite);
        root.GetComponent<OrchardRateVisual>().ApplyArtwork(sprites.ToArray(),NamedSprite(RateRadiance,"Hero"));
    }
}
