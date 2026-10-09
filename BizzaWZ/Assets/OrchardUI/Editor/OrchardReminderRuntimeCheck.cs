#if BIZZA_REAL_WITHDRAW
using System;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static partial class OrchardApprovedRuntimeCheck
{
    private static Run reminderRun;
    private static string reminderFolder;
    private static double reminderNext,reminderDeadline;
    private static long reminderLastWithdrawal;
    private static float reminderBalance;
    private static void StartReminderCheck(bool shorter)
    {
        reminderFolder="Design/OrchardReminderRefinement-20260928/"+(shorter?"RuntimeShort/":"Runtime/");Directory.CreateDirectory(reminderFolder);
        reminderRun=new Run{startedUtc=DateTime.UtcNow.ToString("O"),status="running"};reminderNext=0;reminderDeadline=EditorApplication.timeSinceStartup+100;SaveReminder();
    }
    private static void SaveReminder(){File.WriteAllText(reminderFolder+"verification.json",JsonUtility.ToJson(reminderRun,true));}
    private static void TickReminderCheck()
    {
        if(reminderRun==null||reminderRun.status!="running"||EditorApplication.timeSinceStartup<reminderNext)return;
        reminderNext=EditorApplication.timeSinceStartup+.3;
        try
        {
            if(EditorApplication.timeSinceStartup>reminderDeadline)throw new TimeoutException("Reminder check phase "+reminderRun.phase);
            if(!EditorApplication.isPlaying||!HarvestBridge.Ready||UIModule.Instance==null||UIModule.Instance.Opening||TransparentBlock.IsBlock)return;
            var page=UIModule.Instance.GetPage(UIPageIds.DailyWithdrawPanel) as DailyWithdrawPanel;
            if(reminderRun.phase==0)
            {
                if(UIModule.Instance.HasPopup)return;
                reminderBalance=ItemUtils.GetItemCount(E_ItemType.Dollar);reminderLastWithdrawal=SaveDataUtils.GameData.lastWithdrawTime;
                reminderRun.pages.Add(new PageResult{page="DailyWithdrawPanel",openMethod="InitWZ full initialization and production UIModule.OpenPage; actual account amounts and server payment methods"});
                Open("DailyWithdrawPanel").Forget();reminderRun.phase=1;reminderNext=EditorApplication.timeSinceStartup+4;SaveReminder();return;
            }
            var result=reminderRun.pages[0];
            if(reminderRun.phase==1)
            {
                if(page==null||!page.gameObject.activeInHierarchy||!page.GetComponent<OrchardReminderVisual>().IsReady)return;
                var account=AccountModule.Instance.Os_Current_Uso;
                string balance=WithdrawalUtil.GetCustomizedValueByCountryType((float)account.GetBalance());
                string amount=LanguageUtils.GetText("CurrencyToken")+WithdrawalUtil.GetCustomizedValueByCountryType((float)account.Os_Ewl);
                if(page.balanceTxt.text!=balance||page.clashTxt.text!=amount)return;
                result.opened=true;
                foreach(var path in new[]{"Content (1)/CloseBtn","Content (1)/WithdrawBtn"})
                {var button=page.transform.Find(path).GetComponent<Button>();var hit=Inspect(button);result.buttons.Add(hit);if(!hit.interactable||!hit.centerHitsButton||button.targetGraphic==null)throw new InvalidOperationException("Reminder Button blocked or without a graphic: "+path);}
                foreach(var text in page.GetComponentsInChildren<TMPro.TMP_Text>()){text.ForceMeshUpdate();if(text.isTextOverflowing)throw new InvalidOperationException("Reminder text overflows: "+text.name);}
                foreach(var path in new[]{"Content (1)/ApprovedMethod0","Content (1)/ApprovedMethod1","Content (1)/ApprovedMint","Content (1)/ApprovedCash"})
                {
                    var image=page.transform.Find(path).GetComponent<Image>();if(!image.gameObject.activeInHierarchy)continue;
                    var bounds=GraphicBounds(image);
                    if(!image.enabled||image.sprite==null||image.type!=Image.Type.Simple||image.color!=Color.white||bounds.xMin<0||bounds.yMin<0||bounds.xMax>Screen.width||bounds.yMax>Screen.height)throw new InvalidOperationException("Reminder artwork missing, tinted, flattened or offscreen: "+path);
                }
                var note=GraphicBounds(page.transform.Find("Content (1)/ApprovedBalanceNote").GetComponent<TMPro.TMP_Text>());
                var method=GraphicBounds(page.transform.Find("Content (1)/ApprovedMethod0").GetComponent<Image>());
                if(note.yMin<=method.yMax)throw new InvalidOperationException("Payment note overlaps card highlights.");
                for(int i=0;i<2;i++){var logo=page.transform.Find("Content (1)/ApprovedMethod"+i+"/Logo").GetComponent<Image>();if(logo.gameObject.activeInHierarchy&&(!logo.preserveAspect||logo.sprite==null))throw new InvalidOperationException("Payment logo is missing or stretched.");}
                result.checks.Add("Actual account balance and payout amount refreshed; payment logos retain server-driven visibility and preserve aspect");
                result.checks.Add("New amount rim and method-card highlights use complete untinted sprites; all labels fit, note clears the card edges, and both standard Buttons receive center hits");
                result.screenshot=reminderFolder+"00-reminder.png";CashScreenshot(result.screenshot);
                reminderRun.phase=2;reminderNext=EditorApplication.timeSinceStartup+1;SaveReminder();return;
            }
            if(reminderRun.phase==2)
            {Click(page.transform.Find("Content (1)/WithdrawBtn").GetComponent<Button>());reminderRun.phase=3;reminderNext=EditorApplication.timeSinceStartup+2;SaveReminder();return;}
            if(reminderRun.phase==3)
            {
                var withdrawal=UIModule.Instance.GetPage(UIPageIds.RealWithdrawPanel);
                if(withdrawal==null||!withdrawal.gameObject.activeInHierarchy)return;
                if(UIModule.Instance.PageIsOpen(UIPageIds.DailyWithdrawPanel))throw new InvalidOperationException("Reminder remained open after navigation.");
                result.checks.Add("View withdrawal Button closes the reminder and opens the existing RealWithdrawPanel; no withdrawal submission");
                Click(FindClose(withdrawal));reminderRun.phase=4;reminderNext=EditorApplication.timeSinceStartup+1;SaveReminder();return;
            }
            if(reminderRun.phase==4)
            {if(UIModule.Instance.HasPopup)return;Open("DailyWithdrawPanel").Forget();reminderRun.phase=5;reminderNext=EditorApplication.timeSinceStartup+3;SaveReminder();return;}
            if(reminderRun.phase==5)
            {if(page==null||!page.gameObject.activeInHierarchy)return;Click(page.transform.Find("Content (1)/CloseBtn").GetComponent<Button>());reminderRun.phase=6;reminderNext=EditorApplication.timeSinceStartup+1;SaveReminder();return;}
            if(reminderRun.phase==6&&!UIModule.Instance.PageIsOpen(UIPageIds.DailyWithdrawPanel))
            {
                if(reminderBalance!=ItemUtils.GetItemCount(E_ItemType.Dollar)||reminderLastWithdrawal!=SaveDataUtils.GameData.lastWithdrawTime)throw new InvalidOperationException("Reminder verification changed account state.");
                result.closed=true;result.closeMethod="Reopened reminder then EventSystem Close Button";result.checks.Add("Reopen and close work; account balance and last withdrawal timestamp unchanged");reminderRun.status="complete";SaveReminder();
            }
        }
        catch(Exception e){reminderRun.status="failed";reminderRun.error=e.ToString();SaveReminder();Debug.LogException(e);}
    }
}
#endif
