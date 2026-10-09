#if BIZZA_REAL_WITHDRAW
using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static partial class OrchardApprovedRuntimeCheck
{
    private static Run historyRun;
    private static string historyFolder;
    private static double historyNext,historyDeadline;
    private static long historyLastWithdrawal;
    private static float historyBalance;
    private static readonly List<WithdrawHistoryItem> historyFixtures=new List<WithdrawHistoryItem>();
    private static readonly List<GameObject> historyExistingRows=new List<GameObject>();
    private static bool historyEmpty;
    private static void StartHistoryCheck(bool shorter)
    {
        historyFolder="Design/OrchardHistoryRefinement-20260928/"+(shorter?"RuntimeShort/":"Runtime/");Directory.CreateDirectory(historyFolder);
        historyRun=new Run{startedUtc=DateTime.UtcNow.ToString("O"),status="running"};historyNext=0;historyDeadline=EditorApplication.timeSinceStartup+100;SaveHistory();
    }
    private static void SaveHistory(){File.WriteAllText(historyFolder+"verification.json",JsonUtility.ToJson(historyRun,true));}
    private static AccountModule.OceanShineWithdrawalRecord HistoryRecord(int index,int status=0)
    {
        int row=index%3;
        return new AccountModule.OceanShineWithdrawalRecord{Os_Pym=row==1?"pix":"pagbank",Os_Prc=row==0?.03:.02,Os_Dat=new[]{"27 set 2026","25 set 2026","22 set 2026"}[row],Os_Re="j***@example.com",Os_Cp="***.***.***-**",Os_Rn="Jogador",Os_Sts=status==0?new[]{1,3,4}[row]:status,Os_Tsm="Revise os dados da conta."};
    }
    private static void RestoreHistoryRows(WithdrawHistory page)
    {
        foreach(var row in historyFixtures)if(row!=null){row.gameObject.SetActive(false);UnityEngine.Object.Destroy(row.gameObject);}historyFixtures.Clear();
        foreach(var row in historyExistingRows)if(row!=null)row.SetActive(true);historyExistingRows.Clear();
        if(page!=null){page.emptyHint.SetActive(historyEmpty);page.GetComponentInChildren<ScrollRect>().verticalNormalizedPosition=1;}
    }
    private static void TickHistoryCheck()
    {
        if(historyRun==null||historyRun.status!="running"||EditorApplication.timeSinceStartup<historyNext)return;
        historyNext=EditorApplication.timeSinceStartup+.3;
        var history=UIModule.Instance==null?null:UIModule.Instance.GetPage(UIPageIds.WithdrawHistory) as WithdrawHistory;
        try
        {
            if(EditorApplication.timeSinceStartup>historyDeadline)throw new TimeoutException("History check phase "+historyRun.phase);
            if(!EditorApplication.isPlaying||!HarvestBridge.Ready||UIModule.Instance==null||UIModule.Instance.Opening||TransparentBlock.IsBlock)return;
            if(historyRun.phase==0)
            {
                if(UIModule.Instance.HasPopup)return;
                historyBalance=ItemUtils.GetItemCount(E_ItemType.Dollar);historyLastWithdrawal=SaveDataUtils.GameData.lastWithdrawTime;
                historyRun.pages.Add(new PageResult{page="WithdrawHistory",openMethod="InitWZ full initialization, production UIModule and history request; isolated prefab rows for three-state visual/interaction coverage only, never sent to server or saved"});
                Open("WithdrawHistory").Forget();historyRun.phase=1;historyNext=EditorApplication.timeSinceStartup+4;SaveHistory();return;
            }
            var result=historyRun.pages[0];
            if(historyRun.phase==1)
            {
                if(history==null||!history.gameObject.activeInHierarchy||!history.GetComponent<OrchardHistoryVisual>().IsReady)return;
                result.opened=true;CashScreenshot(historyFolder+"00-live-history.png");
                historyFixtures.Clear();historyExistingRows.Clear();historyEmpty=history.emptyHint.activeSelf;
                foreach(Transform child in history.root)if(child.gameObject.activeSelf){historyExistingRows.Add(child.gameObject);child.gameObject.SetActive(false);}
                history.emptyHint.SetActive(false);
                for(int i=0;i<3;i++){var row=UnityEngine.Object.Instantiate(history.item,history.root,false);row.name="DisposableHistoryFixture"+i;row.gameObject.SetActive(true);row.Init(HistoryRecord(i));historyFixtures.Add(row);}
                Canvas.ForceUpdateCanvases();historyRun.phase=2;historyNext=EditorApplication.timeSinceStartup+1;SaveHistory();return;
            }
            if(historyRun.phase==2)
            {
                foreach(var row in historyFixtures)if(!row.GetComponent<OrchardHistoryVisual>().IsReady)return;
                foreach(int status in new[]{1,2,3,4,1})
                {
                    var row=historyFixtures[0];row.Init(HistoryRecord(0,status));
                    if(row.processingObj.activeSelf!=(status==1)||row.successObj.activeSelf!=(status==3)||row.failObj.activeSelf!=(status==2||status==4)||row.dueText.gameObject.activeSelf!=(status==2||status==4))throw new InvalidOperationException("Reused row retained an incorrect status or failure hint.");
                    if(row.nameTxt.gameObject.activeSelf||row.cpfTxt.gameObject.activeSelf||row.emailTxt.text!="j***@example.com")throw new InvalidOperationException("Compact receiving-account row is not correctly reset.");
                }
                result.checks.Add("Production row Init: statuses 1/2/3/4/1 switch the clock/check/cross and failure hint correctly on a reused item; receiving account reset without overlap");
                var first=historyFixtures[0];var pix=HistoryRecord(1);first.Init(pix);if(first.emailTxt.text!=pix.Os_Cp)throw new InvalidOperationException("PIX document not displayed.");
                var fallback=HistoryRecord(0);fallback.Os_Re="";fallback.Os_Ra="receiver@example.com";first.Init(fallback);if(first.emailTxt.text!=fallback.Os_Ra)throw new InvalidOperationException("Receiver fallback not displayed.");first.Init(HistoryRecord(0));
                Canvas.ForceUpdateCanvases();
                foreach(var row in historyFixtures)
                {
                    if(!row.withdrawImg.preserveAspect)throw new InvalidOperationException("Payment logo stretched.");
                    foreach(var text in row.GetComponentsInChildren<TMPro.TMP_Text>()){text.ForceMeshUpdate();if(text.isTextOverflowing)throw new InvalidOperationException("History text overflows: "+text.name);}
                    var rowBounds=GraphicBounds(row.transform.Find("bg").GetComponent<Image>());
                    if(rowBounds.xMin<0||rowBounds.yMin<0||rowBounds.xMax>Screen.width||rowBounds.yMax>Screen.height)throw new InvalidOperationException("History card is off screen.");
                    if(row.dueText.gameObject.activeSelf&&GraphicBounds(row.dueText).yMin<rowBounds.yMin)throw new InvalidOperationException("Failure hint is clipped by the card.");
                    foreach(var state in new[]{row.processingObj,row.successObj,row.failObj})if(state.activeSelf&&state.GetComponent<Image>().sprite==null)throw new InvalidOperationException("History status artwork missing.");
                }
                foreach(var path in new[]{"CloseBtn","ApprovedHelp","ApprovedSupport"})
                {var hit=Inspect(history.transform.Find(path).GetComponent<Button>());result.buttons.Add(hit);if(!hit.interactable||!hit.centerHitsButton)throw new InvalidOperationException("History Button blocked: "+path);}
                result.checks.Add("Three record cards, all labels and failure reason fit; payment logos preserve aspect; Back, Help and Support Buttons receive pointer hits");
                result.screenshot=historyFolder+"01-history.png";CashScreenshot(result.screenshot);historyRun.phase=3;historyNext=EditorApplication.timeSinceStartup+1;SaveHistory();return;
            }
            if(historyRun.phase==3)
            {
                for(int i=3;i<12;i++){var row=UnityEngine.Object.Instantiate(history.item,history.root,false);row.gameObject.SetActive(true);row.Init(HistoryRecord(i));historyFixtures.Add(row);}
                Canvas.ForceUpdateCanvases();var scroll=history.GetComponentInChildren<ScrollRect>();scroll.StopMovement();scroll.verticalNormalizedPosition=0;
                historyRun.phase=4;historyNext=EditorApplication.timeSinceStartup+1;SaveHistory();return;
            }
            if(historyRun.phase==4)
            {
                var scroll=history.GetComponentInChildren<ScrollRect>();var last=historyFixtures[historyFixtures.Count-1];
                var corners=new Vector3[4];scroll.viewport.GetWorldCorners(corners);var canvas=scroll.GetComponentInParent<Canvas>();var cam=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
                var lower=RectTransformUtility.WorldToScreenPoint(cam,corners[0]);var upper=RectTransformUtility.WorldToScreenPoint(cam,corners[2]);var bounds=GraphicBounds(last.transform.Find("bg").GetComponent<Image>());
                if(bounds.yMin<lower.y-2||bounds.yMax>upper.y+2)throw new InvalidOperationException("Last row cannot be fully reached by scrolling.");
                result.checks.Add("Twelve real prefab rows: scrolling reaches the complete last failed record and its reason inside the rectangular viewport");CashScreenshot(historyFolder+"02-scrolled-history.png");
                Click(history.transform.Find("ApprovedHelp").GetComponent<Button>());historyRun.phase=5;historyNext=EditorApplication.timeSinceStartup+1;SaveHistory();return;
            }
            if(historyRun.phase==5)
            {var faq=UIModule.Instance.GetPage(UIPageIds.QFA);if(faq==null||!faq.gameObject.activeInHierarchy)return;Click(FindClose(faq));historyRun.phase=6;historyNext=EditorApplication.timeSinceStartup+1;SaveHistory();return;}
            if(historyRun.phase==6)
            {if(UIModule.Instance.PageIsOpen(UIPageIds.QFA))return;Click(history.transform.Find("ApprovedSupport").GetComponent<Button>());historyRun.phase=7;historyNext=EditorApplication.timeSinceStartup+1;SaveHistory();return;}
            if(historyRun.phase==7)
            {var support=UIModule.Instance.GetPage(UIPageIds.ServicePanel);if(support==null||!support.gameObject.activeInHierarchy)return;Click(FindClose(support));historyRun.phase=8;historyNext=EditorApplication.timeSinceStartup+1;SaveHistory();return;}
            if(historyRun.phase==8)
            {if(UIModule.Instance.PageIsOpen(UIPageIds.ServicePanel))return;result.checks.Add("Help opens FAQ; bottom question-mark Button opens support; both return to history");RestoreHistoryRows(history);Click(FindClose(history));historyRun.phase=9;historyNext=EditorApplication.timeSinceStartup+1;SaveHistory();return;}
            if(historyRun.phase==9&&!UIModule.Instance.PageIsOpen(UIPageIds.WithdrawHistory))
            {
                if(historyBalance!=ItemUtils.GetItemCount(E_ItemType.Dollar)||historyLastWithdrawal!=SaveDataUtils.GameData.lastWithdrawTime)throw new InvalidOperationException("History verification changed account state.");
                result.closed=true;result.closeMethod="EventSystem Back Button";result.checks.Add("Disposable rows removed, original records/empty state restored, returned to gameplay; balance and withdrawal timestamp unchanged");historyRun.status="complete";SaveHistory();
            }
        }
        catch(Exception e){RestoreHistoryRows(history);historyRun.status="failed";historyRun.error=e.ToString();SaveHistory();Debug.LogException(e);}
    }
}
#endif
