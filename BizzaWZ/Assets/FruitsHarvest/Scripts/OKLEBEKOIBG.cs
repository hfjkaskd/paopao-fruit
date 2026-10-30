using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>主关卡配置管理：逻辑关 → 实体关映射与实体关配置加载。</summary>
internal class OKLEBEKOIBG
{
	[Serializable]
	private class MainLevelConfigArray
	{
		public int cycleStartLevel;

		public MainLevelConfigData[] array;
	}

	private int cycleStartLevel;

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
			json = "{\"cycleStartLevel\":1,\"array\":" + json + "}";
		}
		MainLevelConfigArray wrapper = JsonUtility.FromJson<MainLevelConfigArray>(json);
		if (wrapper?.array == null || wrapper.array.Length == 0)
		{
			throw new InvalidOperationException("Main level configuration is empty");
		}
		if (wrapper.cycleStartLevel < 1 || wrapper.cycleStartLevel > wrapper.array.Length)
		{
			throw new InvalidOperationException("Main level cycle start is outside the configured sequence");
		}
		for (int i = 0; i < wrapper.array.Length; i++)
		{
			MainLevelConfigData config = wrapper.array[i];
			if (config == null || !int.TryParse(config.LevelNum, out int number) || number != i + 1 || string.IsNullOrWhiteSpace(config.LevelList))
			{
				throw new InvalidOperationException("Main levels must be numbered consecutively from 1");
			}
		}
		cycleStartLevel = wrapper.cycleStartLevel;
		mainLevelConfigList = new List<MainLevelConfigData>(wrapper.array);
	}

	private MainLevelConfigData GetConfigByLevelNum(int levelNum)
	{
		if (mainLevelConfigList == null || mainLevelConfigList.Count == 0 || levelNum < 1)
		{
			return null;
		}
		int maxLevelNum = mainLevelConfigList.Count;
		int effective = levelNum;
		if (levelNum > maxLevelNum)
		{
			int cycleLength = maxLevelNum - cycleStartLevel + 1;
			effective = cycleStartLevel + (levelNum - cycleStartLevel) % cycleLength;
		}
		return mainLevelConfigList[effective - 1];
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

	public bool CanRestoreSnapshot(int levelNum, int attemptIndex, LevelConfig snapshot)
	{
		if (snapshot == null || string.IsNullOrEmpty(snapshot.levelID) || snapshot.levelID != GetEffectiveLevelID(levelNum, attemptIndex))
		{
			return false;
		}
		LevelConfig configured = LoadLevelById(snapshot.levelID);
		return configured != null && !string.IsNullOrEmpty(configured.mapID)
			&& snapshot.mapID == configured.mapID
			&& GameRes.Exists(JEFOMCDAPGK.GetMapBundlePath(configured.mapID));
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
