using System.Collections;
using System.Collections.Generic;
using TMPro;
using Framework.Base.UtilModule;
using Orange;
using UnityEngine;
using UnityEngine.UI;

namespace CorePlay
{
	/// <summary>局内主界面：游戏区、收集槽、道具按钮、金币、设置与引导。</summary>
	public class CorePlayUI : BaseUI
	{
		public static string EntryType;

		[SerializeField]
		public Transform m_GameLayer;

		[SerializeField]
		public Transform m_ChooseArea;

		[SerializeField]
		public RectTransform m_BgTrans;

		[SerializeField]
		public Image m_BgImg;

		private static string m_CurrentMapAbPath;

		[SerializeField]
		private CanvasGroup m_GameAreaGroup;

		[SerializeField]
		private RectTransform m_BottomRect;

		[SerializeField]
		public RectTransform m_CollectArea;

		[SerializeField]
		public RectTransform m_CollectFirstPos;

		[SerializeField]
		public CorePlayMoveParams moveParams;

		[SerializeField]
		public Transform shuffleGatherPoint;

		[SerializeField]
		private GameObject m_CommonCoinBtn;

		[SerializeField]
		private GameObject m_SettingBtn;

		[SerializeField]
		private Image m_SettingBtnImg;

		[SerializeField]
		private CorePlayItemBtn m_AddOneBtn;

		[SerializeField]
		private CorePlayItemBtn m_UndoBtn;

		[SerializeField]
		private CorePlayItemBtn m_MagicBtn;

		[SerializeField]
		private CorePlayItemBtn m_ShuffleBtn;

		[SerializeField]
		private List<GameObject> m_LevelTextGos;

		[SerializeField]
		private List<TextMeshProUGUI> m_LevelTexts;

		[SerializeField]
		private Animation m_MagicAni;

		[SerializeField]
		private GameObject m_ShuffleEff;

		[SerializeField]
		private Transform m_DangerousTipTrans;

		[SerializeField]
		private Transform m_NewPlayerGuiderRoot;

		[SerializeField]
		public Transform m_EffRoot;

		[SerializeField]
		public Transform m_EncouragePos;

		private int m_ShuffleEffBatchId;

		public static int m_CollectAreaInterval;

		public static int collectAreaRefSize;

		public static bool IsCoreplayUIOpening;

		private static readonly Dictionary<string, CNJGPJCNFBL.CHAFHHAPNLH> MapAudioDict = new Dictionary<string, CNJGPJCNFBL.CHAFHHAPNLH>
		{
			{ "1002", CNJGPJCNFBL.CHAFHHAPNLH.FarmDay },
			{ "1008", CNJGPJCNFBL.CHAFHHAPNLH.ForestFall },
			{ "1010", CNJGPJCNFBL.CHAFHHAPNLH.ForestDay },
			{ "1011", CNJGPJCNFBL.CHAFHHAPNLH.Sea },
			{ "1014", CNJGPJCNFBL.CHAFHHAPNLH.Rainforest },
			{ "1015", CNJGPJCNFBL.CHAFHHAPNLH.ForestDusk },
			{ "1016", CNJGPJCNFBL.CHAFHHAPNLH.Garden },
			{ "1017", CNJGPJCNFBL.CHAFHHAPNLH.Mountain },
			{ "1018", CNJGPJCNFBL.CHAFHHAPNLH.FarmDay },
			{ "1021", CNJGPJCNFBL.CHAFHHAPNLH.Winter }
		};

		private static readonly Dictionary<CNJGPJCNFBL.CHAFHHAPNLH, string> AmbienceClipDict = new Dictionary<CNJGPJCNFBL.CHAFHHAPNLH, string>
		{
			{ CNJGPJCNFBL.CHAFHHAPNLH.FarmDay, "sfx_amb_farm_day" },
			{ CNJGPJCNFBL.CHAFHHAPNLH.ForestDusk, "sfx_amb_forest_dusk" },
			{ CNJGPJCNFBL.CHAFHHAPNLH.ForestDay, "sfx_amb_forest_day" },
			{ CNJGPJCNFBL.CHAFHHAPNLH.ForestFall, "sfx_amb_forest_day_fall" },
			{ CNJGPJCNFBL.CHAFHHAPNLH.Sea, "sfx_amb_seaShore" },
			{ CNJGPJCNFBL.CHAFHHAPNLH.Rainforest, "sfx_amb_rainforest_day" },
			{ CNJGPJCNFBL.CHAFHHAPNLH.ForestNight, "sfx_amb_forest_night" },
			{ CNJGPJCNFBL.CHAFHHAPNLH.Desert, "sfx_amb_desert" },
			{ CNJGPJCNFBL.CHAFHHAPNLH.Winter, "sfx_amb_winter" },
			{ CNJGPJCNFBL.CHAFHHAPNLH.Mountain, "sfx_amb_mountain" },
			{ CNJGPJCNFBL.CHAFHHAPNLH.ForestRain, "sfx_amb_forest_rain" },
			{ CNJGPJCNFBL.CHAFHHAPNLH.Garden, "sfx_amb_garden" },
			{ CNJGPJCNFBL.CHAFHHAPNLH.MountainNight, "sfx_amb_mountain_night" },
			{ CNJGPJCNFBL.CHAFHHAPNLH.DesertNight, "sfx_amb_desert_night" }
		};

		private Vector3 localPos;

		public static CorePlayUI Instance { get; private set; }

		public override PAIEAGDLCBJ Layer => PAIEAGDLCBJ.Bottom;

		public override int OwnLayerCnt => 10;

		public bool IsMagicAniPlaying { get; private set; }

		public int PlayShuffleEff()
		{
			m_ShuffleEffBatchId++;
			if (m_ShuffleEff != null)
			{
				m_ShuffleEff.SetActive(false);
				m_ShuffleEff.SetActive(true);
			}
			return m_ShuffleEffBatchId;
		}

		public bool StopShuffleEff(int batchId)
		{
			if (batchId != m_ShuffleEffBatchId)
			{
				return false;
			}
			if (m_ShuffleEff != null)
			{
				m_ShuffleEff.SetActive(false);
			}
			return true;
		}

		protected override void Init()
		{
			Instance = this;
			if (m_CollectAreaInterval <= 0)
			{
				m_CollectAreaInterval = 123;
			}
			if (collectAreaRefSize <= 0)
			{
				collectAreaRefSize = 110;
			}
			if (m_SettingBtn != null)
			{
				MCCIJBJGMCK.Get(m_SettingBtn).onClick = OnSettingBtnClick;
			}
			
			InitAllItemBtn();
		}


		protected override void BeforeOpen()
		{
			IsCoreplayUIOpening = true;
			RefreshBasketSorting();
			UpdateBehavior();
		}

		/// <summary>
		/// BaseUI 会先按页面根层写入所有动态 Canvas；篮子内部则需要保持
		/// Box_Root -> 收集层的相对顺序，因此在页面打开后按父 Canvas 重算。
		/// </summary>
		private void RefreshBasketSorting()
		{
			if (m_CollectArea != null && m_CollectArea.parent != null)
			{
				DynamicCanvasLayer.RefreshInChildren(m_CollectArea.parent);
			}
		}

		public void UpdateBehavior()
		{
			UpdateText();
			UpdateSettingBtn();
			
			// 道具解锁等级依赖当前关卡索引，切关后需重刷按钮状态
			if (m_UndoBtn != null)
			{
				m_UndoBtn.Refresh();
			}
			if (m_MagicBtn != null)
			{
				m_MagicBtn.Refresh();
			}
			if (m_ShuffleBtn != null)
			{
				m_ShuffleBtn.Refresh();
			}
			if (m_AddOneBtn != null)
			{
				m_AddOneBtn.Refresh();
			}
		}

		private void UpdateText()
		{
			int level = JEFOMCDAPGK.Instance.CurMainLevelIndex;
			string levelStr = string.Format(OJEEJGGLNPC.Instance.GetText("common_levelwithindex"), level);
			if (m_LevelTexts != null)
			{
				foreach (TextMeshProUGUI text in m_LevelTexts)
				{
					if (text != null)
					{
						text.text = levelStr;
					}
				}
			}
			if (m_LevelTextGos != null)
			{
				for (int i = 0; i < m_LevelTextGos.Count; i++)
				{
					if (m_LevelTextGos[i] != null)
					{
						m_LevelTextGos[i].SetActive(i == 0);
					}
				}
			}
		}

		private void UpdateSettingBtn()
		{
			if (m_SettingBtn != null)
			{
				// 录屏：第1关（教学关）不显示设置按钮
				m_SettingBtn.SetActive(JEFOMCDAPGK.Instance.CurMainLevelIndex > 1);
			}
		}

		protected override void AfterOpen()
		{
			StartLevelFlow();
		}

		private void StartLevelFlow()
		{
			HarvestBridge.StartLevel(this);
	}

		protected override void BeforeClose()
		{
			IsCoreplayUIOpening = false;
			GameAudio.StopAmbience();
			
		}

		protected override void AfterClose()
		{
			m_CurrentMapAbPath = null;
		}

		private void Update()
		{
			if (IsCoreplayUIOpening && !JEFOMCDAPGK.Instance.Data.isLevelFinished)
			{
				JEFOMCDAPGK.Instance.MainLevelData.TickSessionTimer(Time.deltaTime);
			}
		}

		public void InitAllItemBtn()
		{
			if (m_UndoBtn != null)
			{
				m_UndoBtn.Init(POJCEPBNNIP.Undo);
			}
			if (m_MagicBtn != null)
			{
				m_MagicBtn.Init(POJCEPBNNIP.Magic);
			}
			if (m_ShuffleBtn != null)
			{
				m_ShuffleBtn.Init(POJCEPBNNIP.Shuffle);
			}
			if (m_AddOneBtn != null)
			{
				m_AddOneBtn.Init(POJCEPBNNIP.Extra);
			}
		}

		public Vector3 GetItemBtnWorldPosition(POJCEPBNNIP itemType)
		{
			return HarvestBridge.GetPropPosition(itemType);
	}

		public CorePlayItemBtn GetItemBtn(POJCEPBNNIP itemType)
		{
			switch (itemType)
			{
			case POJCEPBNNIP.Undo:
				return m_UndoBtn;
			case POJCEPBNNIP.Magic:
				return m_MagicBtn;
			case POJCEPBNNIP.Shuffle:
				return m_ShuffleBtn;
			case POJCEPBNNIP.Extra:
				return m_AddOneBtn;
			default:
				return null;
			}
		}

		private void OnSettingBtnClick(GameObject InGo)
		{
			HarvestBridge.OpenSettings();
		}

		public void ChangeGameBg(string InMapID)
		{
			string path = JEFOMCDAPGK.GetMapBundlePath(InMapID);
			if (path == m_CurrentMapAbPath)
			{
				return;
			}
			bool isSwitch = !string.IsNullOrEmpty(m_CurrentMapAbPath);
			m_CurrentMapAbPath = path;
			Sprite mapSprite = GameRes.LoadSprite(path);
			if (mapSprite != null && m_BgImg != null)
			{
				m_BgImg.sprite = mapSprite;
			}
			if (isSwitch)
			{
				GameAudio.Play(DLMJOHCOJKN.Play_sfx_anim_game_backgroundChange);
			}
			if (MapAudioDict.TryGetValue(InMapID ?? string.Empty, out CNJGPJCNFBL.CHAFHHAPNLH ambience) && AmbienceClipDict.TryGetValue(ambience, out string clipName))
			{
				GameAudio.PlayAmbience(clipName);
			}
			else
			{
				GameAudio.PlayAmbience("sfx_amb_farm_day");
			}
		}

		public void SetGameAreaAlpha(float alpha)
		{
			if (m_GameAreaGroup != null)
			{
				m_GameAreaGroup.alpha = alpha;
			}
		}

		/// <summary>把配置坐标（已按 1080 画布单位）转换为世界坐标。</summary>
		public Vector3 GetItemWorldPosition(int InPosX, int InPosY)
		{
			Transform layer = m_BgTrans != null ? m_BgTrans.Find("Layer") : null;
			if (layer == null)
			{
				layer = m_GameLayer;
			}
			return layer != null ? layer.TransformPoint(new Vector3(InPosX, InPosY, 0f)) : Vector3.zero;
		}

		public void UpdateAddOneBtn(bool InIsHide, bool WithAni)
		{
			if (m_AddOneBtn == null)
			{
				return;
			}
			if (InIsHide && WithAni)
			{
				m_AddOneBtn.PlayAddOneLockAnim();
				Timer.Instance.Delay(0.3f, delegate
				{
					if (m_AddOneBtn != null)
					{
						m_AddOneBtn.gameObject.SetActive(false);
					}
				});
			}
			else
			{
				m_AddOneBtn.gameObject.SetActive(!InIsHide);
			}
		}

		public void ShowMagicAni()
		{
			if (m_MagicAni != null)
			{
				m_MagicAni.gameObject.SetActive(true);
				if (m_MagicAni["anim_skill_MagicWand"] != null)
				{
					IsMagicAniPlaying = true;
					m_MagicAni.Play("anim_skill_MagicWand");
					CoroutineManager.Instance.StartCor(WaitMagicAniEnd());
				}
			}
			GameAudio.Play(DLMJOHCOJKN.Play_sfx_notice_prop_magicWand);
		}

		private IEnumerator WaitMagicAniEnd()
		{
			float length = m_MagicAni != null && m_MagicAni["anim_skill_MagicWand"] != null ? m_MagicAni["anim_skill_MagicWand"].length : 0.5f;
			yield return new WaitForSeconds(length);
			StopMagicAni();
		}

		public void StopMagicAni()
		{
			IsMagicAniPlaying = false;
			if (m_MagicAni != null)
			{
				m_MagicAni.Stop();
				m_MagicAni.gameObject.SetActive(false);
			}
		}

		public void PlayMagicAni()
		{
			ShowMagicAni();
		}

		public void ShowWillFillTip(Vector3 InTargetPos, int InGridNum)
		{
			if (m_DangerousTipTrans == null)
			{
				return;
			}
			GameObject prefab = GameRes.LoadPrefab("res/local/coreplay/prefab/DangerousTip");
			if (prefab == null)
			{
				return;
			}
			GameObject tipGo = Instantiate(prefab, m_DangerousTipTrans, false);
			Animation anim = tipGo.GetComponent<Animation>();
			if (anim != null && anim["anim_DangerousTip_open"] != null)
			{
				anim.Play("anim_DangerousTip_open");
				if (anim["anim_DangerousTip_loop"] != null)
				{
					anim.PlayQueued("anim_DangerousTip_loop", QueueMode.CompleteOthers);
				}
			}
			Timer.Instance.Delay(2.5f, delegate
			{
				if (tipGo == null)
				{
					return;
				}
				Animation closeAnim = tipGo.GetComponent<Animation>();
				if (closeAnim != null && closeAnim["anim_DangerousTip_close"] != null)
				{
					closeAnim.Play("anim_DangerousTip_close");
					Timer.Instance.Delay(closeAnim["anim_DangerousTip_close"].length, delegate
					{
						if (tipGo != null)
						{
							Destroy(tipGo);
						}
					});
				}
				else
				{
					Destroy(tipGo);
				}
			});
		}

		public void TryStartGuide()
		{
			if (SaveDataUtils.GameData.customTutorialEnd || NewPlayerGuider.Instance != null)
			{
				return;
			}
			if (m_NewPlayerGuiderRoot == null)
			{
				return;
			}
			GameObject prefab = GameRes.LoadPrefab("res/local/coreplay/prefab/NewPlayerGuider");
			if (prefab == null)
			{
				return;
			}
			GameObject guiderGo = Instantiate(prefab, m_NewPlayerGuiderRoot, false);
			NewPlayerGuider guider = guiderGo.GetComponent<NewPlayerGuider>();
			if (guider != null)
			{
				guider.StartGuide(EDLHEMMBABM.Instance.GetTotalItems(), null);
			}
		}
	}
}
