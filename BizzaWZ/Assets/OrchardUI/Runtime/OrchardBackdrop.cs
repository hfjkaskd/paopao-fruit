using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Loads the one shared menu backdrop on demand on every platform.</summary>
[DisallowMultipleComponent]
public sealed class OrchardBackdrop : MonoBehaviour
{
    [SerializeField] private Image target;
    [SerializeField] private string resourcePath = "OrchardUI/Backdrop";

    // All framework pages use the same background; keep a single loaded copy.
    private static Sprite sharedSprite;
    private static ResourceRequest sharedRequest;
    private Coroutine loading;

    private void OnEnable()
    {
        if (target == null) return;
        if (sharedSprite != null)
        {
            Show(sharedSprite);
            return;
        }
        target.enabled = false;
        loading = StartCoroutine(Load());
    }

    private IEnumerator Load()
    {
        if (sharedRequest == null) sharedRequest = Resources.LoadAsync<Sprite>(resourcePath);
        yield return sharedRequest;
        if (sharedSprite == null) sharedSprite = sharedRequest.asset as Sprite;
        loading = null;
        if (sharedSprite == null)
        {
            Debug.LogError("Orchard menu backdrop could not be loaded from Resources/" + resourcePath, this);
            yield break;
        }
        Show(sharedSprite);
    }

    private void Show(Sprite sprite)
    {
        target.sprite = sprite;
        target.color = Color.white;
        target.enabled = true;
    }

    private void OnDisable()
    {
        if (loading != null)
        {
            StopCoroutine(loading);
            loading = null;
        }
    }
}
