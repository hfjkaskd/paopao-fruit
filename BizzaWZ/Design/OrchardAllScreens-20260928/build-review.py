"""Build an offline review page from Unity evidence; never edit screenshots."""
from pathlib import Path
from datetime import datetime, timezone
import json
import os

out = Path(__file__).resolve().parent
project = out.parent.parent
evidence = out.parent / 'OrchardImplementation-20260928'
read = lambda p: json.loads(p.read_text(encoding='utf-8-sig'))
screens = read(evidence / 'review-index.json')
composites = {'tutorial', 'revive', 'get-booster', 'rate-up', 'service-topics', 'welcome-gift', 'daily-mission', 'lucky-help'}
extra_runtime = {'service-topics': 'service-topics', 'rating': '12-StarRatingPopup'}

template = r'''<!doctype html>
<html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
<title>精致果园 · 全部界面调整对照</title>
<style>
*{box-sizing:border-box}body{margin:0;color:#243d31;background:#f4f5ee;font:15px/1.6 system-ui,"Microsoft YaHei",sans-serif}
header{padding:22px 30px;background:#173f32;color:#fff}h1{font-size:24px;margin:0}header p{margin:5px 0 0;color:#d9e9d9}
main{display:grid;grid-template-columns:225px minmax(0,1fr);gap:24px;padding:24px;max-width:1480px;margin:auto}
nav{position:sticky;top:20px;align-self:start;max-height:90vh;overflow:auto;display:flex;flex-direction:column;gap:4px}
button,select,input{font:inherit}button,select{cursor:pointer}nav button{border:0;padding:9px 12px;text-align:left;background:transparent;border-radius:8px;color:inherit}nav button[aria-current=true]{background:#286b4d;color:white}nav small{display:block;font-size:11px;opacity:.7}
.toolbar{display:flex;align-items:center;justify-content:space-between;gap:15px;flex-wrap:wrap}h2{margin:0;font-size:22px}.controls{display:flex;gap:8px;align-items:center;flex-wrap:wrap}select{padding:7px;border:1px solid #b9cabb;background:#fff;border-radius:8px;color:inherit}
.note{background:#e7edda;border-radius:10px;padding:12px 15px;font-size:13px}.pair{display:grid;grid-template-columns:1fr 1fr;gap:18px}.pair figure{padding:12px;margin:0;border-radius:16px;background:white}figcaption{font-size:20px;font-weight:650;margin-bottom:12px}.pair img{width:100%;display:block;border-radius:9px}
.overlay{display:none;position:relative;max-width:570px;margin:auto;background:#fff;border:10px solid white;border-radius:12px;overflow:hidden}.overlay img{width:100%;display:block}.overlay #top{position:absolute;inset:0;width:100%;height:100%;object-fit:fill;opacity:.5}.overlay-controls{display:none;margin:14px 0;gap:12px;align-items:center}.overlay-controls input{flex:1;max-width:400px}
.path{font-size:12px;overflow-wrap:anywhere}.links{display:flex;flex-wrap:wrap;gap:15px;margin:15px 0}a{color:#1e6950}.links a{font-size:13px}footer{padding:20px 30px;background:#e5ebdf;font-size:12px;color:#4d6456}
body[data-view=overlay] .pair{display:none}body[data-view=overlay] .overlay{display:block}body[data-view=overlay] .overlay-controls{display:flex}
@media(max-width:800px){main{display:block;padding:12px}nav{position:static;max-height:160px;margin-bottom:18px}.pair{gap:8px}.pair figure{padding:7px}figcaption{font-size:15px}header{padding:17px}h1{font-size:20px}}
</style>
<header><h1>精致果园 · 全部界面调整对照</h1><p>27 个界面 · 设计参考 / 新版 Unity 预制体 / 正式入口运行截图</p></header>
<main><nav id="nav" aria-label="界面列表"></nav><section><div class="toolbar"><h2 id="title"></h2><div class="controls">
<select id="mode" aria-label="画面来源"><option value="preview">新版预制体</option><option value="runtime">运行截图 · 852 × 1846</option><option value="short">运行截图 · 1080 × 1920</option></select>
<select id="view" aria-label="对照方式"><option value="pair">左右对照</option><option value="overlay">透明叠加</option></select></div></div>
<p class="note" id="note"></p><div class="pair"><figure><figcaption>设计参考</figcaption><img id="reference" alt="设计参考"></figure><figure><figcaption id="caption"></figcaption><img id="actual" alt="Unity 画面"></figure></div>
<div class="overlay-controls"><label for="alpha">Unity 透明度</label><input id="alpha" type="range" min="0" max="100" value="50"><output id="percent">50%</output><span>按画布归一化叠加</span></div><div class="overlay"><img id="bottom" alt="设计底图"><img id="top" alt="Unity 叠加图"></div>
<div class="links"><a id="referenceLink" target="_blank">打开设计原图</a><a id="actualLink" target="_blank">打开 Unity 原图</a><a href="README_LINK">修改与验证记录</a></div><p id="path" class="path"></p></section></main>
<footer>预览中的示例数据仅作用于临时副本。正式截图使用当前账号、当前语言和服务端配置；加载、金额、关卡、奖励和状态不会为了截图写入存档。标注为合成预览的页面使用实际预制体前景和已保存的游戏背景。此页提供视觉验收证据，不将静态预览当作业务测试。生成于 GENERATED_UTC。</footer>
<script>
const screens=SCREEN_DATA;const $=id=>document.getElementById(id);let index=Math.max(0,screens.findIndex(s=>s.key===location.hash.slice(1)));if(!location.hash)index=1;
screens.forEach((s,i)=>{const b=document.createElement('button');b.textContent=s.id+' · '+s.title;const t=document.createElement('small');t.textContent=s.runtime?'预制体 + 运行截图':'预制体视觉预览';b.append(t);b.onclick=()=>{index=i;history.replaceState(null,'','#'+s.key);render()};$('nav').append(b)});
function render(){const s=screens[index],mode=$('mode').value;let source=s[mode],fallback=false;if(!source){source=s.preview;fallback=true;}const live=(mode!=='preview'&&!fallback)||s.previewIsRuntime&&mode==='preview';const caption=live?'Unity 正式运行截图':s.composite?'Unity 预制体预览（含游戏背景）':'Unity 预制体预览';$('title').textContent=s.title;$('caption').textContent=caption;
['reference','bottom'].forEach(id=>$(id).src=s.reference);['actual','top'].forEach(id=>$(id).src=source);$('referenceLink').href=s.reference;$('actualLink').href=source;$('path').textContent=s.prefab;
$('note').textContent=live?(s.runtimeNote||'从 InitWZ 完整初始化后捕获。内容与状态来自当前账号。')+(mode==='short'?'此模式用于检查较短屏幕的适配，不与长屏设计逐像素比较。':''): (fallback?'这个界面尚无本轮该尺寸的运行截图，当前显示预制体视觉预览。':'')+(s.composite?'前景为实际预制体渲染，背景为已保存的游戏截图；这是一张合成预览。':'实际预制体在隔离场景中渲染，背景绑定与布局配置来自项目。')+'示例文案与状态只用于视觉核对。';
Array.from($('nav').children).forEach((b,i)=>b.setAttribute('aria-current',i===index));document.body.dataset.view=$('view').value;}
$('mode').onchange=render;$('view').onchange=render;$('alpha').oninput=()=>{$('top').style.opacity=$('alpha').value/100;$('percent').value=$('alpha').value+'%'};render();
</script></html>'''

cash_review = out.parent / 'OrchardCashRefinement-20260928'
account_review = out.parent / 'OrchardAccountRefinement-20260928'
confirm_review = out.parent / 'OrchardConfirmRefinement-20260928'
history_review = out.parent / 'OrchardHistoryRefinement-20260928'
reminder_review = out.parent / 'OrchardReminderRefinement-20260928'
rate_review = out.parent / 'OrchardRateRefinement-20260928'
service_review = out.parent / 'OrchardServiceRefinement-20260928'
daily_review = out.parent / 'OrchardDailyMissionRefinement-20260928'
spin_review = out.parent / 'OrchardLuckySpinRefinement-20260928'
help_review = out.parent / 'OrchardLuckyHelpRefinement-20260928'
for destination in [out, evidence, cash_review, account_review, confirm_review, history_review, reminder_review, rate_review, service_review, daily_review, spin_review, help_review]:
    data = []
    def link(path):
        return Path(os.path.relpath(path, destination)).as_posix() + '?v=' + str(path.stat().st_mtime_ns)
    for entry in screens:
        s = dict(entry)
        s['reference'] = link((evidence / entry['reference']).resolve())
        s['preview'] = link(evidence / entry['preview'])
        s['composite'] = entry['key'] in composites
        runtime = evidence / entry['runtime'] if entry.get('runtime') else None
        if entry['key'] in extra_runtime:
            runtime = evidence / 'Runtime' / (extra_runtime[entry['key']] + '.png')
        if entry['key'] == 'get-booster':
            runtime = out.parent / 'OrchardBoosterRefinement-20260928/Runtime/00-AddPropPanel.png'
        if entry['key'] == 'cash-withdraw':
            runtime = out.parent / 'OrchardCashRefinement-20260928/Runtime/01-selected-goal.png'
        if entry['key'] == 'withdraw-account':
            runtime = account_review / 'Runtime/00-account.png'
        if entry['key'] == 'withdraw-confirm':
            runtime = confirm_review / 'Runtime/00-confirm.png'
            s['runtimeNote'] = '从 InitWZ 完整初始化后，经正式页面接口打开确认页；金额来自当前账号，姓名、遮罩证件号和邮箱使用不落盘的设计样例。未提交提现。原图为 828 × 1900，游戏按屏幕等比适配。'
        if entry['key'] == 'history':
            runtime = history_review / 'Runtime/01-history.png'
            s['runtimeNote'] = '从 InitWZ 完整初始化后打开正式历史页，以临时预制体记录检查三种状态；日期、金额和遮罩账户为设计样例，未写入存档或服务端。验证记录另存真实历史响应截图。'
        if entry['key'] == 'withdraw-reminder':
            runtime = reminder_review / 'Runtime/00-reminder.png'
        if entry['key'] == 'rate-up':
            runtime = rate_review / 'Runtime/00-rate-up.png'
            s['runtimeNote'] = '从 InitWZ 完整初始化后，通过正式页面接口传入两组临时兑换数据，检查资源加载、金额刷新和按钮。币种、语言沿用当前账号；未改写账号余额或兑换率，未提交提现。'
        if entry['key'] == 'service':
            runtime = service_review / 'Runtime/01-service.png'
            s['runtimeNote'] = '从 InitWZ 完整初始化后打开正式客服页，以临时 ChatElement 预制体呈现四条参考消息。另验证 14 条长消息滚动、键盘高度回调和问题选择/清空/导航；不发送消息、不写入聊天记录。'
        s['runtime'] = link(runtime) if runtime and runtime.exists() else None
        if entry['key'] == 'lucky-help':
            runtime = help_review / 'Runtime/00-lucky-help.png'
            s['runtime'] = link(runtime) if runtime.exists() else None
            s['runtimeNote'] = '从 InitWZ 完整初始化后，经幸运抽奖页问号按钮打开。检查六行完整卡片、18 个图标底框及双语说明排版；分别验证底部按钮和右上角关闭。未触发转动或广告。'
        if entry['key'] == 'lucky-spin':
            runtime = spin_review / 'Runtime/00-lucky-spin.png'
            s['runtime'] = link(runtime) if runtime.exists() else None
            s['runtimeNote'] = '从 InitWZ 完整初始化后打开正式幸运抽奖页，显示实际免费次数。另用不落盘的 0/1 文本样例检查居中，并通过标准返回按钮关闭、重开核对；未消耗转动次数、未播放广告。'
        if entry['key'] == 'daily-mission':
            runtime = daily_review / 'Runtime/01-daily-mission.png'
            s['runtime'] = link(runtime) if runtime.exists() else None
            s['runtimeNote'] = '从 InitWZ 完整初始化后打开正式每日任务页。先记录真实服务器响应，再通过实际显示接口检查临时 8/30、可领取、已领取及零目标状态；样例不写入存档。未播放广告或领取奖励，关闭重开后恢复实际任务数据。'
        short = evidence / 'RuntimeShort' / runtime.name if runtime else None
        if entry['key'] == 'get-booster':
            short = out.parent / 'OrchardBoosterRefinement-20260928/RuntimeShort/00-AddPropPanel.png'
        if entry['key'] == 'cash-withdraw':
            short = out.parent / 'OrchardCashRefinement-20260928/RuntimeShort/01-selected-goal.png'
        if entry['key'] == 'withdraw-account':
            short = account_review / 'RuntimeShort/00-account.png'
        if entry['key'] == 'withdraw-confirm':
            short = confirm_review / 'RuntimeShort/00-confirm.png'
        if entry['key'] == 'history':
            short = history_review / 'RuntimeShort/01-history.png'
        if entry['key'] == 'withdraw-reminder':
            short = reminder_review / 'RuntimeShort/00-reminder.png'
        if entry['key'] == 'rate-up':
            short = rate_review / 'RuntimeShort/00-rate-up.png'
        if entry['key'] == 'service':
            short = service_review / 'RuntimeShort/01-service.png'
        if entry['key'] == 'daily-mission':
            short = daily_review / 'RuntimeShort/01-daily-mission.png'
        if entry['key'] == 'lucky-spin':
            short = spin_review / 'RuntimeShort/00-lucky-spin.png'
        if entry['key'] == 'lucky-help':
            short = help_review / 'RuntimeShort/00-lucky-help.png'
        s['short'] = link(short) if short and short.exists() else None
        if entry['key'] == 'loading':
            for mode, filename in [('runtime','loading-startup-reference.json'),('short','loading-startup-short.json')]:
                report = read(out / filename)
                frames = [f for f in report['frames'] if f['resourcesBound'] and f['visibleAlpha'] > .9 and Path(f['image']).exists()]
                if frames:
                    s[mode] = link(Path(frames[-1]['image']))
        s['previewIsRuntime'] = entry['key'] == 'game-hud'
        if s['previewIsRuntime']:
            s['preview'] = s['runtime']
        data.append(s)
    html = template.replace('SCREEN_DATA', json.dumps(data, ensure_ascii=False)).replace('GENERATED_UTC',datetime.now(timezone.utc).strftime('%Y-%m-%d %H:%M UTC')).replace('README_LINK',Path(os.path.relpath(out/'README.md', destination)).as_posix())
    if destination == cash_review:
        html = html.replace('if(!location.hash)index=1;', "if(!location.hash)index=screens.findIndex(s=>s.key==='cash-withdraw');")
        html = html.replace('../OrchardAllScreens-20260928/README.md', 'README.md')
    if destination == account_review:
        html = html.replace('if(!location.hash)index=1;', "if(!location.hash)index=screens.findIndex(s=>s.key==='withdraw-account');")
        html = html.replace('../OrchardAllScreens-20260928/README.md', 'README.md')
    if destination == confirm_review:
        html = html.replace('if(!location.hash)index=1;', "if(!location.hash)index=screens.findIndex(s=>s.key==='withdraw-confirm');")
        html = html.replace('../OrchardAllScreens-20260928/README.md', 'README.md')
    if destination == history_review:
        html = html.replace('if(!location.hash)index=1;', "if(!location.hash)index=screens.findIndex(s=>s.key==='history');")
        html = html.replace('../OrchardAllScreens-20260928/README.md', 'README.md')
    if destination == reminder_review:
        html = html.replace('if(!location.hash)index=1;', "if(!location.hash)index=screens.findIndex(s=>s.key==='withdraw-reminder');")
        html = html.replace('../OrchardAllScreens-20260928/README.md', 'README.md')
    if destination == rate_review:
        html = html.replace('if(!location.hash)index=1;', "if(!location.hash)index=screens.findIndex(s=>s.key==='rate-up');")
        html = html.replace('../OrchardAllScreens-20260928/README.md', 'README.md')
    if destination == service_review:
        html = html.replace('if(!location.hash)index=1;', "if(!location.hash)index=screens.findIndex(s=>s.key==='service');")
        html = html.replace('../OrchardAllScreens-20260928/README.md', 'README.md')
    if destination == daily_review:
        html = html.replace('if(!location.hash)index=1;', "if(!location.hash)index=screens.findIndex(s=>s.key==='daily-mission');")
        html = html.replace('../OrchardAllScreens-20260928/README.md', 'README.md')
    if destination == spin_review:
        html = html.replace('if(!location.hash)index=1;', "if(!location.hash)index=screens.findIndex(s=>s.key==='lucky-spin');")
        html = html.replace('../OrchardAllScreens-20260928/README.md', 'README.md')
    if destination == help_review:
        html = html.replace('if(!location.hash)index=1;', "if(!location.hash)index=screens.findIndex(s=>s.key==='lucky-help');")
        html = html.replace('../OrchardAllScreens-20260928/README.md', 'README.md')
    (destination/'comparison.html').write_text(html,encoding='utf-8')
    if destination == out:
        (out/'review-index.json').write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'screens':len(data),'runtime':sum(bool(s['runtime']) for s in data),'short':sum(bool(s['short']) for s in data)}))
