# 奖励领取弹窗修正

已修改生产文件 `Assets/BizzaWZ/Final/Real/UI/SlotsPanel/SlotPanel/SlotPanel.prefab`，仅修改其 `Content/GetRewadPanel` 子树。此记录是 Prefab 文件及几何核对结果，不是 Unity 运行验收。

## 本轮修正

| 对象 / fileID | 字段变化 |
| --- | --- |
| bg Rect / 4737857836923716867 | Scale 2→1；Position (0,0)→(0,-80)；Size 487×339→840×640 |
| bg Image / 3129568935050995118 | 通用橙框→SlotIvoryPanel；PPU multiplier 0.7→4，保持 Sliced |
| 6 个 RewardIcon Rect / 7339750857267786092、572969426428037648、2384695331660256314、5899393945800387749、6756194499756007949、8631493306211032524 | 共同中心 (0,-66)→(0,70)，各自尺寸与所有奖励图引用不变 |
| MoneyRoot Rect / 2991638936245649488 | (2.9086,-378)→(0,-135)，动态金额改为放在面板内部 |
| Btn Rect / 6951951041397404055 | (0,-566)→(0,-285)；413×171→440×112 |
| Btn Image / 2180263369653926189 | 通用绿按钮→SlotEmeraldButton；Sliced→Simple |
| Btn/Image Rect / 1407570891747111414 | (0,21)→(0,0)；129×70→106×72 |
| Btn/Image Image / 8486339001066278036 | 错误按钮底图→SlotFidelityCheck；Simple、PreserveAspect=true |

共 21 个字段变化，完整旧值和新值见 [reward-popup-changes.json](reward-popup-changes.json)。未新增节点或组件。

## 核对

- 175 个序列化对象的 ID、顺序、类型、层级、组件列表和激活状态保持一致。
- Button、SlotRewardPanel、金额绑定、奖励种类、各图标引用、动态文本与本地化字段完全不变。
- 隐藏 Title 未启用；未加入烘焙金额或说明文字。
- SlotMachine 嵌套覆盖与 Spine 均未变。
- 图标、金额行与按钮均落入背景边界；奖图至金额空隙约 34 单位，金额至按钮约 44 单位。
- 未控制电脑、未执行 Unity 命令、未领取奖励或触发广告。

文件核对 [reward-popup-validation.json](reward-popup-validation.json) 为 PASS；运行状态和多语言长金额仍需实际运行核验，不将此文件核对称为运行验收。

## 勾选图资产

新引用 `Assets/OrchardUI/Art/SlotFidelityCheck.png`，GUID `e0f53d6d6f0b4f12a08073707ebe074b`，fileID `21300000`。它复用之前由内置 image_gen 生成的紧凑勾选图原始 PNG，本轮未重新生成或处理像素。导入 Sprite rect 为 (7,7,1498,1020)，最大导入尺寸 256，透明通道保留。

导入记录：[Art/SlotFidelityCheck.import.json](Art/SlotFidelityCheck.import.json)。原生成源：[../SlotRestyleImplementation-20260918/Art/SlotEmeraldCheck.png](../SlotRestyleImplementation-20260918/Art/SlotEmeraldCheck.png)。

本轮修改前原件位于 `Before/Assets/BizzaWZ/Final/Real/UI/SlotsPanel/SlotPanel/SlotPanel.prefab`；修正脚本为 [fix_reward_popup.py](fix_reward_popup.py)。
