using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static partial class OrchardApprovedPass
{
    private const string BoosterPlate = "Assets/OrchardUI/Resources/OrchardUI/BoosterReferencePlate.png";
    private const string BoosterControls = "Assets/OrchardUI/Resources/OrchardUI/BoosterReferenceControls.png";

    private static void ImportBoosterReference()
    {
        AssetDatabase.Refresh();
        ImportFidelitySheet(BoosterPlate,new[]{"Panel"},new[]{new Rect(44,352,765,1064)},new[]{Vector4.zero});
        ImportFidelitySheet(BoosterControls,new[]{"Claim","Close","Undo"},
            new[]{new Rect(92,1134,670,184),new Rect(681,429,126,122),new Rect(286,588,269,265)},
            new[]{Vector4.zero,Vector4.zero,Vector4.zero});
        ExtraSprites.Clear();
    }
    private static void FinalizeBoosterReference(GameObject root)
    {
#if BIZZA_REAL_WITHDRAW
        var page=root.GetComponent<AddPropPanel>();
        foreach(string path in new[]{"BG (1)","BG (1)/Image (2)","Content/ApprovedOrb","Content/ApprovedLimit"})Need(root,path).GetComponent<Image>().enabled=false;
        var mask=Need(root,"PageMask").GetComponent<Image>();mask.color=new Color(0,0,0,.27f);
        var panel=Ensure(root,"BoosterReferenceDecoration");var panelImage=panel.GetComponent<Image>()??panel.gameObject.AddComponent<Image>();
        Place(root,panel,44,352,765,1064);panel.SetAsFirstSibling();mask.transform.SetAsFirstSibling();
        panelImage.sprite=null;panelImage.enabled=false;panelImage.raycastTarget=false;panelImage.type=Image.Type.Simple;panelImage.preserveAspect=false;panelImage.color=Color.white;
        var close=page.closeBtn.GetComponent<Image>();Place(root,close.transform,681,429,126,122);
        var claim=page.adBuyBtn.GetComponent<Image>();Place(root,claim.transform,92,1134,670,184);
        foreach(var image in new[]{close,claim}){image.sprite=null;image.overrideSprite=null;image.enabled=false;image.type=Image.Type.Simple;image.preserveAspect=false;image.color=Color.white;image.raycastTarget=true;}
        page.closeBtn.targetGraphic=close;page.adBuyBtn.targetGraphic=claim;
        foreach(var image in page.adBuyBtn.GetComponentsInChildren<Image>(true))if(image!=claim)image.enabled=false;
        Place(root,page.propIcon.transform,286,588,269,265);page.propIcon.type=Image.Type.Simple;page.propIcon.preserveAspect=true;

        var title=Need(root,"BG (1)/Text (TMP)").GetComponent<TMP_Text>();Place(root,title.transform,200,405,454,109);SizeText(title,84);title.rectTransform.localScale=new Vector3(1,1.2f,1);
        var arch=title.GetComponent<OrchardArchedText>()??title.gameObject.AddComponent<OrchardArchedText>();var archData=new SerializedObject(arch);archData.FindProperty("archHeight").floatValue=18;archData.ApplyModifiedPropertiesWithoutUndo();
        Place(root,page.propName.transform,193,880,466,105);SizeText(page.propName,105);page.propName.fontSizeMin=50;page.propName.enableWordWrapping=false;
        var description=Need(root,"Content/ApprovedToolName").GetComponent<TMP_Text>();Place(root,description.transform,133,971,586,63);SizeText(description,40.5f);ReferenceBody(description);
        var oldCopy=description.GetComponent<OrchardLocalizedLabel>();if(oldCopy!=null)Object.DestroyImmediate(oldCopy);
        description.enableWordWrapping=false;
        Place(root,page.limitTxt.transform,212,1055,430,60);SizeText(page.limitTxt,38.5f);ReferenceBody(page.limitTxt);
        foreach(var label in page.adBuyBtn.GetComponentsInChildren<TMP_Text>(true))
        {
            Place(root,label.transform,302,1179,386,91);SizeText(label,59.4f);label.rectTransform.localScale=new Vector3(1,1.16f,1);SetReferenceTitle(label,new Color32(0,77,24,255));label.alignment=TextAlignmentOptions.Center;
            var legacy=label.GetComponent<UILanguageLabel>();if(legacy!=null)legacy.enabled=false;
            BindCopy(label,"Watch & Get 1","Assistir e ganhar 1");
        }
        var footer=Need(root,"Content/ApprovedFooter").GetComponent<TMP_Text>();Place(root,footer.transform,182,1315,490,48);SizeText(footer,30.4f);ReferenceBody(footer);footer.color=new Color32(154,103,70,255);

        var visual=root.GetComponent<OrchardBoosterVisual>()??root.AddComponent<OrchardBoosterVisual>();
        var data=new SerializedObject(visual);
        data.FindProperty("decorationResource").stringValue="OrchardUI/BoosterReferencePlate";data.FindProperty("controlsResource").stringValue="OrchardUI/BoosterReferenceControls";
        data.FindProperty("decoration").objectReferenceValue=panelImage;data.FindProperty("closeImage").objectReferenceValue=close;data.FindProperty("claimImage").objectReferenceValue=claim;
        data.FindProperty("propIcon").objectReferenceValue=page.propIcon;data.FindProperty("propName").objectReferenceValue=page.propName;data.FindProperty("description").objectReferenceValue=description;
        data.FindProperty("usage").objectReferenceValue=page.limitTxt;data.FindProperty("englishUsageFormat").stringValue="Used this level: {0} / {1}";data.FindProperty("portugueseUsageFormat").stringValue="Usado nesta fase: {0} / {1}";
        var props=data.FindProperty("props");props.arraySize=3;
        string[] enNames={"Undo","Shuffle","Magic Wand"},ptNames={"Desfazer","Embaralhar","Varinha mágica"};
        string[] enDescriptions={"Take back your last move.","Give your fruit a fresh shuffle.","Clear a matching set of fruit."},ptDescriptions={"Desfaça sua última jogada.","Misture as frutas novamente.","Remova um trio de frutas iguais."};
        for(int i=0;i<3;i++){var p=props.GetArrayElementAtIndex(i);p.FindPropertyRelative("itemType").intValue=(int)E_ItemType.GameProp_1+i;p.FindPropertyRelative("englishName").stringValue=enNames[i];p.FindPropertyRelative("portugueseName").stringValue=ptNames[i];p.FindPropertyRelative("englishDescription").stringValue=enDescriptions[i];p.FindPropertyRelative("portugueseDescription").stringValue=ptDescriptions[i];p.FindPropertyRelative("iconSprite").stringValue=i==0?"Undo":"";}
        data.ApplyModifiedPropertiesWithoutUndo();
        var business=new SerializedObject(page);business.FindProperty("referenceVisual").objectReferenceValue=visual;business.ApplyModifiedPropertiesWithoutUndo();
#endif
    }
    private static void PreviewBoosterReference(GameObject root)
    {
        var visual=root.GetComponent<OrchardBoosterVisual>();if(visual==null)return;
        var data=new SerializedObject(visual);
        foreach(var binding in new[]{new[]{"decoration",BoosterPlate,"Panel"},new[]{"closeImage",BoosterControls,"Close"},new[]{"claimImage",BoosterControls,"Claim"},new[]{"propIcon",BoosterControls,"Undo"}})
        {var image=(Image)data.FindProperty(binding[0]).objectReferenceValue;image.sprite=NamedSprite(binding[1],binding[2]);image.enabled=true;}
    }
}
