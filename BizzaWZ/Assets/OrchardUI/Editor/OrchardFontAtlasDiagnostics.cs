using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>Explicit prefab font binding authoring and isolated material/glyph validation.</summary>
[InitializeOnLoad]
public static class OrchardFontAtlasDiagnostics
{
    private const string Folder = "Design/FontAtlasDiagnostics-20261009/";
    private const string PrefabPath = "Assets/BizzaWZ/Final/Real/UI/FakeWithdrawPanel/FakeWithdrawPanel.prefab";
    static OrchardFontAtlasDiagnostics() { EditorApplication.update += Tick; }

    private static void Tick()
    {
        string request = Folder + "request.txt";
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(request)) return;
        string command = File.ReadAllText(request).Trim();
        File.Delete(request);
        try
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Prefab font validation requires Edit mode.");
            if (command == "repair-and-validate") RepairAndValidate();
            else throw new ArgumentException("Unknown font diagnostic: " + command);
        }
        catch (Exception error)
        {
            File.WriteAllText(Folder + "result.txt", error.ToString());
            Debug.LogException(error);
        }
    }

    private static void RepairAndValidate()
    {
        var report = new StringBuilder();
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var visual = new SerializedObject(root.GetComponent<OrchardCashVisual>());
            var withdraw = (TMP_Text)visual.FindProperty("withdrawLabel").objectReferenceValue;
            foreach (var label in root.GetComponentsInChildren<TMP_Text>(true))
            {
                var material = label.fontSharedMaterial;
                if (material == null || (material.name != "ReferenceTitleWood" && material.name != "ReferenceTitleGreen" && label != withdraw)) continue;
                var binding = label.GetComponent<OrchardFontMaterialBinding>() ?? label.gameObject.AddComponent<OrchardFontMaterialBinding>();
                var data = new SerializedObject(binding);
                data.FindProperty("label").objectReferenceValue = label;
                data.FindProperty("preset").objectReferenceValue = material;
                data.ApplyModifiedPropertiesWithoutUndo();
                if (label == withdraw) visual.FindProperty("withdrawLabelStyle").objectReferenceValue = binding;
                report.AppendLine("Authored font binding: " + label.name + " / " + material.name);
            }
            visual.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }

        root = PrefabUtility.LoadPrefabContents(PrefabPath);
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var owned = new List<Material>();
        Texture2D foreignAtlas = new Texture2D(64, 64);
        try
        {
            var visual = new SerializedObject(root.GetComponent<OrchardCashVisual>());
            var label = (TMP_Text)visual.FindProperty("withdrawLabel").objectReferenceValue;
            var binding = label.GetComponent<OrchardFontMaterialBinding>();
            foreach (var key in new[] { "disabledLabel", "enabledLabel" })
            {
                var style = (Material)visual.FindProperty(key).objectReferenceValue;
                foreach (string caption in new[] { "Withdraw", "Retirar", "Tarik" })
                {
                    label.text = caption;
                    binding.SetPreset(style);
                    if (!owned.Contains(label.fontSharedMaterial)) owned.Add(label.fontSharedMaterial);
                    Validate(label, style);
                    label.ForceMeshUpdate(true, true);
                    if (label.textInfo.characterCount != caption.Length) throw new InvalidOperationException("Incorrect glyph count: " + caption);
                    report.AppendLine("PASS " + key + " / " + caption + " / matching atlas, metrics and style");
                }
            }
            var broken = new Material(label.font.material);
            owned.Add(broken);
            broken.mainTexture = foreignAtlas;
            broken.SetFloat("_TextureWidth", 64);
            broken.SetFloat("_TextureHeight", 64);
            label.fontSharedMaterial = broken;
            if (label.fontSharedMaterial.mainTexture == label.font.atlasTexture) throw new InvalidOperationException("Mismatch fixture is invalid.");
            binding.SetPreset(broken);
            Validate(label, broken);
            report.AppendLine("PASS foreign atlas preset: corrected texture and metrics without changing the shared preset");
            if (broken.mainTexture != foreignAtlas) throw new InvalidOperationException("Shared preset was mutated.");
            File.WriteAllText(Folder + "validation.txt", report.ToString());
            File.WriteAllText(Folder + "result.txt", "SUCCESS repair-and-validate\n" + DateTime.UtcNow.ToString("O"));
        }
        finally
        {
            foreach (var material in owned) if (material != null) UnityEngine.Object.DestroyImmediate(material);
            UnityEngine.Object.DestroyImmediate(foreignAtlas);
            PrefabUtility.UnloadPrefabContents(root);
        }
        AssetDatabase.SaveAssets();
    }

    private static void Validate(TMP_Text label, Material style)
    {
        var material = label.fontSharedMaterial;
        if (material.mainTexture != label.font.atlasTexture) throw new InvalidOperationException("Font atlas mismatch remains.");
        foreach (string property in new[] { "_TextureWidth", "_TextureHeight", "_GradientScale", "_WeightNormal", "_WeightBold" })
            if (material.GetFloat(property) != label.font.material.GetFloat(property)) throw new InvalidOperationException("Wrong font metric: " + property);
        foreach (string property in new[] { "_FaceColor", "_OutlineColor", "_UnderlayColor" })
            if (material.GetColor(property) != style.GetColor(property)) throw new InvalidOperationException("Style changed: " + property);
    }

}
