# 框架换皮资源审计（只读）

参考美术：`Design/withdraw-ui-2026-09-17/02-sunshine-orchard-v3-no-tree.png`。

本目录只新增分析脚本、JSON、CSV 和资源联系图，没有修改任何运行时代码、Prefab、图片或 meta。`textures.csv` 列出 140 张 UI_Frame PNG 的完整路径、GUID、原图尺寸、9-slice 边界、直接/序列化字段/Prefab 覆盖引用和所在 Prefab；`prefabs.json` 列出 64 个具有图片或文字的非第三方 Prefab 的逐控件图片、Tint 和文字颜色。不能把该数量当成运行时页面数量；真实可达页面需结合 UI 注册与业务入口。

## 推荐的最少共享素材分组

相同功能可复用同一设计源图，按现有尺寸导出，保留目标 PNG `.meta` GUID，从而维持 Prefab、脚本状态数组和配置的引用。不要批量替换所有 Sprite。

### 1. 木框、奶油内容板、木牌标题

- `Common/.../UI_Frame/Common/Common_Frame.png`：15 个 Prefab 引用，1000×1074，原 border L224/B170/R196/T164。优先级最高，换成木质圆角框+奶油内底。
- `Common/.../UI_Frame/Common/Common_Title.png`：12 个 Prefab 引用，1011×237。旧资源是金色丝带，新图为圆润木牌。原图无 9-slice；保留各界面标题文字。
- `Common/.../UI_Frame/Common/bg_InPanel.png`：9 个 Prefab 引用，785×333。可作为淡奶油/浅黄绿信息底板。当前 border L392.5/B166.5/R392.5/T166.5 吃满图片，不适合直接照搬新资源的边缘。
- `Final/.../UI_Frame/Withdrawal/Bg_WithdrawFill.png`：3 个 Prefab 引用，818×1228，border L146/B153/R138/T247。独立的顶部留空输入对话框资源，要按原布局处理标题区域。
- `Final/.../UI_Frame/RealWithdrawPanel/bg_LevelFrame.png`：提现等级卡片，473×252，同时被 `Shadow` 和 `SelectShadow` 黑色覆盖层复用，不能只换底图而忽略状态覆盖。
- `Final/.../UI_Frame/FakeWithdrawPanel/Bg_FakeWithdraw.png`：3 个 Prefab 的金额信息板。
- `Final/.../UI_Frame/DailyTask/Task_Elementbg.png`、`Task_RewardBg.png`、`GetRewardPanel/Bg_Level.png`、`Bg_DailyAd.png`、`WithdrawHistory/Bg_WithdrawHistoryItem.png`：可从同一木质卡片/浅色内板设计导出。
- `ServicePanel/Quick Reply.png`、`front_ba.png`：客服问答面板和条目。`ServiceSelectPanel` 的 8 个 question 图片 alpha 为 0，不能自动清成 1，否则会改变旧交互区域的表现。

### 2. 蓝色次按钮、绿色主按钮、禁用按钮

- `Common/.../Common/Btn_Normael.png`：12 个 Prefab，336×139，当前无切片。
- `Final/.../Common/Btn_CanWithdraw.png`：6 个 Prefab，336×139，宜绿色主 CTA。
- `Final/.../Common/Btn_Claimed.png`：2 个 Prefab；应保留已领取/禁用的语义。
- `Common/.../Common/Btn_Green.png`：414×172，border L207/B85/R207/T87；边界合计吃满原图，需重新确认。
- `Common/.../SettingPanel/Bg_PauseContinue.png`、`Bg_PauseBack.png`、`Final/.../GetRewardPanel/Btn_GetReward.png`：可以复用蓝/绿胶囊按钮源图。
- `Final/.../DanPanel/Icon_CanClaim.png`、`Icon_Claimed.png`、`Icon_NotClaim.png` 是语义状态底图，不是功能图标；可按绿/灰绿/金黄色导出。
- `Final/.../SlotPanel/SlotOther/Btn_Slot.png` 与 `Bg_SlotReward.png` 可做木底按钮，但前者已切片，后者不一定相同。

### 3. 圆形导航与小状态件

- `RealWithdrawPanel/Icon_Back.png`（5）、`Icon_FAQ.png`（5）、`Icon_Historiy.png`（4）：可逐个重绘为蓝色玻璃按钮+金边+白色既有语义图形；不要用同一纯蓝圆片覆盖掉箭头/帮助/记录图形。
- `Common/Btn_Close.png`（8）、`Withdrawal/Icon_CloseFillPanel.png`（3）：原尺寸不同，要独立导出。
- `RealWithdrawPanel/Icon_ChannelSelected.png`（5）、`Withdrawal/Icon_SeleChannel.png`：保留绿色勾选语义。
- `Common/.../GamePanel/Icon_Setting.png`、`SettingPanel` 开关图标、客服入口/发送图标可逐项换皮，保持各自语义以及开/关的可辨性。

### 4. 进度条

统一设计木色/奶油色轨道和鲜绿色填充，分别导出 `Loading/Bg_LoadingProgress`、`Bg_LoadingFill`、`DailyTask/Task_ProgressBg`、`Task_ProgressFill`、`FakeWithdrawPanel/Bg_FakeProgress`、`Bg_FakeProgressFill`、`GetRewardPanel/Bg_DollarFill`、`Bg_DollardProgress`、`Bg_DailyAdFill`、`Bg_DailyAdProgress`、`SlotEnter/SlotEnter_Progressbg`、`SlotEnter_ProgressFill`。

注意有些现有资源的名称与视觉不一致：`Bg_FakeProgress.png` 是绿色条，`Bg_FakeProgressFill.png` 是棕色轨道。应按 Prefab 的 Image.Type/Filled 关系和实际引用认定角色，不能凭文件名覆盖。

### 5. 无大树的天空田野背景

- `Final/.../RealWithdrawPanel/bg_RealWithdraw.png`（1080×2340）：被 `FakeWithdrawPanel`、`RealWithdrawPanel`、`WithdrawDanPanel` 共享。
- `Final/.../ServicePanel/bg.png`（1080×2400）：`ServicePanel` 使用。
- `Final/.../SlotPanel/SlotOther/Bg_Slot.png`（1080×2340）：`SlotPanel`、`SlotFQAPanel` 使用。
- `Common/.../Loading/Bg_Loading.png`（941×1672）：`LoadingPanel` 使用。
- `RealWithdrawPanel/bg_top.png`（1080×163）：提现三页顶栏；建议木色顶栏与木牌主标题协调处理，不要直接塞入完整风景。
- `Common/UI_Frame/Common/Bg_GameTop.png` 与 `Common/UI_Frame/GamePanel/Bg_GameTop.png` 是两张独立 GUID 的同名资源，前者未找到当前扫描 Prefab 引用，后者用于游戏 HUD；不要误替换一张就认为两者均已覆盖。

建议让各全屏页面复用同一新背景 Sprite 引用，或至少同一图源和压缩设置；不要引入每页一张独立高分辨率全屏图。运行时背景加载应沿用既有链路，避免新增强引用集合导致启动全部读入。

## 不能按普通底板全局替换的素材

- PaymentConfig 的国家支付通道 Logo（PagBank / PIX 等）。`WithdrawWay.Init` 会运行时执行 `payIcon.sprite = paymentConfig.GetSpriteByIconKey(...)`，单改 Prefab 默认 logo 无效，也不能覆写为装饰图。
- 金币、货币/国家符号、段位 `Icon_DanLevel1..7`、六种 SlotElement、游戏道具、星级亮灭图片：脚本或配置运行时选择，必须保留含义和数组顺序；若重绘则逐个做相应图案。
- `Loading/ICON.png`、`ICON_notification_small.png`、`Icon_LoadingLogo.png`、`Icon_LoadingProgress.png`：旧文件混有汽车和其他游戏 Logo。需要独立品牌检查，不能把 app icon / 通知 icon 当面板贴图直接统一替换。
- 动态头像/动态下载图片、视频/RenderTexture、Spine/粒子特效/动画图集不属于普通 UI 底板。原有老虎机整机 `Slot.png` 也不宜用大面板直接覆盖。
- `Icon_AD`、`SlotOther/Btn_SlotOk.png` 和图片中的文字/播放图形有实际动作语义，不能用无字底板代替。

## Prefab 颜色与文字重点

- `WithdrawLevelItem.prefab`：`Shadow` 黑色 alpha 0.3764706，`SelectShadow` 黑色 alpha 0.3137255，导致完整卡片发灰。参考图应为浅鼠尾草绿色的内部锁定区、仍然温暖的木框。需配合这两个状态节点的覆盖区域/颜色/图片调整，保留脚本显隐和 `UIGrey` 语义。
- `UIWithdrawalPendingPanel.prefab` 的 `ProgressFill` Tint 为 `(0.38, 1, 0.06, 1)`；绿色新图叠加该 Tint 会变色，宜设白以呈现源图本色。
- `UIDailyTaskElement.prefab` 的 `mask` 使用任务底图乘黑 alpha 0.43137255，和等级卡同样有木框发灰风险。
- 多数旧正文是白字+紫描边。`MainFont_Simple_ZTextMain` 和 `MainFont_Simple_ZTextTitle` 的 outline 宽度均 0.17，颜色 `(0.262,0.144,0.462)`。参考图正文应深蓝、无粗紫边；标题与 CTA 保留白字但改暖深棕/深绿细边。不要把所有白字改深蓝，绿色按钮上的白字应保留。
- `WithdrawLevelItem`：`Level 1` 用 `MainFont_Simple_ZTextMain`，金额 `0~50` 用 `MainFont_Simple_ZTextTitle`，倍率 `1.4X` 用 `MainFont_Simple_Gray`，需逐角色更换 material/tint。
- `RealWithdrawPanel` 已有正文深蓝 `(0.137,0.322,0.435)`、强调绿；`withdrawValueKeyColor = #009870FF` 和 `withdrawChannelKeyColor = #007789FF` 为序列化富文本色，可在 Prefab 统一到新色板。
- `WithdrawInfo`、`WithdrawLevel`、`DailyBonus`、`DailyBonusItem`、`WithdrawHistory`、`WithdrawHistoryItem` 仍有棕色正文；改深蓝会比只换图片更接近参考。
- `ServicePanel`、`ServiceSelectPanel`、`WithdrawFillPanel`、`UIWithdrawalConfirmPanel` 保留黑/灰系统式文本，需要按正文、占位、错误、禁用角色细分；表单错误红色不应改成普通蓝色。
- `ChatElement` 运行时会从序列化 `issueTextColor`、`playerTextColor`、`timeTextColor` 重写文字颜色，因此修改 TMP 默认颜色会被覆盖。现有 issue 白、player 深棕、time 白；在 Prefab 的这些字段改为配套颜色。`issueBubbleColor` 目前代码没有实际使用（图片被设为白 Tint），真正气泡颜色来自两张 Sprite。
- `DailyMissionPanel.cs` 有两处硬编码富文本 `#9039D8`，若需要完全统一新色板，宜改成 Inspector 可配置字段并给现有 Prefab 值；保持业务文案原样。

## 额外静态检查发现

`Common/MenuSystem/Common/UICommons/BG.prefab` 中图片 GUID `68dde9efe07d2c44bbe2792cd56951ad`、`89d451fef247f974facd4dd9021a91dc`、`efc4376bb2ad27e479660660402d8448` 在当前 `Assets/**/*.meta` 未找到。它是旧共用对话框样板，需结合实际引用确定是否可达；不要因此恢复历史工程。这里只记录，不修改。

## 验证建议

换图后重点查看 9-slice 与不同弹窗长宽比、提现等级锁定态、表单错误态、客服左右消息和长文本、任务已领取态、加载进度、老虎机内部区域。所有检查从 `InitWZ.unity` 正式初始化链路进入。审计本身没有运行 Unity，没有声称当前界面运行效果已验证。
