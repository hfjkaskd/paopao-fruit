#if BIZZA_REAL_WITHDRAW
using System;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

public static partial class OrchardApprovedRuntimeCheck
{
    private static Run boosterRun;
    private static string boosterFolder;
    private static double boosterNext,boosterDeadline;
    private static bool boosterOpening;
    private static float propCountBefore;
    private static int propUsesBefore;
    private static readonly E_ItemType[] BoosterTypes={E_ItemType.GameProp_1,E_ItemType.GameProp_2,E_ItemType.GameProp_3};
    private static void StartBoosterCheck(bool shorter)
    {
        boosterFolder="Design/OrchardBoosterRefinement-20260928/"+(shorter?"RuntimeShort/":"Runtime/");Directory.CreateDirectory(boosterFolder);
        boosterRun=new Run{startedUtc=DateTime.UtcNow.ToString("O"),status="running"};boosterDeadline=EditorApplication.timeSinceStartup+120;boosterNext=0;SaveBooster();
    }
    private static void SaveBooster(){File.WriteAllText(boosterFolder+"verification.json",JsonUtility.ToJson(boosterRun,true));}
    private static async UniTask OpenBooster(E_ItemType type)
    {
        boosterOpening=true;
        try{await UIModule.Instance.OpenPage<E_ItemType>(UIPageIds.AddPropPanel,type);}
        catch(Exception e){boosterRun.status="failed";boosterRun.error=e.ToString();SaveBooster();}
        finally{boosterOpening=false;}
    }
    private static void TickBoosterCheck()
    {
        if(boosterRun==null||boosterRun.status!="running"||EditorApplication.timeSinceStartup<boosterNext)return;
        boosterNext=EditorApplication.timeSinceStartup+.2;
        try
        {
            if(EditorApplication.timeSinceStartup>boosterDeadline)throw new TimeoutException("Booster runtime check timed out at phase "+boosterRun.phase);
            if(!EditorApplication.isPlaying||!HarvestBridge.Ready||UIModule.Instance==null||boosterOpening||UIModule.Instance.Opening||TransparentBlock.IsBlock)return;
            var type=BoosterTypes[boosterRun.index];var page=UIModule.Instance.GetPage(UIPageIds.AddPropPanel) as AddPropPanel;
            if(boosterRun.phase==0)
            {
                if(UIModule.Instance.HasPopup)return;
                propCountBefore=ItemUtils.GetItemCount(type);propUsesBefore=NumbericalStatistics._propUseTimes[type];
                boosterRun.pages.Add(new PageResult{page="AddPropPanel / type "+(int)type,openMethod="Production UIModule.OpenPage<E_ItemType>"});
                boosterRun.phase=1;OpenBooster(type).Forget();boosterNext=EditorApplication.timeSinceStartup+2;SaveBooster();return;
            }
            var result=boosterRun.pages[boosterRun.pages.Count-1];
            if(boosterRun.phase==1)
            {
                if(page==null||!page.gameObject.activeInHierarchy||!page.GetComponent<OrchardBoosterVisual>().IsReady)return;
                result.opened=true;result.buttons.Add(Inspect(page.closeBtn));result.buttons.Add(Inspect(page.adBuyBtn));
                foreach(var hit in result.buttons)if(!hit.interactable||!hit.centerHitsButton)throw new InvalidOperationException("Popup button blocked: "+hit.name);
                var config=PropConfigSO.Instance.GetPropConfigInfo(type);
                string expected=string.Format(LanguageUtils.SelectedLanguage=="pt-BR"?"Usado nesta fase: {0} / {1}":"Used this level: {0} / {1}",propUsesBefore,config.preLimitNum);
                if(page.limitTxt.text!=expected)throw new InvalidOperationException("Usage counter does not match production state.");
                var icon=page.propIcon.sprite;if(icon==null||(type==E_ItemType.GameProp_1?icon.name!="Undo":icon!=config.propIcon))throw new InvalidOperationException("Selected prop icon is incorrect.");
                string expectedName=LanguageUtils.SelectedLanguage=="pt-BR"?new[]{"Desfazer","Embaralhar","Varinha mágica"}[boosterRun.index]:new[]{"Undo","Shuffle","Magic Wand"}[boosterRun.index];
                if(page.propName.text!=expectedName)throw new InvalidOperationException("Selected prop name is incorrect: "+page.propName.text);
                page.propName.ForceMeshUpdate();
                if(page.propName.isTextOverflowing||page.propName.textInfo.lineCount!=1)throw new InvalidOperationException("Prop name must fit on one line without overlapping its description.");
                var description=page.transform.Find("Content/ApprovedToolName").GetComponent<TMPro.TMP_Text>();
                if(string.IsNullOrEmpty(description.text)||description.isTextOverflowing)throw new InvalidOperationException("Prop description is empty or overflows.");
                string expectedClaim=LanguageUtils.SelectedLanguage=="pt-BR"?"Assistir e ganhar 1":"Watch & Get 1";
                var claimText=page.adBuyBtn.GetComponentInChildren<TMPro.TMP_Text>();if(claimText.text!=expectedClaim||claimText.isTextOverflowing)throw new InvalidOperationException("Claim label is incorrect or overflows: "+claimText.text);
                result.checks.Add("Resources artwork loaded; selected name/icon/description and real usage count match the requested prop");
                result.screenshot=boosterFolder+boosterRun.index.ToString("00")+"-AddPropPanel.png";
                OrchardSkinValidation.CaptureRuntimeSnapshot(Path.ChangeExtension(result.screenshot,".json"),result.screenshot);ScreenCapture.CaptureScreenshot(result.screenshot);
                boosterRun.phase=2;boosterNext=EditorApplication.timeSinceStartup+.75;SaveBooster();return;
            }
            if(boosterRun.phase==2)
            {
                var hit=Inspect(page.adBuyBtn);var pointer=new PointerEventData(EventSystem.current){position=hit.center,button=PointerEventData.InputButton.Left};
                ExecuteEvents.Execute(page.adBuyBtn.gameObject,pointer,ExecuteEvents.pointerDownHandler);ExecuteEvents.Execute(page.adBuyBtn.gameObject,pointer,ExecuteEvents.pointerUpHandler);
                result.checks.Add("Claim Button receives pointer down/up; click and ad reward intentionally not invoked");
                Click(page.closeBtn);result.closeMethod="EventSystem pointer click on CloseBtn";boosterRun.phase=3;boosterNext=EditorApplication.timeSinceStartup+.75;SaveBooster();return;
            }
            if(boosterRun.phase==3&&!UIModule.Instance.PageIsOpen(UIPageIds.AddPropPanel))
            {
                if(ItemUtils.GetItemCount(type)!=propCountBefore||NumbericalStatistics._propUseTimes[type]!=propUsesBefore)throw new InvalidOperationException("Prop quantity/use count changed during presentation check.");
                result.closed=true;result.checks.Add("Close returned to gameplay; prop quantity and usage count unchanged");boosterRun.index++;boosterRun.phase=0;
                if(boosterRun.index==BoosterTypes.Length)boosterRun.status="complete";SaveBooster();
            }
        }
        catch(Exception e){boosterRun.status="failed";boosterRun.error=e.ToString();SaveBooster();Debug.LogException(e);}
    }
}
#endif
