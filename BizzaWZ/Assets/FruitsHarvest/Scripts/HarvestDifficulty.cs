using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Keep original boards intact; difficulty only limits fruit varieties.</summary>
public static class HarvestDifficulty
{
    [Serializable]
    private class Settings
    {
        public int revision;
        public int introductoryLevels;
        public int introductoryTypes;
        public int[] cycleTypes;
        public int interstitialProtectedLevels;
    }

    private static Settings settings;
    private static Settings Config
    {
        get
        {
            if (settings != null) return settings;
            var asset = Resources.Load<TextAsset>("HarvestDifficulty");
            if (asset == null) throw new InvalidOperationException("Missing HarvestDifficulty configuration");
            settings = JsonUtility.FromJson<Settings>(asset.text);
            if (settings.cycleTypes == null || settings.cycleTypes.Length != 5)
                throw new InvalidOperationException("Difficulty cycle must contain five levels");
            return settings;
        }
    }

    public static bool ProtectInterstitial(int level) => level <= Config.interstitialProtectedLevels;
    public static int Revision => Config.revision;

    public static int TypeLimit(int level) => level <= Config.introductoryLevels
        ? Config.introductoryTypes
        : Config.cycleTypes[(level - Config.introductoryLevels - 1) % Config.cycleTypes.Length];

    public static LevelConfig Apply(LevelConfig original, int level)
    {
        if (original == null) throw new ArgumentNullException(nameof(original));
        // A per-attempt copy prevents tuning one level from changing the resource cache.
        var result = JsonUtility.FromJson<LevelConfig>(JsonUtility.ToJson(original));
        var types = new List<int>();
        foreach (var tile in original.normalTiles)
            if (!types.Contains(tile.eleType)) types.Add(tile.eleType);
        int limit = Mathf.Clamp(TypeLimit(level), 1, types.Count);
        var mapping = new Dictionary<int, int>();
        for (int i = 0; i < types.Count; i++) mapping.Add(types[i], types[i % limit]);
        foreach (var tile in result.normalTiles) tile.eleType = mapping[tile.eleType];
        result.eleClassCount = limit;
        return result;
    }
}
