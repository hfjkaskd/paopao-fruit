using System.Collections.Generic;

using Project.DictData;
using UnityEngine;

/// <summary>多语言文本管理：从 res/local/configs/Text.json 加载 174 个 key 的 32 语言文本。</summary>
public class OJEEJGGLNPC : global::FOLJNEPEKCA<OJEEJGGLNPC>
{
	private List<TextConfig> textConfigList;
    private EDMAJFIKJLE loadedLanguage;

	public readonly Dictionary<string, Dictionary<EDMAJFIKJLE, string>> allLanDict = new Dictionary<string, Dictionary<EDMAJFIKJLE, string>>();

	public readonly Dictionary<string, string> curLanDict = new Dictionary<string, string>();

	public void LoadConfig()
	{
		string json = GameRes.LoadText("res/local/configs/Text");
		if (string.IsNullOrEmpty(json))
		{
			Debug.LogError("[OJEEJGGLNPC] 未找到本地化配置 Text.json");
			return;
		}
		json = json.TrimStart();
		if (json.StartsWith("["))
		{
			json = "{\"array\":" + json + "}";
		}
		TextConfigArray wrapper = JsonUtility.FromJson<TextConfigArray>(json);
		textConfigList = new List<TextConfig>(wrapper.array);

		allLanDict.Clear();
        foreach (TextConfig config in textConfigList) {
            if (string.IsNullOrEmpty(config.key)) continue;
            var lanDict = new Dictionary<EDMAJFIKJLE, string>();
            lanDict[EDMAJFIKJLE.en] = config.en;
            lanDict[EDMAJFIKJLE.ar] = config.ar;
            lanDict[EDMAJFIKJLE.de] = config.de;
            lanDict[EDMAJFIKJLE.es] = config.es;
            lanDict[EDMAJFIKJLE.fr] = config.fr;
            lanDict[EDMAJFIKJLE.hu] = config.hu;
            lanDict[EDMAJFIKJLE.id] = config.id;
            lanDict[EDMAJFIKJLE.it] = config.it;
            lanDict[EDMAJFIKJLE.ja] = config.ja;
            lanDict[EDMAJFIKJLE.ko] = config.ko;
            lanDict[EDMAJFIKJLE.pt] = config.pt;
            lanDict[EDMAJFIKJLE.ro] = config.ro;
            lanDict[EDMAJFIKJLE.ru] = config.ru;
            lanDict[EDMAJFIKJLE.th] = config.th;
            lanDict[EDMAJFIKJLE.vi] = config.vi;
            lanDict[EDMAJFIKJLE.ms] = config.ms;
            lanDict[EDMAJFIKJLE.fil] = config.fil;
            lanDict[EDMAJFIKJLE.sv] = config.sv;
            lanDict[EDMAJFIKJLE.da] = config.da;
            lanDict[EDMAJFIKJLE.fi] = config.fi;
            lanDict[EDMAJFIKJLE.no] = config.no;
            lanDict[EDMAJFIKJLE.pl] = config.pl;
            lanDict[EDMAJFIKJLE.hi] = config.hi;
            lanDict[EDMAJFIKJLE.nl] = config.nl;
            lanDict[EDMAJFIKJLE.fa] = config.fa;
            lanDict[EDMAJFIKJLE.tr] = config.tr;
            lanDict[EDMAJFIKJLE.el] = config.el;
            lanDict[EDMAJFIKJLE.cs] = config.cs;
            lanDict[EDMAJFIKJLE.bg] = config.bg;
            lanDict[EDMAJFIKJLE.he] = config.he;
            lanDict[EDMAJFIKJLE.zh_cn] = config.zh_cn;
            lanDict[EDMAJFIKJLE.zh_tw] = config.zh_tw;
            allLanDict[config.key] = lanDict;
        }
		ClearTextConfigList();
		LoadLocalText();
	}

	private void ClearTextConfigList()
	{
		textConfigList = null;
	}

	public void LoadLocalText()
	{
		curLanDict.Clear();
		EDMAJFIKJLE cur = HarvestLocalization.Current;
        loadedLanguage=cur;
		foreach (var kv in allLanDict)
		{
			string value = null;
			if (!kv.Value.TryGetValue(cur, out value) || string.IsNullOrEmpty(value))
			{
				kv.Value.TryGetValue(EDMAJFIKJLE.en, out value);
			}
			curLanDict[kv.Key] = value;
		}
	}

	public void ReLoadLocalText()
	{
		LoadLocalText();
	}

	public void AddKey(string key, Dictionary<EDMAJFIKJLE, string> dic)
	{
		allLanDict[key] = dic;
	}

	/// <summary>取当前语言文本；找不到时回退 key 本身。</summary>
	public string GetText(string key)
	{
        if(loadedLanguage!=HarvestLocalization.Current) LoadLocalText();
		if (curLanDict.TryGetValue(key, out string value) && !string.IsNullOrEmpty(value))
		{
			return value;
		}
		return key;
	}

	public static EDMAJFIKJLE GetDefaultLocalizationType()
	{
		switch (Application.systemLanguage)
		{
		case SystemLanguage.Arabic:
			return EDMAJFIKJLE.ar;
		case SystemLanguage.German:
			return EDMAJFIKJLE.de;
		case SystemLanguage.Spanish:
			return EDMAJFIKJLE.es;
		case SystemLanguage.French:
			return EDMAJFIKJLE.fr;
		case SystemLanguage.Hungarian:
			return EDMAJFIKJLE.hu;
		case SystemLanguage.Indonesian:
			return EDMAJFIKJLE.id;
		case SystemLanguage.Italian:
			return EDMAJFIKJLE.it;
		case SystemLanguage.Japanese:
			return EDMAJFIKJLE.ja;
		case SystemLanguage.Korean:
			return EDMAJFIKJLE.ko;
		case SystemLanguage.Portuguese:
			return EDMAJFIKJLE.pt;
		case SystemLanguage.Romanian:
			return EDMAJFIKJLE.ro;
		case SystemLanguage.Russian:
			return EDMAJFIKJLE.ru;
		case SystemLanguage.Thai:
			return EDMAJFIKJLE.th;
		case SystemLanguage.Vietnamese:
			return EDMAJFIKJLE.vi;
		case SystemLanguage.Swedish:
			return EDMAJFIKJLE.sv;
		case SystemLanguage.Danish:
			return EDMAJFIKJLE.da;
		case SystemLanguage.Finnish:
			return EDMAJFIKJLE.fi;
		case SystemLanguage.Norwegian:
			return EDMAJFIKJLE.no;
		case SystemLanguage.Polish:
			return EDMAJFIKJLE.pl;
		case SystemLanguage.Dutch:
			return EDMAJFIKJLE.nl;
		case SystemLanguage.Turkish:
			return EDMAJFIKJLE.tr;
		case SystemLanguage.Greek:
			return EDMAJFIKJLE.el;
		case SystemLanguage.Czech:
			return EDMAJFIKJLE.cs;
		case SystemLanguage.Bulgarian:
			return EDMAJFIKJLE.bg;
		case SystemLanguage.Hebrew:
			return EDMAJFIKJLE.he;
		case SystemLanguage.ChineseSimplified:
			return EDMAJFIKJLE.zh_cn;
		case SystemLanguage.ChineseTraditional:
			return EDMAJFIKJLE.zh_tw;
		case SystemLanguage.Chinese:
			return EDMAJFIKJLE.zh_cn;
		default:
			return EDMAJFIKJLE.en;
		}
	}
}
