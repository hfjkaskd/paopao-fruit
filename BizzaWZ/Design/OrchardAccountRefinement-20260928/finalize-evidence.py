from pathlib import Path
from datetime import datetime, timezone
import hashlib
import json
import re

out = Path(__file__).resolve().parent
project = out.parent.parent
read = lambda p: json.loads(p.read_text(encoding='utf-8-sig'))
audit_path = project / 'Design/OrchardImplementation-20260928/audit-current.json'
audit = read(audit_path)
state = read(project / 'Design/OrchardUI/state.json')
assert state['compilationErrors'] == 0 and not state['playing'] and not state['compiling']
assert audit['newIssueCount'] == 0
runs = {}
for name in ['Runtime', 'RuntimeShort']:
    report = read(out / name / 'verification.json')
    assert report['status'] == 'complete' and not report['error'] and report['index'] == 3
    assert len(report['pages']) == 1 and report['pages'][0]['opened'] and report['pages'][0]['closed']
    assert all(b['centerHitsButton'] and b['interactable'] for b in report['pages'][0]['buttons'])
    assert all(e == '开启测试设备False' for e in report['errors'])
    runs[name] = {'startedUtc': report['startedUtc'], 'status': report['status'], 'inputFieldsChecked': report['index'], 'loggedErrors': report['errors']}
assets = [
    'Assets/BizzaWZ/Final/Real/UI/WithdrawFillPanel/WithdrawFillPanel.prefab',
    'Assets/OrchardUI/Resources/OrchardUI/AccountReferenceControls.png',
    'Assets/OrchardUI/Runtime/OrchardAccountVisual.cs',
    'Assets/OrchardUI/Runtime/OrchardInputVisual.cs',
]
summary = {
    'generatedUtc': datetime.now(timezone.utc).isoformat(),
    'project': str(project),
    'entryScene': 'Assets/Game/Resources/Scenes/InitWZ.unity',
    'compilationErrors': state['compilationErrors'],
    'newPrefabAuditIssues': audit['newIssueCount'],
    'existingPrefabAuditErrors': audit['existingErrorCount'],
    'existingPrefabAuditWarnings': audit['warnings'],
    'runs': runs,
    'inputCasesPassed': sum(r['inputFieldsChecked'] for r in runs.values()),
    'knownLog': '开启测试设备False is an existing SDK diagnostic logged at Error level.',
    'assetSha256': {p: hashlib.sha256((project / p).read_bytes()).hexdigest() for p in assets},
    'limitations': ['No withdrawal/payment submitted; no Android/iOS device build performed.', 'The diagnostic hides the keyboard through its existing platform-independent API before testing the Continue Button; keyboard Done behavior is outside this check.', 'Preview uses isolated sample values; runtime retains current account and server values.', 'The result corrects the reported proportions and highlights; no assertion of complete pixel identity.'],
}
(out / 'verification-summary.json').write_text(json.dumps(summary, ensure_ascii=False, indent=2), encoding='utf-8')
(out / 'audit-current.json').write_bytes(audit_path.read_bytes())
overall_path = project / 'Design/OrchardAllScreens-20260928/verification-summary.json'
overall = read(overall_path)
overall['accountRefinement'] = {'summary': '../OrchardAccountRefinement-20260928/verification-summary.json', 'generatedUtc': summary['generatedUtc'], 'inputCasesPassed': summary['inputCasesPassed'], 'newPrefabAuditIssues': 0}
overall_path.write_text(json.dumps(overall, ensure_ascii=False, indent=2), encoding='utf-8')
html = (out / 'comparison.html').read_text(encoding='utf-8')
script = re.search(r'<script>(.*?)</script>', html, re.S).group(1)
(out / 'comparison-script-check.js').write_text(script, encoding='utf-8')
print(json.dumps({k: summary[k] for k in ['compilationErrors', 'newPrefabAuditIssues', 'inputCasesPassed']}, ensure_ascii=False))
