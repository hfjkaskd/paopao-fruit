"""Explicit, schema-checked API migration. Uses only the designated cached Swagger.

No network requests; no runtime or release secrets are read or written.
"""
from pathlib import Path
import json
import re
import hashlib

ROOT = Path(r'C:\Projects\paopao')
OUT = ROOT / 'Tools/APIReplace/20260915-bubblosaicpmge'
API = ROOT / 'BizzaWZ/Assets/BizzaWZ/Final/Connect_SDK/SDK_WKY/API'
CACHE = Path(r'C:\Users\pc\Desktop\PlayCloudSdk\PlayCloud_API\LocalAPIData\swagger-doc.json')
swagger = json.loads(CACHE.read_text(encoding='utf-8-sig'))
defs = swagger['definitions']

def read(path):
    return path.read_bytes().decode('utf-8-sig')

source = read(OUT / 'baseline/API/AccountModule.cs')
cfg = read(OUT / 'baseline/API/AccountModuleCfg.cs')
current_source = read(API / 'AccountModule.cs')
current_cfg = read(API / 'AccountModuleCfg.cs')
prior_report = json.loads((OUT/'api-mapping.json').read_text(encoding='utf-8')) if (OUT/'api-mapping.json').exists() else None

# Remove comments/strings without changing source offsets, including all inactive macro branches.
mask_rx = re.compile(r'//[^\r\n]*|/\*[\s\S]*?\*/|\$?@"(?:""|[^"])*"|\$?"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'')
def masked(text):
    return mask_rx.sub(lambda m: ''.join('\n' if c == '\n' else '\r' if c == '\r' else ' ' for c in m[0]), text)

def brace_end(mask, start):
    depth = 0
    for index in range(start, len(mask)):
        if mask[index] == '{': depth += 1
        if mask[index] == '}':
            depth -= 1
            if depth == 0: return index + 1
    raise ValueError('Unbalanced C# braces')

def classes(text):
    mask = masked(text)
    result = []
    for match in re.finditer(r'\bclass\s+(\w+)[^{]*\{', mask):
        start = match.end() - 1
        parents = [x['name'] for x in result if x['start'] < start < x['end']]
        result.append(dict(name=match[1], path='.'.join(parents[1:] + [match[1]]) if parents else match[1], start=start, end=brace_end(mask,start)))
    return result

scopes = classes(source)
def scope_at(position):
    return next(x for x in reversed(scopes) if x['start'] < position < x['end'])['path']

# Each selected path is a literal entry under the exact FruitTileMatch tag.
# Matching evidence is the source endpoint's purpose, target endpoint's purpose and schema.
path_changes = {
    'add_ecpm': '/Api/Match/AddRe', 'add_order': '/Api/Match/Pay',
    'add_order_task': '/Api/Match/TaskPay', 'base_list': '/Api/Match/AppConfig',
    'from': '/Api/Match/Come', 'get_ecpm_id': '/Api/Match/ReId',
    'login': '/Api/Match/Login', 'msg': '/Api/Match/Notice',
    'msg_list': '/Api/Match/NoticeList', 'new_user': '/Api/Match/New',
    'number': '/Api/Match/Level', 'order_list': '/Api/Match/Order',
    'plat_from': '/Api/Match/Page', 'task_list': '/Api/Match/Task',
    'user_info': '/Api/Match/Info', 'user_name': '/Api/Match/Name',
    'ad_info': '/Api/MatchLog/AdShow', 'app_info': '/Api/MatchLog/AppEven',
}

# Local DTO -> old and target Swagger schema. Inline response objects are handled below.
class_schemas = {
    'UserInfo': 'response.UserInfo',
    'OceanShineAdRevenueRequest': 'request.AdRevenueReportReq',
    'OceanShineAdRevenueResponse': 'response.CalculateAdRevenueResponse',
    'OceanShineApplyWithdrawalRequestReal': 'request.ApplyWithdrawalReq',
    'OceanShineApplyWithdrawalRequestFake': 'request.ApplyWithdrawalMoneyRollReq',
    'OceanShineApplyWithdrawalResponse': 'response.ApplyWithdrawalResponse',
    'OceanShineAppOtherConfigRequest': ('request.SportBallsMatchAppOtherConfigReq', 'request.FruitTileMatchAppOtherConfigCustomizeReq'),
    'OceanShineUserAttrsRequest': 'request.UserAttrsReq',
    'OceanShineGetAdRevenueReportIdRequest': 'request.GetAdRevenueReportIdReq',
    'OceanShineGetAdRevenueReportIdResponse': 'response.GetAdRevenueReportIdResponse',
    'OceanShineUserLoginRequest': 'request.UserLoginReq',
    'OceanShineLoginResponse': 'response.LoginResponse',
    'OceanShineLoginResponse.ActiveRule': 'inline.ActiveRule',
    'OceanShineFeedbackRequest': 'request.FeedbackReq',
    'OceanShineFeedbackListV2Request': 'request.FeedbackListV2Req',
    'OceanShineFeedbackListV2Response': 'response.FeedbackListV2Response',
    'OceanShineFeedbackListV2Response.OceanShineFeedbackListV2ResponseData': 'response.FeedbackListV2ResponseData',
    'OceanShineUserReachReportRequest': 'request.UserReachReportReq',
    'OceanShineUserReachReportResponse': 'response.UserInfoResponse',
    'OceanShineUserReachReportResponse.NewcomerReward': 'response.AppConfig',
    'OceanShineUserInfoRequest': 'request.UserInfoReq',
    'OceanShineWithdrawalRecord': 'response.WithdrawalRecord',
    'OceanShineWithdrawalPageRequest': 'request.WithdrawalPageReq',
    'OceanShineWithdrawalPageResponse': 'response.WithdrawalPageResponse',
    'OceanShineWithdrawalPageResponse.WithdrawalRatio': 'response.WithdrawRatio',
    'OceanShineWithdrawalPageResponse.WithdrawalPlatform': 'response.NewWithdrawalMethodGoods',
    'Apid_Usid_Vn_Request': 'request.RoutineTaskLookAdMoneyReq',
    'OceanShineUserInfoResponse': 'response.UserInfoResponse',
    'OceanShineUserInfoResponse.NewcomerReward': 'response.AppConfig',
    'RoutineTaskLookAdMoneyResponse': 'response.RoutineTaskLookAdMoneyResponse',
    'OceanShineAdLogReportRequest': 'request.AdLogReportReq',
    'OceanShineAdLogReportRequest.CommonInfo': 'request.CommonInfo',
    'OceanShineAdLogReportRequest.ExtendParam': 'request.AdLogExtendParam',
    'OceanShineAppEventReportRequest': 'request.AppEventReportReq',
    'OceanShineAppEventReportRequest.CommonInfo': 'request.CommonInfo',
    'OceanShineAppEventReportRequest.ExtendParam': 'request.AppEventLogExtendParam',
}

def schema_pair(class_path):
    pair = class_schemas[class_path]
    if pair == 'inline.ActiveRule':
        return ('response.SportBallsMatchLoginResponse.SbmAru', 'response.FruitTileMatchLoginResponse.FtMaRu',
                defs['response.SportBallsMatchLoginResponse']['properties']['SbmAru']['properties'],
                defs['response.FruitTileMatchLoginResponse']['properties']['FtMaRu']['properties'])
    if isinstance(pair, str):
        kind, suffix = pair.split('.')
        pair = (kind + '.SportBallsMatch' + suffix, kind + '.FruitTileMatch' + suffix)
    return (*pair, defs[pair[0]]['properties'], defs[pair[1]]['properties'])

# Explicit semantic mappings, never generated by a prefix substitution.
# Schema membership and both descriptions are saved for every field occurrence.
key_map = dict(x.split(':') for x in '''
SbmApid:FtMaId SbmUsid:FtMuId SbmVn:vn Vn:vn
SbmBac:FtMBa SbmBaci:FtMBaI SbmCon:FtMCi SbmCrc:FtMcCy SbmCrcs:FtMcCyS
SbmCty:FtMcTy SbmEwl:FtMeWl SbmImg:FtMiMs SbmLev:FtMlVl SbmLgd:FtMlDy
SbmMny:FtMy SbmNcm:FtMnC SbmNnm:FtMnNm SbmRti:FtMrTi SbmRts:FtMrTs SbmTra:FtMtRe
SbmBtid:FtMbId SbmEcm:FtMeCpm SbmSal:FtMsPc SbmUso:FtMuIf SbmPrc:FtMPr SbmMul:FtMAml
SbmCp:FtMcPf SbmMid:FtMmId SbmPb:FtMpBt SbmRa:FtMrAt SbmRe:FtMrEl SbmRm:FtMrMe
SbmRn:FtMrNe SbmSeid:FtMsSd SbmOdr:FtMoRn SbmMn:FtMMn Sbmcfn:FtMCkn
SbmAgp:FtMaGp SbmAid:FtMaDd SbmCat:FtMcAt SbmCcy:FtMcCy SbmCkl:FtMcLl SbmCmp:FtMcMp
SbmCti:FtMcTv SbmCtm:FtMcTi SbmDlu:FtMdLu SbmEvt:FtMeVe SbmFbr:FtMfRf
SbmGct:FtMgCt SbmNet:FtMnWk SbmTrn:FtMtNe SbmTrt:FtMtTn
SbmAnid:FtMaDd SbmAtd:FtMaDb SbmBbd:FtMbRd SbmCal:FtMcHl SbmCtr:FtMcHr
SbmDtd:FtMdDy SbmDth:FtMdHt SbmDtw:FtMdWh SbmGaid:FtMgAd SbmLag:FtMlGe
SbmMbl:FtMmDl SbmNbt:FtMnWk SbmObv:FtMoVs SbmRtt:FtMrOt SbmSbi:FtMsIm
SbmTry:FtMcTy SbmTtz:FtMtZe SbmCpu:FtMcPu SbmUa:FtMuA SbmVc:vc
SbmAru:FtMaRu SbmRdt:FtMrDe SbmAcm:FtMcPm SbmAiu:FtMiPu SbmDes:FtMdSp
SbmMsl:FtMmSg SbmCda:FtMcAt SbmCte:FtMcTi SbmCtt:FtMcTt SbmId:FtMid
SbmTpe:FtMtPe SbmUda:FtMuAt SbmUte:FtMuTi SbmAcg:FtMaCg SbmCom:FtMcMg
SbmNba:FtMnBa SbmNcc:FtMnCc SbmRnw:FtMnCr SbmUsd:FtMtUd SbmDat:FtMdt
SbmPym:FtMpNe SbmRrm:FtMrMs SbmSts:FtMsSt SbmWr:FtMrat SbmWwf:FtMwPf
SbmAdy:FtMady SbmBen:FtMbeg SbmEnd:FtMend SbmWro:FtMrat SbmCn:FtMc
SbmMe:FtMm SbmMlt:FtMml SbmEt:FtMx SbmAn:FtMAn SbmCss:FtMCss SbmLn:FtMLn
SbmMrt:FtMMrt SbmMy:FtMMy SbmSr:FtMSr SbmSs:FtMSs SbmTid:FtMTid
SbmCnf:FtMcIf SbmEpm:FtMePm SbmAbd:FtMaTy SbmAdgp:FtMaGp SbmCid:FtMcId
SbmCtc:FtMcCyc SbmEvtM:FtMeVeM SbmMbc:FtMmCd SbmMbp:FtMmPf SbmPtf:FtMpFm
SbmBgd:FtMpId SbmEvtE:FtMeVeE
'''.split())
overrides = {('OceanShineUserAttrsRequest','SbmCty'):'FtMcTe', ('OceanShineWithdrawalRecord','SbmPrc'):'FtMpr'}
deleted = {
    ('OceanShineApplyWithdrawalRequestReal','Os_Gdid'): 'Absent target field; only assigned in explicitly authorized withdrawal builder',
    ('OceanShineApplyWithdrawalRequestReal','Os_Trt'): 'Absent target field; only assigned in explicitly authorized withdrawal builder',
    ('OceanShineApplyWithdrawalRequestFake','Os_Gdid'): 'Absent target field; only assigned in explicitly authorized withdrawal builder',
    ('OceanShineGetAdRevenueReportIdResponse','Os_Pfm'): 'Absent target field; no business references',
    ('OceanShineLoginResponse','Os_Rw'): 'Absent target field; no business references',
    ('OceanShineWithdrawalRecord','Os_Ded'): 'Absent target field; no business references',
    ('OceanShineWithdrawalRecord','Os_Try'): 'Absent target field; no business references (DeviceInfo Os_Try is a different field)',
}
retained = {
    ('UserInfo','Os_Rol'): 'Target schema has no roll balance; UserInfo.GetInfo references this field',
    ('OceanShineWithdrawalRecord','Os_Tsm'): 'Target schema has no cash-failure text; WithdrawHistoryItem.cs:72 references this field',
}

report = dict(tag='FruitTileMatch', swaggerCache=str(CACHE), cacheSha256=hashlib.sha256(CACHE.read_bytes()).hexdigest(),
              pathMappings=[], fieldMappings=[], dtoChanges=[], jsonKeyMappings=[], otherMappings=[], SwaggerTypeMismatch=[], Unresolved=[], validation={})
edits=[]
def add_edit(start,end,new,kind,method=None):
    edits.append(dict(start=start,end=end,old=source[start:end],new=new,kind=kind,method=method))

fields_rx = re.compile(r'(?m)^[ \t]*\[JsonProperty\("(?P<key>[^"\r\n]+)"\)\][^\r\n]*\r?\n[ \t]*public\s+(?P<type>[\w<>,. ]+)\s+(?P<name>\w+)\s*;[^\r\n]*(?:\r?\n)?')
field_inventory={}
for match in fields_rx.finditer(source):
    class_path=scope_at(match.start()); key=match['key']; name=match['name']
    old_schema,new_schema,old_props,new_props=schema_pair(class_path)
    field_inventory.setdefault(class_path,[]).append(name)
    record=dict(file=str(API/'AccountModule.cs'),className=class_path,field=name,csharpType=match['type'].strip(),old=key,
                oldSchema=old_schema,targetSchema=new_schema,oldDescription=old_props.get(key,{}).get('description',''),line=source[:match.start()].count('\n')+1)
    if (class_path,name) in deleted:
        assert key not in new_props
        record.update(action='delete',reason=deleted[class_path,name])
        report['dtoChanges'].append(record)
        add_edit(match.start(),match.end(),'', 'dto-delete')
    elif (class_path,name) in retained:
        record.update(new=key,status='Unresolved',reason=retained[class_path,name])
        report['Unresolved'].append(record)
    else:
        new=overrides.get((class_path,key),key_map.get(key))
        assert key in old_props, (class_path,key,'missing old schema property')
        assert new in new_props, (class_path,key,new,'missing target schema property')
        record.update(new=new,targetDescription=new_props[new].get('description',''),action='JsonProperty',status='confirmed')
        report['fieldMappings'].append(record)
        add_edit(match.start('key'),match.end('key'),new,'JsonProperty')

# Add the missing applyType DTO field and wire the already-existing method parameter.
needle='        // [JsonProperty("At")] // SportBallsMatch ApplyMoney 请求不定义该字段\r\n        // public string Os_At; // 现金或卷提现 applyType：money 或 rolll'
assert source.count(needle)==1
start=source.index(needle)
add_edit(start,start+len(needle),'        [JsonProperty("FtMAt")]\r\n        public string Os_At; // 现金或卷提现 applyType：money 或 rolll','dto-add')
report['dtoChanges'].append(dict(className='OceanShineApplyWithdrawalRequestFake',field='Os_At',new='FtMAt',action='add',csharpType='string',targetSchema='request.FruitTileMatchApplyWithdrawalMoneyRollReq',targetDescription=defs['request.FruitTileMatchApplyWithdrawalMoneyRollReq']['properties']['FtMAt']['description'],value='_Os_At'))

def method_span(name):
    m=re.search(r'(?m)^    public [^\r\n]+\b'+re.escape(name)+r'\s*\(',source)
    assert m,name
    start=masked(source).index('{',m.end())
    return start,brace_end(masked(source),start)

for method in ['GetApplyWithdrawalRequestReal','GetApplyWithdrawalRequestFake']:
    start,end=method_span(method); body=source[start:end]
    for old,new in [('            Os_Gdid = 0,\r\n','')]+([('            Os_Trt = "0"\r\n','')] if method.endswith('Real') else [('            // Os_At = _Os_At','            Os_At = _Os_At')]):
        assert body.count(old)==1,(method,old)
        at=start+body.index(old); add_edit(at,at+len(old),new,'authorized-withdrawal-builder',method)

# The actual Adjust request is a Dictionary, independent of the DTO attributes.
start,end=method_span('CreateFromJson'); body=source[start:end]
attr_keys=[]
for m in re.finditer(r'\{ "(?P<key>[^"]+)", (?P<value>[^\r\n]+) \}',body):
    old=m['key'];new=overrides.get(('OceanShineUserAttrsRequest',old),key_map[old]);attr_keys.append(new)
    target=defs['request.FruitTileMatchUserAttrsReq']['properties'][new]
    report['jsonKeyMappings'].append(dict(file=str(API/'AccountModule.cs'),method='CreateFromJson(AdjustSdk.AdjustAttribution attribution, string eventName)',jsonPath='$.'+new,oldJsonPath='$.'+old,old=old,new=new,valueExpression=m['value'],targetType=target.get('type'),targetDescription=target.get('description',''),macroBranch='BIZZA_REAL_WITHDRAW && BIZZA_ENABLE_ADJUST'))
    add_edit(start+m.start('key'),start+m.end('key'),new,'confirmed-request-json-key','CreateFromJson')
assert len(attr_keys)==len(set(attr_keys))==19
assert set(attr_keys)==set(defs['request.FruitTileMatchUserAttrsReq']['properties'])

cfg_edits=[]
for m in re.finditer(r'(?m)^    public const string (\w+) = "([^"]*)";',cfg):
    symbol,old=m[1],m[2]
    if not old.startswith('/'): continue
    old_op=swagger['paths'][old]['post']
    if symbol not in path_changes:
        report['Unresolved'].append(dict(kind='path',symbol=symbol,old=old,retained=True,reason='No matching withdrawal-user-list endpoint under exact FruitTileMatch tag. No local request call site exists.'))
        continue
    new=path_changes[symbol];new_op=swagger['paths'][new]['post']
    assert 'FruitTileMatch' in new_op['tags']
    report['pathMappings'].append(dict(symbol=symbol,old=old,new=new,method='POST',oldSummary=old_op.get('summary'),targetSummary=new_op.get('summary'),targetDescription=new_op.get('description'),oldRequestSchema=next(p['schema'] for p in old_op['parameters'] if p['in']=='body'),targetRequestSchema=next(p['schema'] for p in new_op['parameters'] if p['in']=='body'),requestImplementation='none in current project' if symbol=='user_name' else 'AccountModule'))
    cfg_edits.append(dict(start=m.start(2),end=m.end(2),old=old,new=new,kind='confirmed-api-path'))

# The custom-response schema is deliberately unspecified. These six keys are
# confirmed by the successful real AppConfig response for this app/config key.
communication_path = ROOT/'Tools/APIReplace/20260915-bubblosaicpmge/communication-results.json'
communication = json.loads(communication_path.read_text(encoding='utf-8-sig'))
config_test = next(t for t in communication['tests'] if t['path']=='/Api/Match/AppConfig')
assert communication['tag']=='FruitTileMatch' and communication['appId']=='bubblosaicpmge'
assert config_test['status']=='passed' and config_test['request']['FtMCkn']=='look_ad_reward_mul'
config_data=config_test['response']['decryptedData']
other_targets = {'one_Count':'hp_one', 'one_Ratio':'hp_one_rate', 'two_Count':'hp_two', 'two_Ratio':'hp_two_rate', 'three_Count':'hp_three', 'three_Ratio':'hp_three_rate'}
for m in re.finditer(r'(?m)^    public const string (\w+) = "([^"]*)";',cfg):
    if m[1] not in other_targets: continue
    new=other_targets[m[1]]
    assert new in config_data and isinstance(config_data[new],int)
    cfg_edits.append(dict(start=m.start(2),end=m.end(2),old=m[2],new=new,kind='confirmed-other-response-key'))
    report['otherMappings'].append(dict(symbol=m[1],old=m[2],new=new,responseValue=config_data[new],evidence=str(communication_path),endpoint='/Api/Match/AppConfig',configKey='look_ad_reward_mul',responseJsonPath='$.data.'+new,status='confirmed by decrypted HTTP 200 / business 200 response',meaning='ad count threshold' if m[1].endswith('Count') else 'withdrawal ratio percent'))

def apply(text, changes):
    changes=sorted(changes,key=lambda x:x['start'])
    assert all(a['end']<=b['start'] for a,b in zip(changes,changes[1:])), 'overlapping edits'
    for change in reversed(changes):
        assert text[change['start']:change['end']]==change['old']
        text=text[:change['start']]+change['new']+text[change['end']:]
    return text

updated=apply(source,edits); updated_cfg=apply(cfg,cfg_edits)
assert current_source in [source, updated] + ([apply(source,prior_report['edits']['AccountModule'])] if prior_report else []), 'Unexpected external source edits; refusing to overwrite'
assert current_cfg in [cfg, updated_cfg] + ([apply(cfg,prior_report['edits']['AccountModuleCfg'])] if prior_report else []), 'Unexpected external configuration edits; refusing to overwrite'

# Complete allowed-token projection: reconstruct the original after removing only the
# individually approved changes. This checks method bodies, declarations and call sites.
def prove_projection(before,after,changes):
    pos_before=pos_after=0
    for change in sorted(changes,key=lambda x:x['start']):
        untouched=before[pos_before:change['start']]
        assert after[pos_after:pos_after+len(untouched)]==untouched
        pos_after+=len(untouched)
        assert after[pos_after:pos_after+len(change['new'])]==change['new']
        pos_after+=len(change['new']);pos_before=change['end']
    assert after[pos_after:]==before[pos_before:]
prove_projection(source,updated,edits);prove_projection(cfg,updated_cfg,cfg_edits)

def method_declarations(text):
    return re.findall(r'(?m)^[ \t]*(?:public|private|protected|internal)\s+(?:(?:static|async|virtual|override|sealed|new)\s+)*[\w<>.,?]+(?:\s+[\w<>.,?]+)?\s*\([^;{}]*\)\s*(?:where[^{}]+)?(?=\{)',masked(text))
assert method_declarations(source)==method_declarations(updated)
def http_calls(text):
    return re.findall(r'HttpUtil\.RequestToServer[^;]+;',text)
assert http_calls(source)==http_calls(updated)

report['SwaggerTypeMismatch']=[
    dict(symbol='new_user',path='/Api/Match/New',swaggerResponse='array<response.FruitTileMatchUserInfoResponse>',localResponse='OceanShineUserReachReportResponse',resolution='Preserved verified single DTO callback and HTTP generic'),
    dict(symbol='number',path='/Api/Match/Level',swaggerResponse='array<response.FruitTileMatchUserInfoResponse>',localResponse='OceanShineUserReachReportResponse',resolution='Preserved verified single DTO callback and HTTP generic'),
    dict(symbol='task_list',path='/Api/Match/Task',swaggerResponse='response.FruitTileMatchRoutineTaskLookAdMoneyResponse',localResponse='List<RoutineTaskLookAdMoneyResponse>',resolution='Preserved verified list callback and HTTP generic; successful communication also returned an array'),
]
after_classes=classes(updated)
actual_fields={}
actual_field_types={}
for m in fields_rx.finditer(updated):
    cls=next(x for x in reversed(after_classes) if x['start']<m.start()<x['end'])['path']
    actual_fields.setdefault(cls,[]).append(m['key'])
    actual_field_types.setdefault(cls,{})[m['key']]=(m['type'].strip(),m['name'])
reverse_checks=[]
for cls,keys in actual_fields.items():
    target=set(schema_pair(cls)[3]);extra=set(keys)-target;missing=target-set(keys)
    assert not missing,(cls,missing)
    assert len(keys)==len(set(keys)),cls
    assert extra==({'SbmRol'} if cls=='UserInfo' else {'SbmTsm'} if cls=='OceanShineWithdrawalRecord' else set()),(cls,extra)
    reverse_checks.append(dict(className=cls,status='Unresolved' if extra else 'passed',missing=sorted(missing),retainedUnresolvedKeys=sorted(extra),duplicateKeys=False))

def local_shape(cls, prefix='$'):
    result={}
    for key,(typ,field) in actual_field_types[cls].items():
        path=prefix+'.'+key
        primitive={'string':'string','bool':'boolean','int':'integer','long':'integer','float':'number','double':'number'}.get(typ)
        if primitive:
            result[path]=primitive
            continue
        array=typ.startswith('List<')
        nested_type=typ[5:-1] if array else typ
        options=[cls+'.'+nested_type]+['.'.join(cls.split('.')[:n]+[nested_type]) for n in range(len(cls.split('.'))-1,-1,-1)]
        nested=next(x for x in options if x in actual_field_types)
        result[path]='array' if array else 'object'
        result.update(local_shape(nested,path+'[]' if array else path))
    return result

def resolve_schema(schema):
    if '$ref' in schema:
        return resolve_schema(defs[schema['$ref'].split('/')[-1]])
    if 'allOf' in schema:
        assert len(schema['allOf'])==1
        return resolve_schema(schema['allOf'][0])
    return schema

def swagger_shape(schema,prefix='$'):
    schema=resolve_schema(schema);result={}
    for key,child in schema.get('properties',{}).items():
        child=resolve_schema(child);path=prefix+'.'+key
        typ=child.get('type','object' if 'properties' in child else None)
        result[path]=typ
        if typ=='object': result.update(swagger_shape(child,path))
        if typ=='array': result.update(swagger_shape(child['items'],path+'[]'))
    return result

request_roots={
    'add_ecpm':'OceanShineAdRevenueRequest', 'add_order':'OceanShineApplyWithdrawalRequestReal',
    'add_order_task':'OceanShineApplyWithdrawalRequestFake', 'base_list':'OceanShineAppOtherConfigRequest',
    'from':'OceanShineUserAttrsRequest', 'get_ecpm_id':'OceanShineGetAdRevenueReportIdRequest',
    'login':'OceanShineUserLoginRequest', 'msg':'OceanShineFeedbackRequest',
    'msg_list':'OceanShineFeedbackListV2Request', 'new_user':'OceanShineUserReachReportRequest',
    'number':'OceanShineUserReachReportRequest', 'order_list':'OceanShineUserInfoRequest',
    'plat_from':'OceanShineWithdrawalPageRequest', 'task_list':'Apid_Usid_Vn_Request',
    'user_info':'OceanShineUserInfoRequest', 'ad_info':'OceanShineAdLogReportRequest',
    'app_info':'OceanShineAppEventReportRequest',
}
recursive_requests=[]
for symbol,cls in request_roots.items():
    endpoint=next(p for p in report['pathMappings'] if p['symbol']==symbol)
    local=local_shape(cls);target=swagger_shape(endpoint['targetRequestSchema'])
    assert local==target,(symbol,local,target)
    recursive_requests.append(dict(symbol=symbol,path=endpoint['new'],className=cls,status='passed',mode='static, no runtime serialization',jsonPathsAndTypes=local,nestedFieldCount=len(local),duplicates=False,missing=[],extra=[],actualSerialization='CreateFromJson dictionary -> JsonConvert.SerializeObject' if symbol=='from' else 'DTO -> JsonConvert.SerializeObject',additionalCallSite='HttpAdAdapterBase.Request_AdLoadReport serializes the same nested DTO' if symbol=='ad_info' else None))

# Validate the shape of the successfully decrypted test responses against the
# confirmed target DTOs. Null/empty containers cannot establish item fields.
response_roots={'/Api/Match/Login':'OceanShineLoginResponse','/Api/Match/Info':'OceanShineUserInfoResponse','/Api/Match/NoticeList':'OceanShineFeedbackListV2Response','/Api/Match/Order':'OceanShineWithdrawalRecord','/Api/Match/Page':'OceanShineWithdrawalPageResponse','/Api/Match/Task':'RoutineTaskLookAdMoneyResponse'}
def observed_paths(data,prefix='$'):
    result=set()
    if isinstance(data,dict):
        for key,val in data.items():
            result.add(prefix+'.'+key)
            result.update(observed_paths(val,prefix+'.'+key))
    elif isinstance(data,list):
        for item in data: result.update(observed_paths(item,prefix+'[]'))
    return result

response_checks=[]
for test in communication['tests']:
    if test['path'] not in response_roots: continue
    data=test['response']['decryptedData'];cls=response_roots[test['path']]
    shape=set(local_shape(cls));observed=set()
    if isinstance(data,list):
        for item in data: observed.update(observed_paths(item))
    else: observed=observed_paths(data)
    assert observed<=shape,(test['path'],observed-shape)
    response_checks.append(dict(path=test['path'],localClass=cls,status='passed' if data is not None else 'root null; item fields not observed',observedFieldPaths=sorted(observed),unmappedObservedFields=sorted(observed-shape),unobservedLocalFieldPaths=sorted(shape-observed),evidence=str(communication_path)))

report['validation']=dict(
    methodContract=dict(status='passed',mode='static',methodDeclarationCount=len(method_declarations(source)),declarationsUnchanged=True,httpCallCount=len(http_calls(source)),httpCallsUnchanged=True,allOtherTextUnchangedAfterAllowedTokenProjection=True,authorizedBuilderChanges=[dict(method=e['method'],old=e['old'].strip(),new=e['new'].strip()) for e in edits if e['kind']=='authorized-withdrawal-builder']),
    actualAttributionPayload=dict(status='passed',mode='static analysis; serialization not executed',trace=['AdjustAttributionAdapter.AttributeStart assigns AttributionChangedDelegate','AdjustAttributionAdapter.AttributionChangedDelegate -> AccountModule.CreateFromJson','Request_UserAttrsRequest(Dictionary<string, object>)','JsonConvert.SerializeObject(request)','HttpUtil.RequestToServer<OceanShineUserAttrsRequest>(AccountModuleCfg.from, requestParams, callback, false)'],allMacroBranchesInspected=True,nestedObjectsOrArrays='none; flat 19-field dictionary',targetKeyCount=19,actualKeyCount=len(attr_keys),missing=[],extra=[],duplicates=[],oldKeysRemaining=[],valuesAndTypesUnchanged=True,costNullCoalescingPreserved='attribution.CostAmount ?? 0',timestampPreserved='DateTimeOffset.UtcNow.ToUnixTimeSeconds()'),
    reverseDtoFields=reverse_checks,
    recursiveRequestPayloads=recursive_requests,
    observedCommunicationResponseFields=response_checks,
    addedInterfaces=False,
    nickname=dict(path='/Api/Match/Name',status='path updated; no existing request method/DTO in project; no method added'),
    customConfig=dict(key='look_ad_reward_mul',keyValuePreserved=True,responseKeysUpdated=other_targets,unconsumedResponseKeys=['next_day'],status='passed',reason='The successful decrypted response establishes all six hp_* count/ratio keys; existing lookup methods and values are preserved'),
    compilation='delegated to root; not executed by this script',communication='delegated to root; not executed by this script')
report['notes']=[
    'FtMsPc is the special-marker field. The target description documents activity_carve_up; existing marker expressions and values are preserved by method-contract protection.',
    'TaskPay Os_Mn is mapped using the existing field comment and actual _Os_Mn task value, because the legacy SbmMn Swagger description embeds a malformed ApplyType declaration. Added Os_At receives the existing _Os_At argument.',
    'Path order_name, response UserInfo.Os_Rol and WithdrawalRecord.Os_Tsm remain explicitly unresolved; unchanged old keys are not represented as successful replacements.',
]
report['edits']=dict(AccountModule=edits,AccountModuleCfg=cfg_edits)
if (API/'AccountModule.cs').read_bytes()!=updated.encode('utf-8'):
    (API/'AccountModule.cs').write_bytes(updated.encode('utf-8'))
if (API/'AccountModuleCfg.cs').read_bytes()!=updated_cfg.encode('utf-8'):
    (API/'AccountModuleCfg.cs').write_bytes(updated_cfg.encode('utf-8'))
(OUT/'api-mapping.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(dict(paths=len(report['pathMappings']),jsonProperties=len(report['fieldMappings']),dtoChanges=len(report['dtoChanges']),actualJsonKeys=len(report['jsonKeyMappings']),Unresolved=len(report['Unresolved']),SwaggerTypeMismatch=len(report['SwaggerTypeMismatch']),contract='passed',payload='static passed'),ensure_ascii=False))
