"""Non-AI colour study over captured Game View pixels; never writes Unity assets.

This is a style study, not an exact resource-mapping approval render.
Text, amounts, layout and non-target image regions stay in the captured image.
"""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import colorsys

ROOT = Path(__file__).resolve().parent
OUT = ROOT / 'Effects'
OUT.mkdir(exist_ok=True)
FONT = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 20)
VIEW = (355, 120, 861, 1218)

def study(name, regions):
    source = Image.open(ROOT/'Baseline'/f'{name}-window.png').convert('RGB')
    result = source.copy()
    pixels = result.load()
    for box, hue, saturation in regions:
        x0,y0,x1,y1=box
        for y in range(y0,y1):
            for x in range(x0,x1):
                r,g,b=pixels[x,y]
                h,s,v=colorsys.rgb_to_hsv(r/255,g/255,b/255)
                # Restrict to saturated purple artwork, not white lettering/shadows.
                if 0.67<h<0.83 and s>0.19 and v>0.2:
                    nr,ng,nb=colorsys.hsv_to_rgb(hue, min(0.85,max(0.25,s*saturation)),v)
                    pixels[x,y]=(round(nr*255),round(ng*255),round(nb*255))
    left=source.crop(VIEW); right=result.crop(VIEW)
    left.save(OUT/f'{name}-original.png')
    right.save(OUT/f'{name}-style-study.png')
    sheet=Image.new('RGB',(left.width*2+24,left.height+86),'#142231')
    sheet.paste(left,(0,70)); sheet.paste(right,(left.width+24,70))
    d=ImageDraw.Draw(sheet)
    d.text((12,10),'ORIGINAL GAME',font=FONT,fill='white')
    d.text((left.width+36,10),'TARGET EFFECT - STYLE STUDY R1',font=FONT,fill='white')
    d.text((12,39),'Full Game View / same captured state and scale',font=FONT,fill='#b9ccdd')
    sheet.save(OUT/f'{name}-comparison.png')

# Regions are measured in the baseline screenshots, not assumed sprite dimensions.
study('daily-mission', [
    ((409,370,802,474), .105, 1.4),
    ((380,418,425,978), .105, 1.4),
    ((783,418,835,978), .105, 1.4),
    ((380,922,835,979), .105, 1.4),
    ((495,802,721,887), .555, 1.5),
])
study('withdraw', [
    ((355,121,861,199), .57, 1.1),
    ((369,292,845,333), .105, 1.4),
    ((369,328,395,904), .105, 1.4),
    ((824,328,845,904), .105, 1.4),
    ((370,880,844,927), .105, 1.4),
    ((497,760,722,843), .555, 1.5),
])
