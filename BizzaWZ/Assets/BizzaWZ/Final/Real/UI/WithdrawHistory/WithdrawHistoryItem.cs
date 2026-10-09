#if BIZZA_REAL_WITHDRAW
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

 
public class WithdrawHistoryItem : MonoBehaviour
{
    public TMP_Text nameTxt;
    public TMP_Text timeTxt;
    public TMP_Text emailTxt;
    public TMP_Text cpfTxt;
    public TMP_Text dueText;
    
    public GameObject successObj;
    public GameObject processingObj;
    public GameObject failObj;
    
    public Image withdrawImg;
    public TMP_Text amountTxt;
    
    [SerializeField] private PaymentConfig paymentConfig;
    [SerializeField] private float normalRowHeight = 300;
    [SerializeField] private float failedRowHeight = 342;
     
    public void Init(AccountModule.OceanShineWithdrawalRecord data)
    {
        nameTxt.gameObject.SetActive(false);
        emailTxt.gameObject.SetActive(false);
        cpfTxt.gameObject.SetActive(false);
        timeTxt.text = $"{data.Os_Dat}";
        amountTxt.text = $"{LanguageUtils.GetText("CurrencyToken")}{WithdrawalUtil.GetCustomizedValueByCountryType((float)data.Os_Prc)}";
        // The compact record shows the receiving account on one line, as authored in the prefab.
        // Other server fields stay on the record; no account data is modified here.
        string account = string.Equals(data.Os_Pym, "pix", StringComparison.OrdinalIgnoreCase) ? data.Os_Cp : data.Os_Re;
        if (string.IsNullOrEmpty(account)) account = data.Os_Ra;
        if (string.IsNullOrEmpty(account)) account = data.Os_Cp;
        emailTxt.text = account ?? string.Empty;
        emailTxt.gameObject.SetActive(!string.IsNullOrEmpty(account));
        
        withdrawImg.sprite = paymentConfig.GetSpriteByPayKey(data.Os_Pym);
        
        successObj.SetActive(data.Os_Sts == 3);
        processingObj.SetActive(data.Os_Sts == 1);
        bool isFail = data.Os_Sts == 2 || data.Os_Sts > 3;
        failObj.SetActive(isFail);
        dueText.gameObject.SetActive(isFail && !string.IsNullOrEmpty(data.Os_Tsm));
        dueText.text = data.Os_Tsm;

        float length = transform.GetComponent<RectTransform>().sizeDelta.x;
        transform.GetComponent<RectTransform>().sizeDelta = new Vector2(length, isFail ? failedRowHeight : normalRowHeight);

    }
}

#endif
