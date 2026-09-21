using System;
using Unity.VisualScripting;
using UnityEngine;

public partial class UIPageIds
{
    public static readonly PageId LosePanel = "LosePanel";
}

public partial class GameSaveData : ISaveData
{
    public int currentReviveCount = 0;
    public int levelFailCount;
}

public enum LoseReason
{
    Health,
    Timeout,
}

public struct LevelInfo
{
    public double levelProgress;
    public int levelTotalTarget;
    public int levelAchieveTarget;
    public int levelRemainingTarget; 
    public string levelPropUsedCount;
}

public class LosePanel : UIPageBase<LoseReason, LevelInfo>
{
    public override bool PreserveRectTransformOnOpen => true;

    public BizzaButton reviveButton;

    public BizzaButton bizzaLoseButton1;
    public BizzaButton bizzaLoseButton2;

    public GameObject[] reviveObjs;
    public GameObject[] loseObjs;

    private LoseReason loseReason;
    private LevelInfo levelInfo;
    private int openVersion;
    private bool isOpen;
    private bool rewardPending;

    protected override void OnClose()
    {
        isOpen = false;
        rewardPending = false;
        openVersion++;
        if (reviveButton != null)
        {
            reviveButton.onClick.RemoveListener(OnClickReviveButton);
        }

        if (bizzaLoseButton1 != null)
        {
            bizzaLoseButton1.onClick.RemoveListener(RestartLevel);
        }

        if (bizzaLoseButton2 != null)
        {
            bizzaLoseButton2.onClick.RemoveListener(RestartLevel);
        }
    }

    protected override void OnOpen(LoseReason loseReason, LevelInfo levelInfo)
    {
        isOpen = true;
        rewardPending = false;
        openVersion++;
        this.loseReason = loseReason;
        this.levelInfo = levelInfo;
        if (reviveButton != null)
        {
            reviveButton.onClick.AddListener(OnClickReviveButton);
        }

        if (bizzaLoseButton1 != null)
        {
            bizzaLoseButton1.onClick.AddListener(RestartLevel);
        }

        if (bizzaLoseButton2 != null)
        {
            bizzaLoseButton2.onClick.AddListener(RestartLevel);
        }
        bool isHaveRevive = SaveDataUtils.GameData.currentReviveCount < BridgingUtil.MAX_REVIVE_COUNT;
        SetObjsActive(reviveObjs, isHaveRevive);
        SetObjsActive(loseObjs, !isHaveRevive);
    }

    private void OnClickReviveButton()
    {
        if (!isOpen || rewardPending || SaveDataUtils.GameData.currentReviveCount >= BridgingUtil.MAX_REVIVE_COUNT) return;
        rewardPending = true;
        int version = openVersion;
        #if BIZZA_REAL_WITHDRAW
        BizzaSdk.Ad.ShowRewardAd("Revive", WithdrawalUtil.GetDollarCountBtFree(), a =>
        {
            if (!isOpen || !rewardPending || version != openVersion) return;
            rewardPending = false;
            FlowModule.OnReviveResult(a.success, loseReason, levelInfo);
        });
        #else
        FlowModule.OnReviveResult(true, loseReason, levelInfo);
        #endif
    }

    private void RestartLevel()
    {
        FlowModule.LoadGameLevel();
    }

    private static void SetObjsActive(GameObject[] objs, bool active)
    {
        if (objs == null)
        {
            return;
        }

        foreach (var obj in objs)
        {
            if (obj != null)
            {
                obj.SetActive(active);
            }
        }
    }
}
