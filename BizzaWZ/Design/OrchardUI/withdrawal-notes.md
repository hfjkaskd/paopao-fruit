# 提现链路换皮作者记录

作者工具：`Assets/OrchardUI/Editor/OrchardWithdrawalPass.cs`。总流程在通用 sprite/font pass 后调用 `Apply(root, assetPath)`，统一保存。子任务未自行运行工具或直接编辑生产 prefab。

锁定视觉：无大树的明亮田野、蜂蜜木框与木牌标题、奶油信息面、深蓝正文、绿色金额/主按钮、白字主操作、浅 sage 非当前状态。

## 覆盖（17 个 prefab）

| Prefab | 针对性处理 |
| --- | --- |
| FakeWithdrawPanel | 田野背景、居中木标题、奶油余额和金额选择面、绿色提现按钮、进度条 |
| WithdrawAmountItem | 奶油金额卡、选中/已领取卡状态和颜色，保留 GetTag/GetedTag/Select 的业务控制 |
| RealWithdrawPanel | 田野背景、参考图木牌标题、主金额浅底、绿色加宽主按钮、双列倍率卡、木质完成/进度底板、提示字色和提现提示弹窗 |
| WithdrawInfo | 独立信息模块的木框、金额面、正文、按钮与滚动底 |
| WithdrawWay | 在原标准 Button 上增加静态卡面，将现有 Select 对象视觉改为绿色选中边并附原勾图；支付 Logo 保留 |
| WithdrawLevel | 独立等级列表的木框与标题 |
| WithdrawLevelItem | 倍率金色徽章、深蓝等级和金额、清楚的两行层级、低 alpha 的 sage 遮罩；锁图在原 Shadow 状态对象下随原逻辑显示 |
| DailyBonus | 木质每日奖励面、提示和计时颜色 |
| DailyBonusItem | 奶油任务卡、进度条、当前/已领文字，保留可领/已领识别图 |
| BonusRate | 金色奖励倍率小牌与深蓝数字 |
| WithdrawFillPanel | 资料填写木框、奶油输入框、绿色提交、深蓝正文、浅色 placeholder、独立错误色、输入光标/选区颜色 |
| UIWithdrawalConfirmPanel | 确认木框、绿色金额、奶油账号栏、绿色确认 |
| UIWithdrawalPendingPanel | 状态木框、绿色金额与进度、绿色确认，保留待处理/成功/失败图和状态 |
| WithdrawHistory | 历史木框与木标题、深蓝空记录提示 |
| WithdrawHistoryItem | 奶油记录卡、绿色金额、处理中金色/成功绿色/失败红色文字，保留状态图和可变行高 |
| WithdrawDanPanel | 田野背景、木牌标题、木质段位/提现/进度面、绿色主操作 |
| WithdrawDanItem | 奶油段位卡、徽章、进度、可领绿/准备蓝/已领浅 sage 状态 |

## 真实结构带来的约束

- `WithdrawWay/Frame` **是动态支付 Logo 本身**，其组件被 `payIcon` 引用，并非普通背景。其 sprite 不得替换；`PaymentConfig`、`paymentImage`、`withdrawImg` 同理。
- 当前支付渠道 Logo 图含原紫边的完整白卡。作者 Pass 保留其完整图像；选中状态加绿色边框。Logo 内部像素和支付标识未被修改。
- `RealWithdrawPanel.normalSprite` 与 `canWithdrawSprite` 会在运行时重新赋给按钮。两字段同步配置为主题绿色主按钮，保持参考图主按钮外观；余额门槛、是否允许提现与手指提示逻辑均未变。
- 提现文案中 `withdrawValueKeyColor`、`withdrawChannelKeyColor` 与 `_colorReplaceList` 富文本颜色在序列化字段里更新，避免运行时覆盖静态主题色。
- 倍率卡的原 `Shadow`、`SelectShadow` 仍由原脚本开关，仅把黑色覆盖改为低 alpha 的 sage。原用于将倍率和底板变灰的序列化材质置空，改由现有状态覆盖表达状态；其 Image 数组引用不变。
- 新增的 `OrchardCheck`、`OrchardLock` 仅为 prefab 静态装饰，均不拦截射线，也不带事件或逻辑脚本。
- `RealWithdrawPanel` 原主内容约 1122 高、等级区 1182 高，采用现有 ScrollRect 浏览。运行时根据国家将 `ProgressInfo` 的 y 设置为 320 或 470；保留此定位和滚动行为，未将所有国家和所有支付方式硬压到一屏。
- `FakeWithdrawPanel/Content/WithdrawBtn` 被 `Teach_01` 直接按路径引用，未改层级/名称。所有国家输入根、支付对象、资料字段、错误提示对象和键盘适配引用均保留。
- 成功/失败、准备/可领/已领、进度/完成这些业务状态不被作者工具主动切换。所有业务数字、金额、账户、阈值、事件和 Addressables 地址均保留。

## 验证状态

本文件记录作者实现，不代表运行验收。尚需总流程完成 Unity 编译、保存后引用审计、静态页面渲染与正式入口运行观察。
# Visual verification of regenerated previews

Reviewed actual previews 22–56, including a full-resolution check of RealWithdrawPanel. New circular navigation, title plaque and primary button, and the payment edge overlay fit the complete page. The standalone WithdrawWay root is stretched by the generic preview renderer, so its preserved-aspect payment logo reveals the original baked purple edge there; the actual payment cards in the composed RealWithdrawPanel have the correct ratio and clean new edge.

WithdrawLevelItem.Init fills level/amount and controls locked/current/completed states. WithdrawWay.Init clears selection before the page selects the current payment option. Their static prefab previews therefore legitimately show default text/state combinations; those defaults were not changed as a workaround. WithdrawHistory.Refresh also controls its empty message after the response; static rows and the empty message can coexist in an initialization-free preview.

One clear contrast issue was corrected in the authoring pass: status text uses white lettering over its retained green/amber/red tag images. Status values, tag GameObjects, runtime 230/300 row heights and all bindings remain intact. No Unity commands were run during this review.
