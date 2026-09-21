using UnityEngine;
using UnityEngine.EventSystems;

namespace Orange
{
	[RequireComponent(typeof(Animation))]
	public class BehavClickBtnAni : MonoBehaviour, IPointerDownHandler, IEventSystemHandler, IPointerUpHandler
	{
		[SerializeField]
		private Animation scaleAni;

		[SerializeField]
		private AnimationClip clipDown;

		[SerializeField]
		private AnimationClip clipUp;

		private void Reset()
		{
		}

		public void OnPointerDown(PointerEventData eventData)
		{
		}

		public void OnPointerUp(PointerEventData eventData)
		{
		}

		private void OnDisable()
		{
		}
	}
}
