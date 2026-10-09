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
    private static Run spinRun;
    private static string spinFolder,spinOriginalLabel,spinFormat;
    private static double spinNext,spinDeadline;
    private static int spinSavedProgress;
    private static float spinSavedBalance;
    private static void StartSpinCheck(bool shorter)
    {
        spinFolder="Design/OrchardLuckySpinRefinement-20260928/"+(shorter?"RuntimeShort/":"Runtime/");Directory.CreateDirectory(spinFolder);
        spinRun=new Run{startedUtc=DateTime.UtcNow.ToString("O"),status="running"};spinNext=0;spinDeadline=EditorApplication.timeSinceStartup+85;SaveSpin();
    }
    private static void SaveSpin()=>File.WriteAllText(spinFolder+"verification.json",JsonUtility.ToJson(spinRun,true));
    private static void AssertSpinCenter(SlotPanel page)
    {
        foreach(string path in new[]{"ApprovedTitle","Content/ApprovedFreeCount"})
        {
            var text=page.transform.Find(path).GetComponent<TMP_Text>();text.ForceMeshUpdate();
            var center=page.transform.InverseTransformPoint(text.rectTransform.TransformPoint(text.rectTransform.rect.center));
            if(Mathf.Abs(center.x)>1||Mathf.Abs(text.textBounds.center.x)>3||text.isTextOverflowing||text.alignment!=TextAlignmentOptions.Center)throw new InvalidOperationException("Spin text is not centered or overflows: "+path+" center="+center.x+" glyph="+text.textBounds.center.x);
        }
        if(page.transform.Find("Content/ApprovedFreeCaption").gameObject.activeSelf)throw new InvalidOperationException("Separate free-spin caption remained visible.");
    }
    private static void TickSpinCheck()
    {
        if(spinRun==null||spinRun.status!="running"||EditorApplication.timeSinceStartup<spinNext)return;spinNext=EditorApplication.timeSinceStartup+.3;
        try
        {
            if(EditorApplication.timeSinceStartup>spinDeadline)throw new TimeoutException("Spin check phase "+spinRun.phase);
            if(!EditorApplication.isPlaying||!HarvestBridge.Ready||UIModule.Instance==null||UIModule.Instance.Opening||TransparentBlock.IsBlock)return;
            var page=UIModule.Instance.GetPage(UIPageIds.SlotPanel) as SlotPanel;
            if(spinRun.phase==0)
            {
                if(UIModule.Instance.HasPopup)return;spinSavedProgress=SaveDataUtils.GameData.playerPassLv;spinSavedBalance=ItemUtils.GetItemCount(E_ItemType.Dollar);
                spinRun.pages.Add(new PageResult{page="SlotPanel",openMethod="InitWZ full initialization and production UIModule.OpenPage; actual free-spin availability"});UIModule.Instance.OpenPage(UIPageIds.SlotPanel).Forget();spinRun.phase=1;spinNext=EditorApplication.timeSinceStartup+3;SaveSpin();return;
            }
            var result=spinRun.pages[0];
            if(spinRun.phase==1)
            {
                if(page==null||!page.gameObject.activeInHierarchy)return;
                AssertSpinCenter(page);var text=page.transform.Find("Content/ApprovedFreeCount").GetComponent<TMP_Text>();spinOriginalLabel=text.text;
                var so=new SerializedObject(page);spinFormat=so.FindProperty(LanguageUtils.SelectedLanguage=="pt-BR"?"freeSpinPortuguese":"freeSpinEnglish").stringValue;
                if(text.text!=string.Format(spinFormat,SlotProgressUtil.CanFreeSpin?1:0))throw new InvalidOperationException("Live free-spin count does not match saved progress.");
                var back=page.closeBtn.GetComponent<Image>();var bounds=GraphicBounds(back);var hit=Inspect(page.closeBtn);result.buttons.Add(hit);
                if(!hit.interactable||!hit.centerHitsButton||page.closeBtn.targetGraphic!=back||back.sprite==null||AssetDatabase.GetAssetPath(back.sprite)!="Assets/OrchardUI/Art/SpinReferenceBack.png"||!back.preserveAspect||back.color!=Color.white||bounds.xMin<0||bounds.yMin<0||bounds.xMax>Screen.width||bounds.yMax>Screen.height)throw new InvalidOperationException("Blue arrow Back Button is clipped, not bound or blocked.");
                result.opened=true;result.screenshot=spinFolder+"00-lucky-spin.png";CashScreenshot(result.screenshot);result.checks.Add("Title and unified free-spin label are horizontally centered with no overflow; actual count matches existing progress; blue white-arrow Back is a complete standard Button");spinRun.phase=2;spinNext=EditorApplication.timeSinceStartup+1;SaveSpin();return;
            }
            if(spinRun.phase==2){page.transform.Find("Content/ApprovedFreeCount").GetComponent<TMP_Text>().text=string.Format(spinFormat,1);spinRun.phase=3;spinNext=EditorApplication.timeSinceStartup+.5;SaveSpin();return;}
            if(spinRun.phase==3){AssertSpinCenter(page);CashScreenshot(spinFolder+"01-label-one.png");spinRun.phase=4;spinNext=EditorApplication.timeSinceStartup+1;SaveSpin();return;}
            if(spinRun.phase==4){page.transform.Find("Content/ApprovedFreeCount").GetComponent<TMP_Text>().text=string.Format(spinFormat,0);spinRun.phase=5;spinNext=EditorApplication.timeSinceStartup+.5;SaveSpin();return;}
            if(spinRun.phase==5){AssertSpinCenter(page);CashScreenshot(spinFolder+"02-label-zero.png");result.checks.Add("Temporary text-only fixtures for 0 and 1 remain centered; no progress, reward, ad or reel state is changed");spinRun.phase=6;spinNext=EditorApplication.timeSinceStartup+1;SaveSpin();return;}
            if(spinRun.phase==6){page.transform.Find("Content/ApprovedFreeCount").GetComponent<TMP_Text>().text=spinOriginalLabel;Click(page.closeBtn);spinRun.phase=7;spinNext=EditorApplication.timeSinceStartup+1;SaveSpin();return;}
            if(spinRun.phase==7){if(UIModule.Instance.PageIsOpen(UIPageIds.SlotPanel))return;UIModule.Instance.OpenPage(UIPageIds.SlotPanel).Forget();spinRun.phase=8;spinNext=EditorApplication.timeSinceStartup+2;SaveSpin();return;}
            if(spinRun.phase==8)
            {
                if(page==null||!page.gameObject.activeInHierarchy)return;AssertSpinCenter(page);if(page.transform.Find("Content/ApprovedFreeCount").GetComponent<TMP_Text>().text!=spinOriginalLabel)throw new InvalidOperationException("Reopen lost live free-spin count.");Click(page.closeBtn);spinRun.phase=9;spinNext=EditorApplication.timeSinceStartup+1;SaveSpin();return;
            }
            if(spinRun.phase==9&&!UIModule.Instance.PageIsOpen(UIPageIds.SlotPanel))
            {
                if(spinSavedProgress!=SaveDataUtils.GameData.playerPassLv||spinSavedBalance!=ItemUtils.GetItemCount(E_ItemType.Dollar))throw new InvalidOperationException("Spin review changed account state.");
                result.closed=true;result.closeMethod="EventSystem Back Button twice, with reopening between";result.checks.Add("Back returns to gameplay; reopening restores the actual label; saved progress and balance unchanged");spinRun.status="complete";SaveSpin();
            }
        }
        catch(Exception e){spinRun.status="failed";spinRun.error=e.ToString();SaveSpin();Debug.LogException(e);}
    }
}
#endif
