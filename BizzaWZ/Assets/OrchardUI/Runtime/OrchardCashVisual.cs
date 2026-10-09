#if BIZZA_REAL_WITHDRAW
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Loads the cash page's artwork on demand; page data owns selection and eligibility.</summary>
[DisallowMultipleComponent]
public sealed class OrchardCashVisual : MonoBehaviour
{
    [Serializable] private struct Binding { public Image image; public string sprite; }
    [SerializeField] private string resourcePath;
    [SerializeField] private FakeWithdrawPanel page;
    [SerializeField] private Binding[] bindings = Array.Empty<Binding>();
    [SerializeField] private Image withdrawImage;
    [SerializeField] private Sprite enabledButton;
    [SerializeField] private TMP_Text withdrawLabel;
    [SerializeField] private Material enabledLabel, disabledLabel;
    [SerializeField] private GameObject unavailableHint;
    [SerializeField] private string englishRemainingFormat, portugueseRemainingFormat;
    private Sprite[] sprites;
    private Coroutine loading;
    private bool available;
    public bool IsReady { get; private set; }

    private void OnEnable()
    {
        if (!IsReady) loading = StartCoroutine(Load());
    }
    private void OnDisable()
    {
        if (loading != null) { StopCoroutine(loading); loading = null; }
    }
    private IEnumerator Load()
    {
        var request = Resources.LoadAsync<Texture2D>(resourcePath);
        yield return request;
        loading = null;
        if (request.asset == null) { Debug.LogError("Cash page artwork is missing: " + resourcePath, this); yield break; }
        ApplyArtwork(Resources.LoadAll<Sprite>(resourcePath));
        RefreshCards(page.items);
        page.UpdateProgress();
    }
    public void ApplyArtwork(Sprite[] loaded)
    {
        sprites = loaded;
        IsReady = true;
        foreach (var binding in bindings)
        {
            var sprite = Find(binding.sprite);
            if (binding.image == null || sprite == null) { IsReady = false; continue; }
            binding.image.sprite = sprite;
            binding.image.enabled = true;
        }
        if (!IsReady) Debug.LogError("Cash page artwork has an incomplete binding.", this);
        SetAvailable(available);
    }
    private Sprite Find(string name)
    {
        if (sprites != null) foreach (var sprite in sprites) if (sprite.name == name) return sprite;
        return null;
    }
    public void RefreshCards(IList<WithdrawAmountItem> items)
    {
        if (!IsReady) return;
        var normal = Find("Card"); var selected = Find("SelectedCard"); var check = Find("Check");
        for (int i = 0; i < items.Count; i++) items[i].SetArtwork(normal, selected, check);
    }
    public void SetAvailable(bool canProceed)
    {
        available = canProceed;
        page.withdrawBtn.interactable = canProceed;
        var sprite = canProceed ? enabledButton : Find("DisabledButton");
        if (sprite != null) { withdrawImage.sprite = sprite; withdrawImage.enabled = true; }
        withdrawImage.type = canProceed ? Image.Type.Sliced : Image.Type.Simple;
        withdrawLabel.fontSharedMaterial = canProceed ? enabledLabel : disabledLabel;
        withdrawLabel.color = Color.white;
        unavailableHint.SetActive(!canProceed);
    }
    public void ShowMoneyRemaining(float remaining)
    {
        bool portuguese = LanguageUtils.SelectedLanguage == "pt-BR";
        var culture = System.Globalization.CultureInfo.GetCultureInfo(AccountModule.CountryType == AccountModule.E_CountryType.BR ? "pt-BR" : "en-US");
        string amount = LanguageUtils.GetText("CurrencyToken") + remaining.ToString(AccountModule.CountryType == AccountModule.E_CountryType.ID ? "0" : "0.00", culture);
        page.hintTxt.text = string.Format(portuguese ? portugueseRemainingFormat : englishRemainingFormat, amount);
    }
}
#endif
