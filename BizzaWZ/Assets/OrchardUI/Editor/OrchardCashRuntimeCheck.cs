#if BIZZA_REAL_WITHDRAW
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static partial class OrchardApprovedRuntimeCheck
{
    private static Run cashRun;
    private static string cashFolder,cashStagesBefore;
    private static float cashBalanceBefore;
    private static bool cashStarterBefore;
    private static int cashClicksBefore,cashSelectedBefore;
    private static double cashNext,cashDeadline;
    private static void StartCashCheck(bool shorter)
    {
        cashFolder="Design/OrchardCashRefinement-20260928/"+(shorter?"RuntimeShort/":"Runtime/");Directory.CreateDirectory(cashFolder);
        cashRun=new Run{startedUtc=DateTime.UtcNow.ToString("O"),status="running"};cashNext=0;cashDeadline=EditorApplication.timeSinceStartup+120;SaveCash();
    }
    private static void SaveCash(){File.WriteAllText(cashFolder+"verification.json",JsonUtility.ToJson(cashRun,true));}
    private static void TickCashCheck()
    {
        if(cashRun==null||cashRun.status!="running"||EditorApplication.timeSinceStartup<cashNext)return;
        cashNext=EditorApplication.timeSinceStartup+.25;
        try
        {
            if(EditorApplication.timeSinceStartup>cashDeadline)throw new TimeoutException("Cash check timed out at phase "+cashRun.phase);
            if(!EditorApplication.isPlaying||!HarvestBridge.Ready||UIModule.Instance==null||UIModule.Instance.Opening||TransparentBlock.IsBlock)return;
            var page=UIModule.Instance.GetPage(UIPageIds.FakeWithdrawPanel) as FakeWithdrawPanel;
            if(cashRun.phase==0)
            {
                if(UIModule.Instance.HasPopup)return;
                cashBalanceBefore=ItemUtils.GetItemCount(E_ItemType.Dollar);cashStagesBefore=JsonUtility.ToJson(SaveDataUtils.FakeWithDrawPanelData);
                cashStarterBefore=SaveDataUtils.GameData.fakeWithdrawPanelFirstWithdraw;cashClicksBefore=SaveDataUtils.GameData.btnWithdrawClick;
                cashRun.pages.Add(new PageResult{page="FakeWithdrawPanel",openMethod="HUD dollar Button EventSystem click; production framework/server data"});
                Click(UnityEngine.Object.FindObjectOfType<CurrencyBar>().dollarBtn);cashRun.phase=1;cashNext=EditorApplication.timeSinceStartup+3;SaveCash();return;
            }
            var result=cashRun.pages[0];
            if(cashRun.phase==1)
            {
                if(page==null||!page.gameObject.activeInHierarchy||!page.GetComponent<OrchardCashVisual>().IsReady||page.items.Count==0)return;
                if(string.IsNullOrEmpty(page.items[0].amountTxt.text))return;
                result.opened=true;cashSelectedBefore=page.curSelectIndex;
                result.screenshot=cashFolder+"00-FakeWithdrawPanel.png";CashScreenshot(result.screenshot);
                var viewport=page.root.parent.GetComponent<RectMask2D>();if(viewport==null||viewport.GetComponent<Mask>()!=null)throw new InvalidOperationException("Cash grid still uses the old sprite mask.");
                var viewportBounds=GraphicBounds(page.root.parent.GetComponent<Image>());
                foreach(var item in page.items)
                {
                    if(!item.gameObject.activeInHierarchy)continue;
                    var hit=Inspect(item.btn);result.buttons.Add(hit);if(!hit.centerHitsButton||!hit.interactable)throw new InvalidOperationException("Amount Button is blocked: "+hit.name);
                    var bounds=GraphicBounds(item.btn.targetGraphic);
                    if(bounds.yMax>viewportBounds.yMax-1||bounds.yMin<viewportBounds.yMin+1||bounds.xMin<viewportBounds.xMin||bounds.xMax>viewportBounds.xMax)throw new InvalidOperationException("Amount card is clipped by the viewport: "+item.name);
                    if(item.btn.targetGraphic.gameObject!=item.btn.gameObject)throw new InvalidOperationException("Card artwork must belong to the Button itself.");
                }
                result.checks.Add("All live amount cards and all four borders fit inside RectMask2D with padding; each standard Button owns its visible card and receives center hits");
                cashRun.phase=2;cashRun.index=0;SaveCash();return;
            }
            if(cashRun.phase==2)
            {
                if(cashRun.index>=page.items.Count)
                {
                    Click(page.items[cashSelectedBefore].btn);result.checks.Add("Every selectable amount refreshed the exclusive green check, mint card, text color, progress and eligibility; initial selection restored");
                    cashRun.phase=3;SaveCash();return;
                }
                var item=page.items[cashRun.index];
                if(item.getedObj.activeSelf){cashRun.index++;SaveCash();return;}
                Click(item.btn);cashRun.phase=20;SaveCash();return;
            }
            if(cashRun.phase==20)
            {
                if(page.curSelectIndex!=cashRun.index)throw new InvalidOperationException("Amount selection did not update.");
                for(int i=0;i<page.items.Count;i++)
                {
                    bool selected=i==cashRun.index;var item=page.items[i];var background=item.btn.targetGraphic as Image;
                    if(item.selectObj.activeSelf!=selected||background.sprite.name!=(selected?"SelectedCard":"Card"))throw new InvalidOperationException("Cash selection artwork is out of sync.");
                    if(selected&&(item.selectObj.GetComponent<Image>().sprite.name!="Check"||item.amountTxt.color.g<=item.amountTxt.color.r))throw new InvalidOperationException("Selected check/text color is incorrect.");
                    if(!item.amountTxt.text.StartsWith(LanguageUtils.GetText("CurrencyToken"),StringComparison.Ordinal))throw new InvalidOperationException("Cash amount is missing its currency symbol.");
                    if(selected&&GraphicBounds(item.amountTxt).Overlaps(GraphicBounds(item.selectObj.GetComponent<Image>())))throw new InvalidOperationException("Selected amount overlaps the green check.");
                }
                foreach(var text in page.GetComponentsInChildren<TMPro.TMP_Text>())
                {text.ForceMeshUpdate();if(text.isTextOverflowing)throw new InvalidOperationException("Cash text overflows: "+text.name+" / "+text.text);}
                var footer=page.transform.Find("Content/ReferenceUnavailableHint").gameObject;
                if(footer.activeSelf==page.withdrawBtn.IsInteractable())throw new InvalidOperationException("Unavailable footer contradicts Button state.");
                var buttonSprite=page.withdrawBtn.GetComponent<Image>().sprite;
                if(!page.withdrawBtn.IsInteractable()&&buttonSprite.name!="DisabledButton")throw new InvalidOperationException("Unavailable Button has incorrect artwork.");
                var fillBounds=GraphicBounds(page.progressImg);var trackBounds=GraphicBounds(page.progressImg.transform.parent.Find("bg").GetComponent<Image>());
                if(fillBounds.xMin<trackBounds.xMin||fillBounds.xMax>trackBounds.xMax||fillBounds.yMin<trackBounds.yMin||fillBounds.yMax>trackBounds.yMax)throw new InvalidOperationException("Progress fill leaves its track.");
                if(cashRun.index==1)CashScreenshot(cashFolder+"01-selected-goal.png");
                cashRun.index++;cashRun.phase=2;SaveCash();return;
            }
            if(cashRun.phase==3)
            {
                Click(FindClose(page));result.closeMethod="Close Button EventSystem click";cashRun.phase=4;cashNext=EditorApplication.timeSinceStartup+1;SaveCash();return;
            }
            if(cashRun.phase==4&&!UIModule.Instance.PageIsOpen(UIPageIds.FakeWithdrawPanel))
            {
                if(cashBalanceBefore!=ItemUtils.GetItemCount(E_ItemType.Dollar)||cashStagesBefore!=JsonUtility.ToJson(SaveDataUtils.FakeWithDrawPanelData)||cashStarterBefore!=SaveDataUtils.GameData.fakeWithdrawPanelFirstWithdraw||cashClicksBefore!=SaveDataUtils.GameData.btnWithdrawClick)throw new InvalidOperationException("Cash presentation check changed account/withdrawal state.");
                result.closed=true;result.checks.Add("Closed to gameplay; cash balance, withdrawal stages, starter claim flag and withdrawal-click count unchanged; no withdrawal submitted");cashRun.status="complete";SaveCash();
            }
        }
        catch(Exception e){cashRun.status="failed";cashRun.error=e.ToString();SaveCash();Debug.LogException(e);}
    }
    private static void CashScreenshot(string path)
    {OrchardSkinValidation.CaptureRuntimeSnapshot(Path.ChangeExtension(path,".json"),path);ScreenCapture.CaptureScreenshot(path);}
}
#endif
