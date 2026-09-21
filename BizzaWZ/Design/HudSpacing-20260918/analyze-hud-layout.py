"""Read-only Prefab layout model; never writes Assets or controls Unity.

The screen rectangles are analytical estimates of configured RectTransforms,
not a Unity runtime render. Currency animation extremes are included.
"""
from pathlib import Path
import copy, json, re

HERE = Path(__file__).resolve().parent
PROJECT = HERE.parents[1]
CURRENCY = PROJECT / 'Assets/BizzaWZ/Final/MenuSystem/Common/CurrencyBar/CurrencyBar.prefab'
WIDGET = PROJECT / 'Assets/BizzaWZ/Final/Real/UI/GamePanel/Resources/Resources/GameUiWidget.prefab'
SOURCE_GUID = 'b1f781d8c27eec54db58a4849081f7e5'
INSTANCE = '461910548945330748'

def blocks(text):
    return {m[2]: (m[1], m[3]) for m in re.finditer(r'^--- !u!(\d+) &(-?\d+)[^\n]*\n(.*?)(?=^--- !u!|\Z)', text, re.M | re.S)}

def value(text):
    if text.startswith('{x:'):
        return {k: float(v) for k, v in re.findall(r'([xyzw]): ([^,}]+)', text)}
    try: return float(text)
    except ValueError: return text

raw = blocks(CURRENCY.read_text(encoding='utf-8-sig'))
data = {fid: {k: value(v) for k, v in re.findall(r'^  (\w+): (.*)$', b, re.M)} for fid, (_, b) in raw.items()}
for fid, (_, body) in raw.items():
    padding=re.search(r'^  m_Padding:\n((?:    m_\w+: [^\n]*\n)+)',body,re.M)
    if padding: data[fid]['m_Padding']={k:float(v) for k,v in re.findall(r'    (m_\w+): ([^\n]+)',padding[1])}
widget_blocks = blocks(WIDGET.read_text(encoding='utf-8-sig'))
instance = widget_blocks[INSTANCE][1]
overrides = re.findall(r'    - target: \{fileID: (\d+),[^\n]*\n      propertyPath: (\S+)\n      value: ([^\n]*)\n      objectReference: ([^\n]*)', instance)
for fid, prop, val, ref in overrides:
    if fid not in data: continue
    if '.' in prop:
        field, axis = prop.rsplit('.', 1)
        if field in data[fid] and isinstance(data[fid][field], dict) and axis in data[fid][field]: data[fid][field][axis] = value(val)
    elif prop in data[fid]: data[fid][prop] = value(val) if val else ref

changes = []
def recommend(fid, **fields):
    for key, val in fields.items():
        if isinstance(val, tuple):
            for axis, v in zip('xyzw', val): changes.append({'sourceFileID': str(fid), 'propertyPath': key + '.' + axis, 'value': v})
        else: changes.append({'sourceFileID': str(fid), 'propertyPath': key, 'value': val})

# Only instance overrides in GameUiWidget are proposed. Shared CurrencyBar stays unchanged.
recommend(3651222385711667471, m_SizeDelta=(734, 96), m_AnchoredPosition=(0, -21))
recommend(5570188000274945229, m_Spacing=34, m_ChildAlignment=4, m_ChildForceExpandWidth=0, m_ChildForceExpandHeight=0, m_ChildScaleWidth=0, m_ChildScaleHeight=0)
recommend(5570188000274945229, **{'m_Padding.m_Left':0,'m_Padding.m_Right':0,'m_Padding.m_Top':0,'m_Padding.m_Bottom':0})
for fid in (8418649310548096022, 5563873117335086813): recommend(fid, m_SizeDelta=(350, 96))
for fid in (986012980147803529, 7312935159393972131): recommend(fid, m_AnchoredPosition=(0, 0))
for fid in (5323147067710936225, 3279889485425305485): recommend(fid, m_AnchoredPosition=(0, 0), m_SizeDelta=(280, 78), m_Pivot=(0.5, 0.5))
recommend(8565699910017466507, m_AnchoredPosition=(-128, 0), m_SizeDelta=(80, 80))
recommend(5857102194849081367, m_AnchoredPosition=(-126, 0), m_SizeDelta=(80, 80))
recommend(5465731539300563175, m_AnchoredPosition=(-39, 0), m_SizeDelta=(102, 66))
recommend(3019010923791637543, m_AnchoredPosition=(-90, 0), m_SizeDelta=(102, 66))
for fid in (8063755314690900855, 8271500437455171498): recommend(fid, m_AnchoredPosition=(77, 0), m_SizeDelta=(116, 60))
for fid in (8613941580019178654, 703687057455739089):
    recommend(fid, m_AnchorMin=(0.5, 0.5), m_AnchorMax=(0.5, 0.5), m_AnchoredPosition=(-9, 0), m_SizeDelta=(298, 88))
recommend(1086762022328476964, m_AnchoredPosition=(-456, -21), m_SizeDelta=(120, 96))
recommend(9132789315795967765, m_AnchoredPosition=(0, -6), m_SizeDelta=(88, 66))
recommend(5860728120506579172, m_enableAutoSizing=1, m_fontSize=46, m_fontSizeBase=46, m_fontSizeMin=24, m_fontSizeMax=46)
recommend(7083672919696189805, m_AnchoredPosition=(444, -21), m_SizeDelta=(96, 96))
for fid in (5991443623447234360, 1921367610482460735):
    recommend(fid, m_enableAutoSizing=1, m_fontSize=32, m_fontSizeBase=32, m_fontSizeMin=18, m_fontSizeMax=32)
recommend(2580238628719512035, m_AnchoredPosition=(-42, 0), m_SizeDelta=(-96, 0))
recommend(4119130753064406799, m_enableAutoSizing=1, m_fontSize=30, m_fontSizeBase=30, m_fontSizeMin=20, m_fontSizeMax=30)
recommend(6056023286379245574, m_AnchoredPosition=(9, 0), m_SizeDelta=(-40, 0))
recommend(8953712310009366182, m_enableAutoSizing=1, m_fontSize=24, m_fontSizeBase=24, m_fontSizeMin=12, m_fontSizeMax=24)
recommend(1285147535369721916, m_AnchoredPosition=(0, 0), m_SizeDelta=(92, 46))
recommend(4821673847536142600, m_enableAutoSizing=1, m_fontSize=26, m_fontSizeBase=26, m_fontSizeMin=20, m_fontSizeMax=26)
for fid in (5860728120506579172, 5991443623447234360, 1921367610482460735, 4119130753064406799, 8953712310009366182, 4821673847536142600):
    recommend(fid, m_enableWordWrapping=0, m_margin=(0, 0, 0, 0))

proposed = copy.deepcopy(data)
for c in changes:
    fid, prop, val = c['sourceFileID'], c['propertyPath'], c['value']
    if '.' in prop:
        field, axis = prop.split('.')
        proposed[fid][field][axis] = val
    else: proposed[fid][prop] = val

RECTS = {fid for fid, (typ, _) in raw.items() if typ == '224'}
PARENTS = {fid: re.search(r'fileID: (\d+)', data[fid]['m_Father'])[1] for fid in RECTS}
ROLES = {
 'level': '1086762022328476964', 'settings': '7083672919696189805',
 'gold_card': '5323147067710936225', 'gold_icon': '8565699910017466507',
 'gold_button_visual': '8063755314690900855', 'gold_amount': '5465731539300563175', 'gold_hit': '8613941580019178654',
 'cash_card': '3279889485425305485', 'cash_icon': '5857102194849081367',
 'cash_button_visual': '8271500437455171498', 'cash_amount': '3019010923791637543', 'cash_hit': '703687057455739089',
 'level_text': '9132789315795967765', 'approx_text': '2580238628719512035', 'conversion_text': '6056023286379245574', 'withdraw_text': '1285147535369721916',
}

def model(config, width, height, gold_scale=1, cash_scale=1, button_scale=1):
    scale = height / 2360
    root_fid = '6093203839114191277'
    # RealGamePanel.Content has a top inset of 80 units. Nested CurrencyBar y=-14.
    cache = {root_fid: {'size': (width / scale, 100), 'pivot': (.5,.5), 'position': (0,-94), 'scale': (1,1)}}
    layout_id = '5570188000274945229'
    group_id = '3651222385711667471'
    child_ids = ('8418649310548096022', '5563873117335086813')
    animation_scales = dict(zip(child_ids, (gold_scale, cash_scale)))
    animation_scales.update({'8063755314690900855': button_scale, '8271500437455171498': button_scale})
    def rect(fid):
        if fid in cache: return cache[fid]
        d=config[fid]; parent=rect(PARENTS[fid]); ps=parent['size']; pp=parent['pivot']; pos=parent['position']; psc=parent['scale']
        amin=d['m_AnchorMin']; amax=d['m_AnchorMax']; pivot=d['m_Pivot']; delta=d['m_SizeDelta']; anch=d['m_AnchoredPosition']; ownscale=d['m_LocalScale']
        size=tuple(ps[i]*(amax[k]-amin[k])+delta[k] for i,k in enumerate('xy'))
        local=tuple(ps[i]*(amin[k]+(amax[k]-amin[k])*pivot[k]-pp[i])+anch[k] for i,k in enumerate('xy'))
        if fid in child_ids:
            lg=config[layout_id]; widths=[config[x]['m_SizeDelta']['x'] for x in child_ids]
            factors=[animation_scales[x] if lg['m_ChildScaleWidth'] else 1 for x in child_ids]
            total=sum(w*s for w,s in zip(widths,factors))+lg['m_Spacing']
            padding=lg['m_Padding']; available=ps[0]-padding['m_Left']-padding['m_Right']
            start=padding['m_Left']+max(0, available-total) * (int(lg['m_ChildAlignment']) % 3) / 2
            index=child_ids.index(fid)
            left=start+sum(widths[j]*factors[j]+lg['m_Spacing'] for j in range(index))
            local_x=-ps[0]*pp[0]+left+size[0]*pivot['x']*factors[index]
            fy=animation_scales[fid] if lg['m_ChildScaleHeight'] else 1
            align_y=int(lg['m_ChildAlignment'])//3
            top=max(0,ps[1]-size[1]*fy)*align_y/2
            local_y=ps[1]*(1-pp[1])-top-size[1]*(1-pivot['y'])*fy
            local=(local_x,local_y)
        extra=animation_scales.get(fid,1)
        result={'size':size, 'pivot':tuple(pivot[k] for k in 'xy'), 'position':tuple(pos[i]+local[i]*psc[i] for i in range(2)), 'scale':tuple(psc[i]*ownscale[k]*extra for i,k in enumerate('xy'))}
        cache[fid]=result
        return result
    result={}
    for name,fid in ROLES.items():
        r=rect(fid); x=r['position'][0]-r['size'][0]*r['pivot'][0]*r['scale'][0]; y=r['position'][1]+r['size'][1]*(1-r['pivot'][1])*r['scale'][1]
        result[name]={'x':width/2+x*scale,'top':-y*scale,'width':r['size'][0]*r['scale'][0]*scale,'height':r['size'][1]*r['scale'][1]*scale}
    def union(names):
        rs=[result[n] for n in names]; return min(r['x'] for r in rs),max(r['x']+r['width'] for r in rs)
    bands=[union(['level']),union(['gold_card','gold_icon','gold_button_visual']),union(['cash_card','cash_icon','cash_button_visual']),union(['settings'])]
    gaps=[bands[i+1][0]-bands[i][1] for i in range(3)]
    hits=[union(['gold_hit']),union(['cash_hit']),union(['settings'])]
    hit_gaps=[hits[i+1][0]-hits[i][1] for i in range(2)]
    return {'screen':[width,height], 'scales':{'gold':gold_scale,'cash':cash_scale,'buttons':button_scale}, 'rects':result, 'visibleBands':bands,'visibleGaps':gaps,'hitGaps':hit_gaps,'screenMargins':[bands[0][0],width-bands[-1][1]]}

report={'kind':'Analytical Prefab RectTransform model; NOT Unity runtime screenshot','sourcePrefab':str(CURRENCY),'productionTarget':str(WIDGET),'instanceFileID':INSTANCE,'sourceGuid':SOURCE_GUID,'recommendedOverrides':changes,'assumptions':['CanvasScaler reference 1080x2360 and MatchHeight=1','RealGamePanel.Content top inset 80, CurrencyBar root top offset 14 unchanged','Sprite transparent padding may add visible space beyond measured rectangular bounds','Groups animate 1.25x; green ButtonView existing tween max1.08x; HLG scale flags disabled in proposal','No Canvas SafeArea behavior changed, no new layout scripts or nodes required'],'before':[],'proposed':[]}
for label,cfg in [('before',data),('proposed',proposed)]:
    for screen in [(1080,1920),(1080,2340)]:
        for gs,cs,bs in [(1,1,1),(1.25,1,1.08),(1,1.25,1.08),(1.25,1.25,1.08)]: report[label].append(model(cfg,*screen,gs,cs,bs))
for case in report['proposed']:
    assert min(case['visibleGaps']) >= 0, case
    assert min(case['hitGaps']) >= 0, case
    assert min(case['screenMargins']) >= 20, case
    # Amounts stay inside the corresponding ivory counter, even at animation extremes.
    for kind in ('gold','cash'):
        text=case['rects'][kind+'_amount']; card=case['rects'][kind+'_card']
        assert text['x']>=card['x'] and text['x']+text['width']<=card['x']+card['width']
    approx=case['rects']['approx_text']; conversion=case['rects']['conversion_text']
    assert approx['x']+approx['width']<=conversion['x']
    for kind in ('gold','cash'):
        amount=case['rects'][kind+'_amount']; button=case['rects'][kind+'_button_visual']
        assert amount['x']+amount['width']<=button['x']
font_text=(PROJECT/'Assets/BizzaWZ/Common/Framework/Res/Fonts/MainFont_Simple.asset').read_text(encoding='utf-8-sig')
glyphs={int(i):float(v) for i,v in re.findall(r'  - m_Index: (\d+)\n    m_Metrics:.*?      m_HorizontalAdvance: ([^\n]+)',font_text,re.S)}
char_advances={chr(int(u)):glyphs[int(i)] for u,i in re.findall(r'    m_Unicode: (\d+)\n    m_GlyphIndex: (\d+)',font_text)}
report['textWidthEstimates']=[]
for text,width,font_min,font_max in [('99.999,99',102,18,32),('9999',88,24,46),('R$0,00',76,16,24),('Retirar',92,20,26)]:
    factor=sum(char_advances[c] for c in text)/32
    fitted=min(font_max,width/factor)
    assert fitted>=font_min
    report['textWidthEstimates'].append({'text':text,'width':width,'minimumFont':font_min,'maximumFont':font_max,'estimatedFittedFont':fitted,'method':'MainFont_Simple serialized glyph advances, without kerning or TMP material padding; confirm in TMP render'})
report['approxFallbackNote']='U+2248 is absent from MainFont_Simple character table. Preserve existing font/fallback assets and verify actual TMP glyph bounds within20-unit text rect.'
report['checksPassed']=['all proposed group visual bounds non-overlapping at four animation states and both screens','all proposed currency hit bounds separated from each other and settings','both screen margins >=20px','dynamic amount RectTransforms stay within their card bounds']
(HERE/'hud-layout-proposal.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
for c in report['proposed']:
    print(c['screen'],c['scales'],'gaps',[round(x,2) for x in c['visibleGaps']],'hit gaps',[round(x,2) for x in c['hitGaps']],'margins',[round(x,2) for x in c['screenMargins']])
print('Report only. No production files changed.')
