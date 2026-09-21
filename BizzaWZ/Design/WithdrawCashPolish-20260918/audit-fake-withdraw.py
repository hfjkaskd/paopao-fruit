"""Read-only audit of the cash withdrawal page and its progress visualization."""
from pathlib import Path
import hashlib, importlib.util, json, re

HERE=Path(__file__).resolve().parent
PROJECT=HERE.parents[1]
spec=importlib.util.spec_from_file_location('inspect_ui',PROJECT/'Design/HudPolish-20260918/inspect_ui.py')
helper=importlib.util.module_from_spec(spec); spec.loader.exec_module(helper)
paths=['Assets/BizzaWZ/Final/Real/UI/FakeWithdrawPanel/FakeWithdrawPanel.prefab',
       'Assets/BizzaWZ/Final/Real/UI/FakeWithdrawPanel/WithdrawAmountItem.prefab',
       'Assets/BizzaWZ/Final/Real/UI/FakeWithdrawPanel/FakeWithdrawPanel.cs',
       'Assets/BizzaWZ/Final/Real/UI/FakeWithdrawPanel/WithdrawAmountItem.cs']
report={'mode':'Read-only source and reference screenshot audit. No Assets writes or Unity commands.',
        'screenshot':'C:/Users/pc/AppData/Local/Temp/codex-clipboard-e1501e1d-b298-4db4-b9c1-13f314b37267.png',
        'fileHashes':{p:hashlib.sha256((PROJECT/p).read_bytes()).hexdigest() for p in paths},
        'structures':{p:helper.read(PROJECT/p) for p in paths if p.endswith('.prefab')}}
prefab=(PROJECT/paths[0]).read_text(encoding='utf-8-sig')
source_guid='5fefc70f9b2f3f1488b2b221199dcf24'
instances=[]
for m in re.finditer(r'^--- !u!1001 &(\d+)\n(.*?)(?=^--- !u!|\Z)',prefab,re.M|re.S):
    if f'm_SourcePrefab: {{fileID: 100100000, guid: {source_guid}' not in m[2]: continue
    overrides=[{'sourceFileID':x[0],'propertyPath':x[1],'value':x[2],'objectReference':x[3]} for x in re.findall(r'    - target: \{fileID: (\d+),[^\n]*\n      propertyPath: (\S+)\n      value: ([^\n]*)\n      objectReference: ([^\n]*)',m[2])]
    instances.append({'instanceFileID':m[1],'sourceGuid':source_guid,'overrides':overrides})
report['amountInstances']=instances
report['currentProgress']={
 'kind':'Image.Type.Filled; NOT Slider','hasSliderMinMaxSerialization':bool(re.search(r'm_(?:MinValue|MaxValue):',prefab)),
 'parentRect':{'id':'3131837160562448410','path':'Content/WithdrawProgress/Progress','size':[902.9887,73.36],'position':[0,-24]},
 'trackRect':{'id':'8175018248033638516','size':[897,64]},
 'trackImage':{'id':'8743834095267926959','type':1,'typeName':'Sliced','spriteFileID':1920971194,'spriteGuid':'50c5208c19d79f44ebd86c010248d125','spriteName':'ProgressTrack','sourceSize':[299,76],'sourceBorder':[31,21,31,21],'pixelsPerUnitMultiplier':1},
 'fillRect':{'id':'2071485895475148001','size':[897,64]},
 'fillImage':{'id':'6620554689692068040','type':3,'typeName':'Filled','fillMethod':0,'fillMethodName':'Horizontal','fillOrigin':0,'fillOriginName':'Left','preserveAspect':False,'authoredFillAmount':0.446,'runtimeValueRange':[0,1],'spriteFileID':1192469443,'spriteGuid':'50c5208c19d79f44ebd86c010248d125','spriteName':'ProgressFill','sourceSize':[286,62],'sourceBorder':[22,12,22,12]},
 'percentageText':{'rectID':'2262917192024109782','tmpID':'3339610134613169318','size':[200,50],'fontSize':36,'dynamicFormat':'(Clamp01(GetCurProgress()) * 100).ToString("F2") + "%"'},
 'sourceAspectRatio':286/62,'destinationAspectRatio':897/64,'relativeHorizontalDistortion':(897/64)/(286/62),
 'issues':['Filled image stretches the full286x62 sprite into897x64; its border metadata is not a9-slice in Filled mode','Track and fill use the exact same rectangle, leaving no inset for the track wood rim','Full-width green glossy bevel reads as an enormous capsule and competes with the withdrawal button'],
 'notAFix':'Do not simply change fill Image.Type to Sliced: current fillAmount and DOFillAmount would stop controlling visible progress.'}
report['recommendedProgressRepair']={
 'preserve':['All existing nodes and component IDs','Image.Type.Filled','Horizontal fill fromLeft','progressImg/progressTxt bindings','percentage formatting, selected mission, current stage and thresholds','withdrawal Button count and bindings'],
 'track':{'rectID':'8175018248033638516','suggestedSize':[897,48],'imageID':'8743834095267926959','suggestedPixelsPerUnitMultiplier':3,'note':'Reuse the track9-slice with much smaller border thickness, or use a new thin cream/wood track sprite.'},
 'fill':{'rectID':'2071485895475148001','suggestedSize':[885,36],'imageID':'6620554689692068040','newArtTargetAspect':885/36,'newArt':'A dedicated horizontal green fill with a gentle highlight and minimally rounded ends at about24.6:1 aspect ratio. No text or numbers baked in. Keep Filled type and original runtime fields.'},
 'text':{'tmpID':'3339610134613169318','suggestedFontSize':30,'suggestedMaxSize':30,'note':'Keep white text with existing readable outline; verify at0%,25%,50%,100% because a child of aFilled Image is not clipped by fillAmount.'},
 'layout':'Keep the progress parent, title, main action, amounts and hint at their existing relative positions. Shrink only bar height/inset and update its art.'}
report['pageStyleSuggestions']={
 'keepGrid':'6amount entries remain3columns x2rows. GridContent width892; cell279.8x188.6; spacing20.9x13.17. Do not alter mission count or country-dependent counts.',
 'cardVisual':'Reduce heavy orange frame thickness through existing Sliced Image pixelsPerUnitMultiplier or a slimmer ivory/wood card. Existing get/claimed overlays must use matching style and preserve active-state logic.',
 'cardImageIDs':{'normal':'3062039195826043058','starterAvailable':'5455967628476765425','starterClaimed':'1584464498381216905','selectionCheck':'5590047728298028375'},
 'dynamicAmountTMP':'2196861301424074241',
 'instanceCaution':'Six existing nested instances explicitly override normal, starterAvailable and starterClaimed m_Sprite. Updating only the source WithdrawAmountItem sprite will not update these overrides; source still needs consistency for runtime instantiated items.',
 'selectedStateCaution':'selectObj is the existing check Image; starter green frame is getObj and must not be reinterpreted as universal selection logic.',
 'largePanel':'Keep existing panel geometry; a quieter thin wood border and cream body with fewer corner ornaments would reduce the heavy stacked-frame effect.',
 'topNav':'Retain title/back/history/FAQ arrangement; use the same restrained wood/green family as the current HUD.',
 'progressTitleBounds':'Title Rect210600630110459938 is1065.6wide centeredx639 under1083wide parent: it extends88.8units beyond its parent right. A local width/alignment correction could match the existing892wide content column without adding a node.'}

guid_index={}
for p in (PROJECT/'Assets').rglob('*.meta'):
    mt=re.search(r'^guid: (\w+)$',p.read_text(encoding='utf-8-sig',errors='replace'),re.M)
    if mt: guid_index[mt[1]]=p.with_suffix('')
root_script=re.search(r'^--- !u!114 &4051567103948935487.*?(?=^--- !u!|\Z)',prefab,re.M|re.S)[0]
mission_guids=set(re.findall(r'fileID: 11400000, guid: (\w+)',root_script))
configs=[]
for guid in sorted(mission_guids):
    p=guid_index[guid]; text=p.read_text(encoding='utf-8-sig')
    configs.append({'guid':guid,'path':p.relative_to(PROJECT).as_posix(),'withdrawMoney':float(re.search(r'^  withdrawMoney: (.*)$',text,re.M)[1]),'conditions':[{'enumValue':int(c),'targetValue':float(v)} for c,v in re.findall(r'      condition: (\d+)\n      targetValue: ([^\n]+)',text)]})
report['missionConfigs']=configs
report['businessReadOnlyConclusion']={
 'screenshotSelectedAmount':0.01,'screenshotBalance':200.76,'starterConfig':'USFakeWithdrawMissionConfig1.asset (also referenced by BR list)','condition':'Money, targetValue0.01','rawProgressIfStage0':200.76/0.01,'clampedProgressIfStage0':1,
 'conclusion':'Screenshot100.00% agrees with selected0.01 starter threshold and displayed balance. It is not evidence of a progress-value bug. Exact saved mission stage was not read or changed.',
 'logic':'GetCurProgress divides the selected stage condition value by its target. Money value and target both use ItemUtils.FormatCountFloat, currently identity. A completed/no-current stage returns1. UpdateProgress applies Clamp01 and binds the same value to fill and percentage.',
 'otherMoneyStage0Examples':{str(amount):round(min(1,200.76/amount)*100,4) for amount in [800,1000,2000,3000,5000]},
 'zeroTargetsInReferencedConfigs':sum(c['targetValue']==0 for config in configs for c in config['conditions']),
 'repairScope':'No gameplay/economy/withdrawal logic or mission data needs changing to fix the visual defect.'}
report['collaborationAndChanges']={
 'observedAgents':['root:HUD finish plus upcoming visual revision','hud_static_validation:HUD static render','hud_spacing_audit:this read-only audit','slot_fx_audit:completed'],
 'existingDirtySources':paths,
 'caution':'All4cash withdrawal sources were already modified in git before this audit. Do not reset them. Design/WithdrawPolish-20260918 already contains a previous PagBank/real withdrawal polish; its Before/README/audits are not this cash-page baseline.',
 'writesByThisAudit':['audit-fake-withdraw.py','audit-fake-withdraw-report.json','audit-fake-withdraw-report.md']}
(HERE/'audit-fake-withdraw-report.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'imageFillNotSlider':True,'relativeHorizontalDistortion':report['currentProgress']['relativeHorizontalDistortion'],'existingAmountInstances':len(instances),'referencedMissionConfigs':len(configs),'zeroTargets':report['businessReadOnlyConclusion']['zeroTargetsInReferencedConfigs'],'productionChanged':False}))
