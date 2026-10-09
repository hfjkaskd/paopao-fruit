"""Validate the shipped English, Brazilian Portuguese, and Indonesian UI copy.

Run from the BizzaWZ project root: python Validation/validate_localization.py
"""

from collections import defaultdict
import json
from pathlib import Path
import re
import struct


ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / "Assets"
TABLE = ASSETS / "Game/Resources/ConfigAssets/TableBin/Table01/tbllanguage.bytes"
HARVEST = ASSETS / "FruitsHarvest/Resources/Original/res/local/configs/Text.json"
ORCHARD_GUID = "beb617f2d67ecd646999a830e787bab1"
UI_GUID = "49796e141a58b144486babe2f29d161b"
HARVEST_GUID = "9984842642d93d9132e26b28beb7ab0e"
FAQ_GUID = "5a2b13fe4b806db49ab41df9a6e3c0d0"
LANGUAGES = ("en-US", "pt-BR", "id-ID")
DOCUMENT = re.compile(r"(?ms)^--- !u!(\d+) &(-?\d+)(?: stripped)?\r?\n(.*?)(?=^--- !u!|\Z)")


def read_table():
    data = memoryview(TABLE.read_bytes())
    offset = 8

    def integer():
        nonlocal offset
        value = struct.unpack_from("<i", data, offset)[0]
        offset += 4
        return value

    def string():
        nonlocal offset
        length = 0
        shift = 0
        while True:
            byte = data[offset]
            offset += 1
            length |= (byte & 127) << shift
            shift += 7
            if byte < 128:
                break
        value = bytes(data[offset : offset + length]).decode("utf-8")
        offset += length
        return value

    def optional_string():
        nonlocal offset
        present = data[offset]
        offset += 1
        return string() if present else None

    if bytes(data[:4]) != b"BZC2" or string() != "tbllanguage":
        raise ValueError("Unexpected language table format")
    rows = {}
    for _ in range(integer()):
        key = optional_string()
        rows[key] = {optional_string(): optional_string() for _ in range(integer())}
    return rows


def field(block, name):
    match = re.search(r"^  " + re.escape(name) + r": ?(.*)$", block, re.M)
    return match.group(1).rstrip("\r") if match else ""


def script_guid(block):
    match = re.search(r"^  m_Script: .*guid: ([0-9a-f]+)", block, re.M)
    return match.group(1) if match else ""


def placeholders(value):
    return sorted(re.findall(r"\{(\d+)\}", value))


def main():
    table = read_table()
    original = {row["key"]: row for row in json.loads(HARVEST.read_text(encoding="utf-8"))}
    runtime_keys = set(re.findall(
        r'Add\(table,\s*"([^"]+)"',
        (ASSETS / "FruitsHarvest/Scripts/HarvestLocalization.cs").read_text(encoding="utf-8"),
    ))
    errors = []
    counts = defaultdict(int)

    for key, translations in table.items():
        for language in LANGUAGES:
            if not translations.get(language):
                errors.append(f"Language table {key}: missing {language}")
        values = [translations.get(language) or "" for language in LANGUAGES]
        if any(placeholders(value) != placeholders(values[0]) for value in values[1:]):
            errors.append(f"Language table {key}: format placeholders differ")
    for key, translations in original.items():
        for language in ("en", "pt", "id"):
            if not translations.get(language):
                errors.append(f"Harvest text {key}: missing {language}")
        values = [translations.get(language) or "" for language in ("en", "pt", "id")]
        if any(placeholders(value) != placeholders(values[0]) for value in values[1:]):
            errors.append(f"Harvest text {key}: format placeholders differ")

    for path in ASSETS.rglob("*.prefab"):
        source = path.read_text(encoding="utf-8")
        if not any(guid in source for guid in (ORCHARD_GUID, UI_GUID, HARVEST_GUID, FAQ_GUID)):
            continue
        active_by_object = defaultdict(set)
        documents = {match.group(2): match for match in DOCUMENT.finditer(source)}
        for file_id, match in documents.items():
            if match.group(1) != "114":
                continue
            block = match.group(3)
            guid = script_guid(block)
            if guid not in (ORCHARD_GUID, UI_GUID, HARVEST_GUID, FAQ_GUID):
                continue
            location = f"{path.relative_to(ROOT)}:{source.count(chr(10), 0, match.start()) + 1}"
            enabled = field(block, "m_Enabled") == "1"
            game_object = field(block, "m_GameObject")
            object_id = re.search(r"fileID: (-?\d+)", game_object)
            object_doc = documents.get(object_id.group(1)) if object_id else None
            if object_doc is None:
                errors.append(f"{location}: missing GameObject {game_object}")
            elif (f"component: {{fileID: {file_id}}}" not in object_doc.group(3)
                  and f"addedObject: {{fileID: {file_id}}}" not in source):
                errors.append(f"{location}: localizer is not attached to its GameObject")
            if enabled:
                active_by_object[game_object].add(guid)

            if guid == ORCHARD_GUID:
                counts["orchard"] += 1
                english = field(block, "english")
                portuguese = field(block, "portuguese")
                indonesian = field(block, "indonesian")
                key = field(block, "existingKey")
                target = field(block, "target")
                target_id = re.search(r"fileID: (-?\d+)", target)
                if target_id and target_id.group(1) != "0" and target_id.group(1) not in documents:
                    errors.append(f"{location}: missing target {target}")
                if not english or not portuguese:
                    errors.append(f"{location}: missing English or Portuguese")
                if not indonesian and (key not in table or not table[key].get("id-ID")):
                    errors.append(f"{location}: missing Indonesian and valid fallback key ({key})")
                if key and key not in table:
                    errors.append(f"{location}: unknown fallback key {key}")
            elif guid == UI_GUID and enabled:
                counts["ui"] += 1
                key = field(block, "key")
                if not key:
                    errors.append(f"{location}: empty framework key")
                elif key not in table and key not in runtime_keys:
                    errors.append(f"{location}: unknown framework key {key}")
            elif guid == HARVEST_GUID and enabled:
                counts["harvest"] += 1
                key = field(block, "key")
                if not key:
                    errors.append(f"{location}: empty Harvest key")
                elif key not in original:
                    errors.append(f"{location}: unknown Harvest key {key}")
            elif guid == FAQ_GUID and enabled:
                counts["faq"] += 1
                key = field(block, "key")
                if not key:
                    errors.append(f"{location}: empty FAQ key")
                elif key not in table:
                    errors.append(f"{location}: unknown FAQ key {key}")

        for game_object, guids in active_by_object.items():
            if ORCHARD_GUID in guids and (UI_GUID in guids or HARVEST_GUID in guids):
                errors.append(f"{path.relative_to(ROOT)}: competing localizers on {game_object}")

    formats = {
        "Assets/BizzaWZ/Final/Real/UI/FakeWithdrawPanel/FakeWithdrawPanel.prefab":
            ("englishRemainingFormat", "portugueseRemainingFormat", "indonesianRemainingFormat"),
        "Assets/BizzaWZ/Final/Real/UI/AddPropPanel/AddPropPanel.prefab":
            ("englishUsageFormat", "portugueseUsageFormat", "indonesianUsageFormat"),
        "Assets/BizzaWZ/Final/Real/UI/SlotsPanel/SlotPanel/SlotPanel.prefab":
            ("freeSpinEnglish", "freeSpinPortuguese", "freeSpinIndonesian"),
        "Assets/BizzaWZ/Final/Real/UI/DailyMissionPanel/DailyMissionPanel.prefab":
            ("requirementEnglish", "requirementPortuguese", "requirementIndonesian"),
        "Assets/BizzaWZ/Final/Real/UI/RealWithdrawalPanl/RealWithdrawPanel.prefab":
            ("completeFormatEnglish", "completeFormatPortuguese", "completeFormatIndonesian"),
    }
    formats["Assets/BizzaWZ/Final/Real/UI/DailyMissionPanel/DailyMissionPanel.prefab:countdown"] = (
        "countdownEnglish", "countdownPortuguese", "countdownIndonesian"
    )
    for file_name, names in formats.items():
        source = (ROOT / file_name.split(":")[0]).read_text(encoding="utf-8")
        values = [field(source, name) for name in names]
        if any(not value for value in values):
            errors.append(f"{file_name}: missing localized format {names}")
        elif any(placeholders(value) != placeholders(values[0]) for value in values[1:]):
            errors.append(f"{file_name}: placeholder mismatch {names}")

    booster = (ROOT / "Assets/BizzaWZ/Final/Real/UI/AddPropPanel/AddPropPanel.prefab").read_text(encoding="utf-8")
    for match in re.finditer(r"(?ms)^  - itemType: .*?(?=^  - itemType: |^--- !u!|\Z)", booster):
        copy = match.group(0)
        for name in ("englishName", "portugueseName", "indonesianName",
                     "englishDescription", "portugueseDescription", "indonesianDescription"):
            if not re.search(r"^    " + name + r": \S", copy, re.M):
                errors.append(f"AddPropPanel prop at line {booster.count(chr(10), 0, match.start()) + 1}: missing {name}")

    print(f"Checked {len(table)} framework rows, {len(original)} Harvest rows, "
          f"{counts['orchard']} Orchard labels, {counts['ui']} active framework labels, "
          f"{counts['harvest']} active Harvest labels, and {counts['faq']} FAQ entries.")
    for error in errors:
        print("ERROR:", error)
    if errors:
        raise SystemExit(1)
    print("PASS: formal-market localization bindings and formats are complete.")


if __name__ == "__main__":
    main()
