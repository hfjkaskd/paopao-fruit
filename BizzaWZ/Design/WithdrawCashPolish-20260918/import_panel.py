from pathlib import Path
import re,json,uuid,shutil,hashlib
out=Path(__file__).resolve().parent
project=out.parents[1]
source=Path('C:/Users/pc/.codex/generated_images/01a0af0e-fa04-75a3-8fd6-557bcd874b3f/exec-ee9221ff-1bcd-4421-bd6f-1b491a806b12.png')
asset=project/'Assets/OrchardUI/Art/WithdrawCreamWoodPanel.png'
assert not asset.exists()
meta=(project/'Assets/OrchardUI/Art/HudNaturalCounter.png.meta').read_text(encoding='utf-8')
guid=uuid.uuid4().hex
meta=meta.replace('dbf870bbe4a14284b631a4cd409d5147',guid).replace('HudNaturalCounter','WithdrawCreamWoodPanel')
meta=meta.replace('x: 60\n        y: 159\n        width: 1968\n        height: 442','x: 61\n        y: 41\n        width: 969\n        height: 1367')
meta=meta.replace('border: {x: 155, y: 95, z: 155, w: 95}','border: {x: 128, y: 128, z: 128, w: 128}')
meta=meta.replace('maxTextureSize: 512','maxTextureSize: 1024')
meta=re.sub(r'(spriteID: )[0-9a-f]{32}',lambda m:m[1]+uuid.uuid4().hex,meta)
shutil.copyfile(source,asset)
asset.with_suffix('.png.meta').write_text(meta,encoding='utf-8',newline='\n')
record={'asset':asset.relative_to(project).as_posix(),'guid':guid,'fileID':21300000,'source':str(source),
 'sha256':hashlib.sha256(asset.read_bytes()).hexdigest(),'generatedRgbaUnmodified':asset.read_bytes()==source.read_bytes(),
 'textureSize':[1090,1443],'spriteRect':[61,41,969,1367],'borders':[128,128,128,128],'maxTextureSize':1024}
(out/'Art/panel-import.json').write_text(json.dumps(record,indent=2),encoding='utf-8')
p=project/'Assets/BizzaWZ/Final/Real/UI/FakeWithdrawPanel/FakeWithdrawPanel.prefab'
data=p.read_bytes();text=data.decode('utf-8-sig').replace('\r\n','\n')
changes=[]
for key,value in [('m_Sprite',f'{{fileID: 21300000, guid: {guid}, type: 3}}'),('m_PixelsPerUnitMultiplier','1.3')]:
 m=re.search(rf'(?ms)(^--- !u!114 &2754535437279419372\n(?:(?!^--- !u!).)*?^  {key}: )([^\n]*)',text);assert m
 changes.append({'fileID':'2754535437279419372','propertyPath':key,'before':m[2],'after':value})
 text=text[:m.start(2)]+value+text[m.end(2):]
nl='\r\n' if b'\r\n' in data else '\n'
new=text.replace('\n',nl).encode('utf-8')
if data.startswith(b'\xef\xbb\xbf'):new=b'\xef\xbb\xbf'+new
p.write_bytes(new)
manifest=json.loads((out/'applied-changes.json').read_text(encoding='utf-8'))
manifest['panelRefinement']={'description':'Purpose-drawn portrait cream and thin wood panel replaces the temporary stretched horizontal counter on the same existing Image.', 'changes':changes}
for h in manifest['hashes']:h['after']=hashlib.sha256((project/h['path']).read_bytes()).hexdigest()
(out/'applied-changes.json').write_text(json.dumps(manifest,indent=2,ensure_ascii=False),encoding='utf-8')
print(json.dumps(record))
