using UnityEngine;

/// <summary>全局输入锁：Lock 计数 &gt; 0 时激活全屏遮罩拦截所有点击。</summary>
public static class MCCIJBJGMCK
{
	private static int lockNum;

	private static Transform inputMask;

	public static void Init()
	{
		lockNum = 0;
		GameObject mask = GameObject.Find("InputMask");
		if (mask != null)
		{
			inputMask = mask.transform;
			mask.SetActive(false);
		}
	}

	public static void Lock()
	{
		lockNum++;
		Refresh();
	}

	public static void UnlockOnce()
	{
		lockNum = Mathf.Max(0, lockNum - 1);
		Refresh();
	}

	public static void UnlockAll()
	{
		lockNum = 0;
		Refresh();
	}

	public static bool IsLock()
	{
		return lockNum > 0;
	}

	private static void Refresh()
	{
		if (inputMask != null)
		{
			inputMask.gameObject.SetActive(lockNum > 0);
		}
	}

	public static InputMono Get(GameObject o)
	{
		if (o == null)
		{
			return null;
		}
		InputMono input = o.GetComponent<InputMono>();
		if (input == null)
		{
			input = o.AddComponent<InputMono>();
		}
		return input;
	}

	public static InputMonoNoDrag GetNoDrag(GameObject o)
	{
		if (o == null)
		{
			return null;
		}
		InputMonoNoDrag input = o.GetComponent<InputMonoNoDrag>();
		if (input == null)
		{
			input = o.AddComponent<InputMonoNoDrag>();
		}
		return input;
	}

	public static void DeleteInput(GameObject o)
	{
		if (o == null)
		{
			return;
		}
		InputMono input = o.GetComponent<InputMono>();
		if (input != null)
		{
			Object.Destroy(input);
		}
		InputMonoNoDrag noDrag = o.GetComponent<InputMonoNoDrag>();
		if (noDrag != null)
		{
			Object.Destroy(noDrag);
		}
	}
}
