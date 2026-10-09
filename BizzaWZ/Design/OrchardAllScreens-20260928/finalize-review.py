"""Collect existing Unity evidence and document the delivered artwork."""
from pathlib import Path
from datetime import datetime, timezone
import json
import re
import sys

sys.stdout.reconfigure(encoding='utf-8')
out = Path(__file__).resolve().parent
project = out.parent.parent
evidence = out.parent / 'OrchardImplementation-20260928'
read = lambda p: json.loads(p.read_text(encoding='utf-8-sig'))
screens = read(evidence / 'review-index.json')
audit = read(evidence / 'audit-current.json')
state = read(out.parent / 'OrchardUI/state.json')
runs = {key: read(evidence / folder / 'navigation.json') for key, folder in [('reference','Runtime'),('short','RuntimeShort')]}
hud = {key: read(evidence / folder / 'hud-layout.json') for key, folder in [('reference','Runtime'),('short','RuntimeShort')]}
startup = {key: read(out / ('loading-startup-'+key+'.json')) for key in ['reference','short']}
loading = read(out.parent / 'OrchardLoading/preview-report.json')
assert len(screens) == 27
assert state['compilationErrors'] == 0 and not state['playing']
assert audit['newIssueCount'] == 0
for run in runs.values():
    assert run['status'] == 'complete' and len(run['pages']) == 13 and not run['error']
    assert all(p['opened'] and p['closed'] for p in run['pages'])
for run in hud.values():
    assert run['status'] == 'complete' and not run['error']
    assert all(b['centerHitsButton'] for b in run['pages'][0]['buttons'])
for report in startup.values():
    assert report['status'] == 'LoadingPanel closed normally' and not report['errors']
assert len(loading['frames']) == 15 and not loading['errors']
for s in screens:
    assert (evidence / s['reference']).is_file()
    assert (evidence / s['preview']).is_file()
    assert (project / s['prefab']).is_file()

summary = {
    'generatedUtc': datetime.now(timezone.utc).isoformat(),
    'project': str(project),
    'unityVersion': '2022.3.62f3',
    'entryScene': 'Assets/Game/Resources/Scenes/InitWZ.unity',
    'authoredScreens': len(screens),
    'compilationErrors': state['compilationErrors'],
    'navigation': {k: {'startedUtc':v['startedUtc'],'status':v['status'],'pages':len(v['pages']),'errorsLogged':v['errors']} for k,v in runs.items()},
    'hud': {k: {'startedUtc':v['startedUtc'],'status':v['status'],'checks':v['pages'][0]['checks']} for k,v in hud.items()},
    'loading': {'previewFrames':len(loading['frames']),'previewErrors':loading['errors'],'startup':{k:v['status'] for k,v in startup.items()}},
    'prefabAudit': {k:audit[k] for k in ['generatedUtc','prefabCount','buttonCount','existingErrorCount','warnings','newIssueCount','missingScriptObjects','missingLocalReferences']},
    'limits': ['Static preview fixtures and composite backgrounds are labelled in the gallery; they are not runtime business tests.',
               'SDK log 开启测试设备False is emitted as Debug.LogError by MaxSDKPlugin.cs:79 and appears in both navigation reports.',
               'Existing missing-asset audit records remain and are not equivalent to independent runtime failures.',
               'No Android/iOS device run, store build, real payout, ad reward, support-message send or store-rating submission was performed.']
}
(out/'verification-summary.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2),encoding='utf-8')

assets = [
    ('RewardBackdrop.png','Assets/OrchardUI/Resources/OrchardUI/RewardBackdrop.png','果园通关背景'),
    ('ReferenceDetails.png','Assets/OrchardUI/Art/ReferenceDetails.png','木标题、影片按钮、叶片奖励、橙色进度与设置开关'),
    ('ReferenceServiceDetails.png','Assets/OrchardUI/Art/ReferenceServiceDetails.png','闹钟、礼包、金币、客服气泡与浅蓝按钮'),
    ('ReferenceHeroes.png','Assets/OrchardUI/Resources/OrchardUI/ReferenceHeroes.png','通关宝箱、解锁魔杖、满水果托盘与绿光'),
    ('ReferenceMedals.png','Assets/OrchardUI/Art/ReferenceMedals.png','四级奖章、发送图标与客服头像'),
    ('ReferenceFinish.png','Assets/OrchardUI/Art/ReferenceFinish.png','教学气泡、手势、钱币堆、数字圆片、锁与编辑图标'),
]
requests = read(out/'imagegen-final-requests.json')
assert len(requests) == len(assets)
prompt_lines = ['# 本轮生产美术与最终提示词', '', '生成模式：内置 `image_gen.imagegen`，使用现有设计参考进行编辑。下面保存每次调用实际使用的完整提示词、参考文件与项目中的最终文件路径；未通过命令行生成。', '']
for request, (name, destination, purpose) in zip(requests, assets):
    assert (project/destination).is_file()
    prompt = json.loads(re.search(r'prompt\s*:\s*("(?:[^"\\]|\\.)*")',request['input']).group(1))
    refs = json.loads(re.search(r'referenced_image_paths\s*:\s*(\[[^\]]*\])',request['input']).group(1))
    transparent = re.search(r'transparent_background\s*:\s*(true|false)',request['input']).group(1)
    prompt_lines += [f'## {name}', '', purpose, '', f'- 最终文件：[{name}]({(project/destination).as_posix()})', f'- 时间：{request["timestamp"]}', f'- 透明背景：{transparent}', '- 参考文件：']
    prompt_lines += [f'  - `{ref}`' for ref in refs]
    prompt_lines += ['', '```text', prompt, '```', '']
(out/'imagegen-prompts.md').write_text('\n'.join(prompt_lines),encoding='utf-8')

readme = '''# 精致果园 · 全部界面调整记录

项目：`C:/Projects/paopao/BizzaWZ`，Unity `2022.3.62f3`。本轮对 27 个界面逐页调整并保存到实际 Prefab / 资源配置。

[打开全部界面对照](comparison.html) · [机器可读验证摘要](verification-summary.json) · [生产美术与完整提示词](imagegen-prompts.md)

## 本轮调整

- 加载页恢复完整果园画面与 Orchard Trio 标识；调整进度条尺寸、位置和填充，正式初始化过程中也检查了资源绑定与正常关闭。
- 通关页按参考重新配置背景、木牌、宝箱、奖励数字、COINS 标签、橙色进度、叶片奖励及影片图标；其他弹窗同步调整标题、插画、按钮与留白。
- 提现服务系列逐页调整卡片、输入框、付款平台、状态色、奖章、客服头像和气泡。长文本和动态行保留滚动与实际业务状态。
- 设置开关、评分星星、任务行、七个客服问题、转盘装饰与结果区域均落实到预制体。底部转盘、撤销、魔杖与洗牌按钮采用比例锚点，托盘与按钮分开排列。
- 静态布局与美术保存在 Prefab；运行时代码负责已有事件、状态刷新和按需资源绑定。背景经 Resources 加载；新增图集使用移动端 ASTC 6×6、关闭 mipmap。新增开关与按钮沿用标准 Unity Button 和代码事件绑定。

## 如何看对照

对照页包含左右比较和透明叠加，可切换预制体、852×1846 正式运行截图、1080×1920 正式运行截图。页面显示图像来源，缺少运行截图时明确回退到预制体预览。

27 个界面都有预制体渲染记录；主游戏展示实际运行截图，因为孤立 HUD 不包含游戏场景。教学、复活、道具补充、倍率提升、客服问题、欢迎礼包、每日奖励、转盘帮助这八页的静态预览使用预制体前景与保存的游戏背景合成，界面中有明确标注。

正式截图中的语言、关卡、水果种类、金额、任务和提现状态由当前账号与服务端决定。预制体预览中的设计示例数据仅写入临时副本，未为对图更改正式存档、余额或关卡。

## 已执行验证

| 检查 | 结果 | 证据 |
|---|---|---|
| C# 编译 | 0 个编译错误 | `../OrchardUI/state.json` |
| 正式 InitWZ 初始化后的页面导航 | 两种尺寸各 13/13 页面打开、关闭完成 | [长屏](../OrchardImplementation-20260928/Runtime/navigation.json) / [短屏](../OrchardImplementation-20260928/RuntimeShort/navigation.json) |
| 提现 | 付款方式切换并还原、最后档位可滚动到达、锁定档位页脚完整显示、余额未变化 | 同上 |
| 设置 | 音乐、音效、振动切换并恢复原值 | 同上 |
| 客服 | 帮助打开客服；七个问题可点击；选择问题填入草稿；自定义输入可用，未发送消息 | 同上 |
| 任务、评分、转盘 | 5 条真实任务；三星选择刷新五颗星；原转盘动画完成回调并显示三个结果，未消耗次数 | 同上 |
| 主游戏底部四个 Button | 两种尺寸中心命中成功；完整可见图形在屏幕内，互不重叠且不与托盘重叠 | [长屏](../OrchardImplementation-20260928/Runtime/hud-layout.json) / [短屏](../OrchardImplementation-20260928/RuntimeShort/hud-layout.json) |
| 加载预制体 | 3 种比例 × 5 个进度，共 15 个临时副本，0 个记录错误 | [加载预览报告](../OrchardLoading/preview-report.json) |
| 实际加载 | 两种尺寸均观察到图片绑定、显示与正常关闭，0 个记录错误 | [长屏](loading-startup-reference.json) / [短屏](loading-startup-short.json) |
| Prefab 引用与 Button 审核 | 56 个 Prefab、171 个 Button；新增问题 0、缺失脚本 0、缺失本地对象引用 0 | [完整审核](../OrchardImplementation-20260928/audit-current.json) |

两个导航报告均记录了项目已有 SDK 文本 `开启测试设备False`。其来源是 `MaxSDKPlugin.cs:79` 使用 `Debug.LogError` 输出测试设备状态；本轮导航未记录其他错误或异常。

完整引用审核仍记录 171 条既有错误和 12 条既有警告，包含历史资源 GUID、动画、材质等缺失项，均保留在报告中；重复引用会产生多条证据，不能把这个数字当作独立运行故障数，也不能据此声称整个工程没有问题。

本轮没有执行 Android/iOS 真机测试、商店打包、真实提现、广告奖励发放、客服发送或商店评价提交。静态预览与已检查的页面交互不等同于这些端到端流程全部验证。

## 界面与 Prefab

| 界面 | 项目内 Prefab |
|---|---|
'''
for s in screens:
    readme += f'| {s["id"]} · {s["title"]} | `{s["prefab"]}` |\n'
readme += '''
## 复查入口

- 编辑器制作入口：`Assets/OrchardUI/Editor/OrchardApprovedPass.cs` 及同组制作脚本；最终布局保存在 `../OrchardImplementation-20260928/layouts.json`。
- `author.ps1 preview:all` 只在编辑模式生成隔离副本预览；`author.ps1 audit` 读取现有资产作引用审核。运行检查使用正式 InitWZ 初始化。
- 本轮开始时的文件副本保存在 `Before/`，用于对照本轮改动；工程已有其他未提交修改未作整体回退。
- 对照页生成器为 `build-review.py`，汇总记录生成器为 `finalize-review.py`。这些脚本只处理证据文件，不修改运行截图。
'''
(out/'README.md').write_text(readme,encoding='utf-8')
print(json.dumps({'screens':len(screens),'navigation':[v['status'] for v in runs.values()],'hud':[v['status'] for v in hud.values()],'newAuditIssues':audit['newIssueCount'],'artworkPrompts':len(requests)},ensure_ascii=False))
