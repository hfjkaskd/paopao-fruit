from pathlib import Path
import hashlib,json,re,shutil,uuid

OUT=Path(__file__).resolve().parent
ROOT=OUT.parents[1]
DIR=ROOT/'Assets/OrchardUI/Resources/OrchardUI'
# Alpha bounds supplied by the art owner in top-left pixel coordinates.
BOUNDS={'PropButtonFrame':(38,44,1213,1200),'PropButtonNormal':(50,59,1204,1196),'PropButtonLocked':(79,102,1175,1152)}
report={}; hashlist={}
for name,(x0,y0,x1,y1) in BOUNDS.items():
 source=OUT/'Generated'/(name+'.png');target=DIR/source.name;meta=Path(str(target)+'.meta')
 assert source.exists() and target.exists() and meta.exists()
 for p in (target,meta):
  before=OUT/'Before'/p.relative_to(ROOT)
  assert not before.exists(), 'Refusing to overwrite baseline '+str(before)
  before.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(p,before)
 s=meta.read_text(encoding='utf-8-sig')
 originalguid=re.search(r'^guid: (\w+)',s,re.M)[1]
 spriteid=re.search(r'^    spriteID: (\w+)',s,re.M)[1]
 x=x0-3;y=1254-(y1+3);width=x1-x0+6;height=y1-y0+6
 border=160 if name=='PropButtonLocked' else 0
 s=s.replace('  internalIDToNameTable: []',f'  internalIDToNameTable:\n  - first:\n      213: 21300000\n    second: {name}')
 s=re.sub(r'^  spriteMode: \d+$','  spriteMode: 2',s,flags=re.M)
 s=re.sub(r'^  spriteBorder: .*$',f'  spriteBorder: {{x: {border}, y: {border}, z: {border}, w: {border}}}',s,flags=re.M)
 s=re.sub(r'^    textureCompression: \d+$','    textureCompression: 0',s,flags=re.M)
 s=re.sub(r'^    textureFormat: \S+$','    textureFormat: -1',s,flags=re.M)
 s=re.sub(r'^    overridden: \d+$','    overridden: 0',s,flags=re.M)
 s=re.sub(r'^  spriteGenerateFallbackPhysicsShape: \d+$','  spriteGenerateFallbackPhysicsShape: 0',s,flags=re.M)
 entry=f'''    sprites:
    - serializedVersion: 2
      name: {name}
      rect:
        serializedVersion: 2
        x: {x}
        y: {y}
        width: {width}
        height: {height}
      alignment: 0
      pivot: {{x: 0.5, y: 0.5}}
      border: {{x: {border}, y: {border}, z: {border}, w: {border}}}
      outline: []
      physicsShape: []
      tessellationDetail: 0
      bones: []
      spriteID: {spriteid}
      internalID: 21300000
      vertices: []
      indices:
      edges: []
      weights: []'''
 assert '    sprites: []' in s
 s=s.replace('    sprites: []',entry)
 s=s.replace('    nameFileIdTable: {}',f'    nameFileIdTable:\n      {name}: 21300000')
 assert re.search(r'^guid: (\w+)',s,re.M)[1]==originalguid
 meta.write_text(s,encoding='utf-8',newline='\r\n')
 shutil.copyfile(source,target)
 assert source.read_bytes()==target.read_bytes(), 'Generated art must be copied losslessly'
 for p in (target,meta):hashlist[p.relative_to(ROOT).as_posix()]=hashlib.sha256(p.read_bytes()).hexdigest()
 report[name]={'path':target.relative_to(ROOT).as_posix(),'guid':originalguid,'fileID':21300000,'spriteRect':{'x':x,'y':y,'width':width,'height':height},'border':border,'maxTextureSize':512,'noPixelEdits':True}
(OUT/'prop-background-import-report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
(OUT/'background-assets-ready.json').write_text(json.dumps({'sha256':hashlist,'countPpuMultiplier':6},indent=2),encoding='utf-8')
print(json.dumps(report,indent=2))
