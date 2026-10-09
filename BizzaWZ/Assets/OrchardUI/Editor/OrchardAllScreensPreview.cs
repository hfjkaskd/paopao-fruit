using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static partial class OrchardApprovedPass
{
    private static readonly string[] EnglishReferences={"level-complete","revive","get-booster","new-booster","rate-up","welcome-gift","daily-mission","lucky-spin","lucky-help","game-hud","tutorial"};
    private static void PreviewText(GameObject root,string path,string value)
    {var t=root.transform.Find(path);if(t!=null&&t.TryGetComponent<TMP_Text>(out var text)){text.text=value;text.gameObject.SetActive(true);text.enabled=true;}}
    private static void ApplyPreviewLanguage(GameObject root,bool english)
    {
        foreach(var label in root.GetComponentsInChildren<OrchardLocalizedLabel>(true))
        {var so=new SerializedObject(label);var target=so.FindProperty("target").objectReferenceValue as TMP_Text;if(target!=null)target.text=so.FindProperty(english?"english":"portuguese").stringValue;}
    }
    private static GameObject FixtureItem(string path,Transform parent)
    {
        var instance=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path),parent,false);instance.name="ReferenceFixture";
        foreach(var behaviour in instance.GetComponentsInChildren<MonoBehaviour>(true))
            if(!(behaviour is Graphic)&&!(behaviour is LayoutGroup)&&!(behaviour is ContentSizeFitter)&&!(behaviour is LayoutElement)&&!(behaviour is Selectable))behaviour.enabled=false;
        instance.SetActive(true);return instance;
    }
    private static void ConfigureDetailedReferencePreview(GameObject root,string name)
    {
        bool english=Array.IndexOf(EnglishReferences,name)>=0;ApplyPreviewLanguage(root,english);
        if(name=="loading"){foreach(var art in root.GetComponentsInChildren<OrchardLoadingArt>(true))art.SetProgress(.67f);}
        if(name=="settings")PreviewText(root,"BG (1)/VersionLog","1.0.0");
        if(name=="notification")
        {
            PreviewText(root,"BG (1)/Text (TMP)","Aviso");
            PreviewText(root,"Content/Des","<size=125%>Conexão pausada</size>\nVerifique sua conexão\ne tente novamente.");
            PreviewText(root,"Content/ConfirmBtn/Text (TMP)","Confirmar");
        }
        if(name=="new-booster")
        {
            Need(root,"MainContent/Body/Item").GetComponent<Image>().sprite=NamedSprite(HeroDetailAtlas,"MagicWand");
            PreviewText(root,"MainContent/Body/ItemName","Magic Wand");PreviewText(root,"MainContent/Body/Discription","Clear a matching set of fruit.");
        }
        if(name=="revive")
        {
            PreviewText(root,"bg/ApprovedLevelCaption","LEVEL 24");PreviewText(root,"bg/ReviveBtnGroup/ReviveBtn/Ad_BtnHint","Watch & Revive");
        }
#if BIZZA_REAL_WITHDRAW
        if(name=="rating")
        {
            var rating=root.GetComponent<StarRatingPopup>();
            for(int i=0;i<rating.starImages.Length;i++)rating.starImages[i].sprite=i<3?rating.starOn:rating.starOff;
        }
        if(name=="level-complete")
        {
            var p=root.GetComponent<GetRewardPanel>();p.itemATxt.text="+1,000";p.levelTxt.text="LEVEL 24 COMPLETE";p.rewardText.text="Watch & Claim";p.noThanksText.text="Continue";
            var w=root.GetComponentInChildren<WathAdProgress>(true);w.hintText.text="Bonus progress";w.progressText.text="6 / 10";w.progressBar.rectTransform.anchorMax=new Vector2(.6f,1);
        }
        if(name=="get-booster")
        {
            PreviewBoosterReference(root);
            var p=root.GetComponent<AddPropPanel>();p.propName.text="Undo";PreviewText(root,"Content/ApprovedToolName","Take back your last move.");p.limitTxt.text="Used this level: 0 / 3";
            foreach(var t in p.adBuyBtn.GetComponentsInChildren<TMP_Text>(true))t.text="Watch & Get 1";
        }
        if(name=="daily-mission")
        {
            root.GetComponent<DailyMissionPanel>().hintsTxt.text="Watch 30 videos to receive";
            PreviewText(root,"Content/ApprovedRewardValue","$0.20");PreviewText(root,"Title/refreshTimeTxt","Refreshes in 12:34:56");PreviewText(root,"Content/GoBtn/Text (TMP)","Watch Video");
            Need(root,"Content/ApprovedProgressTrack/Fill").GetComponent<Image>().fillAmount=8f/30;
            PreviewDailyReference(root);
        }
        if(name=="welcome-gift")PreviewText(root,"Panel/Os_UsdText","$5.00");
        if(name=="rate-up")
        {
            PreviewRateReference(root);
            PreviewText(root,"Content (1)/BeforeState/Title","Before");PreviewText(root,"Content (1)/NowState/Title","Now");
            PreviewText(root,"Content (1)/BeforeState/GameObject/beblance","10,000 coins");PreviewText(root,"Content (1)/NowState/GameObject/nowblance","10,000 coins");
            PreviewText(root,"Content (1)/BeforeState/GameObject/beclash","$2.50");PreviewText(root,"Content (1)/NowState/GameObject/nowclash","$5.00");
        }
        if(name=="withdraw-reminder")
        {
            PreviewReminderReference(root);
            PreviewText(root,"Content (1)/HintInfo/Hint","Valor para saque");PreviewText(root,"Content (1)/HintInfo/Num","≈ R$0,03");
            PreviewText(root,"Content (1)/CoinHint (1)/CoinNum/balanceTxt","0,95");
        }
        if(name=="withdraw-confirm")
        {
            var p=root.GetComponent<UIWithdrawalConfirmPanel>();p.NameText.text="Jogador";p.EmailText.text="jogador@example.com";
            PreviewConfirmReference(root);
        }
        if(name=="withdraw-account")
        {
            var p=root.GetComponent<UIWithdrawalPanel>();
            foreach(var input in new[]{p.accountNameInput,p.CPFNumberInput,p.paypalMailInput,p.accPhoneMailInput,p.accountIdentificationInput})
            {
                var so=new SerializedObject(input);so.FindProperty("text").stringValue="";so.ApplyModifiedPropertiesWithoutUndo();
                foreach(var tx in input.GetComponentsInChildren<TMP_Text>(true))if(tx.GetComponent<OrchardLocalizedLabel>()==null)tx.text="";
                if(input.PlaceholderTextRenderer!=null)input.PlaceholderTextRenderer.gameObject.SetActive(true);
            }
            PreviewAccountReference(root);
        }
        if(name=="cash-withdraw")
        {
            var p=root.GetComponent<FakeWithdrawPanel>();p.balanceTxt.text="R$6,40";p.progressImg.fillAmount=.8f;p.progressTxt.text="80%";p.hintTxt.text="Faltam <color=#005B1D>R$1,60</color> para esta meta.";p.fingerObj.SetActive(false);
            var items=root.GetComponentsInChildren<WithdrawAmountItem>(true);
            for(int i=0;i<items.Length;i++){items[i].gameObject.SetActive(i<5);if(i>=5)continue;items[i].amountTxt.text=new[]{"R$8","R$10","R$20","R$30","R$50"}[i];items[i].getObj.SetActive(false);items[i].getedObj.SetActive(false);items[i].selectObj.SetActive(i==0);}
            PreviewCashReference(root);
        }
        if(name=="daily-tasks")PreviewTaskRows(root);
        if(name=="history")PreviewHistoryRows(root);
        if(name=="withdraw-milestones")PreviewMilestoneRows(root);
        if(name=="faq")
        {
            string[] titles={"Como solicitar um saque?","Onde acompanho meu pedido?","Por que o valor muda?","O pedido não foi concluído."};
            string[] body={"Escolha o método e confira os dados da conta.","Consulte o status no Histórico.","O nível pode alterar o valor de troca.","Confira seus dados e consulte o atendimento."};
            var content=Need(root,"Content (1)/Scroll View/Viewport/Content");int i=0;
            foreach(var desc in content.GetComponentsInChildren<FAQDesc>(true))
            {var t=desc.GetComponent<TMP_Text>()??desc.GetComponentInChildren<TMP_Text>();if(t!=null&&i<4){t.text="<b>"+titles[i]+"</b>\n\n"+body[i];i++;}else desc.transform.parent.gameObject.SetActive(false);}
        }
        if(name=="service"){PreviewChatRows(root);PreviewServiceReference(root);}
        if(name=="lucky-help")PreviewLuckyHelpReference(root);
        if(name=="lucky-spin")
        {
            PreviewText(root,"Content/ApprovedFreeCount","1 FREE SPIN");
            PreviewText(root,"Top/IconInfo/IconTxt","10,000");PreviewText(root,"Top/DollarInfo/DollarTxt","$2.50");PreviewText(root,"Content/SlotMachineGroup/Btn/ClickObjs/CanClickObj","Spin");
            PreviewText(root,"Content/ApprovedSlotProgress","5 / 5 levels complete");root.GetComponent<SlotPanel>().slotHintTxt.text="Your next free spin is ready!";
        }
#endif
        foreach(var rounded in root.GetComponentsInChildren<OrchardRoundedFill>(true))rounded.Refresh();
    }
#if BIZZA_REAL_WITHDRAW
    private static void PreviewTaskRows(GameObject root)
    {
        var page=root.GetComponent<UIDailyTaskPage>();page.DailyRootObj.SetActive(false);
        var grids=root.GetComponentsInChildren<GridLayoutGroup>(true);Transform parent=null;
        foreach(var grid in grids)if(grid.transform.name=="Content"){parent=grid.transform;break;}
        if(parent==null&&grids.Length>0)parent=grids[0].transform;if(parent==null)return;
        for(var t=parent;t!=root.transform;t=t.parent)t.gameObject.SetActive(true);
        foreach(Transform child in parent)child.gameObject.SetActive(false);
        for(int i=0;i<5;i++)
        {
            var row=FixtureItem("Assets/BizzaWZ/Final/MenuSystem/Common/Task/UIDailyTaskElement.prefab",parent).GetComponent<UIDailyTaskElement>();
            row.descTxt.text=new[]{"10","20","40","60","90"}[i]+" min online";row.progressTxt.text=new[]{"10 / 10","20 / 20","25 / 40","25 / 60","25 / 90"}[i];
            row.progressBar.fillAmount=new[]{1,1,.625f,.4167f,.2778f}[i];
            row.objRewarded.SetActive(i==0);row.btnReward.gameObject.SetActive(i==1);row.btnAds.gameObject.SetActive(false);row.btnGoto.gameObject.SetActive(i>1);row.redPointUI.gameObject.SetActive(false);
            ApplyPreviewLanguage(row.gameObject,false);
        }
    }
    private static void PreviewHistoryRows(GameObject root)
    {
        var parent=Need(root,"Content/Scroll View/Viewport/Content");foreach(Transform child in parent)child.gameObject.SetActive(false);
        var payment=AssetDatabase.LoadAssetAtPath<PaymentConfig>("Assets/BizzaWZ/Final/Real/Config/PaymentConfig.asset");
        for(int i=0;i<3;i++)
        {
            var row=FixtureItem("Assets/BizzaWZ/Final/Real/UI/WithdrawHistory/WithdrawHistoryItem.prefab",parent).GetComponent<WithdrawHistoryItem>();
            if(i==2)((RectTransform)row.transform).sizeDelta=new Vector2(730,300);
            row.withdrawImg.sprite=payment.GetSpriteByType(i==1?E_PayeeAccountType.PIX:E_PayeeAccountType.Pagbank);
            row.amountTxt.text=i==0?"R$0,03":"R$0,02";row.timeTxt.text=new[]{"27 set 2026","25 set 2026","22 set 2026"}[i];
            row.nameTxt.gameObject.SetActive(false);row.cpfTxt.gameObject.SetActive(false);row.emailTxt.text=i==1?"***.***.***-**":"j***@example.com";
            row.dueText.text="Revise os dados da conta.";row.dueText.gameObject.SetActive(i==2);
            row.processingObj.SetActive(i==0);row.successObj.SetActive(i==1);row.failObj.SetActive(i==2);
            foreach(var t in row.processingObj.GetComponentsInChildren<TMP_Text>(true))t.text="Em análise";
            foreach(var t in row.successObj.GetComponentsInChildren<TMP_Text>(true))t.text="Concluído";
            foreach(var t in row.failObj.GetComponentsInChildren<TMP_Text>(true))t.text="Não concluído";
        }
        Need(root,"Content/Text (TMP)").gameObject.SetActive(false);
        PreviewHistoryReference(root);
    }
    private static void PreviewMilestoneRows(GameObject root)
    {
        var parent=Need(root,"Content/Scroll View/Viewport/Content/WithdrawDan/bg/WithdrawMode/Scroll View/Viewport/Content");foreach(Transform child in parent)child.gameObject.SetActive(false);
        for(int i=0;i<4;i++)
        {
            var row=FixtureItem("Assets/BizzaWZ/Final/Real/UI/WithdrawDanPanel/WithdrawDanItem.prefab",parent).GetComponent<WithdrawDanItem>();
            row.icon.sprite=root.GetComponent<WithdrawDanPanel>().danSprites[i];
            row.danText.text=new[]{"Bronze","Prata","Ouro","Platina"}[i];row.hintText.text="Nível "+new[]{5,20,50,100}[i];
            row.moneyText1.text=row.moneyText2.text=row.moneyText3.text="R$"+new[]{1,2,3,5}[i]+",00";
            row.progressText.text=i==2?"24 / 50":"24 / 100";row.progressImage.rectTransform.anchorMax=new Vector2(i==2?.48f:.24f,1);
            row.prepareStateObj.SetActive(i>1);row.claimStateObj.SetActive(i==1);row.claimedStateObj.SetActive(i==0);
            row.transform.Find("progress").gameObject.SetActive(i>1);row.progressText.gameObject.SetActive(i>1);ApplyPreviewLanguage(row.gameObject,false);
        }
        PreviewText(root,"Content/Scroll View/Viewport/Content/WithdrawInfo/CoinInfo/RealCurrent/Text (TMP)","R$3,00");
        PreviewText(root,"Content/Scroll View/Viewport/Content/WithdrawalInstruction/Progress/progressText","60%");
        PreviewText(root,"Content/Scroll View/Viewport/Content/WithdrawalInstruction/Hint","Continue jogando para avançar.");
        root.GetComponent<WithdrawDanPanel>().progressImg.fillAmount=.6f;
    }
    private static void PreviewChatRows(GameObject root)
    {
        var parent=Need(root,"Content/ChatContent/Viewport/Content");foreach(Transform child in parent)child.gameObject.SetActive(false);
        string[] copy={"Olá! Como podemos ajudar?","Quero acompanhar meu saque.","Abra o Histórico para consultar o status.","Se precisar, escolha uma pergunta abaixo."};
        for(int i=0;i<4;i++)
        {
            var row=FixtureItem("Assets/BizzaWZ/Final/Real/UI/ServicePanel/ChatElement.prefab",parent).GetComponent<ChatElement>();
            row.Init(new ChatInfo{chatcontent=copy[i],time="",spokesperson=i==1?Spokesperson.Player:Spokesperson.Issue});
            ApplyPreviewLanguage(row.gameObject,false);
        }
    }
#endif
}
