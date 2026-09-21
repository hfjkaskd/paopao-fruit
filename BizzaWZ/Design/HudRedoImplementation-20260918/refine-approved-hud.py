from pathlib import Path
import json,re
here=Path(__file__).resolve().parent
project=here.parents[1]
changes=[]
def edit(rel,edits):
    p=project/rel
    text=p.read_text(encoding='utf-8-sig')
    for fid,key,before,after in edits:
        pattern=rf'(^--- !u!\d+ &{fid}\n(?:(?!^--- !u!).)*?^  {re.escape(key)}: )([^\n]*)'
        def replace(m):
            assert m[2]==before,(fid,key,m[2])
            changes.append({'file':rel,'fileID':str(fid),'field':key,'before':before,'after':after})
            return m[1]+after
        text,n=re.subn(pattern,replace,text,flags=re.M|re.S)
        assert n==1
    p.write_text(text,encoding='utf-8',newline='\n')
edit('Assets/BizzaWZ/Final/MenuSystem/Common/CurrencyBar/CurrencyBar.prefab',[
 (4954420463098433676,'m_PixelsPerUnitMultiplier','10','6'),
 (917222461376090861,'m_PixelsPerUnitMultiplier','10','6'),
])
edit('Assets/BizzaWZ/Final/Real/UI/GamePanel/Resources/Resources/GameUiWidget.prefab',[
 (4234855247401888128,'m_Sprite','{fileID: -377845897, guid: 50c5208c19d79f44ebd86c010248d125, type: 3}','{fileID: 21300000, guid: 874d776a383c46d2b2f3de4017fd243a, type: 3}'),
 (4234855247401888128,'m_Color','{r: 1, g: 1, b: 1, a: 0}','{r: 1, g: 1, b: 1, a: 1}'),
 (2314400796718388841,'m_Color','{r: 1, g: 1, b: 1, a: 1}','{r: 1, g: 1, b: 1, a: 0}'),
])
(here/'visual-refinement.json').write_text(json.dumps(changes,indent=2),encoding='utf-8')
print('5 visual refinement fields; no RectTransform changes.')
