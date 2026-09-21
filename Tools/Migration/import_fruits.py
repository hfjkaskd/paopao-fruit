"""Copy only gameplay roots and their GUID/code dependencies; never write to source."""
from pathlib import Path
import re, shutil, json

SOURCE = Path(r'C:\Projects\FruitsHarvestMaster\output_Unity\Assets')
TARGET = Path(r'C:\Projects\paopao\BizzaWZ\Assets\FruitsHarvest')
guid_re = re.compile(r'guid: ([0-9a-f]{32})')
assets = {}
for meta in SOURCE.rglob('*.meta'):
    match = re.search(r'^guid: (\w+)', meta.read_text(errors='ignore'), re.M)
    if match: assets[match[1]] = meta.with_suffix('')
scripts = list((SOURCE/'Scripts/Assembly-CSharp').rglob('*.cs'))
texts = {p:p.read_text(encoding='utf-8-sig') for p in scripts}
names = {}
for p,t in texts.items():
    for name in re.findall(r'\b(?:class|struct|enum|interface)\s+(\w+)',t): names.setdefault(name,set()).add(p)
selected=set()
for root in ['res/local/coreplay','res/local/coreplayeff','res/local/common','res/local/configs',
             'res/local/gameloading','res/local/globalui','res/local/pops/newitempop',
             'res/local/sound','res/local/textmeshpro','res/local/vx','res_server']:
    selected.update(p for p in (SOURCE/root).rglob('*') if p.is_file() and p.suffix!='.meta')
for name in ['EDLHEMMBABM','PLMIHDHFAAL','JEFOMCDAPGK','BMNFNJFCPHG','OJEEJGGLNPC','GameAudio','MgrUI','MgrGlobalUI','Timer','MCCIJBJGMCK']:
    selected.update(names[name])
pending=list(selected)
while pending:
    p=pending.pop()
    if not p.is_file(): continue
    data=texts.get(p) if p.suffix=='.cs' else None
    if data is None:
        if p.stat().st_size>8_000_000: continue
        data=p.read_text(errors='ignore')
    dependencies={assets[g] for g in guid_re.findall(data) if g in assets and assets[g].is_file()}
    if p.suffix=='.cs':
        for word in set(re.findall(r'\b\w+\b', data)):
            dependencies.update(names.get(word,()))
    for dep in dependencies-selected:
        selected.add(dep); pending.append(dep)

# Framework/package MonoScripts must retain their established GUIDs rather than duplicates.
framework_guids={}
for m in TARGET.parent.rglob('*.meta'):
    if TARGET in m.parents: continue
    g=re.search(r'^guid: (\w+)',m.read_text(errors='ignore'),re.M)
    if g: framework_guids[g[1]]=m
manifest=[]
for p in sorted(selected):
    if p.name=='GameResCatalog.asset' or '/Properties/' in p.as_posix(): continue
    rel=p.relative_to(SOURCE)
    if p.suffix=='.cs':
        dest=TARGET/'Scripts'/p.relative_to(SOURCE/'Scripts/Assembly-CSharp') if SOURCE/'Scripts/Assembly-CSharp' in p.parents else TARGET/'Scripts'/p.name
    else: dest=TARGET/'Resources/Original'/rel
    meta=Path(str(p)+'.meta')
    guid=None
    if meta.exists():
        g=re.search(r'^guid: (\w+)',meta.read_text(errors='ignore'),re.M)
        guid=g[1] if g else None
    if guid in framework_guids:
        manifest.append({'source':str(rel),'shared':str(framework_guids[guid]),'guid':guid}); continue
    dest.parent.mkdir(parents=True,exist_ok=True)
    if not dest.exists():
        shutil.copy2(p,dest)
        if meta.exists(): shutil.copy2(meta,str(dest)+'.meta')
    manifest.append({'source':str(rel),'target':str(dest.relative_to(TARGET)),'guid':guid})
Path(__file__).with_name('import-manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
print(f'Selected {len(selected)} dependencies; imported {sum("target" in x for x in manifest)} files')
