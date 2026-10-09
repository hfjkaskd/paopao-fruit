using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Loads the one shared menu backdrop on demand on every platform.</summary>
[DisallowMultipleComponent]
public sealed class OrchardBackdrop : MonoBehaviour
{
    [SerializeField] private Image target;
    [SerializeField] private string resourcePath = "OrchardUI/Backdrop";

    // Requests are shared by asset path. Reward pages and service pages have distinct artwork.
    private static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();
    private static readonly Dictionary<string, ResourceRequest> Requests = new Dictionary<string, ResourceRequest>();
    private Coroutine loading;

    private void OnEnable()
    {
        if (target == null) return;
        if (Sprites.TryGetValue(resourcePath, out var sharedSprite) && sharedSprite != null)
        {
            Show(sharedSprite);
            return;
        }
        target.enabled = false;
        loading = StartCoroutine(Load());
    }

    private IEnumerator Load()
    {
        if (!Requests.TryGetValue(resourcePath, out var sharedRequest))
        {
            sharedRequest = Resources.LoadAsync<Sprite>(resourcePath);
            Requests.Add(resourcePath, sharedRequest);
        }
        yield return sharedRequest;
        var sharedSprite = sharedRequest.asset as Sprite;
        loading = null;
        if (sharedSprite == null)
        {
            Debug.LogError("Orchard menu backdrop could not be loaded from Resources/" + resourcePath, this);
            yield break;
        }
        Sprites[resourcePath] = sharedSprite;
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
