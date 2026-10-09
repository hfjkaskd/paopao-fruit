#if BIZZA_REAL_WITHDRAW
using System;
using System.IO;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

// Tests the production ordering code with controlled load/claim/close completions.
// No gameplay scene, account, ad SDK, save or currency mutation is initialized.
public static class OrchardRewardSequenceValidation
{
    public static void Validate()
    {
        const string output = "Design/RewardSequence-20261008/result.txt";
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        if (File.Exists(output)) File.Delete(output);
        var report = new StringBuilder();
        TestLastMatch(report);
        TestLoadingAndSettlement(report);
        TestAdAndCloseOrder(report, true);
        TestAdAndCloseOrder(report, false);
        TestSettlementOnly(report);

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/BizzaWZ/Final/Real/UI/GetRewardPanel/GetRewardPanel.prefab");
        var page = prefab.GetComponent<GetRewardPanel>();
        Require(!page.MultiPages, "Reward prefab must disallow simultaneous copies.");
        Require(page.claimBtn != null && page.closeBtn != null && page.LevelObj != null &&
            page.itemATxt != null && page.itemBTxt != null && page.rewardText != null,
            "Original reward/claim bindings must remain assigned.");
        report.AppendLine("PASS: reward prefab is single-instance and keeps its claim/amount bindings.");
        report.AppendLine("Unity compilation + isolated production sequence tests only. No device/ad SDK test or APK build.");
        File.WriteAllText(output, report.ToString());
        Debug.Log(report.ToString());
    }

    private static void TestLastMatch(StringBuilder report)
    {
        var sequence = new RewardPanelSequence();
        Require(!sequence.CanShowMatchReward(0, false), "Final match must not show an incidental reward.");
        Require(!sequence.CanShowMatchReward(-1, false), "Invalid remaining count must not show a reward.");
        Require(!sequence.CanShowMatchReward(1, true), "No match popup after settlement starts.");
        Require(sequence.CanShowMatchReward(1, false), "Ordinary match rewards must remain available.");
        report.AppendLine("PASS: final/settled matches suppressed; normal match reward still allowed.");
    }

    private static void TestLoadingAndSettlement(StringBuilder report)
    {
        var sequence = new RewardPanelSequence();
        var loaded = new UniTaskCompletionSource();
        var rewardClosed = new UniTaskCompletionSource();
        var settlementClosed = new UniTaskCompletionSource();
        int visible = 0, maxVisible = 0, wins = 0;

        Require(sequence.TryRun(false, async () =>
        {
            await loaded.Task;
            maxVisible = Math.Max(maxVisible, ++visible);
            await rewardClosed.Task;
            visible--;
        }), "First reward should open.");
        Require(sequence.IsBusy && !sequence.CanShowMatchReward(3, false), "Loading must reserve the slot.");
        Require(!sequence.TryRun(false, UnexpectedPopup), "Rapid repeat during loading must be ignored.");
        Require(sequence.TryRun(true, async () =>
        {
            wins++;
            maxVisible = Math.Max(maxVisible, ++visible);
            await settlementClosed.Task;
            visible--;
        }), "Settlement must be retained while reward loads.");
        Require(!sequence.TryRun(true, UnexpectedPopup), "Duplicate settlement must be ignored.");
        Require(wins == 0, "Settlement cannot open over a loading reward.");
        loaded.TrySetResult();
        Require(visible == 1 && wins == 0, "Loaded reward alone should be visible.");
        rewardClosed.TrySetResult();
        Require(visible == 1 && wins == 1, "Settlement opens exactly once after reward closes.");
        rewardClosed.TrySetResult();
        Require(!sequence.TryRun(false, UnexpectedPopup), "No incidental popup over settlement.");
        settlementClosed.TrySetResult();
        Require(!sequence.IsBusy && visible == 0 && maxVisible == 1 && wins == 1,
            "Sequence must finish without overlap or duplicate settlement.");
        Require(sequence.CanShowMatchReward(3, false), "Next level's normal rewards should be available.");
        report.AppendLine("PASS: slow-load reward -> one queued settlement; rapid repeats ignored; max visible = 1.");
    }

    private static void TestAdAndCloseOrder(StringBuilder report, bool closeFirst)
    {
        var sequence = new RewardPanelSequence();
        var adFinished = new UniTaskCompletionSource();
        var hidden = new UniTaskCompletionSource();
        var winClosed = new UniTaskCompletionSource();
        int wins = 0;
        sequence.TryRun(false, async () => { await adFinished.Task; await hidden.Task; });
        sequence.TryRun(true, () => { wins++; return winClosed.Task; });
        (closeFirst ? hidden : adFinished).TrySetResult();
        Require(wins == 0 && sequence.IsBusy, "Both claim callback and page hide must finish before settlement.");
        (closeFirst ? adFinished : hidden).TrySetResult();
        Require(wins == 1, "Settlement must continue once both conditions complete.");
        winClosed.TrySetResult();
        Require(!sequence.IsBusy, "Completed settlement must release the sequence.");
        report.AppendLine("PASS: " + (closeFirst ? "page closes before delayed ad callback" : "ad completes before close animation") + "; settlement waits.");
    }

    private static void TestSettlementOnly(StringBuilder report)
    {
        var sequence = new RewardPanelSequence();
        var closed = new UniTaskCompletionSource();
        int wins = 0;
        Require(sequence.TryRun(true, () => { wins++; return closed.Task; }), "Settlement can open with no match reward.");
        Require(wins == 1, "Settlement should open immediately when no reward is pending.");
        Require(!sequence.TryRun(true, UnexpectedPopup), "Rapid duplicate win must not show again.");
        closed.TrySetResult();
        Require(!sequence.IsBusy, "Closing settlement must release the slot.");
        report.AppendLine("PASS: last-match settlement opens directly and only once.");
    }

    private static UniTask UnexpectedPopup()
    {
        throw new InvalidOperationException("Rejected popup unexpectedly executed.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
#endif
