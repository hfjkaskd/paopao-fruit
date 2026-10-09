#if BIZZA_REAL_WITHDRAW
using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static partial class OrchardApprovedRuntimeCheck
{
    private static Run confirmRun;
    private static string confirmFolder;
    private static double confirmNext,confirmDeadline;
    private static long confirmLastWithdrawal;
    private static float confirmBalance;
    private static string[] confirmOriginalFields;
    private static void StartConfirmCheck(bool shorter)
    {
        confirmFolder="Design/OrchardConfirmRefinement-20260928/"+(shorter?"RuntimeShort/":"Runtime/");Directory.CreateDirectory(confirmFolder);
        confirmRun=new Run{startedUtc=DateTime.UtcNow.ToString("O"),status="running"};confirmNext=0;confirmDeadline=EditorApplication.timeSinceStartup+100;SaveConfirm();
    }
    private static void SaveConfirm(){File.WriteAllText(confirmFolder+"verification.json",JsonUtility.ToJson(confirmRun,true));}
    private static async UniTask OpenConfirmForm()
    {
        try
        {
            await UIModule.Instance.OpenPage<AccountModule.OceanShineWithdrawalPageResponse.WithdrawalPlatform,List<AccountModule.OceanShineWithdrawalPageResponse.WithdrawalPlatform>,E_WithdrawType,Action,bool>(UIPageIds.UIWithdrawalPanel,paymentPlatforms[0],new List<AccountModule.OceanShineWithdrawalPageResponse.WithdrawalPlatform>(paymentPlatforms),E_WithdrawType.Real,null,false);
        }
        catch(Exception e){confirmRun.status="failed";confirmRun.error=e.ToString();SaveConfirm();}
    }
    private static async UniTask OpenConfirmFixture()
    {
        try
        {
            // Disposable payee strings exercise the existing page contract, without saving form values or submitting a payment.
            var info=new WithDrawInfo(null,E_WithdrawType.Real,E_PayeeAccountType.Pagbank,PayeeAccountType.CpfCnpj,"***.***.***-**","Jogador","jogador@example.com","",paymentPlatforms[0]);
            await UIModule.Instance.OpenPage(UIPageIds.UIWithdrawalConfirmPanel,info);
        }
        catch(Exception e){confirmRun.status="failed";confirmRun.error=e.ToString();SaveConfirm();}
    }
    private static void TickConfirmCheck()
    {
        if(confirmRun==null||confirmRun.status!="running"||EditorApplication.timeSinceStartup<confirmNext)return;
        confirmNext=EditorApplication.timeSinceStartup+.3;
        try
        {
            if(EditorApplication.timeSinceStartup>confirmDeadline)throw new TimeoutException("Confirmation check phase "+confirmRun.phase);
            if(!EditorApplication.isPlaying||!HarvestBridge.Ready||UIModule.Instance==null||UIModule.Instance.Opening||TransparentBlock.IsBlock)return;
            var confirm=UIModule.Instance.GetPage(UIPageIds.UIWithdrawalConfirmPanel) as UIWithdrawalConfirmPanel;
            var form=UIModule.Instance.GetPage(UIPageIds.UIWithdrawalPanel) as UIWithdrawalPanel;
            if(confirmRun.phase==0)
            {
                if(UIModule.Instance.HasPopup)return;
                confirmLastWithdrawal=SaveDataUtils.GameData.lastWithdrawTime;confirmBalance=ItemUtils.GetItemCount(E_ItemType.Dollar);
                confirmRun.pages.Add(new PageResult{page="UIWithdrawalConfirmPanel",openMethod="InitWZ full startup, HUD coin Button, live server platform, production account page, then production confirmation contract with disposable design payee strings; actual account amount retained"});
                Click(UnityEngine.Object.FindObjectOfType<CurrencyBar>().coinBtn);confirmRun.phase=1;confirmNext=EditorApplication.timeSinceStartup+2;SaveConfirm();return;
            }
            var result=confirmRun.pages[0];
            if(confirmRun.phase==1)
            {
                var page=UIModule.Instance.GetPage(UIPageIds.RealWithdrawPanel) as RealWithdrawPanel;
                if(page==null||!page.gameObject.activeInHierarchy)return;
                paymentPlatforms.Clear();foreach(var way in page.withdrawWayRoot.GetComponentsInChildren<WithdrawWay>())if(way.data!=null)paymentPlatforms.Add(way.data);
                if(paymentPlatforms.Count==0)return;
                Click(FindClose(page));confirmRun.phase=2;confirmNext=EditorApplication.timeSinceStartup+1;SaveConfirm();return;
            }
            if(confirmRun.phase==2)
            {if(UIModule.Instance.HasPopup)return;OpenConfirmForm().Forget();confirmRun.phase=3;confirmNext=EditorApplication.timeSinceStartup+2;SaveConfirm();return;}
            if(confirmRun.phase==3)
            {
                if(form==null||!form.gameObject.activeInHierarchy||!form.GetComponent<OrchardAccountVisual>().IsReady)return;
                confirmOriginalFields=new[]{form.accountNameInput.Text,form.CPFNumberInput.Text,form.paypalMailInput.Text};
                OpenConfirmFixture().Forget();confirmRun.phase=4;confirmNext=EditorApplication.timeSinceStartup+2;SaveConfirm();return;
            }
            if(confirmRun.phase==4)
            {
                if(confirm==null||!confirm.gameObject.activeInHierarchy||!confirm.GetComponent<OrchardConfirmVisual>().IsReady)return;
                result.opened=true;
                foreach(var path in new[]{"comfirmClose","ApprovedHelp","BtnOk","ApprovedEdit"})
                {var button=confirm.transform.Find(path).GetComponent<Button>();var hit=Inspect(button);result.buttons.Add(hit);if(!hit.interactable||!hit.centerHitsButton)throw new InvalidOperationException("Confirmation Button blocked: "+hit.name);}
                foreach(var text in confirm.GetComponentsInChildren<TMPro.TMP_Text>())
                {text.ForceMeshUpdate();if(text.isTextOverflowing)throw new InvalidOperationException("Confirmation text overflows: "+text.name);}
                if(!confirm.paymentImage.preserveAspect)throw new InvalidOperationException("Payment logo can stretch.");
                foreach(var path in new[]{"ApprovedMethodPanel","txtContext/Name/ApprovedRow","txtContext/CPF/ApprovedRow","txtContext/Account/ApprovedRow","ReferenceInfoCard","BtnOk","ApprovedEdit"})
                {
                    var image=confirm.transform.Find(path).GetComponent<Image>();var bounds=GraphicBounds(image);
                    if(image.type!=Image.Type.Simple||image.sprite==null||bounds.xMin<0||bounds.yMin<0||bounds.xMax>Screen.width||bounds.yMax>Screen.height)throw new InvalidOperationException("Confirmation artwork stretched, missing or offscreen: "+path);
                }
                result.checks.Add("All four standard Buttons receive center hits; title, amount, labels and payee values fit; all new card, hint and button artwork is visible and on screen; logo preserves aspect");
                var submit=confirm.transform.Find("BtnOk").GetComponent<Button>();var submitHit=Inspect(submit);var pointer=new PointerEventData(EventSystem.current){position=submitHit.center,button=PointerEventData.InputButton.Left};
                ExecuteEvents.Execute(submit.gameObject,pointer,ExecuteEvents.pointerDownHandler);ExecuteEvents.Execute(submit.gameObject,pointer,ExecuteEvents.pointerUpHandler);
                result.checks.Add("Confirm Button pointer down/up state checked without dispatching its click/submit handler");
                result.screenshot=confirmFolder+"00-confirm.png";CashScreenshot(result.screenshot);
                confirmRun.phase=5;confirmNext=EditorApplication.timeSinceStartup+1;SaveConfirm();return;
            }
            if(confirmRun.phase==5)
            {Click(confirm.transform.Find("ApprovedEdit").GetComponent<Button>());confirmRun.phase=6;confirmNext=EditorApplication.timeSinceStartup+1;SaveConfirm();return;}
            if(confirmRun.phase==6)
            {
                if(UIModule.Instance.PageIsOpen(UIPageIds.UIWithdrawalConfirmPanel))return;
                if(form==null||!form.gameObject.activeInHierarchy||form.accountNameInput.Text!=confirmOriginalFields[0]||form.CPFNumberInput.Text!=confirmOriginalFields[1]||form.paypalMailInput.Text!=confirmOriginalFields[2])throw new InvalidOperationException("Edit did not return to the unchanged form.");
                result.checks.Add("Edit Button returned to the existing account form with name, document and email unchanged");
                OpenConfirmFixture().Forget();confirmRun.phase=7;confirmNext=EditorApplication.timeSinceStartup+2;SaveConfirm();return;
            }
            if(confirmRun.phase==7)
            {if(confirm==null||!confirm.gameObject.activeInHierarchy)return;Click(confirm.transform.Find("ApprovedHelp").GetComponent<Button>());confirmRun.phase=8;confirmNext=EditorApplication.timeSinceStartup+1;SaveConfirm();return;}
            if(confirmRun.phase==8)
            {var faq=UIModule.Instance.GetPage(UIPageIds.QFA);if(faq==null||!faq.gameObject.activeInHierarchy)return;Click(FindClose(faq));confirmRun.phase=9;confirmNext=EditorApplication.timeSinceStartup+1;SaveConfirm();return;}
            if(confirmRun.phase==9)
            {if(UIModule.Instance.PageIsOpen(UIPageIds.QFA))return;result.checks.Add("Help opened FAQ and returned to confirmation");Click(FindClose(confirm));confirmRun.phase=10;confirmNext=EditorApplication.timeSinceStartup+1;SaveConfirm();return;}
            if(confirmRun.phase==10)
            {if(UIModule.Instance.PageIsOpen(UIPageIds.UIWithdrawalConfirmPanel))return;Click(FindClose(form));confirmRun.phase=11;confirmNext=EditorApplication.timeSinceStartup+1;SaveConfirm();return;}
            if(confirmRun.phase==11&&!UIModule.Instance.PageIsOpen(UIPageIds.UIWithdrawalPanel))
            {
                if(confirmLastWithdrawal!=SaveDataUtils.GameData.lastWithdrawTime||confirmBalance!=ItemUtils.GetItemCount(E_ItemType.Dollar))throw new InvalidOperationException("Verification changed withdrawal/account state.");
                result.closed=true;result.closeMethod="Back Button then account Back Button";result.checks.Add("Returned to gameplay; withdrawal timestamp and balance unchanged; no payment submitted");confirmRun.status="complete";SaveConfirm();
            }
        }
        catch(Exception e){confirmRun.status="failed";confirmRun.error=e.ToString();SaveConfirm();Debug.LogException(e);}
    }
}
#endif
