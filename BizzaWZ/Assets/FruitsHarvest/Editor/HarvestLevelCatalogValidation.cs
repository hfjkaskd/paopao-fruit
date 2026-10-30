using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

// Reads the production catalog/resources without starting gameplay or changing player saves.
public static class HarvestLevelCatalogValidation
{
    [Serializable]
    private class Catalog
    {
        public int cycleStartLevel;
        public MainLevelConfigData[] array;
    }

    [MenuItem("Tools/Harvest/Validate Playable Levels")]
    public static void Run()
    {
        TextAsset asset = GameRes.LoadTextAsset("res_server/server_configs/MainLevelConfig");
        Require(asset != null, "Main level catalog failed to load");
        Catalog catalog = JsonUtility.FromJson<Catalog>(asset.text);
        Require(catalog.array.Length == 70 && catalog.cycleStartLevel == 11, "Unexpected playable sequence");
        PLMIHDHFAAL levels = PLMIHDHFAAL.Instance;
        levels.Init(asset);
        var checkedBoards = new HashSet<string>();
        var checkedMaps = new HashSet<string>();
        var report = new StringBuilder();
        foreach (MainLevelConfigData row in catalog.array)
        {
            int level = int.Parse(row.LevelNum);
            string[] ids = levels.GetLevelListByLevelNum(level);
            Require(string.Join(",", ids) == row.LevelList, "Wrong sequence at level " + level);
            if (level <= 10) Require(ids[0] == (10000 + level).ToString(), "Early level changed");
            for (int attempt = 0; attempt < ids.Length; attempt++)
            {
                LevelConfig board = ReadBoard(levels, ids[attempt]);
                CheckBoard(board, checkedBoards, checkedMaps);
                Require(levels.CanRestoreMainLevelSnapshot(level, attempt, board), "Current snapshot rejected");
            }
            Require(levels.CanRestoreMainLevelSnapshot(level, -1, ReadBoard(levels, ids[0])), "Negative attempt did not select first board");
            Require(levels.CanRestoreMainLevelSnapshot(level, int.MaxValue, ReadBoard(levels, ids[ids.Length - 1])), "Retry did not select last board");
            if (!string.IsNullOrEmpty(row.Guaranteed_Level))
                CheckBoard(ReadBoard(levels, row.Guaranteed_Level), checkedBoards, checkedMaps);
        }

        for (int level = 71; level <= 5000; level++)
        {
            int expected = catalog.cycleStartLevel + (level - catalog.cycleStartLevel) % (catalog.array.Length - catalog.cycleStartLevel + 1);
            string[] ids = levels.GetLevelListByLevelNum(level);
            Require(string.Join(",", ids) == catalog.array[expected - 1].LevelList, "Cycle mismatch at " + level);
            foreach (string id in ids) Require(checkedBoards.Contains(id), "Cycle selected an unavailable board");
        }
        Require(string.Join(",", levels.GetLevelListByLevelNum(int.MaxValue)) == catalog.array[66].LevelList, "Large level overflow");
        Require(levels.GetLevelListByLevelNum(0).Length == 0, "Invalid level was accepted");
        Require(!levels.CanRestoreMainLevelSnapshot(50, 0, ReadBoard(levels, "200050")), "Removed level 50 snapshot was restored");
        Require(!levels.CanRestoreMainLevelSnapshot(31, 0, ReadBoard(levels, "200031")), "Old numbering snapshot was restored");
        Require(!levels.CanRestoreMainLevelSnapshot(50, 0, null), "Null snapshot was accepted");
        Require(!levels.CanRestoreMainLevelSnapshot(0, 0, new LevelConfig()), "Empty snapshot was accepted");
        LevelConfig current = ReadBoard(levels, levels.GetLevelListByLevelNum(50)[0]);
        var wrongMap = new LevelConfig { levelID = current.levelID, mapID = "1024" };
        Require(!levels.CanRestoreMainLevelSnapshot(50, 0, wrongMap), "Mismatched map was accepted");
        Require(current.mapID == "1017", "Level 50 still selects the missing map");

        report.AppendLine("PASS: 70 consecutive playable levels; first 10 levels preserved.");
        report.AppendLine("PASS: " + checkedBoards.Count + " unique boards (including retry/guaranteed variants); " + checkedMaps.Count + " map sprites loaded through GameRes.");
        report.AppendLine("PASS: levels 71-5000 and int.MaxValue select only available boards, looping levels 11-70.");
        report.AppendLine("PASS: current snapshots accepted; removed/resequenced/mismatched snapshots rejected; retry bounds checked.");
        report.AppendLine("PASS: level 50 selects " + current.levelID + " / Map" + current.mapID + ".");
        report.AppendLine("Player saves were not loaded or modified. No APK/device test performed.");
        string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Tools/Validation/PlayableLevels/unity-validation.txt"));
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        File.WriteAllText(output, report.ToString());
        Debug.Log(report.ToString());
    }

    private static LevelConfig ReadBoard(PLMIHDHFAAL levels, string id)
    {
        LevelConfig board = levels.GetLevelConfig((long.Parse(id) / 100).ToString(), id);
        Require(board != null, "Missing board " + id);
        return board;
    }

    private static void CheckBoard(LevelConfig board, HashSet<string> boards, HashSet<string> maps)
    {
        if (!boards.Add(board.levelID)) return;
        Require(board.normalTiles != null && board.normalTiles.Count > 0 && board.normalTiles.Count % 3 == 0, "Invalid tiles in " + board.levelID);
        Require(!string.IsNullOrEmpty(board.mapID), "Empty map in " + board.levelID);
        if (!maps.Add(board.mapID)) return;
        Sprite sprite = GameRes.LoadSprite(JEFOMCDAPGK.GetMapBundlePath(board.mapID));
        Require(sprite != null && sprite.texture != null, "Map sprite failed to load: " + board.mapID);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
