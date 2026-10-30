using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static partial class OrchardApprovedPass
{
    private const string AccountControls = "Assets/OrchardUI/Resources/OrchardUI/AccountReferenceControls.png";
    private static void RestoreAccountAuthoringRoots(GameObject root)
    {
        RestoreAccountScrollRoots(root);
        var fill=root.transform.Find("Root/FillRoot");
        foreach(var name in new[]{"ReferenceInfoCard","ApprovedHint","ApprovedAccountFootnote"})
        {
            foreach(var child in fill.GetComponentsInChildren<Transform>(true))
                if(child.name==name){child.SetParent(fill,false);break;}
        }
        foreach(var name in new[]{"ReferenceHelpRow","ReferenceFooterRow"})
        {var row=fill.Find("pageContent/"+name);if(row!=null)row.gameObject.SetActive(false);}
    }
    private static void ImportAccountReference()
    {
        AssetDatabase.Refresh();
        ImportFidelitySheet(AccountControls, new[]{"Input", "FocusedInput", "Hint"},
            new[]{new Rect(70,813,713,108), new Rect(70,1167,713,111), new Rect(71,1277,712,114)},
            new[]{Vector4.zero,Vector4.zero,Vector4.zero});
        ExtraSprites.Clear();
    }
    private static void FinalizeAccountReference(GameObject root)
    {
#if BIZZA_REAL_WITHDRAW
        var page = root.GetComponent<UIWithdrawalPanel>();
        Place(root, page.paymentImage.transform, 178,260,498,180);
        page.paymentImage.type = Image.Type.Simple;
        page.paymentImage.preserveAspect = true;
        Place(root, Need(root,"Root/FillRoot/ApprovedPlaque"), 228,86,398,118);
        var inputs = new[]{page.accountNameInput,page.CPFNumberInput,page.paypalMailInput,page.accPhoneMailInput,page.accountIdentificationInput};
        var visuals = new List<OrchardInputVisual>();
        foreach (var input in inputs)
        {
            var background = input.transform.Find("Background").GetComponent<Image>();
            CashImage(background);
            background.raycastTarget = true;
            input.targetGraphic = background;
            input.transition = Selectable.Transition.None;
            LocalRect(input.transform,0,input==page.accountIdentificationInput?7:43,708,108);
            var visual = input.GetComponent<OrchardInputVisual>() ?? input.gameObject.AddComponent<OrchardInputVisual>();
            var binding = new SerializedObject(visual);
            binding.FindProperty("input").objectReferenceValue = input;
            binding.FindProperty("background").objectReferenceValue = background;
            binding.ApplyModifiedPropertiesWithoutUndo();
            visuals.Add(visual);
            foreach(var text in input.GetComponentsInChildren<TMP_Text>(true))
            {
                ReferenceBody(text); SizeText(text,36); text.enableAutoSizing=false;
                text.alignment=TextAlignmentOptions.Left;
            }
        }
        foreach(var errorRow in new[]{page.accountNameErrorTra,page.CPFNumberErrorTra,page.paypalMailErrorTra,page.accPhoneMailErrorTra,page.accountIdentificationErrorTra})
        {
            errorRow.GetComponent<LayoutElement>().preferredHeight=64;
            foreach(var text in errorRow.GetComponentsInChildren<TMP_Text>(true))
            {
                LocalRect(text.transform,0,0,708,36);ReferenceBody(text);SizeText(text,25);
                text.alignment=TextAlignmentOptions.Left;text.enableWordWrapping=true;
                text.color=new Color32(181,52,28,255);
            }
        }
        var hintImage = Need(root,"Root/FillRoot/ReferenceInfoCard").GetComponent<Image>();
        CashImage(hintImage); Place(root,hintImage.transform,74,1275,705,114);
        Need(root,"Root/FillRoot/ReferenceInfoIcon").gameObject.SetActive(false);
        var hint = Need(root,"Root/FillRoot/ApprovedHint").GetComponent<TMP_Text>();
        Place(root,hint.transform,183,1313,572,63); ReferenceBody(hint); SizeText(hint,36);
        hint.alignment=TextAlignmentOptions.MidlineLeft;
        var footer = Need(root,"Root/FillRoot/ApprovedAccountFootnote").GetComponent<TMP_Text>();
        ReferenceBody(footer); footer.color=new Color32(142,100,77,255); SizeText(footer,31);
        // Help, action and footer share the form's layout, so validation rows cannot cover them.
        var helpRow=Ensure(root,"Root/FillRoot/pageContent/ReferenceHelpRow");helpRow.gameObject.SetActive(true);
        var helpLayout=helpRow.GetComponent<LayoutElement>()??helpRow.gameObject.AddComponent<LayoutElement>();helpLayout.preferredHeight=105;
        Place(root,helpRow,74,1301,708,105);helpRow.SetSiblingIndex(1);
        hintImage.transform.SetParent(helpRow,false);LocalRect(hintImage.transform,0,-26,705,114);
        hint.transform.SetParent(helpRow,false);LocalRect(hint.transform,109,12,572,63);
        hint.rectTransform.localScale=new Vector3(.84f,1,1);
        hint.rectTransform.anchoredPosition+=new Vector2(-572*.08f,3);
        var button=Need(root,"Root/FillRoot/pageContent/BtnWithdrawal");button.SetSiblingIndex(2);Place(root,button,74,1406,708,142);
        var footerRow=Ensure(root,"Root/FillRoot/pageContent/ReferenceFooterRow");footerRow.gameObject.SetActive(true);
        var footerLayout=footerRow.GetComponent<LayoutElement>()??footerRow.gameObject.AddComponent<LayoutElement>();footerLayout.preferredHeight=75;
        Place(root,footerRow,74,1548,708,75);footerRow.SetAsLastSibling();
        footer.transform.SetParent(footerRow,false);LocalRect(footer.transform,38,19,628,53);
        footer.rectTransform.localScale=new Vector3(.864f,1,1);
        footer.rectTransform.anchoredPosition+=new Vector2(0,4);
        page.InputRoot.GetComponent<VerticalLayoutGroup>().spacing=0;
        page.defaultHeight=575;
        ConfigureBrazilAccountCopy(page);
        var art=root.GetComponent<OrchardAccountVisual>()??root.AddComponent<OrchardAccountVisual>();
        var data=new SerializedObject(art);data.FindProperty("resourcePath").stringValue="OrchardUI/AccountReferenceControls";
        data.FindProperty("hint").objectReferenceValue=hintImage;
        var list=data.FindProperty("inputs");list.arraySize=visuals.Count;
        for(int i=0;i<visuals.Count;i++)list.GetArrayElementAtIndex(i).objectReferenceValue=visuals[i];
        data.ApplyModifiedPropertiesWithoutUndo();
        ConfigureAccountScroll(root);
#endif
    }
    private static void PreviewAccountReference(GameObject root)
    {
#if BIZZA_REAL_WITHDRAW
        var art=root.GetComponent<OrchardAccountVisual>();if(art==null)return;
        var sprites=new List<Sprite>();foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(AccountControls))if(asset is Sprite sprite)sprites.Add(sprite);
        art.ApplyArtwork(sprites.ToArray());
        root.GetComponent<UIWithdrawalPanel>().paypalMailInput.GetComponent<OrchardInputVisual>().SetSelected(true);
#endif
    }
#if BIZZA_REAL_WITHDRAW
    private static void PreviewBrazilAccountCopy()
    {
        const string folder = "Design/BrazilWithdrawalCopy-20261012/";
        var report = new System.Text.StringBuilder("Actual prefab field-copy and layout preview; no account data, withdrawal submission or APK build.\n");
        foreach (bool pix in new[] { false, true })
        {
            var result = OrchardSkinValidation.PreviewPrefab(AccountScrollPrefab, folder + (pix ? "pix.png" : "pagbank.png"), root =>
            {
                ConfigureReferencePreview(root, "withdraw-account");
                var page = root.GetComponent<UIWithdrawalPanel>();
                foreach (var list in new[] { page.PIXObjList, page.PagBankList, page.PaypalList, page.OVOList, page.ErrorList })
                    foreach (var row in list) row.SetActive(false);
                foreach (var row in pix ? page.PIXObjList : page.PagBankList) row.SetActive(true);
                page.PlatformRoot.SetActive(false);
                page.PlatformIconRoot.SetActive(true);
                page.paymentImage.sprite = page.paymentList.GetSpriteByType(pix ? E_PayeeAccountType.PIX : E_PayeeAccountType.Pagbank);
                var settings = new SerializedObject(page);
                foreach (string key in new[] { "accountNameTitle", "cpfNumberTitle", "pixAccountTitle" })
                    ((GameObject)settings.FindProperty(key).objectReferenceValue).SetActive(false);
                foreach (string key in new[] { "brazilAccountNameTitle", "brazilCpfNumberTitle", "brazilPixAccountTitle" })
                    ((GameObject)settings.FindProperty(key).objectReferenceValue).SetActive(key != "brazilPixAccountTitle" || pix);
                page.accountNameInput.PlaceHolderText = settings.FindProperty("brazilAccountNamePlaceholder").stringValue;
                page.CPFNumberInput.PlaceHolderText = settings.FindProperty("brazilCpfNumberPlaceholder").stringValue;
                if (pix)
                {
                    page.accountIdentificationInput.PlaceHolderText = "11912345678";
                    for (int i = 0; i < page.accountTypeDropdown.Count; i++)
                        page.accountTypeDropdown[i].selectedIcon.SetActive(page.accountTypeDropdown[i].PayeeAccountType == PayeeAccountType.Phone);
                }
            }, 720, 1600, root =>
            {
                var page = root.GetComponent<UIWithdrawalPanel>();
                page.GetComponent<OrchardKeyboardFormScroll>().RefreshContent();
                foreach (var input in new[] { page.accountNameInput, page.CPFNumberInput })
                {
                    var text = input.PlaceholderTextRenderer.GetComponent<TMP_Text>();
                    text.ForceMeshUpdate();
                    if (text.isTextOverflowing) throw new System.InvalidOperationException(input.name + " placeholder overflows: rect=" + text.rectTransform.rect + "; area=" + input.TextAreaTransform.rect + "; content=" + input.TextContentTransform.rect + "; font=" + text.fontSize);
                    report.AppendLine((pix ? "Pix" : "PagBank") + " " + input.name + ": " + text.text.Replace("\n", " ") + "; font=" + text.fontSize + "; lines=" + text.textInfo.lineCount);
                }
            });
            if (!string.IsNullOrEmpty(result.error)) throw new System.InvalidOperationException(result.error);
        }
        System.IO.File.WriteAllText(folder + "result.txt", report.ToString());
    }

    private static void ConfigureBrazilAccountCopy(UIWithdrawalPanel page)
    {
        var settings = new SerializedObject(page);
        settings.FindProperty("brazilAccountNamePlaceholder").stringValue = "O nome deve corresponder ao CIN ou\nRG";
        settings.FindProperty("brazilCpfNumberPlaceholder").stringValue = "99999999999 ou\n99999999999999";
        var nameTitle = (GameObject)settings.FindProperty("accountNameTitle").objectReferenceValue;
        var brazilName = (GameObject)settings.FindProperty("brazilAccountNameTitle").objectReferenceValue;
        var brazilCpf = (GameObject)settings.FindProperty("brazilCpfNumberTitle").objectReferenceValue;
        var brazilPix = (GameObject)settings.FindProperty("brazilPixAccountTitle").objectReferenceValue;
        settings.ApplyModifiedPropertiesWithoutUndo();
        brazilName.GetComponent<TMP_Text>().text = "Nome(O nome deve corresponder ao CIN ou RG.)";
        brazilCpf.GetComponent<TMP_Text>().text = "CPF ou CNPJ";
        brazilPix.GetComponent<TMP_Text>().text = "Tipo da chave Pix";
        brazilPix.transform.parent.GetComponent<LayoutElement>().preferredHeight = 372;
        LocalRect(nameTitle.transform, 0, 0, 708, 88);
        LocalRect(brazilName.transform, 0, 0, 708, 88);
        LocalRect(page.accountNameInput.transform, 0, 91, 708, 108);
        page.accountNameInput.transform.parent.GetComponent<LayoutElement>().preferredHeight = 224;
        foreach (var input in new[] { page.accountNameInput, page.CPFNumberInput })
        {
            input.PlaceholderTextRenderer.GetComponent<TMP_Text>().enableAutoSizing = true;
            input.TextContentTransform.sizeDelta = new Vector2(684, 84);
            input.TextContentTransform.anchoredPosition = Vector2.zero;
        }
        var pixLocalization = page.accountIdentificationInput.PlaceholderTextRenderer.GetComponent<OrchardLocalizedLabel>();
        if (pixLocalization != null) UnityEngine.Object.DestroyImmediate(pixLocalization);
    }
#endif
}
