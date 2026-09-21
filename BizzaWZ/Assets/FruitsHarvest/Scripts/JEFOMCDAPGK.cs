using System.Collections.Generic;
using CorePlay;

/// <summary>关卡进度模型：当前主关索引、胜场数、单关运行数据与快照。</summary>
public class JEFOMCDAPGK : global::IAJDGNOGFGO<Data, JEFOMCDAPGK>
{
	public const string DEFAULT_MAP_ID = "1018";

	private const int HARD_LEVEL_FIRST_SHOW = 10;

	private static readonly Dictionary<string, string> mapBundlePathCache = new Dictionary<string, string>();

	public Data Data => data;

	public int WinCount => data.mainLevelWinCnt;

	/// <summary>当前逻辑关（1 起始）。</summary>
	public int CurMainLevelIndex => data.mainLevelWinCnt + 1;

	public FPJPEHKNPLD CurMainLevelType => FPJPEHKNPLD.Normal;

	public LevelData MainLevelData
	{
		get
		{
			if (data.mainLevelData == null)
			{
				data.mainLevelData = new LevelData();
			}
			return data.mainLevelData;
		}
	}

	protected override string GetKey()
	{
		return "CorePlayModel";
	}

	public override void Init()
	{
		base.Init();
		if (data.hadShownNewItemPop == null)
		{
			data.hadShownNewItemPop = new List<int>();
		}
	}

	protected override void AfterFirstInitData()
	{
		data.hasEnterLevel = false;
		data.mainLevelWinCnt = 0;
		data.mainLevelData = new LevelData();
		data.hadShownNewItemPop = new List<int>();
	}

	public void LevelWin()
	{
		MainLevelData.isWin = true;
		data.mainLevelWinCnt++;
		data.isLevelFinished = true;
		// 为下一关重置单关数据
		data.mainLevelData = new LevelData();
		SaveData();
	}

	public void LevelLose()
	{
		MainLevelData.isLose = true;
		MainLevelData.attempCount++;
		SaveData();
	}

	public void SetWinCount(int value)
	{
		data.mainLevelWinCnt = value;
		SaveData();
	}

	public void RecordItemUse(string itemName, int totalItems)
	{
		string record = string.IsNullOrEmpty(MainLevelData.itemUseRecord) ? "" : MainLevelData.itemUseRecord + ",";
		MainLevelData.itemUseRecord = record + itemName + "@" + totalItems;
		SaveData();
	}

	public void RecordRevive(string InReviveReason, int totalItems)
	{
		string record = string.IsNullOrEmpty(MainLevelData.reviveRecord) ? "" : MainLevelData.reviveRecord + ",";
		MainLevelData.reviveRecord = record + InReviveReason + "@" + totalItems;
		SaveData();
	}

	public void SetShowWillFillTipState(bool InState)
	{
		data.hasShownWillFillTip = InState;
		SaveData();
	}

	public void SetShowFreeReviveState(bool InState)
	{
		data.hasShowFreeRevive = InState;
		SaveData();
	}

	public bool HasShownNewItemPop(POJCEPBNNIP InItemType)
	{
		return data.hadShownNewItemPop != null && data.hadShownNewItemPop.Contains((int)InItemType);
	}

	public void MarkNewItemPopShown(POJCEPBNNIP InItemType)
	{
		if (data.hadShownNewItemPop == null)
		{
			data.hadShownNewItemPop = new List<int>();
		}
		if (!data.hadShownNewItemPop.Contains((int)InItemType))
		{
			data.hadShownNewItemPop.Add((int)InItemType);
		}
		if (data.pendingNewItemPop == (int)InItemType) data.pendingNewItemPop = 0;
		SaveData();
	}

	public bool TryReserveNewItemPop(POJCEPBNNIP candidate, out POJCEPBNNIP selected)
	{
		if (data.pendingNewItemPop != 0 && !data.hadShownNewItemPop.Contains(data.pendingNewItemPop))
		{
			selected = (POJCEPBNNIP)data.pendingNewItemPop;
			return true;
		}
		selected = candidate;
		if (HasShownNewItemPop(candidate)) return false;
		data.pendingNewItemPop = (int)candidate;
		SaveData();
		return true;
	}

	public void LogLevelStart()
	{
		data.hasEnterLevel = true;
		data.isLevelFinished = false;
		MainLevelData.timerRunning = true;
		SaveData();
	}

	public void LogLevelEnd(string endReason, bool isWin)
	{
		MainLevelData.timerRunning = false;
		data.isLevelFinished = true;
		SaveData();
	}

	public static string GetMapBundlePath(string InMapID)
	{
		if (string.IsNullOrEmpty(InMapID))
		{
			InMapID = DEFAULT_MAP_ID;
		}
		if (!mapBundlePathCache.TryGetValue(InMapID, out string path))
		{
			path = "res_server/server_maps/map" + InMapID + "/Map" + InMapID;
			mapBundlePathCache[InMapID] = path;
		}
		return path;
	}
}
