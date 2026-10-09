using System;
using System.IO;
using System.Text;
using AdvancedInputFieldPlugin;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static partial class OrchardApprovedPass
{
    private const string AccountScrollPrefab = "Assets/BizzaWZ/Final/Real/UI/WithdrawFillPanel/WithdrawFillPanel.prefab";
    private const string AccountScrollOutput = "Design/AccountScroll-20261008/";

    private static void RestoreAccountScrollRoots(GameObject root)
    {
        var fill = root.transform.Find("Root/FillRoot");
        var viewport = fill.Find("FormViewport");
        if (viewport == null) return;
        var content = viewport.Find("FormContent");
        while (content.childCount > 0) ReparentRect((RectTransform)content.GetChild(0), fill, new Vector2(.5f, .5f));
        UnityEngine.Object.DestroyImmediate(viewport.gameObject);
        var adapter = root.GetComponent<OrchardKeyboardFormScroll>();
        if (adapter != null) UnityEngine.Object.DestroyImmediate(adapter);
    }

    private static void ReparentRect(RectTransform rect, Transform parent, Vector2 anchor)
    {
        Vector3 position = rect.position;
        Vector2 size = rect.rect.size;
        rect.SetParent(parent, true);
        rect.anchorMin = rect.anchorMax = anchor;
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
        rect.position = position;
    }

    private static void ConfigureAccountScroll(GameObject root)
    {
#if BIZZA_REAL_WITHDRAW
        if (root.transform.Find("Root/FillRoot/FormViewport") != null) return;
        var page = root.GetComponent<UIWithdrawalPanel>();
        var fill = (RectTransform)root.transform.Find("Root/FillRoot");
        var legacy = root.GetComponent<KeyBoardPanel>();
        if (legacy != null) UnityEngine.Object.DestroyImmediate(legacy);

        var viewport = new GameObject("FormViewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
        viewport.layer = fill.gameObject.layer;
        var view = (RectTransform)viewport.transform;
        view.SetParent(fill, false);
        view.anchorMin = view.anchorMax = view.pivot = new Vector2(.5f, 1);
        view.anchoredPosition = new Vector2(0, -240);
        view.sizeDelta = new Vector2(770, 1375);
        var hitSurface = viewport.GetComponent<Image>();
        hitSurface.color = Color.clear;
        hitSurface.raycastTarget = true;

        var contentObject = new GameObject("FormContent", typeof(RectTransform));
        contentObject.layer = fill.gameObject.layer;
        var content = (RectTransform)contentObject.transform;
        content.SetParent(view, false);
        content.anchorMin = content.anchorMax = content.pivot = new Vector2(.5f, 1);
        content.sizeDelta = new Vector2(852, 1375);
        for (int i = fill.childCount - 1; i >= 0; i--)
        {
            var child = (RectTransform)fill.GetChild(i);
            if (child == view || child == page.BG || child.name == "ApprovedPlaque" ||
                child.name == "Title" || child.name == "pageClose") continue;
            ReparentRect(child, content, new Vector2(.5f, 1));
            child.SetAsFirstSibling();
        }

        var scroll = viewport.GetComponent<ScrollRect>();
        scroll.content = content;
        scroll.viewport = view;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.inertia = false;
        scroll.scrollSensitivity = 30;

        var adapter = root.AddComponent<OrchardKeyboardFormScroll>();
        var settings = new SerializedObject(adapter);
        settings.FindProperty("scroll").objectReferenceValue = scroll;
        settings.FindProperty("contentEnd").objectReferenceValue = page.InputRoot;
        settings.FindProperty("viewportHeight").floatValue = 1375;
        var fields = new[] { page.accountNameInput, page.CPFNumberInput, page.paypalMailInput,
            page.accPhoneMailInput, page.accountIdentificationInput };
        var inputs = settings.FindProperty("inputs"); inputs.arraySize = fields.Length;
        for (int i = 0; i < fields.Length; i++) inputs.GetArrayElementAtIndex(i).objectReferenceValue = fields[i];
        settings.ApplyModifiedPropertiesWithoutUndo();
        var pageSettings = new SerializedObject(page);
        pageSettings.FindProperty("accountScroll").objectReferenceValue = adapter;
        pageSettings.ApplyModifiedPropertiesWithoutUndo();
#endif
    }

    public static void ApplyAccountScroll()
    {
        var root = PrefabUtility.LoadPrefabContents(AccountScrollPrefab);
        try
        {
            ConfigureAccountScroll(root);
            PrefabUtility.SaveAsPrefabAsset(root, AccountScrollPrefab);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
        ValidateAccountScroll();
    }

    public static void ValidateAccountScroll()
    {
#if BIZZA_REAL_WITHDRAW
        Directory.CreateDirectory(AccountScrollOutput);
        var report = new StringBuilder("Disposable prefab and native-height/drag fixtures only; no account, saved input, network request or APK build.\n");
        foreach (int height in new[] { 1600, 1280 })
        {
            Exception failure = null;
            var preview = OrchardSkinValidation.PreviewPrefab(AccountScrollPrefab,
                AccountScrollOutput + "keyboard-720x" + height + ".png", root =>
                {
                    ConfigureReferencePreview(root, "withdraw-account");
                    var page = root.GetComponent<UIWithdrawalPanel>();
                    page.InputRoot.Find("InfoContent").gameObject.SetActive(true);
                    foreach (var field in root.GetComponentsInChildren<AdvancedInputField>(true)) field.Text = "";
                    foreach (var b in root.GetComponentsInChildren<ScrollRect>(true)) b.enabled = true;
                }, 720, height, root =>
                {
                    try { CheckAccountScroll(root, height, report); }
                    catch (Exception error) { failure = error; }
                });
            if (!string.IsNullOrEmpty(preview.error)) throw new InvalidOperationException(preview.error);
            if (failure != null) throw failure;
        }
        File.WriteAllText(AccountScrollOutput + "result.txt", report.ToString());
        Debug.Log(report.ToString());
#endif
    }

    public static void PreviewAccountScrollAtRest()
    {
#if BIZZA_REAL_WITHDRAW
        var preview = OrchardSkinValidation.PreviewPrefab(AccountScrollPrefab,
            AccountScrollOutput + "keyboard-closed-720x1600.png",
            root => ConfigureReferencePreview(root, "withdraw-account"), 720, 1600,
            root => root.GetComponent<OrchardKeyboardFormScroll>().ApplyKeyboardHeight(0));
        if (!string.IsNullOrEmpty(preview.error)) throw new InvalidOperationException(preview.error);
#endif
    }

#if BIZZA_REAL_WITHDRAW
    private static void CheckAccountScroll(GameObject root, int height, StringBuilder report)
    {
        var page = root.GetComponent<UIWithdrawalPanel>();
        var adapter = root.GetComponent<OrchardKeyboardFormScroll>();
        var scroll = root.GetComponentInChildren<ScrollRect>(true);
        if (root.GetComponent<KeyBoardPanel>() != null || scroll.movementType != ScrollRect.MovementType.Clamped)
            throw new InvalidOperationException("Whole-page/unbounded keyboard dragging remains enabled.");
        var background = root.transform.Find("OrchardBackdrop");
        var title = root.transform.Find("Root/FillRoot/Title");
        var back = root.transform.Find("Root/FillRoot/pageClose");
        Vector3 backgroundPosition = background.position, titlePosition = title.position, backPosition = back.position;
        Vector3 rootPosition = root.transform.position;
        if (background.IsChildOf(scroll.content) || title.IsChildOf(scroll.content) || back.IsChildOf(scroll.content))
            throw new InvalidOperationException("Fixed artwork/navigation is inside scrolling content.");
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(page.InputRoot);
        adapter.RefreshContent();
        int keyboard = Mathf.RoundToInt(height * .42f);
        adapter.ApplyKeyboardHeight(keyboard);
        var canvas = root.GetComponentInParent<Canvas>();
        var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        var corners = new Vector3[4];
        foreach (var input in new[] { page.accountNameInput, page.CPFNumberInput, page.paypalMailInput })
        {
            adapter.RevealInput((RectTransform)input.transform);
            input.GetComponent<RectTransform>().GetWorldCorners(corners);
            float bottom = RectTransformUtility.WorldToScreenPoint(camera, corners[0]).y;
            float top = RectTransformUtility.WorldToScreenPoint(camera, corners[1]).y;
            scroll.viewport.GetWorldCorners(corners);
            float viewportTop = RectTransformUtility.WorldToScreenPoint(camera, corners[1]).y;
            if (bottom < keyboard + 1 || top > viewportTop + 1)
                throw new InvalidOperationException(input.name + " not fully visible above keyboard: " + bottom + ", " + top);
        }
        var oldEvents = EventSystem.current;
        var eventObject = new GameObject("Disposable account scroll event system", typeof(EventSystem));
        var events = eventObject.GetComponent<EventSystem>();
        try
        {
            var pointer = new PointerEventData(events) { button = PointerEventData.InputButton.Left,
                position = new Vector2(360, height * .7f), pressPosition = new Vector2(360, height * .7f) };
            scroll.OnInitializePotentialDrag(pointer);
            scroll.OnBeginDrag(pointer);
            pointer.position += new Vector2(0, -10000); pointer.delta = new Vector2(0, -10000); scroll.OnDrag(pointer);
            RequireAccountOffset(scroll);
            pointer.position += new Vector2(0, 20000); pointer.delta = new Vector2(0, 20000); scroll.OnDrag(pointer);
            RequireAccountOffset(scroll);
            scroll.OnEndDrag(pointer);
            if (background.position != backgroundPosition || title.position != titlePosition ||
                back.position != backPosition || root.transform.position != rootPosition)
                throw new InvalidOperationException("Keyboard/drag moved the fixed page.");
        }
        finally { UnityEngine.Object.DestroyImmediate(eventObject); if (oldEvents != null) EventSystem.current = oldEvents; }
        adapter.ApplyKeyboardHeight(0);
        if (Mathf.Abs(scroll.content.anchoredPosition.y) > .1f || Mathf.Abs(scroll.viewport.rect.height - 1375) > .1f)
            throw new InvalidOperationException("Keyboard close did not restore the viewport and scroll offset.");
        report.AppendLine("PASS 720x" + height + ": keyboard open; 3 field focus changes; extreme up/down drags clamped; backdrop/title/back/root fixed; keyboard close restores form.");
        // Validation messages increase form height while the keyboard is already open.
        adapter.ApplyKeyboardHeight(keyboard);
        adapter.RevealInput((RectTransform)page.paypalMailInput.transform);
        foreach (var error in page.ErrorList) error.SetActive(true);
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(page.InputRoot);
        adapter.RefreshContent();
        page.paypalMailInput.GetComponent<RectTransform>().GetWorldCorners(corners);
        if (RectTransformUtility.WorldToScreenPoint(camera, corners[0]).y < keyboard + 1)
            throw new InvalidOperationException("Validation rows moved the focused email field behind the keyboard.");
        foreach (var error in page.ErrorList) error.SetActive(false);
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(page.InputRoot);
        adapter.RefreshContent();
        report.AppendLine("PASS: validation-message expansion keeps the selected field above the keyboard.");
        // Leave the email field visible for this diagnostic preview.
        adapter.ApplyKeyboardHeight(keyboard);
        adapter.RevealInput((RectTransform)page.paypalMailInput.transform);
    }

    private static void RequireAccountOffset(ScrollRect scroll)
    {
        float offset = scroll.content.anchoredPosition.y;
        float max = Mathf.Max(0, scroll.content.rect.height - scroll.viewport.rect.height);
        if (offset < -.1f || offset > max + .1f) throw new InvalidOperationException("Form escaped its scroll bounds.");
    }
#endif
}
