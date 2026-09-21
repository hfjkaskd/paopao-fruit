# 主界面顶部栏调整

本轮只调整 GameUiWidget 内 CurrencyBar 实例的视觉和布局覆盖，并新增无文字关卡底图。共享 CurrencyBar、RealGamePanel、GameCanvas 和原有业务脚本保持不变。

- 同一行保留关卡、金币、钞票、兑换/提现和设置，修正负间距，缩小图标与按钮装饰，增加组间留白。
- 关卡改为奶油色细木边叶芽标牌，原动态数字继续显示，并适配四位数。
- 现有布局组不再把余额动画缩放计入排列宽度；原动画保留。
- 原提现按钮点击范围收回至对应卡片，避免覆盖设置。
- 长兑换金额允许字号自动缩至12；正常示例 R$0,00 实际字号22.55不变。

验收采用真实 Prefab 的 Unity 静态渲染：1080×1920、1080×2340，普通/长数字，静止/1.25倍动画极值共8组。字形边界、按钮与分组交叠均通过。静态样例不是运行截图；它没有运行账号、本地化或货币切换业务，因此默认钞票颜色不代表巴西运行态。

- 常规静态预览：`Previews/final/static-hud-1080x1920-sample-rest.png`
- 长数字静态预览：`Previews/final/static-hud-1080x1920-long-rest.png`
- 独立渲染验收：`Previews/final/validation-summary.json`
- 结构与绑定核对：`independent-validation.json`
- 原始备份：`Before/`
- 具体变更：`applied-changes.json`

关卡素材使用内置 image_gen 生成，无文字，未修改生成像素，仅在 Sprite 导入参数中裁去透明边缘。最终文件 `Assets/OrchardUI/Art/HudLevelCreamLeaf.png`；完整提示词见 `Art/generation-prompt.txt`，来源与GUID见 `Art/import.json`。

所有工作均通过后台文件修改和隔离的 PreviewScene 完成；没有操纵鼠标、键盘、窗口、Play开关或游戏按钮。
