"""Build the current Orchard Trio report solely from this run's audit JSON files.

This script does not modify the Unity project, rerun migration, or make requests.
Missing final validation files are shown as pending and can be supplied later.
"""
from datetime import datetime, timezone
from pathlib import Path
import html
import json
import re
import sys

HERE = Path(__file__).resolve().parent
OUT = HERE / 'APIReplacementReport.html'
sys.stdout.reconfigure(encoding='utf-8')

def load(name, required=True):
    path = HERE / name
    if not path.exists():
        if required:
            raise FileNotFoundError(path)
        return {'status': 'Pending', 'reason': '结果文件尚未生成；未据此宣称通过。', 'evidence': name}
    return json.loads(path.read_text(encoding='utf-8-sig'))

def mask(value):
    if value in (None, ''):
        return value
    value = str(value)
    if '[REDACTED]' in value or '***' in value:
        return value
    return value[:3] + '***' + value[-3:] if len(value) > 6 else '***'

def sanitize(value):
    if isinstance(value, list):
        return [sanitize(x) for x in value]
    if isinstance(value, dict):
        result = {key: sanitize(val) for key, val in value.items()}
        label = ' '.join(str(value.get(k, '')) for k in ['originalParameter', 'standardParameter', 'target'])
        if re.search(r'密钥|SDK Key|aes_key|sdkKey|SecretKey', label, re.I):
            for key in ['before', 'after', 'value']:
                if key in result:
                    result[key] = mask(result[key])
        for key in result:
            if key.lower() in {'aes_key', 'sdkkey', 'access_token', 'accesstoken', 'authorization', 'password', 'clientsecret', 'token', 'uid', 'sojusid', 'sojanid', 'sojseid', 'sojgaid'}:
                result[key] = mask(result[key])
        return result
    if isinstance(value, str):
        return re.sub(r'Bearer\s+[A-Za-z0-9._~+/=-]+', 'Bearer [REDACTED]', value)
    return value

api = sanitize(load('api-mapping.json'))
release = sanitize(load('release-config-results.json'))
communication = sanitize(load('communication-results.json'))
contract = sanitize(load('communication-contract-check.json'))
methods = sanitize(load('method-audit-results.json'))
unity = sanitize(load('unity-final-validation.json', False))
scope = sanitize(load('source-scope-results.json', False))

# The current user supplied the complete labels. Older labels in helper metadata
# are canonicalized only in this in-memory report; no result file is rewritten.
def current_parameter_labels(node):
    if isinstance(node, dict):
        canonical = node.get('standardParameter')
        if canonical in ['AppLovin SDK Key', 'Adjust 应用识别码'] and 'originalParameter' in node:
            node['originalParameter'] = canonical
        for child in node.values():
            current_parameter_labels(child)
    elif isinstance(node, list):
        for child in node:
            current_parameter_labels(child)
current_parameter_labels(release)

assert api['tag'] == communication['tag'] == 'SnakeOutJame'
assert len(api['fieldMappings']) == 212
assert len(api['pathMappings']) == 19
assert len(api['jsonKeyMappings']) == 19
assert len(api['Unresolved']) == 2

def e(value):
    if value is None:
        value = '—'
    elif isinstance(value, bool):
        value = '是' if value else '否'
    elif isinstance(value, (dict, list)):
        value = json.dumps(value, ensure_ascii=False, indent=2)
    return html.escape(str(value), quote=True)

def status(value):
    if isinstance(value, dict):
        value = value.get('status', value.get('passed', 'Pending'))
    if value is True:
        return '通过'
    if value is False:
        return '未通过'
    return {'passed': '通过', 'alreadymatched': '原值已符合', 'updated': '已更新', 'pending': '待验证', 'failed': '失败'}.get(str(value).lower(), str(value))

def table(headers, rows, cls=''):
    return '<div class="table-scroll"><table class="' + e(cls) + '"><thead><tr>' + ''.join('<th>' + e(x) + '</th>' for x in headers) + '</tr></thead><tbody>' + ''.join('<tr>' + ''.join('<td>' + e(x) + '</td>' for x in row) + '</tr>' for row in rows) + '</tbody></table></div>'

def details(title, data, open_default=False):
    content = json.dumps(sanitize(data), ensure_ascii=False, indent=2) if not isinstance(data, str) else data
    return '<details' + (' open' if open_default else '') + '><summary>' + e(title) + '</summary><pre>' + e(content) + '</pre></details>'

parts = []
def section(id_, title, body):
    parts.append('<section id="' + e(id_) + '"><h2>' + e(title) + '</h2>' + body + '</section>')

passed = sum(t.get('status') == 'passed' for t in communication['tests'])
failed = sum(t.get('status') == 'failed' for t in communication['tests'])
not_executed = len(communication['notExecuted'])
configured_paths = {p['new'] for p in api['pathMappings']}
configured_not_executed = sum(t['path'] in configured_paths for t in communication['notExecuted'])
not_integrated = not_executed - configured_not_executed
recursive = api['validation']['recursiveRequestPayloads']
recursive_passed = sum(p['status'] == 'passed' for p in recursive)
field_counts = {'paths': len(api['pathMappings']), 'fields': sum(x.get('status')=='confirmed' and x['old']!=x['new'] for x in api['fieldMappings']), 'dictionary': len(api['jsonKeyMappings']), 'other': len(api['otherMappings'])}
compile_brief = status(unity)
if 'compilerErrors' in unity:
    compile_brief += f'；{len(unity["compilerErrors"])} 个编译错误，{unity.get("compilerWarningCount", "未提供")} 条去重编译警告。'

section('summary', '执行结论',
    '<div class="notice"><strong>已完成可确认项；现金 / 任务提现接口仍未完成。</strong><br>'
    '/Out/Jam/ApplyOrderTask 的任务 ID 与提现类型存在 2 项字段歧义，已保留并标记 Unresolved。本报告不将这些差异计为成功。</div>'
    + '<div class="metrics">' + ''.join('<div><b>' + str(n) + '</b><span>' + label + '</span></div>' for n, label in [(field_counts['paths'], '接口路径同步'), (field_counts['fields'], 'DTO 键名同步'), (field_counts['dictionary'], '真实字典键同步'), (passed, '接口通信通过')]) + '</div>'
    + table(['检查项目', '结果'], [
        ['字段结构', '新增 6 个 DTO 字段，删除 0 个字段；现有 C# 字段名及类型保持。'],
        ['请求递归检查', f'{len(recursive)} 条已有请求中 {recursive_passed} 条完全匹配；现金 / 任务提现请求明确未完成。'],
        ['Other 配置', '6 个 hp_* → soj_*；依据本轮 BaseData 实际解密响应，look_ad_reward_mul 值保持。'],
        ['方法契约', status(methods.get('passed')) + '；三组宏检查声明与调用，另以允许修改清单核验方法体。'],
        ['归因验证', '仅静态分析：19 个字典键完整且唯一；未实际运行归因序列化或上报。'],
        ['ChannelConfig', status(release.get('channelConfig', {}))],
        ['Obfuz', status(release.get('obfuz', {}))],
        ['Unity 最终验证', compile_brief],
        ['源码范围验证', status(scope)],
        ['API 通信', f'19 个已有配置接口中 {passed} 项通过、{failed} 项失败、{configured_not_executed} 项未执行；另 {not_integrated} 项未接入接口未执行。'],
        ['验证边界', '未声称完成 APK / IPA 构建、Android / iOS 真机流程或真实提现验证。'],
    ]))

section('unresolved', '未完成项与调用类型差异',
    '<h3>现金 / 任务提现：Incomplete / Unresolved</h3><p>目标 Swagger 仅定义 <code>SoJMn</code>，其描述却包含 '
    '<code>ApplyType string json:&quot;SoJAt&quot;</code>。当前代码分别用 <code>Os_Mn</code> 传任务标识 '
    '<code>task_{id}</code>，用 <code>Os_At</code> 传 <code>money</code>。证据不足以确认两个字段如何映射，未合并或猜测新键。</p>'
    + table(['字段', '当前保留键', '目标候选', '实际取值', '状态'], [[x['className'] + '.' + x['field'], x['old'], x.get('candidate'), x.get('localValue'), '未解决'] for x in api['Unresolved']])
    + '<p>结果：该接口仍缺少已确认映射的 <code>SoJMn</code>，保留 <code>FtMMn</code>、<code>FtMAt</code> 两个旧键；不能作为已完成提现接入交付。需要后端明确任务 ID、提现类型各自的请求键。</p>'
    + '<h3>商品 ID：默认值的验证边界</h3><p>两个提款 DTO 新增 <code>SoJGdid</code>（<code>int Os_Gdid</code>），对应构造方法增加 '
    '<code>Os_Gdid = 0</code>。<strong>这是 C# 整数默认值，Swagger 未定义默认值。</strong>项目现有构造参数未提供独立商品 ID，尚未验证服务器接受 0；不得视为提现业务已验证。</p>'
    + '<h3>SwaggerTypeMismatch：保留本地方法契约</h3>'
    + table(['接口', 'Swagger 响应类型', '本地类型', '处理'], [[x['path'], x['swaggerResponse'], x['localResponse'], '保留方法、回调与 HTTP 泛型；Task 实测根为数组，与本地相符。' if x['symbol']=='task_list' else '保留本地单对象契约；未发送会改变业务状态的请求。'] for x in api['SwaggerTypeMismatch']]))

parameter_rows = [[x.get('originalParameter'), x.get('standardParameter'), x.get('target'), x.get('before'), x.get('after'), status(x.get('status'))] for x in release.get('fields', [])]
parameter_body = '<p>参数来源为本次请求，按本次完整名称 AppLovin SDK Key、Adjust 应用识别码处理；应用 ID 独立写入后台 AppId。旧值仅用于工程现状对比，密钥仅显示脱敏值。</p>' + table(['原始参数名', '规范化参数', '最终字段', '修改前', '修改后', '状态'], parameter_rows)
if release.get('preserved'):
    parameter_body += '<h3>本次留空项：保留原值</h3>' + table(['原始参数', '规范化参数', '字段', '保留值', '原因'], [[x.get('originalParameter'), x.get('standardParameter'), x.get('target'), x.get('value'), '本次留空，按提示词保留原值。'] for x in release['preserved']])
else:
    parameter_body += '<p class="muted">默认国家、横幅广告 ID、开屏广告 ID 的留空保留验证等待最终 ChannelConfig 结果；不填入猜测值。</p>'
parameter_body += details('发行配置完整结果（敏感值脱敏）', release)
section('parameters', '发行参数与规范化落点', parameter_body)

section('paths', '19 条接口路径与 Other 配置',
    table(['常量', '接口用途', '修改前', '修改后', '实现状态'], [[x['symbol'], x['targetSummary'], x['old'], x['new'], '现有常量，无本地请求方法' if x['symbol'] in ['order_name','user_name'] else '字段歧义，接口未完成' if x['symbol']=='add_order_task' else '确认字段已同步'] for x in api['pathMappings']])
    + '<p>只更新当前 AccountModuleCfg 的已有非空路径。OrderNick / NickName 未新增请求方法；本 Tag 的两条彩票接口未加入工程配置。</p>'
    + '<h3>Other 配置：真实返回值作为证据</h3>'
    + table(['常量', '修改前', '修改后', '本次服务器返回', '证据'], [[x['symbol'], x['old'], x['new'], x['responseValue'], x.get('path', x.get('endpoint'))] for x in api['otherMappings']])
    + '<p>请求配置值保持 <code>look_ad_reward_mul</code>；<code>next_day</code> 无现有消费字段，本轮未新增业务逻辑。</p>')

section('attribution', '归因实际构造路径与 payload 核对',
    '<div class="flow">Adjust 归因回调 → CreateFromJson → Request_UserAttrsRequest → JsonConvert.SerializeObject → HttpUtil → /Out/Jam/Come</div>'
    '<p><strong>此项为静态检查，未实际执行 Unity 序列化或归因上报。</strong>检查覆盖 BIZZA_REAL_WITHDRAW / BIZZA_ENABLE_ADJUST 宏内代码，'
    '字典键独立于 DTO 属性同步。该请求是 19 个字段的平面对象，无嵌套对象或数组。空值处理、时间单位、字段值表达式及类型保持。</p>'
    + table(['旧 JSON 路径', '新 JSON 路径', '取值表达式（保持）', '目标类型', '目标语义'], [[x['oldJsonPath'], x['jsonPath'], x['valueExpression'], x['targetType'], x['targetDescription']] for x in api['jsonKeyMappings']])
    + details('归因静态检查明细', api['validation']['actualAttributionPayload'])
    + '<h3>已有请求的递归字段检查</h3>'
    + table(['接口', 'DTO', '结果', '缺失路径', '额外路径', '类型不符'], [[x['path'], x['className'], status(x['status']), x['missing'], x['extra'], x.get('typeMismatch', [])] for x in recursive])
    + '<p>广告加载上报 HttpAdAdapterBase 使用同一 OceanShineAdLogReportRequest；commonInfo / extendParam 的嵌套字段按该 DTO 同步。静态字段检查不替代实际广告发送。</p>'
    + details('完整递归 JSON 路径与类型', recursive))

section('fields', 'API 字段逐项对比',
    '<p>以下为 212 个已确认的 JsonProperty 映射。按 JSON 键的大小写敏感规则统计，212 项发生变更（包括归因版本键 <code>vn → Vn</code>）。每项保留现有 C# 成员名和类型，并列出目标 Schema 描述；2 个未解决字段在前文单列。</p>'
    '<label class="search-label">筛选字段 / DTO / 键名 <input id="field-search" type="search" placeholder="例如 Os_Cty、SoJVn、Login" autocomplete="off"></label>'
    '<p class="muted" id="field-count">显示全部 212 项</p>'
    + table(['DTO / C# 字段', 'C# 类型', '修改前 JSON 键', '修改后 JSON 键', '旧 Schema 含义', '目标 Schema 含义'], [[x['className'] + '.' + x['field'], x['csharpType'], x['old'], x['new'], x['oldDescription'], x['targetDescription']] for x in api['fieldMappings']], 'field-table')
    + '<h3>6 个 DTO 字段新增</h3>'
    + table(['DTO / 字段', '类型', '目标 JSON 键', '目标含义', '状态'], [[x['className'] + '.' + x['field'], x['csharpType'], x['new'], x['targetDescription'], '新增'] for x in api['DTOChanges']])
    + '<h3>反向字段检查</h3>'
    + table(['DTO', '结果', '目标缺失键', '额外保留键', '重复键'], [[x['className'], status(x['status']), x['missing'], x['extra'], x['duplicates']] for x in api['validation']['reverseDtoFields']]))

method_rows = [[', '.join(v['defines']), v['methodCount'], v['unchanged'], len(v['changes']), len(v['added']), len(v['removed']), all(c['callsUnchanged'] for c in v['changes'])] for v in methods['variants']]
section('methods', '方法契约与调用关系',
    '<p>独立 Roslyn 检查包括构造函数声明及调用序列，覆盖 Editor / Debug、Android 和无 Adjust 三组宏。方法体变更仅限 '
    '<code>CreateFromJson</code> 的 19 个确认键名，以及两个提款构造方法新增的商品 ID 默认赋值。</p>'
    + table(['预处理宏', '方法数', '方法体未变', '允许变更方法数', '新增方法', '删除方法', '调用序列相同'], method_rows)
    + details('允许修改投影检查', api['validation']['methodContract'])
    + details('Roslyn 独立检查完整结果', methods))

section('configuration', 'ChannelConfig 与 Obfuz',
    '<h3>二进制配置</h3><p>以项目自己的 ChannelConfigBinarySerializer 执行序列化；下方状态取自本轮实际执行结果。未将二进制配置作为文本替换。</p>'
    + details('ChannelConfig 序列化和回读验证', release.get('channelConfig', {'status':'Pending'}), True)
    + details('其它序列化字段保留验证', release.get('roundtrip', {'status':'Pending'}))
    + '<h3>Obfuz</h3><p>Assembly-CSharp 为目标程序集；静态 / 动态密钥以本次包名配置，代码生成密钥使用包名末段，名称前缀使用本轮随机三字母。密钥值在报告中脱敏。</p>'
    + details('Obfuz 设置与生成产物', release.get('obfuz', {'status':'Pending'}), True)
    + details('隐私链接与多语言资源验证', release.get('privacyLocalization', {'status':'Pending'})))

section('compile', 'Unity 最终验证与修改范围',
    '<p>编译结果与范围检查来自本轮最终验证文件。文件尚未生成时显示待验证；脚本重跑后自动刷新。</p>'
    + table(['项目', '结果'], [
        ['最终状态', compile_brief], ['Unity 版本', unity.get('unityVersion','待验证')],
        ['当前构建目标', unity.get('activeBuildTarget','待验证')],
        ['移除临时脚本后域重载', unity.get('postRemovalDomainReloadPassed','待验证')],
        ['运行时程序集晚于全部已改源码', unity.get('runtimeAssemblyNewerThanChangedRuntimeSources','待验证')],
        ['源码快照数量（前 / 后）', str(scope.get('beforeFileCount','待验证')) + ' / ' + str(scope.get('afterFileCount','待验证'))],
        ['新增 / 删除 C# 文件', str(len(scope.get('added',[]))) + ' / ' + str(len(scope.get('removed',[]))) if 'added' in scope else '待验证'],
    ])
    + details('Unity 最终编译 / 验证结果（含全部警告）', unity)
    + details('源码修改范围与基线对比', scope, True)
    + '<p>编译通过仅表示本轮代码可编译；不表示已构建 APK / IPA，也不表示 Android / iOS 真机玩法、广告或提现流程已通过。</p>')

purpose_cn = {
 '/Out/Jam/BaseData':'读取当前 Other 配置', '/Out/Jam/Login':'一次合成测试账号登录', '/Out/Jam/UserInfo':'读取测试账号信息',
 '/Out/Jam/TalkList':'读取反馈列表', '/Out/Jam/Order':'读取订单列表', '/Out/Jam/OrderNick':'读取提现用户列表（无本地调用实现）',
 '/Out/Jam/PayList':'读取提现平台', '/Out/Jam/Task':'读取每日任务',
}
skip_cn = {
 '/Out/Jam/AddIncome':'会写入收益，未伪造广告收益。', '/Out/Jam/ApplyOrder':'会提交真实提现；商品 ID 默认 0 的服务器接受情况未验证。',
 '/Out/Jam/ApplyOrderTask':'提现字段有歧义，接口未完成；未提交现金或任务提现。', '/Out/Jam/NewUser':'会领取新用户奖励，未执行。',
 '/Out/Jam/Talk':'会提交反馈消息，未发送。', '/Out/Jam/Come':'未提交合成归因，仅检查真实构造代码。',
 '/Out/Jam/Times':'会改变关卡 / 完成次数，未执行。', '/Out/Jam/IncomeId':'会分配收益跟踪 ID，未执行。',
 '/Out/Jam/NickName':'会修改昵称，未执行。', '/Out/JamLog/AdInfo':'未伪造广告事件上报。', '/Out/JamLog/AppInfo':'未伪造应用事件上报。',
 '/Out/Jam/Lottery':'当前配置未接入此接口。', '/Out/Jam/LotteryInfo':'当前配置未接入此接口；未执行抽奖。',
}
section('communication', 'API 通信与请求 / 响应契约检查',
    '<p>HTTP 集成脚本使用项目 XXTEA 实现，执行一次合成测试登录与后续读取操作；登录可能在服务器创建测试账号。'
    '未使用真实用户 / 设备身份。HTTP 测试不是 Unity 构造器、反序列化器或真机运行测试。</p>'
    + table(['接口', '用途', 'HTTP', '业务码', '结果', '响应根类型'], [[t['path'], purpose_cn.get(t['path'], t.get('purpose')), t.get('httpStatus'), t.get('businessCode'), status(t.get('status')), t.get('response',{}).get('rootType')] for t in communication['tests']])
    + '<h3>未执行项</h3><p>下列 13 项包含 11 个已有配置接口，以及 2 个未接入的彩票接口，均明确记录为未执行，不计入成功或失败。</p>'
    + table(['路径', '状态', '原因'], [[t['path'], '未执行', skip_cn.get(t['path'], t.get('reason'))] for t in communication['notExecuted']])
    + '<h3>最终源码与实际请求 / 响应交叉检查</h3>'
    + table(['接口', '配置路径匹配', '精确 Tag 匹配', '实发请求匹配 Schema', '本地请求类型', '本地响应类型', '实发请求匹配本地 DTO'], [[x['path'], x['configuredPathMatches'], x['exactTagMatches'], x['executedRequestKeysMatchSwagger'], x.get('requestType'), x.get('responseType'), x.get('executedRequestKeysMatchFinalDto','无本地 DTO，仅检查配置和 Schema')] for x in contract['checks']])
    + '<p>实际 Task 根为数组，与保留的本地 List 契约一致。Order 返回 null、反馈消息字段为 null、OrderNick 返回 []，这些空容器未覆盖元素字段。OrderNick 没有本地请求方法，其通信成功不代表新增了游戏内调用。</p>'
    + details('当前源码交叉检查完整记录', contract)
    + details('本轮 HTTP 结果（身份与凭证脱敏）', communication))

source_files = ['api-mapping.json','release-config-results.json','communication-results.json','communication-contract-check.json','method-audit-results.json','unity-final-validation.json','source-scope-results.json']
section('provenance', '证据与复核资料',
    table(['项目', '内容'], [
        ['唯一 Unity 工程', release.get('project')],
        ['应用 ID / Android 包名', communication.get('appId','') + ' / ' + communication.get('bundle','')],
        ['API Tag', api['tag']], ['接口域名', communication.get('domain')],
        ['Swagger 缓存', api['swaggerCache']], ['缓存 SHA-256', api['cacheSha256']],
        ['本轮范围', '仅本次目录的结果数据；不读取旧报告、旧 API 工具或旧参数。'],
    ])
    + '<ul class="evidence-list">' + ''.join('<li><a href="' + e(name) + '">' + e(name) + '</a> · ' + ('已生成' if (HERE/name).exists() else '待生成') + '</li>' for name in source_files) + '</ul>')

css = '''
:root{--ink:#193044;--muted:#64778a;--line:#dce5eb;--teal:#0c7968;--paper:#f1f5f6;--warn:#8b5a0e}*{box-sizing:border-box}html{scroll-behavior:smooth}body{margin:0;background:var(--paper);color:var(--ink);font:14px/1.65 "Segoe UI","Microsoft YaHei",sans-serif}header{background:#173644;color:white;padding:38px max(24px,calc((100vw - 1320px)/2)) 30px}.eyebrow{color:#9fdfd0;font-size:12px;letter-spacing:2px}h1{font-size:34px;line-height:1.2;margin:13px 0}header p{margin:5px 0;color:#c3d8df}.header-state{display:inline-block;border:1px solid #c9a25a;color:#ffe6b0;border-radius:30px;padding:5px 13px;margin-top:13px;font-size:13px}main{max-width:1370px;margin:auto;padding:24px}nav{display:flex;flex-wrap:wrap;gap:7px;margin-bottom:22px}nav a{background:white;color:#28586b;text-decoration:none;border:1px solid var(--line);padding:6px 12px;border-radius:7px}section{border:1px solid var(--line);background:white;border-radius:12px;margin:0 0 20px;padding:24px;scroll-margin-top:12px}h2{font-size:22px;line-height:1.4;margin:0 0 18px}h3{font-size:17px;margin:25px 0 12px}p{margin:12px 0}.notice{background:#fff6e4;border-left:4px solid #dda84d;color:#694b1c;padding:16px 19px;margin-bottom:20px}.metrics{display:grid;grid-template-columns:repeat(4,1fr);gap:12px;margin:18px 0}.metrics>div{border:1px solid #dce9e7;background:#f5fbf9;border-radius:8px;padding:14px 16px}.metrics b{display:block;font-size:29px;color:var(--teal);line-height:1.2}.metrics span{font-size:12px;color:var(--muted)}.table-scroll{overflow:auto;max-width:100%}table{width:100%;border-collapse:collapse;font-size:12.5px}td,th{padding:10px 12px;border-bottom:1px solid var(--line);vertical-align:top;text-align:left;overflow-wrap:anywhere;min-width:82px}th{background:#edf3f6;color:#536a7c;font-size:12px}tr:nth-child(even){background:#fafcfd}code,pre{font-family:Consolas,"Microsoft YaHei",monospace}code{font-size:12px;background:#eff4f6;border-radius:3px;padding:2px 4px}details{border:1px solid var(--line);border-radius:8px;margin:13px 0;overflow:hidden}summary{cursor:pointer;background:#f7fafb;padding:12px 15px;font-weight:600;color:#315e70}pre{font-size:12px;white-space:pre-wrap;overflow-wrap:anywhere;max-height:620px;overflow:auto;padding:16px;margin:0;border-top:1px solid var(--line);color:#29475b;background:#fbfcfd}.flow{border-radius:8px;padding:15px 17px;background:#e7f4f0;color:#17695b;font-weight:600}.muted{color:var(--muted);font-size:12px}.search-label{display:block;font-size:13px;color:#526b7c;margin:18px 0 8px}.search-label input{display:block;width:100%;max-width:520px;border:1px solid #bfcfd8;border-radius:6px;padding:10px 12px;margin-top:6px;font:inherit;color:var(--ink)}.evidence-list{padding-left:22px}.evidence-list a{color:var(--teal)}footer{font-size:12px;text-align:center;color:var(--muted);padding:8px 20px 30px}@media(max-width:720px){main{padding:12px}header{padding:28px 18px}section{padding:16px}h1{font-size:28px}.metrics{grid-template-columns:repeat(2,1fr)}td,th{padding:9px 10px}}@media print{body{background:white}header{padding:20px}main{padding:0}nav,.search-label,#field-count{display:none}section{border:0;border-radius:0}pre{max-height:none}.table-scroll{overflow:visible}}
'''
nav = [('summary','结论'),('unresolved','未解决项'),('parameters','发行参数'),('paths','接口路径'),('attribution','归因与请求'),('fields','逐项字段'),('methods','方法契约'),('configuration','二进制 / Obfuz'),('compile','编译与范围'),('communication','通信'),('provenance','证据')]
now = datetime.now(timezone.utc).isoformat(timespec='seconds')
javascript = '''<script>const box=document.getElementById('field-search');const rows=[...document.querySelectorAll('.field-table tbody tr')];box.addEventListener('input',()=>{const q=box.value.trim().toLowerCase();let count=0;for(const row of rows){const show=row.textContent.toLowerCase().includes(q);row.hidden=!show;if(show)count++;}document.getElementById('field-count').textContent=`显示 ${count} / ${rows.length} 项`;});</script>'''
document = '<!doctype html><html lang="zh-CN"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><title>Orchard Trio · API 替换报告</title><style>' + css + '</style></head><body><header><div class="eyebrow">API REPLACEMENT / SNAKE OUT JAME</div><h1>Orchard Trio</h1><p>' + e(communication.get('appId')) + ' · ' + e(communication.get('bundle')) + '</p><p>生成时间 ' + e(now) + '</p><span class="header-state">确认项已同步 · 现金 / 任务提现仍有 2 项字段歧义</span></header><main><nav>' + ''.join('<a href="#' + id_ + '">' + title + '</a>' for id_, title in nav) + '</nav>' + ''.join(parts) + '</main><footer>本轮本地自包含报告 · 凭证脱敏 · 静态检查、HTTP 集成测试与真机验证分别记录</footer>' + javascript + '</body></html>'
assert len(re.findall(r'<table class="field-table">', document)) == 1
assert 'serialization not executed' in document
assert 'Incomplete / Unresolved' in document
OUT.write_text(document, encoding='utf-8')
print('Report generated: ' + str(OUT))
print(json.dumps({'mappedFields':len(api['fieldMappings']),'paths':len(api['pathMappings']),'unresolvedFields':len(api['Unresolved']),'httpPassed':passed,'httpFailed':failed,'notExecuted':not_executed,'unity':status(unity),'scope':status(scope)},ensure_ascii=False))
