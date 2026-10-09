using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

// Explicit authoring pass: persists importer/group settings; never changes runtime loading.
public static class OrchardAndroidPackageOptimization
{
    private const string SharedGroupName = "Shared UI Dependencies";

    [Serializable] public sealed class TextureRule { public string path; public int blockSize; }
    [Serializable] public sealed class Plan
    {
        public string outputDirectory;
        public TextureRule[] textures;
        public string[] sharedAssets;
        public string[] previewAssets;
    }
    [Serializable] public sealed class TextureState
    {
        public string path;
        public int width, height, sourceWidth, sourceHeight, format, androidFormat;
        public string[] spriteIdentities;
    }
    [Serializable] public sealed class Report
    {
        public List<TextureState> before = new List<TextureState>();
        public List<TextureState> after = new List<TextureState>();
        public List<string> sharedAssets = new List<string>();
        public int originalAddressCount;
    }

    public static void ApplyBatch()
    {
        string planPath = Argument("-optimizationPlan");
        var plan = JsonUtility.FromJson<Plan>(File.ReadAllText(planPath));
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            throw new InvalidOperationException("Run this authoring pass with -buildTarget Android.");
        Directory.CreateDirectory(plan.outputDirectory);
        var report = new Report();
        foreach (var rule in plan.textures) report.before.Add(Snapshot(rule.path));
        for (int i = 0; i < plan.previewAssets.Length; ++i)
            Capture(plan.previewAssets[i], Path.Combine(plan.outputDirectory, "texture-before-" + i + ".png"));

        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null) throw new InvalidOperationException("Addressables settings missing.");
        var originalAddresses = new Dictionary<string, string>();
        foreach (var group in settings.groups)
            if (group != null)
                foreach (var entry in group.entries) originalAddresses.Add(entry.guid, entry.address);
        report.originalAddressCount = originalAddresses.Count;

        // Pack separately so opening one page does not pull all shared artwork into memory.
        var shared = settings.FindGroup(SharedGroupName);
        if (shared == null)
            shared = settings.CreateGroup(SharedGroupName, false, false, false, null,
                typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
        var schema = shared.GetSchema<BundledAssetGroupSchema>();
        schema.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kLocalBuildPath);
        schema.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kLocalLoadPath);
        schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackSeparately;
        schema.Compression = BundledAssetGroupSchema.BundleCompressionMode.LZ4;
        schema.IncludeInBuild = true;
        foreach (string path in plan.sharedAssets)
        {
            // Moving Resources assets would invalidate existing synchronous Resources.Load keys.
            if (!path.StartsWith("Assets/", StringComparison.Ordinal) ||
                path.IndexOf("/Resources/", StringComparison.OrdinalIgnoreCase) >= 0)
                throw new InvalidOperationException("Invalid shared dependency: " + path);
            string guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guid)) throw new FileNotFoundException(path);
            var existing = settings.FindAssetEntry(guid);
            if (existing != null && existing.parentGroup != shared)
                throw new InvalidOperationException("Refusing to move an existing explicit entry: " + path);
            var entry = settings.CreateOrMoveEntry(guid, shared, false, false);
            if (existing == null) entry.address = "SharedDependency/" + guid;
            report.sharedAssets.Add(path);
        }
        settings.SetDirty(AddressableAssetSettings.ModificationEvent.BatchModification, shared, true, true);
        AssetDatabase.SaveAssets();

        foreach (var rule in plan.textures)
        {
            var importer = AssetImporter.GetAtPath(rule.path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Not a texture: " + rule.path);
            if (rule.blockSize != 4 && rule.blockSize != 6)
                throw new InvalidOperationException("Only reviewed ASTC 4x4/6x6 rules are supported.");
            var android = importer.GetPlatformTextureSettings("Android");
            // Preserve the effective size limit, sprite slicing, mipmap and read/write settings.
            if (!android.overridden) android.maxTextureSize = importer.maxTextureSize;
            android.overridden = true;
            android.format = rule.blockSize == 6 ? TextureImporterFormat.ASTC_6x6 : TextureImporterFormat.ASTC_4x4;
            android.compressionQuality = 100;
            android.crunchedCompression = false;
            importer.SetPlatformTextureSettings(android);
            importer.SaveAndReimport();
            Debug.Log("[PackageOptimization] Compressed " + rule.path);
        }
        AssetDatabase.SaveAssets();
        for (int i = 0; i < plan.textures.Length; ++i)
        {
            var after = Snapshot(plan.textures[i].path);
            var before = report.before[i];
            if (before.width != after.width || before.height != after.height ||
                before.sourceWidth != after.sourceWidth || before.sourceHeight != after.sourceHeight ||
                !SameIdentities(before.spriteIdentities, after.spriteIdentities))
                throw new InvalidOperationException("Texture dimensions/sprite identity changed: " + after.path);
            report.after.Add(after);
        }
        foreach (var pair in originalAddresses)
        {
            var entry = settings.FindAssetEntry(pair.Key);
            if (entry == null || entry.address != pair.Value)
                throw new InvalidOperationException("Original address changed: " + pair.Value);
        }
        for (int i = 0; i < plan.previewAssets.Length; ++i)
            Capture(plan.previewAssets[i], Path.Combine(plan.outputDirectory, "texture-after-" + i + ".png"));
        File.WriteAllText(Path.Combine(plan.outputDirectory, "optimization-applied.json"), JsonUtility.ToJson(report, true));
        Debug.Log("[PackageOptimization] Applied and validated " + report.after.Count +
            " texture rules and " + report.sharedAssets.Count + " shared dependencies; original addresses preserved.");
    }

    private static TextureState Snapshot(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (importer == null || texture == null) throw new FileNotFoundException(path);
        importer.GetSourceTextureWidthAndHeight(out int sourceWidth, out int sourceHeight);
        var identities = new List<string>();
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
            if (asset is Sprite sprite && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite, out string guid, out long id))
                identities.Add(guid + ":" + id + ":" + sprite.name);
        identities.Sort(StringComparer.Ordinal);
        return new TextureState { path = path, width = texture.width, height = texture.height,
            sourceWidth = sourceWidth, sourceHeight = sourceHeight, format = (int)texture.format,
            androidFormat = (int)importer.GetPlatformTextureSettings("Android").format,
            spriteIdentities = identities.ToArray() };
    }

    private static bool SameIdentities(string[] a, string[] b)
    {
        if (a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; ++i) if (a[i] != b[i]) return false;
        return true;
    }

    private static void Capture(string path, string output)
    {
        var source = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        var target = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        var previous = RenderTexture.active;
        Texture2D pixels = null;
        try
        {
            Graphics.Blit(source, target);
            RenderTexture.active = target;
            pixels = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            pixels.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            pixels.Apply();
            File.WriteAllBytes(output, pixels.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);
            if (pixels != null) UnityEngine.Object.DestroyImmediate(pixels);
        }
    }

    private static string Argument(string name)
    {
        var args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; ++i) if (args[i] == name) return args[i + 1];
        throw new ArgumentException("Missing command line argument " + name);
    }
}
