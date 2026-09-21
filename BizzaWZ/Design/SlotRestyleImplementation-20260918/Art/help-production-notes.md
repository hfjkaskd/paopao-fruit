# 帮助页独立素材

这些是 built-in imagegen 生成的生产素材候选，不是 Unity 运行截图。已视觉检查；所有 PNG 均直接复制输出，没有像素编辑。仅写入 Design，没有修改 Assets、Prefab、C# 或 Unity。

- `SlotEmeraldHelpBody.png`：998×1575 RGBA；alpha64 边界 `(7,2)-(992,1534)`。无内容的下半部深绿柜身、蜂蜜木侧柱、细黄铜边，边缘叶饰，不含表格、文案、奖励或按钮。
- `SlotEmeraldMarquee.png`：V2，1596×985 RGBA；alpha64 边界 `(5,20)-(1591,920)`。星形、两片叶子、九颗暖灯，上下余量较少；为紧凑套入原 994×614 区域，拱顶较高。
- `SlotEmeraldMarquee-v1.png`：V1，1596×985 RGBA；alpha64 边界 `(8,111)-(1588,860)`。拱顶较扁、接近参考冠部本身；未选作默认，因为整体透明留白较多。Root 可以通过 Sprite rect 和 preserveAspect 适配，保持原 Rect。

完整初始提示词见 `help-production-prompts.json`；第二版定向修改提示词和原始生成路径见 `help-production-provenance.json`。冠部两版都保留原始 alpha，可按实际背景与机身衔接选择。
