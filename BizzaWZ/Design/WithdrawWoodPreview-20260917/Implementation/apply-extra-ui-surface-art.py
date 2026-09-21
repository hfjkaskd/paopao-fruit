"""Apply only the approved extra UI sprite/color substitutions; retain every other byte."""
import hashlib
import json
from pathlib import Path
import re
import shutil

ROOT = Path(__file__).resolve().parents[3]
OUT = Path(__file__).resolve().parent
CONTROLS = "50c5208c19d79f44ebd86c010248d125"
NAVIGATION = "ba69b03635224224bafcd70eba3b015d"
ROLE = {
    "Panel": (CONTROLS, 2758570),
    "Title": (CONTROLS, -396990571),
    "ButtonGreen": (CONTROLS, -1142528992),
    "ButtonBlue": (CONTROLS, -764451775),
    "Inset": (CONTROLS, -1559611953),
    "Badge": (CONTROLS, -799567093),
    "ButtonRoundCream": (CONTROLS, -377845897),
    "ProgressFill": (CONTROLS, 1192469443),
    "Input": (CONTROLS, -2106829423),
    "Close": (NAVIGATION, -1869255428),
}
CORE = "Assets/FruitsHarvest/Resources/Original/res/local/coreplay/CorePlayUI.prefab"
NEW = "Assets/FruitsHarvest/Resources/Original/res/local/pops/newitempop/NewItemPop.prefab"
TOAST = "Assets/FruitsHarvest/Resources/Original/res/local/globalui/GlobalText.prefab"
SLOT = "Assets/BizzaWZ/Final/Real/UI/SlotsPanel/BingoAsset/Assets/UiPrefab/SlotMachineGroup.prefab"
ENTRY = "Assets/BizzaWZ/Final/Real/UI/SlotsPanel/BingoAsset/Assets/UiPrefab/SlotEntry.prefab"
PROP = "Assets/BizzaWZ/Common/UI/GamePanel/UIPropEntry.prefab"
REWARD = "Assets/BizzaWZ/Final/Real/UI/GetRewardPanel/GetRewardPanel.prefab"
DANGER = "Assets/FruitsHarvest/Resources/Original/res/local/coreplay/prefab/DangerousTip.prefab"
GUIDE = "Assets/FruitsHarvest/Resources/Original/res/local/coreplay/prefab/NewPlayerGuider.prefab"
PLANS = {
    CORE: {
        "114909473683147852": "Badge",
        "114513653223693148": "ButtonRoundCream",
        "114555560523574011": "Badge",
        "114504490712063035": "Badge",
        "114798730798708461": "Badge",
        "114758405882409070": "Badge",
        "114950706389121088": "Badge",
        "114410865556287939": "ButtonRoundCream",
        "114025440191983101": "Badge",
        "114837227276182950": "ButtonRoundCream",
        "114495838826320329": "Badge",
        "114161367720648980": "Badge",
        "114942066236133985": "ButtonBlue",
        "114282982324125657": "ButtonRoundCream",
        "114389991902224113": "Badge",
        "114837889324699812": "ButtonRoundCream",
        "114886820396282420": "Badge",
        "114926580033004315": "ButtonRoundCream",
        "114343514521034121": "Badge",
    },
    NEW: {
        "114967328832014496": "Title",
        "114017892387517381": "ButtonGreen",
        "114708180620337799": "ButtonGreen",
    },
    TOAST: {"114587765826080250": "Input"},
    SLOT: {"991499807541595864": "ButtonBlue", "4300416282917706685": "Panel"},
    PROP: {"8077983568778643066": "Close"},
    REWARD: {"2408382842900339161": "ProgressFill", "4325750691145160632": "Inset"},
    DANGER: {"114060945828524476": "Input", "114726790013014859": "Input"},
    GUIDE: {"114808113985169905": "Input"},
}

def backup(relative):
    source = ROOT / relative
    target = OUT / "Before" / relative
    target.parent.mkdir(parents=True, exist_ok=True)
    if not target.exists():
        shutil.copy2(source, target)
    return target

def normalized(text):
    # These are the only visual properties this pass is permitted to change.
    text = re.sub(r"(?m)^  m_Sprite: [^\r\n]*", "  m_Sprite: ART", text)
    text = re.sub(r"(?m)^  m_fontColor: [^\r\n]*", "  m_fontColor: ART", text)
    text = re.sub(r"(?m)^(  m_fontColor32:\r?\n    serializedVersion: 2\r?\n)    rgba: \d+", r"\1    rgba: ART", text)
    return text

report = {"kind": "Extra reachable UI art-only substitutions", "files": [], "runtimeOverrides": [], "preserved": []}
for relative, assignments in PLANS.items():
    saved = backup(relative)
    backup(relative + ".meta")
    original = (ROOT / relative).read_bytes().decode("utf-8-sig")
    baseline = saved.read_bytes().decode("utf-8-sig")
    updated = original
    edits = []
    for image_id, role in assignments.items():
        pattern = rf"(?ms)(^--- !u!114 &{image_id}\r?\n.*?)(?=^--- |\Z)"
        match = re.search(pattern, updated)
        assert match, (relative, image_id)
        block = match.group(1)
        old = re.search(r"(?m)^  m_Sprite: ([^\r\n]+)", block)
        assert old, (relative, image_id)
        guid, file_id = ROLE[role]
        new = f"{{fileID: {file_id}, guid: {guid}, type: 3}}"
        changed = block[:old.start(1)] + new + block[old.end(1):]
        updated = updated[:match.start(1)] + changed + updated[match.end(1):]
        baseline_block = re.search(pattern, baseline).group(1)
        baseline_ref = re.search(r"(?m)^  m_Sprite: ([^\r\n]+)", baseline_block).group(1)
        edits.append({"componentFileID": image_id, "property": "m_Sprite", "role": role, "before": baseline_ref, "after": new})
    if relative in (TOAST, DANGER, GUIDE):
        rgba = 23 | (79 << 8) | (125 << 16) | (255 << 24)
        updated, count = re.subn(r"(?m)^  m_fontColor: [^\r\n]+", "  m_fontColor: {r: 0.09019608, g: 0.30980393, b: 0.49019608, a: 1}", updated)
        assert count == (2 if relative == DANGER else 1)
        updated, count = re.subn(r"(?m)^(  m_fontColor32:\r?\n    serializedVersion: 2\r?\n)    rgba: \d+", lambda m: m.group(1) + f"    rgba: {rgba}", updated)
        assert count == (2 if relative == DANGER else 1)
        edits.append({"property": "m_fontColor and m_fontColor32", "after": "#174F7DFF", "reason": "Readable ink on the cream hint surface"})
    assert normalized(original) == normalized(updated), relative
    assert normalized(baseline) == normalized(updated), (relative, "existing baseline mismatch")
    source_bytes = (ROOT / relative).read_bytes()
    (ROOT / relative).write_bytes((b"\xef\xbb\xbf" if source_bytes.startswith(b"\xef\xbb\xbf") else b"") + updated.encode("utf-8"))
    report["files"].append({
        "path": relative,
        "backup": str(saved.relative_to(ROOT)).replace("\\", "/"),
        "edits": edits,
        "allNonArtBytesUnchanged": True,
        "immutableFieldsSha256": hashlib.sha256(normalized(updated).encode("utf-8")).hexdigest(),
        "gameObjectCount": len(re.findall(r"(?m)^--- !u!1 &", updated)),
        "rectTransformCount": len(re.findall(r"(?m)^--- !u!224 &", updated)),
        "componentCount": len(re.findall(r"(?m)^--- !u!114 &", updated)),
    })
report["runtimeOverrides"].append({"script": "Assets/FruitsHarvest/Scripts/CorePlayItemBtn.cs", "properties": ["normalBackground", "lockedBackground", "frameSprite"], "resolution": "Pending approved HarvestPaths remap to three single-Sprite copies cut from the final Controls atlas; business script unchanged"})
report["preserved"] = [
    {"path": ENTRY, "reason": "Every Image is a runtime reel symbol/reward presentation. No generic background is present; preserve all sprites and geometry."},
    {"path": CORE, "reason": "Keep all gameplay item icons, basket, map backgrounds, particles, locked markers, currency, AD meaning, and localizable text."},
    {"path": DANGER, "reason": "Keep the fruit illustration, both speech arrows and all warning text/state logic; replace only the two bubble surfaces and text color."},
    {"path": GUIDE, "reason": "Keep guide finger, tap rings, mask, text and timing; replace only the text card and its text color."},
]
(OUT / "extra-ui-art-report.json").write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
(OUT / "extra-ui-manifest.json").write_text(json.dumps({"prefabs": [CORE, NEW, TOAST, SLOT, ENTRY, DANGER, GUIDE], "existingManifestAdditionalArt": [PROP, REWARD]}, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps({"modifiedFiles": len(report["files"]), "spriteSubstitutions": sum(len(v) for v in PLANS.values()), "allNonArtBytesUnchanged": True}, indent=2))
