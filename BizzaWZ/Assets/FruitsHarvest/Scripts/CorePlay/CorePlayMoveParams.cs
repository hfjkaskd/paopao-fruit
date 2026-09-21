using System;
using UnityEngine;

namespace CorePlay
{
	[Serializable]
	public class CorePlayMoveParams : MonoBehaviour
	{
		[Header("飞入参数（场外 → 收集区）")]
		public AnimationCurve enterCurve;

		public float enterSpeed;

		public float maxArcHeight;

		[Header("区内移动参数（收集区内重排）")]
		public AnimationCurve reorderCurve;

		public float reorderSpeed;

		[Header("洗牌 — 阶段1: Pitch抖动")]
		public AnimationCurve shufflePhase1Curve;

		public float shufflePhase1Speed;

		[Header("洗牌 — 阶段2: 聚拢中心")]
		public AnimationCurve shufflePhase2Curve;

		public float shufflePhase2SpeedMin;

		public float shufflePhase2SpeedMax;

		public float shufflePhase2SpiralAngleMin;

		public float shufflePhase2SpiralAngleMax;

		public float shufflePhase2CenterRadius;

		[Header("洗牌 — 阶段3: 散开新位")]
		public AnimationCurve shufflePhase3Curve;

		public float shufflePhase3SpeedMin;

		public float shufflePhase3SpeedMax;
	}
}
