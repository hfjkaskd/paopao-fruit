"""Technical extraction of atlas pixels for existing Resources-based prop UI states."""
import hashlib
import json
from pathlib import Path
import re
import shutil
import uuid
from PIL import Image

ROOT = Path(__file__).resolve().parents[3]
OUT = Path(__file__).resolve().parent
ATLAS = ROOT / "Assets/OrchardUI/Art/Controls.png"
META = ROOT / "Assets/OrchardUI/Art/Controls.png.meta"
CATALOG = "Assets/FruitsHarvest/Resources/HarvestPaths.txt"
PLAN = [
    ("ItemBg_Normal", "ButtonRoundCream", "PropButtonNormal"),
    ("ItemBg_Lock", "DisabledCard", "PropButtonLocked"),
    ("ItemBg_Gray", "ButtonRoundCream", "PropButtonFrame"),
]

def backup(relative):
    source = ROOT / relative
    target = OUT / "Before" / relative
    target.parent.mkdir(parents=True, exist_ok=True)
    if not target.exists():
        shutil.copy2(source, target)
    return target

source_meta = META.read_text(encoding="utf-8-sig")
sprites = {}
for block in re.split(r"(?m)^    - serializedVersion:", source_meta)[1:]:
    name = re.search(r"(?m)^      name: (.+)$", block)
    if not name:
        continue
    rect = re.search(r"(?s)      rect:\s+serializedVersion: \d+\s+x: (\d+)\s+y: (\d+)\s+width: (\d+)\s+height: (\d+)", block)
    border = re.search(r"(?m)^      border: (.+)$", block)
    if rect and border:
        sprites[name.group(1).strip()] = tuple(map(int, rect.groups())), border.group(1).strip()

atlas = Image.open(ATLAS).convert("RGBA")
template = (ROOT / "Assets/OrchardUI/Resources/OrchardUI/Backdrop.png.meta").read_text(encoding="utf-8-sig")
backup(CATALOG)
backup(CATALOG + ".meta")
catalog = (ROOT / CATALOG).read_bytes().decode("utf-8-sig")
old_catalog = catalog
records = []
for old_name, role, new_name in PLAN:
    for suffix in (".asset", ".asset.meta", ".png", ".png.meta"):
        original = "Assets/FruitsHarvest/Resources/Original/res/local/coreplay/sprite/item/" + old_name + suffix
        if (ROOT / original).exists():
            backup(original)
    (x, y, width, height), border = sprites[role]
    # Unity sprite rect is bottom-left; PNG pixel coordinates are top-left.
    box = (x, atlas.height - y - height, x + width, atlas.height - y)
    crop = atlas.crop(box)
    relative = "Assets/OrchardUI/Resources/OrchardUI/" + new_name + ".png"
    destination = ROOT / relative
    destination.parent.mkdir(parents=True, exist_ok=True)
    crop.save(destination)
    meta_path = Path(str(destination) + ".meta")
    asset_guid = re.search(r"(?m)^guid: (\w+)$", meta_path.read_text()).group(1) if meta_path.exists() else uuid.uuid4().hex
    sprite_meta = re.sub(r"(?m)^guid: \w+$", "guid: " + asset_guid, template)
    sprite_meta = sprite_meta.replace("  alphaIsTransparency: 0", "  alphaIsTransparency: 1")
    sprite_meta = re.sub(r"(?m)^  spriteBorder: .+$", "  spriteBorder: " + border, sprite_meta)
    sprite_meta = re.sub(r"(?m)^(\s*)maxTextureSize: \d+$", r"\g<1>maxTextureSize: 512", sprite_meta)
    sprite_meta = re.sub(r"(?m)^    spriteID: .+$", "    spriteID: " + uuid.uuid5(uuid.NAMESPACE_URL, relative).hex, sprite_meta)
    meta_path.write_text(sprite_meta, encoding="utf-8", newline="\n")
    key = "res/local/coreplay/sprite/item/" + old_name.lower()
    old_path = "Original/res/local/coreplay/sprite/item/" + old_name
    resource_path = "OrchardUI/" + new_name
    pattern = r"(?m)^" + re.escape(key) + r"\t[^\r\n]+"
    catalog, count = re.subn(pattern, key + "\t" + resource_path, catalog)
    assert count == 1, (key, count)
    assert crop.tobytes() == atlas.crop(box).tobytes()
    records.append({"catalogKey": key, "oldResourcePath": old_path, "newResourcePath": resource_path,
                    "newAsset": relative, "guid": asset_guid, "sourceRole": role,
                    "sourceRectUnity": [x, y, width, height], "pixelExactAtlasExtraction": True,
                    "spriteBorder": border, "uncompressedBytes": width * height * 4})

source_bytes = (ROOT / CATALOG).read_bytes()
(ROOT / CATALOG).write_bytes((b"\xef\xbb\xbf" if source_bytes.startswith(b"\xef\xbb\xbf") else b"") + catalog.encode("utf-8"))
baseline = (OUT / "Before" / CATALOG).read_bytes().decode("utf-8-sig")
old_rows = baseline.splitlines()
new_rows = catalog.splitlines()
assert len(old_rows) == len(new_rows)
diffs = [(a, b) for a, b in zip(old_rows, new_rows) if a != b]
assert len(diffs) == 3
assert {a.split("\t")[0] for a, b in diffs} == {x["catalogKey"] for x in records}
report = {"kind": "Runtime UI state sprite resource remap, without business-script changes",
          "sourceAtlas": str(ATLAS.relative_to(ROOT)).replace("\\", "/"),
          "sourceAtlasSha256": hashlib.sha256(ATLAS.read_bytes()).hexdigest(),
          "sourceAtlasMetaSha256": hashlib.sha256(META.read_bytes()).hexdigest(),
          "catalog": CATALOG, "catalogChangedRows": len(diffs), "allOtherCatalogRowsUnchanged": True,
          "businessScriptsChanged": False, "stateSelectionLogicChanged": False, "assets": records,
          "newMaximumUncompressedTextureBytes": sum(x["uncompressedBytes"] for x in records)}
(OUT / "prop-button-resource-report.json").write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
extra_path = OUT / "extra-ui-art-report.json"
extra = json.loads(extra_path.read_text(encoding="utf-8"))
extra["runtimeOverrides"][0]["resolution"] = "Completed: three direct HarvestPaths entries now load pixel-exact single-Sprite cuts from the final shared Controls atlas. Existing locked/unlocked selection code unchanged."
extra["runtimeOverrides"][0]["report"] = "prop-button-resource-report.json"
extra_path.write_text(json.dumps(extra, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps({"newSingleSpriteResources": 3, "catalogChangedRows": 3,
                  "rawTextureBytes": report["newMaximumUncompressedTextureBytes"], "pixelExact": True}, indent=2))
