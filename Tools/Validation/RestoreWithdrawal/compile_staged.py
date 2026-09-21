from pathlib import Path
import json, subprocess

HERE = Path(__file__).resolve().parent
manifest = json.loads((HERE / 'manifest.json').read_text())
project = Path(manifest['root']) / 'output_Unity'
changes = {i['path'].removeprefix('output_Unity/'): i for i in manifest['items'] if i['path'].startswith('output_Unity/')}
unity = Path('C:/Program Files/Unity/Hub/Editor/2022.3.62f3/Editor/Data')
bee = project / 'Library/Bee/artifacts/1900b0aE.dag'
output = HERE / 'compiled'
output.mkdir(exist_ok=True)
results = []
for name in ('Assembly-CSharp', 'Assembly-CSharp-Editor'):
    lines, added = [], set()
    editor = name.endswith('-Editor')
    for line in (bee / (name + '.rsp')).read_text(encoding='utf-8-sig').splitlines():
        if line.startswith(('-out:', '-refout:')): continue
        path = line.strip('"')
        if path in changes:
            line = '"' + changes[path]['stagedPath'].replace('\\', '/') + '"'
            added.add(path)
        if editor and line.startswith('-r:') and 'Assembly-CSharp.ref.dll' in line:
            line = '-r:"' + (output / 'Assembly-CSharp.ref.dll').as_posix() + '"'
        lines.append(line)
    for path, item in changes.items():
        if path.endswith('.cs') and path not in added and ('/Editor/' in path) == editor:
            lines.append('"' + item['stagedPath'].replace('\\', '/') + '"')
    lines.extend(['-out:"' + (output / (name + '.dll')).as_posix() + '"', '-refout:"' + (output / (name + '.ref.dll')).as_posix() + '"'])
    response = HERE / (name + '.rsp')
    response.write_text('\n'.join(lines), encoding='utf-8')
    result = subprocess.run([str(unity / 'NetCoreRuntime/dotnet.exe'), str(unity / 'DotNetSdkRoslyn/csc.dll'), '@' + str(response)], cwd=project, capture_output=True)
    (HERE / (name + '-compile.log')).write_bytes(result.stdout + result.stderr)
    errors = [l for l in (result.stdout + result.stderr).decode(errors='replace').splitlines() if 'error ' in l]
    results.append({'assembly': name, 'exitCode': result.returncode, 'errors': errors})
    if result.returncode: break
(HERE / 'compile-results.json').write_text(json.dumps(results, indent=2), encoding='utf-8')
print(json.dumps(results))
raise SystemExit(any(r['exitCode'] for r in results))
