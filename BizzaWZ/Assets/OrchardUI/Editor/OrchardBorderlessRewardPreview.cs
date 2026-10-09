using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Disposable visual fixtures only; does not run gameplay or alter saved/account data.
public static class OrchardBorderlessRewardPreview
{
    private const string Prefab = "Assets/BizzaWZ/Final/Real/UI/GetRewardPanel/GetRewardPanel.prefab";
    private const string Output = "Design/RewardCompact-20261008/";

    public static void RenderBatch()
    {
        Render("preview-portrait", 852, 1740, false);
        Render("preview-16x9", 1080, 1920, false);
        Render("preview-single-currency", 1080, 2400, true);
        File.WriteAllText(Output + "preview-result.txt", "Rendered actual prefab with sample values. Static preview only; no gameplay or network requests.");
    }

    private static Transform Find(GameObject root, string suffix)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (AnimationUtility.CalculateTransformPath(child, root.transform).EndsWith(suffix, StringComparison.Ordinal)) return child;
        throw new InvalidOperationException("Missing preview element: " + suffix);
    }

    private static void Render(string name, int width, int height, bool singleCurrency)
    {
        var result = OrchardSkinValidation.PreviewPrefab(Prefab, Output + name + ".png", root =>
        {
            foreach (var label in root.GetComponentsInChildren<OrchardLocalizedLabel>(true))
            {
                var data = new SerializedObject(label);
                var target = data.FindProperty("target").objectReferenceValue as TMP_Text;
                if (target != null) target.text = data.FindProperty("english").stringValue;
            }
#if BIZZA_REAL_WITHDRAW
            var panel = root.GetComponent<GetRewardPanel>();
            panel.itemATxt.text = "+1,000";
            panel.itemBTxt.text = "$22.70";
            panel.levelTxt.text = "Level 2";
            panel.rewardText.text = "Claim ×2";
            panel.noThanksText.text = "$11.35";
            panel.bonusRate.gameObject.SetActive(false);
            var video = root.GetComponentInChildren<WathAdProgress>(true);
            video.gameObject.SetActive(true);
            video.hintText.text = "Watch videos to increase rewards";
            video.startText.text = "+0%";
            video.endText.text = "+5%";
            video.progressText.text = "0 / 10";
            video.progressBar.enabled = false;
            panel.progress.gameObject.SetActive(!singleCurrency);
            panel.progress.progressTxt.text = "Earn $59.24 more to withdraw $800";
            panel.progress.progressImg.fillAmount = .74f;
            Find(root, "Content/Rewards/Reward_2").gameObject.SetActive(!singleCurrency);
            var real = root.GetComponentInChildren<Real_AdWatchProgress>(true);
            real.gameObject.SetActive(singleCurrency);
            real.hintTxt.text = "Watch 31 more videos today to withdraw $7.70";
            real.progressImg.fillAmount = .3f;
#endif
            root.GetComponent<OrchardReferenceLayout>().RefreshLayout();
        }, width, height, contextCapture: "Design/OrchardImplementation-20260928/Runtime/gameplay.png");
        if (!string.IsNullOrEmpty(result.error)) throw new InvalidOperationException(result.error);
        File.WriteAllText(Output + name + ".json", JsonUtility.ToJson(result, true));
    }
}
