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
        if (!TryResolve(key,out var path,out var name)) return null;
        if (name == null) return Resources.Load<T>(path);
        foreach (var asset in Resources.LoadAll<T>(path))
            if (string.Equals(asset.name,name,System.StringComparison.OrdinalIgnoreCase)) return asset;
        return Resources.Load<Object>(path) as T;
    }
    public Object Get(string key) {
        if (!TryResolve(key,out var path,out var name)) return null;
        if (name != null) {
            Object match = null;
            foreach (var asset in Resources.LoadAll<Object>(path)) {
                if (!string.Equals(asset.name,name,System.StringComparison.OrdinalIgnoreCase)) continue;
                // A texture and its Sprite may share a name; #sprite aliases select the Sprite.
                if (asset is Sprite) return asset;
                if (match == null) match = asset;
            }
            if (match != null) return match;
        }
        return Resources.Load<Object>(path);
    }
    private bool TryResolve(string key,out string path,out string name) {
        name = null;
        if (!paths.TryGetValue(key.ToLowerInvariant(),out path)) return false;
        int hash = path.IndexOf('#');
        if (hash >= 0) {
            name = path.Substring(hash+1);
            path = path.Substring(0,hash);
        } else {
            hash = key.IndexOf('#');
            if (hash >= 0) name = key.Substring(hash+1);
        }
        return true;
    }
    public bool Contains(string key) => paths.ContainsKey(key.ToLowerInvariant());
}
