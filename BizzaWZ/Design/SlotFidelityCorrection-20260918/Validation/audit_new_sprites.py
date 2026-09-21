"""Read-only GUID, PNG and sprite importer verification; writes one report here."""
from pathlib import Path
from PIL import Image
import hashlib
import json
import re

OUT = Path(__file__).resolve().parent
ROOT = OUT.parents[2]
BASE = json.loads((OUT / 'baseline.json').read_text(encoding='utf-8'))
TARGETS = {
    'SlotHelpCabinetComplete': {
        'guid': '4642228d3bb347a99e7e35263be55b2e', 'max': 2048,
        'source': ROOT / 'Design/SlotFidelityCorrection-20260918/Art/SlotHelpCabinetComplete.png',
        'expectedReferences': {'SlotFQAPanel.prefab': ['5250022705326557845']},
    },
    'SlotFidelityCheck': {
        'guid': 'e0f53d6d6f0b4f12a08073707ebe074b', 'max': 256,
        'source': ROOT / 'Design/SlotRestyleImplementation-20260918/Art/SlotEmeraldCheck.png',
        'expectedReferences': {'SlotFQAPanel.prefab': ['4019988058426453973']},
    },
}
GUIDS = {item['guid']: [] for item in TARGETS.values()}
for path in (ROOT / 'Assets').rglob('*.meta'):
    match = re.search(r'^guid: (\w+)$', path.read_text(encoding='utf-8-sig', errors='replace'), re.M)
    if match and match[1] in GUIDS:
        GUIDS[match[1]].append(path.relative_to(ROOT).as_posix())

reports, all_issues = [], []
for name, spec in TARGETS.items():
    issues = []
    path = ROOT / 'Assets/OrchardUI/Art' / (name + '.png')
    meta_path = Path(str(path) + '.meta')
    meta = meta_path.read_text(encoding='utf-8-sig')
    value = lambda key: re.search(r'^\s*' + re.escape(key) + r': (.*)$', meta, re.M)[1]
    expected_meta = meta_path.relative_to(ROOT).as_posix()
    if GUIDS[spec['guid']] != [expected_meta]:
        issues.append('GUID is missing or duplicated')
    for key, want in [('textureType', '8'), ('spriteMode', '2'), ('enableMipMap', '0'),
                      ('isReadable', '0'), ('alphaIsTransparency', '1'),
                      ('maxTextureSize', str(spec['max']))]:
        if value(key) != want:
            issues.append('Unexpected importer ' + key)
    if '      internalID: 21300000' not in meta or '      ' + name + ': 21300000' not in meta:
        issues.append('Expected Sprite fileID mapping missing')
    section = re.search(r'^    sprites:\n(.*?)^    outline:', meta, re.S | re.M)[1]
    sprite_records = []
    for chunk in re.split(r'^    - serializedVersion: 2\n', section, flags=re.M)[1:]:
        sprite_name = re.search(r'^      name: (.+)$', chunk, re.M)[1]
        sprite_id = re.search(r'^      internalID: (-?\d+)$', chunk, re.M)[1]
        sprite_uuid = re.search(r'^      spriteID: (\w+)$', chunk, re.M)[1]
        sprite_records.append({'name': sprite_name, 'fileID': sprite_id, 'spriteID': sprite_uuid})
        if not re.search(r'^      ' + re.escape(sprite_name) + ': ' + sprite_id + '$', meta, re.M):
            issues.append('Sprite nameFileIdTable differs from internalID: ' + sprite_name)
        if '      pivot: {x: 0.5, y: 0.5}' not in chunk or '      border: {x: 0, y: 0, z: 0, w: 0}' not in chunk:
            issues.append('Unexpected per-sprite pivot or border: ' + sprite_name)
    if len({entry['fileID'] for entry in sprite_records}) != len(sprite_records):
        issues.append('Duplicate Sprite internalID values')
    if len({entry['spriteID'] for entry in sprite_records}) != len(sprite_records):
        issues.append('Duplicate per-sprite UUID values')
    expected_records = ({'SlotHelpCabinetComplete': '21300000', 'SlotHelpTableOverlay': '21300002'}
                        if name == 'SlotHelpCabinetComplete' else {'SlotFidelityCheck': '21300000'})
    if {entry['name']: entry['fileID'] for entry in sprite_records} != expected_records:
        issues.append('Unexpected sprite records or cross-sprite field contamination')
    im = Image.open(path)
    if im.mode != 'RGBA' or im.getchannel('A').getextrema() != (0, 255):
        issues.append('PNG does not have true transparent RGBA')
    rect_match = re.search(r'      rect:\n        serializedVersion: 2\n        x: ([\d.]+)\n        y: ([\d.]+)\n        width: ([\d.]+)\n        height: ([\d.]+)', meta)
    rect = tuple(map(float, rect_match.groups())) if rect_match else None
    if rect is None:
        issues.append('Missing sprite rect')
    else:
        x, y, w, h = rect
        if min(x, y) < 0 or min(w, h) <= 0 or x + w > im.width or y + h > im.height:
            issues.append('Sprite rect is outside PNG bounds')
        bbox = im.getchannel('A').point(lambda a: 255 if a > 24 else 0).getbbox()
        if bbox and not (x <= bbox[0] and im.height - y - h <= bbox[1]
                         and x + w >= bbox[2] and im.height - y >= bbox[3]):
            issues.append('Sprite rect clips visible alpha > 24 content')
    source_same = path.read_bytes() == spec['source'].read_bytes()
    if not source_same:
        issues.append('Production PNG differs from generated source')
    references = []
    for rel in BASE['editable']:
        text = (ROOT / rel).read_text(encoding='utf-8-sig')
        expected_ids = spec['expectedReferences'].get(Path(rel).name, [])
        for fid in expected_ids:
            block = re.search(r'^--- !u!114 &' + fid + r'\n(.*?)(?=^--- !u!|\Z)', text, re.M | re.S)
            want = 'm_Sprite: {fileID: 21300000, guid: ' + spec['guid'] + ', type: 3}'
            if not block or want not in block[1]:
                issues.append('Expected Image reference missing: ' + rel + '/' + fid)
        references.append({'prefab': rel, 'referenceCount': text.count(spec['guid'])})
    reports.append({'name': name, 'result': 'PASS' if not issues else 'FAIL',
                    'guid': spec['guid'], 'guidMetaPaths': GUIDS[spec['guid']],
                    'sourceSize': list(im.size), 'spriteRect': rect,
                    'visibleAlphaBounds': bbox, 'maxTextureSize': spec['max'],
                    'sourcePixelsUnmodified': source_same,
                    'pngSha256': hashlib.sha256(path.read_bytes()).hexdigest(),
                    'references': references, 'spriteRecords': sprite_records, 'issues': issues})
    all_issues += [name + ': ' + issue for issue in issues]
result = {'result': 'PASS' if not all_issues else 'FAIL', 'sprites': reports,
          'issues': all_issues, 'runtimeVerified': False}
(OUT / 'new-sprite-integrity.json').write_text(json.dumps(result, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
print(json.dumps(result, ensure_ascii=False))
raise SystemExit(0 if not all_issues else 1)
