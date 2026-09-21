"""Read-only production audit; only this script's Design report is written.

The comparison preserves every byte outside three narrowly scoped visual values.
No YAML library is needed, and Unity object/field ordering is not normalized away.
Run after all authoring work has stopped. Exit 0 = PASS; exit 1 = FAIL.
"""
from __future__ import annotations

import argparse
from collections import Counter
from datetime import datetime, timezone
import difflib
import hashlib
import json
from pathlib import Path
import re
import sys


HERE = Path(__file__).resolve().parent
PROJECT = HERE.parents[2]
BEFORE = HERE / "Before"
IMAGE_GUID = "fe87c0e1cc204ed48ad3b37840f39efc"
TMP_GUIDS = {
    "f4688fdb7df04437aeb418b961361dc5",  # Package TextMeshProUGUI
    "3f8a1c2b9d4e5f60718293a4b5c6d7e8",  # Project TextMeshProCustom : TextMeshProUGUI
}
BUTTON_GUIDS = {
    "4e29b1a8efbd4b44bb3f3716e73f07ff",  # Package UnityEngine.UI.Button
    "3627447c183081f4eb0ff7951bee04a4",  # Project BizzaButton : Button
}
HEAD = re.compile(r"(?m)^--- !u!(\d+) &(-?\d+)(?: stripped)?\r?$")
SCRIPT = re.compile(r"(?m)^  m_Script: \{fileID: 11500000, guid: ([a-f0-9]{32}), type: 3\}\r?$")
SCALAR = {
    "m_Sprite": re.compile(r"(?m)^(  m_Sprite: )([^\r\n]+)"),
    "m_fontColor": re.compile(r"(?m)^(  m_fontColor: )([^\r\n]+)"),
    "m_fontColor32.rgba": re.compile(
        r"(?m)^(  m_fontColor32:\r?\n    serializedVersion: 2\r?\n    rgba: )(\d+)"
    ),
}


def sha(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest().upper()


def read_json(path: Path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def project_path(raw: str) -> Path:
    normalized = raw.replace("\\", "/")
    prefix = PROJECT.name + "/"
    if normalized.startswith(prefix):
        normalized = normalized[len(prefix):]
    candidate = (PROJECT / normalized).resolve()
    if not candidate.is_relative_to(PROJECT) or not normalized.startswith("Assets/"):
        raise ValueError("Expected project-local Assets path: " + raw)
    return candidate


def documents(text: str):
    matches = list(HEAD.finditer(text))
    if not matches:
        raise ValueError("No Unity YAML object documents found")
    blocks = []
    seen = set()
    for index, match in enumerate(matches):
        key = (match.group(1), match.group(2))
        if key[1] in seen:
            raise ValueError("Duplicate local fileID: " + key[1])
        seen.add(key[1])
        end = matches[index + 1].start() if index + 1 < len(matches) else len(text)
        blocks.append((key, text[match.start():end]))
    return text[:matches[0].start()], blocks


def normalize_visual_values(text: str):
    prefix, blocks = documents(text)
    result = [prefix]
    values = {}
    counts = Counter()
    for (class_id, local_id), block in blocks:
        counts["objects"] += 1
        if class_id == "1":
            counts["gameObjects"] += 1
        elif class_id == "224":
            counts["rectTransforms"] += 1
        elif class_id == "4":
            counts["transforms"] += 1
        elif class_id == "114":
            counts["monoBehaviours"] += 1
        script_match = SCRIPT.search(block)
        script = script_match.group(1) if script_match else ""
        if class_id == "114" and script in BUTTON_GUIDS:
            counts["buttons"] += 1
        allowed = ()
        if class_id == "114" and script == IMAGE_GUID:
            allowed = ("m_Sprite",)
        elif class_id == "114" and script in TMP_GUIDS:
            allowed = ("m_fontColor", "m_fontColor32.rgba")
        for name in allowed:
            pattern = SCALAR[name]
            matches = list(pattern.finditer(block))
            if len(matches) > 1:
                raise ValueError(f"Duplicate {name} in object {local_id}")
            if matches:
                values[(class_id, local_id, script, name)] = matches[0].group(2)
                block = pattern.sub(lambda m: m.group(1) + "<APPROVED_VISUAL_VALUE>", block)
        result.append(block)
    return "".join(result), values, dict(counts), [key for key, _ in blocks]


def inspect_prefab(before_data: bytes, after_data: bytes) -> dict:
    before_text = before_data.decode("utf-8")
    after_text = after_data.decode("utf-8")
    before, before_values, before_counts, before_ids = normalize_visual_values(before_text)
    after, after_values, after_counts, after_ids = normalize_visual_values(after_text)
    art_changes = []
    for key in sorted(before_values.keys() | after_values.keys()):
        if before_values.get(key) == after_values.get(key):
            continue
        art_changes.append({
            "classID": key[0], "componentFileID": key[1], "scriptGuid": key[2],
            "property": key[3], "before": before_values.get(key), "after": after_values.get(key),
        })
    immutable_equal = before == after
    return {
        "passed": immutable_equal,
        "byteIdentical": before_data == after_data,
        "beforeSha256": sha(before_data), "afterSha256": sha(after_data),
        "beforeImmutableSha256": sha(before.encode("utf-8")),
        "afterImmutableSha256": sha(after.encode("utf-8")),
        "allNonArtBytesUnchanged": immutable_equal,
        "objectIdsTypesAndOrderUnchanged": before_ids == after_ids,
        "beforeCounts": before_counts, "afterCounts": after_counts,
        "allowedVisualChanges": art_changes,
        "forbiddenDiff": list(difflib.unified_diff(
            before.splitlines(), after.splitlines(), fromfile="Before (visual values masked)",
            tofile="Current (visual values masked)", lineterm="", n=2,
        ))[:160] if not immutable_equal else [],
        "forbiddenDiffMayBeTruncated": not immutable_equal,
    }


def verify_code() -> dict:
    records = read_json(HERE / "code-hashes.json")
    expected = {}
    for record in records:
        path = project_path(record["path"])
        relative = path.relative_to(PROJECT).as_posix()
        if relative in expected:
            raise ValueError("Duplicate code hash: " + relative)
        expected[relative] = record.get("hash", record.get("sha256", "")).upper()
    changes = []
    for relative, digest in expected.items():
        path = PROJECT / relative
        actual = sha(path.read_bytes()) if path.is_file() else None
        if digest != actual:
            changes.append({"path": relative, "beforeSha256": digest, "afterSha256": actual})
    current = {p.relative_to(PROJECT).as_posix() for p in (PROJECT / "Assets").rglob("*.cs")}
    added = sorted(current - expected.keys())
    return {
        "passed": not changes and not added,
        "baselineCount": len(expected), "currentCount": len(current),
        "unchangedCount": len(expected) - len(changes),
        "changedOrMissing": changes, "added": added,
    }


def run_audit() -> dict:
    baseline_records = read_json(HERE / "baseline-hashes.json")
    baseline_hashes = {r["path"].replace("\\", "/"): r["sha256"].upper() for r in baseline_records}
    prefab_backups = sorted(BEFORE.rglob("*.prefab"))
    rows = []
    meta_rows = []
    backup_issues = []
    for backup in prefab_backups:
        relative = backup.relative_to(BEFORE).as_posix()
        current = project_path(relative)
        if not current.is_file():
            rows.append({"path": relative, "passed": False, "error": "Production prefab missing"})
            continue
        before_data = backup.read_bytes()
        if relative in baseline_hashes and sha(before_data) != baseline_hashes[relative]:
            backup_issues.append({"path": relative, "error": "Backup does not match recorded baseline hash"})
        try:
            row = inspect_prefab(before_data, current.read_bytes())
        except (UnicodeError, ValueError) as error:
            row = {"passed": False, "error": str(error)}
        rows.append({"path": relative, **row})
    for backup in sorted(BEFORE.rglob("*.prefab.meta")):
        relative = backup.relative_to(BEFORE).as_posix()
        current = project_path(relative)
        actual = sha(current.read_bytes()) if current.is_file() else None
        digest = sha(backup.read_bytes())
        meta_rows.append({"path": relative, "passed": digest == actual, "beforeSha256": digest, "afterSha256": actual})
    expected_prefabs = set()
    for manifest in [PROJECT / "Design/OrchardUI/prefab-manifest.json", HERE / "extra-ui-manifest.json"]:
        if manifest.is_file():
            expected_prefabs.update(read_json(manifest).get("prefabs", []))
    backed_paths = {row["path"] for row in rows}
    missing_backup = sorted(expected_prefabs - backed_paths)
    code = verify_code()
    passed = bool(rows) and all(row["passed"] for row in rows) and all(row["passed"] for row in meta_rows) and not backup_issues and code["passed"]
    return {
        "kind": "Read-only production Prefab structure/events and C# integrity comparison",
        "generatedUtc": datetime.now(timezone.utc).isoformat(),
        "project": str(PROJECT), "backupRoot": str(BEFORE),
        "status": "PASS" if passed else "FAIL", "passed": passed,
        "comparisonPolicy": {
            "permitted": [
                "UnityEngine.UI.Image m_Sprite value only (known Image script GUID)",
                "TextMeshProUGUI / TextMeshProCustom m_fontColor value only",
                "TextMeshProUGUI / TextMeshProCustom m_fontColor32.rgba value only",
            ],
            "locked": "All remaining bytes, including document order/IDs/types, GameObjects, hierarchy, all components, RectTransforms, Button fields, m_OnClick, custom onClick, raycast, layouts, localization keys, dynamic text, script references, and business fields.",
            "limitations": "This checks serialized integrity, not art quality, referenced Sprite import validity, runtime behavior, or account transactions. Script hashes cover Assets/**/*.cs. Manifest items without Before backup are disclosed and not included in the Prefab pass claim.",
        },
        "summary": {
            "checkedPrefabs": len(rows), "passedPrefabs": sum(r["passed"] for r in rows),
            "changedPrefabs": sum(not r.get("byteIdentical", False) for r in rows),
            "allowedVisualValueChanges": sum(len(r.get("allowedVisualChanges", [])) for r in rows),
            "checkedPrefabMetaFiles": len(meta_rows),
            "unchangedCodeFiles": code["unchangedCount"],
            "manifestPrefabsWithoutBackup": len(missing_backup),
        },
        "manifestPrefabsWithoutBackup": missing_backup,
        "baselineBackupIntegrityIssues": backup_issues,
        "prefabs": rows, "prefabMetaFiles": meta_rows, "code": code,
    }


def self_test() -> None:
    image = ("%YAML 1.1\n--- !u!114 &12\nMonoBehaviour:\n"
             "  m_Script: {fileID: 11500000, guid: " + IMAGE_GUID + ", type: 3}\n"
             "  m_GameObject: {fileID: 3}\n  m_Sprite: {fileID: 0}\n  m_RaycastTarget: 1\n")
    rect = "--- !u!224 &23\nRectTransform:\n  m_AnchoredPosition: {x: 0, y: 0}\n"
    button = ("--- !u!114 &45\nMonoBehaviour:\n  m_Script: {fileID: 11500000, guid: "
              "3627447c183081f4eb0ff7951bee04a4, type: 3}\n  m_OnClick:\n    m_PersistentCalls:\n      m_Calls: []\n"
              "  onClick:\n    m_PersistentCalls:\n      m_Calls: []\n")
    original = image + rect + button
    cases = [
        ("identical", original, True),
        ("image_sprite_allowed", original.replace("m_Sprite: {fileID: 0}", "m_Sprite: {fileID: 21300000, guid: " + "a" * 32 + ", type: 3}"), True),
        ("rect_change_rejected", original.replace("x: 0", "x: 1"), False),
        ("raycast_change_rejected", original.replace("m_RaycastTarget: 1", "m_RaycastTarget: 0"), False),
        ("custom_event_change_rejected", original.replace("  onClick:", "  onClickChanged:"), False),
        ("component_add_rejected", original + "--- !u!1 &99\nGameObject:\n  m_Name: Added\n", False),
        ("script_swap_rejected", original.replace(IMAGE_GUID, "b" * 32), False),
    ]
    for name, changed, expected in cases:
        assert inspect_prefab(original.encode(), changed.encode())["passed"] == expected, name
    business = original.replace(IMAGE_GUID, "c" * 32)
    assert not inspect_prefab(business.encode(), business.replace("m_Sprite: {fileID: 0}", "m_Sprite: {fileID: 1}").encode())["passed"]
    print(json.dumps({"selfTest": "PASS", "cases": len(cases) + 1}))


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--self-test", action="store_true", help="Run in-memory checks only; do not audit or write a report")
    parser.add_argument("--output", default="structure-integrity.json", help="Report filename inside this Implementation directory")
    args = parser.parse_args()
    if args.self_test:
        self_test()
        return 0
    target = (HERE / args.output).resolve()
    if target.parent != HERE or target.suffix != ".json":
        parser.error("Report must be a JSON file directly inside this Implementation directory")
    report = run_audit()
    target.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"status": report["status"], **report["summary"], "report": str(target)}, ensure_ascii=False))
    return 0 if report["passed"] else 1


if __name__ == "__main__":
    sys.exit(main())
