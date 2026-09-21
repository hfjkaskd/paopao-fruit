using UnityEngine;

public sealed class HarvestRoot : MonoBehaviour
{
    [SerializeField] private MgrUI manager;
    [SerializeField] private MgrGlobalUI global;
    [SerializeField] private CanvasGroup gameplayInput;
    private float settingsCheck;
    private void Start() => HarvestBridge.Initialize(manager, global);
    private void Update()
    {
        // Teach_01 disables framework UI while the original UGUI board teaches gameplay.
        // Only the board has an independent input boundary; its real readiness and masks still apply.
        bool tutorial=SaveDataUtils.GameData!=null && !SaveDataUtils.GameData.customTutorialEnd;
        bool acceptsInput=HarvestBridge.Ready && !TransparentBlock.IsBlock;
        if(gameplayInput.ignoreParentGroups!=tutorial) gameplayInput.ignoreParentGroups=tutorial;
        if(gameplayInput.interactable!=acceptsInput) gameplayInput.interactable=acceptsInput;
        settingsCheck -= Time.unscaledDeltaTime;
        if (settingsCheck > 0f || SaveDataUtils.SettingData == null) return;
        settingsCheck = 0.25f;
        GameAudio.SetMusicEnabled(SaveDataUtils.SettingData.enableMusic);
        GameAudio.SetSfxEnabled(SaveDataUtils.SettingData.enableSound);
    }
}
