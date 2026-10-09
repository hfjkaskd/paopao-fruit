#if BIZZA_REAL_WITHDRAW
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class UIPageIds
{
    public static readonly PageId DailyWithdrawPanel = "DailyWithdrawPanel";
}

 
public class DailyWithdrawPanel : UIPageBase
{
    public TMP_Text clashTxt;

    public TMP_Text balanceTxt;
    public TMP_Text withdrawalTxt;

    public RectTransform root;

    private List<AccountModule.OceanShineWithdrawalPageResponse.WithdrawalPlatform> plats = new();

    [Header("按钮")]
    [SerializeField] private BizzaButton withdrawBtn;
    [SerializeField] private BizzaButton clickBtn;
    [SerializeField] private PaymentConfig methodIcons;
    [SerializeField] private Image[] availableMethodImages;
    protected override void OnAwake()
    {
        
        withdrawBtn.onClick.AddListener(OnClickWithdrawBtn);
        clickBtn.onClick.AddListener(CloseSelf);
    }
    protected override void OnOpen()
    {
        plats.Clear();
        AccountModule.Instance.Request_WithdrawalPageRequest(Refresh);
    }

    private void Refresh(FailHttpResponse<AccountModule.OceanShineWithdrawalPageResponse> response)
    {
        if (!response.success || response.data == null)
        {
            CloseSelf();
            return;
        }

        plats = response.data.Os_Wwf;
        OnRefresh();
    }

    private void OnRefresh()
    {
        double coin = AccountModule.Instance.Os_Current_Uso.GetBalance();
        double clash =  AccountModule.Instance.Os_Current_Uso.Os_Ewl;
        string clashContent = $"{LanguageUtils.GetText("CurrencyToken")}{WithdrawalUtil.GetCustomizedValueByCountryType((float)clash)}";
        clashTxt.text = clashContent;
        balanceTxt.text = $"{WithdrawalUtil.GetCustomizedValueByCountryType((float)coin)}";
        withdrawalTxt.text = clashContent;
        if(availableMethodImages!=null&&methodIcons!=null)
        {
            int shown=0;
            foreach(var platform in plats)
            {
                if(shown>=availableMethodImages.Length)break;
                var sprite=methodIcons.GetSpriteByIconKey(platform.Os_Cn);if(sprite==null)continue;
                var image=availableMethodImages[shown++];image.sprite=sprite;image.transform.parent.gameObject.SetActive(true);
            }
            for(int i=shown;i<availableMethodImages.Length;i++)availableMethodImages[i].transform.parent.gameObject.SetActive(false);
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(root);
    }

    protected override void OnClose()
    {
    }

    public void OnClickWithdrawBtn()
    {
        bool isSelectPlatform = false;
        AccountModule.OceanShineWithdrawalPageResponse.WithdrawalPlatform plat = null;
        foreach (var _plat in plats)
        {
            if (AccountModule.CountryType == AccountModule.E_CountryType.US
                && _plat.Os_Cn.Equals(UIWithdrawalPanel.paypalInfo))
            {
                plat = _plat;
                break;
            }
            else if (AccountModule.CountryType == AccountModule.E_CountryType.BR
                     && _plat.Os_Cn.Equals(UIWithdrawalPanel.pagBankInfo))
            {
                plat = _plat;
                break;
            }
            else if (AccountModule.CountryType == AccountModule.E_CountryType.ID
                     && _plat.Os_Cn.Equals(UIWithdrawalPanel.danaInfo))
            {
                isSelectPlatform = true;
                plat = _plat;
                break;
            }
        }

        if (plat == null)
        {
            LogLogger.LogVerbose(BaseConst.LOG_Game, "没有找到平台");
        }

        CloseSelf();
        // UIModule.Instance.OpenPage<AccountModule.OceanShineWithdrawalPageResponse.WithdrawalPlatform, List<AccountModule.OceanShineWithdrawalPageResponse.WithdrawalPlatform>, E_WithdrawType, Action, bool>
        //     (E_UIPageType.WithdrawConfirmPanel, plat, plats, E_WithdrawType.DailyWithdraw, null, isSelectPlatform);

        UIModule.Instance.OpenPage(UIPageIds.RealWithdrawPanel);
    }
}
#endif
