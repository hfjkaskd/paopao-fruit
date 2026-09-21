"""Author the approved slot help composition using existing Prefab objects only."""
from pathlib import Path
from PIL import Image
import re, json, uuid, hashlib, shutil

HERE = Path(__file__).resolve().parent
PROJECT = HERE.parents[1]
REL = 'Assets/BizzaWZ/Final/Real/UI/SlotsPanel/SlotFQAPanel/SlotFQAPanel.prefab'
path = PROJECT / REL
backup = HERE / 'Before' / REL
backup.parent.mkdir(parents=True, exist_ok=True)
if not backup.exists(): shutil.copy2(path, backup)

src = HERE / 'Art/SlotHelpCabinetComplete.png'
dest = PROJECT / 'Assets/OrchardUI/Art/SlotHelpCabinetComplete.png'
im = Image.open(src)
assert im.mode == 'RGBA' and im.getchannel('A').getextrema() == (0, 255)
bounds = im.getchannel('A').point(lambda a:255 if a>24 else 0).getbbox()
l,t,r,b = bounds
l=max(0,l-4); t=max(0,t-4); r=min(im.width,r+4); b=min(im.height,b+4)
rect = [l, im.height-b, r-l, b-t]
meta_path = Path(str(dest)+'.meta')
if meta_path.exists():
    old_meta = meta_path.read_text(encoding='utf-8-sig')
    guid = re.search(r'^guid: (\w+)',old_meta,re.M)[1]
else:
    guid = uuid.uuid4().hex
meta = (PROJECT/'Assets/OrchardUI/Art/SlotEmeraldMachine.png.meta').read_text(encoding='utf-8-sig')
meta = re.sub(r'^guid: \w+', 'guid: '+guid, meta, flags=re.M)
meta = meta.replace('SlotEmeraldMachine','SlotHelpCabinetComplete')
meta = re.sub(r'(spriteID: )\w+',lambda m:m[1]+uuid.uuid5(uuid.NAMESPACE_URL,guid+m[0]).hex,meta)
replacement = ('      rect:\n        serializedVersion: 2\n'+
               ''.join(f'        {k}: {v}\n' for k,v in zip(['x','y','width','height'],rect))).rstrip()
meta,n = re.subn(r'      rect:\n        serializedVersion: 2\n        x: [0-9.]+\n        y: [0-9.]+\n        width: [0-9.]+\n        height: [0-9.]+',replacement,meta)
assert n==1
# The existing table Image must still occlude the nested machine artwork.
# A second Sprite crops the exact same texture region at the exact same scale,
# preserving the integrated board without painting a mismatched second frame.
table_crop=[108,im.height-391-758,720,758]
entry=re.search(r'(^    - serializedVersion: 2\n      name: SlotHelpCabinetComplete\n.*?)(?=^    outline:)',meta,re.M|re.S)[1]
assert '      internalID: 21300000' in entry
overlay=entry.replace('SlotHelpCabinetComplete','SlotHelpTableOverlay').replace('internalID: 21300000','internalID: 21300002')
overlay=re.sub(r'(spriteID: )\w+',lambda m:m[1]+uuid.uuid5(uuid.NAMESPACE_URL,guid+'table-overlay').hex,overlay)
overlay=re.sub(r'      rect:\n        serializedVersion: 2\n        x: [0-9.]+\n        y: [0-9.]+\n        width: [0-9.]+\n        height: [0-9.]+',
    ('      rect:\n        serializedVersion: 2\n'+''.join(f'        {k}: {value}\n' for k,value in zip(['x','y','width','height'],table_crop))).rstrip(),overlay)
meta=meta.replace(entry,entry+overlay,1)
meta=meta.replace('      SlotHelpCabinetComplete: 21300000','      SlotHelpCabinetComplete: 21300000\n      SlotHelpTableOverlay: 21300002')
assert re.findall(r'^      internalID: (\d+)',meta,re.M)==['21300000','21300002']
meta_path.write_text(meta,encoding='utf-8',newline='\n')
shutil.copy2(src,dest)
(HERE/'Art/help-import.json').write_text(json.dumps({'guid':guid,'fileID':21300000,'source':str(src),'path':dest.relative_to(PROJECT).as_posix(),'rect':rect,'overlaySprite':{'name':'SlotHelpTableOverlay','fileID':21300002,'rect':table_crop},'border':[0,0,0,0],'maxTextureSize':2048,'sourceSize':im.size,'sha256':hashlib.sha256(src.read_bytes()).hexdigest(),'pixelEditing':False},indent=2)+'\n',encoding='utf-8')

text=backup.read_text(encoding='utf-8-sig')
rx=re.compile(r'^--- !u!(\d+) &(-?\d+)(?: stripped)?\n(.*?)(?=^--- !u!|\Z)',re.M|re.S)
blocks={m[2]:{'kind':m[1],'header':m[0][:m[0].find('\n')+1],'body':m[3]} for m in rx.finditer(text)}
changes=[]
def field(fid,key):
    m=re.search(r'^  '+re.escape(key)+r': (.*)$',blocks[str(fid)]['body'],re.M)
    assert m,(fid,key)
    return m[1]
def setf(fid,key,value):
    fid=str(fid); old=field(fid,key)
    if old==value:return
    blocks[fid]['body'],n=re.subn(r'^  '+re.escape(key)+r': .*$',lambda m:'  '+key+': '+value,blocks[fid]['body'],count=1,flags=re.M)
    assert n==1
    changes.append({'fileID':fid,'field':key,'before':old,'after':value})
def v(x,y): return '{x: '+format(x,'.6f').rstrip('0').rstrip('.')+', y: '+format(y,'.6f').rstrip('0').rstrip('.')+'}'
def vec(fid,key): return tuple(map(float,re.findall(r'[xy]: ([-0-9.eE]+)',field(fid,key))))
S=1920/2360
def screen_rect(fid,x,y,w,h):
    setf(fid,'m_AnchorMin',v(.5,.5));setf(fid,'m_AnchorMax',v(.5,.5));setf(fid,'m_Pivot',v(.5,.5))
    setf(fid,'m_AnchoredPosition',v((x+w/2-540)/S,(960-y-h/2)/S))
    setf(fid,'m_SizeDelta',v(w/S,h/S))

# Keep the content container, existing image objects, children and siblings.
setf(617195395320269764,'m_AnchoredPosition',v(0,0))
screen_rect(8805309752385714060,94,102,892,1541)
setf(5250022705326557845,'m_Sprite','{fileID: 21300000, guid: '+guid+', type: 3}')
setf(5250022705326557845,'m_Type','0')
setf(5250022705326557845,'m_PreserveAspect','0')
setf(5250022705326557845,'m_PixelsPerUnitMultiplier','1')

# Redraw the matching board region with the existing foreground table Image.
tx=94+(108-l)*892/(r-l)
ty=102+(391-t)*1541/(b-t)
tw=720*892/(r-l)
th=758*1541/(b-t)
screen_rect(7479695957089711254,tx,ty,tw,th)
setf(9132095855591360206,'m_Color','{r: 1, g: 1, b: 1, a: 1}')
setf(9132095855591360206,'m_Sprite','{fileID: 21300002, guid: '+guid+', type: 3}')
setf(9132095855591360206,'m_Type','0')
setf(9132095855591360206,'m_PreserveAspect','0')

# Remove the obsolete layered cabinet paint only on this page's nested instance.
inst=blocks['3151483872259529701']['body']
group='3e3b346423d816d47876dab1b13e409c'
pattern=r'(    - target: \{fileID: 4300416282917706685, guid: '+group+r', type: 3\}\n      propertyPath: m_Sprite\n      value: \n      objectReference: )[^\n]+'
inst,n=re.subn(pattern,lambda m:m[1]+'{fileID: 0}',inst)
assert n==1
alpha=('    - target: {fileID: 4300416282917706685, guid: '+group+', type: 3}\n'
       '      propertyPath: m_Color.a\n      value: 0\n      objectReference: {fileID: 0}\n')
assert 'propertyPath: m_Color.a' not in inst
inst=inst.replace('    m_RemovedComponents: []',alpha+'    m_RemovedComponents: []')
blocks['3151483872259529701']['body']=inst
changes.append({'fileID':'3151483872259529701','field':'nested Image paint','after':'m_Sprite=null; m_Color.a=0; source object, components, animation and activity unchanged'})

# Six original rows, same order, same dynamic text and reward sprites.
row_ids=re.findall(r'  - \{fileID: (\d+)\}',field('9055370915143298003','m_Children') if False else re.search(r'  m_Children:\n(.*?)  m_Father:',blocks['9055370915143298003']['body'],re.S)[1])
assert len(row_ids)==6
root='9055370915143298003'
setf(root,'m_AnchorMin',v(0,1));setf(root,'m_AnchorMax',v(0,1));setf(root,'m_Pivot',v(0,1))
setf(root,'m_AnchoredPosition',v((223-tx)/S,-(589-ty)/S))
setf(root,'m_SizeDelta',v(652/S,570/S))
setf(71365554334776158,'m_ChildForceExpandWidth','0')
setf(71365554334776158,'m_ChildForceExpandHeight','0')
setf(71365554334776158,'m_Spacing','0')

go_names={fid:re.search(r'^  m_Name: (.*)$',o['body'],re.M)[1] for fid,o in blocks.items() if o['kind']=='1'}
def go_of(fid):return re.search(r'^  m_GameObject: \{fileID: (\d+)\}',blocks[fid]['body'],re.M)[1]
def children(fid):return re.findall(r'  - \{fileID: (\d+)\}',re.search(r'  m_Children:\n(.*?)  m_Father:',blocks[fid]['body'],re.S)[1])
for row_i,row in enumerate(row_ids):
    setf(row,'m_SizeDelta',v(652/S,95/S))
    setf(row,'m_AnchorMin',v(0,1));setf(row,'m_AnchorMax',v(0,1));setf(row,'m_Pivot',v(0,1))
    setf(row,'m_AnchoredPosition',v(0,-row_i*95/S))
    for child in children(row):
        name=go_names[go_of(child)]
        if name in ('Slot_1','Slot_1 (1)','Slot_1 (2)'):
            ix=('Slot_1','Slot_1 (1)','Slot_1 (2)').index(name)
            cx=(272+96*ix-223)/S
            setf(child,'m_SizeDelta',v(84/S,84/S))
            for icon in children(child):
                if blocks[icon]['kind']!='224':continue
                iw,ih=vec(icon,'m_SizeDelta')
                setf(icon,'m_SizeDelta',v(iw,ih))
                setf(icon,'m_AnchoredPosition',v(0,0))
        elif name.startswith('que'):
            cx=(538-223)/S
            qw,qh=vec(child,'m_SizeDelta')
            setf(child,'m_SizeDelta',v(40/S,qh))
        elif name.startswith('des'):cx=(766-223)/S
        elif name.startswith('Image'):
            setf(child,'m_AnchorMin',v(0,.5));setf(child,'m_AnchorMax',v(0,.5));setf(child,'m_Pivot',v(.5,.5))
            setf(child,'m_AnchoredPosition',v((547-223)/S,-46/S))
            setf(child,'m_SizeDelta',v(640/S,1.5/S))
            continue
        elif name.startswith('Slot_1'):
            cx=(614-223)/S
            if row_i>=3:
                rw,rh=vec(child,'m_SizeDelta')
                smaller=(rw-4/S)/rw
                setf(child,'m_SizeDelta',v(rw*smaller,rh*smaller))
        else:raise ValueError((row,name))
        setf(child,'m_AnchorMin',v(0,.5));setf(child,'m_AnchorMax',v(0,.5));setf(child,'m_Pivot',v(.5,.5))
        setf(child,'m_AnchoredPosition',v(cx,0))

screen_rect(1133554623368840100,288,1267,505,188)
screen_rect(769373269381146989,319,1617,442,110)
setf(3677535174969407404,'m_AnchoredPosition',v(0,0))
setf(3677535174969407404,'m_SizeDelta',v(80/S,55/S))
setf(4019988058426453973,'m_Sprite','{fileID: 21300000, guid: e0f53d6d6f0b4f12a08073707ebe074b, type: 3}')
setf(4019988058426453973,'m_Type','0')
setf(4019988058426453973,'m_PreserveAspect','1')

out=text[:text.find('--- !u!')]+''.join(o['header']+o['body'] for o in blocks.values())
if path.read_text(encoding='utf-8-sig') != out:
    path.write_text(out,encoding='utf-8',newline='\n')
(HERE/'help-visual-changes.json').write_text(json.dumps(changes,indent=2)+'\n',encoding='utf-8')
(HERE/'help-layout-targets.json').write_text(json.dumps({'canvas':[1080,1920],'scaleFromCanvasReference':S,'cabinet':[94,102,892,1541],'tableContentContainer':[tx,ty,tw,th],'tableSpriteMatchesCabinetRegion':True,'rowRoot':[223,589,652,570],'tileSize':[84,84],'rowCenters':[[272,368,464,538,614,766,636.5+95*i] for i in range(6)],'footer':[288,1267,505,188],'confirmation':[319,1617,442,110],'hierarchyChanged':False,'newGameObjects':0},indent=2)+'\n',encoding='utf-8')
print(json.dumps({'prefab':REL,'visualChanges':len(changes),'sprite':guid,'rows':len(row_ids)},indent=2))
