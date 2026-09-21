using System;
using System.Collections;
using System.Collections.Generic;
using CorePlay;
using Framework.Base.UtilModule;
using UnityEngine;

/// <summary>核心玩法控制器：关卡解析、点击入槽、三消、胜负判定、道具（撤销/洗牌/魔法/扩容）、快照。</summary>
public class EDLHEMMBABM : global::FOLJNEPEKCA<EDLHEMMBABM>
{
	private struct JELADCKDFLB
	{
		public CollectItem item;

		public int itemId;

		public Transform parent;

		public int siblingIndex;

		public Vector2 anchoredPos;
	}

	private class MKFGFBJPGFC
	{
		public CollectItem[] items;

		public bool timerStarted;

		public bool isClutchSave;

		public bool isAllCleared;

		public int elimGroupCount;

		public bool enableEff;
	}

	private OPIBKKOCCIA occlusionGrid;

	private LevelConfig curLevelConfig;

	private List<CollectItem> TotalItems;

	private List<CollectItem> CollectAreaList;

	private bool ifUseAddOneItem;

	private Stack<JELADCKDFLB> undoStack;

	private HashSet<int> eliminatedIds;

	private List<MKFGFBJPGFC> pendingGroups;

	private int generation;

	private IHJFHNCGPKF collectItemPool;

	private const string LEVEL_MODE_FRUIT = "Fruit";

	private const string LEVEL_MODE_ANIMAL = "Animal";

	private const string LEVEL_MODE_VEGET = "Veget";

	private bool birthLockHeld;

	private Dictionary<int, CollectItem> itemMap;

	private const float ELIMINATE_ANIM_DURATION = 0.1f;

	private const float ELIMINATE_EARLY_REMOVE_DELAY = 0.3f;

	private const float COLLECT_MOVE_SPEED = 2000f;

	private const float COLLECT_ENTER_SPEED = 1200f;

	private static readonly string[] BIRTH_ANIMS = new string[]
	{
		"anim_Fruits_Birth",
		"anim_Fruits_Birth2"
	};

	private static List<CollectItem> preciseCandidatesCache = new List<CollectItem>();

	private const float SHUFFLE_CLICKABLE_THRESHOLD = 0.3f;

	private const float SHUFFLE_EFF_DELAY = 0.05f;

	private const float SHUFFLE_EFF_DURATION = 3f;

	private const float SHUFFLE_PHASE1_OFFSET_MIN = 5f;

	private const float SHUFFLE_PHASE1_OFFSET_MAX = 10f;

	private const float SHUFFLE_PHASE2_TO_PHASE3_DELAY = 0.8f;

	private const float SHUFFLE_PHASE2_MAX_DURATION = 0.7f;

	private int MaxCollectAreaNum => ifUseAddOneItem ? 8 : 7;

	private LevelData MainLevelData => JEFOMCDAPGK.Instance.MainLevelData;

	private CorePlayUI UI => CorePlayUI.Instance;

	private Transform ItemLayer
	{
		get
		{
			if (UI == null || UI.m_BgTrans == null)
			{
				return null;
			}
			Transform layer = UI.m_BgTrans.Find("Layer");
			return layer != null ? layer : UI.m_BgTrans;
		}
	}

	public int GetGeneration()
	{
		return generation;
	}

	public string GetCurLevelTree()
	{
		return curLevelConfig != null ? curLevelConfig.mapID : string.Empty;
	}

	public string GetCurLevelID()
	{
		return curLevelConfig != null ? curLevelConfig.levelID : string.Empty;
	}

	public int GetCurLevelGroupCount()
	{
		return curLevelConfig != null ? curLevelConfig.totalGroupCount : 0;
	}

	public int GetCurLevelTypeCount()
	{
		return curLevelConfig != null ? curLevelConfig.eleClassCount : 0;
	}

	public List<CollectItem> GetTotalItems()
	{
		return TotalItems;
	}

	public List<CollectItem> GetCollectAreaList()
	{
		return CollectAreaList;
	}

	public string GetCurLevelMode()
	{
		return LEVEL_MODE_FRUIT;
	}

	public int GetCurLevelTotalItems()
	{
		return curLevelConfig != null && curLevelConfig.normalTiles != null ? curLevelConfig.normalTiles.Count : 0;
	}

	public void Init()
	{
		TotalItems = new List<CollectItem>();
		CollectAreaList = new List<CollectItem>();
		undoStack = new Stack<JELADCKDFLB>();
		eliminatedIds = new HashSet<int>();
		pendingGroups = new List<MKFGFBJPGFC>();
		itemMap = new Dictionary<int, CollectItem>();
	}

	// ==================== 关卡构建 ====================

	public IEnumerator ParseConfig(LevelConfig InConfig, bool InReplay, Action InEndActions)
	{
		MCCIJBJGMCK.Lock();
		generation++;
		BeforeInitActions();
		curLevelConfig = InConfig;
		if (UI != null)
		{
			UI.ChangeGameBg(InConfig.mapID);
			CorePlayUI.EntryType = InReplay ? "replay" : "normal";
		}
		yield return null;
		CreateAllItems(InConfig);
		LevelData levelData = MainLevelData;
		levelData.levelID = InConfig.levelID;
		levelData.totalItemCount = TotalItems.Count;
		// 必须在 LogLevelStart 置位 hasEnterLevel 之前记录“首次进关”，否则新手引导永远不触发
		bool firstEnterLevel = !SaveDataUtils.GameData.customTutorialEnd;
		if (!InReplay)
		{
			JEFOMCDAPGK.Instance.LogLevelStart();
		}
		yield return null;
		BuildOcclusionGrid();
		SetUseAddOneItemStatus(ifUseAddOneItem, false);
		PlayBirthAnims();
		GameAudio.Play(DLMJOHCOJKN.Play_sfx_noitce_game_start);
		yield return new WaitForSeconds(0.6f);
		RandomIdleAnims();
		SaveSnapshot();
		InEndActions?.Invoke();
		MCCIJBJGMCK.UnlockOnce();
	}

	private void CreateAllItems(LevelConfig InConfig)
	{
		if (InConfig == null || InConfig.normalTiles == null)
		{
			return;
		}
		List<SingleNormalTile> tiles = new List<SingleNormalTile>(InConfig.normalTiles);
		tiles.Sort((a, b) =>
		{
			int cmp = a.layerIndex.CompareTo(b.layerIndex);
			return cmp != 0 ? cmp : a.indexInLayer.CompareTo(b.indexInLayer);
		});
		foreach (SingleNormalTile tile in tiles)
		{
			CollectItem item = CreateItemAtTile(tile, tile.eleType);
			if (item != null)
			{
				TotalItems.Add(item);
				itemMap[item.Id] = item;
			}
		}
	}

	private GameObject GetCollectItemPrefab(int eleType)
	{
		return GameRes.LoadPrefab("res/local/coreplay/prefab/CollectItem");
	}

	private CollectItem GetCollectItemFromPool(SingleNormalTile InTile)
	{
		return GetCollectItemFromPool(InTile, InTile.eleType);
	}

	private CollectItem GetCollectItemFromPool(SingleNormalTile InTile, int InType)
	{
		Transform parent = ItemLayer;
		if (collectItemPool == null)
		{
			GameObject prefab = GetCollectItemPrefab(InType);
			if (prefab == null)
			{
				Debug.LogError("[EDLHEMMBABM] CollectItem prefab 缺失");
				return null;
			}
			collectItemPool = new IHJFHNCGPKF(prefab, 128, parent);
		}
		GameObject go = collectItemPool.Get(parent);
		CollectItem item = go.GetComponent<CollectItem>();
		if (item != null)
		{
			item.Init(InType, InTile.tileID);
		}
		return item;
	}

	private void ReleaseCollectItem(CollectItem item)
	{
		if (item == null)
		{
			return;
		}
		item.KillMove();
		item.UnBindClick();
		itemMap.Remove(item.Id);
		if (collectItemPool != null)
		{
			collectItemPool.Release(item.gameObject);
		}
		else
		{
			UnityEngine.Object.Destroy(item.gameObject);
		}
	}

	private CollectItem GetCollectItem(SingleNormalTile InInfo)
	{
		return CreateItemAtTile(InInfo, InInfo.eleType);
	}

	private CollectItem CreateItemAtTile(SingleNormalTile tile, int type)
	{
		CollectItem item = GetCollectItemFromPool(tile, type);
		if (item == null)
		{
			return null;
		}
		RectTransform rect = item.transform as RectTransform;
		rect.SetParent(ItemLayer, false);
		item.RefreshCanvasLayers();
		rect.anchoredPosition = new Vector2(tile.posX, tile.posY);
		rect.localScale = Vector3.one;
		item.Priority = tile.layerIndex * 10000 + tile.indexInLayer;
		item.Status = LECIONHKEJL.OnField;
		rect.SetAsLastSibling();
		item.gameObject.name = $"Item_{tile.tileID}_{type}";
		return item;
	}

	private void BuildOcclusionGrid()
	{
		occlusionGrid = new OPIBKKOCCIA(UI != null ? UI.m_BgTrans : null);
		occlusionGrid.Build(TotalItems);
	}

	private void BeforeInitActions()
	{
		ReleaseAllItems();
		undoStack.Clear();
		eliminatedIds.Clear();
		pendingGroups.Clear();
		ifUseAddOneItem = false;
		PMNALKNFHEC.Instance.Cleanup();
		if (UI != null && UI.m_EncouragePos != null)
		{
			PMNALKNFHEC.Instance.Init(UI.m_EncouragePos);
		}
		if (NewPlayerGuider.Instance != null)
		{
			UnityEngine.Object.Destroy(NewPlayerGuider.Instance.gameObject);
		}
	}

	private void ReleaseItemList(List<CollectItem> items)
	{
		if (items == null)
		{
			return;
		}
		foreach (CollectItem item in items)
		{
			ReleaseCollectItem(item);
		}
		items.Clear();
	}

	public void ReleaseAllItems()
	{
		ReleaseItemList(TotalItems);
		ReleaseItemList(CollectAreaList);
		itemMap.Clear();
	}

	// ==================== 点击与入槽 ====================

	public void OnClick(CollectItem InCollectItem)
	{
		if (InCollectItem == null || InCollectItem.Status != LECIONHKEJL.OnField)
		{
			return;
		}
		if (CountInCollect() >= MaxCollectAreaNum)
		{
			return;
		}
		RectTransform rect = InCollectItem.transform as RectTransform;
		undoStack.Push(new JELADCKDFLB
		{
			item = InCollectItem,
			itemId = InCollectItem.Id,
			parent = InCollectItem.transform.parent,
			siblingIndex = InCollectItem.transform.GetSiblingIndex(),
			anchoredPos = rect != null ? rect.anchoredPosition : Vector2.zero
		});
		if (NewPlayerGuider.Instance != null)
		{
			NewPlayerGuider.Instance.OnItemClicked(InCollectItem);
		}
		MoveToCollectArea(InCollectItem);
		TryTripleMatch();
		MarkLastGroupIfAllCleared();
		SaveSnapshot();
		CheckGameResult();
	}

	private int CountInCollect()
	{
		int count = 0;
		foreach (CollectItem item in CollectAreaList)
		{
			if (item != null && item.Status == LECIONHKEJL.InCollect)
			{
				count++;
			}
		}
		return count;
	}

	private void MoveToCollectArea(CollectItem item)
	{
		if (occlusionGrid != null)
		{
			occlusionGrid.Remove(item);
		}
		TotalItems.Remove(item);
		int insertIndex = CollectAreaList.Count;
		for (int i = CollectAreaList.Count - 1; i >= 0; i--)
		{
			CollectItem other = CollectAreaList[i];
			if (other != null && other.Type == item.Type && other.Status == LECIONHKEJL.InCollect)
			{
				insertIndex = i + 1;
				break;
			}
		}
		CollectAreaList.Insert(insertIndex, item);
		item.Status = LECIONHKEJL.InCollect;
		item.FromGameToBasket = true;
		item.transform.SetParent(UI.m_CollectArea, true);
		item.RefreshCanvasLayers();
		item.transform.SetAsLastSibling();
		Vector3 target = CalculateCollectAreaPos(insertIndex);
		float distance = Vector3.Distance(item.transform.localPosition, target);
		float speed = UI.moveParams != null && UI.moveParams.enterSpeed > 0f ? UI.moveParams.enterSpeed : COLLECT_ENTER_SPEED;
		float duration = Mathf.Clamp(distance / speed, 0.08f, 0.6f);
		float arcHeight = UI.moveParams != null ? UI.moveParams.maxArcHeight : 120f;
		AnimationCurve curve = UI.moveParams != null ? UI.moveParams.enterCurve : null;
		int capturedGen = generation;
		item.FlyBetweenAreas(target, duration, item.GetCollectAreaScale(), curve, arcHeight, delegate
		{
			if (capturedGen == generation)
			{
				RefreshCollectAreaPositions(true);
			}
		}, generation);
		RefreshCollectAreaPositions(true);
	}

	private void CleanUndoStack()
	{
		if (undoStack.Count == 0)
		{
			return;
		}
		Stack<JELADCKDFLB> kept = new Stack<JELADCKDFLB>();
		JELADCKDFLB[] all = undoStack.ToArray();
		undoStack.Clear();
		for (int i = all.Length - 1; i >= 0; i--)
		{
			if (!eliminatedIds.Contains(all[i].itemId))
			{
				undoStack.Push(all[i]);
			}
		}
	}

	// ==================== 三消 ====================

	private bool CheckCanTripleMatch()
	{
		return FindTripleMatchStartIndex() >= 0;
	}

	private int FindTripleMatchStartIndex()
	{
		int runStart = -1;
		int runType = int.MinValue;
		int runLen = 0;
		for (int i = 0; i < CollectAreaList.Count; i++)
		{
			CollectItem item = CollectAreaList[i];
			if (item == null || item.Status != LECIONHKEJL.InCollect)
			{
				runLen = 0;
				runType = int.MinValue;
				continue;
			}
			if (item.Type == runType)
			{
				runLen++;
			}
			else
			{
				runType = item.Type;
				runStart = i;
				runLen = 1;
			}
			if (runLen == 3)
			{
				return runStart;
			}
		}
		return -1;
	}

	private void TryTripleMatch()
	{
		int startIndex = FindTripleMatchStartIndex();
		while (startIndex >= 0)
		{
			CollectItem[] group = new CollectItem[3];
			for (int i = 0; i < 3; i++)
			{
				CollectItem item = CollectAreaList[startIndex + i];
				group[i] = item;
				item.Status = LECIONHKEJL.LogicMatched;
				item.UnBindClick();
				eliminatedIds.Add(item.Id);
			}
			LevelData levelData = MainLevelData;
			levelData.eliminatedItemCount += 3;
			levelData.elimGroupCount++;
			pendingGroups.Add(new MKFGFBJPGFC
			{
				items = group,
				elimGroupCount = levelData.elimGroupCount,
				enableEff = true
			});
			CleanUndoStack();
			TryTriggerGroupGoal(group);
			startIndex = FindTripleMatchStartIndex();
		}
	}

	/// <summary>三个都已落位时，同步触发 goalFor3。</summary>
	private void TryTriggerGroupGoal(CollectItem[] group)
	{
		foreach (CollectItem item in group)
		{
			if (item != null && item.IsMoving)
			{
				return;
			}
		}
		foreach (CollectItem item in group)
		{
			if (item != null && item.Status == LECIONHKEJL.LogicMatched)
			{
				item.TriggerLogicMatched(ELIMINATE_ANIM_DURATION, generation);
			}
		}
	}

	/// <summary>匹配项落位回调：检查其所属组是否可以触发 goalFor3。</summary>
	public void OnMatchedItemLanded(CollectItem item)
	{
		foreach (MKFGFBJPGFC group in pendingGroups)
		{
			foreach (CollectItem member in group.items)
			{
				if (member == item)
				{
					TryTriggerGroupGoal(group.items);
					return;
				}
			}
		}
	}

	public void ProcessPendingGroups()
	{
		for (int g = pendingGroups.Count - 1; g >= 0; g--)
		{
			MKFGFBJPGFC group = pendingGroups[g];
			bool ready = true;
			foreach (CollectItem item in group.items)
			{
				if (item != null && item.Status != LECIONHKEJL.ReadyForCollect)
				{
					ready = false;
					break;
				}
			}
			if (!ready || group.timerStarted)
			{
				continue;
			}
			group.timerStarted = true;
			EliminateGroup(group);
		}
	}

	private void EliminateGroup(MKFGFBJPGFC group)
	{
		pendingGroups.Remove(group);
		int capturedGen = generation;
		Vector3 synthesisPos = (group.items[0].transform.position + group.items[1].transform.position + group.items[2].transform.position) / 3f;
		GameAudio.Play(DLMJOHCOJKN.Play_sfx_anim_game_fruit_clear);
		PlayEncourageVoice(group.elimGroupCount);
		if (UI != null && UI.m_EncouragePos != null)
		{
			PMNALKNFHEC.Instance.OnEliminate(group.elimGroupCount, group.isClutchSave, group.isAllCleared, UI.m_EncouragePos.position, UI.m_EffRoot);
		}
		foreach (CollectItem item in group.items)
		{
			if (item != null)
			{
				item.PlayAnim("anim_Fruits_disappear");
			}
		}
		Timer.Instance.Delay(ELIMINATE_EARLY_REMOVE_DELAY, delegate
		{
			if (capturedGen != generation)
			{
				return;
			}
			foreach (CollectItem item in group.items)
			{
				if (item != null)
				{
					CollectAreaList.Remove(item);
				}
			}
			RefreshCollectAreaPositions(true);
			if (NewPlayerGuider.Instance != null)
			{
				NewPlayerGuider.Instance.OnEliminationComplete();
			}
			if (group.isAllCleared)
			{
				GameAudio.Play(DLMJOHCOJKN.Play_sfx_anim_game_confetti);
			}
			FlowModule.SynthesisLogic(Mathf.Max(0, (TotalItems.Count + CountInCollect()) / 3), synthesisPos);
			CheckGameResult();
		});
		Timer.Instance.Delay(0.75f, delegate
		{
			if (capturedGen != generation)
			{
				return;
			}
			foreach (CollectItem item in group.items)
			{
				ReleaseCollectItem(item);
			}
		});
	}

	private void PlayEncourageVoice(int elimGroupCount)
	{
		LevelData levelData = MainLevelData;
		int step = elimGroupCount - levelData.lastClutchElimGroupCount;
		uint[] voices = new uint[]
		{
			DLMJOHCOJKN.Play_voice_male_good_low,
			DLMJOHCOJKN.Play_voice_male_great_low,
			DLMJOHCOJKN.Play_voice_male_excelent_low,
			DLMJOHCOJKN.Play_voice_male_amazing_low,
			DLMJOHCOJKN.Play_voice_male_unbelievable_low
		};
		int index = Mathf.Clamp((elimGroupCount - 1) % voices.Length, 0, voices.Length - 1);
		GameAudio.Play(voices[index]);
	}

	private void MarkLastGroupIfAllCleared()
	{
		if (TotalItems.Count > 0 || pendingGroups.Count == 0)
		{
			return;
		}
		foreach (CollectItem item in CollectAreaList)
		{
			if (item != null && item.Status == LECIONHKEJL.InCollect)
			{
				return;
			}
		}
		pendingGroups[pendingGroups.Count - 1].isAllCleared = true;
	}

	// ==================== 胜负 ====================

	private void SetUseAddOneItemStatus(bool InIfUse, bool WithAni)
	{
		ifUseAddOneItem = InIfUse;
		if (UI != null)
		{
			UI.UpdateAddOneBtn(InIfUse, WithAni);
		}
	}

	private void CheckGameResult()
	{
		if (JEFOMCDAPGK.Instance.Data.isLevelFinished)
		{
			return;
		}
		if (CheckLose())
		{
			OnLose();
			return;
		}
		if (CheckWin())
		{
			OnWin();
			return;
		}
		if (!JEFOMCDAPGK.Instance.Data.hasShownWillFillTip && MaxCollectAreaNum - CountInCollect() == 2 && TotalItems.Count > 0)
		{
			ShowWillFillTip();
		}
	}

	private bool CheckWin()
	{
		if (TotalItems.Count > 0)
		{
			return false;
		}
		foreach (CollectItem item in CollectAreaList)
		{
			if (item != null && item.Status == LECIONHKEJL.InCollect)
			{
				return false;
			}
		}
		return true;
	}

	private bool CheckLose()
	{
		return CountInCollect() >= MaxCollectAreaNum;
	}

	public void OnWin(bool InNeedWait = true)
	{
		if (JEFOMCDAPGK.Instance.Data.isLevelFinished)
		{
			return;
		}
		MCCIJBJGMCK.Lock();
		JEFOMCDAPGK.Instance.LogLevelEnd("win", true);
		ClearSnapshot();
		HarvestBridge.CompleteTutorial();
		CoroutineManager.Instance.StartCor(WaitEliminationAndOpenWinUI(InNeedWait ? 6f : 0f));
	}

	public void OnLose()
	{
		if (JEFOMCDAPGK.Instance.Data.isLevelFinished || MainLevelData.isLose)
		{
			return;
		}
		MainLevelData.isLose = true;
		JEFOMCDAPGK.Instance.SaveData();
		OpenLoseUI();
	}

	private IEnumerator WaitEliminationAndOpenWinUI(float InMaxWaitTime)
	{
#if UNITY_EDITOR
		Debug.Log("[HarvestMatch] Final match at " + Time.realtimeSinceStartup);
#endif
		float waited = 0f;
		while (waited < InMaxWaitTime)
		{
			bool hasPending = pendingGroups.Count > 0;
			if (!hasPending)
			{
				foreach (CollectItem item in CollectAreaList)
				{
					if (item != null)
					{
						hasPending = true;
						break;
					}
				}
			}
			if (!hasPending)
			{
				break;
			}
			yield return null;
			waited += Time.deltaTime;
		}
		yield return new WaitForSeconds(0.3f);
		MCCIJBJGMCK.UnlockOnce();
#if UNITY_EDITOR
		Debug.Log("[HarvestMatch] Open win at " + Time.realtimeSinceStartup + "; elimination wait=" + waited);
#endif
		OpenWinUI();
	}

	// ==================== 开局弹窗 ====================

	public void ShowLevelStartPops()
	{
		if (!SaveDataUtils.TeachData.IsCompleted("Teach_01")) return;
		if (!ShowNewItemPopIfNeeded())
		{
			ShowHardLevelTipIfNeeded();
		}
	}

	private void ShowSnapshotStartPops()
	{
	}

	private bool ShowNewItemPopIfNeeded()
	{
		int curLevel = JEFOMCDAPGK.Instance.CurMainLevelIndex;
		POJCEPBNNIP[] itemTypes = new POJCEPBNNIP[]
		{
			POJCEPBNNIP.Undo,
			POJCEPBNNIP.Shuffle,
			POJCEPBNNIP.Magic,
			POJCEPBNNIP.Extra
		};
		foreach (POJCEPBNNIP itemType in itemTypes)
		{
			if (GNEJJHEDEBL.Instance.GetItemUnlockLevel(itemType) > curLevel)
			{
				continue;
			}
			if (JEFOMCDAPGK.Instance.HasShownNewItemPop(itemType))
			{
				continue;
			}
			if (!JEFOMCDAPGK.Instance.TryReserveNewItemPop(itemType, out var selected)) continue;
			int capturedGeneration=generation;
			Timer.Instance.Delay(0.4f, delegate
			{
				if(capturedGeneration!=generation || !HarvestBridge.Ready ||
					JEFOMCDAPGK.Instance.HasShownNewItemPop(selected) ||
					JEFOMCDAPGK.Instance.Data.pendingNewItemPop != (int)selected) return;
				MgrUI.Instance.Open("pops/newitempop/NewItemPop");
			});
			return true;
		}
		return false;
	}

	private bool ShowHardLevelTipIfNeeded()
	{
		return false;
	}

	private bool DoShowLevelTip()
	{
		return false;
	}

	private void OpenWinUI()
	{
		HarvestBridge.Win();
	}

	public void OpenLoseUI()
	{
		MCCIJBJGMCK.Lock();
		Timer.Instance.Delay(0.6f, delegate
		{
			MCCIJBJGMCK.UnlockOnce();
			HarvestBridge.Lose();
		});
	}

	// ==================== 复活 ====================

	public void Revive(string InReviveReason)
	{
		JEFOMCDAPGK.Instance.RecordRevive(InReviveReason, TotalItems.Count);
		MainLevelData.isLose = false;
		JEFOMCDAPGK.Instance.SaveData();
		CoroutineManager.Instance.StartCor(RealRevive());
	}

	private IEnumerator RealRevive(float undoInterval = 0.15f, float shuffleDelay = 0f)
	{
		MCCIJBJGMCK.Lock();
		int undoCount = 0;
		while (CountInCollect() > MaxCollectAreaNum - 3 && undoStack.Count > 0 && undoCount < 3)
		{
			UndoLogic();
			undoCount++;
			yield return new WaitForSeconds(undoInterval);
		}
		SaveSnapshot();
		RefreshCollectAreaPositions(true);
		MCCIJBJGMCK.UnlockOnce();
	}

	// ==================== 收集槽布局 ====================

	private void RefreshCollectAreaPositions(bool InWithAni)
	{
		for (int i = 0; i < CollectAreaList.Count; i++)
		{
			CollectItem item = CollectAreaList[i];
			if (item == null || item.IsMoving)
			{
				continue;
			}
			if (item.Status != LECIONHKEJL.InCollect && item.Status != LECIONHKEJL.LogicMatched && item.Status != LECIONHKEJL.ReadyForCollect)
			{
				continue;
			}
			Vector3 target = CalculateCollectAreaPos(i);
			if ((item.transform.localPosition - target).sqrMagnitude < 1f)
			{
				continue;
			}
			if (!InWithAni)
			{
				item.transform.localPosition = target;
				continue;
			}
			float speed = UI.moveParams != null && UI.moveParams.reorderSpeed > 0f ? UI.moveParams.reorderSpeed : COLLECT_MOVE_SPEED;
			float duration = Mathf.Clamp(Vector3.Distance(item.transform.localPosition, target) / speed, 0.05f, 0.3f);
			AnimationCurve curve = UI.moveParams != null ? UI.moveParams.reorderCurve : null;
			item.MoveInSameArea(target, duration, curve, generation);
		}
	}

	private Vector3 CalculateCollectAreaPos(int InIndex)
	{
		if (UI == null || UI.m_CollectArea == null || UI.m_CollectFirstPos == null)
		{
			return Vector3.zero;
		}
		Vector3 firstLocal = UI.m_CollectArea.InverseTransformPoint(UI.m_CollectFirstPos.position);
		int interval = CorePlayUI.m_CollectAreaInterval > 0 ? CorePlayUI.m_CollectAreaInterval : 123;
		return firstLocal + new Vector3(interval * InIndex, 0f, 0f);
	}

	// ==================== 出生与待机动画 ====================

	private void RandomIdleAnims()
	{
		foreach (CollectItem item in TotalItems)
		{
			if (item != null && item.Status == LECIONHKEJL.OnField)
			{
				item.PlayAnim(item.DefaultIdleAnim);
			}
		}
	}

	private void PlayBirthAnims()
	{
		foreach (CollectItem item in TotalItems)
		{
			if (item != null)
			{
				item.PlayAnim(BIRTH_ANIMS[UnityEngine.Random.Range(0, BIRTH_ANIMS.Length)]);
			}
		}
	}

	public void ShowWillFillTip()
	{
		JEFOMCDAPGK.Instance.SetShowWillFillTipState(true);
		if (UI != null)
		{
			UI.ShowWillFillTip(UI.m_CollectArea != null ? UI.m_CollectArea.position : Vector3.zero, MaxCollectAreaNum - CountInCollect());
		}
	}

	// ==================== 快照 ====================

	private void SaveSnapshot()
	{
		if (curLevelConfig == null)
		{
			return;
		}
		GameSnapshot snapshot = new GameSnapshot
		{
			levelConfig = curLevelConfig,
			ifUseAddOneItem = ifUseAddOneItem,
			totalItems = new List<GameItemData>(),
			collectItems = new List<CollectAreaData>(),
			undoRecords = new List<UndoRecord>()
		};
		foreach (CollectItem item in TotalItems)
		{
			if (item != null)
			{
				snapshot.totalItems.Add(new GameItemData
				{
					tileId = item.Id,
					type = item.Type,
					priority = item.Priority
				});
			}
		}
		for (int i = 0; i < CollectAreaList.Count; i++)
		{
			CollectItem item = CollectAreaList[i];
			if (item != null && item.Status == LECIONHKEJL.InCollect)
			{
				snapshot.collectItems.Add(new CollectAreaData
				{
					tileId = item.Id,
					type = item.Type,
					indexInList = i
				});
			}
		}
		foreach (JELADCKDFLB record in undoStack)
		{
			snapshot.undoRecords.Add(new UndoRecord
			{
				itemId = record.itemId,
				siblingIndex = record.siblingIndex,
				savedPriority = record.item != null ? record.item.Priority : 0,
				anchoredX = record.anchoredPos.x,
				anchoredY = record.anchoredPos.y
			});
		}
		LevelData levelData = MainLevelData;
		levelData.gameSnapshot = snapshot;
		levelData.hasEffectiveSave = true;
		JEFOMCDAPGK.Instance.SaveData();
	}

	private void ClearSnapshot()
	{
		LevelData levelData = MainLevelData;
		levelData.gameSnapshot = null;
		levelData.hasEffectiveSave = false;
		JEFOMCDAPGK.Instance.SaveData();
	}

	public IEnumerator RestoreFromSnapshot(Action InEndActions)
	{
		GameSnapshot snapshot = MainLevelData.gameSnapshot;
		if (snapshot == null || snapshot.levelConfig == null)
		{
			yield return CoroutineManager.Instance.StartCor(ParseConfig(PLMIHDHFAAL.Instance.GetMainLevelConfig(), false, InEndActions));
			yield break;
		}
		MCCIJBJGMCK.Lock();
		generation++;
		BeforeInitActions();
		curLevelConfig = snapshot.levelConfig;
		ifUseAddOneItem = snapshot.ifUseAddOneItem;
		if (UI != null)
		{
			UI.ChangeGameBg(curLevelConfig.mapID);
		}
		yield return null;
		Dictionary<int, SingleNormalTile> tileDict = new Dictionary<int, SingleNormalTile>();
		foreach (SingleNormalTile tile in curLevelConfig.normalTiles)
		{
			tileDict[tile.tileID] = tile;
		}
		// 场上
		List<GameItemData> sorted = new List<GameItemData>(snapshot.totalItems);
		sorted.Sort((a, b) => a.priority.CompareTo(b.priority));
		foreach (GameItemData itemData in sorted)
		{
			if (!tileDict.TryGetValue(itemData.tileId, out SingleNormalTile tile))
			{
				continue;
			}
			CollectItem item = CreateItemAtTile(tile, itemData.type);
			if (item != null)
			{
				item.Priority = itemData.priority;
				TotalItems.Add(item);
				itemMap[item.Id] = item;
			}
		}
		// 槽内
		snapshot.collectItems.Sort((a, b) => a.indexInList.CompareTo(b.indexInList));
		foreach (CollectAreaData collectData in snapshot.collectItems)
		{
			SingleNormalTile fakeTile = tileDict.TryGetValue(collectData.tileId, out SingleNormalTile t)
				? t
				: new SingleNormalTile { tileID = collectData.tileId, eleType = collectData.type };
			CollectItem item = GetCollectItemFromPool(fakeTile, collectData.type);
			if (item == null)
			{
				continue;
			}
			item.transform.SetParent(UI.m_CollectArea, false);
			item.RefreshCanvasLayers();
			item.Status = LECIONHKEJL.InCollect;
			CollectAreaList.Add(item);
			item.transform.localPosition = CalculateCollectAreaPos(CollectAreaList.Count - 1);
			item.SetScale(item.GetCollectAreaScale());
			item.gameObject.name = $"Item_{collectData.tileId}_{collectData.type}";
			itemMap[item.Id] = item;
		}
		// 撤销栈（undoRecords 顺序为栈顶→栈底）
		for (int i = snapshot.undoRecords.Count - 1; i >= 0; i--)
		{
			UndoRecord undoRecord = snapshot.undoRecords[i];
			itemMap.TryGetValue(undoRecord.itemId, out CollectItem item);
			undoStack.Push(new JELADCKDFLB
			{
				item = item,
				itemId = undoRecord.itemId,
				parent = ItemLayer,
				siblingIndex = undoRecord.siblingIndex,
				anchoredPos = new Vector2(undoRecord.anchoredX, undoRecord.anchoredY)
			});
		}
		yield return null;
		BuildOcclusionGrid();
		SetUseAddOneItemStatus(ifUseAddOneItem, false);
		RandomIdleAnims();
		ShowSnapshotStartPops();
		InEndActions?.Invoke();
		MCCIJBJGMCK.UnlockOnce();
	}

	private void ShuffleList<T>(List<T> list)
	{
		for (int i = list.Count - 1; i > 0; i--)
		{
			int j = UnityEngine.Random.Range(0, i + 1);
			T tmp = list[i];
			list[i] = list[j];
			list[j] = tmp;
		}
	}

	public List<CollectItem> GetHigherPriorityCandidates(CollectItem item)
	{
		preciseCandidatesCache.Clear();
		if (occlusionGrid != null)
		{
			occlusionGrid.GetHigherPriorityItems(item, preciseCandidatesCache);
		}
		return preciseCandidatesCache;
	}

	public void ShowCoverArea()
	{
	}

	public void HideCoverArea()
	{
	}

	// ==================== 道具：扩容 ====================

	public bool CheckCanAddOne()
	{
		return !ifUseAddOneItem;
	}

	public bool AddOne(bool InConsumeItem = true)
	{
		if (!CheckCanAddOne())
		{
			return false;
		}

		MainLevelData.extraUseCount++;
		JEFOMCDAPGK.Instance.RecordItemUse("Extra", TotalItems.Count);
		GameAudio.Play(DLMJOHCOJKN.Play_sfx_notice_prop_addSpace);
		SetUseAddOneItemStatus(true, true);
		SaveSnapshot();
		return true;
	}

	// ==================== 道具：撤销 ====================

	public bool CheckCanUndo()
	{
		return undoStack.Count > 0 && undoStack.Peek().item != null && undoStack.Peek().item.Status == LECIONHKEJL.InCollect;
	}

	private JELADCKDFLB UndoLogic()
	{
		JELADCKDFLB record = undoStack.Pop();
		CollectItem item = record.item;
		if (item == null || item.Status != LECIONHKEJL.InCollect)
		{
			return record;
		}
		CollectAreaList.Remove(item);
		item.Status = LECIONHKEJL.Returning;
        // Commit logical ownership before animation: a save or level restart during
        // the return flight must still contain and release this fruit exactly once.
        TotalItems.Add(item);
		item.KillMove();
		item.transform.SetParent(ItemLayer, true);
		item.RefreshCanvasLayers();
		item.transform.SetSiblingIndex(Mathf.Min(record.siblingIndex, ItemLayer.childCount - 1));
		Vector3 target = new Vector3(record.anchoredPos.x, record.anchoredPos.y, 0f);
		float speed = UI.moveParams != null && UI.moveParams.enterSpeed > 0f ? UI.moveParams.enterSpeed : COLLECT_ENTER_SPEED;
		float duration = Mathf.Clamp(Vector3.Distance(item.transform.localPosition, target) / speed, 0.08f, 0.6f);
		float arcHeight = UI.moveParams != null ? UI.moveParams.maxArcHeight : 120f;
		int capturedGen = generation;
		item.PlayAnim("anim_Fruits_ReturnFly");
		item.FlyBetweenAreas(target, duration, 1f, UI.moveParams != null ? UI.moveParams.enterCurve : null, arcHeight, delegate
		{
			if (capturedGen != generation || item == null)
			{
				return;
			}
			item.Status = LECIONHKEJL.OnField;
			item.PlayAnim("anim_Fruits_Return");
			item.PlayQueuedAnim(item.DefaultIdleAnim);
			if (occlusionGrid != null)
			{
				occlusionGrid.Add(item);
			}
			item.BindClick();
            SaveSnapshot();
		}, generation);
		RefreshCollectAreaPositions(true);
		return record;
	}

	public bool Undo(bool InConsumeItem = true)
	{
		if (!CheckCanUndo())
		{
			return false;
		}

		MainLevelData.undoUseCount++;
		JEFOMCDAPGK.Instance.RecordItemUse("Undo", TotalItems.Count);
		GameAudio.Play(DLMJOHCOJKN.Play_sfx_notice_prop_undo);
		UndoLogic();
		SaveSnapshot();
		return true;
	}

	// ==================== 道具：魔法 ====================

	private CollectItem FindMagicBasketTarget()
	{
		// 优先补齐槽内已有 2 个的类型，其次 1 个
		CollectItem best = null;
		int bestCount = 0;
		foreach (CollectItem item in CollectAreaList)
		{
			if (item == null || item.Status != LECIONHKEJL.InCollect)
			{
				continue;
			}
			int count = CountMagicBasketSameType(item);
			int fieldCount = CountFieldType(item.Type);
			if (fieldCount >= 3 - count && count > bestCount)
			{
				best = item;
				bestCount = count;
			}
		}
		return best;
	}

	private int CountMagicBasketSameType(CollectItem basketTarget)
	{
		int count = 0;
		foreach (CollectItem item in CollectAreaList)
		{
			if (item != null && item.Status == LECIONHKEJL.InCollect && item.Type == basketTarget.Type)
			{
				count++;
			}
		}
		return count;
	}

	private int CountFieldType(int type)
	{
		int count = 0;
		foreach (CollectItem item in TotalItems)
		{
			if (item != null && item.Type == type && item.Status == LECIONHKEJL.OnField)
			{
				count++;
			}
		}
		return count;
	}

	public bool CheckCanMagic()
	{
		if (TotalItems.Count == 0)
		{
			return false;
		}
		if (FindMagicBasketTarget() != null)
		{
			return true;
		}
		// 场上任意类型凑满 3 个即可
		Dictionary<int, int> typeCount = new Dictionary<int, int>();
		foreach (CollectItem item in TotalItems)
		{
			if (item == null || item.Status != LECIONHKEJL.OnField)
			{
				continue;
			}
			typeCount.TryGetValue(item.Type, out int count);
			typeCount[item.Type] = count + 1;
			if (count + 1 >= 3)
			{
				return true;
			}
		}
		return false;
	}

	public bool Magic(bool InConsumeItem = true)
	{
		if (!CheckCanMagic())
		{
			return false;
		}
		int targetType;
		int needed;
		CollectItem basketTarget = FindMagicBasketTarget();
		if (basketTarget != null)
		{
			targetType = basketTarget.Type;
			needed = 3 - CountMagicBasketSameType(basketTarget);
		}
		else
		{
			targetType = -1;
			needed = 3;
			List<int> candidates = new List<int>();
			Dictionary<int, int> typeCount = new Dictionary<int, int>();
			foreach (CollectItem item in TotalItems)
			{
				if (item == null || item.Status != LECIONHKEJL.OnField)
				{
					continue;
				}
				typeCount.TryGetValue(item.Type, out int count);
				typeCount[item.Type] = count + 1;
				if (count + 1 == 3)
				{
					candidates.Add(item.Type);
				}
			}
			if (candidates.Count == 0)
			{
				return false;
			}
			targetType = candidates[UnityEngine.Random.Range(0, candidates.Count)];
		}
		List<CollectItem> fieldCandidates = new List<CollectItem>();
		foreach (CollectItem item in TotalItems)
		{
			if (item != null && item.Type == targetType && item.Status == LECIONHKEJL.OnField)
			{
				fieldCandidates.Add(item);
			}
		}
		if (fieldCandidates.Count < needed)
		{
			return false;
		}

		MainLevelData.magicUseCount++;
		JEFOMCDAPGK.Instance.RecordItemUse("Magic", TotalItems.Count);
		ShuffleList(fieldCandidates);
		List<CollectItem> selected = fieldCandidates.GetRange(0, needed);
		MCCIJBJGMCK.Lock();
		int capturedGen = generation;
		if (UI != null)
		{
			UI.PlayMagicAni();
		}
		foreach (CollectItem item in selected)
		{
			item.IsMagicPending = true;
		}
		Timer.Instance.Delay(0.6f, delegate
		{
			if (capturedGen != generation)
			{
				return;
			}
			MCCIJBJGMCK.UnlockOnce();
			for (int i = 0; i < selected.Count; i++)
			{
				CollectItem item = selected[i];
				int idx = i;
				Timer.Instance.Delay(idx * 0.12f, delegate
				{
					if (capturedGen != generation || item == null || item.Status != LECIONHKEJL.OnField)
					{
						return;
					}
					item.IsMagicPending = false;
					MoveToCollectArea(item);
					TryTripleMatch();
					MarkLastGroupIfAllCleared();
					SaveSnapshot();
					CheckGameResult();
				});
			}
		});
		return true;
	}

	// ==================== 道具：洗牌 ====================

	public bool CheckCanShuffle()
	{
		return TotalItems.Count > 1;
	}

	public bool Shuffle(bool InConsumeItem = true)
	{
		if (!CheckCanShuffle())
		{
			return false;
		}

		MainLevelData.shuffleUseCount++;
		JEFOMCDAPGK.Instance.RecordItemUse("Shuffle", TotalItems.Count);
		GameAudio.Play(DLMJOHCOJKN.Play_sfx_notice_prop_shuffle);
		MCCIJBJGMCK.Lock();
		PlayShuffleAnimations(delegate
		{
			FinishShuffle();
		});
		return true;
	}

	private void PlayShuffleAnimations(Action InEndAction = null)
	{
		int capturedGen = generation;
		int effBatchId = UI != null ? UI.PlayShuffleEff() : 0;
		Vector2 center = Vector2.zero;
		if (UI != null && UI.shuffleGatherPoint != null && UI.m_BgTrans != null)
		{
			center = UI.m_BgTrans.InverseTransformPoint(UI.shuffleGatherPoint.position);
		}
		// 阶段1：抖动
		foreach (CollectItem item in TotalItems)
		{
			if (item != null)
			{
				item.PlayAnim("anim_Fruits_Pitch");
			}
		}
		// 阶段2：聚拢
		Timer.Instance.Delay(0.25f, delegate
		{
			if (capturedGen != generation)
			{
				return;
			}
			List<CollectItem> items = new List<CollectItem>(TotalItems);
			int pending = items.Count;
			if (pending == 0)
			{
				InEndAction?.Invoke();
				return;
			}
			float radius = UI != null && UI.moveParams != null && UI.moveParams.shufflePhase2CenterRadius > 0f ? UI.moveParams.shufflePhase2CenterRadius : 120f;
			foreach (CollectItem item in items)
			{
				if (item == null)
				{
					pending--;
					continue;
				}
				Vector2 offset = UnityEngine.Random.insideUnitCircle * radius;
				Vector3 gatherPos = new Vector3(center.x + offset.x, center.y + offset.y, 0f);
				float duration = UnityEngine.Random.Range(0.35f, SHUFFLE_PHASE2_MAX_DURATION);
				item.MoveInSameArea(gatherPos, duration, UI != null && UI.moveParams != null ? UI.moveParams.shufflePhase2Curve : null, generation, delegate
				{
					pending--;
				});
			}
			// 阶段3：换类型后散开回原位
			Timer.Instance.Delay(SHUFFLE_PHASE2_MAX_DURATION + SHUFFLE_PHASE2_TO_PHASE3_DELAY, delegate
			{
				if (capturedGen != generation)
				{
					return;
				}
				ShuffleTypeLogic();
				int scatterPending = 0;
				foreach (CollectItem item in TotalItems)
				{
					if (item == null)
					{
						continue;
					}
					scatterPending++;
					Vector2 fieldPos = GetItemFieldPos(item);
					float duration = UnityEngine.Random.Range(0.3f, 0.55f);
					CollectItem captured = item;
					captured.PlayAnim("anim_Fruits_RefreshFly");
					captured.MoveInSameArea(new Vector3(fieldPos.x, fieldPos.y, 0f), duration, UI != null && UI.moveParams != null ? UI.moveParams.shufflePhase3Curve : null, generation, delegate
					{
						captured.PlayQueuedAnim(captured.DefaultIdleAnim);
						scatterPending--;
						if (scatterPending == 0)
						{
							if (UI != null)
							{
								UI.StopShuffleEff(effBatchId);
							}
							InEndAction?.Invoke();
						}
					});
				}
				if (scatterPending == 0)
				{
					InEndAction?.Invoke();
				}
			});
		});
	}

	private void ShuffleLogicCore()
	{
		ShuffleTypeLogic();
	}

	private void HandleReturningItems()
	{
	}

	private Vector2 GetItemFieldPos(CollectItem item)
	{
		if (curLevelConfig != null && curLevelConfig.normalTiles != null)
		{
			foreach (SingleNormalTile tile in curLevelConfig.normalTiles)
			{
				if (tile.tileID == item.Id)
				{
					return new Vector2(tile.posX, tile.posY);
				}
			}
		}
		RectTransform rect = item.transform as RectTransform;
		return rect != null ? rect.anchoredPosition : Vector2.zero;
	}

	/// <summary>重新分配场上对象的类型（保持每种类型的数量不变）。</summary>
	private void ShuffleTypeLogic()
	{
		List<int> types = new List<int>(TotalItems.Count);
		foreach (CollectItem item in TotalItems)
		{
			if (item != null)
			{
				types.Add(item.Type);
			}
		}
		ShuffleList(types);
		int index = 0;
		foreach (CollectItem item in TotalItems)
		{
			if (item != null)
			{
				item.SetType(types[index]);
				index++;
			}
		}
	}

	private void ComputeShuffleTypes()
	{
	}

	private void FinishShuffle()
	{
		BuildOcclusionGrid();
		SaveSnapshot();
		MCCIJBJGMCK.UnlockAll();
	}

	// ==================== 调试 ====================

	public void DebugRemoveThree()
	{
		Dictionary<int, List<CollectItem>> byType = new Dictionary<int, List<CollectItem>>();
		foreach (CollectItem item in TotalItems)
		{
			if (item == null || item.Status != LECIONHKEJL.OnField)
			{
				continue;
			}
			if (!byType.TryGetValue(item.Type, out List<CollectItem> list))
			{
				list = new List<CollectItem>();
				byType[item.Type] = list;
			}
			list.Add(item);
		}
		foreach (KeyValuePair<int, List<CollectItem>> pair in byType)
		{
			if (pair.Value.Count >= 3)
			{
				for (int i = 0; i < 3; i++)
				{
					OnClick(pair.Value[i]);
				}
				return;
			}
		}
	}

	public void AnalyzeTypeDistribution()
	{
	}
}
