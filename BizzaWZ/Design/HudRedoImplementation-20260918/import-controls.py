from pathlib import Path
from PIL import Image
import re, json, uuid, shutil, hashlib

here = Path(__file__).resolve().parent
project = here.parents[1]
source_dir = Path('C:/Users/pc/.codex/generated_images/01a0af0e-fa04-75a3-8fd6-557bcd874b3f')
specs = {
 'WoodTile': ('exec-b238c379-1ce5-4b2b-8400-e46d8baa7c15.png', (190,190,190,190)),
 'Counter': ('exec-a0cb54d6-5f9f-4096-b953-bdf790e54aaf.png', (155,95,155,95)),
 'Settings': ('exec-ca0ede39-4596-4ac6-a129-eeda3db67874.png', (0,0,0,0)),
 'GreenButton': ('exec-65e15d67-c70b-4ee3-855c-5bd0dfcc6a22.png', (220,110,220,110)),
}
template = (project/'Assets/OrchardUI/Art/HudGift.png.meta').read_text(encoding='utf-8-sig')
records = {}
for name,(filename,border) in specs.items():
    src=source_dir/filename
    dest=project/'Assets/OrchardUI/Art'/('HudNatural'+name+'.png')
    art=here/'Art'/name
    art.mkdir(parents=True,exist_ok=True)
    assert not dest.exists(), dest
    shutil.copy2(src,dest)
    shutil.copy2(src,art/'original.png')
    im=Image.open(src)
    assert im.mode=='RGBA'
    bounds=im.getchannel('A').point(lambda a:255 if a>8 else 0).getbbox()
    x1,y1,x2,y2=bounds
    x1=max(0,x1-6); y1=max(0,y1-6); x2=min(im.width,x2+6); y2=min(im.height,y2+6)
    rect=(x1,im.height-y2,x2-x1,y2-y1)
    guid=uuid.uuid4().hex
    data=re.sub(r'^guid: \w+','guid: '+guid,template,flags=re.M)
    data=re.sub(r'^    spriteID: \w+','    spriteID: '+uuid.uuid4().hex,data,flags=re.M)
    data=data.replace('  spriteMode: 1','  spriteMode: 2')
    sprite_name='HudNatural'+name
    sprite=f'''    sprites:
    - serializedVersion: 2
      name: {sprite_name}
      rect:
        serializedVersion: 2
        x: {rect[0]}
        y: {rect[1]}
        width: {rect[2]}
        height: {rect[3]}
      alignment: 0
      pivot: {{x: 0.5, y: 0.5}}
      border: {{x: {border[0]}, y: {border[1]}, z: {border[2]}, w: {border[3]}}}
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
    data=data.replace('    nameFileIdTable: {}','    nameFileIdTable:\n      '+sprite_name+': 21300000')
    Path(str(dest)+'.meta').write_text(data,encoding='utf-8',newline='\n')
    records[name]={'path':dest.relative_to(project).as_posix(),'guid':guid,'fileID':21300000,'rect':rect,'border':border,'sourceSize':im.size,'alphaBounds':bounds,'pngPixelsUnmodified':True,'sha256':hashlib.sha256(src.read_bytes()).hexdigest(),'source':str(src),'maxTextureSize':512}
(here/'Art/control-import-info.json').write_text(json.dumps(records,indent=2),encoding='utf-8')
print(json.dumps(records,indent=2))
