using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Editor authoring only: the saved prefab owns the settings presentation.
public static class OrchardSettingsPresentation
{
    private const string PrefabPath = "Assets/BizzaWZ/Common/UI/SettingPanel/PausePanel.prefab";
    private const string Output = "Design/SettingsWithoutLanguage-20261008/";

    public static void Apply()
    {
        Directory.CreateDirectory(Output);
        if (!File.Exists(Output + "PausePanel-before.prefab")) File.Copy(PrefabPath, Output + "PausePanel-before.prefab");
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            RemoveLanguageRow(root);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        Render("settings-16x9", 1080, 1920);
        Render("settings-20x9", 1080, 2400);
        File.WriteAllText(Output + "result.txt", "PASS: language row and Dropdown inactive; remaining settings Button references retained. Two static prefab previews rendered. No Play mode or APK build.");
    }

    public static void RemoveLanguageRow(GameObject root)
    {
        var panel = root.GetComponent<PausePanel>();
        panel.languageDropdown.interactable = false;
        panel.languageDropdown.gameObject.SetActive(false);
        foreach (string name in new[] { "ApprovedLanguageRow", "ApprovedLanguageIcon", "ApprovedLanguageLabel" })
        {
            var t = root.transform.Find("BG (1)/" + name);
            if (t != null) t.gameObject.SetActive(false);
        }
        var body = (RectTransform)root.transform.Find("BG (1)");
        if (body.sizeDelta.y < 1000) return; // Already compacted.
        body.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 925);
        foreach (RectTransform child in body)
        {
            if (child.name == "MusicGroup" || child.name == "SoundGroup" || child.name == "LibGroup" || child.anchoredPosition.y > 450)
                child.anchoredPosition += Vector2.down * 99;
            else if (child.name == "GamePauseGroup" || child.name == "VersionLog")
                child.anchoredPosition += Vector2.up * 99;
        }
        var main = (RectTransform)panel.MainPauseGroup.transform;
        main.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 925);
        var mainButton = (RectTransform)panel.MainBackButton.transform;
        mainButton.anchoredPosition += Vector2.up * 99;
    }

    private static void Render(string name, int width, int height)
    {
        var result = OrchardSkinValidation.PreviewPrefab(PrefabPath, Output + name + ".png", root =>
        {
            var panel = root.GetComponent<PausePanel>();
            if (panel.languageDropdown.gameObject.activeSelf || panel.languageDropdown.interactable)
                throw new InvalidOperationException("Language selector is still accessible.");
            foreach (var button in new[] { panel.musicSwitchButton, panel.soundSwitchButton, panel.libSwitchButton, panel.BackButton, panel.ContinueButton, panel.CloseButton, panel.MainBackButton })
                if (button == null) throw new InvalidOperationException("An existing settings button binding was lost.");
            panel.GamePauseGroup.SetActive(true); panel.MainPauseGroup.SetActive(false);
            panel.musicOnIm.gameObject.SetActive(true); panel.musicOffIm.gameObject.SetActive(false);
            panel.soundOnIm.gameObject.SetActive(true); panel.soundOffIm.gameObject.SetActive(false);
            panel.LibOnIm.gameObject.SetActive(true); panel.LibOffIm.gameObject.SetActive(false);
            foreach (var label in root.GetComponentsInChildren<OrchardLocalizedLabel>(true))
            {
                var so = new SerializedObject(label);
                var target = so.FindProperty("target").objectReferenceValue as TMP_Text;
                if (target != null) target.text = so.FindProperty("english").stringValue;
            }
            root.GetComponent<OrchardReferenceLayout>().RefreshLayout();
        }, width, height);
        if (!string.IsNullOrEmpty(result.error)) throw new InvalidOperationException(result.error);
        File.WriteAllText(Output + name + ".json", JsonUtility.ToJson(result, true));
    }
}
