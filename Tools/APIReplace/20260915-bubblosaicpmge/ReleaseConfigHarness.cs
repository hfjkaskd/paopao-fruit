using System;
using System.IO;
using System.Linq;
using Bizza.Sdk;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static class ReleaseConfigHarness
{
    private static readonly JArray Fields = new JArray();

    private static void Field(string original, string normalized, string target, object before, object after, bool sensitive = false)
    {
        Fields.Add(new JObject
        {
            ["originalParameter"] = original,
            ["standardParameter"] = normalized,
            ["target"] = "Assets/StreamingAssets/ChannelConfig.bytes :: " + target,
            ["before"] = sensitive ? Mask(Convert.ToString(before)) : JToken.FromObject(before ?? ""),
            ["after"] = sensitive ? Mask(Convert.ToString(after)) : JToken.FromObject(after ?? ""),
            ["status"] = Equals(before, after) ? "AlreadyMatched" : "Updated"
        });
    }

    private static string Mask(string value)
    {
        if (string.IsNullOrEmpty(value)) return "(empty)";
        return value.Length < 8 ? "***" : value.Substring(0, 3) + "***" + value.Substring(value.Length - 3);
    }

    private static void Require(bool valid, string message)
    {
        if (!valid) throw new InvalidDataException(message);
    }

    public static int Main(string[] args)
    {
        var baselinePath = args[0];
        var outputPath = args[1];
        var resultPath = args[2];
        string aesKey = Environment.GetEnvironmentVariable("APIREPLACE_RELEASE_AES_KEY");
        Require(!string.IsNullOrEmpty(aesKey), "Missing the current request AES key.");

        byte[] baselineBytes = File.ReadAllBytes(baselinePath);
        var before = ChannelConfigBinarySerializer.Deserialize(baselineBytes);
        Require(baselineBytes.SequenceEqual(ChannelConfigBinarySerializer.Serialize(before)), "Baseline did not round-trip byte for byte.");
        Require(before.httpConfig != null, "HTTP configuration is missing.");
        Require(before.sourceAds != null && before.sourceAds.Count(a => a != null && a.AdsSource == E_AdsSource.Max) == 1, "MAX source must resolve uniquely.");

        var config = ChannelConfigBinarySerializer.Deserialize(baselineBytes);
        var beforeMax = before.sourceAds.Single(a => a != null && a.AdsSource == E_AdsSource.Max);
        var max = config.sourceAds.Single(a => a != null && a.AdsSource == E_AdsSource.Max);

        Field("应用ID", "应用ID", "AppId", config.AppId, "bubblosaicpmge");
        config.AppId = "bubblosaicpmge";
        Field("接口域名地址", "接口域名地址", "httpConfig.domain", config.httpConfig.domain, "https://app.fruittile.xin");
        config.httpConfig.domain = "https://app.fruittile.xin";
        Field("密钥", "密钥", "httpConfig.aes_key", config.httpConfig.aes_key, aesKey, true);
        config.httpConfig.aes_key = aesKey;
        Field("游戏类型", "游戏类型", "incomeRate", config.incomeRate, 1.0);
        config.incomeRate = 1.0;
        Field("国家", "国家", "real_CustomConfig.Country", (int)config.real_CustomConfig.Country, (int)AccountModule.E_CountryType.None);
        config.real_CustomConfig.Country = AccountModule.E_CountryType.None;
        Field("Adjust 应用识别码", "Adjust 应用识别码", "adjustKey", config.adjustKey, "2zov4dn3gsu8");
        config.adjustKey = "2zov4dn3gsu8";
        Field("MAX 激励广告 ID", "MAX 激励广告 ID", "sourceAds[AdsSource=Max].rewardAdId", max.rewardAdId, "ddef1150f706c39f");
        max.rewardAdId = "ddef1150f706c39f";
        Field("MAX 插屏广告 ID", "MAX 插屏广告 ID", "sourceAds[AdsSource=Max].interAdId", max.interAdId, "3f64a45c4db0eb7a");
        max.interAdId = "3f64a45c4db0eb7a";

        byte[] updated = ChannelConfigBinarySerializer.Serialize(config);
        var readback = ChannelConfigBinarySerializer.Deserialize(updated);
        Require(updated.SequenceEqual(ChannelConfigBinarySerializer.Serialize(readback)), "Updated configuration did not round-trip byte for byte.");
        Require(readback.AppId == config.AppId && readback.httpConfig.domain == config.httpConfig.domain && readback.httpConfig.aes_key == aesKey
            && readback.incomeRate == 1.0 && readback.real_CustomConfig.Country == AccountModule.E_CountryType.None
            && readback.adjustKey == config.adjustKey, "Updated configuration fields did not match.");
        var readbackMax = readback.sourceAds.Single(a => a != null && a.AdsSource == E_AdsSource.Max);
        Require(readbackMax.rewardAdId == max.rewardAdId && readbackMax.interAdId == max.interAdId, "MAX IDs did not match.");

        // Revert only the authorized field changes in memory; complete binary equality proves
        // all remaining serialized configuration (including user modifications) was preserved.
        readback.AppId = before.AppId;
        readback.httpConfig.domain = before.httpConfig.domain;
        readback.httpConfig.aes_key = before.httpConfig.aes_key;
        readback.incomeRate = before.incomeRate;
        readback.real_CustomConfig.Country = before.real_CustomConfig.Country;
        readback.adjustKey = before.adjustKey;
        readbackMax.rewardAdId = beforeMax.rewardAdId;
        readbackMax.interAdId = beforeMax.interAdId;
        Require(baselineBytes.SequenceEqual(ChannelConfigBinarySerializer.Serialize(readback)), "An unrelated configuration field changed.");

        File.WriteAllBytes(outputPath, updated);
        Require(updated.SequenceEqual(File.ReadAllBytes(outputPath)), "Written bytes did not match.");
        var report = new JObject
        {
            ["status"] = "Passed",
            ["method"] = "Compiled unchanged current project ChannelConfig.cs, ChannelConfigBinarySerializer.cs, XXTEA.cs using Unity Roslyn; executed on Unity Mono with isolated build-only dependency substitutes.",
            ["fields"] = Fields,
            ["preserved"] = new JArray
            {
                new JObject { ["originalParameter"] = "默认国家", ["standardParameter"] = "默认国家", ["target"] = "real_CustomConfig.DefaultCountry", ["value"] = (int)before.real_CustomConfig.DefaultCountry, ["reason"] = "Current user input is blank; original enum value retained." },
                new JObject { ["originalParameter"] = "MAX 横幅广告 ID", ["standardParameter"] = "MAX 横幅广告 ID", ["target"] = "sourceAds[AdsSource=Max].bannerAdId", ["value"] = beforeMax.bannerAdId, ["reason"] = "Current user input is blank; original value retained." },
                new JObject { ["originalParameter"] = "MAX 开屏广告 ID", ["standardParameter"] = "MAX 开屏广告 ID", ["target"] = "sourceAds[AdsSource=Max].openAdId", ["value"] = beforeMax.openAdId, ["reason"] = "Current user input is blank; original value retained." }
            },
            ["roundtrip"] = new JObject { ["baselineByteEquality"] = true, ["updatedByteEquality"] = true, ["authorizedValuesReadBack"] = true, ["allOtherSerializedFieldsByteEquality"] = true, ["writtenFileByteEquality"] = true },
            ["notes"] = new JArray { "Input country NONE resolved to declared enum AccountModule.E_CountryType.None (0).", "Game type 真假混 maps to incomeRate=1.", "Existing serializer reflection remains unchanged; no new runtime code or reflection added." }
        };
        File.WriteAllText(resultPath, report.ToString(Formatting.Indented));
        Console.WriteLine("ChannelConfig updated with project serializer; all round-trip/preservation checks passed.");
        return 0;
    }
}
