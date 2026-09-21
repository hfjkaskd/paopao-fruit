from pathlib import Path
import re,json
ROOT=Path(r'C:\Projects\paopao\BizzaWZ\Assets\FruitsHarvest')
S=ROOT/'Scripts'
def replace_method(path, signature, body):
    p=S/path;t=p.read_text(encoding='utf-8-sig'); start=t.index(signature); a=t.index('{',start); depth=1;b=a+1
    while depth:
        if t[b]=='{': depth+=1
        elif t[b]=='}': depth-=1
        b+=1
    p.write_text(t[:a]+'{\n'+body+'\n\t}'+t[b:],encoding='utf-8')
def edit(path,old,new):
    p=S/path;t=p.read_text(encoding='utf-8-sig');assert old in t,(path,old);p.write_text(t.replace(old,new),encoding='utf-8')

# Remove the old economy from the active gameplay UI; prefab references are cleared in the editor setup.
replace_method('CorePlay/CorePlayUI.cs','private void StartLevelFlow()', '\t\t\tHarvestBridge.StartLevel(this);')
edit('CorePlay/CorePlayUI.cs','m_CommonCoinBtn.Init(FCCMMLGODPL.ShopinPlay, true);','m_CommonCoinBtn.gameObject.SetActive(false);')
edit('CorePlay/CorePlayUI.cs','m_CommonCoinBtn.gameObject.SetActive(JEFOMCDAPGK.Instance.CurMainLevelIndex > 1);','m_CommonCoinBtn.gameObject.SetActive(false);')
edit('CorePlay/CorePlayUI.cs','m_CommonCoinBtn.AddCurCommonCoinBtn();','')
edit('CorePlay/CorePlayUI.cs','MgrUI.Instance.Open("pops/setting/GameSettingUI");','HarvestBridge.OpenSettings();')
edit('CorePlay/CorePlayUI.cs','JEFOMCDAPGK.Instance.CurMainLevelIndex != 1','SaveDataUtils.GameData.customTutorialEnd || NewPlayerGuider.Instance != null')
replace_method('CorePlay/CorePlayUI.cs','public Vector3 GetItemBtnWorldPosition(POJCEPBNNIP itemType)', '\t\t\treturn HarvestBridge.GetPropPosition(itemType);')
edit('EDLHEMMBABM.cs','bool firstEnterLevel = !JEFOMCDAPGK.Instance.Data.hasEnterLevel;','bool firstEnterLevel = !SaveDataUtils.GameData.customTutorialEnd;')
edit('EDLHEMMBABM.cs','if (UI != null && firstEnterLevel)','if (false && UI != null && firstEnterLevel)')
# Tutorial begins only when the existing framework graph has yielded its input blocker.
edit('EDLHEMMBABM.cs','\t\tWinUI.wonLevel = JEFOMCDAPGK.Instance.CurMainLevelIndex;\n\t\tWinUI.prevBoxProgress = FPFGGCMEDND.Instance.CurLevelBoxProgress;\n\t\tWinUI.boxIndexAtWin = FPFGGCMEDND.Instance.NextLevelBoxIndex;\n\t\tJEFOMCDAPGK.Instance.LevelWin();\n\t\tFPFGGCMEDND.Instance.OnLevelPass();\n\t\tWinUI.boxCompleted = FPFGGCMEDND.Instance.CurLevelBoxProgress == 0;\n\t\tWinUI.newBoxProgress = WinUI.boxCompleted ? FPFGGCMEDND.LEVELS_PER_BOX : FPFGGCMEDND.Instance.CurLevelBoxProgress;', '\t\tHarvestBridge.CompleteTutorial();')
replace_method('EDLHEMMBABM.cs','private void OpenWinUI()', '\t\tHarvestBridge.Win();')
edit('EDLHEMMBABM.cs','MgrUI.Instance.Open("coreplaywin/LoseUI");','HarvestBridge.Lose();')
edit('EDLHEMMBABM.cs','\t\t\tCheckGameResult();\n\t\t});', '\t\t\tFlowModule.SynthesisLogic(Mathf.Max(0, (TotalItems.Count + CountInCollect()) / 3));\n\t\t\tCheckGameResult();\n\t\t});')
edit('EDLHEMMBABM.cs','if (JEFOMCDAPGK.Instance.Data.isLevelFinished)\n\t\t{\n\t\t\treturn;\n\t\t}\n\t\tMainLevelData.isLose', 'if (JEFOMCDAPGK.Instance.Data.isLevelFinished || MainLevelData.isLose)\n\t\t{\n\t\t\treturn;\n\t\t}\n\t\tMainLevelData.isLose')
# Generic original model storage is now a field in the framework save, no independent file/PlayerPrefs economy.
replace_method('IAJDGNOGFGO.cs','public virtual void Init()', '''
        string value = HarvestBridge.ReadModel(GetKey());
        data = string.IsNullOrEmpty(value) ? null : JsonUtility.FromJson<DataT>(value);
        if (data == null) { data = new DataT(); isFirstInit = true; AfterFirstInitData(); }
''')
replace_method('IAJDGNOGFGO.cs','public bool TrySaveToDisk()', '''
        if (data == null) return false;
        HarvestBridge.WriteModel(GetKey(), JsonUtility.ToJson(data));
        needSave = false;
        return true;
''')
# Replace reflection-based audio ID and language registration with direct references.
p=S/'GameAudio.cs';t=p.read_text(encoding='utf-8-sig');t=t.replace('using System.Reflection;','');p.write_text(t,encoding='utf-8')
fields=re.findall(r'public static uint (\w+)',(S/'DLMJOHCOJKN.cs').read_text())
replace_method('GameAudio.cs','private static void RegisterEventIds()', '\n'.join(f'        DLMJOHCOJKN.{name} = {i}u; idToClip[{i}u] = "{name.split("_",1)[-1]}";' for i,name in enumerate(fields,1)))
p=S/'OJEEJGGLNPC.cs';t=p.read_text(encoding='utf-8-sig');t=t.replace('using System.Reflection;','');a=t.index('\t\tFieldInfo[]');b=t.index('\t\tClearTextConfigList();',a)
langs=re.findall(r'public string (\w+)',(S/'Project/DictData/TextConfig.cs').read_text())
t=t[:a]+'''        foreach (TextConfig config in textConfigList) {
            if (string.IsNullOrEmpty(config.key)) continue;
            var lanDict = new Dictionary<EDMAJFIKJLE, string>();
'''+''.join(f'            lanDict[EDMAJFIKJLE.{x}] = config.{x};\n' for x in langs if x not in ('key','des'))+'''            allLanDict[config.key] = lanDict;
        }
'''+t[b:];p.write_text(t,encoding='utf-8')
# All original sound effects and ambience obey framework settings; existing framework BGM stays singular.
edit('GameAudio.cs','!inited || !SfxEnabled','!inited || !SaveDataUtils.SettingData.enableSound')
replace_method('GameAudio.cs','public static void PlayMusic(string clipName = "music")','        SoundManager.Instance.PlayBGM("SFX_BGM");')
# Lazy catalog: paths only, never Object references to every texture/audio asset.
catalog=Path(r'C:\Projects\FruitsHarvestMaster\output_Unity\Assets\Resources\GameResCatalog.asset').read_text()
manifest=json.loads(Path(__file__).with_name('import-manifest.json').read_text(encoding='utf-8'))
byguid={x['guid']:x for x in manifest if 'target' in x}
rows=[]
for key,guid in re.findall(r'- key: (.*?)\n\s+asset: \{fileID: -?\d+, guid: (\w+), type: \d+\}',catalog):
    entry=byguid.get(guid)
    if entry and entry['target'].startswith('Resources/'):
        resource=Path(entry['target']).as_posix()[len('Resources/'):]
        resource=resource[:resource.rfind('.')]
        rows.append(key+'\t'+resource)
(ROOT/'Resources/HarvestPaths.txt').write_text('\n'.join(rows),encoding='utf-8')
(S/'GameResCatalog.cs').write_text('''using System.Collections.Generic;
using UnityEngine;
public sealed class GameResCatalog {
    private static GameResCatalog instance;
    public static GameResCatalog Instance => instance ?? (instance = new GameResCatalog());
    private readonly Dictionary<string,string> paths = new Dictionary<string,string>();
    private GameResCatalog() {
        var text = Resources.Load<TextAsset>("HarvestPaths");
        if (text == null) throw new System.InvalidOperationException("HarvestPaths is missing");
        foreach (var row in text.text.Split('\\n')) {
            int tab = row.IndexOf('\\t'); if (tab > 0) paths[row.Substring(0,tab)] = row.Substring(tab+1).Trim();
        }
    }
    public Object Get(string key) {
        if (!paths.TryGetValue(key.ToLowerInvariant(),out var path)) return null;
        int hash = key.IndexOf('#');
        if (hash >= 0) {
            string name = key.Substring(hash+1);
            foreach (var asset in Resources.LoadAll<Object>(path))
                if (asset.name.ToLowerInvariant() == name) return asset;
        }
        return Resources.Load<Object>(path);
    }
    public bool Contains(string key) => paths.ContainsKey(key.ToLowerInvariant());
}
''',encoding='utf-8')
print('Adapted gameplay, save and lazy resource directory; entries:',len(rows))
