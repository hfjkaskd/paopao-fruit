"""Read-only map of UI texture references and current resource hashes."""
from pathlib import Path
import re,json,hashlib
R=Path(__file__).resolve().parents[2]
A=R/'BizzaWZ/Assets'
index={}
for p in A.rglob('*.meta'):
    m=re.search(r'^guid: (\w+)',p.read_text(encoding='utf-8-sig',errors='replace'),re.M)
    if m:index[m[1]]=str(p.relative_to(R))[:-5]
refs={}
pages={}
prefab_dependencies={}
for p in A.rglob('*.prefab'):
    text=p.read_text(encoding='utf-8-sig',errors='replace')
    prefab_dependencies[str(p.relative_to(R))]=[index[g] for g in set(re.findall(r'guid: (\w+)',text)) if g in index and index[g].endswith('.prefab')]
    images=[]
    # Include prefab property overrides and serialized Sprite fields, not just Image blocks.
    texture_guids={g for g in re.findall(r'guid: (\w+)',text) if g in index and Path(index[g]).suffix.lower() in ('.png','.jpg','.jpeg','.tga','.psd')}
    for guid in sorted(texture_guids):
        path=index.get(guid)
        images.append({'guid':guid,'asset':path})
        refs.setdefault(guid,[]).append(str(p.relative_to(R)))
    if '/Final/Real/UI/' in p.as_posix() or '/Common/BizzaGame/' in p.as_posix():
        pages[str(p.relative_to(R))]=images
def depends_on(start,target,seen=None):
    if start==target:return True
    seen=set() if seen is None else seen
    if start in seen:return False
    seen.add(start)
    return any(depends_on(p,target,seen) for p in prefab_dependencies.get(start,[]))
assets=[]
for guid,paths in refs.items():
    path=index.get(guid)
    if not path or '/UI_Frame/' not in path.replace('\\','/'):continue
    absolute=R/path
    affected=sorted(p for p in pages if any(depends_on(p,d) for d in set(paths)))
    assets.append({'guid':guid,'asset':path,'sha256':hashlib.sha256(absolute.read_bytes()).hexdigest(),
                   'prefabs':sorted(set(paths)), 'affected_ui_prefabs_including_inheritance':affected})
out=Path(__file__).resolve().parent/'resource-map.json'
out.write_text(json.dumps({'pages':pages,'shared_ui_assets':assets},ensure_ascii=False,indent=2),encoding='utf-8')
print(f'{len(pages)} UI prefabs inventoried; {len(assets)} shared UI-frame textures mapped.')
for asset in assets:
    if any(n in asset['asset'] for n in ('Common_Frame','Common_Title','Btn_Normael')):
        print(Path(asset['asset']).name,len(asset['prefabs']),'direct prefab references;',len(asset['affected_ui_prefabs_including_inheritance']),'affected UI prefabs')
