# 客服界面视觉修正（2026-09-18）

## 修改范围

- `Assets/BizzaWZ/Final/Real/UI/ServiceSelectPanel/ServiceSelectPanel.prefab`：八个现有快捷回复按钮改为浅色细边卡片，增加文字内边距；客服入口用淡绿色区分。
- `Assets/BizzaWZ/Final/Real/UI/ServicePanel/ServicePanel.prefab`：现有问题选择按钮改为浅色底、深蓝文字，左右增加 32 单位内边距；字体自动适配 26–34，保留换行，极端长度采用省略保护；自定义输入 TextArea 增加相同安全边距。

复用已有 Controls 图集及正文文字材质，没有新增 UI 节点、组件、按钮、运行时代码或图片资源。保留文字来源、本地化、原有交互绑定和发送逻辑。

## 验证

- 独立结构审查通过：两个 Prefab 共 259 个序列化对象保持一致；37 项修改均为记录中的视觉字段。详见 `integrity-review.json` 和 `visual-changes.json`。
- 从正式 InitWZ 启动并完成初始化，在 Unity Play 模式打开真实 ServicePanel。
- 实际点击问题选择入口，确认八个快捷回复完整显示。
- 实际选择第 6 条长问题，确认原回填流程正常，完整文字位于框内。
- 实际选择客服入口，确认切换至原有自定义输入状态、占位文字和发送按钮状态正常。
- Unity 验证状态记录 compilationErrors 为 0。本轮没有发送客服消息，没有验证服务端消息投递。

## 截图类型

- `01-user-quick-replies.png`、`02-user-composer.png`：用户提供的修改前截图。
- `03-runtime-quick-replies.png`：Unity 实际运行截图，快捷回复列表。
- `04-runtime-long-question.png`：Unity 实际运行截图，第 6 条长问题回填。
- `05-runtime-custom-input.png`：Unity 实际运行截图，自定义输入状态。

每张运行截图附同名 JSON 运行状态记录。本轮未制作 AI 效果图或 Prefab 静态预览。

`Before/` 为本轮修改前备份；`apply_polish.py` 仅用于根据该备份复现本轮修改，后续编辑后不要直接重跑。
