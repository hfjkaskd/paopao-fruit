# 已选参考的老虎机换皮实施记录

本次只实施 BizzaWZ 工程中的老虎机主页面和奖励说明页面，按用户最新选择的两张参考制作。2026-09-18 用户要求“后台替换，不要控制我的电脑”后，停止全部窗口切换、鼠标、键盘操作；后续只进行文件替换、既有 Unity 后台资源导入及只读截图检查。

## 最终资源与效果

- 木质外壳、深绿面板、黄铜边、拱形灯牌；连续三格奶油色转轮窗。
- 主界面使用绿色旋转、返回、帮助按钮。
- 奖励说明使用同款灯牌与机身、奶油色表格、独立圆润格子、绿色确认按钮。
- 修正机身装饰：为既有说明文字留出完整深绿空间；帮助页顶部改透明接合区域，避免遮住星形装饰。
- 确认勾通过 PNG 内的透明留白对齐，未移动 RectTransform。
- 所有金额、文本、奖励符号、进度及本地化内容继续来自原组件和业务逻辑，未画入素材。

最终导入素材位于 `../../Assets/OrchardUI/Art/`：

`SlotEmeraldMachine.png`、`SlotEmeraldHelpBody.png`、`SlotEmeraldMarquee.png`、`SlotEmeraldButton.png`、`SlotEmeraldBack.png`、`SlotEmeraldHelp.png`、`SlotEmeraldCheck.png`、`SlotIvoryPanel.png`、`SlotIvoryTile.png`。

只改以下三个已有 Prefab 的视觉字段：

- `Assets/BizzaWZ/Final/Real/UI/SlotsPanel/BingoAsset/Assets/UiPrefab/SlotMachineGroup.prefab`
- `Assets/BizzaWZ/Final/Real/UI/SlotsPanel/SlotPanel/SlotPanel.prefab`
- `Assets/BizzaWZ/Final/Real/UI/SlotsPanel/SlotFQAPanel/SlotFQAPanel.prefab`

## 检查结果与范围

独立静态检查全部通过：3 个 Prefab、36 个视觉字段；293 个 Transform、7 个 Button、4 个 Spine、10 个本地化组件以及 1358 个 C# 文件保持本轮基线。节点、组件、按钮绑定、动画回调、奖励和经济业务绑定未改。28 项最终有效 Sprite 引用通过，其中 18 个既有格子引用新 `SlotIvoryTile`。9 张新素材的 alpha、GUID、Sprite rect、导入上限、源文件 SHA 均通过；原有资源未改。

报告： [结构审计](final-integrity.json)、[资源与有效引用](imports-effective-audit.json)。

Unity 通过既有后台导入工具完成资源刷新；最新状态为编译错误 0、lastError 为空。最终状态副本：[后台导入状态](background-import-state.json)。

在用户要求停止桌面操作之前，已从正式 InitWZ 启动流程进入 GamePlay，通过原 777 按钮打开老虎机，再通过原帮助按钮打开说明页。未点击旋转广告按钮、领取奖励，也未进行广告/奖励到账端到端验收。用户要求停止桌面操作后，未测试确认返回及主页面返回。

## 预览与运行记录严格区分

| 文件 | 类型和范围 |
| --- | --- |
| `01-approved-machine.png` | 用户选定的 AI 参考图，不是 Unity 运行画面 |
| `02-approved-help.png` | 用户选定的 AI 参考图，不是 Unity 运行画面 |
| `03-runtime-slot.png` / `.json` | 第一轮真实 Unity 运行截图，早于底部装饰修正，不代表最终版 |
| `04-runtime-help-first.png` / `.json` | 第一轮真实 Unity 运行截图，记录星形和页脚遮挡问题，不代表最终版 |
| `05-runtime-help-after-import.png` / `.json` | 后台导入后的只读 Unity 运行截图。已确认星形完整、页脚无遮挡、确认勾在按钮内；当前存活页面仍缓存旧格子 Sprite，所以不是完整最终截图 |

当前已打开的说明页仍持有 19 个 `SlotIvoryPanel` 引用（大面板加 18 个旧格子）。磁盘 Prefab 已正确配置 1 个 `SlotIvoryPanel` 和 18 个 `SlotIvoryTile`；重新开始正常运行会加载完整配置。为遵守用户要求，没有控制 Unity 重启或切换页面。最终主页面的装饰修正与完整格子更新尚待重新运行后的目视确认。

## 图片生成和来源

使用内置 `image_gen` 模式；没有使用 API/CLI fallback。生成 PNG 原样复制进工程，没有用 Python 或其他工具改动图片像素。Python 只读尺寸/透明边界、复制文件并配置 Unity Sprite 导入。

最终提示词与来源：

- 机身：[最终提示词](Art/SlotEmeraldMachine-v2.prompt.txt)、[来源](Art/SlotEmeraldMachine-v2.provenance.json)。
- 帮助机身：[修订提示词及来源](Art/help-body-revision-provenance.json)。
- 灯牌：[提示词](Art/help-production-prompts.json)、[来源](Art/help-production-provenance.json)。
- 按钮和导航控件：[提示词集](Art/control-prompts.json)、[来源](Art/control-sources.json)。
- 确认勾：[最终提示词](Art/SlotEmeraldCheck-v2.prompt.txt)。
- 格子：[最终提示词](Art/SlotIvoryTile.prompt.txt)。
- [勾和格子修订来源](Art/refinement-control-provenance.json)、[最终导入清单及源 PNG 哈希](Art/import-info.json)。

原始生成文件位于本目录 `Art/`，实际工程引用的是 `Assets/OrchardUI/Art/`。机身与帮助机身模型返回 999×1575（目标为 998×1575），使用原有 RectTransform 和 full-rect Sprite 适配；未为 1 像素差异调整布局。

`Before/` 是本轮改动前的只读基线。`apply-approved-slot.py` 和 `import-art.py` 是第一轮实施记录，不应直接重跑覆盖最终修订；`apply-visual-refinements.py` 记录最终素材配置，`visual-changes.json` 记录最终视觉差异。
