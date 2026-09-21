from pathlib import Path
import re,json
here=Path(__file__).resolve().parent
project=here.parents[1]
rel='Assets/BizzaWZ/Final/Real/UI/GamePanel/Resources/Resources/GameUiWidget.prefab'
p=project/rel
text=p.read_text(encoding='utf-8-sig')
fid='3586814762122687189'
before='{fileID: 5304515813898986083}'
after='{fileID: 7343093305673801710}'
pattern=rf'(^--- !u!114 &{fid}\n(?:(?!^--- !u!).)*?^  scaleTarget: )([^\n]*)'
def replace(m):
    assert m[2]==before
    return m[1]+after
text,n=re.subn(pattern,replace,text,flags=re.M|re.S)
assert n==1
p.write_text(text,encoding='utf-8',newline='\n')
(here/'visual-feedback.json').write_text(json.dumps([{'file':rel,'fileID':fid,'field':'scaleTarget','before':before,'after':after,'reason':'Keep existing pressScaleRatio feedback visible on the original outer Image now showing gift artwork. Button events, hit area, root, enabled/interactable, targetGraphic and pressScaleRatio are unchanged.'}],indent=2),encoding='utf-8')
