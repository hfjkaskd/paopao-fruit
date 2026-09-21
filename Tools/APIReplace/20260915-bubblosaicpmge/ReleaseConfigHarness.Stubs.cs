// Offline build-only substitutes for unrelated Unity/game dependencies.
// ChannelConfig, its binary serializer, and XXTEA are compiled unchanged from the project.
using System;
namespace Cysharp.Threading.Tasks { }
namespace UnityEngine
{
    public sealed class SpaceAttribute : Attribute { public SpaceAttribute(float value) { } }
    public sealed class MinAttribute : Attribute { public MinAttribute(float value) { } }
    public sealed class TooltipAttribute : Attribute { public TooltipAttribute(string value) { } }
    public sealed class SerializeField : Attribute { }
    public static class Debug { public static void Log(object value) { } }
}
namespace Obfuz { public sealed class ObfuzIgnoreAttribute : Attribute { } }
public static class LogLogger { public static void LogError(string value) { throw new Exception(value); } }
public static class AccountModule
{
    // Matches AccountModule.cs with BIZZA_HTTP_AD enabled; NONE input maps to None=0.
    public enum E_CountryType { None = 0, US = 1, BR = 2, ID = 3 }
}
namespace Bizza.Sdk
{
    public enum E_AdsSource { All = 0, Max = 1, TopOn = 2 }
}
public struct GameAB_CustomData
{
    public int CloseGetRewardCount;
    public int ShowGetRewardCount;
    public int ShowDollarCount;
    public int InterAdCooldownMs;
    public int InterAdStartLevel;
    public int ReviveAdStartLevel;
}
