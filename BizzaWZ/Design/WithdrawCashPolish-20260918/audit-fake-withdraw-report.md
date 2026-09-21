# 现金提现页只读审计

本报告仅依据生产 Prefab、现有脚本、素材元数据与用户截图；没有修改 Assets、执行 Unity 命令或触发提现操作。完整层级、哈希、六个实例覆盖和12份任务配置见同目录 `audit-fake-withdraw-report.json`。原 `Design/WithdrawPolish-20260918` 是历史 PagBank 页面修订记录，本轮集中在此新目录。

## 进度条问题

这不是 Slider，而是 `FakeWithdrawPanel/Content/WithdrawProgress/Progress/real` 的 `Image.Type.Filled`，当前配置为 Horizontal、Left，`preserveAspect=0`，Prefab 初始 `fillAmount=0.446`。没有 Slider 的 minValue/maxValue。运行时脚本计算所选任务阶段的进度，Clamp01 后同时设置 fillAmount 和百分比，范围为0–1。

Controls/ProgressFill 的原图286×62，显示Rect897×64，原长宽比4.61被拉到14.02；水平方向相对垂直方向额外拉伸3.04倍。Filled模式不使用九宫格border，因此圆端、高光被拉长。底框和填充层还完全同尺寸，填充没有内缩，进一步盖住外框。

保留Filled和现有动态逻辑，用接近最终约20:1–25:1比例、低高光的独立填充图。轨道继续使用Sliced细边框，填充Rect比轨道内缩6–8单位。若最终fill897×44，素材应接近20.39:1；若fill885×36，应接近24.58:1。不能简单把Filled改成Sliced，否则现有fillAmount不会控制显示进度。

| 对象 | RectTransform fileID | Image/TMP fileID | 当前值 |
|---|---:|---:|---|
| Progress父容器 | 3131837160562448410 | — | 902.9887×73.36，位置(0,-24) |
| 轨道bg | 8175018248033638516 | 8743834095267926959 | 897×64，Controls/ProgressTrack，Sliced，PPU1 |
| 填充real | 2071485895475148001 | 6620554689692068040 | 897×64，Controls/ProgressFill，Filled Horizontal Left |
| 动态百分比 | 2262917192024109782 | 3339610134613169318 | 200×50，字号36，白字，子节点位于real下 |
| Progress标题 | 210600630110459938 | 6841260854729436532 | 1065.6×50，x639，右边超过父框88.8单位 |

轨道可先试897×56、填充885×44，中心保持不动；百分比保留动态文本，建议字号30–32并验证0%、25%、50%、100%的对比度。Image的填充量不会裁剪其文本子节点，因此0%时文字仍能显示。

## 全页样式修改位置

所有下面列出的背景均可只改 Image sprite/type/PPU/color，不需要新增节点、重排布局或修改按钮绑定。

| 背景 | Image fileID | 当前素材/PPU | 建议 |
|---|---:|---|---|
| 主面板 Content/Frame | 2754535437279419372 | Controls/Panel，Sliced，0.7 | HudNaturalCounter，Sliced；PPU2–3先试 |
| 标题底 ButtomGroup/bg | 6615170243285179318 | Controls/Title，Sliced，1 | HudNaturalCounter奶油牌；PPU2–3；标题正文改现有Body材质与深蓝，保留本地化 |
| 余额底 CashBalance/Balance/bg | 4192234944055056232 | Controls/Inset，Sliced，1 | HudNaturalCounter；PPU4–6降低边框占比 |
| 主操作 WithdrawBtn/bg | 1241199525063855708 | Controls/ButtonGreen，Sliced，1 | HudNaturalGreenButton；PPU3–4先试，保留按钮父节点与动态文字 |
| 普通金额卡 | 3062039195826043058 | Controls/Card，Sliced，1 | HudNaturalCounter；PPU4–6 |
| 新手可领状态 GetTag | 5455967628476765425 | Controls/SelectedCard，Sliced，1 | 保留绿色状态含义，可把现有PPU升到2–3，减细框；或匹配新的薄边素材 |
| 新手已领状态 GetedTag | 1584464498381216905 | Controls/DisabledCard，Sliced，1 | 保留灰/低饱和状态，可用匹配奶油薄边或把PPU升到2–3 |
| 选中勾 Select | 5590047728298028375 | 独立勾，Simple | 保留语义与显示逻辑；若细化造型，仍复用此Image |

HudNaturalCounter GUID为`dbf870bbe4a14284b631a4cd409d5147`，Sprite fileID21300000；GreenButton GUID为`d32dd97f0ef44177bd56a2985c098c92`，fileID21300000。前者源PNG2086×754、maxTexture512、sprite border155/95/155/95；后者源PNG1902×827、maxTexture512、border220/110/220/110。PPU建议是预览起点，最终边框厚度要以Unity导入后Sprite border与真实渲染为准；不要修改共享纹理meta来影响所有页面。

标题TMP为6004225462979585317，余额TMP为8767989740261440782；金额卡TMP为2196861301424074241。金额、百分比、标题、按钮文案均保持原文本/本地化机制。绿色框GetTag表示“新手可领”，并不是所有金额项的通用选中状态；真正selectObj是Select勾，不能调换含义。

## 六个嵌套实例必须同步

`FakeWithdrawPanel.prefab`内六个WithdrawAmountItem实例均显式覆盖3个背景Sprite：normal3062039195826043058、GetTag5455967628476765425、GetedTag1584464498381216905；normal还显式覆盖`m_PixelsPerUnitMultiplier=1`。只改源Prefab不能覆盖这些现有值。

实例fileID：`735769260790555943`、`921752336444412338`、`3145375543058054682`、`3366821845968657574`、`6389070758414889696`、`9024996893891000631`。源GUID为`5fefc70f9b2f3f1488b2b221199dcf24`。对这六个实例同步对应Sprite/PPU，源WithdrawAmountItem也保持一致，以覆盖运行时SetCmptListCount从源Prefab实例化的情况。

原金额布局保持六项、3列2行；GridContent宽892，cell279.8×188.6，spacing20.9×13.17。原根按钮6695874971154272860及WithdrawAmountItem.Init中的代码监听保持不变。

## 100%业务核对

截图选中的是0.01新手档，余额200.76。该档Money条件targetValue0.01，计算200.76/0.01=20076，Clamp01后显示100.00%，与现有业务一致。完成阶段时GetCurProgress也返回1。没有读取运行时存档阶段，因此不声称进行了业务端到端复测。

其余金额若仍处于第一阶段Money条件，余额200.76对应800档25.095%、1000档20.076%、2000档10.038%、3000档6.692%、5000档4.0152%；运行时按现有F2格式显示。引用的12份任务配置没有0目标值。本次美术修正无需改变金额、任务参数或提现逻辑。

四个提现源文件在此次审计前已经有历史未提交修改，不能重置。审计时没有其他提现编辑代理；root负责后续实施，本代理仅保存报告。
