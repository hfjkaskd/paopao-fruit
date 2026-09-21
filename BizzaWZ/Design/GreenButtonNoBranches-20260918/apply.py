"""Disable only decorative Image components; retain all hierarchy and bindings."""
from pathlib import Path
import hashlib, json, shutil, os
from inspect import ROOT, HERE, inventory, blocks

GREEN = {
    '{fileID: -1142528992, guid: 50c5208c19d79f44ebd86c010248d125, type: 3}',
    '{fileID: 21300000, guid: d32dd97f0ef44177bd56a2985c098c92, type: 3}',
    '{fileID: 21300000, guid: 524f480620394cedbcaa19861f9144f6, type: 3}',
}
EDITOR = 'Assets/OrchardUI/Editor/OrchardNavigationPass.cs'
sha = lambda p: hashlib.sha256(p.read_bytes()).hexdigest()
rows = inventory()
targets = [r for r in rows if any(p['sprite'] in GREEN for p in r['parentImages']) and r['enabled']=='1']
paths = sorted({r['prefab'] for r in targets}|{EDITOR})
if (HERE/'baseline.json').exists():
    baseline=json.loads((HERE/'baseline.json').read_text(encoding='utf-8'))
    assert all(sha(ROOT/p)==baseline['files'][p] for p in paths if p!=EDITOR), 'Assets changed since baseline'
else:
    baseline = {'files':{p:sha(ROOT/p) for p in paths}, 'protected':{}}
    for path in (ROOT/'Assets').rglob('*'):
        if path.suffix in ('.prefab','.unity','.cs'):
            baseline['protected'][path.relative_to(ROOT).as_posix()]=sha(path)
    for p in paths:
        dest=HERE/'Before'/p; dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(ROOT/p,dest)
    (HERE/'baseline.json').write_text(json.dumps(baseline,indent=2),encoding='utf-8')
    (HERE/'before-inventory.json').write_text(json.dumps(rows,indent=2),encoding='utf-8')
for p in paths:
    if p==EDITOR: continue
    path=ROOT/p; data=path.read_bytes();text=data.decode('utf-8-sig'); newline='\r\n' if b'\r\n' in data else '\n'
    text=text.replace('\r\n','\n');bs=blocks(text)
    for row in targets:
        if row['prefab']!=p: continue
        old=bs[row['imageID']][1]
        assert old.count('  m_Enabled: 1\n')==1
        new=old.replace('  m_Enabled: 1\n','  m_Enabled: 0\n')
        assert text.count(old)==1
        text=text.replace(old,new)
    encoded=text.replace('\n',newline).encode('utf-8')
    if data.startswith(b'\xef\xbb\xbf'):encoded=b'\xef\xbb\xbf'+encoded
    staged=path.with_suffix('.prefab.no-branches-tmp')
    staged.write_bytes(encoded)
    os.replace(staged,path)
(HERE/'changes.json').write_text(json.dumps({'kind':'Prefab decoration visibility only','count':len(targets),'prefabCount':len(paths)-1,'changes':targets},ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'disabledDecorations':len(targets),'prefabCount':len(paths)-1,'protectedAssets':len(baseline['protected'])}))
