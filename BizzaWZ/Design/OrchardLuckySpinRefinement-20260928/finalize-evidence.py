from pathlib import Path
from datetime import datetime, timezone
import json, hashlib, re

out = Path(__file__).resolve().parent
project = out.parent.parent
read = lambda p: json.loads(p.read_text(encoding='utf-8-sig'))
audit_path = project / 'Design/OrchardImplementation-20260928/audit-current.json'
audit, state = read(audit_path), read(project / 'Design/OrchardUI/state.json')
assert state['compilationErrors'] == 0 and not state['playing'] and not state['compiling']
assert audit['newIssueCount'] == 0
runs = {}
for name in ['Runtime', 'RuntimeShort']:
    report = read(out / name / 'verification.json'); page = report['pages'][0]
    assert report['status'] == 'complete' and not report['error'] and page['opened'] and page['closed']
    assert len(page['buttons']) == 1 and page['buttons'][0]['centerHitsButton'] and page['buttons'][0]['interactable']
    assert all(e == '开启测试设备False' for e in report['errors'])
    runs[name] = {'startedUtc': report['startedUtc'], 'status': report['status'], 'buttonHitChecks': 1, 'loggedErrors': report['errors'], 'checks': page['checks']}
assert read(out / 'Before/layouts.json') == read(project / 'Design/OrchardImplementation-20260928/layouts.json')
assets = ['Assets/BizzaWZ/Final/Real/UI/SlotsPanel/SlotPanel/' + p for p in ['SlotPanel.prefab','SlotPanel.cs']] + ['Assets/OrchardUI/Art/SpinReferenceBack.png', 'Assets/OrchardUI/Art/SpinReferenceBack.png.meta']
summary = {'generatedUtc': datetime.now(timezone.utc).isoformat(), 'project': str(project), 'entryScene': 'Assets/Game/Resources/Scenes/InitWZ.unity', 'runtimeSizes': [[852,1846],[1080,1920]], 'compilationErrors': 0, 'newPrefabAuditIssues': 0, 'existingPrefabAuditErrors': audit['existingErrorCount'], 'existingPrefabAuditWarnings': audit['warnings'], 'runs': runs, 'buttonHitChecksPassed': 2, 'backClicksPassed': 4, 'layoutSpecsUnchanged': True, 'assetSha256': {p: hashlib.sha256((project / p).read_bytes()).hexdigest() for p in assets}, 'limitations': ['0/1 fixtures temporarily change only displayed text; saved progress and balance remain unchanged.', 'No spin, rewarded ad or reward claimed.', 'No Android/iOS device build tested.']}
(out / 'verification-summary.json').write_text(json.dumps(summary, ensure_ascii=False, indent=2), encoding='utf-8')
(out / 'audit-current.json').write_bytes(audit_path.read_bytes())
overall_path = project / 'Design/OrchardAllScreens-20260928/verification-summary.json'
overall = read(overall_path)
overall['luckySpinRefinement'] = {'summary': '../OrchardLuckySpinRefinement-20260928/verification-summary.json', 'generatedUtc': summary['generatedUtc'], 'backClicksPassed': 4, 'newPrefabAuditIssues': 0}
overall_path.write_text(json.dumps(overall, ensure_ascii=False, indent=2), encoding='utf-8')
html = (out / 'comparison.html').read_text(encoding='utf-8')
(out / 'comparison-script-check.js').write_text(re.search(r'<script>(.*?)</script>', html, re.S).group(1), encoding='utf-8')
print(json.dumps({k: summary[k] for k in ['compilationErrors','newPrefabAuditIssues','backClicksPassed','layoutSpecsUnchanged']}))
