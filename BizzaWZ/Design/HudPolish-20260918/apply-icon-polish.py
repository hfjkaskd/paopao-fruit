from pathlib import Path
import re, shutil, uuid, json, hashlib

here=Path(__file__).resolve().parent
project=here.parents[1]
changes=[]
atlas='50c5208c19d79f44ebd86c010248d125'
template=(project/'Assets/OrchardUI/Resources/OrchardUI/PropButtonNormal.png.meta').read_text(encoding='utf-8-sig')
assets={}
for name in ('Lucky777','Gift'):
    source=here/'Art'/(name+'.png')
    dest=project/'Assets/OrchardUI/Art'/('Hud'+name+'.png')
    meta=Path(str(dest)+'.meta')
    guid=re.search(r'^guid: (\w+)',meta.read_text(),re.M)[1] if meta.exists() else uuid.uuid4().hex
    shutil.copy2(source,dest)
    data=re.sub(r'^guid: \w+', 'guid: '+guid,template,flags=re.M)
    data=re.sub(r'^    spriteID: \w+', '    spriteID: '+uuid.uuid4().hex,data,flags=re.M)
    if name=='Lucky777':
        # Import the nonempty region, retaining the generated PNG/alpha untouched.
        # A small border surrounds alpha > 1 bounds (48,162)-(1220,1064).
        data=data.replace('  spriteMode: 1','  spriteMode: 2')
        sprite=f'''    sprites:
    - serializedVersion: 2
      name: HudLucky777
      rect:
        serializedVersion: 2
        x: 40
        y: 182
        width: 1188
        height: 918
      alignment: 0
      pivot: {{x: 0.5, y: 0.5}}
      border: {{x: 0, y: 0, z: 0, w: 0}}
      outline: []
      physicsShape: []
      tessellationDetail: 0
      bones: []
      spriteID: {uuid.uuid4().hex}
      internalID: 21300000
      vertices: []
      indices: 
      edges: []
      weights: []'''
        data=data.replace('    sprites: []',sprite)
        data=data.replace('    nameFileIdTable: {}','    nameFileIdTable:\n      HudLucky777: 21300000')
    meta.write_text(data,encoding='utf-8',newline='\n')
    assets[name]={'path':dest.relative_to(project).as_posix(),'guid':guid,'fileID':21300000,'maxTextureSize':512,'sourceSha256':hashlib.sha256(source.read_bytes()).hexdigest(),'pngPixelsUnmodified':True}

def edit(rel,edits,overrides=()):
    source=project/rel
    backup=here/'Before'/rel
    backup.parent.mkdir(parents=True,exist_ok=True)
    if not backup.exists():shutil.copy2(source,backup)
    text=source.read_text(encoding='utf-8-sig')
    for fid,key,value in edits:
        pattern=rf'(^--- !u!\d+ &{fid}\n(?:(?!^--- !u!).)*?^  {re.escape(key)}: )([^\n]*)'
        def replace(m):
            changes.append({'file':rel,'fileID':str(fid),'field':key,'before':m[2],'after':value})
            return m[1]+value
        text,n=re.subn(pattern,replace,text,flags=re.M|re.S)
        assert n==1,(fid,key,n)
    for fid,key,kind,value in overrides:
        pattern=rf'(    - target: \{{fileID: {fid},[^\n]+\}}\n      propertyPath: {re.escape(key)}\n)(      value: [^\n]*\n      objectReference: [^\n]*)'
        def replace(m):
            before=re.search(rf'^      {kind}: (.*)$',m[2],re.M)[1]
            after=re.sub(rf'^      {kind}: .*$', '      '+kind+': '+value,m[2],flags=re.M)
            changes.append({'file':rel,'fileID':str(fid),'override':key,'field':kind,'before':before,'after':value})
            return m[1]+after
        text,n=re.subn(pattern,replace,text,flags=re.M)
        assert n==1,(fid,key,n)
    source.write_text(text,encoding='utf-8',newline='\n')

sprite=lambda fid:'{fileID: '+str(fid)+', guid: '+atlas+', type: 3}'
new_sprite=lambda name:'{fileID: 21300000, guid: '+assets[name]['guid']+', type: 3}'
edit('Assets/BizzaWZ/Final/Real/UI/SlotsPanel/SlotEnter/SlotEnter.prefab',[
    (5403240756379141583,'m_Sprite',new_sprite('Lucky777')),
    (5403240756379141583,'m_PreserveAspect','1'),
])
edit('Assets/BizzaWZ/Final/Real/UI/GamePanel/Resources/Resources/GameUiWidget.prefab',[
    (2314400796718388841,'m_Sprite',new_sprite('Gift')),
    (2314400796718388841,'m_PreserveAspect','1'),
    (4234855247401888128,'m_Sprite',sprite(-377845897)),
],[
    (5821051965148166099,'m_Sprite','objectReference',sprite(-1757003328)),
    (5860728120506579172,'m_sharedMaterial','objectReference','{fileID: 2100000, guid: 486d0a25be512df4887a3bf53db492b8, type: 2}'),
    (5860728120506579172,'m_fontColor.r','value','0.09019608'),
    (5860728120506579172,'m_fontColor.g','value','0.30980393'),
    (5860728120506579172,'m_fontColor.b','value','0.49019608'),
])
(here/'icon-changes.json').write_text(json.dumps({'assets':assets,'changes':changes},ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'newAssets':assets,'visualFields':len(changes)},indent=2))
