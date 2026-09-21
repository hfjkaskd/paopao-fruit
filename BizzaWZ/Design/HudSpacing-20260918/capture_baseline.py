from pathlib import Path
import hashlib, json

root = Path(__file__).resolve().parents[2]
out = Path(__file__).resolve().parent
paths = [
    'Assets/BizzaWZ/Final/Real/UI/GamePanel/Resources/Resources/GameUiWidget.prefab',
    'Assets/BizzaWZ/Final/MenuSystem/Common/CurrencyBar/CurrencyBar.prefab',
    'Assets/BizzaWZ/Common/UI/GamePanel/RealGamePanel.prefab',
    'Assets/BizzaWZ/Common/Framework/GameCanvas.prefab',
]
items = []
for rel in paths:
    data = (root / rel).read_bytes()
    dest = out / 'Before' / rel
    dest.parent.mkdir(parents=True, exist_ok=True)
    with dest.open('xb') as f:
        f.write(data)
    items.append({'path': rel, 'sha256': hashlib.sha256(data).hexdigest()})
code = {p.relative_to(root).as_posix(): hashlib.sha256(p.read_bytes()).hexdigest()
        for p in (root / 'Assets').rglob('*.cs')}
with (out / 'baseline.json').open('x', encoding='utf-8') as f:
    json.dump({'prefabs': items, 'code': code}, f, ensure_ascii=False, indent=2)
print(json.dumps({'prefabs': len(items), 'scripts': len(code)}))
