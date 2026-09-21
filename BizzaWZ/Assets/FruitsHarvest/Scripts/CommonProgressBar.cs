using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>通用进度条：通过前景条宽度表现进度。</summary>
public class CommonProgressBar : MonoBehaviour
{
	[SerializeField]
	private RectTransform m_ProgressFg;

	[SerializeField]
	private float m_ProgressFullLength;

	private List<uint> numChangeProgressList = new List<uint>();

	private float curProportion;

	public void SetActive(bool InState)
	{
		gameObject.SetActive(InState);
	}

	public void SetStartProgress(float InStartProportion)
	{
		KillTweens();
		curProportion = Mathf.Clamp01(InStartProportion);
		ApplyProportion(curProportion);
	}

	public void DoProgress(float InEndProportion, float InTime, AnimationCurve InAnimationCurve, Action InEndAction = null)
	{
		KillTweens();
		float end = Mathf.Clamp01(InEndProportion);
		uint id = DGNMMHCBFMI.DoNum(end, InTime, () => curProportion, x =>
		{
			curProportion = x;
			if (this != null)
			{
				ApplyProportion(x);
			}
		}, delegate
		{
			if (this != null)
			{
				curProportion = end;
				ApplyProportion(end);
				InEndAction?.Invoke();
			}
		}, InAnimationCurve);
		numChangeProgressList.Add(id);
	}

	private void ApplyProportion(float proportion)
	{
		if (m_ProgressFg != null)
		{
			Vector2 size = m_ProgressFg.sizeDelta;
			size.x = m_ProgressFullLength * proportion;
			m_ProgressFg.sizeDelta = size;
		}
	}

	private void KillTweens()
	{
		foreach (uint id in numChangeProgressList)
		{
			DGNMMHCBFMI.Kill(id);
		}
		numChangeProgressList.Clear();
	}
}
