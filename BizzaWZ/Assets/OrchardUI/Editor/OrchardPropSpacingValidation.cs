#if BIZZA_REAL_WITHDRAW
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Samples the authored animation on disposable prefab previews, without entering gameplay.
public static class OrchardPropSpacingValidation
{
    const string ClipPath = "Assets/FruitsHarvest/Resources/Original/res/local/coreplay/effct/anim/CoreplayUI_Open.anim";
    const string Output = "Design/PropSpacing-20261008/";
    static readonly string[] Names = { "Undo", "Magic", "Shuffle" };

    public static void Validate()
    {
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
        if (clip == null) throw new InvalidOperationException("Missing gameplay opening animation.");
        foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            foreach (string name in Names)
                if (binding.path == "UI/Bottom/" + name && binding.propertyName.StartsWith("m_LocalScale"))
                    throw new InvalidOperationException("Opening animation still overwrites the adaptive Button scale: " + name);

        Directory.CreateDirectory(Output);
        var report = new StringBuilder("Disposable prefab previews; animation sampled at 120 Hz. No Play mode, ads, account writes or APK build.\n");
        foreach (var size in new[] { new Vector2Int(720, 1600), new Vector2Int(720, 1280), new Vector2Int(720, 1680) })
        {
            OrchardHudRefreshAuthoring.Render("props-" + size.x + "x" + size.y, size.x, size.y, Output,
                core => { SetState(core, "UseDirectly"); clip.SampleAnimation(core, clip.length); HideLegacyTop(core); },
                core => CheckAnimation(core, clip, size, report));
        }
        report.AppendLine("PASS: opening animation, quantity/locked/ad states, screen bounds, and original Button raycast checks.");
        File.WriteAllText(Output + "spacing-checks.txt", report.ToString());
        Debug.Log("Prop spacing validation passed: " + Output);
    }

    static void CheckAnimation(GameObject core, AnimationClip clip, Vector2Int size, StringBuilder report)
    {
        var camera = core.transform.parent.GetComponent<Canvas>().worldCamera;
        var expectedScale = new Vector3[3];
        for (int i = 0; i < Names.Length; i++) expectedScale[i] = Prop(core, Names[i]).localScale;

        // Reconstruct the former binding only on a disposable clip to reproduce the reported overlap.
        var former = UnityEngine.Object.Instantiate(clip);
        try
        {
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                foreach (string name in Names)
                    if (binding.path == "UI/Bottom/" + name + "/AniLayer" && binding.propertyName.StartsWith("m_LocalScale"))
                    {
                        var curve = AnimationUtility.GetEditorCurve(clip, binding);
                        AnimationUtility.SetEditorCurve(former, binding, null);
                        var oldBinding = binding; oldBinding.path = "UI/Bottom/" + name;
                        AnimationUtility.SetEditorCurve(former, oldBinding, curve);
                    }
            former.SampleAnimation(core, former.length);
            float oldGap = MinimumGap(core, camera, size, false);
            report.AppendLine(size + " former end-frame minimum gap: " + oldGap.ToString("F2") + " px");
            if (size.y == 1600 && oldGap >= 0) throw new InvalidOperationException("Could not reproduce the reported long-screen overlap.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(former);
            for (int i = 0; i < Names.Length; i++) Prop(core, Names[i]).localScale = expectedScale[i];
        }

        foreach (string state in new[] { "UseDirectly", "Lock", "AD" })
        {
            SetState(core, state);
            float smallestGap = float.MaxValue;
            int frames = Mathf.CeilToInt(clip.length * 120);
            for (int frame = 1; frame <= frames; frame++)
            {
                clip.SampleAnimation(core, clip.length * frame / frames);
                for (int i = 0; i < Names.Length; i++)
                    if ((Prop(core, Names[i]).localScale - expectedScale[i]).sqrMagnitude > .000001f)
                        throw new InvalidOperationException("Animation overwrote the fitted scale of " + Names[i]);
                smallestGap = Mathf.Min(smallestGap, MinimumGap(core, camera, size, true));
            }
            report.AppendLine(size + " " + state + ": minimum animated gap " + smallestGap.ToString("F2") + " px; adaptive scales preserved.");
        }
        SetState(core, "UseDirectly");
        clip.SampleAnimation(core, clip.length);
        HideLegacyTop(core);
    }

    static float MinimumGap(GameObject core, Camera camera, Vector2Int size, bool enforce)
    {
        float gap = float.MaxValue;
        Rect previous = default;
        for (int i = 0; i < Names.Length; i++)
        {
            var prop = Prop(core, Names[i]);
            Rect bounds = GraphicBounds(prop, camera);
            if (i > 0) gap = Mathf.Min(gap, bounds.xMin - previous.xMax);
            if (enforce && (bounds.xMin < 0 || bounds.yMin < 0 || bounds.xMax > size.x || bounds.yMax > size.y))
                throw new InvalidOperationException("Prop graphic or badge is outside screen: " + Names[i]);
            previous = bounds;
        }
        if (enforce && gap < 8) throw new InvalidOperationException("Prop graphics are too close: " + gap + " px");
        return gap;
    }

    static Rect GraphicBounds(Transform prop, Camera camera)
    {
        Vector2 min = Vector2.positiveInfinity, max = Vector2.negativeInfinity;
        var corners = new Vector3[4];
        foreach (var image in prop.GetComponentsInChildren<Image>(false))
        {
            if (!image.enabled || image.color.a <= 0) continue;
            image.rectTransform.GetWorldCorners(corners);
            foreach (Vector3 corner in corners)
            {
                Vector2 point = RectTransformUtility.WorldToScreenPoint(camera, corner);
                min = Vector2.Min(min, point); max = Vector2.Max(max, point);
            }
        }
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    static Transform Prop(GameObject core, string name) => core.transform.Find("UI/Bottom/" + name);

    static void SetState(GameObject core, string state)
    {
        foreach (string name in Names)
            foreach (Transform child in Prop(core, name).Find("AniLayer/ItemStatus"))
                child.gameObject.SetActive(child.name == state);
    }

    static void HideLegacyTop(GameObject core)
    {
        core.transform.Find("UI/Top").gameObject.SetActive(false);
        core.transform.Find("Game").gameObject.SetActive(false);
    }
}
#endif
