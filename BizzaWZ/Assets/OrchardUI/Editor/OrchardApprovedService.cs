using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static partial class OrchardApprovedPass
{
    private static void AuthorServiceComponents()
    {
        foreach (string path in new[] {
            "Assets/BizzaWZ/Final/Real/UI/FakeWithdrawPanel/WithdrawAmountItem.prefab",
            "Assets/BizzaWZ/Final/Real/UI/WithdrawHistory/WithdrawHistoryItem.prefab",
            "Assets/BizzaWZ/Final/Real/UI/ServicePanel/ChatElement.prefab",
            "Assets/BizzaWZ/Final/Real/UI/WithdrawDanPanel/WithdrawDanItem.prefab",
            "Assets/BizzaWZ/Final/MenuSystem/Common/Task/UIDailyTaskElement.prefab" })
        {
            if (!File.Exists(path)) throw new FileNotFoundException(path);
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (root.name == "WithdrawAmountItem") StyleCashCard(root);
                if (root.name == "WithdrawHistoryItem") StyleHistoryCard(root);
                if (root.name == "ChatElement") StyleChat(root);
                if (root.name == "WithdrawDanItem") StyleMilestone(root);
                if (root.name == "UIDailyTaskElement") StyleTask(root);
                NormalizeReferenceCaps(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
    private static void TopContent(RectTransform r)
    {
        r.anchorMin = new Vector2(0, 1); r.anchorMax = new Vector2(1, 1); r.pivot = new Vector2(.5f, 1);
        r.anchoredPosition = Vector2.zero; r.sizeDelta = new Vector2(0, r.sizeDelta.y);
        var fit = r.GetComponent<ContentSizeFitter>(); if (fit != null) { fit.enabled = true; fit.horizontalFit = ContentSizeFitter.FitMode.Unconstrained; fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize; }
    }
    private static void LocalText(Transform t, float x,float y,float w,float h,float size)
    { LocalRect(t,x,y,w,h); TextStyle(t.GetComponent<TMP_Text>(),size); }
    private static void OwnButton(Button button, string role)
    {
        var im = button.GetComponent<Image>() ?? button.gameObject.AddComponent<Image>(); Paint(im,role); im.raycastTarget = true; button.targetGraphic = im;
    }
    private static void StyleCashCard(GameObject root)
    {
        ((RectTransform)root.transform).sizeDelta = new Vector2(350,144);
        Paint(Need(root,"bg").GetComponent<Image>(),"Input"); StretchRect(Need(root,"bg"));
        root.GetComponent<Button>().targetGraphic = Need(root,"bg").GetComponent<Image>();
        StretchRect(Need(root,"Select")); Paint(Need(root,"Select").GetComponent<Image>(),"SelectionRing");
        LocalText(Need(root,"Amount"),30,43,290,67,55); Need(root,"Amount").GetComponent<TMP_Text>().alignment=TextAlignmentOptions.Center;
        foreach(string n in new[]{"GetTag","GetedTag"})
        { LocalRect(Need(root,n),25,8,300,35); Paint(Need(root,n).GetComponent<Image>(),"Badge"); StretchRect(Need(root,n+"/text")); TextStyle(Need(root,n+"/text").GetComponent<TMP_Text>(),24); }
        StyleCashReferenceCard(root);
    }
    private static void StyleHistoryCard(GameObject root)
    {
#if BIZZA_REAL_WITHDRAW
        var p = root.GetComponent<WithdrawHistoryItem>(); ((RectTransform)root.transform).sizeDelta=new Vector2(730,280);
        Paint(Need(root,"bg").GetComponent<Image>(),"Input"); StretchRect(Need(root,"bg"));
        foreach(var l in root.GetComponentsInChildren<LayoutGroup>(true)) l.enabled=false;
        foreach(Transform t in root.transform) if(t.name=="Infos") StretchRect(t);
        LocalRect(p.withdrawImg.transform,26,20,296,105); p.withdrawImg.preserveAspect=true;
        LocalText(p.amountTxt.transform,388,23,310,96,80); p.amountTxt.color=ApprovedGreen;
        LocalText(p.timeTxt.transform,30,144,355,44,34);
        LocalText(p.emailTxt.transform,30,191,355,44,31);
        LocalText(p.nameTxt.transform,30,234,355,31,25);
        LocalText(p.cpfTxt.transform,30,268,355,31,25);
        LocalText(p.dueText.transform,403,232,294,63,25);
        var data=new SerializedObject(p);data.FindProperty("normalRowHeight").floatValue=310;data.FindProperty("failedRowHeight").floatValue=330;data.ApplyModifiedPropertiesWithoutUndo();
        foreach(var go in new[]{p.successObj,p.processingObj,p.failObj})
        {
            LocalRect(go.transform,401,149,295,77);
            foreach(var tx in go.GetComponentsInChildren<TMP_Text>(true)) { StretchRect(tx.transform); TextStyle(tx,36); tx.alignment=TextAlignmentOptions.Center; }
            foreach(var im in go.GetComponentsInChildren<Image>(true)) {StretchRect(im.transform); Paint(im,go==p.successObj?"Inset":"Badge");if(go==p.failObj)im.color=new Color32(255,179,151,255);}
        }
        StyleHistoryReferenceCard(root);
#endif
    }
    private static void StyleChat(GameObject root)
    {
#if BIZZA_REAL_WITHDRAW
        var p=root.GetComponent<ChatElement>();
        PaintReferenceDetail(Need(root,"ChatInfo/Issue_bg").GetComponent<Image>(),"ChatBlue"); PaintReferenceDetail(Need(root,"ChatInfo/Player_bg").GetComponent<Image>(),"ChatMint");
        Need(root,"ChatInfo/Issue_bg").GetComponent<Image>().pixelsPerUnitMultiplier=2;Need(root,"ChatInfo/Player_bg").GetComponent<Image>().pixelsPerUnitMultiplier=2;
        TextStyle(p.chatTxt,37);ReferenceBody(p.chatTxt); p.chatTxt.enableAutoSizing=false; TextStyle(p.timeTxt,23);
        var so=new SerializedObject(p);
        so.FindProperty("maxBubbleWidth").floatValue=550;so.FindProperty("outerHorizontalPadding").floatValue=14;
        so.FindProperty("issueAvatarSpace").floatValue=102;so.FindProperty("issueHeaderHeight").floatValue=45;
        so.FindProperty("bubbleHorizontalPadding").floatValue=45;so.FindProperty("bubbleVerticalPadding").floatValue=24;
        foreach(string name in new[]{"issueTextColor","playerTextColor","timeTextColor"}) so.FindProperty(name).colorValue=ApprovedInk;
        so.ApplyModifiedPropertiesWithoutUndo();
        var avatar=Ensure(root,"ChatInfo/Issue_bg/ApprovedAvatar");var image=avatar.GetComponent<Image>()??avatar.gameObject.AddComponent<Image>();PaintReferenceDetail(image,"ChatAvatar");image.raycastTarget=false;
        LocalRect(avatar,-104,5,92,92);
        var speaker=Copy(root,"ChatInfo/Issue_bg/ApprovedSpeaker","Support","Suporte",0,0,100,50,33);LocalRect(speaker.transform,45,17,250,45);speaker.alignment=TextAlignmentOptions.MidlineLeft;speaker.color=new Color32(0,111,177,255);
#endif
    }
    private static void StyleMilestone(GameObject root)
    {
#if BIZZA_REAL_WITHDRAW
        var p=root.GetComponent<WithdrawDanItem>();((RectTransform)root.transform).sizeDelta=new Vector2(724,160);
        Paint(Need(root,"bg").GetComponent<Image>(),"Input");StretchRect(Need(root,"bg"));
        LocalRect(p.icon.transform,16,8,141,145);p.icon.preserveAspect=true;
        p.danText.transform.SetParent(root.transform,false);
        var oldBadge=p.icon.transform.Find("Image (2)");if(oldBadge!=null)oldBadge.gameObject.SetActive(false);
        LocalText(p.danText.transform,168,38,164,47,43);
        LocalText(p.hintText.transform,168,89,164,45,34); LocalText(p.progressText.transform,332,124,209,34,29);
        LocalRect(Need(root,"progress"),332,98,209,24);StretchRect(Need(root,"progress/bg"));Paint(Need(root,"progress/bg").GetComponent<Image>(),"ProgressTrack");
        StretchRect(Need(root,"progress/FillArea"));StretchRect(p.progressImage.transform);PaintReferenceDetail(p.progressImage,"OrangeFill");
        var buttons=new[]{p.prepareStateBtn,p.claimStateBtn,p.claimedStateBtn};var money=new[]{p.moneyText1,p.moneyText2,p.moneyText3};
        for(int i=0;i<buttons.Length;i++)
        {
            var btn=buttons[i];LocalRect(btn.transform,543,44,165,77);OwnButton(btn,i==1?"ButtonGreen":"ButtonDisabled");
            var old=btn.transform.Find("Image");if(old!=null)old.GetComponent<Image>().enabled=false;
            LocalText(money[i].transform,-212,i==0?-17:0,203,76,53);money[i].color=ApprovedGreen;money[i].raycastTarget=false;
            string path=AnimationUtility.CalculateTransformPath(btn.transform,root.transform)+"/ApprovedStateCaption";
            var label=Copy(root,path,i==0?"Locked":i==1?"Claim":"Claimed",i==0?"Bloqueado":i==1?"Receber":"Recebido",0,0,165,77,34,i==1);StretchRect(label.transform);
        }
        foreach(var text in p.maxlevelTexts)text.gameObject.SetActive(false);
        PaintReferenceDetail(p.prepareStateBtn.GetComponent<Image>(),"LockedDisc");LocalRect(p.prepareStateBtn.transform,589,37,104,104);
        LocalText(p.moneyText1.transform,-257,-7,207,65,53);p.moneyText1.color=ApprovedGreen;
        p.prepareStateBtn.transform.Find("ApprovedStateCaption").gameObject.SetActive(false);
        var data=new SerializedObject(p);data.FindProperty("progressRoot").objectReferenceValue=Need(root,"progress").gameObject;data.FindProperty("compactLevelCaption").boolValue=true;data.ApplyModifiedPropertiesWithoutUndo();
#endif
    }
    private static void StyleTask(GameObject root)
    {
#if BIZZA_REAL_WITHDRAW
        var p=root.GetComponent<UIDailyTaskElement>();((RectTransform)root.transform).sizeDelta=new Vector2(756,190);Paint(root.GetComponent<Image>(),"Input");
        LocalText(p.descTxt.transform,144,22,255,59,41);p.descTxt.enableWordWrapping=false;p.descTxt.enableAutoSizing=true;p.descTxt.fontSizeMin=22;LocalRect(Need(root,"State"),23,51,98,101);
        LocalRect(Need(root,"BarBG"),144,105,320,27);Paint(Need(root,"BarBG").GetComponent<Image>(),"ProgressTrack");
        StretchRect(p.progressBar.transform);Paint(p.progressBar,"ProgressFill");RoundFill(p.progressBar);LocalText(p.progressTxt.transform,4,35,312,37,32);p.progressTxt.alignment=TextAlignmentOptions.Center;
        foreach(var btn in new[]{p.btnReward,p.btnAds,p.btnGoto})
        {LocalRect(btn.transform,528,47,207,88);OwnButton(btn,btn==p.btnGoto?"ButtonBlue":"ButtonGreen");
         foreach(var tx in btn.GetComponentsInChildren<TMP_Text>(true)){tx.gameObject.SetActive(true);StretchRect(tx.transform);TextStyle(tx,42,true);tx.alignment=TextAlignmentOptions.Center;BindCopy(tx,btn==p.btnGoto?"Play":btn==p.btnAds?"Watch":"Claim",btn==p.btnGoto?"Jogar":btn==p.btnAds?"Assistir":"Receber");SetReferenceTitle(tx,btn==p.btnGoto?new Color32(0,85,160,255):ApprovedGreen);}}
        LocalRect(p.objRewarded.transform,528,47,207,88);Paint(p.objRewarded.GetComponent<Image>(),"ButtonDisabled");StretchRect(p.objRewardedMask.transform);
        var claimed=Ensure(root,"rewarded/ApprovedCaption");var claimedText=claimed.GetComponent<TMP_Text>()??claimed.gameObject.AddComponent<TextMeshProUGUI>();StretchRect(claimed);TextStyle(claimedText,37,true);claimedText.alignment=TextAlignmentOptions.Center;BindCopy(claimedText,"Claimed","Recebido");
        foreach(var graphic in Need(root,"State").GetComponentsInChildren<Graphic>(true))graphic.enabled=false;
        foreach(var particle in Need(root,"State").GetComponentsInChildren<Coffee.UIExtensions.UIParticle>(true))particle.enabled=false;
        var clock=Ensure(root,"ApprovedTaskClock");var clockImage=clock.GetComponent<Image>()??clock.gameObject.AddComponent<Image>();PaintReferenceDetail(clockImage,"Alarm");clockImage.raycastTarget=false;LocalRect(clock,15,34,120,125);
        var coin=Ensure(root,"ApprovedRewardCoin");var coinImage=coin.GetComponent<Image>()??coin.gameObject.AddComponent<Image>();PaintReferenceDetail(coinImage,"SproutCoin");coinImage.raycastTarget=false;LocalRect(coin,401,12,70,78);
        LocalRect(p.redPointUI.transform,696,22,36,36);LocalRect(p.redPointUI.red.transform,0,0,36,36);
        var mask=p.objRewardedMask.GetComponent<Image>();if(mask!=null)mask.color=new Color(1,1,1,.08f);
#endif
    }
    private static void SetupApprovedService(GameObject root,string name)
    {
        if(name=="service")
        {
            var r=Need(root,"Content/ChatContent/Viewport/Content").GetComponent<RectTransform>();TopContent(r);
            var layout=r.GetComponent<VerticalLayoutGroup>();layout.spacing=34;layout.padding=new RectOffset(0,0,10,20);
            foreach(var tx in Need(root,"Content/InputNode/InputField").GetComponentsInChildren<TMP_Text>(true))TextStyle(tx,32);
        }
        if(name=="cash-withdraw")
        {
            var grid=Need(root,"Content/WithdrawAmount/Scroll View/Viewport/Content").GetComponent<GridLayoutGroup>();TopContent(grid.GetComponent<RectTransform>());
            grid.cellSize=new Vector2(350,144);grid.spacing=new Vector2(16,16);grid.constraint=GridLayoutGroup.Constraint.FixedColumnCount;grid.constraintCount=2;
#if BIZZA_REAL_WITHDRAW
            foreach(var item in root.GetComponentsInChildren<WithdrawAmountItem>(true))StyleCashCard(item.gameObject);
#endif
        }
        if(name=="history")
        {
            var r=Need(root,"Content/Scroll View/Viewport/Content").GetComponent<RectTransform>();TopContent(r);
            var l=r.GetComponent<VerticalLayoutGroup>();if(l!=null){l.spacing=24;l.childControlHeight=false;l.childControlWidth=true;l.childForceExpandWidth=true;l.childForceExpandHeight=false;}
        }
        if(name=="faq")
        {
            var content=Need(root,"Content (1)/Scroll View/Viewport/Content");TopContent((RectTransform)content);
            var l=content.GetComponent<VerticalLayoutGroup>();l.spacing=22;l.padding=new RectOffset();l.childControlHeight=true;l.childControlWidth=true;l.childForceExpandHeight=false;
            var qs=content.GetComponentsInChildren<FAQDesc>(true);
            for(int i=0;i<qs.Length;i++)
            {
                var q=qs[i].transform;Transform row=q.parent;
                if(row==content){row=Ensure(root,AnimationUtility.CalculateTransformPath(content,root.transform)+"/ApprovedQuestion"+i);q.SetParent(row,false);}
                var image=row.GetComponent<Image>()??row.gameObject.AddComponent<Image>();Paint(image,"Input");image.raycastTarget=false;
                var group=row.GetComponent<VerticalLayoutGroup>()??row.gameObject.AddComponent<VerticalLayoutGroup>();group.padding=new RectOffset(118,24,28,28);group.childControlHeight=true;group.childControlWidth=true;group.childForceExpandHeight=false;
                var element=row.GetComponent<LayoutElement>()??row.gameObject.AddComponent<LayoutElement>();element.minHeight=210;
                var fitter=q.GetComponent<ContentSizeFitter>();if(fitter!=null)fitter.enabled=false;
                var path=AnimationUtility.CalculateTransformPath(row,root.transform)+"/ApprovedQuestionIcon";var icon=Ensure(root,path);var im=icon.GetComponent<Image>()??icon.gameObject.AddComponent<Image>();Paint(im,"nav:Help");im.raycastTarget=false;
                var ignore=icon.GetComponent<LayoutElement>()??icon.gameObject.AddComponent<LayoutElement>();ignore.ignoreLayout=true;LocalRect(icon,24,32,70,74);
            }
        }
        if(name=="daily-tasks")
        {
            foreach(var grid in root.GetComponentsInChildren<GridLayoutGroup>(true)){grid.cellSize=new Vector2(756,190);grid.spacing=new Vector2(0,19);grid.constraint=GridLayoutGroup.Constraint.FixedColumnCount;grid.constraintCount=1;TopContent(grid.GetComponent<RectTransform>());}
#if BIZZA_REAL_WITHDRAW
            var page=root.GetComponent<UIDailyTaskPage>();page.DailyRootObj=Need(root,"Content/DailyTask").gameObject;
            var so=new SerializedObject(page);so.FindProperty("tabRoot").objectReferenceValue=Need(root,"TabGroup").gameObject;so.FindProperty("initialTab").intValue=2;so.ApplyModifiedPropertiesWithoutUndo();
            Need(root,"TabGroup").gameObject.SetActive(page.enableTab);
            Need(root,"Content").SetAsLastSibling();Need(root,"CloseBtn").SetAsLastSibling();
            foreach(var item in root.GetComponentsInChildren<UIDailyTaskElement>(true))StyleTask(item.gameObject);
#endif
        }
        if(name=="withdraw-milestones")
        {
            var outer=Need(root,"Content/Scroll View/Viewport/Content").GetComponent<RectTransform>();TopContent(outer);var fit=outer.GetComponent<ContentSizeFitter>();if(fit!=null)fit.enabled=false;outer.sizeDelta=new Vector2(0,1216);
            var r=Need(root,"Content/Scroll View/Viewport/Content/WithdrawDan/bg/WithdrawMode/Scroll View/Viewport/Content").GetComponent<RectTransform>();TopContent(r);
            var l=r.GetComponent<VerticalLayoutGroup>();l.spacing=17;l.padding=new RectOffset();l.childControlHeight=false;l.childControlWidth=true;l.childForceExpandWidth=true;l.childForceExpandHeight=false;
            var coin=Need(root,"Content/Scroll View/Viewport/Content/WithdrawInfo/CoinInfo/RealCurrent/Image").GetComponent<Image>();coin.sprite=NamedSprite("Assets/OrchardUI/Art/OrchardReelSymbols.png","Coin");coin.type=Image.Type.Simple;coin.preserveAspect=true;
        }
    }
}
