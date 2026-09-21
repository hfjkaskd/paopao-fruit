"""Inspect imported PNGs and effective nested sprite references; never edits Assets."""
from pathlib import Path
from collections import Counter
from datetime import datetime, timezone
import hashlib, json, re, subprocess
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
OUT = Path(__file__).resolve().parent
BASE = 'Assets/BizzaWZ/Final/Real/UI/SlotsPanel/'
MAIN = BASE + 'SlotPanel/SlotPanel.prefab'
HELP = BASE + 'SlotFQAPanel/SlotFQAPanel.prefab'
GROUP = BASE + 'BingoAsset/Assets/UiPrefab/SlotMachineGroup.prefab'
GROUP_GUID = '3e3b346423d816d47876dab1b13e409c'

def read(p): return p.read_text(encoding='utf-8-sig')
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def blocks(text):
    return {m[2]: {'class': m[1], 'body': m[3]} for m in re.finditer(r'^--- !u!(\d+) &(-?\d+)(?: stripped)?\n(.*?)(?=^--- !u!|\Z)', text, re.M | re.S)}
def direct(objs, fid, field):
    m = re.search(r'^  ' + re.escape(field) + r': (.*)$', objs[str(fid)]['body'], re.M)
    return m[1] if m else None

imports = json.loads(read(OUT / 'Art/import-info.json'))
current = {p: blocks(read(ROOT / p)) for p in [MAIN, HELP, GROUP]}
errors = []
checks = []

def effective(page, fid, field):
    matches = []
    pattern = (r'    - target: \{fileID: ' + str(fid) + ', guid: ' + GROUP_GUID + r', type: 3\}\n'
               + r'      propertyPath: ' + re.escape(field) + r'\n      value:([^\n]*)\n      objectReference: ([^\n]+)')
    for instance, obj in current[page].items():
        if obj['class'] != '1001': continue
        for m in re.finditer(pattern, obj['body']):
            matches.append({'instance': instance, 'value': m[2] if m[2] != '{fileID: 0}' else m[1].strip()})
    if len(matches) > 1:
        errors.append('Duplicate effective nested field ' + page + '/' + str(fid) + '/' + field)
    return (matches[0]['value'], 'nested override') if matches else (direct(current[GROUP], fid, field), 'source prefab')

def expected_sprite(name):
    return '{fileID: ' + str(imports[name]['fileID']) + ', guid: ' + imports[name]['guid'] + ', type: 3}'

def check_nested(label, page, fid, name):
    actual, source = effective(page, fid, 'm_Sprite')
    wanted = expected_sprite(name)
    ok = actual == wanted
    checks.append({'label': label, 'page': page, 'fileID': str(fid), 'expectedAsset': name,
                   'actual': actual, 'expected': wanted, 'effectiveSource': source, 'pass': ok})
    if not ok: errors.append(label + ' effective sprite mismatch')

def check_direct(label, page, fid, name):
    actual = direct(current[page], fid, 'm_Sprite')
    wanted = expected_sprite(name)
    ok = actual == wanted
    checks.append({'label': label, 'page': page, 'fileID': str(fid), 'expectedAsset': name,
                   'actual': actual, 'expected': wanted, 'effectiveSource': 'direct', 'pass': ok})
    if not ok: errors.append(label + ' direct sprite mismatch')

check_nested('Main cabinet', MAIN, 4300416282917706685, 'SlotEmeraldMachine')
check_nested('Help cabinet', HELP, 4300416282917706685, 'SlotEmeraldHelpBody')
check_nested('Main spin button', MAIN, 991499807541595864, 'SlotEmeraldButton')
check_nested('Help nested machine button', HELP, 991499807541595864, 'SlotEmeraldButton')
check_direct('Main back button', MAIN, 1396483153454402679, 'SlotEmeraldBack')
check_direct('Main help button', MAIN, 1374036228512148287, 'SlotEmeraldHelp')
check_direct('Help marquee', HELP, 5250022705326557845, 'SlotEmeraldMarquee')
check_direct('Help table panel', HELP, 9132095855591360206, 'SlotIvoryPanel')
check_direct('Help confirmation button', HELP, 2749890413784754410, 'SlotEmeraldButton')
check_direct('Help confirmation icon', HELP, 4019988058426453973, 'SlotEmeraldCheck')

baseline_help = blocks(read(OUT / 'Before' / HELP))
tiles = [fid for fid, block in baseline_help.items() if block['class'] == '114' and 'guid: 894cffb134822554090aca3e317bb8d8' in block['body']]
if len(tiles) != 18: errors.append('Unexpected baseline tile count ' + str(len(tiles)))
for i, fid in enumerate(tiles, 1): check_direct('Help symbol tile ' + str(i), HELP, fid, 'SlotIvoryTile')

asset_reports = []
for name, info in imports.items():
    p = ROOT / info['path']
    meta = read(Path(str(p) + '.meta'))
    issues = []
    def val(key):
        m = re.search(r'^\s*' + re.escape(key) + r': (.*)$', meta, re.M)
        return m[1] if m else None
    requirements = {'guid': info['guid'], 'textureType': '8', 'spriteMode': '2', 'alphaUsage': '1',
                    'alphaIsTransparency': '1', 'enableMipMap': '0', 'isReadable': '0', 'filterMode': '1',
                    'wrapU': '1', 'wrapV': '1', 'nPOTScale': '0', 'spritePixelsToUnits': '100'}
    for key, want in requirements.items():
        if val(key) != want: issues.append('Unexpected ' + key + ': ' + str(val(key)))
    caps = re.findall(r'^\s*maxTextureSize: (.*)$', meta, re.M)
    if not caps or any(c != str(info['maxTextureSize']) for c in caps):
        issues.append('Texture caps inconsistent with manifest')
    rect_match = re.search(r'      rect:\n        serializedVersion: 2\n        x: ([0-9.]+)\n        y: ([0-9.]+)\n        width: ([0-9.]+)\n        height: ([0-9.]+)', meta)
    actual_rect = list(map(float, rect_match.groups())) if rect_match else None
    if actual_rect != info['rect']: issues.append('Sprite rect mismatch')
    border_match = re.search(r'      border: \{x: ([0-9.]+), y: ([0-9.]+), z: ([0-9.]+), w: ([0-9.]+)\}', meta)
    actual_border = list(map(float, border_match.groups())) if border_match else None
    if actual_border != info['border']: issues.append('Sprite border mismatch')
    fid = str(info['fileID'])
    if ('      internalID: ' + fid) not in meta or not re.search(r'^      ' + re.escape(name) + ': ' + fid + '$', meta, re.M):
        issues.append('Sprite fileID/name mapping mismatch')
    matches = subprocess.run(['rg', '-l', '^guid: ' + info['guid'] + '$', str(ROOT / 'Assets'), '-g', '*.meta'], capture_output=True, text=True).stdout.splitlines()
    if len(matches) != 1: issues.append('Asset GUID missing or duplicated: ' + str(matches))
    production_sha = sha(p)
    source = Path(info['source'])
    if production_sha != info['sha256'] or not source.exists() or sha(source) != production_sha:
        issues.append('Imported PNG differs from approved generated source bytes/manifest')
    with Image.open(p) as im:
        size = list(im.size)
        mode = im.mode
        if size != info['sourceSize']: issues.append('PNG dimensions mismatch')
        if mode != 'RGBA': issues.append('PNG lacks explicit RGBA channels')
        a = im.getchannel('A') if 'A' in im.getbands() else None
        hist = a.histogram() if a else [0] * 256
        extrema = list(a.getextrema()) if a else None
        bounds = list(a.getbbox()) if a and a.getbbox() else None
        if not a or not hist[0] or not hist[255]: issues.append('PNG does not contain transparent and opaque pixels')
        x, y, w, h = info['rect']
        if min(x, y) < 0 or min(w, h) <= 0 or x + w > size[0] or y + h > size[1]:
            issues.append('Sprite rect outside PNG')
        l, b, r, t = info['border']
        if min(l,b,r,t) < 0 or l + r > w or b + t > h: issues.append('Invalid sprite border')
    asset_reports.append({'name': name, 'path': info['path'], 'result': 'PASS' if not issues else 'FAIL',
                          'guid': info['guid'], 'guidUnique': len(matches) == 1, 'fileID': info['fileID'],
                          'sourceSize': size, 'mode': mode, 'alphaExtrema': extrema, 'alphaBoundsTopLeft': bounds,
                          'transparentPixels': hist[0], 'opaquePixels': hist[255], 'translucentPixels': sum(hist[1:255]),
                          'rect': actual_rect, 'border': actual_border, 'maxTextureSize': info['maxTextureSize'],
                          'sourceBytesIdentical': source.exists() and sha(source) == production_sha,
                          'sha256': production_sha, 'issues': issues})
    errors.extend(name + ': ' + e for e in issues)

report = {'result': 'PASS' if not errors else 'FAIL', 'checkedUtc': datetime.now(timezone.utc).isoformat(),
          'effectiveSpriteChecks': checks, 'helpSymbolTileCount': len(tiles), 'newAssets': asset_reports,
          'issues': errors, 'limitations': ['Serialized/import checks only. Button/hierarchy/RectTransform/Spine/code freezes are checked by run_integrity_audit.py; runtime acceptance remains separate.']}
(OUT / 'imports-effective-audit.json').write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
print(json.dumps({'result': report['result'], 'effectiveChecks': len(checks), 'tiles': len(tiles),
                  'newAssets': len(asset_reports), 'issues': errors}, ensure_ascii=False))
