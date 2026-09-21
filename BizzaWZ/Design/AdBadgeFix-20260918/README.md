# 道具广告标签修复

实际工程：`C:/Projects/paopao/BizzaWZ`。

生产改动仅为 `Assets/FruitsHarvest/Resources/Original/res/local/coreplay/CorePlayUI.prefab` 中 Undo、Magic、Shuffle、AddOne 四处广告标签的视觉配置。

- 复用已有 Controls/Card 九宫格图片，替换被拉成鼓起椭圆的橙金色底图，使用薄木边奶油底。
- 三个普通道具标签高度 87.3716 → 60，宽 156.0531；AddOne 标签高度 55 → 44，宽 102。标签中心与主道具按钮位置保留。
- 现有播放图标保持比例，与单行深蓝色 AD 对齐；关闭重复的父级阴影 TMP，保留前景文字和全部节点。
- 所有节点、组件、主按钮绑定、广告显隐条件、广告触发及道具使用流程保持。没有修改运行时代码，也没有改变数量/锁定状态逻辑。

`Before/CorePlayUI.prefab` 为本次完整修改前备份；`apply_badge_fix.py` 与 `patch-report.json` 记录精确视觉改动与结构核验。

## 验证结果

`before-*.png`、`after-*.png` 都是 Unity 隔离 PreviewScene 渲染的 **Prefab 静态预览**，不是 AI 效果图或运行截图。为便于看清，图中标签放大 3 倍；实际按钮尺寸以 Prefab 为准。截图中的黄色底色仅为预览背景。

四处标签在修改前均有两层文字；修改后均为两张 Image（底图、播放图标）和一层 AD 文字，无文本溢出。Unity 导入时未记录编译错误。未启动游戏，未点击广告，未验证实际 SDK 回调。

一次性预览脚本归档于 `Validation/`，已从 Assets 移出。本轮未操作桌面窗口、鼠标或键盘。
