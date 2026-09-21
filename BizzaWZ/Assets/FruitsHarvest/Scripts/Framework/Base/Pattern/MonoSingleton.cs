using UnityEngine;

namespace Framework.Base.Pattern
{
	public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
	{
		private static T instance;

		public static T Instance
		{
			get
			{
				if (instance == null)
				{
					instance = Object.FindObjectOfType<T>();
					if (instance == null)
					{
						string goName = "HarvestRuntimeService";
						GameObject go = new GameObject(goName);
						Object.DontDestroyOnLoad(go);
						instance = go.AddComponent<T>();
					}
				}
				return instance;
			}
		}

		public static bool HasInstance => instance != null;

		public GameObject GameObj { get; private set; }

		public Transform Trans { get; private set; }

		protected bool IsStillAlive { get; private set; }

		protected virtual void Awake()
		{
			if (instance == null)
			{
				instance = (T)this;
			}
			GameObj = gameObject;
			Trans = transform;
			IsStillAlive = true;
		}

		public void DestroySelf()
		{
			Object.Destroy(gameObject);
		}

		protected virtual void OnDestroy()
		{
			IsStillAlive = false;
			if (instance == this)
			{
				instance = null;
			}
		}
	}
}
