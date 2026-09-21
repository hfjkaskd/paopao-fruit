using System;
using UnityEngine;

public abstract class JLBIJKJEKKB<T> : IBBEOJMIHNF
{
	private T endValue;

	private float duration;

	private AnimationCurve curve;

	private Action<T> setter;

	private Action complete;

	protected T startValue;

	protected T dValue;

	private float time;

	public bool IsUsing { get; private set; }

	public uint TweenId { get; private set; }

	public void Init(T endValue, float duration, AnimationCurve curve, Func<T> getter, Action<T> setter, Action complete)
	{
		this.endValue = endValue;
		this.duration = Mathf.Max(0.0001f, duration);
		this.curve = curve;
		this.setter = setter;
		this.complete = complete;
		startValue = getter();
		SetDValue(startValue, endValue);
		time = 0f;
		IsUsing = true;
		TweenId = Orange.MyTween.MyTweenCenter.NextTweenId();
	}

	protected abstract void SetDValue(T startValue, T endValue);

	protected abstract T CalValue(float y);

	public void AddTime(float dt)
	{
		if (!IsUsing)
		{
			return;
		}
		time += dt;
		float x = Mathf.Clamp01(time / duration);
		float y = curve != null ? curve.Evaluate(x) : x;
		try
		{
			setter?.Invoke(x >= 1f ? endValue : CalValue(y));
		}
		catch (Exception)
		{
			// 目标对象已被销毁等情况：直接终止补间
			Kill();
			return;
		}
		if (x >= 1f)
		{
			IsUsing = false;
			Action c = complete;
			complete = null;
			c?.Invoke();
		}
	}

	public void Kill()
	{
		IsUsing = false;
		complete = null;
	}

	uint IBBEOJMIHNF.TweenId()
	{
		return TweenId;
	}
}
