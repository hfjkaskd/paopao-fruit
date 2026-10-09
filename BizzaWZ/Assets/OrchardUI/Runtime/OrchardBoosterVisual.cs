using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Loads this popup's authored art and refreshes the selected prop's presentation.</summary>
[DisallowMultipleComponent]
public sealed class OrchardBoosterVisual : MonoBehaviour
{
    [Serializable] private struct PropCopy
    {
        public E_ItemType itemType;
        public string englishName, portugueseName, englishDescription, portugueseDescription;
        public string iconSprite;
    }
    [SerializeField] private string decorationResource;
    [SerializeField] private string controlsResource;
    [SerializeField] private Image decoration, closeImage, claimImage, propIcon;
    [SerializeField] private TMP_Text propName, description;
    [SerializeField] private TMP_Text usage;
    [SerializeField] private string englishUsageFormat, portugueseUsageFormat;
    [SerializeField] private PropCopy[] props = Array.Empty<PropCopy>();
    private E_ItemType selected;
    private Sprite fallbackIcon;
    private int usedCount, maximumUses;
    private Sprite[] controls;
    private Coroutine loading;
    public bool IsReady { get; private set; }

    private void OnEnable()
    {
        BizzaEventSystem.Set(EventDefine.Frame.LanguageChange, RefreshCopy, true);
        if (!IsReady) loading = StartCoroutine(Load());
    }
    private void OnDisable()
    {
        BizzaEventSystem.Set(EventDefine.Frame.LanguageChange, RefreshCopy, false);
        if (loading != null) { StopCoroutine(loading); loading = null; }
    }
    public void Show(E_ItemType itemType, Sprite configuredIcon, int used, int maximum)
    {
        selected = itemType;
        fallbackIcon = configuredIcon;
        usedCount = used;
        maximumUses = maximum;
        RefreshCopy();
        RefreshIcon();
    }
    private IEnumerator Load()
    {
        var plateRequest = Resources.LoadAsync<Texture2D>(decorationResource);
        var controlsRequest = Resources.LoadAsync<Texture2D>(controlsResource);
        yield return plateRequest;
        yield return controlsRequest;
        loading = null;
        if (plateRequest.asset == null || controlsRequest.asset == null)
        { Debug.LogError("Booster popup artwork is missing: " + decorationResource + " / " + controlsResource, this); yield break; }
        var plates = Resources.LoadAll<Sprite>(decorationResource);
        controls = Resources.LoadAll<Sprite>(controlsResource);
        IsReady = Bind(decoration, Find(plates, "Panel")) & Bind(closeImage, Find(controls, "Close")) & Bind(claimImage, Find(controls, "Claim"));
        if (!IsReady) Debug.LogError("Booster popup has incomplete sprite bindings.", this);
        RefreshIcon();
    }
    private static bool Bind(Image image, Sprite sprite)
    {
        if (image == null || sprite == null) return false;
        image.sprite = sprite;
        image.enabled = true;
        return true;
    }
    private static Sprite Find(Sprite[] sprites, string name)
    {
        if (sprites != null) foreach (var sprite in sprites) if (sprite.name == name) return sprite;
        return null;
    }
    private void RefreshIcon()
    {
        Sprite sprite = fallbackIcon;
        foreach (var copy in props)
            if (copy.itemType == selected && !string.IsNullOrEmpty(copy.iconSprite))
            { sprite = Find(controls, copy.iconSprite) ?? fallbackIcon; break; }
        Bind(propIcon, sprite);
    }
    private void RefreshCopy()
    {
        bool portuguese = LanguageUtils.SelectedLanguage == "pt-BR";
        if (usage != null) usage.text = string.Format(portuguese ? portugueseUsageFormat : englishUsageFormat, usedCount, maximumUses);
        foreach (var copy in props)
        {
            if (copy.itemType != selected) continue;
            propName.text = portuguese ? copy.portugueseName : copy.englishName;
            description.text = portuguese ? copy.portugueseDescription : copy.englishDescription;
            return;
        }
        if (description != null) description.text = string.Empty;
    }
}
