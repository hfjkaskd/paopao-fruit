# 收款账户页比例与高光修正 · 2026-09-28

按用户提供的对照图修正 `withdraw-account` 收款账户页。截图内容对应表单页，浏览器原来的 `withdraw-confirm` 锚点并非此次修改对象。

- PagBank 图标由强制拉伸改为原始宽高比显示，恢复圆形标志；仍使用当前支付平台配置。
- 将接近方形的通用输入框素材替换为参考图比例的宽条素材，保留浅金边、白色高光、圆角、轻微投影及白色输入底。
- 蓝色选中边框绑定真实 AdvancedInputField 焦点事件，取消焦点后恢复金色边框。没有改变输入组件、校验规则或提现链路。
- 恢复提示条的圆形信息图标，校准字体粗细、提示文案与脚注。提示条、继续按钮、脚注进入原表单的 VerticalLayoutGroup，校验消息展开时一起向下排布。
- 输入文字使用插件支持的 `TextAlignmentOptions.Left`，修复运行时将 `MidlineLeft` 当成默认顶对齐、导致文字贴近上边框的问题。
- 静态布局和引用存入 `WithdrawFillPanel.prefab`；运行时只按需加载图片、响应输入焦点。没有创建本地模拟提现入口。

## 预览与验证

[最新对照页](comparison.html#withdraw-account)支持参考图、预制体预览、正式运行截图和短屏截图。

隔离预览中的 `R$0,03` 和蓝色邮件框只是视觉样例。正式运行截图从 InitWZ 完整启动，使用当前账号与服务端支付平台。运行检查覆盖输入框命中、获得 / 取消焦点、蓝金边框切换、输入 / 恢复文本、继续按钮的空表单校验、错误提示排布和返回。

空表单校验不会进入确认付款页面；校验函数会保存表单，因此检查器立即恢复并保存原有表单字段。没有提交提现或执行支付。未进行 Android / iOS 真机打包测试。

检查器在验证继续按钮前，通过现有跨平台键盘 API 收起键盘；键盘自身 Done 键行为不在本轮验证范围内。

详细结果在 [长屏检查](Runtime/verification.json)、[短屏检查](RuntimeShort/verification.json)和 [汇总](verification-summary.json)。静态预览不作为实际业务验证证据。

## 素材与制作记录

使用内置 `image_gen`，从已批准参考图提取普通输入框、蓝色焦点输入框及信息条。完整提示词见 [imagegen-prompts.json](imagegen-prompts.json)。Unity 原生 Sprite 切片由 `OrchardAccountReferencePass.cs` 配置。

成品：`Assets/OrchardUI/Resources/OrchardUI/AccountReferenceControls.png`。页面打开时通过 Resources 异步加载；Android / iOS 使用 ASTC 6×6，无 mipmap。未用脚本绘改参考图或运行截图。

制作命令：`account-import`、`apply:withdraw-account`、`preview:withdraw-account`。诊断命令：`account`、`account-short`。本轮前的预制体与预览保存在 `Before/`。
