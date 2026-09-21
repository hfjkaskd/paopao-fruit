using UnityEngine;
using UnityEngine.UI;

namespace Orange
{
	/// <summary>不产生任何网格的透明射线接收器，用于全屏点击拦截。</summary>
	[RequireComponent(typeof(CanvasRenderer))]
	public class NonImageGraphic : Graphic
	{
		public override void SetMaterialDirty()
		{
		}

		public override void SetVerticesDirty()
		{
		}

		protected override void OnPopulateMesh(VertexHelper vh)
		{
			vh.Clear();
		}
	}
}
