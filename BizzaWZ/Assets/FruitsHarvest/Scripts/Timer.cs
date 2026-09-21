using System;
using System.Collections;
using UnityEngine;

public class Timer : BaseSingleMono<Timer>
{
	private float timerForSecondAdd;

	private float timerForMinuteAdd;

	private DateTime date;

	private DateTime newDate;

	public static event Action OnSecond;

	public static event Action OnMinute;

	public void Init()
	{
		date = DateTime.Now;
	}

	public void Delay(float time, Action action)
	{
		if (action == null)
		{
			return;
		}
		if (time <= 0f)
		{
			action();
			return;
		}
		StartCoroutine(DelayI(time, action));
	}

	public void DelayFrames(int frames, Action action)
	{
		if (action == null)
		{
			return;
		}
		StartCoroutine(DelayFramesI(frames, action));
	}

	private IEnumerator DelayI(float time, Action action)
	{
		yield return new WaitForSeconds(time);
		action();
	}

	private IEnumerator DelayFramesI(int frames, Action action)
	{
		for (int i = 0; i < frames; i++)
		{
			yield return null;
		}
		action();
	}

	private void Update()
	{
		timerForSecondAdd += Time.unscaledDeltaTime;
		if (timerForSecondAdd >= 1f)
		{
			timerForSecondAdd -= 1f;
			OnSecond?.Invoke();
		}
		timerForMinuteAdd += Time.unscaledDeltaTime;
		if (timerForMinuteAdd >= 60f)
		{
			timerForMinuteAdd -= 60f;
			OnMinute?.Invoke();
		}
	}
}
