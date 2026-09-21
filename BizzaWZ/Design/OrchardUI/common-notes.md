# 基础、HUD 与共用界面换皮覆盖

作者工具入口：`OrchardCommonPass.Apply(GameObject root, string assetPath)`。只在调用方加载的 Prefab 内容上配置视觉，由主作者流程保存。工具属于 Editor 资源制作流程，生成的 Prefab/ScriptableObject 供 Editor 和真机使用同一运行逻辑；不引入运行时 Editor 分支。

| 页面或组件 | 有针对性的处理 |
| --- | --- |
| LoadingPanel | 共享无大树背景；关闭旧 BG、HarvestLoadingVisual/bg、LoadingAnim_Raw 的 Graphic 显示；两套现有进度轨道/填充统一；Start 主按钮统一为绿；保留相机、视频组件、初始化、隐私链接、进度字段、原品牌图和按钮对象。 |
| PausePanel | 木框、木牌标题、奶油内板；继续绿、返回蓝；音乐/音效/震动控制底板统一，开关图标语义与脚本显隐不变；语言下拉底板统一。 |
| LosePanel | 木框标题与内容板；复活绿、退出蓝；保留广告标记与全部按钮回调。 |
| WhiteWinPanel | 处理其独有 RawImage + 运行时皮肤配置链路；创建 OrchardVictoryPanelSkin 克隆，Card/NextButton 共享图集 texture 并在各自原 RawImage 写入对应 uvRect；运行时只重赋 texture 不触碰 uvRect，故无需改业务代码；保留英雄图、运行代码和引用；正文/面板内标题深蓝，主按钮白字。 |
| AddPropPanel | 共用弹窗壳、绿广告按钮、蓝关闭按钮；动态道具图保留并开启等比显示。 |
| RealGamePanel | 仅顶栏换为木色，不加全屏背景、不改核心玩法。 |
| GameUiWidget | 任务/每日奖励的蓝色按钮底、段位奖励标签统一；礼物、段位图标和业务状态保留。 |
| UIPropEntry | 原 Button 保留，把 BG 视觉重新归入同一个 Button 下并停用冗余 BG 图片；蓝色按钮底与数量徽章；锁图、动态道具图、序列化引用、回调保持。 |
| CurrencyBar | 奶油金额底、绿提现按钮、蓝设置底、木色等级徽章；货币图标、实时数值、账户流程保留。 |
| BroadCastBar | 原黑底广播改奶油/木色底板，正文深蓝；不改内容或滚动。 |
| CommonConfirmTipsPanel | 区分同名 Image (2) 的标题与内板；统一确认按钮，保留原动作/文案。 |
| UICommons/BG | 修复旧缺失图片引用为木框、标题牌和内板，旧装饰层仍按原激活状态。 |
| UITeachTipsPage | 奶油提示卡和深蓝正文。 |
| UITeachMaskFocusPage | 提示外框使用 SelectionRing；聚焦/遮罩材质和点击处理不变。 |
| UITeachMaskPage、UITeachFingerMovePage、PageMask、Mask | 仅现有文字样式；不改遮罩透明度、挖孔、指引手势或交互。 |

该文件由父作者流程统一调用；子任务未运行 Unity、未修改既有 Prefab、没有添加任何事件监听或持久化 UnityEvent。

需要整体 QA：LoadingPanel 真实加载/Start/隐私行为；Pause 开关两态与葡语布局；Lose 双按钮与广告图标；WhiteWinPanel 的 RawImage 图集 UV 取样/新纹理拉伸和品牌英雄搭配；HUD 不遮挡果树玩法；引导蒙版区域保持透明。WhiteWinPanel 使用既有图集区域，不复制整 atlas 为单独面板图。
