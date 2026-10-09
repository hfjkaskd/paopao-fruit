#if BIZZA_REAL_WITHDRAW
using System;
using System.Collections;
using System.Collections.Generic;
using Bizza.Sdk;
using Cysharp.Threading.Tasks;
using Obfuz;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class UIPageIds
{
    public static readonly PageId GetRewardPanel = "GetRewardPanel";
}


public class GetRewardPanel : UIPageBase<ItemEntry, ItemEntry, DoubleGetRewardPanel.E_UseScene, Action<bool>>
{
    public BizzaButton claimBtn;
    public BizzaButton closeBtn;
    [SerializeField] private Button approvedNextButton;
    public ItemEntry itemA;
    public ItemEntry itemB;
    // private DoubleGetRewardPanel.E_UseScene doubleGetRewardPanel;
    public GameObject LevelObj;
    public TMP_Text itemATxt;
    public TMP_Text itemBTxt; // 另一个货币的
    public TMP_Text levelTxt;
    public Transform itemAPos;
    public Transform itemBPos;
    public GameObject levelTips;
    public TMP_Text noThanksText;
    public TMP_Text rewardText;

    private string iconName => WzCurrencySprites.InlineSpriteIndex(AccountModule.CountryType, true);

    public BonusRate bonusRate;
    public WithdrawProgress progress;

    public GameObject MaxDollarTip;

    private DoubleGetRewardPanel.E_UseScene _useScene;

    private bool isLookAd = false;
    private bool isNoCD;
    private int rewardLevel;

    // 每次打开独立保存领取状态，旧广告回调不能关闭复用后的新界面。
    private sealed class RewardClaim
    {
        public readonly UniTaskCompletionSource Completion = new UniTaskCompletionSource();
        public bool IsWin;
        public bool Started;
        public bool Finished;
        public bool PageClosed;
        public bool HasDollar;
        public float NormalRewardCount;
        public Vector3 RewardPosition;
        public Action<bool> Callback;
    }

    private RewardClaim rewardClaim;
    public UniTask RewardCompletion => rewardClaim != null ? rewardClaim.Completion.Task : UniTask.CompletedTask;
    private static bool HasDollar => !ChannelConfig.Instance.real_CustomConfig.singleCurrencyMode;

    void Awake()
    {
        if(approvedNextButton!=null) approvedNextButton.onClick.AddListener(() => closeBtn.onClick.Invoke());
        noThanksText.spriteAsset = commonSpriteAsset;

        claimBtn.onClick.AddListener(() => { LookAd(); });

        closeBtn.onClick.AddListener(() =>
        {
            RewardClaim claim = rewardClaim;
            if (claim != null)
            {
                if (!TryBeginClaim(claim)) return;
                try
                {
                    NumbericalStatistics.CheckCloseGetReward(E_AdPos.GetReward, dollarCount, null, rewardLevel);
                }
                finally
                {
                    FinishClaim(claim, true);
                }
                return;
            }

        });

        void LookAd()
        {
#if BIZZA_REAL_WITHDRAW
            RewardClaim claim = rewardClaim;
            if (claim != null)
            {
                if (!TryBeginClaim(claim)) return;
                if (!BizzaSdk.Ad.Inited)
                {
                    FinishClaim(claim, true, false);
                    return;
                }

                try
                {
                    // 由 SDK 保留“插屏补充激励”；失败兜底不累计普通领取关闭次数。
                    BizzaSdk.Ad.ShowRewardAd(E_AdPos.AddCoin.ToString(), HasDollar ? itemB.Count : 0,
                        result => FinishClaim(claim, !result.success, result.success));
                    UIModule.Instance.m_curadvertistics--;
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                    FinishClaim(claim, true, false);
                }
                return;
            }

#else
#endif
        }
    }

    protected override void OnClose()
    {
        if (rewardClaim != null)
        {
            rewardClaim.PageClosed = true;
            // 原生关闭按普通奖励处理；广告进行中交给该次广告回调结算。
            if (!rewardClaim.Started) FinishClaim(rewardClaim, true);
        }
    }

    private bool TryBeginClaim(RewardClaim claim)
    {
        if (claim.Started || claim.Finished || claim.PageClosed) return false;

        claim.Started = true;
        isRecover = true;
        claimBtn.interactable = false;
        closeBtn.interactable = false;
        return true;
    }

    private void FinishClaim(RewardClaim claim, bool grantNormalReward, bool? adSuccess = null)
    {
        if (claim.Finished) return;
        claim.Finished = true;

        try
        {
            if (adSuccess.HasValue && this != null && ReferenceEquals(rewardClaim, claim) && !claim.PageClosed)
            {
                isLookAd = adSuccess.Value;
                claim.Callback?.Invoke(adSuccess.Value);
            }
        }
        finally
        {
            try
            {
                claim.Callback = null;
                if (claim.IsWin && grantNormalReward && claim.HasDollar && claim.NormalRewardCount > 0)
                {
                    ItemUtils.AddItem(ItemUtils.ToCorrect(new ItemEntry
                    {
                        Type = E_ItemType.Dollar,
                        Count = claim.NormalRewardCount,
                    }), new AddItemParam
                    {
                        playAnim = true,
                        isAd = false,
                        startPos = claim.RewardPosition,
                        bUiPos = false,
                        source = DoubleGetRewardPanel.GetItemSource(DoubleGetRewardPanel.E_UseScene.CloseGetReward),
                    });
                }

                if (ReferenceEquals(rewardClaim, claim))
                {
                    if (this != null && !claim.PageClosed)
                    {
                        claim.PageClosed = true;
                        CloseSelf();
                    }
                    if (claim.IsWin) CustomWinClose();
                }
            }
            finally
            {
                claim.Completion.TrySetResult();
            }
        }
    }

    protected override void OnOpen(ItemEntry a, ItemEntry b, DoubleGetRewardPanel.E_UseScene useScene,
        Action<bool> _callback)
    {
        LogLogger.LogVerbose(BaseConst.LOG_Game, $"打开了界面 GetRewardPanel");
        itemA = a;
        rewardLevel = HarvestBridge.CurrentLevel > 0 ? HarvestBridge.CurrentLevel : SaveDataUtils.GameData.playerSelectedLv;
        itemB = b;
        _useScene = useScene;
        rewardClaim = new RewardClaim
        {
            IsWin = useScene == DoubleGetRewardPanel.E_UseScene.WinPanel,
            Callback = _callback,
        };
        isLookAd = false;
        {
            curTime = 0;
            isRecover = false;
            claimBtn.interactable = false;
            closeBtn.interactable = true;
        }
        isNoCD = useScene == DoubleGetRewardPanel.E_UseScene.DailyTask;
        LevelObj.SetActive(false);
        OnRefresh();
        if (useScene == DoubleGetRewardPanel.E_UseScene.WinPanel)
        {
            Transform rewardOrigin = BroadcastBarController.Instance != null
                ? BroadcastBarController.Instance.entryAObj?.transform
                : null;
            rewardClaim.HasDollar = HasDollar;
            rewardClaim.NormalRewardCount = dollarCount;
            rewardClaim.RewardPosition = rewardOrigin != null ? rewardOrigin.position
                : itemBPos != null ? itemBPos.position : transform.position;
        }
    }

    private float timer = 0.5f;
    private float curTime = 0;
    private bool isRecover = false;
    [SerializeField] private TMP_SpriteAsset commonSpriteAsset;

    private void Update() // 这里是为了防止恭喜获得界面弹出， 玩家点击游戏物体不小心点击到按钮做的防误触
    {
        if (isRecover) return;

        curTime += Time.deltaTime;
        if (curTime >= timer)
        {
            claimBtn.interactable = true;
            isRecover = true;
        }
    }

    private float dollarCount = 0;
    /// <summary>
    /// 打开时的界面
    /// </summary>
    private void OnRefresh()
    {
        MaxDollarTip.gameObject.SetActive(true);
        dollarCount = HasDollar ? itemB.Count : 0;
        itemB.Count = dollarCount;
        itemATxt.text = "+" + ItemUtils.GetItemText(itemA);
        noThanksText.text = LanguageUtils.GetText("Btn_NoThanks");

        bool win = _useScene == DoubleGetRewardPanel.E_UseScene.WinPanel;
        levelTips.SetObjActive(win);
        rewardText.text = LanguageUtils.GetText("Btn_Claim");
        if (win)
        {
#if BIZZA_REAL_WITHDRAW
            if (!ChannelConfig.Instance.real_CustomConfig.singleCurrencyMode)
            {
                itemB.Count = dollarCount * 2;
                rewardText.text = $"{rewardText.text}×2";
            }
            // SaveDataUtils.GameData.playerUnlockedLv++;
#endif

#if BIZZA_REAL_WITHDRAW
            AccountModule.Instance.Request_UserReachLevelReportRequest(null);
            LevelObj.SetActive(true);
            levelTxt.text = LanguageUtils.GetFormatText("Menu_LevelBtn", rewardLevel);
            LogLogger.LogVerbose(BaseConst.LOG_Game, "上报 关卡");
            if (!Bizza.Sdk.ChannelConfig.Instance.real_CustomConfig.singleCurrencyMode)
            {
                string _clash = LanguageUtils.GetText("CurrencyToken") + WithdrawalUtil.GetCustomizedFloatByCountryType(dollarCount);
                noThanksText.text = $"{LanguageUtils.GetFormatText("GetRewardPage_NoThanks", iconName, _clash)}";
            }
#endif
            SoundManager.Instance.PlaySFX("Win");
            SaveDataUtils.Save();

            // BizzaEventSystem.Emit(EventDefine.BizzaPlayerAction.PlayerOpenUI, UIPageIds.GetRewardPanel + "_Win");
        }
        else
        {
            //BizzaEventSystem.Emit(EventDefine.BizzaPlayerAction.PlayerOpenUI, UIPageIds.GetRewardPanel);
        }

        // The cash column has its own amount, including the local currency unit.
        itemBTxt.gameObject.SetActive(HasDollar && itemB.Count > 0f);
        itemBTxt.text = LanguageUtils.GetText("CurrencyToken") + " " + ItemUtils.GetItemText(itemB);

#if BIZZA_REAL_WITHDRAW
        float value = AccountModule.Instance.GetMaxDrawithRatio(true);
        bonusRate.gameObject.SetActive(value > 0);
        bonusRate.Init($"{value}%", false, true);
#else
#endif
    }

    ///
    /// 自定义的游戏胜利
    private void CustomWinClose()
    {
        FlowModule.LoadGameLevel();
    }
}
#endif
