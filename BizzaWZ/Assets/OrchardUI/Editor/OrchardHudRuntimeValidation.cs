#if BIZZA_REAL_WITHDRAW
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Explicit verification through the formal startup and actual HUD click handlers.</summary>
[InitializeOnLoad]
public static class OrchardHudRuntimeValidation
{
    private const string Key = "OrchardHudRuntimeValidation.State";
    private const string ScenePath = "Assets/Game/Resources/Scenes/InitWZ.unity";
    private static readonly string[] Fields = { "coinBtn", "dollarBtn", "settingBtn" };
    private static readonly PageId[] Pages = { UIPageIds.RealWithdrawPanel, UIPageIds.FakeWithdrawPanel, UIPageIds.PausePanel };
    private static State state;
    private static double nextTick;
    private static double nextReport;
    private static readonly string CommandPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../Design/OrchardUI/hud-runtime.command"));

    [Serializable]
    private sealed class State
    {
        public string startedUtc;
        public string status = "Waiting for formal startup";
        public string waitReason;
        public string error;
        public string scope = "Formal InitWZ startup; actual EventSystem hit and standard pointer events for HUD navigation only. No withdrawal submission, consent acceptance, tutorial bypass, or direct balance/save edits.";
        public bool playObserved;
        public bool uniTaskPlayerLoopInjected;
        public bool editorPaused;
        public bool applicationFocused;
        public int frameCount;
        public bool finished;
        public bool passed;
        public int index;
        public int phase;
        public double deadline;
        public double earliestNextClick;
        public List<Result> buttons = new List<Result>();
    }

    [Serializable]
    private sealed class Result
    {
        public string field;
        public string expectedPage;
        public string visual;
        public Vector2 screenPoint;
        public string topHit;
        public string handler;
        public bool correctHandler;
        public bool pageOpened;
        public bool pageClosed;
    }

    static OrchardHudRuntimeValidation() { EditorApplication.update += Tick; }

    // Launch without -quit: this runner exits only after Play mode has stopped.
    public static void Validate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Run this entry in a fresh Unity process without -quit.");
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        state = new State { startedUtc = DateTime.UtcNow.ToString("o"), deadline = EditorApplication.timeSinceStartup + 240d };
        Save();
        EditorApplication.EnterPlaymode();
    }

    private static void Tick()
    {
        if (state == null)
        {
            string saved = SessionState.GetString(Key, string.Empty);
            if (string.IsNullOrEmpty(saved))
            {
                if (EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(CommandPath)) return;
                string command = File.ReadAllText(CommandPath).Trim();
                File.Delete(CommandPath);
                if (command == "validate") Validate();
                return;
            }
            state = JsonUtility.FromJson<State>(saved);
        }
        if (EditorApplication.timeSinceStartup < nextTick) return;
        nextTick = EditorApplication.timeSinceStartup + 0.2d;
        try
        {
            if (state.finished)
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) { EditorApplication.ExitPlaymode(); return; }
                Save();
                SessionState.EraseString(Key);
                EditorApplication.Exit(state.passed ? 0 : 1);
                return;
            }
            if (EditorApplication.timeSinceStartup > state.deadline)
                throw new TimeoutException(state.status + ": " + state.waitReason);
            if (!EditorApplication.isPlaying)
            {
                if (state.playObserved) throw new InvalidOperationException("Play mode ended before verification completed.");
                return;
            }
            if (!state.playObserved) { state.playObserved = true; Save(); }
            EditorApplication.QueuePlayerLoopUpdate();
            if (EditorApplication.timeSinceStartup >= nextReport)
            {
                nextReport = EditorApplication.timeSinceStartup + 2d;
                state.uniTaskPlayerLoopInjected = Cysharp.Threading.Tasks.PlayerLoopHelper.IsInjectedUniTaskPlayerLoop();
                state.editorPaused = EditorApplication.isPaused;
                state.applicationFocused = Application.isFocused;
                state.frameCount = Time.frameCount;
                Save();
            }
            if (!HarvestBridge.Ready || UIModule.Instance == null) { state.waitReason = "HarvestBridge.Ready or UIModule not ready"; return; }
            PageId pageId = Pages[state.index];
            if (state.phase == 1)
            {
                UIPageBase opened = UIModule.Instance.GetPage(pageId);
                if (opened == null || !opened.gameObject.activeInHierarchy || UIModule.Instance.Opening) { state.waitReason = "Expected page has not become active: " + pageId.Value; return; }
                state.buttons[state.index].pageOpened = true;
                UIModule.Instance.ClosePage(pageId);
                state.phase = 2;
                state.status = "Waiting for page close: " + pageId.Value;
                state.deadline = EditorApplication.timeSinceStartup + 30d;
                state.earliestNextClick = EditorApplication.timeSinceStartup + 1.5d;
                Save();
                return;
            }
            if (state.phase == 2)
            {
                if (UIModule.Instance.PageIsOpen(pageId) || EditorApplication.timeSinceStartup < state.earliestNextClick) return;
                state.buttons[state.index].pageClosed = true;
                if (++state.index == Pages.Length) { Finish(true, string.Empty); return; }
                state.phase = 0;
                state.deadline = EditorApplication.timeSinceStartup + 30d;
                Save();
                return;
            }
            if (TransparentBlock.IsBlock || !SaveDataUtils.GameData.customTutorialEnd || !SaveDataUtils.TeachData.IsCompleted("Teach_01") ||
                (TeachModule.Instance != null && TeachModule.Instance.RunningGraphCount > 0) || UIModule.Instance.HasPopup || UIModule.Instance.Opening)
            {
                state.waitReason = "Normal UI is blocked, a tutorial is incomplete, or another popup is open; no bypass attempted";
                return;
            }
            ClickCurrent();
        }
        catch (Exception exception) { Finish(false, exception.ToString()); }
    }

    private static void ClickCurrent()
    {
        var bar = UnityEngine.Object.FindObjectOfType<CurrencyBar>();
        if (bar == null || EventSystem.current == null) { state.waitReason = "No active CurrencyBar or EventSystem"; return; }
        Button button = state.index == 0 ? bar.coinBtn : state.index == 1 ? bar.dollarBtn : bar.settingBtn;
        if (button == null || !button.isActiveAndEnabled || !button.IsInteractable()) throw new InvalidOperationException(Fields[state.index] + " is not interactable.");
        Graphic visual = button.targetGraphic;
        if (state.index < 2)
        {
            var group = bar.transform.Find(state.index == 0 ? "CurrentGroup/GoldGroup" : "CurrentGroup/DollarGroup");
            visual = null;
            if (group != null) foreach (var candidate in group.GetComponentsInChildren<Graphic>()) if (candidate.name == "ButtonView") { visual = candidate; break; }
        }
        if (visual == null) throw new InvalidOperationException("Authored button visual is missing.");
        Canvas canvas = visual.canvas.rootCanvas;
        var point = RectTransformUtility.WorldToScreenPoint(canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, visual.rectTransform.TransformPoint(visual.rectTransform.rect.center));
        var pointer = new PointerEventData(EventSystem.current) { position = point, button = PointerEventData.InputButton.Left, eligibleForClick = true };
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, hits);
        GameObject hit = hits.Count > 0 ? hits[0].gameObject : null;
        GameObject handler = hit != null ? ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit) : null;
        var result = new Result { field = Fields[state.index], expectedPage = Pages[state.index].Value, visual = PathOf(visual.transform), screenPoint = point,
            topHit = hit != null ? PathOf(hit.transform) : "none", handler = handler != null ? PathOf(handler.transform) : "none", correctHandler = handler == button.gameObject };
        state.buttons.Add(result);
        Save();
        if (!result.correctHandler) throw new InvalidOperationException(result.field + " visible center is intercepted by " + result.topHit + ", handler " + result.handler);
        if (UIModule.Instance.PageIsOpen(Pages[state.index])) throw new InvalidOperationException("Expected page was already open before its HUD click.");
        pointer.pointerCurrentRaycast = hits[0];
        pointer.pointerPressRaycast = hits[0];
        pointer.pointerPress = handler;
        pointer.pressPosition = point;
        ExecuteEvents.Execute(handler, pointer, ExecuteEvents.pointerDownHandler);
        ExecuteEvents.Execute(handler, pointer, ExecuteEvents.pointerUpHandler);
        ExecuteEvents.Execute(handler, pointer, ExecuteEvents.pointerClickHandler);
        state.phase = 1;
        state.status = "Waiting for page open: " + result.expectedPage;
        state.waitReason = "Click dispatched through actual standard Button handler";
        state.deadline = EditorApplication.timeSinceStartup + 30d;
        Save();
    }

    private static void Finish(bool passed, string error)
    {
        state.finished = true; state.passed = passed; state.error = error; state.status = passed ? "Passed" : "Failed or blocked";
        Save();
        if (EditorApplication.isPlayingOrWillChangePlaymode) EditorApplication.ExitPlaymode();
    }

    private static void Save()
    {
        string json = JsonUtility.ToJson(state, true);
        SessionState.SetString(Key, json);
        string path = Path.GetFullPath(Path.Combine(Application.dataPath, "../Design/OrchardUI/hud-runtime-validation.json"));
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, json);
    }

    private static string PathOf(Transform item)
    {
        string path = item.name;
        while (item.parent != null) { item = item.parent; path = item.name + "/" + path; }
        return path;
    }
}
#endif
