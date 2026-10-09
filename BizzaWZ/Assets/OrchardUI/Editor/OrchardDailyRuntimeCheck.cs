#if BIZZA_REAL_WITHDRAW
using System;
using System.IO;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static partial class OrchardApprovedRuntimeCheck
{
    private static Run dailyRun;
    private static string dailyFolder,dailyCountdown;
    private static double dailyNext,dailyDeadline;
    private static int dailySavedProgress,dailyAdClicks;
    private static float dailyBalance;
    private static long dailyLastWithdrawal;
    private static void StartDailyCheck(bool shorter)
    {
        dailyFolder="Design/OrchardDailyMissionRefinement-20260928/"+(shorter?"RuntimeShort/":"Runtime/");Directory.CreateDirectory(dailyFolder);
        dailyRun=new Run{startedUtc=DateTime.UtcNow.ToString("O"),status="running"};dailyNext=0;dailyDeadline=EditorApplication.timeSinceStartup+110;SaveDaily();
    }
    private static void SaveDaily()=>File.WriteAllText(dailyFolder+"verification.json",JsonUtility.ToJson(dailyRun,true));
    private static void CheckDailyView(DailyMissionPanel page,int current,int maximum,int state)
    {
        var progress=page.transform.Find("Content/ApprovedProgressValue").GetComponent<TMP_Text>();var fill=page.transform.Find("Content/ApprovedProgressTrack/Fill").GetComponent<Image>();
        if(progress.text!=$"{current} / {maximum}"||!Mathf.Approximately(fill.fillAmount,maximum>0?(float)current/maximum:0))throw new InvalidOperationException("Daily progress text/fill disagrees with task data.");
        if(page.GoObj.activeSelf!=(state<=1)||page.WithdrawObj.activeSelf!=(state==2)||page.ClaimedObj.activeSelf!=(state==3)||page.claimedHint.gameObject.activeSelf!=(state==3))throw new InvalidOperationException("Daily task state visibility mismatch.");
        foreach(var text in page.GetComponentsInChildren<TMP_Text>()){text.ForceMeshUpdate();if(text.isTextOverflowing)throw new InvalidOperationException("Daily text overflows: "+text.name);}
    }
    private static void CheckDailyButton(DailyMissionPanel page,string path)
    {
        var button=page.transform.Find(path).GetComponent<Button>();var hit=Inspect(button);dailyRun.pages[0].buttons.Add(hit);
        if(!hit.interactable||!hit.centerHitsButton||button.targetGraphic!=button.GetComponent<Image>())throw new InvalidOperationException("Daily Button blocked or detached from its visible graphic: "+path);
    }
    private static void TickDailyCheck()
    {
        if(dailyRun==null||dailyRun.status!="running"||EditorApplication.timeSinceStartup<dailyNext)return;dailyNext=EditorApplication.timeSinceStartup+.3;
        try
        {
            if(EditorApplication.timeSinceStartup>dailyDeadline)throw new TimeoutException("Daily check phase "+dailyRun.phase);
            if(!EditorApplication.isPlaying||!HarvestBridge.Ready||UIModule.Instance==null||UIModule.Instance.Opening||TransparentBlock.IsBlock)return;
            var page=UIModule.Instance.GetPage(UIPageIds.DailyMissionPanel) as DailyMissionPanel;
            if(dailyRun.phase==0)
            {
                if(UIModule.Instance.HasPopup)return;
                dailyRun.pages.Add(new PageResult{page="DailyMissionPanel",openMethod="InitWZ full initialization and production UIModule.OpenPage; capture live server response before temporary presentation fixtures"});
                UIModule.Instance.OpenPage(UIPageIds.DailyMissionPanel).Forget();dailyRun.phase=1;dailyNext=EditorApplication.timeSinceStartup+4;SaveDaily();return;
            }
            var result=dailyRun.pages[0];
            if(dailyRun.phase==1)
            {
                if(page==null||!page.gameObject.activeInHierarchy||!page.GetComponent<OrchardDailyVisual>().IsReady)return;
                dailySavedProgress=SaveDataUtils.GameData.userLookDailyAdCount;dailyAdClicks=SaveDataUtils.GameData.btnDailyTaskClick;dailyBalance=ItemUtils.GetItemCount(E_ItemType.Dollar);dailyLastWithdrawal=SaveDataUtils.GameData.lastWithdrawTime;
                int state=page.GoObj.activeSelf?1:page.WithdrawObj.activeSelf?2:3;CheckDailyView(page,dailySavedProgress,Math.Max(0,SaveDataUtils.GameData.userLookDailyAdCountMax),state);
                result.opened=true;dailyCountdown=page.refreshTimeTxt.text;CashScreenshot(dailyFolder+"00-live-daily.png");result.checks.Add("Live response populates progress and state after formal initialization");dailyRun.phase=2;dailyNext=EditorApplication.timeSinceStartup+2;SaveDaily();return;
            }
            if(dailyRun.phase==2)
            {
                if(dailyCountdown==page.refreshTimeTxt.text)throw new InvalidOperationException("Daily refresh countdown stopped.");
                page.RefreshTaskView(8,30,30,.2,1);dailyRun.phase=3;dailyNext=EditorApplication.timeSinceStartup+.7;SaveDaily();return;
            }
            if(dailyRun.phase==3)
            {
                CheckDailyView(page,8,30,1);CheckDailyButton(page,"Title/CloseBtn");CheckDailyButton(page,"Content/GoBtn");
                string expected=LanguageUtils.GetText("CurrencyToken")+WithdrawalUtil.GetCustomizedValueByCountryType(.2f);
                if(page.transform.Find("Content/ApprovedRewardValue").GetComponent<TMP_Text>().text!=expected||page.hintsTxt.text.Contains(expected))throw new InvalidOperationException("Reward must be formatted dynamically and not duplicated in the requirement caption.");
                foreach(string path in new[]{"BG (2)/Image (2)","Title/CloseBtn","Content/ApprovedGift","Content/ApprovedAmount","Content/ApprovedProgressTrack","Content/GoBtn","Title/ReferenceClock"})
                {var image=page.transform.Find(path).GetComponent<Image>();var bounds=GraphicBounds(image);if(image.sprite==null||!image.enabled||image.color!=Color.white||image.type!=Image.Type.Simple||bounds.xMin<0||bounds.yMin<0||bounds.xMax>Screen.width||bounds.yMax>Screen.height)throw new InvalidOperationException("Daily art is clipped, tinted or missing: "+path);}
                var fill=page.transform.Find("Content/ApprovedProgressTrack/Fill").GetComponent<Image>();if(Mathf.Abs(fill.rectTransform.rect.width-568*8f/30)>1)throw new InvalidOperationException("Daily fill width did not follow progress.");
                result.screenshot=dailyFolder+"01-daily-mission.png";CashScreenshot(result.screenshot);result.checks.Add("Reward appears once; 8/30 uses a proportional orange fill over dark track; countdown advances; seven complete untinted art sprites fit screen");dailyRun.phase=4;dailyNext=EditorApplication.timeSinceStartup+1;SaveDaily();return;
            }
            if(dailyRun.phase==4){page.RefreshTaskView(30,30,30,.2,2);dailyRun.phase=5;dailyNext=EditorApplication.timeSinceStartup+.7;SaveDaily();return;}
            if(dailyRun.phase==5){CheckDailyView(page,30,30,2);CheckDailyButton(page,"Content/WithdrawBtn");CashScreenshot(dailyFolder+"02-ready.png");dailyRun.phase=6;dailyNext=EditorApplication.timeSinceStartup+1;SaveDaily();return;}
            if(dailyRun.phase==6){page.RefreshTaskView(30,30,30,.2,3);dailyRun.phase=7;dailyNext=EditorApplication.timeSinceStartup+.7;SaveDaily();return;}
            if(dailyRun.phase==7){CheckDailyView(page,30,30,3);CheckDailyButton(page,"Content/ClaimedBtn");CashScreenshot(dailyFolder+"03-claimed.png");result.checks.Add("Incomplete / ready / claimed state presents exactly one standard action Button; each action receives center hits. Ad and payout actions are not invoked.");dailyRun.phase=8;dailyNext=EditorApplication.timeSinceStartup+1;SaveDaily();return;}
            if(dailyRun.phase==8){page.RefreshTaskView(9,0,0,.2,0);dailyRun.phase=9;dailyNext=EditorApplication.timeSinceStartup+.7;SaveDaily();return;}
            if(dailyRun.phase==9)
            {CheckDailyView(page,0,0,0);if(page.transform.Find("Content/ApprovedProgressTrack/Fill").GetComponent<RectTransform>().rect.width!=0)throw new InvalidOperationException("Zero target renders a nonempty fill.");result.checks.Add("Zero target safely produces 0/0 and empty fill");Click(page.transform.Find("Title/CloseBtn").GetComponent<Button>());dailyRun.phase=10;dailyNext=EditorApplication.timeSinceStartup+1;SaveDaily();return;}
            if(dailyRun.phase==10){if(UIModule.Instance.HasPopup)return;UIModule.Instance.OpenPage(UIPageIds.DailyMissionPanel).Forget();dailyRun.phase=11;dailyNext=EditorApplication.timeSinceStartup+4;SaveDaily();return;}
            if(dailyRun.phase==11)
            {
                if(page==null||!page.gameObject.activeInHierarchy)return;int state=page.GoObj.activeSelf?1:page.WithdrawObj.activeSelf?2:3;CheckDailyView(page,SaveDataUtils.GameData.userLookDailyAdCount,Math.Max(0,SaveDataUtils.GameData.userLookDailyAdCountMax),state);
                Click(page.transform.Find("Title/CloseBtn").GetComponent<Button>());dailyRun.phase=12;dailyNext=EditorApplication.timeSinceStartup+1;SaveDaily();return;
            }
            if(dailyRun.phase==12&&!UIModule.Instance.PageIsOpen(UIPageIds.DailyMissionPanel))
            {
                if(dailySavedProgress!=SaveDataUtils.GameData.userLookDailyAdCount||dailyAdClicks!=SaveDataUtils.GameData.btnDailyTaskClick||dailyBalance!=ItemUtils.GetItemCount(E_ItemType.Dollar)||dailyLastWithdrawal!=SaveDataUtils.GameData.lastWithdrawTime)throw new InvalidOperationException("Daily presentation verification changed account/ad state.");
                result.closed=true;result.closeMethod="EventSystem Close, reopen actual server state, EventSystem Close";result.checks.Add("Reopen restores live data; saved daily count, ad-click count, balance and withdrawal time unchanged");dailyRun.status="complete";SaveDaily();
            }
        }
        catch(Exception e){dailyRun.status="failed";dailyRun.error=e.ToString();SaveDaily();Debug.LogException(e);}
    }
}
#endif
