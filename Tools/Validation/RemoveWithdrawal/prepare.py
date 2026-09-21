from pathlib import Path
import hashlib, json, re, subprocess, zipfile

ROOT = Path('C:/Projects/DJS_Apple/FruitsHarvestMaster-source').resolve()
OUT = Path(__file__).resolve().parent
STAGE = OUT / 'staged'
STAGE.mkdir(exist_ok=True)

def git(*args):
    return subprocess.check_output(['git', '-c', 'safe.directory=' + ROOT.as_posix(), '-C', str(ROOT), *args])

def sha(data):
    return hashlib.sha256(data).hexdigest()

prefixes = (
    'Tests/Withdrawal/', 'Tools/MinimizeWithdrawalHudPatch.py',
    'output_Unity/Assets/Editor/WithdrawalUI',
    'output_Unity/Assets/Resources/WithdrawalConfig.asset',
    'output_Unity/Assets/Resources/WithdrawalImported',
    'output_Unity/Assets/Scripts/Assembly-CSharp/Withdrawal',
    'output_Unity/Assets/WithdrawalImported',
    'output_Unity/Assets/res/local/withdrawal',
    'output_Unity/Docs/WithdrawalPort/',
    'output_Unity/Tools/ImportNutWithdrawal.py',
    'output_Unity/Tools/NutWithdrawalBindings.json',
    'output_Unity/Tools/NutWithdrawalImportManifest.json',
)
added = git('diff', '--name-only', '--diff-filter=A', '8101930', 'HEAD').decode().splitlines()
assert added and all(p.startswith(prefixes) for p in added)
changes = {}

# These three files only gained the simulation entry/catalog links. Verify that
# current data still matches HEAD before restoring their pre-integration content.
for rel in (
    'output_Unity/Assets/res/local/home/Home.prefab',
    'output_Unity/Assets/res/local/coreplay/CorePlayUI.prefab',
    'output_Unity/Assets/Resources/GameResCatalog.asset',
):
    changes[rel] = git('show', '8101930:' + rel)

for rel, expected in (
    ('output_Unity/Assets/Scripts/Assembly-CSharp/GameEntry.cs', 2),
    ('output_Unity/Assets/Scripts/Assembly-CSharp/EDLHEMMBABM.cs', 4),
):
    data = (ROOT / rel).read_bytes()
    data, count = re.subn(rb'(?m)^[\t ]*FruitWithdrawal\.WithdrawalGameplayBridge\.[^\r\n]*\r?\n', b'', data)
    assert count == expected, (rel, count)
    data = re.sub(rb'(?m)^[\t ]*// Save the reward against the existing attempt before LevelWin replaces its data\.\r?\n', b'', data)
    changes[rel] = data

rel = 'output_Unity/Assets/Scripts/Assembly-CSharp/CorePlay/LevelData.cs'
data = (ROOT / rel).read_bytes()
data, n = re.subn(rb'(?m)^\t\t// One identifier per attempt, retained when the board snapshot is restored\.\r?\n\t\tpublic string withdrawalRunId;\r?\n\r?\n', b'', data)
assert n == 1
data, n = re.subn(rb'(?m)^\t\t\twithdrawalRunId = null;\r?\n', b'', data)
assert n == 1
changes[rel] = data

items = []
for rel in sorted(set(added) | set(changes)):
    target = (ROOT / rel).resolve()
    assert target.is_relative_to(ROOT) and '.git' not in target.relative_to(ROOT).parts
    original = target.read_bytes()
    # All removal targets are tracked and unmodified. Never discard concurrent edits.
    assert original.replace(b'\r\n', b'\n') == git('show', 'HEAD:' + rel).replace(b'\r\n', b'\n'), rel
    entry = {'path': rel, 'originalSha256': sha(original), 'action': 'replace' if rel in changes else 'delete'}
    if rel in changes:
        staged = STAGE / rel
        staged.parent.mkdir(parents=True, exist_ok=True)
        staged.write_bytes(changes[rel])
        entry['stagedPath'] = str(staged)
        entry['stagedSha256'] = sha(changes[rel])
    items.append(entry)

backup = OUT / 'before-removal.zip'
with zipfile.ZipFile(backup, 'w', zipfile.ZIP_DEFLATED) as z:
    for item in items:
        z.write(ROOT / item['path'], item['path'])

manifest = {'root': str(ROOT), 'backup': str(backup), 'items': items}
(OUT / 'manifest.json').write_text(json.dumps(manifest, indent=2), encoding='utf-8')
print(json.dumps({'replace': len(changes), 'delete': len(added), 'backupBytes': backup.stat().st_size, 'manifest': str(OUT / 'manifest.json')}))
