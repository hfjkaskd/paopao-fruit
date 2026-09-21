from pathlib import Path
import re, shutil, json, hashlib

here=Path(__file__).resolve().parent
project=here.parents[1]
rel=Path('Assets/FruitsHarvest/Resources/Original/res/local/coreplay/CorePlayUI.prefab')
src=project/rel
backup=here/'Before'/rel
backup.parent.mkdir(parents=True, exist_ok=True)
if not backup.exists(): shutil.copy2(src,backup)
text=src.read_text(encoding='utf-8-sig')
changes=[]
def field(fid,key,value):
    global text
    pattern=rf'(^--- !u!\d+ &{fid}\n(?:(?!^--- !u!).)*?^  {re.escape(key)}: )([^\n]*)'
    def change(m):
        changes.append({'file':rel.as_posix(),'fileID':str(fid),'field':key,'before':m[2],'after':value})
        return m[1]+value
    text,n=re.subn(pattern,change,text,flags=re.M|re.S)
    assert n==1,(fid,key,n)

# The existing Button still targets this same Image. A sliced card preserves
# its corner proportions inside the authored 162 x 172 rectangle.
field(114942066236133985,'m_Sprite','{fileID: -1757003328, guid: 50c5208c19d79f44ebd86c010248d125, type: 3}')
field(114942066236133985,'m_Type','1')
field(114942066236133985,'m_PixelsPerUnitMultiplier','2')
# Reuse the metal lock already used by the adjacent prop buttons.
field(114703169052324433,'m_Sprite','{fileID: 21300000, guid: d50b72d34a3861a4bb998a4330504119, type: 3}')
field(114703169052324433,'m_PreserveAspect','1')
field(224856321325849650,'m_AnchoredPosition','{x: 0, y: 0}')
with src.open('w',encoding='utf-8',newline='\n') as f:f.write(text)
(here/'lock-changes.json').write_text(json.dumps(changes,ensure_ascii=False,indent=2),encoding='utf-8')
cs_hashes={str(p.relative_to(project)).replace('\\','/'):hashlib.sha256(p.read_bytes()).hexdigest() for p in (project/'Assets').rglob('*.cs')}
(here/'code-hashes.json').write_text(json.dumps(cs_hashes,indent=2),encoding='utf-8')
print(json.dumps({'visualFields':len(changes),'codeFilesBaselined':len(cs_hashes)}))
