using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>Asset authoring corrections against all approved September screen references.</summary>
public static partial class OrchardApprovedPass
{
    private const string DetailAtlas = "Assets/OrchardUI/Art/ReferenceDetails.png";
    private const string ServiceDetailAtlas = "Assets/OrchardUI/Art/ReferenceServiceDetails.png";
    private const string HeroDetailAtlas = "Assets/OrchardUI/Resources/OrchardUI/ReferenceHeroes.png";
    private const string MedalAtlas = "Assets/OrchardUI/Art/ReferenceMedals.png";
    private const string FinishAtlas = "Assets/OrchardUI/Art/ReferenceFinish.png";
    private static void ImportReferenceDetails()
    {
        AssetDatabase.Refresh();
        OrchardSkinAuthoring.ImportArt();
        ImportReferenceBodyFont();
        ImportFidelitySheet(FinishAtlas,new[]{"GuideBubble","Glove","CoinStack","BlueDisc","LockedDisc","Pencil"},
            new[]{new Rect(15,116,551,307),new Rect(585,74,397,403),new Rect(1024,116,507,328),new Rect(43,548,419,411),new Rect(565,562,404,398),new Rect(1115,559,381,381)},
            new[]{new Vector4(110,80,80,80),Vector4.zero,Vector4.zero,Vector4.zero,Vector4.zero,Vector4.zero});
        ImportFidelitySheet(HeroDetailAtlas,new[]{"CelebrationChest","MagicWand","FullTray","GreenHalo"},
            new[]{new Rect(12,10,786,514),new Rect(946,35,445,490),new Rect(29,538,771,465),new Rect(893,524,597,493)},
            new[]{Vector4.zero,Vector4.zero,Vector4.zero,Vector4.zero});
        ImportFidelitySheet(MedalAtlas,new[]{"Bronze","Silver","Gold","Platinum","Send","ChatAvatar"},
            new[]{new Rect(79,98,405,380),new Rect(562,98,405,380),new Rect(1057,98,399,380),new Rect(77,552,407,383),new Rect(583,559,364,367),new Rect(1076,556,362,367)},
            new[]{Vector4.zero,Vector4.zero,Vector4.zero,Vector4.zero,Vector4.zero,Vector4.zero});
        ImportFidelitySheet(DetailAtlas,
            new[]{"WoodTitle","WoodSubtitle","Video","BonusLeaf","OrangeFill","Orb","ToggleOn","ToggleOff","Knob"},
            new[]{new Rect(44,55,682,290),new Rect(754,181,433,130),new Rect(1220,116,277,240),new Rect(112,397,307,319),new Rect(495,517,654,130),new Rect(1217,449,268,263),new Rect(41,776,578,201),new Rect(668,774,543,202),new Rect(1284,781,200,198)},
            new[]{new Vector4(110,65,110,65),new Vector4(70,55,70,55),Vector4.zero,Vector4.zero,new Vector4(75,0,75,0),Vector4.zero,new Vector4(100,0,100,0),new Vector4(100,0,100,0),Vector4.zero});
        ImportFidelitySheet(ServiceDetailAtlas,new[]{"Alarm","ClosedGift","SproutCoin","ChatBlue","ChatMint","SoftBlue"},
            new[]{new Rect(20,158,485,352),new Rect(520,104,552,445),new Rect(1143,213,270,287),new Rect(15,666,499,213),new Rect(533,666,491,213),new Rect(1039,682,481,199)},
            new[]{Vector4.zero,Vector4.zero,Vector4.zero,new Vector4(65,65,65,65),new Vector4(65,65,65,65),new Vector4(100,0,100,0)});
        var importer=(TextureImporter)AssetImporter.GetAtPath("Assets/OrchardUI/Resources/OrchardUI/RewardBackdrop.png");
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.mipmapEnabled=false;
        importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=2048;
        foreach(string platform in new[]{"Android","iPhone"}){var p=importer.GetPlatformTextureSettings(platform);p.overridden=true;p.maxTextureSize=2048;p.format=TextureImporterFormat.ASTC_6x6;importer.SetPlatformTextureSettings(p);}
        importer.SaveAndReimport();ExtraSprites.Clear();
    }
    private static void PaintReferenceDetail(Image image,string name)
    {
        string atlas=name=="Alarm"||name=="ClosedGift"||name=="SproutCoin"||name=="ChatBlue"||name=="ChatMint"||name=="SoftBlue"?ServiceDetailAtlas:DetailAtlas;
        if(name=="CelebrationChest"||name=="MagicWand"||name=="FullTray"||name=="GreenHalo")atlas=HeroDetailAtlas;
        if(name=="Bronze"||name=="Silver"||name=="Gold"||name=="Platinum"||name=="Send"||name=="ChatAvatar")atlas=MedalAtlas;
        if(name=="GuideBubble"||name=="Glove"||name=="CoinStack"||name=="BlueDisc"||name=="LockedDisc"||name=="Pencil")atlas=FinishAtlas;
        image.sprite=NamedSprite(atlas,name);image.overrideSprite=null;image.color=Color.white;image.enabled=true;
        image.type=image.sprite.border==Vector4.zero?Image.Type.Simple:Image.Type.Sliced;
        image.preserveAspect=image.type==Image.Type.Simple;image.pixelsPerUnitMultiplier=1;
        if(name=="WoodTitle"||name=="WoodSubtitle"){image.type=Image.Type.Simple;image.preserveAspect=false;}
    }
    private static Image Detail(GameObject root,string path,string sprite,float x,float y,float w,float h,bool behind=false)
    {
        var t=Ensure(root,path);var im=t.GetComponent<Image>()??t.gameObject.AddComponent<Image>();
        Paint(im,sprite);im.raycastTarget=false;Place(root,t,x,y,w,h);if(behind)t.SetAsFirstSibling();return im;
    }
    private static TMP_Text Copy(GameObject root,string path,string en,string pt,float x,float y,float w,float h,float size,bool title=false)
    {
        var text=ApprovedDynamicText(root,path,x,y,w,h,size);BindCopy(text,en,pt);
        text.gameObject.SetActive(true);text.enabled=true;
        if(title)SetReferenceTitle(text,ApprovedInk);return text;
    }
    private static void SizeText(TMP_Text text,float size)
    {
        text.fontSize=text.fontSizeMax=size;text.fontSizeMin=size*.72f;
        text.enableAutoSizing=true;text.margin=Vector4.zero;
    }
    private static void SetReferenceTitle(TMP_Text text,Color outline)
    {
        string tag=outline.b>outline.g&&outline.b>outline.r?"Blue":outline.g>outline.r?"Green":"Wood";
        string path="Assets/OrchardUI/Generated/ReferenceTitle"+tag+".mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material==null){material=new Material(ApprovedFont.material);AssetDatabase.CreateAsset(material,path);}
        material.SetColor("_FaceColor",Color.white);material.SetFloat("_FaceDilate",.18f);
        material.SetFloat("_OutlineWidth",.18f);material.SetColor("_OutlineColor",outline);material.EnableKeyword("OUTLINE_ON");
        material.SetColor("_UnderlayColor",new Color(outline.r*.65f,outline.g*.65f,outline.b*.65f,.85f));
        material.SetFloat("_UnderlayOffsetY",-.55f);material.SetFloat("_UnderlayDilate",.12f);material.SetFloat("_UnderlaySoftness",.08f);material.EnableKeyword("UNDERLAY_ON");
        EditorUtility.SetDirty(material);text.font=ApprovedFont;text.fontSharedMaterial=material;text.color=Color.white;text.UpdateMeshPadding();
    }
    private static void RoundFill(Image image,bool orange=false)
    {
        if(image==null)return;
        if(orange)PaintReferenceDetail(image,"OrangeFill");else Paint(image,"ButtonGreen");
        image.type=Image.Type.Sliced;image.preserveAspect=false;
        image.pixelsPerUnitMultiplier=image.sprite.rect.height/Mathf.Max(18,image.rectTransform.rect.height);
        var rounded=image.GetComponent<OrchardRoundedFill>()??image.gameObject.AddComponent<OrchardRoundedFill>();
        var rect=image.rectTransform;var parent=(RectTransform)rect.parent;
        Vector3 corner=parent.InverseTransformPoint(rect.TransformPoint(new Vector3(rect.rect.xMin,rect.rect.yMax,0)));
        var so=new SerializedObject(rounded);so.FindProperty("image").objectReferenceValue=image;
        so.FindProperty("fullSize").vector2Value=rect.rect.size;
        so.FindProperty("topLeft").vector2Value=new Vector2(corner.x-parent.rect.xMin,corner.y-parent.rect.yMax);so.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void FinalizeReferenceDetails(GameObject root,string name)
    {
        if(name=="withdraw-main")return;
        // Match reference glyph height, not just the nominal TMP point size.
        foreach(var text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            bool title=text.fontSharedMaterial!=null&&text.fontSharedMaterial.name.IndexOf("Title",StringComparison.OrdinalIgnoreCase)>=0;
            if(title)
            {
                var button=text.GetComponentInParent<Button>();Color outline=ApprovedInk;
                if(button!=null&&button.targetGraphic is Image im&&im.sprite!=null)
                    outline=im.sprite.name.Contains("Blue")?new Color32(0,88,163,255):new Color32(0,78,25,255);
                SetReferenceTitle(text,outline);
            }
        }
        if(name=="settings")ReferenceSettings(root);
        if(name=="level-complete")ReferenceWin(root);
        if(name=="get-booster")
        {
            var orb=Detail(root,"Content/ApprovedOrb","detail:Orb",260,558,335,326,true);
            // The existing business-controlled prop icon remains above the authored halo.
            var prop=Need(root,"Content/PropIcon");Place(root,prop,295,594,268,271);
            Place(root,Need(root,"Content/PropName"),199,886,454,99);SizeText(Need(root,"Content/PropName").GetComponent<TMP_Text>(),96);
            Copy(root,"Content/ApprovedToolName","One helpful move at a time.","Uma jogada de cada vez.",116,991,620,64,43);
            var icon=Need(root,"Content/BtnGroup/ButtonAnim/AdBtn/Image (3)").GetComponent<Image>();PaintReferenceDetail(icon,"Video");
        }
        if(name=="new-booster")
        {
            Detail(root,"MainContent/ApprovedUnlockedPlaque","detail:WoodSubtitle",254,423,345,84);
            Copy(root,"MainContent/ApprovedUnlocked","UNLOCKED","DESBLOQUEADA",277,430,298,64,47,true);
            SetFlatMint(Need(root,"MainContent/ApprovedReady").GetComponent<Image>());
            Need(root,"MainContent/ApprovedReadyText").GetComponent<TMP_Text>().color=ApprovedGreen;
            Need(root,"MainContent/ApprovedReady").SetSiblingIndex(Need(root,"MainContent/ApprovedReadyText").GetSiblingIndex());
            var halo=Detail(root,"MainContent/ApprovedHalo","detail:GreenHalo",160,476,552,552);halo.transform.SetSiblingIndex(Need(root,"MainContent/Body").GetSiblingIndex());
            Need(root,"MainContent/ApprovedReady").SetAsLastSibling();Need(root,"MainContent/ApprovedReadyText").SetAsLastSibling();
            Need(root,"MainContent/ApprovedUnlockedPlaque").SetAsLastSibling();Need(root,"MainContent/ApprovedUnlocked").SetAsLastSibling();
        }
        if(name=="revive")
        {
            Detail(root,"bg/ApprovedLevelPlaque","detail:WoodSubtitle",305,410,254,81);
            Copy(root,"bg/ApprovedLevelCaption","KEEP GOING","CONTINUE",322,422,220,59,36,true);
            var caption=Need(root,"bg/ApprovedLevelCaption");var local=caption.GetComponent<OrchardLocalizedLabel>();if(local!=null)Object.DestroyImmediate(local);
            var lose=new SerializedObject(root.GetComponent<LosePanel>());lose.FindProperty("levelCaption").objectReferenceValue=caption.GetComponent<TMP_Text>();lose.ApplyModifiedPropertiesWithoutUndo();
            Copy(root,"bg/ApprovedReviveDescription","Make room and keep harvesting.","Abra espaço e continue colhendo.",116,977,620,65,43);
            Detail(root,"bg/ApprovedReviveIcon","asset:Assets/OrchardUI/Resources/OrchardUI/PropGoldenUndo.png",352,1022,148,148);
            PaintReferenceDetail(Need(root,"bg/ReviveBtnGroup/ReviveBtn/Ad_Image").GetComponent<Image>(),"Video");
        }
        if(name=="withdraw-account")ReferenceAccount(root);
        if(name=="withdraw-confirm")
        {
            foreach(string path in new[]{"ApprovedMethodLabel","txtContext/Name/Text (TMP)","txtContext/CPF/Text (TMP)","txtContext/Account/Title"})
                Need(root,path).GetComponent<TMP_Text>().alignment=TextAlignmentOptions.MidlineLeft;
            var hint=Need(root,"Text (TMP)").GetComponent<TMP_Text>();BindCopy(hint,"Check your account before submitting.","Confira sua conta antes de enviar.");hint.color=new Color32(0,84,160,255);
            ReferenceInfoCard(root,hint,74,1208,699,80);
            PaintReferenceDetail(Need(root,"ApprovedEdit").GetComponent<Image>(),"SoftBlue");
        }
        if(name=="withdraw-pending")
        {
#if BIZZA_REAL_WITHDRAW
            var p=root.GetComponent<UIWithdrawalPendingPanel>();RoundFill(p.progressFill);OrchardSkinAuthoring.SetBody(p.progressText);p.progressText.color=ApprovedInk;
#endif
        }
        if(name=="cash-withdraw")
        {
#if BIZZA_REAL_WITHDRAW
            var p=root.GetComponent<FakeWithdrawPanel>();RoundFill(p.progressImg);
            BindCopy(Need(root,"Content/CashBalance/Title").GetComponent<TMP_Text>(),"My balance","Meu saldo");
            BindCopy(Need(root,"Content/WithdrawAmount/Title").GetComponent<TMP_Text>(),"Select amount","Selecionar valor");
            BindCopy(Need(root,"Content/WithdrawProgress/Title").GetComponent<TMP_Text>(),"Current goal","Meta atual");
            BindCopy(Need(root,"Content/WithdrawBtn/Text").GetComponent<TMP_Text>(),"Withdraw","Retirar");
            foreach(string path in new[]{"Content/WithdrawAmount/Title","Content/WithdrawProgress/Title"})Need(root,path).GetComponent<TMP_Text>().alignment=TextAlignmentOptions.MidlineLeft;
            var title=Need(root,"ButtomGroup/Title").GetComponent<TMP_Text>();BindCopy(title,"Cash\nwithdrawal","Saque em\ndinheiro");title.enableWordWrapping=true;SizeText(title,55);
#endif
        }
        if(name=="daily-mission")
        {
            PaintReferenceDetail(Need(root,"Content/ApprovedGift").GetComponent<Image>(),"ClosedGift");
            var track=Need(root,"Content/ApprovedProgressTrack").GetComponent<Image>();Place(root,track.transform,102,1111,654,79);
            var fill=Need(root,"Content/ApprovedProgressTrack/Fill").GetComponent<Image>();RoundFill(fill,true);
            Copy(root,"Content/ApprovedProgressCaption","Today's progress","Progresso de hoje",101,1053,543,48,35).alignment=TextAlignmentOptions.MidlineLeft;
            Detail(root,"Content/ApprovedVideoIcon","detail:Video",104,1106,90,78);
            Detail(root,"Content/ApprovedGiftVideo","detail:Video",490,715,123,112);
            foreach(string path in new[]{"Content/GoBtn","Content/WithdrawBtn"})Detail(root,path+"/ApprovedFilm","detail:Video",207,1262,100,90);
            foreach(string path in new[]{"Content/GoBtn/Text (TMP)","Content/WithdrawBtn/Text (TMP)"}){Place(root,Need(root,path),322,1265,376,88);SizeText(Need(root,path).GetComponent<TMP_Text>(),65);}
        }
        if(name=="daily-tasks")
        {
            BindCopy(Need(root,"ApprovedSubtitle").GetComponent<TMP_Text>(),"Enjoy your time in the orchard.","Aproveite seu tempo no pomar.");
            foreach(var im in root.GetComponentsInChildren<Image>(true))if(im.sprite!=null&&im.sprite.name=="Clock")PaintReferenceDetail(im,"Alarm");
        }
        if(name=="service")
        {
            BindCopy(Need(root,"Title/Title").GetComponent<TMP_Text>(),"Support","Atendimento");
            BindCopy(Need(root,"Content/InputNode/SelectQuestionBtn /Text (TMP)").GetComponent<TMP_Text>(),"Quick questions","Perguntas rápidas");
            PaintReferenceDetail(Need(root,"Content/InputNode/SelectQuestionBtn ").GetComponent<Image>(),"SoftBlue");
        }
        if(name=="withdraw-milestones")
        {
            BindCopy(Need(root,"Title/Text").GetComponent<TMP_Text>(),"My milestones","Minhas conquistas");
            BindCopy(Need(root,"Content/Scroll View/Viewport/Content/WithdrawInfo/Title").GetComponent<TMP_Text>(),"Reward balance","Saldo de recompensas");
        }
        if(name=="withdraw-reminder")
        {
            BindCopy(Need(root,"Content (1)/ApprovedBalanceTitle").GetComponent<TMP_Text>(),"My balance","Meu saldo");
            BindCopy(Need(root,"Content (1)/WithdrawBtn/Text (TMP)").GetComponent<TMP_Text>(),"View withdrawal","Ver saque");
            BindCopy(Need(root,"Content (1)/ApprovedFooter").GetComponent<TMP_Text>(),"See the details on the next screen.","Consulte os detalhes na próxima tela.");
        }
        if(name=="history")PaintReferenceDetail(Need(root,"ApprovedSupport").GetComponent<Image>(),"SoftBlue");
        if(name=="lucky-spin")
        {
            var badge=root.transform.Find("Content/ApprovedFreeBadge");if(badge!=null)badge.gameObject.SetActive(false);
            Place(root,Need(root,"Content/ApprovedFreeCount"),280,990,50,57);Place(root,Need(root,"Content/ApprovedFreeCaption"),335,990,260,57);
            var line=Detail(root,"Content/ApprovedCheckLine","ButtonGreen",128,1360,590,22,true);
            line.transform.SetSiblingIndex(Need(root,"Content/ApprovedCheck0").GetSiblingIndex());
            for(int i=0;i<5;i++)Need(root,"Content/ApprovedCheck"+i).SetAsLastSibling();
        }
        FinalizeSecondReferencePass(root,name);
        NormalizeReferenceCaps(root);
        if(name=="get-booster")FinalizeBoosterReference(root);
        if(name=="cash-withdraw")FinalizeCashReference(root);
        if(name=="withdraw-account")FinalizeAccountReference(root);
        if(name=="withdraw-confirm")FinalizeConfirmReference(root);
        if(name=="history")FinalizeHistoryReference(root);
        if(name=="withdraw-reminder")FinalizeReminderReference(root);
        if(name=="rate-up")FinalizeRateReference(root);
        if(name=="service")FinalizeServiceReference(root);
        if(name=="daily-mission")FinalizeDailyReference(root);
        if(name=="lucky-spin")FinalizeSpinReference(root);
        if(name=="lucky-help")FinalizeLuckyHelpReference(root);
    }
    private static void ReferenceSettings(GameObject root)
    {
        var page=root.GetComponent<PausePanel>();
        var on=new[]{page.musicOnIm,page.soundOnIm,page.LibOnIm};var off=new[]{page.musicOffIm,page.soundOffIm,page.LibOffIm};
        var buttons=new[]{page.musicSwitchButton,page.soundSwitchButton,page.libSwitchButton};
        for(int i=0;i<3;i++)
        {
            PaintReferenceDetail(buttons[i].GetComponent<Image>(),"ToggleOff");
            PaintReferenceDetail(on[i],"ToggleOn");PaintReferenceDetail(off[i],"ToggleOff");
            foreach(var state in new[]{on[i],off[i]})
            {
                var knob=state.transform.Find("ApprovedKnob");if(knob!=null){PaintReferenceDetail(knob.GetComponent<Image>(),"Knob");LocalRect(knob,state==on[i]?143:12,10,76,78);}
                var label=state.transform.Find("ApprovedStateText");if(label!=null){LocalRect(label,state==on[i]?30:97,20,103,62);SizeText(label.GetComponent<TMP_Text>(),45);}
            }
            var text=Need(root,"BG (1)/"+new[]{"MusicGroup","SoundGroup","LibGroup"}[i]+"/ApprovedLabel").GetComponent<TMP_Text>();
            Place(root,text.transform,224,541+i*198,300,78);SizeText(text,58);text.alignment=TextAlignmentOptions.MidlineLeft;
        }
        var lang=Need(root,"BG (1)/ApprovedLanguageLabel").GetComponent<TMP_Text>();Place(root,lang.transform,224,1133,190,75);SizeText(lang,54);lang.alignment=TextAlignmentOptions.MidlineLeft;
        SizeText(page.languageDropdown.captionText,44);
        OrchardSettingsPresentation.RemoveLanguageRow(root);
    }
    private static void ReferenceWin(GameObject root)
    {
#if BIZZA_REAL_WITHDRAW
        var p=root.GetComponent<GetRewardPanel>();var w=root.GetComponentInChildren<WathAdProgress>(true);
        PaintReferenceDetail(Need(root,"BG/ApprovedChest").GetComponent<Image>(),"CelebrationChest");Place(root,Need(root,"BG/ApprovedChest"),102,440,655,424);
        var plaque=Need(root,"BG/ApprovedPlaque").GetComponent<Image>();PaintReferenceDetail(plaque,"WoodTitle");Place(root,plaque.transform,135,178,582,223);
        PaintReferenceDetail(p.levelTxt.transform.parent.GetComponent<Image>(),"WoodSubtitle");Place(root,p.levelTxt.transform.parent,229,348,396,79);
        SizeText(p.levelTxt,42);SetReferenceTitle(p.levelTxt,ApprovedInk);
        p.levelTxt.rectTransform.offsetMin=new Vector2(20,0);p.levelTxt.rectTransform.offsetMax=new Vector2(-20,0);SizeText(p.levelTxt,38);
        Place(root,p.itemATxt.transform,131,865,590,128);SizeText(p.itemATxt,146);p.itemATxt.alignment=TextAlignmentOptions.Center;
        LocalRect(Need(root,"BG/Content/WathAdProgress/progress"),24,72,529,73);
        PaintReferenceDetail(w.progressBar,"OrangeFill");w.progressBar.type=Image.Type.Sliced;w.progressBar.pixelsPerUnitMultiplier=2;
        SetReferenceTitle(w.progressText,new Color32(168,65,0,255));SizeText(w.progressText,44);
        var claimButton=(RectTransform)Need(root,"BG/Content/ButtonAnim/Button");
        var bonusRateRect=(RectTransform)p.bonusRate.transform;
        bonusRateRect.SetParent(claimButton,false);
        bonusRateRect.SetAsLastSibling();
        bonusRateRect.anchorMin=Vector2.one;bonusRateRect.anchorMax=Vector2.one;
        bonusRateRect.pivot=new Vector2(.5f,.5f);
        bonusRateRect.anchoredPosition=new Vector2(-16,-32);bonusRateRect.sizeDelta=new Vector2(100,100);
        PaintReferenceDetail(p.bonusRate.GetComponent<Image>(),"BonusLeaf");
        LocalRect(p.bonusRate.bonusRateTxt.transform,9,57,110,59);SetReferenceTitle(p.bonusRate.bonusRateTxt,ApprovedGreen);SizeText(p.bonusRate.bonusRateTxt,41);
        var ad=Need(root,"BG/Content/ButtonAnim/Button/Image (2)");PaintReferenceDetail(ad.GetComponent<Image>(),"Video");LocalRect(ad,180,28,100,94);
        Place(root,p.rewardText.transform,406,1274,350,99);SizeText(p.rewardText,73);p.rewardText.fontSizeMin=38; p.rewardText.alignment=TextAlignmentOptions.MidlineLeft;SetReferenceTitle(p.rewardText,ApprovedGreen);
        SizeText(p.noThanksText,60);SetReferenceTitle(p.noThanksText,new Color32(0,86,168,255));
#endif
    }
    private static void ReferenceAccount(GameObject root)
    {
#if BIZZA_REAL_WITHDRAW
        var p=root.GetComponent<UIWithdrawalPanel>();
        SizeText(Need(root,"Root/FillRoot/ApprovedAmount").GetComponent<TMP_Text>(),115);
        foreach(string path in new[]{"Root/FillRoot/ApprovedAmountTitle","Root/FillRoot/ApprovedDetailsTitle"})Need(root,path).GetComponent<TMP_Text>().alignment=TextAlignmentOptions.MidlineLeft;
        const string prefix="Root/FillRoot/pageContent/InfoContent/";
        BindCopy(Need(root,prefix+"Name/Text (TMP)").GetComponent<TMP_Text>(),"Full name","Nome completo");
        BindCopy(Need(root,prefix+"CPF/Text (TMP)").GetComponent<TMP_Text>(),"CPF / CNPJ","CPF / CNPJ");
        BindCopy(Need(root,prefix+"EmailInfo/Title").GetComponent<TMP_Text>(),"PagBank email","E-mail do PagBank");
        foreach(var input in new[]{p.accountNameInput,p.CPFNumberInput,p.paypalMailInput,p.accPhoneMailInput,p.accountIdentificationInput})
        {
            var placeholder=input.PlaceholderTextRenderer!=null?input.PlaceholderTextRenderer.GetComponent<TMP_Text>():null;
            if(placeholder!=null)
            {
                string en=input==p.accountNameInput?"Enter your name":input==p.CPFNumberInput?"Enter your document":"you@example.com";
                string pt=input==p.accountNameInput?"Digite seu nome":input==p.CPFNumberInput?"Digite seu documento":"voce@exemplo.com";
                BindCopy(placeholder,en,pt);placeholder.color=new Color32(145,143,137,255);SizeText(placeholder,39);
            }
        }
        var hint=Need(root,"Root/FillRoot/ApprovedHint").GetComponent<TMP_Text>();hint.color=new Color32(0,104,168,255);ReferenceInfoCard(root,hint,74,1297,699,85);
        Copy(root,"Root/FillRoot/ApprovedAccountFootnote","Use your PagBank account details.","Use os dados da sua conta PagBank.",112,1567,628,53,31);
#endif
    }
    private static void ReferenceInfoCard(GameObject root,TMP_Text label,float x,float y,float width,float height)
    {
        string prefix=AnimationUtility.CalculateTransformPath(label.transform.parent,root.transform);if(prefix.Length>0)prefix+="/";
        var old=root.transform.Find(prefix+"ApprovedHintPlate");if(old!=null)old.gameObject.SetActive(false);
        var card=Detail(root,prefix+"ReferenceInfoCard","detail:ChatBlue",x,y,width,height);
        card.transform.SetAsLastSibling();card.type=Image.Type.Sliced;card.pixelsPerUnitMultiplier=2;
        label.transform.SetAsLastSibling();Place(root,label.transform,x+101,y+9,width-121,height-18);
        Detail(root,prefix+"ReferenceInfoIcon","nav:Help",x+20,y+13,60,60);
    }

    private static void NormalizeReferenceCaps(GameObject root)
    {
        foreach(var im in root.GetComponentsInChildren<Image>(true))
        {
            if(im.sprite==null||im.type!=Image.Type.Sliced)continue;
            string n=im.sprite.name;
            if(n=="ButtonGreen"||n=="ButtonBlue"||n=="ButtonDisabled"||n=="ProgressTrack"||n=="ProgressFill"||n=="OrangeFill"||n=="ToggleOn"||n=="ToggleOff"||n=="SoftBlue"||n=="Badge")
                im.pixelsPerUnitMultiplier=im.sprite.rect.height/Mathf.Max(16,im.rectTransform.rect.height);
        }
    }
}
