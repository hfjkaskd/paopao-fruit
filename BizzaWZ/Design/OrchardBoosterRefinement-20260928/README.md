# 道具补充弹窗精修

本轮只精修 `AddPropPanel`。背景面板、弧形木牌、薄边绿球、放射光、三颗星芒、浅色次数胶囊均按 `07-get-booster.png` 重新提取；领取按钮保留参考中的影片图标，关闭按钮恢复蓝底白色 X。

[查看英文设计对照预览](../OrchardImplementation-20260928/Previews/get-booster.png) · [全部界面对照](../OrchardAllScreens-20260928/comparison.html#get-booster)

标题使用动态弧形文字，校正了字面尺寸和基线；道具名称、说明、真实次数与按钮文案可正常刷新。实际运行中原先误用的“提现次数”和 `Gratis` 也已替换为道具使用次数及看广告领取提示。三种道具分别显示自己的名称、图标和说明。

静态结构、美术位置、字体大小、弧度及英文／葡萄牙文文案均保存在实际 [AddPropPanel.prefab](C:/Projects/paopao/BizzaWZ/Assets/BizzaWZ/Final/Real/UI/AddPropPanel/AddPropPanel.prefab)。运行时仅加载资源和刷新内容，沿用原广告成功后增加道具的业务回调，广告位使用稳定字符串。

新增美术通过内置 `image_gen.imagegen` 编辑参考图生成，使用真实透明背景，最终文件为：

- [BoosterReferencePlate.png](C:/Projects/paopao/BizzaWZ/Assets/OrchardUI/Resources/OrchardUI/BoosterReferencePlate.png)：非交互装饰层。
- [BoosterReferenceControls.png](C:/Projects/paopao/BizzaWZ/Assets/OrchardUI/Resources/OrchardUI/BoosterReferenceControls.png)：领取按钮、关闭按钮与撤销图标，分别在 Unity 中切片。
- [完整提示词](imagegen-prompts.json)：记录内置模式、输入参考和两次实际提示词。

两张图按页面显示时通过 Resources 异步加载；移动端采用 ASTC 6×6、无 mipmap。按钮图形与标准 Button 位于相同节点，保留代码事件绑定。

## 验证范围

从正式 `InitWZ` 场景完整初始化，在 852×1846 和 1080×1920 两种尺寸分别打开撤销、洗牌、魔杖三种状态：

- 检查资源绑定、名称、图标、说明、真实使用次数、领取文案及文字溢出。
- 检查关闭与领取 Button 的实际 EventSystem 命中，执行领取按钮按下／释放及关闭按钮点击。
- 检查关闭返回游戏后，道具数量和使用次数保持原值。

[长屏检查](Runtime/verification.json) · [短屏检查](RuntimeShort/verification.json)

运行截图采用当前葡萄牙语账号；英文对图数据仅用于隔离预制体副本。背景是当前真实游戏关卡，因此树形、水果及 HUD 与设计示例不同。未提交广告点击、发放道具奖励或执行真机验证。两份报告中记录的 `开启测试设备False` 来自已有 SDK 的 `Debug.LogError` 状态日志。

## 相关实现

- `Assets/OrchardUI/Editor/OrchardBoosterReferencePass.cs`：仅此页面的制作参数和 Sprite 切片。
- `Assets/OrchardUI/Runtime/OrchardBoosterVisual.cs`：按需资源加载、三种道具与次数文案刷新。
- `Assets/OrchardUI/Runtime/OrchardArchedText.cs`：TMP 重建时应用 Prefab 配置的标题弧度。
- `Assets/OrchardUI/Editor/OrchardBoosterRuntimeCheck.cs`：两种尺寸下的三种状态检查。

修改前的该页面预制体、业务脚本和预览保留在 `Before/`。
