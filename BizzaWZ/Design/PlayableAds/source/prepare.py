import json, re, uuid, shutil, pathlib
from PIL import Image

ROOT = pathlib.Path(__file__).resolve().parents[3]
WORK = pathlib.Path(__file__).resolve().parent
PROJECT = WORK / 'Cocos'
SOURCE = ROOT / 'Assets/FruitsHarvest/Resources/Original'
ART = ROOT / 'Assets/OrchardUI/Art'
TEMPLATE = pathlib.Path('F:/Cocos/Creator/3.8.3/resources/templates/empty-2d')
PROJECT.mkdir(parents=True, exist_ok=True)
for name in ['settings', 'profiles', '.creator']:
    shutil.copytree(TEMPLATE/name, PROJECT/name, dirs_exist_ok=True)
shutil.copy(TEMPLATE/'tsconfig.json', PROJECT/'tsconfig.json')

def write(path, data):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(data, ensure_ascii=False, separators=(',', ':')), encoding='utf-8')

def uid(name): return str(uuid.uuid5(uuid.NAMESPACE_URL, 'orchard-trio-playable/'+name))
write(PROJECT/'package.json', {'name':'OrchardTrioPlayable','uuid':uid('project'),'creator':{'version':'3.8.3'}})
modules=['base','gfx-webgl','2d','ui','tween']
write(PROJECT/'settings/v2/packages/engine.json', {'__version__':'1.0.7','modules':{'includeModules':modules}})
write(PROJECT/'settings/v2/packages/project.json', {'__version__':'1.0.6','general':{'designResolution':{'width':1080,'height':1920,'fitWidth':False,'fitHeight':True}}})

assets={}
def picture(name, path=None, size=None, crop=None, jpeg=False, image=None):
    im = image.copy() if image is not None else Image.open(path)
    if crop: im=im.crop(crop)
    if size: im.thumbnail(size, Image.Resampling.LANCZOS)
    extension='.jpg' if jpeg else '.png'
    out=PROJECT/'assets/art'/f'{name}{extension}'
    out.parent.mkdir(parents=True,exist_ok=True)
    if jpeg: im.convert('RGB').save(out,quality=84,optimize=True)
    else: im.convert('RGBA').save(out,optimize=True)
    u=uid(name); w,h=im.size
    texture={'ver':'1.0.22','importer':'texture','imported':True,'uuid':u+'@6c48a','displayName':name,'id':'6c48a','name':'texture','files':['.json'],'subMetas':{},'userData':{'wrapModeS':'clamp-to-edge','wrapModeT':'clamp-to-edge','minfilter':'linear','magfilter':'linear','mipfilter':'none','anisotropy':0,'isUuid':True,'imageUuidOrDatabaseUri':u}}
    frame={'ver':'1.0.12','importer':'sprite-frame','imported':True,'uuid':u+'@f9941','displayName':name,'id':'f9941','name':'spriteFrame','files':['.json'],'subMetas':{},'userData':{'trimType':'none','trimThreshold':1,'rotated':False,'offsetX':0,'offsetY':0,'trimX':0,'trimY':0,'width':w,'height':h,'rawWidth':w,'rawHeight':h,'borderTop':0,'borderBottom':0,'borderLeft':0,'borderRight':0,'packable':False,'pixelsToUnit':100,'pivotX':.5,'pivotY':.5,'meshType':0,'isUuid':True,'imageUuidOrDatabaseUri':u+'@6c48a','atlasUuid':''}}
    write(pathlib.Path(str(out)+'.meta'),{'ver':'1.0.27','importer':'image','imported':True,'uuid':u,'files':[extension,'.json'],'subMetas':{'6c48a':texture,'f9941':frame},'userData':{'type':'sprite-frame','hasAlpha':not jpeg,'redirect':u+'@f9941'}})
    assets[name]=u+'@f9941'

catalog=json.loads((SOURCE/'res_server/server_configs/MainLevelConfig.json').read_text())['array']
levels=[]
for entry in catalog[:5]:
    level_id=entry['LevelList'].split(',')[0].strip()
    group=str(int(level_id)//100)
    group_levels=json.loads((SOURCE/f'res_server/server_levelconfigs/mainlevel/{group}/LevelConfigs.json').read_text())['Levels']
    levels.append(next(level for level in group_levels if level['levelID']==level_id))
assert len({level['levelID'] for level in levels})==5
assert [len(level['normalTiles']) for level in levels]==[9,15,30,30,45]
sizes={x['eleID']:[x['sizeX'],x['sizeY']] for x in json.loads((SOURCE/'res_server/server_configs/AllEleImageConfig.json').read_text())['eleImageConfigs']}
used=set()
for level in levels:
    types=list(dict.fromkeys(t['eleType'] for t in level['normalTiles']))
    for tile in level['normalTiles']:
        tile['eleType']=types[types.index(tile['eleType'])%3]
        used.add(tile['eleType'])
    picture('map'+level['mapID'],SOURCE/f"res_server/server_maps/map{level['mapID']}/Map{level['mapID']}.png",(700,1024),jpeg=True)
for fruit in used: picture('fruit'+str(fruit),SOURCE/f'res_server/server_images/fruit/Fruit_{fruit}.png',(210,210))
for name,path,size in [
    ('basket',SOURCE/'res/local/coreplay/sprite/basket_01.png',(900,220)),
    ('finger',SOURCE/'res/local/coreplay/sprite/newguider/GuideFinger.png',(238,227)),
    ('undo',ROOT/'Assets/OrchardUI/Resources/OrchardUI/PropGoldenUndo.png',(190,190)),
    ('shuffle',ROOT/'Assets/OrchardUI/Resources/OrchardUI/PropGoldenShuffle.png',(190,190)),
    ('magic',ROOT/'Assets/OrchardUI/Resources/OrchardUI/PropGoldenMagic.png',(190,190)),
    ('prop',ROOT/'Assets/OrchardUI/Resources/OrchardUI/PropMintButton.png',(190,190)),
    ('lock',ART/'HudCopperLock.png',(60,60)),
    ('icon',ROOT/'Assets/BizzaWZ/Common/BizzaGame/Z_ReplaceAssets/UI_Frame/Loading/ICON.png',(300,300))
    ,('coin',ROOT/'Assets/BizzaWZ/Final/Real/GameAssets/WzTexture_Money/GoldCoin_US.png',(140,140))
    ,('cash',ROOT/'Assets/BizzaWZ/Final/Real/GameAssets/WzTexture_Money/StackMoney_US.png',(160,160))
    ,('banknote',ROOT/'Assets/BizzaWZ/Final/Real/GameAssets/WzTexture_Money/PieceMoney_US.png',(140,90))
    ,('coinPile',ROOT/'Assets/BizzaWZ/Final/Real/GameAssets/WzTexture_Money/PileGold_US.png',(300,230))
    ,('cashPile',ROOT/'Assets/BizzaWZ/Final/Real/GameAssets/WzTexture_Money/PileMoney_US.png',(300,230))
    ,('lucky',ART/'HudLucky777.png',(224,220))
    ,('gift',ART/'MilestoneBadges/OrchardBadge01.png',(224,220))
    ,('giftPlate',ART/'HudMilestoneEntryPlate.png',(240,240))
    ,('slotLock',SOURCE/'res/local/coreplay/sprite/item/ExtraButton.png',(150,160))
    ,('track',ART/'BottomHudProgressTrack.png',(240,80))
    ,('fill',ART/'BottomHudProgressFill.png',(240,80))
]: picture(name,path,size)
picture('bubble',ART/'ReferenceFinish.png',size=(640,360),crop=(15,116,566,423))
atlas=Image.open(ART/'HudRefreshAtlas.png')
meta=(ART/'HudRefreshAtlas.png.meta').read_text()
for m in re.finditer(r'name: (HudBalance|HudAction|HudLevel|HudSettings)\s+rect:\s+serializedVersion: 2\s+x: (\d+)\s+y: (\d+)\s+width: (\d+)\s+height: (\d+)',meta):
    name,x,y,w,h=m.groups();x,y,w,h=map(int,(x,y,w,h))
    picture(name,image=atlas,crop=(x,atlas.height-y-h,x+w,atlas.height-y),size=(500,200))
controls=Image.open(ART/'Controls.png')
for m in re.finditer(r'name: (Title|ButtonBlue|Panel)\s+rect:\s+serializedVersion: \d+\s+x: (\d+)\s+y: (\d+)\s+width: (\d+)\s+height: (\d+)',(ART/'Controls.png.meta').read_text()):
    name,x,y,w,h=m.groups();x,y,w,h=map(int,(x,y,w,h))
    picture(name,image=controls,crop=(x,controls.height-y-h,x+w,controls.height-y),size=(800,700))
picture('white',image=Image.new('RGBA',(2,2),'white'))
font=ROOT/'Assets/OrchardUI/Fonts/Fidelity/Baloo2-ExtraBold.ttf'
fontout=PROJECT/'assets/art/OrchardFont.ttf'; shutil.copy(font,fontout)
write(pathlib.Path(str(fontout)+'.meta'),{'ver':'1.0.1','importer':'ttf-font','imported':True,'uuid':uid('font'),'files':['.ttf'],'subMetas':{},'userData':{}})
texts=json.loads((SOURCE/'res/local/configs/Text.json').read_text())
if isinstance(texts,dict): texts=texts.get('array',texts.get('Array',[]))
textmap={x['key']:x['en'] for x in texts}
config={'levels':levels,'sizes':sizes,'text':textmap,'store':'https://play.google.com/store/apps/details?id=com.wiwitsugeh.orchardtrio','name':'Orchard Trio','capacity':7,'guideType':39,'slotInterval':123,'slotY':-648,'slotFirstX':-431,'slotScale':110,'motionSeconds':.27,'matchSeconds':.20,'portraitWidth':1327.5,'portraitHeight':2360,'landscapeWidth':2360,'landscapeHeight':1327.5,'boardOffsetY':0,'initialBalance':89.13,'displayBalances':[89.13,89.71,90.81,103.33,116.08,139.96],'rewardCoins':1000,'levelRewards':[9.07,9.07,9.07,12.61,9.13]}
write(PROJECT/'assets/GameConfig.json',config)
write(PROJECT/'assets/GameConfig.json.meta',{'ver':'1.0.1','importer':'json','imported':True,'uuid':uid('config'),'files':['.json'],'subMetas':{},'userData':{}})

scene=[{'__type__':'cc.SceneAsset','_name':'OrchardPlayable','_objFlags':0,'_native':'','scene':{'__id__':1}},
       {'__type__':'cc.Scene','_name':'OrchardPlayable','_objFlags':0,'_parent':None,'_children':[],'_active':True,'_components':[],'_prefab':None,'autoReleaseAssets':False,'_id':uid('scene')}]
def vec(x=0,y=0,z=0):return {'__type__':'cc.Vec3','x':x,'y':y,'z':z}
def color(r=255,g=255,b=255,a=255):return {'__type__':'cc.Color','r':r,'g':g,'b':b,'a':a}
def component(node,typ,**kw):
    i=len(scene);scene.append({'__type__':typ,'_name':'','_objFlags':0,'node':{'__id__':node},'_enabled':True,'__prefab':None,**kw,'_id':uid(str(i))})
    scene[node]['_components'].append({'__id__':i});return i
def node(name,parent=1,x=0,y=0,w=100,h=100,art=None,text=None,font=44,active=True,button=False,tint=None):
    i=len(scene);scene.append({'__type__':'cc.Node','_name':name,'_objFlags':0,'_parent':{'__id__':parent},'_children':[],'_active':active,'_components':[],'_prefab':None,'_lpos':vec(x,y),'_lrot':{'__type__':'cc.Quat','x':0,'y':0,'z':0,'w':1},'_lscale':vec(1,1,1),'_layer':33554432,'_euler':vec(),'_id':uid('node'+str(i))})
    scene[parent]['_children'].append({'__id__':i})
    component(i,'cc.UITransform',_contentSize={'__type__':'cc.Size','width':w,'height':h},_anchorPoint={'__type__':'cc.Vec2','x':.5,'y':.5})
    if art: component(i,'cc.Sprite',_spriteFrame={'__uuid__':assets[art],'__expectedType__':'cc.SpriteFrame'},_type=0,_sizeMode=0,_color=tint or color(),_isTrimmedMode=False,_useGrayscale=False)
    if text is not None:
        component(i,'cc.Label',_string=text,_fontSize=font,_lineHeight=font*1.1,_fontFamily='Arial',_horizontalAlign=1,_verticalAlign=1,_overflow=2,_enableWrapText=True,_font={'__uuid__':uid('font'),'__expectedType__':'cc.Font'},_isSystemFontUsed=False,_color=tint or color(91,39,14))
        if tint and tint['r']>200:component(i,'cc.LabelOutline',_color=color(92,43,12),_width=2)
    if button: component(i,'cc.Button',_interactable=True,_transition=3,_duration=.1,_zoomScale=.94,_target={'__id__':i},clickEvents=[])
    return i
canvas=node('Canvas',w=1327.5,h=2360)
cam=node('Camera',parent=canvas)
scene[cam]['_lpos']=vec(0,0,1000)
ci=component(cam,'cc.Camera',_projection=0,_priority=0,_orthoHeight=1180,_near=0,_far=2000,_color=color(15,44,22),_depth=1,_stencil=0,_clearFlags=7,_rect={'__type__':'cc.Rect','x':0,'y':0,'width':1,'height':1},_visibility=33554432,_targetTexture=None)
component(canvas,'cc.Canvas',_cameraComponent={'__id__':ci},_alignCanvasWithScreen=True)
root=node('Root',parent=canvas,w=1327.5,h=2360)
bg=node('Background',parent=root,w=1760,h=2520,art='map1002')
backgrounds=[]
for level in levels:
    backgrounds.append(node('Map'+level['mapID'],parent=root,w=1760,h=2520,art='map'+level['mapID'],active=False))
header=node('Header',parent=root,y=972,w=1327.5,h=220)
node('LevelPlate',parent=header,x=-535,w=205,h=205,art='HudLevel')
levelLabel=node('Level',parent=header,x=-535,w=130,h=100,text='1',font=58)
for name,x,art,value in [('CoinBalance',-200,'coin','0.00'),('CashBalance',210,'cash','89.13')]:
    counter=node(name,parent=header,x=x,w=378,h=166,art='HudBalance')
    node('Icon',parent=counter,x=-122,w=120,h=120,art=art)
    node('Amount',parent=counter,x=55,y=34,w=224,h=62,text=value,font=49)
    button=node('Withdraw',parent=counter,x=55,y=-36,w=220,h=75,art='HudAction',button=True)
    node('Text',parent=button,w=205,h=62,text='≈ $0.00' if art=='coin' else 'Withdraw',font=32,tint=color())
settingsButton=node('SettingsButton',parent=header,x=540,w=195,h=190,art='HudSettings',button=True)
progress=node('Progress',parent=header,x=210,y=-120,w=350,h=40,text='',font=32,active=False)
board=node('Board',parent=root,w=1327.5,h=1700)
boards=[]; fruits=[]
for l,level in enumerate(levels):
    boardRoot=node('Level'+str(l+1),parent=board,w=1327.5,h=1700,active=l==0)
    boards.append(boardRoot);f=[]
    for tile in sorted(level['normalTiles'],key=lambda t:(t['layerIndex'],t['indexInLayer'])):
        w,h=sizes[tile['eleType']]
        f.append(node('Fruit'+str(tile['tileID']),parent=boardRoot,x=tile['posX'],y=tile['posY'],w=w,h=h,art='fruit'+str(tile['eleType']),button=True))
    fruits.append(f)
node('Basket',parent=root,y=-690,w=1066,h=254,art='basket')
slot=node('Slots',parent=root,w=1327.5,h=300)
slotLock=node('BasketLock',parent=root,x=460,y=-665,w=150,h=160,art='slotLock')
node('LockIcon',parent=slotLock,w=72,h=83,art='lock')
props=node('Props',parent=root,y=-1000,w=1327.5,h=240)
undo=node('Undo',parent=props,x=-248,w=212,h=219,art='prop',button=True)
node('Icon',parent=undo,w=190,h=190,art='undo')
node('CountPlate',parent=undo,x=72,y=-78,w=62,h=68,art='HudBalance')
node('Count',parent=undo,x=72,y=-78,w=60,h=50,text='1',font=37)
magic=node('Magic',parent=props,w=212,h=219,art='prop')
node('Icon',parent=magic,w=190,h=190,art='magic')
node('Lock',parent=magic,x=72,y=-78,w=58,h=68,art='lock')
shuffle=node('Shuffle',parent=props,x=248,w=212,h=219,art='prop',button=True)
node('Icon',parent=shuffle,w=190,h=190,art='shuffle')
node('CountPlate',parent=shuffle,x=72,y=-78,w=62,h=68,art='HudBalance')
node('Count',parent=shuffle,x=72,y=-78,w=60,h=50,text='1',font=37)
lucky=node('Lucky',parent=props,x=-530,w=224,h=220,art='lucky',button=True)
node('Track',parent=lucky,y=-84,w=224,h=68,art='track')
node('Fill',parent=lucky,y=-84,w=224,h=68,art='fill')
node('Count',parent=lucky,y=-84,w=215,h=55,text='0/5',font=38,tint=color())
gift=node('Gift',parent=props,x=530,w=224,h=220,art='giftPlate',button=True)
node('GiftIcon',parent=gift,y=17,w=210,h=210,art='gift')
node('AmountPlate',parent=gift,y=-94,w=215,h=68,art='HudBalance')
node('Amount',parent=gift,y=-94,w=205,h=55,text='$300',font=39)
fly=node('RewardFlights',parent=root,w=1327.5,h=2360)
for i in range(10):node('Bill'+str(i),parent=fly,w=125,h=74,art='banknote',active=False)
guide=node('Guide',parent=root,w=1327.5,h=2360)
node('Bubble',parent=guide,y=0,w=760,h=170,art='bubble')
guideText=node('GuideText',parent=guide,y=10,w=700,h=100,text=textmap.get('guide_newplayer_1','Tap 3 matching fruits!'),font=48)
finger=node('Finger',parent=guide,x=345,y=200,w=190,h=180,art='finger')
modal=node('Result',parent=root,w=1327.5,h=2360,active=False)
node('Shade',parent=modal,w=1327.5,h=2360,art='white',tint=color(7,16,5,210))
node('Banner',parent=modal,y=670,w=950,h=265,art='Title')
resultTitle=node('ResultTitle',parent=modal,y=670,w=800,h=140,text='Well Done!',font=94,tint=color())
node('RewardCoin',parent=modal,x=-290,y=180,w=340,h=250,art='coinPile')
node('RewardCash',parent=modal,x=280,y=180,w=340,h=250,art='cashPile')
node('RewardCoinsText',parent=modal,x=-290,y=-25,w=420,h=120,text='+1000',font=88,tint=color(255,220,49))
resultText=node('ResultText',parent=modal,x=280,y=-25,w=420,h=120,text='$ 9.07',font=80,tint=color())
nextBtn=node('Next',parent=modal,y=-335,w=850,h=150,art='HudAction',button=True)
nextText=node('NextText',parent=nextBtn,w=800,h=130,text='Claim',font=65,tint=color())
skipBtn=node('Skip',parent=modal,y=-540,w=630,h=125,art='ButtonBlue',button=True)
node('SkipText',parent=skipBtn,w=580,h=110,text='No Thanks',font=47,tint=color())
retryBtn=node('Retry',parent=modal,y=-540,w=630,h=125,art='ButtonBlue',button=True,active=False)
node('RetryText',parent=retryBtn,w=580,h=110,text=textmap.get('lose_pop_restart','Try again'),font=47,tint=color())
node('GameName',parent=modal,y=-750,w=750,h=90,text='Orchard Trio',font=48,tint=color(255,247,190))
settings=node('Settings',parent=root,w=1327.5,h=2360,active=False)
node('Shade',parent=settings,w=1327.5,h=2360,art='white',tint=color(7,16,5,190))
node('Panel',parent=settings,w=950,h=750,art='Panel')
node('Title',parent=settings,y=230,w=700,h=130,text='Settings',font=74)
soundButton=node('Sound',parent=settings,w=680,h=130,art='HudAction',button=True)
node('Text',parent=soundButton,w=640,h=120,text='Sound: On',font=56,tint=color())
resumeButton=node('Resume',parent=settings,y=-230,w=680,h=130,art='ButtonBlue',button=True)
node('Text',parent=resumeButton,w=640,h=120,text='Continue',font=56,tint=color())
install=node('Install',parent=root,y=-1140,w=420,h=60,art='HudAction',button=True)
node('InstallText',parent=install,w=395,h=55,text='Download',font=36,tint=color())
scriptUid=uid('script')
alphabet='ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/'
hexid=scriptUid.replace('-','')
scriptType=hexid[:5]+''.join(alphabet[int(hexid[i:i+3],16)>>6]+alphabet[int(hexid[i:i+3],16)&63] for i in range(5,32,3))
component(root,scriptType,config={'__uuid__':uid('config'),'__expectedType__':'cc.JsonAsset'},board={'__id__':board},backgrounds=[{'__id__':x} for x in backgrounds],boards=[{'__id__':x} for x in boards],slots={'__id__':slot},levelLabel={'__id__':levelLabel},progress={'__id__':progress},guide={'__id__':guide},finger={'__id__':finger},result={'__id__':modal},resultTitle={'__id__':resultTitle},resultText={'__id__':resultText},nextButton={'__id__':nextBtn},nextText={'__id__':nextText},retryButton={'__id__':retryBtn},installButton={'__id__':install},undoButton={'__id__':undo},shuffleButton={'__id__':shuffle})
write(PROJECT/'assets/OrchardPlayable.scene',scene)
write(PROJECT/'assets/OrchardPlayable.scene.meta',{'ver':'1.1.50','importer':'scene','imported':True,'uuid':uid('scene'),'files':['.json'],'subMetas':{},'userData':{}})
write(PROJECT/'assets/OrchardPlayable.ts.meta',{'ver':'4.0.24','importer':'typescript','imported':True,'uuid':scriptUid,'files':[],'subMetas':{},'userData':{}})
shutil.copy(WORK/'OrchardPlayable.ts',PROJECT/'assets/OrchardPlayable.ts')
write(WORK/'build_config.json',{'name':'OrchardTrio','platform':'web-mobile','buildPath':str(WORK/'build'),'outputName':'web','debug':False,'md5Cache':False,'sourceMaps':False,'startScene':uid('scene'),'scenes':[{'url':'db://assets/OrchardPlayable.scene','uuid':uid('scene')}],'includedModules':modules,'replaceSplashScreen':True,'packages':{'web-mobile':{'orientation':'auto','embedWebDebugger':False}}})
print('Prepared',len(levels),'levels;',len(used),'fruit textures;',sum(p.stat().st_size for p in (PROJECT/'assets/art').glob('*') if p.suffix!='.meta'),'asset bytes')
