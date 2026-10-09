from pathlib import Path
from datetime import datetime, timezone
import hashlib
import json
import re

out=Path(__file__).resolve().parent
project=out.parent.parent
read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
audit_path=project/'Design/OrchardImplementation-20260928/audit-current.json'
audit,state=read(audit_path),read(project/'Design/OrchardUI/state.json')
assert state['compilationErrors']==0 and not state['playing'] and not state['compiling']
assert audit['newIssueCount']==0
runs={}
for name in ['Runtime','RuntimeShort']:
    report=read(out/name/'verification.json');page=report['pages'][0]
    assert report['status']=='complete' and not report['error'] and page['opened'] and page['closed']
    assert len(page['buttons'])==7 and all(b['centerHitsButton'] and b['interactable'] for b in page['buttons'])
    assert all(e=='开启测试设备False' for e in report['errors'])
    runs[name]={'startedUtc':report['startedUtc'],'status':report['status'],'buttonHitChecks':7,'loggedErrors':report['errors'],'checks':page['checks']}
assert read(out/'Before/layouts.json')==read(project/'Design/OrchardImplementation-20260928/layouts.json')
assets=['Assets/BizzaWZ/Final/Real/UI/ServicePanel/'+p for p in ['ServicePanel.prefab','ChatElement.prefab','ServicePanel.cs','ChatElement.cs','ViewportResizer.cs']]+['Assets/OrchardUI/Resources/OrchardUI/ServiceReferenceControls.png','Assets/OrchardUI/Runtime/OrchardServiceVisual.cs']
summary={'generatedUtc':datetime.now(timezone.utc).isoformat(),'project':str(project),'entryScene':'Assets/Game/Resources/Scenes/InitWZ.unity','referenceSize':[852,1846],'runtimeSizes':[[852,1846],[1080,1920]],'compilationErrors':0,'newPrefabAuditIssues':0,'existingPrefabAuditErrors':audit['existingErrorCount'],'existingPrefabAuditWarnings':audit['warnings'],'runs':runs,'buttonHitChecksPassed':14,'layoutSpecsUnchanged':True,'assetSha256':{p:hashlib.sha256((project/p).read_bytes()).hexdigest() for p in assets},'limitations':['Four temporary ChatElement rows reproduce the reference; fourteen temporary rows test scrolling. No messages sent or saved.','No support message submitted.','No Android/iOS device build tested.','Visual refinement does not prove complete pixel identity.']}
(out/'verification-summary.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2),encoding='utf-8')
(out/'audit-current.json').write_bytes(audit_path.read_bytes())
overall_path=project/'Design/OrchardAllScreens-20260928/verification-summary.json'
overall=read(overall_path);overall['serviceRefinement']={'summary':'../OrchardServiceRefinement-20260928/verification-summary.json','generatedUtc':summary['generatedUtc'],'buttonHitChecksPassed':14,'newPrefabAuditIssues':0}
overall_path.write_text(json.dumps(overall,ensure_ascii=False,indent=2),encoding='utf-8')
html=(out/'comparison.html').read_text(encoding='utf-8')
(out/'comparison-script-check.js').write_text(re.search(r'<script>(.*?)</script>',html,re.S).group(1),encoding='utf-8')
print(json.dumps({k:summary[k] for k in ['compilationErrors','newPrefabAuditIssues','buttonHitChecksPassed','layoutSpecsUnchanged']},ensure_ascii=False))
