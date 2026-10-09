# 客服页裁切与清晰度修正

[最新对照](comparison.html) · [验证摘要](verification-summary.json) · [素材生成记录](imagegen-prompts.json)

工程：`C:/Projects/paopao/BizzaWZ`，Unity 2022.3.62f3。

## 修正

- 聊天视口保留完整底边和留白，使用矩形遮罩。支持消息按实际文字高度排版，长内容可滚动到最后一条，避免固定视口切掉第四条气泡。
- 重做浅蓝/浅绿聊天气泡、蓝色问题按钮、白色输入框及圆形图标。恢复今日标签两侧和输入区顶部的分隔线，空输入时隐藏清空按钮。
- 气泡尾巴完整包含在九宫格固定边缘内，长消息只延展正文区域，避免尾巴被纵向拉长。
- 按参考校准头像、气泡宽度、正文比例、发送按钮和输入提示。单行消息与多行消息使用预制体配置的不同留白，布局计算考虑文字缩放。
- 快捷问题按钮和输入框可同时显示：选题填入输入框，开始手动编辑切回自定义消息，清空后仍可输入或重新选题。开关保存在 ServicePanel 预制体，按钮事件保持代码绑定。
- 修复旧 ViewportResizer 在键盘关闭时覆盖视口锚点的问题；现在只按实际遮挡抬高底边，键盘高度归零恢复原始偏移，保留上沿与锚点。

## 验证

从正式 `Assets/Game/Resources/Scenes/InitWZ.unity` 完整初始化。852 × 1846 与 1080 × 1920 各检查四条参考消息的完整气泡和文字、14 条长消息滚动、键盘高度回调恢复，以及快捷问题选择、输入清空、FAQ、历史和返回。每个尺寸检查 7 个按钮命中，包括发送按钮可用状态；**未点击发送、未向客服发送测试消息**。

视觉样例通过临时 ChatElement 实例创建，不调用消息记录或发送接口；原有行在测试结束恢复，存档聊天数量保持不变。`00-live-service.png` 保留正式接口打开的页面，`01-service.png` 为明确标注的临时消息样例，`02-long-scroll.png` 为长消息滚动证据。截图未经图片编辑。

验证仅覆盖 Unity 编辑器中正式业务链路和键盘高度回调，没有 Android/iOS 真机键盘测试，也没有执行客服消息提交。素材与字体属于按参考精修，未声称全图像素完全一致。

## 主要文件

- 页面与聊天行：`Assets/BizzaWZ/Final/Real/UI/ServicePanel/ServicePanel.prefab`、`ChatElement.prefab`
- 数据/交互与动态排版：同目录 `ServicePanel.cs`、`ChatElement.cs`、`ViewportResizer.cs`
- 素材：`Assets/OrchardUI/Resources/OrchardUI/ServiceReferenceControls.png`
- 制作配置：`Assets/OrchardUI/Editor/OrchardServiceReferencePass.cs`
- 按需加载：`Assets/OrchardUI/Runtime/OrchardServiceVisual.cs`
- 定向检查：`Assets/OrchardUI/Editor/OrchardServiceRuntimeCheck.cs`

素材由内置 image_gen 编辑模式生成，最终项目路径、参考、完整提示词与原件路径见生成记录。使用 Unity 原生 Sprite 切片，移动平台 ASTC 6×6，无 mipmap；大图通过 Resources 按需加载。修改前预制体与预览保存在 `Before/`。
