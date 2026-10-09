using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Loads form artwork when the existing withdrawal account page is opened.</summary>
[DisallowMultipleComponent]
public sealed class OrchardAccountVisual : MonoBehaviour
{
    [SerializeField] private string resourcePath;
    [SerializeField] private Image hint;
    [SerializeField] private OrchardInputVisual[] inputs = Array.Empty<OrchardInputVisual>();
    private Coroutine loading;
    public bool IsReady { get; private set; }
    private void OnEnable() { if (!IsReady) loading = StartCoroutine(Load()); }
    private void OnDisable() { if (loading != null) { StopCoroutine(loading); loading = null; } }
    private IEnumerator Load()
    {
        var request = Resources.LoadAsync<Texture2D>(resourcePath);
        yield return request;
        loading = null;
        if (request.asset == null) { Debug.LogError("Account artwork is missing: " + resourcePath, this); yield break; }
        ApplyArtwork(Resources.LoadAll<Sprite>(resourcePath));
    }
    public void ApplyArtwork(Sprite[] sprites)
    {
        Sprite normal = null, focused = null, info = null;
        foreach (var sprite in sprites)
        {
            if (sprite.name == "Input") normal = sprite;
            else if (sprite.name == "FocusedInput") focused = sprite;
            else if (sprite.name == "Hint") info = sprite;
        }
        IsReady = normal != null && focused != null && info != null;
        if (!IsReady) { Debug.LogError("Account artwork is incomplete.", this); return; }
        hint.sprite = info; hint.enabled = true;
        foreach (var input in inputs) input.SetArtwork(normal, focused);
    }
}
