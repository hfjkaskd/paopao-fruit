"""Read-only GUID collision check for migrated Unity assets."""
from pathlib import Path
from collections import defaultdict
import re
import sys

sys.stdout.reconfigure(encoding="utf-8")
assets = Path(__file__).resolve().parents[2] / "BizzaWZ" / "Assets"
index = defaultdict(list)
for meta in assets.rglob("*.meta"):
    match = re.search(r"^guid: ([0-9a-f]{32})$", meta.read_text(encoding="utf-8-sig", errors="replace"), re.M)
    if match:
        index[match[1]].append(meta.relative_to(assets).as_posix())
collisions = {guid: paths for guid, paths in index.items()
              if len(paths) > 1 and any(path.startswith("FruitsHarvest/") for path in paths)}
print(f"Indexed {len(index)} GUIDs; migrated collisions: {len(collisions)}")
for guid, paths in collisions.items():
    print(guid, paths)
sys.exit(bool(collisions))
