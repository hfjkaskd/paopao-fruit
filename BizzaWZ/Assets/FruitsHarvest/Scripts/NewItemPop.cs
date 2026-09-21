using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>新道具解锁弹窗：展示道具名与说明，Claim 后图标飞向道具入口。</summary>
public class NewItemPop : BaseUI
{
	private POJCEPBNNIP popType;

	[SerializeField]
	private GameObject m_ClaimBtn;

	[SerializeField]
	private Image m_ItemImage;

	[SerializeField]
	private TextMeshProCustom m_ItemName;

	[SerializeField]
	private TextMeshProCustom m_ItemDiscription;

	[SerializeField]
	private CommonSingleFly m_ItemFly;

	private static readonly Dictionary<POJCEPBNNIP, string> ItemTextKeyDict = new Dictionary<POJCEPBNNIP, string>
	{
		{ POJCEPBNNIP.Undo, "guide_undo" },
		{ POJCEPBNNIP.Shuffle, "guide_shuffle" },
		{ POJCEPBNNIP.Magic, "guide_magicwand" },
		{ POJCEPBNNIP.Extra, "guide_extraslot" }
	};

	private static readonly Dictionary<POJCEPBNNIP, string> ItemIconDict = new Dictionary<POJCEPBNNIP, string>
	{
		{ POJCEPBNNIP.Undo, "res/local/pops/newitempop/sprite/IconUndo" },
		{ POJCEPBNNIP.Shuffle, "res/local/pops/newitempop/sprite/IconShuffle" },
		{ POJCEPBNNIP.Magic, "res/local/pops/newitempop/sprite/IconMagic" },
		{ POJCEPBNNIP.Extra, "res/local/pops/newitempop/sprite/IconExtra" }
	};

	private bool claimed;

	public override PAIEAGDLCBJ Layer => PAIEAGDLCBJ.Top;


	protected override void Init()
	{
		if (m_ClaimBtn != null)
		{
			MCCIJBJGMCK.Get(m_ClaimBtn).onClick = OnClaimClick;
		}
	}

	protected override void BeforeOpen()
	{
		popType = (POJCEPBNNIP)JEFOMCDAPGK.Instance.Data.pendingNewItemPop;
		claimed = false;
		GameAudio.Play(DLMJOHCOJKN.Play_sfx_ui_panel_newProp_open);
		InitItemType();
	}

	private void InitItemType()
	{
		if (ItemTextKeyDict.TryGetValue(popType, out string key))
		{
			if (m_ItemName != null)
			{
				m_ItemName.text = OJEEJGGLNPC.Instance.GetText(key);
			}
			if (m_ItemDiscription != null)
			{
				m_ItemDiscription.text = OJEEJGGLNPC.Instance.GetText(key + "_text");
			}
		}
		if (m_ItemImage != null && ItemIconDict.TryGetValue(popType, out string iconPath))
		{
			Sprite icon = GameRes.LoadSprite(iconPath);
			if (icon != null)
			{
				m_ItemImage.sprite = icon;
				m_ItemImage.SetNativeSize();
			}
		}
	}

	private void OnClaimClick(GameObject go)
	{
		if (claimed || JEFOMCDAPGK.Instance.HasShownNewItemPop(popType) ||
			JEFOMCDAPGK.Instance.Data.pendingNewItemPop != (int)popType || !ItemTextKeyDict.ContainsKey(popType))
		{
			return;
		}
		claimed = true;
        JEFOMCDAPGK.Instance.MarkNewItemPopShown(popType);
		GameAudio.Play(DLMJOHCOJKN.Play_sfx_ui_panel_newProp_close);
		Vector3 targetPos = CorePlay.CorePlayUI.Instance != null
			? CorePlay.CorePlayUI.Instance.GetItemBtnWorldPosition(popType)
			: transform.position;
		if (m_ItemFly != null && m_ItemImage != null)
		{
			MCCIJBJGMCK.Lock();
			m_ItemFly.FlyObject(m_ItemImage.gameObject, m_ItemImage.gameObject, m_ItemImage.transform.position, targetPos, 1f, null, delegate
			{
				MCCIJBJGMCK.UnlockOnce();
				PlayEntryUnlockEffect();
				CloseSelf();
			});
		}
		else
		{
			PlayEntryUnlockEffect();
			CloseSelf();
		}
	}

	/// <summary>图标飞达后，对应入口播放解锁发光（录屏 20.8–21.8s 的入口引导发光）。</summary>
	private void PlayEntryUnlockEffect()
	{
		if (CorePlay.CorePlayUI.Instance == null)
		{
			return;
		}
		CorePlayItemBtn btn = CorePlay.CorePlayUI.Instance.GetItemBtn(popType);
		if (btn != null)
		{
			btn.PlayUnlockEffect();
		}
	}

	private void CloseSelf()
	{
		MgrUI.Instance.Close("pops/newitempop/NewItemPop", true, false);
	}
}
