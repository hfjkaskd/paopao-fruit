using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>Explicit one-time prefab authoring. Never executes as a runtime skin fallback.</summary>
[InitializeOnLoad]
public static class OrchardSkinAuthoring
{
    public const string Root = "Assets/OrchardUI/";
    public const string AtlasPath = Root + "Art/Controls.png";
    public const string BackgroundPath = Root + "Resources/OrchardUI/Backdrop.png";
    private const string Output = "Design/OrchardUI/";
    private static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>(StringComparer.Ordinal);
    private static readonly Dictionary<string, Material> TextMaterials = new Dictionary<string, Material>();
    public static readonly Color BodyColor = new Color32(23, 79, 125, 255);
    private static bool busy;

    [Serializable] private sealed class Manifest { public string[] prefabs; }
    [Serializable] private sealed class SliceList { public Slice[] sprites; }
    [Serializable] private sealed class Slice
    {
        public string name;
        public int x, y, width, height;
        public int left, bottom, right, top;
    }
    [Serializable] private sealed class PageChange
    {
        public string prefab;
        public int imageCount;
        public int textCount;
        public int buttonCount;
        public string error;
    }
    [Serializable] private sealed class ChangeReport
    {
        public string generatedUtc;
        public string reference = "Design/OrchardUI/locked-reference.png";
        public List<PageChange> pages = new List<PageChange>();
    }

    static OrchardSkinAuthoring() { EditorApplication.update += Tick; }
    private static void Tick()
    {
        if (busy || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        string commandPath = Output + "authoring.command";
        if (!File.Exists(commandPath)) return;
        string command = File.ReadAllText(commandPath).Trim();
        File.Delete(commandPath);
        try
        {
            busy = true;
            if (command == "import") ImportArt();
            else if (command == "apply") ApplyAll();
            else throw new InvalidOperationException("Unknown Orchard authoring command: " + command);
            File.WriteAllText(Output + "authoring-result.txt", "SUCCESS " + command + " " + DateTime.UtcNow.ToString("O"));
        }
        catch (Exception e)
        {
            File.WriteAllText(Output + "authoring-result.txt", "FAILED " + command + "\n" + e);
            Debug.LogException(e);
        }
        finally { busy = false; }
    }

    [MenuItem("Tools/Orchard UI/Import Locked Art")]
    public static void ImportArt()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before authoring assets.");
        Directory.CreateDirectory(Root + "Generated");
        AssetDatabase.Refresh();
        ConfigureTexture(BackgroundPath, false);
        ConfigureTexture(AtlasPath, true);
        var importer = (TextureImporter)AssetImporter.GetAtPath(AtlasPath);
        var config = JsonUtility.FromJson<SliceList>(File.ReadAllText(Output + "atlas-slices.json"));
        var factories = new SpriteDataProviderFactories();
        factories.Init();
        var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        var previousIds = new Dictionary<string, GUID>(StringComparer.Ordinal);
        foreach (var previous in provider.GetSpriteRects()) previousIds[previous.name] = previous.spriteID;
        var slices = new SpriteRect[config.sprites.Length];
        var nameIds = new List<SpriteNameFileIdPair>();
        for (int i = 0; i < slices.Length; i++)
        {
            Slice item = config.sprites[i];
            GUID id = previousIds.TryGetValue(item.name, out GUID existingId) ? existingId : GUID.Generate();
            slices[i] = new SpriteRect
            {
                name = item.name,
                spriteID = id,
                rect = new Rect(item.x, item.y, item.width, item.height),
                pivot = new Vector2(.5f, .5f),
                alignment = SpriteAlignment.Center,
                border = new Vector4(item.left, item.bottom, item.right, item.top)
            };
            nameIds.Add(new SpriteNameFileIdPair(item.name, id));
        }
        provider.SetSpriteRects(slices);
        provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(nameIds);
        provider.Apply();
        importer.SaveAndReimport();
        Sprites.Clear();
        LoadSprites();
        if (Sprites.Count < 16) throw new InvalidOperationException("Expected sixteen imported Orchard sprites.");
        OrchardNavigationPass.ImportArt();
        AssetDatabase.SaveAssets();
    }

    private static void ConfigureTexture(string path, bool atlas)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new FileNotFoundException("Missing Orchard artwork", path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = atlas ? SpriteImportMode.Multiple : SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100;
        importer.mipmapEnabled = false;
        importer.isReadable = false;
        importer.alphaIsTransparency = atlas;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        foreach (string platform in new[] { "Android", "iPhone" })
        {
            var settings = importer.GetPlatformTextureSettings(platform);
            settings.overridden = true;
            settings.maxTextureSize = 2048;
            settings.format = TextureImporterFormat.ASTC_6x6;
            settings.compressionQuality = 80;
            importer.SetPlatformTextureSettings(settings);
        }
        importer.SaveAndReimport();
    }

    private static void LoadSprites()
    {
        if (Sprites.Count != 0) return;
        foreach (Object item in AssetDatabase.LoadAllAssetsAtPath(AtlasPath))
            if (item is Sprite sprite) Sprites[sprite.name] = sprite;
    }

    public static Sprite SpriteFor(string role)
    {
        LoadSprites();
        if (!Sprites.TryGetValue(role, out Sprite result)) throw new InvalidOperationException("Missing Orchard sprite role " + role);
        return result;
    }

    public static void ApplySprite(Image image, string role)
    {
        if (image == null) return;
        bool filled = image.type == Image.Type.Filled;
        image.sprite = SpriteFor(role);
        image.overrideSprite = null;
        image.material = null;
        image.color = Color.white;
        image.type = filled ? Image.Type.Filled : (role.StartsWith("ButtonRound", StringComparison.Ordinal) ? Image.Type.Simple : Image.Type.Sliced);
        image.preserveAspect = role.StartsWith("ButtonRound", StringComparison.Ordinal);
        image.fillCenter = true;
        image.pixelsPerUnitMultiplier = role == "Panel" ? .7f : (role == "Badge" ? 2f : 1f);
        EditorUtility.SetDirty(image);
    }

    public static void SetBody(TMP_Text text)
    {
        if (text == null) return;
        text.color = BodyColor;
        SetTextMaterial(text, false);
        text.enableVertexGradient = false;
        text.raycastTarget = false;
    }

    public static void SetTitle(TMP_Text text)
    {
        if (text == null) return;
        text.color = Color.white;
        SetTextMaterial(text, true);
        text.enableVertexGradient = false;
        text.raycastTarget = false;
    }

    private static void SetTextMaterial(TMP_Text text, bool title)
    {
        if (text.font == null || text.font.material == null) return;
        string fontGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(text.font));
        string key = fontGuid + (title ? "-Title" : "-Body");
        if (!TextMaterials.TryGetValue(key, out Material material))
        {
            Directory.CreateDirectory(Root + "Generated");
            string path = Root + "Generated/" + key + ".mat";
            material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(text.font.material) { name = "Orchard " + (title ? "Title" : "Body") };
                AssetDatabase.CreateAsset(material, path);
            }
            if (material.HasProperty("_FaceColor")) material.SetColor("_FaceColor", Color.white);
            if (material.HasProperty("_FaceDilate")) material.SetFloat("_FaceDilate", 0);
            if (material.HasProperty("_OutlineWidth")) material.SetFloat("_OutlineWidth", title ? .13f : 0f);
            if (material.HasProperty("_OutlineColor")) material.SetColor("_OutlineColor", new Color32(72, 46, 25, 255));
            if (material.HasProperty("_UnderlayColor")) material.SetColor("_UnderlayColor", Color.clear);
            material.DisableKeyword("UNDERLAY_ON");
            material.DisableKeyword("UNDERLAY_INNER");
            EditorUtility.SetDirty(material);
            TextMaterials[key] = material;
        }
        text.fontSharedMaterial = material;
        text.UpdateMeshPadding();
    }

    public static void EnsureBackdrop(GameObject root)
    {
        Transform current = root.transform.Find("OrchardBackdrop");
        GameObject backdrop = current != null ? current.gameObject : new GameObject("OrchardBackdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(AspectRatioFitter), typeof(OrchardBackdrop));
        backdrop.layer = root.layer;
        backdrop.transform.SetParent(root.transform, false);
        backdrop.transform.SetAsFirstSibling();
        var rect = (RectTransform)backdrop.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        var image = backdrop.GetComponent<Image>();
        image.sprite = null; // Large artwork is loaded from Resources at runtime, never a prefab dependency.
        image.color = Color.white;
        image.raycastTarget = false;
        image.enabled = false;
        var fitter = backdrop.GetComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = 941f / 1672f;
        var so = new SerializedObject(backdrop.GetComponent<OrchardBackdrop>());
        so.FindProperty("target").objectReferenceValue = image;
        so.FindProperty("resourcePath").stringValue = "OrchardUI/Backdrop";
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    [MenuItem("Tools/Orchard UI/Apply Locked Skin to Manifest")]
    public static void ApplyAll()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before editing prefabs.");
        LoadSprites();
        SpriteFor("Panel");
        var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(Output + "prefab-manifest.json"));
        var report = new ChangeReport { generatedUtc = DateTime.UtcNow.ToString("O") };
        foreach (string path in manifest.prefabs)
        {
            var entry = new PageChange { prefab = path };
            report.pages.Add(entry);
            GameObject page = null;
            try
            {
                page = PrefabUtility.LoadPrefabContents(path);
                ApplyCommonStyles(page);
                OrchardCommonPass.Apply(page, path);
                OrchardServiceRewardPass.Apply(page, path);
                OrchardWithdrawalPass.Apply(page, path);
                OrchardNavigationPass.Apply(page, path);
                FinalizeSharedDetails(page);
                entry.imageCount = page.GetComponentsInChildren<Image>(true).Length;
                entry.textCount = page.GetComponentsInChildren<TMP_Text>(true).Length;
                entry.buttonCount = page.GetComponentsInChildren<Button>(true).Length;
                PrefabUtility.SaveAsPrefabAsset(page, path, out bool saved);
                if (!saved) throw new InvalidOperationException("Unity refused to save prefab: " + path);
            }
            catch (Exception e) { entry.error = e.ToString(); Debug.LogException(e); }
            finally { if (page != null) PrefabUtility.UnloadPrefabContents(page); }
        }
        AssetDatabase.SaveAssets();
        File.WriteAllText(Output + "apply-report.json", JsonUtility.ToJson(report, true));
        foreach (var entry in report.pages)
            if (!string.IsNullOrEmpty(entry.error)) throw new InvalidOperationException("Some Orchard pages failed; inspect apply-report.json");
        Debug.Log("[OrchardUI] Authored " + report.pages.Count + " framework prefabs.");
    }

    private static void ApplyCommonStyles(GameObject page)
    {
        foreach (Image image in page.GetComponentsInChildren<Image>(true))
        {
            if (image.sprite == null) continue;
            string role = RoleFor(image.sprite);
            if (role != null)
            {
                float originalAlpha = image.color.a;
                ApplySprite(image, role);
                // Preserve invisible interaction overlays and intentional translucent overlays.
                if (originalAlpha < .1f) image.color = new Color(1, 1, 1, originalAlpha);
            }
            else if (IsOldFullScreenBackground(image.sprite))
            {
                image.sprite = null;
                image.color = Color.clear;
                image.enabled = false;
            }
        }

        // Sprite states stored by scripts (normal/claimed/available/etc.) must use the same skin.
        foreach (MonoBehaviour component in page.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (component == null) continue;
            var serialized = new SerializedObject(component);
            SerializedProperty property = serialized.GetIterator();
            bool changed = false;
            while (property.NextVisible(true))
            {
                if (property.propertyType != SerializedPropertyType.ObjectReference || !(property.objectReferenceValue is Sprite sprite)) continue;
                string role = RoleFor(sprite);
                if (role == null) continue;
                property.objectReferenceValue = SpriteFor(role);
                changed = true;
            }
            if (changed) serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        foreach (TMP_Text text in page.GetComponentsInChildren<TMP_Text>(true))
        {
            bool isHeading = IsTitleOrButton(text.transform);
            Color previous = text.color;
            if (isHeading) SetTitle(text);
            else
            {
                SetBody(text);
                if (previous.g > previous.r * 1.5f && previous.g > previous.b * 1.3f)
                    text.color = new Color32(13, 146, 48, 255);
                else if (previous.r > .7f && previous.r > previous.g * 1.7f)
                    text.color = new Color32(185, 70, 47, 255);
                if (previous.a < .9f) { Color color = text.color; color.a = previous.a; text.color = color; }
            }
        }

        foreach (Button button in page.GetComponentsInChildren<Button>(true))
        {
            if (button.targetGraphic == null)
            {
                Graphic ownGraphic = button.GetComponent<Graphic>();
                if (ownGraphic != null && !(ownGraphic is TMP_Text)) button.targetGraphic = ownGraphic;
            }
            if (button.targetGraphic == null) continue;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, .98f, 1f);
            colors.pressedColor = new Color(.86f, .92f, .86f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(.74f, .80f, .72f, .76f);
            colors.fadeDuration = .1f;
            button.colors = colors;
        }
    }

    private static bool IsTitleOrButton(Transform transform)
    {
        Transform current = transform;
        for (int depth = 0; current != null && depth < 3; depth++, current = current.parent)
        {
            if (current.GetComponent<Button>() != null) return true;
            var image = current.GetComponent<Image>();
            if (image != null && image.sprite != null && AssetDatabase.GetAssetPath(image.sprite) == AtlasPath)
            {
                string role = image.sprite.name;
                if (role == "Title" || role == "ButtonGreen" || role == "ButtonBlue" || role == "ButtonDisabled" || role == "Badge") return true;
            }
        }
        return false;
    }

    private static void FinalizeSharedDetails(GameObject page)
    {
        foreach (Image image in page.GetComponentsInChildren<Image>(true))
        {
            if (image.sprite == null || AssetDatabase.GetAssetPath(image.sprite) != AtlasPath) continue;
            if (image.sprite.name != "Title") continue;
            Transform parent = image.transform.parent;
            if (parent == null) continue;
            for (int i = 0; i < parent.childCount; i++)
            {
                if (!parent.GetChild(i).TryGetComponent<TMP_Text>(out var label)) continue;
                Vector3 center = label.rectTransform.TransformPoint(label.rectTransform.rect.center);
                if (image.rectTransform.rect.Contains(image.rectTransform.InverseTransformPoint(center)))
                    SetTitle(label);
            }
        }
    }

    private static bool IsOldFullScreenBackground(Sprite sprite)
    {
        string path = AssetDatabase.GetAssetPath(sprite).Replace('\\', '/');
        return path.EndsWith("/RealWithdrawPanel/bg_RealWithdraw.png", StringComparison.Ordinal) || path.EndsWith("/ServicePanel/bg.png", StringComparison.Ordinal) || path.EndsWith("/SlotOther/Bg_Slot.png", StringComparison.Ordinal);
    }

    private static string RoleFor(Sprite sprite)
    {
        string path = AssetDatabase.GetAssetPath(sprite).Replace('\\', '/');
        if (path == AtlasPath) return sprite.name;
        if (!path.Contains("/Z_ReplaceAssets/UI_Frame/")) return null;
        string name = Path.GetFileNameWithoutExtension(path);
        switch (name)
        {
            case "Common_Frame": case "Bg_WithdrawFill": case "Bg_FakeWithdraw": case "Bg_SlotReward": case "Bg_SlotGetReward": case "Bg_RewardMax": return "Panel";
            case "Common_Title": case "bg_top": return "Title";
            case "Btn_Normael": case "Btn_CanWithdraw": case "Btn_Green": case "Bg_FakeCanWithdraw": case "Btn_SlotOk": case "Bg_PauseContinue": return "ButtonGreen";
            case "Btn_Slot": case "Bg_PauseBack": case "Quick Reply": return "ButtonBlue";
            case "Btn_Claimed": case "Bg_FakeNotCanWithdraw": return "ButtonDisabled";
            case "bg_InPanel": case "Bg_EmptyField": case "Service_bubble_1": case "Service_bubble_2": return "Input";
            case "Bg_WithdrawHistoryItem": case "bg_LevelFrame": case "Task_Elementbg": case "Task_RewardBg": case "bg_HintReward": case "Bg_PauseItem": return "Card";
            case "Bg_Rate": case "bg_Rate": case "bg_Multiple": case "Bg_Multiple": return "Badge";
            case "Bg_FakeProgressFill": case "Task_ProgressBg": case "SlotEnter_Progressbg": return "ProgressTrack";
            case "Bg_FakeProgress": case "Task_ProgressFill": case "SlotEnter_ProgressFill": return "ProgressFill";
            default: return null;
        }
    }
}
