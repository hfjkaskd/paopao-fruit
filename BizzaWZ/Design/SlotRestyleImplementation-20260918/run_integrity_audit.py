"""Read-only verification; writes reports only under this Design directory."""
from pathlib import Path
import hashlib
import json
import re
import struct
from collections import Counter

ROOT = Path(__file__).resolve().parents[2]
OUT = Path(__file__).resolve().parent
IMAGE_GUID = 'fe87c0e1cc204ed48ad3b37840f39efc'
TMP_GUID = 'f4688fdb7df04437aeb418b961361dc5'
IMAGE_FIELDS = {'m_Sprite', 'm_Color', 'm_Type', 'm_PreserveAspect', 'm_PixelsPerUnitMultiplier', 'm_FillCenter', 'm_Material'}
TMP_FIELDS = {'m_fontColor', 'm_fontColor32', 'm_sharedMaterial', 'm_fontAsset', 'm_fontSize', 'm_fontSizeBase',
              'm_fontSizeMin', 'm_fontSizeMax', 'm_enableAutoSizing', 'm_fontStyle', 'm_fontWeight', 'm_margin'}
BLOCK_RX = re.compile(r'^--- !u!(\d+) &(-?\d+)( stripped)?\n(.*?)(?=^--- !u!|\Z)', re.M | re.S)
FIELD_RX = re.compile(r'^  ([A-Za-z_][A-Za-z_0-9]*):[^\n]*(?:\n(?!  [A-Za-z_][A-Za-z_0-9]*:)[^\n]*)*', re.M)
OVERRIDE_RX = re.compile(r'    - target: \{fileID: (-?\d+), guid: (\w+), type: 3\}\n'
                         r'      propertyPath: ([^\n]+)\n'
                         r'      value:([^\n]*)\n'
                         r'      objectReference: ([^\n]+)\n')

def read(path):
    return path.read_text(encoding='utf-8-sig')

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def load(name):
    return json.loads(read(OUT / name))

def dump(name, data):
    (OUT / name).write_text(json.dumps(data, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')

def blocks(text):
    return {m[2]: {'class': m[1], 'stripped': bool(m[3]), 'body': m[4]} for m in BLOCK_RX.finditer(text)}

def fields(body):
    return {m[1]: m[0] for m in FIELD_RX.finditer(body)}

def script(body):
    found = re.search(r'^  m_Script: \{fileID: \d+, guid: (\w+), type: 3\}$', body, re.M)
    return found[1] if found else None

def allow_for(block):
    if not block or block['class'] != '114' or block['stripped']:
        return set()
    guid = script(block['body'])
    return IMAGE_FIELDS if guid == IMAGE_GUID else TMP_FIELDS if guid == TMP_GUID else set()

def protected_category(block):
    if block['class'] in ('4', '224'):
        return 'Transform'
    if block['class'] == '1':
        return 'GameObjectHierarchyAndComponentList'
    if block['class'] == '225':
        return 'CanvasGroup'
    if block['class'] == '114' and not allow_for(block):
        body = block['body']
        if '  m_OnClick:' in body or '  onClick:' in body:
            return 'Button'
        if '  skeletonDataAsset:' in body:
            return 'Spine'
        if re.search(r'^  key:', body, re.M):
            return 'LocalizationBinding'
        return 'OtherComponentOrBusinessBinding'
    return None

baseline = load('audit-baseline.json')
baseline_blocks = {b['path']: blocks(read(OUT / 'Before' / b['path'])) for b in baseline['prefabs']}
guid_paths = {}
for rel in baseline_blocks:
    meta = ROOT / (rel + '.meta')
    match = re.search(r'^guid: (\w+)$', read(meta), re.M) if meta.exists() else None
    if match:
        guid_paths[match[1]] = rel

def override_is_visual(fid, guid, key):
    rel = guid_paths.get(guid)
    obj = baseline_blocks.get(rel, {}).get(fid)
    return key.split('.')[0] in allow_for(obj)

def split_overrides(body):
    records = {}
    duplicates = []
    for match in OVERRIDE_RX.finditer(body):
        fid, guid, key, value, ref = match.groups()
        token = (fid, guid, key)
        if token in records:
            duplicates.append(token)
        records[token] = {'raw': match[0], 'value': value.strip(), 'objectReference': ref,
                          'visual': override_is_visual(fid, guid, key)}
    frozen = OVERRIDE_RX.sub(lambda m: '' if override_is_visual(m[1], m[2], m[3]) else m[0], body)
    return records, duplicates, frozen

errors = []
changes = []
prefab_reports = []
frozen_checks = []
allowlist = []
for base in baseline['prefabs']:
    rel = base['path']
    backup = OUT / 'Before' / rel
    current = ROOT / rel
    old_text = read(backup)
    old = baseline_blocks[rel]
    issues = []
    if sha(backup) != base['sha256']:
        issues.append('Immutable backup hash mismatch')
    if not current.exists():
        errors.append(rel + ': Missing prefab')
        continue
    new_text = read(current)
    new = blocks(new_text)
    old_headers = [(i, b['class'], b['stripped']) for i, b in old.items()]
    new_headers = [(i, b['class'], b['stripped']) for i, b in new.items()]
    if old_headers != new_headers:
        issues.append('Serialized object IDs/order/classes changed')
    if base['guardOnly'] and new_text != old_text:
        issues.append('Guard-only nested prefab changed')
    start_changes = len(changes)
    for fid, previous in old.items():
        actual = new.get(fid)
        if actual is None:
            continue
        category = protected_category(previous)
        if category:
            same = actual == previous
            frozen_checks.append({'path': rel, 'fileID': fid, 'category': category, 'unchanged': same})
            if not same:
                issues.append(category + ' changed: ' + fid)
        allowed = set() if base['guardOnly'] else allow_for(previous)
        if allowed:
            allowlist.append({'path': rel, 'fileID': fid, 'scriptGuid': script(previous['body']),
                              'allowedFields': sorted(allowed)})
        if previous == actual:
            continue
        if previous['class'] == '1001' and not base['guardOnly']:
            prev_ov, prev_dup, prev_frozen = split_overrides(previous['body'])
            now_ov, now_dup, now_frozen = split_overrides(actual['body'])
            if now_dup != prev_dup:
                issues.append('New duplicate nested overrides: ' + fid)
            if prev_frozen != now_frozen:
                issues.append('Nested prefab has nonvisual modification: ' + fid)
            for token in sorted(set(prev_ov) | set(now_ov)):
                p, n = prev_ov.get(token), now_ov.get(token)
                if p == n:
                    continue
                target, guid, field = token
                approved = override_is_visual(target, guid, field)
                changes.append({'path': rel, 'instanceFileID': fid, 'targetFileID': target,
                                'targetGuid': guid, 'field': field, 'kind': 'nestedOverride',
                                'before': p, 'after': n, 'allowed': approved})
                if not approved:
                    issues.append('Nested override outside visual allowlist: ' + target + '/' + field)
            continue
        prev_fields, now_fields = fields(previous['body']), fields(actual['body'])
        changed_keys = [k for k in sorted(set(prev_fields) | set(now_fields)) if prev_fields.get(k) != now_fields.get(k)]
        for key in changed_keys:
            approved = key in allowed
            changes.append({'path': rel, 'fileID': fid, 'field': key, 'kind': 'direct',
                            'before': prev_fields.get(key), 'after': now_fields.get(key), 'allowed': approved})
            if not approved:
                issues.append('Field outside visual allowlist: ' + fid + '/' + key)
        # Removing the full permitted field blocks must leave exact equality, catching unparsed changes.
        def freeze(body):
            return FIELD_RX.sub(lambda m: '' if m[1] in allowed else m[0], body)
        if freeze(previous['body']) != freeze(actual['body']):
            issues.append('Protected serialized content changed: ' + fid)
    report = {'path': rel, 'result': 'PASS' if not issues else 'FAIL', 'guardOnly': base['guardOnly'],
              'serializedObjects': len(old), 'classCounts': dict(Counter(b['class'] for b in old.values())),
              'objectIdsOrderClassesUnchanged': old_headers == new_headers,
              'visualChanges': len(changes) - start_changes, 'beforeSha256': sha(backup), 'afterSha256': sha(current),
              'issues': issues}
    prefab_reports.append(report)
    errors.extend(rel + ': ' + e for e in issues)

base_code = load('audit-code-hashes.json')
now_code = {p.relative_to(ROOT).as_posix(): sha(p) for p in (ROOT / 'Assets').rglob('*.cs')}
code_report = {'result': 'PASS' if base_code == now_code else 'FAIL', 'baselineCount': len(base_code),
               'currentCount': len(now_code), 'changed': [p for p in base_code if p in now_code and base_code[p] != now_code[p]],
               'added': sorted(set(now_code) - set(base_code)), 'missing': sorted(set(base_code) - set(now_code))}
if base_code != now_code:
    errors.append('C# source hashes or file set changed')

base_art = load('audit-art-hashes.json')
art_changes = [p for p, h in base_art.items() if not (ROOT / p).exists() or sha(ROOT / p) != h]
if art_changes:
    errors.append('Existing art or slot resources modified: ' + str(art_changes))

base_all = load('audit-all-prefab-hashes.json')
now_all = {p.relative_to(ROOT).as_posix(): sha(p) for p in (ROOT / 'Assets').rglob('*.prefab')}
editable = {p['path'] for p in baseline['prefabs'] if not p['guardOnly']}
outside_changes = [p for p, h in base_all.items() if p not in editable and now_all.get(p) != h]
new_prefabs = sorted(set(now_all) - set(base_all))
if outside_changes or new_prefabs:
    errors.append('Project prefab change outside approved three-prefab scope')

new_assets = []
for p in sorted((ROOT / 'Assets/OrchardUI/Art').rglob('*.png')):
    rel = p.relative_to(ROOT).as_posix()
    if rel in base_art:
        continue
    issues = []
    raw = p.read_bytes()
    png_valid = raw[:8] == b'\x89PNG\r\n\x1a\n'
    size = list(struct.unpack('>II', raw[16:24])) if png_valid else None
    if not png_valid:
        issues.append('Invalid PNG signature')
    meta_path = Path(str(p) + '.meta')
    meta = read(meta_path) if meta_path.exists() else ''
    def val(key):
        match = re.search(r'^\s*' + re.escape(key) + r': (.*)$', meta, re.M)
        return match[1] if match else None
    for key, want in [('textureType', '8'), ('enableMipMap', '0'), ('isReadable', '0'), ('alphaIsTransparency', '1')]:
        if val(key) != want:
            issues.append('Unexpected import ' + key + ': ' + str(val(key)))
    guid = val('guid')
    used = any(guid and guid in read(ROOT / b['path']) for b in baseline['prefabs'] if not b['guardOnly'])
    new_assets.append({'path': rel, 'sourceSize': size, 'guid': guid, 'spriteMode': val('spriteMode'),
                       'maxTextureSize': val('maxTextureSize'), 'referencedByApprovedPrefabs': used,
                       'sha256': sha(p), 'result': 'PASS' if not issues else 'FAIL', 'issues': issues})
    errors.extend(rel + ': ' + e for e in issues)

report = {
    'result': 'PASS' if not errors else 'FAIL',
    'scope': 'Independent parsed visual-field audit. Runtime visual and functional QA is separate.',
    'prefabCount': len(prefab_reports), 'serializedObjectCount': sum(p['serializedObjects'] for p in prefab_reports),
    'modifiedPrefabCount': sum(p['beforeSha256'] != p['afterSha256'] for p in prefab_reports),
    'visualFieldChanges': len(changes), 'allDetectedChangesAllowed': all(c['allowed'] for c in changes),
    'prefabs': prefab_reports, 'changes': changes,
    'frozenObjectCategories': dict(Counter(c['category'] for c in frozen_checks)),
    'allFrozenObjectsUnchanged': all(c['unchanged'] for c in frozen_checks), 'frozenChecks': frozen_checks,
    'codeHashes': code_report, 'existingArt': {'baselineCount': len(base_art), 'changed': art_changes},
    'projectPrefabGuard': {'baselineCount': len(base_all), 'currentCount': len(now_all),
                           'outsideScopeChanges': outside_changes, 'added': new_prefabs},
    'newSprites': new_assets, 'issues': errors,
    'limitations': ['Static serialized verification only; does not claim Unity runtime or real-device execution.',
                   'Existing data, text strings, localization keys, Button settings, RectTransforms, Spine fields and economy bindings must remain exactly equal to baseline.'],
}
dump('audit-allowlist.json', {'directFields': allowlist, 'nestedPolicy': 'Only the corresponding Image/TMP visual fields of baseline-resolved source objects; transform, component, button, text and business overrides are frozen.'})
dump('final-integrity.json', report)
print(json.dumps({'result': report['result'], 'prefabs': report['prefabCount'],
                  'objects': report['serializedObjectCount'], 'visualChanges': len(changes),
                  'frozen': report['frozenObjectCategories'], 'issues': errors}, ensure_ascii=False))
