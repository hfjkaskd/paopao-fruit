# 全界面效果图 · Premium Orchard Puzzle

用户目标：参考现有游戏以及用户提供的 Withdrawal 效果图，为其他现有界面制作高品质、轻松休闲、接近 App Store 精品休闲游戏的视觉效果图。

## 范围

这是视觉概念交付，不改 Unity 运行代码、Prefab、业务规则和账号流程。只以当前 BizzaWZ 工程为依据。主提现页使用用户原图作为视觉锚点；其他完整页面、弹窗和关键状态逐张输出。基础组件与重复列表条目不各自冒充完整页面。

## 统一风格

- 竖屏完整 UI，优先 9:19.5，图中不要手机外壳或画板外标题。
- 阳光果园：清透天蓝、鲜活叶绿、蜂蜜金、奶油白。木质只用于标题与少量结构，避免满屏厚重木框。
- 精品 2.5D 手绘卡通，圆润体积、柔和环境遮蔽、干净边缘、克制高光；保留参考图的精致程度，避免廉价塑料、过曝霓虹和写实纹理。
- 白色粗圆标题搭配深棕描边；正文深棕、字号清晰、空间充足。主操作绿色，次操作蓝色，奖励/进度橙金，禁用状态柔和灰蓝。
- 背景装饰压低对比度，苹果和树叶只适量在边角；不能让装饰叶片遮住文字、按钮或数值。
- 弹窗背后保留可辨认的摘果游戏场景并适度暗化，面板高亮清晰；不使用纯黑背景。
- 内容使用与参考图一致的英文。金额、日期、计数用于展示层级，仅为效果图示例，实际落地使用现有配置。
- 不新造关卡地图、商城、排行榜等当前不存在的业务入口。游戏本体保留果树、水果、木质收集槽，不改成方格三消或 UI 卡牌棋盘。

## ImageGen common prompt

Use case: ui-mockup. Create a new, complete, premium casual orchard puzzle mobile interface by transforming the supplied Withdrawal style reference into the requested different screen. The reference is a VISUAL STYLE reference, not the target screen content. Preserve its sunny orchard palette, warm small wooden header plaques, softly rounded ivory panels with refined honey-gold bevels, chocolate brown rounded typography, juicy leaf-green main buttons, sky-blue secondary controls, crisp professionally painted 2.5D icon art and soft dimensional lighting. Aim for an exceptionally polished App Store featured casual puzzle game. Keep the screen calm, readable and spacious with precise aligned hierarchy. Portrait 9:19.5, full bleed, one screen per image, high resolution. No phone frame, no multi-screen collage, no external captions, no watermark. No overdecorated leaves on controls, no neon bloom, no dark casino aesthetic, no photographic objects. UI labels in English as specified. Any source screenshot is STRUCTURE/CONTENT reference only: improve its cramped typography and legacy UI while retaining the underlying real feature. Popup screens show a softly dimmed recognizable fruit-tree harvesting playfield behind, with a wooden collection tray near bottom; never invent a square match-3 grid.

## 交付

使用内置 image_gen 工具。每张效果图的完整生成提示词保存在 prompts/，最终 PNG 保存在 images/。生成结果用于美术与产品评审，不代表 Unity 已完成实现。
