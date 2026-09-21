from pathlib import Path
import re, json, hashlib

out = Path(__file__).resolve().parent
root = out.parents[1]
rel = 'Assets/BizzaWZ/Final/Real/UI/GamePanel/Resources/Resources/GameUiWidget.prefab'
target = root / rel
before = target.read_bytes()
baseline = (out / 'Before' / rel).read_bytes()
assert before == baseline, 'Production prefab changed since this turn baseline; inspect before applying'
newline = '\r\n' if b'\r\n' in before else '\n'
text = before.decode('utf-8-sig').replace('\r\n', '\n')
proposal = json.loads((out / 'hud-layout-proposal.json').read_text(encoding='utf-8'))
art = json.loads((out / 'Art/import.json').read_text(encoding='utf-8'))
guid = proposal['sourceGuid']
instance = proposal['instanceFileID']
pattern = rf'(?ms)^--- !u!1001 &{instance}\n.*?(?=^--- !u!|\Z)'
match = re.search(pattern, text)
assert match
original_block = match[0]
block = original_block
edits = list(proposal['recommendedOverrides'])
edits += [
    {'sourceFileID':'5821051965148166099','propertyPath':'m_Sprite','objectReference':f"{{fileID: 21300000, guid: {art['guid']}, type: 3}}"},
    {'sourceFileID':'5821051965148166099','propertyPath':'m_Type','value':0},
    {'sourceFileID':'5821051965148166099','propertyPath':'m_PreserveAspect','value':1},
    {'sourceFileID':'5860728120506579172','propertyPath':'m_sharedMaterial','objectReference':'{fileID: 2100000, guid: 486d0a25be512df4887a3bf53db492b8, type: 2}'},
    {'sourceFileID':'5860728120506579172','propertyPath':'m_fontColor.r','value':0.09019608},
    {'sourceFileID':'5860728120506579172','propertyPath':'m_fontColor.g','value':0.30980393},
    {'sourceFileID':'5860728120506579172','propertyPath':'m_fontColor.b','value':0.49019608},
]
changes = []
seen = set()
for edit in edits:
    fid, key = edit['sourceFileID'], edit['propertyPath']
    assert (fid,key) not in seen, (fid,key)
    seen.add((fid,key))
    assert key.startswith(('m_AnchoredPosition.', 'm_SizeDelta.', 'm_AnchorMin.', 'm_AnchorMax.',
                           'm_Pivot.', 'm_Padding.', 'm_margin.', 'm_font', 'm_enable', 'm_Spacing', 'm_Child',
                           'm_Sprite', 'm_Type', 'm_PreserveAspect', 'm_sharedMaterial')), key
    value = str(edit.get('value',''))
    obj = edit.get('objectReference','{fileID: 0}')
    start = f'    - target: {{fileID: {fid}, guid: {guid}, type: 3}}\n      propertyPath: {key}\n'
    expr = re.escape(start) + r'      value: ([^\n]*)\n      objectReference: ([^\n]*)\n'
    old = re.search(expr, block)
    replacement = start + f'      value: {value}\n      objectReference: {obj}\n'
    if old:
        if old[1] == value and old[2] == obj:
            continue
        changes.append({'fileID':fid,'propertyPath':key,'before':{'value':old[1],'objectReference':old[2]},'after':{'value':value,'objectReference':obj}})
        block = block[:old.start()] + replacement + block[old.end():]
    else:
        assert '    m_RemovedComponents:' in block
        changes.append({'fileID':fid,'propertyPath':key,'before':'inherited','after':{'value':value,'objectReference':obj}})
        block = block.replace('    m_RemovedComponents:', replacement + '    m_RemovedComponents:', 1)
after_text = text[:match.start()] + block + text[match.end():]
after = after_text.replace('\n',newline).encode('utf-8')
if before.startswith(b'\xef\xbb\xbf'):
    after = b'\xef\xbb\xbf' + after
target.write_bytes(after)
record = {'file':rel,'scope':'Only main GameUiWidget nested CurrencyBar visual and layout overrides',
          'beforeSha256':hashlib.sha256(before).hexdigest(),'afterSha256':hashlib.sha256(after).hexdigest(),
          'lineEnding':repr(newline),'changes':changes}
(out / 'applied-changes.json').write_text(json.dumps(record,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'changes':len(changes),'prefabsChanged':1,'afterSha256':record['afterSha256']}))
