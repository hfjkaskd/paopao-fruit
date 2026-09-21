using UnityEngine;

/// <summary>Gameplay models serialized through the framework save strategy.</summary>
public abstract class IAJDGNOGFGO<DataT, ModelT> : HAHGPHDEOLA where DataT : class, new() where ModelT : class, new()
{
	private static ModelT instance;

	protected DataT data;


	protected bool isFirstInit;

	public static ModelT Instance
	{
		get
		{
			if (instance == null)
			{
				instance = new ModelT();
			}
			return instance;
		}
		private set
		{
			instance = value;
		}
	}

	public virtual void Init()
	{

        string value = HarvestBridge.ReadModel(GetKey());
        data = string.IsNullOrEmpty(value) ? null : JsonUtility.FromJson<DataT>(value);
        if (data == null) { data = new DataT(); isFirstInit = true; AfterFirstInitData(); }

	}

	protected virtual void AfterFirstInitData()
	{
	}

	public void SaveData()
	{
		TrySaveToDisk();
	}

	protected abstract string GetKey();


	public bool TrySaveToDisk()
	{

        if (data == null) return false;
        HarvestBridge.WriteModel(GetKey(), JsonUtility.ToJson(data));
        return true;

	}
}
