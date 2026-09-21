# 绿色按钮去树枝叶片装饰

用户要求：截图中这种绿色按钮的树枝、叶片装饰全部去掉。

已修改当前唯一交付工程 BizzaWZ，共 21 个 Prefab、26 对（52 个）叶片 Image。仅将装饰 Image 的 m_Enabled 从 1 改为 0，保留所有 GameObject、组件、层级、位置、按钮本体、动态文字和交互绑定。金额、奖励、提现和玩法代码没有修改。

截图对应 PausePanel / GamePauseGroup / ContinueBtn。其左右叶片来自 Navigation 图集的独立装饰，并未画进绿色按钮素材；因此无需重画图片。

同类处理覆盖继续、复活、领取、广告、提现、确认、新手奖励、每日任务及老虎机奖励按钮，包括使用 ButtonGreen、HudNaturalGreenButton、SlotEmeraldButton 的现有装饰。

OrchardNavigationPass 的编辑器换皮规则同步改为只为原有 Title 添加叶片，并关闭绿色按钮已有的叶片，避免以后应用皮肤时复发。标题和背景原有装饰保持原样。

## 验证范围

- baseline.json / Before：修改前文件哈希和备份。
- changes.json：精确的 52 项 Image 可见性修改清单。
- independent-validation.json：修改后的独立序列化与引用审计。
- 本次没有控制鼠标、键盘、窗口、播放模式或游戏页面，没有运行提现、奖励或账户业务。
- 已实例化的运行时页面可能仍显示旧对象；退出并重新运行后会使用修改后的 Prefab。本次未强制切换用户正在运行的游戏。

没有新生成的 AI 素材、效果图或运行截图。
