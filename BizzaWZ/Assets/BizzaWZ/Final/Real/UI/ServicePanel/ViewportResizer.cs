#if BIZZA_REAL_WITHDRAW
using UnityEngine;

public class ViewportResizer : MonoBehaviour
{
    public RectTransform viewportRect;
    public float bottomPadding = 300f;
    [SerializeField] private float minimumHeight = 80f;
    private Vector2 authoredMin, authoredMax;
    private bool captured;
    private readonly Vector3[] corners = new Vector3[4];
    private void Awake() { CaptureBounds(); }
    private void CaptureBounds()
    {
        if(captured || viewportRect==null)return;
        authoredMin=viewportRect.offsetMin;authoredMax=viewportRect.offsetMax;captured=true;
    }
    public void UpdateViewportBottom(float keyboardHeight)
    {
        CaptureBounds();if(!captured)return;
        viewportRect.offsetMin=authoredMin;viewportRect.offsetMax=authoredMax;
        if(keyboardHeight<=0)return;
        viewportRect.GetWorldCorners(corners);
        var canvas=viewportRect.GetComponentInParent<Canvas>();
        var camera=canvas!=null&&canvas.renderMode!=RenderMode.ScreenSpaceOverlay?canvas.worldCamera:null;
        float bottom=RectTransformUtility.WorldToScreenPoint(camera,corners[0]).y;
        float top=RectTransformUtility.WorldToScreenPoint(camera,corners[1]).y;
        float pixelsPerUnit=(top-bottom)/Mathf.Max(1f,viewportRect.rect.height);
        float inset=Mathf.Max(0,keyboardHeight+bottomPadding-bottom)/Mathf.Max(.001f,pixelsPerUnit);
        inset=Mathf.Min(inset,Mathf.Max(0,viewportRect.rect.height-minimumHeight));
        viewportRect.offsetMin=authoredMin+new Vector2(0,inset);
    }
}
#endif
