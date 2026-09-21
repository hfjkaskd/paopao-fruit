from pathlib import Path
import re, sys, json

def read(path):
    text = Path(path).read_text(encoding='utf-8-sig')
    blocks = {}
    for m in re.finditer(r'^--- !u!(\d+) &(-?\d+)[^\n]*\n(.*?)(?=^--- !u!|\Z)', text, re.M | re.S):
        blocks[m[2]] = (m[1], m[3])
    def field(block, key):
        m = re.search(r'^  '+re.escape(key)+r': *(.*)$',block,re.M)
        return m[1] if m else None
    gos = {i: field(b, 'm_Name') for i,(t,b) in blocks.items() if t=='1'}
    transforms = {}
    go_trans = {}
    for i,(t,b) in blocks.items():
        if t not in ('4','224'): continue
        gm = re.search(r'm_GameObject: \{fileID: (\d+)\}',b)
        fm = re.search(r'm_Father: \{fileID: (\d+)\}',b)
        if gm:
            transforms[i] = (gm[1],fm[1] if fm else '0')
            go_trans[gm[1]] = i
    def path_for(go):
        tr=go_trans.get(go)
        if not tr:return gos.get(go,go)
        parent=transforms[tr][1]
        return (path_for(transforms[parent][0])+'/' if parent in transforms else '')+gos.get(go,go)
    rows=[]
    for i,(t,b) in blocks.items():
        gm = re.search(r'm_GameObject: \{fileID: (\d+)\}',b)
        if not gm: continue
        row={'id':i,'type':t,'path':path_for(gm[1])}
        for key in ('m_AnchoredPosition','m_SizeDelta','m_LocalScale','m_AnchorMin','m_AnchorMax','m_Pivot','m_Sprite','m_Type','m_PreserveAspect','m_FillMethod','m_FillAmount','m_FillOrigin','m_PixelsPerUnitMultiplier','m_Color','m_fontColor','m_fontSize','m_fontSizeMin','m_fontSizeMax','m_text','m_Script','m_TargetGraphic','m_ItemBG','m_ItemIcon'):
            value=field(b,key)
            if value is not None: row[key]=value
        if len(row)>3:rows.append(row)
    return rows

if __name__=='__main__':
    for row in read(sys.argv[1]):
        if len(sys.argv)<3 or re.search(sys.argv[2],row['path']): print(json.dumps(row,ensure_ascii=False))
