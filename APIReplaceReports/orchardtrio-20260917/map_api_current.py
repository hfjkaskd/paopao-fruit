"""Current-run-only schema migration; no prior reports/tools are loaded."""
import hashlib
import json
import re
from pathlib import Path

RUN = Path(__file__).resolve().parent
PROJECT = Path(r'C:\Projects\paopao\BizzaWZ')
API = PROJECT / 'Assets/BizzaWZ/Final/Connect_SDK/SDK_WKY/API'
SWAGGER = Path(r'C:\Users\pc\Desktop\PlayCloudSdk\PlayCloud_API\LocalAPIData\swagger-doc.json')
document = json.loads(SWAGGER.read_text(encoding='utf-8-sig'))
definitions = document['definitions']
def read(path):
    return path.read_bytes().decode('utf-8-sig')
before = read(RUN/'baseline/API/AccountModule.cs')
before_cfg = read(RUN/'baseline/API/AccountModuleCfg.cs')

literal = re.compile(r'//[^\r\n]*|/\*[\s\S]*?\*/|\$?@"(?:""|[^"])*"|\$?"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'')
def code_mask(text):
    return literal.sub(lambda m: ''.join(c if c in '\r\n' else ' ' for c in m[0]),text)
def block_end(text,start):
    depth=0
    for i in range(start,len(text)):
        depth += (text[i]=='{') - (text[i]=='}')
        if depth==0:return i+1
    raise ValueError('unbalanced braces')
def class_spans(text):
    masked=code_mask(text);result=[]
    for m in re.finditer(r'\bclass\s+(\w+)[^{]*\{',masked):
        start=m.end()-1
        parent=next((c for c in reversed(result) if c['start']<start<c['end']),None)
        path=(parent['name']+'.' if parent and parent['name']!='AccountModule' else '')+m[1]
        result.append(dict(name=path,start=start,end=block_end(masked,start)))
    return result
def owner(scopes,at):
    return next(c for c in reversed(scopes) if c['start']<at<c['end'])['name']
scopes=class_spans(before)
field_pattern=re.compile(r'(?m)^[ \t]*\[JsonProperty\("(?P<key>[^"]+)"\)\][^\r\n]*\r?\n[ \t]*public\s+(?P<type>[\w<>., ]+)\s+(?P<name>\w+)\s*;[^\r\n]*(?:\r?\n)?')

# Explicit endpoint selection verified against both source purpose and target Tag.
endpoints={
 'add_ecpm':'/Out/Jam/AddIncome','add_order':'/Out/Jam/ApplyOrder',
 'add_order_task':'/Out/Jam/ApplyOrderTask','base_list':'/Out/Jam/BaseData',
 'from':'/Out/Jam/Come','get_ecpm_id':'/Out/Jam/IncomeId','login':'/Out/Jam/Login',
 'msg':'/Out/Jam/Talk','msg_list':'/Out/Jam/TalkList','new_user':'/Out/Jam/NewUser',
 'number':'/Out/Jam/Times','order_list':'/Out/Jam/Order','order_name':'/Out/Jam/OrderNick',
 'plat_from':'/Out/Jam/PayList','task_list':'/Out/Jam/Task','user_info':'/Out/Jam/UserInfo',
 'user_name':'/Out/Jam/NickName','ad_info':'/Out/JamLog/AdInfo','app_info':'/Out/JamLog/AppInfo',
}
class_schema={
 'UserInfo':'response.UserInfo',
 'OceanShineAdRevenueRequest':'request.AdRevenueReportReq',
 'OceanShineAdRevenueResponse':'response.CalculateAdRevenueResponse',
 'OceanShineApplyWithdrawalRequestReal':'request.ApplyWithdrawalReq',
 'OceanShineApplyWithdrawalRequestFake':'request.ApplyWithdrawalMoneyRollReq',
 'OceanShineApplyWithdrawalResponse':'response.ApplyWithdrawalResponse',
 'OceanShineAppOtherConfigRequest':'request.AppOtherConfigReq',
 'OceanShineUserAttrsRequest':'request.UserAttrsReq',
 'OceanShineGetAdRevenueReportIdRequest':'request.GetAdRevenueReportIdReq',
 'OceanShineGetAdRevenueReportIdResponse':'response.GetAdRevenueReportIdResponse',
 'OceanShineUserLoginRequest':'request.UserLoginReq',
 'OceanShineLoginResponse':'response.LoginResponse',
 'OceanShineLoginResponse.ActiveRule':'inline.ActiveRule',
 'OceanShineFeedbackRequest':'request.FeedbackReq',
 'OceanShineFeedbackListV2Request':'request.FeedbackListV2Req',
 'OceanShineFeedbackListV2Response':'response.FeedbackListV2Response',
 'OceanShineFeedbackListV2Response.OceanShineFeedbackListV2ResponseData':'response.FeedbackListV2ResponseData',
 'OceanShineUserReachReportRequest':'request.UserReachReportReq',
 'OceanShineUserReachReportResponse':'response.UserInfoResponse',
 'OceanShineUserReachReportResponse.NewcomerReward':'response.AppConfig',
 'OceanShineUserInfoRequest':'request.UserInfoReq',
 'OceanShineWithdrawalRecord':'response.WithdrawalRecord',
 'OceanShineWithdrawalPageRequest':'request.WithdrawalPageReq',
 'OceanShineWithdrawalPageResponse':'response.WithdrawalPageResponse',
 'OceanShineWithdrawalPageResponse.WithdrawalRatio':'response.WithdrawRatio',
 'OceanShineWithdrawalPageResponse.WithdrawalPlatform':'response.NewWithdrawalMethodGoods',
 'Apid_Usid_Vn_Request':'request.RoutineTaskLookAdMoneyReq',
 'RoutineTaskLookAdMoneyResponse':'response.RoutineTaskLookAdMoneyResponse',
 'OceanShineUserInfoResponse':'response.UserInfoResponse',
 'OceanShineUserInfoResponse.NewcomerReward':'response.AppConfig',
 'OceanShineAdLogReportRequest':'request.AdLogReportReq',
 'OceanShineAdLogReportRequest.CommonInfo':'request.CommonInfo',
 'OceanShineAdLogReportRequest.ExtendParam':'request.AdLogExtendParam',
 'OceanShineAppEventReportRequest':'request.AppEventReportReq',
 'OceanShineAppEventReportRequest.CommonInfo':'request.CommonInfo',
 'OceanShineAppEventReportRequest.ExtendParam':'request.AppEventLogExtendParam',
}
def schemas(cls):
    kind,suffix=class_schema[cls].split('.')
    if kind=='inline':
        old='response.FruitTileMatchLoginResponse';new='response.SnakeOutJameLoginResponse'
        return old+'.FtMaRu',new+'.SoJAru',definitions[old]['properties']['FtMaRu']['properties'],definitions[new]['properties']['SoJAru']['properties']
    old=kind+'.FruitTileMatch'+('AppOtherConfigCustomizeReq' if cls=='OceanShineAppOtherConfigRequest' else suffix)
    new=kind+'.SnakeOutJame'+suffix
    return old,new,definitions[old]['properties'],definitions[new]['properties']

# Every key here was individually read in the target property list. This is an
# explicit semantic/member map; no prefix replacement is performed on source text.
member_keys=dict(pair.split('=') for pair in '''
Os_Apid=SoJApid Os_Usid=SoJUsid Os_Vn=SoJVn Vn=Vn
Os_Bac=SoJBac Os_Baci=SoJBaci Os_Con=SoJCon Os_Crc=SoJCrc Os_Crcs=SoJCrcs
Os_Cty=SoJCty Os_Ewl=SoJEwl Os_Img=SoJImg Os_Lev=SoJLev Os_Lgd=SoJLgd
Os_Mny=SoJMny Os_Ncm=SoJNcm Os_Nnm=SoJNnm Os_Rol=SoJRol Os_Rti=SoJRti Os_Rts=SoJRts Os_Tra=SoJTra
Os_Btid=SoJBtid Os_Ecm=SoJEcm Os_Sal=SoJSal Os_Uso=SoJUso Os_Prc=SoJPrc Os_Mul=SoJMul
Os_Cp=SoJCp Os_Mid=SoJMid Os_Pb=SoJPb Os_Ra=SoJRa Os_Re=SoJRe Os_Rm=SoJRm Os_Rn=SoJRn Os_Seid=SoJSeid Os_Odr=SoJOdr Os_Cfn=SoJcfn
Os_Agp=SoJAgp Os_Aid=SoJAid Os_Cat=SoJCat Os_Ccy=SoJCcy Os_Ckl=SoJCkl Os_Cmp=SoJCmp
Os_Cti=SoJCti Os_Ctm=SoJCtm Os_Dlu=SoJDlu Os_Evt=SoJEvt Os_Fbr=SoJFbr Os_Gct=SoJGct
Os_Net=SoJNet Os_Trn=SoJTrn Os_Trt=SoJTrt
Os_Anid=SoJAnid Os_Atd=SoJAtd Os_Bbd=SoJBbd Os_Cal=SoJCal Os_Ctr=SoJCtr
Os_Dtd=SoJDtd Os_Dth=SoJDth Os_Dtw=SoJDtw Os_Gaid=SoJGaid Os_Lag=SoJLag Os_Mbl=SoJMbl
Os_Nbt=SoJNbt Os_Obv=SoJObv Os_Rtt=SoJRtt Os_Sbi=SoJSbi Os_Try=SoJTry Os_Ttz=SoJTtz Os_Cpu=SoJCpu Os_Ua=SoJUa Os_Vc=SoJVc
Os_Aru=SoJAru Os_Rdt=SoJRdt Os_Acm=SoJAcm Os_Aiu=SoJAiu Os_Des=SoJDes Os_Msl=SoJMsl
Os_Cda=SoJCda Os_Cte=SoJCte Os_Ctt=SoJCtt Os_Id=SoJId Os_Tpe=SoJTpe Os_Uda=SoJUda Os_Ute=SoJUte
Os_Acg=SoJAcg Os_Com=SoJCom Os_Nba=SoJNba Os_Ncc=SoJNcc Os_Rnw=SoJRnw Os_Usd=SoJUsd
Os_Dat=SoJDat Os_Pym=SoJPym Os_Rrm=SoJRrm Os_Sts=SoJSts Os_Tsm=SoJTsm
Os_Wr=SoJWr Os_Wwf=SoJWwf Os_Ady=SoJAdy Os_Ben=SoJBen Os_End=SoJEnd Os_Wro=SoJWro
Os_Cn=SoJCn Os_Me=SoJMe Os_Mlt=SoJMlt Os_Et=SoJEt
Os_An=SoJAn Os_Css=SoJCss Os_Ln=SoJLn Os_Mrt=SoJMrt Os_My=SoJMy Os_Sr=SoJSr Os_Ss=SoJSs Os_Tid=SoJTid
Os_Cnf=SoJCnf Os_Epm=SoJEpm Os_Abd=SoJAbd Os_Adgp=SoJAdgp Os_Cid=SoJCid Os_Ctc=SoJCtc
Os_EvtM=SoJEvtM Os_Mbc=SoJMbc Os_Mbp=SoJMbp Os_Ptf=SoJPtf Os_Bgd=SoJBgd Os_EvtE=SoJEvtE
'''.split())
report={'tag':'SnakeOutJame','swaggerCache':str(SWAGGER),'cacheSha256':hashlib.sha256(SWAGGER.read_bytes()).hexdigest(),
 'pathMappings':[],'fieldMappings':[],'DTOChanges':[],'jsonKeyMappings':[],'otherMappings':[],
 'Unresolved':[],'SwaggerTypeMismatch':[],'validation':{},'notes':[]}
edits=[];cfg_edits=[]
def edit(start,end,new,category,**meta):
    edits.append(dict(start=start,end=end,old=before[start:end],new=new,category=category,**meta))
inventory={}
for f in field_pattern.finditer(before):
    cls=owner(scopes,f.start());name=f['name'];key=f['key'];typ=f['type'].strip()
    inventory.setdefault(cls,[]).append(f)
    old_schema,target_schema,old_props,target_props=schemas(cls)
    old_prop=old_props.get(key)
    if not old_prop and key in ['SbmRol','SbmTsm']:
        kind,suffix=class_schema[cls].split('.')
        old_schema=kind+'.SportBallsMatch'+suffix
        old_prop=definitions[old_schema]['properties'][key]
    assert old_prop is not None,(cls,name,key)
    row=dict(file=str(API/'AccountModule.cs'),className=cls,field=name,csharpType=typ,old=key,oldSchema=old_schema,targetSchema=target_schema,oldDescription=old_prop.get('description',''),beforeLine=before[:f.start()].count('\n')+1)
    if cls=='OceanShineApplyWithdrawalRequestFake' and name in ['Os_Mn','Os_At']:
        row.update(new=key,status='Unresolved',candidate='SoJMn',reason='Target exposes only SoJMn but its description embeds ApplyType/json:"SoJAt". Current Os_Mn is task_{id}; Os_At is money/roll apply type. Cannot uniquely confirm two-to-one semantic mapping from this schema.',targetDescription=target_props['SoJMn']['description'],localValue='_Os_Mn' if name=='Os_Mn' else '_Os_At')
        report['Unresolved'].append(row)
        continue
    new=member_keys[name]
    assert new in target_props,(cls,name,new)
    row.update(new=new,targetDescription=target_props[new].get('description',''),status='confirmed',action='JsonProperty')
    report['fieldMappings'].append(row)
    edit(f.start('key'),f.end('key'),new,'JsonProperty',className=cls,field=name)

def add_fields(cls,fields):
    # Insertion immediately after the class opening preserves all existing method text.
    c=next(s for s in scopes if s['name']==cls)
    declarations=''
    target_props=schemas(cls)[3]
    for name,typ,key,description in fields:
        assert key in target_props and name not in [f['name'] for f in inventory[cls]]
        declarations+='\r\n        [JsonProperty("'+key+'")]\r\n        public '+typ+' '+name+'; // '+description+'\r\n'
        report['DTOChanges'].append(dict(action='add',className=cls,field=name,csharpType=typ,new=key,targetSchema=schemas(cls)[1],targetDescription=target_props[key].get('description',''),businessReferences='new field; no existing references'))
    edit(c['start']+1,c['start']+1,declarations,'dto-add',className=cls)

add_fields('OceanShineApplyWithdrawalRequestReal',[('Os_Gdid','int','SoJGdid','商品ID')])
add_fields('OceanShineApplyWithdrawalRequestFake',[('Os_Gdid','int','SoJGdid','商品ID')])
add_fields('OceanShineGetAdRevenueReportIdResponse',[('Os_Pfm','string','SoJPfm','广告平台名称')])
add_fields('OceanShineLoginResponse',[('Os_Rw','int','SoJRw','1为审核模式，0或2为投放模式')])
add_fields('OceanShineWithdrawalRecord',[('Os_Ded','double','SoJDed','扣除金额或现金提现时转roll金额'),('Os_Try','int','SoJTry','1现金提现，2roll提现')])

def find_method(name,text=before):
    m=re.search(r'(?m)^    public [^\r\n]+\b'+re.escape(name)+r'\s*\(',text)
    assert m,name
    masked=code_mask(text);start=masked.index('{',m.end())
    return start,block_end(masked,start)
for name in ['GetApplyWithdrawalRequestReal','GetApplyWithdrawalRequestFake']:
    start,end=find_method(name);needle='            Os_Mid = _Os_Mid,'
    assert before[start:end].count(needle)==1
    at=before.index(needle,start,end)
    edit(at,at,'            Os_Gdid = 0,\r\n','authorized-withdrawal-builder',method=name,reason='New optional integer goods field initialized with C# default; no goods ID parameter exists in protected method signature')
report['notes'].append('Both new SoJGdid integers use the C# int default 0 because protected builder signatures provide payment configuration ID, not a distinct goods ID. Swagger defines no default. The project has no established goods-ID value source, and server acceptance of 0 has not been tested; this is not a claim of verified withdrawal behavior.')

start,end=find_method('CreateFromJson')
attr_map={x['old']:x['new'] for x in report['fieldMappings'] if x['className']=='OceanShineUserAttrsRequest'}
attr_keys=[]
for entry in re.finditer(r'\{ "(?P<key>[^"]+)", (?P<expression>[^\r\n]+) \}',before[start:end]):
    old=entry['key'];new=attr_map[old]
    prop=definitions['request.SnakeOutJameUserAttrsReq']['properties'][new]
    row=dict(file=str(API/'AccountModule.cs'),method='CreateFromJson(AdjustSdk.AdjustAttribution attribution, string eventName)',old=old,new=new,oldJsonPath='$.'+old,jsonPath='$.'+new,valueExpression=entry['expression'],targetType=prop['type'],targetDescription=prop.get('description',''),macro='BIZZA_REAL_WITHDRAW && BIZZA_ENABLE_ADJUST')
    report['jsonKeyMappings'].append(row);attr_keys.append(new)
    edit(start+entry.start('key'),start+entry.end('key'),new,'request-json-key',method='CreateFromJson')
assert len(attr_keys)==len(set(attr_keys))==19
assert set(attr_keys)==set(definitions['request.SnakeOutJameUserAttrsReq']['properties'])

for item in re.finditer(r'(?m)^    public const string (\w+) = "([^"]*)";',before_cfg):
    symbol,old=item[1],item[2]
    if not old.startswith('/'):continue
    new=endpoints[symbol];old_op=document['paths'][old]['post'];op=document['paths'][new]['post']
    assert 'SnakeOutJame' in op['tags']
    row=dict(symbol=symbol,old=old,new=new,method='POST',oldSummary=old_op.get('summary'),targetSummary=op.get('summary'),targetDescription=op.get('description'),oldRequestSchema=next(p['schema'] for p in old_op['parameters'] if p['in']=='body'),targetRequestSchema=next(p['schema'] for p in op['parameters'] if p['in']=='body'),requestImplementation='none; existing constant only' if symbol in ['order_name','user_name'] else 'existing AccountModule method')
    report['pathMappings'].append(row)
    cfg_edits.append(dict(start=item.start(2),end=item.end(2),old=old,new=new,category='api-path'))
assert len(report['pathMappings'])==19

# Custom-response keys require this run's actual backend response, not a naming guess.
comm_path=RUN/'communication-results.json'
communication=json.loads(comm_path.read_text(encoding='utf-8-sig'))
assert communication['tag']=='SnakeOutJame' and communication['appId']=='orchardtrio'
config_test=next(t for t in communication['tests'] if t['path']=='/Out/Jam/BaseData')
assert config_test['status']=='passed' and config_test['httpStatus']==200 and str(config_test['businessCode'])=='200'
assert config_test['request']['SoJcfn']=='look_ad_reward_mul'
actual_config=config_test['response']['decryptedData']
other_keys={'one_Count':'soj_one','one_Ratio':'soj_one_rate','two_Count':'soj_two','two_Ratio':'soj_two_rate','three_Count':'soj_three','three_Ratio':'soj_three_rate'}
for item in re.finditer(r'(?m)^    public const string (\w+) = "([^"]*)";',before_cfg):
    if item[1] not in other_keys:continue
    new=other_keys[item[1]]
    assert new in actual_config and isinstance(actual_config[new],int)
    cfg_edits.append(dict(start=item.start(2),end=item.end(2),old=item[2],new=new,category='confirmed-other-key'))
    report['otherMappings'].append(dict(symbol=item[1],old=item[2],new=new,responseValue=actual_config[new],evidence=str(comm_path),jsonPath='$.response.decryptedData.'+new,path='/Out/Jam/BaseData',configValue='look_ad_reward_mul',status='confirmed by current-run HTTP 200/business 200 response'))

def apply(text,changes):
    ordered=sorted(changes,key=lambda e:e['start'])
    assert all(a['end']<=b['start'] for a,b in zip(ordered,ordered[1:]))
    for e in reversed(ordered):
        assert text[e['start']:e['end']]==e['old']
        text=text[:e['start']]+e['new']+text[e['end']:]
    return text
after=apply(before,edits);after_cfg=apply(before_cfg,cfg_edits)

# Full-text projections allow precisely the recorded JSON tokens, DTO additions,
# and explicit withdrawal initializer additions. Nothing else may change.
def projection(original,current,changes):
    old_pos=new_pos=0
    for e in sorted(changes,key=lambda e:e['start']):
        gap=original[old_pos:e['start']]
        assert current[new_pos:new_pos+len(gap)]==gap
        new_pos+=len(gap)
        assert current[new_pos:new_pos+len(e['new'])]==e['new']
        new_pos+=len(e['new']);old_pos=e['end']
    assert current[new_pos:]==original[old_pos:]
projection(before,after,edits);projection(before_cfg,after_cfg,cfg_edits)
http_calls=lambda text:re.findall(r'HttpUtil\.RequestToServer[^;]+;',text)
assert http_calls(before)==http_calls(after)
decls=lambda text:re.findall(r'(?m)^[ \t]*(?:public|private|protected|internal)\s+(?:(?:static|async|virtual|override|sealed|new)\s+)*[\w<>.,?]+(?:\s+[\w<>.,?]+)?\s*\([^;{}]*\)\s*(?:where[^{}]+)?(?=\{)',code_mask(text))
assert decls(before)==decls(after)

after_scopes=class_spans(after);after_fields={}
for f in field_pattern.finditer(after):
    cls=owner(after_scopes,f.start());after_fields.setdefault(cls,[]).append(dict(key=f['key'],type=f['type'].strip(),field=f['name']))
reverse=[]
for cls,fields in after_fields.items():
    keys=[f['key'] for f in fields];target=set(schemas(cls)[3]);missing=target-set(keys);extra=set(keys)-target
    assert len(keys)==len(set(keys)),cls
    if cls=='OceanShineApplyWithdrawalRequestFake':
        assert missing=={'SoJMn'} and extra=={'FtMMn','FtMAt'}
    else:assert not missing and not extra,(cls,missing,extra)
    reverse.append(dict(className=cls,status='Unresolved' if missing or extra else 'passed',missing=sorted(missing),extra=sorted(extra),duplicates=[]))

def local_shape(cls,path='$'):
    shape={}
    for f in after_fields[cls]:
        key=f['key'];typ=f['type'];at=path+'.'+key
        primitive={'string':'string','int':'integer','long':'integer','double':'number','float':'number','bool':'boolean'}.get(typ)
        if primitive:shape[at]=primitive;continue
        array=typ.startswith('List<');typ=typ[5:-1] if array else typ
        candidates=[cls+'.'+typ]+['.'.join(cls.split('.')[:i]+[typ]) for i in range(len(cls.split('.'))-1,-1,-1)]
        nested=next(c for c in candidates if c in after_fields)
        shape[at]='array' if array else 'object';shape.update(local_shape(nested,at+'[]' if array else at))
    return shape
def resolved(schema):
    if '$ref' in schema:return resolved(definitions[schema['$ref'].split('/')[-1]])
    if 'allOf' in schema:return resolved(schema['allOf'][0])
    return schema
def schema_shape(schema,path='$'):
    shape={}
    for key,value in resolved(schema).get('properties',{}).items():
        value=resolved(value);at=path+'.'+key;typ=value.get('type','object' if 'properties' in value else None);shape[at]=typ
        if typ=='object':shape.update(schema_shape(value,at))
        if typ=='array':shape.update(schema_shape(value['items'],at+'[]'))
    return shape
request_classes={
 'add_ecpm':'OceanShineAdRevenueRequest','add_order':'OceanShineApplyWithdrawalRequestReal','add_order_task':'OceanShineApplyWithdrawalRequestFake',
 'base_list':'OceanShineAppOtherConfigRequest','from':'OceanShineUserAttrsRequest','get_ecpm_id':'OceanShineGetAdRevenueReportIdRequest',
 'login':'OceanShineUserLoginRequest','msg':'OceanShineFeedbackRequest','msg_list':'OceanShineFeedbackListV2Request','new_user':'OceanShineUserReachReportRequest',
 'number':'OceanShineUserReachReportRequest','order_list':'OceanShineUserInfoRequest','plat_from':'OceanShineWithdrawalPageRequest',
 'task_list':'Apid_Usid_Vn_Request','user_info':'OceanShineUserInfoRequest','ad_info':'OceanShineAdLogReportRequest','app_info':'OceanShineAppEventReportRequest',
}
recursive=[]
for symbol,cls in request_classes.items():
    p=next(p for p in report['pathMappings'] if p['symbol']==symbol);actual=local_shape(cls);expected=schema_shape(p['targetRequestSchema'])
    missing=set(expected)-set(actual);extra=set(actual)-set(expected);wrong=[k for k in actual.keys()&expected.keys() if actual[k]!=expected[k]]
    assert not wrong
    if symbol!='add_order_task':assert actual==expected
    recursive.append(dict(symbol=symbol,path=p['new'],className=cls,status='Unresolved' if missing or extra else 'passed',mode='static; runtime serialization not executed',actualJsonPathsAndTypes=actual,missing=sorted(missing),extra=sorted(extra),typeMismatch=wrong))

report['SwaggerTypeMismatch']=[
 dict(symbol='new_user',path=endpoints['new_user'],swaggerResponse='array<response.SnakeOutJameUserInfoResponse>',localResponse='OceanShineUserReachReportResponse',resolution='Local callback and HTTP generic preserved'),
 dict(symbol='number',path=endpoints['number'],swaggerResponse='array<response.SnakeOutJameUserInfoResponse>',localResponse='OceanShineUserReachReportResponse',resolution='Local callback and HTTP generic preserved'),
 dict(symbol='task_list',path=endpoints['task_list'],swaggerResponse='response.SnakeOutJameRoutineTaskLookAdMoneyResponse',localResponse='List<RoutineTaskLookAdMoneyResponse>',resolution='Local callback and HTTP generic preserved'),
]
report['validation']={
 'methodContract':dict(status='passed',mode='static complete permitted-token projection',methodDeclarationCount=len(decls(before)),declarationsUnchanged=True,httpCallCount=len(http_calls(before)),httpCallsUnchanged=True,allOtherMethodTextUnchanged=True,authorizedBuilderChanges=[e for e in edits if e['category']=='authorized-withdrawal-builder']),
 'actualAttributionPayload':dict(status='passed',mode='static only; serialization not executed',trace=['AdjustAttributionAdapter.AttributionChangedDelegate','AccountModule.CreateFromJson','Request_UserAttrsRequest(Dictionary<string, object>)','JsonConvert.SerializeObject(request)','HttpUtil.RequestToServer<OceanShineUserAttrsRequest>'],macrosInspected=['BIZZA_REAL_WITHDRAW','BIZZA_ENABLE_ADJUST'],targetKeyCount=19,actualKeyCount=19,missing=[],extra=[],duplicates=[],confirmedOldKeysRemaining=[],valuesAndTypesUnchanged=True,timeUnitPreserved='DateTimeOffset.UtcNow.ToUnixTimeSeconds()',nullHandlingPreserved='attribution.CostAmount ?? 0'),
 'reverseDtoFields':reverse,'recursiveRequestPayloads':recursive,
 'existingPathsWithoutRequests':['order_name','user_name'],
 'otherConfig':dict(status='passed',requestConfigValue='look_ad_reward_mul',requestConfigValuePreserved=True,currentResponseKeys=list(other_keys.values()),evidence=str(comm_path)),
 'compilation':'not executed by this mapper; root owns Unity compilation',
 'communication':'not executed by this mapper; separate current-run task',
}
report['interfaceStatus']=[dict(symbol=p['symbol'],path=p['new'],status='Incomplete / Unresolved' if p['symbol']=='add_order_task' else 'path confirmed; no existing request implementation' if p['symbol'] in ['order_name','user_name'] else 'confirmed fields synchronized',reason='SoJMn meaning cannot be uniquely confirmed; FtMMn and FtMAt are retained and missing target SoJMn is explicitly unresolved.' if p['symbol']=='add_order_task' else '') for p in report['pathMappings']]
report['edits']={'AccountModule':edits,'AccountModuleCfg':cfg_edits}
existing_report=RUN/'api-mapping.json'
previous=json.loads(existing_report.read_text(encoding='utf-8-sig')) if existing_report.exists() else None
allowed_source=[before,after]+([apply(before,previous['edits']['AccountModule'])] if previous else [])
allowed_cfg=[before_cfg,after_cfg]+([apply(before_cfg,previous['edits']['AccountModuleCfg'])] if previous else [])
assert read(API/'AccountModule.cs') in allowed_source,'Unexpected concurrent API edit'
assert read(API/'AccountModuleCfg.cs') in allowed_cfg,'Unexpected concurrent configuration edit'
for path,text in [(API/'AccountModule.cs',after),(API/'AccountModuleCfg.cs',after_cfg)]:
    if path.read_bytes()!=text.encode('utf-8'):path.write_bytes(text.encode('utf-8'))
existing_report.write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'paths':len(report['pathMappings']),'JsonProperties':len(report['fieldMappings']),'DTOAdditions':len(report['DTOChanges']),'actualDictionaryKeys':len(report['jsonKeyMappings']),'Unresolved':len(report['Unresolved']),'SwaggerTypeMismatch':len(report['SwaggerTypeMismatch'])}))
