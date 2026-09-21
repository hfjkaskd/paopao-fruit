using System;
using System.Collections.Generic;
using Orange;
using UnityEngine;
using UnityEngine.UI;

/// <summary>场上可交互对象（水果）：点击判定、遮挡率、入槽飞行、消除表现。</summary>
public class CollectItem : MonoBehaviour
{
	[SerializeField]
	private Image m_CollectImage;

	[SerializeField]
	private Image m_CollectEFImage;

	[SerializeField]
	private Image m_CollectEF2Image;

	[SerializeField]
	private Animation m_Anim;

	[SerializeField]
	private Transform m_ScaleTrans;

	[SerializeField]
	private Image m_GlowImage;

	[NonSerialized]
	public int Id;

	[NonSerialized]
	public int Type;

	[NonSerialized]
	public LECIONHKEJL Status;

	[NonSerialized]
	public int Priority;

	[NonSerialized]
	public float OcclusionRatio;

	[NonSerialized]
	public int GridMinCol;

	[NonSerialized]
	public int GridMaxCol;

	[NonSerialized]
	public int GridMinRow;

	[NonSerialized]
	public int GridMaxRow;

	[NonSerialized]
	public int VisibleCellCount;

	[NonSerialized]
	public int TotalCoveredCells;

	[NonSerialized]
	public string DefaultIdleAnim;

	[NonSerialized]
	public bool FromGameToBasket;

	[NonSerialized]
	public bool IsMagicPending;

	[NonSerialized]
	public float? CachedEffectSizeX;

	[NonSerialized]
	public float? CachedEffectSizeY;

	public static float CLICKABLE_OCCLUSION_THRESHOLD = 0.8f;

	public static float CENTER_WEIGHT = 3f;

	public static float CENTER_EXTENT_RATIO = 0.25f;

	public static bool s_DebugLogPreciseOcclusion;

	[NonSerialized]
	private Vector2[] cachedOBBVerts0 = new Vector2[4];

	[NonSerialized]
	private Vector2[] cachedOBBVerts1 = new Vector2[4];

	private Transform savedParent;

	private int savedSiblingIndex;

	private uint moveTweenId;

	private uint scaleTweenId;

	private bool isPointerDown;

	private bool cachedDownCanInteract;

	private static readonly Dictionary<int, string> abPathCache = new Dictionary<int, string>();

	private static readonly Dictionary<int, string> nameCache = new Dictionary<int, string>();

	private static readonly Dictionary<int, Sprite> spriteCache = new Dictionary<int, Sprite>();

	public static Material GlowSharedMat;

	private static Dictionary<int, SingleEleImageConfig> singleEleImageDict;

	public static bool FruitClickArea;

	private float moveProgress;

	public static bool NeedShowFruitsGoalForMatched = true;

	[Serializable]
	private class ItemIdNameArray
	{
		public ItemIdNameConfig[] array;
	}

	private static List<ItemIdNameConfig> itemIdNameList;

	public RectTransform CollectImageRect => m_CollectImage != null ? m_CollectImage.rectTransform : null;

	public bool IsMoving => moveTweenId != 0;

	public static Dictionary<int, SingleEleImageConfig> SingleEleImageDict
	{
		get
		{
			if (singleEleImageDict == null)
			{
				singleEleImageDict = IIMPLNFEFGF.Instance.GetAllDict();
			}
			return singleEleImageDict;
		}
	}

	private bool CanInteract()
	{
		if (Status != LECIONHKEJL.OnField)
		{
			return false;
		}
		List<CollectItem> candidates = EDLHEMMBABM.Instance.GetHigherPriorityCandidates(this);
		float ratio = CalculatePreciseOcclusionRatio(candidates);
		OcclusionRatio = ratio;
		return ratio <= CLICKABLE_OCCLUSION_THRESHOLD;
	}

	private static void EnsureNameConfig()
	{
		if (itemIdNameList != null)
		{
			return;
		}
		itemIdNameList = new List<ItemIdNameConfig>();
		string json = GameRes.LoadText("res_server/server_configs/ItemIdNameConfig");
		if (string.IsNullOrEmpty(json))
		{
			return;
		}
		json = json.TrimStart();
		if (json.StartsWith("["))
		{
			json = "{\"array\":" + json + "}";
		}
		ItemIdNameArray wrapper = JsonUtility.FromJson<ItemIdNameArray>(json);
		if (wrapper != null && wrapper.array != null)
		{
			itemIdNameList.AddRange(wrapper.array);
		}
	}

	private static (string, string) GetPath(int eleType)
	{
		return ("res_server/server_images/fruit", GetEleName(eleType));
	}

	public static string GetEleImagePath(int eleType)
	{
		return "res_server/server_images/fruit/" + GetEleName(eleType);
	}

	private static string GetEleName(int eleType)
	{
		if (nameCache.TryGetValue(eleType, out string cached))
		{
			return cached;
		}
		EnsureNameConfig();
		string name = "Fruit_" + eleType;
		string key = eleType.ToString();
		foreach (ItemIdNameConfig config in itemIdNameList)
		{
			if (config.item_id == key)
			{
				name = config.item_name;
				break;
			}
		}
		nameCache[eleType] = name;
		return name;
	}

	private static string GetDefaultIdleAnim(int eleType)
	{
		return "anim_Fruits_Idle" + (eleType % 3);
	}

	public static bool TryGetEleSize(int eleType, out float sizeX, out float sizeY)
	{
		if (SingleEleImageDict != null && SingleEleImageDict.TryGetValue(eleType, out SingleEleImageConfig config))
		{
			sizeX = config.sizeX;
			sizeY = config.sizeY;
			return true;
		}
		sizeX = 140f;
		sizeY = 140f;
		return false;
	}

	/// <summary>进入收集槽后的显示缩放：把对象等比缩放到槽位参考尺寸内。</summary>
	public float GetCollectAreaScale()
	{
		TryGetEleSize(Type, out float sizeX, out float sizeY);
		float maxSide = Mathf.Max(sizeX, sizeY);
		float refSize = CorePlay.CorePlayUI.collectAreaRefSize;
		if (maxSide <= 0f || refSize <= 0f)
		{
			return 1f;
		}
		return Mathf.Min(1f, refSize / maxSide);
	}

	public void SetScale(float scale)
	{
		Transform t = m_ScaleTrans != null ? m_ScaleTrans : transform;
		t.localScale = Vector3.one * scale;
	}

	public void Init(int eleType, int tileId)
	{
		Id = tileId;
		SetType(eleType);
		Status = LECIONHKEJL.Default;
		Priority = 0;
		OcclusionRatio = 0f;
		FromGameToBasket = false;
		IsMagicPending = false;
		isPointerDown = false;
		cachedDownCanInteract = false;
		SetScale(1f);
		savedParent = null;
		BindClick();
	}

	public void SetType(int eleType)
	{
		Type = eleType;
		Sprite sprite;
		try
		{
			sprite = GetItemSprite(eleType);
		}
		catch (Exception e)
		{
			Debug.LogError($"[CollectItem] GetItemSprite({eleType}) 异常: {e}");
			sprite = null;
		}
		if (m_CollectImage != null)
		{
			m_CollectImage.sprite = sprite;
			TryGetEleSize(eleType, out float sizeX, out float sizeY);
			m_CollectImage.rectTransform.sizeDelta = new Vector2(sizeX, sizeY);
			CachedEffectSizeX = sizeX;
			CachedEffectSizeY = sizeY;
		}
		if (m_CollectEFImage != null)
		{
			m_CollectEFImage.sprite = sprite;
		}
		if (m_CollectEF2Image != null)
		{
			m_CollectEF2Image.sprite = sprite;
		}
		if (m_GlowImage != null)
		{
			m_GlowImage.sprite = sprite;
		}
		DefaultIdleAnim = GetDefaultIdleAnim(eleType);
	}

	public static Sprite GetItemSprite(int InEleType)
	{
		if (spriteCache.TryGetValue(InEleType, out Sprite cached) && cached != null)
		{
			return cached;
		}
		Sprite sprite = GameRes.LoadSprite(GetEleImagePath(InEleType));
		spriteCache[InEleType] = sprite;
		return sprite;
	}

	public static void ClearSpriteCache()
	{
		spriteCache.Clear();
	}

	public void BindClick()
	{
		InputMono input = MCCIJBJGMCK.Get(gameObject);
		input.playClickSound = false;
		input.onClick = OnClick;
		input.onDown = OnPointerDown;
		input.onUp = OnPointerUp;
	}

	public void UnBindClick()
	{
		InputMono input = gameObject.GetComponent<InputMono>();
		if (input != null)
		{
			input.onClick = null;
			input.onDown = null;
			input.onUp = null;
		}
	}

	private void OnClick(GameObject InGo)
	{
		if (!cachedDownCanInteract || Status != LECIONHKEJL.OnField)
		{
			return;
		}
		cachedDownCanInteract = false;
		GameAudio.Play(DLMJOHCOJKN.Play_sfx_ui_button_game_fruit);
		EDLHEMMBABM.Instance.OnClick(this);
	}

	private void OnPointerDown(GameObject InGo)
	{
		isPointerDown = true;
		cachedDownCanInteract = CanInteract();
		if (cachedDownCanInteract)
		{
			PlayAnim("anim_Fruits_Pitch");
		}
	}

	private void OnPointerUp(GameObject InGo)
	{
		if (!isPointerDown)
		{
			return;
		}
		isPointerDown = false;
		if (Status == LECIONHKEJL.OnField)
		{
			PlayAnim("anim_Fruits_PitchOff");
			PlayQueuedAnim(DefaultIdleAnim);
		}
	}

	public void ComputeGridBounds(RectTransform bgTrans, float gridCellSize, Vector2 gridOrigin, int gridCols, int gridRows, float? effectSizeX = null, float? effectSizeY = null)
	{
		ComputeGridBoundsAt(bgTrans, gridCellSize, gridOrigin, gridCols, gridRows, transform.position, effectSizeX, effectSizeY);
	}

	public void ComputeGridBoundsAt(RectTransform bgTrans, float gridCellSize, Vector2 gridOrigin, int gridCols, int gridRows, Vector3 worldPos, float? effectSizeX = null, float? effectSizeY = null)
	{
		if (bgTrans == null || gridCellSize <= 0f)
		{
			return;
		}
		Vector2 local = bgTrans.InverseTransformPoint(worldPos);
		float halfX = (effectSizeX ?? CachedEffectSizeX ?? 100f) * 0.5f;
		float halfY = (effectSizeY ?? CachedEffectSizeY ?? 100f) * 0.5f;
		GridMinCol = Mathf.FloorToInt((local.x - halfX - gridOrigin.x) / gridCellSize);
		GridMaxCol = Mathf.FloorToInt((local.x + halfX - gridOrigin.x) / gridCellSize);
		GridMinRow = Mathf.FloorToInt((local.y - halfY - gridOrigin.y) / gridCellSize);
		GridMaxRow = Mathf.FloorToInt((local.y + halfY - gridOrigin.y) / gridCellSize);
		TotalCoveredCells = Mathf.Max(0, (GridMaxCol - GridMinCol + 1) * (GridMaxRow - GridMinRow + 1));
	}

	public void UpdateOcclusionRatio()
	{
		List<CollectItem> candidates = EDLHEMMBABM.Instance.GetHigherPriorityCandidates(this);
		OcclusionRatio = CalculatePreciseOcclusionRatio(candidates);
	}

	public DHNOEMBJBIL.ILLJPIBKBGF BuildOBB(RectTransform bgTrans)
	{
		RectTransform rect = CollectImageRect != null ? CollectImageRect : transform as RectTransform;
		Vector3[] worldCorners = new Vector3[4];
		rect.GetWorldCorners(worldCorners);
		Vector2[] corners = new Vector2[4];
		if (bgTrans != null)
		{
			for (int i = 0; i < 4; i++)
			{
				corners[i] = bgTrans.InverseTransformPoint(worldCorners[i]);
			}
		}
		else
		{
			for (int i = 0; i < 4; i++)
			{
				corners[i] = worldCorners[i];
			}
		}
		return DHNOEMBJBIL.BuildOBBFromCorners(corners);
	}

	/// <summary>加权采样遮挡率：中心 25% 区域权重 3，其余权重 1。</summary>
	public float CalculatePreciseOcclusionRatio(List<CollectItem> candidates)
	{
		if (candidates == null || candidates.Count == 0)
		{
			return 0f;
		}
		RectTransform bg = CorePlay.CorePlayUI.Instance != null ? CorePlay.CorePlayUI.Instance.m_BgTrans : null;
		DHNOEMBJBIL.ILLJPIBKBGF selfOBB = BuildOBB(bg);
		DHNOEMBJBIL.ILLJPIBKBGF centerOBB = selfOBB.CreateCenterOBB(CENTER_EXTENT_RATIO);
		var candidateOBBs = new List<DHNOEMBJBIL.ILLJPIBKBGF>(candidates.Count);
		foreach (CollectItem c in candidates)
		{
			if (c != null)
			{
				candidateOBBs.Add(c.BuildOBB(bg));
			}
		}
		const int SAMPLES = 12;
		float cos = Mathf.Cos(selfOBB.angleRad);
		float sin = Mathf.Sin(selfOBB.angleRad);
		Vector2 ax = new Vector2(cos, sin);
		Vector2 ay = new Vector2(-sin, cos);
		float totalWeight = 0f;
		float coveredWeight = 0f;
		for (int iy = 0; iy < SAMPLES; iy++)
		{
			for (int ix = 0; ix < SAMPLES; ix++)
			{
				float u = (ix + 0.5f) / SAMPLES * 2f - 1f;
				float v = (iy + 0.5f) / SAMPLES * 2f - 1f;
				Vector2 p = selfOBB.center + ax * (u * selfOBB.halfExtents.x) + ay * (v * selfOBB.halfExtents.y);
				bool inCenter = PointInOBB(p, centerOBB);
				float w = inCenter ? CENTER_WEIGHT : 1f;
				totalWeight += w;
				for (int k = 0; k < candidateOBBs.Count; k++)
				{
					if (PointInOBB(p, candidateOBBs[k]))
					{
						coveredWeight += w;
						break;
					}
				}
			}
		}
		float ratio = totalWeight > 0f ? coveredWeight / totalWeight : 0f;
		if (s_DebugLogPreciseOcclusion)
		{
			Debug.Log($"[CollectItem] {name} occlusion={ratio:F3} candidates={candidates.Count}");
		}
		return ratio;
	}

	private static bool PointInOBB(Vector2 p, DHNOEMBJBIL.ILLJPIBKBGF obb)
	{
		Vector2 d = p - obb.center;
		float cos = Mathf.Cos(obb.angleRad);
		float sin = Mathf.Sin(obb.angleRad);
		float localX = d.x * cos + d.y * sin;
		float localY = -d.x * sin + d.y * cos;
		return Mathf.Abs(localX) <= obb.halfExtents.x && Mathf.Abs(localY) <= obb.halfExtents.y;
	}

	/// <summary>入槽落位时播放 goal 表现，返回其时长。</summary>
	private float PlayGoalIfEntering()
	{
		if (!FromGameToBasket)
		{
			return 0f;
		}
		FromGameToBasket = false;
		PlayAnim("anim_Fruits_goal");
		return GetAnimLength("anim_Fruits_goal");
	}

	public void TriggerLogicMatched(float delay, int generation)
	{
		HandleLogicMatched(delay, generation);
	}

	/// <summary>逻辑匹配后的表现：goalFor3 → 延迟 → ReadyForCollect → 驱动 pending 流程。</summary>
	private void HandleLogicMatched(float delay, int generation)
	{
		if (NeedShowFruitsGoalForMatched)
		{
			PlayAnim("anim_Fruits_goalFor3");
		}
		Timer.Instance.Delay(delay, delegate
		{
			if (this == null || generation != EDLHEMMBABM.Instance.GetGeneration())
			{
				return;
			}
			if (Status == LECIONHKEJL.LogicMatched)
			{
				Status = LECIONHKEJL.ReadyForCollect;
				EDLHEMMBABM.Instance.ProcessPendingGroups();
			}
		});
	}

	/// <summary>飞入收集槽：贝塞尔弧线 + 缩放。toPos 为父节点内局部坐标。</summary>
	public void FlyBetweenAreas(Vector3 toPos, float duration, float targetScale, AnimationCurve curve, float arcHeight, Action onComplete, int generation = 0)
	{
		KillMove();
		Vector3 start = transform.localPosition;
		Vector3 end = toPos;
		Vector3 mid = (start + end) * 0.5f + Vector3.up * arcHeight;
		Transform scaleTrans = m_ScaleTrans != null ? m_ScaleTrans : transform;
		float startScale = scaleTrans.localScale.x;
		moveProgress = 0f;
		moveTweenId = DGNMMHCBFMI.DoNum(1f, duration, () => moveProgress, x =>
		{
			moveProgress = x;
			if (this == null)
			{
				return;
			}
			float t = x;
			Vector3 a = Vector3.Lerp(start, mid, t);
			Vector3 b = Vector3.Lerp(mid, end, t);
			transform.localPosition = Vector3.Lerp(a, b, t);
			scaleTrans.localScale = Vector3.one * Mathf.Lerp(startScale, targetScale, t);
		}, delegate
		{
			if (this == null)
			{
				return;
			}
			moveTweenId = 0;
			transform.localPosition = end;
			scaleTrans.localScale = Vector3.one * targetScale;
			GameAudio.Play(DLMJOHCOJKN.Play_sfx_anim_game_fruit_land);
			if (Status == LECIONHKEJL.LogicMatched)
			{
				EDLHEMMBABM.Instance.OnMatchedItemLanded(this);
			}
			else
			{
				PlayGoalIfEntering();
			}
			onComplete?.Invoke();
		}, curve);
	}

	/// <summary>槽内重排移动。</summary>
	public void MoveInSameArea(Vector3 toPos, float duration, AnimationCurve curve, int generation = 0, Action onComplete = null)
	{
		KillMove();
		Vector3 start = transform.localPosition;
		moveProgress = 0f;
		moveTweenId = DGNMMHCBFMI.DoNum(1f, duration, () => moveProgress, x =>
		{
			moveProgress = x;
			if (this != null)
			{
				transform.localPosition = Vector3.Lerp(start, toPos, x);
			}
		}, delegate
		{
			if (this != null)
			{
				moveTweenId = 0;
				transform.localPosition = toPos;
				// A triple can form while an earlier fruit is still reordering.
				// Reordering must notify the same landing gate as entering the basket.
				if (generation == EDLHEMMBABM.Instance.GetGeneration() && Status == LECIONHKEJL.LogicMatched)
				{
#if UNITY_EDITOR
					Debug.Log("[HarvestMatch] Reorder landed: " + Id + " at " + Time.realtimeSinceStartup);
#endif
					EDLHEMMBABM.Instance.OnMatchedItemLanded(this);
				}
				onComplete?.Invoke();
			}
		}, curve);
	}

	public void KillMove()
	{
		if (moveTweenId != 0)
		{
			DGNMMHCBFMI.Kill(moveTweenId);
			moveTweenId = 0;
		}
		if (scaleTweenId != 0)
		{
			DGNMMHCBFMI.Kill(scaleTweenId);
			scaleTweenId = 0;
		}
	}

	/// <summary>换父节点或切换独立 Canvas 后，按当前父 Canvas 重建内部显示层级。</summary>
	public void RefreshCanvasLayers()
	{
		DynamicCanvasLayer.RefreshInChildren(transform);
	}

	public void RevertToStart()
	{
		if (savedParent != null)
		{
			transform.SetParent(savedParent, true);
			transform.SetSiblingIndex(savedSiblingIndex);
			RefreshCanvasLayers();
		}
	}

	public void SaveHierarchy()
	{
		savedParent = transform.parent;
		savedSiblingIndex = transform.GetSiblingIndex();
	}

	public void PlayAnim(string animName)
	{
		if (m_Anim != null && !string.IsNullOrEmpty(animName) && m_Anim[animName] != null)
		{
			m_Anim.Play(animName);
		}
	}

	public float GetAnimLength(string animName)
	{
		if (m_Anim != null && !string.IsNullOrEmpty(animName) && m_Anim[animName] != null)
		{
			return m_Anim[animName].length;
		}
		return 0f;
	}

	public void PlayQueuedAnim(string animName)
	{
		if (m_Anim != null && !string.IsNullOrEmpty(animName) && m_Anim[animName] != null)
		{
			m_Anim.PlayQueued(animName, QueueMode.CompleteOthers);
		}
	}
}
