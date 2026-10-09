using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

public static partial class OrchardApprovedPass
{
    private const string FidelityAtlas="Assets/OrchardUI/Art/FidelityControls.png";
    private const string FidelityPayments="Assets/OrchardUI/Art/FidelityPayments.png";
    private const string FidelityPills="Assets/OrchardUI/Art/FidelityPills.png";
    private static void ImportFidelityArt()
    {
        AssetDatabase.Refresh();
        ImportFidelitySheet(FidelityAtlas,new[]{"Panel","Card","ButtonGreen","Inset","Badge","Cash","PinkCash","Check"},
            new[]{new Rect(36,64,603,357),new Rect(672,155,543,201),new Rect(27,478,612,170),new Rect(665,475,561,170),new Rect(189,749,335,136),new Rect(788,670,316,279),new Rect(228,955,285,273),new Rect(824,981,235,233)},
            new[]{new Vector4(90,90,90,90),new Vector4(55,55,55,55),new Vector4(95,0,95,0),new Vector4(45,45,45,45),new Vector4(76,0,76,0),Vector4.zero,Vector4.zero,Vector4.zero});
        ImportFidelitySheet(FidelityPayments,new[]{"Pagbank","PIX","Selected","Unselected"},
            new[]{new Rect(23,258,594,215),new Rect(655,252,570,224),new Rect(36,797,574,216),new Rect(643,798,574,216)},
            new[]{Vector4.zero,Vector4.zero,new Vector4(66,66,66,66),new Vector4(66,66,66,66)});
        ImportFidelitySheet(FidelityPills,new[]{"Mint","Footer"},new[]{new Rect(81,319,1375,131),new Rect(180,606,1177,99)},new[]{new Vector4(70,65,70,65),new Vector4(55,49,55,49)});
        var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ApprovedFontPath);
        if(font==null)
        {
            font=TMP_FontAsset.CreateFontAsset(AssetDatabase.LoadAssetAtPath<Font>("Assets/OrchardUI/Fonts/Fidelity/Baloo2-ExtraBold.ttf"),90,10,GlyphRenderMode.SDFAA,1024,1024,AtlasPopulationMode.Dynamic,true);
            font.name="Orchard Baloo 2 ExtraBold";
            font.fallbackFontAssetTable=new List<TMP_FontAsset>{AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/BizzaWZ/Common/Framework/Res/Fonts/MainFont_Simple.asset")};
            AssetDatabase.CreateAsset(font,ApprovedFontPath);AssetDatabase.AddObjectToAsset(font.material,font);
            foreach(var t in font.atlasTextures)AssetDatabase.AddObjectToAsset(t,font);
            font.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789ÀÁÂÃÄÇÈÉÊËÌÍÎÏÑÒÓÔÕÖÙÚÛÜàáâãäçèéêëìíîïñòóôõöùúûü$€≈%/.,:;!?() -+");
        }
        var metrics=font.faceInfo;metrics.ascentLine=73;metrics.descentLine=-18;metrics.lineHeight=96;font.faceInfo=metrics;EditorUtility.SetDirty(font);
        var display=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/OrchardUI/Fonts/LilitaOne SDF.asset");
        var displayMetrics=display.faceInfo;displayMetrics.ascentLine=83.07f;displayMetrics.descentLine=-19.8f;displayMetrics.lineHeight=102.87f;display.faceInfo=displayMetrics;EditorUtility.SetDirty(display);
        approvedFont=font;ExtraSprites.Clear();
#if BIZZA_REAL_WITHDRAW
        var config=AssetDatabase.LoadAssetAtPath<PaymentConfig>("Assets/BizzaWZ/Final/Real/Config/PaymentConfig.asset");
        foreach(var payment in config.PaymentDatas)
        {if(payment.paymentKey=="pagbank")payment.paymentIcon=NamedSprite(FidelityPayments,"Pagbank");if(payment.paymentKey=="pix")payment.paymentIcon=NamedSprite(FidelityPayments,"PIX");}
        EditorUtility.SetDirty(config);
#endif
        AssetDatabase.SaveAssets();
    }
    private static void ImportFidelitySheet(string path,string[] names,Rect[] topRects,Vector4[] borders)
    {
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;
        importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.spritePixelsPerUnit=100;
        importer.GetSourceTextureWidthAndHeight(out int width,out int height);
        var factory=new SpriteDataProviderFactories();factory.Init();var provider=factory.GetSpriteEditorDataProviderFromObject(importer);provider.InitSpriteEditorDataProvider();
        var existing=new Dictionary<string,GUID>();foreach(var r in provider.GetSpriteRects())existing[r.name]=r.spriteID;
        var slices=new SpriteRect[names.Length];var pairs=new List<SpriteNameFileIdPair>();
        for(int i=0;i<names.Length;i++){var id=existing.TryGetValue(names[i],out var prior)?prior:GUID.Generate();Rect r=topRects[i];r.y=height-r.y-r.height;slices[i]=new SpriteRect{name=names[i],spriteID=id,rect=r,border=borders[i],pivot=new Vector2(.5f,.5f),alignment=SpriteAlignment.Center};pairs.Add(new SpriteNameFileIdPair(names[i],id));}
        provider.SetSpriteRects(slices);provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(pairs);provider.Apply();
        foreach(string platform in new[]{"Android","iPhone"}){var p=importer.GetPlatformTextureSettings(platform);p.overridden=true;p.maxTextureSize=2048;p.format=TextureImporterFormat.ASTC_6x6;importer.SetPlatformTextureSettings(p);}
        importer.SaveAndReimport();
    }
    private static bool TryFidelitySurface(Image image,string role)
    {
        if(role!="Panel"&&role!="Card"&&role!="ButtonGreen"&&role!="Inset"&&role!="Badge")return false;
        if(AssetDatabase.LoadAssetAtPath<Texture2D>(FidelityAtlas)==null)return false;
        image.sprite=NamedSprite(FidelityAtlas,role);image.overrideSprite=null;image.material=null;image.color=Color.white;image.enabled=true;
        image.type=Image.Type.Sliced;image.preserveAspect=false;image.pixelsPerUnitMultiplier=role=="Badge"?2.6f:role=="Card"?1.35f:1;
        return true;
    }
    private static void SetFlatMint(Image image)
    {
        image.sprite=NamedSprite(FidelityPills,"Mint");image.overrideSprite=null;image.type=Image.Type.Sliced;
        image.color=Color.white;image.pixelsPerUnitMultiplier=2f;image.raycastTarget=false;
    }
    private static void FinalizeFidelityWithdrawal(GameObject root)
    {
#if BIZZA_REAL_WITHDRAW
        const string info="Content/Scroll View/Viewport/Content/WithdrawInfo/";
        SetFlatMint(Need(root,info+"ApprovedHint").GetComponent<Image>());
        foreach(string path in new[]{"CoinInfo/CurrentCount/Image","CoinInfo/RateCount/Image"})
        {
            var t=Need(root,info+path);var amend=t.GetComponent<WzIconAmend>();if(amend!=null)UnityEngine.Object.DestroyImmediate(amend);
            var im=t.GetComponent<Image>();im.sprite=NamedSprite(FidelityAtlas,"PinkCash");im.type=Image.Type.Simple;im.preserveAspect=true;
        }
        var method=Need(root,info+"WithdrawMode/WithdrawWayTitle").GetComponent<TMP_Text>();method.alignment=TextAlignmentOptions.MidlineLeft;
        var footer=Need(root,"Content/Scroll View/Viewport/Content/WithdrawLevel/ProgressInfo").GetComponent<Image>();
        footer.sprite=NamedSprite(FidelityPills,"Footer");footer.type=Image.Type.Sliced;footer.pixelsPerUnitMultiplier=1.4f;
        var menu=Need(root,"ServiceBtn");Place(root,menu,769,945,46,46);
        LocalRect(Need(root,"ServiceBtn/Image"),30,-5,18,18);
        // The approved screen exposes support through its existing Help -> Support route.
        menu.gameObject.SetActive(false);
        var page=root.GetComponent<RealWithdrawPanel>();
        var pageSettings=new SerializedObject(page);
        pageSettings.FindProperty("normalSprite").objectReferenceValue=NamedSprite(FidelityAtlas,"ButtonGreen");
        pageSettings.FindProperty("canWithdrawSprite").objectReferenceValue=NamedSprite(FidelityAtlas,"ButtonGreen");
        pageSettings.ApplyModifiedPropertiesWithoutUndo();
        page.clashTxt.font=ApprovedFont;OrchardSkinAuthoring.SetBody(page.clashTxt);page.clashTxt.color=ApprovedGreen;
        page.clashTxt.characterSpacing=0;page.clashTxt.wordSpacing=0;
        page.clashTxt.rectTransform.localScale=new Vector3(.86f,1,1);
        var exchangeHint=Need(root,info+"MoreWithdraw_Hint").GetComponent<TMP_Text>();
        exchangeHint.fontSize=30;exchangeHint.fontSizeMax=30;
        foreach(var tx in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if(tx.fontSharedMaterial==null||!tx.fontSharedMaterial.name.Contains("Title"))continue;
            tx.font=ApprovedFont;OrchardSkinAuthoring.SetTitle(tx);
            tx.fontSharedMaterial.SetFloat("_FaceDilate",.20f);
            tx.fontSharedMaterial.SetFloat("_OutlineWidth",.16f);
            tx.fontSharedMaterial.SetColor("_UnderlayColor",new Color32(66,36,15,230));
            tx.fontSharedMaterial.SetFloat("_UnderlayOffsetY",-.35f);
            tx.fontSharedMaterial.SetFloat("_UnderlaySoftness",.1f);
            tx.fontSharedMaterial.EnableKeyword("UNDERLAY_ON");EditorUtility.SetDirty(tx.fontSharedMaterial);
        }
        var action=Need(root,info+"WithdrawBtn/Btn/Text (TMP)").GetComponent<TMP_Text>();
        const string actionMaterialPath="Assets/OrchardUI/Generated/FidelityActionTitle.mat";
        var actionMaterial=AssetDatabase.LoadAssetAtPath<Material>(actionMaterialPath);
        if(actionMaterial==null){actionMaterial=new Material(action.fontSharedMaterial);AssetDatabase.CreateAsset(actionMaterial,actionMaterialPath);}
        else actionMaterial.CopyPropertiesFromMaterial(action.fontSharedMaterial);
        actionMaterial.SetColor("_OutlineColor",new Color32(0,77,26,255));actionMaterial.SetColor("_UnderlayColor",new Color32(0,63,21,230));
        action.fontSharedMaterial=actionMaterial;EditorUtility.SetDirty(actionMaterial);
        StyleWithdrawalProgress(root);
#endif
    }
}
