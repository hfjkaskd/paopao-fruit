from pathlib import Path
import re, json

root = Path('C:/Projects/paopao/BizzaWZ')
out = root / 'Design/BottomHudImplementation-20260920'
paths = [
    'Assets/BizzaWZ/Final/Real/UI/SlotsPanel/SlotEnter/SlotEnter.prefab',
    'Assets/BizzaWZ/Final/Real/UI/GamePanel/Resources/Resources/GameUiWidget.prefab'
]
def blocks(text):
    return {(int(m.group(1)), m.group(2)):m.group(0) for m in re.finditer(r'(?ms)^--- !u!(\d+) &(\d+)(?: stripped)?\n.*?(?=^--- !u!|\Z)',text)}

checks = []
for path in paths:
    before = blocks((out / 'Before' / path).read_text())
    after = blocks((root / path).read_text())
    assert before.keys() == after.keys()
    changed = [k for k in before if before[k] != after[k]]
    assert all(k[0] not in [1, 224, 222] for k in changed)
    record = {'path':path, 'objectsUnchanged':len(before), 'nodeCount':sum(k[0]==1 for k in before),
              'allRectsAndHierarchyUnchanged':True, 'changedObjects':[{'class':k[0],'fileID':k[1]} for k in changed]}
    if path == paths[0]:
        assert set(changed) == {(114,'2597468376317513793'),(114,'7786913375694585872'),(114,'5078707581075184592')}
        for key in [(114,'7307305298524958330'),(114,'7089875217308296933'),(114,'5403240756379141583')]:
            assert before[key] == after[key]
        fill = after[(114,'7786913375694585872')]
        for field in ['m_Type: 3','m_FillMethod: 0','m_FillOrigin: 0','m_FillAmount: 0']:
            assert field in fill
        assert 'm_text: 0/5' in after[(114,'5078707581075184592')]
        record.update({'bindingAnd777IconUnchanged':True,'fillType':'Horizontal Filled from Left', 'defaultProgress':'0/5 matches 0.0'})
    checks.append(record)
runtime = (root/'Assets/BizzaWZ/Final/Real/UI/SlotsPanel/SlotEnter/SlotEnter.cs').read_text()
assert 'progressImag.fillAmount = (float)levelIndex / slotLimit;' in runtime
report = {'structuralChecks':checks, 'runtimeFormulaRetained':True,
          'fractions':[{'count':n,'required':5,'expectedFill':n/5} for n in [0,1,4,5]],
          'unityRenderPending':True,'note':'Static serialization check only; no Play Mode or advertisement/reward flow executed.'}
(out/'progress-structural-verification.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report,indent=2))
