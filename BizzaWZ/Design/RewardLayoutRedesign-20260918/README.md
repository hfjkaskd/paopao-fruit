# 奖励结算页：位置、进度条、文字与按钮

最新预览：`02-ai-reward-layout-preview.png`，供用户确认。本轮仅设计预览并只读检查当前工程，没有改动 Assets、Prefab、代码、账号或 Unity 播放状态，也没有操作电脑界面。

图片类型：**AI 效果图**。不是 Prefab 静态预览，也不是 Unity 运行截图。AI 会重新绘制周边像素，落地时仅调整本轮指定的节点和素材，不以整张效果图覆盖游戏。

## 调整内容

- 标题牌缩薄，与完整可见的关卡标签分开。
- 绿色广告领取按钮保留一个视频图标和原文案，去掉侧边树枝，文字与图标居中；普通领取金额仍是独立入口。
- 广告奖励卡改为较薄边框；两端图标、百分比、Extras 分开排列；提示文字留出内边距；进度样例6/10=60%。
- 下方现金进度与两端钞票/PIX图标留间距；样例余额200.76/目标800=25.095%；原提现提示及币种含义保留。
- 中央金币1000、粉色钞票19.2、max标签、两种领取方式及所有动态文本来源保持原意。

## 生成记录

使用内置 image_gen 编辑模式。初稿提示词：`prompt.txt`；针对初稿的修正提示词：`refinement-prompt.txt`。最终图片原样复制，没有修改生成像素。

- 用户截图：`References/current-user-markup.png`。
- 初稿：`01-ai-draft.png`，存在百分比压图标、背景托盘遗漏，已由第二版修正，不作为确认版本。
- 最终生成原图：`C:/Users/pc/.codex/generated_images/01a0af0e-fa04-75a3-8fd6-557bcd874b3f/exec-9257553f-d5d8-4b3b-a4bd-16e89c1ea7bc.png`。
- 进度条参考来自项目原版 SlotEnter_Progressbg.png 和 SlotEnter_ProgressFill.png，不改变奖励语义。

## 实施前需保留的绑定与结构

主页面 `Assets/BizzaWZ/Final/Real/UI/GetRewardPanel/GetRewardPanel.prefab`。

1. 标题 BG 是嵌套实例4513770577257915395，源GUID0d5de169c40649143ba1505c5d27527e。现木牌RT8409235070765857127为856×210、局部y511；等级RT6915577366139743424为279×145、局部y432，竖向重叠98.5单位。牌可改约800×132并上移到y585；标题TMP的RT3804901329987287260同步，等级现中心可保留。精确值需最终以渲染验收。
2. 主领取Button4189992113526766931和普通领取Button990767400992513373都必须保留。现主按钮476×172、中心y-300，普通领取800×120、中心y-437，点击区域重叠9单位；缩减高度与普通领取自身Rect后，验证ButtonIdleAnim缩放最大状态也不相交。不创建新交互节点。
3. 主按钮文字TMP4284177494034142888，视频图标原95×93；适当缩图标并增加文字空间。动态文案及倍率仍由GetRewardPanel刷新。
4. 广告加成的两端图标原120×88/y23，百分比200×50/y10，Extras200×50/y-21。使用现有节点重新拉开间距。WathAdProgress用Sliced Image并调整Fill.anchorMax.x，必须保留锚点填充机制，不能随意改成Filled。
5. 截图底部金额模式实际是主Prefab内的 **Fake_WithdrawProgress**，绑定WithdrawProgress.cs，用DOFillAmount(cur/target)；保持Horizontal Filled。其槽750×52、填充750×41无水平内缩，需要匹配尺寸的槽/填充素材并留边。不要误改成广告次数模式。
6. 另一个嵌套Real_WithdrawProgress.prefab使用Real_AdWatchProgress.cs，依广告次数计算，和金额模式通过singleCurrencyMode互斥。样式可以协调，原模式、绑定、经济含义必须保留。
7. 当前生产主按钮两片树叶Image已禁用，截图来自旧对象；本次效果图延续无树枝按钮。图片没有表示代码已经实施。

全部可复用现有RectTransform、Image、TMP、标准Button。不增加控件、不重排节点、不烘焙金额/标题/动态文字、不改变领取、广告、奖励或提现业务。用户确认后再实施并验证。
