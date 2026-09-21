# 本轮独立验证

本目录由独立审计代理保存。基线是在本轮两个页面开始修改前生成的，区别于此前还原工作的基线。

- `baseline.json`：13 个 Slots Prefab 基线、全工程 200 个 Prefab 与 1358 个 C# 文件的 SHA-256。
- `Before/`：13 个 Slots Prefab 及其 meta 的不可变副本。
- `run_validation.py`：只读生产文件、只在本目录写报告的独立校验器。
- `final-integrity.json`：最近一次校验结果和逐项差异；以文件内时间为准。
- `new-sprite-integrity.json`：完整机柜、同纹理奖励板裁片、确认勾的 GUID、独立子 Sprite ID、导入边界与原始 PNG 一致性。
- `help-geometry.json`：实际 RectTransform / VerticalLayoutGroup 计算出的像素矩形、内容相交检查、同纹理裁片映射。
- `symbol-insets.json`：18 个动态图标回到原尺寸后的实际 Sprite alpha 边界与格子留白核对。

仅 `SlotPanel/SlotPanel.prefab` 和 `SlotFQAPanel/SlotFQAPanel.prefab` 允许变化。允许 Image、TMP 的外观参数以及现有 RectTransform / LayoutGroup 的尺寸、位置、间距。所有序列化对象 ID、顺序、类别、GameObject 节点及组件列表、父子关系、Button 配置及事件、CanvasGroup、Spine 动画字段、文本内容、本地化绑定和业务字段必须保留。嵌套 Prefab override 必须能解析回基线源组件，且只允许对应的视觉参数。

运行：

```powershell
python BizzaWZ/Design/SlotFidelityCorrection-20260918/Validation/run_validation.py
python BizzaWZ/Design/SlotFidelityCorrection-20260918/Validation/audit_new_sprites.py
python BizzaWZ/Design/SlotFidelityCorrection-20260918/Validation/audit_help_geometry.py
python BizzaWZ/Design/SlotFidelityCorrection-20260918/Validation/audit_symbol_insets.py
```

这是结构与资源源码审计，不等于 Unity 运行验收，也不表示像素级还原。审计不启动、停止或操作 Unity，不控制鼠标、键盘或窗口。

## 最终独立检查结果

- Slots 范围结构通过：13 个 Prefab、1155 个序列化对象，246 处允许的外观/局部布局差异。节点、组件、按钮绑定、动态文本、本地化及奖励/动画字段保留。
- 资源检查通过：两个新 PNG 与生成源逐字节一致；完整机柜与奖励板裁片分别使用 21300000、21300002，两者 spriteID 和文件 ID 均独立，名称映射正确。
- 几何检查通过：1080×1920 下机柜、奖励表、6 行内容、说明与确认按钮命中目标；84px 符号格配 95px 行距，等号/奖励/描述没有矩形相交。原奖励板 Image 重用同纹理裁片，其屏幕映射误差小于 0.000001px，并在嵌套旧机身之后绘制。
- 18 个图标已恢复原 Rect 尺寸。对实际原图 alpha > 24 区域独立核对后，格子外边缘到可见图标的最小距离为 11.1864px，通过保守的 10px 边缘留白要求；没有调整图标含义、引用或动态替换逻辑。
- 全工程严格 guard 没有通过：审计期间还观察到 Loading、提现页及 Navigation 的范围外变化，完整路径保留于 `final-integrity.json`。不能据此声称全工程或全部 1358 个原有 C# 都未改变；本审计没有推断这些并发修改的作者，没有修改或回滚它们。

## 现有静态预览能力（只读检查）

`Assets/OrchardUI/Editor/OrchardSkinValidation.cs` 的 `PreviewPrefab` 第 563 行明确拒绝 Play 模式；`PreviewAll` 同样要求 Edit 模式。当前实现没有可以在 Play 中调用的隔离静态预览入口。`capture-runtime` 只读当前运行实例，不能证明新改的 Prefab 已应用到缓存页面。没有为了验证而修改或调用这些流程。
