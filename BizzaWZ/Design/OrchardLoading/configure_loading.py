"""Author the loading prefab and Unity sprite slices; source PNG stays byte-identical."""
from pathlib import Path
import re
import uuid

project = Path(__file__).resolve().parents[2]
prefab = project / 'Assets/BizzaWZ/Common/MenuSystem/Common/LoadingPanel/LoadingPanel.prefab'
atlas = project / 'Assets/OrchardUI/Resources/OrchardUI/LoadingArtwork.png'
template = (project / 'Assets/OrchardUI/Resources/OrchardUI/Backdrop.png.meta').read_text()
atlas_guid = 'f452a0c8f2c5460191650640506126ea'
template = re.sub(r'guid: [0-9a-f]{32}', 'guid: ' + atlas_guid, template, count=1)
template = template.replace('spriteMode: 1', 'spriteMode: 2')
template = template.replace('spriteGenerateFallbackPhysicsShape: 1', 'spriteGenerateFallbackPhysicsShape: 0')
slices = [
    ('Full', 0, 0, 941, 1672),
    ('EmptyTrack', 560, 1333, 32, 40),
    ('EmptyCap', 699, 1333, 30, 40),
    ('Fill', 211, 1334, 298, 37),
]
entries = []
for index, (name, x, top, width, height) in enumerate(slices):
    entries.append(f'''    - serializedVersion: 2
      name: {name}
      rect:
        serializedVersion: 2
        x: {x}
        y: {1672-top-height}
        width: {width}
        height: {height}
      alignment: 0
      pivot: {{x: 0.5, y: 0.5}}
      border: {{x: 0, y: 0, z: 0, w: 0}}
      outline: []
      physicsShape: []
      tessellationDetail: 0
      bones: []
      spriteID: {uuid.uuid5(uuid.NAMESPACE_URL, 'OrchardLoading/'+name).hex}
      internalID: {21300000+index*2}
      vertices: []
      indices: 
      edges: []
      weights: []''')
template = template.replace('    sprites: []', '    sprites:\n' + '\n'.join(entries))
template = template.replace('    nameFileIdTable: {}', '    nameFileIdTable:\n' + '\n'.join(f'      {s[0]}: {21300000+i*2}' for i,s in enumerate(slices)))
atlas.with_suffix('.png.meta').write_text(template, encoding='utf-8')

text = prefab.read_text(encoding='utf-8-sig')
parts = re.split(r'(?=^--- !u!)', text, flags=re.M)
blocks = {int(re.match(r'--- !u!\d+ &(-?\d+)', p).group(1)): p for p in parts[1:]}
def replace(block, old, new):
    assert old in blocks[block], (block, old)
    blocks[block] = blocks[block].replace(old, new)

# Keep the framework page and camera; retire the old art branch only.
replace(3158357223558867452, '  m_IsActive: 1', '  m_IsActive: 0')
replace(3903310660382451288, '  m_Name: OrchardBackdrop', '  m_Name: OrchardLoadingArtwork')
replace(3903310660382451288, '  - component: {fileID: 9020678367638495870}', '  - component: {fileID: 9020678367638495870}\n  - component: {fileID: 870000000000000001}')
replace(1527961995316299987, '  m_Children: []', '  m_Children:\n  - {fileID: 870000000000000011}\n  - {fileID: 870000000000000021}\n  - {fileID: 870000000000000031}')
replace(5225150929342076997, '  m_Enabled: 0', '  m_Enabled: 1')
replace(1033283518471801383, '  m_AspectRatio: 0.56279904', '  m_AspectRatio: 0.562799043')
replace(9020678367638495870, 'guid: 3adf67a7cf9cfce47889f4706cce980c', 'guid: 7ed409675aa249da9ba9b79bf835051a')
replace(9020678367638495870, '  target: {fileID: 5225150929342076997}\n  resourcePath: OrchardUI/Backdrop', '''  resourcePath: OrchardUI/LoadingArtwork
  visualGroup: {fileID: 870000000000000001}
  bindings:
  - target: {fileID: 5225150929342076997}
    spriteName: Full
  - target: {fileID: 870000000000000013}
    spriteName: EmptyTrack
  - target: {fileID: 870000000000000023}
    spriteName: EmptyCap
  - target: {fileID: 870000000000000033}
    spriteName: Fill''')
replace(1704553334090239444, '  progressBar: {fileID: 0}', '  progressBar: {fileID: 870000000000000033}')
replace(1704553334090239444, '  progressTxt: {fileID: 4458470412056299634}', '  progressTxt: {fileID: 0}')
replace(1704553334090239444, '  harvestProgress: {fileID: 2916335268062610726}', '  harvestProgress: {fileID: 0}')

def header(kind, ident, typename):
    return f'''--- !u!{kind} &{ident}
{typename}:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
'''

added = header(225, 870000000000000001, 'CanvasGroup') + '''  m_GameObject: {fileID: 3903310660382451288}
  m_Enabled: 1
  m_Alpha: 0
  m_Interactable: 0
  m_BlocksRaycasts: 0
  m_IgnoreParentGroups: 0
'''
for ident, name, x, top, width, height, flip, filled in [
    (870000000000000010, 'EmptyTrack', 239, 1333, 460, 40, False, False),
    (870000000000000020, 'EmptyLeftCap', 209, 1333, 30, 40, True, False),
    (870000000000000030, 'LoadingProgress', 211, 1334, 514, 37, False, True),
]:
    added += header(1,ident,'GameObject') + f'''  serializedVersion: 6
  m_Component:
  - component: {{fileID: {ident+1}}}
  - component: {{fileID: {ident+2}}}
  - component: {{fileID: {ident+3}}}
  m_Layer: 5
  m_Name: {name}
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
'''
    added += header(224,ident+1,'RectTransform') + f'''  m_GameObject: {{fileID: {ident}}}
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: {-1 if flip else 1}, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: 1527961995316299987}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
  m_AnchorMin: {{x: {x/941:.10f}, y: {(1672-top-height)/1672:.10f}}}
  m_AnchorMax: {{x: {(x+width)/941:.10f}, y: {(1672-top)/1672:.10f}}}
  m_AnchoredPosition: {{x: 0, y: 0}}
  m_SizeDelta: {{x: 0, y: 0}}
  m_Pivot: {{x: 0.5, y: 0.5}}
'''
    added += header(222,ident+2,'CanvasRenderer') + f'''  m_GameObject: {{fileID: {ident}}}
  m_CullTransparentMesh: 1
'''
    added += header(114,ident+3,'MonoBehaviour') + f'''  m_GameObject: {{fileID: {ident}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: fe87c0e1cc204ed48ad3b37840f39efc, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  m_Material: {{fileID: 0}}
  m_Color: {{r: 1, g: 1, b: 1, a: 1}}
  m_RaycastTarget: 0
  m_RaycastPadding: {{x: 0, y: 0, z: 0, w: 0}}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_Sprite: {{fileID: 0}}
  m_Type: {3 if filled else 0}
  m_PreserveAspect: 0
  m_FillCenter: 1
  m_FillMethod: 0
  m_FillAmount: 0
  m_FillClockwise: 1
  m_FillOrigin: 0
  m_UseSpriteMesh: 0
  m_PixelsPerUnitMultiplier: 1
'''

prefab.write_text(parts[0] + ''.join(blocks.values()) + added, encoding='utf-8')
print('Configured loading prefab, exact source artwork, four Sprite slices.')
