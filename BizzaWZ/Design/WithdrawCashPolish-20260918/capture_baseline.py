from pathlib import Path
import json, hashlib
out=Path(__file__).resolve().parent
project=out.parents[1]
paths=[
 'Assets/BizzaWZ/Final/Real/UI/FakeWithdrawPanel/FakeWithdrawPanel.prefab',
 'Assets/BizzaWZ/Final/Real/UI/FakeWithdrawPanel/WithdrawAmountItem.prefab',
 'Assets/BizzaWZ/Final/Real/UI/FakeWithdrawPanel/FakeWithdrawPanel.cs',
 'Assets/BizzaWZ/Final/Real/UI/FakeWithdrawPanel/WithdrawAmountItem.cs',
 'Assets/OrchardUI/Art/Controls.png.meta',
 'Assets/OrchardUI/Art/HudNaturalCounter.png.meta',
 'Assets/OrchardUI/Art/HudNaturalGreenButton.png.meta',
]
items=[]
for rel in paths:
 data=(project/rel).read_bytes()
 dest=out/'Before'/rel
 dest.parent.mkdir(parents=True,exist_ok=True)
 with dest.open('xb') as f:f.write(data)
 items.append({'path':rel,'sha256':hashlib.sha256(data).hexdigest()})
with (out/'baseline.json').open('x',encoding='utf-8') as f:json.dump(items,f,indent=2)
print('Saved exact current baseline for '+str(len(items))+' scoped files.')
