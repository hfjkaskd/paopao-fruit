#if BIZZA_REAL_WITHDRAW
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

public partial class UIPageIds
{
    public static readonly PageId SlotPanel = "SlotPanel";
}

public class SlotPanel : UIPageBase
{
    public SlotMachineManager slotMachineManager;

    public TMP_Text slotHintTxt;
    public GameObject canClickObj;
    public GameObject notCanClickObj;

    public SlotRewardPanel slotRewardPanel;

    private float coinValue;

    private bool isSloting = false; public bool IsSloting => isSloting;
    private int spinRequest;

    public BizzaButton closeBtn;
    public BizzaButton faqBtn;
    public BizzaButton slotBtn;
    [SerializeField] private TMP_Text approvedFreeLabel;
    [SerializeField] private string freeSpinEnglish;
    [SerializeField] private string freeSpinPortuguese;
    [SerializeField] private GameObject idleReelDecoration;

    protected override void OnAwake()
    {
        base.OnAwake();

        closeBtn.onClick.AddListener(OnClosePanel);
        faqBtn.onClick.AddListener(OnClickFQA);
        slotBtn.onClick.AddListener(PlaySlotBtn);
        slotRewardPanel.Claimed += OnRewardClaimed;
    }

    protected override void OnClose()
    {
        ++spinRequest;
        // A native Back can close the page while the reward is visible.
        // Settle the existing result once, rather than discard the won reward.
        if (slotRewardPanel.HasUnclaimedReward) slotRewardPanel.OnClickClose();
        BizzaEventSystem.Set(EventDefine.CustomGameEvent.SlotProgressChanged, OnProgressChanged, false);
        SoundManager.Instance.PlayBGM("BGMusic");
    }

    protected override void OnOpen()
    {
        ++spinRequest;
        isSloting = false;
        coinValue = 0;
        slotRewardPanel.gameObject.SetActive(false);
        BizzaEventSystem.Set(EventDefine.CustomGameEvent.SlotProgressChanged, OnProgressChanged, true);
        Refresh();
        SoundManager.Instance.PlayBGM("SevenBgm");
    }

    private void OnProgressChanged()
    {
        if (!isSloting) Refresh();
    }

    private void Refresh()
    {
        if (idleReelDecoration != null) idleReelDecoration.SetActive(!isSloting);
#if BIZZA_REAL_WITHDRAW
        bool isCanclick = SlotProgressUtil.CanFreeSpin;
        bool canStart = !isSloting && !slotRewardPanel.HasUnclaimedReward;
        canClickObj.SetActive(isCanclick);
        notCanClickObj.SetActive(!isCanclick);
        // The original primary button starts an ad spin when no free spin is available.
        slotBtn.interactable = canStart;
        closeBtn.interactable = canStart;
        faqBtn.interactable = canStart;
        if (approvedFreeLabel != null)
        {
            int count = isCanclick ? 1 : 0;
            string format = LanguageUtils.SelectedLanguage == "pt-BR" ? freeSpinPortuguese : freeSpinEnglish;
            approvedFreeLabel.text = string.IsNullOrEmpty(format) ? count.ToString() : string.Format(format, count);
        }
        if (isCanclick)
        {
            slotHintTxt.text = LanguageUtils.GetText("SlotPanel_HaveSpin");
        }
        else
        {
            slotHintTxt.text = LanguageUtils.GetText("SlotPanel_AdSpin");
        }
#endif
    }

    public void OnClosePanel()
    {
        if (isSloting || slotRewardPanel.HasUnclaimedReward) return;
        CloseSelf();
    }

    public void OnClickFQA()
    {
        if (isSloting || slotRewardPanel.HasUnclaimedReward) return;
        UIModule.Instance.OpenPage(UIPageIds.SlotFAQPanel);
    }

    [Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.MethodName)]
    public void PlaySlotBtn()
    {
#if BIZZA_REAL_WITHDRAW
        if (isSloting || slotRewardPanel.HasUnclaimedReward) return;
        if (!SlotProgressUtil.CanFreeSpin)
        {
            PlayAdSpin();
            return;
        }
        isSloting = true;
        coinValue = 0;
        ++spinRequest;
        SlotProgressUtil.SetProgress(0);
        Refresh();
        OnPlaySlot(false);
#endif
    }

    [Button("Test Slot Btn")]
    public void TestSlotBtn()
    {
#if BIZZA_REAL_WITHDRAW
        PlayAdSpin();
#endif
    }

    private void PlayAdSpin()
    {
        if (isSloting || slotRewardPanel.HasUnclaimedReward) return;
        isSloting = true;
        coinValue = 0;
        int request = ++spinRequest;
        Refresh();
        BizzaSdk.Ad.ShowRewardAd("USSlot", WithdrawalUtil.GetDollarCountByReward(), result =>
        {
            if (this == null || request != spinRequest || !gameObject.activeInHierarchy) return;
            OnAdResult(result);
        });
        UIModule.Instance.m_curadvertistics--;
    }

    private void OnAdResult(Bizza.Sdk.ShowAdResult param)
    {
        #if BIZZA_REAL_WITHDRAW
        bool isSuccess = param.success;
        AccountModule.OceanShineAdRevenueResponse response = param.response;
        if (!isSuccess || response == null)
        {
            isSloting = false;
            Refresh();
            return;
        }
        coinValue = (float)response.GetBalance();
        //         LogUtil.Error("OnAdResult coinValue: " + coinValue);
        OnPlaySlot(true);
        #endif
    }

    private void OnPlaySlot(bool isAd)
    {
        int request = spinRequest;
        float _dollar = isAd ? WithdrawalUtil.GetDollarCountByReward() : WithdrawalUtil.GetDollarCountBtFree();
        SoundManager.Instance.PlaySFX("SevenSpin");
        slotMachineManager.PlayAnim(
            isAd,
            (string type) =>
            {
                if (this == null || request != spinRequest || !gameObject.activeInHierarchy) return;
                slotRewardPanel.gameObject.SetActive(true);
                slotRewardPanel.Init(coinValue, _dollar, type, isAd);
                isSloting = false;
                Refresh();
            }
        );
    }

    private void OnRewardClaimed()
    {
        if (!IsClosing) Refresh();
    }

    private void OnDestroy()
    {
        if (slotRewardPanel != null) slotRewardPanel.Claimed -= OnRewardClaimed;
    }

}
#endif
