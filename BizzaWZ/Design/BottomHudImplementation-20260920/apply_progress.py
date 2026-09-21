from pathlib import Path
import re, shutil, uuid, json, hashlib, difflib
from PIL import Image

ROOT = Path('C:/Projects/paopao/BizzaWZ')
OUT = ROOT / 'Design/BottomHudImplementation-20260920'
FILES = [
    'Assets/BizzaWZ/Final/Real/UI/SlotsPanel/SlotEnter/SlotEnter.prefab',
    'Assets/BizzaWZ/Final/Real/UI/GamePanel/Resources/Resources/GameUiWidget.prefab',
    'Assets/OrchardUI/Editor/OrchardServiceRewardPass.cs',
]
before = {}
for rel in FILES:
    path = ROOT / rel
    destination = OUT / 'Before' / rel
    destination.parent.mkdir(parents=True, exist_ok=True)
    if destination.exists():
        raise RuntimeError('Refusing to overwrite baseline: ' + str(destination))
    shutil.copy2(path, destination)
    before[rel] = path.read_bytes()

template = (ROOT / 'Assets/OrchardUI/Art/HudLucky777.png.meta').read_text()
assets = []
for name, source, rect in [
    ('BottomHudProgressTrack', 'exec-667ff323-ac4c-4bb1-a077-fb56fde07cbe.png', (50, 231, 2054, 282)),
    ('BottomHudProgressFill', 'exec-52bc8aef-b596-4e11-b21c-4cec136c5d57.png', (49, 316, 1885, 161)),
]:
    source_path = Path('C:/Users/pc/.codex/generated_images/01a0c1ed-0e34-7502-af96-698526ef196f') / source
    destination = ROOT / ('Assets/OrchardUI/Art/' + name + '.png')
    assert not destination.exists()
    # Copy generated pixels unchanged; Unity's sprite rect excludes transparent padding.
    shutil.copy2(source_path, destination)
    guid, sprite_id, sheet_id = [uuid.uuid4().hex for _ in range(3)]
    meta = template.replace('338551b9f3534cca9a7630ada90800c4', guid)
    meta = meta.replace('HudLucky777', name).replace('9ee49840cf9945669b1876911c9366a1', sprite_id)
    meta = meta.replace('410e3757a6d2469e903cb2beda6d3de7', sheet_id)
    for key, old, value in zip(('x', 'y', 'width', 'height'), (40, 182, 1188, 918), rect):
        meta = meta.replace('        ' + key + ': ' + str(old), '        ' + key + ': ' + str(value))
    destination.with_suffix('.png.meta').write_text(meta, newline='\n')
    image = Image.open(destination)
    alpha = image.getchannel('A')
    assets.append({'name':name, 'guid':guid, 'fileID':21300000, 'path':str(destination.relative_to(ROOT)).replace('\\','/'),
                   'source':str(source_path), 'size':list(image.size), 'mode':image.mode,
                   'alphaExtrema':list(alpha.getextrema()), 'unitySpriteRect':list(rect),
                   'sourceBytesUnchanged':hashlib.sha256(source_path.read_bytes()).digest()==hashlib.sha256(destination.read_bytes()).digest()})

track_ref = '{fileID: 21300000, guid: ' + assets[0]['guid'] + ', type: 3}'
fill_ref = '{fileID: 21300000, guid: ' + assets[1]['guid'] + ', type: 3}'
slot = before[FILES[0]].decode('utf-8')
newline = '\r\n' if '\r\n' in slot else '\n'

def set_field(text, file_id, field, value):
    pattern = r'(?ms)(^--- !u!\d+ &' + str(file_id) + r'\r?\n.*?)(?=^--- !u!|\Z)'
    match = re.search(pattern, text)
    assert match, file_id
    block = match.group(1)
    replacement, count = re.subn(r'(?m)^(  ' + re.escape(field) + r': )[^\r\n]*', lambda m:m.group(1)+value, block)
    assert count == 1, (file_id, field, count)
    return text[:match.start()] + replacement + text[match.end():]

for file_id, field, value in [
    (2597468376317513793, 'm_Sprite', track_ref),
    (2597468376317513793, 'm_Type', '0'),
    (2597468376317513793, 'm_PixelsPerUnitMultiplier', '1'),
    (7786913375694585872, 'm_Sprite', fill_ref),
    (7786913375694585872, 'm_FillAmount', '0'),
    (5078707581075184592, 'm_enableWordWrapping', '0'),
]:
    slot = set_field(slot, file_id, field, value)
(ROOT / FILES[0]).write_bytes(slot.encode('utf-8'))

widget = before[FILES[1]].decode('utf-8')
def set_override(text, target, field, value=None, reference=None):
    pattern = (r'(?m)(    - target: \{fileID: ' + str(target) + r', guid: 8c21a225bdfdef34099599e454f631c0, type: 3\}\r?\n'
               r'      propertyPath: ' + re.escape(field) + r'\r?\n      value: )([^\r\n]*)(\r?\n      objectReference: )([^\r\n]*)')
    def change(match):
        return match.group(1)+(value if value is not None else match.group(2))+match.group(3)+(reference if reference is not None else match.group(4))
    result, count = re.subn(pattern, change, text)
    assert count == 1, (target, field, count)
    return result
widget = set_override(widget, 2597468376317513793, 'm_Type', value='0')
widget = set_override(widget, 2597468376317513793, 'm_Sprite', reference=track_ref)
widget = set_override(widget, 7786913375694585872, 'm_Sprite', reference=fill_ref)
(ROOT / FILES[1]).write_bytes(widget.encode('utf-8'))

authoring = before[FILES[2]].decode('utf-8')
authoring_newline = '\r\n' if '\r\n' in authoring else '\n'
authoring = authoring.replace('            case "SlotEnter": ApplySlotEntry(root); break;', '''            case "SlotEnter": ApplySlotEntry(root); break;
            case "GameUiWidget":
                Transform slotEntry = Find(root, "SlotEnter");
                if (slotEntry != null) ApplySlotEntry(slotEntry.gameObject);
                break;'''.replace('\n', authoring_newline))
start = authoring.index('    private static void ApplySlotEntry(GameObject root)')
end = authoring.index('    private static Transform Find', start)
new_method = '''    private static void ApplySlotEntry(GameObject root)
    {
        // Dedicated unlettered sprites keep the approved dark trough and live fill separate.
        // Preserve every existing RectTransform, the 777 icon and the reward animation.
        Transform track = Find(root, "Content/Progress");
        Transform fill = Find(root, "Content/Progress/progress");
        if (track != null && track.TryGetComponent<Image>(out var trackImage))
        {
            trackImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/OrchardUI/Art/BottomHudProgressTrack.png");
            trackImage.type = Image.Type.Simple;
            trackImage.color = Color.white;
            trackImage.preserveAspect = false;
            trackImage.pixelsPerUnitMultiplier = 1;
        }
        if (fill != null && fill.TryGetComponent<Image>(out var fillImage))
        {
            fillImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/OrchardUI/Art/BottomHudProgressFill.png");
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImage.color = Color.white;
            fillImage.preserveAspect = false;
        }
        Transform counter = Find(root, "Content/Progress/Text (TMP)");
        if (counter != null && counter.TryGetComponent<TMP_Text>(out var label))
        {
            OrchardSkinAuthoring.SetTitle(label);
            label.enableWordWrapping = false;
        }
    }

'''.replace('\n', authoring_newline)
authoring = authoring[:start] + new_method + authoring[end:]
(ROOT / FILES[2]).write_bytes(authoring.encode('utf-8'))

changes = []
for rel in FILES:
    after = (ROOT / rel).read_bytes()
    patch = ''.join(difflib.unified_diff(before[rel].decode().splitlines(True), after.decode().splitlines(True), fromfile='before/'+rel, tofile='after/'+rel))
    (OUT / (Path(rel).name + '.progress.diff')).write_text(patch, newline='')
    changes.append({'path':rel, 'beforeSHA256':hashlib.sha256(before[rel]).hexdigest(), 'afterSHA256':hashlib.sha256(after).hexdigest()})
report = {'assets':assets, 'files':changes,
          'unchanged':['All RectTransforms','777 icon','All nodes and components','Button bindings','SlotEnter runtime formula and economy'],
          'rendering':{'track':[224,40], 'fill':[202,24], 'fillType':'Horizontal Filled / Left','prefabDefault':'0/5 and 0.0', 'runtimeOneOfFive':0.2},
          'artWorkflow':'Built-in image_gen only, source PNG bytes unchanged, transparent padding excluded by Unity Sprite rect; no raster editing.'}
(OUT / 'progress-implementation.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print(json.dumps(report, indent=2))
