using System;
using System.Collections;
using System.Collections.Generic;
using CorePlay;
using Cysharp.Threading.Tasks;
using UnityEngine;

public partial class GameSaveData
{
    public Dictionary<string, string> harvestModels = new Dictionary<string, string>();
}

/// <summary>Gameplay caller state is separate from the framework's BridgingUtil callbacks.</summary>
public static class HarvestBridge
{
    private static bool initialized;
    private static bool starting;
    private static bool ready;
    private static bool settled;
    private static int session;
    private static GameObject gameplayPrefab;
    public static bool Ready => ready;
    public static int CurrentLevel { get; private set; }

    public static Vector3 GetGuideWorldPosition()
    {
        var items=EDLHEMMBABM.Instance.GetTotalItems();
        CollectItem target=null;
        foreach(var item in items)
        {
            if(item==null || item.Status!=LECIONHKEJL.OnField) continue;
            if(target==null) target=item;
            if(item.Type==39) { target=item; break; }
        }
        if(target==null) throw new InvalidOperationException("Guide requested before gameplay targets were ready");
        var root=target.GetComponentInParent<Canvas>().rootCanvas;
        var screen=RectTransformUtility.WorldToScreenPoint(root.renderMode==RenderMode.ScreenSpaceOverlay ? null : root.worldCamera,target.transform.position);
        var camera=Camera.main;
        return camera!=null ? camera.ScreenToWorldPoint(new Vector3(screen.x,screen.y,camera.nearClipPlane+1f)) : (Vector3)screen;
    }

    public static async UniTask Preload()
    {
        // Readiness belongs to this loaded scene, never to the previous saved session.
        SaveDataUtils.GameData.customTutorialCanPlay=false;
        if (gameplayPrefab != null) return;
        gameplayPrefab = await Resources.LoadAsync<GameObject>("HarvestRoot") as GameObject;
        if (gameplayPrefab == null) throw new InvalidOperationException("Missing configured HarvestRoot prefab");
    }

    public static GameObject Attach(Transform parent)
    {
        if (gameplayPrefab == null) throw new InvalidOperationException("Gameplay preload was not awaited");
        return UnityEngine.Object.Instantiate(gameplayPrefab, parent, false);
    }

    public static void Initialize(MgrUI manager, MgrGlobalUI global)
    {
        if (!initialized)
        {
            MCCIJBJGMCK.Init();
            Timer.Instance.Init();
            GameAudio.Init();
            BMNFNJFCPHG.Instance.Init();
            OJEEJGGLNPC.Instance.LoadConfig();
            HarvestLocalization.Install();
            PLMIHDHFAAL.Instance.Init(GameRes.LoadTextAsset("res_server/server_configs/MainLevelConfig"));
            JEFOMCDAPGK.Instance.Init();
            EDLHEMMBABM.Instance.Init();
            initialized = true;
        }
        manager.Init();
        global.Init();
        manager.Open("coreplay/CorePlayUI", false);
    }

    public static void StartLevel(CorePlayUI ui)
    {
        if (starting) return;
        starting = true;
        ready = false;
        settled = false;
        int token = ++session;
        ui.StartCoroutine(LoadLevel(ui, token));
    }

    private static IEnumerator LoadLevel(CorePlayUI ui, int token)
    {
        SaveDataUtils.GameData.customTutorialCanPlay = false;
        var model = JEFOMCDAPGK.Instance;
        int level = Mathf.Max(1, SaveDataUtils.GameData.playerSelectedLv);
        CurrentLevel = level;
        bool sameLevel = model.CurMainLevelIndex == level;
        model.Data.mainLevelWinCnt = level - 1;
        var data = model.MainLevelData;
        bool restore = sameLevel && data.hasEffectiveSave && data.gameSnapshot?.levelConfig != null && !model.Data.isLevelFinished && !data.isLose;
        // Incomplete basic instruction restarts the teaching board, never marks it completed.
        restore &= SaveDataUtils.GameData.customTutorialEnd;
        restore &= data.difficultyRevision == HarvestDifficulty.Revision;
        int savedReviveCount=SaveDataUtils.GameData.currentReviveCount;
        int savedLevelReviveCount=SaveDataUtils.GameData.levelReviveCount;
        FlowModule.GameStart();
        if (restore) yield return EDLHEMMBABM.Instance.RestoreFromSnapshot(null);
        else
        {
            data.Reset(false);
            data.difficultyRevision = HarvestDifficulty.Revision;
            model.Data.isLevelFinished = false;
            yield return EDLHEMMBABM.Instance.ParseConfig(PLMIHDHFAAL.Instance.GetMainLevelConfig(), false, null);
        }
        if (token != session || ui == null) yield break;
        ui.UpdateBehavior();
        Canvas.ForceUpdateCanvases();
        yield return null;
        if (restore)
        {
            // GameStart clears per-attempt counters. A restored board continues the
            // saved attempt; restore its counters without spending inventory again.
            NumbericalStatistics._propUseTimes[E_ItemType.GameProp_1]=data.undoUseCount;
            NumbericalStatistics._propUseTimes[E_ItemType.GameProp_2]=data.shuffleUseCount;
            NumbericalStatistics._propUseTimes[E_ItemType.GameProp_3]=data.magicUseCount;
            NumbericalStatistics._propUseTimes[E_ItemType.GameProp_4]=data.extraUseCount;
            SaveDataUtils.GameData.currentReviveCount=savedReviveCount;
            SaveDataUtils.GameData.levelReviveCount=savedLevelReviveCount;
        }
        ready = true;
        starting = false;
        FlowModule.CanShowGuide();
        // Teach_01 owns its own masks and reward introduction. Observe its real blocker.
        yield return null;
        while (TransparentBlock.IsBlock && token == session) yield return null;
        if (token == session && !SaveDataUtils.GameData.customTutorialEnd) ui.TryStartGuide();
        SaveDataUtils.Save();
        while(token==session && !SaveDataUtils.TeachData.IsCompleted("Teach_01")) yield return null;
        if(token==session) EDLHEMMBABM.Instance.ShowLevelStartPops();
    }

    public static void CompleteTutorial()
    {
        if (ready && !SaveDataUtils.GameData.customTutorialEnd) FlowModule.NewPlayerGuideEnd();
    }

    public static void Restart()
    {
        if (starting) return;
        session++;
        ready = false;
        if (UIPropEntry.usingPropType != E_ItemType.None) PropUtils.UsePropOver(UIPropEntry.usingPropType, true);
        UIModule.Instance.ClosePage(UIPageIds.LosePanel);
        UIModule.Instance.ClosePage(UIPageIds.PausePanel);
        MgrUI.Instance.Close("pops/newitempop/NewItemPop", false, false);
        CorePlayUI.Instance.StopMagicAni();
        JEFOMCDAPGK.Instance.MainLevelData.hasEffectiveSave = false;
        MCCIJBJGMCK.UnlockAll();
        StartLevel(CorePlayUI.Instance);
    }

    private static LevelInfo ResultInfo()
    {
        var c = EDLHEMMBABM.Instance;
        int total = c.GetCurLevelTotalItems();
        int remaining = c.GetTotalItems().Count + c.GetCollectAreaList().Count;
        return new LevelInfo { levelTotalTarget = total, levelRemainingTarget = remaining,
            levelAchieveTarget = total - remaining, levelProgress = total > 0 ? (double)(total-remaining)/total : 0 };
    }

    public static void Win()
    {
        if (settled) return;
        settled = true;
        ready = false;
        FlowModule.OpenGameWinPanel(BizzaLevelResultType.Win, ResultInfo());
        SaveDataUtils.Save();
    }

    public static void Lose()
    {
        if (settled) return;
        settled = true;
        ready = false;
        FlowModule.OpenGameLosePanel(BizzaLevelResultType.Fail, LoseReason.Health, ResultInfo());
        SaveDataUtils.Save();
    }

    public static void Revive(bool success)
    {
        if (!success || !settled || !JEFOMCDAPGK.Instance.MainLevelData.isLose) return;
        settled = false;
        ready = true;
        UIModule.Instance.ClosePage(UIPageIds.LosePanel);
        EDLHEMMBABM.Instance.Revive("framework_reward_ad");
        SaveDataUtils.Save();
    }

    public static bool UseProp(int index)
    {
        if (!ready || starting || settled || MCCIJBJGMCK.IsLock() || TransparentBlock.IsBlock) return false;
        var c = EDLHEMMBABM.Instance;
        bool success;
        switch (index)
        {
            case 1: success = c.Undo(false); break;
            case 2: success = c.Shuffle(false); break;
            case 3: success = c.Magic(false); break;
            case 4: success = c.AddOne(false); break;
            default: return false;
        }
        if (!success) MgrGlobalUI.Instance.ShowGlobalText(OJEEJGGLNPC.Instance.GetText("gameplay_undogray_tips"), FFMLGGBCOOO.neutral);
        return success;
    }

    public static E_ItemType MapProp(POJCEPBNNIP type)
    {
        switch(type)
        {
            case POJCEPBNNIP.Undo: return E_ItemType.GameProp_1;
            case POJCEPBNNIP.Shuffle: return E_ItemType.GameProp_2;
            case POJCEPBNNIP.Magic: return E_ItemType.GameProp_3;
            case POJCEPBNNIP.Extra: return E_ItemType.GameProp_4;
            default: return E_ItemType.None;
        }
    }

    public static Vector3 GetPropPosition(POJCEPBNNIP type)
    {
        var ui = CorePlayUI.Instance;
        var button = ui != null ? ui.GetItemBtn(type) : null;
        if (button != null) return button.transform.position;
        return Vector3.zero;
    }

    public static void OpenSettings() => UIModule.Instance.OpenPage(UIPageIds.PausePanel).Forget();
    public static string ReadModel(string key)
    {
        var models = SaveDataUtils.GameData.harvestModels;
        return models != null && models.TryGetValue(key, out var value) ? value : null;
    }
    public static void WriteModel(string key, string value)
    {
        var data = SaveDataUtils.GameData;
        data.harvestModels ??= new Dictionary<string,string>();
        data.harvestModels[key] = value;
        SaveDataUtils.Save();
    }
}
