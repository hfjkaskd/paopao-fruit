using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using AdvancedInputFieldPlugin;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static partial class OrchardApprovedPass
{
    private const string ServiceInputPrefab = "Assets/BizzaWZ/Final/Real/UI/ServicePanel/ServicePanel.prefab";
    private const string ServiceInputOutput = "Design/ServiceFreeInput-20261008/";

    private static TMP_FontAsset ServiceMessageFont()
    {
        const string folder = "Assets/OrchardUI/Fonts/Service";
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/OrchardUI/Fonts", "Service");
        var font = CreateServiceFont(folder + "/Message SDF.asset", "Assets/OrchardUI/Fonts/Fidelity/Baloo2-SemiBold.ttf", 90, 10);
        // This fallback belongs only to editable support messages, not the global UI font chain.
        var fallback = CreateServiceFont(folder + "/CJK SDF.asset", folder + "/NotoSansSC-Regular.otf", 48, 5);
        font.fallbackFontAssetTable = new List<TMP_FontAsset> { fallback };
        font.faceInfo = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BodyFontPath).faceInfo;
        EditorUtility.SetDirty(font);
        return font;
    }

    private static TMP_FontAsset CreateServiceFont(string path, string source, int size, int padding)
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if (font != null) return font;
        font = TMP_FontAsset.CreateFontAsset(AssetDatabase.LoadAssetAtPath<Font>(source), size, padding,
            UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
        font.name = Path.GetFileNameWithoutExtension(path);
        AssetDatabase.CreateAsset(font, path);
        AssetDatabase.AddObjectToAsset(font.material, font);
        foreach (var texture in font.atlasTextures) AssetDatabase.AddObjectToAsset(texture, font);
        EditorUtility.SetDirty(font);
        AssetDatabase.SaveAssets();
        return font;
    }

    private static void ConfigureServiceMessageText(TMP_Text text)
    {
        text.font = ServiceMessageFont();
        text.fontSharedMaterial = text.font.material;
        text.UpdateMeshPadding();
    }

    private static void ConfigureServiceInput(GameObject root)
    {
#if BIZZA_REAL_WITHDRAW
        var page = root.GetComponent<ServicePanel>();
        const string input = "Content/InputNode/";
        var background = root.transform.Find(input + "bg").GetComponent<Image>();
        var button = background.GetComponent<Button>() ?? background.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        button.transition = Selectable.Transition.None;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        background.raycastTarget = true;
        page.inputText.interactable = true;
        page.inputText.gameObject.SetActive(true);
        var inputSettings = new SerializedObject(page.inputText);
        inputSettings.FindProperty("readOnly").boolValue = false;
        inputSettings.FindProperty("contentType").intValue = (int)ContentType.STANDARD;
        inputSettings.FindProperty("characterValidation").intValue = (int)CharacterValidation.NONE;
        inputSettings.FindProperty("keyboardType").intValue = (int)KeyboardType.DEFAULT;
        inputSettings.FindProperty("characterLimit").intValue = 0;
        inputSettings.FindProperty("emojisAllowed").boolValue = true;
        inputSettings.FindProperty("autofillType").intValue = 0;
        inputSettings.FindProperty("onEndEdit.m_PersistentCalls.m_Calls").ClearArray();
        inputSettings.FindProperty("onActionBarAction.m_PersistentCalls.m_Calls").ClearArray();
        inputSettings.ApplyModifiedPropertiesWithoutUndo();
        // Keep the visual input and text in the same UI plane; the vendor handles caret/selection.
        var inputRect = (RectTransform)page.inputText.transform;
        var position = inputRect.localPosition; position.z = 0; inputRect.localPosition = position;
        foreach (var text in page.inputText.GetComponentsInChildren<TMP_Text>(true))
        {
            ConfigureServiceMessageText(text);
            text.rectTransform.localScale = Vector3.one;
            text.enabled = true;
            text.raycastTarget = true;
            text.richText = false;
        }

        var oldKeyboard = root.GetComponent<KeyBoardPanel>();
        if (oldKeyboard != null) UnityEngine.Object.DestroyImmediate(oldKeyboard);
        var keyboard = root.GetComponent<OrchardServiceKeyboardLayout>() ?? root.AddComponent<OrchardServiceKeyboardLayout>();
        var keyboardSettings = new SerializedObject(keyboard);
        keyboardSettings.FindProperty("inputBounds").objectReferenceValue = background.rectTransform;
        keyboardSettings.FindProperty("chatViewport").objectReferenceValue = page.scrollRect.viewport;
        keyboardSettings.FindProperty("quickQuestions").objectReferenceValue = page.selectQuestionButton.gameObject;
        string[] paths = { input + "bg", input + "InputField", input + "ClearBtn", input + "CanSendBtn", input + "NotCanSendBtn", "Content/ReferenceInputLine" };
        var rects = keyboardSettings.FindProperty("movingRects"); rects.arraySize = paths.Length;
        for (int i = 0; i < paths.Length; i++) rects.GetArrayElementAtIndex(i).objectReferenceValue = root.transform.Find(paths[i]);
        keyboardSettings.ApplyModifiedPropertiesWithoutUndo();
        var pageSettings = new SerializedObject(page);
        pageSettings.FindProperty("messageInputButton").objectReferenceValue = button;
        pageSettings.FindProperty("keyboardLayout").objectReferenceValue = keyboard;
        pageSettings.FindProperty("keepQuestionPickerVisible").boolValue = true;
        pageSettings.ApplyModifiedPropertiesWithoutUndo();
#endif
    }

    public static void ApplyServiceInput()
    {
        var root = PrefabUtility.LoadPrefabContents(ServiceInputPrefab);
        try { ConfigureServiceInput(root); PrefabUtility.SaveAsPrefabAsset(root, ServiceInputPrefab); }
        finally { PrefabUtility.UnloadPrefabContents(root); }
#if BIZZA_REAL_WITHDRAW
        var chat = PrefabUtility.LoadPrefabContents(ServiceChatPrefab);
        try
        {
            ConfigureServiceMessageText(chat.GetComponent<ChatElement>().chatTxt);
            PrefabUtility.SaveAsPrefabAsset(chat, ServiceChatPrefab);
        }
        finally { PrefabUtility.UnloadPrefabContents(chat); }
#endif
        AssetDatabase.SaveAssets();
        ValidateServiceInput();
    }

    public static void ValidateServiceInput()
    {
#if BIZZA_REAL_WITHDRAW
        Directory.CreateDirectory(ServiceInputOutput);
        var report = new StringBuilder("Disposable prefab/raycast/text/keyboard-height checks. No support message, network request or save mutation.\n");
        foreach (int height in new[] { 1280, 1600 })
        {
            var result = OrchardSkinValidation.PreviewPrefab(ServiceInputPrefab,
                ServiceInputOutput + "keyboard-720x" + height + ".png", root =>
                {
                    ConfigureReferencePreview(root, "service");
                    var page = root.GetComponent<ServicePanel>();
                    page.inputText.gameObject.SetActive(true);
                    page.canSendObj.SetActive(true); page.notCanSendObj.SetActive(false);
                }, 720, height, root => CheckServiceInput(root, height, report));
            if (!string.IsNullOrEmpty(result.error)) throw new InvalidOperationException(result.error);
        }
        report.AppendLine("No Android IME/device test; native keyboard focus uses the existing AdvancedInputField.ManualSelect API. No APK built.");
        File.WriteAllText(ServiceInputOutput + "result.txt", report.ToString());
        Debug.Log(report.ToString());
#endif
    }

#if BIZZA_REAL_WITHDRAW
    private static void CheckServiceInput(GameObject root, int height, StringBuilder report)
    {
        var page = root.GetComponent<ServicePanel>();
        var field = page.inputText;
        var settings = new SerializedObject(page);
        var focus = settings.FindProperty("messageInputButton").objectReferenceValue as Button;
        var adapter = root.GetComponent<OrchardServiceKeyboardLayout>();
        if (focus == null || focus.targetGraphic != focus.GetComponent<Image>() || !focus.targetGraphic.raycastTarget ||
            field.ReadOnly || !field.interactable || field.CharacterValidation != CharacterValidation.NONE || field.CharacterLimit != 0 ||
            root.GetComponent<KeyBoardPanel>() != null)
            throw new InvalidOperationException("Free input is not connected to its visible button or remains restricted.");
        var canvas = root.GetComponentInParent<Canvas>();
        var camera = canvas.worldCamera;
        var raycaster = canvas.gameObject.GetComponent<GraphicRaycaster>() ?? canvas.gameObject.AddComponent<GraphicRaycaster>();
        var previousEvents = EventSystem.current;
        var eventsObject = new GameObject("Disposable input raycast EventSystem", typeof(EventSystem));
        var events = eventsObject.GetComponent<EventSystem>();
        try
        {
            Canvas.ForceUpdateCanvases();
            camera.Render(); // Assign CanvasRenderer depths before running the real GraphicRaycaster.
            var bounds = focus.GetComponent<RectTransform>();
            foreach (float x in new[] { .08f, .5f, .92f })
            foreach (float y in new[] { .25f, .5f, .75f })
            {
                Vector2 point = RectTransformUtility.WorldToScreenPoint(camera, bounds.TransformPoint(new Vector3(
                    Mathf.Lerp(bounds.rect.xMin, bounds.rect.xMax, x), Mathf.Lerp(bounds.rect.yMin, bounds.rect.yMax, y))));
                var pointer = new PointerEventData(events) { position = point };
                var hits = new List<RaycastResult>(); raycaster.Raycast(pointer, hits);
                if (hits.Count == 0) throw new InvalidOperationException("Message box is not hittable: point=" + point +
                    " canvas=" + canvas.name + " graphicCanvas=" + focus.targetGraphic.canvas.name +
                    " graphics=" + GraphicRegistry.GetGraphicsForCanvas(canvas).Count +
                    " enabled=" + focus.targetGraphic.isActiveAndEnabled + " depth=" + focus.targetGraphic.depth +
                    " screen=" + Screen.width + "x" + Screen.height + " camera=" + camera.pixelRect);
                var hit = hits[0].gameObject.transform;
                if (hit != focus.transform && hit != field.transform && !hit.IsChildOf(field.transform))
                    throw new InvalidOperationException("Message box blocked by " + hit.name);
            }
            // Exercise the plugin's real text storage and the existing page's send-state decision without sending.
            foreach (string sample in new[] { "Olá! Preciso de ajuda: R$ 0,01.", "Hello 123 ! @ #", "自由输入测试", "" })
            {
                uint[] missing;
                if (!field.TextRenderer.GetComponent<TMP_Text>().font.HasCharacters(sample, out missing, true, true) ||
                    !page.chatElementPrefab.chatTxt.font.HasCharacters(sample, out missing, true, true))
                    throw new InvalidOperationException("Input/chat font cannot display the entered text: " + sample);
                field.Text = sample;
                if (field.Text != sample || page.CanSend != !string.IsNullOrWhiteSpace(sample))
                    throw new InvalidOperationException("Custom input did not round trip or enable Send correctly.");
            }
            var background = root.transform.Find("OrchardBackdrop");
            var title = root.transform.Find("Title");
            Vector3 backgroundPosition = background.position, titlePosition = title.position;
            Vector3 originalInput = bounds.position;
            var viewport = page.scrollRect.viewport;
            Vector2 min = viewport.offsetMin, max = viewport.offsetMax;
            foreach (float ratio in new[] { .35f, .5f })
            {
                int keyboard = Mathf.RoundToInt(height * ratio);
                adapter.ApplyKeyboardHeight(keyboard);
                var corners = new Vector3[4]; bounds.GetWorldCorners(corners);
                float bottom = RectTransformUtility.WorldToScreenPoint(camera, corners[0]).y;
                float top = RectTransformUtility.WorldToScreenPoint(camera, corners[1]).y;
                viewport.GetWorldCorners(corners);
                float chatBottom = RectTransformUtility.WorldToScreenPoint(camera, corners[0]).y;
                if (bottom <= keyboard || chatBottom < top || background.position != backgroundPosition || title.position != titlePosition)
                    throw new InvalidOperationException("Keyboard covers composer/chat or moves background/header.");
                adapter.ApplyKeyboardHeight(0);
                if ((bounds.position - originalInput).sqrMagnitude > .01f || viewport.offsetMin != min || viewport.offsetMax != max)
                    throw new InvalidOperationException("Closing keyboard did not restore input/chat viewport.");
            }
            // Show a typed message in this static visual fixture; business lifecycle remains disabled.
            field.Text = height == 1600 ? "自由输入测试 123" : "Olá! Preciso de ajuda.";
            field.PlaceholderTextRenderer.Hide(); field.ProcessedTextRenderer.Hide(); field.TextRenderer.Show();
            var text = field.TextRenderer.GetComponent<TMP_Text>(); text.text = field.Text;
            text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = Vector2.zero; text.rectTransform.offsetMax = Vector2.zero;
            text.rectTransform.localScale = Vector3.one; text.ForceMeshUpdate(true, true);
            adapter.ApplyKeyboardHeight(Mathf.RoundToInt(height * .42f));
            Canvas.ForceUpdateCanvases();
            report.AppendLine("PASS 720x" + height + ": nine input-area raycasts; Latin/Chinese/numbers/punctuation round trip and input/chat glyph coverage; empty/nonempty send state; two keyboard heights; background/title fixed; composer visible; chat above composer; closing restores layout.");
        }
        finally { UnityEngine.Object.DestroyImmediate(eventsObject); if (previousEvents != null) EventSystem.current = previousEvents; }
    }
#endif
}
