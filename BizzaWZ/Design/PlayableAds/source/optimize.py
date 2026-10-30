import pathlib, io
from PIL import Image
from fontTools import subset
build=pathlib.Path(__file__).resolve().parent/'build/web'
for p in build.rglob('*.ttf'):
    font=subset.load_font(str(p),subset.Options())
    sub=subset.Subsetter();sub.populate(unicodes=list(range(32,256)))
    sub.subset(font);subset.save_font(font,str(p),subset.Options())
for p in (build/'assets/main/native').rglob('*.png'):
    im=Image.open(p).convert('RGBA')
    b=io.BytesIO();im.quantize(colors=256,method=Image.Quantize.FASTOCTREE).save(b,format='PNG',optimize=True)
    if len(b.getvalue())<p.stat().st_size:p.write_bytes(b.getvalue())
print('Optimized font and PNG assets')
