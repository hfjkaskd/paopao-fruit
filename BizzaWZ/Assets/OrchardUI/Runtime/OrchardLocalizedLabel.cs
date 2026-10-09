using TMPro;
using UnityEngine;

/// <summary>Prefab-authored copy for the approved English and Brazilian Portuguese layouts.</summary>
[RequireComponent(typeof(TMP_Text))]
public sealed class OrchardLocalizedLabel : MonoBehaviour
{
    [SerializeField] private TMP_Text target;
    [SerializeField] private string english;
    [SerializeField] private string portuguese;
    [SerializeField] private string existingKey;
    private void OnEnable()
    {
        Refresh();
        BizzaEventSystem.Set(EventDefine.Frame.LanguageChange, Refresh, true);
    }
    private void OnDisable() { BizzaEventSystem.Set(EventDefine.Frame.LanguageChange, Refresh, false); }
    private void Refresh()
    {
        if (target == null) target = GetComponent<TMP_Text>();
        string language = LanguageUtils.SelectedLanguage;
        if (language == "pt-BR") target.text = portuguese;
        else if (language == "en-US" || string.IsNullOrEmpty(existingKey)) target.text = english;
        else target.text = LanguageUtils.GetText(existingKey, english);
    }
}
