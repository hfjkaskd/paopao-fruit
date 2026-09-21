from pathlib import Path
import re, hashlib, json, zipfile

ROOT = Path('C:/Projects/DJS_Apple/FruitsHarvestMaster-source')
OUT = Path(__file__).resolve().parent
changes = {}
prefab = 'output_Unity/Assets/res/local/home/Home.prefab'
original = (ROOT / prefab).read_bytes()
text = original.decode('utf-8')
matches = list(re.finditer(r'^--- !u!(\d+) &(\d+)[^\n]*\n.*?(?=^--- !u!|\Z)', text, re.M | re.S))
blocks = {m[2]: m[0] for m in matches}
removed = set()
def remove_game_object(go):
    removed.add(go)
    components = re.findall(r'component: \{fileID: (\d+)\}', blocks[go])
    removed.update(components)
    transforms = [c for c in components if blocks[c].startswith(('--- !u!224 ', '--- !u!4 '))]
    assert len(transforms) == 1
    tr = blocks[transforms[0]]
    children = re.search(r'  m_Children:(.*?)  m_Father:', tr, re.S)[1]
    for child in re.findall(r'fileID: (\d+)', children):
        child_go = re.search(r'm_GameObject: \{fileID: (\d+)\}', blocks[child])[1]
        remove_game_object(child_go)

remove_game_object('1161607090078389')
new = text[:matches[0].start()] + ''.join(m[0] for m in matches if m[2] not in removed)
new, n = re.subn(r'(?m)^  - \{fileID: 224150646884989824\}\r?\n', '', new)
assert n == 1
new, n = re.subn(r'(?m)^  m_NoADBtn: \{fileID: 1161607090078389\}\r?\n', '', new)
assert n == 1
for fid in removed:
    assert not re.search(r'fileID: ' + fid + r'\b', new), fid
assert 'NoAdBtn' not in new and 'WithdrawalHud' not in new
changes[prefab] = new.encode('utf-8')

rel = 'output_Unity/Assets/Scripts/Assembly-CSharp/HomeUI.cs'
data = (ROOT / rel).read_bytes()
for pattern in (
    rb'\t\[SerializeField\]\r?\n\tprivate GameObject m_NoADBtn;\r?\n\r?\n',
    rb'\t\tif \(m_NoADBtn != null\)\r?\n\t\t\{\r?\n\t\t\tMCCIJBJGMCK.Get\(m_NoADBtn\).onClick = OnNoADBtnClick;\r?\n\t\t\}\r?\n',
    rb'\t\tRefreshADBtnState\(\);\r?\n',
    rb'\tprivate void OnNoADBtnClick\(GameObject InGameObject\)\r?\n\t\{\r?\n\t\tMgrUI.Instance.Open\("pops/removead/RemoveADUI"\);\r?\n\t\}\r?\n\r?\n',
    rb'\tprivate void RefreshADBtnState\(\)\r?\n\t\{\r?\n\t\tif \(m_NoADBtn != null\)\r?\n\t\t\{\r?\n\t\t\tm_NoADBtn.SetActive\(true\);\r?\n\t\t\}\r?\n\t\}\r?\n\r?\n',
):
    data, n = re.subn(pattern, b'', data)
    assert n == 1, pattern
changes[rel] = data

for name in ('Home_Open', 'Home_Close'):
    rel = 'output_Unity/Assets/res/local/home/ani/' + name + '.anim'
    data = (ROOT / rel).read_bytes()
    matches = list(re.finditer(rb'^  - serializedVersion: 2\r?\n.*?(?=^  - |^  m_|\Z)', data, re.M | re.S))
    targets = [m for m in matches if b'path: UI/Top/NoAdBtn' in m[0]]
    assert len(targets) == 1
    m = targets[0]
    changes[rel] = data[:m.start()] + data[m.end():]
    assert b'NoAdBtn' not in changes[rel]

items = []
with zipfile.ZipFile(OUT / 'before-removal.zip', 'w', zipfile.ZIP_DEFLATED) as backup:
    for rel, data in changes.items():
        backup.write(ROOT / rel, rel)
        stage = OUT / 'staged' / rel
        stage.parent.mkdir(parents=True, exist_ok=True)
        stage.write_bytes(data)
        items.append({'path': rel, 'action': 'replace', 'originalSha256': hashlib.sha256((ROOT / rel).read_bytes()).hexdigest(), 'stagedSha256': hashlib.sha256(data).hexdigest(), 'stagedPath': str(stage)})
(OUT / 'manifest.json').write_text(json.dumps({'root': str(ROOT), 'backup': str(OUT / 'before-removal.zip'), 'items': items}, indent=2), encoding='utf-8')
(OUT / 'removed-fileids.json').write_text(json.dumps(sorted(removed)), encoding='utf-8')
print(json.dumps({'updatedFiles': len(items), 'prefabObjectsRemoved': len(removed), 'animationCurvesRemoved': 2}))
