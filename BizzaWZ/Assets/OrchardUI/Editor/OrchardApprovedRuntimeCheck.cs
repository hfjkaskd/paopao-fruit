#if BIZZA_REAL_WITHDRAW
using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Explicit read-only navigation verification against the normal initialized game.</summary>
[InitializeOnLoad]
public static partial class OrchardApprovedRuntimeCheck
{
    private const string CommandFolder="Design/OrchardImplementation-20260928/Runtime/";
    private static string Folder=CommandFolder;
    private static readonly string[] Pages={"RealWithdrawPanel","FakeWithdrawPanel","PausePanel","FAQPanel","WithdrawHistory","ServicePanel","WithdrawDanPanel","SlotPanel","SlotFAQPanel","DailyMissionPanel","UI_DailyTaskPage","UIWithdrawalPanel","StarRatingPopup"};
    private static readonly List<AccountModule.OceanShineWithdrawalPageResponse.WithdrawalPlatform> paymentPlatforms=new List<AccountModule.OceanShineWithdrawalPageResponse.WithdrawalPlatform>();
    [Serializable] private sealed class Run { public string startedUtc,status,error; public int index,phase; public List<PageResult> pages=new List<PageResult>();public List<string> errors=new List<string>(); }
    [Serializable] private sealed class PageResult {public string page,openMethod,closeMethod,screenshot;public bool opened,closed;public List<Hit> buttons=new List<Hit>();public List<string> checks=new List<string>();}
    [Serializable] private sealed class Hit {public string name,topHit;public bool interactable,centerHitsButton;public Vector2 center;}
    private static Run run;private static double next,deadline;private static bool opening;
    private static WithdrawLevelItem originalTier;
    private static string originalBalance;
    private static bool withdrawalVerified;
    private static bool supportVerified;
    private static bool topicsVerified,ratingVerified;
    private static bool reelVerified,reelCompleted;
    private static int originalSpinProgress;
    private static UnityEngine.Random.State originalRandom;
    private static string hudCaptureFolder;
    private static double hudReadyAt;
    static OrchardApprovedRuntimeCheck(){EditorApplication.update+=Tick;Application.logMessageReceived+=Log;}
    private static void Log(string message,string stack,LogType type)
    {
        if(type!=LogType.Error&&type!=LogType.Exception)return;
        if(run!=null&&run.status=="running"){run.errors.Add(message);Save();}
        if(boosterRun!=null&&boosterRun.status=="running"){boosterRun.errors.Add(message);SaveBooster();}
        if(cashRun!=null&&cashRun.status=="running"){cashRun.errors.Add(message);SaveCash();}
        if(accountRun!=null&&accountRun.status=="running"){accountRun.errors.Add(message);SaveAccount();}
        if(confirmRun!=null&&confirmRun.status=="running"){confirmRun.errors.Add(message);SaveConfirm();}
        if(historyRun!=null&&historyRun.status=="running"){historyRun.errors.Add(message);SaveHistory();}
        if(reminderRun!=null&&reminderRun.status=="running"){reminderRun.errors.Add(message);SaveReminder();}
        if(rateRun!=null&&rateRun.status=="running"){rateRun.errors.Add(message);SaveRate();}
        if(serviceRun!=null&&serviceRun.status=="running"){serviceRun.errors.Add(message);SaveService();}
        if(dailyRun!=null&&dailyRun.status=="running"){dailyRun.errors.Add(message);SaveDaily();}
        if(spinRun!=null&&spinRun.status=="running"){spinRun.errors.Add(message);SaveSpin();}
        if(luckyHelpRun!=null&&luckyHelpRun.status=="running"){luckyHelpRun.errors.Add(message);SaveLuckyHelp();}
    }
    private static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        string command=CommandFolder+"command.txt";
        if(File.Exists(command))
        {
            string value;try{value=File.ReadAllText(command).Trim();File.Delete(command);}catch(IOException){return;}
            if(value=="start"||value=="start-short")
            {
                Folder=value=="start-short"?"Design/OrchardImplementation-20260928/RuntimeShort/":CommandFolder;
                withdrawalVerified=false;supportVerified=false;topicsVerified=false;ratingVerified=false;reelVerified=false;reelCompleted=false;originalTier=null;
                run=new Run{startedUtc=DateTime.UtcNow.ToString("O"),status="running"};deadline=EditorApplication.timeSinceStartup+180;Save();
            }
            if(value=="hud"||value=="hud-short"){hudCaptureFolder=value=="hud-short"?"Design/OrchardImplementation-20260928/RuntimeShort/":CommandFolder;hudReadyAt=0;}
            if(value=="booster"||value=="booster-short")StartBoosterCheck(value=="booster-short");
            if(value=="cash"||value=="cash-short")StartCashCheck(value=="cash-short");
            if(value=="account"||value=="account-short")StartAccountCheck(value=="account-short");
            if(value=="confirm"||value=="confirm-short")StartConfirmCheck(value=="confirm-short");
            if(value=="history"||value=="history-short")StartHistoryCheck(value=="history-short");
            if(value=="reminder"||value=="reminder-short")StartReminderCheck(value=="reminder-short");
            if(value=="rate"||value=="rate-short")StartRateCheck(value=="rate-short");
            if(value=="service"||value=="service-short")StartServiceCheck(value=="service-short");
            if(value=="daily"||value=="daily-short")StartDailyCheck(value=="daily-short");
            if(value=="spin"||value=="spin-short")StartSpinCheck(value=="spin-short");
            if(value=="lucky-help"||value=="lucky-help-short")StartLuckyHelpCheck(value=="lucky-help-short");
        }
        TickBoosterCheck();
        TickCashCheck();
        TickAccountCheck();FinishAccountCheck();
        TickConfirmCheck();
        TickHistoryCheck();
        TickReminderCheck();
        TickRateCheck();
        TickServiceCheck();
        TickDailyCheck();
        TickSpinCheck();
        TickLuckyHelpCheck();
        if(hudCaptureFolder!=null&&EditorApplication.isPlaying&&HarvestBridge.Ready&&UIModule.Instance!=null&&!UIModule.Instance.HasPopup&&!TransparentBlock.IsBlock)
        {if(hudReadyAt==0)hudReadyAt=EditorApplication.timeSinceStartup+3;else if(EditorApplication.timeSinceStartup>=hudReadyAt)VerifyHud();}
        if(run==null||run.status!="running"||EditorApplication.timeSinceStartup<next)return;
        next=EditorApplication.timeSinceStartup+.25;
        try
        {
            if(EditorApplication.timeSinceStartup>deadline)throw new TimeoutException("Navigation wait: "+Pages[run.index]+" phase "+run.phase);
            if(!EditorApplication.isPlaying||!HarvestBridge.Ready||UIModule.Instance==null)return;
            EditorApplication.QueuePlayerLoopUpdate();
            if(opening||UIModule.Instance.Opening)return;
            string id=Pages[run.index];var page=UIModule.Instance.GetPage(new PageId(id));
            if(run.phase==0)
            {
                if(UIModule.Instance.HasPopup||TransparentBlock.IsBlock)return;
                var result=new PageResult{page=id};run.pages.Add(result);run.phase=1;deadline=EditorApplication.timeSinceStartup+35;
                var bar=UnityEngine.Object.FindObjectOfType<CurrencyBar>();
                if(run.index<3&&bar!=null)
                {var button=run.index==0?bar.coinBtn:run.index==1?bar.dollarBtn:bar.settingBtn;Click(button);result.openMethod="EventSystem pointer click on HUD Button";}
                else {result.openMethod="Production UIModule.OpenPage";Open(id).Forget();}
                next=EditorApplication.timeSinceStartup+2;Save();return;
            }
            var current=run.pages[run.pages.Count-1];
            if(run.phase==1)
            {
                if(page==null||!page.gameObject.activeInHierarchy)return;
                current.opened=true;
                if(page is RealWithdrawPanel withdrawal)
                {paymentPlatforms.Clear();foreach(var way in withdrawal.withdrawWayRoot.GetComponentsInChildren<WithdrawWay>())if(way.data!=null)paymentPlatforms.Add(way.data);}
                foreach(var button in page.GetComponentsInChildren<Button>())current.buttons.Add(Inspect(button));
                current.screenshot=Folder+run.index.ToString("00")+"-"+id+".png";
                OrchardSkinValidation.CaptureRuntimeSnapshot(Path.ChangeExtension(current.screenshot,".json"),current.screenshot);ScreenCapture.CaptureScreenshot(current.screenshot);
                run.phase=2;next=EditorApplication.timeSinceStartup+1;Save();return;
            }
            if(run.phase==2)
            {
                if(page is ServicePanel service && !topicsVerified)
                {
                    Click(service.selectQuestionButton);run.phase=40;next=EditorApplication.timeSinceStartup+1;Save();return;
                }
                if(page is StarRatingPopup rating && !ratingVerified)
                {
                    Click(rating.starButtons[2]);run.phase=45;next=EditorApplication.timeSinceStartup+1;Save();return;
                }
                if(page is SlotPanel slots && !reelVerified)
                {
                    // Exercise the existing presentation callback without consuming a spin,
                    // requesting an ad or wiring the account reward callback.
                    originalSpinProgress=SlotProgressUtil.Current;originalRandom=UnityEngine.Random.state;
                    slots.slotMachineManager.PlayAnim(false,result=>reelCompleted=!string.IsNullOrEmpty(result));
                    run.phase=50;next=EditorApplication.timeSinceStartup+1;Save();return;
                }
                if(page is FAQPanel && !supportVerified)
                {
                    Click(page.transform.Find("ApprovedSupport").GetComponent<Button>());
                    run.phase=30;next=EditorApplication.timeSinceStartup+1;Save();return;
                }
                if(page is RealWithdrawPanel withdrawal && !withdrawalVerified)
                {
                    originalTier=withdrawal.CurrentWithdrawLevelItem;originalBalance=withdrawal.balanceTxt.text;
                    var ways=withdrawal.withdrawWayRoot.GetComponentsInChildren<WithdrawWay>();
                    WithdrawWay originalWay=null;foreach(var way in ways)if(way.selectedObj.activeSelf)originalWay=way;
                    foreach(var way in ways)
                    {
                        Click(way.GetComponent<Button>());
                        if(!way.selectedObj.activeSelf)throw new InvalidOperationException("Payment selection failed.");
                        foreach(var other in ways)if(other!=way&&other.selectedObj.activeSelf)throw new InvalidOperationException("Multiple payment selections remain active.");
                    }
                    if(originalWay!=null)Click(originalWay.GetComponent<Button>());
                    current.checks.Add("Payment Buttons switched and restored the selected platform");
                    var scroll=withdrawal.GetComponentInChildren<ScrollRect>();
                    scroll.StopMovement();scroll.verticalNormalizedPosition=0;
                    Canvas.ForceUpdateCanvases();
                    run.phase=20;next=EditorApplication.timeSinceStartup+.75;Save();return;
                }
                if(page is PausePanel settings)
                {
                    VerifyToggle(settings.musicSwitchButton,()=>SaveDataUtils.SettingData.enableMusic);current.checks.Add("Music: toggled through pointer click and restored");
                    VerifyToggle(settings.soundSwitchButton,()=>SaveDataUtils.SettingData.enableSound);current.checks.Add("Sound: toggled through pointer click and restored");
                    VerifyToggle(settings.libSwitchButton,()=>SaveDataUtils.SettingData.enableVibrate);current.checks.Add("Vibration: toggled through pointer click and restored");
                }
                if(page is UIDailyTaskPage tasks)
                {
                    if(tasks.enableTab){Click(tasks.tabWeekly);Click(tasks.tabPlaytime);Click(tasks.tabDaily);current.checks.Add("Enabled task tabs: pointer clicks completed");}
                    int count=tasks.dailyPlaytimeRoot.GetComponentsInChildren<UIDailyTaskElement>().Length;
                    if(count==0)throw new InvalidOperationException("Configured playtime tasks were not displayed.");
                    current.checks.Add("Production task rows displayed: "+count);
                }
                Button close=FindClose(page);
                if(close!=null){Click(close);current.closeMethod="EventSystem pointer click: "+close.name;}
                else {UIModule.Instance.ClosePage(new PageId(id));current.closeMethod="Production UIModule.ClosePage";}
                run.phase=3;next=EditorApplication.timeSinceStartup+1.5;Save();return;
            }
            if(run.phase==20 && page is RealWithdrawPanel scrolled)
            {
                var tiers=scrolled.withdrawLevelRoot.GetComponentsInChildren<WithdrawLevelItem>();
                if(tiers.Length==0)throw new InvalidOperationException("No withdrawal tiers displayed.");
                var last=tiers[tiers.Length-1];Click(last.GetComponent<Button>());
                if(scrolled.CurrentWithdrawLevelItem!=last||!last.selectObj.activeSelf)throw new InvalidOperationException("Last tier selection failed.");
                foreach(var tier in tiers)
                {
                    var border=tier.transform.Find("ApprovedSelectionBorder");
                    if(border==null||border.gameObject.activeSelf!=tier.selectObj.activeSelf)throw new InvalidOperationException("Selection border is out of sync.");
                }
                current.checks.Add("Last tier is reachable after scrolling; its Button updates the selection and border");
                run.phase=21;next=EditorApplication.timeSinceStartup+.75;Save();return;
            }
            if(run.phase==21 && page is RealWithdrawPanel lower)
            {
                // Selecting a locked tier expands the progress footer; include its new height.
                Canvas.ForceUpdateCanvases();
                var scroll=lower.GetComponentInChildren<ScrollRect>();scroll.StopMovement();scroll.verticalNormalizedPosition=0;
                Canvas.ForceUpdateCanvases();
                var footerCorners=new Vector3[4];var viewportCorners=new Vector3[4];
                ((RectTransform)lower.completeHintTxt.transform.parent).GetWorldCorners(footerCorners);
                scroll.viewport.GetWorldCorners(viewportCorners);
                if(footerCorners[0].y<viewportCorners[0].y-1)throw new InvalidOperationException("Expanded progress footer cannot be scrolled fully into view.");
                current.checks.Add("Expanded locked-tier progress footer scrolls fully into the viewport");
                ScreenCapture.CaptureScreenshot(Folder+"withdrawal-scrolled.png");
                run.phase=22;next=EditorApplication.timeSinceStartup+.75;Save();return;
            }
            if(run.phase==22 && page is RealWithdrawPanel toRestore)
            {
                var scroll=toRestore.GetComponentInChildren<ScrollRect>();scroll.StopMovement();scroll.verticalNormalizedPosition=1;Canvas.ForceUpdateCanvases();
                run.phase=23;next=EditorApplication.timeSinceStartup+.75;Save();return;
            }
            if(run.phase==23 && page is RealWithdrawPanel restored)
            {
                if(originalTier!=null)Click(originalTier.GetComponent<Button>());
                if(restored.balanceTxt.text!=originalBalance)throw new InvalidOperationException("Balance changed during visual navigation verification.");
                current.checks.Add("Original tier restored; account balance unchanged; no withdrawal submitted");
                withdrawalVerified=true;run.phase=2;next=EditorApplication.timeSinceStartup+.5;Save();return;
            }
            if(run.phase==30)
            {
                var support=UIModule.Instance.GetPage(UIPageIds.ServicePanel);
                if(support==null||!support.gameObject.activeInHierarchy)return;
                var close=FindClose(support);if(close==null)throw new InvalidOperationException("Support page has no reachable close Button.");
                Click(close);run.phase=31;next=EditorApplication.timeSinceStartup+1;Save();return;
            }
            if(run.phase==31 && !UIModule.Instance.PageIsOpen(UIPageIds.ServicePanel))
            {
                current.checks.Add("Help -> Support Button opens the production service page; close returns to Help");
                supportVerified=true;run.phase=2;next=EditorApplication.timeSinceStartup+.5;Save();return;
            }
            if(run.phase==40)
            {
                var topics=UIModule.Instance.GetPage(UIPageIds.ServiceSelectPanel) as ServiceSelectPanel;
                if(topics==null||!topics.gameObject.activeInHierarchy)return;
                foreach(var button in topics.defaultButtons)if(!Inspect(button).centerHitsButton)throw new InvalidOperationException("Quick question is blocked: "+button.name);
                ScreenCapture.CaptureScreenshot(Folder+"service-topics.png");
                run.phase=41;next=EditorApplication.timeSinceStartup+.5;Save();return;
            }
            if(run.phase==41)
            {
                var topics=(ServiceSelectPanel)UIModule.Instance.GetPage(UIPageIds.ServiceSelectPanel);
                Click(topics.defaultButtons[0]);run.phase=42;next=EditorApplication.timeSinceStartup+1;Save();return;
            }
            if(run.phase==42&&!UIModule.Instance.PageIsOpen(UIPageIds.ServiceSelectPanel))
            {
                var service=(ServicePanel)page;
                if(string.IsNullOrEmpty(service.inputText.Text))throw new InvalidOperationException("Question selection did not fill the message draft.");
                current.checks.Add("All seven question Buttons reachable; selecting one fills the draft without sending");
                Click(service.selectQuestionButton);run.phase=43;next=EditorApplication.timeSinceStartup+1;Save();return;
            }
            if(run.phase==43)
            {
                var topics=UIModule.Instance.GetPage(UIPageIds.ServiceSelectPanel) as ServiceSelectPanel;
                if(topics==null||!topics.gameObject.activeInHierarchy)return;
                Click(topics.customButton);run.phase=44;next=EditorApplication.timeSinceStartup+1;Save();return;
            }
            if(run.phase==44&&!UIModule.Instance.PageIsOpen(UIPageIds.ServiceSelectPanel))
            {
                var service=(ServicePanel)page;
                if(!service.inputText.gameObject.activeInHierarchy||!string.IsNullOrEmpty(service.inputText.Text))throw new InvalidOperationException("Write message did not return an empty editable input.");
                current.checks.Add("Write message returns to the editable input; draft cleared; no support message sent");
                topicsVerified=true;run.phase=2;next=EditorApplication.timeSinceStartup+.5;Save();return;
            }
            if(run.phase==45&&page is StarRatingPopup selectedRating)
            {
                for(int i=0;i<selectedRating.starImages.Length;i++)if(selectedRating.starImages[i].sprite!=(i<3?selectedRating.starOn:selectedRating.starOff))throw new InvalidOperationException("Rating selection is out of sync.");
                current.checks.Add("Three-star selection updates all five visuals; no store rating submitted");
                ScreenCapture.CaptureScreenshot(current.screenshot);ratingVerified=true;run.phase=2;next=EditorApplication.timeSinceStartup+.5;Save();return;
            }
            if(run.phase==50)
            {
                ScreenCapture.CaptureScreenshot(Folder+"slot-animation.png");run.phase=51;next=EditorApplication.timeSinceStartup+.5;Save();return;
            }
            if(run.phase==51&&reelCompleted&&page is SlotPanel settledSlots)
            {
                foreach(var entry in settledSlots.slotMachineManager.slotEntries)
                    if(entry.img1.sprite==null)throw new InvalidOperationException("A reel completed without a visible result sprite.");
                if(SlotProgressUtil.Current!=originalSpinProgress)throw new InvalidOperationException("Presentation test consumed spin progress.");
                UnityEngine.Random.state=originalRandom;
                current.checks.Add("Existing reel animation reaches its completion callback with three result sprites; no spin consumed, advertisement requested or account reward issued");
                ScreenCapture.CaptureScreenshot(Folder+"slot-settled.png");reelVerified=true;run.phase=2;next=EditorApplication.timeSinceStartup+.75;Save();return;
            }
            if(run.phase==3&&!UIModule.Instance.PageIsOpen(new PageId(id)))
            {
                current.closed=true;run.index++;run.phase=0;deadline=EditorApplication.timeSinceStartup+35;
                if(run.index==Pages.Length)run.status="complete";Save();
            }
        }
        catch(Exception e){run.status="failed";run.error=e.ToString();Save();Debug.LogException(e);}
    }
    private static async UniTask Open(string id)
    {
        opening=true;
        try
        {
            if(id=="UIWithdrawalPanel")
            {
                if(paymentPlatforms.Count==0)throw new InvalidOperationException("No production payment platform available for form verification.");
                await UIModule.Instance.OpenPage<AccountModule.OceanShineWithdrawalPageResponse.WithdrawalPlatform,List<AccountModule.OceanShineWithdrawalPageResponse.WithdrawalPlatform>,E_WithdrawType,Action,bool>(UIPageIds.UIWithdrawalPanel,paymentPlatforms[0],new List<AccountModule.OceanShineWithdrawalPageResponse.WithdrawalPlatform>(paymentPlatforms),E_WithdrawType.Real,null,false);
            }
            else await UIModule.Instance.OpenPage(new PageId(id));
        }
        catch(Exception e){run.status="failed";run.error=e.ToString();Save();}
        finally{opening=false;}
    }
    private static Button FindClose(UIPageBase page)
    {
        if(page is PausePanel pause)return pause.CloseButton;
        if(page is UIDailyTaskPage tasks)return tasks.closeBtn;
        if(page is SlotPanel slot)return slot.closeBtn;
        if(page is SlotFAQPanel) return page.transform.Find("Content/Btn").GetComponent<Button>();
        foreach(var button in page.GetComponentsInChildren<Button>())
        {string n=button.name.ToLowerInvariant();if((n.Contains("close")||n=="back"||n=="approvedback"||n=="backbtn"||n=="backbutton")&&Inspect(button).centerHitsButton)return button;}
        return null;
    }
    private static Hit Inspect(Button button)
    {
        var graphic=button.targetGraphic;var canvas=button.GetComponentInParent<Canvas>().rootCanvas;
        var rect=graphic!=null?graphic.rectTransform:(RectTransform)button.transform;
        var point=RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,rect.TransformPoint(rect.rect.center));
        var pointer=new PointerEventData(EventSystem.current){position=point,button=PointerEventData.InputButton.Left};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
        var hit=hits.Count>0?hits[0].gameObject:null;var handler=hit!=null?ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit):null;
        bool onScreen=point.x>=0&&point.x<=Screen.width&&point.y>=0&&point.y<=Screen.height;
        return new Hit{name=button.name,center=point,interactable=button.IsInteractable(),topHit=hit!=null?hit.name:"none",centerHitsButton=onScreen&&handler==button.gameObject};
    }
    private static void Click(Button button)
    {
        var hit=Inspect(button);if(!hit.interactable||!hit.centerHitsButton)throw new InvalidOperationException("Button center blocked: "+hit.name+" by "+hit.topHit);
        var pointer=new PointerEventData(EventSystem.current){position=hit.center,button=PointerEventData.InputButton.Left,eligibleForClick=true};
        ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerDownHandler);ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerUpHandler);ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);
    }
    private static void VerifyToggle(Button button,Func<bool> read)
    {
        bool original=read();Click(button);if(read()==original)throw new InvalidOperationException("Toggle did not change: "+button.name);
        Click(button);if(read()!=original)throw new InvalidOperationException("Toggle did not restore: "+button.name);
    }
    private static void VerifyHud()
    {
        var core=UnityEngine.Object.FindObjectOfType<CorePlay.CorePlayUI>();var slot=UnityEngine.Object.FindObjectOfType<SlotEnter>();
        if(core==null||slot==null)return;
        string folder=hudCaptureFolder;hudCaptureFolder=null;var report=new Run{startedUtc=DateTime.UtcNow.ToString("O"),status="complete"};
        var result=new PageResult{page="Game HUD",opened=true,screenshot=folder+"gameplay.png"};report.pages.Add(result);
        try
        {
            var buttons=new[]{slot.GetComponent<Button>(),core.transform.Find("UI/Bottom/Undo").GetComponent<Button>(),core.transform.Find("UI/Bottom/Magic").GetComponent<Button>(),core.transform.Find("UI/Bottom/Shuffle").GetComponent<Button>()};
            var rectangles=new Rect[buttons.Length];
            var trayBounds=GraphicBounds(core.transform.Find("UI/Bottom/Box_Root/BastetUp").GetComponent<Graphic>());
            for(int i=0;i<buttons.Length;i++)
            {
                var hit=Inspect(buttons[i]);result.buttons.Add(hit);if(!hit.centerHitsButton)throw new InvalidOperationException("HUD button blocked: "+hit.name+" by "+hit.topHit);
                rectangles[i]=GraphicBounds(buttons[i].targetGraphic);
                foreach(var graphic in buttons[i].GetComponentsInChildren<Graphic>())
                {
                    if(!graphic.enabled||graphic.color.a<=0)continue;
                    var bounds=GraphicBounds(graphic);var combined=rectangles[i];rectangles[i]=Rect.MinMaxRect(Mathf.Min(combined.xMin,bounds.xMin),Mathf.Min(combined.yMin,bounds.yMin),Mathf.Max(combined.xMax,bounds.xMax),Mathf.Max(combined.yMax,bounds.yMax));
                }
                if(rectangles[i].xMin<0||rectangles[i].yMin<0||rectangles[i].xMax>Screen.width||rectangles[i].yMax>Screen.height)throw new InvalidOperationException("HUD visual is outside the screen: "+hit.name);
                if(rectangles[i].Overlaps(trayBounds))throw new InvalidOperationException("HUD visual overlaps the tray: "+hit.name);
                for(int j=0;j<i;j++)if(rectangles[i].Overlaps(rectangles[j]))throw new InvalidOperationException("HUD buttons overlap: "+hit.name+" and "+buttons[j].name);
            }
            result.checks.Add("All four lower HUD Buttons receive center pointer hits; their complete visible graphics remain on-screen, do not overlap one another, and do not overlap the tray");
            OrchardSkinValidation.CaptureRuntimeSnapshot(Path.ChangeExtension(result.screenshot,".json"),result.screenshot);ScreenCapture.CaptureScreenshot(result.screenshot);
        }
        catch(Exception e){report.status="failed";report.error=e.ToString();}
        File.WriteAllText(folder+"hud-layout.json",JsonUtility.ToJson(report,true));
    }
    private static Rect GraphicBounds(Graphic graphic)
    {
        var canvas=graphic.GetComponentInParent<Canvas>().rootCanvas;var corners=new Vector3[4];graphic.rectTransform.GetWorldCorners(corners);
        var camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;var bottom=RectTransformUtility.WorldToScreenPoint(camera,corners[0]);var top=RectTransformUtility.WorldToScreenPoint(camera,corners[2]);return Rect.MinMaxRect(bottom.x,bottom.y,top.x,top.y);
    }
    private static void Save(){Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"navigation.json",JsonUtility.ToJson(run,true));}
}
#endif
