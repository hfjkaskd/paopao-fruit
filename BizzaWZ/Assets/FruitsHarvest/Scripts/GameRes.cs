using UnityEngine;

/// <summary>
/// 统一资源加载门面。所有运行时加载都走这里（编辑器与打包后一致）。
/// path 形如 "res/local/home/Home"、"res_server/server_images/fruit/Fruit_0"。
/// </summary>
public static class GameRes
{
	public static T Load<T>(string path) where T : Object
	{
		Object obj = GameResCatalog.Instance.Load<T>(Normalize(path));
		if (obj == null)
		{
			return null;
		}
		if (obj is T t)
		{
			return t;
		}
		// 纹理与 Sprite 之间的兼容：请求 Sprite 但登记的是主资源时尝试子资源
		if (typeof(T) == typeof(Sprite))
		{
			Object sub = GameResCatalog.Instance.Get(Normalize(path) + "#" + LastName(path));
			if (sub is T st)
			{
				return st;
			}
		}
		if (obj is GameObject go && typeof(Component).IsAssignableFrom(typeof(T)))
		{
			return go.GetComponent<T>();
		}
		return null;
	}

	public static bool Exists(string path)
	{
		return GameResCatalog.Instance.Contains(Normalize(path));
	}

	public static GameObject LoadPrefab(string path)
	{
		GameObject prefab = Load<GameObject>(path);
		if (prefab == null)
		{
			Debug.LogError("[GameRes] 未找到预制体: " + path);
		}
		return prefab;
	}

	/// <summary>加载 Sprite：支持独立 .asset 精灵、含子精灵的贴图（path#spriteName）。</summary>
	public static Sprite LoadSprite(string path)
	{
		Sprite s = Load<Sprite>(path);
		if (s == null)
		{
			Debug.LogWarning("[GameRes] 未找到Sprite: " + path);
		}
		return s;
	}

	public static AudioClip LoadAudio(string clipName)
	{
		return Load<AudioClip>("res/local/sound/" + clipName);
	}

	public static string LoadText(string path)
	{
		TextAsset ta = Load<TextAsset>(path);
		return ta != null ? ta.text : null;
	}

	public static TextAsset LoadTextAsset(string path)
	{
		return Load<TextAsset>(path);
	}

	private static string Normalize(string path)
	{
		if (string.IsNullOrEmpty(path))
		{
			return path;
		}
		path = path.Replace('\\', '/').ToLowerInvariant();
		if (path.StartsWith("assets/"))
		{
			path = path.Substring("assets/".Length);
		}
		int dot = path.LastIndexOf('.');
		int slash = path.LastIndexOf('/');
		if (dot > slash && !path.Contains("#"))
		{
			string ext = path.Substring(dot + 1);
			if (ext == "prefab" || ext == "png" || ext == "jpg" || ext == "asset" || ext == "ogg" || ext == "wav" || ext == "json" || ext == "mixer" || ext == "anim" || ext == "mat")
			{
				path = path.Substring(0, dot);
			}
		}
		return path;
	}

	private static string LastName(string path)
	{
		path = path.Replace('\\', '/');
		int slash = path.LastIndexOf('/');
		string name = slash >= 0 ? path.Substring(slash + 1) : path;
		int dot = name.LastIndexOf('.');
		if (dot > 0)
		{
			name = name.Substring(0, dot);
		}
		return name.ToLowerInvariant();
	}
}
