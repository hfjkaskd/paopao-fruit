"""Copy original imagegen PNGs; adjust Sprite import rects, never image pixels or UI layout."""
from pathlib import Path
from PIL import Image
import hashlib, json, re, shutil, uuid

here = Path(__file__).resolve().parent
project = here.parents[1]
art = here / 'Art'
info_path = art / 'import-info.json'
info = json.loads(info_path.read_text(encoding='utf-8'))
replacements = {
    'SlotEmeraldMachine': 'SlotEmeraldMachine-v2.png',
    'SlotEmeraldHelpBody': 'SlotEmeraldHelpBody-v2.png',
    'SlotEmeraldCheck': 'SlotEmeraldCheck-v2.png',
    'SlotIvoryTile': 'SlotIvoryTile.png',
}
for filename in replacements.values():
    assert (art / filename).is_file(), filename

changes = []
for name, filename in replacements.items():
    source = art / filename
    im = Image.open(source)
    assert im.mode == 'RGBA' and im.getchannel('A').getextrema() == (0, 255)
    bounds = im.getchannel('A').point(lambda a: 255 if a > 24 else 0).getbbox()
    rect = [0, 0, im.width, im.height]
    dest = project / 'Assets/OrchardUI/Art' / (name + '.png')
    if name in info:
        record = info[name]
        meta = Path(str(dest) + '.meta').read_text(encoding='utf-8-sig')
    else:
        record = dict(info['SlotIvoryPanel'])
        record.update(path=dest.relative_to(project).as_posix(), guid=uuid.uuid4().hex,
                      border=[0,0,0,0], maxTextureSize=256)
        meta = (project / (info['SlotIvoryPanel']['path'] + '.meta')).read_text(encoding='utf-8-sig')
        meta = re.sub(r'^guid: \w+', 'guid: ' + record['guid'], meta, flags=re.M)
        meta = meta.replace('SlotIvoryPanel', name)
        meta = re.sub(r'(spriteID: )\w+', lambda m: m[1]+uuid.uuid4().hex, meta)
        meta = re.sub(r'(maxTextureSize: )\d+', r'\g<1>256', meta)
        meta = re.sub(r'      border: \{[^\n]+\}', '      border: {x: 0, y: 0, z: 0, w: 0}', meta)
    new_rect = ('      rect:\n        serializedVersion: 2\n        x: 0\n        y: 0\n'
                f'        width: {im.width}\n        height: {im.height}')
    meta, n = re.subn(r'      rect:\n        serializedVersion: 2\n        x: [0-9.]+\n        y: [0-9.]+\n        width: [0-9.]+\n        height: [0-9.]+', new_rect, meta)
    assert n == 1, (name, n)
    Path(str(dest) + '.meta').write_text(meta, encoding='utf-8', newline='\n')
    shutil.copy2(source, dest)
    record.update(rect=rect, sourceSize=list(im.size), alphaBounds=list(bounds),
                  source=str(source), sha256=hashlib.sha256(source.read_bytes()).hexdigest(),
                  pngPixelsUnmodified=True)
    info[name] = record
    changes.append({'asset': name, 'source': str(source), 'rect': rect,
                    'sameGuid': name != 'SlotIvoryTile'})

help_rel = 'Assets/BizzaWZ/Final/Real/UI/SlotsPanel/SlotFQAPanel/SlotFQAPanel.prefab'
help_path = project / help_rel
text = help_path.read_text(encoding='utf-8-sig')
baseline = (here / 'Before' / help_rel).read_text(encoding='utf-8-sig')
blocks = re.findall(r'^--- !u!114 &(\d+)\n(.*?)(?=^--- !u!|\Z)', baseline, re.M|re.S)
tile_ids = [fid for fid, body in blocks if 'guid: 894cffb134822554090aca3e317bb8d8' in body]
assert len(tile_ids) == 18
sprite = '{fileID: 21300000, guid: ' + info['SlotIvoryTile']['guid'] + ', type: 3}'
for fid in tile_ids:
    pattern = r'(^--- !u!114 &' + fid + r'\n.*?^  m_Sprite: )[^\n]+'
    text, n = re.subn(pattern, lambda m: m[1]+sprite, text, flags=re.M|re.S)
    assert n == 1, fid
help_path.write_text(text, encoding='utf-8', newline='\n')
visual_path = here / 'visual-changes.json'
visual = json.loads(visual_path.read_text(encoding='utf-8'))
for item in visual:
    if item['file'] == help_rel and item['fileID'] in tile_ids and item['field'] == 'm_Sprite':
        item['after'] = sprite
visual_path.write_text(json.dumps(visual, indent=2)+'\n', encoding='utf-8')
info_path.write_text(json.dumps(info, indent=2)+'\n', encoding='utf-8')
(here / 'visual-refinements.json').write_text(json.dumps(changes, indent=2)+'\n', encoding='utf-8')
print(json.dumps({'assets': changes, 'existingTileImagesUpdated': len(tile_ids), 'layoutChanged': False}, indent=2))
