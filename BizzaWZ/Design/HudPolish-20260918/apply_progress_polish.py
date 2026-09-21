"""Narrow prefab-only progress presentation changes; no runtime or hierarchy edits."""
from pathlib import Path
import json
import re
import shutil

project = Path(__file__).resolve().parents[2]
output = Path(__file__).resolve().parent
paths = [
    'Assets/BizzaWZ/Final/Real/UI/SlotsPanel/SlotEnter/SlotEnter.prefab',
    'Assets/BizzaWZ/Final/Real/UI/GamePanel/Resources/Resources/GameUiWidget.prefab',
]
changes = []
for relative in paths:
    source = project / relative
    backup = output / 'Before' / relative
    backup.parent.mkdir(parents=True, exist_ok=True)
    if not backup.exists():
        shutil.copy2(source, backup)

def replace_field(text, relative, file_id, key, expected, value):
    pattern = re.compile(r'(^--- !u!\d+ &' + str(file_id) + r'[^\n]*\n)(.*?)(?=^--- !u!|\Z)', re.M | re.S)
    match = pattern.search(text)
    assert match, (relative, file_id)
    old = f'  {key}: {expected}'
    new = f'  {key}: {value}'
    assert match[2].count(old + '\n') == 1, (file_id, key)
    body = match[2].replace(old + '\n', new + '\n', 1)
    changes.append({'path': relative, 'fileID': str(file_id), 'property': key, 'before': expected, 'after': value})
    return text[:match.start(2)] + body + text[match.end(2):]

relative = paths[0]
text = (project / relative).read_text(encoding='utf-8-sig')
for entry in [
    (2597468376317513793, 'm_PixelsPerUnitMultiplier', '1', '3'),
    (8611193201050741118, 'm_AnchoredPosition', '{x: 0.1867, y: 1.6}', '{x: 0, y: 0}'),
    (8611193201050741118, 'm_SizeDelta', '{x: 219.8943, y: 30}', '{x: 202, y: 24}'),
    (2151320124694660842, 'm_AnchoredPosition', '{x: 0, y: 0.6}', '{x: 0, y: 0}'),
]:
    text = replace_field(text, relative, *entry)
(project / relative).write_text(text, encoding='utf-8', newline='\n')

relative = paths[1]
text = (project / relative).read_text(encoding='utf-8-sig')
target_id = '5078707581075184592'
guid = '8c21a225bdfdef34099599e454f631c0'
for key, before, after in [
    ('m_fontColor.b', '0.49019608', '1'),
    ('m_fontColor.g', '0.30980393', '1'),
    ('m_fontColor.r', '0.09019608', '1'),
]:
    old = f'    - target: {{fileID: {target_id}, guid: {guid}, type: 3}}\n      propertyPath: {key}\n      value: {before}\n'
    new = old.replace(f'      value: {before}\n', f'      value: {after}\n')
    assert text.count(old) == 1, key
    text = text.replace(old, new, 1)
    changes.append({'path': relative, 'prefabInstance': '8263945193493257299', 'fileID': target_id, 'property': key, 'before': before, 'after': after})
before = '{fileID: 2100000, guid: 486d0a25be512df4887a3bf53db492b8, type: 2}'
after = '{fileID: 2100000, guid: 7727608c2306e9d4f99315146a923875, type: 2}'
old = f'    - target: {{fileID: {target_id}, guid: {guid}, type: 3}}\n      propertyPath: m_sharedMaterial\n      value: \n      objectReference: {before}\n'
new = old.replace(before, after)
assert text.count(old) == 1
text = text.replace(old, new, 1)
changes.append({'path': relative, 'prefabInstance': '8263945193493257299', 'fileID': target_id, 'property': 'm_sharedMaterial', 'before': before, 'after': after})
(project / relative).write_text(text, encoding='utf-8', newline='\n')

report = {
    'purpose': 'Inset the existing horizontal progress fill, reduce track border thickness and restore outlined white dynamic text.',
    'assetChoice': 'Reuse existing Controls/ProgressTrack and Controls/ProgressFill; inspected original orchard ProgressBg and Filler, whose bulky black well/caps are less suitable for the current 224x40 fixed track.',
    'changes': changes,
    'preserved': ['Serialized object IDs and order', 'Hierarchy and components', 'SlotEnter progressImag/progressTxt bindings', 'Image.Type.Filled horizontal Left and fillAmount', 'Progress text contents and 32 point size', 'Button logic and all C# sources'],
}
(output / 'progress-changes.json').write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
print(json.dumps({'changedFields': len(changes), 'paths': paths}, ensure_ascii=False))
