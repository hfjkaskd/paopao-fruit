using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Disposable preview of the three existing gameplay prop buttons.</summary>
[InitializeOnLoad]
public static class OrchardPropArtPreview
{
    static OrchardPropArtPreview()
    {
        EditorApplication.update += () =>
        {
            const string command = "Design/PropButtonRefresh-20261007/preview.command";
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(command)) return;
            File.Delete(command);
            Render();
        };
    }
    public static void Render()
    {
        const string output = "Design/PropButtonRefresh-20261007/";
        Directory.CreateDirectory(output);
        var result = OrchardSkinValidation.PreviewPrefab(
            "Assets/FruitsHarvest/Resources/Original/res/local/coreplay/CorePlayUI.prefab",
            output + "buttons-preview.png", Arrange, 900, 300);
        File.WriteAllText(output + "preview-report.json", JsonUtility.ToJson(result, true));
        if (!string.IsNullOrEmpty(result.error) || result.activeGraphics < 10) throw new InvalidOperationException("Prop preview did not render its graphics: " + result.error);
    }

    private static void Arrange(GameObject root)
    {
        PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        var props = root.GetComponentsInChildren<CorePlayItemBtn>(true);
        foreach (Transform child in root.transform) child.gameObject.SetActive(false);
        // This solid backdrop exists only in the isolated preview scene.
        var backdrop = new GameObject("Preview backdrop", typeof(RectTransform), typeof(Image));
        backdrop.transform.SetParent(root.transform, false);
        var backdropRect = (RectTransform)backdrop.transform;
        backdropRect.anchorMin = Vector2.zero;
        backdropRect.anchorMax = Vector2.one;
        backdropRect.offsetMin = backdropRect.offsetMax = Vector2.zero;
        backdrop.GetComponent<Image>().color = new Color32(255, 230, 175, 255);
        foreach (var prop in props)
        {
            int index = prop.name == "Undo" ? 0 : prop.name == "Magic" ? 1 : prop.name == "Shuffle" ? 2 : -1;
            if (index < 0) continue;
            prop.transform.SetParent(root.transform, false);
            prop.gameObject.SetActive(true);
            var rect = (RectTransform)prop.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2(-290 + index * 290, 0);
            rect.localScale = Vector3.one;
            var data = new SerializedObject(prop);
            SetActive(data, "m_LockIcon", index > 0);
            SetActive(data, "m_OnlyUnlockIcon", index == 0);
            SetActive(data, "m_UseDirectly", index == 0);
            SetActive(data, "m_WatchAD", false);
            SetActive(data, "m_UseCoin", false);
            SetActive(data, "m_UseEff", false);
            var count = data.FindProperty("m_LeftItemNum").objectReferenceValue as TMP_Text;
            if (count != null) count.text = "1";
        }
    }

    private static void SetActive(SerializedObject data, string field, bool active)
    {
        var target = data.FindProperty(field).objectReferenceValue as GameObject;
        if (target != null) target.SetActive(active);
    }
}
