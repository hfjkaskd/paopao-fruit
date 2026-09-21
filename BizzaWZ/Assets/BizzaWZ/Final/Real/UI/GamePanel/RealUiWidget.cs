#if BIZZA_REAL_WITHDRAW
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RealUiWidget : MonoBehaviour
{
    public BizzaButton taskBtn;
    public TMP_Text levelTxt;


    public ItemForCountry itemForCountry;


    void Awake()
    {
        taskBtn.onClick.AddListener(() =>
        {
            _ = UIModule.Instance.OpenPage(UIPageIds.UI_DailyTaskPage);
        });
    }

    void OnEnable()
    {
        BizzaEventSystem.On(EventDefine.Item.GameStart, RefreshLevelText);
        RefreshLevelText();
        itemForCountry.OnRefresh();
    }

    void OnDisable()
    {
        BizzaEventSystem.Off(EventDefine.Item.GameStart, RefreshLevelText);
    }

    public void RefreshLevelText()
    {
        if (levelTxt == null || SaveDataUtils.GameData == null) return;
        levelTxt.text = SaveDataUtils.GameData.playerSelectedLv.ToString();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
#endif
