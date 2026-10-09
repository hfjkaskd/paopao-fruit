using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static partial class OrchardApprovedPass
{
    private const string SpinBackArt="Assets/OrchardUI/Art/SpinReferenceBack.png";
    private static void ImportSpinReference()
    {
        AssetDatabase.Refresh();
        ImportFidelitySheet(SpinBackArt,new[]{"Back"},new[]{new Rect(109,112,1039,1043)},new[]{Vector4.zero});
        var importer=(TextureImporter)AssetImporter.GetAtPath(SpinBackArt);importer.maxTextureSize=512;
        foreach(string platform in new[]{"Android","iPhone"}){var settings=importer.GetPlatformTextureSettings(platform);settings.maxTextureSize=512;settings.format=TextureImporterFormat.ASTC_4x4;importer.SetPlatformTextureSettings(settings);}
        importer.SaveAndReimport();ExtraSprites.Clear();
    }
    private static void FinalizeSpinReference(GameObject root)
    {
#if BIZZA_REAL_WITHDRAW
        var page=root.GetComponent<SlotPanel>();var button=page.closeBtn;var back=button.GetComponent<Image>();
        Place(root,button.transform,22,49,111,112);back.sprite=NamedSprite(SpinBackArt,"Back");back.overrideSprite=null;back.enabled=true;back.color=Color.white;back.type=Image.Type.Simple;back.preserveAspect=true;back.raycastTarget=true;button.targetGraphic=back;
        var title=Need(root,"ApprovedTitle").GetComponent<TMP_Text>();Place(root,title.transform,216,79,420,104);SetReferenceTitle(title,ApprovedInk);SizeText(title,86);title.fontSizeMin=42;title.alignment=TextAlignmentOptions.Center;title.enableWordWrapping=false;title.rectTransform.localScale=new Vector3(.96f,1.08f,1);
        var arch=title.GetComponent<OrchardArchedText>()??title.gameObject.AddComponent<OrchardArchedText>();var archSo=new SerializedObject(arch);archSo.FindProperty("archHeight").floatValue=8;archSo.ApplyModifiedPropertiesWithoutUndo();
        var count=Need(root,"Content/ApprovedFreeCount").GetComponent<TMP_Text>();Place(root,count.transform,246,982,360,78);SetReferenceTitle(count,ApprovedInk);SizeText(count,48);count.alignment=TextAlignmentOptions.Center;count.enableWordWrapping=false;count.overflowMode=TextOverflowModes.Ellipsis;count.rectTransform.localScale=new Vector3(.96f,1.12f,1);
        Need(root,"Content/ApprovedFreeCaption").gameObject.SetActive(false);
        var so=new SerializedObject(page);so.FindProperty("freeSpinEnglish").stringValue="{0} FREE SPIN";so.FindProperty("freeSpinPortuguese").stringValue="{0} GIRO GRÁTIS";so.ApplyModifiedPropertiesWithoutUndo();count.text="0 GIRO GRÁTIS";
        OrchardSlotFunctionAuthoring.Configure(root);
#endif
    }
}
