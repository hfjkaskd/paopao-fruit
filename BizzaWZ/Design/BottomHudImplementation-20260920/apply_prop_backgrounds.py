"""One-off minimal production patch, run only after the approved backgrounds exist.

Does not modify any prop/lock/play glyph, RectTransform, state, or binding.
"""
from pathlib import Path
import re, json, hashlib, shutil

ROOT=Path(__file__).resolve().parents[2]
OUT=Path(__file__).resolve().parent
PREFAB=ROOT/'Assets/FruitsHarvest/Resources/Original/res/local/coreplay/CorePlayUI.prefab'
FRAME='{fileID: 21300000, guid: 8fb700bae53a45a1aec44c9fb828a9a8, type: 3}'
FACE='{fileID: 21300000, guid: 65c699fa67ab48d9b81323f99c572dc8, type: 3}'
LOCKED='{fileID: 21300000, guid: 5efc5492c4d249f4b6e89a326e5c2276, type: 3}'
BODY='{fileID: 2100000, guid: 4e90e2c922a361241b3fbce18c873684, type: 2}'
NAVY='{r: 0.09, g: 0.32, b: 0.43, a: 1}'
NAVY32=23 | (82 << 8) | (110 << 16) | (255 << 24)

IDS={
 'Undo': {'frame':'114410865556287939','face':'114282982324125657','countBg':'114504490712063035','countLabel':'114706308911820470'},
 'Magic': {'frame':'114513653223693148','face':'114837889324699812','countBg':'114798730798708461','countLabel':'114517549520547945'},
 'Shuffle': {'frame':'114837227276182950','face':'114926580033004315','countBg':'114886820396282420','countLabel':'114317682953123289'},
}

def split(s):return {m[2]:(m[1],m[3]) for m in re.finditer(r'--- !u!(\d+) &(\d+)\n(.*?)(?=--- !u!|\Z)',s,re.S)}
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()

def main():
 raw=PREFAB.read_bytes(); old=raw.decode('utf-8-sig').replace('\r\n','\n'); new=old
 before=OUT/'Before'/'Assets/FruitsHarvest/Resources/Original/res/local/coreplay/CorePlayUI.prefab'
 if before.exists():raise RuntimeError('Before snapshot already exists: avoid reapplying')
 approved=OUT/'background-assets-ready.json'
 if not approved.exists():raise RuntimeError('Missing background-assets-ready.json from art owner')
 config=json.loads(approved.read_text(encoding='utf-8-sig'))
 for name,expected in config['sha256'].items():
  assert sha(ROOT/name)==expected, 'Art readiness hash differs: '+name
 glyphdir=ROOT/'Assets/FruitsHarvest/Resources/Original/res/local/coreplay/sprite/item'
 glyphs={str(p.relative_to(ROOT)):sha(p) for p in glyphdir.iterdir() if p.name.startswith(('Undo_','Magic_','Shuffle_','Item_Lock.','Play.'))}
 for baseline in json.loads((OUT/'prop-glyph-baseline.json').read_text(encoding='utf-8-sig')):
  assert sha(ROOT/baseline['path'])==baseline['sha256'], 'Prop glyph differs from pre-implementation baseline: '+baseline['path']
 catalog=ROOT/'Assets/FruitsHarvest/Resources/HarvestPaths.txt'; cataloghash=sha(catalog)
 changes=[]
 def change(cid, fields, color32=False):
  nonlocal new
  pat=r'(--- !u!114 &'+cid+r'\n)(.*?)(?=--- !u!|\Z)';m=re.search(pat,new,re.S);assert m
  body=m[2]
  for k,v in fields.items():
   body,n=re.subn(r'^  '+re.escape(k)+r':[^\n]*$', '  '+k+': '+str(v),body,flags=re.M);assert n==1,(cid,k,n)
  if color32:
   body,n=re.subn(r'(  m_fontColor32:\n    serializedVersion: 2\n    rgba:) \d+',r'\g<1> '+str(NAVY32),body);assert n==1
  if body==m[2]:return
  new=new[:m.start(2)]+body+new[m.end(2):]
  changes.append({'id':cid,'fields':list(fields)+(['m_fontColor32'] if color32 else [])})
 for owner,ids in IDS.items():
  change(ids['frame'],{'m_Sprite':FRAME})
  change(ids['face'],{'m_Sprite':FACE})
  change(ids['countBg'],{'m_Sprite':LOCKED,'m_Type':1,'m_PixelsPerUnitMultiplier':config.get('countPpuMultiplier',3)})
  change(ids['countLabel'],{'m_sharedMaterial':BODY,'m_fontColor':NAVY},True)
 b,a=split(old),split(new)
 assert b.keys()==a.keys()
 changed={i for i in b if b[i]!=a[i]}; assert changed=={c['id'] for c in changes}
 assert all(b[i]==a[i] for i in b if b[i][0]!='114')
 assert all(sha(ROOT/p)==h for p,h in glyphs.items())
 assert sha(catalog)==cataloghash
 before.parent.mkdir(parents=True,exist_ok=True);before.write_bytes(raw)
 newline='\r\n' if b'\r\n' in raw else '\n';out=new.replace('\n',newline).encode('utf-8')
 if raw.startswith(b'\xef\xbb\xbf'):out=b'\xef\xbb\xbf'+out
 PREFAB.write_bytes(out)
 report={'scope':'Undo/Magic/Shuffle backgrounds and existing count label contrast only',
 'beforeSha256':hashlib.sha256(raw).hexdigest(),'afterSha256':sha(PREFAB),
 'componentCountBefore':len(b),'componentCountAfter':len(a),'changes':changes,
 'checks':{'allGlyphAssetsUnchanged':True,'catalogUnchanged':True,'allRectTransformsUnchanged':True,'allButtonsAndStateComponentsUnchanged':True,'allAdAndLockNodesUnchanged':True},'glyphSha256':glyphs}
 (OUT/'prop-background-patch-report.json').write_text(json.dumps(report,indent=2))
 print(json.dumps({'changedComponentCount':len(changed),'checks':report['checks']}))

if __name__=='__main__':main()
