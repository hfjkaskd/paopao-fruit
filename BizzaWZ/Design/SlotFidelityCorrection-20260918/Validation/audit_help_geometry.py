"""Independent RectTransform + existing VerticalLayoutGroup geometry evaluation.

Uses a 1080x1920 screen and the project's height-matched 1080x2360 reference.
This evaluates serialized layout; it is not a Unity render or glyph measurement.
"""
from pathlib import Path
import json
import re
from itertools import combinations

OUT = Path(__file__).resolve().parent
ROOT = OUT.parents[2]
PREFAB = ROOT / 'Assets/BizzaWZ/Final/Real/UI/SlotsPanel/SlotFQAPanel/SlotFQAPanel.prefab'
TEXT = PREFAB.read_text(encoding='utf-8-sig')
B = {m[2]: {'class': m[1], 'body': m[3]} for m in re.finditer(
    r'^--- !u!(\d+) &(-?\d+)(?: stripped)?\n(.*?)(?=^--- !u!|\Z)', TEXT, re.M | re.S)}
SCALE = 1920 / 2360


def field(fid, key):
    m = re.search(r'^  ' + re.escape(key) + r': (.*)$', B[str(fid)]['body'], re.M)
    return m[1] if m else None


def vector(fid, key):
    return tuple(map(float, re.findall(r'[xyz]: ([-\d.eE]+)', field(fid, key))))


def reference(fid, key):
    return re.search(r'fileID: (-?\d+)', field(fid, key))[1]


def children(fid):
    m = re.search(r'  m_Children:\n(.*?)  m_Father:', B[str(fid)]['body'], re.S)
    return re.findall(r'- \{fileID: (\d+)\}', m[1]) if m else []


def name(fid):
    return field(reference(fid, 'm_GameObject'), 'm_Name')


RECTS = {'3430944964572896150': [0, 0, 1080, 1920]}


def rect(fid):
    fid = str(fid)
    if fid in RECTS:
        return RECTS[fid]
    px, py, pw, ph = rect(reference(fid, 'm_Father'))
    lo, hi, pivot = vector(fid, 'm_AnchorMin'), vector(fid, 'm_AnchorMax'), vector(fid, 'm_Pivot')
    pos, delta = vector(fid, 'm_AnchoredPosition'), vector(fid, 'm_SizeDelta')
    w, h = pw * (hi[0] - lo[0]) + delta[0] * SCALE, ph * (hi[1] - lo[1]) + delta[1] * SCALE
    ax = px + pw * (lo[0] + (hi[0] - lo[0]) * pivot[0])
    ay = py + ph * (lo[1] + (hi[1] - lo[1]) * pivot[1])
    result = [ax + pos[0] * SCALE - pivot[0] * w,
              ay + pos[1] * SCALE - pivot[1] * h, w, h]
    RECTS[fid] = result
    return result


def top_rect(fid):
    x, y, w, h = rect(fid)
    return [x, 1920 - y - h, w, h]


def within(inner, outer, eps=.01):
    x, y, w, h = inner
    a, b, c, d = outer
    return x >= a - eps and y >= b - eps and x + w <= a + c + eps and y + h <= b + d + eps


def overlap(a, b):
    x, y, w, h = a
    p, q, r, s = b
    dx, dy = min(x + w, p + r) - max(x, p), min(y + h, q + s) - max(y, q)
    return [dx, dy] if dx > .01 and dy > .01 else None


TARGETS = {
    'cabinet': ('8805309752385714060', [94, 102, 892, 1541]),
    'tableContainer': ('7479695957089711254',
                       [94 + (108 - 27) * 892 / 886, 102 + (391 - 8) * 1541 / 1583,
                        720 * 892 / 886, 758 * 1541 / 1583]),
    'rowRoot': ('9055370915143298003', [223, 589, 652, 570]),
    'footer': ('1133554623368840100', [288, 1267, 505, 188]),
    'confirmation': ('769373269381146989', [319, 1617, 442, 110]),
}
errors, warnings, targets = [], [], {}
canvas_path = ROOT / 'Assets/BizzaWZ/Common/Framework/GameCanvas.prefab'
canvas_source = canvas_path.read_text(encoding='utf-8-sig')
canvas_fields = {}
for key, expected in [('m_ReferenceResolution', '{x: 1080, y: 2360}'),
                       ('m_MatchWidthOrHeight', '1'), ('m_UiScaleMode', '1'),
                       ('m_PixelPerfect', '0')]:
    observed = re.search(r'^  ' + key + r': (.*)$', canvas_source, re.M)[1]
    canvas_fields[key] = observed
    if observed != expected:
        errors.append('Canvas assumption differs from actual prefab: ' + key)
for label, (fid, expected) in TARGETS.items():
    actual = top_rect(fid)
    error = max(abs(a - b) for a, b in zip(actual, expected))
    targets[label] = {'fileID': fid, 'expected': expected, 'actual': actual, 'maxPixelError': error}
    if error > .02:
        errors.append('Target rectangle differs: ' + label)
    if not within(actual, [0, 0, 1080, 1920]):
        errors.append('Outside screen: ' + label)

# The existing foreground table must repeat the exact source region already
# painted in the one-piece cabinet, so it occludes old nested art without a
# second shifted border. Validate texture/sprite identity and pixel mapping.
sprite_meta = (ROOT / 'Assets/OrchardUI/Art/SlotHelpCabinetComplete.png.meta').read_text(encoding='utf-8-sig')
meta_guid = re.search(r'^guid: (\w+)', sprite_meta, re.M)[1]
sprite_section = re.search(r'^    sprites:\n(.*?)^    outline:', sprite_meta, re.S | re.M)[1]
sprite_rects = {}
for chunk in re.split(r'^    - serializedVersion: 2\n', sprite_section, flags=re.M)[1:]:
    id_match = re.search(r'^      internalID: (-?\d+)', chunk, re.M)
    rect_match = re.search(r'      rect:\n        serializedVersion: 2\n        x: ([\d.]+)\n        y: ([\d.]+)\n        width: ([\d.]+)\n        height: ([\d.]+)', chunk)
    if id_match and rect_match:
        sprite_rects[id_match[1]] = list(map(float, rect_match.groups()))

overlay = {'textureGuid': meta_guid, 'spriteRects': sprite_rects}
sprite_ids = []
for fid, label in [('5250022705326557845', 'cabinet'), ('9132095855591360206', 'table')]:
    sm = re.search(r'fileID: (-?\d+), guid: (\w+)', field(fid, 'm_Sprite'))
    if not sm or sm[2] != meta_guid or sm[1] not in sprite_rects:
        errors.append('Missing same-texture sprite reference: ' + label)
        sprite_ids.append(None)
    else:
        sprite_ids.append(sm[1])
    if field(fid, 'm_Type') != '0' or field(fid, 'm_PreserveAspect') != '0':
        errors.append('Overlay must use Simple Image with exact assigned geometry: ' + label)
    if field(fid, 'm_Color') != '{r: 1, g: 1, b: 1, a: 1}':
        errors.append('Overlay must use opaque unmodified tint: ' + label)

if all(sprite_ids):
    main_sprite, table_sprite = [sprite_rects[fid] for fid in sprite_ids]
    if main_sprite != [27, 81, 886, 1583] or table_sprite != [108, 523, 720, 758]:
        errors.append('Unexpected cabinet/table source crop')
    if sprite_ids[0] == sprite_ids[1]:
        errors.append('Table uses complete cabinet instead of the matched crop')
    sx, sy, sw, sh = main_sprite
    tx, ty, tw, th = table_sprite
    x, y, w, h = targets['cabinet']['actual']
    mapped = [x + (tx - sx) * w / sw,
              y + ((sy + sh) - (ty + th)) * h / sh,
              tw * w / sw, th * h / sh]
    actual = targets['tableContainer']['actual']
    error = max(abs(a - b) for a, b in zip(actual, mapped))
    overlay.update({'cabinetSpriteID': sprite_ids[0], 'tableSpriteID': sprite_ids[1],
                    'mappedCropRect': mapped, 'actualTableRect': actual, 'maxPixelMappingError': error})
    if error > .002:
        errors.append('Table crop is not aligned to identical background texture pixels')
content_children = children('617195395320269764')
overlay['existingSiblingOrder'] = content_children
if content_children.index('7479695957089711254') <= content_children.index('3481540104426233132'):
    errors.append('Foreground table does not draw after existing nested machine')

group_id = '71365554334776158'
if field(group_id, 'm_Enabled') != '1':
    errors.append('VerticalLayoutGroup disabled')
for key in ('m_ChildControlWidth', 'm_ChildControlHeight', 'm_ChildForceExpandWidth',
            'm_ChildForceExpandHeight', 'm_ChildScaleWidth', 'm_ChildScaleHeight', 'm_ReverseArrangement'):
    if field(group_id, key) != '0':
        errors.append('Unexpected layout behavior: ' + key)
if field(group_id, 'm_ChildAlignment') != '0' or field(group_id, 'm_Spacing') != '0':
    errors.append('Layout is not UpperLeft with zero additional spacing')
pad = re.search(r'  m_Padding:\n(.*?)  m_ChildAlignment:', B[group_id]['body'], re.S)[1]
if any(float(v) != 0 for v in re.findall(r': ([-\d.]+)', pad)):
    errors.append('Nonzero layout padding')

row_ids = children('9055370915143298003')
if len(row_ids) != 6:
    errors.append('Expected six rows')
rows, all_tiles, bounds_overlaps = [], [], []
for index, row_id in enumerate(row_ids):
    row_rect = top_rect(row_id)
    wanted = [223, 589 + index * 95, 652, 95]
    if max(abs(a - b) for a, b in zip(row_rect, wanted)) > .02:
        errors.append('Row geometry differs: ' + str(index))
    if not within(row_rect, targets['tableContainer']['actual']):
        errors.append('Row outside table: ' + str(index))
    contents, tile_rects, reward = [], [], None
    for child in children(row_id):
        label, box = name(child), top_rect(child)
        if label.startswith('Image'):
            continue  # Existing decorative row separator.
        contents.append({'fileID': child, 'name': label, 'rect': box})
        if not within(box, targets['tableContainer']['actual']):
            errors.append('Row content outside ivory table: ' + child)
        if label in ('Slot_1', 'Slot_1 (1)', 'Slot_1 (2)'):
            all_tiles.append((child, box))
            tile_rects.append(box)
            for icon in children(child):
                if not within(top_rect(icon), box):
                    errors.append('Symbol outside tile: ' + icon)
        elif label.startswith('Slot_1'):
            reward = box
    for a, b in combinations(contents, 2):
        amount = overlap(a['rect'], b['rect'])
        if amount:
            bounds_overlaps.append({'row': index + 1, 'first': a['name'], 'second': b['name'], 'pixels': amount})
    rows.append({'index': index + 1, 'fileID': row_id, 'rect': row_rect,
                 'expectedVLGRect': wanted, 'contents': contents})

tile_collisions = []
for (aid, a), (bid, b) in combinations(all_tiles, 2):
    amount = overlap(a, b)
    if amount:
        tile_collisions.append({'first': aid, 'second': bid, 'pixels': amount})
if tile_collisions:
    errors.append('Symbol tile rectangles overlap')
for item in bounds_overlaps:
    if item['first'].startswith('que') or item['second'].startswith('que'):
        warnings.append('Equal-sign TMP rectangle overlaps a neighboring reward box; empty text-box margins may account for this. Glyph rendering is not evaluated.')
    elif item['first'].startswith('des') or item['second'].startswith('des'):
        errors.append('Reward and description rectangles overlap in row ' + str(item['row']))

check = top_rect('3677535174969407404')
if not within(check, targets['confirmation']['actual']):
    errors.append('Confirmation check outside button')
for label in ('tableContainer', 'footer'):
    if not within(targets[label]['actual'], targets['cabinet']['actual']):
        errors.append(label + ' outside cabinet')
for a, b in (('tableContainer', 'footer'), ('footer', 'confirmation')):
    if overlap(targets[a]['actual'], targets[b]['actual']):
        errors.append(a + ' overlaps ' + b)

report = {'result': 'PASS' if not errors else 'FAIL', 'screen': [1080, 1920],
          'canvasReference': [1080, 2360], 'matchHeight': 1, 'scale': SCALE,
          'canvasSource': {'path': str(canvas_path), 'observedFields': canvas_fields},
          'targets': targets, 'matchedTextureOverlay': overlay, 'rows': rows, 'symbolTileRectOverlaps': tile_collisions,
          'rowContentRectOverlaps': bounds_overlaps, 'checkRect': check,
          'issues': sorted(set(errors)), 'warnings': sorted(set(warnings)),
          'limitations': ['Serialized layout and Unity standard VerticalLayoutGroup rules only; no Unity execution or import claim.',
                         'No font glyph or bitmap raster overlap test is performed.',
                         'Cabinet artwork intentionally extends behind content; that background overlap is not a layout error.']}
(OUT / 'help-geometry.json').write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
print(json.dumps({k: report[k] for k in ('result', 'targets', 'issues', 'warnings')}, ensure_ascii=False))
raise SystemExit(0 if not errors else 1)
