using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static partial class OrchardApprovedPass
{
    private const string BodyFontPath="Assets/OrchardUI/Fonts/Fidelity/Baloo2 SemiBold SDF.asset";
    private static void ImportReferenceBodyFont()
    {
        if(AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BodyFontPath)!=null)return;
        var font=TMP_FontAsset.CreateFontAsset(AssetDatabase.LoadAssetAtPath<Font>("Assets/OrchardUI/Fonts/Fidelity/Baloo2-SemiBold.ttf"),90,10,UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,1024,1024,AtlasPopulationMode.Dynamic,true);
        font.name="Orchard Baloo 2 SemiBold";AssetDatabase.CreateAsset(font,BodyFontPath);AssetDatabase.AddObjectToAsset(font.material,font);foreach(var texture in font.atlasTextures)AssetDatabase.AddObjectToAsset(texture,font);
        var metrics=font.faceInfo;metrics.ascentLine=73;metrics.descentLine=-18;metrics.lineHeight=96;font.faceInfo=metrics;
        font.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789ÀÁÂÃÇÉÊÍÓÔÕÚàáâãçéêíóôõú$≈%/.,:;!?() -+");EditorUtility.SetDirty(font);AssetDatabase.SaveAssets();
    }
    private static void ReferenceBody(TMP_Text text)
    {
        var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BodyFontPath);if(font==null)return;
        text.font=font;text.fontSharedMaterial=font.material;text.UpdateMeshPadding();
    }
    private static void FinalizeSecondReferencePass(GameObject root,string name)
    {
        if(name=="service")
        {
            Copy(root,"Content/ApprovedToday","Today","Hoje",368,245,116,58,36);
            Detail(root,"Content/ApprovedTodayPlate","Input",368,245,116,58).transform.SetSiblingIndex(Need(root,"Content/ApprovedToday").GetSiblingIndex());
            Need(root,"Content/ApprovedToday").SetAsLastSibling();
            Place(root,Need(root,"Content/ChatContent/Viewport"),65,326,722,699);
            Place(root,Need(root,"Content/InputNode/SelectQuestionBtn "),206,1063,441,94);
            Place(root,Need(root,"Content/InputNode/SelectQuestionBtn /Text (TMP)"),290,1079,332,63);
            var quick=Need(root,"Content/InputNode/SelectQuestionBtn /Text (TMP)").GetComponent<TMP_Text>();OrchardSkinAuthoring.SetBody(quick);quick.color=new Color32(0,109,177,255);SizeText(quick,42);
            Detail(root,"Content/InputNode/SelectQuestionBtn /ApprovedQuestion","nav:Help",240,1080,60,60);
            foreach(string button in new[]{"CanSendBtn","NotCanSendBtn"})
            {var b=Need(root,"Content/InputNode/"+button);PaintReferenceDetail(b.GetComponent<Image>(),"Send");var arrow=b.Find("ApprovedArrow");if(arrow!=null)arrow.gameObject.SetActive(false);}
            var input=Need(root,"Content/InputNode/InputField").GetComponent<AdvancedInputFieldPlugin.AdvancedInputField>();
            if(input!=null&&input.PlaceholderTextRenderer!=null){var placeholder=input.PlaceholderTextRenderer.GetComponent<TMP_Text>();BindCopy(placeholder,"Write your message...","Escreva sua mensagem...");SizeText(placeholder,42);ReferenceBody(placeholder);placeholder.color=new Color32(141,141,141,255);}
            SizeText(quick,35);Place(root,quick.transform,304,1079,319,63);
        }
        if(name=="history")
        {
            var button=Need(root,"ApprovedSupport");var label=button.GetComponentInChildren<TMP_Text>();
            if(label!=null){OrchardSkinAuthoring.SetBody(label);label.color=new Color32(0,104,163,255);SizeText(label,63);}
        }
        if(name=="withdraw-milestones")ReferenceMilestones(root);
        if(name=="faq")
        {
            foreach(var desc in root.GetComponentsInChildren<FAQDesc>(true)){var text=desc.GetComponent<TMP_Text>();if(text!=null){ReferenceBody(text);SizeText(text,35);}}
            var viewport=Need(root,"Content (1)/Scroll View/Viewport").GetComponent<RectTransform>();viewport.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,955);
        }
        if(name=="daily-tasks")
        {
            Place(root,Need(root,"ApprovedSubtitle"),120,467,612,66);
            foreach(var grid in root.GetComponentsInChildren<GridLayoutGroup>(true))
            {grid.spacing=new Vector2(0,15);var viewport=grid.transform.parent;Place(root,viewport,48,546,756,1015);}
            Place(root,Need(root,"CloseBtn"),725,219,95,98);
        }
        if(name=="revive")
        {
            foreach(var im in root.GetComponentsInChildren<Image>(true))if(im.sprite!=null&&(im.sprite.name=="FruitTray"||im.sprite.name=="Tray"||im.sprite.name=="FullTray"))PaintReferenceDetail(im,"FullTray");
        }
        if(name=="lucky-spin")
        {
            var cabinet=Need(root,"Content/SlotMachineGroup/ApprovedCabinet").GetComponent<Image>();Place(root,cabinet.transform,21,258,810,1050);cabinet.preserveAspect=false;
            Place(root,Need(root,"Content/SlotMachineGroup/ApprovedReelPaper"),133,565,586,408);
            const string maskPath="Content/SlotMachineGroup/Content/RewardGroup/SlotMask";
            Place(root,Need(root,maskPath),145,574,562,395);
            var idle=Ensure(root,maskPath+"/ApprovedIdleReelDecoration");StretchRect(idle);idle.SetAsFirstSibling();
            for(int i=0;i<6;i++)
            {
                string path=maskPath+"/ApprovedIdleReelDecoration/Symbol"+i;
                var symbol=Detail(root,path,"Input",184+(i%3)*176,i<3?563:891,133,133);
                symbol.sprite=i==4?NamedSprite("Assets/OrchardUI/Art/Navigation.png","LeavesLeft"):AssetDatabase.LoadAssetAtPath<Sprite>("Assets/FruitsHarvest/Resources/Original/res_server/server_images/fruit/Fruit_"+new[]{36,7,10,10,7,36}[i]+".png");
                symbol.type=Image.Type.Simple;symbol.preserveAspect=true;symbol.color=new Color(1,1,1,.6f);
            }
            for(int i=0;i<root.GetComponent<SlotPanel>().slotMachineManager.slotEntries.Length;i++)LocalRect(root.GetComponent<SlotPanel>().slotMachineManager.slotEntries[i].transform,44+i*176,153,122,122);
            var slotData=new SerializedObject(root.GetComponent<SlotPanel>());slotData.FindProperty("idleReelDecoration").objectReferenceValue=idle.gameObject;slotData.ApplyModifiedPropertiesWithoutUndo();
            var freeCount=Need(root,"Content/ApprovedFreeCount").GetComponent<TMP_Text>();var freeCaption=Need(root,"Content/ApprovedFreeCaption").GetComponent<TMP_Text>();SetReferenceTitle(freeCount,ApprovedInk);SetReferenceTitle(freeCaption,ApprovedInk);
            var coin=Need(root,"Top/IconInfo/Image").GetComponent<Image>();var iconAmend=coin.GetComponent<WzIconAmend>();if(iconAmend!=null)Object.DestroyImmediate(iconAmend);PaintReferenceDetail(coin,"CoinStack");
            Detail(root,"Content/ApprovedAdSpin/Film","detail:Video",228,1547,90,81);
            var label=Need(root,"Content/ApprovedAdSpin/Label").GetComponent<TMP_Text>();Place(root,label.transform,323,1538,315,65);SizeText(label,54);
            Copy(root,"Content/ApprovedAdSpin/Extra","Get another spin","Ganhe mais um giro",320,1598,309,36,28,true);
        }
        if(name=="lucky-help")
        {
            var list=Need(root,"Content/bg2 (1)/root");int i=0;
            foreach(Transform row in list)
            {
                foreach(Transform child in row)
                {
                    if(child.name.StartsWith("que")){var tx=child.GetComponent<TMP_Text>();tx.text="→";tx.color=new Color32(225,130,3,255);SizeText(tx,46);}
                    if(child.name=="Slot_1 (4)"||child.name=="Slot_1 (3)")
                    {var im=child.GetComponent<Image>();if(im!=null){im.sprite=NamedSprite(i<3?FidelityAtlas:ServiceDetailAtlas,i<3?"Cash":"SproutCoin");im.type=Image.Type.Simple;im.preserveAspect=true;}}
                }
                i++;
            }
        }
        if(name=="withdraw-reminder")
        {
            SetFlatMint(Need(root,"Content (1)/ApprovedMint").GetComponent<Image>());
            Need(root,"Content (1)/Image").gameObject.SetActive(false);
            var cash=Detail(root,"Content (1)/ApprovedCash","Card",313,477,220,125);cash.sprite=NamedSprite(FidelityAtlas,"PinkCash");cash.type=Image.Type.Simple;cash.preserveAspect=true;
            Place(root,Need(root,"Content (1)/ApprovedBalanceTitle"),150,607,552,57);
            Place(root,Need(root,"Content (1)/CoinHint (1)/CoinNum/balanceTxt"),154,662,544,92);SizeText(Need(root,"Content (1)/CoinHint (1)/CoinNum/balanceTxt").GetComponent<TMP_Text>(),102);
            Place(root,Need(root,"Content (1)/ApprovedMint"),67,758,718,238);
            Place(root,Need(root,"Content (1)/HintInfo/Hint"),90,786,671,69);
            Place(root,Need(root,"Content (1)/HintInfo/Num"),104,858,644,118);
            Need(root,"Content (1)/CoinHint (1)/CoinNum/=").gameObject.SetActive(false);
            Need(root,"Content (1)/CoinHint (1)/CoinNum/withdrawalTxt").gameObject.SetActive(false);
            Copy(root,"Content (1)/ApprovedBalanceNote","Check your withdrawal options.","Confira as opções de saque.",101,1015,650,54,37);
            var page=root.GetComponent<DailyWithdrawPanel>();var data=new SerializedObject(page);data.FindProperty("methodIcons").objectReferenceValue=AssetDatabase.LoadAssetAtPath<PaymentConfig>("Assets/BizzaWZ/Final/Real/Config/PaymentConfig.asset");var icons=data.FindProperty("availableMethodImages");icons.arraySize=2;
            for(int i=0;i<2;i++){string path="Content (1)/ApprovedMethod"+i;Detail(root,path,"Input",70+i*366,1075,350,163);var logo=Detail(root,path+"/Logo","Card",99+i*366,1104,291,105);logo.sprite=NamedSprite(FidelityPayments,i==0?"Pagbank":"PIX");logo.type=Image.Type.Simple;logo.preserveAspect=true;icons.GetArrayElementAtIndex(i).objectReferenceValue=logo;}data.ApplyModifiedPropertiesWithoutUndo();
            Place(root,Need(root,"Content (1)/WithdrawBtn"),103,1268,646,142);Place(root,Need(root,"Content (1)/WithdrawBtn/Text (TMP)"),140,1283,572,112);
            Place(root,Need(root,"Content (1)/ApprovedFooter"),87,1426,678,59);
        }
        if(name=="service-topics")ReferenceTopics(root);
        if(name=="rate-up")
        {
            foreach(var image in root.GetComponentsInChildren<Image>(true))
                if(image.GetComponent<WzIconAmend>()!=null){Object.DestroyImmediate(image.GetComponent<WzIconAmend>());PaintReferenceDetail(image,"CoinStack");}
        }
        if(name=="get-booster")foreach(var text in root.GetComponent<AddPropPanel>().adBuyBtn.GetComponentsInChildren<TMP_Text>(true))SizeText(text,64);
    }
    private static void ReferenceTopics(GameObject root)
    {
        var page=root.GetComponent<ServiceSelectPanel>();
        var duplicate=root.transform.Find("ApprovedEdit");if(duplicate!=null)duplicate.gameObject.SetActive(false);
        string[] en={"How do I withdraw?","How long does withdrawal take?","Why did my withdrawal fail?","How many times can I withdraw?","What is the daily limit?","Why is my account unavailable?","Can I use a VPN?"};
        string[] pt={"Como faço um saque?","Quanto tempo leva o saque?","Por que meu saque falhou?","Quantas vezes posso sacar?","Qual é o limite diário?","Por que minha conta está indisponível?","Posso usar VPN?"};
        for(int i=0;i<page.defaultButtons.Length;i++)
        {
            var button=page.defaultButtons[i];Place(root,button.transform,42,603+i*120,768,108);
            var label=page.defaultTexts[i];BindCopy(label,en[i],pt[i]);var legacy=label.GetComponent<UILanguageLabel>();if(legacy!=null)legacy.enabled=false;
            Place(root,label.transform,163,620+i*120,555,72);ReferenceBody(label);SizeText(label,41);
            string path=AnimationUtility.CalculateTransformPath(button.transform,root.transform);
            foreach(var text in button.GetComponentsInChildren<TMP_Text>(true))if(text!=label)text.gameObject.SetActive(false);
            foreach(var image in button.GetComponentsInChildren<Image>(true))if(image.transform!=button.transform)image.enabled=false;
            Detail(root,path+"/ApprovedNumberDisc","detail:BlueDisc",70,628+i*120,68,68);
            var number=Copy(root,path+"/ApprovedNumber",(i+1).ToString(),(i+1).ToString(),76,631+i*120,56,58,48,true);SetReferenceTitle(number,new Color32(0,85,160,255));
            var arrow=Copy(root,path+"/ApprovedNext","›","›",742,626+i*120,45,58,59);arrow.color=new Color32(0,119,214,255);
        }
        Place(root,page.customButton.transform,57,1484,738,132);
        OwnButton(page.customButton,"ButtonGreen");
        foreach(var image in page.customButton.GetComponentsInChildren<Image>(true))if(image.transform!=page.customButton.transform)image.enabled=false;
        string customPath=AnimationUtility.CalculateTransformPath(page.customButton.transform,root.transform);Detail(root,customPath+"/ApprovedPencil","detail:Pencil",109,1501,95,95);
        var customLabel=page.customButton.transform.Find("Text (TMP)").GetComponent<TMP_Text>();
        var customLanguage=customLabel.GetComponent<UILanguageLabel>();if(customLanguage!=null)customLanguage.enabled=false;
        foreach(var label in page.customButton.GetComponentsInChildren<TMP_Text>(true))label.gameObject.SetActive(label==customLabel);
        BindCopy(customLabel,"Write message","Escrever mensagem");Place(root,customLabel.transform,219,1501,525,99);SizeText(customLabel,60);SetReferenceTitle(customLabel,ApprovedGreen);
    }
    private static void ReferenceMilestones(GameObject root)
    {
#if BIZZA_REAL_WITHDRAW
        const string c="Content/Scroll View/Viewport/Content/";
        var outer=Detail(root,"Content/ApprovedOuterPanel","Panel",19,232,814,1249,true);
        var section=Need(root,c+"WithdrawDan");
        Copy(root,c+"WithdrawDan/ApprovedHeading","Level rewards","Recompensas por nível",75,682,683,72,55).alignment=TextAlignmentOptions.MidlineLeft;
        var page=root.GetComponent<WithdrawDanPanel>();
        for(int i=0;i<page.danSprites.Count;i++)page.danSprites[i]=NamedSprite(MedalAtlas,new[]{"Bronze","Silver","Gold","Platinum"}[Mathf.Min(i,3)]);
        PaintReferenceDetail(Need(root,c+"WithdrawInfo/CoinInfo/RealCurrent/Image").GetComponent<Image>(),"Gold");
        BindCopy(Need(root,c+"WithdrawalInstruction/Title/DailyText").GetComponent<TMP_Text>(),"Current goal","Meta atual");
        BindCopy(Need(root,c+"WithdrawInfo/WithdrawBtn/Btn/Text (TMP)").GetComponent<TMP_Text>(),"Withdraw","Retirar");
        var balanceTitle=Need(root,c+"WithdrawInfo/Title").GetComponent<TMP_Text>();Place(root,balanceTitle.transform,253,315,322,49);SizeText(balanceTitle,37);balanceTitle.enableWordWrapping=false;
        RoundFill(page.progressImg,true);
        Need(root,"ButtomGroup/HistoryBtn").gameObject.SetActive(false);
        foreach(var item in root.GetComponentsInChildren<WithdrawDanItem>(true))StyleMilestone(item.gameObject);
#endif
    }
}
