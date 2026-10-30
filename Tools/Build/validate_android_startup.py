"""Reject Android packages whose manifest entry points are missing from DEX.

This inspects class definitions, not just string references. SafeDK can finish
instrumentation while the packaging task still includes the original DEX files.
"""

import argparse
import json
from pathlib import Path
import re
import struct
import subprocess
import xml.etree.ElementTree as ET
import zipfile


def dex_classes(data):
    if not data.startswith(b"dex\n"):
        raise ValueError("Invalid DEX file")
    string_count, string_offset = struct.unpack_from("<II", data, 56)
    type_count, type_offset = struct.unpack_from("<II", data, 64)
    class_count, class_offset = struct.unpack_from("<II", data, 96)
    result = set()
    for index in range(class_count):
        type_index = struct.unpack_from("<I", data, class_offset + index * 32)[0]
        if type_index >= type_count:
            raise ValueError("Invalid DEX class index")
        string_index = struct.unpack_from("<I", data, type_offset + type_index * 4)[0]
        if string_index >= string_count:
            raise ValueError("Invalid DEX type name")
        offset = struct.unpack_from("<I", data, string_offset + string_index * 4)[0]
        # Skip the ULEB128 UTF-16 length. DEX names use modified UTF-8;
        # manifest entry points use regular names, while obfuscated SDK names
        # may contain Unicode or surrogate code units.
        while data[offset] & 0x80:
            offset += 1
        offset += 1
        end = data.index(0, offset)
        descriptor = data[offset:end].decode("utf-8", errors="replace")
        if descriptor.startswith("L") and descriptor.endswith(";"):
            result.add(descriptor[1:-1].replace("/", "."))
    return result


def qualify(package, name):
    if name.startswith("."):
        return package + name
    return name if "." in name else package + "." + name


def apk_entry_points(apk, aapt):
    tree = subprocess.run(
        [str(aapt), "dump", "xmltree", str(apk), "AndroidManifest.xml"],
        check=True, capture_output=True, encoding="utf-8", errors="replace"
    ).stdout
    package = re.search(r'A: package="([^"]+)"', tree).group(1)
    current_element = ""
    required = set()
    components = {"application", "activity", "service", "receiver", "provider"}
    for line in tree.splitlines():
        element = re.match(r"\s*E: ([\w-]+)", line)
        if element:
            current_element = element.group(1)
            continue
        attribute = re.match(r'\s*A: android:(name|targetActivity)[^=]*="([^"]+)"', line)
        if not attribute:
            continue
        key, value = attribute.groups()
        if current_element in components and key == "name":
            required.add(qualify(package, value))
        elif current_element == "activity-alias" and key == "targetActivity":
            required.add(qualify(package, value))
    if not required:
        raise ValueError("No Android entry points found")
    return package, required


def aab_entry_points(aab, java, bundletool):
    manifest = subprocess.run(
        [str(java), "-Xmx256m", "-jar", str(bundletool), "dump", "manifest",
         "--bundle=" + str(aab), "--module=base"],
        check=True, capture_output=True, encoding="utf-8", errors="replace"
    ).stdout
    root = ET.fromstring(manifest)
    package = root.attrib["package"]
    android = "{http://schemas.android.com/apk/res/android}"
    required = set()
    for element in root.iter():
        if element.tag in {"application", "activity", "service", "receiver", "provider"}:
            value = element.get(android + "name")
        elif element.tag == "activity-alias":
            value = element.get(android + "targetActivity")
        else:
            continue
        if value:
            required.add(qualify(package, value))
    return package, required


def validate(path, package, required, prefix):
    classes = set()
    with zipfile.ZipFile(path) as archive:
        for name in archive.namelist():
            if re.fullmatch(prefix + r"classes\d*\.dex", name):
                classes.update(dex_classes(archive.read(name)))
    if not classes:
        raise ValueError("No DEX class definitions found")
    missing = sorted(name for name in required
                     if not name.startswith("android.") and name not in classes)
    if missing:
        raise ValueError("Manifest classes missing from DEX: " + ", ".join(missing))
    return {"file": str(path), "package": package,
            "entryPointCount": len(required), "classDefinitionCount": len(classes),
            "entryPoints": sorted(required), "passed": True}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("apk", type=Path)
    parser.add_argument("--android-player", required=True, type=Path)
    parser.add_argument("--aab", type=Path)
    parser.add_argument("--report", type=Path)
    args = parser.parse_args()
    aapt = args.android_player / "SDK/build-tools/34.0.0/aapt.exe"
    package, required = apk_entry_points(args.apk, aapt)
    reports = [validate(args.apk, package, required, "")]
    if args.aab:
        package2, required2 = aab_entry_points(
            args.aab, args.android_player / "OpenJDK/bin/java.exe",
            args.android_player / "Tools/bundletool-all-1.17.2.jar")
        if package2 != package or required2 != required:
            raise ValueError("APK and AAB manifests declare different entry points")
        reports.append(validate(args.aab, package2, required2, "base/dex/"))
    output = json.dumps(reports, indent=2, ensure_ascii=False)
    if args.report:
        args.report.write_text(output + "\n", encoding="utf-8")
    print(output)


if __name__ == "__main__":
    main()
