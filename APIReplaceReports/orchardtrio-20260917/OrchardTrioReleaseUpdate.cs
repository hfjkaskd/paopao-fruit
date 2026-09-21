// Temporary Editor helper for the current Orchard Trio request only.
// Import into Assets/Editor, execute OrchardTrioReleaseUpdate.ApplyAndVerify,
// then remove the temporary copy and its meta before the final project compile.
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Bizza.Sdk;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Obfuz.EncryptionVM;
using Obfuz.Settings;
using Obfuz.Utils;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

public static class OrchardTrioReleaseUpdate
{
    private const string Project = "C:/Projects/paopao/BizzaWZ";
    private const string Work = "C:/Projects/paopao/APIReplaceReports/orchardtrio-20260917";
    private const string Package = "com.wiwitsugeh.orchardtrio";
    private const string RequestPath = Work + "/release-update.request.json";
    private static double nextRequestCheck;
    private static bool executing;

    [InitializeOnLoadMethod]
    private static void RegisterExplicitRequestWatcher()
    {
        EditorApplication.update -= CheckExplicitRequest;
        EditorApplication.update += CheckExplicitRequest;
    }

    private static void CheckExplicitRequest()
    {
        if (executing || EditorApplication.isCompiling || EditorApplication.isUpdating
            || EditorApplication.isPlayingOrWillChangePlaymode) return;
        double now = EditorApplication.timeSinceStartup;
        if (now < nextRequestCheck) return;
        nextRequestCheck = now + 1.0;
        if (!File.Exists(RequestPath)) return;
        executing = true;
        EditorApplication.update -= CheckExplicitRequest;
        try { ApplyAndVerify(); }
        catch (Exception exception) { Debug.LogError("Orchard Trio release update failed: " + exception.Message); }
        finally { executing = false; }
    }

    private static string ConsumeRequestedKey()
    {
        if (!File.Exists(RequestPath))
            return Environment.GetEnvironmentVariable("APIREPLACE_ORCHARD_AES_KEY");
        // The request is an explicit one-shot authorization from this task. Consume it
        // before any asset write, so a domain reload cannot replay the operation.
        string requestText = File.ReadAllText(RequestPath);
        File.Delete(RequestPath);
        var request = JObject.Parse(requestText);
        Require((string)request["requestId"] == "orchardtrio-20260917", "Unexpected release update request ID.");
        return (string)request["aesKey"];
    }

    private static void Require(bool valid, string message)
    {
        if (!valid) throw new InvalidDataException(message);
    }

    private static string Mask(string value)
    {
        if (string.IsNullOrEmpty(value)) return "(empty)";
        return value.Length < 8 ? "***" : value.Substring(0, 3) + "***" + value.Substring(value.Length - 3);
    }

    private static void Field(JArray fields, string original, string normalized, string target, object before, object after, bool sensitive = false)
    {
        fields.Add(new JObject
        {
            ["originalParameter"] = original,
            ["standardParameter"] = normalized,
            ["target"] = "Assets/StreamingAssets/ChannelConfig.bytes :: " + target,
            ["before"] = sensitive ? Mask(Convert.ToString(before)) : JToken.FromObject(before ?? ""),
            ["after"] = sensitive ? Mask(Convert.ToString(after)) : JToken.FromObject(after ?? ""),
            ["status"] = Equals(before, after) ? "AlreadyMatched" : "Updated"
        });
    }

    private static string HashFile(string path)
    {
        using (var sha = SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
    }

    public static void ApplyAndVerify()
    {
        string resultPath = Path.Combine(Work, "release-config-results.json");
        var result = JObject.Parse(File.ReadAllText(resultPath));
        try
        {
            Require(string.Equals(Path.GetFullPath(Application.dataPath).Replace('\\', '/'), Project + "/Assets", StringComparison.OrdinalIgnoreCase), "Wrong Unity project; no changes applied.");
            string key = ConsumeRequestedKey();
            Require(!string.IsNullOrEmpty(key), "Current request AES key is required via the one-shot request or APIREPLACE_ORCHARD_AES_KEY.");
            // The user may already have this project open. Update only the requested
            // PlayerSettings properties through Unity so its in-memory state agrees
            // with the staged on-disk configuration without replacing other settings.
            PlayerSettings.productName = "Orchard Trio";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, Package);
            Require(PlayerSettings.productName == "Orchard Trio", "Product name mismatch.");
            Require(PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android) == Package, "Android package mismatch.");

            string relative = "Assets/StreamingAssets/ChannelConfig.bytes";
            string baselinePath = Path.Combine(Work, "baseline", relative);
            string targetPath = Path.Combine(Project, relative);
            byte[] baseline = File.ReadAllBytes(baselinePath);
            var before = ChannelConfigBinarySerializer.Deserialize(baseline);
            Require(baseline.SequenceEqual(ChannelConfigBinarySerializer.Serialize(before)), "Baseline ChannelConfig byte round-trip failed.");
            var config = ChannelConfigBinarySerializer.Deserialize(baseline);
            Require(config.httpConfig != null, "Missing HTTP config.");
            Require(config.sourceAds != null && config.sourceAds.Count(ad => ad != null && ad.AdsSource == E_AdsSource.Max) == 1, "MAX source must resolve uniquely.");
            var adBefore = before.sourceAds.Single(ad => ad != null && ad.AdsSource == E_AdsSource.Max);
            var adCurrent = config.sourceAds.Single(ad => ad != null && ad.AdsSource == E_AdsSource.Max);
            var fields = new JArray();
            Field(fields, "应用ID", "应用ID", "AppId", config.AppId, "orchardtrio");
            config.AppId = "orchardtrio";
            Field(fields, "接口域名地址", "接口域名地址", "httpConfig.domain", config.httpConfig.domain, "https://snakes.xin");
            config.httpConfig.domain = "https://snakes.xin";
            Field(fields, "密钥", "密钥", "httpConfig.aes_key", config.httpConfig.aes_key, key, true);
            config.httpConfig.aes_key = key;
            Field(fields, "游戏类型", "游戏类型", "incomeRate", config.incomeRate, 1.0);
            config.incomeRate = 1.0;
            Field(fields, "国家", "国家", "real_CustomConfig.Country", (int)config.real_CustomConfig.Country, (int)AccountModule.E_CountryType.None);
            config.real_CustomConfig.Country = AccountModule.E_CountryType.None;
            Field(fields, "Adjust", "Adjust 应用识别码", "adjustKey", config.adjustKey, "d1hq1hl2p05c");
            config.adjustKey = "d1hq1hl2p05c";
            Field(fields, "MAX 激励广告 ID", "MAX 激励广告 ID", "sourceAds[AdsSource=Max].rewardAdId", adCurrent.rewardAdId, "f5ccdcd203e833b6");
            adCurrent.rewardAdId = "f5ccdcd203e833b6";
            Field(fields, "MAX 插屏广告 ID", "MAX 插屏广告 ID", "sourceAds[AdsSource=Max].interAdId", adCurrent.interAdId, "00fcf19979f18911");
            adCurrent.interAdId = "00fcf19979f18911";

            byte[] updated = ChannelConfigBinarySerializer.Serialize(config);
            var readback = ChannelConfigBinarySerializer.Deserialize(updated);
            Require(updated.SequenceEqual(ChannelConfigBinarySerializer.Serialize(readback)), "Updated ChannelConfig byte round-trip failed.");
            var adReadback = readback.sourceAds.Single(ad => ad != null && ad.AdsSource == E_AdsSource.Max);
            Require(readback.AppId == "orchardtrio" && readback.httpConfig.domain == "https://snakes.xin"
                && readback.httpConfig.aes_key == key && readback.adjustKey == "d1hq1hl2p05c"
                && readback.incomeRate == 1.0 && readback.real_CustomConfig.Country == AccountModule.E_CountryType.None
                && adReadback.rewardAdId == "f5ccdcd203e833b6" && adReadback.interAdId == "00fcf19979f18911", "Requested ChannelConfig values did not read back.");
            Require(readback.real_CustomConfig.DefaultCountry == before.real_CustomConfig.DefaultCountry
                && adReadback.bannerAdId == adBefore.bannerAdId && adReadback.openAdId == adBefore.openAdId, "A blank input was overwritten.");
            // Undo only current-request changes in memory. Whole-file equality proves
            // every other serialized field, including existing user changes, was preserved.
            readback.AppId = before.AppId;
            readback.httpConfig.domain = before.httpConfig.domain;
            readback.httpConfig.aes_key = before.httpConfig.aes_key;
            readback.adjustKey = before.adjustKey;
            readback.incomeRate = before.incomeRate;
            readback.real_CustomConfig.Country = before.real_CustomConfig.Country;
            adReadback.rewardAdId = adBefore.rewardAdId;
            adReadback.interAdId = adBefore.interAdId;
            Require(baseline.SequenceEqual(ChannelConfigBinarySerializer.Serialize(readback)), "Unrelated ChannelConfig bytes changed.");
            byte[] currentFile = File.ReadAllBytes(targetPath);
            Require(currentFile.SequenceEqual(baseline) || currentFile.SequenceEqual(updated), "ChannelConfig changed since this request's baseline; stop before overwriting.");
            File.WriteAllBytes(targetPath, updated);
            Require(updated.SequenceEqual(File.ReadAllBytes(targetPath)), "ChannelConfig disk write verification failed.");
            var roundtrip = new JObject
            {
                ["baselineByteEquality"] = true, ["updatedByteEquality"] = true,
                ["requestedValuesReadBack"] = true, ["otherSerializedFieldsByteEquality"] = true,
                ["blankParametersPreserved"] = true, ["writtenFileByteEquality"] = true
            };
            result["channelConfig"] = new JObject { ["status"] = "Passed", ["serializer"] = "Actual Unity Bizza.Sdk.ChannelConfigBinarySerializer", ["fields"] = fields, ["roundtrip"] = roundtrip };
            result["roundtrip"] = roundtrip.DeepClone();
            result["preserved"] = new JArray
            {
                new JObject { ["originalParameter"] = "默认国家", ["standardParameter"] = "默认国家", ["target"] = "real_CustomConfig.DefaultCountry", ["value"] = (int)before.real_CustomConfig.DefaultCountry, ["reason"] = "Blank current request value; preserved." },
                new JObject { ["originalParameter"] = "MAX 横幅广告 ID", ["standardParameter"] = "MAX 横幅广告 ID", ["target"] = "sourceAds[AdsSource=Max].bannerAdId", ["value"] = adBefore.bannerAdId, ["reason"] = "Blank current request value; preserved." },
                new JObject { ["originalParameter"] = "MAX 开屏广告 ID", ["standardParameter"] = "MAX 开屏广告 ID", ["target"] = "sourceAds[AdsSource=Max].openAdId", ["value"] = adBefore.openAdId, ["reason"] = "Blank current request value; preserved." }
            };
            var allFields = (JArray)result["fields"];
            for (int i = allFields.Count - 1; i >= 0; i--)
                if (((string)allFields[i]["target"]).StartsWith(relative + " :: ", StringComparison.Ordinal)) allFields.RemoveAt(i);
            foreach (var item in fields) allFields.Add(item.DeepClone());
            var changedFiles = (JArray)result["changedReleaseFiles"];
            if (!changedFiles.Values<string>().Contains(relative)) changedFiles.Add(relative);

            // Read the staged file explicitly instead of trusting an already-cached
            // singleton. Synchronize only current-request properties in the singleton;
            // do not Save it, which could overwrite unrelated cached/disk settings.
            var loadedSettings = InternalEditorUtility.LoadSerializedFileAndForget("ProjectSettings/Obfuz.asset");
            Require(loadedSettings.Length == 1 && loadedSettings[0] is ObfuzSettings, "Could not reload Obfuz settings from disk.");
            var settings = (ObfuzSettings)loadedSettings[0];
            Require(settings.assemblySettings.assembliesToObfuscate.SequenceEqual(new[] { "Assembly-CSharp" }), "Obfuz assembly mismatch.");
            Require(settings.secretSettings.defaultStaticSecretKey == Package && settings.secretSettings.defaultDynamicSecretKey == Package, "Obfuz secret settings mismatch.");
            Require(settings.encryptionVMSettings.codeGenerationSecretKey == "orchardtrio", "Obfuz code generation key mismatch.");
            Require(settings.symbolObfusSettings.obfuscatedNamePrefix.Length == 3 && settings.symbolObfusSettings.obfuscatedNamePrefix.All(c => c >= 'a' && c <= 'z'), "Obfuz prefix mismatch.");
            var cachedSettings = ObfuzSettings.Instance;
            cachedSettings.assemblySettings.assembliesToObfuscate = new[] { "Assembly-CSharp" };
            cachedSettings.secretSettings.defaultStaticSecretKey = Package;
            cachedSettings.secretSettings.defaultDynamicSecretKey = Package;
            cachedSettings.encryptionVMSettings.codeGenerationSecretKey = "orchardtrio";
            cachedSettings.symbolObfusSettings.obfuscatedNamePrefix = settings.symbolObfusSettings.obfuscatedNamePrefix;
            string vmPath = settings.encryptionVMSettings.codeOutputPath;
            string staticPath = settings.secretSettings.staticSecretKeyOutputPath;
            string dynamicPath = settings.secretSettings.dynamicSecretKeyOutputPath;
            foreach (string path in new[] { vmPath, staticPath, dynamicPath })
            {
                string resolved = Path.GetFullPath(Path.Combine(Project, path)).Replace('\\', '/');
                Require(resolved.StartsWith(Project + "/Assets/", StringComparison.OrdinalIgnoreCase), "Obfuz output must remain under this project's Assets.");
                Require(File.Exists(Path.Combine(Work, "baseline", path)), "Missing current-request Obfuz output baseline.");
            }
            var generator = new VirtualMachineCodeGenerator(settings.encryptionVMSettings.codeGenerationSecretKey, settings.encryptionVMSettings.encryptionOpCodeCount);
            generator.Generate(vmPath);
            byte[] staticKey = KeyGenerator.GenerateKey(settings.secretSettings.defaultStaticSecretKey, VirtualMachine.SecretKeyLength);
            byte[] dynamicKey = KeyGenerator.GenerateKey(settings.secretSettings.defaultDynamicSecretKey, VirtualMachine.SecretKeyLength);
            File.WriteAllBytes(staticPath, staticKey);
            File.WriteAllBytes(dynamicPath, dynamicKey);
            Require(generator.ValidateMatch(vmPath), "Generated VM does not match settings.");
            Require(staticKey.SequenceEqual(File.ReadAllBytes(staticPath)) && dynamicKey.SequenceEqual(File.ReadAllBytes(dynamicPath)), "Generated secret file mismatch.");
            var generated = new JArray();
            foreach (string path in new[] { vmPath, staticPath, dynamicPath })
            {
                generated.Add(new JObject { ["path"] = path, ["sha256"] = HashFile(path) });
                if (!changedFiles.Values<string>().Contains(path)) changedFiles.Add(path);
            }
            result["obfuz"] = new JObject
            {
                ["status"] = "Passed", ["generator"] = "Project VirtualMachineCodeGenerator.Generate and KeyGenerator.GenerateKey (same implementation as ObfuzMenu)",
                ["prefix"] = settings.symbolObfusSettings.obfuscatedNamePrefix,
                ["vmMatchesSettings"] = true, ["staticKeyMatchesSettings"] = true, ["dynamicKeyMatchesSettings"] = true,
                ["keyLengthBytes"] = staticKey.Length, ["outputs"] = generated,
                ["generatedVmCompilation"] = "Pending final project compilation after this helper is removed"
            };
            result["unityVersion"] = Application.unityVersion;
            result["status"] = "Passed";
            result["notes"] = new JArray
            {
                "Country NONE maps to declared AccountModule.E_CountryType.None (0); game type 真假混 maps to incomeRate=1.",
                "Empty default country/banner/open ad inputs are preserved.",
                "All ChannelConfig reads/writes used the current project's actual serializer in Unity, without copied serializer code or binary text replacement.",
                "Updated only the two requested PlayerSettings properties in the open editor; reloaded Obfuz settings from disk and synchronized only requested cached properties without saving other cached settings.",
                "No runtime method or gameplay logic changed; this temporary helper must be removed before the final compile."
            };
            File.WriteAllText(resultPath, result.ToString(Formatting.Indented));
            AssetDatabase.Refresh();
            Debug.Log("Orchard Trio release configuration, ChannelConfig round-trip and Obfuz generation verified. Report: " + resultPath);
        }
        catch (Exception exception)
        {
            result["status"] = "Failed";
            result["failure"] = exception.Message;
            File.WriteAllText(resultPath, result.ToString(Formatting.Indented));
            throw;
        }
    }
}
