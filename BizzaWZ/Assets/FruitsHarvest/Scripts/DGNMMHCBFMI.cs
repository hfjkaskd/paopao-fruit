using System;
using Spine.Unity;
using UnityEngine;
using UnityEngine.UI;

/// <summary>MyTween 静态入口：各种 Do* 扩展方法。</summary>
public static class DGNMMHCBFMI
{
	public static void Init()
	{
	}

	public static void Kill(uint tweenId)
	{
		if (Orange.MyTween.MyTweenCenter.Instance != null)
		{
			Orange.MyTween.MyTweenCenter.Instance.Kill(tweenId);
		}
	}

	private static AnimationCurve DefaultCurve(AnimationCurve curve)
	{
		if (curve != null)
		{
			return curve;
		}
		return MyTweenEase.Instance != null ? MyTweenEase.Instance.linner : null;
	}

	private static uint StartFloat(float endValue, float duration, Func<float> getter, Action<float> setter, Action complete, AnimationCurve curve)
	{
		PIINPFGBHJN t = new PIINPFGBHJN();
		t.Init(endValue, duration, DefaultCurve(curve), getter, setter, complete);
		Orange.MyTween.MyTweenCenter.Instance.AddTween(t);
		return t.TweenId;
	}

	private static uint StartVector(Vector3 endValue, float duration, Func<Vector3> getter, Action<Vector3> setter, Action complete, AnimationCurve curve)
	{
		GNJJPNOGLGP t = new GNJJPNOGLGP();
		t.Init(endValue, duration, DefaultCurve(curve), getter, setter, complete);
		Orange.MyTween.MyTweenCenter.Instance.AddTween(t);
		return t.TweenId;
	}

	private static uint StartColor(Color endValue, float duration, Func<Color> getter, Action<Color> setter, Action complete, AnimationCurve curve)
	{
		BNMDFFGAHEH t = new BNMDFFGAHEH();
		t.Init(endValue, duration, DefaultCurve(curve), getter, setter, complete);
		Orange.MyTween.MyTweenCenter.Instance.AddTween(t);
		return t.TweenId;
	}

	public static uint DoMove(this Transform transform, Vector3 endValue, float duration, Action complete = null, AnimationCurve curve = null)
	{
		return StartVector(endValue, duration, () => transform.position, v => transform.position = v, complete, curve);
	}

	public static uint DoMoveX(this Transform transform, float endValue, float duration, Action complete = null, AnimationCurve curve = null)
	{
		return StartFloat(endValue, duration, () => transform.position.x, v =>
		{
			Vector3 p = transform.position;
			p.x = v;
			transform.position = p;
		}, complete, curve);
	}

	public static uint DoMoveY(this Transform transform, float endValue, float duration, Action complete = null, AnimationCurve curve = null)
	{
		return StartFloat(endValue, duration, () => transform.position.y, v =>
		{
			Vector3 p = transform.position;
			p.y = v;
			transform.position = p;
		}, complete, curve);
	}

	public static uint DoMoveZ(this Transform transform, float endValue, float duration, Action complete = null, AnimationCurve curve = null)
	{
		return StartFloat(endValue, duration, () => transform.position.z, v =>
		{
			Vector3 p = transform.position;
			p.z = v;
			transform.position = p;
		}, complete, curve);
	}

	public static uint DoAnchorMove(this Transform transform, Vector2 endValue, float duration, Action complete = null, AnimationCurve curve = null)
	{
		RectTransform rect = transform as RectTransform;
		return StartVector(endValue, duration, () => rect.anchoredPosition, v => rect.anchoredPosition = v, complete, curve);
	}

	public static uint DoAnchorMoveX(this Transform transform, float endValue, float duration, Action complete = null, AnimationCurve curve = null)
	{
		RectTransform rect = transform as RectTransform;
		return StartFloat(endValue, duration, () => rect.anchoredPosition.x, v =>
		{
			Vector2 p = rect.anchoredPosition;
			p.x = v;
			rect.anchoredPosition = p;
		}, complete, curve);
	}

	public static uint DoAnchorMoveY(this Transform transform, float endValue, float duration, Action complete = null, AnimationCurve curve = null)
	{
		RectTransform rect = transform as RectTransform;
		return StartFloat(endValue, duration, () => rect.anchoredPosition.y, v =>
		{
			Vector2 p = rect.anchoredPosition;
			p.y = v;
			rect.anchoredPosition = p;
		}, complete, curve);
	}

	public static uint DoSizeDelta(this Transform transform, Vector2 endValue, float duration, Action complete = null, AnimationCurve curve = null)
	{
		RectTransform rect = transform as RectTransform;
		return StartVector(endValue, duration, () => rect.sizeDelta, v => rect.sizeDelta = v, complete, curve);
	}

	public static uint DoScale(this Transform transform, float endValue, float duration, Action complete = null, AnimationCurve curve = null)
	{
		return StartVector(Vector3.one * endValue, duration, () => transform.localScale, v => transform.localScale = v, complete, curve);
	}

	public static uint DoFade(this Image image, float endValue, float duration, Action complete = null, AnimationCurve curve = null)
	{
		return StartFloat(endValue, duration, () => image.color.a, v =>
		{
			Color c = image.color;
			c.a = v;
			image.color = c;
		}, complete, curve);
	}

	public static uint DoColor(this Image image, Color endValue, float duration, Action complete = null, AnimationCurve curve = null)
	{
		return StartColor(endValue, duration, () => image.color, v => image.color = v, complete, curve);
	}

	public static uint DoColor(this SkeletonGraphic spine, Color endValue, float duration, Action complete = null, AnimationCurve curve = null)
	{
		return StartColor(endValue, duration, () => spine.color, v => spine.color = v, complete, curve);
	}

	public static uint DoFillAmount(this Image image, float endValue, float duration, Action complete = null, AnimationCurve curve = null)
	{
		return StartFloat(endValue, duration, () => image.fillAmount, v => image.fillAmount = v, complete, curve);
	}

	public static uint DoFade(this CanvasGroup cg, float endValue, float duration, Action complete = null, AnimationCurve curve = null)
	{
		return StartFloat(endValue, duration, () => cg.alpha, v => cg.alpha = v, complete, curve);
	}

	public static uint DoNum(float endValue, float duration, Func<float> getter, Action<float> setter, Action complete = null, AnimationCurve curve = null)
	{
		return StartFloat(endValue, duration, getter, setter, complete, curve);
	}

	public static uint DoSliderValue(this Slider slider, float endValue, float duration, Action complete = null, AnimationCurve curve = null)
	{
		return StartFloat(endValue, duration, () => slider.value, v => slider.value = v, complete, curve);
	}
}
