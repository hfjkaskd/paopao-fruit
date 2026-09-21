"""Visual-only Prefab edits for quick replies and the service composer."""
from pathlib import Path
import json
import re

here = Path(__file__).resolve().parent
project = here.parents[1]
ui = project / 'Assets/BizzaWZ/Final/Real/UI'
changes = []
current_file = ''

def field(text, file_id, key, value):
    pattern = rf'(^--- !u!\d+ &{file_id}\n(?:(?!^--- !u!).)*?^  {re.escape(key)}: )([^\n]*)'
    def replace(match):
        changes.append(dict(file=current_file, fileID=str(file_id), field=key, before=match[2], after=value))
        return match[1] + value
    text, count = re.subn(pattern, replace, text, flags=re.M | re.S)
    assert count == 1, (file_id, key, count)
    return text

atlas = '50c5208c19d79f44ebd86c010248d125'
input_sprite = '{fileID: -2106829423, guid: ' + atlas + ', type: 3}'
inset_sprite = '{fileID: -1559611953, guid: ' + atlas + ', type: 3}'
body = '{fileID: 2100000, guid: 486d0a25be512df4887a3bf53db492b8, type: 2}'
ink = '{r: 0.09019608, g: 0.30980393, b: 0.49019608, a: 1}'

current_file = 'ServiceSelectPanel'
quick = (here / 'Before/ServiceSelectPanel.prefab').read_text(encoding='utf-8-sig')
row_images = [870467269385508032, 8971746123383350376, 8198691382104103530,
              8461153238993002826, 1223054394243737722, 3031890628177687760,
              3558711379614374223, 6767774041948264514]
row_texts = [6978226949653803335, 6737498048608474292, 7304933104185548933,
             7477266037482157431, 2459552015238615346, 1383874386300295906,
             3302444863855316968, 3899219166351189172]
for file_id in row_images:
    quick = field(quick, file_id, 'm_Sprite', input_sprite)
    quick = field(quick, file_id, 'm_PixelsPerUnitMultiplier', '2.5')
for file_id in row_texts:
    quick = field(quick, file_id, 'm_margin', '{x: 32, y: 8, z: 32, w: 8}')
# A quiet sage tint distinguishes the existing custom-message action.
quick = field(quick, row_images[-1], 'm_Color', '{r: 0.9098039, g: 0.9529412, b: 0.8745098, a: 1}')

current_file = 'ServicePanel'
service = (here / 'Before/ServicePanel.prefab').read_text(encoding='utf-8-sig')
service = field(service, 5036257019631139849, 'm_Sprite', inset_sprite)
service = field(service, 5036257019631139849, 'm_PixelsPerUnitMultiplier', '2')
# Inset the existing text Rect, so every line lives inside the rounded background.
service = field(service, 2620388809294053373, 'm_SizeDelta', '{x: -64, y: -24}')
service = field(service, 6689890846442346167, 'm_sharedMaterial', body)
service = field(service, 6689890846442346167, 'm_fontColor', ink)
service = field(service, 6689890846442346167, 'm_fontSize', '34')
service = field(service, 6689890846442346167, 'm_fontSizeBase', '34')
service = field(service, 6689890846442346167, 'm_enableAutoSizing', '1')
service = field(service, 6689890846442346167, 'm_fontSizeMin', '26')
service = field(service, 6689890846442346167, 'm_fontSizeMax', '34')
service = field(service, 6689890846442346167, 'm_overflowMode', '1')
# The original masked TextArea controls the real editor's scrolling and clipping.
service = field(service, 5317192524105659328, 'm_SizeDelta', '{x: -64, y: -24}')

for name, text in [('ServiceSelectPanel', quick), ('ServicePanel', service)]:
    (ui / name / (name + '.prefab')).write_text(text, encoding='utf-8', newline='\n')
(here / 'visual-changes.json').write_text(json.dumps(changes, indent=2), encoding='utf-8')
print('Updated 2 Prefabs using existing sprites/materials; no runtime code changes.')
