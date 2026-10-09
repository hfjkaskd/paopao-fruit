using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

public static partial class OrchardApprovedPass
{
    private const string ApprovedFontPath = "Assets/OrchardUI/Fonts/Fidelity/Baloo2 ExtraBold SDF.asset";
    private static TMP_FontAsset approvedFont;
    private static TMP_FontAsset ApprovedFont
    {
        get
        {
            if (approvedFont == null) approvedFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ApprovedFontPath);
            return approvedFont;
        }
    }
    private static void ImportApprovedArt()
    {
        OrchardSkinAuthoring.ImportArt();
        const string heroes = "Assets/OrchardUI/Art/ServiceHeroes.png";
        var importer = (TextureImporter)AssetImporter.GetAtPath(heroes);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 100; importer.mipmapEnabled = false; importer.isReadable = false;
        importer.alphaIsTransparency = true; importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear; importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = 2048; importer.textureCompression = TextureImporterCompression.Uncompressed;
        foreach (string platform in new[] { "Android", "iPhone" })
        {
            var settings = importer.GetPlatformTextureSettings(platform);
            settings.overridden = true; settings.maxTextureSize = 2048; settings.format = TextureImporterFormat.ASTC_6x6;
            importer.SetPlatformTextureSettings(settings);
        }
        var factories = new SpriteDataProviderFactories(); factories.Init();
        var provider = factories.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
        var previous = new Dictionary<string, GUID>(); foreach (var rect in provider.GetSpriteRects()) previous[rect.name] = rect.spriteID;
        var names = new[] { "Envelope", "Clock" };
        var bounds = new[] { new Rect(55, 95, 996, 566), new Rect(1085, 90, 798, 646) };
        var slices = new SpriteRect[2]; var pairs = new List<SpriteNameFileIdPair>();
        for (int i = 0; i < 2; i++)
        {
            var id = previous.TryGetValue(names[i], out var old) ? old : GUID.Generate();
            slices[i] = new SpriteRect { name = names[i], spriteID = id, rect = bounds[i], pivot = new Vector2(.5f, .5f), alignment = SpriteAlignment.Center };
            pairs.Add(new SpriteNameFileIdPair(names[i], id));
        }
        provider.SetSpriteRects(slices); provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(pairs);
        provider.Apply(); importer.SaveAndReimport(); ExtraSprites.Clear();
        ImportSystemIcons();
        ImportRewardArt();
        ImportPaymentLogos();
        ImportReelsAndLoading();
        if (ApprovedFont == null)
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>("Assets/OrchardUI/Fonts/LilitaOne-Regular.ttf");
            if (font == null) throw new FileNotFoundException("The approved UI font has not imported.");
            approvedFont = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            approvedFont.name = "Orchard Lilita One";
            var fallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/BizzaWZ/Common/Framework/Res/Fonts/MainFont_Simple.asset");
            approvedFont.fallbackFontAssetTable = new List<TMP_FontAsset>();
            if (fallback != null) approvedFont.fallbackFontAssetTable.Add(fallback);
            AssetDatabase.CreateAsset(approvedFont, ApprovedFontPath);
            AssetDatabase.AddObjectToAsset(approvedFont.material, approvedFont);
            foreach (var texture in approvedFont.atlasTextures) AssetDatabase.AddObjectToAsset(texture, approvedFont);
            approvedFont.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789ÀÁÂÃÄÇÈÉÊËÌÍÎÏÑÒÓÔÕÖÙÚÛÜàáâãäçèéêëìíîïñòóôõöùúûü$€≈%/.,:;!?() -+");
        }
        AssetDatabase.SaveAssets();
    }
    private static void ImportReelsAndLoading()
    {
        var importer=(TextureImporter)AssetImporter.GetAtPath("Assets/OrchardUI/Art/OrchardReelSymbols.png");
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=4096;importer.textureCompression=TextureImporterCompression.Uncompressed;
        var factory=new SpriteDataProviderFactories();factory.Init();var provider=factory.GetSpriteEditorDataProviderFromObject(importer);provider.InitSpriteEditorDataProvider();
        var names=new[]{"Apple","Pear","Coin","Juice","Gem"};var bounds=new[]{new Rect(35,130,402,431),new Rect(468,128,374,445),new Rect(870,125,412,440),new Rect(1290,124,416,444),new Rect(1714,118,437,430)};
        var ids=new Dictionary<string,GUID>();foreach(var r in provider.GetSpriteRects())ids[r.name]=r.spriteID;
        var rects=new SpriteRect[5];var pairs=new List<SpriteNameFileIdPair>();
        for(int i=0;i<5;i++){var id=ids.TryGetValue(names[i],out var old)?old:GUID.Generate();rects[i]=new SpriteRect{name=names[i],spriteID=id,rect=bounds[i],pivot=new Vector2(.5f,.5f),alignment=SpriteAlignment.Center};pairs.Add(new SpriteNameFileIdPair(names[i],id));}
        provider.SetSpriteRects(rects);provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(pairs);provider.Apply();importer.SaveAndReimport();
        var loading=(TextureImporter)AssetImporter.GetAtPath("Assets/OrchardUI/Resources/OrchardUI/ApprovedLoading.png");
        loading.textureType=TextureImporterType.Sprite;loading.spriteImportMode=SpriteImportMode.Single;loading.spritePixelsPerUnit=100;loading.mipmapEnabled=false;loading.alphaIsTransparency=false;loading.npotScale=TextureImporterNPOTScale.None;loading.maxTextureSize=2048;
        foreach(string platform in new[]{"Android","iPhone"}){var ps=loading.GetPlatformTextureSettings(platform);ps.overridden=true;ps.maxTextureSize=2048;ps.format=TextureImporterFormat.ASTC_6x6;loading.SetPlatformTextureSettings(ps);}loading.SaveAndReimport();
    }
    private static void ImportPaymentLogos()
    {
#if BIZZA_REAL_WITHDRAW
        const string configPath="Assets/BizzaWZ/Final/Real/Config/PaymentConfig.asset";
        string backup=Output+"BeforeAdditional/"+configPath;
        if(!File.Exists(backup)){Directory.CreateDirectory(Path.GetDirectoryName(backup));File.Copy(configPath,backup);}
        var config=AssetDatabase.LoadAssetAtPath<PaymentConfig>(configPath);
        foreach(var data in config.PaymentDatas)
        {
            if(data.paymentIcon==null)continue;
            string source=AssetDatabase.GetAssetPath(data.paymentIcon);
            string path="Assets/OrchardUI/Art/Payment-"+data.paymentKey+".png";
            if(source==path)continue;
            File.Copy(source,path,true);AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;importer.mipmapEnabled=false;
            importer.alphaIsTransparency=true;importer.npotScale=TextureImporterNPOTScale.None;importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.GetSourceTextureWidthAndHeight(out int w,out int h);
            var factory=new SpriteDataProviderFactories();factory.Init();var provider=factory.GetSpriteEditorDataProviderFromObject(importer);provider.InitSpriteEditorDataProvider();
            var id=GUID.Generate();string name=data.paymentKey;
            var rect=new SpriteRect{name=name,spriteID=id,rect=new Rect(w*.075f,h*.2f,w*.85f,h*.6f),pivot=new Vector2(.5f,.5f),alignment=SpriteAlignment.Center};
            provider.SetSpriteRects(new[]{rect});provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(new[]{new SpriteNameFileIdPair(name,id)});provider.Apply();importer.SaveAndReimport();
            data.paymentIcon=NamedSprite(path,name);
        }
        EditorUtility.SetDirty(config);AssetDatabase.SaveAssets();
#endif
    }
    private static void ImportRewardArt()
    {
        const string path = "Assets/OrchardUI/Art/RewardHeroes.png";
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit=100;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.isReadable=false;
        importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;
        var factory=new SpriteDataProviderFactories();factory.Init();var provider=factory.GetSpriteEditorDataProviderFromObject(importer);provider.InitSpriteEditorDataProvider();
        var ids=new Dictionary<string,GUID>();foreach(var r in provider.GetSpriteRects())ids[r.name]=r.spriteID;
        var names=new[]{"Chest","Tray","Gift","Rate"};var rects=new SpriteRect[4];var pairs=new List<SpriteNameFileIdPair>();
        for(int i=0;i<4;i++){var id=ids.TryGetValue(names[i],out var old)?old:GUID.Generate();rects[i]=new SpriteRect{name=names[i],spriteID=id,rect=new Rect(i%2*768,i<2?512:0,768,512),pivot=new Vector2(.5f,.5f),alignment=SpriteAlignment.Center};pairs.Add(new SpriteNameFileIdPair(names[i],id));}
        provider.SetSpriteRects(rects);provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(pairs);provider.Apply();
        foreach(string platform in new[]{"Android","iPhone"}){var ps=importer.GetPlatformTextureSettings(platform);ps.overridden=true;ps.maxTextureSize=2048;ps.format=TextureImporterFormat.ASTC_6x6;importer.SetPlatformTextureSettings(ps);}
        importer.SaveAndReimport();
        var machine=(TextureImporter)AssetImporter.GetAtPath("Assets/OrchardUI/Art/OrchardPrizeCabinet.png");
        machine.textureType=TextureImporterType.Sprite;machine.spriteImportMode=SpriteImportMode.Single;machine.alphaIsTransparency=true;machine.mipmapEnabled=false;machine.isReadable=false;machine.npotScale=TextureImporterNPOTScale.None;machine.maxTextureSize=2048;
        foreach(string platform in new[]{"Android","iPhone"}){var ps=machine.GetPlatformTextureSettings(platform);ps.overridden=true;ps.maxTextureSize=2048;ps.format=TextureImporterFormat.ASTC_6x6;machine.SetPlatformTextureSettings(ps);}machine.SaveAndReimport();
    }
    private static void ImportSystemIcons()
    {
        const string path = "Assets/OrchardUI/Art/SystemIcons.png";
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 100; importer.mipmapEnabled = false; importer.isReadable = false;
        importer.alphaIsTransparency = true; importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear; importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = 2048; importer.textureCompression = TextureImporterCompression.Uncompressed;
        foreach (string platform in new[] { "Android", "iPhone" })
        {
            var settings = importer.GetPlatformTextureSettings(platform);
            settings.overridden = true; settings.maxTextureSize = 2048; settings.format = TextureImporterFormat.ASTC_6x6;
            importer.SetPlatformTextureSettings(settings);
        }
        var names = new[] { "Fruits", "Cloud", "Music", "Sound", "Vibration", "Language" };
        var topRects = new[] { new Rect(29, 39, 638, 378), new Rect(699, 79, 492, 351), new Rect(188, 477, 313, 351), new Rect(753, 507, 379, 330), new Rect(174, 877, 358, 349), new Rect(767, 875, 340, 340) };
        var factory = new SpriteDataProviderFactories(); factory.Init();
        var provider = factory.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
        var oldIds = new Dictionary<string, GUID>(); foreach (var r in provider.GetSpriteRects()) oldIds[r.name] = r.spriteID;
        var rects = new SpriteRect[names.Length]; var pairs = new List<SpriteNameFileIdPair>();
        for (int i = 0; i < names.Length; i++)
        {
            var id = oldIds.TryGetValue(names[i], out var old) ? old : GUID.Generate();
            Rect r = topRects[i]; r.y = 1254 - r.y - r.height;
            rects[i] = new SpriteRect { name = names[i], spriteID = id, rect = r, pivot = new Vector2(.5f, .5f), alignment = SpriteAlignment.Center };
            pairs.Add(new SpriteNameFileIdPair(names[i], id));
        }
        provider.SetSpriteRects(rects); provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(pairs); provider.Apply();
        importer.SaveAndReimport();
    }
}
