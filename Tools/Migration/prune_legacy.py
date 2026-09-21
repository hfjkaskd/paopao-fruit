from pathlib import Path
import re
S=Path(r'C:\Projects\paopao\BizzaWZ\Assets\FruitsHarvest\Scripts')
def remove_blocks(t,signature):
    while signature in t:
        a=t.index(signature);b=t.index('{',a)+1;d=1
        while d:
            if t[b]=='{':d+=1
            if t[b]=='}':d-=1
            b+=1
        t=t[:a]+t[b:]
    return t
p=S/'CorePlay/CorePlayUI.cs';t=p.read_text(encoding='utf-8-sig')
t=remove_blocks(t,'if (m_CommonCoinBtn != null)')
t=t.replace('private CommonCoinBtn m_CommonCoinBtn;','private GameObject m_CommonCoinBtn;')
p.write_text(t,encoding='utf-8')
# Only the original unlock metadata remains relevant; inventory belongs to Bizza.
(S/'GNEJJHEDEBL.cs').write_text('''public sealed class GNEJJHEDEBL : FOLJNEPEKCA<GNEJJHEDEBL> {
    public int GetItemUnlockLevel(POJCEPBNNIP type) {
        var config = PropConfigSO.Instance.GetPropConfigInfo(HarvestBridge.MapProp(type));
        return config != null && config.unlockFunction ? config.unlockCondition.unlockLevel : 1;
    }
}
''',encoding='utf-8')
p=S/'HarvestBridge.cs';t=p.read_text();t=t.replace('            GNEJJHEDEBL.Instance.Init();\n','');p.write_text(t,encoding='utf-8')
# Delete the old inventory calls, retain only the original success effects and record.
p=S/'EDLHEMMBABM.cs';t=p.read_text(encoding='utf-8-sig')
t=re.sub(r'^\s*GNEJJHEDEBL\.Instance\.UseItem\([^\n]*\);','',t,flags=re.M)
p.write_text(t,encoding='utf-8')
(S/'CorePlayItemBtn.cs').write_text('''using UnityEngine;
// Retained original visual component for unlock/expansion animation references.
public sealed class CorePlayItemBtn : MonoBehaviour {
    [SerializeField] private GameObject m_UseEff;
    [SerializeField] private Animation m_BtnAnim;
    private POJCEPBNNIP itemType;
    public void Init(POJCEPBNNIP type) { itemType=type; Refresh(); }
    public void Refresh() { gameObject.SetActive(false); }
    public void PlayUnlockEffect() { if(m_UseEff!=null) { m_UseEff.SetActive(false); m_UseEff.SetActive(true); } }
    public void PlayAddOneLockAnim() { if(m_BtnAnim!=null) m_BtnAnim.Play("anim_AddOneBtn_lock"); }
}
''',encoding='utf-8')
p=S/'MgrGlobalUI.cs';t=p.read_text(encoding='utf-8-sig');a=t.index('\tpublic void ShowGlobalText(');b=t.index('\n\tpublic void ShowSwitchBgLoading()',a)
method=t[a:b]
prefix='''using UnityEngine;
using TMPro;
public sealed class MgrGlobalUI : MonoBehaviour {
    public static MgrGlobalUI Instance { get; private set; }
    [SerializeField] public Transform gameObjectPoolRoot;
    private GameObject GlobalText;
    private int showGlobalTextCnt;
    public void Init() { Instance=this; LoadGlobalText(); }
    private void LoadGlobalText() {
        if(GlobalText!=null) return;
        var prefab=GameRes.LoadPrefab("res/local/globalui/GlobalText");
        if(prefab==null) throw new System.InvalidOperationException("Missing original text tip prefab");
        GlobalText=Instantiate(prefab,transform,false); GlobalText.SetActive(false);
    }
'''
p.write_text(prefix+method+'\n}',encoding='utf-8')
# Editor setup strips the original entry/loading components as missing scripts, without executing them.
p=S.parent/'Editor/HarvestSetup.cs';t=p.read_text();t=t.replace('var entry=host.GetComponentInChildren<GameEntry>(true);\n        if(entry != null) UnityEngine.Object.DestroyImmediate(entry);','foreach(var node in host.GetComponentsInChildren<Transform>(true)) GameObjectUtility.RemoveMonoBehavioursWithMissingScript(node.gameObject);')
t=t.replace('var original=visual.GetComponent<GameLoadingUI>();\n        if(original!=null) UnityEngine.Object.DestroyImmediate(original);','GameObjectUtility.RemoveMonoBehavioursWithMissingScript(visual);')
p.write_text(t,encoding='utf-8')
texts={p:p.read_text(encoding='utf-8-sig') for p in S.rglob('*.cs')}
types={}
for p,t in texts.items():
    # Include delegates as actual dependencies; ignore comments in the reachability walk.
    for name in re.findall(r'\b(?:class|struct|enum|interface)\s+(\w+)|\bdelegate\s+\w+\s+(\w+)',t):
        n=name[0] or name[1]; types.setdefault(n,set()).add(p)
seed={'HarvestBridge','HarvestRoot','EDLHEMMBABM','CorePlayUI','CollectItem','MgrUI','MgrGlobalUI','NewItemPop','NewPlayerGuider',
      'InputMono','InputMonoNoDrag','MyTweenCenter','MyTweenEase','TextMeshProCustom','CommonProgressBar',
      'DynamicCanvasLayer','DynamicParticleLayer','ProtectedAreaAdapt','ProtectedAreaAdaptReverse','SafeAreaSim','GameRes','GameResCatalog'}
keep=set();pending=set().union(*(types.get(x,set()) for x in seed))
while pending:
    p=pending.pop(); keep.add(p)
    t=re.sub(r'//[^\n]*|/\*[\s\S]*?\*/|"(?:\\.|[^"\\])*"','',texts[p])
    for word in set(re.findall(r'\b\w+\b',t)):
        pending.update(types.get(word,set())-keep)
removed=[]
for p in texts.keys()-keep:
    resolved=p.resolve()
    assert resolved.is_relative_to(S.resolve())
    removed.append(str(p.relative_to(S)))
    p.unlink()
    meta=Path(str(p)+'.meta')
    if meta.exists():meta.unlink()
Path(__file__).with_name('excluded-legacy-code.txt').write_text('\n'.join(sorted(removed)),encoding='utf-8')
print('Kept',len(keep),'gameplay scripts; excluded',len(removed),'legacy code files')
