from pathlib import Path
import re,json
ROOT=Path(__file__).resolve().parents[2]
P=ROOT/'Assets/FruitsHarvest/Resources/Original/res/local/coreplay/CorePlayUI.prefab'
T=P.read_text(encoding='utf-8-sig')
B={m[2]:(m[1],m[3]) for m in re.finditer(r'--- !u!(\d+) &(\d+)\n(.*?)(?=--- !u!|\Z)',T,re.S)}
G={};R={};C={}
def val(t,k):
 m=re.search(r'^  '+re.escape(k)+r': (.*)$',t,re.M);return m[1] if m else None
for i,(k,t) in B.items():
 if k=='1':G[i]=val(t,'m_Name')
 if k=='224':R[i]=(re.search(r'm_GameObject: \{fileID: (\d+)\}',t)[1],re.search(r'm_Father: \{fileID: (\d+)\}',t)[1])
 if k=='114':
  g=re.search(r'm_GameObject: \{fileID: (\d+)\}',t)
  if g:C.setdefault(g[1],[]).append(i)
gr={v[0]:k for k,v in R.items()}
def path(r):
 g,p=R[r];return (path(p)+'/' if p in R else '')+G.get(g,'?')
out=[]
for i,(g,parent) in R.items():
 p=path(i)
 if any('/Bottom/'+x+'/' in p or p.endswith('/Bottom/'+x) for x in ['Undo','Magic','Shuffle']):
  t=B[i][1]
  row={'path':p,'rectID':i,'rect':{k:val(t,k) for k in ['m_AnchoredPosition','m_SizeDelta','m_LocalScale','m_AnchorMin','m_AnchorMax']},'components':[]}
  for c in C.get(g,[]):
   z=B[c][1]
   keys=['m_Sprite','m_Type','m_PreserveAspect','m_PixelsPerUnitMultiplier','m_Enabled','m_Color','m_fontColor','m_fontSize','m_ItemBG','m_ItemIcon','outerFrame','m_UseDirectly','m_LeftItemNum','m_WatchAD','m_LockIcon']
   f={k:val(z,k) for k in keys if val(z,k)!=None}
   if f:row['components'].append({'id':c,'fields':f})
  out.append(row)
assets=[]
base=ROOT/'Assets/FruitsHarvest/Resources/Original/res/local/coreplay/sprite/item'
for name in ['ItemBg_Normal','ItemBg_Lock','ItemBg_Gray','NumBg','AdBg','Play','Item_Lock','Undo_Normal','Magic_Normal','Shuffle_Normal']:
 a=base/(name+'.asset');t=a.read_text();meta=Path(str(a)+'.meta').read_text()
 assets.append({'name':name,'guid':re.search(r'guid: (\w+)',meta)[1],'rect':re.search(r'  m_Rect:\n(.*?)(?=  m_Offset)',t,re.S)[1],'pixelsToUnits':val(t,'m_PixelsToUnits'),'pivot':val(t,'m_Pivot'),'texture':re.search(r'    texture: (.*)',t)[1]})
result={'prefab':str(P),'nodes':out,'assets':assets}
(Path(__file__).parent/'prop-mapping-audit.json').write_text(json.dumps(result,ensure_ascii=False,indent=2))
print(json.dumps(result,ensure_ascii=False,indent=2))
