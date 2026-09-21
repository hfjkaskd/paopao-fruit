using System.Collections.Generic;
using UnityEngine;

/// <summary>OBB 与多边形裁剪数学工具，用于精确遮挡率计算。</summary>
public static class DHNOEMBJBIL
{
	public struct ILLJPIBKBGF
	{
		public Vector2 center;

		public Vector2 halfExtents;

		public float angleRad;

		public Vector2[] GetVertices()
		{
			Vector2[] verts = new Vector2[4];
			GetVerticesNonAlloc(verts);
			return verts;
		}

		public void GetVerticesNonAlloc(Vector2[] outVerts)
		{
			float cos = Mathf.Cos(angleRad);
			float sin = Mathf.Sin(angleRad);
			Vector2 ax = new Vector2(cos, sin) * halfExtents.x;
			Vector2 ay = new Vector2(-sin, cos) * halfExtents.y;
			outVerts[0] = center - ax - ay;
			outVerts[1] = center + ax - ay;
			outVerts[2] = center + ax + ay;
			outVerts[3] = center - ax + ay;
		}

		public Rect GetAABB()
		{
			Vector2[] v = GetVertices();
			float minX = Mathf.Min(Mathf.Min(v[0].x, v[1].x), Mathf.Min(v[2].x, v[3].x));
			float maxX = Mathf.Max(Mathf.Max(v[0].x, v[1].x), Mathf.Max(v[2].x, v[3].x));
			float minY = Mathf.Min(Mathf.Min(v[0].y, v[1].y), Mathf.Min(v[2].y, v[3].y));
			float maxY = Mathf.Max(Mathf.Max(v[0].y, v[1].y), Mathf.Max(v[2].y, v[3].y));
			return new Rect(minX, minY, maxX - minX, maxY - minY);
		}

		public float Area()
		{
			return 4f * halfExtents.x * halfExtents.y;
		}

		/// <summary>以同心方式取面积比例为 areaRatio 的中心 OBB。</summary>
		public ILLJPIBKBGF CreateCenterOBB(float areaRatio)
		{
			float scale = Mathf.Sqrt(Mathf.Clamp01(areaRatio));
			return new ILLJPIBKBGF
			{
				center = center,
				halfExtents = halfExtents * scale,
				angleRad = angleRad
			};
		}
	}

	private const float EPSILON = 1E-06f;

	public static ILLJPIBKBGF BuildOBBFromCorners(Vector2[] worldCorners)
	{
		// RectTransform.GetWorldCorners 顺序：左下、左上、右上、右下
		Vector2 bl = worldCorners[0];
		Vector2 tl = worldCorners[1];
		Vector2 tr = worldCorners[2];
		Vector2 br = worldCorners[3];
		Vector2 center = (bl + tr) * 0.5f;
		Vector2 xAxis = br - bl;
		Vector2 yAxis = tl - bl;
		return new ILLJPIBKBGF
		{
			center = center,
			halfExtents = new Vector2(xAxis.magnitude * 0.5f, yAxis.magnitude * 0.5f),
			angleRad = Mathf.Atan2(xAxis.y, xAxis.x)
		};
	}

	/// <summary>Sutherland–Hodgman：返回 subject 在凸多边形 clip 内的部分。</summary>
	private static Vector2[] ClipPolygon(Vector2[] subject, Vector2[] clip)
	{
		List<Vector2> output = new List<Vector2>(subject);
		for (int i = 0; i < clip.Length; i++)
		{
			if (output.Count == 0)
			{
				return null;
			}
			Vector2 a = clip[i];
			Vector2 b = clip[(i + 1) % clip.Length];
			List<Vector2> input = output;
			output = new List<Vector2>(input.Count + 2);
			for (int j = 0; j < input.Count; j++)
			{
				Vector2 p = input[j];
				Vector2 q = input[(j + 1) % input.Count];
				bool pIn = Cross(b - a, p - a) >= -EPSILON;
				bool qIn = Cross(b - a, q - a) >= -EPSILON;
				if (pIn)
				{
					output.Add(p);
					if (!qIn)
					{
						output.Add(LineIntersect(p, q, a, b));
					}
				}
				else if (qIn)
				{
					output.Add(LineIntersect(p, q, a, b));
				}
			}
		}
		return output.Count >= 3 ? output.ToArray() : null;
	}

	private static float Cross(Vector2 a, Vector2 b)
	{
		return a.x * b.y - a.y * b.x;
	}

	/// <summary>把 subjectPolys 中位于 clipOBB 之外的部分输出到 outPolys（近似：逐边裁剪取补集）。</summary>
	public static void ClipPolygonsOutside(List<Vector2[]> subjectPolys, ILLJPIBKBGF clipOBB, Vector2[] clipVerts, List<Vector2[]> outPolys)
	{
		// 对每条裁剪边，把主体多边形分割为边外侧的部分，逐边累积
		foreach (Vector2[] subject in subjectPolys)
		{
			List<Vector2[]> pieces = new List<Vector2[]> { subject };
			List<Vector2[]> next = new List<Vector2[]>();
			for (int i = 0; i < clipVerts.Length; i++)
			{
				Vector2 a = clipVerts[i];
				Vector2 b = clipVerts[(i + 1) % clipVerts.Length];
				next.Clear();
				foreach (Vector2[] piece in pieces)
				{
					// 边外侧部分直接输出（不再参与后续裁剪）
					Vector2[] outside = ClipHalfPlane(piece, b, a);
					if (outside != null)
					{
						outPolys.Add(outside);
					}
					// 边内侧部分继续下一条边
					Vector2[] inside = ClipHalfPlane(piece, a, b);
					if (inside != null)
					{
						next.Add(inside);
					}
				}
				List<Vector2[]> tmp = pieces;
				pieces = new List<Vector2[]>(next);
				next = tmp;
			}
		}
	}

	/// <summary>把 subjectPolys 中位于 clipOBB 之内的部分输出到 outPolys。</summary>
	public static void ClipPolygonsInside(List<Vector2[]> subjectPolys, ILLJPIBKBGF clipOBB, Vector2[] clipVerts, List<Vector2[]> outPolys)
	{
		foreach (Vector2[] subject in subjectPolys)
		{
			Vector2[] clipped = ClipPolygon(subject, clipVerts);
			if (clipped != null)
			{
				outPolys.Add(clipped);
			}
		}
	}

	/// <summary>保留 a→b 左侧（内侧）的多边形部分。</summary>
	private static Vector2[] ClipHalfPlane(Vector2[] subject, Vector2 a, Vector2 b)
	{
		List<Vector2> output = new List<Vector2>(subject.Length + 2);
		for (int j = 0; j < subject.Length; j++)
		{
			Vector2 p = subject[j];
			Vector2 q = subject[(j + 1) % subject.Length];
			bool pIn = Cross(b - a, p - a) >= -EPSILON;
			bool qIn = Cross(b - a, q - a) >= -EPSILON;
			if (pIn)
			{
				output.Add(p);
				if (!qIn)
				{
					output.Add(LineIntersect(p, q, a, b));
				}
			}
			else if (qIn)
			{
				output.Add(LineIntersect(p, q, a, b));
			}
		}
		return output.Count >= 3 ? output.ToArray() : null;
	}

	public static void ReleasePolyList(List<Vector2[]> list)
	{
		list.Clear();
	}

	public static Vector2[] GetPooledArrayCopy(Vector2[] src, int length)
	{
		Vector2[] copy = new Vector2[length];
		System.Array.Copy(src, copy, length);
		return copy;
	}

	public static Vector2 LineIntersect(Vector2 p1, Vector2 p2, Vector2 q1, Vector2 q2)
	{
		Vector2 r = p2 - p1;
		Vector2 s = q2 - q1;
		float denominator = Cross(r, s);
		if (Mathf.Abs(denominator) < EPSILON)
		{
			return p1;
		}
		float t = Cross(q1 - p1, s) / denominator;
		return p1 + r * t;
	}

	public static float PolygonArea(Vector2[] poly)
	{
		if (poly == null || poly.Length < 3)
		{
			return 0f;
		}
		float area = 0f;
		for (int i = 0; i < poly.Length; i++)
		{
			Vector2 a = poly[i];
			Vector2 b = poly[(i + 1) % poly.Length];
			area += Cross(a, b);
		}
		return Mathf.Abs(area) * 0.5f;
	}

	public static float TotalArea(List<Vector2[]> polys)
	{
		float total = 0f;
		foreach (Vector2[] poly in polys)
		{
			total += PolygonArea(poly);
		}
		return total;
	}
}
