"""Read project files only; append verification and label corrections to this run's report."""
from pathlib import Path
import hashlib
import json
import os
import re

expected_sdk_key = os.environ.get("ORCHARDTRIO_APPLOVIN_SDK_KEY", "").strip()
if not expected_sdk_key:
    raise ValueError("ORCHARDTRIO_APPLOVIN_SDK_KEY must be provided.")

project = Path("C:/Projects/paopao/BizzaWZ")
work = Path(__file__).resolve().parent
report_path = work / "release-config-results.json"
report = json.loads(report_path.read_text(encoding="utf-8-sig"))
assert report["status"] == "Passed"
checks = {}


def verify_scalars(relative, patterns):
    before = (work / "baseline" / relative).read_bytes()
    after = (project / relative).read_bytes()
    restored = after.decode("utf-8")
    original = before.decode("utf-8")
    for pattern, expected in patterns:
        old = re.search(pattern, original, re.MULTILINE)
        new = re.search(pattern, restored, re.MULTILINE)
        assert old is not None and new is not None
        assert new.group(2) == expected, relative + " field mismatch"
        restored = restored[:new.start(2)] + old.group(2) + restored[new.end(2):]
    assert restored.encode("utf-8") == before, relative + " unrelated bytes changed"
    checks[relative] = "Requested values verified; all other bytes preserved."


verify_scalars("ProjectSettings/ProjectSettings.asset", [
    (r"^(  productName: )([^\r\n]*)", "Orchard Trio"),
    (r"^(  applicationIdentifier:\r?\n    Android: )([^\r\n]*)", "com.wiwitsugeh.orchardtrio")])
verify_scalars("ProjectSettings/Obfuz.asset", [
    (r"^(    defaultStaticSecretKey: )([^\r\n]*)", "com.wiwitsugeh.orchardtrio"),
    (r"^(    defaultDynamicSecretKey: )([^\r\n]*)", "com.wiwitsugeh.orchardtrio"),
    (r"^(    codeGenerationSecretKey: )([^\r\n]*)\r?\n    encryptionOpCodeCount:", "orchardtrio"),
    (r"^(    obfuscatedNamePrefix: )([^\r\n]*)", report["obfuz"]["prefix"])])
assert re.fullmatch("[a-z]{3}", report["obfuz"]["prefix"])
obfuz = (project / "ProjectSettings/Obfuz.asset").read_text()
assert "    assembliesToObfuscate:\n    - Assembly-CSharp\n" in obfuz

relative = "Assets/MaxSdk/Resources/AppLovinSettings.asset"
sdk_bytes = (project / relative).read_bytes()
assert sdk_bytes == (work / "baseline" / relative).read_bytes()
sdk_key = re.search(r"^  sdkKey: ([^\r\n]*)", sdk_bytes.decode(), re.MULTILINE).group(1)
assert sdk_key == expected_sdk_key
checks[relative] = "Current request SDK key matches; complete original file preserved."

privacy = "https://docs.google.com/document/d/1UrrjU_q_GCdd77yPUV3tPJtGvgP5apasI3nJJWgFsOg/edit?usp=sharing"
privacy_targets = [
    "ProjectSettings/AppLovinInternalSettings.json",
    "Assets/BizzaWZ/Common/MenuSystem/Common/LoadingPanel/LoadingPanel.prefab",
    "Assets/FruitsHarvest/Resources/Original/res/local/configs/Text.json"]
for relative in privacy_targets:
    before = (work / "baseline" / relative).read_bytes()
    after = (project / relative).read_bytes()
    mapping = next(field for field in report["fields"] if field["target"].startswith(relative + " :: "))
    assert mapping["after"] == privacy
    assert after.replace(privacy.encode(), mapping["before"].encode()) == before
    checks[relative] = "Current request privacy URL verified; all other bytes preserved."
rows = json.loads((project / privacy_targets[2]).read_bytes())
row = next(item for item in rows if item["key"] == "coldstart_start_privacy")
languages = [language for language in row if language != "key"]
assert len(languages) == 32 and all(row[language].count(privacy) == 1 for language in languages)

# Independently reproduce the inspected project KeyGenerator's SHA-512 expansion.
block = hashlib.sha512(b"com.wiwitsugeh.orchardtrio").digest()
key_bytes = bytearray()
while len(key_bytes) < 1024:
    key_bytes.extend(block)
    block = hashlib.sha512(block).digest()
for name in ("defaultStaticSecretKey.bytes", "defaultDynamicSecretKey.bytes"):
    actual = (project / "Assets/Resources/Obfuz" / name).read_bytes()
    assert actual == key_bytes
checks["generatedKeyDerivation"] = "Both 1024-byte key outputs independently match the inspected project KeyGenerator algorithm and requested Android package."

hashes = []
for output in report["obfuz"]["outputs"]:
    relative = output["path"]
    before = (work / "baseline" / relative).read_bytes()
    after = (project / relative).read_bytes()
    digest = hashlib.sha256(after).hexdigest()
    assert digest == output["sha256"] and before != after
    hashes.append({"path": relative, "beforeSha256": hashlib.sha256(before).hexdigest(), "afterSha256": digest, "bytes": len(after), "matchesUnityGenerationReport": True})
relative = "Assets/StreamingAssets/ChannelConfig.bytes"
before = (work / "baseline" / relative).read_bytes()
after = (project / relative).read_bytes()
assert before != after and all(report["roundtrip"].values())
hashes.append({"path": relative, "beforeSha256": hashlib.sha256(before).hexdigest(), "afterSha256": hashlib.sha256(after).hexdigest(), "bytes": len(after), "serializationVerification": "All actual-Unity serializer round-trip/preservation checks in this run passed."})
assert not (work / "release-update.request.json").exists()

# Parent confirmed that these are the exact labels in the current user message.
for collection in (report["fields"], report["channelConfig"]["fields"]):
    for field in collection:
        if field["standardParameter"] == "AppLovin SDK Key":
            field["originalParameter"] = "AppLovin SDK Key"
        elif field["standardParameter"] == "Adjust 应用识别码":
            field["originalParameter"] = "Adjust 应用识别码"
report["finalReadOnlyVerification"] = {"status": "Passed", "checks": checks, "hashes": hashes, "oneShotRequestConsumed": True, "projectFilesWrittenByThisVerification": False}
report["notes"].append("The temporary helper was removed after execution; the ensuing Editor domain reload clears its temporary ChannelConfig singleton. Final compilation evidence is recorded separately by the root task.")
report_path.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print("Read-only final release verification passed. Current labels corrected in report; project files unchanged.")
