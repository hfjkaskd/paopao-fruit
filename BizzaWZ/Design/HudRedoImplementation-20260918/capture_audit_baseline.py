from pathlib import Path
import json,re,hashlib,shutil
root=Path(__file__).resolve().parents[2]
out=Path(__file__).resolve().parent
# Snapshot exact current production before the approved second HUD pass; never overwrite a pre-existing baseline.
paths=[
'Assets/BizzaWZ/Common/UI/GamePanel/RealGamePanel.prefab',
'Assets/BizzaWZ/Final/MenuSystem/Common/CurrencyBar/CurrencyBar.prefab',
'Assets/BizzaWZ/Final/Real/UI/GamePanel/Resources/Resources/GameUiWidget.prefab',
'Assets/FruitsHarvest/Resources/Original/res/local/coreplay/CorePlayUI.prefab',
'Assets/BizzaWZ/Final/Real/UI/DailyMissionPanel/DailyMissionPanel.prefab',
'Assets/BizzaWZ/Final/Real/UI/SlotsPanel/SlotEnter/SlotEnter.prefab']
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def create_json(p,data):
 with p.open('x',encoding='utf-8') as f:json.dump(data,f,ensure_ascii=False,indent=2);f.write('\n')
backups=[]
for rel in paths:
 source=root/rel;dest=out/'Before'/rel;dest.parent.mkdir(parents=True,exist_ok=True)
 if dest.exists():raise FileExistsError('Refusing to overwrite baseline '+str(dest))
 with dest.open('xb') as f:f.write(source.read_bytes())
 text=source.read_text(encoding='utf-8-sig');objects=re.findall(r'^--- !u!(\d+) &(\d+)( stripped)?$',text,re.M)
 backups.append({'path':rel,'sha256':sha(source),'serializedObjects':[{'class':t,'fileID':i,'stripped':bool(s)} for t,i,s in objects],'guardOnly':rel.endswith('DailyMissionPanel.prefab') or rel.endswith('SlotEnter.prefab')})
code={p.relative_to(root).as_posix():sha(p) for p in sorted((root/'Assets').rglob('*.cs'))}
create_json(out/'audit-code-hashes.json',code)
art={p.relative_to(root).as_posix():sha(p) for folder in ['Assets/OrchardUI/Art','Assets/OrchardUI/Resources/OrchardUI'] for p in sorted((root/folder).rglob('*')) if p.is_file()}
create_json(out/'audit-art-hashes.json',art)
create_json(out/'audit-baseline.json',{'scope':'Approved natural wood HUD, gift entry and tray lock; other serialized fields are frozen. DailyMissionPanel and SlotEnter are guard-only copies.','codeFileCount':len(code),'prefabs':backups})
print(json.dumps({'prefabsBackedUp':len(backups),'csFiles':len(code),'artFilesHashed':len(art),'output':str(out)}))
