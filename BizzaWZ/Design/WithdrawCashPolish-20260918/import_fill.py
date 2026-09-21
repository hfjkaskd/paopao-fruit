from pathlib import Path
import re, json, uuid, shutil, hashlib
out=Path(__file__).resolve().parent
project=out.parents[1]
source=Path('C:/Users/pc/.codex/generated_images/01a0af0e-fa04-75a3-8fd6-557bcd874b3f/exec-0666a8b8-bc9d-4144-a26f-bb7ec2b3f03c.png')
asset=project/'Assets/OrchardUI/Art/WithdrawProgressSoftFill.png'
assert not asset.exists() and not asset.with_suffix('.png.meta').exists()
meta=(project/'Assets/OrchardUI/Art/HudNaturalCounter.png.meta').read_text(encoding='utf-8')
guid=uuid.uuid4().hex
meta=meta.replace('dbf870bbe4a14284b631a4cd409d5147',guid).replace('HudNaturalCounter','WithdrawProgressSoftFill')
meta=meta.replace('x: 60\n        y: 159\n        width: 1968\n        height: 442','x: 16\n        y: 421\n        width: 1728\n        height: 53')
meta=meta.replace('border: {x: 155, y: 95, z: 155, w: 95}','border: {x: 0, y: 0, z: 0, w: 0}')
meta=meta.replace('maxTextureSize: 512','maxTextureSize: 1024')
meta=re.sub(r'(spriteID: )[0-9a-f]{32}',lambda m:m[1]+uuid.uuid4().hex,meta)
shutil.copyfile(source,asset)
asset.with_suffix('.png.meta').write_text(meta,encoding='utf-8',newline='\n')
record={'asset':asset.relative_to(project).as_posix(),'guid':guid,'fileID':21300000,
 'source':str(source),'sha256':hashlib.sha256(asset.read_bytes()).hexdigest(),'generatedRgbaUnmodified':asset.read_bytes()==source.read_bytes(),
 'textureSize':[1760,894],'spriteRect':[16,421,1728,53],
 'rendering':'Image Filled Horizontal Left, PreserveAspect enabled; visible end cap proportions retained, letterbox within existing inset fill rectangle.',
 'note':'The generator returned a thinner strip than requested. PreserveAspect avoids distorting its end caps. Sprite crop is importer metadata only.'}
(out/'Art').mkdir(exist_ok=True)
(out/'Art/import.json').write_text(json.dumps(record,indent=2),encoding='utf-8')
print(json.dumps(record))
