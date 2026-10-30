using System.IO;
using System.Text.RegularExpressions;
using UnityEditor.Android;
using UnityEngine;

// Runs after MAX's Android export callback. Keep the Ad Review build plugin
// at the validated version instead of resolving an untested latest version.
public sealed class OrchardAndroidBuildCompatibility : IPostGenerateGradleAndroidProject
{
    public int callbackOrder => int.MaxValue;

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        string rootGradle = Path.Combine(Path.GetDirectoryName(path), "build.gradle");
        if (!File.Exists(rootGradle)) return;
        string original = File.ReadAllText(rootGradle);
        if (!Regex.IsMatch(original,
                @"id ['""]com\.android\.application['""] version ['""]7\.4\.2['""]"))
            return;

        string updated = Regex.Replace(original,
            @"(id ['""]com\.applovin\.quality['""] version )['""][^'""]+['""]",
            "$1'5.12.12'");
        if (updated == original) return;
        File.WriteAllText(rootGradle, updated);
        Debug.Log("[OrchardAndroidBuild] Pinned Ad Review build plugin 5.12.12 for AGP 7.4.2.");
    }
}
