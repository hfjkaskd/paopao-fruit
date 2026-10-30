using System;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEngine;

/// <summary>Explicit, disposable tutorial previews and Android font dependency compilation.</summary>
public static class OrchardTutorialFontValidation
{
    private const string Output = "Design/TutorialFontValidation-20261011/";
    private const string FontPath = "Assets/BizzaWZ/Common/Framework/Res/Fonts/MainFont_Simple.asset";
    private const string MobileShaderPath = "Assets/BizzaWZ/Common/ThirdParty/TextMesh Pro/Shaders/TMP_SDF-Mobile.shader";
    private const string BodyPath = "Assets/OrchardUI/Generated/7cc23ba99c7035347900a2e939f7ab60-Body.mat";
    private const string TitlePath = "Assets/OrchardUI/Generated/7cc23ba99c7035347900a2e939f7ab60-Title.mat";
    private const string PrefabPath = "Assets/BizzaWZ/Final/Framework/Runtime/Module/Teach/UITeach/UITeachTipsPage.prefab";

    public static void RebuildContentBatch()
    {
        Directory.CreateDirectory(Output);
        // Regenerate dependency data after migrating the font's shader. Existing APKs
        // demonstrated null shader pointers in separately packed material presets.
        foreach (string path in new[] { FontPath, BodyPath, TitlePath, MobileShaderPath })
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        BuildCache.PurgeCache(false);
        AddressableAssetSettings.BuildPlayerContent(out var result);
        if (!string.IsNullOrEmpty(result.Error)) throw new InvalidOperationException(result.Error);
        File.WriteAllText(Output + "content-rebuild.txt", "SUCCESS clean Addressables dependency rebuild");
    }

    public static void RunBatch()
    {
        Directory.CreateDirectory(Output);
        var report = new StringBuilder();
        try
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                throw new InvalidOperationException("Tutorial shader validation must target Android.");
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(MobileShaderPath);
            var body = AssetDatabase.LoadAssetAtPath<Material>(BodyPath);
            var title = AssetDatabase.LoadAssetAtPath<Material>(TitlePath);
            if (font == null || shader == null || body == null || title == null)
                throw new InvalidOperationException("Missing tutorial font dependency.");
            foreach (var material in new[] { font.material, body, title })
            {
                if (material.shader != shader || material.mainTexture != font.atlasTexture)
                    throw new InvalidOperationException("Wrong shader/atlas: " + material.name);
                foreach (string property in new[] { "_TextureWidth", "_TextureHeight", "_GradientScale" })
                    if (material.GetFloat(property) != font.material.GetFloat(property))
                        throw new InvalidOperationException("Wrong font metric: " + material.name + " / " + property);
                report.AppendLine("PASS matching mobile shader, font atlas and metrics: " + material.name);
            }
            if (!title.IsKeywordEnabled("OUTLINE_ON") || body.IsKeywordEnabled("OUTLINE_ON"))
                throw new InvalidOperationException("Incorrect title/body outline variants.");
            string[] languages = { "en-US", "pt-BR", "id-ID" };
            string[] messages = {
                "Gold coins can also be exchanged for cash and withdrawn in full.",
                "As moedas também podem ser convertidas em dinheiro e sacadas integralmente.",
                "Koin dapat dikonversi menjadi uang tunai dan dicairkan seluruhnya."
            };
            for (int i = 0; i < messages.Length; i++)
            {
                string caption = messages[i], language = languages[i];
                var preview = OrchardSkinValidation.PreviewPrefab(PrefabPath, Output + language + ".png",
                    root => {
                        var tips = root.GetComponent<UITeachTipsPage>();
                        tips.bg.color = Color.clear;
                        tips.panel.gameObject.SetActive(true);
                        tips.panel.rectTransform.anchoredPosition = Vector2.zero;
                        tips.content.text = caption;
                    }, 1000, 400,
                    root => {
                        var label = root.GetComponent<UITeachTipsPage>().content;
                        if (label.font != font || label.fontSharedMaterial != body)
                            throw new InvalidOperationException("Tutorial prefab uses a different font/material.");
                        label.ForceMeshUpdate(true, true);
                        if (label.isTextOverflowing || label.textInfo.characterCount != caption.Length)
                            throw new InvalidOperationException("Tutorial text clipped or incomplete: " + language);
                        foreach (char character in caption)
                            if (!char.IsWhiteSpace(character) && !font.HasCharacter(character))
                                throw new InvalidOperationException("Missing tutorial glyph: " + language + " / " + character);
                        report.AppendLine("PASS complete glyphs, matching material and no overflow: " + language);
                    });
                if (!string.IsNullOrEmpty(preview.error)) throw new InvalidOperationException(preview.error);
            }
            string bundles = Output + "AndroidBundles";
            Directory.CreateDirectory(bundles);
            var manifest = BuildPipeline.BuildAssetBundles(bundles, new[] {
                new AssetBundleBuild {
                    assetBundleName = "tutorial-font-android.bundle",
                    assetNames = new[] { FontPath, BodyPath, TitlePath }
                }
            }, BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.Android);
            if (manifest == null || ShaderUtil.ShaderHasError(shader))
                throw new InvalidOperationException("Android tutorial font bundle/shader compilation failed.");
            report.AppendLine("PASS Android font/material/shader dependency bundle compiled.");
            report.AppendLine("Static previews and Android compilation only; no device is connected. No APK/AAB built.");
            File.WriteAllText(Output + "validation.txt", report.ToString());
            Debug.Log(report.ToString());
        }
        catch (Exception error)
        {
            File.WriteAllText(Output + "validation.txt", report + "\nFAILED\n" + error);
            throw;
        }
    }
}
