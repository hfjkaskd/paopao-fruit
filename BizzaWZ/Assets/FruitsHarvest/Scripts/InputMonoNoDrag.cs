using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InputMonoNoDrag : Button
{
	public new Action<GameObject> onClick;

	public Action<GameObject> onDown;

	public Action<GameObject> onUp;

	public bool playClickSound = true;

	private bool Blocked => MCCIJBJGMCK.IsLock();

	protected override void Awake()
	{
		base.Awake();
		base.onClick.AddListener(Click);
		transition = Transition.None;
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
