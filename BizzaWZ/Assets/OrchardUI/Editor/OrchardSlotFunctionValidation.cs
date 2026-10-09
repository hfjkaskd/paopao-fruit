#if BIZZA_REAL_WITHDRAW
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Disposable prefab inspection only. Never initializes accounts, saves, or advertisements.
public static class OrchardSlotFunctionValidation
{
    private const string PrefabPath = "Assets/BizzaWZ/Final/Real/UI/SlotsPanel/SlotPanel/SlotPanel.prefab";
    private const string Output = "Design/SlotSpacing-20261008";

    public static void Validate()
    {
        Directory.CreateDirectory(Output);
        if (File.Exists(Output + "/result.txt")) File.Delete(Output + "/result.txt");
        foreach (int height in new[] { 1280, 1600 })
            foreach (string state in new[] { "ad", "free", "reward" })
                RenderVerified(state + "-" + height, state, height);
        var help = OrchardSkinValidation.PreviewPrefab(
            "Assets/BizzaWZ/Final/Real/UI/SlotsPanel/SlotFQAPanel/SlotFQAPanel.prefab", Output + "/help.png", root =>
            {
                root.GetComponent<OrchardReferenceLayout>().RefreshLayout();
                root.GetComponent<OrchardLuckyHelpVisual>().ApplyArtwork(Resources.LoadAll<Sprite>("OrchardUI/LuckyHelpReferenceControls"));
                English(root);
            }, 720, 1280);
        if (!string.IsNullOrEmpty(help.error)) throw new InvalidOperationException(help.error);
        File.WriteAllText(Output + "/result.txt", "PASS: Unity compilation, prefab bindings, 0/1 spin presentation fixtures, reel animation bindings, modal input occlusion and active button raycasts at 720x1280 and 720x1600.\nNo Play mode, live advertisements, accounts, balances, saves or APK build were used. Ad SDK and reward settlement still require a device run.\n");
    }

    private static void English(GameObject root)
    {
        foreach (var local in root.GetComponentsInChildren<OrchardLocalizedLabel>(true))
        {
            var data = new SerializedObject(local);
            var text = data.FindProperty("target").objectReferenceValue as TMP_Text;
            if (text != null) text.text = data.FindProperty("english").stringValue;
        }
    }

    private static void RenderVerified(string name, string state, int height)
    {
        Exception checkFailure = null;
        var result = OrchardSkinValidation.PreviewPrefab(PrefabPath, Output + "/" + name + ".png", root =>
        {
            root.GetComponent<OrchardReferenceLayout>().RefreshLayout();
            English(root);
            var page = root.GetComponent<SlotPanel>();
            bool free = state == "free", reward = state == "reward";
            page.canClickObj.SetActive(free);
            page.notCanClickObj.SetActive(!free);
            page.canClickObj.GetComponent<TMP_Text>().text = "Spin";
            root.transform.Find("Top/IconInfo/IconTxt").GetComponent<TMP_Text>().text = "2";
            root.transform.Find("Top/DollarInfo/DollarTxt").GetComponent<TMP_Text>().text = "$123.71";
            root.transform.Find("Content/ApprovedFreeCount").GetComponent<TMP_Text>().text = free ? "1 FREE SPIN" : "0 FREE SPIN";
            page.slotHintTxt.text = free ? "Your free spin is ready!" : "Watch videos and get spins";
            page.slotRewardPanel.gameObject.SetActive(reward);
            if (reward)
            {
                foreach (var entry in page.slotRewardPanel.slotRewardIcons) entry.image.gameObject.SetActive(false);
                page.slotRewardPanel.slotRewardIcons[5].image.gameObject.SetActive(true);
                page.slotRewardPanel.coinText.text = "+2";
                page.slotRewardPanel.dollarText.text = "+$12.34";
            }
        }, 720, height, root => { try { Check(root, state, name); } catch (Exception error) { checkFailure = error; } });
        File.WriteAllText(Output + "/" + name + ".json", JsonUtility.ToJson(result, true));
        if (!string.IsNullOrEmpty(result.error)) throw new InvalidOperationException(result.error);
        if (checkFailure != null) throw checkFailure;
    }

    private static void Check(GameObject root, string state, string name)
    {
        var page = root.GetComponent<SlotPanel>();
        if (page.slotMachineManager == null || page.slotRewardPanel == null || page.slotMachineManager.slotEntries.Length != 3)
            throw new InvalidOperationException("Lost original spin/reward bindings.");
        if (!page.slotMachineManager.anim.enabled || !page.slotMachineManager.anim.gameObject.activeInHierarchy || page.slotMachineManager.anim.skeletonDataAsset == null)
            throw new InvalidOperationException("Original result-completion animation is unavailable.");
        foreach (var entry in page.slotMachineManager.slotEntries)
        {
            var clip = entry.GetComponent<Animation>().GetClip("SlotEntry");
            if (clip == null || clip.length <= 0 || entry.img1 == null || entry.img2 == null)
                throw new InvalidOperationException("Missing original reel animation.");
            bool next = false, end = false;
            foreach (var e in AnimationUtility.GetAnimationEvents(clip)) { next |= e.functionName == "OnAnimNext"; end |= e.functionName == "OnAnimEnd"; }
            if (!next || !end) throw new InvalidOperationException("Lost reel result animation events.");
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                if (entry.transform.Find(binding.path) == null) throw new InvalidOperationException("Reel animation target missing: " + binding.path);
        }
        foreach (string path in OrchardSlotFunctionAuthoring.RemovedFooterPaths)
            if (root.transform.Find(path).gameObject.activeInHierarchy)
                throw new InvalidOperationException("Non-original footer is still visible: " + path);
        if (root.transform.Find("Content/ApprovedAdSpin").GetComponent<Button>().interactable)
            throw new InvalidOperationException("Removed ad shortcut remains interactable.");
        var claim = page.slotRewardPanel.btnObj.GetComponent<Button>();
        var rewardData = new SerializedObject(page.slotRewardPanel);
        if (rewardData.FindProperty("approvedClaimButton").objectReferenceValue != claim)
            throw new InvalidOperationException("Claim Button binding missing.");
        if (state == "ad" && page.notCanClickObj.GetComponent<Image>().enabled)
            throw new InvalidOperationException("Ad spin still displays the disabled cover.");
        foreach (var b in root.GetComponentsInChildren<Button>(true))
            if (b.onClick.GetPersistentEventCount() != 0 || (b is BizzaButton legacyButton && legacyButton.onClick.GetPersistentEventCount() != 0))
                throw new InvalidOperationException("Inspector event binding remains: " + AnimationUtility.CalculateTransformPath(b.transform, root.transform) + " type=" + b.GetType().Name + " base="+b.onClick.GetPersistentEventCount()+" custom="+(b is BizzaButton custom?custom.onClick.GetPersistentEventCount():0));
        var checkButtons = new[] { page.closeBtn, page.faqBtn, page.slotBtn };
        var canvas = root.GetComponentInParent<Canvas>();
        var camera = canvas.worldCamera;
        var registered = new List<BaseRaycaster>();
        foreach (var c in canvas.GetComponentsInChildren<Canvas>(true))
        {
            var raycaster = c.GetComponent<GraphicRaycaster>() ?? c.gameObject.AddComponent<GraphicRaycaster>();
            // The general static renderer disables MonoBehaviours including pre-existing
            // nested raycasters. Restore them on this disposable input-check clone only.
            raycaster.enabled = true;
            if (RaycasterManager.GetRaycasters().Contains(raycaster)) continue;
            RaycasterManager.GetRaycasters().Add(raycaster); registered.Add(raycaster);
        }
        var eventObject = new GameObject("Disposable slot raycast inspection");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(eventObject, root.scene);
        var previous = EventSystem.current;
        var events = eventObject.AddComponent<EventSystem>(); events.enabled = false;
        var report = new StringBuilder("Static prefab checks; no click dispatch or account initialization.\n");
        try
        {
            camera.Render(); Canvas.ForceUpdateCanvases();
            foreach (var b in checkButtons)
            {
                if (b == null || b.targetGraphic == null || !b.interactable) throw new InvalidOperationException("Missing or disabled Button: " + b);
                var hit = Hit(b, events, camera);
                if (state == "reward")
                {
                    if (hit == null || !hit.transform.IsChildOf(page.slotRewardPanel.transform))
                        throw new InvalidOperationException("Reward modal allows click-through to " + b.name);
                }
                else if (hit == null || ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit) != b.gameObject)
                {
                    var point = RectTransformUtility.WorldToScreenPoint(camera,b.targetGraphic.rectTransform.TransformPoint(b.targetGraphic.rectTransform.rect.center));
                    var diagnostics = new StringBuilder();
                    for (var t=b.transform;t!=null;t=t.parent)
                        foreach (var component in t.GetComponents<Component>())
                            if (component is ICanvasRaycastFilter filter) diagnostics.AppendLine(t.name + "/" + component.GetType().Name + " valid=" + filter.IsRaycastLocationValid(point,camera));
                    diagnostics.AppendLine("registered="+GraphicRegistry.GetRaycastableGraphicsForCanvas(b.targetGraphic.canvas).Contains(b.targetGraphic)+" contains="+RectTransformUtility.RectangleContainsScreenPoint(b.targetGraphic.rectTransform,point,camera)+" world="+b.transform.position+" forward="+b.transform.forward+" view="+camera.ScreenToViewportPoint(point)+" relative="+Display.RelativeMouseAt(point));
                    foreach (var module in RaycasterManager.GetRaycasters())
                    {
                        if (module == null || module.gameObject.scene != root.scene) continue;
                        var raw = new List<RaycastResult>();module.Raycast(new PointerEventData(events){position=point},raw);
                        diagnostics.AppendLine("module="+module.name+" camera="+module.eventCamera+" active="+module.IsActive()+" canvasMatch="+(module.GetComponent<Canvas>()==b.targetGraphic.canvas)+" raw="+raw.Count);
                    }
                    var ray = camera.ScreenPointToRay(point);var forward=b.transform.forward;
                    diagnostics.AppendLine("ray="+ray+" distance="+(Vector3.Dot(forward,b.transform.position-ray.origin)/Vector3.Dot(forward,ray.direction))+" cameraZ="+camera.WorldToScreenPoint(b.transform.position).z);
                    throw new InvalidOperationException("Blocked Button: " + b.name + "; hit=" + (hit == null ? "none" : AnimationUtility.CalculateTransformPath(hit.transform, root.transform)) + "; target=" + AnimationUtility.CalculateTransformPath(b.targetGraphic.transform,root.transform) + "; enabled=" + b.targetGraphic.enabled + "; active=" + b.gameObject.activeInHierarchy + "; raycast=" + b.targetGraphic.raycastTarget + "; depth=" + b.targetGraphic.depth + "; point=" + point + "; padding=" + b.targetGraphic.raycastPadding + "; culled=" + b.targetGraphic.canvasRenderer.cull + "; graphicRaycast=" + b.targetGraphic.Raycast(point,camera) + "\n" + diagnostics);
                }
                report.AppendLine(b.name + ": " + (state == "reward" ? "blocked by reward modal" : "standard Button receives raycast"));
            }
            if (state == "reward" && ExecuteEvents.GetEventHandler<IPointerClickHandler>(Hit(claim, events, camera)) != claim.gameObject)
                throw new InvalidOperationException("Claim Button is blocked.");
            foreach (var tx in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (!tx.isActiveAndEnabled) continue;
                tx.ForceMeshUpdate(true,true);
                if (tx.isTextOverflowing) throw new InvalidOperationException("Text overflow: " + tx.name + " " + tx.text);
            }
            report.AppendLine("PASS: reel references, buttons, text bounds and modal ordering.");
            File.WriteAllText(Output + "/" + name + "-checks.txt", report.ToString());
        }
        finally
        {
            foreach (var raycaster in registered) RaycasterManager.GetRaycasters().Remove(raycaster);
            if (previous != null) EventSystem.current = previous;
            UnityEngine.Object.DestroyImmediate(eventObject);
        }
    }

    private static GameObject Hit(Button button, EventSystem events, Camera camera)
    {
        var rect = button.targetGraphic.rectTransform;
        var pointer = new PointerEventData(events) { position = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center)) };
        var hits = new List<RaycastResult>(); events.RaycastAll(pointer, hits);
        foreach (var hit in hits) if (hit.gameObject.scene == button.gameObject.scene) return hit.gameObject;
        return null;
    }

    public static void Inspect()
    {
        Directory.CreateDirectory(Output);
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var report = new StringBuilder();
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.GetComponent<Image>() == null && t.GetComponent<TMP_Text>() == null && t.GetComponent<Button>() == null) continue;
                string path = AnimationUtility.CalculateTransformPath(t, root.transform);
                var r = t as RectTransform;
                report.AppendLine(path + " active=" + t.gameObject.activeSelf + " position=" + r.anchoredPosition + " size=" + r.sizeDelta + " scale=" + r.localScale);
                foreach (var g in t.GetComponents<Graphic>()) report.AppendLine("  " + g.GetType().Name + " enabled=" + g.enabled + " raycast=" + g.raycastTarget + " color=" + g.color + (g is Image im ? " sprite=" + im.sprite : " text=" + ((TMP_Text)g).text));
                foreach (var b in t.GetComponents<Button>()) report.AppendLine("  Button interactable=" + b.interactable + " listeners=" + b.onClick.GetPersistentEventCount());
            }
            File.WriteAllText(Output + "/hierarchy-before.txt", report.ToString());
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        Render("before-idle", false);
        Render("before-reward", true);
    }

    private static void Render(string name, bool reward)
    {
        var result = OrchardSkinValidation.PreviewPrefab(PrefabPath, Output + "/" + name + ".png", root =>
        {
            root.GetComponent<OrchardReferenceLayout>().RefreshLayout();
            root.transform.Find("Content/GetRewadPanel").gameObject.SetActive(reward);
            if (reward)
            {
                foreach (var text in root.transform.Find("Content/GetRewadPanel").GetComponentsInChildren<TMP_Text>(true)) text.text = text.name == "Title" ? "Reward" : "+12.34";
            }
        }, 720, 1280);
        File.WriteAllText(Output + "/" + name + ".json", JsonUtility.ToJson(result, true));
        if (!string.IsNullOrEmpty(result.error)) throw new InvalidOperationException(result.error);
    }
}
#endif
