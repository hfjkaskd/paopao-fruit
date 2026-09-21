"""Aggregate completed tool results without marking unexecuted checks successful."""
from pathlib import Path
import hashlib
import json
import re

HERE = Path(__file__).resolve().parent
PROJECT = HERE.parents[2] / 'BizzaWZ'
def read(name):
    return json.loads((HERE/name).read_text(encoding='utf-8-sig'))
def save(name, value):
    (HERE/name).write_text(json.dumps(value,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

generation_log=(HERE/'unity-generation.log').read_text(encoding='utf-8-sig',errors='replace')
final_log=(HERE/'unity-final-compile.log').read_text(encoding='utf-8-sig',errors='replace')
errors=list(dict.fromkeys(re.findall(r'^.*(?:error CS\d+|error :|Aborting batchmode|Compilation failed).*$','\n'.join([generation_log,final_log]),re.M)))
warnings=list(dict.fromkeys(re.findall(r'^.*warning CS\d+.*$',final_log,re.M)))
assert not errors, errors
assert 'APIReplace release configuration and Obfuz generation verification passed.' in generation_log
assert 'Exiting batchmode successfully now!' in generation_log
assert 'Exiting batchmode successfully now!' in final_log
assert 'Csc Library/Bee/artifacts/1300b0aE.dag/Assembly-CSharp.dll' in final_log
assert 'Csc Library/Bee/artifacts/1300b0aE.dag/Assembly-CSharp-Editor.dll' in final_log
compilation={
    'status':'Passed','unityVersion':'2022.3.62f3','buildTarget':'Android',
    'generationExitCode':0,'finalExitCode':0,
    'generationLog':'unity-generation.log','finalLog':'unity-final-compile.log',
    'finalRuntimeAssemblyRecompiled':True,'finalEditorAssemblyRecompiled':True,
    'finalCompilationIncludesGeneratedVm':True,
    'compilerErrorCount':len(errors),'compilerWarningCount':len(warnings),
    'warnings':warnings,'errors':errors,
    'initialSandboxAttempt':'Not compiled: licensing IPC refused; only this batch process was stopped. Retried in approved normal host environment, succeeded.',
    'apkOrIpaBuild':'Not executed; scope is script compilation and API replacement.',
    'androidOrIosDeviceTest':'Not executed; no device gameplay test performed.'
}
save('compilation-results.json',compilation)

obfuz=read('obfuz-generation-results.json')
assert obfuz['status']=='Passed'
obfuz['generatedVmCompilation']='Passed; final Unity batch exit 0, runtime and Editor assemblies rebuilt after VM generation and temporary helper removal.'
save('obfuz-generation-results.json',obfuz)
release=read('release-config-results.json')
release['obfuz']=obfuz
save('release-config-results.json',release)
api=read('api-mapping.json')
api['validation']['compilation']={'status':'Passed','evidence':'compilation-results.json'}
api['validation']['communication']={'executed':7,'passed':7,'failed':0,'notExecuted':12,'evidence':'communication-results.json'}
save('api-mapping.json',api)

methods=read('method-audit-results.json')
assert methods['signatureOk']
assert all(m['invocationSequenceUnchanged'] for v in methods['variants'] for m in v['changed'])
assert all(not v['added'] and not v['removed'] for v in methods['variants'])
assert api['validation']['methodContract']['allOtherTextUnchangedAfterAllowedTokenProjection']
communication=read('communication-results.json')
network_audit=read('communication-contract-audit.json')
source=PROJECT/'Assets/BizzaWZ/Final/Connect_SDK/SDK_WKY/API/AccountModule.cs'
assert sha(source)==network_audit['sourceSha256']
assert communication['summary']['passed']==7 and communication['summary']['failed']==0
assert network_audit['status']=='passed'
assert not (PROJECT/'Assets/Editor/ApiReplaceReleaseConfig.cs').exists()
assert not (PROJECT/'Assets/Editor/ApiReplaceReleaseConfig.cs.meta').exists()
files=[
    'Assets/BizzaWZ/Final/Connect_SDK/SDK_WKY/API/AccountModule.cs',
    'Assets/BizzaWZ/Final/Connect_SDK/SDK_WKY/API/AccountModuleCfg.cs',
    *release['changedReleaseFiles'],
    *[x['path'] for x in obfuz['outputs']]
]
records=[]
for file in dict.fromkeys(files):
    path=PROJECT/file
    baseline=HERE/'baseline'/file
    if not baseline.exists():
        baseline=HERE/'baseline'/path.name
    if not baseline.exists() and '/API/' in file:
        baseline=HERE/'baseline/API'/path.name
    assert path.exists(),file
    assert baseline.exists(),file
    records.append({'path':file,'beforeSha256':sha(baseline),'afterSha256':sha(path),'changed':sha(baseline)!=sha(path)})

before={line[3:] for line in (HERE/'initial-git-status.txt').read_text(encoding='utf-8-sig').splitlines() if len(line)>3}
after={line[3:] for line in (HERE/'final-git-status.txt').read_text(encoding='utf-8-sig').splitlines() if len(line)>3}
new_status_paths=sorted(after-before)
expected={'BizzaWZ/'+f for f in files}
unexpected=[p for p in new_status_paths if p not in expected]
assert not unexpected,unexpected
verification={
    'confirmedReplacementChecks':'Passed','overallCompletion':'Confirmed items completed; 3 Unresolved retained, not fully resolved.',
    'methodDeclarationsUnchanged':True,'methodInvocationSequencesUnchanged':True,
    'allOtherMethodTextPreservedAfterAuthorizedChanges':True,
    'attributionValidation':'Static only; 19 keys complete, unique, no confirmed old keys remaining; expressions/types preserved.',
    'actualHttpResponseComparedAgainstFinalSourceSha256':True,
    'channelConfigOriginalSerializer':'Passed in source harness and actual Unity',
    'unrelatedSerializedFieldsBytePreservation':True,
    'privacyLocalizationNonUrlBytesPreserved':release['privacyLocalization']['nonUrlBytesPreserved'],
    'temporaryEditorHelperRemoved':True,
    'unexpectedNewGitStatusPaths':unexpected,
    'initialUserDirtyPathsStillPresent':before.issubset(after),
    'files':records,
    'limitations':['3 Unresolved items retained','3 SwaggerTypeMismatch entries preserve local contracts','12 API requests not executed with per-path reasons','No real attribution serialization/send executed; static verification only','No APK/IPA build or Android/iOS device gameplay test']
}
save('final-verification.json',verification)
print(json.dumps({'compilation':'passed','warnings':len(warnings),'errors':len(errors),'modifiedFiles':len(records),'unresolved':3,'httpTests':'7/7'},ensure_ascii=False))
