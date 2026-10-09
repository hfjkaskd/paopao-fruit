using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static partial class OrchardApprovedPass
{
    private const string HelpControls="Assets/OrchardUI/Resources/OrchardUI/LuckyHelpReferenceControls.png";
    private static void ImportLuckyHelpReference()
    {
        AssetDatabase.Refresh();
        ImportFidelitySheet(HelpControls,new[]{"Title","Guide","Progress","Video","Row","Tile","Arrow","Close","Action","CashSmall","CashLarge","Info"},
            new[]{new Rect(76,29,735,201),new Rect(31,246,825,293),new Rect(44,563,342,269),new Rect(418,580,426,238),new Rect(29,854,831,148),new Rect(73,1039,226,227),new Rect(380,1088,161,142),new Rect(614,1048,219,219),new Rect(16,1290,858,212),new Rect(78,1556,214,174),new Rect(352,1532,246,216),new Rect(661,1574,139,138)},
            new[]{Vector4.zero,Vector4.zero,Vector4.zero,Vector4.zero,Vector4.zero,Vector4.zero,Vector4.zero,Vector4.zero,Vector4.zero,Vector4.zero,Vector4.zero,Vector4.zero});ExtraSprites.Clear();
    }
    private static Image HelpArt(GameObject root,string path,float x,float y,float w,float h,List<Image> images,List<string> names,string sprite)
    {
        var t=Ensure(root,path);var image=t.GetComponent<Image>()??t.gameObject.AddComponent<Image>();Place(root,t,x,y,w,h);CashImage(image);image.preserveAspect=false;images.Add(image);names.Add(sprite);return image;
    }
    private static void FinalizeLuckyHelpReference(GameObject root)
    {
#if BIZZA_REAL_WITHDRAW
        var images=new List<Image>();var names=new List<string>();
        Need(root,"bg").GetComponent<Image>().enabled=false;
        var mask=Ensure(root,"PageMaskReference");var dim=mask.GetComponent<Image>()??mask.gameObject.AddComponent<Image>();StretchRect(mask);mask.SetAsFirstSibling();dim.enabled=true;dim.sprite=null;dim.color=new Color(0,0,0,.35f);dim.raycastTarget=false;
        foreach(var rect in root.GetComponentsInChildren<RectTransform>(true))if(rect.name.StartsWith("OrchardNavLeaves"))rect.gameObject.SetActive(false);
        Place(root,Need(root,"Content/bg2 (1)"),16,279,820,1401);
        HelpArt(root,"Content/ApprovedPlaque",130,222,592,148,images,names,"Title");
        var title=Need(root,"Content/ApprovedTitle").GetComponent<TMP_Text>();Place(root,title.transform,211,250,430,109);SetReferenceTitle(title,ApprovedInk);SizeText(title,84);title.alignment=TextAlignmentOptions.Center;title.rectTransform.localScale=new Vector3(.96f,1.12f,1);
        var arch=title.GetComponent<OrchardArchedText>()??title.gameObject.AddComponent<OrchardArchedText>();var archSo=new SerializedObject(arch);archSo.FindProperty("archHeight").floatValue=7;archSo.ApplyModifiedPropertiesWithoutUndo();
        Need(root,"Content/des").gameObject.SetActive(false);
        var subtitle=Copy(root,"Content/ReferenceSubtitle","A little luck from the orchard","Um pouco de sorte no pomar",178,374,496,50,36);subtitle.rectTransform.localScale=new Vector3(.97f,1.1f,1);
        for(int i=0;i<2;i++){var leaf=Detail(root,"Content/ReferenceLeaf"+i,"Input",i==0?149:662,379,40,39);leaf.sprite=NamedSprite("Assets/OrchardUI/Art/Navigation.png",i==0?"LeavesLeft":"LeavesRight");leaf.type=Image.Type.Simple;leaf.preserveAspect=true;}
        HelpArt(root,"Content/ReferenceGuide",63,430,725,236,images,names,"Guide");
        HelpArt(root,"Content/ReferenceProgress",136,435,151,116,images,names,"Progress");
        var five=Copy(root,"Content/ReferenceFive","5/5","5/5",151,478,94,50,39);SetReferenceTitle(five,new Color32(21,103,25,255));
        var fiveBg=Detail(root,"Content/ReferenceFiveBg","ButtonGreen",155,480,86,44);fiveBg.transform.SetSiblingIndex(five.transform.GetSiblingIndex());five.transform.SetAsLastSibling();
        HelpArt(root,"Content/ReferenceVideo",113,557,172,99,images,names,"Video");
        foreach(var text in new[]{Copy(root,"Content/ReferenceLevelHelp","Complete 5 levels\nfor a free spin.","Complete 5 fases\npara um giro grátis.",313,444,445,92,36),Copy(root,"Content/ReferenceVideoHelp","Watch a video\nfor another spin.","Assista a um vídeo\npara outro giro.",313,558,445,94,36)}){text.alignment=TextAlignmentOptions.MidlineLeft;text.enableWordWrapping=true;text.rectTransform.localScale=new Vector3(.97f,1.09f,1);}
        ServiceLine(root,"Content/ReferenceRuleTop",69,680,715,new Color32(231,171,102,255));ServiceLine(root,"Content/ReferenceRuleBottom",77,1436,700,new Color32(231,171,102,255));
        var list=Need(root,"Content/bg2 (1)/root");Place(root,list,64,696,724,728);
        string[] en={"Small\nreward","Big\nreward","Surprise\nreward","Small\nreward","Big\nreward","Double\nreward"};string[] pt={"Prêmio\npequeno","Prêmio\ngrande","Prêmio\nsurpresa","Prêmio\npequeno","Prêmio\ngrande","Prêmio\nduplo"};
        int index=0;
        foreach(Transform row in list)
        {
            int i=index++;string path="Content/bg2 (1)/root/"+row.name;float y=696+i*122;
            HelpArt(root,path,64,y,724,114,images,names,"Row");
            for(int c=0;c<3;c++)
            {
                string tilePath=path+"/Slot_1"+(c==0?"":" ("+c+")");HelpArt(root,tilePath,71+c*118,y+6,111,104,images,names,"Tile");
                var icon=Need(root,tilePath+"/Content").GetComponent<Image>();LocalRect(icon.transform,11,7,89,87);icon.color=Color.white;icon.enabled=true;icon.type=Image.Type.Simple;icon.preserveAspect=true;
            }
            foreach(Transform child in row)
            {
                if(child.name.StartsWith("que"))child.gameObject.SetActive(false);
                if(child.name.StartsWith("des"))
                {
                    var label=child.GetComponent<TMP_Text>();var old=label.GetComponent<UILanguageLabel>();if(old!=null)old.enabled=false;LocalRect(child,585,14,130,88);TextStyle(label,35);BindCopy(label,en[i],pt[i]);label.alignment=TextAlignmentOptions.MidlineLeft;label.enableWordWrapping=true;label.rectTransform.localScale=new Vector3(.96f,1.1f,1);
                }
                if(child.name=="Slot_1 (4)"||child.name=="Slot_1 (3)")child.gameObject.SetActive(false);
            }
            HelpArt(root,path+"/ReferenceArrow",435,y+39,42,37,images,names,"Arrow");
            if(i<3||i==5)HelpArt(root,path+"/ReferenceCash",i==5?491:505,y+(i==5?13:15),i==5?104:112,i==5?85:90,images,names,i==0?"CashSmall":"CashLarge");
            if(i>=3)
            {
                var coins=Detail(root,path+"/ReferenceCoins","Input",i==5?548:502,y+(i==5?40:14),i==5?82:116,i==5?66:89);PaintReferenceDetail(coins,"CoinStack");coins.type=Image.Type.Simple;coins.preserveAspect=true;coins.color=Color.white;
            }
        }
        HelpArt(root,"Content/ReferenceInfo",201,1452,35,35,images,names,"Info");
        var footer=Need(root,"Content/ApprovedFooter").GetComponent<TMP_Text>();Place(root,footer.transform,249,1448,507,50);SizeText(footer,27);footer.alignment=TextAlignmentOptions.MidlineLeft;footer.color=new Color32(122,70,40,255);
        HelpArt(root,"Content/Btn",69,1501,714,157,images,names,"Action");var action=Need(root,"Content/Btn").GetComponent<Image>();action.raycastTarget=true;root.GetComponent<SlotFAQPanel>().bizzaButton.targetGraphic=action;
        var actionText=Need(root,"Content/Btn/ApprovedLabel").GetComponent<TMP_Text>();Place(root,actionText.transform,188,1515,476,123);SetReferenceTitle(actionText,new Color32(0,77,22,255));SizeText(actionText,98);actionText.alignment=TextAlignmentOptions.Center;
        var closeImage=HelpArt(root,"Content/ReferenceClose",731,267,99,96,images,names,"Close");var close=closeImage.GetComponent<Button>()??closeImage.gameObject.AddComponent<Button>();close.targetGraphic=closeImage;closeImage.raycastTarget=true;ClearAuthoredListeners(close);var pageSo=new SerializedObject(root.GetComponent<SlotFAQPanel>());pageSo.FindProperty("closeButton").objectReferenceValue=close;pageSo.ApplyModifiedPropertiesWithoutUndo();
        var visual=root.GetComponent<OrchardLuckyHelpVisual>()??root.AddComponent<OrchardLuckyHelpVisual>();var so=new SerializedObject(visual);so.FindProperty("resourcePath").stringValue="OrchardUI/LuckyHelpReferenceControls";var bindings=so.FindProperty("bindings");bindings.arraySize=images.Count;
        for(int i=0;i<images.Count;i++){var b=bindings.GetArrayElementAtIndex(i);b.FindPropertyRelative("image").objectReferenceValue=images[i];b.FindPropertyRelative("sprite").stringValue=names[i];}so.ApplyModifiedPropertiesWithoutUndo();
        OrchardSlotFunctionAuthoring.ConfigureHelp(root);
#endif
    }
    private static void PreviewLuckyHelpReference(GameObject root)
    {
        var sprites=new List<Sprite>();foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(HelpControls))if(asset is Sprite sprite)sprites.Add(sprite);root.GetComponent<OrchardLuckyHelpVisual>().ApplyArtwork(sprites.ToArray());
    }
}
