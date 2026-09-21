using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>主关卡配置管理：逻辑关 → 实体关映射与实体关配置加载。</summary>
internal class OKLEBEKOIBG
{
	[Serializable]
	private class MainLevelConfigArray
	{
		public MainLevelConfigData[] array;
	}

	private const int CYCLE_START_INDEX = 210;

	public string assetPath;

	public string folderName = "mainlevel";

	public Func<int> getCurrentLevelIndex;

	public int preDownloadCount;

	private List<MainLevelConfigData> mainLevelConfigList;

	private static Dictionary<string, LevelConfig> levelIdToLevelConfig = new Dictionary<string, LevelConfig>();

	public void Init(TextAsset InTextAsset)
	{
		if (InTextAsset == null)
		{
			Debug.LogError("[OKLEBEKOIBG] MainLevelConfig TextAsset 为空");
			return;
		}
		string json = InTextAsset.text.TrimStart();
		if (json.StartsWith("["))
		{
			json = "{\"array\":" + json + "}";
		}
		MainLevelConfigArray wrapper = JsonUtility.FromJson<MainLevelConfigArray>(json);
		mainLevelConfigList = new List<MainLevelConfigData>(wrapper.array);
	}

	private MainLevelConfigData GetConfigByLevelNum(int levelNum)
	{
		if (mainLevelConfigList == null || mainLevelConfigList.Count == 0)
		{
			return null;
		}
		int maxLevelNum = 0;
		foreach (MainLevelConfigData config in mainLevelConfigList)
		{
			if (int.TryParse(config.LevelNum, out int n) && n > maxLevelNum)
			{
				maxLevelNum = n;
			}
		}
		int effective = levelNum;
		if (levelNum > maxLevelNum)
		{
			int cycleLength = maxLevelNum - CYCLE_START_INDEX + 1;
			if (cycleLength <= 0)
			{
				effective = maxLevelNum;
			}
			else
			{
				effective = CYCLE_START_INDEX + (levelNum - CYCLE_START_INDEX) % cycleLength;
			}
		}
		string key = effective.ToString();
		foreach (MainLevelConfigData config in mainLevelConfigList)
		{
			if (config.LevelNum == key)
			{
				return config;
			}
		}
		return null;
	}

	public string[] GetLevelListByLevelNum(int levelNum)
	{
		MainLevelConfigData config = GetConfigByLevelNum(levelNum);
		if (config == null || string.IsNullOrEmpty(config.LevelList))
		{
			return new string[0];
		}
		return config.LevelList.Split(',');
	}

	private string GetGuaranteedLevel(int levelNum)
	{
		MainLevelConfigData config = GetConfigByLevelNum(levelNum);
		return config != null ? config.Guaranteed_Level : null;
	}

	/// <summary>按尝试次数索引实体关：负值钳0，越界取最后一项。</summary>
	public string GetEffectiveLevelID(int levelNum, int attemptIndex)
	{
		string[] list = GetLevelListByLevelNum(levelNum);
		if (list.Length == 0)
		{
			return null;
		}
		int idx = Mathf.Clamp(attemptIndex, 0, list.Length - 1);
		return list[idx].Trim();
	}

	public LevelConfig GetLevelByAttempt(int levelNum, int attemptIndex)
	{
		string levelId = GetEffectiveLevelID(levelNum, attemptIndex);
		if (string.IsNullOrEmpty(levelId))
		{
			return null;
		}
		return LoadLevelById(levelId);
	}

	private LevelConfig LoadLevelById(string levelId)
	{
		if (levelIdToLevelConfig.TryGetValue(levelId, out LevelConfig cached))
		{
			return cached;
		}
		string groupKey = GetLevelGroupKey(levelId);
		return GetLevelConfigFromAB(groupKey, levelId);
	}

	private bool IsLevelAvailable(string levelId)
	{
		return true;
	}

	private static string GetLevelGroupKey(string levelId)
	{
		if (long.TryParse(levelId, out long id))
		{
			return (id / 100).ToString();
		}
		return levelId;
	}

	public bool CheckLevelFullyAvailable(string levelId)
	{
		return true;
	}

	public bool CheckNextNLevelsAvailable(int startLevelNum, int count)
	{
		return true;
	}

	public LevelConfig GetLevelConfigFromAB(string groupKey, string levelId)
	{
		string path = "res_server/server_levelconfigs/" + folderName + "/" + groupKey + "/LevelConfigs";
		string json = GameRes.LoadText(path);
		if (string.IsNullOrEmpty(json))
		{
			Debug.LogError("[OKLEBEKOIBG] 未找到关卡配置: " + path);
			return null;
		}
		LevelConfigList list = JsonUtility.FromJson<LevelConfigList>(json);
		LevelConfig result = null;
		if (list != null && list.Levels != null)
		{
			foreach (LevelConfig config in list.Levels)
			{
				levelIdToLevelConfig[config.levelID] = config;
				if (config.levelID == levelId)
				{
					result = config;
				}
			}
		}
		return result;
	}

	public void PreDownloadLevels(int startLevelNum, int count)
	{
	}

	public Dictionary<string, HashSet<string>> GetGroupedLevelIds(int startLevelNum, int count)
	{
		var result = new Dictionary<string, HashSet<string>>();
		for (int i = 0; i < count; i++)
		{
			string[] list = GetLevelListByLevelNum(startLevelNum + i);
			foreach (string levelId in list)
			{
				string groupKey = GetLevelGroupKey(levelId.Trim());
				if (!result.TryGetValue(groupKey, out HashSet<string> set))
				{
					set = new HashSet<string>();
					result[groupKey] = set;
				}
				set.Add(levelId.Trim());
			}
		}
		return result;
	}

	private void ExtractAndEnqueueDependencies(string groupKey, Dictionary<string, HashSet<string>> groupToLevelIds)
	{
	}

	public void PreDownloadMapBundles(List<int> mapIDs)
	{
	}

	public void PreDownloadImageBundles(List<int> eleTypes)
	{
	}
}
