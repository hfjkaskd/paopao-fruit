"""Apply only the current request's confirmed release configuration fields."""
from pathlib import Path
import hashlib
import json
import re
import secrets
import string

work = Path(__file__).resolve().parent
project = work.parents[2] / "BizzaWZ"
baseline = work / "baseline"
fields = []
changed = []


def mask(value):
    return value[:3] + "***" + value[-3:] if len(value) >= 8 else "***"


def record(original, standard, target, before, after, sensitive=False):
    fields.append({
        "originalParameter": original, "standardParameter": standard,
        "target": target, "before": mask(before) if sensitive else before,
        "after": mask(after) if sensitive else after,
        "status": "AlreadyMatched" if before == after else "Updated"
    })


def read(relative, baseline_name=None):
    path = project / relative
    backup = baseline / (baseline_name or relative)
    if not backup.exists():
        backup.parent.mkdir(parents=True, exist_ok=True)
        backup.write_bytes(path.read_bytes())
    return path.read_bytes().decode("utf-8-sig")


def write(relative, before, after):
    if before != after:
        path = project / relative
        original_bytes = path.read_bytes()
        encoding = "utf-8-sig" if original_bytes.startswith(b"\xef\xbb\xbf") else "utf-8"
        path.write_bytes(after.encode(encoding))
        changed.append(relative)


def replace_scalar(text, pattern, value, original, standard, target, sensitive=False, reported_value=None):
    matches = list(re.finditer(pattern, text, flags=re.MULTILINE))
    assert len(matches) == 1, f"Expected one field: {target}, found {len(matches)}"
    match = matches[0]
    old = match.group(2)
    record(original, standard, target, old, reported_value if reported_value is not None else value, sensitive)
    return text[:match.start(2)] + value + text[match.end(2):]


project_settings = "ProjectSettings/ProjectSettings.asset"
before = read(project_settings, "ProjectSettings.asset")
after = replace_scalar(before, r"^(  productName: )([^\r\n]*)", '"Bubblosaic: Picture Merge"', "应用名称", "应用名称", project_settings + " :: productName", reported_value="Bubblosaic: Picture Merge")
after = replace_scalar(after, r"^(  applicationIdentifier:\r?\n    Android: )([^\r\n]*)", "com.webpack.picturemerge", "包名", "Android 包名", project_settings + " :: applicationIdentifier.Android")
write(project_settings, before, after)

obfuz_settings = "ProjectSettings/Obfuz.asset"
before = read(obfuz_settings, "Obfuz.asset")
assert "    assembliesToObfuscate:\r\n    - Assembly-CSharp\r\n" in before or "    assembliesToObfuscate:\n    - Assembly-CSharp\n" in before
record("包名", "Android 包名 / Obfuz", obfuz_settings + " :: assemblySettings.assembliesToObfuscate", ["Assembly-CSharp"], ["Assembly-CSharp"])
after = before
for key in ("defaultStaticSecretKey", "defaultDynamicSecretKey"):
    after = replace_scalar(after, r"^(    " + key + r": )([^\r\n]*)", "com.webpack.picturemerge", "包名", "Android 包名 / Obfuz", obfuz_settings + " :: secretSettings." + key, True)
after = replace_scalar(after, r"^(    codeGenerationSecretKey: )([^\r\n]*)\r?\n    encryptionOpCodeCount:", "picturemerge", "包名", "Android 包名 / Obfuz", obfuz_settings + " :: encryptionVMSettings.codeGenerationSecretKey", True)
name_prefix = "".join(secrets.choice(string.ascii_lowercase) for _ in range(3))
after = replace_scalar(after, r"^(    obfuscatedNamePrefix: )([^\r\n]*)", name_prefix, "包名", "Android 包名 / Obfuz 随机前缀", obfuz_settings + " :: symbolObfusSettings.obfuscatedNamePrefix")
write(obfuz_settings, before, after)

sdk_settings = "Assets/MaxSdk/Resources/AppLovinSettings.asset"
before = read(sdk_settings)
sdk_key = "eEzDO_Ksp3ps7QJkce3MBdxPdGy2Ug0KHSZXf3Wn89mZxIpqFTNbJDa0cvpXa9Ss1lilI2DZclctfNt6GexyJZ"
after = replace_scalar(before, r"^(  sdkKey: )([^\r\n]*)", sdk_key, "AppLovin SDK Key", "AppLovin SDK Key", sdk_settings + " :: sdkKey", True)
write(sdk_settings, before, after)

privacy_url = "https://docs.google.com/document/d/1P9mcb86TrNkjX-tOO8SeSlvSOGa1648MxkWQ9THaFXY/edit?usp=sharing"
consent_settings = "ProjectSettings/AppLovinInternalSettings.json"
before = read(consent_settings)
settings = json.loads(before)
old_url = settings["consentFlowPrivacyPolicyUrl"]
record("Privacy Policy URL", "Privacy Policy URL", consent_settings + " :: consentFlowPrivacyPolicyUrl", old_url, privacy_url)
assert before.count(json.dumps(old_url)) == 1
after = before.replace(json.dumps(old_url), json.dumps(privacy_url))
assert json.loads(after)["consentFlowPrivacyPolicyUrl"] == privacy_url
write(consent_settings, before, after)

loading_prefab = "Assets/BizzaWZ/Common/MenuSystem/Common/LoadingPanel/LoadingPanel.prefab"
before = read(loading_prefab)
old_url = "https://privacy.flyfoxgames.com/privacy.html"
assert before.count(old_url) == 1
record("Privacy Policy URL", "Privacy Policy URL", loading_prefab + " :: PrivacyPolicy TMP link", old_url, privacy_url)
after = before.replace(old_url, privacy_url)
write(loading_prefab, before, after)

localization_settings = "Assets/FruitsHarvest/Resources/Original/res/local/configs/Text.json"
before = read(localization_settings)
localization = json.loads(before)
privacy_rows = [row for row in localization if row["key"] == "coldstart_start_privacy"]
assert len(privacy_rows) == 1
privacy_row = privacy_rows[0]
languages = [language for language in privacy_row if language != "key"]
assert len(languages) == 32
assert all(privacy_row[language].count(old_url) == 1 for language in languages)
assert before.count(old_url) == len(languages)
after = before.replace(old_url, privacy_url)
assert after.replace(privacy_url, old_url) == before
record("Privacy Policy URL", "Privacy Policy URL", localization_settings + " :: coldstart_start_privacy [32 languages]", old_url, privacy_url)
write(localization_settings, before, after)

# The runtime loads all three outputs. Back them up before the Unity helper regenerates them.
obfuz_outputs = ["Assets/Obfuz/GeneratedEncryptionVirtualMachine.cs", "Assets/Resources/Obfuz/defaultStaticSecretKey.bytes", "Assets/Resources/Obfuz/defaultDynamicSecretKey.bytes"]
for relative in obfuz_outputs:
    backup = baseline / relative
    if not backup.exists():
        backup.parent.mkdir(parents=True, exist_ok=True)
        backup.write_bytes((project / relative).read_bytes())

channel = json.loads((work / "channel-config-results.json").read_text(encoding="utf-8-sig"))
result = {
    "status": "ReleaseSettingsUpdated;ObfuzGenerationPending",
    "project": str(project),
    "fields": channel["fields"] + fields,
    "preserved": channel["preserved"],
    "roundtrip": channel["roundtrip"],
    "channelConfig": channel,
    "changedReleaseFiles": ["Assets/StreamingAssets/ChannelConfig.bytes"] + changed,
    "obfuz": {"status": "PendingUnityGeneration", "prefix": name_prefix, "outputs": obfuz_outputs, "reason": "GameObfuz loads Resources secret bytes and GeneratedEncryptionVirtualMachine at runtime; generated outputs must match the updated settings."},
    "notes": ["Blank values preserved; AppLovin SDK key already matched the latest request.", "Privacy URL updated in MAX consent settings, active loading Prefab, and its actual coldstart_start_privacy localization data for all 32 languages; text content and unrelated original prefabs preserved.", "No runtime C# method or serialization implementation modified.", "Android package updated only in Android PlayerSettings; unspecified other platform identifiers retained."]
}
(work / "release-config-results.json").write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(f"Updated {len(changed)} release settings files; Obfuz prefix {name_prefix}; result JSON written with masked secrets.")
