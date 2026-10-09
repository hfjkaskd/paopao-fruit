using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static partial class OrchardApprovedPass
{
    private const string ReminderControls="Assets/OrchardUI/Resources/OrchardUI/ReminderReferenceControls.png";
    private static void ImportReminderReference()
    {
        AssetDatabase.Refresh();
        ImportFidelitySheet(ReminderControls,new[]{"Method","Amount","PinkCash"},
            new[]{new Rect(178,85,798,340),new Rect(27,499,1099,343),new Rect(258,913,637,389)},
            new[]{Vector4.zero,Vector4.zero,Vector4.zero});
        ExtraSprites.Clear();
    }
    private static void FinalizeReminderReference(GameObject root)
    {
        const string content="Content (1)/";
        var paths=new[]{content+"ApprovedMethod0",content+"ApprovedMethod1",content+"ApprovedMint",content+"ApprovedCash"};
        var names=new[]{"Method","Method","Amount","PinkCash"};
        var visual=root.GetComponent<OrchardReminderVisual>()??root.AddComponent<OrchardReminderVisual>();
        var so=new SerializedObject(visual);so.FindProperty("resourcePath").stringValue="OrchardUI/ReminderReferenceControls";
        var bindings=so.FindProperty("bindings");bindings.arraySize=paths.Length;
        for(int i=0;i<paths.Length;i++)
        {
            var image=Need(root,paths[i]).GetComponent<Image>();CashImage(image);
            var binding=bindings.GetArrayElementAtIndex(i);binding.FindPropertyRelative("image").objectReferenceValue=image;binding.FindPropertyRelative("sprite").stringValue=names[i];
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        Place(root,Need(root,"BG (2)"),24,391,806,1117);
        Place(root,Need(root,"BG (2)/Image (2)"),153,327,548,138);
        var title=Need(root,"BG (2)/Text (TMP)").GetComponent<TMP_Text>();Place(root,title.transform,173,351,507,88);SizeText(title,66);title.rectTransform.localScale=new Vector3(.89f,1,1);
        Place(root,Need(root,content+"CloseBtn"),713,379,105,105);
        var cash=Need(root,content+"ApprovedCash").GetComponent<Image>();Place(root,cash.transform,302,475,248,134);cash.preserveAspect=true;
        var balanceTitle=Need(root,content+"ApprovedBalanceTitle").GetComponent<TMP_Text>();Place(root,balanceTitle.transform,150,595,552,64);SizeText(balanceTitle,46);balanceTitle.rectTransform.localScale=new Vector3(.98f,1.1f,1);
        var balance=Need(root,content+"CoinHint (1)/CoinNum/balanceTxt").GetComponent<TMP_Text>();Place(root,balance.transform,153,645,544,104);SizeText(balance,102);balance.rectTransform.localScale=new Vector3(.895f,1.03f,1);
        Place(root,Need(root,content+"ApprovedMint"),59,753,734,249);
        var amountTitle=Need(root,content+"HintInfo/Hint").GetComponent<TMP_Text>();Place(root,amountTitle.transform,90,780,671,69);ReferenceBody(amountTitle);SizeText(amountTitle,48);amountTitle.rectTransform.localScale=new Vector3(.844f,.946f,1);
        var legacy=amountTitle.GetComponent<UILanguageLabel>();if(legacy!=null)legacy.enabled=false;
        BindCopy(amountTitle,"Withdrawal amount","Valor para saque");
        var amount=Need(root,content+"HintInfo/Num").GetComponent<TMP_Text>();Place(root,amount.transform,104,843,644,128);SizeText(amount,113);amount.rectTransform.localScale=new Vector3(.962f,1.263f,1);
        var note=Need(root,content+"ApprovedBalanceNote").GetComponent<TMP_Text>();Place(root,note.transform,101,1005,650,58);ReferenceBody(note);SizeText(note,34);note.color=ApprovedInk;note.rectTransform.localScale=new Vector3(1,1.088f,1);
        for(int i=0;i<2;i++)
        {
            Place(root,Need(root,content+"ApprovedMethod"+i),65+i*366,1073,358,172);
            var logo=Need(root,content+"ApprovedMethod"+i+"/Logo").GetComponent<Image>();Place(root,logo.transform,99+i*366,1104,291,105);logo.type=Image.Type.Simple;logo.preserveAspect=true;logo.color=Color.white;
        }
        var buttonLabel=Need(root,content+"WithdrawBtn/Text (TMP)").GetComponent<TMP_Text>();SizeText(buttonLabel,81);buttonLabel.rectTransform.localScale=new Vector3(.94f,1.06f,1);
        var footer=Need(root,content+"ApprovedFooter").GetComponent<TMP_Text>();Place(root,footer.transform,87,1419,678,59);ReferenceBody(footer);SizeText(footer,31);footer.rectTransform.localScale=new Vector3(.904f,.925f,1);
    }
    private static void PreviewReminderReference(GameObject root)
    {
        var sprites=new List<Sprite>();foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(ReminderControls))if(asset is Sprite sprite)sprites.Add(sprite);
        root.GetComponent<OrchardReminderVisual>().ApplyArtwork(sprites.ToArray());
    }
}
