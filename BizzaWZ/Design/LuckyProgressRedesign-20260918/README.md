# 777 入口进度条：AI 局部效果图，待确认

用户以截图明确指定 777 下方的进度条。本轮只设计该进度条，不实施前一轮托盘预览，也不修改 Unity 工程素材或运行状态。

预览：`01-ai-original-style-progress.png`。

生成方式：内置 image_gen，编辑模式。完整提示词：`prompt.txt`。生成输出原样复制，未修改像素。

原输出：`C:/Users/pc/.codex/generated_images/01a0af0e-fa04-75a3-8fd6-557bcd874b3f/exec-d20852c1-fc3f-4900-8f02-a14eb6b37178.png`。

## 设计

按项目保留的原版进度条设计：浅色细圆角边框、深蓝底槽、绿色柔和渐变填充、居中白色细描边动态文字。示例显示4/5，即80%；部分填充的右缘为直线裁切，与原有Horizontal Filled组件兼容。去掉当前木框和夸张高光，不添加叶片、分段格、移动端帽或额外节点。

生成输入：

- `References/current-user-crop.png`：用户截图，编辑目标。
- `Assets/BizzaWZ/Final/BizzaGame/Z_ReplaceAssets/UI_Frame/SlotPanel/SlotEnter/SlotEnter_Progressbg.png`：原版深蓝白边底槽，样式参考。
- `Assets/BizzaWZ/Final/BizzaGame/Z_ReplaceAssets/UI_Frame/SlotPanel/SlotEnter/SlotEnter_ProgressFill.png`：原版绿色填充，样式参考。

## 生成前检查与实施边界

已读取当前 SlotEnter.prefab 与 SlotEnter.cs、SlotProgressUtil：

- 轨道224×40，位置(0,-88)，填充202×24，文字200×50且居中。
- 原进度算法是Current/5，Current限制0..5；截图中的4/5对应80%，没有证据表明该数值计算错误。
- 当前Controls图集填充素材286×62被用于202×24的矩形且不保留比例，存在约1.82倍的相对水平拉伸。后续实施需采用匹配宽高比的填充纹理，保留当前Filled水平从左侧填充方式。
- 使用原有轨道Image、填充Image、TMP动态文字，保留777按钮、图标素材、位置、点击区域、进度与奖励逻辑。
- 不将4/5画进工程素材，不直接把整张效果图导入为按钮。AI预览会重绘周围像素，实际实施只替换进度条相关资源与必要导入配置。

这是 AI 效果图，尚未替换 Unity，不是 Prefab 静态预览或 Unity 运行截图。没有操作电脑界面、播放模式或游戏账号。
