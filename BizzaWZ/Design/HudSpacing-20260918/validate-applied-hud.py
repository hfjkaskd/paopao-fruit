"""Independent read-only verification of the applied main-HUD overrides."""
from pathlib import Path
import hashlib, json, re

HERE=Path(__file__).resolve().parent
PROJECT=HERE.parents[1]
BASELINE=json.loads((HERE/'baseline.json').read_text(encoding='utf-8'))
APPLIED=json.loads((HERE/'applied-changes.json').read_text(encoding='utf-8'))
PROPOSAL=json.loads((HERE/'hud-layout-proposal.json').read_text(encoding='utf-8'))
TARGET=APPLIED['file']
INSTANCE='461910548945330748'
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
report={'kind':'Independent source/resource/analytical geometry verification; not runtime visual or functional verification','errors':[]}
def check(condition, message):
    if not condition: report['errors'].append(message)

def blocks(t): return {m[2]:(m[1],m[0]) for m in re.finditer(r'^--- !u!(\d+) &(-?\d+)[^\n]*\n(.*?)(?=^--- !u!|\Z)',t,re.M|re.S)}
before=(HERE/'Before'/TARGET).read_text(encoding='utf-8-sig')
after=(PROJECT/TARGET).read_text(encoding='utf-8-sig')
bb,ab=blocks(before),blocks(after)
check(bb.keys()==ab.keys(),'Serialized object IDs changed')
changed=[fid for fid in bb if bb[fid]!=ab.get(fid)]
check(changed==[INSTANCE],f'Unexpected changed serialized objects: {changed}')
report['changedSerializedObjects']=changed
report['serializedObjectCounts']={typ:sum(t==typ for t,_ in bb.values()) for typ in sorted({t for t,_ in bb.values()})}

mod_pattern=r'    - target: \{fileID: (\d+), guid: ([^,]+), type: (\d+)\}\n      propertyPath: (\S+)\n      value: ([^\n]*)\n      objectReference: ([^\n]*)\n'
def mods(block):
    pairs=[((m[0],m[1],m[2],m[3]),(m[4],m[5])) for m in re.findall(mod_pattern,block)]
    check(len(dict(pairs))==len(pairs),'Duplicate override keys')
    return dict(pairs)
bm,am=mods(bb[INSTANCE][1]),mods(ab[INSTANCE][1])
check(set(bm)<=set(am),'Existing override(s) removed')
check(re.sub(mod_pattern,'',bb[INSTANCE][1])==re.sub(mod_pattern,'',ab[INSTANCE][1]),'Non-property instance content changed')
diff={k:am[k] for k in am if am[k]!=bm.get(k)}
allowed={(c['sourceFileID'],c['propertyPath']) for c in PROPOSAL['recommendedOverrides']}
allowed.update({('5821051965148166099','m_Sprite'),('5821051965148166099','m_Type'),('5821051965148166099','m_PreserveAspect'),('5860728120506579172','m_sharedMaterial'),('5860728120506579172','m_fontColor.r'),('5860728120506579172','m_fontColor.g'),('5860728120506579172','m_fontColor.b')})
for k in diff:
    check(k[1]=='b1f781d8c27eec54db58a4849081f7e5',f'Unexpected source GUID: {k}')
    check((k[0],k[3]) in allowed,f'Unapproved property difference: {k}')
    check(not re.search(r'm_text$|m_Script|m_OnClick|onClick|curLevelTxt|levelTxt|coinBtn|dollarBtn|settingBtn|m_Name|m_IsActive|m_Enabled',k[3]),f'Behavior/content mutation: {k}')
check({(k[0],k[3]) for k in diff}=={(c['fileID'],c['propertyPath']) for c in APPLIED['changes']},'Applied change manifest mismatch')
check(sha(PROJECT/TARGET)==APPLIED['afterSha256'],'After hash differs from applied manifest')
report['changedOverrideCount']=len(diff)
report['hierarchyComponentsBindingsEventsSerializedTextUnchanged']=not report['errors']
report['prefabHashes']=[]
for entry in BASELINE['prefabs']:
    digest=sha(PROJECT/entry['path']); unchanged=digest==entry['sha256']
    if entry['path']!=TARGET: check(unchanged,'Shared prefab changed: '+entry['path'])
    report['prefabHashes'].append({'path':entry['path'],'unchanged':unchanged,'sha256':digest})
code_now={p.relative_to(PROJECT).as_posix():sha(p) for p in (PROJECT/'Assets').rglob('*.cs')}
code_before=BASELINE['code']
code_diff={p for p in code_before if code_now.get(p)!=code_before.get(p)}
new_code=set(code_now)-set(code_before)
validation_tools={
    'Assets/OrchardUI/Editor/HudSpacingPreview.cs':'Design/HudSpacing-20260918/Validation/HudSpacingPreview.cs',
    'Assets/OrchardUI/Editor/WithdrawPreview.cs':'Design/WithdrawCashPolish-20260918/Validation/WithdrawPreview.cs',
}
validated_tools=[]
for p in sorted(new_code):
    archive=validation_tools.get(p)
    tool_source=(PROJECT/p).read_text(encoding='utf-8-sig')
    is_expected=archive is not None and (PROJECT/archive).exists() and 'using UnityEditor;' in tool_source and ('public static class '+Path(p).stem) in tool_source
    check(is_expected,'Unexpected new C# source: '+p)
    if is_expected: validated_tools.append({'path':p,'archivedValidationSource':archive,'identicalToCurrentArchive':code_now[p]==sha(PROJECT/archive),'sha256':code_now[p],'classification':'Additional temporary Editor-only validation tool; excluded from original-code invariance claim'})
check(not code_diff,'Existing C# changed: '+str(sorted(code_diff)))
report['code']={'baselineCount':len(code_before),'currentCount':len(code_now),'changedExisting':sorted(code_diff),'additionalEditorValidationTools':validated_tools}

# Check new Sprite internalID and image hash; verify changed TMP material's atlas and shader references.
art=json.loads((HERE/'Art/import.json').read_text(encoding='utf-8'))
meta=(PROJECT/(art['asset']+'.meta')).read_text(encoding='utf-8')
check(re.search(r'^guid: '+art['guid']+'$',meta,re.M) is not None,'New Sprite GUID mismatch')
check('internalID: 21300000' in meta and 'HudLevelCreamLeaf: 21300000' in meta,'New Sprite fileID missing')
check(sha(PROJECT/art['asset'])==art['sha256'],'New Sprite bytes differ from imported source')
sprite_ref=am[('5821051965148166099','b1f781d8c27eec54db58a4849081f7e5','3','m_Sprite')][1]
check(sprite_ref==f"{{fileID: 21300000, guid: {art['guid']}, type: 3}}",'Level Image does not use new Sprite')
needed={'486d0a25be512df4887a3bf53db492b8','7727608c2306e9d4f99315146a923875','7cc23ba99c7035347900a2e939f7ab60','92702b813c1497449ae7f317befd32ca'}
guid_paths={}
for p in (PROJECT/'Assets').rglob('*.meta'):
    match=re.search(r'^guid: (\w+)$',p.read_text(encoding='utf-8-sig',errors='replace'),re.M)
    if match and match[1] in needed: guid_paths[match[1]]=p.with_suffix('')
check(set(guid_paths)==needed,'Missing font/material/shader GUIDs: '+str(needed-set(guid_paths)))
font_path=guid_paths.get('7cc23ba99c7035347900a2e939f7ab60')
if font_path:
    font=font_path.read_text(encoding='utf-8-sig')
    check('--- !u!114 &11400000' in font,'MainFont fileID 11400000 absent')
    check('--- !u!28 &-2849837040299456729' in font,'MainFont atlas internal fileID absent')
for guid in ('486d0a25be512df4887a3bf53db492b8','7727608c2306e9d4f99315146a923875'):
    p=guid_paths.get(guid)
    if not p: continue
    material=p.read_text(encoding='utf-8-sig')
    check('--- !u!21 &2100000' in material,'Material fileID absent: '+str(p))
    check('m_Texture: {fileID: -2849837040299456729, guid: 7cc23ba99c7035347900a2e939f7ab60, type: 2}' in material,'TMP material atlas mismatch: '+str(p))
report['resources']={'newSprite':art,'fontMaterialShaderPaths':{g:str(p.relative_to(PROJECT)) for g,p in guid_paths.items()}}

# Load only the definitions preceding report emission. No previous report is overwritten.
analysis_script=(HERE/'analyze-hud-layout.py').read_text(encoding='utf-8')
definitions=analysis_script[:analysis_script.index("report={'kind'")]
env={'__file__':str(HERE/'analyze-hud-layout.py')}
exec(compile(definitions,str(HERE/'analyze-hud-layout.py'),'exec'),env)
actual=env['data']
report['actualAppliedGeometry']=[]
for screen in ((1080,1920),(1080,2340)):
    for gs,cs,bs in ((1,1,1),(1.25,1,1.08),(1,1.25,1.08),(1.25,1.25,1.08)):
        case=env['model'](actual,*screen,gs,cs,bs)
        report['actualAppliedGeometry'].append(case)
        check(min(case['visibleGaps'])>=0,'Visible overlap: '+str((screen,gs,cs,bs)))
        check(min(case['hitGaps'])>=0,'Hit rect overlap: '+str((screen,gs,cs,bs)))
        check(min(case['screenMargins'])>=20,'Screen clipping: '+str((screen,gs,cs,bs)))
        for kind in ('gold','cash'):
            amount=case['rects'][kind+'_amount']; button=case['rects'][kind+'_button_visual']
            check(amount['x']+amount['width']<=button['x'],'Amount overlaps button: '+str((screen,gs,cs,bs,kind)))
        a=case['rects']['approx_text']; b=case['rects']['conversion_text']
        check(a['x']+a['width']<=b['x'],'Approximation text rectangle overlaps conversion text')
report['passed']=not report['errors']
report['limits']=['No Unity command or desktop control performed','Text glyph/material padding and fallback rendering need the separate TMP render','No gameplay, withdrawal, settings or reward action was triggered','Whole-Assets change history cannot be proven from the four-Prefab plus C# baseline; scope above is explicitly limited to recorded baseline']
(HERE/'independent-validation.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'passed':report['passed'],'changedOverrides':len(diff),'serializedObjects':len(bb),'scriptsVerified':len(code_before),'geometryCases':len(report['actualAppliedGeometry']),'errors':report['errors']},ensure_ascii=False))
