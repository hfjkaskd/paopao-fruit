from pathlib import Path
import json, re, hashlib
out = Path(__file__).resolve().parent
root = out.parents[1]
p = root / 'Assets/BizzaWZ/Final/Real/UI/GamePanel/Resources/Resources/GameUiWidget.prefab'
data = p.read_bytes()
pattern = rb'(target: \{fileID: 8953712310009366182, guid: b1f781d8c27eec54db58a4849081f7e5, type: 3\}\r?\n      propertyPath: m_fontSizeMin\r?\n      value: )16(\r?\n)'
data, count = re.subn(pattern, lambda m: m[1] + b'12' + m[2], data)
assert count == 1
p.write_bytes(data)
record = json.loads((out/'applied-changes.json').read_text(encoding='utf-8'))
record['afterSha256'] = hashlib.sha256(data).hexdigest()
for change in record['changes']:
    if change['fileID'] == '8953712310009366182' and change['propertyPath'] == 'm_fontSizeMin':
        change['after']['value'] = '12'
record['refinement'] = 'Static Unity TMP preview revealed a long conversion price reaching the button rim at font minimum16. Lowered only its adaptive minimum to12; normal price retains auto-size22.55.'
(out/'applied-changes.json').write_text(json.dumps(record,ensure_ascii=False,indent=2),encoding='utf-8')
proposal = json.loads((out/'hud-layout-proposal.json').read_text(encoding='utf-8'))
for change in proposal['recommendedOverrides']:
    if change['sourceFileID'] == '8953712310009366182' and change['propertyPath'] == 'm_fontSizeMin':
        change['value'] = 12
(out/'hud-layout-proposal.json').write_text(json.dumps(proposal,ensure_ascii=False,indent=2),encoding='utf-8')
model = out/'analyze-hud-layout.py'
text = model.read_text(encoding='utf-8').replace('m_fontSizeMax=24)', 'm_fontSizeMax=24)')
text = text.replace('recommend(8953712310009366182, m_enableAutoSizing=1, m_fontSize=24, m_fontSizeBase=24, m_fontSizeMin=16, m_fontSizeMax=24)',
                    'recommend(8953712310009366182, m_enableAutoSizing=1, m_fontSize=24, m_fontSizeBase=24, m_fontSizeMin=12, m_fontSizeMax=24)')
model.write_text(text,encoding='utf-8')
print('Conversion long-price minimum changed to12 only; normal auto-size unaffected.')
