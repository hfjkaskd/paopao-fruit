#if BIZZA_REAL_WITHDRAW
using System.Collections.Generic;
using Bizza.Sdk;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Regional artwork shared by balances, rewards, reels and collection effects.</summary>
public static class WzCurrencySprites
{
    // Small shared sprites are loaded once per role/country, not once per icon or particle.
    private static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();

    public static string CountryKey(AccountModule.E_CountryType country)
    {
        switch (country)
        {
            case AccountModule.E_CountryType.US: return "US";
            case AccountModule.E_CountryType.BR: return "BR";
            case AccountModule.E_CountryType.ID: return "ID";
            default: return string.Empty;
        }
    }

    public static string InlineSpriteIndex(AccountModule.E_CountryType country, bool cash)
    {
        // Indices in the existing WzMoneyIcon_Common TMP atlas: coin / banknote pairs.
        switch (country)
        {
            case AccountModule.E_CountryType.ID: return cash ? "1" : "0";
            case AccountModule.E_CountryType.BR: return cash ? "3" : "2";
            case AccountModule.E_CountryType.US: return cash ? "5" : "4";
            default: return string.Empty;
        }
    }

    public static string RoleKey(E_WzIconType role)
    {
        switch (role)
        {
            case E_WzIconType.MoneyIcon: return "PieceMoney";
            case E_WzIconType.PieceMoney: return "PieceMoney";
            case E_WzIconType.StackMoney: return "StackMoney";
            case E_WzIconType.PileMoney: return "PileMoney";
            case E_WzIconType.PileWealth: return "PileWealth";
            case E_WzIconType.PileGold: return "PileGold";
            case E_WzIconType.HundredMoney: return "HundredMoney";
            case E_WzIconType.AbundanceWealth: return "AbundanceWealth";
            case E_WzIconType.MoneyEnhancement: return "MoneyEnhancement";
            case E_WzIconType.GoldCoin: return "GoldCoin";
            default: return string.Empty; // Bubble frames are decoration, not a currency resource.
        }
    }

    public static string Address(string role, AccountModule.E_CountryType country, bool singleCurrency)
    {
        string suffix = CountryKey(country);
        if (string.IsNullOrEmpty(role) || string.IsNullOrEmpty(suffix)) return string.Empty;
        if (singleCurrency)
        {
            switch (role)
            {
                case "GoldCoin": case "MoneyEnhancement": role = "StackMoney"; break;
                case "PileGold": case "PileWealth": role = "HundredMoney"; break;
            }
        }
        return role + "_" + suffix;
    }

    public static Sprite Load(E_WzIconType role) { return Load(RoleKey(role)); }
    public static Sprite Load(string role)
    {
        if (ChannelConfig.Instance == null) return null;
        string address = Address(role, AccountModule.CountryType, ChannelConfig.Instance.real_CustomConfig.singleCurrencyMode);
        if (string.IsNullOrEmpty(address)) return null;
        if (Sprites.TryGetValue(address, out var sprite) && sprite != null) return sprite;
        sprite = AssetUtils.LoadAssetSync<Sprite>(address);
        if (sprite != null) Sprites[address] = sprite;
        return sprite;
    }

    public static void Apply(Image image, string role, bool nativeSize = false)
    {
        if (image == null) return;
        var sprite = Load(role);
        if (sprite == null) return;
        image.overrideSprite = null;
        image.sprite = sprite;
        if (nativeSize) image.SetNativeSize();
        // Visibility belongs to the page. Loading an icon must not reveal hidden rewards.
    }
}
#endif
