from pathlib import Path
from PIL import Image
import re,json,uuid,shutil,hashlib
here=Path(__file__).resolve().parent
project=here.parents[1]
art=here/'Art'
sources=json.loads((art/'control-sources.json').read_text())
generated=Path('C:/Users/pc/.codex/generated_images/01a0af0e-fa04-75a3-8fd6-557bcd874b3f')
for name,filename in sources.items():
    shutil.copy2(generated/filename,art/(name+'.png'))
template=(project/'Assets/OrchardUI/Art/HudGift.png.meta').read_text(encoding='utf-8-sig')
specs={
'SlotEmeraldMachine':(2048,(0,0,0,0),False),
'SlotEmeraldHelpBody':(2048,(0,0,0,0),False),
'SlotEmeraldMarquee':(1024,(0,0,0,0),True),
'SlotEmeraldButton':(1024,(250,95,250,95),True),
'SlotEmeraldBack':(256,(0,0,0,0),True),
'SlotEmeraldHelp':(256,(0,0,0,0),True),
'SlotEmeraldCheck':(256,(0,0,0,0),True),
'SlotIvoryPanel':(1024,(130,130,130,130),True),
}
records={}
for name,(maxsize,border,trim) in specs.items():
    src=art/(name+'.png')
    dest=project/'Assets/OrchardUI/Art'/(name+'.png')
    assert not dest.exists(),dest
    im=Image.open(src)
    assert im.mode=='RGBA' and im.getchannel('A').getextrema()==(0,255),(name,im.mode)
    bounds=im.getchannel('A').point(lambda a:255 if a>24 else 0).getbbox()
    if trim:
        l,t,r,b=bounds
        l=max(0,l-4);t=max(0,t-4);r=min(im.width,r+4);b=min(im.height,b+4)
    else: l,t,r,b=0,0,im.width,im.height
    rect=(l,im.height-b,r-l,b-t)
    guid=uuid.uuid4().hex
    data=re.sub(r'^guid: \w+','guid: '+guid,template,flags=re.M)
    data=re.sub(r'^    spriteID: \w+','    spriteID: '+uuid.uuid4().hex,data,flags=re.M)
    data=re.sub(r'(maxTextureSize: )\d+',lambda m:m[1]+str(maxsize),data)
    data=data.replace('  spriteMode: 1','  spriteMode: 2')
    sprite=f'''    sprites:
    - serializedVersion: 2
      name: {name}
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
    data=data.replace('    nameFileIdTable: {}','    nameFileIdTable:\n      '+name+': 21300000')
    assert 'spriteMode: 2' in data and f'name: {name}' in data
    Path(str(dest)+'.meta').write_text(data,encoding='utf-8',newline='\n')
    shutil.copy2(src,dest)
    records[name]={'path':dest.relative_to(project).as_posix(),'guid':guid,'fileID':21300000,'rect':rect,'border':border,'sourceSize':im.size,'alphaBounds':bounds,'pngPixelsUnmodified':True,'sha256':hashlib.sha256(src.read_bytes()).hexdigest(),'source':str(src),'maxTextureSize':maxsize}
(here/'Art/import-info.json').write_text(json.dumps(records,indent=2),encoding='utf-8')
print(json.dumps(records,indent=2))

