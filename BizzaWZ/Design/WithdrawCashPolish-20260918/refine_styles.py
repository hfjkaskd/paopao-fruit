from pathlib import Path
import re,json,hashlib
out=Path(__file__).resolve().parent
project=out.parents[1]
panel='Assets/BizzaWZ/Final/Real/UI/FakeWithdrawPanel/FakeWithdrawPanel.prefab'
item='Assets/BizzaWZ/Final/Real/UI/FakeWithdrawPanel/WithdrawAmountItem.prefab'
counter='{fileID: 21300000, guid: dbf870bbe4a14284b631a4cd409d5147, type: 3}'
record=json.loads((out/'applied-changes.json').read_text(encoding='utf-8'))
changes=[]
def set_field(text,rel,fid,key,value):
 pattern=rf'(?ms)(^--- !u!\d+ &{fid}\n(?:(?!^--- !u!).)*?^  {re.escape(key)}: )([^\n]*)'
 m=re.search(pattern,text);assert m,(fid,key)
 changes.append({'file':rel,'fileID':str(fid),'propertyPath':key,'before':m[2],'after':str(value)})
 return text[:m.start(2)]+str(value)+text[m.end(2):]
files={rel:(project/rel).read_bytes() for rel in (panel,item)}
text=files[panel].decode('utf-8-sig').replace('\r\n','\n')
text=set_field(text,panel,8743834095267926959,'m_Sprite','{fileID: 1920971194, guid: 50c5208c19d79f44ebd86c010248d125, type: 3}')
text=set_field(text,panel,8743834095267926959,'m_PixelsPerUnitMultiplier',1.3)
guid='5fefc70f9b2f3f1488b2b221199dcf24'
pattern=rf'(?m)(    - target: \{{fileID: (5455967628476765425|1584464498381216905), guid: {guid}, type: 3\}}\n      propertyPath: m_Sprite\n      value: \n      objectReference: )([^\n]*)'
def sprite(m):
 changes.append({'file':panel,'fileID':m[2],'propertyPath':'m_Sprite','before':m[3],'after':counter,'nested':True})
 return m[1]+counter
text,n=re.subn(pattern,sprite,text);assert n==12
# These source card values are inherited; remove no overrides or objects.
pattern=rf'(?m)(    - target: \{{fileID: (5455967628476765425|1584464498381216905), guid: {guid}, type: 3\}}\n      propertyPath: m_PixelsPerUnitMultiplier\n      value: )2.5'
text,n=re.subn(pattern,lambda m:m[1]+'4',text);assert n==12
new={panel:text}
text=files[item].decode('utf-8-sig').replace('\r\n','\n')
for fid,color in [(5455967628476765425,'{r: 0.78, g: 1, b: 0.82, a: 1}'),(1584464498381216905,'{r: 0.8, g: 0.87, b: 0.82, a: 1}')]:
 text=set_field(text,item,fid,'m_Sprite',counter)
 text=set_field(text,item,fid,'m_PixelsPerUnitMultiplier',4)
 text=set_field(text,item,fid,'m_Color',color)
new[item]=text
for rel,text in new.items():
 raw=files[rel];nl='\r\n' if b'\r\n' in raw else '\n'
 data=text.replace('\n',nl).encode('utf-8')
 if raw.startswith(b'\xef\xbb\xbf'):data=b'\xef\xbb\xbf'+data
 (project/rel).write_bytes(data)
record['refinements']={'description':'Restore correctly rounded existing track; unify new-user and claimed card surfaces without changing status semantics.', 'changes':changes}
for entry in record['hashes']:entry['after']=hashlib.sha256((project/entry['path']).read_bytes()).hexdigest()
(out/'applied-changes.json').write_text(json.dumps(record,ensure_ascii=False,indent=2),encoding='utf-8')
print('Rounded track and matching status cards refined; source state tints remain inherited.')
