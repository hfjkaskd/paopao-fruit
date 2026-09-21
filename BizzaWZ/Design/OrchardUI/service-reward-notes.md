# 客服、活动奖励与 Slot 界面换皮作者记录

美术锁定：蓝天白云、低矮田野和草地，无背景大树；蜂蜜木框、奶油内容面、绿色主按钮、蓝色辅助按钮、深蓝正文和浅鼠尾草锁定状态。

作者工具：`Assets/OrchardUI/Editor/OrchardServiceRewardPass.cs`。由总作者流程先执行通用 sprite / font pass，再调用 `Apply(GameObject root, string assetPath)`，最后保存 prefab。此文件不添加运行时逻辑，不自行打开或保存 prefab，也未被单独运行。

## 针对性覆盖（19 个 prefab）

| 范围 | Prefab | 处理 |
| --- | --- | --- |
| 客服 | ServicePanel | 全屏田野背景；木质标题、奶油聊天区、输入框、蓝色快捷回复；输入和占位文字配色；光标与选择高亮的序列化颜色 |
| 客服 | ServiceSelectPanel | 木框弹窗、奶油快速回复卡、正文留白与换行 |
| 客服 | ServiceBtn | 保留完整蓝色客服按钮图（根 sprite 含气泡图标，子 Image 仅为红点），避免按钮变为空白底板 |
| 客服 | ChatElement | 客服和玩家气泡、正文/时间色；同步运行时会使用的序列化颜色和既有边距参数 |
| 客服 | FAQPanel | 奶油阅读面、深蓝问答；同步富文本 token 颜色与现有预览字符串颜色；保留滚动与遮罩 |
| 活动奖励 | DailyMissionPanel | 任务面板、绿色领取/前往、浅 sage 已领状态、提示和时间层级 |
| 活动奖励 | DailyWithdrawPanel | 奶油金额面、绿色金额与提现按钮 |
| 活动奖励 | ExchangeRatePanel | 原汇率浅 sage 卡、当前汇率选中卡、绿色结果金额 |
| 活动奖励 | NewbieGiftPage | 木质面板、金额底板、绿色领取按钮 |
| 活动奖励 | GetRewardPanel | 奖励徽章、广告奖励进度面、进度条、绿色继续/领取；保留金币飞行目标与支付图标 |
| 活动奖励 | Real_WithdrawProgress | 统一进度条与正文，保留支付图标和进度数据 |
| 活动奖励 | StarRatingPopup | 木质弹窗、深蓝正文与绿色确认；保留星星贴图、数量和点击状态 |
| 活动奖励 | UIDailyTaskPage | 木框内容、木标题、奶油/绿色 tab 状态、活动进度条 |
| 活动奖励 | UIDailyTaskElement | 奶油任务卡、蓝色前往、绿色领取/广告、进度条与较轻的禁用遮罩 |
| 活动奖励 | UIActivityTaskElement | 奶油奖励浮层与数字；保留未开/已开/已完成宝箱语义图 |
| 活动奖励 | ItemForCountry | 奖励徽章文字；保留不同国家/任务入口的识别图 |
| Slot | SlotPanel | 全屏田野背景、奖励木框、绿色领取、正文/金额配色；保留转轮、机器镂空框和 Spine 动画结构 |
| Slot | SlotFQAPanel | 全屏田野背景、木框说明面、奶油结果面、蓝色返回、深蓝说明 |
| Slot | SlotEnter | 统一进度条与数字，保留 777 入口图和奖励动画 |

## 保留约束

- 不改 PageId、Addressables 地址、业务数字、支付 Logo、金额、任务条件、奖励值或事件绑定。
- 不重命名、不搬迁、不删除既有业务层级；保留 ScrollRect、Mask、输入法重排与目标引用。
- `Image.Type.Filled` 进度图保留 Filled 类型。Slot 的滚轮和镂空机器框不使用实心通用卡片覆盖。
- 仅 ServicePanel、SlotPanel、SlotFQAPanel 使用全屏背景；其余覆盖型弹窗保留遮罩。
- FAQ 富文本颜色和 ChatElement 动态颜色在 prefab 的序列化字段中配置，避免运行时覆盖静态皮肤颜色。

## 总清单口径

`prefab-manifest.json` 包含 **56** 个框架可见 UI prefab。最初的 60 个候选包含 `[GameInstance]`、`GameCanvas`、纯基础 `PageMask` 和 `Mask`；这四个已排除。保留 4 个可见引导页面、TransitionBlock 过场、WhiteWinPanel 兼容页。排除 ThirdParty 示例、Original 原游戏 UI、BingoAsset 游戏/特效资产、VFX、GameMode 和非 UI SDK dispatcher。

## 尚待总流程验证

本子任务仅完成作者代码与覆盖清单。Unity 编译、总流程保存、页面实际渲染、不同分辨率和语言检查由根任务统一完成，不将代码编写视为换皮已验收。
# Visual verification of regenerated previews

Reviewed every actual PNG from previews 22–56, using QA-only contact sheets and full-resolution checks of ServicePanel, RealWithdrawPanel, and SlotPanel. Fixed the clearly visible presentation defects in this editor pass: compact ServicePanel's existing wood heading to match other full pages; auto-fit quick-reply text at 24–32 units inside the original 80-unit rows; use dark text for DailyMission's secondary sentence on cream; use light text for Slot's action labels and machine-body instructions, Slot FAQ's machine-body paragraph, and GetReward progress hints that sit directly on its dark overlay. No localization strings, business states, source machine artwork, or UI bindings were changed.

Static ServicePanel previews show both its input and default question control because OnOpen is intentionally not run by the preview utility. `SwitchDefaultInputState` makes those mutually exclusive at runtime; do not "fix" it by removing a control. Empty country/reward illustrations are runtime-populated data placeholders. QA-only contact sheets are under `Design/OrchardUI/QA/`.
