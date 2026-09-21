from pathlib import Path
import hashlib, json, re
from datetime import datetime, timezone

root = Path(__file__).resolve().parents[2]
out = Path(__file__).resolve().parent
slot_root = root / 'Assets/BizzaWZ/Final/Real/UI/SlotsPanel'

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def write_new(path, data):
    with path.open('x', encoding='utf-8') as stream:
        json.dump(data, stream, ensure_ascii=False, indent=2)
        stream.write('\n')

if (out / 'audit-baseline.json').exists():
    raise FileExistsError('The immutable baseline already exists; refusing to overwrite it.')

meta_index = {}
for p in (root / 'Assets').rglob('*.prefab.meta'):
    match = re.search(r'^guid: (\w+)$', p.read_text(encoding='utf-8-sig'), re.M)
    if match:
        meta_index[match[1]] = Path(str(p)[:-5])

paths = set(slot_root.rglob('*.prefab'))
queue = list(paths)
while queue:
    source = queue.pop()
    text = source.read_text(encoding='utf-8-sig')
    for guid in re.findall(r'm_SourcePrefab: \{fileID: \d+, guid: (\w+), type: 3\}', text):
        target = meta_index.get(guid)
        if target is not None and target not in paths:
            paths.add(target)
            queue.append(target)

editable = {
    'Assets/BizzaWZ/Final/Real/UI/SlotsPanel/SlotPanel/SlotPanel.prefab',
    'Assets/BizzaWZ/Final/Real/UI/SlotsPanel/SlotFQAPanel/SlotFQAPanel.prefab',
    'Assets/BizzaWZ/Final/Real/UI/SlotsPanel/BingoAsset/Assets/UiPrefab/SlotMachineGroup.prefab',
}
prefabs = []
for source in sorted(paths):
    rel = source.relative_to(root).as_posix()
    dest = out / 'Before' / rel
    dest.parent.mkdir(parents=True, exist_ok=True)
    raw = source.read_bytes()
    with dest.open('xb') as stream:
        stream.write(raw)
    text = raw.decode('utf-8-sig').replace('\r\n', '\n')
    objects = re.findall(r'^--- !u!(\d+) &(-?\d+)( stripped)?$', text, re.M)
    prefabs.append({'path': rel, 'sha256': hashlib.sha256(raw).hexdigest(),
                    'guardOnly': rel not in editable,
                    'serializedObjects': [{'class': c, 'fileID': i, 'stripped': bool(s)} for c, i, s in objects]})

code = {p.relative_to(root).as_posix(): sha(p) for p in sorted((root / 'Assets').rglob('*.cs'))}
write_new(out / 'audit-code-hashes.json', code)

art_folders = ['Assets/OrchardUI/Art', 'Assets/OrchardUI/Resources/OrchardUI',
               'Assets/BizzaWZ/Final/BizzaGame/Z_ReplaceAssets/UI_Frame/SlotPanel']
art_paths = {p for folder in art_folders for p in (root / folder).rglob('*') if p.is_file()}
art_paths.update(p for p in slot_root.rglob('*') if p.is_file() and not p.name.endswith(('.prefab', '.cs')))
art = {p.relative_to(root).as_posix(): sha(p) for p in sorted(art_paths)}
write_new(out / 'audit-art-hashes.json', art)
all_prefab_hashes = {p.relative_to(root).as_posix(): sha(p) for p in sorted((root / 'Assets').rglob('*.prefab'))}
write_new(out / 'audit-all-prefab-hashes.json', all_prefab_hashes)
write_new(out / 'audit-baseline.json', {
    'capturedUtc': datetime.now(timezone.utc).isoformat(),
    'scope': 'Approved slot-machine and slot-help art restyle. Only visual fields may change; all hierarchy, geometry, logic, buttons, Spine and localization bindings are frozen.',
    'codeFileCount': len(code), 'artFileCount': len(art), 'allProjectPrefabCount': len(all_prefab_hashes),
    'prefabs': prefabs,
})
print(json.dumps({'prefabsBackedUp': len(prefabs), 'csFiles': len(code), 'artFilesHashed': len(art),
                  'allProjectPrefabsHashed': len(all_prefab_hashes), 'output': str(out)}))
