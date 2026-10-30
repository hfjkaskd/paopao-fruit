"""Sign a BizzaWZ Android export using the Gradle bundled with its Unity editor.

Keep the AGP 7.4.2 / Gradle 7.5.1 pairing, with Ad Review plugin 5.12.12.
Build APK and AAB in separate Gradle invocations: SafeDK selects incompatible
DEX input fields when both packaging tasks are present in the same task graph.
"""

import argparse
from pathlib import Path
import os
import re
import shutil
import subprocess
import tempfile

from validate_android_startup import apk_entry_points, aab_entry_points, validate


def package(args):
    export = args.export.resolve()
    output = args.output.resolve()
    android = args.unity_editor.resolve() / "Data/PlaybackEngines/AndroidPlayer"
    launcher = android / "Tools/gradle/lib/gradle-launcher-7.5.1.jar"
    if not launcher.is_file():
        raise ValueError("Expected Unity 2022.3 bundled Gradle 7.5.1")
    root_gradle = (export / "build.gradle").read_text(encoding="utf-8")
    if not re.search(r"id ['\"]com.android.application['\"] version ['\"]7\.4\.2['\"]", root_gradle):
        raise ValueError("Expected Android Gradle Plugin 7.4.2 in this export")
    if re.search(r"id ['\"]com.applovin.quality['\"]", root_gradle) and not re.search(
            r"id ['\"]com.applovin.quality['\"] version ['\"]5\.12\.12['\"]", root_gradle):
        raise ValueError("Expected Ad Review build plugin 5.12.12 for this export")
    java = android / "OpenJDK/bin/java.exe"
    bundletool = android / "Tools/bundletool-all-1.17.2.jar"
    aapt = android / "SDK/build-tools/34.0.0/aapt.exe"
    output.mkdir(parents=True, exist_ok=True)
    secrets = {}
    credentials = (args.signing_directory / "密码.txt").read_text(encoding="utf-8-sig")
    for field in ["storePassword", "keyPassword", "keyAlias"]:
        match = re.search(r"(?m)^\s*" + field + r"\s*[:=]?\s*[\"']([^\"']+)[\"']", credentials)
        if not match:
            raise ValueError("Missing signing field: " + field)
        secrets[field] = match.group(1)
    originals = {}
    log_path = output / "Gradle-build.log"
    try:
        with tempfile.TemporaryDirectory(prefix="signing-", dir=output) as temporary:
            key = Path(temporary) / "release.keystore"
            shutil.copy2(args.signing_directory / "Wiwitsugeh", key)
            env = os.environ.copy()
            env.pop("JAVA_TOOL_OPTIONS", None)
            env.update({
                "TEMP": str(output), "TMP": str(output),
                "ORCHARD_RELEASE_KEYSTORE": str(key),
                "ORCHARD_RELEASE_STORE_PASSWORD": secrets["storePassword"],
                "ORCHARD_RELEASE_KEY_ALIAS": secrets["keyAlias"],
                "ORCHARD_RELEASE_KEY_PASSWORD": secrets["keyPassword"],
            })
            replacements = {
                "storeFile": "file(System.getenv('ORCHARD_RELEASE_KEYSTORE'))",
                "storePassword": "System.getenv('ORCHARD_RELEASE_STORE_PASSWORD')",
                "keyAlias": "System.getenv('ORCHARD_RELEASE_KEY_ALIAS')",
                "keyPassword": "System.getenv('ORCHARD_RELEASE_KEY_PASSWORD')",
            }
            signing_blocks = 0
            for path in export.rglob("*.gradle"):
                if not path.is_file():
                    continue
                text = path.read_text(encoding="utf-8")
                if "storePassword" not in text:
                    continue
                originals[path] = path.read_bytes()
                for field, replacement in replacements.items():
                    text, count = re.subn(r"(?m)^(\s*)" + field + r"\s+.*$",
                                         lambda m: m.group(1) + field + " " + replacement, text)
                    if count != 1:
                        raise ValueError("Unexpected exported signing configuration")
                path.write_text(text, encoding="utf-8")
                signing_blocks += 1
            if signing_blocks == 0:
                raise ValueError("The Unity export has no release signing configuration")
            jvm = "-Xmx2048m -XX:ActiveProcessorCount=2 -XX:HeapBaseMinAddress=8G -XX:+UseSerialGC -XX:TieredStopAtLevel=1"
            command = [str(java), "-Xmx256m", "-XX:ActiveProcessorCount=2",
                       "-XX:HeapBaseMinAddress=8G", "-XX:+UseSerialGC",
                       "-classpath", str(launcher), "org.gradle.launcher.GradleMain",
                       "--no-daemon", "--max-workers=1", "-Dorg.gradle.jvmargs=" + jvm]
            startup = subprocess.STARTUPINFO()
            startup.dwFlags |= subprocess.STARTF_USESHOWWINDOW
            startup.wShowWindow = subprocess.SW_HIDE
            apk = export / "launcher/build/outputs/apk/release/launcher-release.apk"
            aab = export / "launcher/build/outputs/bundle/release/launcher-release.aab"
            with log_path.open("wb") as log:
                for task, suffix, artifact in [
                        (":launcher:assembleRelease", "apk", apk),
                        (":launcher:bundleRelease", "aab", aab)]:
                    log.write(("\nSeparate Gradle invocation: " + task + "\n").encode())
                    log.flush()
                    result = subprocess.run(command + [task], cwd=export, env=env, stdout=log,
                                            stderr=subprocess.STDOUT, startupinfo=startup)
                    if result.returncode:
                        raise RuntimeError("Gradle failed; see " + str(log_path))
                    if suffix == "apk":
                        package_id, required = apk_entry_points(apk, aapt)
                        validate(apk, package_id, required, "")
                        badging = subprocess.run([str(aapt), "dump", "badging", str(apk)], check=True,
                                                 capture_output=True, encoding="utf-8").stdout
                        code, version = re.search(r"versionCode='([^']+)' versionName='([^']+)'", badging).groups()
                        if package_id != "com.wiwitsugeh.orchardtrio" or (version, code) != (args.version, str(args.version_code)):
                            raise ValueError("Unexpected release package or version")
                    else:
                        package2, required2 = aab_entry_points(aab, java, bundletool)
                        if package2 != package_id or required2 != required:
                            raise ValueError("APK/AAB manifest entry points differ")
                        validate(aab, package2, required2, "base/dex/")
                    shutil.copy2(artifact, output / ("OrchardTrio_" + version + "_" + code + "." + suffix))
                    print(suffix.upper() + " packaged; manifest entry points verified.", flush=True)
            (output / "gradle-exit-code.txt").write_text("0\n", encoding="utf-8")
    finally:
        for path, data in originals.items():
            path.write_bytes(data)
        (output / "generated-signing-restored.txt").write_text("restored\n", encoding="utf-8")
        (output / "signing-temp-cleaned.txt").write_text("removed\n", encoding="utf-8")
        if log_path.exists():
            log = log_path.read_text(encoding="utf-8", errors="replace")
            for secret in secrets.values():
                log = log.replace(secret, "<redacted>")
            log_path.write_text(log, encoding="utf-8")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("export", type=Path)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--signing-directory", required=True, type=Path)
    parser.add_argument("--unity-editor", required=True, type=Path,
                        help="Unity Editor directory containing Unity.exe")
    parser.add_argument("--version", required=True, help="Expected release version name")
    parser.add_argument("--version-code", required=True, type=int, help="Expected Android version code")
    package(parser.parse_args())
