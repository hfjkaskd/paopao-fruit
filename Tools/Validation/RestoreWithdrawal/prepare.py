from pathlib import Path
import hashlib, json, zipfile

HERE = Path(__file__).resolve().parent
old = json.loads((HERE.parent / 'RemoveWithdrawal/manifest.json').read_text())
root = Path(old['root'])
home = 'output_Unity/Assets/res/local/home/Home.prefab'
hash_bytes = lambda data: hashlib.sha256(data).hexdigest()
items = []
with zipfile.ZipFile(old['backup']) as source, zipfile.ZipFile(HERE / 'before-restore.zip', 'w', zipfile.ZIP_DEFLATED) as backup:
    for entry in old['items']:
        rel = entry['path']
        if rel == home:
            continue
        target = root / rel
        if entry['action'] == 'delete':
            assert not target.exists(), 'Recreated file: ' + rel
            original_hash = None
        else:
            original_hash = hash_bytes(target.read_bytes())
            assert original_hash == entry['stagedSha256'], 'Concurrent edit: ' + rel
            backup.write(target, rel)
        data = source.read(rel)
        assert hash_bytes(data) == entry['originalSha256'], 'Invalid backup: ' + rel
        if rel == 'output_Unity/Assets/Editor/WithdrawalUIAuthoring.cs':
            before = b'AttachHud("Assets/res/local/home/Home.prefab", hudPrefab); AttachHud("Assets/res/local/coreplay/CorePlayUI.prefab", hudPrefab);'
            assert data.count(before) == 1
            data = data.replace(before, b'AttachHud("Assets/res/local/coreplay/CorePlayUI.prefab", hudPrefab);')
        if rel == 'Tools/MinimizeWithdrawalHudPatch.py':
            before = b'    "output_Unity/Assets/res/local/home/Home.prefab",\n'
            assert data.count(before) == 1
            data = data.replace(before, b'')
        if rel == 'output_Unity/Docs/WithdrawalPort/README.md':
            text = data.decode('utf-8-sig')
            text = text.replace('入口：首页的现金／钻石栏；关卡内左下角带 TEST 标签的钱图标。', '入口：仅保留关卡内左下角带 TEST 标签的钱图标；首页现金／钻石测试栏与 ADS 按钮已移除。')
            text = text.replace('`Home.prefab` 与 `CorePlayUI.prefab` 原有 YAML 文本', '`CorePlayUI.prefab` 原有 YAML 文本')
            data = text.encode('utf-8')
        stage = HERE / 'staged' / rel
        stage.parent.mkdir(parents=True, exist_ok=True)
        stage.write_bytes(data)
        items.append({'path': rel, 'action': 'create' if original_hash is None else 'replace', 'originalSha256': original_hash, 'stagedSha256': hash_bytes(data), 'stagedPath': str(stage)})

guards = []
ads = json.loads((HERE.parent / 'RemoveNoAds/manifest.json').read_text())
for entry in ads['items']:
    target = root / entry['path']
    assert hash_bytes(target.read_bytes()) == entry['stagedSha256']
    guards.append({'path': entry['path'], 'sha256': entry['stagedSha256']})
(HERE / 'manifest.json').write_text(json.dumps({'root': str(root), 'backup': str(HERE / 'before-restore.zip'), 'items': items, 'unchangedGuards': guards}, indent=2), encoding='utf-8')
print(json.dumps({'restoredFiles': len(items), 'protectedHomepageAndAdsFiles': len(guards)}))
