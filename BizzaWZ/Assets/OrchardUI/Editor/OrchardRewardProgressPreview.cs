using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Renders disposable prefab copies at boundary values without changing account or game state.
[InitializeOnLoad]
public static class OrchardRewardProgressPreview
{
    private const string DirectoryPath = "Design/OrchardUI/RewardProgressChecks/";
    private const string CommandPath = "Design/OrchardUI/reward-progress.command";
    private const string PrefabPath = "Assets/BizzaWZ/Final/Real/UI/GetRewardPanel/GetRewardPanel.prefab";
    private static double nextPoll;

    static OrchardRewardProgressPreview() { EditorApplication.update += Poll; }

    private static void Poll()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling ||
            EditorApplication.isUpdating || EditorApplication.timeSinceStartup < nextPoll) return;
        nextPoll = EditorApplication.timeSinceStartup + 1;
        if (!File.Exists(CommandPath)) return;
        File.Delete(CommandPath);
        try { Render(); }
        catch (Exception exception)
        {
            Directory.CreateDirectory(DirectoryPath);
            File.WriteAllText(DirectoryPath + "result.txt", exception.ToString());
            Debug.LogException(exception);
        }
    }

    [MenuItem("Tools/Orchard UI/Preview Reward Progress Boundaries")]
    public static void Render()
    {
        Directory.CreateDirectory(DirectoryPath);
        foreach (int percent in new[] { 0, 10, 50, 100 })
        {
            float value = percent / 100f;
            var result = OrchardSkinValidation.PreviewPrefab(PrefabPath,
                DirectoryPath + "reward-" + percent + ".png", root =>
                {
                    var middle = Find(root, "Content/WathAdProgress/progress/bg/FillArea/Fill").GetComponent<Image>();
                    var anchor = middle.rectTransform.anchorMax;
                    anchor.x = value;
                    middle.rectTransform.anchorMax = anchor;
                    middle.enabled = value > 0;
                    if (percent == 10)
                        File.WriteAllText(DirectoryPath + "fill-geometry.txt",
                            "Sprite=" + middle.sprite.name + ", rect=" + middle.sprite.rect +
                            ", spritePPU=" + middle.sprite.pixelsPerUnit + ", imagePPU=" + middle.pixelsPerUnit +
                            ", multiplier=" + middle.pixelsPerUnitMultiplier + ", border=" + middle.sprite.border +
                            ", fillArea=" + ((RectTransform)middle.transform.parent).rect +
                            ", fillRect=" + middle.rectTransform.rect);
                    Find(root, "Content/WathAdProgress").gameObject.SetActive(true);
                    Find(root, "Content/Fake_WithdrawProgress").gameObject.SetActive(true);
                    Find(root, "Content/Real_WithdrawProgress").gameObject.SetActive(false);
                    Find(root, "Content/Fake_WithdrawProgress/progress/Image").GetComponent<Image>().fillAmount = value;
                    foreach (var text in Find(root, "Content/WathAdProgress").GetComponentsInChildren<TMPro.TMP_Text>(true))
                        if (text.name == "ProgressValue") text.text = (percent / 10) + "/10";
                });
            if (!string.IsNullOrEmpty(result.error)) throw new InvalidOperationException(result.error);
        }
        var realResult = OrchardSkinValidation.PreviewPrefab(PrefabPath,
            DirectoryPath + "reward-real-10.png", root =>
            {
                Find(root, "Content/Fake_WithdrawProgress").gameObject.SetActive(false);
                Find(root, "Content/Real_WithdrawProgress").gameObject.SetActive(true);
                Find(root, "Content/Real_WithdrawProgress/progress/Image").GetComponent<Image>().fillAmount = .1f;
            });
        if (!string.IsNullOrEmpty(realResult.error)) throw new InvalidOperationException(realResult.error);
        File.WriteAllText(DirectoryPath + "result.txt", "PASS: rendered 0%, 10%, 50%, 100% and nested real progress at 10%; source prefabs and save data were not modified.");
    }
    private static Transform Find(GameObject root, string suffix)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (AnimationUtility.CalculateTransformPath(child, root.transform).EndsWith(suffix, StringComparison.Ordinal)) return child;
        throw new InvalidOperationException("Missing preview element: " + suffix);
    }

}
