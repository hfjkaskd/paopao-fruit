"""Independent read-only validation against the immutable start-of-turn baseline.

Only the two approved prefabs may change. Permitted edits are Image/TMP style,
RectTransform geometry and existing layout component geometry. Hierarchy,
component records, interactions, text/localization, game state and C# are frozen.
No Unity automation or rendering is performed. Reports go only beside this file.
"""
from pathlib import Path
from collections import Counter
import datetime
import hashlib
import json
import re

OUT = Path(__file__).resolve().parent
ROOT = OUT.parents[2]
BASE = json.loads((OUT / 'baseline.json').read_text(encoding='utf-8'))
EDITABLE = set(BASE['editable'])
IMAGE_GUID = 'fe87c0e1cc204ed48ad3b37840f39efc'
TMP_GUID = 'f4688fdb7df04437aeb418b961361dc5'
LAYOUT_GUIDS = {'59f8146938fff824cb5fd77236b75775', '30649d3a9faa99c48a7b1166b86bf2a0'}
IMAGE_FIELDS = {'m_Sprite', 'm_Color', 'm_Type', 'm_PreserveAspect',
                'm_PixelsPerUnitMultiplier', 'm_FillCenter', 'm_Material'}
TMP_FIELDS = {'m_fontColor', 'm_fontColor32', 'm_sharedMaterial', 'm_fontAsset',
              'm_fontSize', 'm_fontSizeBase', 'm_fontSizeMin', 'm_fontSizeMax',
              'm_enableAutoSizing', 'm_fontStyle', 'm_fontWeight', 'm_margin',
              'm_characterSpacing', 'm_wordSpacing', 'm_lineSpacing',
              'm_paragraphSpacing', 'm_HorizontalAlignment', 'm_VerticalAlignment',
              'm_textAlignment', 'm_enableWordWrapping', 'm_overflowMode'}
RECT_FIELDS = {'m_AnchoredPosition', 'm_SizeDelta', 'm_AnchorMin', 'm_AnchorMax',
               'm_Pivot', 'm_LocalPosition', 'm_LocalScale'}
LAYOUT_FIELDS = {'m_Padding', 'm_ChildAlignment', 'm_Spacing',
                 'm_ChildForceExpandWidth', 'm_ChildForceExpandHeight',
                 'm_ChildControlWidth', 'm_ChildControlHeight',
                 'm_ChildScaleWidth', 'm_ChildScaleHeight'}
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


def blocks(text):
    return {m[2]: {'class': m[1], 'stripped': bool(m[3]), 'body': m[4]}
            for m in BLOCK_RX.finditer(text)}


def script(block):
    m = re.search(r'^  m_Script: \{fileID: \d+, guid: (\w+), type: 3\}$', block['body'], re.M)
    return m[1] if m else None


def allowed(block):
    if block is None or block['stripped']:
        return set()
    if block['class'] == '224':
        return RECT_FIELDS
    if block['class'] != '114':
        return set()
    guid = script(block)
    if guid == IMAGE_GUID:
        return IMAGE_FIELDS
    if guid == TMP_GUID:
        return TMP_FIELDS
    return LAYOUT_FIELDS if guid in LAYOUT_GUIDS else set()


def fields(body):
    return {m[1]: m[0] for m in FIELD_RX.finditer(body)}


def category(block):
    if block['class'] == '1':
        return 'GameObject identity, hierarchy and component list'
    if block['class'] == '224':
        return 'RectTransform protected parent/children/rotation'
    if block['class'] == '4':
        return 'Transform'
    if block['class'] == '225':
        return 'CanvasGroup'
    if block['class'] == '114':
        body = block['body']
        if '  m_OnClick:' in body or '  onClick:' in body:
            return 'Button settings and event bindings'
        if '  skeletonDataAsset:' in body:
            return 'Spine animation'
        if re.search(r'^  key:', body, re.M):
            return 'Localization binding'
        if script(block) == IMAGE_GUID:
            return 'Image protected behavior fields'
        if script(block) == TMP_GUID:
            return 'TMP text content and protected behavior'
        return 'Other component or business binding'
    return 'Other serialized object'


baseline_blocks = {}
guid_paths = {}
for item in BASE['slotPrefabs']:
    rel = item['path']
    path = OUT / 'Before' / rel
    baseline_blocks[rel] = blocks(read(path))
    match = re.search(r'^guid: (\w+)$', read(Path(str(path) + '.meta')), re.M)
    if match:
        guid_paths[match[1]] = rel


def override_allowed(fid, guid, prop):
    source = baseline_blocks.get(guid_paths.get(guid), {}).get(fid)
    return prop.split('.')[0] in allowed(source)


def split_overrides(body):
    records, dup = {}, []
    for m in OVERRIDE_RX.finditer(body):
        fid, guid, prop, value, reference = m.groups()
        token = (fid, guid, prop)
        if token in records:
            dup.append(token)
        records[token] = {'value': value.strip(), 'objectReference': reference}
    frozen = OVERRIDE_RX.sub(lambda m: '' if override_allowed(m[1], m[2], m[3]) else m[0], body)
    return records, dup, frozen


errors, changes, reports = [], [], []
protected_counts = Counter()
for item in BASE['slotPrefabs']:
    rel = item['path']
    old_path, new_path = OUT / 'Before' / rel, ROOT / rel
    issues = []
    if sha(old_path) != item['sha256']:
        issues.append('Baseline copy hash changed')
    if not new_path.exists():
        errors.append(rel + ': Prefab missing')
        continue
    old, new = baseline_blocks[rel], blocks(read(new_path))
    old_headers = [(fid, b['class'], b['stripped']) for fid, b in old.items()]
    new_headers = [(fid, b['class'], b['stripped']) for fid, b in new.items()]
    if old_headers != new_headers:
        issues.append('Serialized object IDs, order or classes changed')
    if rel not in EDITABLE and read(old_path) != read(new_path):
        issues.append('Read-only source Prefab changed')
    start = len(changes)
    for fid, previous in old.items():
        actual = new.get(fid)
        if actual is None:
            continue
        protected_counts[category(previous)] += 1
        if previous == actual:
            continue
        if previous['class'] == '1001' and rel in EDITABLE:
            before, bd, frozen_before = split_overrides(previous['body'])
            after, ad, frozen_after = split_overrides(actual['body'])
            if ad != bd:
                issues.append('New duplicate overrides: ' + fid)
            if frozen_before != frozen_after:
                issues.append('Protected nested prefab fields changed: ' + fid)
            for token in sorted(set(before) | set(after)):
                if before.get(token) == after.get(token):
                    continue
                target, guid, prop = token
                ok = override_allowed(target, guid, prop)
                changes.append({'path': rel, 'instanceID': fid, 'targetID': target,
                                'targetGuid': guid, 'kind': 'nestedOverride', 'field': prop,
                                'before': before.get(token), 'after': after.get(token), 'allowed': ok})
                if not ok:
                    issues.append('Nested override not visual: ' + target + '/' + prop)
            continue
        permit = allowed(previous) if rel in EDITABLE else set()
        before, after = fields(previous['body']), fields(actual['body'])
        for field in sorted(set(before) | set(after)):
            if before.get(field) == after.get(field):
                continue
            ok = field in permit
            changes.append({'path': rel, 'fileID': fid, 'kind': 'direct', 'field': field,
                            'before': before.get(field), 'after': after.get(field), 'allowed': ok})
            if not ok:
                issues.append('Field outside visual allowlist: ' + fid + '/' + field)
        def frozen(body):
            return FIELD_RX.sub(lambda m: '' if m[1] in permit else m[0], body)
        if frozen(previous['body']) != frozen(actual['body']):
            issues.append('Protected content changed: ' + fid)
    reports.append({'path': rel, 'result': 'PASS' if not issues else 'FAIL',
                    'serializedObjects': len(old), 'objectIdsOrderClassesUnchanged': old_headers == new_headers,
                    'visualFieldChanges': len(changes) - start, 'issues': issues})
    errors += [rel + ': ' + issue for issue in issues]

now_prefabs = {p.relative_to(ROOT).as_posix(): sha(p) for p in (ROOT / 'Assets').rglob('*.prefab')}
outside = [p for p, h in BASE['prefabs'].items() if p not in EDITABLE and now_prefabs.get(p) != h]
new_prefabs = sorted(set(now_prefabs) - set(BASE['prefabs']))
if outside or new_prefabs:
    errors.append('Project Prefabs outside two-page scope changed or were added')
now_code = {p.relative_to(ROOT).as_posix(): sha(p) for p in (ROOT / 'Assets').rglob('*.cs')}
code_changed = [p for p, h in BASE['code'].items() if now_code.get(p) != h]
code_added = sorted(set(now_code) - set(BASE['code']))
if code_changed or code_added:
    errors.append('C# sources changed or were added')

report = {
    'generatedUtc': datetime.datetime.now(datetime.timezone.utc).isoformat(),
    'result': 'PASS' if not errors else 'FAIL',
    'approvedPrefabResult': 'PASS' if all(p['result'] == 'PASS' for p in reports) else 'FAIL',
    'existingCodeResult': 'PASS' if not code_changed else 'FAIL',
    'scope': 'Independent serialized structure and source audit; no runtime visual/function claim.',
    'prefabCount': len(reports), 'serializedObjectCount': sum(p['serializedObjects'] for p in reports),
    'visualFieldChanges': len(changes), 'allDetectedChangesAllowed': all(c['allowed'] for c in changes),
    'protectedCounts': dict(protected_counts), 'prefabs': reports, 'changes': changes,
    'projectPrefabGuard': {'baselineCount': len(BASE['prefabs']), 'currentCount': len(now_prefabs),
                           'outsideScopeChanges': outside, 'added': new_prefabs},
    'codeGuard': {'baselineCount': len(BASE['code']), 'currentCount': len(now_code),
                  'changedOrMissing': code_changed, 'added': code_added},
    'issues': errors,
    'limitations': ['Does not launch, stop, navigate, capture or otherwise control Unity or the desktop.',
                   'Permits local visual geometry only; does not assert pixel similarity to AI reference.',
                   'All text content, localization, reward scripts, event bindings, hierarchy and component identity are frozen.',
                   'Workspace-wide added files or changed unrelated prefabs are reported verbatim; this audit does not infer who authored them or revert concurrent work.'],
}
(OUT / 'final-integrity.json').write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
print(json.dumps({k: report[k] for k in ('result', 'prefabCount', 'serializedObjectCount', 'visualFieldChanges', 'protectedCounts', 'issues')}, ensure_ascii=False))
raise SystemExit(0 if not errors else 1)
