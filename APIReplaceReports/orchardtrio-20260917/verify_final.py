import datetime
import hashlib
import json
import re
from pathlib import Path

RUN = Path(__file__).resolve().parent
ROOT = RUN.parent.parent
PROJECT = ROOT / 'BizzaWZ'

def read(name):
    return json.loads((RUN / name).read_text(encoding='utf-8-sig'))

def save(name, value):
    (RUN / name).write_text(json.dumps(value, ensure_ascii=False, indent=2), encoding='utf-8')

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

before = read('source-hashes-before.json')
after = {p.relative_to(ROOT).as_posix(): sha(p)
         for p in (PROJECT / 'Assets').rglob('*.cs')
         if 'PlayCloud_API' not in p.parts}
changed = sorted(p for p in before.keys() & after.keys() if before[p] != after[p])
added = sorted(after.keys() - before.keys())
removed = sorted(before.keys() - after.keys())
allowed = {
    'BizzaWZ/Assets/BizzaWZ/Final/Connect_SDK/SDK_WKY/API/AccountModule.cs',
    'BizzaWZ/Assets/BizzaWZ/Final/Connect_SDK/SDK_WKY/API/AccountModuleCfg.cs',
    'BizzaWZ/Assets/Obfuz/GeneratedEncryptionVirtualMachine.cs',
}
scope = {'status': 'Passed' if set(changed) == allowed and not added and not removed else 'Failed',
         'beforeFileCount': len(before), 'afterFileCount': len(after),
         'excluded': 'SDK documentation/tool copy under PlayCloud_API',
         'changed': [{'path': p, 'beforeSha256': before[p], 'afterSha256': after[p]} for p in changed],
         'added': added, 'removed': removed,
         'unexpectedChanged': sorted(set(changed) - allowed),
         'temporaryHelperRemoved': not (PROJECT / 'Assets/Editor/OrchardTrioReleaseUpdate.cs').exists(),
         'oneShotSecretRequestConsumed': not (RUN / 'release-update.request.json').exists(),
         'meaning': 'Every other C# source in the captured project scope remains byte-identical.'}
save('source-scope-results.json', scope)

start = read('unity-validation-start.json')
final = read('unity-final-start.json')
log_path = Path(start['log'])
with log_path.open('rb') as stream:
    stream.seek(start['startByte'])
    all_log = stream.read().decode('utf-8', errors='replace')
with log_path.open('rb') as stream:
    stream.seek(final['startByte'])
    final_log = stream.read().decode('utf-8', errors='replace')
(RUN / 'unity-compilation.log').write_text(all_log, encoding='utf-8')
(RUN / 'unity-final-compilation.log').write_text(final_log, encoding='utf-8')
error_pattern = r'^.*(?:error CS\d+|Scripts have compiler errors|Compilation failed|CompilationFailureException).*$'
errors = sorted(set(re.findall(error_pattern, all_log, re.MULTILINE)))
warnings = sorted(set(re.findall(r'^.*warning CS\d+.*$', all_log, re.MULTILINE)))
reload_passed = 'Mono: successfully reloaded assembly' in final_log
release_passed = 'Orchard Trio release configuration, ChannelConfig round-trip and Obfuz generation verified.' in all_log
asm_results = []
for name in ['Assembly-CSharp.dll', 'Assembly-CSharp-Editor.dll']:
    path = PROJECT / 'Library/ScriptAssemblies' / name
    asm_results.append({'path': path.relative_to(PROJECT).as_posix(), 'sha256': sha(path),
                        'lastWriteUtc': datetime.datetime.fromtimestamp(path.stat().st_mtime, datetime.timezone.utc).isoformat()})
runtime_timestamp = (PROJECT / 'Library/ScriptAssemblies/Assembly-CSharp.dll').stat().st_mtime
newest_source = max((ROOT / p).stat().st_mtime for p in changed)
runtime_fresh = runtime_timestamp >= newest_source
passed = not errors and reload_passed and release_passed and runtime_fresh and scope['status'] == 'Passed'
validation = {
    'status': 'Passed' if passed else 'Failed', 'checkedUtc': datetime.datetime.now(datetime.timezone.utc).isoformat(),
    'unityVersion': '2022.3.62f3', 'project': str(PROJECT), 'activeBuildTarget': 'Android',
    'mode': 'Actual existing Unity Editor process 32312; refresh, compile and domain reload after temporary helper removal',
    'compilerErrors': errors, 'compilerWarningCount': len(warnings), 'compilerWarnings': warnings,
    'releaseVerificationLogged': release_passed, 'postRemovalDomainReloadPassed': reload_passed,
    'runtimeAssemblyNewerThanChangedRuntimeSources': runtime_fresh,
    'temporaryHelperRemoved': scope['temporaryHelperRemoved'], 'assemblies': asm_results,
    'logEvidence': ['unity-compilation.log', 'unity-final-compilation.log'],
    'notExecuted': ['Android APK/AAB build', 'Android/iOS device runtime', 'Real or task withdrawal submission', 'Unity Play Mode request chain'],
    'existingEditorMessages': {
        'duplicateNewtonsoftVersions': 'Duplicate assembly \'Newtonsoft.Json.dll\'' in all_log,
        'disposedCancellationTokenMessage': 'Attempted to call .Dispose on an already disposed CancellationTokenSource' in all_log,
        'note': 'These messages were also present before the current verification; no compiler errors occurred.'
    }
}
save('unity-final-validation.json', validation)
if passed:
    release = read('release-config-results.json')
    release['obfuz']['generatedVmCompilation'] = 'Passed: actual Unity Editor compilation and post-helper-removal domain reload; see unity-final-validation.json'
    release['finalUnityCompilationEvidence'] = 'unity-final-validation.json'
    save('release-config-results.json', release)
print(json.dumps({'sourceScope': scope['status'], 'sourceFiles': len(before), 'changed': changed,
                  'unityCompile': validation['status'], 'compilerErrors': len(errors),
                  'compilerWarningCount': len(warnings), 'postRemovalDomainReloadPassed': reload_passed,
                  'runtimeAssemblyNewerThanSources': runtime_fresh}, ensure_ascii=False, indent=2))
if not passed:
    raise SystemExit(1)
