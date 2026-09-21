# 主界面四处视觉修正（2026-09-18）

## 结果

- 顶部 HUD：原有厚橙木横条改为奶油底、细木边；关卡牌改为奶油木框；金额底框简化，两个绿色按钮采用九宫格以保留边缘比例。
- 托盘扩展按钮：将被纵向拉伸的横向蓝胶囊改为现有木框卡片，复用相邻道具的金属锁图案并居中；原有 Button 和锁定流程保留。
- 幸运入口：粉色珠宝 777 改为木质三连奶油牌、绿色 777 与少量叶片，保留抽奖含义。进度轨道边框减薄、填充内缩至 202×24 并居中，动态文字改为白色细描边。
- 每日奖励：入口及对应每日任务弹窗共用新的木质礼盒、绿色丝带；入口底圆改为奶油色。奖励内容与入口含义保持不变。

所有改动位于现有 Prefab 的视觉字段。未增加或删除节点、组件或按钮，未改变按钮点击区域、动态文本来源、奖励、进度计算或经济逻辑。

## 验证与边界

独立审查 `final-integrity.json`：6 个 Prefab、1203 个序列化对象，40 项视觉字段改动全部与日志相符；层级、组件、Button、业务及本地化保持一致；1358 个 C# 文件哈希完全一致，27 项嵌套 Prefab 覆盖与继承检查通过。

运行验证从正式 `Assets/Game/Resources/Scenes/InitWZ.unity` 启动，完成框架初始化后进入 GamePlay：

- 1080×1920 实际画面确认四处修改生效，动态进度显示 4/5。
- 点击 777 入口，正常进入原 SlotPanel 并返回。
- 点击每日礼包入口，正常进入原 DailyMissionPanel 并关闭。
- 点击托盘锁按钮，显示原本地化锁定提示 `Os itens ainda não foram desbloqueados.`。
- 没有触发抽奖、广告、奖励领取或提现。本轮没有修改进度或账号数据来模拟不同状态。

## 素材来源与截图类型

`01-runtime-before.png/.json` 为修改前 Unity 实际运行记录；`02-runtime-hud.png/.json` 为修改后 Unity 实际运行记录；`03-runtime-daily-gift.png/.json` 为重新从 InitWZ 启动后确认每日任务弹窗使用新礼盒的实际运行记录。运行截图由 Unity 帧末捕获，不是 AI 效果图或静态 Prefab 预览。

两个新图标使用内置 image_gen 生成，并已保存到工程：

- `Assets/OrchardUI/Art/HudLucky777.png`
- `Assets/OrchardUI/Art/HudGift.png`

原始生成图、最终透明 PNG、完整提示词及透明通道检查记录在 `Art/`；提示词为 `Art/prompts.json`，777 的最终紧凑版修订提示词为 `Art/lucky-refinement-prompt.txt`。PNG 像素与生成输出保持一致；777 仅通过 Unity Sprite 导入矩形去除多余透明留白。两张导入最大尺寸均为 512，关闭 Mipmap。

## 备份与修改记录

`Before/Assets/...` 保存本轮改动前的完整 Prefab。`hud-changes.json`、`lock-changes.json`、`progress-changes.json`、`icon-changes.json`、`daily-gift-changes.json` 记录本轮改动。脚本仅用于复现本轮修改，后续手动修改后不要直接重跑。
