from pathlib import Path
import re, json

here=Path(__file__).resolve().parent
project=here.parents[1]
assets=json.loads((here/'Art/control-import-info.json').read_text())
changes=[]
def sprite(name):
    return '{fileID: 21300000, guid: '+assets[name]['guid']+', type: 3}'
def external(guid): return '{fileID: 21300000, guid: '+guid+', type: 3}'
def edit(rel,edits,overrides=()):
    source=project/rel
    backup=here/'Before'/rel
    assert backup.exists()
    text=source.read_text(encoding='utf-8-sig')
    assert text==backup.read_text(encoding='utf-8-sig'), 'Asset changed since baseline: '+rel
    for fid,key,value in edits:
        pattern=rf'(^--- !u!\d+ &{fid}\n(?:(?!^--- !u!).)*?^  {re.escape(key)}: )([^\n]*)'
        def replace(m):
            if m[2]!=value: changes.append({'file':rel,'fileID':str(fid),'field':key,'before':m[2],'after':value})
            return m[1]+value
        text,n=re.subn(pattern,replace,text,flags=re.M|re.S)
        assert n==1,(fid,key,n)
    for fid,key,kind,value in overrides:
        pattern=rf'(    - target: \{{fileID: {fid},[^\n]+\}}\n      propertyPath: {re.escape(key)}\n)(      value: [^\n]*\n      objectReference: [^\n]*)'
        def replace(m):
            before=re.search(rf'^      {kind}: (.*)$',m[2],re.M)[1]
            after=re.sub(rf'^      {kind}: .*$', '      '+kind+': '+value,m[2],flags=re.M)
            if before!=value: changes.append({'file':rel,'fileID':str(fid),'override':key,'field':kind,'before':before,'after':value})
            return m[1]+after
        text,n=re.subn(pattern,replace,text,flags=re.M)
        assert n==1,(fid,key,n)
    source.write_text(text,encoding='utf-8',newline='\n')

edit('Assets/BizzaWZ/Common/UI/GamePanel/RealGamePanel.prefab',[
    (7874943109805462537,'m_Color','{r: 1, g: 1, b: 1, a: 0}'),
])
edits=[
    (5821051965148166099,'m_Sprite',sprite('WoodTile')),
    (5821051965148166099,'m_Type','1'),
    (5821051965148166099,'m_PixelsPerUnitMultiplier','8'),
    (5860728120506579172,'m_fontColor','{r: 1, g: 1, b: 1, a: 1}'),
    (5860728120506579172,'m_sharedMaterial','{fileID: 2100000, guid: 7727608c2306e9d4f99315146a923875, type: 2}'),
    (8140369000614230474,'m_Sprite',sprite('Settings')),
    (8140369000614230474,'m_Type','0'),
    (8140369000614230474,'m_PreserveAspect','1'),
]
for fid in (4954420463098433676,917222461376090861):
    edits.extend([(fid,'m_Sprite',sprite('Counter')),(fid,'m_Type','1'),(fid,'m_PixelsPerUnitMultiplier','10')])
for fid in (8565961504699753555,7563967730577256576):
    edits.extend([(fid,'m_Sprite',sprite('GreenButton')),(fid,'m_Type','1'),(fid,'m_PixelsPerUnitMultiplier','10')])
edit('Assets/BizzaWZ/Final/MenuSystem/Common/CurrencyBar/CurrencyBar.prefab',edits)
edit('Assets/BizzaWZ/Final/Real/UI/GamePanel/Resources/Resources/GameUiWidget.prefab',[
    (2314400796718388841,'m_Sprite',external('874d776a383c46d2b2f3de4017fd243a')),
    (2314400796718388841,'m_Type','0'),
    (2314400796718388841,'m_PreserveAspect','1'),
    (4234855247401888128,'m_Color','{r: 1, g: 1, b: 1, a: 0}'),
],[
    (5821051965148166099,'m_Sprite','objectReference',sprite('WoodTile')),
    (5860728120506579172,'m_sharedMaterial','objectReference','{fileID: 2100000, guid: 7727608c2306e9d4f99315146a923875, type: 2}'),
    (5860728120506579172,'m_fontColor.r','value','1'),
    (5860728120506579172,'m_fontColor.g','value','1'),
    (5860728120506579172,'m_fontColor.b','value','1'),
])
edit('Assets/FruitsHarvest/Resources/Original/res/local/coreplay/CorePlayUI.prefab',[
    (114942066236133985,'m_Sprite',sprite('WoodTile')),
    (114942066236133985,'m_Type','1'),
    (114942066236133985,'m_PixelsPerUnitMultiplier','8'),
    (114703169052324433,'m_Sprite',external('5eb204cd2c0044dd8ed9415687e8a41f')),
    (114703169052324433,'m_Type','0'),
    (114703169052324433,'m_PreserveAspect','1'),
])
(here/'visual-changes.json').write_text(json.dumps(changes,indent=2),encoding='utf-8')
print(json.dumps({'visualFields':len(changes),'prefabCount':len(set(c['file'] for c in changes))}))
