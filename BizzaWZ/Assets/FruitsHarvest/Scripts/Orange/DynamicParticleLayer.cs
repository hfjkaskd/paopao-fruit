using UnityEngine;
using UnityEngine.Rendering;

namespace Orange
{
	[RequireComponent(typeof(Renderer))]
	public class DynamicParticleLayer : MonoBehaviour
	{
		[Header("生效组件：Renderer、SortingGroup")]
		[Header("如果是BaseUI写相对于根节点的偏移，如果不是写相对于最近的Canvas的偏移")]
		[SerializeField]
		private int offset;

		[Header("OnEnable时刷新层级（BaseUI中的不用勾）")]
		[SerializeField]
		private bool auto;

		public void SetLayer(int baseLayer)
		{
			SortingGroup group = GetComponent<SortingGroup>();
			if (group != null)
			{
				group.sortingOrder = baseLayer + offset;
				return;
			}
			Renderer r = GetComponent<Renderer>();
			if (r != null)
			{
				r.sortingOrder = baseLayer + offset;
			}
		}

		private void OnEnable()
		{
			if (!auto)
			{
				return;
			}
			Canvas parentCanvas = FindCanvas(transform.parent);
			if (parentCanvas != null)
			{
				SetLayer(parentCanvas.sortingOrder);
			}
		}

		private Canvas FindCanvas(Transform t)
		{
			while (t != null)
			{
				Canvas canvas = t.GetComponent<Canvas>();
				if (canvas != null)
				{
					return canvas;
				}
				t = t.parent;
			}
			return null;
		}
	}
}
