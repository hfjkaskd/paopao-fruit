using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>简单 GameObject 对象池。</summary>
public class IHJFHNCGPKF
{
	private GameObject cardinalObj;

	private readonly int capacity;

	private readonly Stack<GameObject> pool = new Stack<GameObject>();

	private readonly Action<GameObject> onGetCallback;

	private readonly Action<GameObject> onReleaseCallback;

	private readonly Action<GameObject> onPreloadCallBack;

	private readonly Transform poolParent;

	public int CountAll { get; private set; }

	public int CountActive => CountAll - pool.Count;

	public int CountInactive => pool.Count;

	public IHJFHNCGPKF(GameObject InCardinalObj, int InCapacity, Transform InParent = null, bool InIsPreload = false, int InPreloadCount = 0, Action<GameObject> InOnGetCallback = null, Action<GameObject> InOnReleaseCallback = null, Action<GameObject> InPreloadCallBack = null)
	{
		cardinalObj = InCardinalObj;
		capacity = InCapacity;
		poolParent = InParent;
		onGetCallback = InOnGetCallback;
		onReleaseCallback = InOnReleaseCallback;
		onPreloadCallBack = InPreloadCallBack;
		if (InIsPreload)
		{
			for (int i = 0; i < InPreloadCount; i++)
			{
				GameObject go = UnityEngine.Object.Instantiate(cardinalObj, poolParent, false);
				go.SetActive(false);
				CountAll++;
				onPreloadCallBack?.Invoke(go);
				pool.Push(go);
			}
		}
	}

	public GameObject Get(Transform InParent)
	{
		GameObject go = null;
		while (pool.Count > 0 && go == null)
		{
			go = pool.Pop();
		}
		if (go == null)
		{
			go = UnityEngine.Object.Instantiate(cardinalObj, InParent, false);
			CountAll++;
		}
		else if (InParent != null)
		{
			go.transform.SetParent(InParent, false);
		}
		go.SetActive(true);
		onGetCallback?.Invoke(go);
		return go;
	}

	public void Release(GameObject InObj)
	{
		if (InObj == null)
		{
			return;
		}
		onReleaseCallback?.Invoke(InObj);
		if (pool.Count >= capacity && capacity > 0)
		{
			UnityEngine.Object.Destroy(InObj);
			CountAll--;
			return;
		}
		InObj.SetActive(false);
		if (poolParent != null)
		{
			InObj.transform.SetParent(poolParent, false);
		}
		pool.Push(InObj);
	}

	public void Clear()
	{
		while (pool.Count > 0)
		{
			GameObject go = pool.Pop();
			if (go != null)
			{
				UnityEngine.Object.Destroy(go);
				CountAll--;
			}
		}
	}
}
