using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Applies the lucky-spin help sheet to prefab-authored graphics on demand.</summary>
[DisallowMultipleComponent]
public sealed class OrchardLuckyHelpVisual : MonoBehaviour
{
    [Serializable] private struct Binding { public Image image; public string sprite; }
    [SerializeField] private string resourcePath;
    [SerializeField] private Binding[] bindings=Array.Empty<Binding>();
    private Coroutine loading;
    public bool IsReady { get; private set; }
    private void OnEnable() { if(!IsReady)loading=StartCoroutine(Load()); }
    private void OnDisable() { if(loading!=null){StopCoroutine(loading);loading=null;} }
    private IEnumerator Load()
    {
        var request=Resources.LoadAsync<Texture2D>(resourcePath);yield return request;loading=null;
        if(request.asset==null){Debug.LogError("Lucky-spin help artwork is missing: "+resourcePath,this);yield break;}
        ApplyArtwork(Resources.LoadAll<Sprite>(resourcePath));
    }
    public void ApplyArtwork(Sprite[] sprites)
    {
        IsReady=true;
        foreach(var binding in bindings)
        {
            Sprite found=null;foreach(var sprite in sprites)if(sprite.name==binding.sprite){found=sprite;break;}
            if(found==null||binding.image==null){IsReady=false;continue;}
            binding.image.sprite=found;binding.image.enabled=true;
        }
        if(!IsReady)Debug.LogError("Lucky-spin help artwork has an incomplete binding.",this);
    }
}
