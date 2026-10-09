# 精致果园 · 全部界面调整记录

项目：`C:/Projects/paopao/BizzaWZ`，Unity `2022.3.62f3`。本轮对 27 个界面逐页调整并保存到实际 Prefab / 资源配置。

[打开全部界面对照](comparison.html) · [机器可读验证摘要](verification-summary.json) · [生产美术与完整提示词](imagegen-prompts.md)

后续精修：[道具补充弹窗](../OrchardBoosterRefinement-20260928/README.md)。该页现已使用独立参考美术层与弧形标题，并增加三种道具在两种尺寸下的正式运行检查；对照页中的该页面已更新。

后续精修：[现金提现页](../OrchardCashRefinement-20260928/README.md)。修复金额卡片上缘裁剪，重新校准卡片、余额框、选中勾、字体、进度、灰色按钮与提示；已增加金额切换、边界、货币符号和状态同步在两种尺寸下的正式运行检查。[直接查看本次对照](../OrchardCashRefinement-20260928/comparison.html)。

## 本轮调整

后续精修：[余额提醒页](../OrchardReminderRefinement-20260928/README.md)。恢复支付卡片的完整金色边缘与局部反光，以及金额框的绿色轮廓；调整说明文字间距和关闭按钮比例。[查看本次对照](../OrchardReminderRefinement-20260928/comparison.html)。

后续精修：[提现记录页](../OrchardHistoryRefinement-20260928/README.md)。统一浅黄时钟、浅绿勾选、浅红叉号三种状态，调整记录卡片和帮助按钮。补充状态复用、列表滚动、帮助/客服/返回在两种屏幕尺寸下的检查。[查看本次对照](../OrchardHistoryRefinement-20260928/comparison.html)。

后续精修：[确认提现页](../OrchardConfirmRefinement-20260928/README.md)。按原图 828 × 1900 比例排版，修正卡片边框、浅蓝提示条、信息图标和编辑按钮；保留实际页面逻辑。[查看本次对照](../OrchardConfirmRefinement-20260928/comparison.html)。

后续精修：[收款账户页](../OrchardAccountRefinement-20260928/README.md)。恢复付款 Logo 比例和输入框边缘高光，增加真实输入焦点蓝框，修复运行时文字顶对齐及校验提示与说明条重叠。[直接查看本次对照](../OrchardAccountRefinement-20260928/comparison.html)。

- 加载页恢复完整果园画面与 Orchard Trio 标识；调整进度条尺寸、位置和填充，正式初始化过程中也检查了资源绑定与正常关闭。
- 通关页按参考重新配置背景、木牌、宝箱、奖励数字、COINS 标签、橙色进度、叶片奖励及影片图标；其他弹窗同步调整标题、插画、按钮与留白。
- 提现服务系列逐页调整卡片、输入框、付款平台、状态色、奖章、客服头像和气泡。长文本和动态行保留滚动与实际业务状态。
- 设置开关、评分星星、任务行、七个客服问题、转盘装饰与结果区域均落实到预制体。底部转盘、撤销、魔杖与洗牌按钮采用比例锚点，托盘与按钮分开排列。
- 静态布局与美术保存在 Prefab；运行时代码负责已有事件、状态刷新和按需资源绑定。背景经 Resources 加载；新增图集使用移动端 ASTC 6×6、关闭 mipmap。新增开关与按钮沿用标准 Unity Button 和代码事件绑定。

## 如何看对照

对照页包含左右比较和透明叠加，可切换预制体、852×1846 正式运行截图、1080×1920 正式运行截图。页面显示图像来源，缺少运行截图时明确回退到预制体预览。

27 个界面都有预制体渲染记录；主游戏展示实际运行截图，因为孤立 HUD 不包含游戏场景。教学、复活、道具补充、倍率提升、客服问题、欢迎礼包、每日奖励、转盘帮助这八页的静态预览使用预制体前景与保存的游戏背景合成，界面中有明确标注。

正式截图中的语言、关卡、水果种类、金额、任务和提现状态由当前账号与服务端决定。预制体预览中的设计示例数据仅写入临时副本，未为对图更改正式存档、余额或关卡。

## 已执行验证

| 检查 | 结果 | 证据 |
|---|---|---|
| C# 编译 | 0 个编译错误 | `../OrchardUI/state.json` |
| 正式 InitWZ 初始化后的页面导航 | 两种尺寸各 13/13 页面打开、关闭完成 | [长屏](../OrchardImplementation-20260928/Runtime/navigation.json) / [短屏](../OrchardImplementation-20260928/RuntimeShort/navigation.json) |
| 提现 | 付款方式切换并还原、最后档位可滚动到达、锁定档位页脚完整显示、余额未变化 | 同上 |
| 设置 | 音乐、音效、振动切换并恢复原值 | 同上 |
| 客服 | 帮助打开客服；七个问题可点击；选择问题填入草稿；自定义输入可用，未发送消息 | 同上 |
| 任务、评分、转盘 | 5 条真实任务；三星选择刷新五颗星；原转盘动画完成回调并显示三个结果，未消耗次数 | 同上 |
| 主游戏底部四个 Button | 两种尺寸中心命中成功；完整可见图形在屏幕内，互不重叠且不与托盘重叠 | [长屏](../OrchardImplementation-20260928/Runtime/hud-layout.json) / [短屏](../OrchardImplementation-20260928/RuntimeShort/hud-layout.json) |
| 加载预制体 | 3 种比例 × 5 个进度，共 15 个临时副本，0 个记录错误 | [加载预览报告](../OrchardLoading/preview-report.json) |
| 实际加载 | 两种尺寸均观察到图片绑定、显示与正常关闭，0 个记录错误 | [长屏](loading-startup-reference.json) / [短屏](loading-startup-short.json) |
| Prefab 引用与 Button 审核 | 56 个 Prefab、171 个 Button；新增问题 0、缺失脚本 0、缺失本地对象引用 0 | [完整审核](../OrchardImplementation-20260928/audit-current.json) |

两个导航报告均记录了项目已有 SDK 文本 `开启测试设备False`。其来源是 `MaxSDKPlugin.cs:79` 使用 `Debug.LogError` 输出测试设备状态；本轮导航未记录其他错误或异常。

完整引用审核仍记录 171 条既有错误和 12 条既有警告，包含历史资源 GUID、动画、材质等缺失项，均保留在报告中；重复引用会产生多条证据，不能把这个数字当作独立运行故障数，也不能据此声称整个工程没有问题。

本轮没有执行 Android/iOS 真机测试、商店打包、真实提现、广告奖励发放、客服发送或商店评价提交。静态预览与已检查的页面交互不等同于这些端到端流程全部验证。

## 界面与 Prefab

| 界面 | 项目内 Prefab |
|---|---|
| 00 · 已确认提现主界面 | `Assets/BizzaWZ/Final/Real/UI/RealWithdrawalPanl/RealWithdrawPanel.prefab` |
| 01 · 启动 / 加载 | `Assets/BizzaWZ/Common/MenuSystem/Common/LoadingPanel/LoadingPanel.prefab` |
| 02 · 主游戏 / HUD | `Assets/BizzaWZ/Final/Real/UI/GamePanel/Resources/Resources/GameUiWidget.prefab` |
| 03 · 新手教学 | `Assets/FruitsHarvest/Resources/Original/res/local/coreplay/prefab/NewPlayerGuider.prefab` |
| 04 · 通关 / 奖励领取 | `Assets/BizzaWZ/Final/Real/UI/GetRewardPanel/GetRewardPanel.prefab` |
| 05 · 失败 / 复活 | `Assets/BizzaWZ/Common/BizzaGame/LosePanel/LosePanel.prefab` |
| 06 · 设置与暂停 | `Assets/BizzaWZ/Common/UI/SettingPanel/PausePanel.prefab` |
| 07 · 道具补充 | `Assets/BizzaWZ/Final/Real/UI/AddPropPanel/AddPropPanel.prefab` |
| 08 · 新道具解锁 | `Assets/FruitsHarvest/Resources/Original/res/local/pops/newitempop/NewItemPop.prefab` |
| 09 · 每日任务（框架附录） | `Assets/BizzaWZ/Final/MenuSystem/Common/Task/UIDailyTaskPage.prefab` |
| 10 · 游戏评价 | `Assets/BizzaWZ/Final/Real/UI/StarRatingPopup/StarRatingPopup.prefab` |
| 11 · 现金提现（框架分支） | `Assets/BizzaWZ/Final/Real/UI/FakeWithdrawPanel/FakeWithdrawPanel.prefab` |
| 12 · 收款账户 | `Assets/BizzaWZ/Final/Real/UI/WithdrawFillPanel/WithdrawFillPanel.prefab` |
| 13 · 提现确认 | `Assets/BizzaWZ/Final/Real/UI/UIWithdrawalConfirmPanel/UIWithdrawalConfirmPanel.prefab` |
| 14 · 提现处理中 | `Assets/BizzaWZ/Final/Real/UI/UIWithdrawalPendingPanel/UIWithdrawalPendingPanel.prefab` |
| 15 · 提现记录 | `Assets/BizzaWZ/Final/Real/UI/WithdrawHistory/WithdrawHistory.prefab` |
| 16 · 段位奖励（框架分支） | `Assets/BizzaWZ/Final/Real/UI/WithdrawDanPanel/WithdrawDanPanel.prefab` |
| 17 · 可提现提醒 | `Assets/BizzaWZ/Final/Real/UI/DailyWithdrawPanel/DailyWithdrawPanel.prefab` |
| 18 · 兑换率提升 | `Assets/BizzaWZ/Final/Real/UI/ExchangeRatePanel/ExchangeRatePanel.prefab` |
| 19 · 常见问题 | `Assets/BizzaWZ/Final/Real/UI/FAQPanel/FAQPanel.prefab` |
| 20 · 客服对话 | `Assets/BizzaWZ/Final/Real/UI/ServicePanel/ServicePanel.prefab` |
| 21 · 客服快捷问题 | `Assets/BizzaWZ/Final/Real/UI/ServiceSelectPanel/ServiceSelectPanel.prefab` |
| 22 · 新手礼 | `Assets/BizzaWZ/Final/Real/UI/NewbieGiftPage/NewbieGiftPage.prefab` |
| 23 · 每日视频任务 | `Assets/BizzaWZ/Final/Real/UI/DailyMissionPanel/DailyMissionPanel.prefab` |
| 24 · 幸运抽奖 | `Assets/BizzaWZ/Final/Real/UI/SlotsPanel/SlotPanel/SlotPanel.prefab` |
| 25 · 幸运抽奖规则 | `Assets/BizzaWZ/Final/Real/UI/SlotsPanel/SlotFQAPanel/SlotFQAPanel.prefab` |
| 26 · 通用提示 | `Assets/BizzaWZ/Final/FunctionTools/CommonPublic/UI/CommonConfirmTipsPanel.prefab` |

## 复查入口

- 客服页裁切专项修正：聊天视口留白、气泡与输入控件、长消息排版和键盘关闭后的视口恢复。长短屏与按钮验证见 [客服页修改与验证](../OrchardServiceRefinement-20260928/README.md)。

- 兑换率提升弹窗专项调整：叶片木牌、主金币与放射光、前后卡片底色和星光、连接箭头、绿色金额及按钮比例。实际预制体与长短屏验证见 [Rate Up 修改与验证](../OrchardRateRefinement-20260928/README.md)。

- 编辑器制作入口：`Assets/OrchardUI/Editor/OrchardApprovedPass.cs` 及同组制作脚本；最终布局保存在 `../OrchardImplementation-20260928/layouts.json`。
- `author.ps1 preview:all` 只在编辑模式生成隔离副本预览；`author.ps1 audit` 读取现有资产作引用审核。运行检查使用正式 InitWZ 初始化。
- 本轮开始时的文件副本保存在 `Before/`，用于对照本轮改动；工程已有其他未提交修改未作整体回退。
- 对照页生成器为 `build-review.py`，汇总记录生成器为 `finalize-review.py`。这些脚本只处理证据文件，不修改运行截图。
