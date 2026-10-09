# 幸运抽奖：文字居中与返回按钮

工程：`C:/Projects/paopao/BizzaWZ`，Unity 2022.3.62f3；正式入口 `Assets/Game/Resources/Scenes/InitWZ.unity`。

本轮将数字与“FREE SPIN / GIRO GRÁTIS”合成一个动态文本标签，整体对齐橙色底牌中心；标题同样居中并调整字号。原 `SlotPanel.prefab` 中的标准返回 Button 换成蓝底白色左箭头，保留代码绑定的返回事件。

免费次数继续取自实际关卡进度，英葡文案格式保存在预制体。未改变转动、奖励、广告业务。

[查看对照](comparison.html#lucky-spin)。预制体模式使用设计示例；运行模式来自当前账号。`Runtime/01-label-one.png`、`02-label-zero.png` 仅临时改变文本来检查排版，不更改进度、余额或免费次数。

通过正式启动场景检查 852×1846、1080×1920：文字中心与溢出、完整按钮素材、实际免费次数、返回、重开与余额/进度保持。结果见 `verification-summary.json`。未触发转动或广告，未做 Android/iOS 真机验证。

原文件备份在 `Before/`。返回图标使用内置 imagegen 根据参考图生成，最终资源为 `Assets/OrchardUI/Art/SpinReferenceBack.png`，导入分辨率上限 512；完整提示词在 [imagegen-prompts.json](imagegen-prompts.json)。
