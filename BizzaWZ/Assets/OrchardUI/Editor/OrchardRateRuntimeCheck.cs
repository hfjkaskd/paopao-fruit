#if BIZZA_REAL_WITHDRAW
using System;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static partial class OrchardApprovedRuntimeCheck
{
    private static Run rateRun;
    private static string rateFolder;
    private static double rateNext,rateDeadline;
    private static float rateBalance;
    private static long rateLastWithdrawal;
    private static ExchangeRateInfo RateFixture(bool second) => new ExchangeRateInfo{beforeBlance=second?1.25:10000,beforeClash=second?.03:2.5,nowBlance=second?1.25:10000,nowClash=second?.04:5};
    private static void StartRateCheck(bool shorter)
    {
        rateFolder="Design/OrchardRateRefinement-20260928/"+(shorter?"RuntimeShort/":"Runtime/");Directory.CreateDirectory(rateFolder);
        rateRun=new Run{startedUtc=DateTime.UtcNow.ToString("O"),status="running"};rateNext=0;rateDeadline=EditorApplication.timeSinceStartup+100;SaveRate();
    }
    private static void SaveRate()=>File.WriteAllText(rateFolder+"verification.json",JsonUtility.ToJson(rateRun,true));
    private static void ValidateRateAmounts(ExchangeRatePanel page,ExchangeRateInfo data)
    {
        string currency=LanguageUtils.GetText("CurrencyToken");
        if(page.beforeBlanceText.text!=WithdrawalUtil.GetCustomizedValueByCountryType((float)data.beforeBlance)||page.nowBlanceText.text!=WithdrawalUtil.GetCustomizedValueByCountryType((float)data.nowBlance)||page.beforeClashText.text!=currency+WithdrawalUtil.GetCustomizedValueByCountryType((float)data.beforeClash)||page.nowClashText.text!=currency+WithdrawalUtil.GetCustomizedValueByCountryType((float)data.nowClash))throw new InvalidOperationException("Rate data did not refresh through the production page contract.");
    }
    private static void TickRateCheck()
    {
        if(rateRun==null||rateRun.status!="running"||EditorApplication.timeSinceStartup<rateNext)return;
        rateNext=EditorApplication.timeSinceStartup+.3;
        try
        {
            if(EditorApplication.timeSinceStartup>rateDeadline)throw new TimeoutException("Rate check phase "+rateRun.phase);
            if(!EditorApplication.isPlaying||!HarvestBridge.Ready||UIModule.Instance==null||UIModule.Instance.Opening||TransparentBlock.IsBlock)return;
            var page=UIModule.Instance.GetPage(UIPageIds.ExchangeRatePanel) as ExchangeRatePanel;
            if(rateRun.phase==0)
            {
                if(UIModule.Instance.HasPopup)return;
                rateBalance=ItemUtils.GetItemCount(E_ItemType.Dollar);rateLastWithdrawal=SaveDataUtils.GameData.lastWithdrawTime;
                rateRun.pages.Add(new PageResult{page="ExchangeRatePanel",openMethod="InitWZ full initialization; production UIModule.OpenPage with temporary ExchangeRateInfo values, no account mutation"});
                UIModule.Instance.OpenPage(UIPageIds.ExchangeRatePanel,RateFixture(false)).Forget();rateRun.phase=1;rateNext=EditorApplication.timeSinceStartup+3;SaveRate();return;
            }
            var result=rateRun.pages[0];
            if(rateRun.phase==1)
            {
                if(page==null||!page.gameObject.activeInHierarchy||!page.GetComponent<OrchardRateVisual>().IsReady)return;
                ValidateRateAmounts(page,RateFixture(false));result.opened=true;
                foreach(string path in new[]{"Content (1)/CloseBtn","Content (1)/WithdrawBtn"})
                {var button=page.transform.Find(path).GetComponent<Button>();var hit=Inspect(button);result.buttons.Add(hit);if(!hit.interactable||!hit.centerHitsButton||button.targetGraphic!=button.GetComponent<Image>())throw new InvalidOperationException("Rate standard Button does not receive its visible center: "+path);}
                foreach(var text in page.GetComponentsInChildren<TMPro.TMP_Text>()){text.ForceMeshUpdate();if(text.isTextOverflowing)throw new InvalidOperationException("Rate text overflows: "+text.name);}
                var title=page.transform.Find("BG (2)/Text (TMP)").GetComponent<TMPro.TMP_Text>();
                if(title.textBounds.size.x*title.rectTransform.localScale.x>410)throw new InvalidOperationException("Localized Rate title extends beyond the wooden plaque inset.");
                foreach(string path in new[]{"BG (2)/Image (2)","Content (1)/ApprovedRate","Content (1)/BeforeState/bg","Content (1)/NowState/bg","Content (1)/ReferenceConnector","Content (1)/WithdrawBtn","Content (1)/CloseBtn"})
                {
                    var image=page.transform.Find(path).GetComponent<Image>();var bounds=GraphicBounds(image);
                    if(!image.enabled||image.sprite==null||image.type!=Image.Type.Simple||image.color!=Color.white||bounds.xMin<0||bounds.yMin<0||bounds.xMax>Screen.width||bounds.yMax>Screen.height)throw new InvalidOperationException("Rate artwork missing, tinted or clipped: "+path);
                }
                foreach(var text in new[]{page.beforeClashText,page.nowClashText})if(text.color.g<text.color.r+.2f)throw new InvalidOperationException("Rate amounts lost their green emphasis.");
                result.checks.Add("Both standard Buttons receive center hits; seven complete untinted artwork sprites fit the screen; text has no overflow; old and new amounts are green");
                result.checks.Add("Currency and all four amounts come from production formatting of temporary ExchangeRateInfo, not static preview text");
                result.screenshot=rateFolder+"00-rate-up.png";CashScreenshot(result.screenshot);rateRun.phase=2;rateNext=EditorApplication.timeSinceStartup+1;SaveRate();return;
            }
            if(rateRun.phase==2){Click(page.transform.Find("Content (1)/WithdrawBtn").GetComponent<Button>());rateRun.phase=3;rateNext=EditorApplication.timeSinceStartup+2;SaveRate();return;}
            if(rateRun.phase==3)
            {
                var withdrawal=UIModule.Instance.GetPage(UIPageIds.RealWithdrawPanel);if(withdrawal==null||!withdrawal.gameObject.activeInHierarchy)return;
                if(UIModule.Instance.PageIsOpen(UIPageIds.ExchangeRatePanel))throw new InvalidOperationException("Rate page remained open after Check.");
                result.checks.Add("Check closes the rate popup and opens the existing RealWithdrawPanel; no payment submitted");Click(FindClose(withdrawal));rateRun.phase=4;rateNext=EditorApplication.timeSinceStartup+1;SaveRate();return;
            }
            if(rateRun.phase==4){if(UIModule.Instance.HasPopup)return;UIModule.Instance.OpenPage(UIPageIds.ExchangeRatePanel,RateFixture(true)).Forget();rateRun.phase=5;rateNext=EditorApplication.timeSinceStartup+3;SaveRate();return;}
            if(rateRun.phase==5)
            {
                if(page==null||!page.gameObject.activeInHierarchy)return;ValidateRateAmounts(page,RateFixture(true));CashScreenshot(rateFolder+"01-updated-values.png");result.checks.Add("Reopen updates all four amounts with a second temporary data set");rateRun.phase=6;rateNext=EditorApplication.timeSinceStartup+1;SaveRate();return;
            }
            if(rateRun.phase==6){Click(page.transform.Find("Content (1)/CloseBtn").GetComponent<Button>());rateRun.phase=7;rateNext=EditorApplication.timeSinceStartup+1;SaveRate();return;}
            if(rateRun.phase==7&&!UIModule.Instance.PageIsOpen(UIPageIds.ExchangeRatePanel))
            {
                if(rateBalance!=ItemUtils.GetItemCount(E_ItemType.Dollar)||rateLastWithdrawal!=SaveDataUtils.GameData.lastWithdrawTime)throw new InvalidOperationException("Rate verification changed saved account state.");
                result.closed=true;result.closeMethod="EventSystem Close Button after reopening";result.checks.Add("Close works; saved balance and last withdrawal timestamp unchanged");rateRun.status="complete";SaveRate();
            }
        }
        catch(Exception e){rateRun.status="failed";rateRun.error=e.ToString();SaveRate();Debug.LogException(e);}
    }
}
#endif
