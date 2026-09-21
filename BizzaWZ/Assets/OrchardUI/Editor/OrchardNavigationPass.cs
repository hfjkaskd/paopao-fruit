using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>Explicit prefab authoring for navigation artwork; no runtime UI construction or event changes.</summary>
public static class OrchardNavigationPass
{
    public const string AtlasPath = "Assets/OrchardUI/Art/Navigation.png";
    private const string CommonArt = "Assets/BizzaWZ/Common/BizzaGame/Z_ReplaceAssets/UI_Frame/";
    private const string FinalArt = "Assets/BizzaWZ/Final/BizzaGame/Z_ReplaceAssets/UI_Frame/";
    private static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>(StringComparer.Ordinal);

    // Measured on the approved 1254 x 1254 transparent atlas. Coordinates below
    // use the source image's top-left origin. Tight bounds exclude the noisy gaps.
    private readonly struct Slice
    {
        public readonly string name;
        public readonly int x, top, width, height;
        public Slice(string name, int x, int top, int width, int height)
        { this.name = name; this.x = x; this.top = top; this.width = width; this.height = height; }
    }

    private static readonly Slice[] Slices =
    {
        new Slice("Back", 70, 82, 326, 321),
        new Slice("History", 462, 83, 326, 320),
        new Slice("Help", 855, 83, 326, 320),
        new Slice("Close", 70, 471, 326, 320),
        new Slice("Settings", 462, 471, 326, 321),
        new Slice("Chat", 855, 471, 327, 321),
        new Slice("LeavesLeft", 110, 870, 252, 297),
        new Slice("LeavesRight", 508, 869, 254, 298),
        new Slice("FlowerLeaves", 885, 861, 297, 311)
    };

    /// <summary>The caller copies approved artwork to AtlasPath before invoking this method.</summary>
    public static void ImportArt()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play before importing Orchard navigation artwork.");
        if (!File.Exists(AtlasPath)) throw new FileNotFoundException("Missing approved navigation atlas", AtlasPath);
        AssetDatabase.ImportAsset(AtlasPath, ImportAssetOptions.ForceSynchronousImport);
        var importer = AssetImporter.GetAtPath(AtlasPath) as TextureImporter;
        if (importer == null) throw new InvalidOperationException("Navigation artwork is not a TextureImporter asset.");
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 100;
        importer.mipmapEnabled = false;
        importer.isReadable = false;
        importer.alphaIsTransparency = true;
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
            settings.format = TextureImporterFormat.ASTC_4x4;
            settings.compressionQuality = 80;
            importer.SetPlatformTextureSettings(settings);
        }
        importer.SaveAndReimport();
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath);
        if (texture == null || texture.width != 1254 || texture.height != 1254)
            throw new InvalidOperationException("Navigation slices require the approved 1254 x 1254 source atlas.");

        var factories = new SpriteDataProviderFactories();
        factories.Init();
        var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        var previousIds = new Dictionary<string, GUID>(StringComparer.Ordinal);
        foreach (SpriteRect previous in provider.GetSpriteRects()) previousIds[previous.name] = previous.spriteID;
        var rects = new SpriteRect[Slices.Length];
        var nameIds = new List<SpriteNameFileIdPair>(Slices.Length);
        for (int i = 0; i < Slices.Length; i++)
        {
            Slice slice = Slices[i];
            GUID id = previousIds.TryGetValue(slice.name, out GUID previousId) ? previousId : GUID.Generate();
            rects[i] = new SpriteRect
            {
                name = slice.name,
                spriteID = id,
                rect = new Rect(slice.x, 1254 - slice.top - slice.height, slice.width, slice.height),
                alignment = SpriteAlignment.Center,
                pivot = new Vector2(.5f, .5f),
                border = Vector4.zero
            };
            nameIds.Add(new SpriteNameFileIdPair(slice.name, id));
        }
        provider.SetSpriteRects(rects);
        provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(nameIds);
        provider.Apply();
        importer.SaveAndReimport();
        Sprites.Clear();
        LoadSprites();
        foreach (Slice slice in Slices) SpriteFor(slice.name);
        AssetDatabase.SaveAssets();
    }

    /// <summary>Run after the common and page-specific passes, before saving the prefab.</summary>
    public static void Apply(GameObject root, string assetPath)
    {
        if (root == null || string.IsNullOrEmpty(assetPath) ||
            !assetPath.Replace('\\', '/').StartsWith("Assets/BizzaWZ/", StringComparison.Ordinal)) return;
        LoadSprites();
        // Snapshot before adding decorations. The exact sprite allowlist deliberately
        // excludes pause/play, sound, music, vibration, reward and payment artwork.
        Image[] images = root.GetComponentsInChildren<Image>(true);
        foreach (Image source in images)
        {
            string role = NavigationRole(source.sprite);
            if (role == null) continue;
            Button button = OwningButton(source.transform, root.transform);
            Image surface = source;
            if (button != null)
            {
                surface = button.GetComponent<Image>();
                if (surface == null) surface = button.gameObject.AddComponent<Image>();
                if (surface != source)
                {
                    // Keep the original child component, sprite and every inbound
                    // binding. Its old glyph stays available for repeat authoring.
                    source.enabled = false;
                    source.raycastTarget = false;
                }
                button.targetGraphic = surface;
                surface.raycastTarget = true;
                RemapButtonStates(button);
            }
            SetNavigationImage(surface, role);
            if (role == "Close")
            {
                surface.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 104f);
                surface.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 104f);
            }
            if (role == "Chat") AlignChat(surface.rectTransform);
        }

        foreach (Image plate in images)
        {
            // Green actions stay undecorated, including previously authored prefabs.
            // Preserve their nodes, artwork, labels and Button bindings.
            if (IsGreenButton(plate.sprite))
            {
                HideButtonLeaves(plate);
                continue;
            }
            if (plate.sprite == null || AssetDatabase.GetAssetPath(plate.sprite) != OrchardSkinAuthoring.AtlasPath) continue;
            string role = plate.sprite.name;
            if (role == "Title") AddLeafPair(plate, true);
        }
    }

    private static bool IsGreenButton(Sprite sprite)
    {
        if (sprite == null) return false;
        string path = AssetDatabase.GetAssetPath(sprite);
        return (path == OrchardSkinAuthoring.AtlasPath && sprite.name == "ButtonGreen") ||
            path == "Assets/OrchardUI/Art/HudNaturalGreenButton.png" ||
            path == "Assets/OrchardUI/Art/SlotEmeraldButton.png";
    }

    private static void HideButtonLeaves(Image plate)
    {
        foreach (Image decoration in plate.GetComponentsInChildren<Image>(true))
        {
            if (decoration == plate || decoration.sprite == null) continue;
            if (AssetDatabase.GetAssetPath(decoration.sprite) != AtlasPath) continue;
            string role = decoration.sprite.name;
            if (role != "LeavesLeft" && role != "LeavesRight" && role != "FlowerLeaves") continue;
            decoration.enabled = false;
            EditorUtility.SetDirty(decoration);
        }
    }

    private static string NavigationRole(Sprite sprite)
    {
        if (sprite == null) return null;
        string path = AssetDatabase.GetAssetPath(sprite).Replace('\\', '/');
        if (path == AtlasPath)
        {
            switch (sprite.name)
            {
                case "Back": case "History": case "Help": case "Close": case "Settings": case "Chat": return sprite.name;
                default: return null;
            }
        }
        switch (path)
        {
            case FinalArt + "RealWithdrawPanel/Icon_Back.png": return "Back";
            case FinalArt + "RealWithdrawPanel/Icon_Historiy.png": return "History";
            case FinalArt + "RealWithdrawPanel/Icon_FAQ.png": return "Help";
            case CommonArt + "Common/Btn_Close.png":
            case FinalArt + "ServicePanel/Icon_CloseQuickPanel.png":
            case FinalArt + "Withdrawal/Icon_CloseFillPanel.png": return "Close";
            case CommonArt + "GamePanel/Icon_Setting.png": return "Settings";
            case FinalArt + "ServicePanel/Icon_ServiceEnter.png": return "Chat";
            default: return null;
        }
    }

    private static Button OwningButton(Transform image, Transform root)
    {
        Transform current = image;
        for (int depth = 0; current != null && depth < 3; depth++, current = current.parent)
        {
            if (current.TryGetComponent<Button>(out var button)) return button;
            if (current == root) break;
        }
        return null;
    }

    private static void SetNavigationImage(Image image, string role)
    {
        image.sprite = SpriteFor(role);
        image.overrideSprite = null;
        image.material = null;
        image.color = Color.white;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.useSpriteMesh = false;
        image.pixelsPerUnitMultiplier = 1f;
        image.enabled = true;
        EditorUtility.SetDirty(image);
    }

    private static void RemapButtonStates(Button button)
    {
        SpriteState states = button.spriteState;
        states.highlightedSprite = ReplacementFor(states.highlightedSprite);
        states.pressedSprite = ReplacementFor(states.pressedSprite);
        states.selectedSprite = ReplacementFor(states.selectedSprite);
        states.disabledSprite = ReplacementFor(states.disabledSprite);
        button.spriteState = states;
    }

    private static Sprite ReplacementFor(Sprite sprite)
    {
        string role = NavigationRole(sprite);
        return role == null ? sprite : SpriteFor(role);
    }

    private static void AlignChat(RectTransform rect)
    {
        Vector2 position = rect.anchoredPosition;
        rect.anchorMin = new Vector2(1f, rect.anchorMin.y);
        rect.anchorMax = new Vector2(1f, rect.anchorMax.y);
        rect.pivot = new Vector2(1f, .5f);
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 100f);
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 100f);
        rect.anchoredPosition = new Vector2(-18f, position.y);
    }

    private static void AddLeafPair(Image plate, bool title)
    {
        RectTransform rect = plate.rectTransform;
        float width = Mathf.Abs(rect.rect.width);
        float height = Mathf.Abs(rect.rect.height);
        if (width < 1f) width = Mathf.Abs(rect.sizeDelta.x);
        if (height < 1f) height = Mathf.Abs(rect.sizeDelta.y);
        // Small badges, compact task actions and invisible hit overlays stay clear.
        if (!plate.enabled || plate.color.a < .9f || width < (title ? 260f : 240f) || height < 70f) return;
        if (HasOtherLeafDecoration(plate.transform)) return;
        float size = Mathf.Clamp(height * (title ? .42f : .36f), 28f, title ? 64f : 54f);
        float y = title ? height * .12f : 0f;
        Leaf(plate, "OrchardNavLeavesLeft", "LeavesLeft", false, size, y);
        Leaf(plate, "OrchardNavLeavesRight", "LeavesRight", true, size, y);
    }

    private static bool HasOtherLeafDecoration(Transform plate)
    {
        foreach (Image child in plate.GetComponentsInChildren<Image>(true))
        {
            if (child.transform == plate || child.name == "OrchardNavLeavesLeft" || child.name == "OrchardNavLeavesRight") continue;
            if (child.sprite == null) continue;
            string name = child.sprite.name;
            if (name == "LeavesLeft" || name == "LeavesRight" || name == "FlowerLeaves") return true;
        }
        return false;
    }

    private static void Leaf(Image plate, string objectName, string role, bool right, float size, float y)
    {
        Transform existing = plate.transform.Find(objectName);
        GameObject leaf = existing != null ? existing.gameObject :
            new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
        leaf.layer = plate.gameObject.layer;
        leaf.transform.SetParent(plate.transform, false);
        // Behind all label children, with most of the ornament outside the plate.
        // No reparenting of existing objects or decoration near the text area.
        leaf.transform.SetAsFirstSibling();
        var rect = (RectTransform)leaf.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(right ? 1f : 0f, .5f);
        rect.pivot = new Vector2(.5f, .5f);
        rect.sizeDelta = new Vector2(size * .85f, size);
        rect.anchoredPosition = new Vector2((right ? 1f : -1f) * size * .33f, y);
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
        var image = leaf.GetComponent<Image>();
        SetNavigationImage(image, role);
        image.raycastTarget = false;
        image.maskable = plate.maskable;
        var layout = leaf.GetComponent<LayoutElement>();
        if (layout == null) layout = leaf.AddComponent<LayoutElement>();
        layout.ignoreLayout = true;
    }

    private static Sprite SpriteFor(string role)
    {
        LoadSprites();
        if (!Sprites.TryGetValue(role, out Sprite sprite))
            throw new InvalidOperationException("Import approved Orchard navigation sprites before applying: " + role);
        return sprite;
    }

    private static void LoadSprites()
    {
        if (Sprites.Count != 0) return;
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(AtlasPath))
            if (asset is Sprite sprite) Sprites[sprite.name] = sprite;
    }
}
