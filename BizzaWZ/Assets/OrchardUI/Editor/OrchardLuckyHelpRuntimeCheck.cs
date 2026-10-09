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
    private static Run luckyHelpRun;
    private static string luckyHelpFolder;
    private static double luckyHelpNext,luckyHelpDeadline;
    private static int luckyHelpProgress;
    private static float luckyHelpBalance;
    private static void StartLuckyHelpCheck(bool shorter)
    {
        luckyHelpFolder="Design/OrchardLuckyHelpRefinement-20260928/"+(shorter?"RuntimeShort/":"Runtime/");Directory.CreateDirectory(luckyHelpFolder);
        luckyHelpRun=new Run{startedUtc=DateTime.UtcNow.ToString("O"),status="running"};luckyHelpNext=0;luckyHelpDeadline=EditorApplication.timeSinceStartup+90;SaveLuckyHelp();
    }
    private static void SaveLuckyHelp()=>File.WriteAllText(luckyHelpFolder+"verification.json",JsonUtility.ToJson(luckyHelpRun,true));
    private static void HelpHit(Button button)
    {
        var hit=Inspect(button);luckyHelpRun.pages[0].buttons.Add(hit);if(!hit.interactable||!hit.centerHitsButton||button.targetGraphic!=button.GetComponent<Image>())throw new InvalidOperationException("Help Button is blocked or has a detached visual: "+button.name);
    }
    private static void TickLuckyHelpCheck()
    {
        if(luckyHelpRun==null||luckyHelpRun.status!="running"||EditorApplication.timeSinceStartup<luckyHelpNext)return;luckyHelpNext=EditorApplication.timeSinceStartup+.3;
        try
        {
            if(EditorApplication.timeSinceStartup>luckyHelpDeadline)throw new TimeoutException("Help phase "+luckyHelpRun.phase);
            if(!EditorApplication.isPlaying||!HarvestBridge.Ready||UIModule.Instance==null||UIModule.Instance.Opening||TransparentBlock.IsBlock)return;
            var spin=UIModule.Instance.GetPage(UIPageIds.SlotPanel) as SlotPanel;var page=UIModule.Instance.GetPage(UIPageIds.SlotFAQPanel) as SlotFAQPanel;
            if(luckyHelpRun.phase==0)
            {
                if(UIModule.Instance.HasPopup)return;luckyHelpProgress=SaveDataUtils.GameData.playerPassLv;luckyHelpBalance=ItemUtils.GetItemCount(E_ItemType.Dollar);
                luckyHelpRun.pages.Add(new PageResult{page="SlotFAQPanel",openMethod="InitWZ full initialization, open existing SlotPanel then EventSystem FAQ Button"});UIModule.Instance.OpenPage(UIPageIds.SlotPanel).Forget();luckyHelpRun.phase=1;luckyHelpNext=EditorApplication.timeSinceStartup+2;SaveLuckyHelp();return;
            }
            var result=luckyHelpRun.pages[0];
            if(luckyHelpRun.phase==1){if(spin==null||!spin.gameObject.activeInHierarchy)return;HelpHit(spin.faqBtn);Click(spin.faqBtn);luckyHelpRun.phase=2;luckyHelpNext=EditorApplication.timeSinceStartup+2;SaveLuckyHelp();return;}
            if(luckyHelpRun.phase==2)
            {
                if(page==null||!page.gameObject.activeInHierarchy||!page.GetComponent<OrchardLuckyHelpVisual>().IsReady)return;
                var maskBounds=GraphicBounds(page.transform.Find("PageMaskReference").GetComponent<Image>());
                if(maskBounds.xMin>1||maskBounds.yMin>1||maskBounds.xMax<Screen.width-1||maskBounds.yMax<Screen.height-1)throw new InvalidOperationException("Help background dim does not cover the full screen.");
                var rows=page.transform.Find("Content/bg2 (1)/root");if(rows.childCount!=6)throw new InvalidOperationException("Help reward rows changed.");
                int tiles=0;
                foreach(Transform row in rows)
                {
                    var card=row.GetComponent<Image>();if(card.type!=Image.Type.Simple||card.color!=Color.white||card.sprite==null||card.sprite.name!="Row")throw new InvalidOperationException("Help row is tinted, sliced or missing.");
                    for(int i=0;i<3;i++)
                    {
                        var tile=row.Find("Slot_1"+(i==0?"":" ("+i+")")).GetComponent<Image>();var symbol=tile.transform.Find("Content").GetComponent<Image>();var bounds=GraphicBounds(tile);
                        if(tile.sprite==null||tile.sprite.name!="Tile"||tile.type!=Image.Type.Simple||tile.color!=Color.white||symbol.color!=Color.white||!symbol.preserveAspect||bounds.xMin<0||bounds.yMin<0||bounds.xMax>Screen.width||bounds.yMax>Screen.height)throw new InvalidOperationException("Help tile/symbol is clipped or washed out.");tiles++;
                    }
                }
                foreach(var text in page.GetComponentsInChildren<TMP_Text>()){text.ForceMeshUpdate();if(text.isTextOverflowing)throw new InvalidOperationException("Help text overflows: "+text.name);}
                if(page.transform.Find("Content/des").gameObject.activeSelf||page.transform.Find("Content/ReferenceFive").GetComponent<TMP_Text>().text!="5/5")throw new InvalidOperationException("Guide illustration/copy did not replace the old paragraph.");
                HelpHit(page.bizzaButton);HelpHit(page.transform.Find("Content/ReferenceClose").GetComponent<Button>());
                result.opened=true;result.screenshot=luckyHelpFolder+"00-lucky-help.png";CashScreenshot(result.screenshot);result.checks.Add("Six complete warm row sprites and 18 full-color symbol tiles fit screen without sliced borders or faded alpha; localized text has no overflow; two illustrated instructions replace the old paragraph; background dim covers the full screen");luckyHelpRun.phase=3;luckyHelpNext=EditorApplication.timeSinceStartup+1;SaveLuckyHelp();return;
            }
            if(luckyHelpRun.phase==3){Click(page.bizzaButton);luckyHelpRun.phase=4;luckyHelpNext=EditorApplication.timeSinceStartup+1;SaveLuckyHelp();return;}
            if(luckyHelpRun.phase==4){if(UIModule.Instance.PageIsOpen(UIPageIds.SlotFAQPanel))return;if(spin==null||!spin.gameObject.activeInHierarchy)throw new InvalidOperationException("Got it lost the underlying spin page.");Click(spin.faqBtn);luckyHelpRun.phase=5;luckyHelpNext=EditorApplication.timeSinceStartup+2;SaveLuckyHelp();return;}
            if(luckyHelpRun.phase==5){if(page==null||!page.gameObject.activeInHierarchy)return;Click(page.transform.Find("Content/ReferenceClose").GetComponent<Button>());luckyHelpRun.phase=6;luckyHelpNext=EditorApplication.timeSinceStartup+1;SaveLuckyHelp();return;}
            if(luckyHelpRun.phase==6)
            {
                if(UIModule.Instance.PageIsOpen(UIPageIds.SlotFAQPanel))return;if(spin==null||!spin.gameObject.activeInHierarchy)throw new InvalidOperationException("Close lost the spin page.");Click(spin.closeBtn);luckyHelpRun.phase=7;luckyHelpNext=EditorApplication.timeSinceStartup+1;SaveLuckyHelp();return;
            }
            if(luckyHelpRun.phase==7&&!UIModule.Instance.PageIsOpen(UIPageIds.SlotPanel))
            {
                if(luckyHelpProgress!=SaveDataUtils.GameData.playerPassLv||luckyHelpBalance!=ItemUtils.GetItemCount(E_ItemType.Dollar))throw new InvalidOperationException("Help navigation changed account state.");
                result.closed=true;result.closeMethod="Got it, reopen through FAQ, new X, then parent Back";result.checks.Add("FAQ opener, Got it and X receive hits; both help dismissals reveal existing SlotPanel; progress and balance unchanged; no spin or ad invoked");luckyHelpRun.status="complete";SaveLuckyHelp();
            }
        }
        catch(Exception e){luckyHelpRun.status="failed";luckyHelpRun.error=e.ToString();SaveLuckyHelp();Debug.LogException(e);}
    }
}
#endif
