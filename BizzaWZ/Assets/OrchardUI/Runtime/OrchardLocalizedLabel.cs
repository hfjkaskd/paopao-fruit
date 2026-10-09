using TMPro;
using UnityEngine;

/// <summary>Prefab-authored copy with optional Indonesian text and language-table fallback.</summary>
[RequireComponent(typeof(TMP_Text))]
public sealed class OrchardLocalizedLabel : MonoBehaviour
{
    [SerializeField] private TMP_Text target;
    [SerializeField] private string english;
    [SerializeField] private string portuguese;
    [SerializeField] private string indonesian;
    [SerializeField] private string existingKey;
    private void OnEnable()
    {
        Refresh();
        BizzaEventSystem.Set(EventDefine.Frame.LanguageChange, Refresh, true);
    }
    private void OnDisable() { BizzaEventSystem.Set(EventDefine.Frame.LanguageChange, Refresh, false); }
    public void Refresh()
    {
        if (target == null) target = GetComponent<TMP_Text>();
        string language = LanguageUtils.SelectedLanguage;
        if (language == "pt-BR") target.text = portuguese;
        else if (language == "id-ID" && !string.IsNullOrEmpty(indonesian)) target.text = indonesian;
        else if (language == "en-US" || string.IsNullOrEmpty(existingKey)) target.text = english;
        else target.text = LanguageUtils.GetText(existingKey, english);
    }
}
