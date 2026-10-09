using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static partial class OrchardApprovedPass
{
    private static void SetupApprovedRewards(GameObject root,string name)
    {
#if BIZZA_REAL_WITHDRAW
        if(name=="level-complete")
        {
            var p=root.GetComponent<GetRewardPanel>();
            var next=Need(root,"NextBtn").GetComponent<Button>();ClearAuthoredListeners(next);
            var pageSo=new SerializedObject(p);pageSo.FindProperty("approvedNextButton").objectReferenceValue=next;pageSo.ApplyModifiedPropertiesWithoutUndo();
            foreach(var l in Need(root,"BG/Content/Rewards").GetComponentsInChildren<LayoutGroup>(true))l.enabled=false;
            foreach(var f in Need(root,"BG/Content/Rewards").GetComponentsInChildren<ContentSizeFitter>(true))f.enabled=false;
            foreach(var im in Need(root,"BG/Content/Rewards").GetComponentsInChildren<Image>(true))im.enabled=false;
            foreach(var t in Need(root,"BG/Content/Rewards").GetComponentsInChildren<TMP_Text>(true))if(t!=p.itemATxt&&t!=p.itemBTxt)t.enabled=false;
            Place(root,p.itemATxt.transform,115,878,622,133);TextStyle(p.itemATxt,103);p.itemATxt.alignment=TextAlignmentOptions.Center;
            Place(root,p.itemBTxt.transform,276,994,300,60);TextStyle(p.itemBTxt,40);p.itemBTxt.alignment=TextAlignmentOptions.Center;
            Place(root,p.levelTxt.transform,252,353,348,61);TextStyle(p.levelTxt,36,true);p.levelTxt.alignment=TextAlignmentOptions.Center;
            var lt=p.levelTxt.transform.parent.GetComponent<Image>();if(lt!=null){Paint(lt,"Badge");Place(root,lt.transform,228,341,396,82);StretchRect(p.levelTxt.transform);}
            foreach(Transform t in Need(root,"BG")) if(t.name=="Image (2)"&&t!=p.levelTxt.transform.parent)t.GetComponent<Image>().enabled=false;
            foreach(string n in new[]{"Real_WithdrawProgress","Fake_WithdrawProgress"})
                foreach(var g in Need(root,"BG/Content/"+n).GetComponentsInChildren<Graphic>(true))g.enabled=false;
            var w=root.GetComponentInChildren<WathAdProgress>(true);
            Paint(Need(root,"BG/Content/WathAdProgress/bg").GetComponent<Image>(),"Inset");StretchRect(Need(root,"BG/Content/WathAdProgress/bg"));
            LocalRect(Need(root,"BG/Content/WathAdProgress/progress"),28,72,515,71);
            var track=Need(root,"BG/Content/WathAdProgress/progress/bg");StretchRect(track);Paint(track.GetComponent<Image>(),"ProgressTrack");
            var area=Need(root,"BG/Content/WathAdProgress/progress/bg/FillArea");StretchRect(area);((RectTransform)area).offsetMin=new Vector2(9,8);((RectTransform)area).offsetMax=new Vector2(-9,-8);
            Paint(w.progressBar,"ProgressFill");StretchRect(w.progressBar.transform);
            LocalText(w.hintText.transform,31,16,634,44,31);w.hintText.alignment=TextAlignmentOptions.Center;
            LocalText(w.progressText.transform,42,82,487,52,37);w.progressText.alignment=TextAlignmentOptions.Center;
            Need(root,"BG/Content/WathAdProgress/Icons").gameObject.SetActive(false);
            var claimButton=(RectTransform)Need(root,"BG/Content/ButtonAnim/Button");
            var bonusRateRect=(RectTransform)p.bonusRate.transform;
            bonusRateRect.SetParent(claimButton,false);
            bonusRateRect.SetAsLastSibling();
            bonusRateRect.anchorMin=Vector2.one;bonusRateRect.anchorMax=Vector2.one;
            bonusRateRect.pivot=new Vector2(.5f,.5f);
            bonusRateRect.anchoredPosition=new Vector2(-16,-32);bonusRateRect.sizeDelta=new Vector2(100,100);
            foreach(var im in p.bonusRate.GetComponentsInChildren<Image>(true))im.enabled=false;
            var badge=p.bonusRate.GetComponent<Image>()??p.bonusRate.gameObject.AddComponent<Image>();Paint(badge,"ButtonGreen");badge.raycastTarget=false;
            StretchRect(p.bonusRate.bonusRateTxt.transform);TextStyle(p.bonusRate.bonusRateTxt,35,true);p.bonusRate.bonusRateTxt.alignment=TextAlignmentOptions.Center;
            var ad=Need(root,"BG/Content/ButtonAnim/Button/Image (2)");LocalRect(ad,180,28,100,94);
            Place(root,p.rewardText.transform,406,1264,350,121);TextStyle(p.rewardText,58,true);p.rewardText.alignment=TextAlignmentOptions.MidlineLeft;
            TextStyle(p.noThanksText,49,true);p.noThanksText.alignment=TextAlignmentOptions.Center;
        }
        if(name=="get-booster")
        {
            foreach(var tx in Need(root,"Content/BtnGroup/ButtonAnim/AdBtn").GetComponentsInChildren<TMP_Text>(true)) { StretchRect(tx.transform);TextStyle(tx,57,true);tx.alignment=TextAlignmentOptions.Center; }
            var children=Need(root,"BG (1)").GetComponentsInChildren<Image>(true);
            bool first=true;foreach(var im in children)if(im.name=="Image (2)"){if(!first)im.enabled=false;first=false;}
            Need(root,"Content/BtnGroup/ButtonAnim/Pop").gameObject.SetActive(false);
            var adIcon=Need(root,"Content/BtnGroup/ButtonAnim/AdBtn/Image (3)");LocalRect(adIcon,42,38,88,87);
            foreach(var tx in root.GetComponent<AddPropPanel>().adBuyBtn.GetComponentsInChildren<TMP_Text>(true)){LocalRect(tx.transform,145,26,465,110);TextStyle(tx,49,true);}
        }
        if(name=="daily-mission")
        {
            foreach(var btn in Need(root,"Content").GetComponentsInChildren<Button>(true))
                foreach(var im in btn.GetComponentsInChildren<Image>(true))if(im.transform!=btn.transform)im.enabled=false;
            var page=root.GetComponent<DailyMissionPanel>();
            var amount=ApprovedDynamicText(root,"Content/ApprovedRewardValue",121,936,610,85,84);amount.color=ApprovedGreen;
            var progressText=ApprovedDynamicText(root,"Content/ApprovedProgressValue",188,1116,476,66,41);
            var track=Ensure(root,"Content/ApprovedProgressTrack");var trackImage=track.GetComponent<Image>()??track.gameObject.AddComponent<Image>();Paint(trackImage,"ProgressTrack");Place(root,track,97,1106,658,87);track.SetAsFirstSibling();
            var fill=Ensure(root,"Content/ApprovedProgressTrack/Fill");var fillImage=fill.GetComponent<Image>()??fill.gameObject.AddComponent<Image>();Paint(fillImage,"ProgressFill");StretchRect(fill);fillImage.type=Image.Type.Filled;fillImage.fillMethod=Image.FillMethod.Horizontal;fillImage.fillAmount=0;
            var so=new SerializedObject(page);so.FindProperty("approvedRewardAmount").objectReferenceValue=amount;so.FindProperty("approvedProgressText").objectReferenceValue=progressText;so.FindProperty("approvedProgressFill").objectReferenceValue=fillImage;so.ApplyModifiedPropertiesWithoutUndo();
            Place(root,page.hintsTxt.transform,106,853,640,74);TextStyle(page.hintsTxt,36);page.hintsTxt.alignment=TextAlignmentOptions.Center;
        }
        if(name=="lucky-spin")
        {
            var p=root.GetComponent<SlotPanel>();var m=p.slotMachineManager;
            var oldSafeArea=Need(root,"Top").GetComponent<TopSafeAreaAdapter>();if(oldSafeArea!=null)Object.DestroyImmediate(oldSafeArea);
            var claim=p.slotRewardPanel.btnObj.GetComponent<Button>();ClearAuthoredListeners(claim);
            var rewardSo=new SerializedObject(p.slotRewardPanel);rewardSo.FindProperty("approvedClaimButton").objectReferenceValue=claim;rewardSo.ApplyModifiedPropertiesWithoutUndo();
            // Keep the Spine animation running: its completion callback resolves the result.
            // Only its old cabinet artwork is transparent, with authored sprites above it.
            m.anim.color=Color.clear;
            m.anim.raycastTarget=false;
            foreach(var graphic in m.GetComponentsInChildren<Spine.Unity.SkeletonGraphic>(true))graphic.raycastTarget=false;
            var reward=Need(root,"Content/SlotMachineGroup/Content/RewardGroup");
            var follower=reward.GetComponent<Spine.Unity.BoneFollowerGraphic>();if(follower!=null)Object.DestroyImmediate(follower);
            Place(root,reward,77,498,698,470);
            var mask=Need(root,"Content/SlotMachineGroup/Content/RewardGroup/SlotMask");
            Place(root,mask,145,607,562,362);mask.GetComponent<Image>().color=Color.white;
            var reelPaper=Ensure(root,"Content/SlotMachineGroup/ApprovedReelPaper");var paper=reelPaper.GetComponent<Image>()??reelPaper.gameObject.AddComponent<Image>();Paint(paper,"Input");Place(root,reelPaper,133,601,586,377);reelPaper.SetAsFirstSibling();
            Need(root,"Content/SlotMachineGroup/Content/RewardGroup/Bg_Front").GetComponent<Image>().enabled=false;
            for(int i=0;i<m.slotEntries.Length;i++)
            {
                var entry=m.slotEntries[i];LocalRect(entry.transform,44+i*176,133,122,122);
                var animation=entry.GetComponent<Animation>();if(animation!=null)animation.playAutomatically=false;
                entry.img1.rectTransform.anchorMin=entry.img1.rectTransform.anchorMax=new Vector2(.5f,.5f);entry.img1.rectTransform.anchoredPosition=Vector2.zero;
                entry.GetComponent<Image>().enabled=false;
                foreach(var im in new[]{entry.img1,entry.img2}){im.preserveAspect=true;im.rectTransform.sizeDelta=new Vector2(149,149);}
                entry.img1.sprite=NamedSprite("Assets/OrchardUI/Art/OrchardReelSymbols.png",i==1?"Coin":"Apple");
                entry.img2.sprite=entry.img1.sprite;entry.img2.gameObject.SetActive(false);
            }
            m.rewardIm1.sprite=NamedSprite("Assets/OrchardUI/Art/OrchardReelSymbols.png","Apple");m.rewardIm2.sprite=NamedSprite("Assets/OrchardUI/Art/OrchardReelSymbols.png","Coin");m.rewardIm3.sprite=m.rewardIm1.sprite;
            var machineSo=new SerializedObject(m);var symbols=machineSo.FindProperty("slotEntryss");
            for(int i=0;i<symbols.arraySize;i++)symbols.GetArrayElementAtIndex(i).FindPropertyRelative("sprite").objectReferenceValue=NamedSprite("Assets/OrchardUI/Art/OrchardReelSymbols.png",new[]{"Apple","Pear","Coin","Juice","Gem"}[i]);
            machineSo.ApplyModifiedPropertiesWithoutUndo();
            var free=ApprovedDynamicText(root,"Content/ApprovedFreeCount",286,953,50,54,43);
            var freeLabel=ApprovedDynamicText(root,"Content/ApprovedFreeCaption",336,953,234,54,38);BindCopy(freeLabel,"FREE SPIN","GIRO GRÁTIS");
            var progress=ApprovedDynamicText(root,"Content/ApprovedSlotProgress",199,1416,454,55,45);
            Place(root,p.slotHintTxt.transform,77,1479,698,35);TextStyle(p.slotHintTxt,24);p.slotHintTxt.alignment=TextAlignmentOptions.Center;
            var adRoot=Ensure(root,"Content/ApprovedAdSpin");Place(root,adRoot,150,1523,551,133);var ad=adRoot.GetComponent<Button>()??adRoot.gameObject.AddComponent<Button>();OwnButton(ad,"ButtonBlue");
            var label=ApprovedDynamicText(root,"Content/ApprovedAdSpin/Label",190,1541,471,93,56);BindCopy(label,"Watch Video","Assistir vídeo");OrchardSkinAuthoring.SetTitle(label);
            var so=new SerializedObject(p);so.FindProperty("approvedFreeLabel").objectReferenceValue=free;
            // Legacy visual pass inputs; FinalizeSpinReference disables this added footer.
            for(int i=0;i<5;i++){var t=Ensure(root,"Content/ApprovedCheck"+i);var im=t.GetComponent<Image>()??t.gameObject.AddComponent<Image>();im.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/BizzaWZ/Final/BizzaGame/Z_ReplaceAssets/UI_Frame/RealWithdrawPanel/Icon_ChannelSelected.png");im.preserveAspect=true;im.raycastTarget=false;Place(root,t,107+i*143,1339,68,68);}
            so.ApplyModifiedPropertiesWithoutUndo();
            p.slotBtn.transform.SetAsLastSibling();
        }
        if(name=="lucky-help")
        {
            var list=Need(root,"Content/bg2 (1)/root");var l=list.GetComponent<VerticalLayoutGroup>();l.enabled=false;
            int row=0;foreach(Transform t in list)
            {
                int rowIndex=row++;LocalRect(t,0,rowIndex*122,718,111);
                var bg=t.GetComponent<Image>()??t.gameObject.AddComponent<Image>();Paint(bg,"Input");bg.raycastTarget=false;
                foreach(var tx in t.GetComponentsInChildren<TMP_Text>(true)){TextStyle(tx,29);}
                string[] symbols={"Apple","Pear","Coin","Juice","Gem"};
                for(int i=0;i<3;i++)
                {var cell=t.Find("Slot_1"+(i==0?"":" ("+i+")"));LocalRect(cell,8+i*117,4,106,101);Paint(cell.GetComponent<Image>(),"Input");
                 var icon=cell.Find("Content").GetComponent<Image>();icon.sprite=NamedSprite("Assets/OrchardUI/Art/OrchardReelSymbols.png",symbols[rowIndex<5?rowIndex:i]);icon.preserveAspect=true;LocalRect(icon.transform,12,10,82,81);}
                foreach(Transform child in t)
                {
                    if(child.name.StartsWith("que",System.StringComparison.Ordinal))LocalText(child,365,25,45,60,36);
                    if(child.name.StartsWith("des",System.StringComparison.Ordinal))LocalText(child,539,16,168,79,30);
                    if(child.name=="Slot_1 (4)"||child.name=="Slot_1 (3)")LocalRect(child,426,20,100,77);
                    if(child.name.StartsWith("Image",System.StringComparison.Ordinal))child.GetComponent<Image>().enabled=false;
                }
            }
        }
#endif
        if(name=="new-booster")
        {
            var shadow = Need(root,"Shadow");
            shadow.GetComponent<Image>().enabled = true;
            StretchRect(shadow);
            Need(root,"MainContent/ApprovedPlaque").SetAsLastSibling();Need(root,"MainContent/Title").SetAsLastSibling();
            foreach(var b in root.GetComponentsInChildren<Orange.PopMidScale>(true))b.enabled=false;
            var claim=Need(root,"MainContent/ClaimBtn");var btn=claim.GetComponent<Button>()??claim.gameObject.AddComponent<Button>();OwnButton(btn,"ButtonGreen");
            var so=new SerializedObject(root.GetComponent<NewItemPop>());so.FindProperty("approvedClaimButton").objectReferenceValue=btn;so.ApplyModifiedPropertiesWithoutUndo();
            foreach(var graphic in claim.GetComponents<Graphic>())if(!(graphic is Image))graphic.raycastTarget=false;
            Need(root,"MainContent/ClaimBtn/Image/LevelTextShadow").GetComponent<TMP_Text>().enabled=true;
            Need(root,"MainContent/ClaimBtn/Image/LevelTextShadow/LevelText").GetComponent<TMP_Text>().enabled=false;
        }
    }
    private static TMP_Text ApprovedDynamicText(GameObject root,string path,float x,float y,float w,float h,float size)
    {
        var t=Ensure(root,path);var tx=t.GetComponent<TMP_Text>()??t.gameObject.AddComponent<TextMeshProUGUI>();
        TextStyle(tx,size);tx.alignment=TextAlignmentOptions.Center;Place(root,t,x,y,w,h);return tx;
    }
    private static void BindCopy(TMP_Text text,string english,string portuguese)
    {
        var label=text.GetComponent<OrchardLocalizedLabel>()??text.gameObject.AddComponent<OrchardLocalizedLabel>();
        var so=new SerializedObject(label);so.FindProperty("target").objectReferenceValue=text;so.FindProperty("english").stringValue=english;so.FindProperty("portuguese").stringValue=portuguese;so.ApplyModifiedPropertiesWithoutUndo();text.text=portuguese;
    }
    private static void ClearAuthoredListeners(Button button)
    {
        var so=new SerializedObject(button);
        foreach(string path in new[]{"m_OnClick.m_PersistentCalls.m_Calls","onClick.m_PersistentCalls.m_Calls"})
        {var calls=so.FindProperty(path);if(calls!=null)calls.arraySize=0;}
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
