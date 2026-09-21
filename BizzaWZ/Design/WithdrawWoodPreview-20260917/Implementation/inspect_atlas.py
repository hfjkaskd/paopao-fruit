"""Read-only alpha component inspection for Unity sprite import rectangles."""
from PIL import Image
from collections import deque
from pathlib import Path
import json

root = Path(__file__).parent
specs = json.loads((root.parents[1] / 'OrchardUI' / 'atlas-slices.json').read_text(encoding='utf-8-sig'))['sprites']
im = Image.open(root / 'Controls-refined.png').convert('RGBA')
w,h = im.size
alpha = im.getchannel('A')
result=[]
for spec in specs:
    name=spec['name']
    x0,y0=max(0,spec['x']-16),max(0,h-spec['y']-spec['height']-16)
    x1,y1=min(w,spec['x']+spec['width']+16),min(h,h-spec['y']+16)
    aw,ah=x1-x0,y1-y0
    data=alpha.crop((x0,y0,x1,y1)).tobytes()
    seen=bytearray(aw*ah)
    components=[]
    for p,v in enumerate(data):
        if seen[p] or v<128: continue
        q=deque([p]);seen[p]=1
        mnx,mny,mxx,mxy,count=aw,ah,0,0,0
        while q:
            n=q.popleft();x=n%aw;y=n//aw;count+=1
            mnx=min(mnx,x);mny=min(mny,y);mxx=max(mxx,x);mxy=max(mxy,y)
            for nn in (n-1 if x else -1,n+1 if x+1<aw else -1,n-aw if y else -1,n+aw if y+1<ah else -1):
                if nn>=0 and not seen[nn] and data[nn]>=128:
                    seen[nn]=1;q.append(nn)
        components.append((count,mnx+x0,mny+y0,mxx+x0+1,mxy+y0+1))
    comp=max(components)
    count,left,top,right,bottom=comp
    result.append(dict(name=name,pixels=count,left=left,top=top,right=right,bottom=bottom,x=left,y=h-bottom,width=right-left,height=bottom-top))
print(json.dumps(dict(size=im.size,alpha_range=alpha.getextrema(),sprites=result),indent=2))
