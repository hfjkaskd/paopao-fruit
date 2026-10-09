# 界面清单与当前分支说明

整理日期：2026-09-27。只读检查当前唯一工程 `C:/Projects/paopao/BizzaWZ`，未运行或修改历史工程，未使用 Tools 历史操作说明。

本清单为视觉设计依据，区分已接入链路、地区/配置分支与保留未启用页面；不是新增业务建议。金额、进度、在线时长示例的实际运行值仍由现有配置和服务端决定。

## 已确认入口与当前配置

- 启动遵循 `Assets/Game/Resources/Scenes/InitWZ.unity` -> 框架初始化/账号 -> Loading -> RealGamePanel。正式游戏页创建 HarvestBridge.Attach，并加载 `Resources/GameUiWidget`。不另造首页、商城、关卡地图或排行榜。
- Android、Standalone、iPhone 的 ProjectSettings 均含 `BIZZA_REAL_WITHDRAW`。
- `Assets/BizzaWZ/Final/BizzaGame/Resources/Bizza/ChannelConfig/ChannelConfig/Oversea.asset` 中 `singleCurrencyMode: 1`。Editor 配置资产为 0，但 ChannelConfigTable 的 `editorChannel: Android`；应按当前通道实际资产决定，不能以 Editor 资产推断运行行为。
- 金币提现 `RealWithdrawPanel` 是主视觉锚点。框架 `FakeWithdrawPanel` 仍是真实业务代码中的现金分支，不是历史 TEST；但单货币入口会被相关显示组件隐藏。
- `BadgeShow` 在单货币且非 BR 地区时隐藏段位入口。ItemForCountry 的地区分支为 US/ID -> WithdrawDanPanel，BR -> DailyMissionPanel。
- 正式通关走 `FlowModule.OpenGameWinPanel -> Real_GetRewardPanelUtil -> GetRewardPanel`。WhiteWinPanel / RecoveredVictoryPanel 属非 BIZZA_REAL_WITHDRAW 分支；其残留 "All dragons escaped" 不应进入果园视觉稿。
- `GameUiWidget.prefab` 的 `TaskButton` 是 `m_IsActive: 0`；`UIDailyTaskPage.enableTab: 0` 且 `OnTabClick(int)` 为空。任务页只作为现有框架附录，不标注已启用，不增加每日/每周/在线三标签。
- CommonConfirmTipsPanel 通过 Addressables 的 `SDKPanel/CommonConfirmTipsPanel` 打开，覆盖网络、账号等提示；是一个标题、正文和单 Confirm 按钮的共用样式。

## 核心游戏及通用界面

以下源路径以 `C:/Projects/paopao/BizzaWZ/` 为根。

| 视觉画面 | 真实结构及交互 | 源路径与链路 | 交付合并原则 |
|---|---|---|---|
| Loading | 果园主视觉、加载进度、百分比 | Assets/BizzaWZ/Common/MenuSystem/Common/LoadingPanel/LoadingPanel.prefab / .cs；GameMode_Loading | 启动/关卡加载共享，不另出广告加载页 |
| 游戏 HUD | 原果树水果玩法、木质收集槽、撤销/魔棒/洗牌、框架金币/提现/设置、抽奖入口 | Assets/BizzaWZ/Common/UI/GamePanel/RealGamePanel.prefab；Assets/FruitsHarvest/Resources/HarvestRoot.prefab；Assets/BizzaWZ/Final/Real/UI/GamePanel/Resources/Resources/GameUiWidget.prefab | 不改方格三消，不新建菜单 |
| 新手教学 | 对现有玩法与功能的遮罩、指示手、提示 | Assets/BizzaWZ/Final/Framework/Runtime/Module/Teach/UITeach/；Final/BizzaGame/Resources/Actions/Teach_01.asset | 作为 HUD 叠加状态，不当独立业务页 |
| 通关/获得奖励 | 当前关卡、金币/奖励数、额外视频领取、普通领取或 Next | Assets/BizzaWZ/Final/Real/UI/GetRewardPanel/GetRewardPanel.prefab / .cs；FlowModule 正式分支 | WinPanel、DailyTask、Bubble 等使用场景合并同系统；奖励额外数受货币配置影响 |
| 失败与复活 | 有复活次数：Revive 广告、Retry；无次数：Retry | Assets/BizzaWZ/Common/BizzaGame/LosePanel/LosePanel.prefab / .cs | 两种状态可独立展现，源为同一个 Prefab |
| 设置/暂停 | 音乐、音效、振动开关、语言选择、Continue、Restart、关闭 | Assets/BizzaWZ/Common/UI/SettingPanel/PausePanel.prefab；Common/BizzaGame/Z_ReplaceAssets/PausePanel.cs | 主菜单设置分组和游戏暂停共用，不额外构造主菜单 |
| 道具补充 | 道具图标与名称、次数限制、Free 激励视频获取、关闭 | Assets/BizzaWZ/Final/Real/UI/AddPropPanel/AddPropPanel.prefab / .cs | 不同道具换图标/名称即可，不分别当新页面 |
| 新道具解锁 | 原玩法 NewItemPop | Assets/FruitsHarvest/Resources/Original/res/local/pops/newitempop/NewItemPop.prefab；FruitsHarvest/Scripts/NewItemPop.cs | 具体状态以原玩法清单为准 |
| Daily Tasks（附录） | 五种在线任务，进度条，Claim/Go/Claimed | Assets/BizzaWZ/Final/MenuSystem/Common/Task/UIDailyTaskPage.prefab；对应 Src/UI；Assets/Game/Resources/ConfigAssets/TableBin/Table01/tbldailytaskconfig.bytes | 10/20/40/60/90 分钟；当前未启用，无新增任务 |
| Rating | 5 个可选星级与 Confirm | Assets/BizzaWZ/Final/Real/UI/StarRatingPopup/StarRatingPopup.prefab / .cs；UIWithdrawalConfirmPanel 条件打开 | 选中/未选中是一个页面状态，不诱导评分换奖励 |
| 通用提示 | Notification 标题、错误/信息正文、Confirm | Assets/BizzaWZ/Final/FunctionTools/CommonPublic/UI/CommonConfirmTipsPanel.prefab / .cs | 网络失败、地区/账号说明等更换正文，统一样式 |

## 提现及账户链路

| 视觉画面 | 真实结构及交互 | 源路径与链路 | 状态合并 |
|---|---|---|---|
| 金币提现 | 金币余额、兑换金额、支付方式、等级兑换档、等级进度、Withdraw、History、FAQ、客服 | Assets/BizzaWZ/Final/Real/UI/RealWithdrawalPanl/RealWithdrawPanel.prefab / .cs；CurrencyBar.coinBtn | 用户参考图为视觉锚点；档位锁定/余额不足/已满足是同页状态 |
| 提现条件提示 | 最低金额差额或未到等级 | RealWithdrawalPanl/WithdrawHintPanel.cs，内嵌在提现 Prefab | 通用小提示/锁定状态可合并，不新设业务页 |
| 现金提现（配置分支） | Cash Balance、Select Amount、进度、Withdraw、History、FAQ | Final/Real/UI/FakeWithdrawPanel/FakeWithdrawPanel.prefab / .cs；CurrencyBar.dollarBtn | 单货币分支隐藏入口，仍为真实框架业务；绝非 TEST |
| 段位提现（地区/配置分支） | 当前段位、段位卡片、已过关目标、奖励金额、提现进度和按钮 | Final/Real/UI/WithdrawDanPanel/WithdrawDanPanel.prefab / .cs；ItemForCountry US/ID | 已领、可领、未达成统一在卡片里表现 |
| 每日提现任务（BR分支） | 观看视频任务、进度、刷新倒计时、Go/Withdraw/Claimed | Final/Real/UI/DailyMissionPanel/DailyMissionPanel.prefab / .cs；ItemForCountry BR | 与普通 Daily Tasks 不混淆；3 状态共一页 |
| 账户填写 | 提现金额、支付平台、按国家呈现收款字段、校验信息、继续 | Final/Real/UI/WithdrawFillPanel/WithdrawFillPanel.prefab；UIWithdrawalPanel.cs | US: PayPal 邮箱；BR: PIX/PagBank 姓名/CPF/类型/账户；ID: OVO/Dana 账号/姓名。地区表单可用附录状态 |
| 账户确认 | 金额、平台、账户/姓名摘要、Submit/确认 | Final/Real/UI/UIWithdrawalConfirmPanel/UIWithdrawalConfirmPanel.prefab；WithdrawFillPanel/UIWithdrawalConfirmPanel.cs | 是提交前复核，不当成功页 |
| 提交处理中/结果 | 0/20 进度、请求提示、服务端结果后成功/失败图标和 Confirm | Final/Real/UI/UIWithdrawalPendingPanel/UIWithdrawalPendingPanel.prefab / .cs | 成功仅表示对应返回结果；不要新增到账承诺 |
| 提现历史 | 时间、金额、平台、账户及状态；失败原因；空状态 | Final/Real/UI/WithdrawHistory/WithdrawHistory.prefab / .cs；WithdrawHistoryItem | 处理中/成功/失败可放一列表；空状态附例，无需独立业务 |
| FAQ | 问答滚动内容、关闭 | Final/Real/UI/FAQPanel/FAQPanel.prefab / .cs | 问题和答案沿用现有本地化或配置，不虚构支付政策 |

## 服务、奖励及额外功能

| 视觉画面 | 真实结构及交互 | 源路径与链路 | 状态合并 |
|---|---|---|---|
| 客服 Feedback | 对话列表、输入框、Send、快捷提问、FAQ、History | Final/Real/UI/ServicePanel/ServicePanel.prefab / .cs；ServiceBtn | 输入、空/有消息、键盘抬升共一页 |
| Quick Reply | 可选常见问题列表，选后回客服 | Final/Real/UI/ServiceSelectPanel/ServiceSelectPanel.prefab / .cs | 不新增独立帮助中心分类 |
| 新手礼 | 奖励主视觉、金额/币数、Reward/Continue | Final/Real/UI/NewbieGiftPage/NewbieGiftPage.prefab / .cs；Teach_01.asset | 币种取单/双货币配置 |
| 兑换率提升 | 升级前后币数及兑换金额、Check 进入提现 | Final/Real/UI/ExchangeRatePanel/ExchangeRatePanel.prefab / .cs；AccountModule.CheckPlayerWithdrawRateExchange | 不将汇率提示画成新付费升级产品 |
| 每日可提现提醒 | 当前余额与可提现金额、Withdraw 进入主提现 | Final/Real/UI/DailyWithdrawPanel/DailyWithdrawPanel.prefab / .cs；AccountModule | 提醒卡与主提现区分 |
| 幸运抽奖 | 抽奖机、当前货币、Spin、免费机会/视频机会、FAQ | Final/Real/UI/SlotsPanel/SlotPanel/SlotPanel.prefab / .cs；SlotEnter | 每过 5 关免费机会；无机会走现有视频入口 |
| 抽奖结果 | 奖励图标、金币/货币数量、领取 | SlotsPanel/SlotPanel/SlotRewardPanel.cs，内嵌 SlotPanel | 结果类与通用奖励风格一致，可做同页状态 |
| 抽奖规则 | 对应图标/组合奖励说明，获取机会说明 | Final/Real/UI/SlotsPanel/SlotFQAPanel/SlotFQAPanel.prefab；SlotFAQPanel.cs | 不新增付费投注、押注额、现金赌场元素 |

## 明确排除

第三方样例和 NiceVibrations / AdvancedInputField demo；TestServicePanel 测试入口；历史 TEST 本地提现；非正式分支的旧龙主题白版胜利；缺乏当前入口支持的独立首页、地图、商城、排行榜；单独把 CurrencyBar、ServiceBtn、WithdrawHistoryItem、WithdrawWay、BroadcastBar、粒子特效等组件算成完整界面。
