#if BIZZA_REAL_WITHDRAW
using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public static class Real_GetRewardPanelUtil
{
    private static readonly RewardPanelSequence sequence = new RewardPanelSequence();

    public static bool CanShowMatchReward(int remainingMatches, bool levelSettled)
    {
        return sequence.CanShowMatchReward(remainingMatches, levelSettled);
    }
    /// <summary>
    /// 打开恭喜获得界面
    /// </summary>
    /// <param name="e_UseScene"></param>
    /// <param name="action"></param>
    public static bool OpenGetRewardPanel(DoubleGetRewardPanel.E_UseScene e_UseScene, Action<bool> action = null)
    {
        var ui = UIModule.Instance;
        if (ui == null)
        {
            action?.Invoke(false);
            return false;
        }
        float moneyValue = WithdrawalUtil.GetDollarCountByReward();
        var money = new ItemEntry()
        {
            Type = E_ItemType.Dollar,
            Count = WithdrawalUtil.GetCustomizedFloatByCountryType(moneyValue)
        };

        var coin = new ItemEntry()
        {
            Type = E_ItemType.Gold,
            Count = 1000
        };

        // Snapshot the reward before a queued settlement advances the saved level.
        bool accepted = sequence.TryRun(e_UseScene == DoubleGetRewardPanel.E_UseScene.WinPanel,
            async () =>
            {
                // Also respect pages opened by another framework entry point.
                var existing = ui.GetPage(UIPageIds.GetRewardPanel) as GetRewardPanel;
                if (existing != null) await WaitForClose(existing);
                if (ui == null || ui != UIModule.Instance) return;

                var page = await ui.OpenPage<ItemEntry, ItemEntry, DoubleGetRewardPanel.E_UseScene, Action<bool>>
                    (UIPageIds.GetRewardPanel, coin, money, e_UseScene, action) as GetRewardPanel;
                if (page != null) await WaitForClose(page);
            });
        if (!accepted) action?.Invoke(false);
        return accepted;
    }

    private static async UniTask WaitForClose(GetRewardPanel page)
    {
        // An ad callback can still be settling after the page is forcibly hidden.
        await page.RewardCompletion;
        await UniTask.WaitUntil(() => page == null || !page.gameObject.activeInHierarchy);
    }

    public static void ShowRewardAd(E_AdPos pos, Action<bool> action)
    {
        float moneyValue = WithdrawalUtil.GetDollarCountByReward();
        var money = new ItemEntry()
        {
            Type = E_ItemType.Dollar,
            Count = WithdrawalUtil.GetCustomizedFloatByCountryType(moneyValue)
        };
        BizzaSdk.Ad.ShowRewardAd(pos.ToString(), money.Count, (Bizza.Sdk.ShowAdResult showAdResult) =>
        {
            bool success = showAdResult.success;
            LogLogger.LogAdInfo("激励广告成功");
            action?.Invoke(success);
        }
        );
    }
}
#endif
