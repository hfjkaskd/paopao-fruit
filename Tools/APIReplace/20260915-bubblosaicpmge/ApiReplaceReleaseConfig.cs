// Temporary Editor-only execution helper. Copy to Assets/Editor, run
// -executeMethod ApiReplaceReleaseConfig.GenerateAndVerify, then remove the copy and meta.
// The retained source in Tools is the audit/reproduction artifact.
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
using UnityEngine;

public static class ApiReplaceReleaseConfig
{
    private static void Require(bool valid, string message)
    {
        if (!valid) throw new InvalidDataException(message);
    }

    private static string Sha256(byte[] bytes)
    {
        using (var sha = SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
    }

    public static void GenerateAndVerify()
    {
        string work = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Tools/APIReplace/20260915-bubblosaicpmge"));
        string output = Path.Combine(work, "obfuz-generation-results.json");
        try
        {
            Require(PlayerSettings.productName == "Bubblosaic: Picture Merge", "Product name does not match current request.");
            Require(PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android) == "com.webpack.picturemerge", "Android package does not match current request.");
            var settings = ObfuzSettings.Instance;
            Require(settings.assemblySettings.assembliesToObfuscate.Contains("Assembly-CSharp"), "Assembly-CSharp is not configured for Obfuz.");
            Require(settings.secretSettings.defaultStaticSecretKey == "com.webpack.picturemerge"
                && settings.secretSettings.defaultDynamicSecretKey == "com.webpack.picturemerge", "Obfuz secret settings do not match current Android package.");
            Require(settings.encryptionVMSettings.codeGenerationSecretKey == "picturemerge", "Obfuz VM settings do not match package suffix.");
            Require(settings.symbolObfusSettings.obfuscatedNamePrefix.Length == 3
                && settings.symbolObfusSettings.obfuscatedNamePrefix.All(c => c >= 'a' && c <= 'z'), "Obfuz name prefix must contain three lowercase letters.");

            var vm = new VirtualMachineCodeGenerator(settings.encryptionVMSettings.codeGenerationSecretKey,
                settings.encryptionVMSettings.encryptionOpCodeCount);
            string vmPath = settings.encryptionVMSettings.codeOutputPath;
            string staticPath = settings.secretSettings.staticSecretKeyOutputPath;
            string dynamicPath = settings.secretSettings.dynamicSecretKeyOutputPath;
            // These are the same generator and key derivation calls used by ObfuzMenu.
            vm.Generate(vmPath);
            byte[] staticKey = KeyGenerator.GenerateKey(settings.secretSettings.defaultStaticSecretKey, VirtualMachine.SecretKeyLength);
            byte[] dynamicKey = KeyGenerator.GenerateKey(settings.secretSettings.defaultDynamicSecretKey, VirtualMachine.SecretKeyLength);
            File.WriteAllBytes(staticPath, staticKey);
            File.WriteAllBytes(dynamicPath, dynamicKey);
            Require(vm.ValidateMatch(vmPath), "Generated VM source does not match current settings.");
            Require(File.ReadAllBytes(staticPath).SequenceEqual(staticKey), "Static key file mismatch.");
            Require(File.ReadAllBytes(dynamicPath).SequenceEqual(dynamicKey), "Dynamic key file mismatch.");

            byte[] currentBytes = File.ReadAllBytes(Path.Combine(Application.streamingAssetsPath, "ChannelConfig.bytes"));
            byte[] baselineBytes = File.ReadAllBytes(Path.Combine(work, "baseline/ChannelConfig.bytes"));
            var config = ChannelConfigBinarySerializer.Deserialize(currentBytes);
            var before = ChannelConfigBinarySerializer.Deserialize(baselineBytes);
            Require(currentBytes.SequenceEqual(ChannelConfigBinarySerializer.Serialize(config)), "Actual Unity ChannelConfig round-trip mismatch.");
            Require(config.AppId == "bubblosaicpmge" && config.httpConfig.domain == "https://app.fruittile.xin"
                && config.adjustKey == "2zov4dn3gsu8" && config.incomeRate == 1.0
                && config.real_CustomConfig.Country == AccountModule.E_CountryType.None, "Actual Unity release configuration mismatch.");
            var max = config.sourceAds.Single(ad => ad != null && ad.AdsSource == E_AdsSource.Max);
            var originalMax = before.sourceAds.Single(ad => ad != null && ad.AdsSource == E_AdsSource.Max);
            Require(max.rewardAdId == "ddef1150f706c39f" && max.interAdId == "3f64a45c4db0eb7a", "Actual Unity MAX ID mismatch.");
            Require(config.real_CustomConfig.DefaultCountry == before.real_CustomConfig.DefaultCountry
                && max.bannerAdId == originalMax.bannerAdId && max.openAdId == originalMax.openAdId, "A blank user parameter was overwritten.");
            config.AppId = before.AppId;
            config.httpConfig.domain = before.httpConfig.domain;
            config.httpConfig.aes_key = before.httpConfig.aes_key;
            config.adjustKey = before.adjustKey;
            config.incomeRate = before.incomeRate;
            config.real_CustomConfig.Country = before.real_CustomConfig.Country;
            max.rewardAdId = originalMax.rewardAdId;
            max.interAdId = originalMax.interAdId;
            Require(baselineBytes.SequenceEqual(ChannelConfigBinarySerializer.Serialize(config)), "Actual Unity unrelated serialized field preservation mismatch.");

            var result = new JObject
            {
                ["status"] = "Passed",
                ["unityVersion"] = Application.unityVersion,
                ["generator"] = "Project Obfuz VirtualMachineCodeGenerator.Generate + KeyGenerator.GenerateKey (same calls as ObfuzMenu)",
                ["vmMatchesSettings"] = true,
                ["staticKeyMatchesSettings"] = true,
                ["dynamicKeyMatchesSettings"] = true,
                ["keyLengthBytes"] = staticKey.Length,
                ["prefix"] = settings.symbolObfusSettings.obfuscatedNamePrefix,
                ["unityChannelConfigRoundtrip"] = true,
                ["unityOtherSerializedFieldsPreserved"] = true,
                ["unityBlankParametersPreserved"] = true,
                ["outputs"] = new JArray
                {
                    new JObject { ["path"] = vmPath, ["sha256"] = Sha256(File.ReadAllBytes(vmPath)) },
                    new JObject { ["path"] = staticPath, ["sha256"] = Sha256(File.ReadAllBytes(staticPath)) },
                    new JObject { ["path"] = dynamicPath, ["sha256"] = Sha256(File.ReadAllBytes(dynamicPath)) }
                },
                ["generatedVmCompilation"] = "Requires final project compile after generation"
            };
            File.WriteAllText(output, result.ToString(Formatting.Indented));
            string releasePath = Path.Combine(work, "release-config-results.json");
            var release = JObject.Parse(File.ReadAllText(releasePath));
            release["status"] = "Passed";
            release["obfuz"] = result;
            File.WriteAllText(releasePath, release.ToString(Formatting.Indented));
            AssetDatabase.Refresh();
            Debug.Log("APIReplace release configuration and Obfuz generation verification passed. Report: " + output);
        }
        catch (Exception exception)
        {
            File.WriteAllText(output, new JObject { ["status"] = "Failed", ["error"] = exception.Message }.ToString(Formatting.Indented));
            throw;
        }
    }
}
