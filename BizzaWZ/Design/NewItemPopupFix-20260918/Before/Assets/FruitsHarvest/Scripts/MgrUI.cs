using System.Collections.Generic;
using UnityEngine;

public class MgrUI : MonoBehaviour
{
	private Dictionary<string, BaseUI> ExistDic;

	private List<BaseUI> bottomList;

	private List<BaseUI> midList;

	private List<BaseUI> topList;

	private List<BaseUI> openOrderList;

	private const int BottomLayer = -1000;

	private const int MidLayer = -800;

	private const int TopLayer = -600;

	private const int LayerSpacing = 20;

	private Transform bottom;

	private Transform mid;

	private Transform top;

	private readonly Dictionary<BaseUI, string> pathByUI = new Dictionary<BaseUI, string>();

	public static MgrUI Instance { get; private set; }

	public void Init()
	{
		Instance = this;
		ExistDic = new Dictionary<string, BaseUI>();
		bottomList = new List<BaseUI>();
		midList = new List<BaseUI>();
		topList = new List<BaseUI>();
		openOrderList = new List<BaseUI>();
		bottom = transform.Find("Bottom");
		mid = transform.Find("Mid");
		top = transform.Find("Top");
	}

	public bool IsOpenning(string path)
	{
		return ExistDic != null && ExistDic.TryGetValue(path, out BaseUI ui) && ui != null && ui.IsOpening;
	}

	private Transform GetParentByLayer(PAIEAGDLCBJ layer)
	{
		switch (layer)
		{
		case PAIEAGDLCBJ.Bottom:
			return bottom;
		case PAIEAGDLCBJ.Mid:
			return mid;
		case PAIEAGDLCBJ.Top:
			return top;
		default:
			return bottom;
		}
	}

	private List<BaseUI> GetListByLayer(PAIEAGDLCBJ layer)
	{
		switch (layer)
		{
		case PAIEAGDLCBJ.Bottom:
			return bottomList;
		case PAIEAGDLCBJ.Mid:
			return midList;
		case PAIEAGDLCBJ.Top:
			return topList;
		default:
			return bottomList;
		}
	}

	private BaseUI GetListLast(PAIEAGDLCBJ layer)
	{
		List<BaseUI> list = GetListByLayer(layer);
		return list.Count > 0 ? list[list.Count - 1] : null;
	}

	private int GetOrder(PAIEAGDLCBJ layer)
	{
		int order;
		switch (layer)
		{
		case PAIEAGDLCBJ.Mid:
			order = MidLayer;
			break;
		case PAIEAGDLCBJ.Top:
			order = TopLayer;
			break;
		default:
			order = BottomLayer;
			break;
		}
		List<BaseUI> list = GetListByLayer(layer);
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i] != null)
			{
				int next = list[i].MyOrder + Mathf.Max(list[i].OwnLayerCnt, 0) + LayerSpacing;
				if (next > order)
				{
					order = next;
				}
			}
		}
		return order;
	}

	private void AddToOpenOrderList(BaseUI ui)
	{
		openOrderList.Remove(ui);
		openOrderList.Add(ui);
	}

	public BaseUI GetTopUI()
	{
		for (int i = openOrderList.Count - 1; i >= 0; i--)
		{
			if (openOrderList[i] != null && openOrderList[i].IsOpening)
			{
				return openOrderList[i];
			}
		}
		return null;
	}

	private BaseUI LoadUI(string path)
	{
		GameObject prefab = GameRes.LoadPrefab("res/local/" + path);
		if (prefab == null)
		{
			return null;
		}
		BaseUI prefabUI = prefab.GetComponent<BaseUI>();
		Transform parent = prefabUI != null ? GetParentByLayer(prefabUI.Layer) : bottom;
		GameObject go = Object.Instantiate(prefab, parent, false);
		go.name = prefab.name;
		BaseUI ui = go.GetComponent<BaseUI>();
		if (ui == null)
		{
			Debug.LogError("[MgrUI] 预制体缺少BaseUI组件: " + path);
			Object.Destroy(go);
			return null;
		}
		ExistDic[path] = ui;
		pathByUI[ui] = path;
		return ui;
	}

	public void Preload(string path, bool asyncLoad = false)
	{
		if (ExistDic.ContainsKey(path))
		{
			return;
		}
		BaseUI ui = LoadUI(path);
		if (ui != null)
		{
			ui.gameObject.SetActive(false);
			ui.AfterPreload();
		}
	}

	public void Open(string path, bool InNeedPlaySound = true, bool asyncLoad = false)
	{
		if (!ExistDic.TryGetValue(path, out BaseUI ui) || ui == null)
		{
			ui = LoadUI(path);
		}
		if (ui == null || ui.IsOpening)
		{
			return;
		}
		List<BaseUI> list = GetListByLayer(ui.Layer);
		int order = GetOrder(ui.Layer);
		if (!list.Contains(ui))
		{
			list.Add(ui);
		}
		AddToOpenOrderList(ui);
		if (InNeedPlaySound)
		{
			GameAudio.Play(DLMJOHCOJKN.Play_sfx_ui_panel_common_open);
		}
		ui.transform.SetAsLastSibling();
		ui.BaseUIOpen(order);
	}

	public void DelayOpen(string path, float time, bool lockInput = true, bool asyncLoad = true)
	{
		if (lockInput)
		{
			MCCIJBJGMCK.Lock();
		}
		Timer.Instance.Delay(time, delegate
		{
			if (lockInput)
			{
				MCCIJBJGMCK.UnlockOnce();
			}
			Open(path);
		});
	}

	public void Close(string InPath, bool InNeedDestroy, bool InNeedPlaySound = true)
	{
		if (!ExistDic.TryGetValue(InPath, out BaseUI ui) || ui == null)
		{
			return;
		}
		if (InNeedPlaySound)
		{
			GameAudio.Play(DLMJOHCOJKN.Play_sfx_ui_panel_common_close);
		}
		GetListByLayer(ui.Layer).Remove(ui);
		openOrderList.Remove(ui);
		if (InNeedDestroy)
		{
			ExistDic.Remove(InPath);
			pathByUI.Remove(ui);
		}
		ui.BaseUIClose(InNeedDestroy);
	}

	/// <summary>判断某个Bottom页面是否是（排除指定路径后的）第一个底层页面。</summary>
	public bool JudgeBottomUIFirst(BaseUI InJudgeUI, string InExceptBottomUIPath)
	{
		for (int i = 0; i < bottomList.Count; i++)
		{
			BaseUI ui = bottomList[i];
			if (ui == null)
			{
				continue;
			}
			if (pathByUI.TryGetValue(ui, out string p) && p == InExceptBottomUIPath)
			{
				continue;
			}
			return ui == InJudgeUI;
		}
		return false;
	}
}
