"""Read-only targeted alpha/geometry check for the existing 18 slot symbols."""
from pathlib import Path
from PIL import Image
import json
import re

OUT = Path(__file__).resolve().parent
ROOT = OUT.parents[2]
PREFAB = ROOT / 'Assets/BizzaWZ/Final/Real/UI/SlotsPanel/SlotFQAPanel/SlotFQAPanel.prefab'
BASE = OUT / 'Before/Assets/BizzaWZ/Final/Real/UI/SlotsPanel/SlotFQAPanel/SlotFQAPanel.prefab'
S = 1920 / 2360


def blocks(path):
    return {m[2]: (m[1], m[3]) for m in re.finditer(
        r'^--- !u!(\d+) &(-?\d+)(?: stripped)?\n(.*?)(?=^--- !u!|\Z)',
        path.read_text(encoding='utf-8-sig'), re.M | re.S)}


B, OLD = blocks(PREFAB), blocks(BASE)


def field(fid, key, source=B):
    m = re.search(r'^  ' + re.escape(key) + r': (.*)$', source[fid][1], re.M)
    return m[1] if m else None


def vector(fid, key, source=B):
    return tuple(map(float, re.findall(r'[xy]: ([-\d.eE]+)', field(fid, key, source))))


def children(fid):
    m = re.search(r'  m_Children:\n(.*?)  m_Father:', B[fid][1], re.S)
    return re.findall(r'fileID: (\d+)', m[1]) if m else []


def gameobject(fid):
    return re.search(r'fileID: (\d+)', field(fid, 'm_GameObject'))[1]


asset_info = {}
asset_dir = ROOT / 'Assets/BizzaWZ/Final/BizzaGame/Z_ReplaceAssets/UI_Frame/SlotPanel/SlotIcon'
for meta_path in asset_dir.glob('*.png.meta'):
    meta = meta_path.read_text(encoding='utf-8-sig')
    guid = re.search(r'^guid: (\w+)', meta, re.M)[1]
    path = Path(str(meta_path)[:-5])
    im = Image.open(path)
    asset_info[guid] = {'path': str(path), 'size': im.size,
                        'alphaBounds': im.getchannel('A').point(lambda a: 255 if a > 24 else 0).getbbox(),
                        'spriteMode': int(re.search(r'^  spriteMode: (\d+)', meta, re.M)[1])}

items, issues = [], []
for row_index, row in enumerate(children('9055370915143298003')):
    for tile in children(row):
        tile_name = field(gameobject(tile), 'm_Name')
        if tile_name not in ['Slot_1', 'Slot_1 (1)', 'Slot_1 (2)']:
            continue
        tile_w, tile_h = [n * S for n in vector(tile, 'm_SizeDelta')]
        for icon in children(tile):
            image_ids = [fid for fid, block in B.items() if block[0] == '114'
                         and field(fid, 'm_GameObject') == '{fileID: ' + gameobject(icon) + '}'
                         and field(fid, 'm_Sprite')]
            assert len(image_ids) == 1
            image_id = image_ids[0]
            guid = re.search(r'guid: (\w+)', field(image_id, 'm_Sprite'))[1]
            asset = asset_info[guid]
            assert asset['spriteMode'] == 1
            assert field(image_id, 'm_Type') == '0' and field(image_id, 'm_PreserveAspect') == '0'
            iw, ih = [n * S for n in vector(icon, 'm_SizeDelta')]
            dx, dy = [n * S for n in vector(icon, 'm_AnchoredPosition')]
            sw, sh = asset['size']
            l, t, r, b = asset['alphaBounds']
            visible = [dx - iw / 2 + l / sw * iw, -dy - ih / 2 + t / sh * ih,
                       (r - l) / sw * iw, (b - t) / sh * ih]
            x, y, w, h = visible
            margins = {'left': x + tile_w / 2, 'top': y + tile_h / 2,
                       'right': tile_w / 2 - x - w, 'bottom': tile_h / 2 - y - h}
            baseline_size = vector(icon, 'm_SizeDelta', OLD)
            now_size = vector(icon, 'm_SizeDelta')
            original_size_restored = max(abs(a - b) for a, b in zip(now_size, baseline_size)) < .0001
            if not original_size_restored:
                issues.append('Icon still enlarged relative to baseline: ' + icon)
            if min(margins.values()) < 10:
                issues.append('Visible alpha enters conservative 10px border inset: ' + icon)
            items.append({'row': row_index + 1, 'tile': tile, 'icon': icon,
                          'asset': asset, 'tileSizeScreen': [tile_w, tile_h],
                          'iconRectScreen': [iw, ih], 'visibleAlphaRectRelativeToTileCenter': visible,
                          'outerEdgeMargins': margins, 'originalSizeRestored': original_size_restored})
if len(items) != 18:
    issues.append('Did not find exactly 18 original symbols')
report = {'result': 'PASS' if not issues else 'FAIL', 'symbolCount': len(items),
          'screen': [1080, 1920], 'items': items, 'issues': issues,
          'minimumOuterEdgeMargin': min(min(item['outerEdgeMargins'].values()) for item in items),
          'interpretation': 'Alpha > 24 bounds projected through existing Simple Image rects. A 10px outer-edge inset is a conservative visual-space requirement; no literal gold-border segmentation or Unity rendering is claimed.'}
(OUT / 'symbol-insets.json').write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
print(json.dumps({key: report[key] for key in ['result', 'symbolCount', 'minimumOuterEdgeMargin', 'issues']}, ensure_ascii=False))
raise SystemExit(0 if not issues else 1)
