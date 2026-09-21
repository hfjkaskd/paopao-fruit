using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using Bizza.Sdk;

public static class ChannelConfigUpdate
{
    static string Json(string value) { if(value == null) return "null"; return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t") + "\""; }
    static string Hash(byte[] bytes) { using(var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant(); }
    static string Snapshot(ChannelConfig c)
    {
        var b = new StringBuilder();
        b.Append("{\n  \"AppId\": ").Append(Json(c.AppId));
        b.Append(",\n  \"domain\": ").Append(Json(c.httpConfig.domain));
        b.Append(",\n  \"aes_key\": \"[redacted]\",\n  \"aes_key_length\": ").Append(c.httpConfig.aes_key == null ? 0 : c.httpConfig.aes_key.Length);
        b.Append(",\n  \"incomeRate\": ").Append(c.incomeRate.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
        b.Append(",\n  \"defaultCountryValue\": ").Append((int)c.real_CustomConfig.DefaultCountry);
        b.Append(",\n  \"countryValue\": ").Append((int)c.real_CustomConfig.Country);
        b.Append(",\n  \"adjustKey\": ").Append(Json(c.adjustKey));
        b.Append(",\n  \"sourceAds\": [");
        for(int i=0; i<c.sourceAds.Count; i++) { var a = c.sourceAds[i]; if(i>0) b.Append(','); if(a == null) { b.Append("null"); continue; } b.Append("\n    {\"AdsSourceValue\": ").Append((int)a.AdsSource).Append(", \"rewardAdId\": ").Append(Json(a.rewardAdId)).Append(", \"interAdId\": ").Append(Json(a.interAdId)).Append(", \"bannerAdId\": ").Append(Json(a.bannerAdId)).Append(", \"openAdId\": ").Append(Json(a.openAdId)).Append('}'); }
        return b.Append("\n  ]\n}\n").ToString();
    }
    static AdConfig GetMax(ChannelConfig c)
    {
        if(c.sourceAds == null) throw new Exception("sourceAds is null");
        var entries = c.sourceAds.Where(a => a != null && a.AdsSource == E_AdsSource.Max).ToArray();
        if(entries.Length != 1) throw new Exception("Expected exactly one MAX config, found " + entries.Length);
        return entries[0];
    }
    static void Assert(bool condition, string message) { if(!condition) throw new Exception(message); }
    public static int Main(string[] args)
    {
        try
        {
            if(args.Length != 4) throw new Exception("Arguments: backupBytes targetBytes reportDirectory serializerSource");
            var key = Environment.GetEnvironmentVariable("APIR_CHANNEL_AES_KEY");
            if(string.IsNullOrEmpty(key)) throw new Exception("APIR_CHANNEL_AES_KEY is required");
            byte[] original = File.ReadAllBytes(args[0]);
            Assert(original.SequenceEqual(File.ReadAllBytes(args[1])), "Target differs from approved before snapshot; aborting write");
            var before = ChannelConfigBinarySerializer.Deserialize(original);
            var originalMax = GetMax(before);
            byte[] normalizedBefore = ChannelConfigBinarySerializer.Serialize(before);
            var updated = ChannelConfigBinarySerializer.Deserialize(original);
            Assert(updated.httpConfig != null, "httpConfig is null");
            updated.AppId = "bubblosaicpmge";
            updated.httpConfig.domain = "https://app.fruittile.xin";
            updated.httpConfig.aes_key = key;
            updated.incomeRate = 1.0;
            updated.real_CustomConfig.Country = AccountModule.E_CountryType.None;
            updated.adjustKey = "2zov4dn3gsu8";
            var max = GetMax(updated);
            max.rewardAdId = "ddef1150f706c39f";
            max.interAdId = "3f64a45c4db0eb7a";
            byte[] serialized = ChannelConfigBinarySerializer.Serialize(updated);
            var roundtrip = ChannelConfigBinarySerializer.Deserialize(serialized);
            var finalMax = GetMax(roundtrip);
            Assert(roundtrip.AppId == updated.AppId && roundtrip.httpConfig.domain == updated.httpConfig.domain && roundtrip.httpConfig.aes_key == key && roundtrip.incomeRate == 1.0 && roundtrip.real_CustomConfig.Country == AccountModule.E_CountryType.None && roundtrip.adjustKey == updated.adjustKey, "Target configuration roundtrip failed");
            Assert(finalMax.rewardAdId == max.rewardAdId && finalMax.interAdId == max.interAdId, "MAX ad IDs roundtrip failed");
            Assert(finalMax.bannerAdId == originalMax.bannerAdId && finalMax.openAdId == originalMax.openAdId && roundtrip.real_CustomConfig.DefaultCountry == before.real_CustomConfig.DefaultCountry, "Blank-input field preservation failed");
            Assert(serialized.SequenceEqual(ChannelConfigBinarySerializer.Serialize(roundtrip)), "Serialized bytes roundtrip is not deterministic");
            var preserved = ChannelConfigBinarySerializer.Deserialize(serialized);
            preserved.AppId = before.AppId;
            preserved.httpConfig.domain = before.httpConfig.domain;
            preserved.httpConfig.aes_key = before.httpConfig.aes_key;
            preserved.incomeRate = before.incomeRate;
            preserved.real_CustomConfig.Country = before.real_CustomConfig.Country;
            preserved.adjustKey = before.adjustKey;
            var preservedMax = GetMax(preserved);
            preservedMax.rewardAdId = originalMax.rewardAdId;
            preservedMax.interAdId = originalMax.interAdId;
            Assert(normalizedBefore.SequenceEqual(ChannelConfigBinarySerializer.Serialize(preserved)), "Unrequested serialized fields changed");
            File.WriteAllText(Path.Combine(args[2], "channel-before.json"), Snapshot(before), new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(args[2], "channel-after.json"), Snapshot(roundtrip), new UTF8Encoding(false));
            File.WriteAllBytes(args[1], serialized);
            Assert(File.ReadAllBytes(args[1]).SequenceEqual(serialized), "Disk write verification failed");
            string evidence = "{\n  \"status\": \"Passed\",\n  \"serializer\": \"Compiled exact current project source; direct calls to Deserialize and Serialize\",\n  \"serializerSource\": " + Json(args[3]) + ",\n  \"serializerSourceSha256\": " + Json(Hash(File.ReadAllBytes(args[3]))) + ",\n  \"beforeSha256\": " + Json(Hash(original)) + ",\n  \"afterSha256\": " + Json(Hash(serialized)) + ",\n  \"beforeLength\": " + original.Length + ",\n  \"afterLength\": " + serialized.Length + ",\n  \"beforeAlreadyCanonical\": " + (original.SequenceEqual(normalizedBefore) ? "true" : "false") + ",\n  \"serializedRoundtrip\": true,\n  \"targetValuesVerified\": true,\n  \"blankInputsPreserved\": [\"defaultCountry\", \"bannerAdId\", \"openAdId\"],\n  \"allUntargetedSerializedFieldsPreserved\": true,\n  \"diskReadbackVerified\": true,\n  \"aesKeyChanged\": " + (before.httpConfig.aes_key != key ? "true" : "false") + "\n}\n";
            File.WriteAllText(Path.Combine(args[2], "channel-validation.json"), evidence, new UTF8Encoding(false));
            Console.WriteLine("ChannelConfig update and full serialization preservation checks passed. Reports: " + args[2]);
            return 0;
        }
        catch(Exception e) { Console.Error.WriteLine(e.GetType().Name + ": " + e.Message); return 1; }
    }
}