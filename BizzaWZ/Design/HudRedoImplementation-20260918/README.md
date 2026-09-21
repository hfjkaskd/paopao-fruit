# 已确认方案：主界面木质 HUD 实施

用户已确认 `../HudRedoPreview-20260918/02-ai-natural-wood-preview.png` 的方向。本目录记录本轮在 BizzaWZ 工程内的落地与验收。

## 本轮变更

- 顶部原整片奶油底设为透明，露出已有天空；关卡采用木牌与独立白色动态数字，两组金额采用奶油木框与叶绿按钮；设置按钮采用木圆盘和奶油齿轮。
- 礼包取消厚圆圈，改为奶油纸盒、珊瑚橙丝带和绿叶。复用原 240×240 外层 Image 呈现礼盒，原内层 Image 保留并透明。原按钮的按压缩放目标同步指向实际显示图，缩放比例、按钮根节点、点击范围与业务事件保持不变。
- 托盘按钮背景使用独立木块，锁为另一个独立铜锁 Image，保留原锁定状态控制。

保持全部现有 RectTransform、节点顺序、组件数量、动态金额、奖励含义、本地化和运行时代码。未修改未标红的 777、进度条、底部三个道具、场景，以及每日礼包弹窗。

## 素材

使用内置 image_gen，以确认图作风格参考生成 6 张透明 PNG：

- `Assets/OrchardUI/Art/HudNaturalWoodTile.png`
- `Assets/OrchardUI/Art/HudNaturalCounter.png`
- `Assets/OrchardUI/Art/HudNaturalSettings.png`
- `Assets/OrchardUI/Art/HudNaturalGreenButton.png`
- `Assets/OrchardUI/Art/HudNaturalGift.png`
- `Assets/OrchardUI/Art/HudCopperLock.png`

PNG 与生成输出像素一致；透明留白通过 Unity Sprite 导入矩形处理，木框和按钮配置九宫格，齿轮、礼包与锁等比显示。纹理最大导入尺寸 512（铜锁 256），关闭 Mipmap 和 Read/Write。

完整提示词：`Art/prompts.json`、`Art/Gift/prompt.txt`、`Art/Lock/prompt.txt`。各生成原图与导入记录也在 `Art/`。

## 变更记录

按顺序：`visual-changes.json` → `visual-refinement.json` → `visual-feedback.json`。

`Before/` 是本轮基线；`final-integrity.json` 为独立结构与引用审查。脚本用于记录本轮修改过程，不应在后续手动编辑后直接重跑。

## 截图类型

`01-runtime-first-pass.png/.json` 是第一轮 Unity 实际运行截图与只读组件快照，尚未包含礼包显示范围、金额框圆角的最后微调。

确认图为 AI 效果图；所有本目录标为 runtime 的 PNG 都由 Unity 运行帧末捕获，不是 AI 效果图或静态 Prefab 预览。

`02-runtime-final.png/.json` 为最终主界面；`03-runtime-settings`、`04-runtime-gift-entry` 为实际点击后的页面截图与组件快照。`05-runtime-after-lock` 是点击锁按钮后恢复的主界面；短时锁定提示在 Windows UI 即时截图中已确认，但保存的后续帧末 PNG 中提示已消失。

## 验收结果

正式从 InitWZ 启动进入 GamePlay，1080×1920 运行画面检查通过。设置入口、每日礼包入口和托盘锁按钮均经实际点击验证，原页面或原本地化提示正常响应；未执行广告、奖励领取、抽奖或提现。Unity 保持游戏运行状态。本轮没有 Android/iOS 真机执行，未强改账号或解锁状态。详见 `runtime-checks.json`。

独立审查通过：4 个修改 Prefab、2 个保护 Prefab，共 1203 个对象；295 个 RectTransform 保持完全一致；1358 个 C# 文件和 16 个既有素材/Meta 哈希一致。三份日志 30 项视觉操作可精确还原基线。按钮的唯一字段例外是礼包 `scaleTarget` 按压视觉引用，点击事件、点击范围和按压比例均保留。

审查另记录了原有 Animation 组件的缺失引用（GUID `1497b04c3b9879042bab62ffb8dafc41`），本轮与基线一致，未修改；礼包现有父节点呼吸 Tween 保留。
