from pathlib import Path
import hashlib
import json
import os
import re

ROOT = Path(r'C:/Projects/paopao/BizzaWZ')
DESIGN = ROOT / 'Design/NewItemPopupFix-20260918'
REL = Path('Assets/FruitsHarvest/Resources/Original/res/local/pops/newitempop/NewItemPop.prefab')
TARGET = ROOT / REL
source_bytes = TARGET.read_bytes()
newline = '\r\n' if b'\r\n' in source_bytes else '\n'
source = source_bytes.decode('utf-8').replace('\r\n', '\n')
pattern = re.compile(r'^--- !u!(\d+) &(-?\d+)\n.*?(?=^--- !u!|\Z)', re.M | re.S)
before = {m[2]: m[0] for m in pattern.finditer(source)}
blocks = before.copy()
changes = []

def field(fid, name, value):
    fid = str(fid)
    value = str(value)
    regex = re.compile(r'^(  ' + re.escape(name) + r': )([^\n]*)$', re.M)
    matches = list(regex.finditer(blocks[fid]))
    assert len(matches) == 1, (fid, name, len(matches))
    old = matches[0][2]
    if old == value:
        return
    blocks[fid] = regex.sub(lambda m: m[1] + value, blocks[fid])
    changes.append({'fileID': fid, 'field': name, 'before': old, 'after': value})

def color(fid):
    field(fid, 'm_fontColor', '{r: 1, g: 0.95686275, b: 0.8392157, a: 1}')
    rgba = (255 << 24) | (214 << 16) | (244 << 8) | 255
    old = re.search(r'(?m)^  m_fontColor32:\n    serializedVersion: 2\n    rgba: (\d+)\n', blocks[str(fid)])
    assert old
    blocks[str(fid)] = blocks[str(fid)].replace(old[0], old[0].replace(old[1], str(rgba)))
    changes.append({'fileID': str(fid), 'field': 'm_fontColor32.rgba', 'before': old[1], 'after': str(rgba)})

def font(fid, low, high, wrap):
    for key, val in [('m_enableAutoSizing', 1), ('m_fontSize', high), ('m_fontSizeBase', high),
                     ('m_fontSizeMin', low), ('m_fontSizeMax', high), ('m_enableWordWrapping', wrap),
                     ('m_HorizontalAlignment', 2), ('m_VerticalAlignment', 512)]:
        field(fid, key, val)

# Keep every existing GameObject, component, animation path, and interaction reference.
# The old baked title and its image-only glints cannot display the localized title.
disabled = [114967328832014496, 114919942235722503, 114632746491512912,
            114693329660128220, 114708180620337799, 114831874417830518]
for fid in disabled:
    field(fid, 'm_Enabled', 0)
field(224518489005915545, 'm_SizeDelta', '{x: 980, y: 160}')

# Retain the outer sliced button Image and all Button/input/animation hierarchy.
field(224433013960508438, 'm_SizeDelta', '{x: 680, y: 190}')
field(224709890794832471, 'm_AnchoredPosition', '{x: 0, y: 0}')
field(224709890794832471, 'm_SizeDelta', '{x: 0, y: 0}')
field(224589652054545887, 'm_AnchoredPosition', '{x: 0, y: 0}')
field(224589652054545887, 'm_SizeDelta', '{x: 500, y: 120}')
font(114499517081926786, 24, 64, 0)

field(224011164340880974, 'm_SizeDelta', '{x: 680, y: 100}')
font(114992816746925483, 36, 64, 0)
font(114291632710222552, 28, 52, 1)
color(114291632710222552)

# Title has no Graphic, so reuse it instead of creating a new UI node.
TITLE_GO = '1618682377417806'
CR = '222918202609180001'
TMP = '114918202609180001'
LOC = '114918202609180002'
assert all(i not in before for i in [CR, TMP, LOC])
original_title = blocks[TITLE_GO]
anchor = '  - component: {fileID: 224518489005915545}\n'
assert original_title.count(anchor) == 1
blocks[TITLE_GO] = original_title.replace(anchor, anchor + ''.join(
    '  - component: {fileID: ' + i + '}\n' for i in [CR, TMP, LOC]))
changes.append({'fileID': TITLE_GO, 'field': 'm_Component', 'added': [CR, TMP, LOC]})
blocks[CR] = before['222056248600188076'].replace('&222056248600188076', '&' + CR).replace(
    'm_GameObject: {fileID: 1711914853252429}', 'm_GameObject: {fileID: ' + TITLE_GO + '}')
blocks[TMP] = before['114992816746925483'].replace('&114992816746925483', '&' + TMP).replace(
    'm_GameObject: {fileID: 1711914853252429}', 'm_GameObject: {fileID: ' + TITLE_GO + '}')
field(TMP, 'm_text', 'NEW TOOL UNLOCKED!')
field(TMP, 'm_sharedMaterial', '{fileID: 2100000, guid: 5ebd41d56b09f8545a7500763f8239d2, type: 2}')
field(TMP, 'm_enableVertexGradient', 0)
font(TMP, 28, 62, 1)
color(TMP)
blocks[LOC] = before['114271925019662038'].replace('&114271925019662038', '&' + LOC).replace(
    'm_GameObject: {fileID: 1320256170494724}', 'm_GameObject: {fileID: ' + TITLE_GO + '}').replace(
    '  key: common_continue', '  key: guide_newtool_title')

result = pattern.sub(lambda m: blocks[m[2]], source) + ''.join(blocks[i] for i in [CR, TMP, LOC])
parsed = {m[2]: m[0] for m in pattern.finditer(result)}
assert len(parsed) == len(before) + 3
assert set(before) <= set(parsed)
assert re.findall(r'^--- !u!1 &(.*)$', result, re.M) == re.findall(r'^--- !u!1 &(.*)$', source, re.M)

def structural_signature(data):
    return re.findall(r'^  (m_Name|m_Father|m_Children|m_AnchorMin|m_AnchorMax|m_Pivot|m_IsActive):.*$|^  - \{fileID:.*$', data, re.M)

for fid, old in before.items():
    new = parsed[fid]
    # Parenting, child arrays, node names, active states, anchors and pivots unchanged.
    p = r'^  (?:m_Name|m_Father|m_Children|m_AnchorMin|m_AnchorMax|m_Pivot|m_IsActive):.*$|^  - \{fileID:.*$'
    assert re.findall(p, old, re.M) == re.findall(p, new, re.M), fid
    if old.startswith('--- !u!111'):
        assert new == old, ('animation', fid)
assert parsed['114733206282342372'] == before['114733206282342372']
assert parsed['114017892387517381'] == before['114017892387517381']

backup = DESIGN / 'Before' / REL
backup.parent.mkdir(parents=True, exist_ok=True)
if backup.exists():
    assert backup.read_bytes() == source_bytes, 'Existing baseline differs: never overwrite it.'
else:
    backup.write_bytes(source_bytes)
after_bytes = result.replace('\n', newline).encode('utf-8')
assert TARGET.read_bytes() == source_bytes, 'Prefab changed concurrently before write.'
temporary = TARGET.with_suffix('.prefab.newitemfix.tmp')
temporary.write_bytes(after_bytes)
os.replace(temporary, TARGET)
report = {
    'target': str(REL).replace('\\', '/'),
    'beforeSHA256': hashlib.sha256(source_bytes).hexdigest(),
    'afterSHA256': hashlib.sha256(after_bytes).hexdigest(),
    'beforeObjects': len(before), 'afterObjects': len(parsed),
    'gameObjectsBefore': len(re.findall(r'^--- !u!1 &', source, re.M)),
    'gameObjectsAfter': len(re.findall(r'^--- !u!1 &', result, re.M)),
    'addedComponents': [
        {'fileID': CR, 'type': 'CanvasRenderer', 'gameObject': TITLE_GO},
        {'fileID': TMP, 'type': 'TMPro.TextMeshProCustom', 'gameObject': TITLE_GO},
        {'fileID': LOC, 'type': 'MultipleLanguage.TextNeedLocalization', 'key': 'guide_newtool_title', 'gameObject': TITLE_GO}],
    'disabledVisualComponents': [str(fid) for fid in disabled],
    'unchanged': ['All existing GameObjects and hierarchy', 'All existing components retained',
                  'All Animation components and clips', 'NewItemPop serialized gameplay bindings',
                  'Outer sliced green button Image', 'All existing localized keys',
                  'ClaimBtn input target GameObject and code binding', 'All business C#'],
    'changedExistingBlocks': [fid for fid, old in before.items() if old != parsed[fid]],
    'changes': changes,
    'validation': 'Structure-only PASS; Unity import and rendered TMP geometry handled by root agent.'
}
(DESIGN / 'prefab-change-report.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps({k: report[k] for k in ['target', 'beforeObjects', 'afterObjects', 'gameObjectsBefore', 'gameObjectsAfter', 'afterSHA256']}, indent=2))
