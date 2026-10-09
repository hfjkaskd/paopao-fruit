using UnityEngine;

namespace Orange
{
	// The original clip references remain on the prefab; CorePlayItemBtn handles pointer feedback.
	[RequireComponent(typeof(Animation))]
	public class BehavClickBtnAni : MonoBehaviour
	{
		[SerializeField] private Animation scaleAni;
		[SerializeField] private AnimationClip clipDown;
		[SerializeField] private AnimationClip clipUp;
	}
}
