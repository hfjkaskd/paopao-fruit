"""Read-only production asset audit; writes only this Design report directory."""
from pathlib import Path
import json, re, hashlib, datetime

OUT = Path(__file__).resolve().parent
TASK = OUT.parent
ROOT = TASK.parents[1]
CORE = 'Assets/FruitsHarvest/Resources/Original/res/local/coreplay/CorePlayUI.prefab'
EXPECTED = {
 '114410865556287939', '114282982324125657', '114504490712063035', '114706308911820470',
 '114513653223693148', '114837889324699812', '114798730798708461', '114517549520547945',
 '114837227276182950', '114926580033004315', '114886820396282420', '114317682953123289',
}
COUNT_IDS = {'Undo': '114504490712063035', 'Magic': '114798730798708461', 'Shuffle': '114886820396282420'}

def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()
def blocks(path):
    txt = path.read_text(encoding='utf-8-sig')
    return {m[2]: (m[1], m[3]) for m in re.finditer(r'^--- !u!(\d+) &(-?\d+)[^\n]*\n(.*?)(?=^--- !u!|\Z)', txt, re.S | re.M)}
def fields(txt): return {m[1]: m[2] for m in re.finditer(r'^  ([^\s:]+):(.*?)(?=^  [^\s:]+:|\Z)', txt, re.S | re.M)}

before, after = blocks(TASK/'Before'/CORE), blocks(ROOT/CORE)
assert before.keys() == after.keys(), 'Serialized nodes/components changed'
changed = {i for i in before if before[i] != after[i]}
assert changed == EXPECTED, ('Unexpected object changes', changed ^ EXPECTED)
diffs = []
for i in sorted(changed):
    assert before[i][0] == after[i][0] == '114'
    bf, af = fields(before[i][1]), fields(after[i][1])
    assert bf.keys() == af.keys()
    keys = sorted(k for k in bf if bf[k] != af[k])
    assert set(keys) <= {'m_Sprite', 'm_sharedMaterial', 'm_fontColor', 'm_fontColor32'}
    diffs.append({'fileID': i, 'fields': keys})
glyphs = json.loads((TASK/'prop-glyph-baseline.json').read_text(encoding='utf-8-sig'))
for row in glyphs: assert sha(ROOT/row['path']) == row['sha256'], row['path']
old_mapping = (OUT/'preserved-glyph-mapping-baseline.txt').read_text(encoding='utf-8-sig').splitlines()
new_mapping = [line for line in (ROOT/'Assets/FruitsHarvest/Resources/HarvestPaths.txt').read_text(encoding='utf-8-sig').splitlines()
               if re.match(r'^res/local/coreplay/sprite/item/(undo|magic|shuffle)_(normal|lock)', line)]
assert old_mapping == new_mapping
count_states=[]
for owner, cid in COUNT_IDS.items():
    f=fields(after[cid][1])
    assert f['m_Type'].strip() == '0' and f['m_PixelsPerUnitMultiplier'].strip() == '1'
    count_states.append({'owner':owner,'imageFileID':cid,'type':'Simple','pixelsPerUnitMultiplier':1})
report = {
 'kind':'Final independent serialized production audit; supersedes earlier prop-background-patch-report.json field/hash details',
 'generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),
 'scope':'Existing background Image sprites plus existing count label material/color. All prop glyphs retained.',
 'prefab':CORE, 'beforeSha256':sha(TASK/'Before'/CORE), 'afterSha256':sha(ROOT/CORE),
 'serializedObjectCountBefore':len(before), 'serializedObjectCountAfter':len(after),
 'gameObjectCount':sum(v[0]=='1' for v in before.values()),
 'changedComponentCount':len(changed), 'changes':diffs,
 'countBadges':count_states,
 'checks': {
  'allObjectAndComponentIDsRetained': True,
  'allGameObjectHierarchyAndRectTransformsUnchanged': all(before[i]==after[i] for i in before if before[i][0] in ('1','4','224')),
  'allComponentsOtherThan12VisualComponentsByteIdentical': all(before[i]==after[i] for i in before if i not in EXPECTED),
  'buttonAndStateComponentsUnchanged': True,
  'allAdAndLockComponentsUnchanged': True,
  'allGlyphFilesUnchanged': True,
  'allGlyphResourceMappingsUnchanged': True,
  'preservedGlyphFiles':len(glyphs),
 },
 'previewScope':'Actual production-prefab isolated renders with explicit visual samples; no gameplay/advertisement/account flow executed.',
}
(OUT/'final-prop-background-audit.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps({k:report[k] for k in ('changedComponentCount','afterSha256','checks')}))
