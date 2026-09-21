# 果园风格漂浮宝箱

按用户要求替换漂浮气泡中的宝箱为暖木色、金色包边与锁扣的圆润宝箱。

生产素材：`Assets/BizzaWZ/Common/BizzaGame/Z_ReplaceAssets/UI_Frame/GamePanel/Icon_Bubble.png`。保持 GUID `d8e0dab8e175190499aacf53e7e1ef4f`、fileID `21300000`、单 Sprite。生成原图为 1337×1176 RGBA，导入最大尺寸 512，不生成 mipmap，使用透明通道。保留原生成像素，不进行人工光栅重绘。

只替换该 PNG 及其导入压缩/最大尺寸设置。实际入口 `UIBizzaAAA.prefab` 的 `Safe Zone/BtnFlowTreature/Image` 仍为 133×117，外层标准 Button 仍为 200×200。两层原气泡、点击绑定、漂浮运动、显示间隔和奖励流程均未修改。

通过项目资源引用检索，原宝箱 GUID 仅由此 Prefab 引用，未发现动画换帧或其他页面引用。`Before` 留存原图、导入设置、气泡与页面的基线，`Validation/source-change-audit.json` 记录实际变更范围。

素材由内置 imagegen 工具生成，完整提示词见 `Generated/prompt.txt`，生成输出副本见 `Generated/OrchardBubbleChest.png`。原低分辨率箱子仅为含义参考，现有 HudNaturalGift 为渲染风格参考。

`Validation/static-bubble-chest-1x.png` 与 `2x.png` 是原 Prefab 子树经 Unity 隔离渲染的静态预览，分别为原尺寸与两倍放大。它们不是 AI 效果图，也不是游戏运行截图。验证不会启动/停止游戏、点击宝箱或广告、改变账号数据；不据此宣称完成奖励流程或真机测试。临时验证脚本在核查后从 Assets 移除，仅于 Validation 存档。

最终检查：两种尺寸渲染无报错，三层 Image 正常，原 RectTransform 保留，八个被核查源文件在渲染前后哈希一致。实际导入纹理为 512×450，原 GUID/fileID 解析正确。临时脚本已移除；后台 Unity 最终状态为 compilationErrors=0、compiling=false，保持用户原有 Play 状态。
