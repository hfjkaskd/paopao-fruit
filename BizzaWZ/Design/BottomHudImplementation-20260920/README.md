# 底部按钮与 777 进度条实施记录

已按用户确认的底部样式实施，并遵守后续补充：撤销、魔棒、洗牌及其锁定版本继续使用原道具图标。777 标识、AD 播放图标、锁图标也保持原资源。

本次仅修改三种按钮底板及次数角标的背景/文字对比度，以及 777 独立轨道、独立填充的素材与配置。原节点、组件数量、RectTransform、按钮绑定、资源路径和动态数据机制保持不变。无运行时代码修改。

## 生产文件

- `Assets/OrchardUI/Resources/OrchardUI/PropButtonFrame.png`：木质圆框。
- `Assets/OrchardUI/Resources/OrchardUI/PropButtonNormal.png`：奶油色圆形内底。
- `Assets/OrchardUI/Resources/OrchardUI/PropButtonLocked.png`：圆角木框；次数角标使用 Simple，避免小尺寸九宫格变形。
- `Assets/FruitsHarvest/Resources/Original/res/local/coreplay/CorePlayUI.prefab`：仅十二个已有 Image/TMP 组件的视觉字段变化。
- `Assets/OrchardUI/Art/BottomHudProgressTrack.png` 和 `BottomHudProgressFill.png`：深色轨道与独立绿色填充。
- `Assets/BizzaWZ/Final/Real/UI/SlotsPanel/SlotEnter/SlotEnter.prefab` 及 `Assets/BizzaWZ/Final/Real/UI/GamePanel/Resources/Resources/GameUiWidget.prefab`：同步进度条源配置与嵌套覆盖。
- `Assets/OrchardUI/Editor/OrchardServiceRewardPass.cs`：同步已有美术配置工具，避免重新应用时覆盖本次进度样式。

## 验证与预览类型

`Validation/static-*.png` 是从实际工程 Prefab 创建隔离副本后，经 Unity 渲染的静态预览。保留原布局，使用纯色背景便于查看底部 UI；不是 AI 效果图，也不是游戏运行截图。所有业务组件在副本激活前移除，显式设置显示状态，不调用游戏、账号或广告业务。

- 0/5、1/5、4/5、5/5 对应填充 0%、20%、80%、100%，使用 Horizontal/Left Filled。
- 已检查混合状态、全部解锁、全部锁定，以及 AD 和次数显示。六种静态状态无文本溢出。
- 三个原 Resources 路径使用 `Resources.Load<Sprite>` 均加载成功，GUID 和 fileID 保持原映射。
- 图标文件哈希、Prefab 结构和按钮绑定由独立报告核对。
- 验证过程未保存或修改源 Prefab；未启动/停止 Play、点击按钮、播放广告或改变账号数据。未据此宣称完成真机或业务流程测试。
- 临时渲染脚本仅存档于本目录 Validation，不保留在 Assets 中。

首轮 `prop-background-patch-report.json` 中次数角标的 Sliced/PPU 配置后来修正为 Simple/PPU 1；最终状态以最终核验报告及 `Validation/static-validation.json` 为准。

原文件备份在 `Before`，生成原图与提示词在 `Generated`；所有资源图片均来自图像生成工具，透明边距通过 Unity Sprite rect 配置裁切。
