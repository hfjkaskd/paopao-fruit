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
    private static Run serviceRun;
    private static string serviceFolder;
    private static double serviceNext,serviceDeadline;
    private static int serviceSavedCount;
    private static readonly List<ChatElement> serviceFixtures=new List<ChatElement>();
    private static readonly List<GameObject> serviceOriginals=new List<GameObject>();
    private static Vector2 serviceMin,serviceMax,serviceAnchorMin,serviceAnchorMax;
    private static readonly string[] ServiceCopy={"Olá! Como podemos ajudar?","Quero acompanhar meu saque.","Abra o Histórico para consultar o status.","Se precisar, escolha uma pergunta abaixo."};
    private static void StartServiceCheck(bool shorter)
    {
        serviceFolder="Design/OrchardServiceRefinement-20260928/"+(shorter?"RuntimeShort/":"Runtime/");Directory.CreateDirectory(serviceFolder);
        serviceRun=new Run{startedUtc=DateTime.UtcNow.ToString("O"),status="running"};serviceNext=0;serviceDeadline=EditorApplication.timeSinceStartup+140;SaveService();
    }
    private static void SaveService()=>File.WriteAllText(serviceFolder+"verification.json",JsonUtility.ToJson(serviceRun,true));
    private static void ServiceHit(Button button,PageResult result)
    {var hit=Inspect(button);result.buttons.Add(hit);if(!hit.interactable||!hit.centerHitsButton)throw new InvalidOperationException("Service Button blocked: "+button.name);}
    private static void ServiceFixture(ServicePanel page,int index,bool longMessage=false)
    {
        var row=UnityEngine.Object.Instantiate(page.chatElementPrefab,page.contentRoot,false);row.name="DisposableServiceFixture"+index;row.gameObject.SetActive(true);
        row.Init(new ChatInfo{time="",spokesperson=index%4==1?Spokesperson.Player:Spokesperson.Issue,chatcontent=longMessage?"Abra o Histórico para consultar o status. Confira os dados da sua conta antes de continuar. Se precisar, escolha uma pergunta abaixo.":ServiceCopy[index%4]});serviceFixtures.Add(row);
    }
    private static void ServiceRestore(ServicePanel page)
    {
        foreach(var row in serviceFixtures)if(row!=null){row.gameObject.SetActive(false);UnityEngine.Object.Destroy(row.gameObject);}serviceFixtures.Clear();
        foreach(var row in serviceOriginals)if(row!=null)row.SetActive(true);serviceOriginals.Clear();
        if(page!=null){page.viewportResizer.UpdateViewportBottom(0);page.inputText.Text="";}
    }
    private static void ServiceCheckVisible(ServicePanel page,ChatElement row)
    {
        var viewport=GraphicBounds(page.scrollRect.viewport.GetComponent<Image>());
        var bubble=row.transform.Find(row.chatInfo.spokesperson==Spokesperson.Issue?"ChatInfo/Issue_bg":"ChatInfo/Player_bg").GetComponent<Image>();var bounds=GraphicBounds(bubble);
        if(bounds.yMin<viewport.yMin-2||bounds.yMax>viewport.yMax+2||bounds.xMin<viewport.xMin-2||bounds.xMax>viewport.xMax+2)throw new InvalidOperationException("Chat bubble clipped by viewport.");
        row.chatTxt.ForceMeshUpdate();if(row.chatTxt.isTextOverflowing)throw new InvalidOperationException("Chat text overflow.");
        var text=GraphicBounds(row.chatTxt);if(text.yMin<bounds.yMin||text.yMax>bounds.yMax)throw new InvalidOperationException("Chat text outside its bubble.");
    }
    private static void TickServiceCheck()
    {
        if(serviceRun==null||serviceRun.status!="running"||EditorApplication.timeSinceStartup<serviceNext)return;serviceNext=EditorApplication.timeSinceStartup+.3;
        var page=UIModule.Instance==null?null:UIModule.Instance.GetPage(UIPageIds.ServicePanel) as ServicePanel;
        try
        {
            if(EditorApplication.timeSinceStartup>serviceDeadline)throw new TimeoutException("Service phase "+serviceRun.phase);
            if(!EditorApplication.isPlaying||!HarvestBridge.Ready||UIModule.Instance==null||UIModule.Instance.Opening||TransparentBlock.IsBlock)return;
            if(serviceRun.phase==0)
            {
                if(UIModule.Instance.HasPopup)return;serviceSavedCount=SaveDataUtils.GameData.chatInfos.Count;
                serviceRun.pages.Add(new PageResult{page="ServicePanel",openMethod="InitWZ full initialization, production support/history read; temporary real ChatElement prefabs for layout, never sent or saved"});
                Open("ServicePanel").Forget();serviceRun.phase=1;serviceNext=EditorApplication.timeSinceStartup+5;SaveService();return;
            }
            var result=serviceRun.pages[0];
            if(serviceRun.phase==1)
            {
                if(page==null||!page.gameObject.activeInHierarchy||!page.GetComponent<OrchardServiceVisual>().IsReady)return;result.opened=true;CashScreenshot(serviceFolder+"00-live-service.png");
                serviceFixtures.Clear();serviceOriginals.Clear();foreach(Transform child in page.contentRoot)if(child.gameObject.activeSelf){serviceOriginals.Add(child.gameObject);child.gameObject.SetActive(false);}
                for(int i=0;i<4;i++)ServiceFixture(page,i);Canvas.ForceUpdateCanvases();page.scrollRect.StopMovement();page.scrollRect.verticalNormalizedPosition=1;
                serviceRun.phase=2;serviceNext=EditorApplication.timeSinceStartup+1;SaveService();return;
            }
            if(serviceRun.phase==2)
            {
                foreach(var row in serviceFixtures)if(!row.GetComponent<OrchardServiceVisual>().IsReady)return;
                foreach(var row in serviceFixtures)ServiceCheckVisible(page,row);
                if(serviceFixtures[1].chatTxt.textInfo.lineCount!=1)throw new InvalidOperationException("Reference player sentence should fit on one line.");
                if(!page.inputText.gameObject.activeInHierarchy||!page.selectQuestionButton.gameObject.activeInHierarchy||page.transform.Find("Content/InputNode/ClearBtn").gameObject.activeSelf)throw new InvalidOperationException("Input/picker initial state does not match the prefab design.");
                foreach(string path in new[]{"Title/CloseBtn","Title/HistoryBtn","Title/FQA","Content/InputNode/SelectQuestionBtn ","Content/InputNode/NotCanSendBtn"})ServiceHit(page.transform.Find(path).GetComponent<Button>(),result);
                result.checks.Add("Four real chat prefabs fit completely inside viewport; player sentence stays one line; final bubble bottom and all text remain visible; five initial Buttons receive hits");
                result.screenshot=serviceFolder+"01-service.png";CashScreenshot(result.screenshot);serviceRun.phase=3;serviceNext=EditorApplication.timeSinceStartup+1;SaveService();return;
            }
            if(serviceRun.phase==3){Click(page.selectQuestionButton);serviceRun.phase=4;serviceNext=EditorApplication.timeSinceStartup+1;SaveService();return;}
            if(serviceRun.phase==4)
            {
                var topics=UIModule.Instance.GetPage(UIPageIds.ServiceSelectPanel) as ServiceSelectPanel;if(topics==null||!topics.gameObject.activeInHierarchy)return;
                Click(topics.defaultButtons[0]);serviceRun.phase=5;serviceNext=EditorApplication.timeSinceStartup+1;SaveService();return;
            }
            if(serviceRun.phase==5)
            {
                if(!page.CanSend||string.IsNullOrEmpty(page.inputText.Text))throw new InvalidOperationException("Quick question did not fill the visible input.");
                ServiceHit(page.transform.Find("Content/InputNode/CanSendBtn").GetComponent<Button>(),result);ServiceHit(page.transform.Find("Content/InputNode/ClearBtn").GetComponent<Button>(),result);
                Click(page.transform.Find("Content/InputNode/ClearBtn").GetComponent<Button>());serviceRun.phase=6;serviceNext=EditorApplication.timeSinceStartup+1;SaveService();return;
            }
            if(serviceRun.phase==6)
            {
                if(page.CanSend||!page.inputText.gameObject.activeInHierarchy||!page.selectQuestionButton.gameObject.activeInHierarchy)throw new InvalidOperationException("Clear did not restore editable input and question picker.");
                result.checks.Add("Quick questions opens the existing picker, selected question fills input, enabled Send and Clear are reachable; Clear restores the empty input; Send never clicked");
                for(int i=4;i<14;i++)ServiceFixture(page,i,true);Canvas.ForceUpdateCanvases();page.scrollRect.StopMovement();page.scrollRect.verticalNormalizedPosition=0;
                serviceRun.phase=7;serviceNext=EditorApplication.timeSinceStartup+1;SaveService();return;
            }
            if(serviceRun.phase==7)
            {
                ServiceCheckVisible(page,serviceFixtures[serviceFixtures.Count-1]);CashScreenshot(serviceFolder+"02-long-scroll.png");
                serviceRun.phase=70;serviceNext=EditorApplication.timeSinceStartup+1;SaveService();return;
            }
            if(serviceRun.phase==70)
            {
                var v=page.scrollRect.viewport;serviceMin=v.offsetMin;serviceMax=v.offsetMax;serviceAnchorMin=v.anchorMin;serviceAnchorMax=v.anchorMax;
                page.viewportResizer.UpdateViewportBottom(Screen.height*.7f);Canvas.ForceUpdateCanvases();page.scrollRect.verticalNormalizedPosition=0;
                serviceRun.phase=8;serviceNext=EditorApplication.timeSinceStartup+1;SaveService();return;
            }
            if(serviceRun.phase==8)
            {
                var v=page.scrollRect.viewport;if(v.anchorMin!=serviceAnchorMin||v.anchorMax!=serviceAnchorMax||v.offsetMax!=serviceMax)throw new InvalidOperationException("Keyboard moved authored viewport anchors/top edge.");
                page.viewportResizer.UpdateViewportBottom(0);if((v.offsetMin-serviceMin).sqrMagnitude>.01f||(v.offsetMax-serviceMax).sqrMagnitude>.01f)throw new InvalidOperationException("Keyboard close did not restore authored viewport.");
                result.checks.Add("Fourteen long messages scroll to a complete last bubble; keyboard-height callback preserves anchors and top edge, zero height restores both original offsets");
                Click(page.transform.Find("Title/FQA").GetComponent<Button>());serviceRun.phase=9;serviceNext=EditorApplication.timeSinceStartup+1;SaveService();return;
            }
            if(serviceRun.phase==9){var faq=UIModule.Instance.GetPage(UIPageIds.QFA);if(faq==null||!faq.gameObject.activeInHierarchy)return;Click(FindClose(faq));serviceRun.phase=10;serviceNext=EditorApplication.timeSinceStartup+1;SaveService();return;}
            if(serviceRun.phase==10){if(UIModule.Instance.PageIsOpen(UIPageIds.QFA))return;Click(page.transform.Find("Title/HistoryBtn").GetComponent<Button>());serviceRun.phase=11;serviceNext=EditorApplication.timeSinceStartup+1;SaveService();return;}
            if(serviceRun.phase==11){var history=UIModule.Instance.GetPage(UIPageIds.WithdrawHistory);if(history==null||!history.gameObject.activeInHierarchy)return;Click(FindClose(history));serviceRun.phase=12;serviceNext=EditorApplication.timeSinceStartup+1;SaveService();return;}
            if(serviceRun.phase==12){if(UIModule.Instance.PageIsOpen(UIPageIds.WithdrawHistory))return;ServiceRestore(page);Click(FindClose(page));serviceRun.phase=13;serviceNext=EditorApplication.timeSinceStartup+1;SaveService();return;}
            if(serviceRun.phase==13&&!UIModule.Instance.PageIsOpen(UIPageIds.ServicePanel))
            {
                if(SaveDataUtils.GameData.chatInfos.Count!=serviceSavedCount)throw new InvalidOperationException("Verification changed saved chat history.");
                result.closed=true;result.closeMethod="EventSystem Back Button";result.checks.Add("FAQ and History navigation return correctly; temporary rows removed and original rows restored; saved chat count unchanged, no message sent");serviceRun.status="complete";SaveService();
            }
        }
        catch(Exception e){ServiceRestore(page);serviceRun.status="failed";serviceRun.error=e.ToString();SaveService();Debug.LogException(e);}
    }
}
#endif
