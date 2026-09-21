# 现金提现页样式与进度条修正

当前改动已写入正式工程 BizzaWZ 的 FakeWithdrawPanel 和 WithdrawAmountItem。层级、组件数量、六个金额档位、选中状态含义、事件绑定、本地化、钱包、任务和提现流程均保留。

## 进度条

原 Filled Image 将286×62的短条拉成897×64，圆端与高光横向变形，且填充与底槽同尺寸，遮住边框。现在使用独立低高光填充素材，保留 Horizontal / Left / Filled 和原 fillAmount 逻辑，开启 PreserveAspect；底槽897×48、填充区域865×36，绿色实际图形按自身比例居中显示，不再拉扁圆端。百分比仍由原业务动态刷新。

用户截图选中0.01新手档，余额200.76；100%符合该档现有条件。此次没有修改进度计算、任务目标、奖励或金额。

## 页面

- 主面板换成专门绘制的竖版薄木边奶油框，叶片直接画在框内。
- 标题、卡片和提现按钮减轻粗边与高光；余额数字使用深绿色。
- 六个卡片保留原排列。新手可领/已领外观与普通卡片统一，同时保留各自颜色含义和原勾选节点。
- 收紧 Progress 标题文本框的右边界，左边缘与原位置保持一致。

## 验收类型

`Previews/final-verified/` 内的三张图是 **Unity Prefab 静态预览，使用显式样例数值**。它们不是AI整页效果图，也不是游戏运行截图。

- `static-withdraw-100.png`：100%静态填充。
- `static-withdraw-035.png`：35%几何样例。
- `static-withdraw-000.png`：0%几何样例。
- `report.json`：实际Unity填充网格、字形边界、源Prefab前后哈希。
- `validation-summary.json`：独立渲染结果复核。
- `independent-validation.json`：结构、引用、业务配置不变核对。

0%和35%仅改变副本填充与百分比来验收几何，并未模拟任务状态机；页面其余文案仍为同一静态样例。尚未运行账号、广告或提现操作验证完整业务链路。所有工作在后台完成，未操控鼠标、键盘、窗口或Play开关。临时预览工具已经从 Assets 移除，仅保留 `Validation/` 源码归档。

## 素材与记录

新增素材均使用内置 image_gen 生成，原RGBA像素完整保留；裁边和九宫格通过导入配置完成。

- `Assets/OrchardUI/Art/WithdrawProgressSoftFill.png`，提示词 `Art/generation-prompt.txt`，来源/元数据 `Art/import.json`。
- `Assets/OrchardUI/Art/WithdrawCreamWoodPanel.png`，提示词 `Art/panel-generation-prompt.txt`，来源/元数据 `Art/panel-import.json`。
- 两张纹理导入上限均为1024，无mipmap，不增加运行时UI节点或样式代码。
- 原始文件备份见 `Before/`，变化记录见 `applied-changes.json`。记录包含首轮和后续细化，最终状态以独立验收与生产文件为准。

同轮主界面顶部栏的间距、关卡牌和点击范围修订，详见 `../HudSpacing-20260918/README.md`。
