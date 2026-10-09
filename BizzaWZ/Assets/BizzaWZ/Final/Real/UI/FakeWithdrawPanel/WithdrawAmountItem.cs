#if BIZZA_REAL_WITHDRAW
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

 
public class WithdrawAmountItem : MonoBehaviour
{
    public TMP_Text amountTxt;

    public GameObject getObj;
    public GameObject getedObj;

    public GameObject selectObj;
    public BizzaButton btn;
    [SerializeField] private Image cardBackground;
    [SerializeField] private Color selectedAmountColor;
    [SerializeField] private Color normalAmountColor;
    [SerializeField] private Vector3 selectedAmountScale = Vector3.one;
    [SerializeField] private Vector3 normalAmountScale = Vector3.one;
    [SerializeField] private Vector2 selectedAmountSize;
    [SerializeField] private Vector2 normalAmountSize;
    private Sprite normalCard, selectedCard;
    private bool selected;
    private int _index;
    private bool _isStarterItem;
    private FakeWithdrawPanel _panel;

    public void Init(FakeWithdrawPanel panel, int index, string amount, bool canGet, bool isStarterItem)
    {
        _panel = panel;
        _index = index;
        amountTxt.text = amount;
        Refresh(canGet, isStarterItem);
        btn.onClick.RemoveListener(OnClick);
        btn.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        if (_isStarterItem && _panel.isReward) return;
        _panel.SetSelectIndex(_index);
        _panel.OnRefresh();
    }
    
    public void Refresh(bool canGet, bool isStarterItem)
    {
        _isStarterItem = isStarterItem;
        getObj.SetActive(isStarterItem && canGet);
        getedObj.SetActive(isStarterItem && !canGet);
    }

    public void OnSelectState(bool isSelect)
    {
        SetSelectState(isSelect);
    }

    public void SetSelectState(bool isSelect)
    {
        selected = isSelect;
        selectObj.SetActive(isSelect);
        if (cardBackground == null) return;
        amountTxt.color = isSelect ? selectedAmountColor : normalAmountColor;
        amountTxt.rectTransform.localScale = isSelect ? selectedAmountScale : normalAmountScale;
        amountTxt.rectTransform.sizeDelta = isSelect ? selectedAmountSize : normalAmountSize;
        var sprite = isSelect ? selectedCard : normalCard;
        if (sprite != null) { cardBackground.sprite = sprite; cardBackground.enabled = true; }
    }

    public void SetArtwork(Sprite normal, Sprite highlighted, Sprite check)
    {
        normalCard = normal;
        selectedCard = highlighted;
        var image = selectObj.GetComponent<Image>();
        if (image != null && check != null) { image.sprite = check; image.enabled = true; }
        SetSelectState(selected);
    }
}
#endif
