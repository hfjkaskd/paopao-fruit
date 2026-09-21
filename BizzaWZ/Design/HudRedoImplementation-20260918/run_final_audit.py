from pathlib import Path
import json,re,hashlib,struct,subprocess
root=Path(__file__).resolve().parents[2];out=Path(__file__).resolve().parent
read=lambda p:p.read_text(encoding='utf-8-sig')
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
changes=[]
log_names=[]
for log_name in ['visual-changes.json','visual-refinement.json','visual-feedback.json']:
 if (out/log_name).exists():
  log_data=json.loads(read(out/log_name));changes.extend(log_data if isinstance(log_data,list) else log_data['changes']);log_names.append(log_name)
allow=json.loads(read(out/'audit-allowlist.json'))
baseline=json.loads(read(out/'audit-baseline.json'))
errors=[]
allowed={(x['path'],x['fileID'],f) for x in allow['directFields'] for f in x['allowedFields']}
ov_allowed={(allow['nestedOverrides']['path'],x['targetFileID'],f) for x in allow['nestedOverrides']['allowedOverrides'] for f in x['properties']}
header=lambda t:re.findall(r'^--- !u!(\d+) &(\d+)( stripped)?$',t,re.M)
def blocks(t):return {m[2]:{'class':m[1],'stripped':bool(m[3]),'body':m[4]} for m in re.finditer(r'^--- !u!(\d+) &(\d+)( stripped)?\n(.*?)(?=^--- !u!|\Z)',t,re.M|re.S)}
def direct(d,id,key):
 m=re.search(r'^  '+re.escape(key)+r': (.*)$',d[str(id)]['body'],re.M)
 return m[1] if m else None
files=[];prefabs={};button_checks=[]
for base in baseline['prefabs']:
 rel=base['path'];p=root/rel;b=out/'Before'/rel;old=read(b);new=read(p);restore=new;problems=[]
 if sha(b)!=base['sha256']:problems.append('Backup no longer matches immutable baseline hash')
 if header(old)!=header(new):problems.append('Object IDs/order/classes changed')
 prev,cur=blocks(old),blocks(new);prefabs[rel]=cur
 for fid,ob in prev.items():
  if ob['class']!='114' or not ('  m_OnClick:' in ob['body'] or '  onClick:' in ob['body']):continue
  actual=cur[fid]['body'];expected=ob['body'];exception=None
  if fid=='3586814762122687189':
   bf='  scaleTarget: {fileID: 5304515813898986083}';af='  scaleTarget: {fileID: 7343093305673801710}'
   if af in actual:expected=expected.replace(bf,af);exception={'field':'scaleTarget','before':'5304515813898986083','after':'7343093305673801710'}
  ok=actual==expected
  button_checks.append({'path':rel,'fileID':fid,'allFieldsUnchangedExceptDocumentedVisualReference':ok,'visualFeedbackException':exception})
  if not ok:problems.append('Button field changed outside visual feedback exception '+fid)
 rects=[k for k,v in prev.items() if v['class']=='224']
 unchanged_rects=all(cur.get(k)==prev[k] for k in rects)
 if not unchanged_rects:problems.append('RectTransform changed')
 logs=[c for c in changes if c['file']==rel]
 for c in reversed(logs):
  fid=c['fileID'];field=c.get('override',c['field'])
  if 'override' in c:
   if (rel,fid,field) not in ov_allowed:problems.append('Override outside whitelist '+fid+'/'+field)
   pattern=r'(    - target: \{fileID: '+fid+r',[^\n]+\}\n      propertyPath: '+re.escape(field)+r'\n)(      value: [^\n]*\n      objectReference: [^\n]*)'
   ms=list(re.finditer(pattern,restore,re.M));kind=c['field']
   if len(ms)!=1:problems.append('Missing/duplicate override '+fid+'/'+field);continue
   m=ms[0];body=m[0];fp=r'^      '+re.escape(kind)+r': (.*)$';fm=re.search(fp,body,re.M)
   if fm is None or fm[1]!=c['after']:problems.append('Final override not equal logged after '+fid+'/'+field)
   replaced=re.sub(fp,lambda m:'      '+kind+': '+c['before'],body,flags=re.M)
  else:
   if (rel,fid,field) not in allowed:problems.append('Field outside whitelist '+fid+'/'+field)
   ms=list(re.finditer(r'^--- !u!\d+ &'+fid+r'\n.*?(?=^--- !u!|\Z)',restore,re.M|re.S))
   if len(ms)!=1:problems.append('Missing/duplicate object '+fid);continue
   m=ms[0];body=m[0];fp=r'^  '+re.escape(field)+r': (.*)$';fm=re.search(fp,body,re.M)
   if fm is None or fm[1]!=c['after']:problems.append('Final value not equal logged after '+fid+'/'+field)
   replaced=re.sub(fp,lambda m:'  '+field+': '+c['before'],body,flags=re.M)
  restore=restore[:m.start()]+replaced+restore[m.end():]
 if restore!=old:problems.append('Unlogged difference remains after reversing visual changes')
 if base['guardOnly'] and new!=old:problems.append('Guard-only prefab changed')
 errors += [rel+': '+x for x in problems]
 files.append({'path':rel,'result':'PASS' if not problems else 'FAIL','guardOnly':base['guardOnly'],'serializedObjects':len(prev),'rectTransforms':len(rects),'allRectTransformsUnchanged':unchanged_rects,'visualChanges':len(logs),'objectIdsOrderClassesUnchanged':header(old)==header(new),'exactlyReversibleVisualDiff':restore==old,'hierarchyComponentListsBusinessBindingsLocalizationUnchanged':restore==old,'beforeSha256':sha(b),'afterSha256':sha(p),'issues':problems})
expected_files={x['path'] for x in baseline['prefabs']}
if any(c['file'] not in expected_files for c in changes):errors.append('Logged prefab outside baseline scope')
codebase=json.loads(read(out/'audit-code-hashes.json'));code={p.relative_to(root).as_posix():sha(p) for p in (root/'Assets').rglob('*.cs')}
code_report={'result':'PASS' if codebase==code else 'FAIL','baselineCount':len(codebase),'currentCount':len(code),'changed':[p for p in codebase if p in code and codebase[p]!=code[p]],'added':sorted(set(code)-set(codebase)),'missing':sorted(set(codebase)-set(code))}
if codebase!=code:errors.append('Code baseline hash/set differs')
artbase=json.loads(read(out/'audit-art-hashes.json'));artchanged=[p for p,h in artbase.items() if not (root/p).exists() or sha(root/p)!=h]
if artchanged:errors.append('Previously shared art changed: '+str(artchanged))
# Resolve actual nested CurrencyBar values; local source changes must survive the instance overrides.
widget_rel='Assets/BizzaWZ/Final/Real/UI/GamePanel/Resources/Resources/GameUiWidget.prefab';currency_rel='Assets/BizzaWZ/Final/MenuSystem/Common/CurrencyBar/CurrencyBar.prefab'
widget=prefabs[widget_rel];currency=prefabs[currency_rel];overrides=[]
for inst,obj in widget.items():
 if obj['class']!='1001':continue
 for m in re.finditer(r'    - target: \{fileID: (\d+), guid: (\w+), type: 3\}\n      propertyPath: (.*?)\n      value: (.*?)\n      objectReference: (.*?)\n',obj['body']):
  fid,guid,key,value,ref=m.groups();overrides.append({'instance':inst,'id':fid,'guid':guid,'key':key,'value':ref if ref!='{fileID: 0}' else value})
checks=[]
final_currency_changes={}
for c in changes:
 if c['file']==currency_rel:final_currency_changes[(c['fileID'],c['field'])]=c
for c in final_currency_changes.values():
 key=c['field'];fid=c['fileID'];want=c['after']
 if key=='m_fontColor':
  channel_values=re.findall(r'([rgba]): ([0-9.]+)',want)
  for ch,v in channel_values:
   candidates=[x for x in overrides if x['id']==fid and x['key']=='m_fontColor.'+ch and x['guid']=='b1f781d8c27eec54db58a4849081f7e5']
   actual=candidates[0]['value'] if candidates else dict(re.findall(r'([rgba]): ([0-9.]+)',direct(currency,fid,key)))[ch]
   checks.append({'target':fid,'field':key+'.'+ch,'actual':actual,'expected':v,'pass':actual==v,'source':'override' if candidates else 'source prefab'})
 else:
  candidates=[x for x in overrides if x['id']==fid and x['key']==key and x['guid']=='b1f781d8c27eec54db58a4849081f7e5']
  actual=candidates[0]['value'] if candidates else direct(currency,fid,key)
  checks.append({'target':fid,'field':key,'actual':actual,'expected':want,'pass':actual==want,'source':'override' if candidates else 'source prefab'})
if any(not c['pass'] for c in checks):errors.append('Nested CurrencyBar effective values mismatch')
# Import config and PNG provenance, without editing/cropping generated pixels.
controls=json.loads(read(out/'Art/control-import-info.json'));gift=json.loads(read(out/'Art/Gift/import-info.json'));lock=json.loads(read(out/'Art/Lock/metadata.json'))
newassets=[]
for name,c in controls.items():newassets.append({'name':name,'path':c['path'],'guid':c['guid'],'fileID':c['fileID'],'size':c['sourceSize'],'rect':c['rect'],'border':c['border'],'sha':c['sha256'],'cap':c['maxTextureSize'],'mode':'2','original':out/'Art'/name/'original.png'})
newassets.append({'name':'Gift','path':'Assets/OrchardUI/Art/HudNaturalGift.png','guid':gift['guid'],'fileID':gift['fileID'],'size':[gift['width'],gift['height']],'rect':gift['spriteRectBottomLeft'],'border':[0,0,0,0],'sha':gift['productionImageSha256'],'cap':gift['maxTextureSize'],'mode':'1','original':out/'Art/Gift/original.png'})
newassets.append({'name':'Lock','path':'Assets/OrchardUI/Art/HudCopperLock.png','guid':lock['guid'],'fileID':lock['fileID'],'size':lock['sourceSize'],'rect':[lock['spriteRectBottomLeft'][k] for k in ['x','y','width','height']],'border':[0,0,0,0],'sha':lock['sha256'],'cap':lock['maxTextureSize'],'mode':'2','original':out/'Art/Lock/generated-original.png'})
asset_reports=[]
for a in newassets:
 p=root/a['path'];meta=read(Path(str(p)+'.meta'));raw=p.read_bytes();size=list(struct.unpack('>II',raw[16:24]));problems=[]
 def val(k):
  m=re.search(r'^\s*'+re.escape(k)+r': (.*)$',meta,re.M);return m[1] if m else None
 required={'guid':a['guid'],'textureType':'8','spriteMode':a['mode'],'alphaUsage':'1','alphaIsTransparency':'1','enableMipMap':'0','isReadable':'0','filterMode':'1','wrapU':'1','wrapV':'1','nPOTScale':'0','spritePixelsToUnits':'100'}
 for k,v in required.items():
  if val(k)!=v:problems.append('Unexpected '+k+': '+str(val(k)))
 if any(v!=str(a['cap']) for v in re.findall(r'^\s*maxTextureSize: (.*)$',meta,re.M)):problems.append('Inconsistent texture cap')
 if size!=a['size']:problems.append('PNG dimensions differ')
 if sha(p).lower()!=a['sha'].lower() or sha(p)!=sha(a['original']):problems.append('PNG source bytes differ')
 x,y,w,h=a['rect']
 if min(x,y)<0 or min(w,h)<=0 or x+w>size[0] or y+h>size[1]:problems.append('Sprite rect outside PNG')
 if a['mode']=='2':
  m=re.search(r'      rect:\n        serializedVersion: 2\n        x: ([0-9.]+)\n        y: ([0-9.]+)\n        width: ([0-9.]+)\n        height: ([0-9.]+)',meta)
  if not m or list(map(float,m.groups()))!=a['rect']:problems.append('Meta rect differs from import manifest')
  m=re.search(r'      border: \{x: ([0-9.]+), y: ([0-9.]+), z: ([0-9.]+), w: ([0-9.]+)\}',meta)
  if not m or list(map(float,m.groups()))!=a['border']:problems.append('Meta border differs from import manifest')
  if ('internalID: '+str(a['fileID'])) not in meta or not re.search(r'^      [^\n]+: '+str(a['fileID'])+'$',meta,re.M):problems.append('Sprite internal ID mapping missing')
 left,bottom,right,top=a['border']
 if min(a['border'])<0 or left+right>w or bottom+top>h:problems.append('Invalid 9-slice border')
 if a['guid'] not in '\n'.join(c['after'] for c in changes if c.get('field') in ['m_Sprite','objectReference']):problems.append('New sprite not referenced by requested visual changes')
 errors += [a['path']+': '+e for e in problems]
 asset_reports.append({'path':a['path'],'result':'PASS' if not problems else 'FAIL','guid':a['guid'],'fileID':a['fileID'],'sourceSize':size,'spriteRect':a['rect'],'border':a['border'],'maxTextureSize':a['cap'],'spriteMode':a['mode'],'pngPixelsIdenticalToGeneratedSource':sha(p)==sha(a['original']),'sha256':sha(p),'alphaAndBilinearClamp':True,'mipmaps':False,'readWrite':False,'issues':problems})
# Referenced new GUIDs are unique across the currently installed art metas.
meta_guids={}
for p in (root/'Assets/OrchardUI').rglob('*.meta'):
 m=re.search(r'^guid: (\w+)$',read(p),re.M)
 if m:meta_guids.setdefault(m[1],[]).append(p.relative_to(root).as_posix())
for a in newassets:
 if len(meta_guids.get(a['guid'],[]))!=1:errors.append('Missing or duplicate new asset GUID '+a['guid'])
legacy_guid='1497b04c3b9879042bab62ffb8dafc41'
legacy_id='1041074903250054671'
legacy_before=blocks(read(out/'Before'/widget_rel))[legacy_id]
legacy_now=widget[legacy_id]
legacy_search=subprocess.run(['rg','-l','--fixed-strings',legacy_guid,str(root/'Assets'),'--glob','*.meta'],capture_output=True,text=True)
known_findings=[{'kind':'Pre-existing missing legacy Animation clip reference','prefab':widget_rel,'componentFileID':legacy_id,'missingClipGuid':legacy_guid,'metadataSearchExitCode':legacy_search.returncode,'metadataMatches':legacy_search.stdout.splitlines(),'componentExactlyMatchesBaseline':legacy_before==legacy_now,'status':'Baseline issue preserved; no Animation fields or initialization behavior changed in this visual pass.'}]
gift_checks=[]
for fid,key,want in [('4234855247401888128','m_Sprite','{fileID: 21300000, guid: 874d776a383c46d2b2f3de4017fd243a, type: 3}'),('4234855247401888128','m_Color','{r: 1, g: 1, b: 1, a: 1}'),('2314400796718388841','m_Color','{r: 1, g: 1, b: 1, a: 0}'),('3586814762122687189','scaleTarget','{fileID: 7343093305673801710}'),('3586814762122687189','pressScaleRatio','0.92')]:
 got=direct(widget,fid,key);gift_checks.append({'target':fid,'field':key,'actual':got,'expected':want,'pass':got==want})
if any(not c['pass'] for c in gift_checks):errors.append('Final gift presentation/press-feedback synchronization mismatch')
report={'result':'PASS' if not errors else 'FAIL','scope':'Independent serialized-asset audit of approved natural wood HUD / gift entry / tray lock implementation; normal Unity runtime QA is separate.','prefabCount':len(files),'modifiedPrefabCount':sum(bool(x['visualChanges']) for x in files),'guardPrefabCount':sum(x['guardOnly'] for x in files),'serializedObjectCount':sum(x['serializedObjects'] for x in files),'rectTransformCount':sum(x['rectTransforms'] for x in files),'visualFieldEdits':len(changes),'logs':log_names,'buttonIntegrity':{'result':'PASS' if all(x['allFieldsUnchangedExceptDocumentedVisualReference'] for x in button_checks) else 'FAIL','checks':button_checks,'visualFeedbackException':allow.get('buttonVisualFeedbackException')},'prefabs':files,'codeHashes':code_report,'existingArt':{'baselineFiles':len(artbase),'changed':artchanged,'result':'PASS' if not artchanged else 'FAIL'},'nestedCurrencyEffectiveValues':{'result':'PASS' if all(x['pass'] for x in checks) else 'FAIL','checks':checks},'newSprites':asset_reports,'giftPresentationAndPressFeedback':{'result':'PASS' if all(x['pass'] for x in gift_checks) else 'FAIL','checks':gift_checks},'knownBaselineFindings':known_findings,'issues':errors,'approvalReference':'Design/HudRedoPreview-20260918/02-ai-natural-wood-preview.png','limitations':['This is asset/configuration verification, not a Unity runtime screenshot or a gameplay execution test.','PPU/border appearance is subject to root runtime visual QA; rerun this audit after any logged tuning.']}
(out/'final-integrity.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'result':report['result'],'prefabs':report['prefabCount'],'changedPrefabs':report['modifiedPrefabCount'],'objects':report['serializedObjectCount'],'rectsUnchanged':report['rectTransformCount'],'visualEdits':len(changes),'codeCount':len(code),'newSprites':len(newassets),'issues':errors},ensure_ascii=False))
