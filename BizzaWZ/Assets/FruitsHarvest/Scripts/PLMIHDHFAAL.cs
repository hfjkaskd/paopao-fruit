using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>配置总管理：主关卡表与实体关配置入口。</summary>
public class PLMIHDHFAAL : global::FOLJNEPEKCA<PLMIHDHFAAL>
{
	private OKLEBEKOIBG MainLevel;

	public void Init(TextAsset InMainTextAsset)
	{
		if (InMainTextAsset == null)
		{
			InMainTextAsset = GameRes.LoadTextAsset("res_server/server_configs/MainLevelConfig");
		}
		MainLevel = new OKLEBEKOIBG
		{
			assetPath = "res_server/server_levelconfigs",
			folderName = "mainlevel",
			getCurrentLevelIndex = () => JEFOMCDAPGK.Instance.CurMainLevelIndex,
			preDownloadCount = 10
		};
		MainLevel.Init(InMainTextAsset);
	}

	public LevelConfig GetMainLevelConfig()
	{
		int level = JEFOMCDAPGK.Instance.CurMainLevelIndex;
		return HarvestDifficulty.Apply(MainLevel.GetLevelByAttempt(level, JEFOMCDAPGK.Instance.MainLevelData.attempCount), level);
	}

	public string GetCurMainLevelID()
	{
		return MainLevel.GetEffectiveLevelID(JEFOMCDAPGK.Instance.CurMainLevelIndex, JEFOMCDAPGK.Instance.MainLevelData.attempCount);
	}

	public bool GetLevelIDResourcesDownloaded(string InLevelID)
	{
		return true;
	}

	public void PreDownloadLevels(int startLevelNum, int count)
	{
	}

	public void PreDownloadMapBundles(List<int> mapIDs, Action onComplete = null)
	{
		onComplete?.Invoke();
	}

	public void PreDownloadImageBundles(List<int> eleTypes, Action onComplete = null)
	{
		onComplete?.Invoke();
	}

	public string[] GetLevelListByLevelNum(int levelNum)
	{
		return MainLevel.GetLevelListByLevelNum(levelNum);
	}

	public bool CheckNextNLevelsAvailable(int startLevelNum, int count)
	{
		return true;
	}

	public LevelConfig GetLevelConfig(string groupKey, string levelId)
	{
		return MainLevel.GetLevelConfigFromAB(groupKey, levelId);
	}

	public Dictionary<string, HashSet<string>> GetGroupedLevelIds(int startLevelNum, int count)
	{
		return MainLevel.GetGroupedLevelIds(startLevelNum, count);
	}
}
