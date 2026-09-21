from pathlib import Path
import re,json,sys
here=Path(__file__).resolve().parent
project=here.parents[1]
sys.path.insert(0,str(project/'Design/HudPolish-20260918'))
from inspect_ui import read
imports=json.loads((here/'Art/import-info.json').read_text())
changes=[]
base='Assets/BizzaWZ/Final/Real/UI/SlotsPanel/'
group=base+'BingoAsset/Assets/UiPrefab/SlotMachineGroup.prefab'
main=base+'SlotPanel/SlotPanel.prefab'
help_page=base+'SlotFQAPanel/SlotFQAPanel.prefab'
def sprite(name):return '{fileID: 21300000, guid: '+imports[name]['guid']+', type: 3}'
def edit(rel,edits,overrides=(),adds=()):
    path=project/rel
    text=path.read_text(encoding='utf-8-sig')
    backup=here/'Before'/rel
    assert text==backup.read_text(encoding='utf-8-sig'),'Baseline changed '+rel
    for fid,key,value in edits:
        pattern=rf'(^--- !u!\d+ &{fid}\n(?:(?!^--- !u!).)*?^  {re.escape(key)}: )([^\n]*)'
        def repl(m):
            if m[2]!=value:changes.append({'file':rel,'fileID':str(fid),'field':key,'before':m[2],'after':value})
            return m[1]+value
        text,n=re.subn(pattern,repl,text,flags=re.M|re.S)
        assert n==1,(rel,fid,key,n)
    for fid,key,kind,value in overrides:
        pattern=rf'(    - target: \{{fileID: {fid},[^\n]+\}}\n      propertyPath: {re.escape(key)}\n)(      value: [^\n]*\n      objectReference: [^\n]*)'
        def repl(m):
            old=re.search(rf'^      {kind}: (.*)$',m[2],re.M)[1]
            after=re.sub(rf'^      {kind}: .*$', '      '+kind+': '+value,m[2],flags=re.M)
            if old!=value:changes.append({'file':rel,'fileID':str(fid),'override':key,'field':kind,'before':old,'after':value})
            return m[1]+after
        text,n=re.subn(pattern,repl,text,flags=re.M)
        assert n==1,(rel,fid,key,n)
    for instance,fid,key,value in adds:
        match=re.search(rf'^--- !u!1001 &{instance}\n.*?(?=^--- !u!|\Z)',text,re.M|re.S)
        assert match
        block=match[0]
        assert not re.search(rf'fileID: {fid},[^\n]+\}}\n      propertyPath: {re.escape(key)}\n',block)
        chunk=f'    - target: {{fileID: {fid}, guid: 3e3b346423d816d47876dab1b13e409c, type: 3}}\n      propertyPath: {key}\n      value: \n      objectReference: {value}\n'
        assert '    m_RemovedComponents:' in block
        new=block.replace('    m_RemovedComponents:',chunk+'    m_RemovedComponents:',1)
        text=text[:match.start()]+new+text[match.end():]
        changes.append({'file':rel,'fileID':str(fid),'instance':str(instance),'override':key,'field':'objectReference','before':None,'after':value,'addedVisualOverride':True})
    path.write_text(text,encoding='utf-8',newline='\n')

edit(group,[
(4300416282917706685,'m_Sprite',sprite('SlotEmeraldMachine')),
(4300416282917706685,'m_Type','0'),
(4300416282917706685,'m_PreserveAspect','0'),
(991499807541595864,'m_Sprite',sprite('SlotEmeraldButton')),
(991499807541595864,'m_Type','0'),
(991499807541595864,'m_PreserveAspect','0'),
])
main_edits=[]
for fid,name in [(1396483153454402679,'SlotEmeraldBack'),(1374036228512148287,'SlotEmeraldHelp')]:
    main_edits.extend([(fid,'m_Sprite',sprite(name)),(fid,'m_Type','0'),(fid,'m_PreserveAspect','1')])
edit(main,main_edits,[
(991499807541595864,'m_Sprite','objectReference',sprite('SlotEmeraldButton')),
(991499807541595864,'m_Type','value','0'),
])
help_edits=[
(5250022705326557845,'m_Sprite',sprite('SlotEmeraldMarquee')),
(5250022705326557845,'m_Type','0'),
(5250022705326557845,'m_PreserveAspect','0'),
(9132095855591360206,'m_Sprite',sprite('SlotIvoryPanel')),
(9132095855591360206,'m_Type','1'),
(9132095855591360206,'m_PixelsPerUnitMultiplier','4'),
(2749890413784754410,'m_Sprite',sprite('SlotEmeraldButton')),
(2749890413784754410,'m_Type','0'),
(2749890413784754410,'m_PreserveAspect','0'),
(4019988058426453973,'m_Sprite',sprite('SlotEmeraldCheck')),
(4019988058426453973,'m_Type','0'),
(4019988058426453973,'m_PreserveAspect','1'),
]
tiles=[r['id'] for r in read(project/help_page) if '894cffb134822554090aca3e317bb8d8' in r.get('m_Sprite','')]
assert len(tiles)==18,tiles
for fid in tiles:
    help_edits.extend([(fid,'m_Sprite',sprite('SlotIvoryPanel')),(fid,'m_Type','0')])
edit(help_page,help_edits,[
(991499807541595864,'m_Sprite','objectReference',sprite('SlotEmeraldButton')),
(991499807541595864,'m_Type','value','0'),
],[(3151483872259529701,4300416282917706685,'m_Sprite',sprite('SlotEmeraldHelpBody'))])
(here/'visual-changes.json').write_text(json.dumps(changes,indent=2),encoding='utf-8')
print(json.dumps({'changedFields':len(changes),'prefabCount':len(set(c['file'] for c in changes)),'symbolTiles':len(tiles)}))

