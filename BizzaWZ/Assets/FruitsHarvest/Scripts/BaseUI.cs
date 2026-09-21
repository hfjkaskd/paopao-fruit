using System;
using Orange;
using UnityEngine;
using UnityEngine.UI;

public abstract class BaseUI : MonoBehaviour
{
	private Animation ani;

	private bool inited;

	public string m_OpenAniName;

	public string m_IdleAniName;

	public string m_CloseAniName;

	public int MyOrder { get; private set; }

	/// <summary>本页面占用的动态层级数量（子 Canvas / 粒子层偏移的最大值）。</summary>
	public virtual int OwnLayerCnt => 0;

	public virtual PAIEAGDLCBJ Layer => default(PAIEAGDLCBJ);

	public bool IsOpening { get; private set; }

	public virtual void AfterPreload()
	{
	}

	protected abstract void Init();

	protected abstract void BeforeOpen();

	protected virtual void SetAniName()
	{
	}

	protected virtual void AfterOpen()
	{
	}

	protected virtual void BeforeClose()
	{
	}

	protected virtual void AfterClose()
	{
	}

	protected virtual void WhenDestroy()
	{
	}

	private void TryInit()
	{
		if (inited)
		{
			return;
		}
		inited = true;
		ani = GetComponent<Animation>();
		// UI 根节点缩进安全区；带 ProtectedAreaAdaptReverse 的节点（背景/全屏页）会自行扩回全屏
		SafeAreaSim.InsetRect(transform as RectTransform);
		SetAniName();
		Init();
	}

	private void ApplyOrder(int order)
	{
		MyOrder = order;
		Canvas canvas = GetComponent<Canvas>();
		if (canvas == null)
		{
			canvas = gameObject.AddComponent<Canvas>();
		}
		canvas.overrideSorting = true;
		canvas.sortingOrder = order;
		if (GetComponent<GraphicRaycaster>() == null)
		{
			gameObject.AddComponent<GraphicRaycaster>();
		}
		DynamicCanvasLayer[] canvasLayers = GetComponentsInChildren<DynamicCanvasLayer>(true);
		for (int i = 0; i < canvasLayers.Length; i++)
		{
			canvasLayers[i].SetLayer(order);
		}
		DynamicParticleLayer[] particleLayers = GetComponentsInChildren<DynamicParticleLayer>(true);
		for (int i = 0; i < particleLayers.Length; i++)
		{
			particleLayers[i].SetLayer(order);
		}
	}

	private float PlayAni(string aniName)
	{
		if (ani == null || string.IsNullOrEmpty(aniName))
		{
			return 0f;
		}
		AnimationState state = ani[aniName];
		if (state == null)
		{
			return 0f;
		}
		ani.Play(aniName);
		return state.length;
	}

	private void PlayIdle()
	{
		if (ani != null && !string.IsNullOrEmpty(m_IdleAniName) && ani[m_IdleAniName] != null)
		{
			ani.Play(m_IdleAniName);
		}
	}

	public void BaseUIOpen(int order)
	{
		TryInit();
		ApplyOrder(order);
		BeforeOpen();
		gameObject.SetActive(true);
		IsOpening = true;
		float openTime = PlayAni(m_OpenAniName);
		if (openTime > 0f)
		{
			MCCIJBJGMCK.Lock();
			Timer.Instance.Delay(openTime, delegate
			{
				MCCIJBJGMCK.UnlockOnce();
				if (this != null)
				{
					PlayIdle();
					AfterOpen();
				}
			});
		}
		else
		{
			PlayIdle();
			AfterOpen();
		}
	}

	public void BaseUIClose(bool needDestroy)
	{
		if (!IsOpening)
		{
			return;
		}
		IsOpening = false;
		BeforeClose();
		float closeTime = PlayAni(m_CloseAniName);
		Action finish = delegate
		{
			if (this == null)
			{
				return;
			}
			AfterClose();
			if (needDestroy)
			{
				WhenDestroy();
				UnityEngine.Object.Destroy(gameObject);
			}
			else
			{
				gameObject.SetActive(false);
			}
		};
		if (closeTime > 0f)
		{
			MCCIJBJGMCK.Lock();
			Timer.Instance.Delay(closeTime, delegate
			{
				MCCIJBJGMCK.UnlockOnce();
				finish();
			});
		}
		else
		{
			finish();
		}
	}
}
