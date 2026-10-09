# 每日任务界面精修

工程：`C:/Projects/paopao/BizzaWZ`，Unity 2022.3.62f3。正式启动场景为 `Assets/Game/Resources/Scenes/InitWZ.unity`。

参考：`Design/PremiumOrchardUI-20260927/images/23-daily-mission.png`。

## 本轮调整

- 重做弧形叶片木牌、带视频标识的礼盒、金边关闭按钮、叶片绿色按钮和蓝色时钟。
- 奖励区改为柔和薄边绿色卡片，放大奖励金额；观看次数说明、金额和当前进度各占独立位置。
- 恢复浅橙色进度分组、深棕色底槽、橙色动态填充和白色描边进度数字。
- 字号、位置、图形尺寸全部保存在原 `DailyMissionPanel.prefab` 中。大图经 Resources 按需加载，未将页面烘焙成整张不可交互截图。
- 保留原标准 Button 和代码事件绑定、广告与提现业务链路。任务响应调用统一显示方法，避免金额重复；广告完成后的显示仍等待正式任务响应刷新。

## 验证与查看

[设计与 Unity 对照](comparison.html#daily-mission)，包含预制体、852×1846 运行画面、1080×1920 运行画面及叠加模式。

`Runtime/00-live-daily.png` 与 `RuntimeShort/00-live-daily.png` 保存正式页面初次打开时的实际任务状态。其余状态图使用临时显示数据检查 8/30、可领取、已领取、零目标，未改写存档、未观看广告、未领取奖励。关闭重开后再核对真实任务状态与倒计时。最终检查结果见 `verification-summary.json`。

预制体预览中的背景是已保存的实际游戏画面，前景是 Unity 渲染；运行截图为完整实际画面。关卡内容、币种、语言由当前游戏决定。此次检查不等同于 Android/iOS 真机测试或逐像素完全一致证明。

## 美术资源

使用内置 imagegen，根据原图生成透明组件图集；完整提示词保存在 [imagegen-prompts.json](imagegen-prompts.json)。最终图集：`Assets/OrchardUI/Resources/OrchardUI/DailyReferenceControls.png`。Unity 切片保留原始 alpha，移动端使用 ASTC 6×6，无 mipmap。

原预制体、预览与布局文件备份保存在 `Before/`。本轮只重新应用每日任务页的样式。
