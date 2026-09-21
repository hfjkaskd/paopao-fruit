using System;
using UnityEngine;

/// <summary>批量金币飞行：从起点散开生成，错峰飞向终点。</summary>
public class CommonFlyScript : MonoBehaviour
{
	[Header("飞金币参数")]
	[SerializeField]
	private Transform m_DefaultEndPos;

	[SerializeField]
	private Transform m_FlyObjectParent;

	[SerializeField]
	private float staggerDelay;

	[SerializeField]
	private float duration;

	[SerializeField]
	private float arcHeight;

	[SerializeField]
	private AnimationCurve flyCurve;

	[SerializeField]
	private float spawnSpreadX;

	[SerializeField]
	private float spawnSpreadY;

	[SerializeField]
	private float endDelay;

	[SerializeField]
	private float targetScale;

	[SerializeField]
	private AnimationCurve scaleCurve;

	[SerializeField]
	private string objectAppearAniName;

	[SerializeField]
	private float appearAniTime;

	private IHJFHNCGPKF objectPool;

	public void Init(IHJFHNCGPKF InObjectPool)
	{
		objectPool = InObjectPool;
	}

	public void DoFlyCoins(int InNum, Vector3 InStartPos, Vector3? InEndpos = null, Action InFirtEndAction = null, Action InEndAction = null, Action<int> InOnCoinLanded = null)
	{
		if (objectPool == null || InNum <= 0)
		{
			InFirtEndAction?.Invoke();
			InEndAction?.Invoke();
			return;
		}
		Vector3 end = InEndpos ?? (m_DefaultEndPos != null ? m_DefaultEndPos.position : transform.position);
		float flyDuration = duration > 0f ? duration : 0.6f;
		float stagger = staggerDelay > 0f ? staggerDelay : 0.05f;
		int landed = 0;
		bool firstLanded = false;
		for (int i = 0; i < InNum; i++)
		{
			int idx = i;
			Timer.Instance.Delay(idx * stagger, delegate
			{
				GameObject coin = objectPool.Get(m_FlyObjectParent != null ? m_FlyObjectParent : transform);
				Vector3 spawnOffset = new Vector3(UnityEngine.Random.Range(-spawnSpreadX, spawnSpreadX), UnityEngine.Random.Range(-spawnSpreadY, spawnSpreadY), 0f);
				Vector3 myStart = InStartPos + spawnOffset;
				coin.transform.position = myStart;
				coin.transform.localScale = Vector3.one;
				Animation appearAnim = coin.GetComponent<Animation>();
				if (appearAnim != null && !string.IsNullOrEmpty(objectAppearAniName) && appearAnim[objectAppearAniName] != null)
				{
					appearAnim.Play(objectAppearAniName);
				}
				float wait = appearAniTime > 0f ? appearAniTime : 0.15f;
				Timer.Instance.Delay(wait, delegate
				{
					Vector3 mid = (myStart + end) * 0.5f + Vector3.up * arcHeight;
					float progress = 0f;
					DGNMMHCBFMI.DoNum(1f, flyDuration, () => progress, x =>
					{
						progress = x;
						if (coin != null)
						{
							Vector3 a = Vector3.Lerp(myStart, mid, x);
							Vector3 b = Vector3.Lerp(mid, end, x);
							coin.transform.position = Vector3.Lerp(a, b, x);
							float st = scaleCurve != null && scaleCurve.length > 0 ? scaleCurve.Evaluate(x) : x;
							float scale = Mathf.LerpUnclamped(1f, targetScale > 0f ? targetScale : 0.6f, st);
							coin.transform.localScale = Vector3.one * scale;
						}
					}, delegate
					{
						objectPool.Release(coin);
						landed++;
						InOnCoinLanded?.Invoke(landed);
						if (!firstLanded)
						{
							firstLanded = true;
							InFirtEndAction?.Invoke();
						}
						if (landed >= InNum)
						{
							Timer.Instance.Delay(Mathf.Max(endDelay, 0f), delegate
							{
								InEndAction?.Invoke();
							});
						}
					}, flyCurve);
				});
			});
		}
	}
}
