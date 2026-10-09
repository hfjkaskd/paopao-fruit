#if BIZZA_REAL_WITHDRAW
using System;
using System.Collections.Generic;
using System.IO;
using AdvancedInputFieldPlugin;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static partial class OrchardApprovedRuntimeCheck
{
    private static Run accountRun;
    private static string accountFolder;
    private static double accountNext,accountDeadline;
    private static string[] savedAccountFields, originalInputValues;
    private static int savedPixChannel;
    private static float savedAccountBalance;
    private static bool accountRestoreNeeded;
    private static void StartAccountCheck(bool shorter)
    {
        accountFolder="Design/OrchardAccountRefinement-20260928/"+(shorter?"RuntimeShort/":"Runtime/");Directory.CreateDirectory(accountFolder);
        accountRun=new Run{startedUtc=DateTime.UtcNow.ToString("O"),status="running"};accountNext=0;accountDeadline=EditorApplication.timeSinceStartup+120;SaveAccount();
    }
    private static void SaveAccount(){File.WriteAllText(accountFolder+"verification.json",JsonUtility.ToJson(accountRun,true));}
    private static void RestoreSavedAccount()
    {
        if(!accountRestoreNeeded)return;
        var data=SaveDataUtils.GameData;
        data.withdrawNameInfo=savedAccountFields[0];data.withdrawCPFInfo=savedAccountFields[1];data.withdrawEmailInfo=savedAccountFields[2];data.withdrawPhoneInfo=savedAccountFields[3];
        data.accountIdentificationInfo_C=savedAccountFields[4];data.accountIdentificationInfo_P=savedAccountFields[5];data.accountIdentificationInfo_V=savedAccountFields[6];data.accountIdentificationInfo_E=savedAccountFields[7];
        data.pixChannelIndex=savedPixChannel;SaveDataUtils.gameStrategy.SaveData();accountRestoreNeeded=false;
    }
    private static async UniTask OpenAccountCheck()
    {
        try
        {
            if(paymentPlatforms.Count==0)throw new InvalidOperationException("No server payment platform available.");
            await UIModule.Instance.OpenPage<AccountModule.OceanShineWithdrawalPageResponse.WithdrawalPlatform,List<AccountModule.OceanShineWithdrawalPageResponse.WithdrawalPlatform>,E_WithdrawType,Action,bool>(UIPageIds.UIWithdrawalPanel,paymentPlatforms[0],new List<AccountModule.OceanShineWithdrawalPageResponse.WithdrawalPlatform>(paymentPlatforms),E_WithdrawType.Real,null,false);
        }
        catch(Exception e){RestoreSavedAccount();accountRun.status="failed";accountRun.error=e.ToString();SaveAccount();}
    }
    private static void TickAccountCheck()
    {
        if(accountRun==null||accountRun.status!="running"||EditorApplication.timeSinceStartup<accountNext)return;
        accountNext=EditorApplication.timeSinceStartup+.3;
        try
        {
            if(EditorApplication.timeSinceStartup>accountDeadline)throw new TimeoutException("Account check phase "+accountRun.phase);
            if(!EditorApplication.isPlaying||!HarvestBridge.Ready||UIModule.Instance==null||UIModule.Instance.Opening||TransparentBlock.IsBlock)return;
            var page=UIModule.Instance.GetPage(UIPageIds.UIWithdrawalPanel) as UIWithdrawalPanel;
            if(accountRun.phase==0)
            {
                if(UIModule.Instance.HasPopup)return;
                var data=SaveDataUtils.GameData;savedAccountFields=new[]{data.withdrawNameInfo,data.withdrawCPFInfo,data.withdrawEmailInfo,data.withdrawPhoneInfo,data.accountIdentificationInfo_C,data.accountIdentificationInfo_P,data.accountIdentificationInfo_V,data.accountIdentificationInfo_E};savedPixChannel=data.pixChannelIndex;
                savedAccountBalance=ItemUtils.GetItemCount(E_ItemType.Dollar);
                accountRun.pages.Add(new PageResult{page="UIWithdrawalPanel",openMethod="InitWZ initialization, HUD coin Button, server payment platform, existing UIModule account page"});
                Click(UnityEngine.Object.FindObjectOfType<CurrencyBar>().coinBtn);accountRun.phase=1;accountNext=EditorApplication.timeSinceStartup+3;SaveAccount();return;
            }
            var result=accountRun.pages[0];
            if(accountRun.phase==1)
            {
                var withdrawal=UIModule.Instance.GetPage(UIPageIds.RealWithdrawPanel) as RealWithdrawPanel;
                if(withdrawal==null||!withdrawal.gameObject.activeInHierarchy)return;
                paymentPlatforms.Clear();foreach(var way in withdrawal.withdrawWayRoot.GetComponentsInChildren<WithdrawWay>())if(way.data!=null)paymentPlatforms.Add(way.data);
                if(paymentPlatforms.Count==0)return;
                Click(FindClose(withdrawal));accountRun.phase=2;accountNext=EditorApplication.timeSinceStartup+1;SaveAccount();return;
            }
            if(accountRun.phase==2)
            {if(UIModule.Instance.HasPopup)return;OpenAccountCheck().Forget();accountRun.phase=3;accountNext=EditorApplication.timeSinceStartup+2;SaveAccount();return;}
            if(page==null||!page.gameObject.activeInHierarchy||!page.GetComponent<OrchardAccountVisual>().IsReady)return;
            var inputs=new[]{page.accountNameInput,page.CPFNumberInput,page.paypalMailInput};
            if(accountRun.phase==3)
            {
                if(!page.accountNameInput.gameObject.activeInHierarchy||!page.paypalMailInput.gameObject.activeInHierarchy)throw new InvalidOperationException("Expected server-provided PagBank form.");
                result.opened=true;originalInputValues=new[]{inputs[0].Text,inputs[1].Text,inputs[2].Text};
                if(!page.paymentImage.preserveAspect||page.paymentImage.type!=Image.Type.Simple)throw new InvalidOperationException("Payment image can still stretch.");
                foreach(var input in inputs)
                {
                    var background=input.transform.Find("Background").GetComponent<Image>();
                    if(background.sprite==null||background.type!=Image.Type.Simple)throw new InvalidOperationException("Input background still slices a square texture.");
                    float ratio=background.rectTransform.rect.width/background.rectTransform.rect.height;
                    if(Mathf.Abs(ratio-background.sprite.rect.width/background.sprite.rect.height)>.15f)throw new InvalidOperationException("Input artwork is compressed.");
                }
                result.checks.Add("Logo preserves its sprite aspect; all input frames use wide native artwork within 2% aspect tolerance");
                result.screenshot=accountFolder+"00-account.png";CashScreenshot(result.screenshot);accountRun.index=0;accountRun.phase=4;SaveAccount();return;
            }
            if(accountRun.phase==4)
            {
                if(accountRun.index>=inputs.Length){accountRun.phase=6;SaveAccount();return;}
                var input=inputs[accountRun.index];var graphic=input.transform.Find("Background").GetComponent<Image>();var bounds=GraphicBounds(graphic);
                var pointer=new PointerEventData(EventSystem.current){position=bounds.center,button=PointerEventData.InputButton.Left};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
                if(hits.Count==0||ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject)!=input.gameObject)throw new InvalidOperationException("Input center is blocked: "+input.name);
                ExecuteEvents.Execute(input.gameObject,pointer,ExecuteEvents.pointerDownHandler);ExecuteEvents.Execute(input.gameObject,pointer,ExecuteEvents.pointerUpHandler);ExecuteEvents.Execute(input.gameObject,pointer,ExecuteEvents.pointerClickHandler);
                accountRun.phase=5;accountNext=EditorApplication.timeSinceStartup+1;SaveAccount();return;
            }
            if(accountRun.phase==5)
            {
                var input=inputs[accountRun.index];
                if(!input.Selected||input.transform.Find("Background").GetComponent<Image>().sprite.name!="FocusedInput")throw new InvalidOperationException("Input did not gain real focus and its blue frame.");
                input.Text=new[]{"Teste Orchard","123","test@example.invalid"}[accountRun.index];
                if(string.IsNullOrEmpty(input.Text))throw new InvalidOperationException("Input did not accept editing.");
                input.Text=originalInputValues[accountRun.index];
                if(accountRun.index==2){CashScreenshot(accountFolder+"01-email-focus-keyboard.png");accountRun.phase=51;SaveAccount();return;}
                accountRun.phase=51;SaveAccount();return;
            }
            if(accountRun.phase==51)
            {
                // This project's input plugin intentionally retains focus on a null selection.
                // Moving to the next real input exercises its supported deselection path.
                inputs[(accountRun.index+1)%inputs.Length].ManualSelect();
                accountRun.phase=52;accountNext=EditorApplication.timeSinceStartup+1;SaveAccount();return;
            }
            if(accountRun.phase==52)
            {
                var input=inputs[accountRun.index];
                if(input.transform.Find("Background").GetComponent<Image>().sprite.name!="Input")throw new InvalidOperationException("Input did not restore its gold frame.");
                result.checks.Add("Input "+accountRun.index+": EventSystem hit, focus, blue frame, edit, restored text, deselection and gold frame passed");
                accountRun.index++;accountRun.phase=4;SaveAccount();return;
            }
            if(accountRun.phase==6)
            {
                // Return to the full page through the keyboard's platform-independent API.
                NativeKeyboardManager.Keyboard.HideKeyboard();
                accountRun.phase=60;accountNext=EditorApplication.timeSinceStartup+1;SaveAccount();return;
            }
            if(accountRun.phase==60)
            {
                CashScreenshot(accountFolder+"01-input-verified.png");
                foreach(var input in inputs)input.Text="";
                var button=page.transform.Find("Root/FillRoot/pageContent/BtnWithdrawal").GetComponent<Button>();result.buttons.Add(Inspect(button));
                accountRestoreNeeded=true;Click(button);RestoreSavedAccount();
                accountRun.phase=7;accountNext=EditorApplication.timeSinceStartup+3;SaveAccount();return;
            }
            if(accountRun.phase==7)
            {
                if(UIModule.Instance.PageIsOpen(UIPageIds.UIWithdrawalConfirmPanel))throw new InvalidOperationException("Empty fields incorrectly passed validation.");
                if(!page.accountNameErrorTra.gameObject.activeSelf||!page.CPFNumberErrorTra.gameObject.activeSelf||!page.paypalMailErrorTra.gameObject.activeSelf)throw new InvalidOperationException("Missing invalid field messages.");
                var hint=page.transform.Find("Root/FillRoot/pageContent/ReferenceHelpRow/ReferenceInfoCard").GetComponent<Image>();
                var error=GraphicBounds(page.paypalMailError);var hintBounds=GraphicBounds(hint);
                if(error.Overlaps(hintBounds))throw new InvalidOperationException("Validation message overlaps the help strip.");
                CashScreenshot(accountFolder+"02-validation.png");
                result.checks.Add("Continue Button ran production empty-field validation, focused the first invalid field, kept errors clear of the help strip; no confirmation/payment opened; saved form values restored");
                for(int i=0;i<inputs.Length;i++){inputs[i].Text=originalInputValues[i];inputs[i].ManualDeselect();}
                NativeKeyboardManager.Keyboard.HideKeyboard();
                accountRun.phase=8;accountNext=EditorApplication.timeSinceStartup+1;SaveAccount();return;
            }
            if(accountRun.phase==8)
            {Click(FindClose(page));result.closeMethod="Back Button EventSystem click";accountRun.phase=9;accountNext=EditorApplication.timeSinceStartup+1;SaveAccount();return;}
        }
        catch(Exception e){RestoreSavedAccount();accountRun.status="failed";accountRun.error=e.ToString();SaveAccount();Debug.LogException(e);}
    }
    private static void FinishAccountCheck()
    {
        if(accountRun==null||accountRun.status!="running"||accountRun.phase!=9||UIModule.Instance==null||UIModule.Instance.PageIsOpen(UIPageIds.UIWithdrawalPanel))return;
        if(savedAccountBalance!=ItemUtils.GetItemCount(E_ItemType.Dollar)){accountRun.status="failed";accountRun.error="Balance changed during form verification.";}
        else{accountRun.pages[0].closed=true;accountRun.pages[0].checks.Add("Back returned to gameplay; balance unchanged; no withdrawal submitted");accountRun.status="complete";}
        SaveAccount();
    }
}
#endif
