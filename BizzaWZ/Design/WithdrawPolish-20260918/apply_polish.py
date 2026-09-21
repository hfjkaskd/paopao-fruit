"""Small, reviewed Prefab visual edits; preserves hierarchy and business fields."""
from pathlib import Path
import json
import re

here = Path(__file__).resolve().parent
project = here.parents[1]
folder = project / 'Assets/BizzaWZ/Final/Real/UI/RealWithdrawalPanl'
changes = []

def field(text, file_id, key, value):
    pattern = rf'(^--- !u!\d+ &{file_id}\n(?:(?!^--- !u!).)*?^  {re.escape(key)}: )([^\n]*)'
    def replace(match):
        changes.append(dict(fileID=str(file_id), field=key, before=match[2], after=value))
        return match[1] + value
    text, count = re.subn(pattern, replace, text, flags=re.M | re.S)
    assert count == 1, (file_id, key, count)
    return text

page = (here / 'Before/RealWithdrawPanel.prefab').read_text(encoding='utf-8-sig')
# Grid pivot at the upper left and scale 1.22 previously biased the cards right.
page = field(page, 2086328851805122007, 'm_AnchoredPosition', '{x: -22.8716, y: -10}')
# Keep the existing order and sizes, with clear space below the dynamic hint.
page = field(page, 2448252221185092958, 'm_AnchoredPosition', '{x: 0, y: -365}')
page = field(page, 6954170809707330440, 'm_AnchoredPosition', '{x: -2.9609985, y: 33}')
page = field(page, 6242918543077416618, 'm_AnchoredPosition', '{x: 0, y: 375}')
# Align the three top information groups around the existing balance baseline.
page = field(page, 1775918613987139441, 'm_AnchoredPosition', '{x: 231, y: -49.525}')
page = field(page, 909907137678787225, 'm_AnchoredPosition', '{x: -351, y: -49.525}')

card = (here / 'Before/WithdrawLevelItem.prefab').read_text(encoding='utf-8-sig')
# Existing inset art and body font material: no new bitmap, node or component.
card = field(card, 7282447878676501664, 'm_AnchoredPosition', '{x: -70, y: -50}')
card = field(card, 7282447878676501664, 'm_SizeDelta', '{x: 100, y: 46}')
card = field(card, 168637483215687939, 'm_Sprite', '{fileID: -1559611953, guid: 50c5208c19d79f44ebd86c010248d125, type: 3}')
card = field(card, 168637483215687939, 'm_PixelsPerUnitMultiplier', '4')
card = field(card, 6525991162497755553, 'm_sharedMaterial', '{fileID: 2100000, guid: 486d0a25be512df4887a3bf53db492b8, type: 2}')
card = field(card, 6525991162497755553, 'm_fontColor', '{r: 0.14901961, g: 0.36862746, b: 0.3372549, a: 1}')
# Keep existing nested overrides consistent with the source card.
for target, prop, before, after in [
    ('168637483215687939', 'm_PixelsPerUnitMultiplier', 'value: 2', 'value: 4'),
    ('6525991162497755553', 'm_sharedMaterial', 'guid: 7727608c2306e9d4f99315146a923875', 'guid: 486d0a25be512df4887a3bf53db492b8'),
]:
    pattern = rf'(    - target: \{{fileID: {target}, guid: 0089d04ef066fab4ab571d694d70d62e, type: 3\}}\n      propertyPath: {prop}\n      value: [^\n]*\n      objectReference: [^\n]*)'
    page, count = re.subn(pattern, lambda match: match[0].replace(before, after), page)
    assert count == 5, (target, prop, count)
    changes.append(dict(nestedTarget=target, field=prop, count=count, before=before, after=after))

for name, text in [('RealWithdrawPanel', page), ('WithdrawLevelItem', card)]:
    (folder / (name + '.prefab')).write_text(text, encoding='utf-8', newline='\n')
(here / 'visual-changes.json').write_text(json.dumps(changes, indent=2), encoding='utf-8')
print('Updated 2 Prefabs; all changes recorded in visual-changes.json.')
