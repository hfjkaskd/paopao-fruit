"""Compare this run's real HTTP payloads with current project declarations.

This is a static check, not execution of Unity request builders/deserialization.
It reads only this run's report, the designated live Unity project, and Swagger.
"""
import hashlib
import json
import re
from datetime import datetime, timezone
from pathlib import Path

OUTPUT = Path(__file__).resolve().parent
PROJECT = Path('C:/Projects/paopao/BizzaWZ')
API = PROJECT / 'Assets/BizzaWZ/Final/Connect_SDK/SDK_WKY/API'
SWAGGER = Path('C:/Users/pc/Desktop/PlayCloudSdk/PlayCloud_API/LocalAPIData/swagger-doc.json')
module_file = API / 'AccountModule.cs'
module_text = module_file.read_text(encoding='utf-8-sig')
spec = json.loads(SWAGGER.read_text(encoding='utf-8-sig'))
network = json.loads((OUTPUT / 'communication-results.json').read_text(encoding='utf-8-sig'))

def blank_literals(text):
    pattern = r'//[^\n]*|/\*[\s\S]*?\*/|@"(?:[^"]|"")*"|"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\''
    return re.sub(pattern, lambda m: re.sub(r'[^\n]', ' ', m[0]), text)

structural = blank_literals(module_text)
ends = {}
opening = []
for at, char in enumerate(structural):
    if char == '{':
        opening.append(at)
    elif char == '}':
        ends[opening.pop()] = at
declarations = []
for match in re.finditer(r'\bclass\s+(\w+)', structural):
    begin = structural.index('{', match.end())
    enclosing = [d for d in declarations if d['begin'] < begin < d['end']]
    full_name = (enclosing[-1]['name'] + '.' if enclosing else '') + match[1]
    declarations.append(dict(name=full_name, short=match[1], begin=begin, end=ends[begin], fields={}))
for field in re.finditer(r'\[JsonProperty\("([^"]+)"\)\]\s*public\s+([\w.<>\[\]]+)\s+(\w+)\s*[;=]', module_text):
    enclosing = [d for d in declarations if d['begin'] < field.start() < d['end']]
    enclosing[-1]['fields'][field[1]] = dict(type=field[2], member=field[3])

def find_class(type_name, owner='AccountModule'):
    possible_scopes = [owner]
    while '.' in possible_scopes[-1]:
        possible_scopes.append(possible_scopes[-1].rsplit('.', 1)[0])
    for scope in possible_scopes:
        found = [d for d in declarations if d['name'] == scope + '.' + type_name]
        if found:
            return found[0]
    found = [d for d in declarations if d['short'] == type_name]
    if len(found) != 1:
        raise ValueError('Local class cannot be uniquely resolved: ' + type_name)
    return found[0]

issues = []
coverage_notes = []
def check_local(value, type_name, location, owner='AccountModule'):
    if value is None:
        coverage_notes.append(dict(location=location, reason='Null data: nested or element fields were not exercised.'))
        return
    if type_name.startswith('List<') or type_name.endswith('[]'):
        child_type = type_name[5:-1] if type_name.startswith('List<') else type_name[:-2]
        if not isinstance(value, list):
            issues.append(dict(location=location, expected=type_name, actual=type(value).__name__))
            return
        if not value:
            coverage_notes.append(dict(location=location, reason='Empty array: element fields were not exercised.'))
        for number, item in enumerate(value):
            check_local(item, child_type, f'{location}[{number}]', owner)
        return
    scalar = dict(string=str, int=int, long=int, float=(int, float), double=(int, float), decimal=(int, float), bool=bool)
    if type_name in scalar:
        if not isinstance(value, scalar[type_name]) or (isinstance(value, bool) and type_name != 'bool'):
            issues.append(dict(location=location, expected=type_name, actual=type(value).__name__))
        return
    current = find_class(type_name, owner)
    if not isinstance(value, dict):
        issues.append(dict(location=location, expected=current['name'], actual=type(value).__name__))
        return
    for name, member_value in value.items():
        if name not in current['fields']:
            issues.append(dict(location=location + '.' + name, reason='Actual response key has no matching final local JsonProperty.'))
        else:
            check_local(member_value, current['fields'][name]['type'], location + '.' + name, current['name'])

configuration_text = (API / 'AccountModuleCfg.cs').read_text(encoding='utf-8-sig').split('/*', 1)[0]
constants = dict(re.findall(r'public const string (\w+) = "([^"]*)";', configuration_text))
contracts = {
    '/Out/Jam/BaseData': ('base_list', 'OceanShineAppOtherConfigRequest', None),
    '/Out/Jam/Login': ('login', 'OceanShineUserLoginRequest', 'OceanShineLoginResponse'),
    '/Out/Jam/UserInfo': ('user_info', 'OceanShineUserInfoRequest', 'OceanShineUserInfoResponse'),
    '/Out/Jam/TalkList': ('msg_list', 'OceanShineFeedbackListV2Request', 'OceanShineFeedbackListV2Response'),
    '/Out/Jam/Order': ('order_list', 'OceanShineUserInfoRequest', 'List<OceanShineWithdrawalRecord>'),
    '/Out/Jam/OrderNick': ('order_name', None, None),
    '/Out/Jam/PayList': ('plat_from', 'OceanShineWithdrawalPageRequest', 'OceanShineWithdrawalPageResponse'),
    '/Out/Jam/Task': ('task_list', 'Apid_Usid_Vn_Request', 'List<RoutineTaskLookAdMoneyResponse>'),
}
checks = []
for test in network['tests']:
    if test['status'] != 'passed':
        continue
    path = test['path']
    endpoint, request_type, response_type = contracts[path]
    operation = spec['paths'][path]['post']
    request_definition = spec['definitions'][test['requestSchema']]
    record = dict(path=path, configuredPathMatches=constants.get(endpoint) == path,
                  exactTagMatches='SnakeOutJame' in operation['tags'],
                  executedRequestKeysMatchSwagger=set(test['request']) == set(request_definition['properties']),
                  requestType=request_type, responseType=response_type)
    if not all(record[k] for k in ('configuredPathMatches','exactTagMatches','executedRequestKeysMatchSwagger')):
        issues.append(dict(path=path, reason='Path, Tag, or request key set mismatch.', check=record))
    if request_type:
        local = find_class(request_type)
        record['executedRequestKeysMatchFinalDto'] = set(test['request']) == set(local['fields'])
        if not record['executedRequestKeysMatchFinalDto']:
            issues.append(dict(path=path, reason='Final local DTO request key set mismatch.', localKeys=sorted(local['fields']), executedKeys=sorted(test['request'])))
        check_local(test['request'], request_type, path + '.request')
    if response_type:
        check_local(test['response']['decryptedData'], response_type, path + '.response')
    if path == '/Out/Jam/OrderNick':
        record['runtimeCoverage'] = 'No AccountModuleCfg.order_name caller or dedicated DTO exists in the project; only endpoint/Swagger and server array response were checked.'
        coverage_notes.append(dict(location=path, reason='No local runtime caller; returned [] so response element types were not exercised.'))
    if path == '/Out/Jam/BaseData':
        returned = test['response']['decryptedData']
        record['otherKeys'] = {field: dict(configured=constants.get(field), present=constants.get(field) in returned) for field in
                               ('one_Count','one_Ratio','two_Count','two_Ratio','three_Count','three_Ratio')}
        record['actualOtherData'] = returned
        if not all(item['present'] for item in record['otherKeys'].values()):
            issues.append(dict(path=path, reason='A final Other constant does not exist in the real returned configuration.'))
        record['unchangedConfigurationValue'] = test['request'].get('SoJcfn') == 'look_ad_reward_mul' and 'Os_Cfn = "look_ad_reward_mul"' in module_text
    checks.append(record)

coverage_notes.append(dict(location='/Out/Jam/Task', reason='Actual root is array, compatible with preserved local List<RoutineTaskLookAdMoneyResponse>; Swagger describes an object.'))
result = dict(status='passed' if not issues else 'issues_found', checkedUtc=datetime.now(timezone.utc).isoformat(),
              method='Static comparison of this run real payloads with final C# JsonProperty/member types and configured endpoints. No Unity request builder or deserializer executed.',
              sourceSha256=hashlib.sha256(module_file.read_bytes()).hexdigest(), checks=checks, issues=issues, coverageNotes=coverage_notes)
(OUTPUT / 'communication-contract-check.json').write_text(json.dumps(result, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
print(json.dumps(dict(status=result['status'], checks=len(checks), issueCount=len(issues)), ensure_ascii=False))
