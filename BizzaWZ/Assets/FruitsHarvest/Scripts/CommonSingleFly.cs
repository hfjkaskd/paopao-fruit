using System;
using UnityEngine;

/// <summary>单对象弧线飞行（带缩放曲线）。</summary>
public class CommonSingleFly : MonoBehaviour
{
	[Header("单个飞行")]
	[SerializeField]
	private float duration;

	[SerializeField]
	private float arcHeight;

	[SerializeField]
	private AnimationCurve flyCurve;

	[SerializeField]
	private float endDelay;

	[SerializeField]
	private float startScale;

	[SerializeField]
	private float targetScale;

	[SerializeField]
	private AnimationCurve scaleCurve;

	public void FlyObject(GameObject InFlyGo, GameObject InScaleGo, Vector3 InStartPos, Vector3 InEndPos, float InDirection, Action InFlyEndAction, Action InAfterEndDelayAction)
	{
		if (InFlyGo == null)
		{
			InFlyEndAction?.Invoke();
			InAfterEndDelayAction?.Invoke();
			return;
		}
		float flyDuration = duration > 0f ? duration : 0.5f;
		float sScale = startScale > 0f ? startScale : 1f;
		float tScale = targetScale > 0f ? targetScale : 1f;
		Vector3 start = InStartPos;
		Vector3 end = InEndPos;
		Vector3 mid = (start + end) * 0.5f + new Vector3(InDirection * arcHeight * 0.5f, arcHeight, 0f);
		InFlyGo.transform.position = start;
		Transform scaleTrans = InScaleGo != null ? InScaleGo.transform : InFlyGo.transform;
		scaleTrans.localScale = Vector3.one * sScale;
		float progress = 0f;
		DGNMMHCBFMI.DoNum(1f, flyDuration, () => progress, x =>
		{
			progress = x;
			if (InFlyGo == null)
			{
				return;
			}
			Vector3 a = Vector3.Lerp(start, mid, x);
			Vector3 b = Vector3.Lerp(mid, end, x);
			InFlyGo.transform.position = Vector3.Lerp(a, b, x);
			float st = scaleCurve != null && scaleCurve.length > 0 ? scaleCurve.Evaluate(x) : x;
			scaleTrans.localScale = Vector3.one * Mathf.LerpUnclamped(sScale, tScale, st);
		}, delegate
		{
			if (InFlyGo != null)
			{
				InFlyGo.transform.position = end;
				scaleTrans.localScale = Vector3.one * tScale;
			}
			InFlyEndAction?.Invoke();
			Timer.Instance.Delay(Mathf.Max(endDelay, 0f), delegate
			{
				InAfterEndDelayAction?.Invoke();
			});
		}, flyCurve);
	}
}
