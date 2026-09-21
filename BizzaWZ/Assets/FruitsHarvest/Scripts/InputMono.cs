using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InputMono : Button, IBeginDragHandler, IDragHandler, IEndDragHandler
{
	public new Action<GameObject> onClick;

	public Action<GameObject> onDown;

	public Action<GameObject> onUp;

	public Action<GameObject> onBeginDrag;

	public Action<GameObject> onDrag;

	public Action<GameObject> onEndDrag;

	public bool playClickSound = true;

	public bool LockSelf { get; set; }

	private bool Blocked => LockSelf || MCCIJBJGMCK.IsLock();

	protected override void Awake()
	{
		base.Awake();
		base.onClick.AddListener(Click);
		transition = Transition.None;
	}

	public void OnBeginDrag(PointerEventData eventData)
	{
		if (!Blocked)
		{
			onBeginDrag?.Invoke(gameObject);
		}
	}

	public void OnDrag(PointerEventData eventData)
	{
		if (!Blocked)
		{
			onDrag?.Invoke(gameObject);
		}
	}

	public void OnEndDrag(PointerEventData eventData)
	{
		if (!Blocked)
		{
			onEndDrag?.Invoke(gameObject);
		}
	}

	private void Click()
	{
		if (Blocked)
		{
			return;
		}
		if (playClickSound && onClick != null)
		{
			GameAudio.Play(DLMJOHCOJKN.Play_sfx_ui_button_common);
		}
		onClick?.Invoke(gameObject);
	}

	public override void OnPointerDown(PointerEventData eventData)
	{
		base.OnPointerDown(eventData);
		if (!Blocked)
		{
			onDown?.Invoke(gameObject);
		}
	}

	public override void OnPointerUp(PointerEventData eventData)
	{
		base.OnPointerUp(eventData);
		if (!Blocked)
		{
			onUp?.Invoke(gameObject);
		}
	}
}
