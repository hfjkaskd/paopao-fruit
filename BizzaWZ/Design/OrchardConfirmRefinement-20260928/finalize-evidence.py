from pathlib import Path
from datetime import datetime, timezone
import hashlib
import json
import re

out = Path(__file__).resolve().parent
project = out.parent.parent
read = lambda p: json.loads(p.read_text(encoding='utf-8-sig'))
audit_path = project / 'Design/OrchardImplementation-20260928/audit-current.json'
audit, state = read(audit_path), read(project / 'Design/OrchardUI/state.json')
assert state['compilationErrors'] == 0 and not state['playing'] and not state['compiling']
assert audit['newIssueCount'] == 0
runs = {}
for name in ['Runtime', 'RuntimeShort']:
    report = read(out / name / 'verification.json')
    assert report['status'] == 'complete' and not report['error']
    page = report['pages'][0]
    assert page['opened'] and page['closed'] and len(page['buttons']) == 4
    assert all(b['centerHitsButton'] and b['interactable'] for b in page['buttons'])
    assert all(e == '开启测试设备False' for e in report['errors'])
    runs[name] = {'startedUtc': report['startedUtc'], 'status': report['status'], 'buttonHitChecks': 4, 'loggedErrors': report['errors']}
before_layouts = read(out / 'Before/layouts.json')['pages']
after_layouts = read(project / 'Design/OrchardImplementation-20260928/layouts.json')['pages']
assert all(a == b for a, b in zip(before_layouts, after_layouts) if a['name'] != 'withdraw-confirm')
assets = ['Assets/BizzaWZ/Final/Real/UI/UIWithdrawalConfirmPanel/UIWithdrawalConfirmPanel.prefab', 'Assets/OrchardUI/Resources/OrchardUI/ConfirmReferenceControls.png', 'Assets/OrchardUI/Runtime/OrchardConfirmVisual.cs']
summary = {
    'generatedUtc': datetime.now(timezone.utc).isoformat(),
    'project': str(project), 'entryScene': 'Assets/Game/Resources/Scenes/InitWZ.unity',
    'designAndPreviewSize': [828,1900], 'runtimeSizes': [[852,1846],[1080,1920]],
    'compilationErrors': state['compilationErrors'], 'newPrefabAuditIssues': audit['newIssueCount'],
    'existingPrefabAuditErrors': audit['existingErrorCount'], 'existingPrefabAuditWarnings': audit['warnings'],
    'otherPageLayoutSpecsUnchanged': True,
    'runs': runs, 'buttonHitChecksPassed': sum(r['buttonHitChecks'] for r in runs.values()),
    'knownLog': '开启测试设备False is an existing SDK diagnostic logged at Error level.',
    'assetSha256': {p:hashlib.sha256((project/p).read_bytes()).hexdigest() for p in assets},
    'limitations': ['Confirmation click/payment submission not executed; only pointer down/up visual state and hit target verified.', 'Runtime uses temporary design payee strings with actual account amount; no payee fields saved.', 'No Android/iOS device build tested.', 'Matching canvas and corrected elements do not establish complete pixel identity.']
}
(out/'verification-summary.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2),encoding='utf-8')
(out/'audit-current.json').write_bytes(audit_path.read_bytes())
overall_path = project/'Design/OrchardAllScreens-20260928/verification-summary.json'
overall=read(overall_path)
overall['confirmRefinement']={'summary':'../OrchardConfirmRefinement-20260928/verification-summary.json','generatedUtc':summary['generatedUtc'],'buttonHitChecksPassed':summary['buttonHitChecksPassed'],'newPrefabAuditIssues':0}
overall_path.write_text(json.dumps(overall,ensure_ascii=False,indent=2),encoding='utf-8')
html=(out/'comparison.html').read_text(encoding='utf-8')
(out/'comparison-script-check.js').write_text(re.search(r'<script>(.*?)</script>',html,re.S).group(1),encoding='utf-8')
print(json.dumps({k:summary[k] for k in ['compilationErrors','newPrefabAuditIssues','buttonHitChecksPassed','otherPageLayoutSpecsUnchanged']},ensure_ascii=False))
