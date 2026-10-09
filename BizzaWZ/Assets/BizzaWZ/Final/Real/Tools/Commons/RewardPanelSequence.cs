#if BIZZA_REAL_WITHDRAW
using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Covers loading, claiming and closing, not just an already visible page.
/// A settlement waits for the current reward; incidental rewards never stack.
/// </summary>
public sealed class RewardPanelSequence
{
    private UniTaskCompletionSource tail;
    private bool settlementPending;

    public bool IsBusy => tail != null;

    public bool CanShowMatchReward(int remainingMatches, bool levelSettled)
    {
        return remainingMatches > 0 && !levelSettled && !IsBusy;
    }

    public bool TryRun(bool settlement, Func<UniTask> showUntilClosed)
    {
        if (settlement ? settlementPending : IsBusy) return false;

        var previous = tail;
        var completion = new UniTaskCompletionSource();
        tail = completion;
        if (settlement) settlementPending = true;
        Run(previous, completion, settlement, showUntilClosed).Forget();
        return true;
    }

    private async UniTask Run(UniTaskCompletionSource previous, UniTaskCompletionSource completion,
        bool settlement, Func<UniTask> showUntilClosed)
    {
        try
        {
            if (previous != null) await previous.Task;
            await showUntilClosed();
        }
        catch (Exception error)
        {
            Debug.LogException(error);
        }
        finally
        {
            if (ReferenceEquals(tail, completion)) tail = null;
            if (settlement) settlementPending = false;
            completion.TrySetResult();
        }
    }
}
#endif
