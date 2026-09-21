# 原版道具栏恢复

- 复用原游戏 CorePlayUI 的 Undo / Magic / Shuffle / AddOne 节点和动画；底部顺序为撤销、魔法、洗牌，扩槽在托盘右侧。
- RealGamePanel 不再实例化框架圆形道具栏，原 propsRoot 在 Prefab 中关闭。
- 原按钮同节点绑定 UIPropEntry，沿用框架库存、解锁条件、使用限次和补充道具面板；InputMono 继承标准 Unity Button，点击事件由代码绑定。
- 道具教学目标位置取原按钮；显示原版数量徽标、广告提示、锁定图片。扩槽层级配置在 Prefab 中，位于托盘上方。
- HarvestSetup 的常规配置保留原按钮，Restore Original Prop Presentation 菜单可重新配置绑定。

验证：Unity 实际启动进入第 5 关，1080×1920；Prefab 内部引用无缺失，迁入 GUID 冲突为 0。未做 Android/iOS 真机验证。
