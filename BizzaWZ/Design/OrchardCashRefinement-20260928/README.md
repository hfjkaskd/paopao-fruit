# 现金提现页修正 · 2026-09-28

针对用户提供的现金提现对照图，修复金额卡片上缘像被裁断的问题，并重新校准页面视觉。

- 旧 `Mask + UIMask` 图片遮罩替换为标准 `RectMask2D`，网格四周留出边距；卡片完整边缘位于遮罩内。
- 金额卡片使用完整的普通 / 选中背景、独立绿色勾选标记和对应文字颜色。可见卡片 Image 就在标准 Button 本体上，金额和勾选标记不重叠。
- 校准标题、余额框、金额字号、卡片尺寸间距、分隔线、进度条、灰色提现按钮和底部提示；补齐粉色钞票图标。
- 进度文案从填充条移至进度容器，避免进度变小时文字跟着偏移或被挤压。完整填充仍保持在轨道内。
- 金额标签补回真实货币符号；金额不足时显示当前真实差额。按钮可用状态与原有条件校验保持对应，服务端数据未就绪时不可提交。
- 保留框架提现链路及所有实际档位，包括当前配置中的新手档位。原图 `R$6,40`、五张卡片、`80%` 只在隔离预览副本中展示，不写入账号或配置。
- 修复页面关闭时误用 `SetLinster(true)` 的事件订阅，关闭 / 销毁时解除订阅。

## 复查

- [交互对照页](../OrchardAllScreens-20260928/comparison.html#cash-withdraw)：预制体、正式运行、短屏三种视图，支持透明叠加。
- [隔离预制体预览](../OrchardImplementation-20260928/Previews/cash-withdraw.png)：使用设计中的示例数据。
- [正式运行截图](Runtime/01-selected-goal.png)、[短屏运行截图](RuntimeShort/01-selected-goal.png)：使用当前账号数据。
- [852 × 1846 检查](Runtime/verification.json)、[1080 × 1920 检查](RuntimeShort/verification.json)：从正式 InitWZ 完整初始化，经 HUD 的现金 Button 打开页面；切换各金额并恢复初始选择；检查点击、边框裁剪、文字溢出、金额 / 勾选重叠、进度条边界、不可用提示和返回。
- 检查前后的现金余额、提现阶段、新手领取标志和提现点击次数一致。没有点击提交提现，没有执行实际付款，没有进行 Android / iOS 真机打包测试。
- [汇总](verification-summary.json)记录编译和预制体审核结果。审核仍包含工程原有缺失引用，不将它们报告为本次修复。

## 资产与实现

页面和金额组件分别保存于 `Assets/BizzaWZ/Final/Real/UI/FakeWithdrawPanel/FakeWithdrawPanel.prefab` 与同目录的 `WithdrawAmountItem.prefab`。静态层级、尺寸、字体、状态颜色、提示格式均保存在预制体中；运行时代码仅加载已配置图片并刷新状态。

本轮使用内置 `image_gen` 从参考图提取控件；完整提示词保存在 [imagegen-prompts.json](imagegen-prompts.json)。成品为 `Assets/OrchardUI/Resources/OrchardUI/CashReferenceControls.png`，由 Unity 原生 Sprite Editor 数据导入 / 切片，页面打开时通过 Resources 异步加载。Android / iOS 配置为 ASTC 6×6、关闭 mipmap。未用脚本绘改截图像素。

制作入口为 `OrchardCashReferencePass.cs`（`cash-import` / `apply:cash-withdraw`）；运行检查为 `OrchardCashRuntimeCheck.cs`（`cash` / `cash-short`）。本次开始时的文件快照位于 `Before/`。视觉文字位置测量只用于局部尺寸核对，不等同于全图像素一致性证明。
