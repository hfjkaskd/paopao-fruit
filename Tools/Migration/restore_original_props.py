from pathlib import Path
import re
src=Path('C:/Projects/FruitsHarvestMaster/output_Unity/Assets/res/local/coreplay/CorePlayUI.prefab').read_text()
p=Path('BizzaWZ/Assets/FruitsHarvest/Resources/Original/res/local/coreplay/CorePlayUI.prefab'); dst=p.read_text()
def blocks(s): return {m[1]:m[0] for m in re.findall(r'(--- !u!\d+ &(\d+)\n.*?)(?=--- !u!|\Z)',s,re.S)}
a,b=blocks(src),blocks(dst)
selected=set()
for key in ['m_UndoBtn','m_MagicBtn','m_ShuffleBtn','m_AddOneBtn']:
 cid=re.search(key+r': \{fileID: (\d+)',src)[1]
 gid=re.search(r'm_GameObject: \{fileID: (\d+)',a[cid])[1]
 tid=re.search(r'component: \{fileID: (\d+)',a[gid])[1]
 def walk(t):
  g=re.search(r'm_GameObject: \{fileID: (\d+)',a[t])[1]
  selected.add(g)
  selected.update(re.findall(r'component: \{fileID: (\d+)',a[g]))
  children=a[t].split('  m_Children:')[1].split('  m_Father:')[0]
  for c in re.findall(r'fileID: (\d+)',children): walk(c)
 walk(tid)
 parent=re.search(r'm_Father: \{fileID: (\d+)',a[tid])[1]
 assert parent in b
 b[parent]=b[parent].replace('  m_Children: []','  m_Children:').replace('  m_Children:\n','  m_Children:\n  - {fileID: '+tid+'}\n')
 for k in b:
  b[k]=re.sub(r'('+key+r': \{fileID: )0',r'\g<1>'+cid,b[k])
assert not selected.intersection(b), 'Already restored or IDs conflict'
b.update({k:a[k] for k in a if k in selected})
p.write_text('%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n'+''.join(b.values()))
print('Restored',len(selected),'serialized objects')
