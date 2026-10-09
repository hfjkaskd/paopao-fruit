using System.Collections;
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
    [SerializeField] private RectTransform clickFeedbackTarget;
    private POJCEPBNNIP itemType;
    private Sprite normalIcon, lockedIcon, normalBackground, lockedBackground, frameSprite;
    private Vector3 clickFeedbackBaseScale;
    private Coroutine clickFeedbackRoutine;
    private bool clickFeedbackReady;
    private bool clickFeedbackPressed;

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
        input.onDown = OnClickFeedbackDown;
        input.onUp = OnClickFeedbackUp;
        if (clickFeedbackTarget != null && !clickFeedbackReady)
        {
            clickFeedbackBaseScale = clickFeedbackTarget.localScale;
            clickFeedbackReady = true;
        }
        entry.Init(PropConfigSO.Instance.GetPropConfigInfo(HarvestBridge.MapProp(type)));
    }

    private void OnClickFeedbackDown(GameObject clicked)
    {
        if (!clickFeedbackReady || !HarvestBridge.Ready || TransparentBlock.IsBlock) return;
        clickFeedbackPressed = true;
        StopClickFeedbackRoutine();
        clickFeedbackRoutine = StartCoroutine(TweenClickFeedback(clickFeedbackBaseScale * 0.94f, 0.08f));
    }

    private void OnClickFeedbackUp(GameObject clicked)
    {
        if (!clickFeedbackPressed) return;
        clickFeedbackPressed = false;
        StopClickFeedbackRoutine();
        clickFeedbackRoutine = StartCoroutine(ReleaseClickFeedback());
    }

    private IEnumerator ReleaseClickFeedback()
    {
        yield return TweenClickFeedback(clickFeedbackBaseScale * 1.03f, 0.09f);
        yield return TweenClickFeedback(clickFeedbackBaseScale, 0.1f);
        clickFeedbackRoutine = null;
    }

    private IEnumerator TweenClickFeedback(Vector3 target, float duration)
    {
        Vector3 start = clickFeedbackTarget.localScale;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            clickFeedbackTarget.localScale = Vector3.Lerp(start, target, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        clickFeedbackTarget.localScale = target;
    }

    private void StopClickFeedbackRoutine()
    {
        if (clickFeedbackRoutine == null) return;
        StopCoroutine(clickFeedbackRoutine);
        clickFeedbackRoutine = null;
    }

    private void ResetClickFeedback()
    {
        clickFeedbackPressed = false;
        StopClickFeedbackRoutine();
        if (clickFeedbackReady) clickFeedbackTarget.localScale = clickFeedbackBaseScale;
    }

    private void OnDisable()
    {
        ResetClickFeedback();
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
            // Keep the prefab's icon size when swapping artwork of different resolutions.
            m_ItemIcon.sprite = unlocked ? normalIcon : lockedIcon;
        }
    }

    public void PlayUnlockEffect()
    {
        if (m_UseEff != null) { m_UseEff.SetActive(false); m_UseEff.SetActive(true); }
    }

    public void PlayAddOneLockAnim()
    {
        ResetClickFeedback();
        if (m_BtnAnim != null && m_BtnAnim["anim_AddOneBtn_lock"] != null)
            m_BtnAnim.Play("anim_AddOneBtn_lock");
    }
}


