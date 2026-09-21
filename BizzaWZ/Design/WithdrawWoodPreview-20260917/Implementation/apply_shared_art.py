"""Import generated pixels and update sprite rectangles only; no prefab or code authoring."""
from pathlib import Path
import json, re, shutil, subprocess, sys

here=Path(__file__).resolve().parent
project=here.parents[2]
atlas=project/'Assets/OrchardUI/Art/Controls.png'
meta=atlas.with_suffix('.png.meta')
measurement=json.loads(subprocess.check_output([sys.executable,str(here/'inspect_atlas.py')],text=True))
assert measurement['size']==[1254,1254]
before=meta.read_text(encoding='utf-8-sig')
after=before
for s in measurement['sprites']:
    # Keep GUID, spriteID, internalID, pivot, 9-slice borders and import settings.
    pattern=r'(      name: '+re.escape(s['name'])+r'\n      rect:\n        serializedVersion: 2\n)        x: [^\n]+\n        y: [^\n]+\n        width: [^\n]+\n        height: [^\n]+'
    # One pixel of alpha feather around the connected opaque shape.
    rect=dict(x=max(0,s['x']-1),y=max(0,s['y']-1),width=s['width']+2,height=s['height']+2)
    replacement=r'\g<1>'+''.join('        '+k+': '+str(v)+'\n' for k,v in rect.items()).rstrip('\n')
    after,n=re.subn(pattern,replacement,after)
    assert n==1, (s['name'],n)
after=re.sub(r'(      name: Panel\n.*?      border: )\{x: [^}]+\}',r'\g<1>{x: 70, y: 70, z: 70, w: 70}',after,count=1,flags=re.S)
shutil.copyfile(here/'Controls-refined.png',atlas)
meta.write_text(after,encoding='utf-8',newline='\n')
shutil.copyfile(here/'Backdrop-soft.png',project/'Assets/OrchardUI/Resources/OrchardUI/Backdrop.png')
(here/'sprite-rectangles.json').write_text(json.dumps(measurement,indent=2),encoding='utf-8')
print('Updated shared controls, sprite rect metadata and background; no scene, prefab, gameplay code or business data changed by this importer.')
