using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 首关新手引导。
/// 阶段1：半透明遮罩压暗全场，目标水果提亮，手指依次指向 3 个蓝莓，文案“点击3个相同水果！”。
/// 阶段2：遮罩消失，文案切换“匹配所有水果即可获胜！”，玩家自由操作；
/// 停顿超过约1.2s 后手指进入跟随模式，持续指向当前组的下一个目标，直到该组消除完成
/// （对应录屏 13.0s 桃子上的手指表现）。
/// </summary>
public class NewPlayerGuider : MonoBehaviour
{
	private enum NHOGGEEGCJB
	{
		None = 0,
		Phase1 = 1,
		Phase2 = 2
	}

	[SerializeField]
	private GameObject m_GuideFinger;

	[SerializeField]
	private TextMeshProCustom m_GuideText;

	[SerializeField]
	private Canvas m_MaskCanvas;

	[SerializeField]
	private Image m_MaskBG;

	[SerializeField]
	private Animation m_FingerAnim;

	[SerializeField]
	private Animation m_GuideTextAnim;

	private RectTransform m_FingerRect;

	private uint m_MaskFadeTweenId;

	private NHOGGEEGCJB m_Phase;

	private List<CollectItem> m_Phase1Targets;

	private readonly List<CollectItem> m_HighlightedItems = new List<CollectItem>();

	private Action phase2Action;

	private float lastInteractionTime;

	private bool m_Phase2Following;

	private const int BlueberryType = 39;

	// Guide prefab: mask -900, highlighted fruit -899, finger/text -898.
	private const int HIGHLIGHT_ORDER = -899;

	private const float PHASE2_IDLE_TIME = 1.2f;

	public static NewPlayerGuider Instance { get; private set; }

	private void Awake()
	{
		Instance = this;
		if (m_GuideFinger != null)
		{
			m_FingerRect = m_GuideFinger.transform as RectTransform;
		}
	}

	private void OnDestroy()
	{
		ClearHighlights();
		if (Instance == this)
		{
			Instance = null;
		}
	}

	private void Update()
	{
		if (m_Phase != NHOGGEEGCJB.Phase2 || m_Phase2Following)
		{
			return;
		}
		if (Time.time - lastInteractionTime > PHASE2_IDLE_TIME)
		{
			CollectItem target = FindPhase2Target();
			if (target != null)
			{
				m_Phase2Following = true;
				ShowFingerAt(target);
			}
		}
	}

	public void StartGuide(List<CollectItem> totalItems, Action InPhase2Action)
	{
		phase2Action = InPhase2Action;
		m_Phase1Targets = new List<CollectItem>();
		if (totalItems != null)
		{
			foreach (CollectItem item in totalItems)
			{
				if (item != null && item.Type == BlueberryType)
				{
					m_Phase1Targets.Add(item);
				}
			}
			if (m_Phase1Targets.Count < 3 && totalItems.Count >= 3)
			{
				// 蓝莓不足时退化为首个类型
				m_Phase1Targets.Clear();
				int fallbackType = totalItems[0].Type;
				foreach (CollectItem item in totalItems)
				{
					if (item != null && item.Type == fallbackType)
					{
						m_Phase1Targets.Add(item);
					}
				}
			}
		}
		StartPhase1();
	}

	private void StartPhase1()
	{
		m_Phase = NHOGGEEGCJB.Phase1;
		if (m_GuideText != null)
		{
			m_GuideText.text = OJEEJGGLNPC.Instance.GetText("guide_newplayer_1");
		}
		if (m_GuideTextAnim != null && m_GuideTextAnim["anim_GuideText"] != null)
		{
			m_GuideTextAnim.Play("anim_GuideText");
		}
		ShowMask(true);
		if (m_Phase1Targets != null)
		{
			foreach (CollectItem item in m_Phase1Targets)
			{
				SetHighlight(item, true);
			}
			if (m_Phase1Targets.Count > 0)
			{
				ShowFingerAt(m_Phase1Targets[0]);
			}
		}
	}

	private void StartPhase2(bool playTransitionAnim = true)
	{
		m_Phase = NHOGGEEGCJB.Phase2;
		lastInteractionTime = Time.time;
		HideFinger();
		ClearHighlights();
		ShowMask(false);
		if (m_GuideText != null)
		{
			m_GuideText.text = OJEEJGGLNPC.Instance.GetText("guide_newplayer_2");
		}
		if (playTransitionAnim && m_GuideTextAnim != null && m_GuideTextAnim["anim_GuideText"] != null)
		{
			m_GuideTextAnim.Play("anim_GuideText");
		}
		phase2Action?.Invoke();
	}

	public void OnItemClicked(CollectItem item)
	{
		lastInteractionTime = Time.time;
		if (m_Phase == NHOGGEEGCJB.Phase1)
		{
			HandlePhase1Click(item);
		}
		else if (m_Phase == NHOGGEEGCJB.Phase2 && m_Phase2Following)
		{
			// 跟随模式：继续指向当前组的下一个目标（点击回调时该对象尚未离场，需排除）
			CollectItem next = FindPhase2Target(item, item != null ? item.Type : -1);
			if (next != null)
			{
				ShowFingerAt(next);
			}
			else
			{
				HideFinger();
			}
		}
	}

	public void OnEliminationComplete()
	{
		lastInteractionTime = Time.time;
		if (m_Phase == NHOGGEEGCJB.Phase1)
		{
			StartPhase2();
		}
		else if (m_Phase == NHOGGEEGCJB.Phase2)
		{
			m_Phase2Following = false;
			HideFinger();
			if (EDLHEMMBABM.Instance.GetTotalItems().Count == 0)
			{
				// 场上清空即关卡完成，引导随之结束
				Destroy(gameObject);
			}
		}
	}

	private void HandlePhase1Click(CollectItem item)
	{
		if (m_Phase1Targets == null || !m_Phase1Targets.Contains(item))
		{
			return;
		}
		m_Phase1Targets.Remove(item);
		if (m_Phase1Targets.Count > 0)
		{
			ShowFingerAt(m_Phase1Targets[0]);
		}
		else
		{
			HideFinger();
		}
	}

	/// <summary>阶段2目标：优先槽内最新一件（或指定类型）的同类型场上对象，否则任意场上对象。</summary>
	private CollectItem FindPhase2Target(CollectItem exclude = null, int preferTypeOverride = -1)
	{
		int preferType = preferTypeOverride;
		if (preferType < 0)
		{
			List<CollectItem> slot = EDLHEMMBABM.Instance.GetCollectAreaList();
			if (slot != null)
			{
				for (int i = slot.Count - 1; i >= 0; i--)
				{
					if (slot[i] != null && slot[i].Status == LECIONHKEJL.InCollect)
					{
						preferType = slot[i].Type;
						break;
					}
				}
			}
		}
		List<CollectItem> field = EDLHEMMBABM.Instance.GetTotalItems();
		if (field == null)
		{
			return null;
		}
		CollectItem fallback = null;
		foreach (CollectItem item in field)
		{
			if (item == null || item == exclude || item.Status != LECIONHKEJL.OnField)
			{
				continue;
			}
			if (item.Type == preferType)
			{
				return item;
			}
			if (fallback == null)
			{
				fallback = item;
			}
		}
		return fallback;
	}

	private void ShowFingerAt(CollectItem item)
	{
		if (m_GuideFinger == null || item == null)
		{
			return;
		}
		m_GuideFinger.SetActive(true);
		m_GuideFinger.transform.position = item.transform.position;
		if (m_FingerAnim != null && m_FingerAnim["finger"] != null)
		{
			m_FingerAnim.Play("finger");
		}
	}

	private void HideFinger()
	{
		if (m_GuideFinger != null)
		{
			m_GuideFinger.SetActive(false);
		}
	}

	private void ShowMask(bool show)
	{
		if (m_MaskBG == null)
		{
			return;
		}
		if (m_MaskFadeTweenId != 0)
		{
			DGNMMHCBFMI.Kill(m_MaskFadeTweenId);
			m_MaskFadeTweenId = 0;
		}
		m_MaskBG.enabled = true;
		m_MaskBG.raycastTarget = false;
		float target = show ? 0.6f : 0f;
		m_MaskFadeTweenId = m_MaskBG.DoFade(target, 0.3f, delegate
		{
			if (this != null && !show && m_MaskBG != null)
			{
				m_MaskBG.enabled = false;
			}
		});
	}

	/// <summary>把目标水果提亮到遮罩之上。</summary>
	private void SetHighlight(CollectItem item, bool highlight)
	{
		if (item == null)
		{
			return;
		}
		Canvas canvas = item.gameObject.GetComponent<Canvas>();
		if (highlight)
		{
			if (canvas == null)
			{
				canvas = item.gameObject.AddComponent<Canvas>();
			}
			canvas.overrideSorting = true;
			canvas.sortingOrder = HIGHLIGHT_ORDER;
			if (!m_HighlightedItems.Contains(item))
			{
				m_HighlightedItems.Add(item);
			}
		}
		else if (canvas != null)
		{
			canvas.overrideSorting = false;
			m_HighlightedItems.Remove(item);
		}
		item.RefreshCanvasLayers();
	}

	private void ClearHighlights()
	{
		for (int i = m_HighlightedItems.Count - 1; i >= 0; i--)
		{
			CollectItem item = m_HighlightedItems[i];
			if (item != null)
			{
				Canvas canvas = item.gameObject.GetComponent<Canvas>();
				if (canvas != null)
				{
					canvas.overrideSorting = false;
				}
				item.RefreshCanvasLayers();
			}
		}
		m_HighlightedItems.Clear();
	}
}
