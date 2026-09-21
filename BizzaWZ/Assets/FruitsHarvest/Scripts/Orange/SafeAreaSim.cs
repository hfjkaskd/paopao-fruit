using UnityEngine;

namespace Orange
{
	/// <summary>
	/// 安全区（画布像素，基于 1080 宽参考画布）。
	/// 录屏设备为带刘海/手势条的 1080×2400 手机：顶部约 100px、底部约 195px。
	/// 桌面运行时 Screen.safeArea 无嵌入，默认采用录屏设备的模拟值以对齐目标录屏；
	/// 可用命令行 -safearea top,bottom 覆盖（如 -safearea 0,0 关闭）。
	/// </summary>
	public static class SafeAreaSim
	{
		private const float DEFAULT_TOP = 100f;

		private const float DEFAULT_BOTTOM = 195f;

		private static bool parsed;

		private static float top;

		private static float bottom;

		public static float Top
		{
			get
			{
				EnsureParsed();
				return top;
			}
		}

		public static float Bottom
		{
			get
			{
				EnsureParsed();
				return bottom;
			}
		}

		private static void EnsureParsed()
		{
			if (parsed)
			{
				return;
			}
			parsed = true;
			top = 0f;
			bottom = 0f;
			string[] args = System.Environment.GetCommandLineArgs();
			for (int i = 0; i < args.Length - 1; i++)
			{
				if (args[i] == "-safearea")
				{
					string[] parts = args[i + 1].Split(',');
					if (parts.Length >= 1)
					{
						float.TryParse(parts[0], out top);
					}
					if (parts.Length >= 2)
					{
						float.TryParse(parts[1], out bottom);
					}
					return;
				}
			}
			// 真机：若系统报告了安全区，优先使用（转换为画布像素）
			Rect safe = Screen.safeArea;
			if (safe.width > 0f && (safe.yMin > 0f || safe.yMax < Screen.height))
			{
				float toCanvas = 1080f / Screen.width;
				top = (Screen.height - safe.yMax) * toCanvas;
				bottom = safe.yMin * toCanvas;
			}
		}

		/// <summary>把 RectTransform 缩进安全区（要求纵向 stretch 锚点）。</summary>
		public static void InsetRect(RectTransform rt, bool topOnly = false)
		{
			if (rt == null || Mathf.Approximately(rt.anchorMin.y, rt.anchorMax.y))
			{
				return;
			}
			rt.offsetMax += new Vector2(0f, -Top);
			if (!topOnly)
			{
				rt.offsetMin += new Vector2(0f, Bottom);
			}
		}

		/// <summary>把（父链已缩进的）RectTransform 反向扩回全屏。</summary>
		public static void ExpandRect(RectTransform rt, bool changeSizeDelta)
		{
			if (rt == null)
			{
				return;
			}
			if (!Mathf.Approximately(rt.anchorMin.y, rt.anchorMax.y))
			{
				rt.offsetMax += new Vector2(0f, Top);
				rt.offsetMin += new Vector2(0f, -Bottom);
				return;
			}
			// 非 stretch：尺寸补偿 + 按锚点位置回移，保持绝对位置与覆盖范围
			if (changeSizeDelta)
			{
				rt.sizeDelta += new Vector2(0f, Top + Bottom);
			}
			float anchorY = rt.anchorMin.y;
			rt.anchoredPosition += new Vector2(0f, Top * anchorY - Bottom * (1f - anchorY));
		}
	}
}
