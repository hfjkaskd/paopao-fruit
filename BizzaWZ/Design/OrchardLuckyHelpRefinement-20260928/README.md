# 幸运抽奖说明页：卡片清晰度调整

工程 `C:/Projects/paopao/BizzaWZ`，Unity 2022.3.62f3；正式入口 `Assets/Game/Resources/Scenes/InitWZ.unity`。

本轮修改原 `SlotFQAPanel.prefab`：六行奖励卡片与 18 个图标底框换成专用暖色素材，使用完整 Simple Sprite 保留边缘反光和轮廓，不再沿用被压缩的通用输入框。符号保持原始色彩、100% 不透明度与等比显示。

按参考恢复两条图文说明、叶片标题和按钮、右上角关闭、完整奖励名称及信息图标。背景遮罩独立铺满屏幕，短屏两侧不会漏亮，前景不随背景压暗。底部原按钮和新增关闭按钮均为标准 Button，在 `SlotFAQPanel.OnAwake` 绑定关闭事件。

[设计/Unity 对照](comparison.html#lucky-help) 提供预制体与两种尺寸的实际运行截图。预制体前景由 Unity 渲染，背景是本轮幸运抽奖页截图；运行模式为从正式入口进入游戏后，经问号按钮打开的实际界面。

验证覆盖 852×1846、1080×1920、六行卡片、18 个图标底框、文字溢出、打开、关闭和重开；结果见 `verification-summary.json`。未触发转动、广告或奖励。未做 Android/iOS 真机验证。

美术使用内置 imagegen 生成，最终图集：`Assets/OrchardUI/Resources/OrchardUI/LuckyHelpReferenceControls.png`。完整提示词：[imagegen-prompts.json](imagegen-prompts.json)。图集通过 Resources 按需加载；切片、文字、颜色、尺寸均保存在预制体，运行时不创建静态 UI。

原预制体、脚本、预览及布局在 `Before/`。本轮未更改转动次数或奖励发放逻辑。
