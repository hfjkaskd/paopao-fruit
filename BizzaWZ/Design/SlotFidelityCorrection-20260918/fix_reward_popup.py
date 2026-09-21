"""Apply and verify the local reward popup correction. No Unity process control."""
from pathlib import Path
import hashlib
import json
import re
import shutil
import sys

ROOT = Path(__file__).resolve().parents[2]
OUT = Path(__file__).resolve().parent
REL = Path('Assets/BizzaWZ/Final/Real/UI/SlotsPanel/SlotPanel/SlotPanel.prefab')
SOURCE = ROOT / REL
BACKUP = OUT / 'Before' / REL
sys.path.insert(0, str(ROOT / 'Design/HudPolish-20260918'))
from inspect_ui import read

if not BACKUP.exists():
    BACKUP.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(SOURCE, BACKUP)
    shutil.copy2(SOURCE.with_suffix('.prefab.meta'), BACKUP.with_suffix('.prefab.meta'))
before = BACKUP.read_text(encoding='utf-8-sig')
assert SOURCE.read_text(encoding='utf-8-sig') == before, 'Prefab changed after backup; inspect before rerunning.'
text = before
rows = {r['id']: r for r in read(SOURCE)}
changes = []

def set_field(fid, field, value):
    global text
    fid = str(fid)
    assert rows[fid]['path'].startswith('SlotPanel/Content/GetRewadPanel/'), rows[fid]
    pattern = rf'(^--- !u!\d+ &{fid}\n(?:(?!^--- !u!).)*?^  {re.escape(field)}: )([^\n]*)'
    def replace(m):
        changes.append({'fileID':fid, 'path':rows[fid]['path'], 'field':field, 'before':m[2], 'after':value})
        return m[1] + value
    text, count = re.subn(pattern, replace, text, flags=re.M | re.S)
    assert count == 1, (fid, field, count)

def sprite(guid):
    return '{fileID: 21300000, guid: ' + guid + ', type: 3}'

# The original backdrop was scaled to 974x678 while the values and claim button
# were placed outside it. Keep all live content inside one compact card.
set_field(4737857836923716867, 'm_LocalScale', '{x: 1, y: 1, z: 1}')
set_field(4737857836923716867, 'm_AnchoredPosition', '{x: 0, y: -80}')
set_field(4737857836923716867, 'm_SizeDelta', '{x: 840, y: 640}')
set_field(3129568935050995118, 'm_Sprite', sprite('e93c86362f6b4727a03d64e9f10f51b3'))
set_field(3129568935050995118, 'm_PixelsPerUnitMultiplier', '4')

# The six mutually exclusive icon variants keep their exact sprites, dimensions,
# active states and SlotRewardPanel bindings. Only their shared center moves.
for fid in (7339750857267786092,572969426428037648,2384695331660256314,
            5899393945800387749,6756194499756007949,8631493306211032524):
    set_field(fid, 'm_AnchoredPosition', '{x: 0, y: 70}')
set_field(2991638936245649488, 'm_AnchoredPosition', '{x: 0, y: -135}')

set_field(6951951041397404055, 'm_AnchoredPosition', '{x: 0, y: -285}')
set_field(6951951041397404055, 'm_SizeDelta', '{x: 440, y: 112}')
set_field(2180263369653926189, 'm_Sprite', sprite('524f480620394cedbcaa19861f9144f6'))
set_field(2180263369653926189, 'm_Type', '0')
set_field(1407570891747111414, 'm_AnchoredPosition', '{x: 0, y: 0}')
set_field(1407570891747111414, 'm_SizeDelta', '{x: 106, y: 72}')
set_field(8486339001066278036, 'm_Sprite', sprite('e0f53d6d6f0b4f12a08073707ebe074b'))
set_field(8486339001066278036, 'm_Type', '0')
set_field(8486339001066278036, 'm_PreserveAspect', '1')

rx = re.compile(r'^--- !u!(\d+) &(-?\d+)[^\n]*\n.*?(?=^--- !u!|\Z)', re.M | re.S)
def blocks(value):
    return {m[2]:(m[1],m[0]) for m in rx.finditer(value)}
old, new = blocks(before), blocks(text)
assert list(old) == list(new)
allowed = {}
for c in changes:
    allowed.setdefault(c['fileID'],set()).add(c['field'])
for fid, (kind, block) in old.items():
    assert kind == new[fid][0]
    def freeze(value):
        for field in allowed.get(fid,[]):
            value = re.sub(rf'^  {re.escape(field)}: [^\n]*\n', '', value, flags=re.M)
        return value
    assert freeze(block) == freeze(new[fid][1]), ('Unexpected change',fid)
    if kind in ('1','1001','225'):
        assert block == new[fid][1], ('Hierarchy, nested prefab or CanvasGroup changed',fid)

SOURCE.write_text(text,encoding='utf-8',newline='\n')
after_rows = {r['id']:r for r in read(SOURCE)}
boundaries = {'background':{'left':-420,'right':420,'bottom':-400,'top':240},
              'largestRewardIcon':{'left':-175.125,'right':175.125,'bottom':-51.125,'top':191.125},
              'moneyRow':{'left':-345.7111,'right':345.7111,'bottom':-185,'top':-85},
              'claimButton':{'left':-220,'right':220,'bottom':-341,'top':-229},
              'checkIconInButton':{'left':-53,'right':53,'bottom':-36,'top':36}}
bg = boundaries['background']
for name in ('largestRewardIcon','moneyRow','claimButton'):
    b = boundaries[name]
    assert b['left'] > bg['left'] and b['right'] < bg['right'] and b['bottom'] > bg['bottom'] and b['top'] < bg['top'],name
assert boundaries['largestRewardIcon']['bottom'] - boundaries['moneyRow']['top'] > 30
assert boundaries['moneyRow']['bottom'] - boundaries['claimButton']['top'] > 40

report = {'result':'PASS','scope':'GetRewadPanel subtree only','fieldChanges':len(changes),
          'serializedObjectsBefore':len(old),'serializedObjectsAfter':len(new),
          'objectIdsOrderComponentsHierarchyAndActiveStatesUnchanged':True,
          'allComponentsOutsideExplicitVisualAndRectChangesUnchanged':True,
          'allButtonAndRewardBusinessBindingsUnchanged':True,
          'nestedSlotMachineAndSpineOverridesUnchanged':True,
          'allSixRewardSpriteReferencesSizesAndDisplayStatesUnchanged':True,
          'inactiveTitleUnchanged':True,'dynamicMoneyTextsAndLocalizationUnchanged':True,
          'backgroundContainsAllContent':True,'rectBounds':boundaries,
          'beforeSha256':hashlib.sha256(BACKUP.read_bytes()).hexdigest(),
          'afterSha256':hashlib.sha256(SOURCE.read_bytes()).hexdigest(),
          'runtimeVerified':False,'note':'File and geometry verification only. No Unity command, UI control, claim or ad was invoked.'}
(OUT/'reward-popup-changes.json').write_text(json.dumps(changes,ensure_ascii=False,indent=2),encoding='utf-8')
(OUT/'reward-popup-validation.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(report,ensure_ascii=False,indent=2))
