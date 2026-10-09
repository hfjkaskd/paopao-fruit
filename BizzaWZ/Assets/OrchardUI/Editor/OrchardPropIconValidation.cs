using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class OrchardPropIconValidation
{
    private static readonly string OutputDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Design/OrchardUI"));
    private static readonly string CommandPath = Path.Combine(OutputDirectory, "prop-icons.command");
    private static double nextPoll;

    [Serializable]
    private sealed class Check
    {
        public string key;
        public string expected;
        public string actual;
        public bool uppercaseKeyPassed;
        public bool untypedAliasPassed;
        public bool passed;
    }

    [Serializable]
    private sealed class Report
    {
        public string checkedAtUtc;
        public bool passed;
        public List<Check> keys = new List<Check>();
        public List<Check> legacyKeys = new List<Check>();
        public List<Check> propConfig = new List<Check>();
        public List<string> errors = new List<string>();
    }

    static OrchardPropIconValidation()
    {
        EditorApplication.update += Poll;
    }

    private static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.timeSinceStartup < nextPoll) return;
        nextPoll = EditorApplication.timeSinceStartup + 0.5;
        if (!File.Exists(CommandPath)) return;
        string command = File.ReadAllText(CommandPath).Trim();
        if (!string.Equals(command, "validate", StringComparison.OrdinalIgnoreCase)) return;
        File.Delete(CommandPath);
        Validate();
    }

    [MenuItem("Tools/Orchard UI/Validate Prop Icon Mappings")]
    public static void Validate()
    {
        var report = new Report { checkedAtUtc = DateTime.UtcNow.ToString("o") };
        try
        {
            string[] names = { "Undo", "Magic", "Shuffle" };
            string[] states = { "Normal", "Lock", "Gray", "Hard", "SuperHard" };
            E_ItemType[] itemTypes = { E_ItemType.GameProp_1, E_ItemType.GameProp_3, E_ItemType.GameProp_2 };
            PropConfigSO config = Resources.Load<PropConfigSO>("Configs/PropConfig");
            for (int i = 0; i < names.Length; i++)
            {
                string expectedName = "PropGolden" + names[i];
                Sprite expected = Resources.Load<Sprite>("OrchardUI/" + expectedName);
                if (expected == null || expected.name != expectedName)
                    report.errors.Add("Missing canonical sprite: " + expectedName);
                foreach (string state in states)
                {
                    string basename = names[i].ToLowerInvariant() + "_" + state.ToLowerInvariant();
                    string key = "res/local/coreplay/sprite/item/" + basename;
                    CheckKey(report.keys, key, expected, expectedName);
                    CheckKey(report.keys, key + "#" + basename, expected, expectedName);
                }
                string icon = "icon" + names[i].ToLowerInvariant();
                string popupKey = "res/local/pops/newitempop/sprite/" + icon;
                CheckKey(report.keys, popupKey, expected, expectedName);
                CheckKey(report.keys, popupKey + "#" + icon, expected, expectedName);
                PropConfigInfo entry = config != null ? config.GetPropConfigInfo(itemTypes[i]) : null;
                Sprite actual = entry != null ? entry.propIcon : null;
                report.propConfig.Add(new Check {
                    key = "PropConfig/" + names[i], expected = expectedName,
                    actual = actual != null ? actual.name : "<null>",
                    passed = expected != null && actual == expected
                });
            }
            Sprite legacy = Resources.Load<Sprite>("Original/res/local/coreplay/sprite/item/Extra_Icon");
            CheckKey(report.legacyKeys, "res/local/coreplay/sprite/item/extra_icon", legacy, "Extra_Icon");
            CheckKey(report.legacyKeys, "res/local/coreplay/sprite/item/extra_icon#extra_icon", legacy, "Extra_Icon");
            report.passed = report.keys.Count == 36 && report.errors.Count == 0;
            foreach (Check check in report.keys) report.passed &= check.passed;
            foreach (Check check in report.legacyKeys) report.passed &= check.passed;
            foreach (Check check in report.propConfig) report.passed &= check.passed;
        }
        catch (Exception exception)
        {
            report.passed = false;
            report.errors.Add(exception.ToString());
        }
        Directory.CreateDirectory(OutputDirectory);
        string reportPath = Path.Combine(OutputDirectory, "prop-icon-validation.json");
        File.WriteAllText(reportPath, JsonUtility.ToJson(report, true));
        string message = "Prop icon validation " + (report.passed ? "passed" : "FAILED") + ": " + reportPath;
        if (report.passed) Debug.Log(message);
        else Debug.LogError(message);
    }

    private static void CheckKey(List<Check> checks, string key, Sprite expected, string expectedName)
    {
        Sprite actual = GameRes.LoadSprite(key);
        Sprite uppercase = GameResCatalog.Instance.Load<Sprite>(key.ToUpperInvariant());
        bool uppercasePassed = expected != null && uppercase == expected;
        bool untypedPassed = key.IndexOf('#') < 0 ||
            (expected != null && GameResCatalog.Instance.Get(key) == expected &&
             GameResCatalog.Instance.Get(key.ToUpperInvariant()) == expected);
        checks.Add(new Check {
            key = key, expected = expectedName,
            actual = actual != null ? actual.name : "<null>",
            uppercaseKeyPassed = uppercasePassed,
            untypedAliasPassed = untypedPassed,
            passed = expected != null && actual == expected && uppercasePassed && untypedPassed
        });
    }
}
