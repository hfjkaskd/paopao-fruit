using System.Collections.Generic;
using UnityEngine;

namespace Orange.MyTween
{
	public class MyTweenCenter : MonoBehaviour
	{
		private readonly List<IBBEOJMIHNF> tweens = new List<IBBEOJMIHNF>();

		private readonly List<IBBEOJMIHNF> tweensUpdate = new List<IBBEOJMIHNF>();

		private static uint nextTweenId = 1u;

		public static MyTweenCenter Instance { get; private set; }

		private void Awake()
		{
			Instance = this;
		}

		public static uint NextTweenId()
		{
			return nextTweenId++;
		}

		private void Update()
		{
			if (tweens.Count == 0)
			{
				return;
			}
			tweensUpdate.Clear();
			tweensUpdate.AddRange(tweens);
			float dt = Time.deltaTime;
			for (int i = 0; i < tweensUpdate.Count; i++)
			{
				tweensUpdate[i].AddTime(dt);
			}
			// 移除已完成的补间
			for (int i = tweens.Count - 1; i >= 0; i--)
			{
				if (tweens[i] is PIINPFGBHJN f && !f.IsUsing)
				{
					tweens.RemoveAt(i);
				}
				else if (tweens[i] is GNJJPNOGLGP v && !v.IsUsing)
				{
					tweens.RemoveAt(i);
				}
				else if (tweens[i] is BNMDFFGAHEH c && !c.IsUsing)
				{
					tweens.RemoveAt(i);
				}
			}
		}

		public void AddTween(IBBEOJMIHNF oneTween)
		{
			if (oneTween != null && !tweens.Contains(oneTween))
			{
				tweens.Add(oneTween);
			}
		}

		public void RemoveTween(IBBEOJMIHNF oneTween)
		{
			tweens.Remove(oneTween);
		}

		public bool Contains(IBBEOJMIHNF oneTween)
		{
			return tweens.Contains(oneTween);
		}

		public static bool Equals(float a, float b)
		{
			return Mathf.Abs(a - b) < 0.0001f;
		}

		public void Kill(uint tweenId)
		{
			for (int i = tweens.Count - 1; i >= 0; i--)
			{
				if (tweens[i].TweenId() == tweenId)
				{
					tweens[i].Kill();
					tweens.RemoveAt(i);
				}
			}
		}
	}
}
