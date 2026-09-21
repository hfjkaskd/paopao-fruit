from pathlib import Path
import hashlib
import json
import os
import re

ROOT = Path(__file__).resolve().parents[2]
PREFAB = ROOT / 'Assets/FruitsHarvest/Resources/Original/res/local/coreplay/CorePlayUI.prefab'
OUT = Path(__file__).resolve().parent
BEFORE = OUT / 'Before' / 'CorePlayUI.prefab'
CARD = '{fileID: -1757003328, guid: 50c5208c19d79f44ebd86c010248d125, type: 3}'
BODY = '{fileID: 2100000, guid: 4e90e2c922a361241b3fbce18c873684, type: 2}'
NAVY = '{r: 0.09, g: 0.32, b: 0.43, a: 1}'
NAVY32 = 23 | (82 << 8) | (110 << 16) | (255 << 24)

original_bytes = PREFAB.read_bytes()
original = original_bytes.decode('utf-8-sig')
newline = '\r\n' if b'\r\n' in original_bytes else '\n'
text = original.replace('\r\n', '\n')
initial = text
changes = []

def change(file_id, fields, multiline_text=False):
    global text
    pattern = r'(--- !u!\d+ &' + str(file_id) + r'\n)(.*?)(?=--- !u!|\Z)'
    match = re.search(pattern, text, re.S)
    if match is None:
        raise RuntimeError('Missing component ' + str(file_id))
    old = match.group(2)
    updated = old
    if multiline_text:
        updated, count = re.subn(r"  m_text: 'AD\n\n'\n", '  m_text: AD\n', updated)
        if count != 1:
            raise RuntimeError('Unexpected AD multiline text ' + str(file_id))
    for key, value in fields.items():
        updated, count = re.subn(r'^  ' + re.escape(key) + r':[^\n]*$', '  ' + key + ': ' + str(value), updated, flags=re.M)
        if count != 1:
            raise RuntimeError('Missing/duplicate field ' + key + ' in ' + str(file_id))
    if updated == old:
        raise RuntimeError('No change for ' + str(file_id))
    changes.append({'component': str(file_id), 'fields': list(fields) + (['m_text'] if multiline_text else [])})
    text = text[:match.start(2)] + updated + text[match.end(2):]

def front_fields(size, minimum):
    return {
        'm_sharedMaterial': BODY,
        'm_fontColor': NAVY,
        'm_fontSize': size,
        'm_fontSizeBase': size,
        'm_fontSizeMin': minimum,
        'm_fontSizeMax': size,
        'm_HorizontalAlignment': 2,
        'm_VerticalAlignment': 512,
    }

ordinary = {
    'Undo': [114950706389121088, 224977247909169898, 114756779511318474, 224927647628675231, 114523791136275799, 224125909588503792, 114929544267463227],
    'Magic': [114555560523574011, 224687983308432875, 114928127326489887, 224669598577343238, 114376155919033083, 224439577530107520, 114058318715168512],
    'Shuffle': [114161367720648980, 224418063802667073, 114757836356347682, 224411273952157860, 114649153637225918, 224404576429919846, 114601988992672481],
}
for owner, ids in ordinary.items():
    bg, bg_rect, icon, icon_rect, shadow, label_rect, front = ids
    change(bg, {'m_Sprite': CARD, 'm_Type': 1, 'm_PixelsPerUnitMultiplier': 3})
    change(bg_rect, {'m_SizeDelta': '{x: 156.0531, y: 60}'})
    change(icon, {'m_PreserveAspect': 1})
    change(icon_rect, {'m_AnchoredPosition': '{x: -27.3, y: 0}'})
    change(shadow, {'m_Enabled': 0})
    change(label_rect, {'m_AnchoredPosition': '{x: 16.7, y: 0}', 'm_SizeDelta': '{x: 83.9883, y: 44}'})
    change(front, front_fields(32, 20), multiline_text=True)

change(114495838826320329, {'m_Sprite': CARD, 'm_Type': 1, 'm_PixelsPerUnitMultiplier': 3})
change(224749364211270325, {'m_SizeDelta': '{x: 102, y: 44}'})
change(114280369416922100, {'m_PreserveAspect': 1})
change(224007631855938283, {'m_AnchoredPosition': '{x: -30, y: 82}', 'm_SizeDelta': '{x: 22, y: 26}'})
change(114901895415661471, {'m_Enabled': 0})
change(224835092874668729, {'m_AnchoredPosition': '{x: 7, y: 82}', 'm_SizeDelta': '{x: 40, y: 32}'})
change(114871677951225513, front_fields(26, 18))

# Keep TMP's Color32 backing field consistent with the serialized float color.
front_ids = [values[-1] for values in ordinary.values()] + [114871677951225513]
for file_id in front_ids:
    pattern = r'(--- !u!\d+ &' + str(file_id) + r'\n)(.*?)(?=--- !u!|\Z)'
    match = re.search(pattern, text, re.S)
    block, count = re.subn(r'(  m_fontColor32:\n    serializedVersion: 2\n    rgba:) \d+', r'\g<1> ' + str(NAVY32), match.group(2))
    if count != 1:
        raise RuntimeError('Missing Color32 ' + str(file_id))
    text = text[:match.start(2)] + block + text[match.end(2):]

def blocks(t):
    return {m.group(2): (m.group(1), m.group(3)) for m in re.finditer(r'--- !u!(\d+) &(\d+)\n(.*?)(?=--- !u!|\Z)', t, re.S)}

before_blocks, after_blocks = blocks(initial), blocks(text)
assert before_blocks.keys() == after_blocks.keys(), 'Component/object list changed'
allowed = {item['component'] for item in changes}
different = {key for key in before_blocks if before_blocks[key] != after_blocks[key]}
assert different == allowed, 'Unexpected changed component'
for key, (kind, block) in before_blocks.items():
    if kind not in ('114', '224'):
        assert block == after_blocks[key][1], 'Object/hierarchy/binding changed: ' + key
    if kind == '224':
        for field in ['m_Father', 'm_Children', 'm_AnchorMin', 'm_AnchorMax', 'm_LocalScale']:
            exp = r'^  ' + field + r':.*(?:\n  - \{fileID: \d+\})*'
            assert re.search(exp, block, re.M).group(0) == re.search(exp, after_blocks[key][1], re.M).group(0), 'Hierarchy/anchor/scale changed'

BEFORE.parent.mkdir(parents=True, exist_ok=True)
if BEFORE.exists():
    raise RuntimeError('Before snapshot already exists; this script is intentionally single-use')
BEFORE.write_bytes(original_bytes)
written = text.replace('\n', newline).encode('utf-8')
if original_bytes.startswith(b'\xef\xbb\xbf'):
    written = b'\xef\xbb\xbf' + written
temp = PREFAB.with_name(PREFAB.name + '.ad-badge-tmp')
temp.write_bytes(written)
os.replace(temp, PREFAB)
report = {
    'scope': 'Only four production prop AD badge visual components in flattened CorePlayUI.prefab',
    'beforeSha256': hashlib.sha256(original_bytes).hexdigest(),
    'afterSha256': hashlib.sha256(written).hexdigest(),
    'componentCountBefore': len(before_blocks),
    'componentCountAfter': len(after_blocks),
    'changedComponents': changes,
    'checks': {
        'objectAndComponentIdsPreserved': True,
        'hierarchyAnchorsScalePreserved': True,
        'adGroupGameObjectActivationPreserved': True,
        'buttonAndGameplayComponentsUnchanged': True,
        'animationReferencesUnchanged': True,
        'disconnectedTemplatePrefabsUntouched': True,
    },
}
(OUT / 'patch-report.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps({'modifiedPrefab': str(PREFAB), 'changedComponentCount': len(different), 'structureChecks': 'PASS'}, ensure_ascii=False))
