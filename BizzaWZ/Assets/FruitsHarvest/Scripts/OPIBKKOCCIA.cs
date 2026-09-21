using System.Collections.Generic;
using UnityEngine;

/// <summary>遮挡/空间索引：维护场上对象，查询与指定对象相交且优先级更高的候选。</summary>
public class OPIBKKOCCIA
{
	private const float GRIDSIZE = 10f;

	private readonly List<CollectItem> allItems = new List<CollectItem>();

	private RectTransform bgTrans;

	public OPIBKKOCCIA(RectTransform bgTrans)
	{
		this.bgTrans = bgTrans;
	}

	public void Build(List<CollectItem> items)
	{
		allItems.Clear();
		if (items != null)
		{
			foreach (CollectItem item in items)
			{
				if (item != null)
				{
					allItems.Add(item);
				}
			}
		}
	}

	public void Add(CollectItem item, Vector3? worldPos = null)
	{
		if (item != null && !allItems.Contains(item))
		{
			allItems.Add(item);
		}
	}

	public void Remove(CollectItem item)
	{
		allItems.Remove(item);
	}

	/// <summary>取比 item 优先级更高、仍在场上、且 AABB 相交的对象。</summary>
	public void GetHigherPriorityItems(CollectItem item, List<CollectItem> outList)
	{
		outList.Clear();
		if (item == null || bgTrans == null)
		{
			return;
		}
		Rect selfRect = item.BuildOBB(bgTrans).GetAABB();
		for (int i = 0; i < allItems.Count; i++)
		{
			CollectItem other = allItems[i];
			if (other == null || other == item)
			{
				continue;
			}
			if (other.Status != LECIONHKEJL.OnField && other.Status != LECIONHKEJL.Birth)
			{
				continue;
			}
			if (other.Priority <= item.Priority)
			{
				continue;
			}
			Rect otherRect = other.BuildOBB(bgTrans).GetAABB();
			if (selfRect.Overlaps(otherRect))
			{
				outList.Add(other);
			}
		}
	}

	public static void ShowDebug(OPIBKKOCCIA grid)
	{
	}

	public static void HideDebug()
	{
	}
}
