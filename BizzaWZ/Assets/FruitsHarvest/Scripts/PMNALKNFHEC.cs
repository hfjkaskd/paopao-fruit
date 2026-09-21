using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 消除赞美词条系统（Good/Great/Excellent/Amazing/Unbelievable…）。
/// 规则（依据录屏）：按本局连续消除组数走阶梯，每 5 组一轮；
/// 第 2 轮起走到 Unbelievable 档时改用 Unbelievables 预制体并叠 x{轮数} 数字（录屏 190s“Unbelievable X2”）。
/// 全清时用 Clear，绝境救回用 ClutchSave。同屏最多一条，新词顶掉旧词。
/// </summary>
public class PMNALKNFHEC : global::FOLJNEPEKCA<PMNALKNFHEC>
{
	private class JFCNJKFAEBI
	{
		public IHJFHNCGPKF pool;

		public float animLength;

		public Vector2 cachedAnchoredPos;

		public Vector2 cachedSizeDelta;

		/// <summary>预制体内烘焙的各 Canvas sortingOrder（按层级遍历顺序）。</summary>
		public List<int> bakedOrders;
	}

	private readonly Dictionary<string, JFCNJKFAEBI> pools = new Dictionary<string, JFCNJKFAEBI>();

	private static readonly string[] CyclePaths = new string[]
	{
		"res/local/coreplayeff/encourage/prefab/Good",
		"res/local/coreplayeff/encourage/prefab/Great",
		"res/local/coreplayeff/encourage/prefab/Excellent",
		"res/local/coreplayeff/encourage/prefab/Amazing",
		"res/local/coreplayeff/encourage/prefab/Unbelievable"
	};

	private const string UnbelievablesPath = "res/local/coreplayeff/encourage/prefab/Unbelievables";

	private const string ClearPath = "res/local/coreplayeff/encourage/prefab/Clear";

	private const string ClutchSavePath = "res/local/coreplayeff/encourage/prefab/ClutchSave";

	private const string NumSpriteRoot = "res/local/coreplayeff/encourage/sprite/num/x{0}";

	private Transform effRoot;

	private int sessionGen;

	private GameObject pendingEncourageGo;

	private readonly HashSet<GameObject> manuallyReleased = new HashSet<GameObject>();

	private readonly Dictionary<GameObject, JFCNJKFAEBI> activeObjects = new Dictionary<GameObject, JFCNJKFAEBI>();

	public void Init(Transform InRoot)
	{
		effRoot = InRoot;
		sessionGen++;
	}

	public void OnEliminate(int InElimGroupCount, bool InIsClutchSave, bool InIsAllCleared, Vector3 InEncourageWorldPos, Transform InEdgeEffRoot)
	{
		if (effRoot == null)
		{
			return;
		}
		string path;
		int numIdx = 0;
		if (InIsAllCleared)
		{
			path = ClearPath;
		}
		else if (InIsClutchSave)
		{
			path = ClutchSavePath;
		}
		else
		{
			int idx = (InElimGroupCount - 1) % CyclePaths.Length;
			int cycle = (InElimGroupCount - 1) / CyclePaths.Length;
			if (idx == CyclePaths.Length - 1 && cycle >= 1)
			{
				path = UnbelievablesPath;
				numIdx = cycle + 1;
			}
			else
			{
				path = CyclePaths[idx];
			}
		}
		GameObject go = Show(path, InEncourageWorldPos);
		if (go != null && numIdx > 0)
		{
			ApplyNumSprite(go, numIdx);
		}
	}

	private GameObject Show(string InPrefabPath, Vector3 InWorldPos)
	{
		ReleasePendingEncourage();
		JFCNJKFAEBI entry = CreatePoolEntry(InPrefabPath);
		if (entry == null)
		{
			return null;
		}
		GameObject go = entry.pool.Get(effRoot);
		RectTransform rt = go.transform as RectTransform;
		if (rt != null)
		{
			rt.anchoredPosition = entry.cachedAnchoredPos;
			rt.sizeDelta = entry.cachedSizeDelta;
			rt.localScale = Vector3.one;
		}
		ApplySortingAboveHost(go, entry);
		Animation ani = go.GetComponent<Animation>();
		if (ani != null)
		{
			ani.Rewind();
			ani.Play();
		}
		pendingEncourageGo = go;
		activeObjects[go] = entry;
		manuallyReleased.Remove(go);
		int capturedGen = sessionGen;
		float len = entry.animLength > 0f ? entry.animLength : 1.2f;
		Timer.Instance.Delay(len + 0.05f, delegate
		{
			if (capturedGen != sessionGen || go == null || manuallyReleased.Contains(go))
			{
				manuallyReleased.Remove(go);
				return;
			}
			ReleaseObject(go);
		});
		return go;
	}

	/// <summary>同屏只保留一条：新词出现时立刻回收未播完的旧词。</summary>
	private void ReleasePendingEncourage()
	{
		if (pendingEncourageGo != null && activeObjects.ContainsKey(pendingEncourageGo))
		{
			manuallyReleased.Add(pendingEncourageGo);
			ReleaseObject(pendingEncourageGo);
		}
		pendingEncourageGo = null;
	}

	private void ReleaseObject(GameObject InGo)
	{
		if (InGo == null || !activeObjects.TryGetValue(InGo, out JFCNJKFAEBI entry))
		{
			return;
		}
		activeObjects.Remove(InGo);
		if (pendingEncourageGo == InGo)
		{
			pendingEncourageGo = null;
		}
		entry.pool.Release(InGo);
	}

	/// <summary>Unbelievables 词条上的 x{n} 数字贴图（Num 节点）。</summary>
	private void ApplyNumSprite(GameObject InGo, int InNumIdx)
	{
		Transform numTrans = FindDeepChild(InGo.transform, "Num");
		if (numTrans == null)
		{
			return;
		}
		Image img = numTrans.GetComponent<Image>();
		if (img == null)
		{
			return;
		}
		Sprite sprite = GameRes.LoadSprite(string.Format(NumSpriteRoot, InNumIdx));
		if (sprite != null)
		{
			img.sprite = sprite;
			img.SetNativeSize();
		}
	}

	private static Transform FindDeepChild(Transform InParent, string InName)
	{
		if (InParent.name == InName)
		{
			return InParent;
		}
		for (int i = 0; i < InParent.childCount; i++)
		{
			Transform found = FindDeepChild(InParent.GetChild(i), InName);
			if (found != null)
			{
				return found;
			}
		}
		return null;
	}

	/// <summary>
	/// 词条预制体内烘焙 sortingOrder 约 2–32，可能低于宿主 UI 动态层级；
	/// 按预制体缓存的烘焙值整体平移到宿主 UI 之上（保持内部相对顺序，文档允许统一 offset）。
	/// </summary>
	private void ApplySortingAboveHost(GameObject InGo, JFCNJKFAEBI InEntry)
	{
		BaseUI hostUI = effRoot != null ? effRoot.GetComponentInParent<BaseUI>() : null;
		if (hostUI == null || InEntry.bakedOrders == null)
		{
			return;
		}
		int delta = hostUI.MyOrder + 40;
		Canvas[] canvases = InGo.GetComponentsInChildren<Canvas>(true);
		for (int i = 0; i < canvases.Length && i < InEntry.bakedOrders.Count; i++)
		{
			if (canvases[i].overrideSorting)
			{
				canvases[i].sortingOrder = InEntry.bakedOrders[i] + delta;
			}
		}
	}

	private JFCNJKFAEBI CreatePoolEntry(string InPrefabPath)
	{
		if (pools.TryGetValue(InPrefabPath, out JFCNJKFAEBI cached))
		{
			return cached;
		}
		GameObject prefab = GameRes.Load<GameObject>(InPrefabPath);
		if (prefab == null)
		{
			Debug.LogWarning("[Encourage] 词条预制体缺失: " + InPrefabPath);
			pools[InPrefabPath] = null;
			return null;
		}
		JFCNJKFAEBI newEntry = new JFCNJKFAEBI();
		RectTransform prefabRt = prefab.transform as RectTransform;
		if (prefabRt != null)
		{
			newEntry.cachedAnchoredPos = prefabRt.anchoredPosition;
			newEntry.cachedSizeDelta = prefabRt.sizeDelta;
		}
		Animation ani = prefab.GetComponent<Animation>();
		if (ani != null && ani.clip != null)
		{
			newEntry.animLength = ani.clip.length;
		}
		newEntry.bakedOrders = new List<int>();
		foreach (Canvas canvas in prefab.GetComponentsInChildren<Canvas>(true))
		{
			newEntry.bakedOrders.Add(canvas.sortingOrder);
		}
		newEntry.pool = new IHJFHNCGPKF(prefab, 3, effRoot);
		pools[InPrefabPath] = newEntry;
		return newEntry;
	}

	/// <summary>切局清理：递增会话代际并回收全部激活词条。</summary>
	public void Cleanup()
	{
		sessionGen++;
		ReleaseAllActive();
	}

	public void ReleaseAllActive()
	{
		if (activeObjects.Count == 0)
		{
			pendingEncourageGo = null;
			return;
		}
		List<GameObject> actives = new List<GameObject>(activeObjects.Keys);
		foreach (GameObject go in actives)
		{
			ReleaseObject(go);
		}
		pendingEncourageGo = null;
		manuallyReleased.Clear();
	}
}
