from pathlib import Path
import re

root = Path(__file__).resolve().parents[2]
p = root / 'Assets/BizzaWZ/Final/Real/UI/GetRewardPanel/GetRewardPanel.prefab'
s = p.read_text(encoding='utf-8-sig')
b = {int(m[1]):m[0] for m in re.finditer(r'--- !u!\d+ &(-?\d+).*?(?=\n--- !u!|\Z)',s,re.S)}
order=list(b)

def field(i,k,v):
 b[i],n=re.subn(r'^  '+re.escape(k)+':.*$', '  '+k+': '+str(v), b[i], flags=re.M);assert n==1,(i,k)

# Correct currency caption follows the same existing mode component as the reward icons.
old=[8961100082026100811,8961100082026100812,8961100082026100813,8961100082026100814,8961100082026100815]
new=[8961100082026100821,8961100082026100822,8961100082026100823,8961100082026100824,8961100082026100825]
for a,c in zip(old,new):
 block=b[a]
 for x,y in zip(old,new):block=block.replace(str(x),str(y))
 b[c]=block;order.append(c)
field(new[0],'m_Name','SingleCurrencyCaption')
field(new[1],'m_Father','{fileID: 8167171475660969831}')
b[8167171475660969831]=b[8167171475660969831].replace('  m_Father:',f'  - {{fileID: {new[1]}}}\n  m_Father:')
for go,component,reverse in [(2235243323553433836,8961100082026100831,0),(new[0],8961100082026100832,1)]:
 b[component]=b[7940352436091000235].replace('7940352436091000235',str(component)).replace('1808007802654680893',str(go)).replace('isReverse: 0','isReverse: '+str(reverse))
 order.append(component)
 b[go]=b[go].replace('  m_Layer:',f'  - component: {{fileID: {component}}}\n  m_Layer:')

# Reuse a sliced round sprite as a dark inset, leaving the existing gold rim visible.
# This is serialized in the prefab; no runtime hierarchy/style creation is needed.
for go,rt,renderer,img,parent in [
 (8961100082026100841,8961100082026100842,8961100082026100843,8961100082026100844,7376750644824637098),
 (8961100082026100851,8961100082026100852,8961100082026100853,8961100082026100854,2048175119597911864)]:
 originalGo=int(re.search(r'm_GameObject: \{fileID: (\d+)',b[parent])[1])
 originalImage=8860291489413871472 if parent==7376750644824637098 else 5148114096883086679
 originalRenderer=next(int(x) for x in re.findall(r'component: \{fileID: (\d+)',b[originalGo]) if b.get(int(x),'').startswith('--- !u!222'))
 remap={originalGo:go,parent:rt,originalRenderer:renderer,originalImage:img}
 for a,c in remap.items():
  block=b[a]
  for x,y in remap.items():block=block.replace(str(x),str(y))
  b[c]=block;order.append(c)
 field(go,'m_Name','DarkInset')
 field(rt,'m_Father','{fileID: '+str(parent)+'}')
 b[rt]=re.sub(r'  m_Children:.*?\n  m_Father:', '  m_Children: []\n  m_Father:',b[rt],flags=re.S)
 for k,v in {'m_AnchorMin':'{x: 0, y: 0}','m_AnchorMax':'{x: 1, y: 1}','m_AnchoredPosition':'{x: 0, y: 0}','m_SizeDelta':'{x: -10, y: -10}'}.items():field(rt,k,v)
 field(img,'m_Color','{r: 0.23, g: 0.25, b: 0.23, a: 1}')
 field(img,'m_RaycastTarget',0)
 if '  m_Children: []' in b[parent]: b[parent]=b[parent].replace('  m_Children: []',f'  m_Children:\n  - {{fileID: {rt}}}')
 else:b[parent]=b[parent].replace('  m_Children:\n',f'  m_Children:\n  - {{fileID: {rt}}}\n',1)

# Use the default locale's configured art in the serialized preview; runtime still
# refreshes these through the existing WzIconAmend/country configuration.
for img,name in [(2738604318736417619,'PileGold_US'),(7522282916600658977,'PileMoney_US'),(181572282916775767,'MoneyEnhancement_US'),(5462716822942165971,'MoneyEnhancement_US'),(8246477513213878394,'StackMoney_US')]:
 meta=(root/f'Assets/BizzaWZ/Final/Real/GameAssets/WzTexture_Money/{name}.png.meta').read_text()
 guid=re.search(r'guid: (\w+)',meta)[1]
 field(img,'m_Sprite',f'{{fileID: 21300000, guid: {guid}, type: 3}}')
p.write_text(s[:s.index('--- !u!')]+'\n'.join(b[i].rstrip() for i in order)+'\n',encoding='utf-8',newline='\n')
print('Added dark progress insets and mode-specific caption.')
