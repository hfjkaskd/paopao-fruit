# 提现页云朵星月主题 — 待确认的 AI 视觉预览

本轮仅制作单页效果图，尚未将本方案实施到 Unity。唯一工程：`C:/Projects/paopao/BizzaWZ`，Unity 2022.3.62f3。

## 文件身份

- `01-current-unity-runtime.png`：本轮从正式 `InitWZ` 启动、完成框架初始化后，通过现有 `UIModule` 打开的 `RealWithdrawPanel` 的 Unity 运行截图。是换皮前当前工程的布局依据，并非本方案实施结果。
- `02-style-reference.jpg`：用户提供的美术参考图，只借用配色、质感和主题氛围。
- `03-ai-cloud-preview.png`：内置 image_gen 生成的 AI 效果图，供方向确认，不是 Prefab 静态渲染或 Unity 运行截图，不可作为整体贴图导入。
- `prompt.md`：本轮完整生成提示词。

工程已有的 `Design/OrchardUI/Previews/37-RealWithdrawPanel.png` 实际为纯色空白，本轮未采用它作为有效截图或布局依据。

## 已核对的工程结构

主 Prefab：`Assets/BizzaWZ/Final/Real/UI/RealWithdrawalPanl/RealWithdrawPanel.prefab`。以当前实际 Prefab 为固定骨架，保留已有田园换皮后的节点和位置，本轮不恢复或重排历史结构。

- 顶部标题底板及返回、历史、FAQ 三个按钮。
- 原 `ScrollRect` 与主信息区：余额、已过关数、汇率、折现金额、支付方式、说明文字、提现按钮、按钮下提示。
- 当前运行数据生成的 PagBank / pix 两个渠道和六张双列档位卡。
- 右侧现有客服入口和底部完成金额提示。
- 金额、等级、倍率、提示及标题沿用现有动态文本与本地化；效果图文字只是当前运行状态的示例。

## 运行语义核对

运行配置来自 `Assets/StreamingAssets/ChannelConfig.bytes`，本次实际 `singleCurrencyMode=false`。所以这张页面的两处货币图标保留金币，不能只根据独立的 `Oversea.asset` 判断成钞票。其他页面原为钞票奖励的，仍须保留钞票语义。

运行截图六档分别为 1 / 50 / 120 / 250 / 500 / 1000，倍率为 1.0X / 1.2X / 1.4X / 1.6X / 1.8X / 2.0X；最低提现金额示例为 R$0,01。预览使用这些当前值，未照抄参考图的档位或门槛。实际数量和值继续由现有服务器响应决定。

支付渠道 `WithdrawWay/Frame` 是原渠道 Logo，不能当普通背景替换；品牌标识继续使用现有配置与素材。

## 确认后的实施范围

1. 将低对比蓝紫云朵背景、珍珠白淡紫框体、蓝色按钮、高光边缘、选中和锁定状态美术映射到现有 Image / Sprite。所有装饰画进原背景、标题底板或边框素材，并保留原外接尺寸、透明留白和九宫格边界。
2. 原 RectTransform、层级、Button 数量、交互绑定、ScrollRect、动态内容与经济逻辑保持不变。无新增装饰节点、实时模糊或粒子层。
3. 主按钮的 `normalSprite` / `canWithdrawSprite` 及富文本颜色需同步到现有序列化样式字段，避免运行时恢复旧色。保留所有业务状态差异。
4. 单页先实施并通过正式入口检查视觉与按钮功能，再扩展其他页面。AI 图不能替代运行验收。

不重跑已有 `OrchardWithdrawalPass`，因为该工具会改变尺寸、布局并添加节点，与本轮固定骨架要求不符。

本轮未改换皮用的生产 Prefab、脚本、素材或配置；新增交付仅在 Design 目录。运行截图通过项目已有的截图工具取得。
