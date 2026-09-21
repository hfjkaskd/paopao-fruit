# 托盘与扩容按钮重新设计：待确认的 AI 局部效果图

本轮仅制作设计预览，没有修改任何 Assets、Prefab、脚本、玩法或运行状态，没有操作电脑界面。

预览：`01-ai-original-style-preview.png`。使用内置 image_gen 编辑模式生成，提示词完整记录在 `prompt.txt`。

原输出：`C:/Users/pc/.codex/generated_images/01a0af0e-fa04-75a3-8fd6-557bcd874b3f/exec-286b5101-a605-41a0-affa-86508dffa091.png`。复制到本目录，未修改像素。

## 设计方向

参考原游戏圆润的浅木托盘和蓝白扩容按钮，简化木纹、软化边沿，将右侧方木牌及铜锁改为适合原透视的圆润蓝色按钮与清晰白锁。保持当前背景、托盘占位、右侧按钮位置和底部控件关系。没有添加格子、枝叶或其他装饰。

## 生成前核对

- 现有用户局部截图：`References/current-user-crop.png`，作为编辑目标。
- 原蓝色按钮：`Assets/FruitsHarvest/Resources/Original/res/local/coreplay/sprite/item/ExtraButton.png`。
- 原蓝白锁：`Assets/FruitsHarvest/Resources/Original/res/local/coreplay/sprite/item/Extra_Lock.png`。
- 只读参考了当前仓库的早期 BizzaWZ 迁移截图 `C:/Projects/paopao/Tools/Migration/unlock-completed-reentry.png` 和 `unlock-pending-reentry.png`，证实原移植版为浅金木盆与蓝色扩容道具；没有按历史记录启动或操作任何工程。
- 检查 `CorePlayUI.prefab`：Box_Root 1080×300、底部位置(-2,355)；托盘三层1066×254；AddOne位于(433,45)，背景162×172，锁69×79。
- 原逻辑容量为7，使用Extra后为8；锁、+1、库存及AD由现有状态控制。

## 后续实施边界

待用户确认后，把美术拆分到现有托盘后层/前沿/阴影及既有按钮背景/锁图标中。效果图中的锁不可烘焙进托盘，不能增加固定隔断或改变收集位置、按钮数量、解锁状态逻辑。

这是 AI 效果图，不是 Prefab 静态预览，也不是 Unity 运行截图。图片中细节与像素占位最终仍需在工程中验证。
