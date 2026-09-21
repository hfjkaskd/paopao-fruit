"""Read serialized Unity objects without loading gameplay or modifying assets."""
from pathlib import Path
import re, json

ROOT = Path(__file__).resolve().parents[2]
HERE = Path(__file__).resolve().parent
NAV = 'ba69b03635224224bafcd70eba3b015d'
LEAVES = {'812891126', '-419736011', '1530500536'}
IMAGE = 'fe87c0e1cc204ed48ad3b37840f39efc'

def blocks(text):
    return {m[2]: (m[1], m[0]) for m in re.finditer(r'^--- !u!(\d+) &(-?\d+)[^\n]*\n.*?(?=^--- !u!|\Z)', text, re.M|re.S)}

def field(text, name):
    match = re.search(r'^  '+re.escape(name)+r': (.*)$', text, re.M)
    return match[1] if match else None

def localref(text, name):
    value = field(text, name) or ''
    match = re.search(r'fileID: (-?\d+)', value)
    return match[1] if match else None

def inventory():
    result = []
    for path in sorted((ROOT/'Assets').rglob('*.prefab')):
        text = path.read_text(encoding='utf-8-sig')
        if NAV not in text: continue
        bs = blocks(text)
        gos = {fid: field(b,'m_Name') for fid,(typ,b) in bs.items() if typ == '1'}
        trs = {fid: (localref(b,'m_GameObject'),localref(b,'m_Father')) for fid,(typ,b) in bs.items() if typ in ('224','4')}
        go_tr = {go:fid for fid,(go,parent) in trs.items() if go}
        images = {}
        for fid,(typ,b) in bs.items():
            if typ == '114' and IMAGE in (field(b,'m_Script') or ''):
                images.setdefault(localref(b,'m_GameObject'),[]).append((fid,b))
        def namepath(tr):
            seen=set(); parts=[]
            while tr in trs and tr not in seen:
                seen.add(tr);go,tr=trs[tr];parts.append(gos.get(go,'<nested>'))
            return '/'.join(reversed(parts))
        for go, ims in images.items():
            for fid,b in ims:
                sprite=field(b,'m_Sprite') or ''
                if NAV not in sprite or not any('fileID: '+leaf+',' in sprite for leaf in LEAVES): continue
                tr=go_tr.get(go);parent=trs.get(tr,(None,None))[1];pgo=trs.get(parent,(None,None))[0]
                result.append({'prefab':path.relative_to(ROOT).as_posix(),'path':namepath(tr),'imageID':fid,
                    'enabled':field(b,'m_Enabled'),'name':gos.get(go), 'sprite':sprite,
                    'parentImages':[{'id':i,'sprite':field(v,'m_Sprite'),'enabled':field(v,'m_Enabled')} for i,v in images.get(pgo,[])]})
    return result

if __name__ == '__main__':
    rows=inventory()
    (HERE/'inventory.json').write_text(json.dumps(rows,ensure_ascii=False,indent=2),encoding='utf-8')
    for r in rows:
        print(r['prefab'],r['path'],r['enabled'],[v['sprite'] for v in r['parentImages']])
    print('Leaf Image count:',len(rows))
