# 确认提现页精修 · 2026-09-28

针对确认提现页对照图的差异，修改实际 `UIWithdrawalConfirmPanel.prefab`。

- 原参考画布为 828 × 1900。新增按页面保存的设计尺寸，当前页面按原比例排版和预览；实际屏幕继续等比适配，避免把纵向布局压短。其他页面保留原设计尺寸。
- 信息行从拉伸的近方形输入框换成宽条卡片，恢复细金边、白色内高光、圆角和浅奶油底。
- 提示条取消聊天气泡尾巴与多余金框，恢复平整浅蓝底和左侧信息图标。
- 编辑按钮恢复完整高度、浅蓝渐变和深蓝文字；确认按钮、金额框、木牌、信封插画及文字位置重新校准。
- 付款 Logo 保持资源原始比例；姓名、证件号、邮箱、金额仍由原有业务刷新。静态结构在 Prefab 中保存，按钮保持原代码绑定。

[打开最新对照](comparison.html#withdraw-confirm) · [修改前预览](Before/withdraw-confirm.png) · [当前预制体预览](../OrchardImplementation-20260928/Previews/withdraw-confirm.png)

## 验证范围

[长屏检查](Runtime/verification.json)与[短屏检查](RuntimeShort/verification.json)从正式 `InitWZ` 初始化，通过 HUD 和服务端支付平台打开原账户页，再通过原有页面参数接口打开确认页。金额使用当前账户值，姓名、遮罩证件号、邮箱使用不落盘的设计样例；不是实际收款人。

检查返回、帮助、编辑资料三条路径；编辑返回后原输入字段保持不变。检查四个按钮的点击命中，确认按钮只执行 pointer down / up 视觉状态，不派发提交点击。没有申请提现、执行支付或修改提现时间和余额。未做 Android / iOS 真机打包验证。

另检查控件边界、文字溢出和 Logo 比例。此记录不宣称整幅画面的每个像素完全一致。编译及预制体引用审核结果见 [验证汇总](verification-summary.json)。

## 美术与实现

内置 `image_gen` 提取参考控件，完整提示词保存在 [imagegen-prompts.json](imagegen-prompts.json)。工程图片为 `Assets/OrchardUI/Resources/OrchardUI/ConfirmReferenceControls.png`，使用 Unity 原生 Sprite 切片。页面打开时 Resources 异步加载，Android / iOS 为 ASTC 6×6、关闭 mipmap。未用脚本修改设计图或截图像素。

制作命令：`confirm-import` / `apply:withdraw-confirm` / `preview:withdraw-confirm`。运行诊断：`confirm` / `confirm-short`。本轮开始时的预制体、布局和截图保存在 `Before/`。
