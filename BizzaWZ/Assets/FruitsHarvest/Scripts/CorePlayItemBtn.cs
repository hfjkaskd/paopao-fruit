using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Original prefab presentation backed by the framework inventory and prop use flow.</summary>
public sealed class CorePlayItemBtn : MonoBehaviour
{
    [SerializeField] private Image m_ItemBG;
    [SerializeField] private Image m_ItemIcon;
    [SerializeField] private GameObject m_UseEff;
    [SerializeField] private Animation m_BtnAnim;
    [SerializeField] private GameObject m_OnlyUnlockIcon;
    [SerializeField] private GameObject m_LockIcon;
    [SerializeField] private GameObject m_UseCoin;
    [SerializeField] private GameObject m_WatchAD;
    [SerializeField] private GameObject m_UseDirectly;
    [SerializeField] private TextMeshProCustom m_LeftItemNum;
    [SerializeField] private UIPropEntry entry;
    [SerializeField] private InputMono input;
    [SerializeField] private Image outerFrame;
    private POJCEPBNNIP itemType;
    private Sprite normalIcon, lockedIcon, normalBackground, lockedBackground, frameSprite;

    public void Init(POJCEPBNNIP type)
    {
        itemType = type;
        string prefix;
        switch (type)
        {
            case POJCEPBNNIP.Undo: prefix = "Undo"; break;
            case POJCEPBNNIP.Magic: prefix = "Magic"; break;
            case POJCEPBNNIP.Shuffle: prefix = "Shuffle"; break;
            default: prefix = null; break;
        }
        if (prefix != null)
        {
            normalIcon = GameRes.LoadSprite("res/local/coreplay/sprite/item/" + prefix + "_Normal");
            lockedIcon = GameRes.LoadSprite("res/local/coreplay/sprite/item/" + prefix + "_Lock");
            normalBackground = GameRes.LoadSprite("res/local/coreplay/sprite/item/ItemBg_Normal");
            lockedBackground = GameRes.LoadSprite("res/local/coreplay/sprite/item/ItemBg_Lock");
            frameSprite = GameRes.LoadSprite("res/local/coreplay/sprite/item/ItemBg_Gray");
        }
        input.onClick = OnClick;
        entry.Init(PropConfigSO.Instance.GetPropConfigInfo(HarvestBridge.MapProp(type)));
    }

    private void OnClick(GameObject clicked)
    {
        if (!HarvestBridge.Ready || TransparentBlock.IsBlock) return;
        entry.OnClickProp();
    }

    public void Refresh()
    {
        if (entry != null && entry.PropConfigInfo != null)
            entry.Init(entry.PropConfigInfo);
    }

    public void ShowState(bool unlocked, float count)
    {
        if (m_LockIcon != null) m_LockIcon.SetActive(!unlocked);
        if (m_OnlyUnlockIcon != null) m_OnlyUnlockIcon.SetActive(unlocked);
        if (m_UseDirectly != null) m_UseDirectly.SetActive(unlocked && count > 0);
        if (m_UseCoin != null) m_UseCoin.SetActive(false);
        if (m_WatchAD != null) m_WatchAD.SetActive(unlocked && count <= 0);
        if (m_LeftItemNum != null) m_LeftItemNum.text = count.ToString();
        if (itemType == POJCEPBNNIP.Extra) return;
        if (outerFrame != null)
        {
            outerFrame.enabled = unlocked;
            if (frameSprite != null) outerFrame.sprite = frameSprite;
        }
        if (m_ItemBG != null)
        {
            m_ItemBG.sprite = unlocked ? normalBackground : lockedBackground;
            m_ItemBG.enabled = true;
        }
        if (m_ItemIcon != null)
        {
            m_ItemIcon.sprite = unlocked ? normalIcon : lockedIcon;
            m_ItemIcon.SetNativeSize();
        }
    }

    public void PlayUnlockEffect()
    {
        if (m_UseEff != null) { m_UseEff.SetActive(false); m_UseEff.SetActive(true); }
    }

    public void PlayAddOneLockAnim()
    {
        if (m_BtnAnim != null && m_BtnAnim["anim_AddOneBtn_lock"] != null)
            m_BtnAnim.Play("anim_AddOneBtn_lock");
    }
}


