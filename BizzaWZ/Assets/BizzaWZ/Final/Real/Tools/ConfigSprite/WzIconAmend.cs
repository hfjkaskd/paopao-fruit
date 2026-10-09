#if BIZZA_REAL_WITHDRAW
using cfg;
using UnityEngine;
using UnityEngine.UI;

[Obfuz.ObfuzIgnore]
[DisallowMultipleComponent]
public class WzIconAmend : MonoBehaviour
{
    public E_WzIconType iconType;
    public Image image;
    public bool isNativeSize = false;

    private void OnEnable()
    {
        BizzaEventSystem.On(EventDefine.Login.InitContentByCountry, UpdateContent);
        UpdateContent();
    }

    private void OnDisable()
    {
        BizzaEventSystem.Off(EventDefine.Login.InitContentByCountry, UpdateContent);
    }

    private void UpdateContent()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }
        if (Tables.Instance == null || Tables.Instance.TblCommonWzTexture == null 
            || Tables.Instance.TblCommonWzTexture.DataMap == null)
        {
            LogLogger.LogVerbose(BaseConst.LOG_Asset, "WzIconAmend - 未初始化");
            return;
        }
        if (image == null && TryGetComponent(out image) == false)
        {
            LogLogger.LogVerbose(BaseConst.LOG_Asset, "未设置图片");
            return;
        }

        WzCurrencySprites.Apply(image, WzCurrencySprites.RoleKey(iconType), isNativeSize);
    }


}

public static partial class EventDefine
{
    public static class Login
    {
        public static GameEvent InitContentByCountry = new();
    }
}
#endif
