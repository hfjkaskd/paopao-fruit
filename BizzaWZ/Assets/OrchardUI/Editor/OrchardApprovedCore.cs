using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static partial class OrchardApprovedPass
{
    private static void ApplyPreservedPage(GameObject root,string name)
    {
        if(name=="loading")
        {
            var art=Need(root,"OrchardLoadingArtwork");var image=art.GetComponent<Image>();image.sprite=null;image.enabled=true;
            art.GetComponent<AspectRatioFitter>().aspectRatio=853f/1844f;
            var component=art.GetComponent<OrchardLoadingArt>();var so=new SerializedObject(component);
            so.FindProperty("resourcePath").stringValue="OrchardUI/ApprovedLoading";
            var bindings=so.FindProperty("bindings");bindings.arraySize=1;
            bindings.GetArrayElementAtIndex(0).FindPropertyRelative("target").objectReferenceValue=image;
            bindings.GetArrayElementAtIndex(0).FindPropertyRelative("spriteName").stringValue="ApprovedLoading";
            so.ApplyModifiedPropertiesWithoutUndo();
            Need(root,"OrchardLoadingArtwork/EmptyLeftCap").GetComponent<Image>().enabled=false;
            var track=Need(root,"OrchardLoadingArtwork/EmptyTrack");Paint(track.GetComponent<Image>(),"ProgressTrack");NormalizedRect(track,73,1452,706,93);
            var trackRoot=Need(root,"OrchardLoadingArtwork/LoadingProgressTrack");NormalizedRect(trackRoot,94,1468,665,61);
            var fill=Need(root,"OrchardLoadingArtwork/LoadingProgressTrack/LoadingProgress");Paint(fill.GetComponent<Image>(),"ButtonGreen");StretchRect(fill);
            var label=Ensure(root,"OrchardLoadingArtwork/ApprovedLoadingLabel");var tx=label.GetComponent<TMP_Text>()??label.gameObject.AddComponent<TextMeshProUGUI>();TextStyle(tx,33);tx.text="Loading...";tx.alignment=TextAlignmentOptions.Center;NormalizedRect(label,257,1545,338,51);
        }
        else if(name=="game-hud")
        {
            foreach(var tx in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if(tx.name.StartsWith("DollarAdd"))continue;
                float size=tx.fontSize;bool heading=tx.color.r>.9f&&tx.color.g>.9f&&tx.color.b>.9f;TextStyle(tx,size,heading);tx.alignment=TextAlignmentOptions.Center;
            }
            var settings=Need(root,"CurrencyBar/PauseButton");Paint(settings.GetComponent<Image>(),"nav:Settings");Need(root,"CurrencyBar/PauseButton/Icon").GetComponent<Image>().enabled=false;
            var slot=(RectTransform)Need(root,"SlotEnter");slot.anchorMin=slot.anchorMax=new Vector2(.13f,0);slot.pivot=new Vector2(.5f,.5f);slot.anchoredPosition=new Vector2(0,222);
            const string corePath="Assets/FruitsHarvest/Resources/Original/res/local/coreplay/CorePlayUI.prefab";
            var core=PrefabUtility.LoadPrefabContents(corePath);
            try
            {
                string[] names={"Undo","Magic","Shuffle"};
                for(int i=0;i<names.Length;i++)
                {
                    var button=(RectTransform)Need(core,"UI/Bottom/"+names[i]);button.anchorMin=button.anchorMax=new Vector2(.37f+i*.23f,0);button.anchoredPosition=new Vector2(0,222);
                }
                var tray=(RectTransform)Need(core,"UI/Bottom/Box_Root");tray.anchoredPosition=new Vector2(tray.anchoredPosition.x,490);
                PrefabUtility.SaveAsPrefabAsset(core,corePath);
            }
            finally{PrefabUtility.UnloadPrefabContents(core);}
            BindMissingButtonVisuals(root);
        }
        else if(name=="tutorial")
        {
            var guide = new SerializedObject(root.GetComponent<NewPlayerGuider>());
            var guideText = (TMP_Text)guide.FindProperty("m_GuideText").objectReferenceValue;
            var group=guideText.transform.parent;var image=group.GetComponent<Image>()??group.gameObject.AddComponent<Image>();Paint(image,"Input");image.raycastTarget=false;
            foreach(var tx in group.GetComponentsInChildren<TMP_Text>(true)){TextStyle(tx,44);tx.alignment=TextAlignmentOptions.Center;}
            foreach(var im in group.GetComponentsInChildren<Image>(true))if(im!=image)im.enabled=false;
            PaintReferenceDetail(image,"GuideBubble");image.type=Image.Type.Simple;image.preserveAspect=false;
            var bubble=(RectTransform)group;bubble.anchorMin=bubble.anchorMax=bubble.pivot=new Vector2(.5f,.5f);bubble.anchoredPosition=new Vector2(60,633);bubble.sizeDelta=new Vector2(420,160);
            LocalRect(guideText.transform,85,15,317,115);SizeText(guideText,43);
            var number=Ensure(root,"Bg/MainContent/GuideText/Image/ApprovedNumber");var numberImage=number.GetComponent<Image>()??number.gameObject.AddComponent<Image>();PaintReferenceDetail(numberImage,"BlueDisc");LocalRect(number,16,25,65,65);numberImage.raycastTarget=false;
            var one=Ensure(root,"Bg/MainContent/GuideText/Image/ApprovedNumber/Label");var oneText=one.GetComponent<TMP_Text>()??one.gameObject.AddComponent<TextMeshProUGUI>();TextStyle(oneText,51,true);oneText.text="1";oneText.alignment=TextAlignmentOptions.Center;StretchRect(one);
            var footer=Ensure(root,"Bg/MainContent/GuideText/ApprovedFooter");var footerImage=footer.GetComponent<Image>()??footer.gameObject.AddComponent<Image>();Paint(footerImage,"Input");footerImage.raycastTarget=false;
            var rect=(RectTransform)footer;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=new Vector2(0,-364);rect.sizeDelta=new Vector2(490,81);
            var footerLabel=Ensure(root,"Bg/MainContent/GuideText/ApprovedFooter/Label");var text=footerLabel.GetComponent<TMP_Text>()??footerLabel.gameObject.AddComponent<TextMeshProUGUI>();TextStyle(text,39);text.text="Match all fruits to win!";text.alignment=TextAlignmentOptions.Center;StretchRect(footerLabel);
            guide.FindProperty("phaseOneBubble").objectReferenceValue=group.gameObject;guide.FindProperty("secondaryGuideText").objectReferenceValue=text;guide.ApplyModifiedPropertiesWithoutUndo();
            var finger=Need(root,"Bg/MainContent/GuideFinger/GuideFinger/bigfinger/bigfinger (1)").GetComponent<Image>();PaintReferenceDetail(finger,"Glove");
        }
    }
    private static void NormalizedRect(Transform t,float x,float y,float w,float h)
    {
        var r=(RectTransform)t;r.anchorMin=new Vector2(x/853f,1-(y+h)/1844f);r.anchorMax=new Vector2((x+w)/853f,1-y/1844f);r.offsetMin=r.offsetMax=Vector2.zero;r.localScale=Vector3.one;
    }
}
