"""Apply approved R1 colours to existing UI sprites, preserving geometry/imports.

No generated imagery. Originals are immutable backups; repeat runs use backups.
"""
from pathlib import Path
import colorsys, hashlib, json, shutil
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
HERE = Path(__file__).resolve().parent
assets = json.loads((HERE/'resource-map.json').read_text(encoding='utf-8'))['shared_ui_assets']
# Explicitly reviewed contact-sheet entries, not a global asset-name replacement.
gold = {0,5,30,40,54}
blue = {1,11,12,17,42,43,46,50,82,83,87,92}
manifest = []
for index in sorted(gold | blue):
    entry = assets[index]
    path = ROOT/entry['asset']
    backup = HERE/'OriginalAssets'/entry['asset']
    backup.parent.mkdir(parents=True, exist_ok=True)
    if not backup.exists():
        shutil.copy2(path, backup)
        shutil.copy2(Path(str(path)+'.meta'), Path(str(backup)+'.meta'))
    original = Image.open(backup).convert('RGBA')
    output = original.copy()
    pixels = output.load()
    count = 0
    for y in range(output.height):
        for x in range(output.width):
            r,g,b,a = pixels[x,y]
            h,s,v = colorsys.rgb_to_hsv(r/255,g/255,b/255)
            machine = index in {82,83,87,92}
            selected = (.60 < h < .95 and s > .015) if machine else (.64 < h < .86 and s > .19)
            if a and selected and v > .2:
                hue, saturation = (.105,1.4) if index in gold else (.555,1.5)
                if index == 17: hue,saturation = .57,1.1
                # Preserve pale painted highlights; hard saturation cutoffs leave
                # magenta speckles in the machine's textured awning.
                target_s = min(.85,s*saturation) if machine else min(.85,max(.25,s*saturation))
                rgb = colorsys.hsv_to_rgb(hue,target_s,v)
                pixels[x,y] = (*[round(c*255) for c in rgb],a)
                count += 1
    assert output.size == original.size
    assert output.getchannel('A').tobytes() == original.getchannel('A').tobytes()
    assert Path(str(path)+'.meta').read_bytes() == Path(str(backup)+'.meta').read_bytes()
    output.save(path)
    manifest.append(dict(entry, direction='warm-gold' if index in gold else 'blue',
        backup=str(backup.relative_to(ROOT)), size=output.size, changed_pixels=count,
        result_sha256=hashlib.sha256(path.read_bytes()).hexdigest(),
        meta_sha256=hashlib.sha256(Path(str(path)+'.meta').read_bytes()).hexdigest(),
        alpha_unchanged=True))
(HERE/'applied-manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
print(f'Applied approved palette to {len(manifest)} sprites; dimensions, alpha and all .meta files unchanged.')
