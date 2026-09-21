using System.Collections.Generic;
using UnityEngine;

/// <summary>元素图片配置管理：eleID → colorType/sizeX/sizeY。</summary>
public class IIMPLNFEFGF : global::FOLJNEPEKCA<IIMPLNFEFGF>
{
	private Dictionary<int, SingleEleImageConfig> eleImageDict;

	public void Init(TextAsset jsonText)
	{
		eleImageDict = new Dictionary<int, SingleEleImageConfig>();
		if (jsonText == null)
		{
			jsonText = GameRes.LoadTextAsset("res_server/server_configs/AllEleImageConfig");
		}
		if (jsonText == null)
		{
			Debug.LogError("[IIMPLNFEFGF] 未找到 AllEleImageConfig");
			return;
		}
		AllEleImageConfig config = JsonUtility.FromJson<AllEleImageConfig>(jsonText.text);
		if (config != null && config.eleImageConfigs != null)
		{
			foreach (SingleEleImageConfig c in config.eleImageConfigs)
			{
				eleImageDict[c.eleID] = c;
			}
		}
	}

	private void EnsureInit()
	{
		if (eleImageDict == null)
		{
			Init(null);
		}
	}

	public SingleEleImageConfig GetConfig(int eleId)
	{
		EnsureInit();
		eleImageDict.TryGetValue(eleId, out SingleEleImageConfig config);
		return config;
	}

	public bool TryGetConfig(int eleId, out SingleEleImageConfig config)
	{
		EnsureInit();
		return eleImageDict.TryGetValue(eleId, out config);
	}

	public Dictionary<int, SingleEleImageConfig> GetAllDict()
	{
		EnsureInit();
		return eleImageDict;
	}
}
