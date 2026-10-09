"""Build local review artifacts from saved evidence. Does not modify images or game assets."""
from pathlib import Path
import json
import difflib
from datetime import datetime, timezone

folder = Path(__file__).resolve().parent
root = folder.parent.parent
read = lambda p: json.loads(p.read_text(encoding='utf-8-sig'))
screens = read(folder / 'screen-map.json')['screens']
layouts = {p['name']: p for p in read(folder / 'layouts.json')['pages']}
keys = ['withdraw-main','loading','game-hud','tutorial','level-complete','revive','settings',
        'get-booster','new-booster','daily-tasks','rating','cash-withdraw','withdraw-account',
        'withdraw-confirm','withdraw-pending','history','withdraw-milestones','withdraw-reminder',
        'rate-up','faq','service','service-topics','welcome-gift','daily-mission','lucky-spin','lucky-help','notification']
runtime = dict(zip(['withdraw-main','cash-withdraw','settings','faq','history','service','withdraw-milestones',
                   'lucky-spin','lucky-help','daily-mission','daily-tasks','withdraw-account'],
                  ['00-RealWithdrawPanel','01-FakeWithdrawPanel','02-PausePanel','03-FAQPanel','04-WithdrawHistory',
                   '05-ServicePanel','06-WithdrawDanPanel','07-SlotPanel','08-SlotFAQPanel','09-DailyMissionPanel',
                   '10-UI_DailyTaskPage','11-UIWithdrawalPanel']))
if (folder / 'Runtime/gameplay.png').exists(): runtime['game-hud'] = 'gameplay'
data = []
for s, key in zip(screens, keys):
    ref = '../' + s['referenceSet'] + '/' + s['image']
    preview = 'Previews/' + key + '.png'
    actual = 'Runtime/' + runtime[key] + '.png' if key in runtime else None
    assert (folder / ref).exists(), ref
    assert (folder / preview).exists(), preview
    if actual and not (folder / actual).exists(): actual = None
    data.append(dict(id=s['id'], title=s['title'], key=key, reference=ref, preview=preview,
                     runtime=actual, prefab=layouts[key]['prefab']))
(folder/'review-index.json').write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')

html = '''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>精致果园 · Unity 实装对照</title><style>
*{box-sizing:border-box}body{margin:0;background:#f5f6ef;color:#263d31;font:15px/1.6 system-ui,"Microsoft YaHei",sans-serif}header{padding:22px 28px;background:#173f32;color:#fff}h1{margin:0;font-size:25px}header p{margin:7px 0 0;color:#d4e8d9}main{display:grid;grid-template-columns:240px 1fr;gap:24px;padding:24px}nav{display:flex;flex-direction:column;gap:5px;max-height:85vh;overflow:auto;position:sticky;top:20px;align-self:start}button,select,a{font:inherit}nav button{border:0;text-align:left;border-radius:9px;padding:10px 12px;cursor:pointer;background:transparent;color:#263d31}nav button.active{background:#286a4c;color:white}nav small{display:block;opacity:.65;font-size:11px}.toolbar{display:flex;justify-content:space-between;gap:12px;align-items:center}h2{margin:0}select{padding:8px;border-radius:8px;border:1px solid #bcc9bf}.note{padding:12px 16px;background:#e8eddc;border-radius:10px;margin:15px 0;font-size:13px}code{word-break:break-all;font-size:12px}.pair{display:grid;grid-template-columns:1fr 1fr;gap:20px}.pair figure{margin:0;background:#fff;padding:14px;border-radius:14px}.pair figcaption{font-weight:650;margin-bottom:12px}.pair img{width:100%;height:auto;display:block;border-radius:7px}.pair a{display:block}footer{padding:25px;font-size:12px;color:#6d796f}@media(max-width:900px){main{display:block}nav{position:static;max-height:180px;margin-bottom:20px}.pair{gap:8px}.pair figure{padding:7px}.toolbar{align-items:start;flex-direction:column}}
</style><header><h1>精致果园 · Unity 实装对照</h1><p>两批效果图合并为 27 个界面。左侧为设计参考，右侧为保存的 Unity 画面。</p></header>
<main><nav id="nav"></nav><section><div class="toolbar"><h2 id="title"></h2><select id="mode"><option value="runtime">优先查看运行截图</option><option value="preview">查看预制体静态预览</option></select></div>
<p class="note" id="note"></p><div class="pair"><figure><figcaption>设计参考</figcaption><a id="refLink" target="_blank"><img id="ref" alt="设计参考"></a></figure><figure><figcaption id="actualCaption"></figcaption><a id="actualLink" target="_blank"><img id="actual" alt="Unity 画面"></a></figure></div><p><code id="prefab"></code></p></section></main>
<footer>静态预览中的示例值只应用于临时预览对象。运行截图来自 InitWZ 正式启动后的当前账号；金额、语言、关卡、可见业务分支以真实配置为准。对照页用于视觉检查，不代表逐像素一致或全部业务验收。</footer>
<script>const screens=SCREEN_DATA;let selected=0;const $=id=>document.getElementById(id);screens.forEach((s,i)=>{let b=document.createElement('button');b.textContent=s.id+' · '+s.title;let m=document.createElement('small');m.textContent=s.runtime?'有运行截图':'静态预览';b.append(m);b.onclick=()=>{selected=i;render()};$('nav').append(b)});function render(){const s=screens[selected],live=$('mode').value==='runtime'&&s.runtime;Array.from($('nav').children).forEach((b,i)=>b.classList.toggle('active',i===selected));$('title').textContent=s.title;$('ref').src=$('refLink').href=s.reference;$('actual').src=$('actualLink').href=live?s.runtime:s.preview;$('actualCaption').textContent=live?'Unity 运行截图':'Unity 预制体静态预览';$('note').textContent=live?'正式框架初始化后的界面。不同屏幕比例、账号状态和业务数据会改变内容长度与可见状态。':'隔离预制体渲染，业务脚本停用。用于检查美术、排版和资源；不代表此流程已经完成运行验证。';$('prefab').textContent=s.prefab} $('mode').onchange=render;render();</script></html>'''
(folder/'comparison.html').write_text(html.replace('SCREEN_DATA',json.dumps(data,ensure_ascii=False)),encoding='utf-8')

before=read(root/'Design/OrchardUI/audit-implementation-before.json')
after=read(folder/'audit-current.json')
b={x['key']:x for x in before['issues']}; a={x['key']:x for x in after['issues']}
comparison={'generatedUtc':datetime.now(timezone.utc).isoformat(),
            'baseline':'Design/OrchardUI/audit-implementation-before.json',
            'current':'Design/OrchardImplementation-20260928/audit-current.json',
            'newIssues':[v for k,v in a.items() if k not in b],
            'resolvedIssues':[v for k,v in b.items() if k not in a],
            'remainingIssues':len(a),'beforeErrors':before['errors'],'afterErrors':after['errors'],
            'missingScripts':after['missingScriptObjects'],'missingLocalReferences':after['missingLocalReferences']}
(folder/'audit-comparison.json').write_text(json.dumps(comparison,ensure_ascii=False,indent=2),encoding='utf-8')
diff=[]
for old in (folder/'BeforeAdditional').rglob('*.cs'):
    relative=old.relative_to(folder/'BeforeAdditional'); new=root/relative
    diff.extend(difflib.unified_diff(old.read_text(encoding='utf-8-sig').splitlines(),new.read_text(encoding='utf-8-sig').splitlines(),fromfile=str(relative)+' before',tofile=str(relative)+' after',lineterm=''))
(folder/'runtime-code-changes.diff').write_text('\n'.join(diff),encoding='utf-8')
print(json.dumps({'screens':len(data),'runtimeImages':sum(bool(d['runtime']) for d in data),'newAuditIssues':len(comparison['newIssues']),'resolvedAuditIssues':len(comparison['resolvedIssues'])}))
