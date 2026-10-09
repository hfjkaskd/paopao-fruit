using System;
using System.Collections.Generic;
using Bizza;
using Bizza.GameAnalytics;
using Bizza.Loading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 游戏加载阶段
/// 加载数据表=>加载主场景
/// </summary>
public class GameMode_Loading : GameModeBase
{
        public override E_GameModeType GameModeType => E_GameModeType.Loading;

        #region Loading UI

        private float progress;
        [LabelText("加载速度")]
        [Range(0f, 1f)]
        public float progressSpeed = 0.2f;

        private LoadingProcedure _loadingProcedure;
        protected override void OnInit()
        {
                base.OnInit();
                _loadingProcedure = new LoadingProcedure();

                GameExtensionRegistry.RunLoadingProcedureHooks(_loadingProcedure);

#if BIZZA_REAL_WITHDRAW
                _loadingProcedure.AddTask(new WKY_SDKTask()); // SDK流程
#endif      
#if BIZZA_IPINTERCEPT && !DEBUG_MODE
                _loadingProcedure.AddTask(new IPCheckTask()); // IP拦截
#endif
#if BIZZA_REMOTEGROUPDATA
                _loadingProcedure.AddTask(new InitRemoteGroupDataTask()); // 远程配置拉取
#endif
                // _loadingProcedure.AddTask(new LoadGameResTask()); // 加载游戏资源 表格资源
                _loadingProcedure.AddTask(new PreLoadAssetsTask()); // 预加载资源
                _loadingProcedure.AddTask(new LoadGameDataTask()); // 加载游戏数据
                RunLoadingSafe().Forget();
                UIModule.Instance.OpenPage(UIPageIds.LoadingPanel).Forget();
        }

        private void OnLoadingFinish()
        {
                LogLogger.LogInfo("OnLoadingFinish");
#if BIZZA_REAL_WITHDRAW
                var account = AccountModule.Instance;
                if (account?.Os_Current_Uso != null)
                {
                SaveDataUtils.GameData.playerSelectedLv =
                        account.Os_Current_Uso.Os_Rts;
                }
#endif
                StartGame();
        }

        private async UniTaskVoid RunLoadingSafe()
        {
                try
                {
                        await UniTask.DelayFrame(1);
                        await _loadingProcedure.Run(OnLoadingFinish);
                }
                catch (Exception e)
                {
                        LogLogger.LogError($"加载流程失败: {e}");
                        var args = new CommonConfirmTipsPanel.Args
                        {
                                isLanguage = true,
                                des = "HTTPNetworkProblem",
                                onFinish = Application.Quit,
                        };
                        await SDKAssetHandler.OpenCommonConfirmTipsPanel(args);
                }
        }

        public void StartGame()
        {
                bool teachEnable = GameExtensionRegistry.ShouldEnableTeach();

                BizzaGameAnalytics.TrackGameLoadComplete();
                LogLogger.LogInfo($"ReadyChangeScene {teachEnable}");
		// Keep the page visible while GamePlay creates its UI and board.
		progress = 0.95f;
		BizzaEventSystem.Emit(EventDefine.Frame.LoadingProgress, progress);
		TransitionBlock.ToGamePlay();
                BizzaEventSystem.Emit(EventDefine.Frame.LoadingStageComplete);

                if (SoundManager.Instance != null && SaveDataUtils.SettingData != null)
                {
                        SoundManager.Instance.MuteBGM(!SaveDataUtils.SettingData.enableMusic);
                }
                GameUtils.DelayDo(() =>
                {
                        SoundManager.Instance?.PlayBGM("SFX_BGM");
                }, 1f).Forget();
        }

        private void Update()
        {
                progress += Time.deltaTime * progressSpeed;
                float min = 0;
                float max = _loadingProcedure != null ? Mathf.Min(_loadingProcedure.progress, 0.95f) : 0;
                progress = Mathf.Clamp(progress, min, max);
                BizzaEventSystem.Emit(EventDefine.Frame.LoadingProgress, progress);
        }

        #endregion
}
