using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Loads the loading page's authored sprite atlas on demand on every platform.</summary>
[DisallowMultipleComponent]
public sealed class OrchardLoadingArt : MonoBehaviour
{
    [Serializable]
    private struct SpriteBinding
    {
        public Image target;
        public string spriteName;
    }

    [SerializeField] private string resourcePath;
    [SerializeField] private CanvasGroup visualGroup;
    [SerializeField] private SpriteBinding[] bindings = Array.Empty<SpriteBinding>();

    private Coroutine loading;

    private void OnEnable()
    {
        if (visualGroup == null)
        {
            Debug.LogError("Loading artwork requires its authored CanvasGroup.", this);
            return;
        }

        visualGroup.alpha = 0f;
        if (string.IsNullOrWhiteSpace(resourcePath) || bindings == null || bindings.Length == 0)
        {
            Debug.LogError("Loading artwork requires a Resources path and sprite bindings.", this);
            return;
        }

        loading = StartCoroutine(Load());
    }

    private IEnumerator Load()
    {
        // Warm the texture asynchronously before resolving its Sprite sub-assets.
        ResourceRequest request = Resources.LoadAsync<Texture2D>(resourcePath);
        yield return request;
        loading = null;

        if (request.asset == null)
        {
            Debug.LogError("Loading artwork could not be loaded from Resources/" + resourcePath, this);
            yield break;
        }

        Sprite[] sprites = Resources.LoadAll<Sprite>(resourcePath);
        var resolvedSprites = new Sprite[bindings.Length];
        for (int bindingIndex = 0; bindingIndex < bindings.Length; bindingIndex++)
        {
            SpriteBinding binding = bindings[bindingIndex];
            if (binding.target == null || string.IsNullOrEmpty(binding.spriteName))
            {
                Debug.LogError("Loading artwork has an incomplete sprite binding at index " + bindingIndex, this);
                yield break;
            }

            for (int spriteIndex = 0; spriteIndex < sprites.Length; spriteIndex++)
            {
                if (!string.Equals(sprites[spriteIndex].name, binding.spriteName, StringComparison.Ordinal)) continue;
                resolvedSprites[bindingIndex] = sprites[spriteIndex];
                break;
            }

            if (resolvedSprites[bindingIndex] == null)
            {
                Debug.LogError("Loading artwork is missing sprite '" + binding.spriteName + "' in Resources/" + resourcePath, this);
                yield break;
            }
        }

        for (int bindingIndex = 0; bindingIndex < bindings.Length; bindingIndex++)
            bindings[bindingIndex].target.sprite = resolvedSprites[bindingIndex];

        visualGroup.alpha = 1f;
    }

    private void OnDisable()
    {
        if (loading == null) return;
        StopCoroutine(loading);
        loading = null;
    }
}
