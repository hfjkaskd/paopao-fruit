using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Bizza.Sdk;

// Read-only runtime observations and explicit editor commands for reproducible validation.
[InitializeOnLoad]
public static class HarvestValidation
{
    private const string Folder="../Tools/Migration/";
    private static double next;
    static HarvestValidation() { EditorApplication.update += Tick; }
    private static void Tick()
    {
        if(EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if(File.Exists(Folder+"editor.command"))
        {
            string command=File.ReadAllText(Folder+"editor.command").Trim();
            File.Delete(Folder+"editor.command");
            try
            {
                if(command=="play")
                {
                    if(EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before recompiling");
                    EditorSceneManager.OpenScene("Assets/Game/Resources/Scenes/InitWZ.unity");
                    EditorApplication.isPlaying=true;
                }
                else if(command=="stop") EditorApplication.isPlaying=false;
                else if(command=="setup") HarvestSetup.Build();
                else if(command=="verify-difficulty-cycle")
                {
                    if(!HarvestBridge.Ready) throw new InvalidOperationException("Wait for gameplay to load");
                    var levels=PLMIHDHFAAL.Instance;
                    var report=new StringBuilder();
                    for(int level=1;level<=15;level++)
                    {
                        string id=levels.GetLevelListByLevelNum(level)[0];
                        var original=levels.GetLevelConfig((long.Parse(id)/100).ToString(),id);
                        var config=HarvestDifficulty.Apply(original,level);
                        if(config==null) throw new InvalidOperationException("Missing level "+level);
                        if(config.normalTiles.Count!=original.normalTiles.Count) throw new InvalidOperationException("Tile count changed");
                        for(int i=0;i<config.normalTiles.Count;i++)
                        {
                            var before=original.normalTiles[i]; var after=config.normalTiles[i];
                            if(before.posX!=after.posX || before.posY!=after.posY || before.layerIndex!=after.layerIndex || before.tileID!=after.tileID)
                                throw new InvalidOperationException("Original layout changed");
                        }
                        var counts=config.normalTiles.GroupBy(tile=>tile.eleType);
                        if(counts.Any(group=>group.Count()%3!=0)) throw new InvalidOperationException("Unmatched fruit in level "+level);
                        report.AppendLine(level+": id="+config.levelID+" types="+config.eleClassCount+" tiles="+config.normalTiles.Count+" originalTiles="+original.normalTiles.Count+" layers="+(config.normalTiles.Max(tile=>tile.layerIndex)+1)+" interProtected="+HarvestDifficulty.ProtectInterstitial(level));
                    }
                    File.WriteAllText(Folder+"difficulty-cycle-validation.txt",report.ToString());
                    int savedCloseCount=NumbericalStatistics.CloseGetRewardNum;
                    try
                    {
                        for(int protectedLevel=1;protectedLevel<=3;protectedLevel++)
                        {
                            NumbericalStatistics.CloseGetRewardNum=100;
                            for(int close=0;close<10;close++)
                                if(NumbericalStatistics.CheckCloseGetReward(E_AdPos.GetReward,0,null,protectedLevel))
                                    throw new InvalidOperationException("Interstitial escaped protection");
                            if(NumbericalStatistics.CloseGetRewardNum!=0) throw new InvalidOperationException("Protected closes accumulated");
                        }
                        if(HarvestDifficulty.ProtectInterstitial(4)) throw new InvalidOperationException("Level four still protected");
                        File.WriteAllText(Folder+"intro-interstitial-validation.txt","PASS: levels 1-3 reject 10 closes each, including existing over-threshold counts. Source level 3 stays protected when current level is higher. Protected closes do not carry over. Level 4 policy enabled.");
                    }
                    finally { NumbericalStatistics.CloseGetRewardNum=savedCloseCount; }
                }
                else if(command=="reward-interval-four")
                {
                    if(EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before editing release config");
                    const string path="Assets/StreamingAssets/ChannelConfig.bytes";
                    var original=File.ReadAllBytes(path);
                    var config=ChannelConfigBinarySerializer.Deserialize(original);
                    if(config.adStatisticsLevelRanges==null || config.adStatisticsLevelRanges.Count==0)
                        throw new InvalidOperationException("Missing reward interval ranges");
                    var report=new StringBuilder();
                    // Fill the uncovered range without changing its other default statistics.
                    int lastLevel=config.adStatisticsLevelRanges.Max(range=>range.EndLevel);
                    if(lastLevel<int.MaxValue)
                        config.adStatisticsLevelRanges.Add(new AdStatisticsLevelRange
                        {
                            StartLevel=lastLevel+1,
                            EndLevel=int.MaxValue,
                            Statistics=new GameAB_CustomData { ShowGetRewardCount=4 }
                        });
                    foreach(var range in config.adStatisticsLevelRanges)
                    {
                        report.AppendLine(range.StartLevel+"-"+range.EndLevel+": match="+range.Statistics.ShowGetRewardCount+" -> 4, inter="+range.Statistics.CloseGetRewardCount+" -> 3");
                        var statistics=range.Statistics;
                        statistics.ShowGetRewardCount=4;
                        statistics.CloseGetRewardCount=3;
                        range.Statistics=statistics;
                    }
                    var result=ChannelConfigBinarySerializer.Serialize(config);
                    var verified=ChannelConfigBinarySerializer.Deserialize(result);
                    foreach(var range in verified.adStatisticsLevelRanges)
                        if(range.Statistics.ShowGetRewardCount!=4 || range.Statistics.CloseGetRewardCount!=3) throw new InvalidOperationException("Reward interval verification failed");
                    if(!File.Exists(Folder+"ChannelConfig.before-reward-interval.bytes")) File.WriteAllBytes(Folder+"ChannelConfig.before-reward-interval.bytes",original);
                    File.WriteAllBytes(path,result);
                    File.WriteAllText(Folder+"reward-interval-change.txt",report.ToString());
                    AssetDatabase.Refresh();
                }
                else if(command=="ad-fail") ChannelConfig.Instance.real_CustomConfig.failOpenAd=true;
                else if(command=="ad-normal") ChannelConfig.Instance.real_CustomConfig.failOpenAd=false;
                else if(command=="new-profile")
                {
                    if(EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before resetting local test data");
                    var backup=new System.Collections.Generic.Dictionary<string,string>();
                    foreach(var key in new[]{"GameSaveData","TeachSaveData","ItemSaveData"})
                    {
                        backup[key]=PlayerPrefs.GetString(key,"");
                    }
                    Directory.CreateDirectory("C:/Projects/paopao-validation-private");
                    File.WriteAllText("C:/Projects/paopao-validation-private/profile-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".json",Newtonsoft.Json.JsonConvert.SerializeObject(backup));
                    foreach(var key in backup.Keys) PlayerPrefs.DeleteKey(key);
                    PlayerPrefs.Save();
                }
            }
            catch(Exception e) { Debug.LogException(e); }
        }
        if(EditorApplication.timeSinceStartup<next) return;
        next=EditorApplication.timeSinceStartup+2;
        var b=new StringBuilder();
        b.AppendLine("playing="+EditorApplication.isPlaying+" compiling="+EditorApplication.isCompiling);
        if(EditorApplication.isPlaying)
        {
            b.AppendLine("screen="+Screen.width+"x"+Screen.height+" block="+TransparentBlock.IsBlock+" ready="+HarvestBridge.Ready);
            b.AppendLine("inputLock="+MCCIJBJGMCK.IsLock()+" mouse="+Input.mousePosition);
            var events=UnityEngine.EventSystems.EventSystem.current;
            if(events!=null)
            {
                var hits=new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
                events.RaycastAll(new UnityEngine.EventSystems.PointerEventData(events){position=Input.mousePosition},hits);
                foreach(var hit in hits.Take(8))
                {
                    var handler=UnityEngine.EventSystems.ExecuteEvents.GetEventHandler<UnityEngine.EventSystems.IPointerClickHandler>(hit.gameObject);
                    b.AppendLine("pointerHit="+hit.gameObject.name+" canvas="+hit.sortingOrder+" module="+hit.module.name+" handler="+(handler!=null?handler.name:"none"));
                }
            }
            var data=SaveDataUtils.GameData;
            if(data!=null && ChannelConfig.Instance!=null)
                b.AppendLine("matchRewardProgress="+NumbericalStatistics.ShowGetRewardNum+" threshold="+NumbericalStatistics.ShowGetRewardCount+" interProgress="+NumbericalStatistics.CloseGetRewardNum+" interThreshold="+NumbericalStatistics.CloseGetRewardCount);
            if(data!=null) b.AppendLine("level="+data.playerSelectedLv+" guideReady="+data.customTutorialCanPlay+" guideEnd="+data.customTutorialEnd+" revive="+data.currentReviveCount);
            if(HarvestBridge.Ready)
            {
                var unlockData=JEFOMCDAPGK.Instance.Data;
                b.AppendLine("unlockPending="+unlockData.pendingNewItemPop+" completed="+string.Join(",",unlockData.hadShownNewItemPop));
                b.AppendLine("board="+EDLHEMMBABM.Instance.GetTotalItems().Count+" basket="+EDLHEMMBABM.Instance.GetCollectAreaList().Count+" teach01="+SaveDataUtils.TeachData.IsCompleted("Teach_01"));
                for(int i=24;i<=27;i++) b.AppendLine("stock="+i+":"+ItemUtils.GetItemCount((E_ItemType)i));
            }
            foreach(var c in UnityEngine.Object.FindObjectsOfType<Canvas>()) b.AppendLine("canvas="+c.name+" order="+c.sortingOrder+" override="+c.overrideSorting);
            foreach(var g in UnityEngine.Object.FindObjectsOfType<CanvasGroup>())
                if(!g.interactable) b.AppendLine("disabledGroup="+g.name+" parent="+g.transform.parent?.name+" block="+g.blocksRaycasts+" alpha="+g.alpha);
            foreach(var scroll in UnityEngine.Object.FindObjectsOfType<ScrollRect>())
                b.AppendLine("scroll="+scroll.name+" enabled="+scroll.isActiveAndEnabled+" content="+scroll.content.rect+" viewport="+scroll.viewport?.rect+" position="+scroll.verticalNormalizedPosition);
            foreach(var t in UnityEngine.Object.FindObjectsOfType<TMPro.TMP_Text>()) b.AppendLine("text="+t.name+": "+t.text);
            foreach(var btn in UnityEngine.Object.FindObjectsOfType<Button>())
            {
                var canvas=btn.GetComponentInParent<Canvas>();
                var camera=canvas!=null && canvas.renderMode!=RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
                b.AppendLine("button="+btn.name+" interactable="+btn.interactable+" screen="+RectTransformUtility.WorldToScreenPoint(camera,btn.transform.position));
            }
        }
        File.WriteAllText(Folder+"runtime-state.txt",b.ToString());
    }
}
