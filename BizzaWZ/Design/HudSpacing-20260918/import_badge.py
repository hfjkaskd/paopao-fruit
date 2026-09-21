from pathlib import Path
import re, shutil, uuid, json, hashlib

project = Path(__file__).resolve().parents[2]
out = Path(__file__).resolve().parent
source = Path('C:/Users/pc/.codex/generated_images/01a0af0e-fa04-75a3-8fd6-557bcd874b3f/exec-970945dd-7847-4021-89af-563a18c3277c.png')
asset = project / 'Assets/OrchardUI/Art/HudLevelCreamLeaf.png'
if asset.exists() or asset.with_suffix('.png.meta').exists():
    raise FileExistsError('Do not overwrite a prior badge import')
meta = (project / 'Assets/OrchardUI/Art/HudNaturalWoodTile.png.meta').read_text(encoding='utf-8')
guid = uuid.uuid4().hex
meta = meta.replace('e2548555c85142bab2890d2c35044f37', guid).replace('HudNaturalWoodTile', 'HudLevelCreamLeaf')
meta = meta.replace('x: 74\n        y: 78\n        width: 1105\n        height: 1081',
                    'x: 134\n        y: 159\n        width: 1205\n        height: 845')
meta = meta.replace('border: {x: 190, y: 190, z: 190, w: 190}', 'border: {x: 0, y: 0, z: 0, w: 0}')
meta = re.sub(r'(spriteID: )[0-9a-f]{32}', lambda m: m[1] + uuid.uuid4().hex, meta)
shutil.copyfile(source, asset)
asset.with_suffix('.png.meta').write_text(meta, encoding='utf-8', newline='\n')
record = {'asset': asset.relative_to(project).as_posix(), 'guid': guid, 'fileID': 21300000,
          'source': str(source), 'sourcePixelsPreserved': source.read_bytes() == asset.read_bytes(),
          'sha256': hashlib.sha256(asset.read_bytes()).hexdigest(),
          'textureSize': [1432,1098], 'spriteRect': [134,159,1205,845],
          'cropExplanation': 'Unity sprite rectangle includes visible alpha >= 8 plus a 4-pixel transparent pad; image bytes untouched.',
          'maxTextureSize': 512, 'alpha': True, 'usage': 'Main HUD existing level Image only'}
(out / 'Art/import.json').write_text(json.dumps(record, indent=2), encoding='utf-8')
print(json.dumps(record))
