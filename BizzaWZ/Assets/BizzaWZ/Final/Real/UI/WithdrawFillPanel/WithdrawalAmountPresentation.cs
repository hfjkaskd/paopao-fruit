#if BIZZA_REAL_WITHDRAW
/// <summary>Shared display amount for the account and confirmation steps of the existing flow.</summary>
public static class WithdrawalAmountPresentation
{
    public static string Format(E_WithdrawType type)
    {
        string currency = LanguageUtils.GetText("CurrencyToken");
        if (type == E_WithdrawType.Real) return currency + AccountModule.Instance.Get_S_Ewl();
        if (type == E_WithdrawType.DailyMission) return currency + "0,2";
        if (type == E_WithdrawType.Fake)
        {
            float amount = AccountModule.CountryType == AccountModule.E_CountryType.ID ? 20f : .01f;
            return currency + WithdrawalUtil.GetCustomizedValueByCountryType(amount);
        }
        return string.Empty;
    }
}
#endif
