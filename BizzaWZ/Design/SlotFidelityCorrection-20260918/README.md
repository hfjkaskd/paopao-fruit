# 已确认效果图的工程还原修正

本轮已实际修改 BizzaWZ 的帮助页和奖励领取弹窗。用户再次要求继续还原，并重发已确认的帮助页效果图；无需再次选择方向。最新附件与 ../SlotRestyleImplementation-20260918/02-approved-help.png 的 SHA-256 一致。

## 帮助页

生产文件：Assets/BizzaWZ/Final/Real/UI/SlotsPanel/SlotFQAPanel/SlotFQAPanel.prefab。

- 用既有背景 Image 承载完整机柜素材：灯牌、连续木框、内嵌奶油奖励板、叶藤、金边、厚底座及脚座。原来的独立机身保留节点及组件，取消旧机身绘制。原奖励板 Image 使用同纹理的对应裁片，以完全相同的纹理比例和位置遮挡后面的老虎机图案，不再叠加不同样式的框。
- 调整既有 RectTransform 与 LayoutGroup，使六行奖励、页脚与确认按钮落入参考图对应区域；没有增删或重排节点。
- 六行内容保持原顺序及奖励含义，全部原有文字、本地化、动态图标、按钮绑定保留。确认勾更换为无多余透明留白的 Sprite，并恢复居中和大小。

布局以正式 GameCanvas 的高度适配参数计算：参考分辨率 1080×2360，在 1080×1920 时缩放为 1920/2360。目标数据见 help-layout-targets.json，逐字段记录见 help-visual-changes.json，修正脚本为 apply-help-fidelity.py。

## 奖励领取弹窗

生产文件：Assets/BizzaWZ/Final/Real/UI/SlotsPanel/SlotPanel/SlotPanel.prefab，仅修改 Content/GetRewadPanel 的视觉与布局。

把奖图、动态金额和确认按钮纳入金边奶油面板，按钮使用深绿样式，并把错误的按钮底图图标替换成大勾。六种奖励图和奖励领取流程均保留。详见 reward-popup-report.md、reward-popup-changes.json 和 reward-popup-validation.json。

## 素材与来源

- Art/SlotHelpCabinetComplete.png：内置 image_gen 生成的完整空白机柜，已接入工程；这是生产美术素材，不是 Unity 运行截图。
- Art/SlotHelpCabinetComplete.prompt.txt：生成所用提示词。Art/provenance.json：来源与模式。
- Art/help-import.json：完整背景及奖励板遮挡裁片的 Sprite 导入区域及哈希；两个 Sprite 共享同一纹理。Art/SlotFidelityCheck.import.json：复用勾图的导入记录。
- PNG 均保持生成器原始像素，使用 Sprite 导入区域去掉透明空边；未把文字、金额或奖励图烘焙到背景。
- Before/：本轮修改前的 Prefab 备份。不会自动还原备份或覆盖用户后续修改。

## 验证边界

独立审计记录在 Validation/：Slots 范围内 13 个 Prefab、1155 个对象的结构检查通过，246 处变化均为允许的视觉/布局字段；层级、Button、奖励字段、本地化保留。几何和新 Sprite 检查通过。18 个格子内部图标保持原尺寸，实际 alpha 内容距离格子外沿至少约 11 像素。全仓 guard 检测到其他任务的 Loading、Navigation、提现页并发变化，报告保留 FAIL 及完整路径；没有回滚这些变化，也不声称全仓源码完全未变。字段审计通过不等于视觉或功能运行验收。

Validation/help-layer-check-static.png 是 Unity 临时 Prefab 的中间静态绘制，只用于检查奖励板遮挡；它生成于最后一次图标边距修正之前，使用序列化英文文本、未运行地区资源绑定，而且既有预览工具未应用正式 GameCanvas 的缩放参数，顶部裁切不代表正式运行布局，不能作为最终验收图。完整比例另由 help-geometry.json 按真实 GameCanvas 参数核对。

导入后的 Unity 只读审计见 Validation/unity-imported-asset-audit.json：56 个 Prefab 中没有新增资源引用问题，已有 239 条问题保留在报告内，未掩盖为全工程零错误。

Unity 导入日志已记录两个 Prefab 和两个新 Sprite 的成功导入，摘录见 Validation/unity-import-evidence.txt。本轮没有取得改后页面的 Unity 运行截图，也没有触发广告、转动或领取奖励，因此不声称已完成运行验收或像素级一致。未把 AI 参考图作为工程截图交付。

全程通过后台文件修改与读取完成，没有操作鼠标、键盘、切换窗口或启动/停止 Play。
