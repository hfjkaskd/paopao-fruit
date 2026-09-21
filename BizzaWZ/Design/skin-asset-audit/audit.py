from pathlib import Path
import re,json,csv
from collections import Counter,defaultdict
from PIL import Image,ImageDraw,ImageFont

ROOT=Path(__file__).resolve().parents[2]
OUT=Path(__file__).parent
ASSETS=ROOT/'Assets'
textures={}
for p in (ASSETS/'BizzaWZ').rglob('*.png'):
    if '/Z_ReplaceAssets/UI_Frame/' not in p.as_posix(): continue
    meta=p.with_suffix('.png.meta').read_text(encoding='utf-8-sig')
    guid=re.search(r'^guid: (\w+)',meta,re.M).group(1)
    border=re.search(r'spriteBorder: (.+)',meta).group(1)
    im=Image.open(p)
    textures[guid]={'path':p.relative_to(ROOT).as_posix(),'guid':guid,'width':im.width,'height':im.height,'border':border,'refs':[],'colors':Counter(),'objects':[]}
prefabs=[]
for p in (ASSETS/'BizzaWZ').rglob('*.prefab'):
    if '/ThirdParty/' in p.as_posix() or '/ArtPlugins/' in p.as_posix(): continue
    txt=p.read_text(encoding='utf-8-sig')
    for guid in set(re.findall(r'guid: ([a-f0-9]{32})',txt)):
        if guid in textures:
            textures[guid]['refs'].append(p.relative_to(ROOT).as_posix())
    blocks=re.split(r'(?=^--- !u!)',txt,flags=re.M)
    names={}
    for b in blocks:
        m=re.match(r'--- !u!1 &(\d+)\n',b)
        if m:
            nm=re.search(r'^  m_Name: (.*)$',b,re.M)
            names[m.group(1)]=nm.group(1) if nm else '?'
    refs=[]; images=[]; texts=[]
    for b in blocks:
        go=re.search(r'^  m_GameObject: \{fileID: (\d+)\}',b,re.M)
        name=names.get(go.group(1),'?') if go else '?'
        c=re.search(r'^  m_Color: (.*)',b,re.M)
        t=re.search(r'^  m_[Tt]ext: (.*)',b,re.M)
        tc=re.search(r'^  m_fontColor: (.*)',b,re.M)
        sp=re.search(r'^  m_Sprite: \{fileID: [^,]+, guid: ([a-f0-9]+), type: \d+\}',b,re.M)
        typ=re.search(r'^  m_Type: (\d+)',b,re.M)
        if sp:
            guid=sp.group(1); refs.append(guid)
            item={'name':name,'guid':guid,'path':textures.get(guid,{}).get('path','OTHER'),'color':c.group(1) if c else '', 'type':typ.group(1) if typ else ''}
            images.append(item)
            if guid in textures:
                entry=textures[guid];entry['refs'].append(p.relative_to(ROOT).as_posix());entry['colors'][item['color']]+=1;entry['objects'].append({'prefab':p.relative_to(ROOT).as_posix(),**item})
        if t: texts.append({'name':name,'text':t.group(1),'color':tc.group(1) if tc else c.group(1) if c else ''})
    if images or texts: prefabs.append({'path':p.relative_to(ROOT).as_posix(),'images':images,'texts':texts})
rows=[]
for t in textures.values():
    t['colors']=dict(t['colors']);t['refs']=sorted(set(t['refs']))
    rows.append({k:v for k,v in t.items() if k!='objects'})
(OUT/'textures.json').write_text(json.dumps(list(textures.values()),ensure_ascii=False,indent=2),encoding='utf-8')
(OUT/'prefabs.json').write_text(json.dumps(prefabs,ensure_ascii=False,indent=2),encoding='utf-8')
with (OUT/'textures.csv').open('w',newline='',encoding='utf-8-sig') as f:
    w=csv.writer(f);w.writerow(['path','guid','width','height','spriteBorder','prefab_count','colors','prefabs'])
    for t in sorted(rows,key=lambda x:(-len(x['refs']),x['path'])):w.writerow([t['path'],t['guid'],t['width'],t['height'],t['border'],len(t['refs']),json.dumps(t['colors']),'\n'.join(t['refs'])])
font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',13)
ts=sorted(textures.values(),key=lambda t:t['path'])
for page in range((len(ts)+39)//40):
    subset=ts[page*40:(page+1)*40]
    sheet=Image.new('RGB',(1200,1100),'#dae2e3');d=ImageDraw.Draw(sheet)
    for i,t in enumerate(subset):
        x=i%5*240;y=i//5*137
        src=Image.open(ROOT/t['path']).convert('RGBA');src.thumbnail((225,95))
        sheet.paste(src,(x+(240-src.width)//2,y),src)
        label=Path(t['path']).name
        d.text((x+5,y+97),label,fill='#152830',font=font)
        d.text((x+5,y+115),f"{t['width']}x{t['height']} refs {len(t['refs'])}",fill='#152830',font=font)
    sheet.save(OUT/f'contact-{page+1}.jpg')
print(f'{len(textures)} textures; {len(prefabs)} prefabs; {len(ts)//40+1} contact sheets')
print('\n'.join(f"{len(t['refs']):2} {t['width']}x{t['height']} {t['border']} {t['path']}" for t in sorted(rows,key=lambda x:-len(x['refs']))[:35]))
