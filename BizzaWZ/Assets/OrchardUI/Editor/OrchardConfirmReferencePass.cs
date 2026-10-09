using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static partial class OrchardApprovedPass
{
    private const string ConfirmControls="Assets/OrchardUI/Resources/OrchardUI/ConfirmReferenceControls.png";
    private static void ConfirmTextShape(TMP_Text text,float x,float y,bool left,float verticalShift=0)
    {
        var rect=text.rectTransform;
        if(left){float width=rect.rect.width;rect.sizeDelta=new Vector2(width/x,rect.sizeDelta.y);}
        rect.localScale=new Vector3(x,y,1);
        rect.anchoredPosition+=new Vector2(0,verticalShift);
    }
    private static void ImportConfirmReference()
    {
        AssetDatabase.Refresh();
        ImportFidelitySheet(ConfirmControls,new[]{"Title","Envelope","Amount","Method","Row","Hint","Confirm","Edit"},
            new[]{new Rect(122,84,671,174),new Rect(159,299,602,343),new Rect(39,657,843,204),new Rect(44,883,835,134),
                new Rect(46,1031,832,127),new Rect(43,1173,837,107),new Rect(39,1304,842,186),new Rect(157,1514,608,122)},
            new[]{Vector4.zero,Vector4.zero,Vector4.zero,Vector4.zero,Vector4.zero,Vector4.zero,Vector4.zero,Vector4.zero});
        ExtraSprites.Clear();
    }
    private static void FinalizeConfirmReference(GameObject root)
    {
#if BIZZA_REAL_WITHDRAW
        var page=root.GetComponent<UIWithdrawalConfirmPanel>();
        var paths=new[]{"Title/ApprovedPlaque","ApprovedHero","ApprovedAmountPanel","ApprovedMethodPanel","txtContext/Name/ApprovedRow","txtContext/CPF/ApprovedRow","txtContext/Account/ApprovedRow","ReferenceInfoCard","BtnOk","ApprovedEdit"};
        var names=new[]{"Title","Envelope","Amount","Method","Row","Row","Row","Hint","Confirm","Edit"};
        var visual=root.GetComponent<OrchardConfirmVisual>()??root.AddComponent<OrchardConfirmVisual>();
        var data=new SerializedObject(visual);data.FindProperty("resourcePath").stringValue="OrchardUI/ConfirmReferenceControls";
        var bindings=data.FindProperty("bindings");bindings.arraySize=paths.Length;
        for(int i=0;i<paths.Length;i++)
        {
            var image=Need(root,paths[i]).GetComponent<Image>();CashImage(image);
            if(image.TryGetComponent<Button>(out var button)){image.raycastTarget=true;button.targetGraphic=image;}
            var binding=bindings.GetArrayElementAtIndex(i);binding.FindPropertyRelative("image").objectReferenceValue=image;binding.FindPropertyRelative("sprite").stringValue=names[i];
        }
        data.ApplyModifiedPropertiesWithoutUndo();
        Need(root,"ApprovedHero").GetComponent<Image>().preserveAspect=true;
        Place(root,Need(root,"ApprovedHero"),159,288,495,282);
        Place(root,Need(root,"ReferenceInfoCard"),70,1240,688,87);
        Need(root,"ApprovedHint").gameObject.SetActive(false);Need(root,"ReferenceInfoIcon").gameObject.SetActive(false);
        var hint=Need(root,"Text (TMP)").GetComponent<TMP_Text>();Place(root,hint.transform,191,1254,526,58);
        ReferenceBody(hint);SizeText(hint,36);hint.alignment=TextAlignmentOptions.MidlineLeft;hint.enableWordWrapping=false;
        hint.color=new Color32(0,83,151,255);
        ConfirmTextShape(hint,.96f,1.13f,true,1.5f);
        var edit=Need(root,"ApprovedEdit/Label").GetComponent<TMP_Text>();SizeText(edit,49);edit.color=new Color32(0,80,153,255);edit.rectTransform.localScale=new Vector3(.88f,1,1);
        edit.rectTransform.anchoredPosition+=new Vector2(0,-2);
        ConfirmTextShape(Need(root,"Title/Text (TMP)").GetComponent<TMP_Text>(),.93f,1.06f,false,2);
        ConfirmTextShape(Need(root,"ApprovedAmountTitle").GetComponent<TMP_Text>(),.9f,1,false,-1);
        ConfirmTextShape(page.PaymentValueText,.927f,1.138f,false,-2);
        ConfirmTextShape(Need(root,"BtnOk/Text (TMP)").GetComponent<TMP_Text>(),.838f,.908f,false,3);
        page.paymentImage.type=Image.Type.Simple;page.paymentImage.preserveAspect=true;
        foreach(var text in new[]{page.NameText,page.CPF_CNPJText,page.EmailText})
        {SizeText(text,36);text.enableWordWrapping=false;text.overflowMode=TextOverflowModes.Ellipsis;}
        BindCopy(Need(root,"ApprovedMethodLabel").GetComponent<TMP_Text>(),"Method","Método");
        ConfirmTextShape(Need(root,"ApprovedMethodLabel").GetComponent<TMP_Text>(),.83f,.9f,true,-1);
        ConfirmTextShape(page.NameText,.916f,1.067f,true,-4);
        ConfirmTextShape(page.CPF_CNPJText,1.22f,.958f,true,1);
        ConfirmTextShape(page.EmailText,.953f,1.107f,true,-3);
#endif
    }
    private static void PreviewConfirmReference(GameObject root)
    {
        var visual=root.GetComponent<OrchardConfirmVisual>();if(visual==null)return;
        var sprites=new List<Sprite>();foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(ConfirmControls))if(asset is Sprite sprite)sprites.Add(sprite);
        visual.ApplyArtwork(sprites.ToArray());
    }
}
