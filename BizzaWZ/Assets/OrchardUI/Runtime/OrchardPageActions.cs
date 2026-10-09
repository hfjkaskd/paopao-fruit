using UnityEngine;
using UnityEngine.UI;

/// <summary>Code-bound standard buttons for navigation added by the approved layouts.</summary>
public sealed class OrchardPageActions : MonoBehaviour
{
    [SerializeField] private UIPageBase page;
    [SerializeField] private Button backButton;
    [SerializeField] private Button editButton;
    [SerializeField] private Button helpButton;
    [SerializeField] private Button supportButton;
    private void Awake()
    {
        if (backButton != null) backButton.onClick.AddListener(Close);
        if (editButton != null) editButton.onClick.AddListener(Close);
        if (helpButton != null) helpButton.onClick.AddListener(Help);
        if (supportButton != null) supportButton.onClick.AddListener(Support);
    }
    private void Close() { if (page != null) page.CloseSelf(); }
    private void Help()
    {
#if BIZZA_REAL_WITHDRAW
        UIModule.Instance.OpenPage(UIPageIds.QFA);
#endif
    }
    private void Support()
    {
#if BIZZA_REAL_WITHDRAW
        UIModule.Instance.OpenPage(UIPageIds.ServicePanel);
#endif
    }
}
