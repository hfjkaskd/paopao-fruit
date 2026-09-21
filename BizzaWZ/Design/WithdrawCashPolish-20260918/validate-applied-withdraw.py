"""Independent read-only validation against the cash-page turn baseline."""
from pathlib import Path
from datetime import datetime, timezone
import hashlib, json, re

HERE=Path(__file__).resolve().parent
PROJECT=HERE.parents[1]
baseline=json.loads((HERE/'baseline.json').read_text(encoding='utf-8'))
applied=json.loads((HERE/'applied-changes.json').read_text(encoding='utf-8'))
panel='Assets/BizzaWZ/Final/Real/UI/FakeWithdrawPanel/FakeWithdrawPanel.prefab'
item='Assets/BizzaWZ/Final/Real/UI/FakeWithdrawPanel/WithdrawAmountItem.prefab'
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
report={'kind':'Independent serialized-data, resource and analytical fill verification. Not a runtime screenshot or business test.','errors':[]}
def check(cond,message):
    if not cond: report['errors'].append(message)
def blocks(text): return {m[2]:(m[1],m[0]) for m in re.finditer(r'^--- !u!(\d+) &(-?\d+)[^\n]*\n(.*?)(?=^--- !u!|\Z)',text,re.M|re.S)}
def fields(block): return dict(re.findall(r'^  (\w+): *(.*)$',block,re.M))
modpattern=r'    - target: \{fileID: (\d+), guid: ([^,]+), type: (\d+)\}\n      propertyPath: (\S+)\n      value: ([^\n]*)\n      objectReference: ([^\n]*)\n'
def mods(block):
    pairs=[((m[0],m[1],m[2],m[3]),(m[4],m[5])) for m in re.findall(modpattern,block)]
    check(len(dict(pairs))==len(pairs),'Duplicate nested override')
    return dict(pairs)

normal='3062039195826043058'; available='5455967628476765425'; claimed='1584464498381216905'; amount='2196861301424074241'
instances={'735769260790555943','921752336444412338','3145375543058054682','3366821845968657574','6389070758414889696','9024996893891000631'}
source_guid='5fefc70f9b2f3f1488b2b221199dcf24'
font_fields={'m_fontSize','m_fontSizeBase','m_fontSizeMax'}
allowed_fields={
 '2754535437279419372':{'m_Sprite','m_Type','m_PixelsPerUnitMultiplier'},
 '6615170243285179318':{'m_Sprite','m_Type','m_PixelsPerUnitMultiplier'},
 '1241199525063855708':{'m_Sprite','m_Type','m_PixelsPerUnitMultiplier'},
 '8743834095267926959':{'m_Sprite','m_Type','m_PixelsPerUnitMultiplier'},
 '4192234944055056232':{'m_PixelsPerUnitMultiplier'},
 '6004225462979585317':font_fields|{'m_sharedMaterial','m_fontColor'},
 '8767989740261440782':font_fields|{'m_fontColor'},
 '8654869823511539002':font_fields,
 '8175018248033638516':{'m_SizeDelta'},
 '2071485895475148001':{'m_SizeDelta'},
 '6620554689692068040':{'m_Sprite','m_PreserveAspect'},
 '3339610134613169318':font_fields|{'m_sharedMaterial','m_fontColor'},
 '210600630110459938':{'m_AnchoredPosition','m_SizeDelta'},
 normal:{'m_Sprite','m_PixelsPerUnitMultiplier'},
 available:{'m_Sprite','m_PixelsPerUnitMultiplier','m_Color'}, claimed:{'m_Sprite','m_PixelsPerUnitMultiplier','m_Color'}, amount:font_fields,
 '1775683569243512909':{'m_SizeDelta','m_AnchoredPosition'},
}
allowed_nested={(fid,prop) for fid in (normal,available,claimed) for prop in ('m_Sprite','m_PixelsPerUnitMultiplier')}|{(amount,k) for k in font_fields}
diffs=[]; after_blocks={}; report['prefabs']=[]
for rel in (panel,item):
    before=(HERE/'Before'/rel).read_text(encoding='utf-8-sig'); after=(PROJECT/rel).read_text(encoding='utf-8-sig')
    bb,ab=blocks(before),blocks(after); after_blocks[rel]=ab
    check(list(bb)==list(ab),'Serialized object IDs/order changed: '+rel)
    for fid in bb:
        typ,b=bb[fid]; _,a=ab[fid]
        if b==a: continue
        if typ=='1001':
            check(fid in instances,'Unexpected modified instance '+fid)
            bm,am=mods(b),mods(a)
            check(set(bm)<=set(am),'Nested override removed '+fid)
            check(re.sub(modpattern,'',b)==re.sub(modpattern,'',a),'Nested hierarchy/bindings changed '+fid)
            for key,val in am.items():
                if bm.get(key)==val: continue
                check(key[1]==source_guid and (key[0],key[3]) in allowed_nested,'Unexpected nested property '+str(key))
                diffs.append({'file':rel,'instance':fid,'fileID':key[0],'propertyPath':key[3],'before':bm.get(key),'after':val})
        else:
            bf,af=fields(b),fields(a)
            changed={k for k in set(bf)|set(af) if bf.get(k)!=af.get(k)}
            check(changed<=allowed_fields.get(fid,set()),'Nonvisual or unexpected direct fields '+str((fid,changed)))
            allowed=allowed_fields.get(fid,set())
            remove=lambda txt:'\n'.join(line for line in txt.split('\n') if not any(line.startswith('  '+key+':') for key in allowed))
            check(remove(b)==remove(a),'Nested serialized content changed in component '+fid)
            diffs.extend({'file':rel,'fileID':fid,'propertyPath':key,'before':bf.get(key),'after':af.get(key)} for key in sorted(changed))
    report['prefabs'].append({'path':rel,'serializedObjectCount':len(bb),'hierarchyComponentOrderPreserved':list(bb)==list(ab),'before':sha(HERE/'Before'/rel),'after':sha(PROJECT/rel)})
actual_keys={(d['file'],d.get('instance'),d['fileID'],d['propertyPath']) for d in diffs}
manifest_rows=applied['changes']+applied.get('refinements',{}).get('changes',[])+applied.get('panelRefinement',{}).get('changes',[])
manifest_keys=set()
for d in manifest_rows:
    for iid in (instances if d.get('nested') else (d.get('instance'),)):
        manifest_keys.add((d.get('file',panel),iid,d['fileID'],d['propertyPath']))
# Refinements may restore a field to its baseline value, e.g. the track sprite.
check(actual_keys<=manifest_keys,'An actual final difference was not documented in the change manifest')
report['manifestCoverage']={'allActualFinalDifferencesDocumented':actual_keys<=manifest_keys,'documentedButRestoredToBaseline':[list(k) for k in sorted(manifest_keys-actual_keys,key=str)],'note':'Final change count is calculated independently from Before versus current production, not the accumulated edit history.'}
for entry in applied['hashes']:
    check(sha(PROJECT/entry['path'])==entry['after'],'Applied hash mismatch '+entry['path'])
    check(sha(HERE/'Before'/entry['path'])==entry['before'],'Before hash mismatch '+entry['path'])
report['visualChangeCount']=len(diffs)
report['actualVisualDifferences']=diffs
report['allOriginalTextBindingsEventsDataUnchanged']=not report['errors']
report['unchangedRecordedFiles']=[]
for row in baseline:
    if row['path'] in (panel,item): continue
    unchanged=sha(PROJECT/row['path'])==row['sha256']
    check(unchanged,'Recorded non-target file changed '+row['path'])
    report['unchangedRecordedFiles'].append({'path':row['path'],'unchanged':unchanged})

# All6existing cards must agree with the source. New pool entries use this same source prefab.
counter='{fileID: 21300000, guid: dbf870bbe4a14284b631a4cd409d5147, type: 3}'
source_normal=fields(after_blocks[item][normal][1])
check(source_normal['m_Sprite']==counter and source_normal['m_PixelsPerUnitMultiplier']=='4','Source normal card configuration differs')
status_colors={available:'{r: 0.78, g: 1, b: 0.82, a: 1}',claimed:'{r: 0.8, g: 0.87, b: 0.82, a: 1}'}
for fid,color in status_colors.items():
    sf=fields(after_blocks[item][fid][1])
    check(sf['m_Sprite']==counter and sf['m_PixelsPerUnitMultiplier']=='4' and sf['m_Color']==color,'Source card state differs '+fid)
cards=[]
for iid in sorted(instances):
    mm=mods(after_blocks[panel][iid][1]); key=lambda fid,prop:(fid,source_guid,'3',prop)
    sprite=mm[key(normal,'m_Sprite')][1]; ppu=mm[key(normal,'m_PixelsPerUnitMultiplier')][0]
    check(sprite==counter and ppu=='4','Existing card differs from source: '+iid)
    effective_states={}
    for fid,color in status_colors.items():
        sf=fields(after_blocks[item][fid][1])
        state_sprite=mm.get(key(fid,'m_Sprite'),('',sf['m_Sprite']))[1]
        state_ppu=mm.get(key(fid,'m_PixelsPerUnitMultiplier'),(sf['m_PixelsPerUnitMultiplier'],''))[0]
        check(state_sprite==counter and state_ppu=='4','Starter card border differs '+iid)
        check(not any(k[0]==fid and k[3].startswith('m_Color') for k in mm),'Unexpected nested color override '+iid)
        effective_states[fid]={'sprite':state_sprite,'ppu':state_ppu,'inheritedColor':sf['m_Color']}
    for prop in font_fields: check(mm[key(amount,prop)][0]=='52','Nested amount font differs '+iid)
    cards.append({'instanceFileID':iid,'normalSprite':sprite,'normalPPU':ppu,'states':effective_states,'amountFont':52})
report['sixCardConsistency']=cards

fill=fields(after_blocks[panel]['6620554689692068040'][1])
for key,want in [('m_Type','3'),('m_FillMethod','0'),('m_FillOrigin','0'),('m_FillAmount','0.446')]: check(fill[key]==want,'Progress behavior field changed '+key)
check(fill['m_PreserveAspect']=='1','Fill must preserve the generated shape aspect')
art=json.loads((HERE/'Art/import.json').read_text(encoding='utf-8'))
check(fill['m_Sprite']==f"{{fileID: 21300000, guid: {art['guid']}, type: 3}}",'Wrong fill sprite reference')
check(sha(PROJECT/art['asset'])==art['sha256'],'Generated fill pixels differ from import manifest')
meta=(PROJECT/(art['asset']+'.meta')).read_text(encoding='utf-8-sig')
check(re.search('^guid: '+art['guid']+'$',meta,re.M) is not None,'Fill meta GUID mismatch')
check('internalID: 21300000' in meta and 'WithdrawProgressSoftFill: 21300000' in meta,'Fill sprite internalID is missing')
rect=lambda fid:{k:float(v) for k,v in re.findall(r'([xy]): ([^,}]+)',fields(after_blocks[panel][fid][1])['m_SizeDelta'])}
track=rect('8175018248033638516'); fill_rect=rect('2071485895475148001')
sprite_ratio=art['spriteRect'][2]/art['spriteRect'][3]
visual_width=min(fill_rect['x'],fill_rect['y']*sprite_ratio); visual_height=visual_width/sprite_ratio
check(visual_width<=track['x'] and visual_height<=track['y'],'Fill extends beyond track')
report['progress']={'type':fill['m_Type'],'method':fill['m_FillMethod'],'origin':fill['m_FillOrigin'],'authoredAmountUnchanged':fill['m_FillAmount'],'preserveAspect':True,'trackSize':track,'fillRect':fill_rect,'sourceSpriteAspect':sprite_ratio,'fullProgressImageQuad':[visual_width,visual_height],'insideTrackMargins':[(track['x']-visual_width)/2,(track['y']-visual_height)/2], 'analyticalVisibleQuadWidths':{str(f):visual_width*f for f in (0,.35,1)},'note':'Geometry uses importer crop rectangle and Image.PreserveAspect. Actual alpha can have additional padding. No dynamic business value was changed.'}
report['newFillResource']=art
panel_art=json.loads((HERE/'Art/panel-import.json').read_text(encoding='utf-8'))
panel_image=fields(after_blocks[panel]['2754535437279419372'][1])
check(panel_image['m_Sprite']==f"{{fileID: 21300000, guid: {panel_art['guid']}, type: 3}}",'Wrong final main panel sprite reference')
check(panel_image['m_Type']=='1' and panel_image['m_PixelsPerUnitMultiplier']=='1.3','Final panel must use Sliced and PPU 1.3')
check(sha(PROJECT/panel_art['asset'])==panel_art['sha256'],'Generated main panel pixels differ from import manifest')
panel_meta=(PROJECT/(panel_art['asset']+'.meta')).read_text(encoding='utf-8-sig')
check(re.search('^guid: '+panel_art['guid']+'$',panel_meta,re.M) is not None,'Main panel meta GUID mismatch')
check('internalID: 21300000' in panel_meta and 'WithdrawCreamWoodPanel: 21300000' in panel_meta,'Main panel sprite internalID is missing')
report['newPanelResource']=panel_art
track_image=fields(after_blocks[panel]['8743834095267926959'][1])
check(track_image['m_Sprite']=='{fileID: 1920971194, guid: 50c5208c19d79f44ebd86c010248d125, type: 3}' and track_image['m_Type']=='1' and track_image['m_PixelsPerUnitMultiplier']=='1.3','Track must retain original rounded sliced sprite with PPU 1.3')
report['progress']['roundedTrack']=dict((k,track_image[k]) for k in ('m_Sprite','m_Type','m_PixelsPerUnitMultiplier'))

# Resolve every external reference newly introduced in the final delta. The unchanged
# references and existing code are verified by the baseline comparisons above/below.
reference_pairs=set()
for d in diffs:
    value=d['after']
    value=value[1] if isinstance(value,tuple) else value
    for fid,guid in re.findall(r'fileID: (-?\d+), guid: ([0-9a-f]{32})',str(value)):
        reference_pairs.add((fid,guid))
meta_by_guid={}
for mp in (PROJECT/'Assets').rglob('*.meta'):
    mt=mp.read_text(encoding='utf-8-sig',errors='replace')
    gm=re.search(r'^guid: ([0-9a-f]{32})$',mt,re.M)
    if gm and any(gm[1]==g for _,g in reference_pairs): meta_by_guid.setdefault(gm[1],[]).append((mp,mt))
report['introducedExternalReferences']=[]
for fid,guid in sorted(reference_pairs):
    matches=meta_by_guid.get(guid,[])
    check(len(matches)==1,'Missing or duplicate new reference GUID '+guid)
    if len(matches)!=1: continue
    mp,mt=matches[0]; asset=mp.with_suffix('')
    check(asset.exists(),'Missing referenced asset '+str(asset))
    if fid=='21300000': valid=bool(re.search(r'internalID: '+fid+r'\b',mt))
    else: valid=bool(re.search(r'^--- !u!\d+ &'+fid+r'\b',asset.read_text(encoding='utf-8-sig',errors='replace'),re.M))
    check(valid,'Missing referenced fileID '+fid+' in '+str(asset))
    report['introducedExternalReferences'].append({'guid':guid,'fileID':fid,'asset':asset.relative_to(PROJECT).as_posix(),'valid':valid})

static=json.loads((HERE/'Previews/final-verified/report.json').read_text(encoding='utf-8'))
static_cases=[]
for case in static['cases']:
    check(not case['error'] and case['visibleGlyphOverflowCount']==0 and case['amountItemCount']==6,'Final static preview failed '+str(case['sampleProgress']))
    for source in case['sourceAssets']:
        check(source['unchanged'] and source['before']==source['after'] and sha(PROJECT/source['path'])==source['after'],'Static preview is stale or changed its source '+source['path'])
    images=case['progressImages']; t=next(i for i in images if i['path'].endswith('/bg')); f=next(i for i in images if i['path'].endswith('/real'))
    tb=t['renderedMeshBounds']; fb=f['renderedMeshBounds']
    inside=(fb['width']==0 or (fb['x']>=tb['x']-.01 and fb['y']>=tb['y']-.01 and fb['x']+fb['width']<=tb['x']+tb['width']+.01 and fb['y']+fb['height']<=tb['y']+tb['height']+.01))
    check(inside,'Actual static fill mesh exceeds track '+str(case['sampleProgress']))
    static_cases.append({'sampleProgress':case['sampleProgress'],'screenshot':case['screenshot'],'visibleGlyphOverflowCount':case['visibleGlyphOverflowCount'],'fillMeshInsideTrack':inside,'fillMeshBounds':fb,'amountItems':case['amountItemCount'],'tmpFlagsWithoutVisibleGlyphOverflow':[t['path'] for t in case['texts'] if t['active'] and t['tmpOverflow'] and not t['glyphOverflow']]})
check({round(c['sampleProgress'],6) for c in static_cases}=={0,.35,1},'Static preview progress cases incomplete')
report['staticPreviewEvidence']={'kind':static['kind'],'generatedUtc':static['generatedUtc'],'cases':static_cases,'note':'Read-only consumption of the separate renderer output. TMP layout may set isTextOverflowing while all rendered glyphs fit; exact such flags are recorded, not concealed.'}

# Same current source-code baseline established earlier in this user turn.
hud_baseline=json.loads((PROJECT/'Design/HudSpacing-20260918/baseline.json').read_text(encoding='utf-8'))
changed_code=[p for p,digest in hud_baseline['code'].items() if not (PROJECT/p).exists() or sha(PROJECT/p)!=digest]
check(not changed_code,'Previously existing C# changed: '+str(changed_code))
report['originalCodeHashes']={'count':len(hud_baseline['code']),'changed':changed_code,'temporaryNewEditorValidationToolsExcluded':True}

cutoff=(HERE/'baseline.json').stat().st_mtime
recent_prefabs=[p.relative_to(PROJECT).as_posix() for p in (PROJECT/'Assets').rglob('*.prefab') if p.stat().st_mtime>=cutoff]
unexpected_recent=sorted(set(recent_prefabs)-{panel,item})
check(not unexpected_recent,'Other prefabs written after cash baseline timestamp: '+str(unexpected_recent))
report['otherPrefabWriteCheck']={'method':'Filesystem last-write timestamps since cash baseline; not a full-project hash baseline','baselineTimeUtc':datetime.fromtimestamp(cutoff,timezone.utc).isoformat(),'recentPrefabs':sorted(recent_prefabs),'unexpectedRecentPrefabs':unexpected_recent}
report['limitations']=['Cash baseline covers 7 explicit files; whole-project non-change is supported by write timestamps, not a missing full-prefab hash snapshot','This independent verifier executes no Unity commands or business actions','0/35/100% evidence is static prefab rendering with explicit sample values, not runtime or real account data']
report['passed']=not report['errors']
(HERE/'independent-validation.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'passed':report['passed'],'visualChanges':len(diffs),'prefabs':len(report['prefabs']),'originalScriptsVerified':len(hud_baseline['code']),'unexpectedRecentPrefabs':unexpected_recent,'errors':report['errors']}))
