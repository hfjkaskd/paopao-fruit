# Orchard UI 换皮验收记录

记录日期：2026-09-17。工程：`C:/Projects/paopao/BizzaWZ`；Unity：`2022.3.62f3`。

本轮已把锁定的无大树田野背景、木框奶油面板、蓝色导航和绿色主按钮应用到 manifest 中的 **56 个 Prefab**。范围包含独立页面及复用组件，**不是 56 个独立页面**。静态结构审计统计 **162 个 Unity 标准 Button**。

## 最终应用与静态证据

| 证据 | 记录时间（UTC） | 结果与范围 |
| --- | --- | --- |
| [apply-report.json](apply-report.json) | 11:14:13.371 | 56 项应用记录，0 条应用异常 |
| [authoring-result.txt](authoring-result.txt) | 11:14:15.275 | `SUCCESS apply` |
| [audit-current.json](audit-current.json) | 11:14:15.398 | 56 个 Prefab、162 个 Button 的结构审计 |
| [preview-report.json](preview-report.json) | 11:14:19.465 | 56 张 1080×1920 静态 PNG，0 条预览失败记录 |

最终作者脚本完成了 Unity 导入编译并实际执行上述流程。这里记录的是本轮作者工具、资源应用和指定 Prefab 的结果，不代表全工程所有资源或所有业务流程都没有问题。

交付时 Unity 已退出 Play；状态报告为 `compiling=false`、`compilationErrors=0`、`lastError` 为空。交付前检查的最新 1500 行 Editor 日志未出现 NullReference、MissingReference 或 C# 编译错误；这项检查不等同于整个历史日志没有错误。

静态预览通过独立 PreviewScene 和真实 UI 组件生成；业务脚本与动画禁用，按需背景只绑定到临时预览实例。图片可能保留序列化默认语言、金额、选中态与提示内容，不等同于初始化后的运行状态。预览工具包含纯色输出检测；像红点这样的独立小组件本身覆盖面积很小。

## 结构审计差分与遗留问题

相对于 [audit-baseline.json](audit-baseline.json)，最终报告为：

| 指标 | 数量 |
| --- | ---: |
| 新增错误 | **0** |
| 新增问题（含警告） | **0** |
| Missing Script 对象 | **0** |
| 断裂的本地 fileID 引用 | **0** |
| 已解决的基线问题记录 | 260 |
| 仍存在的基线错误记录 | 197 |
| 仍存在的基线警告记录 | 42 |

遗留错误涉及 **45 个不同的缺失资产 GUID**，在多个 Prefab 中形成 **67 条 GUID 证据**，以及 **130 个未解析的序列化引用槽**，合计 197 条错误记录。同一个缺失资产可以对应多条证据；这些数量不是 197 个独立运行故障，也不能因界面能显示而认定它们不存在。

42 条既存警告包括 36 个 Button 未配置 `targetGraphic`，以及 6 条 Inspector 持久事件绑定。报告保留这些问题，没有通过过滤或忽略它们得到“零新增”。`0 Missing Script` 和 `0 本地断链` 仅指本次 56 项审计范围；**不能将结论简写为“全工程无错误”**。

## 正式入口运行观察

已从 `Assets/Game/Resources/Scenes/InitWZ.unity` 启动并完成原框架初始化、资源与账号流程，再由框架进入 `GamePlay.unity`。运行截图中的活动场景为 GamePlay，是正式流程的正常结果，并非直接启动玩法场景绕过初始化。

下列 7 套 PNG/JSON 是本轮保留的运行观察证据，分辨率均为 1080×1920。页面通过真实 `UIModule` 打开/关闭；截图与诊断来自真实运行实例。

| 已观察界面 | PNG 原图 | 同帧请求时的运行诊断 |
| --- | --- | --- |
| 游戏 HUD / RealGamePanel | [10:54:34](RuntimeCaptures/runtime-20260917-105434-279.png) | [JSON](RuntimeCaptures/runtime-20260917-105434-279.json) |
| 设置 / PausePanel | [10:55:18](RuntimeCaptures/runtime-20260917-105518-598.png) | [JSON](RuntimeCaptures/runtime-20260917-105518-598.json) |
| 金币兑换与提现 / RealWithdrawPanel | [10:57:27](RuntimeCaptures/runtime-20260917-105727-627.png) | [JSON](RuntimeCaptures/runtime-20260917-105727-627.json) |
| FAQ | [10:58:24](RuntimeCaptures/runtime-20260917-105824-011.png) | [JSON](RuntimeCaptures/runtime-20260917-105824-011.json) |
| 提现记录 / WithdrawHistory | [10:59:52](RuntimeCaptures/runtime-20260917-105952-683.png) | [JSON](RuntimeCaptures/runtime-20260917-105952-683.json) |
| 客服 / ServicePanel | [10:59:57](RuntimeCaptures/runtime-20260917-105957-178.png) | [JSON](RuntimeCaptures/runtime-20260917-105957-178.json) |
| 现金提现 / FakeWithdrawPanel，最终金额卡修正后 | [11:15:42](RuntimeCaptures/runtime-20260917-111542-057.png) | [JSON](RuntimeCaptures/runtime-20260917-111542-057.json) |

最终 FakeWithdrawPanel 原图已人工复核：金额文字清晰，新人奖励提示位于选中卡内部并居中，选中标志和主按钮显示正常。其余六套截图记录了对应页面的运行观察，时间早于这次只针对现金提现金额卡的最后布局调整；不宣称它们全部在最后一次应用后重新截取。

诊断 JSON 记录了运行 Image 的 Sprite/资源路径/位置/显示状态、TMP 的文字与字体、Button 的组件状态。`clickable` 是组件、CanvasGroup 和射线图形的状态估计，不等于自动点击测试，也不验证其他窗口遮挡。中间迭代图保留在 `RuntimeCaptures/Iterations/`，不计入顶层 7 套交付索引。

## 验收边界

- 本轮未点击支付提交、广告或客服发送，未进行真实资金交易、广告播放或消息发送；未重置存档。
- 本轮未进行 Android/iOS 真机测试。ASTC 实际显示、设备比例与安全区、加载峰值、纹理内存、帧率和切页性能仍需设备验证。
- 没有覆盖所有国家/账号组合、所有表单错误态、长消息边界、奖励领取结果及极端业务状态。静态截图数量与运行观察页面数量不能相互替代。
- 现有账号、提现门槛、金额、奖励、资源加载和状态切换业务链路保持原流程；本记录只对列明的视觉与结构证据负责。

## 查看画廊

[gallery.html](gallery.html) 已构建完成，共 **63 张图片：56 张静态预览 + 7 张实际运行截图**。画廊明确区分“静态 Prefab 默认值”和“实际运行”；缩略图链接到原图，运行图附同名 JSON。需要重新生成时，在工程根目录运行 `python -X utf8 Design/OrchardUI/build-gallery.py`。制作设置、三张共享贴图与回退说明见 [README.md](README.md)。
