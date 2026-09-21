# SlotFQAPanel 只读审查（2026-09-18）

此记录用于先出 AI 效果图；本次审查没有修改 Assets、Prefab、脚本，没有操作 Unity，也没有生成图片。

## 审查对象

- `Assets/BizzaWZ/Final/Real/UI/SlotsPanel/SlotFQAPanel/SlotFQAPanel.prefab`
- `Assets/BizzaWZ/Final/Real/UI/SlotsPanel/SlotFQAPanel/SlotFAQPanel.cs`
- 嵌套 `Assets/BizzaWZ/Final/Real/UI/SlotsPanel/BingoAsset/Assets/UiPrefab/SlotMachineGroup.prefab`（GUID `3e3b346423d816d47876dab1b13e409c`）
- `Assets/OrchardUI/Art/Controls.png.meta`
- `Assets/BizzaWZ/Final/BizzaGame/Z_ReplaceAssets/UI_Frame/SlotPanel/SlotEnter/jackpot.png`
- `Assets/BizzaWZ/Final/BizzaGame/Z_ReplaceAssets/UI_Frame/SlotPanel/SlotOther/Btn_SlotOk.png`
- `Assets/Game/Resources/ConfigAssets/TableBin/Table01/tbllanguage.bytes`（只读取现有本地化数据）

## 已定位问题

1. 顶部长方形空框原来是老虎机标志图。`Content/bg2` 的当前 Image `5250022705326557845` 使用 Controls/Panel（fileID `2758570`），Sliced，PPU multiplier `0.7`。HEAD 中同 Image 原来使用 `jackpot.png`（GUID `98902215167c80443b647fdd72025413`），Simple。原图为透明外轮廓的 MILLIONAIRE JACKPOT 装饰标志。通用奶油面板替换整张标志是空框的直接原因，不是标题 TMP 丢失；没有独立的标题 TMP 节点。
2. 下方巨大机身也误用了通用 Panel。嵌套 `SlotMachineGroup/Buttom` 的 Image `4300416282917706685` 使用 Controls/Panel，但 `m_Type=0`（Simple），在 `997×1575.5061` 的长矩形内直接拉伸。这解释图中夸张膨胀、纵向变形的金色边框。原机身 Spine 节点 `Content/SkeletonGraphic (01LHJ)` 仍存在，GameObject `6509552871252993361` 的 `m_IsActive=0`；不能简单恢复动画并假定现有预览布局不受影响。
3. 页脚规则文字不是缺字，而是低对比。`Content/des` 的 TMP `6958643258227197305` 使用奶白字体颜色 `(1,0.9764706,0.90588236,1)`，落在奶白机身上；字体 36，Body 材质。可通过原 TMP 的颜色/材质解决，不应把正文画进背景。
4. 底部关闭按钮的子图标误用按钮底图。`Content/Btn` 与 `Content/Btn/Image (1)` 都被设置为 Controls/ButtonBlue（fileID `-764451775`）、Sliced；子图标只有 `129×70`，所以缩成嘴唇状。HEAD 子图标原来是 `Btn_SlotOk.png`（GUID `f53e0e047e51faa4ba9a3e28e5755517`），内容为白色金边 OK。应在这个现有 Image 中恢复清楚的 OK/确认视觉，保留唯一关闭按钮和原交互。

## 固定的 Rect 与层级

以下为 Prefab 本地数值，未经运行时 CanvasScaler 转换；不要把它们误当截图像素。

| 现有节点 | Rect fileID | Anchored Position | SizeDelta | 说明 |
|---|---:|---|---|---|
| SlotFQAPanel/Content | 617195395320269764 | (0,-251) | 70×70 | 中心锚点与中心 pivot |
| Content/bg2 | 8805309752385714060 | (0,1016) | 994×614 | 顶部装饰现有 Image，可画机台冠部和星光，不能添加新功能标题 |
| Content/SlotMachineGroup | 来源 2012189451324540617，实例 3151483872259529701 | (0,198) | 1016×2142 | 现有嵌套实例 |
| SlotMachineGroup/Buttom | 来源 2068490866458744859 | (0,-44) | 997×1575.5061 | 主机身底图，中心锚点 |
| Content/bg2 (1) | 7479695957089711254 | (-3.2722,283) | 962.4584×791.8394 | 奖励表底板 Image 9132095855591360206 |
| Content/bg2 (1)/root | 9055370915143298003 | (469,-466) | 702.4222×594.743 | 左上 anchor，中心 pivot；原 VerticalLayoutGroup 71365554334776158 |
| 六个 SlotResult 行 | 各自保留 | 布局组件决定 | 321.49×100 | VerticalLayoutGroup spacing=0，control width/height=false |
| 每行的三个符号底框 | 各自保留 | (50,-50)、(157.16333,-50)、(264.32666,-50) | 100×100 | 左上 anchor，三列顺序不变 |
| 每行等号 | 各自保留 | (200.6,0) | 81.4035×50 | 中心 anchor，TMP 字号55 |
| 每行奖励图标 | 各自保留 | (460,-50) | 前三行116.75×80.75；后三行130×97.5 | 左上 anchor，动态地区货币组件保留 |
| 每行说明文字 | 各自保留 | 约(483.48,0) | 245.706×88.4 | 中心 anchor，TMP 字号36；原本地化机制保留 |
| Content/des | 1133554623368840100 | (-11.3288,-301) | 620.6275×230.3623 | 完整规则正文，动态本地化 |
| Content/Btn | 769373269381146989 | (0,-715) | 505.4303×119.0305 | 唯一关闭 Button |
| Content/Btn/Image (1) | 3677535174969407404 | (0,19) | 129×70 | 原 OK 图标，无 TMP；应保留位置 |

## 六行奖励表：内容与顺序必须保留

| 行节点 | 3个原符号 | 奖励类型 | 本地化 key | 当前 pt-BR 文本 |
|---|---|---|---|---|
| SlotResult | 筹码、筹码、筹码 | `HundredMoney`，现金堆 | SlotFAQSmallDes_1 | Uma pequena quantidade |
| SlotResult (1) | 黑桃A、黑桃A、黑桃A | `HundredMoney`，现金堆 | SlotFAQSmallDes_2 | Muitos |
| SlotResult (2) | 骰子、骰子、骰子 | `HundredMoney`，现金堆 | SlotFAQSmallDes_3 | Surpresa |
| SlotResult (3) | 紫色饮品、紫色饮品、紫色饮品 | `PileWealth`，金币与现金堆 | SlotFAQSmallDes_4 | Uma pequena quantidade |
| SlotResult (4) | 钻石、钻石、钻石 | `PileWealth`，金币与现金堆 | SlotFAQSmallDes_5 | Muitos |
| SlotResult (5) | 筹码、黑桃A、骰子 | `PileWealth`，金币与现金堆 | SlotFAQSmallDes_6 | Recompensas em dobro |

这些奖励 Image 上有 `WzIconAmend`：前三行 iconType=6（HundredMoney），后三行 iconType=4（PileWealth）。实际地区图标通过原 `UIUtils.SetWzSprite` 更新，单货币配置也走原逻辑。不能为统一美术把全部奖励改成金币、钻石、水果。

原符号素材均在 `.../SlotPanel/SlotIcon/`：

- `Icon_SlotElement1.png` 筹码，GUID `e34b24831d150bf4bb6fbe6811001817`。
- `Icon_SlotElement2.png` 黑桃 A，GUID `f04f33a5e9b78c245962dfb7e125da8e`。
- `Icon_SlotElement3.png` 骰子，GUID `cbb7ab6fa8571b14a87b99809f76d417`。
- `Icon_SlotElement4.png` 紫色饮品，GUID `522bbe5d8c00a9b4483b267b22d3d4ba`。
- `Icon_SlotElement5.png` 钻石，GUID `3429d6fc3b94c294990b042c39ad201b`。
- 18个符号底框共用 `Bg_SlotElement.png`，GUID `894cffb134822554090aca3e317bb8d8`；可统一替换其外观，内层符号保留原独立 Image。

正文 key `SlotFAQDes` 的现有 pt-BR 内容：

> Você ganhará 1 oportunidade de sorteio a cada 5 níveis que completar. Quanto mais anúncios assistir, maior será a recompensa.

本文不改变规则文案、经济配置、概率或奖品。6 行说明和页脚仍是 `UILanguageLabel` 更新的 TMP。预览可以展示当前葡语，但素材不能烘焙这些内容。

## 可实施的美术方向

- 保持明显的传统三转轴老虎机识别：金属边、适量小灯、机台冠部、内凹转轴格，不再把整页做成两张大木相框。
- 主机身在现有 `Buttom` 内绘制细金边、深色机壳及下方说明安静区域。按其约 0.633 的长宽比定制底图，或采用边界已验证的九宫格，避免把近方形面板 Simple 拉成长机身。
- `Content/bg2` 作为机台冠部/透明装饰外轮廓，必须限定于已有区域，与下方机身视觉衔接；不新增功能文字和标题节点。不需要填满994×614的奶白大矩形。
- 奖励表保留六行、三列符号、等号、奖励图标、说明的全部几何关系。用现有 `bg2 (1)` 绘制干净的奶油白内凹表板及很淡的六行分隔底纹，不新增行节点。每个现有符号底框可改为奶白金边，减少蓝色小方框的拼贴感。
- 原现金堆和金币现金混合图标保持地区更新，不增加奖品；表格说明用深棕/深蓝文字确保对比。页脚在原620.6×230.4区域使用深色清晰正文，可沿用现有 Body 材质。
- 原底部 Button 改成与主旋转按钮同一材质的清楚机台按钮；原子Image恢复OK语义。`SlotFAQPanel.OnAwake()` 的 `bizzaButton.onClick.AddListener(CloseSelf)` 不变，按钮数量与点击区域不变。
- 不启用新按钮、不新增拉杆、不新增标题或奖池金额。顶部装饰仅是画在现有Image里的静态图案。正式实施时对嵌套源Prefab与帮助页override一起核对，不能只改一处后误以为所有实例同步。

## 后续确认后才执行的验收

检查既有页面入口与关闭返回，葡语最长说明不溢出，六行奖励含义保持；从 InitWZ 正式启动验证。报告必须把 AI 效果图、Prefab 静态预览、Unity 运行截图分别标明；本文件不是运行验证记录。

## 最终 AI 效果图只读复核

复核文件：`09-ai-slot-help-final.png`，对照 `02-user-help.png`。这是 AI 生成的效果图，虽然沿用了 Unity 编辑器外框，仍不是 Unity 实际运行截图，也不是 Prefab 静态预览。此轮复核没有修改 Assets。

- 六行原符号顺序、前三行现金堆、后三行金币与现金堆均保留。六条葡语描述依次为 Uma pequena quantidade、Muitos、Surpresa、Uma pequena quantidade、Muitos、Recompensas em dobro，未增加奖励或金额。
- 页脚完整葡语规则仍然可见，奶白文字在深绿色背景上已有清楚对比。后续实施仍须使用原 TMP 与本地化组件，不能从 AI 图中切下这些文字当素材。
- 只保留原位置的一个底部确认/关闭按钮，改成绿色机台按钮与明确勾号；没有新增拉杆、顶部关闭按钮、奖池或功能标题。实际绑定仍须保留 CloseSelf。
- 按各自游戏 viewport 归一化观察，最终图奖励表约 L=.130、R=.865、T=.320、B=.658；原图约 L=.139、R=.859、T=.319、B=.653。六行中心 y 约 .408/.451/.494/.537/.580/.623，与原图 .412/.454/.496/.538/.580/.622 接近；规则中心约 .733，按钮中心约 .909，分别对应原图 .734 与 .908。首版中奖励表明显变窄、行列整体上移、按钮上移以及近方形构图问题已纠正。
- 仍需将 AI 外观适配到原 Prefab 精确 Rect；以上接近说明预览构图可落地，不代表 AI 像素与工程完全一致。顶部现有装饰区与机身空白区保留，金边、灯泡、星形与叶子装饰可直接绘入对应背景素材，无需增加节点。
