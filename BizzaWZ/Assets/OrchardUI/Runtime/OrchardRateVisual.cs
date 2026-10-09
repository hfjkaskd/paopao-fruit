using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Loads the rate comparison's prefab-authored artwork when its page opens.</summary>
[DisallowMultipleComponent]
public sealed class OrchardRateVisual : MonoBehaviour
{
    [Serializable] private struct Binding { public Image image; public string sprite; }
    [SerializeField] private string resourcePath;
    [SerializeField] private string heroResourcePath;
    [SerializeField] private Image heroImage;
    [SerializeField] private bool useRegionalHero;
    [SerializeField] private Binding[] bindings=Array.Empty<Binding>();
    private Coroutine loading;
    public bool IsReady { get; private set; }
    private void OnEnable() { if(!IsReady)loading=StartCoroutine(Load()); }
    private void OnDisable() { if(loading!=null){StopCoroutine(loading);loading=null;} }
    private IEnumerator Load()
    {
        var request=Resources.LoadAsync<Texture2D>(resourcePath);
        if(useRegionalHero)
        {
            yield return request;loading=null;
            if(request.asset==null){Debug.LogError("Rate artwork is missing.",this);yield break;}
            ApplyArtwork(Resources.LoadAll<Sprite>(resourcePath),null);
            yield break;
        }
        var hero=Resources.LoadAsync<Texture2D>(heroResourcePath);
        yield return request;yield return hero;loading=null;
        if(request.asset==null||hero.asset==null){Debug.LogError("Rate artwork is missing.",this);yield break;}
        var heroSprites=Resources.LoadAll<Sprite>(heroResourcePath);
        ApplyArtwork(Resources.LoadAll<Sprite>(resourcePath),heroSprites.Length>0?heroSprites[0]:null);
    }
    public void ApplyArtwork(Sprite[] sprites,Sprite hero)
    {
        IsReady=useRegionalHero||(hero!=null&&heroImage!=null);
        if(!useRegionalHero&&IsReady){heroImage.sprite=hero;heroImage.enabled=true;}
        foreach(var binding in bindings)
        {
            Sprite found=null;foreach(var sprite in sprites)if(sprite.name==binding.sprite){found=sprite;break;}
            if(found==null||binding.image==null){IsReady=false;continue;}
            binding.image.sprite=found;binding.image.enabled=true;
        }
        if(!IsReady)Debug.LogError("Rate artwork has an incomplete binding.",this);
    }
}
