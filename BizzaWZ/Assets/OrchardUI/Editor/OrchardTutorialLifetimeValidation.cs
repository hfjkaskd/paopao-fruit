#if BIZZA_REAL_WITHDRAW
using System;
using System.IO;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Isolated UI lifecycle regression: real module + production tip prefab, no account,
// gameplay, advertisements, withdrawals or saved-profile initialization.
[InitializeOnLoad]
public static class OrchardTutorialLifetimeValidation
{
    private const string Pending = "OrchardTutorialLifetimeValidation.Pending";
    private const string Output = "Design/TutorialLifetime-20261023/result.txt";
    private const string Prefab = "Assets/BizzaWZ/Final/Framework/Runtime/Module/Teach/UITeach/UITeachTipsPage.prefab";

    static OrchardTutorialLifetimeValidation()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false))
            {
                SessionState.SetBool(Pending, false);
                Validate().Forget();
            }
        };
    }

    public static void Run()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Output));
        SessionState.SetBool(Pending, true);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorApplication.isPlaying = true;
    }

    private static async UniTask Validate()
    {
        EditorApplication.LockReloadAssemblies();
        var report = new StringBuilder();
        GameObject fixture = null;
        int exitCode = 1;
        try
        {
            await TableUtils.LoadTableConfigs();
            fixture = new GameObject("TutorialLifetimeFixture", typeof(RectTransform), typeof(Canvas));
            var root = (RectTransform)fixture.transform;
            root.sizeDelta = new Vector2(1080, 2360);
            foreach (string name in new[] { "BaseLayer", "PopupLayer", "TopLayer", "SystemLayer", "TeachLayer" })
            {
                var layer = new GameObject(name, typeof(RectTransform));
                layer.transform.SetParent(root, false);
                ((RectTransform)layer.transform).sizeDelta = root.sizeDelta;
            }
            var module = fixture.AddComponent<UIModule>();
            var settings = new SerializedObject(module);
            settings.FindProperty("m_pageRoot").objectReferenceValue = root;
            settings.ApplyModifiedPropertiesWithoutUndo();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefab).GetComponent<UITeachTipsPage>();
            module.SetPageLoader(new TipLoader(prefab));
            module.InitGameModule();

            var target = CreateTarget(root);
            var tip = await OpenTip(module, target);
            Require(tip.panel.gameObject.activeSelf, "Targeted tip should appear above the active button.");
            await UniTask.DelayFrame(2);
            Require(module.GetPage<UITeachTipsPage>() == tip, "An untouched active target should keep its guide.");
            target.GetComponent<Button>().onClick.Invoke();
            Require(module.GetPage<UITeachTipsPage>() == null && !tip.panel.gameObject.activeSelf,
                "Clicking the target must immediately remove the tip.");
            report.AppendLine("PASS: target click closes tip immediately; ordinary active guide remains visible.");

            tip = await OpenTip(module, target);
            target.SetActive(false);
            await UniTask.DelayFrame(2);
            Require(module.GetPage<UITeachTipsPage>() == null, "Hidden page/button must not leave a gameplay overlay.");
            target.SetActive(true);
            report.AppendLine("PASS: target/page hidden -> tip removed on the next update.");

            tip = await OpenTip(module, target);
            UnityEngine.Object.Destroy(target);
            await UniTask.DelayFrame(2);
            Require(module.GetPage<UITeachTipsPage>() == null, "Destroyed target must dismiss its tip.");
            report.AppendLine("PASS: destroyed target -> tip removed.");

            target = CreateTarget(root);
            tip = await OpenTip(module, target);
            module.ClosePage(tip);
            // A later guide must not be closed by a listener left on the former target.
            var replacement = CreateTarget(root, "ReplacementTarget");
            var next = await OpenTip(module, replacement);
            target.GetComponent<Button>().onClick.Invoke();
            Require(module.GetPage<UITeachTipsPage>() == next, "Closed tip left a listener on its old target.");
            replacement.GetComponent<Button>().onClick.Invoke();
            Require(module.GetPage<UITeachTipsPage>() == null, "Replacement target should dismiss its own guide.");
            report.AppendLine("PASS: repeated open/close does not retain old button listeners.");

            var missing = new UITeachTipsPage.InitParam
            {
                content = "Teach_Withdraw", posIdx = -1, heightValue = .2f,
                targetPath = "TutorialLifetimeFixture/TargetThatDoesNotExist"
            };
            tip = await module.OpenPage(UIPageIds.UI_TeachTip, missing) as UITeachTipsPage;
            Require(!tip.panel.gameObject.activeSelf, "Missing target must never display a fixed-position fallback.");
            await UniTask.Delay(1800);
            Require(module.GetPage<UITeachTipsPage>() == null, "Missing target lookup must close its guide.");
            report.AppendLine("PASS: missing target stays invisible and closes after the bounded lookup.");

            // A non-RectTransform target is rejected synchronously inside OnOpen. This
            // reproduces the race where polling GetPage never observes the opened tip.
            var invalidTarget = new GameObject("InvalidTutorialTarget");
            invalidTarget.transform.SetParent(root, false);
            var action = new TipActionProbe();
            action.content = new Variable_String_Direct { directValue = "Teach_Withdraw" };
            action.block = new Variable_Bool_Direct { directValue = false };
            action.targetPath = new Variable_String_Direct { directValue = "TutorialLifetimeFixture/InvalidTutorialTarget" };
            action.Begin();
            await UniTask.DelayFrame(1);
            Require(action.Poll() == E_ExecuteState.Success, "Immediately dismissed non-blocking tip stalled its action graph.");
            action.block = new Variable_Bool_Direct { directValue = true };
            action.Begin();
            await UniTask.DelayFrame(1);
            Require(action.Poll() == E_ExecuteState.Success, "Immediately dismissed blocking tip stalled its action graph.");
            report.AppendLine("PASS: both blocking and non-blocking actions complete after an immediate target dismissal.");

            tip = await module.OpenPage(UIPageIds.UI_TeachTip,
                new UITeachTipsPage.InitParam { content = "Teach_coin", posIdx = -1, heightValue = .5f }) as UITeachTipsPage;
            await UniTask.DelayFrame(2);
            Require(module.GetPage<UITeachTipsPage>() == tip && tip.panel.gameObject.activeSelf,
                "Untargeted tutorial tips must retain their original lifecycle.");
            module.ClosePage(tip);
            report.AppendLine("PASS: untargeted tips retain the original lifecycle.");
            report.AppendLine("Unity Play Mode; actual production prefab and UI module. No APK/AAB built or device package changed.");
            exitCode = 0;
        }
        catch (Exception error)
        {
            report.AppendLine("FAIL: " + error);
            Debug.LogException(error);
        }
        finally
        {
            if (fixture != null) UnityEngine.Object.Destroy(fixture);
            File.WriteAllText(Output, report.ToString());
            Debug.Log(report.ToString());
            EditorApplication.UnlockReloadAssemblies();
            EditorApplication.Exit(exitCode);
        }
    }

    private static GameObject CreateTarget(Transform root, string name = "TargetButton")
    {
        var target = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        target.transform.SetParent(root, false);
        ((RectTransform)target.transform).sizeDelta = new Vector2(600, 160);
        return target;
    }

    private static async UniTask<UITeachTipsPage> OpenTip(UIModule module, GameObject target)
    {
        return await module.OpenPage(UIPageIds.UI_TeachTip, new UITeachTipsPage.InitParam
        {
            content = "Teach_Withdraw", posIdx = -1, heightValue = .2f,
            targetPath = "TutorialLifetimeFixture/" + target.name
        }) as UITeachTipsPage;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class TipLoader : IUIPageLoader
    {
        private readonly UIPageBase prefab;
        public TipLoader(UIPageBase prefab) { this.prefab = prefab; }
        public UniTask<UIPageBase> LoadPagePrefabAsync(PageId pageId) => UniTask.FromResult(prefab);
        public void ReleasePagePrefab(PageId pageId) { }
        public void ReleaseAllPagePrefabs() { }
    }

    private sealed class TipActionProbe : Action_Teach_ShowTip
    {
        public void Begin() { OnEnter(default); }
        public E_ExecuteState Poll() => OnExecute(default);
    }
}
#endif
