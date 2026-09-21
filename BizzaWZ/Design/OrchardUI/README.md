# Orchard UI 美术锁定与交接

本次换皮以 [locked-reference.png](locked-reference.png) 为唯一视觉基准：无大树的蓝天田野背景、温暖圆润木框、奶油内板、蓝色导航、绿色主按钮、少量叶片点缀。保持轻松明亮的休闲 puzzle 气质。背景不再加入大树；金额、支付标识、按钮图标和信息层级仍需清晰可读。

工程仅为 `C:/Projects/paopao/BizzaWZ`，Unity `2022.3.62f3`。正式运行必须从 `Assets/Game/Resources/Scenes/InitWZ.unity` 进入，沿用初始化、账号、资源和提现链路。

## 美术与文字规则

| 用途 | 已配置色值 / 规则 |
| --- | --- |
| 正文、金额标签 | `#174F7D`，无描边 |
| 木牌标题、主按钮文字 | `#FFFFFF`；共用 TMP 描边 `#482E19`，宽度 `0.13`，无 Underlay |
| 奶油、锁定浅绿 | `#FFF9E7`、`#DAE6C3` |
| 奖励绿、提现金额绿 | `#12993B`、`#0D9B23` |
| 提现金额强调、渠道强调 | `#00967A`、`#007B91` |
| 次要信息 | 提现 `#69898B`；客服/奖励 `#628386` |
| 表单错误、等待提示 | `#B84C36`、`#AD741E`；倍率富文本为 `#AF741E` |
| 精灵 Tint | 正常态 `#FFFFFF`，让贴图自身的木纹、渐变与高光生效 |

木纹、按钮蓝绿渐变和叶片颜色来自图集，不再用额外的深色 Tint 覆盖。禁用卡片用浅灰绿内底，保留木框。关/开、已领取/可领取、失败/成功等状态仍需明确区分。X、返回、帮助、齿轮、音乐、震动、发送等语义图标不得被空白按钮底替换。

## 资源与制作方式

| 文件 | 用途 |
| --- | --- |
| `Design/OrchardUI/locked-reference.png` | 已确认的整体视觉参考 |
| `Assets/OrchardUI/Art/Controls.png` | 1254×1254 共用透明图集，16 个 Sprite |
| `Assets/OrchardUI/Art/Navigation.png` | 1254×1254 共用透明导航图集，返回、历史、帮助、关闭、设置、客服及三组叶片，共 9 个 Sprite |
| `Design/OrchardUI/atlas-slices.json` | Sprite 区域与九宫格边界的制作源数据 |
| `Assets/OrchardUI/Resources/OrchardUI/Backdrop.png` | 941×1672 共用田野背景 |
| `Assets/OrchardUI/Runtime/OrchardBackdrop.cs` | `Resources.LoadAsync<Sprite>("OrchardUI/Backdrop")`，复用一次请求和一份已加载背景 |
| `Assets/OrchardUI/Generated/` | 按字体生成的正文/标题材质；胜利页独有皮肤配置 |
| `Assets/OrchardUI/Editor/OrchardSkinAuthoring.cs` | 导入及统一 Prefab 作者入口 |
| `Assets/OrchardUI/Editor/OrchardCommonPass.cs` | 加载、基础弹窗、HUD、广播与引导的针对性调整 |
| `Assets/OrchardUI/Editor/OrchardServiceRewardPass.cs` | 客服、任务、奖励、每日内容、老虎机和评分 |
| `Assets/OrchardUI/Editor/OrchardWithdrawalPass.cs` | 提现与关联表单、历史、段位、待处理页面 |
| `Assets/OrchardUI/Editor/OrchardNavigationPass.cs` | 导航语义图标与标题/按钮叶片装饰，静态写入 Prefab |
| `Assets/OrchardUI/Editor/OrchardSkinValidation.cs` | 基线、应用后结构审计和预览辅助 |
| `Design/OrchardUI/build-gallery.py` | 读取已完成的预览/运行报告，生成本地 `gallery.html`，不生成或修改图片 |

16 个共用角色为 `Panel`、`Title`、`ButtonGreen`、`ButtonBlue`、`SelectedCard`、`DisabledCard`、`Card`、`Inset`、`ButtonRoundBlue`、`Badge`、`ButtonRoundCream`、`ProgressTrack`、`ProgressFill`、`ButtonDisabled`、`Input`、`SelectionRing`。圆钮等比显示；板、卡、胶囊按钮使用九宫格；原 Filled 进度类型保留。

遵循 Prefab-First：静态层级、按钮底板、图标、字号和材质由作者工具写入 Prefab/配置；运行时代码负责原有状态与交互。背景在需要的全屏页按需加载，Prefab 不强引用大背景；游戏 HUD 不增加全屏遮挡层。Editor 与 Android/iOS 使用同一运行逻辑。

三张共享贴图的导入设置为关闭 Mipmap、Bilinear、Clamp、不可读、最大尺寸 2048。Android/iPhone 上 `Controls` 与 `Backdrop` 使用 **ASTC 6×6**，新增 `Navigation` 为保留小图标边缘使用 **ASTC 4×4**，质量均为 80；桌面导入为未压缩，便于检查图集边缘。按当前像素尺寸估算，三张共享图在支持 ASTC 的设备上压缩纹理数据约 **2.84 MiB**，未包含字体、其他图片和驱动开销；实际加载峰值与帧表现待真机验证。避免每页复制一张背景或生成单独大面板图。

`WhiteWinPanel` 是现有 RawImage 特例：作者流程生成 `Assets/OrchardUI/Generated/OrchardVictoryPanelSkin.asset`，板与按钮共享图集纹理，各 RawImage 配置独立 `uvRect`。原运行脚本只更新 texture，保留 UV，不需要另复制图集或改动初始化流程。

## 覆盖范围与保留内容

[prefab-manifest.json](prefab-manifest.json) 当前列出 **56 个 Prefab**，包含页面与复用组件，并非 56 个独立页面。覆盖加载、设置、失败/胜利、游戏 HUD、货币栏、道具入口、广播、共用确认与引导；提现、支付方式、等级卡、段位、填写/确认/等待/历史；客服会话、FAQ、快捷问题；每日任务、每日奖励、兑换率、领奖、新手礼、老虎机与评分。

当前 manifest 审计范围包含 **162 个 Unity 标准 Button**。组件统计与视觉截图是不同的验收证据；最终新增/既存问题数以最后一次 `audit-current.json` 为准。

最终审计（2026-09-17 11:14 UTC）为 **0 新增错误、0 Missing Script、0 本地 fileID 断链**；仍记录了涉及 45 个不同缺失资产 GUID 的既存错误，不能视为全工程无错。完整计数、56 张静态预览和 7 套真实运行截图索引见 [validation-summary.md](validation-summary.md)。

支付 Logo、货币/国家图标、动态奖励和道具图、星级、已有品牌图、业务语义图标与核心玩法资产按角色保留。原 `GameUiWidget`、`FakeWithdrawPanel`、`RealWithdrawPanel` 的业务路径和代码绑定 Button 保留；不恢复 TEST 本地模拟提现入口，不改动历史工程。

## 应用、备份与验收

制作菜单顺序为 `Tools → Orchard UI → Import Locked Art`，再执行 `Apply Locked Skin to Manifest`。应用前关闭 Play；作者流程读取 manifest，加载现有 Prefab、配置视觉、保存资源。材质与新增子节点复用既有名称，保持重复制作可检查。

原始 56 个 Prefab 备份位于 **`Design/OrchardUI/Before/Assets/`**，按工程内路径保留。它是本轮视觉回退依据，不是启动入口。回退某页前应先比对后续修改，按对应相对路径恢复，避免覆盖之后的业务改动。

本轮完成了 Unity 编译、Prefab 应用、基线差分审计和静态预览；正式运行从 `InitWZ` 完成原初始化/账号流程后进入 `GamePlay`。具体报告时间、数值和运行截图索引见 [validation-summary.md](validation-summary.md)。原始 `audit-baseline.json` 保留作为既存问题对照。

- 结构审计仅覆盖 manifest 中的 56 个 Prefab 与 162 个标准 Button；“无新增错误”不等于全工程没有既存资源问题。
- 静态图用于检查布局、色彩和资源呈现，保留 Prefab 默认文案与状态，不证明所有业务状态都已运行。
- 正式入口已观察 HUD、设置、金币提现、现金提现、FAQ、提现记录、客服页面；使用原 `UIModule` 打开/关闭页面。未点击支付提交、广告或客服发送，也未重置存档。
- 未进行 Android/iOS 真机测试；ASTC 实际显示、设备比例差异、加载峰值、纹理内存和切页性能仍需设备验收。表单错误态、所有账户/国家组合和业务极端状态不在本轮运行观察范围内。

应用与验收证据为 `apply-report.json`、`audit-current.json`、`preview-report.json` 和 `RuntimeCaptures` 中的 PNG/JSON。页面专项细节见 [withdrawal-notes.md](withdrawal-notes.md)、[service-reward-notes.md](service-reward-notes.md)；基础页以当前 `OrchardCommonPass.cs` 为准，其中 UIPropEntry 已保留原 BG 路径、取消重父层级。

## 画廊与运行诊断

[gallery.html](gallery.html) 已包含最终的 **56 张静态预览与 7 张实际运行截图，共 63 张**。后续重新制作并完成预览后，在工程根目录运行：

```powershell
python -X utf8 Design/OrchardUI/build-gallery.py
```

脚本读取完整 `preview-report.json`、56 项 manifest 及 `RuntimeCaptures` 中已有 PNG/JSON，生成 [gallery.html](gallery.html)。支持全部、实际运行、静态预览筛选及页面名搜索，缩略图直接打开本次生成的原图；未生成替代图片。预览忙碌、报告不完整、图片缺失或正在改写时会停止构建。脚本使用 Python 标准库，无额外依赖。

- **静态预览**：`Previews/*.png` 通过独立 PreviewScene 的真实 UI 组件渲染，业务脚本与动画禁用；按需背景只在临时预览实例上绑定。默认文字、金额、选中态或隐藏内容可能与真实运行不同，不能用于判断账号和支付数据。
- **实际运行**：`RuntimeCaptures/runtime-*.png` 是已从正式入口启动后的 Game 画面。同名 JSON 记录启用层级下的 Image 资源/位置、TMP 文字/字体和 Button 状态，用于定位重复图标、遗漏资源或交互状态。`clickable` 是组件与 CanvasGroup 状态估计，不是实际点击或遮挡命中测试。
- **运行页面标识**：画廊从同名 JSON 的已打开页面及层级顺序识别主要页面；没有 JSON 的旧截图明确显示“页面未识别”，不根据文件名猜测业务状态。

验证命令写入 `Design/OrchardUI/validation.command`：`audit`、`preview`、`capture-runtime`；`open:` / `close:` 仅支持 `RealWithdrawPanel`、`FakeWithdrawPanel`、`PausePanel`、`FAQPanel`、`WithdrawHistory`、`ServicePanel` 六个白名单，且必须处于正式初始化完成的 Play 状态。打开/关闭由原 `UIModule` 执行，不点击提现、广告或客服发送操作，不重置存档。每两秒更新的 `state.json` 用于观察进度；各流程由操作方统一编排，避免并发写入命令。
