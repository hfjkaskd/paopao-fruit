"""Static check of executed payloads against final local DTOs; no network calls."""
import hashlib
import json
import re
from pathlib import Path

here = Path(__file__).resolve().parent
workspace = here.parents[2]
project = workspace / 'BizzaWZ'
source_path = project / 'Assets/BizzaWZ/Final/Connect_SDK/SDK_WKY/API/AccountModule.cs'
config_path = source_path.with_name('AccountModuleCfg.cs')
source = source_path.read_text(encoding='utf-8-sig')
executed = json.loads((here / 'communication-results.json').read_text(encoding='utf-8-sig'))

# Mask literals/comments only to find class boundaries. JsonProperty text is read
# from the original source at those boundaries; no C# code is run or rewritten.
lex = re.compile(r'//[^\r\n]*|/\*[\s\S]*?\*/|@"(?:[^"]|"")*"|"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'')
masked = lex.sub(lambda m: ''.join('\n' if c == '\n' else ' ' for c in m.group()), source)
brace_end = {}
stack = []
for index, character in enumerate(masked):
    if character == '{':
        stack.append(index)
    elif character == '}':
        brace_end[stack.pop()] = index
classes = []
for match in re.finditer(r'\bclass\s+(\w+)', masked):
    start = masked.index('{', match.end())
    end = brace_end[start]
    parent = next((c for c in reversed(classes) if c['start'] < start < c['end']), None)
    name = (parent['name'] + '.' if parent else '') + match[1]
    classes.append({'name': name, 'short': match[1], 'start': start, 'end': end, 'fields': {}})
field_pattern = re.compile(r'\[JsonProperty\("([^"]+)"\)\]\s*public\s+([\w.<>\[\]]+)\s+(\w+)\s*(?:;|=)')
for match in field_pattern.finditer(source):
    owner = next(c for c in reversed(classes) if c['start'] < match.start() < c['end'])
    owner['fields'][match[1]] = {'type': match[2], 'field': match[3]}
by_name = {c['name']: c for c in classes}

def resolve(name, parent='AccountModule'):
    scope = parent
    while scope:
        candidate = scope + '.' + name
        if candidate in by_name:
            return by_name[candidate]
        scope = scope.rpartition('.')[0]
    candidates = [c for c in classes if c['short'] == name]
    if len(candidates) != 1:
        raise ValueError(f'Ambiguous local class {parent}:{name}')
    return candidates[0]

checks = []
issues = []
notes = []

def check_response(value, expected_type, path, parent='AccountModule'):
    if value is None:
        notes.append({'path': path, 'note': 'Server returned null; nested/element DTO population was not exercised.'})
        return
    if expected_type.startswith('List<'):
        if not isinstance(value, list):
            issues.append({'path': path, 'expected': expected_type, 'actual': type(value).__name__})
            return
        for index, item in enumerate(value):
            check_response(item, expected_type[5:-1], f'{path}[{index}]', parent)
        return
    primitive = {'string': str, 'bool': bool, 'int': int, 'long': int, 'float': (int, float), 'double': (int, float)}
    if expected_type in primitive:
        expected_python = primitive[expected_type]
        if not isinstance(value, expected_python) or (isinstance(value, bool) and expected_type != 'bool'):
            issues.append({'path': path, 'expected': expected_type, 'actual': type(value).__name__})
        return
    dto = resolve(expected_type, parent)
    if not isinstance(value, dict):
        issues.append({'path': path, 'expected': dto['name'], 'actual': type(value).__name__})
        return
    for key, child in value.items():
        field = dto['fields'].get(key)
        if field is None:
            issues.append({'path': path + '.' + key, 'problem': 'No matching local JsonProperty.'})
        else:
            check_response(child, field['type'], path + '.' + key, dto['name'])

contracts = {
    '/Api/Match/AppConfig': ('OceanShineAppOtherConfigRequest', None, 'base_list'),
    '/Api/Match/Login': ('OceanShineUserLoginRequest', 'OceanShineLoginResponse', 'login'),
    '/Api/Match/Info': ('OceanShineUserInfoRequest', 'OceanShineUserInfoResponse', 'user_info'),
    '/Api/Match/NoticeList': ('OceanShineFeedbackListV2Request', 'OceanShineFeedbackListV2Response', 'msg_list'),
    '/Api/Match/Order': ('OceanShineUserInfoRequest', 'List<OceanShineWithdrawalRecord>', 'order_list'),
    '/Api/Match/Page': ('OceanShineWithdrawalPageRequest', 'OceanShineWithdrawalPageResponse', 'plat_from'),
    '/Api/Match/Task': ('Apid_Usid_Vn_Request', 'List<RoutineTaskLookAdMoneyResponse>', 'task_list'),
}
config = config_path.read_text(encoding='utf-8-sig').split('/*', 1)[0]
constants = dict(re.findall(r'public const string (\w+) = "([^"]*)";', config))
for test in executed['tests']:
    if test['status'] != 'passed':
        continue
    path = test['path']
    request_type, response_type, endpoint = contracts[path]
    dto = resolve(request_type)
    sent_keys = set(test['request'])
    dto_keys = set(dto['fields'])
    entry = {'path': path, 'requestDto': dto['name'], 'requestKeySetMatches': sent_keys == dto_keys,
             'missingFromExecutedRequest': sorted(dto_keys - sent_keys), 'extraExecutedRequestKeys': sorted(sent_keys - dto_keys),
             'configuredPathMatchesExecutedPath': constants.get(endpoint) == path,
             'preservedResponseType': response_type, 'responseCheck': 'static field/type comparison to decrypted real response'}
    if not entry['requestKeySetMatches'] or not entry['configuredPathMatchesExecutedPath']:
        issues.append(entry)
    for key, value in test['request'].items():
        if key in dto['fields']:
            check_response(value, dto['fields'][key]['type'], path + '.request.' + key, dto['name'])
    if response_type:
        check_response(test['response']['decryptedData'], response_type, path + '.response')
    else:
        other = test['response']['decryptedData']
        entry['otherConstantKeysPresent'] = {name: constants.get(name) in other for name in (
            'one_Count', 'one_Ratio', 'two_Count', 'two_Ratio', 'three_Count', 'three_Ratio')}
        if not all(entry['otherConstantKeysPresent'].values()):
            issues.append({'path': path, 'otherConstants': entry['otherConstantKeysPresent']})
    checks.append(entry)

notes.append({'path': '/Api/Match/Task', 'note': 'Actual response root is array. This agrees with preserved local List<RoutineTaskLookAdMoneyResponse>; Swagger describes object.'})
out = {
    'status': 'passed' if not issues else 'issues_found',
    'method': 'Static check only. No Unity code invoked; redacted real network response values compared with final JsonProperty keys and C# field types.',
    'sourceSha256': hashlib.sha256(source_path.read_bytes()).hexdigest(),
    'checks': checks, 'issues': issues, 'notes': notes,
}
(here / 'communication-contract-audit.json').write_text(json.dumps(out, indent=2, ensure_ascii=False) + '\n', encoding='utf-8')
print(json.dumps({'status': out['status'], 'checks': len(checks), 'issues': len(issues)}, ensure_ascii=False))
