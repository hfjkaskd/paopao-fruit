using UnityEngine;
using UnityEngine.UI;

namespace Orange
{
	public class DynamicCanvasLayer : MonoBehaviour
	{
		[Header("生效组件：Canvas")]
		[Header("如果是BaseUI写相对于根节点的偏移，如果不是写相对于最近的Canvas的偏移")]
		[SerializeField]
		private int offset;

		[Header("自动添加GraphicRaycaster")]
		[SerializeField]
		private bool addGraphicRaycaster;

		[Header("OnEnable时刷新层级（BaseUI中的不用勾）")]
		[SerializeField]
		private bool auto;

		public void SetLayer(int baseLayer)
		{
			Canvas canvas = GetComponent<Canvas>();
			if (canvas == null)
			{
				canvas = gameObject.AddComponent<Canvas>();
			}
			canvas.overrideSorting = true;
			canvas.sortingOrder = baseLayer + offset;
			if (addGraphicRaycaster && GetComponent<GraphicRaycaster>() == null)
			{
				gameObject.AddComponent<GraphicRaycaster>();
			}
		}

		/// <summary>按最近一个实际参与排序的父 Canvas 重新计算层级。</summary>
		public void RefreshFromParent()
		{
			Canvas parentCanvas = FindCanvas(transform.parent);
			if (parentCanvas != null)
			{
				SetLayer(parentCanvas.sortingOrder);
			}
		}

		/// <summary>
		/// 父节点优先刷新整棵子树，保证嵌套 Canvas 使用已经更新后的父层级。
		/// 仅刷新勾选 auto 的组件，避免改写由业务代码手动控制的层级。
		/// 不检查 enabled：水果动画会把该组件动画为 disabled，但换父节点时
		/// 仍必须显式更新它已经创建出来的 Canvas 排序。
		/// </summary>
		public static void RefreshInChildren(Transform root)
		{
			if (root == null)
			{
				return;
			}
			DynamicCanvasLayer[] layers = root.GetComponentsInChildren<DynamicCanvasLayer>(true);
			// GetComponentsInChildren 的返回顺序不作为业务约定；显式按深度排序，
			// 确保父 Canvas 一定先于子 Canvas 完成刷新。
			for (int i = 0; i < layers.Length - 1; i++)
			{
				int shallowest = i;
				int shallowestDepth = GetDepth(layers[i].transform, root);
				for (int j = i + 1; j < layers.Length; j++)
				{
					int depth = GetDepth(layers[j].transform, root);
					if (depth < shallowestDepth)
					{
						shallowest = j;
						shallowestDepth = depth;
					}
				}
				if (shallowest != i)
				{
					DynamicCanvasLayer temp = layers[i];
					layers[i] = layers[shallowest];
					layers[shallowest] = temp;
				}
			}
			for (int i = 0; i < layers.Length; i++)
			{
				DynamicCanvasLayer layer = layers[i];
				if (layer != null && layer.auto)
				{
					layer.RefreshFromParent();
				}
			}
		}

		private static int GetDepth(Transform current, Transform root)
		{
			int depth = 0;
			while (current != null && current != root)
			{
				depth++;
				current = current.parent;
			}
			return depth;
		}

		private void OnEnable()
		{
			if (!auto)
			{
				return;
			}
			RefreshFromParent();
		}

		private void OnTransformParentChanged()
		{
			if (auto && isActiveAndEnabled)
			{
				RefreshFromParent();
			}
		}

		private Canvas FindCanvas(Transform t)
		{
			while (t != null)
			{
				Canvas canvas = t.GetComponent<Canvas>();
				// 非 override 的嵌套 Canvas 不建立独立排序边界，其 sortingOrder
				// 不参与最终绘制；继续向上寻找真正生效的 Canvas。
				if (canvas != null && (canvas.overrideSorting || canvas.isRootCanvas))
				{
					return canvas;
				}
				t = t.parent;
			}
			return null;
		}
	}
}
