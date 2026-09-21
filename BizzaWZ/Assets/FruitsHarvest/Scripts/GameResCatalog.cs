using System.Collections.Generic;
using UnityEngine;
public sealed class GameResCatalog {
    private static GameResCatalog instance;
    public static GameResCatalog Instance => instance ?? (instance = new GameResCatalog());
    private readonly Dictionary<string,string> paths = new Dictionary<string,string>();
    private GameResCatalog() {
        var text = Resources.Load<TextAsset>("HarvestPaths");
        if (text == null) throw new System.InvalidOperationException("HarvestPaths is missing");
        foreach (var row in text.text.Split('\n')) {
            int tab = row.IndexOf('\t'); if (tab > 0) paths[row.Substring(0,tab)] = row.Substring(tab+1).Trim();
        }
    }
    public T Load<T>(string key) where T : Object {
        if (!paths.TryGetValue(key.ToLowerInvariant(),out var path)) return null;
        if (key.IndexOf('#') >= 0) return Get(key) as T;
        return Resources.Load<T>(path);
    }
    public Object Get(string key) {
        if (!paths.TryGetValue(key.ToLowerInvariant(),out var path)) return null;
        int hash = key.IndexOf('#');
        if (hash >= 0) {
            string name = key.Substring(hash+1);
            foreach (var asset in Resources.LoadAll<Object>(path))
                if (asset.name.ToLowerInvariant() == name) return asset;
        }
        return Resources.Load<Object>(path);
    }
    public bool Contains(string key) => paths.ContainsKey(key.ToLowerInvariant());
}
