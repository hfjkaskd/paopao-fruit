# 新道具解锁弹窗修复

本次实际修改工程：`C:/Projects/paopao/BizzaWZ`。未控制窗口、鼠标、键盘或 Play 模式，未操作账号、存档、奖励。

## 原因与修改

- `NewItemPop.prefab` 的旧标题原本烘焙在图片里，换皮后成为无字木牌。保留原有 38 个节点，在原 Title 节点配置 TMP、CanvasRenderer 和既有本地化组件，绑定 `guide_newtool_title`。禁用空木牌及旧标题高光的 Image，保留节点、组件及动画路径。
- ClaimBtn 两层 Image 都引用完整绿色按钮，导致双层边框；两层文字也重复。禁用内层重复 Image 和旧阴影文字组件，保留真正的本地化文字。按钮从 720×274 调整为 680×190，中心不变。名称与说明配置自适应字号，说明改为奶油白。
- `GameUiWidget.prefab` 关闭独立 Canvas 排序后，实际继承框架的 0 层，位于原游戏弹窗 -600 层之上。恢复现有 Canvas 的独立排序为 -700，使 HUD、宝箱、礼物及 777 处于全屏弹窗遮罩之下。框架弹窗、教学、输入锁及全局提示仍在上层。同步 `HarvestSetup.cs` 的作者配置，避免重新配置时恢复错误层级。

## 实际生产文件

1. `Assets/FruitsHarvest/Resources/Original/res/local/pops/newitempop/NewItemPop.prefab`
2. `Assets/BizzaWZ/Final/Real/UI/GamePanel/Resources/Resources/GameUiWidget.prefab`
3. `Assets/FruitsHarvest/Editor/HarvestSetup.cs`（仅资源配置工具的排序参数与注释）

原节点、原组件、动画、按钮代码绑定、解锁记录、领取与图标飞行动画逻辑均保留。运行时代码 `NewItemPop.cs`、`MgrUI.cs` 与修改前 SHA256 一致。完整备份在 `Before/`。

## 验证与图片性质

- `00-user-reported-runtime.png` 是用户提供的修改前运行画面。
- `Validation/static-*.png` 是 Unity 在隔离 PreviewScene 中渲染的 **Prefab 静态预览**，不是 AI 效果图，也不是运行截图。背景为预览用场景素材，未运行关卡树、HUD、账号或奖励流程。
- 葡语字符串直接读取原 `Text.json`，四种道具使用原 Icon 资源；在隔离对象上采样原入场动画结束帧。Undo、Shuffle、Magic、Extra 均在 1080×1920 检查，Undo 另验 1080×2160。五张图都显示四个文本组件，无截断、溢出或文字网格越界。
- 静态层级检查确认 Widget -700 < Popup -600，全屏遮罩 Image 的 raycastTarget 保持开启；未模拟实际点击或广告/领取。
- 独立文件审查通过：`independent-review.json`。结构修改记录：`prefab-change-report.json`。Unity 验证结果：`Validation/static-validation-report.json`。
- 一次性 Editor 验证脚本已移出 Assets，仅归档在 `Validation/`。没有留下新的后台轮询脚本。

本轮没有进入 Play 模式，因此不宣称已完成真机或完整运行流程验收。
