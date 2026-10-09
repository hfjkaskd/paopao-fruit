"""One-off serialized prefab edit. Does not change reward/withdrawal business logic."""
from pathlib import Path
import re

root = Path(__file__).resolve().parents[2]
path = root / 'Assets/BizzaWZ/Final/Real/UI/GetRewardPanel/GetRewardPanel.prefab'
source = path.read_text(encoding='utf-8-sig')
blocks = {int(m.group(1)): m.group(0) for m in re.finditer(r'--- !u!\d+ &(-?\d+).*?(?=\n--- !u!|\Z)', source, re.S)}
order = list(blocks)

def field(i, key, value):
    blocks[i], n = re.subn(r'^  ' + re.escape(key) + r':.*$', '  ' + key + ': ' + str(value), blocks[i], flags=re.M)
    assert n == 1, (i, key, n)

def vec(x, y): return '{x: %s, y: %s}' % (x, y)

def rect(i, x, y, w, h):
    for k, v in {'m_AnchorMin': vec(.5,.5), 'm_AnchorMax':vec(.5,.5), 'm_Pivot':vec(.5,.5),
                 'm_AnchoredPosition':vec(x,y), 'm_SizeDelta':vec(w,h),
                 'm_LocalScale':'{x: 1, y: 1, z: 1}'}.items(): field(i,k,v)

def override(instance, target, prop, value='', ref='{fileID: 0}'):
    b = blocks[instance]
    guid = re.search(r'm_SourcePrefab: \{fileID: 100100000, guid: (\w+)', b)[1]
    pattern = r'(    - target: \{fileID: ' + str(target) + r', guid: \w+, type: 3\}\n      propertyPath: ' + re.escape(prop) + r'\n)      value:.*\n      objectReference:.*'
    new = r'\g<1>      value: ' + str(value) + '\n      objectReference: ' + ref
    b,n = re.subn(pattern,new,b)
    if not n:
        entry = f'    - target: {{fileID: {target}, guid: {guid}, type: 3}}\n      propertyPath: {prop}\n      value: {value}\n      objectReference: {ref}\n'
        b = b.replace('    m_RemovedComponents:', entry+'    m_RemovedComponents:')
    assert n <= 1
    blocks[instance]=b

def orect(instance, target, x,y,w,h):
    for prop,val in [('m_AnchorMin.x',.5),('m_AnchorMin.y',.5),('m_AnchorMax.x',.5),('m_AnchorMax.y',.5),('m_Pivot.x',.5),('m_Pivot.y',.5),
                     ('m_AnchoredPosition.x',x),('m_AnchoredPosition.y',y),('m_SizeDelta.x',w),('m_SizeDelta.y',h)]: override(instance,target,prop,val)

WHITE='{r: 1, g: 0.98, b: 0.89, a: 1}'
GOLD='{r: 1, g: 0.83, b: 0.25, a: 1}'
OUTLINE='{fileID: 2100000, guid: b301682d6bdb3594c9c6e82b8c85d064, type: 2}'
# Reuse the title's authored dark outline material (same font atlas).
bg = blocks[4513770577257915395]
mat = re.search(r'target: \{fileID: 8056987933707987308,.*?propertyPath: m_sharedMaterial\n      value:.*?\n      objectReference: (\{[^\n]+)', bg, re.S)
if mat: OUTLINE=mat[1]

def text(i, size, minimum=None, color=WHITE, wrap=0):
    for k,v in {'m_Enabled':1,'m_RaycastTarget':0,'m_fontSize':size,'m_fontSizeBase':size,'m_enableAutoSizing':1,
                'm_fontSizeMin':minimum or size*.7,'m_fontSizeMax':size,'m_fontColor':color,'m_sharedMaterial':OUTLINE,
                'm_enableWordWrapping':wrap,'m_HorizontalAlignment':2,'m_VerticalAlignment':512}.items():field(i,k,v)

# The live gameplay remains visible behind a full-screen dark mask.
field(3074068380883675928,'m_IsActive',0)
field(4040113458832869929,'m_Enabled',0)
override(8343248915482114991,44964824660721137,'m_Color.a',.68)
override(4513770577257915395,1577418779977228660,'m_Enabled',0)
orect(4513770577257915395,8452426323194273140,0,0,852,1740)
field(4950157706890519921,'referenceSize',vec(852,1740))
blocks[4950157706890519921]=blocks[4950157706890519921].replace('position: {x: 0, y: -61}','position: {x: 0, y: 0}')
rect(4107249317512020636,0,0,852,1740)
rect(2253690655653916033,0,689,560,195)
orect(4513770577257915395,3804901329987287260,0,698,508,104)
for k,v in [('m_fontSize',82),('m_fontSizeMax',82),('m_fontSizeMin',48)]: override(4513770577257915395,8056987933707987308,k,v)
rect(6915577366139743424,0,594,370,72)
rect(5581791564478493828,0,391,464,300)

# Center active reward columns; single-currency mode uses the same prefab layout.
rect(8845840204847326021,0,60,704,300)
field(3820746567381903813,'m_Enabled',1)
field(3820746567381903813,'m_Spacing',72)
field(3820746567381903813,'m_ChildForceExpandWidth',0)
field(3820746567381903813,'m_ChildForceExpandHeight',0)
for i in [4644310843815806476,8324291154502178685,3000482346810684074,6391259612027711828,8167171475660969831,2191633550928138527]:rect(i,0,0,310,300)
for i in [84945338393759323,5380032551831635630]:rect(i,0,68,216,162)
for i in [2738604318736417619,7522282916600658977,8376197837530485006]:field(i,'m_Enabled',1);field(i,'m_RaycastTarget',0)
for i in [5540113194464029937,5112077072826833720]:rect(i,0,-57,310,92)
for i in [335629526288715014,2507324131923430329]:field(i,'m_Enabled',0)
for i in [6967940775231086710,4376882983465900431]:rect(i,0,0,310,92)
text(6718678499887503870,76,42,color=GOLD)
text(8179635061413538526,72,38)
rect(8491569502534748734,104,141,78,39)

# Move the existing coin caption with its column instead of leaving it screen-anchored.
blocks[4513770577257915395],n=re.subn(r'    - targetCorrespondingSourceObject: [^\n]+\n      insertIndex: 5\n      addedObject: \{fileID: 881216713368515842\}\n','',blocks[4513770577257915395]);assert n==1
field(881216713368515842,'m_Father','{fileID: 8167171475660969831}')
blocks[8167171475660969831]=blocks[8167171475660969831].replace('  m_Father:', '  - {fileID: 881216713368515842}\n  m_Father:')
rect(881216713368515842,0,-125,300,42)
text(8778501133961524696,28,22)

# Add a matching serialized CASH caption under the existing currency-mode-controlled column.
old=[2235243323553433836,881216713368515842,2642285029457122413,8778501133961524696,2934022078209570895]
new=[8961100082026100811,8961100082026100812,8961100082026100813,8961100082026100814,8961100082026100815]
mapping=dict(zip(old,new))
for a,b in mapping.items():
    block=blocks[a]
    for x,y in mapping.items():block=block.replace(str(x),str(y))
    blocks[b]=block;order.append(b)
field(new[0],'m_Name','CashCaption')
field(new[1],'m_Father','{fileID: 2191633550928138527}')
field(new[3],'m_text','CASH')
field(new[4],'english','CASH');field(new[4],'portuguese','DINHEIRO')
blocks[2191633550928138527]=blocks[2191633550928138527].replace('  m_Father:','  - {fileID: '+str(new[1])+'}\n  m_Father:')

# Separate video bonus progress from withdrawal progress, restoring original bound fields.
rect(5925290283851715495,0,-222,724,160)
field(2942238314673121631,'m_Enabled',0)
rect(6151764153449044812,0,74,704,58)
text(3620419705038272570,29,22,wrap=1)
rect(2069120713410374195,0,-10,452,64)
field(8860291489413871472,'m_Sprite','{fileID: 658283961, guid: 50c5208c19d79f44ebd86c010248d125, type: 3}')
rect(8748771307536403268,0,-10,420,50)
text(6542237316633747035,38,28)
field(7101388485578168173,'m_IsActive',1)
rect(7042234844801606061,0,0,724,120)
rect(956698414368219759,-304,-12,120,100)
rect(7851602894308505669,304,-12,120,100)
for i in [323728205064899692,1621983990230081181]:rect(i,0,16,105,70)
for i in [5690271709555295342,6919374649067699061]:rect(i,0,-45,142,42)
for i in [5941598888810479201,5371907132730170724]:text(i,30,24,color=GOLD)
for i in [568522056829968327,5885403615013984728]:field(i,'m_Enabled',0)
# Existing optional exchange-rate badge stays available beside the treasure chest.
orect(2070231283788701308,4920020890215000392,244,629,92,92)
orect(2070231283788701308,6648028364865917251,0,-14,86,40)
override(2070231283788701308,3341867253804193504,'m_fontSize',28)
override(2070231283788701308,3341867253804193504,'m_fontSizeMax',28)
override(2070231283788701308,3341867253804193504,'m_fontSizeMin',20)

# Keep both standard Button components and their code-bound callbacks intact.
rect(2020470049091444973,0,-402,660,150)
field(764215225145458180,'m_PixelsPerUnitMultiplier',170/150)
rect(3319438981270811713,59,0,470,106)
rect(7276637336425866716,-233,0,100,94)
text(4284177494034142888,64,38,color='{r: 1, g: 1, b: 1, a: 1}')
field(4284177494034142888,'m_sharedMaterial','{fileID: 2100000, guid: add56e5aadbd58a41b831694e15f2ae6, type: 2}')
rect(5413554490840950030,0,-561,480,106)
rect(1150488803724519214,0,0,424,82)

rect(8918131099644128992,0,-711,724,120)
rect(2048175119597911864,0,0,546,46)
rect(3019204934672517506,0,0,532,32)
rect(4582475832162146758,-325,0,94,86)
rect(558429857382813570,325,0,88,82)
rect(4134157486740831222,0,-78,734,82)
for i in [5148114096883086679,2408382842900339161,8246477513213878394,3141858843155459926]:field(i,'m_Enabled',1);field(i,'m_RaycastTarget',0)
text(5004473922195349271,28,22,wrap=1)

# Same placement for the framework's single-currency withdrawal variant.
real=9081509531121479751
orect(real,5923966043079866411,0,-711,724,120)
orect(real,5114781415189342544,0,0,546,46)
orect(real,2127013743659642457,0,0,532,32)
orect(real,1458936111277559134,-325,0,94,86)
orect(real,6329702882924675696,325,0,88,82)
orect(real,6287681074423477637,0,-78,734,82)
for i in [201807982791193085,1041491878047427331,6822706113396905384,7382339704331736872,9134999131909219619]:override(real,i,'m_Enabled',1)
for k,v in [('m_fontSize',28),('m_fontSizeMax',28),('m_fontSizeMin',22),('m_enableAutoSizing',1),('m_HorizontalAlignment',2),('m_fontColor.r',1),('m_fontColor.g',.98),('m_fontColor.b',.89)]:override(real,1041491878047427331,k,v)
override(real,1041491878047427331,'m_sharedMaterial',ref=OUTLINE)

result=source[:source.index('--- !u!')]+'\n'.join(blocks[i].rstrip() for i in order)+'\n'
path.write_text(result,encoding='utf-8',newline='\n')
print('Updated reward prefab; %s serialized objects; outline material %s' % (len(order),OUTLINE))
