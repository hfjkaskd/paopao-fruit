# 提现记录页样式统一

工程：`C:/Projects/paopao/BizzaWZ`。参考：`Design/OrchardServiceSystem-20260927/images/15-withdraw-history.png`，852 × 1846。

[查看最新对照](comparison.html) · [验证摘要](verification-summary.json)

## 已落实到游戏

- 三种状态使用同一套浅色胶囊：浅黄时钟、浅绿勾选、浅红叉号，分别对应审核中、完成、未完成。标签由真实状态控制，复用记录时同步切换图标、文字和失败原因。
- 卡片统一细金边、浅奶油底；重新校准平台标志、金额、日期、账户信息与失败原因的位置和比例。
- 底部帮助按钮恢复浅蓝质感与问号图标，文字大小按参考缩小；保留现有客服入口。
- 收款信息收拢为卡片上的单行账户：PIX 优先证件号，其他方式优先收款邮箱，缺失时显示收款账户，再回退证件号。服务器原始记录不被改写。
- 列表使用矩形裁剪，记录可以滚动；图标保持原比例。全部布局、字体参数和可见按钮结构保存于实际预制体，图片按页打开时加载。

## 验证与证据

由正式 `InitWZ` 场景完整初始化后打开生产历史页。`Runtime/00-live-history.png` 和短屏对应文件保留当前账号真实历史响应的画面。为覆盖三种状态，`01-history.png` 使用临时实例化的真实记录预制体并调用现有 `Init` 方法；示例日期、金额和遮罩账户不保存、不发送到服务端。

两种尺寸：852 × 1846、1080 × 1920。检查状态 1 → 2 → 3 → 4 → 1 的复用切换、账户回退、文字边界、12 条记录滚动、帮助/客服/返回按钮和账号状态不变。测试后移除临时记录并恢复原有记录和空态。没有发起提现，也没有发送客服消息。

这是 Unity 编辑器正式运行路径与预制体的验证；本轮未进行 Android/iOS 真机打包，不将视觉接近声称为全图像素完全一致。完整结果见各 Runtime 目录的 `verification.json`。

## 美术与实现文件

- 生产资源：`Assets/OrchardUI/Resources/OrchardUI/HistoryReferenceControls.png`，保留透明通道；Android/iOS 使用 ASTC 6×6，关闭 mipmap。
- 使用内置 image_gen 编辑模式；[首次完整提示词](imagegen-prompt.json)、[细化提示词](imagegen-refinement-prompt.json)。保留生成原图，仅用 Unity 原生 Sprite 切片导入，未重绘或修改验证截图。
- 页面与记录：`Assets/BizzaWZ/Final/Real/UI/WithdrawHistory/WithdrawHistory.prefab`、`WithdrawHistoryItem.prefab`、`WithdrawHistoryItem.cs`。
- 静态编辑器配置：`Assets/OrchardUI/Editor/OrchardHistoryReferencePass.cs`；运行时资源绑定：`Assets/OrchardUI/Runtime/OrchardHistoryVisual.cs`。
- 定向检查：`Assets/OrchardUI/Editor/OrchardHistoryRuntimeCheck.cs`。`Before` 保存修改前预览与预制体备份。
