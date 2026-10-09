#if BIZZA_REAL_WITHDRAW
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class UIPageIds
{
    public static readonly PageId SlotFAQPanel = "SlotFAQPanel";
}

public class SlotFAQPanel : UIPageBase
{
    public BizzaButton bizzaButton;
    [SerializeField] private Button closeButton;

    protected override void OnAwake()
    {
        bizzaButton.onClick.AddListener(CloseSelf);
        if (closeButton != null) closeButton.onClick.AddListener(CloseSelf);
    }
    protected override void OnClose()
    {
   
    }

    protected override void OnOpen()
    {
        
    }
}
#endif
