from pathlib import Path
import re, json, hashlib
out=Path(__file__).resolve().parent
project=out.parents[1]
counter='{fileID: 21300000, guid: dbf870bbe4a14284b631a4cd409d5147, type: 3}'
green='{fileID: 21300000, guid: d32dd97f0ef44177bd56a2985c098c92, type: 3}'
fill='{fileID: 21300000, guid: 2b6139ace56a4deda57bf471391ee675, type: 3}'
body='{fileID: 2100000, guid: 486d0a25be512df4887a3bf53db492b8, type: 2}'
blue='{r: 0.09019608, g: 0.30980393, b: 0.49019608, a: 1}'
records=[]
files={}
def load(rel):
 p=project/rel; data=p.read_bytes(); assert data==(out/'Before'/rel).read_bytes(), 'Concurrent modification: '+rel
 files[rel]=[data, data.decode('utf-8-sig').replace('\r\n','\n')]
def field(rel,fid,key,value):
 text=files[rel][1]
 pattern=rf'(?ms)(^--- !u!\d+ &{fid}\n(?:(?!^--- !u!).)*?^  {re.escape(key)}: )([^\n]*)'
 m=re.search(pattern,text); assert m,(fid,key)
 value=str(value)
 if m[2]==value:return
 records.append({'file':rel,'fileID':str(fid),'propertyPath':key,'before':m[2],'after':value})
 files[rel][1]=text[:m.start(2)]+value+text[m.end(2):]
panel='Assets/BizzaWZ/Final/Real/UI/FakeWithdrawPanel/FakeWithdrawPanel.prefab'
item='Assets/BizzaWZ/Final/Real/UI/FakeWithdrawPanel/WithdrawAmountItem.prefab'
load(panel);load(item)
# Existing Image nodes only; preserve their original hierarchy and positions.
for fid,sprite,ppu in [(2754535437279419372,counter,2),(6615170243285179318,counter,2.5),
                       (1241199525063855708,green,3),(8743834095267926959,counter,4)]:
 field(panel,fid,'m_Sprite',sprite);field(panel,fid,'m_Type',1);field(panel,fid,'m_PixelsPerUnitMultiplier',ppu)
field(panel,4192234944055056232,'m_PixelsPerUnitMultiplier',3)
field(panel,6004225462979585317,'m_sharedMaterial',body)
field(panel,6004225462979585317,'m_fontColor',blue)
for key in ('m_fontSize','m_fontSizeBase','m_fontSizeMax'):field(panel,6004225462979585317,key,60)
field(panel,8767989740261440782,'m_fontColor','{r: 0.12941177, g: 0.4509804, b: 0.25882354, a: 1}')
for key in ('m_fontSize','m_fontSizeBase','m_fontSizeMax'):field(panel,8767989740261440782,key,64)
for key in ('m_fontSize','m_fontSizeBase','m_fontSizeMax'):field(panel,8654869823511539002,key,58)
# Preserve Image.Filled, Horizontal, Left, serialized amount and all runtime bindings.
field(panel,8175018248033638516,'m_SizeDelta','{x: 897, y: 48}')
field(panel,2071485895475148001,'m_SizeDelta','{x: 865, y: 36}')
field(panel,6620554689692068040,'m_Sprite',fill)
field(panel,6620554689692068040,'m_PreserveAspect',1)
field(panel,3339610134613169318,'m_sharedMaterial',body)
field(panel,3339610134613169318,'m_fontColor',blue)
for key in ('m_fontSize','m_fontSizeBase','m_fontSizeMax'):field(panel,3339610134613169318,key,30)
# Constrain this existing heading to the same content span, without moving its left edge.
field(panel,210600630110459938,'m_AnchoredPosition','{x: 551.2, y: -37.795}')
field(panel,210600630110459938,'m_SizeDelta','{x: 890, y: 50}')
field(item,3062039195826043058,'m_Sprite',counter)
field(item,3062039195826043058,'m_PixelsPerUnitMultiplier',4)
for fid in (5455967628476765425,1584464498381216905):field(item,fid,'m_PixelsPerUnitMultiplier',2.5)
for key in ('m_fontSize','m_fontSizeBase','m_fontSizeMax'):field(item,2196861301424074241,key,52)
field(item,1775683569243512909,'m_SizeDelta','{x: 60, y: 60}')
field(item,1775683569243512909,'m_AnchoredPosition','{x: -33, y: 33}')
# Explicit existing nested overrides also need to agree with the source card prefab.
instances=['735769260790555943','921752336444412338','3145375543058054682','3366821845968657574','6389070758414889696','9024996893891000631']
guid='5fefc70f9b2f3f1488b2b221199dcf24'
overrides=[('3062039195826043058','m_Sprite','',counter),('3062039195826043058','m_PixelsPerUnitMultiplier','4','{fileID: 0}'),
           ('5455967628476765425','m_PixelsPerUnitMultiplier','2.5','{fileID: 0}'),('1584464498381216905','m_PixelsPerUnitMultiplier','2.5','{fileID: 0}')]
for key in ('m_fontSize','m_fontSizeBase','m_fontSizeMax'):overrides.append(('2196861301424074241',key,'52','{fileID: 0}'))
for instance in instances:
 text=files[panel][1];m=re.search(rf'(?ms)^--- !u!1001 &{instance}\n.*?(?=^--- !u!|\Z)',text);assert m
 block=m[0]
 for fid,key,value,obj in overrides:
  start=f'    - target: {{fileID: {fid}, guid: {guid}, type: 3}}\n      propertyPath: {key}\n'
  expr=re.escape(start)+r'      value: ([^\n]*)\n      objectReference: ([^\n]*)\n'
  old=re.search(expr,block)
  replacement=start+f'      value: {value}\n      objectReference: {obj}\n'
  if old and old[0]==replacement:continue
  records.append({'file':panel,'instance':instance,'fileID':fid,'propertyPath':key,'before':old[0] if old else 'inherited','after':replacement})
  if old:block=block[:old.start()]+replacement+block[old.end():]
  else:block=block.replace('    m_RemovedComponents:',replacement+'    m_RemovedComponents:',1)
 files[panel][1]=text[:m.start()]+block+text[m.end():]
hashes=[]
for rel,(original,text) in files.items():
 nl='\r\n' if b'\r\n' in original else '\n'
 data=text.replace('\n',nl).encode('utf-8')
 if original.startswith(b'\xef\xbb\xbf'):data=b'\xef\xbb\xbf'+data
 (project/rel).write_bytes(data)
 hashes.append({'path':rel,'before':hashlib.sha256(original).hexdigest(),'after':hashlib.sha256(data).hexdigest()})
(out/'applied-changes.json').write_text(json.dumps({'scope':'Visual-only two prefabs. No changed text values, callbacks, data, tasks or currencies.','hashes':hashes,'changes':records},ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'prefabs':len(files),'visualChanges':len(records)}))
