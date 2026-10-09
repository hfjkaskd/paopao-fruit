# 余额提醒页清晰度调整

工程：`C:/Projects/paopao/BizzaWZ`，Unity 2022.3.62f3。

[最新对照](comparison.html) · [验证摘要](verification-summary.json) · [素材完整提示词](imagegen-prompt.json)

## 调整

- 替换支付方式卡片的背景，恢复完整金色轮廓、边角反光与轻微底部阴影，避免原有通用切片将上缘高光压成泛白的细线。
- 恢复金额框的绿色外沿、内层反光和渐变，替换无边框的平面薄荷底色。
- 增加提示文字与支付卡片之间的净空，按原图校准金额、标题、说明文字和关闭按钮比例；恢复带金色亮点的粉色奖励图标。
- 将金额区静态标题绑定为参考文案“Valor para saque”，避免旧语言标签在运行时覆盖为另一句话；金额数据本身仍由原业务刷新。
- 图片绑定、字体尺寸、颜色和布局保存于实际 `DailyWithdrawPanel.prefab`。大图在打开页面时按 Resources 路径加载，无运行时创建静态 UI 的代码。

## 实际业务保持

本页业务仍使用 `DailyWithdrawPanel.cs` 的服务端数据刷新。余额、提现金额、平台列表和“查看提现”进入 `RealWithdrawPanel` 的路径未修改。关闭及查看按钮保持原有标准 Button 与代码事件绑定。

预览中的 `0,95` 和 `≈ R$0,03` 仅是临时视觉示例；运行截图显示当前账号实际数据。测试从正式 `Assets/Game/Resources/Scenes/InitWZ.unity` 初始化，检查 852 × 1846 与 1080 × 1920 的文字、图片、关闭、进入正式提现页和重新打开。未提交提现。

验证仅覆盖 Unity 编辑器正式运行路径，本轮未进行 Android/iOS 真机打包；不将视觉调整表述为全图像素完全一致。结果见 Runtime / RuntimeShort 目录的 `verification.json`。

## 文件

- 页面：`Assets/BizzaWZ/Final/Real/UI/DailyWithdrawPanel/DailyWithdrawPanel.prefab`
- 新素材：`Assets/OrchardUI/Resources/OrchardUI/ReminderReferenceControls.png`
- 编辑器配置：`Assets/OrchardUI/Editor/OrchardReminderReferencePass.cs`
- 运行时加载：`Assets/OrchardUI/Runtime/OrchardReminderVisual.cs`
- 定向验证：`Assets/OrchardUI/Editor/OrchardReminderRuntimeCheck.cs`

素材由内置 image_gen 编辑模式生成，完整参考路径、提示词及生成文件位置见 `imagegen-prompt.json`。保留透明通道，以 Unity 原生 Sprite 切片导入；Android/iOS 使用 ASTC 6×6，不生成 mipmap。`Before` 保留修改前预览和预制体备份，验证截图未做图像编辑。
