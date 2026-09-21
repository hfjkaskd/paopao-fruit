from pathlib import Path
import json, hashlib, re, secrets, string, subprocess, datetime
ROOT=Path(r'C:\Projects\paopao\BizzaWZ')
RUN=Path(r'C:\Projects\paopao\APIReplaceReports\bubblosaicpmge-20260916')
UNITY=Path(r'C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Data')
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
files={p.relative_to(ROOT).as_posix():sha(p) for base in ('Assets','Packages') for p in (ROOT/base).rglob('*.cs')}
(RUN/'source-hashes-before.json').write_text(json.dumps(files,indent=2),encoding='utf-8')
# One requested random prefix. Retain all other already matching serialized settings.
p=ROOT/'ProjectSettings/Obfuz.asset'
b=p.read_bytes(); old=re.search(rb'obfuscatedNamePrefix: ([a-zA-Z]+)',b).group(1).decode()
prefix=''.join(secrets.choice(string.ascii_lowercase) for _ in range(3))
while prefix==old: prefix=''.join(secrets.choice(string.ascii_lowercase) for _ in range(3))
b=re.sub(rb'(obfuscatedNamePrefix: )[a-zA-Z]+',lambda m:m.group(1)+prefix.encode(),b,count=1)
p.write_bytes(b)
(RUN/'obfuz-prefix.json').write_text(json.dumps({'old':old,'new':prefix,'random':'secrets.choice ASCII lowercase, three letters'},indent=2),encoding='utf-8')
# Compile using Unity's generated Android/Editor response files, changing only output destinations.
results=[]
for assembly in ('Assembly-CSharp','Assembly-CSharp-Editor'):
    source=ROOT/'Library/Bee/artifacts/1300b0aE.dag'/f'{assembly}.rsp'
    text=source.read_text(encoding='utf-8-sig')
    for opt,suffix in [('out','.dll'),('refout','.ref.dll')]:
        text=re.sub(rf'^-{opt}:.*$',f'-{opt}:"{(RUN/(assembly+suffix)).as_posix()}"',text,flags=re.M)
    if assembly.endswith('-Editor'):
        text=text.replace('Library/Bee/artifacts/1300b0aE.dag/Assembly-CSharp.ref.dll',(RUN/'Assembly-CSharp.ref.dll').as_posix())
    dest=RUN/f'{assembly}.validation.rsp';dest.write_text(text,encoding='utf-8')
    proc=subprocess.run([str(UNITY/'NetCoreRuntime/dotnet.exe'),str(UNITY/'DotNetSdkRoslyn/csc.dll'),'@'+str(dest)],cwd=str(ROOT),capture_output=True,text=True,encoding='utf-8',errors='replace')
    output=proc.stdout+proc.stderr;(RUN/f'{assembly}.compile.log').write_text(output,encoding='utf-8')
    errors=[line for line in output.splitlines() if re.search(r'\berror (CS|BC)\d+',line)]
    warnings=[line for line in output.splitlines() if 'warning CS' in line]
    results.append({'assembly':assembly,'exit_code':proc.returncode,'errors':errors,'warning_count':len(warnings),'source_response':str(source),'response_sha256':sha(dest),'log':f'{assembly}.compile.log','scope':'Unity 2022.3.62f3 Roslyn, generated Android+Editor defines, no Unity batchmode, no APK/IL2CPP build'})
    print(assembly, 'exit=',proc.returncode,'errors=',len(errors),'warnings=',len(warnings))
(RUN/'compile.json').write_text(json.dumps(results,ensure_ascii=False,indent=2),encoding='utf-8')
