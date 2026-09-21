using UnityEngine;

public class BaseSingleMono<T> : MonoBehaviour where T : BaseSingleMono<T>
{
	private static T t;

	public static T Instance
	{
		get
		{
			if (t == null)
			{
				t = Object.FindObjectOfType<T>();
				if (t == null)
				{
					GameObject go = new GameObject(typeof(T).Name);
					Object.DontDestroyOnLoad(go);
					t = go.AddComponent<T>();
				}
			}
			return t;
		}
	}
}
