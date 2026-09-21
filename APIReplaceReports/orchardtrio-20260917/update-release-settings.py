"""Apply this request's confirmed textual release settings, preserving unrelated bytes."""
from pathlib import Path
import json
import os
import re
import secrets
import string

sdk_key = os.environ.get("ORCHARDTRIO_APPLOVIN_SDK_KEY", "").strip()
if not sdk_key:
    raise ValueError("ORCHARDTRIO_APPLOVIN_SDK_KEY must be provided.")

PROJECT = Path("C:/Projects/paopao/BizzaWZ")
WORK = Path(__file__).resolve().parent
BASELINE = WORK / "baseline"
fields = []
changed = []


def snapshot(relative):
    current = PROJECT / relative
    backup = BASELINE / relative
    if not backup.exists():
        backup.parent.mkdir(parents=True, exist_ok=True)
        backup.write_bytes(current.read_bytes())
    return current.read_bytes()


def mask(value):
    return value[:3] + "***" + value[-3:] if len(value) >= 8 else "***"


def field(original, standard, target, before, after, sensitive=False):
    fields.append({"originalParameter": original, "standardParameter": standard,
                   "target": target, "before": mask(before) if sensitive else before,
                   "after": mask(after) if sensitive else after,
                   "status": "AlreadyMatched" if before == after else "Updated"})


def scalar(text, pattern, new_value, original, standard, target, sensitive=False):
    matches = list(re.finditer(pattern, text, flags=re.MULTILINE))
    assert len(matches) == 1, f"Ambiguous field: {target}"
    match = matches[0]
    field(original, standard, target, match.group(2), new_value, sensitive)
    return text[:match.start(2)] + new_value + text[match.end(2):]


def save(relative, before, after):
    if before != after:
        (PROJECT / relative).write_bytes(after)
        changed.append(relative)


relative = "ProjectSettings/ProjectSettings.asset"
before = snapshot(relative)
after = scalar(before.decode("utf-8"), r"^(  productName: )([^\r\n]*)", "Orchard Trio", "应用名称", "应用名称", relative + " :: productName")
after = scalar(after, r"^(  applicationIdentifier:\r?\n    Android: )([^\r\n]*)", "com.wiwitsugeh.orchardtrio", "包名", "Android 包名", relative + " :: applicationIdentifier.Android")
save(relative, before, after.encode("utf-8"))

relative = "ProjectSettings/Obfuz.asset"
before = snapshot(relative)
after = before.decode("utf-8")
assembly_match = re.search(r"assembliesToObfuscate:\r?\n((?:    - [^\r\n]+\r?\n)+)", after)
assert assembly_match is not None
assemblies = re.findall(r"    - ([^\r\n]+)", assembly_match.group(1))
assert assemblies == ["Assembly-CSharp"], "Expected configured Assembly-CSharp; avoid silently replacing other assemblies."
field("包名", "Android 包名 / Obfuz", relative + " :: assemblySettings.assembliesToObfuscate", assemblies, assemblies)
for key in ("defaultStaticSecretKey", "defaultDynamicSecretKey"):
    after = scalar(after, r"^(    " + key + r": )([^\r\n]*)", "com.wiwitsugeh.orchardtrio", "包名", "Android 包名 / Obfuz", relative + " :: secretSettings." + key, True)
after = scalar(after, r"^(    codeGenerationSecretKey: )([^\r\n]*)\r?\n    encryptionOpCodeCount:", "orchardtrio", "包名", "Android 包名 / Obfuz", relative + " :: encryptionVMSettings.codeGenerationSecretKey", True)
prefix = "".join(secrets.choice(string.ascii_lowercase) for _ in range(3))
after = scalar(after, r"^(    obfuscatedNamePrefix: )([^\r\n]*)", prefix, "包名", "Android 包名 / Obfuz 随机前缀", relative + " :: symbolObfusSettings.obfuscatedNamePrefix")
save(relative, before, after.encode("utf-8"))

relative = "Assets/MaxSdk/Resources/AppLovinSettings.asset"
before = snapshot(relative)
after = scalar(before.decode("utf-8"), r"^(  sdkKey: )([^\r\n]*)", sdk_key, "SDK Key", "AppLovin SDK Key", relative + " :: sdkKey", True)
save(relative, before, after.encode("utf-8"))

privacy = "https://docs.google.com/document/d/1UrrjU_q_GCdd77yPUV3tPJtGvgP5apasI3nJJWgFsOg/edit?usp=sharing"
relative = "ProjectSettings/AppLovinInternalSettings.json"
before = snapshot(relative)
content = before.decode("utf-8")
old = json.loads(content)["consentFlowPrivacyPolicyUrl"]
old_token = json.dumps(old)
assert content.count(old_token) == 1
after = content.replace(old_token, json.dumps(privacy))
field("Privacy Policy URL", "Privacy Policy URL", relative + " :: consentFlowPrivacyPolicyUrl", old, privacy)
save(relative, before, after.encode("utf-8"))

relative = "Assets/BizzaWZ/Common/MenuSystem/Common/LoadingPanel/LoadingPanel.prefab"
before = snapshot(relative)
content = before.decode("utf-8")
assert "key: coldstart_start_privacy" in content
urls = re.findall(r'https://[^\s<>"\\]+', content)
assert len(urls) == 1, "Resolve loading Privacy link uniquely before updating."
old = urls[0]
after = content.replace(old, privacy)
field("Privacy Policy URL", "Privacy Policy URL", relative + " :: Privacy TMP hyperlink", old, privacy)
assert after.replace(privacy, old) == content
save(relative, before, after.encode("utf-8"))

relative = "Assets/FruitsHarvest/Resources/Original/res/local/configs/Text.json"
before = snapshot(relative)
content = before.decode("utf-8")
rows = json.loads(content)
privacy_rows = [row for row in rows if row["key"] == "coldstart_start_privacy"]
assert len(privacy_rows) == 1
row = privacy_rows[0]
languages = [language for language in row if language != "key"]
old_urls = {url for language in languages for url in re.findall(r'<link="([^"]+)"', row[language])}
assert len(old_urls) == 1
old = next(iter(old_urls))
assert all(row[language].count(old) == 1 for language in languages)
assert content.count(old) == len(languages)
after = content.replace(old, privacy)
assert after.replace(privacy, old) == content
field("Privacy Policy URL", "Privacy Policy URL", relative + " :: coldstart_start_privacy [" + str(len(languages)) + " languages]", old, privacy)
save(relative, before, after.encode("utf-8"))

generated_paths = ["Assets/Obfuz/GeneratedEncryptionVirtualMachine.cs", "Assets/Resources/Obfuz/defaultStaticSecretKey.bytes", "Assets/Resources/Obfuz/defaultDynamicSecretKey.bytes"]
for relative in ["Assets/StreamingAssets/ChannelConfig.bytes"] + generated_paths:
    snapshot(relative)

result = {
    "status": "TextSettingsUpdated;UnitySerializationAndGenerationPending",
    "project": str(PROJECT),
    "fields": fields,
    "changedReleaseFiles": changed,
    "channelConfig": {"status": "PendingUnityExecution", "serializer": "Bizza.Sdk.ChannelConfigBinarySerializer"},
    "obfuz": {"status": "SettingsUpdated;GenerationPending", "prefix": prefix, "outputs": generated_paths},
    "privacyLocalization": {"status": "Passed", "key": "coldstart_start_privacy", "languageCount": len(languages), "nonUrlBytesPreserved": True, "runtimePath": "LoadingPanel TextNeedLocalization -> OJEEJGGLNPC.GetText / LoadConfig -> GameRes -> HarvestPaths -> Resources/Original/res/local/configs/Text.json"},
    "notes": ["Only current request values are targets; existing project values were read solely for before snapshots and preservation.", "Blank default country/banner/open ad parameters will be retained by the Unity serializer helper.", "AppLovin SDK key already matches the request.", "No runtime method or gameplay logic changed."]
}
(WORK / "release-config-results.json").write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(f"Release text settings updated: {len(changed)} files; privacy languages: {len(languages)}; Obfuz prefix: {prefix}.")
