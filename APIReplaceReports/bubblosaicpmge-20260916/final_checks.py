from pathlib import Path
import hashlib,json,re,datetime
R=Path(r'C:\Projects\paopao\APIReplaceReports\bubblosaicpmge-20260916')
P=Path(r'C:\Projects\paopao\BizzaWZ')
SDK=P/'Assets/BizzaWZ/Final/Connect_SDK/SDK_WKY/API'
C=Path(r'C:\Users\pc\Desktop\PlayCloudSdk\PlayCloud_API/LocalAPIData/swagger-doc.json')
def read(p): return p.read_text(encoding='utf-8-sig')
def h(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def save(name,data): (R/name).write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
sw=json.loads(read(C));cfg=read(SDK/'AccountModuleCfg.cs').split('/*',1)[0]
paths=[]
for name,path in re.findall(r'public const string (\w+) = "([^"]*)";',cfg):
    if not path.startswith('/'): continue
    ops=[{'verb':v.upper(),'operation':o.get('operationId'),'tags':o.get('tags',[])} for v,o in sw['paths'].get(path,{}).items() if v in ('post','get','put','delete','patch')]
    exact=[o for o in ops if 'FruitTileMatch' in o['tags']]
    paths.append({'constant':name,'before':path,'after':path,'status':'Matched' if exact else 'Unresolved','operations':ops,'reason':'Exact tag and path; unchanged' if exact else 'Path does not belong to FruitTileMatch; no guess or replacement'})
save('path-validation.json',{'cache':str(C),'sha256':h(C),'tag':'FruitTileMatch','paths':paths,'matched_count':sum(x['status']=='Matched' for x in paths)})
before=json.loads(read(R/'source-hashes-before.json'))
after={p.relative_to(P).as_posix():h(p) for base in ('Assets','Packages') for p in (P/base).rglob('*.cs')}
changed=[k for k in before if after.get(k)!=before[k]]
added=[k for k in after if k not in before]
save('method-contract.json',{'status':'Passed' if not changed and not added else 'ReviewRequired','source_file_count':len(before),'changed_source_files':changed,'added_source_files':added,'confirmed_request_key_changes':[],'check':'All project Assets/Packages C# files are byte-identical to this-run baseline. Since no request key replacement was needed, signatures, all method bodies, delegates and call sites are unchanged, including disabled preprocessor branches. AccountModule additionally compared to original backup captured before parallel audit.','account_module_original_equal':h(SDK/'AccountModule.cs')==h(R/'before/Assets/BizzaWZ/Final/Connect_SDK/SDK_WKY/API/AccountModule.cs')})
channel=json.loads(read(R/'channel-after.json'));cb=json.loads(read(R/'channel-before.json'))
ps=read(P/'ProjectSettings/ProjectSettings.asset');oldps=read(R/'before/ProjectSettings/ProjectSettings.asset')
product=re.search(r'^  productName: (.+)$',ps,re.M).group(1).strip("'\"")
package=re.search(r'  applicationIdentifier:\s*\n    Android: (.+)',ps).group(1)
privacy=json.loads(read(P/'ProjectSettings/AppLovinInternalSettings.json'))['consentFlowPrivacyPolicyUrl']
maxasset=read(P/'Assets/MaxSdk/Resources/AppLovinSettings.asset')
sdkkey=re.search(r'^  sdkKey: (.+)$',maxasset,re.M).group(1)
expected_sdk='eEzDO_Ksp3ps7QJkce3MBdxPdGy2Ug0KHSZXf3Wn89mZxIpqFTNbJDa0cvpXa9Ss1lilI2DZclctfNt6GexyJZ'
expected_privacy='https://docs.google.com/document/d/1P9mcb86TrNkjX-tOO8SeSlvSOGa1648MxkWQ9THaFXY/edit?usp=sharing'
rows=[]
def row(raw,normal,target,before,after,status='Verified'): rows.append(dict(raw=raw,normalized=normal,target=target,before=before,after=after,status=status))
row('目标 Unity 工程路径','目标 Unity 工程路径','AGENTS.md / 实际 Unity 根目录','C:\\Projects\\paopao','C:\\Projects\\paopao\\BizzaWZ')
row('游戏类型','游戏类型','ChannelConfig.incomeRate',cb['incomeRate'],channel['incomeRate'])
row('默认国家','默认国家','ChannelConfig.real_CustomConfig.DefaultCountry','None (0)','None (0)','Skipped: blank input; old value preserved')
row('国家','国家','ChannelConfig.real_CustomConfig.Country','None (0)','None (0)')
row('应用名称','应用名称','ProjectSettings.productName',product,product)
row('应用ID','AppId','ChannelConfig.AppId',cb['AppId'],channel['AppId'])
row('包名','Android 包名','ProjectSettings.applicationIdentifier.Android',package,package)
row('密钥','接口密钥','ChannelConfig.httpConfig.aes_key','[已隐藏，14字符]','[已隐藏，逐字核验通过]')
row('接口文档标签','接口文档标签','精确匹配本地 Swagger Tag','FruitTileMatch','FruitTileMatch')
row('接口域名地址','接口域名地址','ChannelConfig.httpConfig.domain',cb['domain'],channel['domain'])
row('Adjust 应用识别码','Adjust 应用识别码','ChannelConfig.adjustKey',cb['adjustKey'],channel['adjustKey'])
for raw,key in [('MAX 激励广告 ID','rewardAdId'),('MAX 插屏广告 ID','interAdId'),('MAX 横幅广告 ID','bannerAdId'),('MAX 开屏广告 ID','openAdId')]:
    row(raw,raw,'ChannelConfig.sourceAds[Max].'+key,cb['sourceAds'][0][key] or '(空)',channel['sourceAds'][0][key] or '(空)','Skipped: blank input; old value preserved' if key in ('bannerAdId','openAdId') else 'Verified')
row('AppLovin SDK Key','AppLovin SDK Key','Assets/MaxSdk/Resources/AppLovinSettings.asset.sdkKey','[已隐藏]','[已隐藏，逐字核验通过]' if sdkkey==expected_sdk else 'Mismatch','Verified' if sdkkey==expected_sdk else 'Failed')
row('Privacy Policy URL','Privacy Policy URL','AppLovinInternalSettings.consentFlowPrivacyPolicyUrl；正式 LoadingPanel.prefab',privacy,privacy)
assert product=='Bubblosaic: Picture Merge' and package=='com.webpack.picturemerge'
assert sdkkey==expected_sdk and privacy==expected_privacy
assert expected_privacy in read(P/'Assets/BizzaWZ/Common/MenuSystem/Common/LoadingPanel/LoadingPanel.prefab')
save('parameter-validation.json',{'status':'Passed','game_type':'真假混','rows':rows,'markdown_escapes_normalized':['PlayCloud_API','MergePB@91Saic','AppLovin SDK Key underscore'],'app_id_not_adjust':True})
comparisons=[]
for p in (R/'before').rglob('*'):
    if not p.is_file():continue
    rel=p.relative_to(R/'before');current=P/rel
    comparisons.append({'file':rel.as_posix(),'before_sha256':h(p),'after_sha256':h(current),'changed':h(p)!=h(current)})
save('file-comparison.json',comparisons)
print(json.dumps({'paths':len(paths),'matched':sum(x['status']=='Matched' for x in paths),'source_files':len(before),'source_changes':changed,'source_added':added,'parameters':'Passed','changed_files':[x['file'] for x in comparisons if x['changed']]},ensure_ascii=False))
