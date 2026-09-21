"""Build a self-contained, escaped, credential-redacted API replacement report."""
import datetime
import difflib
import hashlib
import html
import json
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
PROJECT = ROOT / 'BizzaWZ'

def read(name):
    return json.loads((HERE / name).read_text(encoding='utf-8-sig'))

def esc(value):
    if isinstance(value, (dict, list)):
        value = json.dumps(value, ensure_ascii=False, indent=2)
    elif value is None:
        value = '—'
    elif value is True:
        value = '通过'
    elif value is False:
        value = '否'
    return html.escape(str(value))

def table(headers, rows):
    return '<div class="table-wrap"><table><thead><tr>' + ''.join('<th>'+esc(h)+'</th>' for h in headers) + '</tr></thead><tbody>' + ''.join('<tr>'+''.join('<td>'+esc(c)+'</td>' for c in row)+'</tr>' for row in rows) + '</tbody></table></div>'

def detail(title, data):
    return '<details><summary>'+esc(title)+'</summary><pre>'+esc(data)+'</pre></details>'

api = read('api-mapping.json')
release = read('release-config-results.json')
communication = read('communication-results.json')
contract = read('method-audit-results.json')
network_audit = read('communication-contract-audit.json')
compile_result = read('compilation-results.json')
obfuz = read('obfuz-generation-results.json')
verification = read('final-verification.json')

parts = []
def section(anchor, title, content):
    parts.append('<section id="'+anchor+'"><h2>'+title+'</h2>'+content+'</section>')

section('summary', '执行结论', '''<div class="notice">已完成所有可确认的替换；仍有 <strong>3 项 Unresolved</strong>，因此不能标记为全部接口无差异完成。目标文档缺失的路径及被业务引用的无对应字段已保留。</div>''' +
    table(['检查项目', '结果'], [
        ['接口路径', f'{len(api["pathMappings"])} 条已更新；order_name 无目标路径，保留旧值'],
        ['DTO / 真实请求', f'{len(api["fieldMappings"])} 个 JsonProperty 键、{len(api["jsonKeyMappings"])} 个归因字典键；7 字段删除、1 字段新增'],
        ['Other 配置', '根据实际服务器返回确认并替换 6 个 hp_* 键'],
        ['方法契约', '独立 Roslyn 三组宏检查通过；所有方法签名和调用序列保持，方法体只含允许的键名/提款字段赋值变更'],
        ['发行配置 / ChannelConfig', '项目原序列化器及实际 Unity 往返验证通过；其余序列化字段逐字节保留'],
        ['Obfuz', '设置、静态密钥、动态密钥、生成 VM 同步且校验通过'],
        ['Unity 编译', compile_result['status'] + ' · Unity 2022.3.62f3 · Android 目标 · 最终退出码 '+str(compile_result['finalExitCode'])],
        ['API 通信', '7/7 通过（HTTP 200 + 业务 200 + XXTEA 解密）；12 项未执行，原因逐项记录'],
        ['验证边界', 'HTTP 脚本使用项目 XXTEA 源码；未运行 Android/iOS 真机测试；归因 payload 为静态检查，未发送真实归因上报']
    ]))

unresolved_notes = {
    'Os_Rol': '目标无卷余额字段，GetInfo 仍引用；保留旧键。若服务端不返回该字段，将维持默认值，需确认业务预期。',
    'Os_Tsm': '目标无现金提现失败提示字段，提现历史 UI 仍引用；保留旧键。该提示无法按当前目标文档完整同步。',
    'order_name': '精确 FruitTileMatch Tag 无对应提现用户列表路径；当前工程无调用点，保留旧路径。需要后端给出明确路径后才能替换。'
}
section('unresolved', '未解决项与 Swagger 类型差异',
    table(['位置', '保留内容', '原因与影响'], [
        [x.get('className', 'AccountModuleCfg')+'.'+x.get('field', x.get('symbol', 'order_name')), x.get('old', x.get('path', '')), unresolved_notes.get(x.get('field', x.get('symbol', 'order_name')), x['reason'])]
        for x in api['Unresolved']
    ]) + '<h3>SwaggerTypeMismatch · 保留本地已验证调用契约</h3>' +
    table(['接口', 'Swagger', '本地类型', '处理'], [[x['path'],x['swaggerResponse'],x['localResponse'], '保持本地方法、回调和 HTTP 泛型；Task 实际返回数组，与本地一致。' if x['symbol']=='task_list' else '保留本地单对象调用；未执行有业务写入的真实请求。'] for x in api['SwaggerTypeMismatch']]))

section('parameters', '发行参数：修改前后与落点',
    '<p>仅使用本次用户参数；Markdown 中的转义符不属于密钥值。密钥显示已脱敏。</p>' +
    table(['原始参数名','归一化参数','最终目标字段','修改前','修改后','状态'], [[x['originalParameter'],x['standardParameter'],x['target'],x['before'],x['after'],x['status']] for x in release['fields']]) +
    '<h3>留空项</h3>' + table(['原始参数','标准参数','目标字段','保留值','原因'], [[x['originalParameter'],x['standardParameter'],x['target'],x['value'],'本次输入为空，按提示词保留'] for x in release['preserved']]) +
    detail('隐私链接运行时加载路径与 32 种语言检查', release.get('privacyLocalization', {})))

section('paths', '接口路径与 Other 配置',
    table(['常量','接口含义','修改前','修改后','HTTP'], [[x['symbol'],x['targetSummary'],x['old'],x['new'],x['method']] for x in api['pathMappings']]) +
    '<p>仅更新 AccountModuleCfg 已有非空接口。user_name 有配置路径但本地没有请求实现；保留此结构，未新增调用。look_ad_reward_mul 配置值保持不变。</p>' +
    table(['常量','修改前','修改后','实测值','证据接口'], [[x['symbol'],x['old'],x['new'],x['responseValue'],x['endpoint']] for x in api['otherMappings']]))

section('payload', '归因真实请求与递归字段检查',
    '<div class="flow">Adjust 归因回调 → CreateFromJson → Request_UserAttrsRequest → JsonConvert.SerializeObject → HttpUtil → /Api/Match/Come</div>' +
    '<p><strong>验证方式：静态分析，未实际运行归因序列化或上报。</strong>已检查 Adjust 宏内代码及全部 19 个真实字典项：字段集合无遗漏、无重复、无额外旧键，值表达式、空值处理、时间单位和类型未改变。</p>' +
    table(['原 JSON 路径','目标 JSON 路径','值表达式（保持）','目标类型','字段含义'], [[x['oldJsonPath'],x['jsonPath'],x['valueExpression'],x['targetType'],x['targetDescription']] for x in api['jsonKeyMappings']]) +
    detail('实际 payload 静态检查结果',api['validation']['actualAttributionPayload']) +
    detail('17 条实际请求的递归 JSON 路径与类型检查（含广告嵌套对象）',api['validation'].get('recursiveRequestPayloads',{})))

section('fields', 'DTO 字段差异与反向检查',
    '<p>现有 C# 字段名称和类型保持；表内行号以修改前基线为准。提款 DTO 的增删同步到两个允许调整的构造方法赋值。</p>' +
    table(['DTO / C# 字段','类型','原键','目标键','目标字段含义'], [[x['className']+'.'+x['field'],x['csharpType'],x['old'],x['new'],x.get('targetDescription','')] for x in api['fieldMappings']]) +
    '<h3>DTO 字段增删</h3>' + table(['DTO / 字段','动作','原键','新键','原因 / 取值'], [[x['className']+'.'+x['field'],x['action'],x.get('old','—'),x.get('new','—'),x.get('reason',x.get('value',''))] for x in api['dtoChanges']]) +
    detail('反向字段检查（被保留的 2 个响应旧键明确列为 Unresolved）',api['validation']['reverseDtoFields']))

method_rows=[]
for v in contract['variants']:
    method_rows.append([', '.join(v['symbols']),v['methods'],v['unchanged'],len(v['changed']),len(v['added']),len(v['removed']),all(m['invocationSequenceUnchanged'] for m in v['changed'])])
section('contract', '方法契约验证',
    '<p>Roslyn 按 C# 语法树分别检查 Debug/Editor、Android/Release 和无 Adjust 三组宏。全部声明（含构造函数）一致。发生方法体变化的仅 CreateFromJson 和两个提款构造方法，内部调用序列全部一致。</p>' +
    table(['宏配置','方法声明数','方法体完全一致','合法变更方法数','新增声明','删除声明','调用序列一致'],method_rows) +
    detail('逐项允许 token 投影与提款例外',api['validation']['methodContract']) +
    detail('独立方法体差异（完整前后内容，便于复核）',contract))

section('configuration', 'ChannelConfig 与 Obfuz 校验',
    '<p>ChannelConfig.bytes 使用项目当前 ChannelConfigBinarySerializer 序列化；没有对二进制执行文本替换。先用当前源代码离线执行，再在实际 Unity 中反读验证。把本次授权字段恢复成原值后，序列化字节与原始基线完全一致。</p>' +
    table(['检查','结果'],list(release['roundtrip'].items())) +
    detail('Unity 中的 Obfuz 生成和配置校验',obfuz))

section('compile', '编译检查',
    table(['项目','值'], [[k,v] for k,v in compile_result.items() if k not in ('warnings','errors')]) +
    detail('编译错误',compile_result.get('errors',[])) +
    detail('编译警告（去重；未扩大范围修改原项目）',compile_result.get('warnings',[])))

section('communication', 'API 通信结果',
    '<p>使用一次合成测试登录及后续读取操作。未使用真实用户/设备身份；该登录可能创建服务端测试账号。请求使用当前 AppId、包名、域名和密钥，HTTP 测试通过不代表每项业务流程或真机验证已完成。</p>' +
    table(['接口','请求用途','HTTP','业务码','结果','响应根类型'], [[x['path'],x['purpose'],x.get('httpStatus','—'),x.get('businessCode','—'),x['status'],x.get('response',{}).get('decryptedRootType','—')] for x in communication['tests']]) +
    '<h3>未执行的通信</h3>' + table(['路径','状态','原因'],[[x['path'],'未执行',x['reason']] for x in communication['notExecuted']]) +
    '<p>空订单/反馈列表未观察到元素内容，因此其子字段只有文档静态检查覆盖。/Api/Match/Task 实测根为数组，与本地 List 契约一致。</p>' +
    detail('实际请求和脱敏响应',communication) + detail('实际通信与最终工程的字段 / 类型交叉核对',network_audit))

section('files', '最终文件检查与本次差异',detail('验证清单与修改文件 SHA-256',verification))
for filename in ['AccountModuleCfg.cs','AccountModule.cs']:
    before=(HERE/'baseline/API'/filename).read_text(encoding='utf-8-sig').splitlines()
    after=(PROJECT/'Assets/BizzaWZ/Final/Connect_SDK/SDK_WKY/API'/filename).read_text(encoding='utf-8-sig').splitlines()
    diff='\n'.join(difflib.unified_diff(before,after,fromfile='执行前基线/'+filename,tofile='执行后/'+filename,lineterm=''))
    parts.append(detail(filename+' 本次完整差异（基于用户当前工作区）',diff))

section('provenance','来源与复核资料',table(['项目','来源'],[
    ['执行提示词','C:/Users/pc/Desktop/PlayCloudSdk/PlayCloud_API/APIReplacePrompt.md'],
    ['目标项目',str(PROJECT)],
    ['接口标签',api['tag']],
    ['Swagger 缓存',api['swaggerCache']],
    ['缓存 SHA-256',api['cacheSha256']],
    ['基线','执行前当前工作区快照；保留用户已有未提交修改'],
    ['参数','只使用本次用户消息；未读取 ParamTemp.md 或旧发行参数'],
    ['测试边界','未构建 APK/IPA，未执行真机游戏流程，未发起真实提现或广告/收益写入']
]))

css='''*{box-sizing:border-box}html{scroll-behavior:smooth}body{margin:0;background:#eef2f7;color:#1a2b42;font:14px/1.65 "Segoe UI","Microsoft YaHei",sans-serif}header{background:#152840;color:#fff;padding:44px max(24px,calc((100vw - 1380px)/2)) 36px}header .eyebrow{color:#8bdbc6;letter-spacing:2px;font-size:12px}h1{font-size:32px;line-height:1.3;margin:12px 0}header p{color:#b8c8da;margin:5px 0}main{max-width:1428px;margin:auto;padding:24px}nav{display:flex;flex-wrap:wrap;gap:8px;margin-bottom:22px}nav a{background:#fff;border:1px solid #d5dee9;border-radius:7px;padding:7px 12px;color:#254b74;text-decoration:none}section{background:#fff;border:1px solid #dce4ee;border-radius:12px;padding:24px;margin-bottom:20px;scroll-margin-top:12px}h2{font-size:21px;margin:0 0 17px}h3{font-size:16px;margin-top:24px}.notice{padding:15px 18px;border-left:4px solid #da922b;background:#fff5df;margin-bottom:18px}.flow{background:#eaf5f3;color:#1e6459;padding:16px;border-radius:8px;font-weight:600;margin-bottom:16px}.table-wrap{overflow-x:auto}table{width:100%;border-collapse:collapse;font-size:13px}td,th{padding:11px 12px;text-align:left;vertical-align:top;border-bottom:1px solid #e2e8f0;overflow-wrap:anywhere;min-width:90px}th{background:#f0f4f9;color:#4b5e76;font-size:12px}tr:nth-child(even){background:#fbfcfe}details{border:1px solid #dce4ee;border-radius:8px;margin:14px 0;background:#fff}summary{cursor:pointer;font-weight:600;padding:13px 16px;color:#335779}pre{font:12px/1.65 Consolas,"Microsoft YaHei",monospace;white-space:pre-wrap;overflow-wrap:anywhere;background:#f6f8fb;padding:18px;margin:0;border-top:1px solid #dce4ee;max-height:650px;overflow:auto}footer{padding:10px 0 32px;color:#62748c;text-align:center;font-size:12px}.badge{display:inline-block;border:1px solid #76865e;color:#fbe0a1;border-radius:18px;padding:3px 12px;margin-top:15px}@media(max-width:700px){main{padding:12px}section{padding:16px}header{padding:28px 20px}h1{font-size:25px}}@media print{body{background:#fff}nav{display:none}header{padding:20px}main{padding:0}section{break-inside:auto;border:0}pre{max-height:none}details{break-inside:auto}}'''
nav=[('summary','结论'),('unresolved','未解决项'),('parameters','发行参数'),('paths','路径'),('payload','归因'),('fields','字段'),('contract','方法契约'),('configuration','二进制 / Obfuz'),('compile','编译'),('communication','通信'),('files','文件差异')]
document='<!doctype html><html lang="zh-CN"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><title>Bubblosaic API 替换报告</title><style>'+css+'</style></head><body><header><div class="eyebrow">API REPLACEMENT / FRUIT TILE MATCH</div><h1>Bubblosaic: Picture Merge</h1><p>bubblosaicpmge · com.webpack.picturemerge · 真假混 · NONE</p><p>'+esc(datetime.datetime.now().astimezone().isoformat(timespec='seconds'))+'</p><span class="badge">已确认项已完成 · 3 项待确认</span></header><main><nav>'+''.join('<a href="#'+a+'">'+t+'</a>' for a,t in nav)+'</nav>'+''.join(parts)+'</main><footer>自包含本地报告 · 敏感凭证已脱敏 · 基于本次工作区基线</footer></body></html>'
out=HERE/'API替换报告-bubblosaicpmge.html'
out.write_text(document,encoding='utf-8')
print(json.dumps({'report':str(out),'bytes':out.stat().st_size,'sha256':hashlib.sha256(out.read_bytes()).hexdigest()}))
