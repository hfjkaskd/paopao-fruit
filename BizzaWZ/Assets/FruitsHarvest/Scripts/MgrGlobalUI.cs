using UnityEngine;
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
	public void ShowGlobalText(string InShowText, FFMLGGBCOOO InGlobalTip)
	{
		if (GlobalText == null)
		{
			LoadGlobalText();
		}
		if (GlobalText == null)
		{
			return;
		}
		TextMeshProUGUI text = GlobalText.GetComponentInChildren<TextMeshProUGUI>(true);
		if (text != null)
		{
			text.text = InShowText;
		}
		GlobalText.SetActive(true);
		Animation ani = GlobalText.GetComponent<Animation>();
		float duration = 1.5f;
		if (ani != null && ani["GlobalText_in"] != null)
		{
			ani.Play("GlobalText_in");
			duration = Mathf.Max(duration, ani["GlobalText_in"].length);
		}
		switch (InGlobalTip)
		{
		case FFMLGGBCOOO.positive:
			GameAudio.Play(DLMJOHCOJKN.Play_sfx_notice_common_positive);
			break;
		case FFMLGGBCOOO.negative:
			GameAudio.Play(DLMJOHCOJKN.Play_sfx_notice_common_negative);
			break;
		default:
			GameAudio.Play(DLMJOHCOJKN.Play_sfx_notice_common_neutral);
			break;
		}
		int cnt = ++showGlobalTextCnt;
		Timer.Instance.Delay(duration, delegate
		{
			if (cnt == showGlobalTextCnt && GlobalText != null)
			{
				GlobalText.SetActive(false);
			}
		});
	}

}