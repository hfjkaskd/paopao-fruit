# Rate Up 弹窗细节调整

工程：`C:/Projects/paopao/BizzaWZ`，Unity 2022.3.62f3。

[最新对照](comparison.html) · [验证摘要](verification-summary.json) · [素材生成记录](imagegen-prompts.json) · [素材精修记录](imagegen-polish-prompt.json)

## 修改

- 换成带两侧叶片的弧形木牌，保留可本地化的动态标题；校准英文弧度，并允许长葡语标题缩小到木牌内沿。
- 放大金币与向上箭头，恢复淡金色放射光、前后卡片的标题底色、四颗星光和卡片之间的向下箭头。
- 按参考调整两张卡片的尺寸、金币位置、金额层级及按钮位置。旧、新金额和“≈”统一使用绿色，恢复 NOW 卡片更大的兑换数字。
- 卡片使用完整透明 Sprite，避免通用九宫格压扁图案和高光；减轻遮罩并保留边角高光。
- 布局、字体、颜色、标准 Button 及图片绑定均保存到实际 `ExchangeRatePanel.prefab`。大图在页面打开时通过 Resources 加载，运行时不创建静态层级。

## 验证范围

从正式 `Assets/Game/Resources/Scenes/InitWZ.unity` 完整初始化，以生产 `UIModule.OpenPage` 和 `ExchangeRateInfo` 接口打开页面。两组临时数据用于检查四个金额刷新，不写入账户和兑换率；运行截图沿用当前币种和语言。业务脚本 `ExchangeRatePanel.cs` 未修改。

检查 852 × 1846 与 1080 × 1920：资源完成加载，文字与图片不越界，标题不超出木牌内沿，关闭和 Check 按钮中心命中，Check 进入现有 `RealWithdrawPanel`，再次打开刷新金额并可关闭。未提交提现，验证前后存档余额与最近提现时间一致。

本轮验证是 Unity 编辑器正式运行路径，未打包 Android/iOS 真机。效果图背景与现有玩法背景存在美术差异；前景对照用于审核本次还原结果，不将近似素材声称为完全像素一致。

## 文件与素材

- 页面：`Assets/BizzaWZ/Final/Real/UI/ExchangeRatePanel/ExchangeRatePanel.prefab`
- 素材：`Assets/OrchardUI/Resources/OrchardUI/RateReferenceControls.png`、`RateReferenceRadiance.png`
- 预制体配置：`Assets/OrchardUI/Editor/OrchardRateReferencePass.cs`
- 资源加载：`Assets/OrchardUI/Runtime/OrchardRateVisual.cs`
- 定向验证：`Assets/OrchardUI/Editor/OrchardRateRuntimeCheck.cs`

素材由内置 image_gen 编辑模式生成，保留生成原件，最终版本位置与完整提示词见上述记录。使用 Unity 原生 Sprite 切片，保留透明通道，Android/iOS ASTC 6×6，无 mipmap。`Before` 保存改前预制体与预览，Runtime / RuntimeShort 保存未经图片编辑的 Unity 截图和验证明细。
