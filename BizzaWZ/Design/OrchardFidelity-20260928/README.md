# 提现界面还原校正

工程：`C:/Projects/paopao/BizzaWZ`。本轮基于用户指出的“参考图主体占满画面，但运行画面整体缩小”的对照继续修改现有预制体。

## 查看

- [原图 / Unity 对照与叠加检查](comparison.html)：可切换正式运行、相同示例数据的隔离预制体渲染、短屏运行及滚动后截图。
- [验证记录](verification.json)：以文件中的时间、尺寸与结果为准。
- [全部 27 页](../OrchardImplementation-20260928/comparison.html)。共享素材、字体和按钮修正已同步，本轮布局校正重点是提现主界面。

## 已保存到工程的修改

1. 提现预制体以 852×1846 设计坐标排版。运行时按宽度适配，较短屏幕保留主体宽度并使用原有 ScrollRect 查看底部，修正旧版整页缩小的问题。
2. 重新制作柔焦果园背景、奶油色金边面板、绿色按钮、浅绿金额栏、纸币和支付标识，调整六张档位卡片、倍率胶囊、底部金额条与间距。
3. 使用 Baloo 2 ExtraBold，并校正用于拉丁文排版的字体度量、标题描边和按钮文字。字体许可随资源保存在 `Assets/OrchardUI/Fonts/Fidelity/baloo2-OFL.txt`。
4. 修正支付列表的旧 padding；支付、档位的选择状态仍由原有业务组件控制。提现按钮正常/可提现状态都引用新素材，避免刷新时换回旧按钮。
5. 主页面多出的客服悬浮按钮已停用，客服入口保留在右上角帮助页的“联系支持”标准 Button。此路径已加入运行验证，不发送消息。
6. UI 结构、尺寸、颜色、图片和字体保存在预制体 / 素材中。运行时仅处理尺寸适配、状态与已有业务数据；没有反射调用，没有新增模拟提现入口或修改账户余额。

## 验证边界

通过正式 `InitWZ.unity` 入口运行。852×1846 和 1080×1920 分别记录 12 个页面的打开、返回；检查支付方式切换恢复、底部档位可达与选中边框、帮助到客服路径、设置开关切换恢复，以及真实任务配置的显示。具体结果见验证记录。

补查期间验证编辑器在资源刷新处停滞，重启为批处理编辑器后恢复。批处理首次使用默认 640×480 尺寸，支付按钮可达检查失败，记录保存在 `batch-default-resolution-failure.json`；显式恢复 1080×1920 后重新执行全部 12 页检查通过，并确认选择锁定档位后展开的底部进度区可完整滚入视口。该过程中没有加入游戏运行逻辑的 Editor 兜底。最终已退出 Play mode 并结束本任务启动的验证进程。

静态预览为便于对照使用示例等级 84 与 R$0,03；正式运行保留当前账号等级、余额、汇率、支付门槛和锁定状态。两者有不同的数据来源，未用示例值替换真实业务。

版式已重新校正；字体字形、部分图标绘制和材质仍不是原图的逐像素复制，不能据此宣称数学意义上的像素完全相等。尚未完成 Android / iOS 真机和资金到账、广告收益、客服发送等外部业务闭环测试。本轮没有新增引用审计问题；工程原有的外部资源缺失记录仍保留在审计文件中。

## 资源、方法与备份

图像均通过内置 image_gen 参考原图编辑生成，再由 Unity Sprite 导入器切片，保留 RGBA 透明度；没有将整张效果图覆盖在可点击界面上。背景通过 Resources 按需加载，移动端图片设置 ASTC 6×6。

- `Assets/OrchardUI/Resources/OrchardUI/Backdrop.png`
- `Assets/OrchardUI/Art/FidelityControls.png`
- `Assets/OrchardUI/Art/FidelityPayments.png`
- `Assets/OrchardUI/Art/FidelityPills.png`

[背景、控件与支付标识提示词](prompts.md)，[提示条与底部胶囊提示词](pills-prompt.md)。

修改前备份：`before-fidelity.zip`、`Before/`。制作入口：`Assets/OrchardUI/Editor/OrchardFidelityPass.cs`，布局规格仍在 `Design/OrchardImplementation-20260928/layouts.json`。`build-review.py` 只读取现有截图和报告并生成 HTML / JSON，不修改游戏资源或图片。
