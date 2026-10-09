using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static partial class OrchardApprovedPass
{
    private const string CashControls = "Assets/OrchardUI/Resources/OrchardUI/CashReferenceControls.png";
    private const string CashCardPrefab = "Assets/BizzaWZ/Final/Real/UI/FakeWithdrawPanel/WithdrawAmountItem.prefab";
    private static void CashText(TMP_Text text)
    { text.font=ApprovedFont;text.fontSharedMaterial=ApprovedFont.material;text.UpdateMeshPadding(); }
    private static void ImportCashReference()
    {
        AssetDatabase.Refresh();
        ImportFidelitySheet(CashControls,new[]{"Card","SelectedCard","Check","PinkCash","DisabledButton","Track","Fill","Balance","Title"},
            new[]{new Rect(432,675,380,158),new Rect(41,674,381,159),new Rect(442,1048,88,91),new Rect(490,297,107,103),
                new Rect(54,1530,745,160),new Rect(49,1366,755,100),new Rect(141,1238,549,69),new Rect(40,407,773,190),new Rect(209,101,413,157)},
            new[]{Vector4.zero,Vector4.zero,Vector4.zero,Vector4.zero,Vector4.zero,new Vector4(58,0,58,0),new Vector4(42,0,42,0),Vector4.zero,Vector4.zero});
        ExtraSprites.Clear();
        var root=PrefabUtility.LoadPrefabContents(CashCardPrefab);
        try { StyleCashCard(root); StyleCashReferenceCard(root); PrefabUtility.SaveAsPrefabAsset(root,CashCardPrefab); }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
    private static void CashImage(Image image)
    {
        image.sprite=null;image.overrideSprite=null;image.type=Image.Type.Simple;image.preserveAspect=false;
        image.color=Color.white;image.enabled=false;image.raycastTarget=false;image.material=null;
    }
    private static void StyleCashReferenceCard(GameObject root)
    {
#if BIZZA_REAL_WITHDRAW
        var card=root.GetComponent<WithdrawAmountItem>();
        ((RectTransform)root.transform).sizeDelta=new Vector2(352,144);
        var old=Need(root,"bg").GetComponent<Image>();old.enabled=false;old.raycastTarget=false;
        var background=root.GetComponent<Image>()??root.AddComponent<Image>();CashImage(background);background.raycastTarget=true;
        card.btn.targetGraphic=background;card.btn.transition=Selectable.Transition.ColorTint;
        var colors=ColorBlock.defaultColorBlock;colors.pressedColor=new Color(.91f,.91f,.91f,1);card.btn.colors=colors;
        var check=card.selectObj.GetComponent<Image>();CashImage(check);LocalRect(check.transform,271,38,69,72);
        LocalText(card.amountTxt.transform,43,28,266,88,79);CashText(card.amountTxt);card.amountTxt.alignment=TextAlignmentOptions.Center;
        card.amountTxt.rectTransform.localScale=new Vector3(.84f,1,1);
        card.amountTxt.enableWordWrapping=false;card.amountTxt.enableAutoSizing=true;card.amountTxt.fontSizeMin=36;
        var data=new SerializedObject(card);data.FindProperty("cardBackground").objectReferenceValue=background;
        data.FindProperty("selectedAmountColor").colorValue=new Color32(0,91,61,255);data.FindProperty("normalAmountColor").colorValue=ApprovedInk;
        data.FindProperty("selectedAmountScale").vector3Value=new Vector3(.95f,1.06f,1);data.FindProperty("normalAmountScale").vector3Value=new Vector3(.84f,1,1);
        data.FindProperty("selectedAmountSize").vector2Value=new Vector2(180,88);data.FindProperty("normalAmountSize").vector2Value=new Vector2(266,88);data.ApplyModifiedPropertiesWithoutUndo();
        foreach(var tag in new[]{card.getObj,card.getedObj})
        {
            LocalRect(tag.transform,35,7,280,30);tag.transform.SetAsLastSibling();
            foreach(var text in tag.GetComponentsInChildren<TMP_Text>(true)){SizeText(text,23);text.enableWordWrapping=false;}
            foreach(Transform child in tag.transform)if(child.name.StartsWith("OrchardNavLeaves"))child.gameObject.SetActive(false);
        }
        card.SetSelectState(false);
#endif
    }
    private static void FinalizeCashReference(GameObject root)
    {
#if BIZZA_REAL_WITHDRAW
        var page=root.GetComponent<FakeWithdrawPanel>();
        Place(root,Need(root,"Content/Frame"),23,211,806,1360);
        var title=Need(root,"ButtomGroup/Title").GetComponent<TMP_Text>();Place(root,title.transform,274,72,302,112);SizeText(title,58);title.lineSpacing=-10;
        Place(root,Need(root,"ButtomGroup/bg"),228,61,391,142);
        var balanceTitle=Need(root,"Content/CashBalance/Title").GetComponent<TMP_Text>();Place(root,balanceTitle.transform,257,265,225,64);SizeText(balanceTitle,51);CashText(balanceTitle);balanceTitle.rectTransform.localScale=new Vector3(.93f,1,1);
        var pink=Ensure(root,"Content/CashBalance/ReferenceCashIcon").gameObject;var pinkImage=pink.GetComponent<Image>()??pink.AddComponent<Image>();Place(root,pink.transform,488,247,82,86);CashImage(pinkImage);
        Place(root,page.balanceTxt.transform,120,362,612,138);SizeText(page.balanceTxt,132);page.balanceTxt.rectTransform.localScale=new Vector3(.835f,1.066f,1);page.balanceTxt.color=new Color32(0,80,17,255);
        var balance=Need(root,"Content/CashBalance/Balance/bg").GetComponent<Image>();Place(root,balance.transform,66,340,722,178);CashImage(balance);
        var amountTitle=Need(root,"Content/WithdrawAmount/Title").GetComponent<TMP_Text>();Place(root,amountTitle.transform,48,529,706,66);SizeText(amountTitle,47);CashText(amountTitle);amountTitle.rectTransform.localScale=new Vector3(.93f,1,1);
        var scroll=Need(root,"Content/WithdrawAmount/Scroll View").GetComponent<ScrollRect>();Place(root,scroll.transform,65,597,724,473);
        var viewport=scroll.viewport;StretchRect(viewport);
        var oldMask=viewport.GetComponent<Mask>();if(oldMask!=null)Object.DestroyImmediate(oldMask);
        var mask=viewport.GetComponent<RectMask2D>()??viewport.gameObject.AddComponent<RectMask2D>();mask.padding=Vector4.zero;mask.softness=Vector2Int.zero;
        var viewportImage=viewport.GetComponent<Image>();if(viewportImage!=null)viewportImage.enabled=false;
        var grid=scroll.content.GetComponent<GridLayoutGroup>();TopContent(scroll.content);grid.cellSize=new Vector2(352,144);grid.spacing=new Vector2(10,12);grid.padding=new RectOffset(3,3,7,7);
        scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.verticalNormalizedPosition=1;
        foreach(var item in root.GetComponentsInChildren<WithdrawAmountItem>(true))StyleCashReferenceCard(item.gameObject);
        var divider=Ensure(root,"Content/ReferenceDivider");var dividerImage=divider.GetComponent<Image>()??divider.gameObject.AddComponent<Image>();dividerImage.sprite=null;dividerImage.color=new Color32(222,193,141,160);dividerImage.raycastTarget=false;Place(root,divider,81,1091,692,2);
        var goalTitle=Need(root,"Content/WithdrawProgress/Title").GetComponent<TMP_Text>();Place(root,goalTitle.transform,65,1109,687,59);SizeText(goalTitle,43);CashText(goalTitle);goalTitle.rectTransform.localScale=new Vector3(.96f,1,1);
        var progress=Need(root,"Content/WithdrawProgress/Progress");Place(root,progress,76,1173,702,85);
        var track=Need(root,"Content/WithdrawProgress/Progress/bg").GetComponent<Image>();StretchRect(track.transform);CashImage(track);track.type=Image.Type.Sliced;track.pixelsPerUnitMultiplier=1.28f;
        Place(root,page.progressImg.transform,87,1184,680,63);RoundFill(page.progressImg);CashImage(page.progressImg);page.progressImg.type=Image.Type.Sliced;page.progressImg.pixelsPerUnitMultiplier=1.23f;
        page.progressTxt.transform.SetParent(progress,false);Place(root,page.progressTxt.transform,286,1184,255,62);SizeText(page.progressTxt,51);SetReferenceTitle(page.progressTxt,new Color32(26,112,32,255));
        Place(root,page.hintTxt.transform,109,1259,640,61);SizeText(page.hintTxt,36);CashText(page.hintTxt);page.hintTxt.rectTransform.localScale=new Vector3(.914f,1,1);page.hintTxt.enableWordWrapping=true;
        Place(root,page.withdrawBtn.transform,88,1339,677,143);
        var withdrawImage=page.withdrawBtn.GetComponent<Image>();Paint(withdrawImage,"ButtonGreen");var enabledSprite=withdrawImage.sprite;
        var label=Need(root,"Content/WithdrawBtn/Text").GetComponent<TMP_Text>();Place(root,label.transform,202,1356,447,112);SizeText(label,79);label.rectTransform.localScale=new Vector3(.90f,1,1);SetReferenceTitle(label,new Color32(0,77,24,255));var enabledMaterial=label.fontSharedMaterial;
        const string disabledPath="Assets/OrchardUI/Generated/CashDisabledTitle.mat";
        var disabledMaterial=AssetDatabase.LoadAssetAtPath<Material>(disabledPath);
        if(disabledMaterial==null){disabledMaterial=new Material(enabledMaterial);AssetDatabase.CreateAsset(disabledMaterial,disabledPath);}
        disabledMaterial.SetColor("_FaceColor",new Color32(233,239,232,255));disabledMaterial.SetColor("_OutlineColor",new Color32(88,111,99,255));disabledMaterial.SetFloat("_OutlineWidth",.12f);disabledMaterial.SetColor("_UnderlayColor",new Color32(52,70,58,140));EditorUtility.SetDirty(disabledMaterial);
        CashImage(withdrawImage);withdrawImage.raycastTarget=true;page.withdrawBtn.targetGraphic=withdrawImage;
        var buttonColors=page.withdrawBtn.colors;buttonColors.disabledColor=Color.white;page.withdrawBtn.colors=buttonColors;
        var footer=Copy(root,"Content/ReferenceUnavailableHint","Complete the goal to continue.","Complete a meta para continuar.",118,1484,616,49,30.5f);ReferenceBody(footer);
        Need(root,"ServiceBtn").gameObject.SetActive(false);
        var visual=root.GetComponent<OrchardCashVisual>()??root.AddComponent<OrchardCashVisual>();var data=new SerializedObject(visual);
        data.FindProperty("resourcePath").stringValue="OrchardUI/CashReferenceControls";data.FindProperty("page").objectReferenceValue=page;
        var bindings=data.FindProperty("bindings");var images=new[]{pinkImage,balance,track,page.progressImg,Need(root,"ButtomGroup/bg").GetComponent<Image>()};var names=new[]{"PinkCash","Balance","Track","Fill","Title"};bindings.arraySize=images.Length;
        for(int i=0;i<images.Length;i++){CashImage(images[i]);var binding=bindings.GetArrayElementAtIndex(i);binding.FindPropertyRelative("image").objectReferenceValue=images[i];binding.FindPropertyRelative("sprite").stringValue=names[i];}
        track.type=page.progressImg.type=Image.Type.Sliced;
        data.FindProperty("withdrawImage").objectReferenceValue=withdrawImage;data.FindProperty("enabledButton").objectReferenceValue=enabledSprite;
        data.FindProperty("withdrawLabel").objectReferenceValue=label;data.FindProperty("enabledLabel").objectReferenceValue=enabledMaterial;data.FindProperty("disabledLabel").objectReferenceValue=disabledMaterial;
        data.FindProperty("unavailableHint").objectReferenceValue=footer.gameObject;data.ApplyModifiedPropertiesWithoutUndo();
        data.FindProperty("englishRemainingFormat").stringValue="Need <color=#005B1D>{0}</color> more to reach this goal.";
        data.FindProperty("portugueseRemainingFormat").stringValue="Faltam <color=#005B1D>{0}</color> para esta meta.";data.ApplyModifiedPropertiesWithoutUndo();
        var business=new SerializedObject(page);business.FindProperty("referenceVisual").objectReferenceValue=visual;business.ApplyModifiedPropertiesWithoutUndo();
        visual.SetAvailable(false);
#endif
    }
    private static void PreviewCashReference(GameObject root)
    {
#if BIZZA_REAL_WITHDRAW
        var visual=root.GetComponent<OrchardCashVisual>();if(visual==null)return;
        var sprites=new List<Sprite>();foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(CashControls))if(asset is Sprite sprite)sprites.Add(sprite);
        visual.ApplyArtwork(sprites.ToArray());var items=root.GetComponentsInChildren<WithdrawAmountItem>(true);visual.RefreshCards(items);
        for(int i=0;i<items.Length;i++)items[i].SetSelectState(i==0);visual.SetAvailable(false);
#endif
    }
}
