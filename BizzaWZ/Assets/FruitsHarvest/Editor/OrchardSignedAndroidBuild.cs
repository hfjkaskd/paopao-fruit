using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Credentials come from the build process environment and are never stored in source.
public static class OrchardSignedAndroidBuild
{
    public static void BuildReleaseBatch()
    {
        string outputDirectory = Path.GetFullPath(Required("ORCHARD_RELEASE_OUTPUT"));
        string keystore = Path.GetFullPath(Required("ORCHARD_RELEASE_KEYSTORE"));
        string storePassword = Required("ORCHARD_RELEASE_STORE_PASSWORD");
        string alias = Required("ORCHARD_RELEASE_KEY_ALIAS");
        string keyPassword = Required("ORCHARD_RELEASE_KEY_PASSWORD");
        string format = Environment.GetEnvironmentVariable("ORCHARD_RELEASE_FORMAT") ?? "both";
        if (format != "both" && format != "aab" && format != "export")
            throw new InvalidOperationException("Unsupported release format.");
        if (!File.Exists(keystore)) throw new FileNotFoundException("Release keystore is missing.");
        string version = Required("ORCHARD_RELEASE_VERSION");
        if (!int.TryParse(Required("ORCHARD_RELEASE_VERSION_CODE"), out int versionCode) || versionCode <= 0)
            throw new InvalidOperationException("Invalid release version code.");
        if (PlayerSettings.bundleVersion != version || PlayerSettings.Android.bundleVersionCode != versionCode)
            throw new InvalidOperationException("Project version does not match the requested release.");

        string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
        if (scenes.Length == 0 || scenes[0] != "Assets/Game/Resources/Scenes/InitWZ.unity")
            throw new InvalidOperationException("Release must start through InitWZ.");

        bool oldCustom = PlayerSettings.Android.useCustomKeystore;
        string oldKeystore = PlayerSettings.Android.keystoreName;
        string oldStorePassword = PlayerSettings.Android.keystorePass;
        string oldAlias = PlayerSettings.Android.keyaliasName;
        string oldKeyPassword = PlayerSettings.Android.keyaliasPass;
        bool oldBundle = EditorUserBuildSettings.buildAppBundle;
        bool oldExport = EditorUserBuildSettings.exportAsGoogleAndroidProject;
        bool oldDevelopment = EditorUserBuildSettings.development;
        bool oldDebugging = EditorUserBuildSettings.allowDebugging;
        bool oldProfiler = EditorUserBuildSettings.connectProfiler;
        const string jvmHeapPreference = "AndroidJVMMaxHeapSize";
        bool hadJvmHeapPreference = EditorPrefs.HasKey(jvmHeapPreference);
        int oldJvmHeap = EditorPrefs.GetInt(jvmHeapPreference, 4096);
        try
        {
            EditorPrefs.SetInt(jvmHeapPreference, 2048);
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
                throw new InvalidOperationException("Cannot switch to Android.");
            HarvestLevelCatalogValidation.Run();
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = keystore;
            PlayerSettings.Android.keystorePass = storePassword;
            PlayerSettings.Android.keyaliasName = alias;
            PlayerSettings.Android.keyaliasPass = keyPassword;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = format == "export";
            EditorUserBuildSettings.development = false;
            EditorUserBuildSettings.allowDebugging = false;
            EditorUserBuildSettings.connectProfiler = false;
            Directory.CreateDirectory(outputDirectory);
            if (format == "export")
            {
                UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.BuildPlayerContent(out var content);
                if (!string.IsNullOrEmpty(content.Error)) throw new InvalidOperationException(content.Error);
                string gradleProject = Required("ORCHARD_RELEASE_GRADLE_PROJECT");
                BuildReport export = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = Path.GetFullPath(gradleProject),
                    target = BuildTarget.Android,
                    options = BuildOptions.None
                });
                if (export.summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException("Android project export failed, errors=" + export.summary.totalErrors);
                File.WriteAllText(Path.Combine(outputDirectory, "unity-export-summary.txt"),
                    "Succeeded\nTime: " + export.summary.totalTime + "\nWarnings: " + export.summary.totalWarnings);
                Debug.Log("[OrchardRelease] Android Gradle project export succeeded.");
            }
            else
            {
                if (format == "both") Build(scenes, outputDirectory, false);
                Build(scenes, outputDirectory, true);
                Debug.Log("[OrchardRelease] Requested signed Android builds succeeded.");
            }
        }
        finally
        {
            PlayerSettings.Android.useCustomKeystore = oldCustom;
            PlayerSettings.Android.keystoreName = oldKeystore;
            PlayerSettings.Android.keystorePass = oldStorePassword;
            PlayerSettings.Android.keyaliasName = oldAlias;
            PlayerSettings.Android.keyaliasPass = oldKeyPassword;
            EditorUserBuildSettings.buildAppBundle = oldBundle;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = oldExport;
            EditorUserBuildSettings.development = oldDevelopment;
            EditorUserBuildSettings.allowDebugging = oldDebugging;
            EditorUserBuildSettings.connectProfiler = oldProfiler;
            if (hadJvmHeapPreference) EditorPrefs.SetInt(jvmHeapPreference, oldJvmHeap);
            else EditorPrefs.DeleteKey(jvmHeapPreference);
            AssetDatabase.SaveAssets();
            File.WriteAllText(Path.Combine(outputDirectory, "signing-settings-restored.txt"), "restored");
        }
    }

    private static void Build(string[] scenes, string outputDirectory, bool appBundle)
    {
        string extension = appBundle ? "aab" : "apk";
        string output = Path.Combine(outputDirectory, "OrchardTrio_" + PlayerSettings.bundleVersion + "_" +
            PlayerSettings.Android.bundleVersionCode + "." + extension);
        EditorUserBuildSettings.buildAppBundle = appBundle;
        Debug.Log("[OrchardRelease] Building " + extension + ": " + output);
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = output,
            target = BuildTarget.Android,
            options = BuildOptions.None
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException("Release " + extension + " failed, errors=" + report.summary.totalErrors);
        string symbols = Path.Combine(Application.dataPath, "Obfuz/SymbolObfus/symbol-mapping.xml");
        if (File.Exists(symbols)) File.Copy(symbols, Path.Combine(outputDirectory, extension + "-symbol-mapping.xml"), true);
        File.WriteAllText(Path.Combine(outputDirectory, extension + "-build-summary.txt"),
            "Succeeded\nBytes: " + report.summary.totalSize + "\nWarnings: " + report.summary.totalWarnings +
            "\nErrors: " + report.summary.totalErrors + "\nTime: " + report.summary.totalTime);
        Debug.Log("[OrchardRelease] " + extension + " succeeded.");
    }

    private static string Required(string name)
    {
        string value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrEmpty(value)) throw new InvalidOperationException("Missing build environment: " + name);
        return value;
    }
}
