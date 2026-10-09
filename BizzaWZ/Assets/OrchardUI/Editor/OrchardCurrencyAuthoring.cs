#if BIZZA_REAL_WITHDRAW
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bizza.Sdk;
using TMPro;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using UnityEngine.UI;

public static class OrchardCurrencyAuthoring
{
    private const string Output = "Design/CurrencyIcons-20261008";
    [Serializable] private class Inventory { public List<Entry> entries = new List<Entry>(); }
    [Serializable] private class Entry
    {
        public string prefab, path, sprite, asset, binding;
        public bool active;
        public int role = -1;
    }

    public static void InventoryBatch()
    {
        Directory.CreateDirectory(Output);
        var report = new Inventory();
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/BizzaWZ", "Assets/OrchardUI", "Assets/FruitsHarvest/Resources" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            foreach (var image in root.GetComponentsInChildren<Image>(true))
            {
                var amend = image.GetComponent<WzIconAmend>();
                string name = image.sprite == null ? "" : image.sprite.name;
                string asset = image.sprite == null ? "" : AssetDatabase.GetAssetPath(image.sprite);
                if (amend == null && !IsCurrency(name, asset)) continue;
                report.entries.Add(new Entry { prefab=path, path=AnimationUtility.CalculateTransformPath(image.transform,root.transform),
                    sprite=name, asset=asset, active=Active(image.transform, root.transform), role=amend==null?-1:(int)amend.iconType });
            }
            foreach (var script in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (script == null || !script.GetType().Name.StartsWith("Orchard")) continue;
                var data = new SerializedObject(script);
                var bindings = data.FindProperty("bindings");
                if (bindings == null || !bindings.isArray) continue;
                for(int i=0;i<bindings.arraySize;i++)
                {
                    var item=bindings.GetArrayElementAtIndex(i);
                    var name=item.FindPropertyRelative("sprite");
                    var image=item.FindPropertyRelative("image");
                    if(name==null || image==null || !IsCurrency(name.stringValue,"")) continue;
                    var target=image.objectReferenceValue as Image;
                    report.entries.Add(new Entry {prefab=path,path=target==null?"null":AnimationUtility.CalculateTransformPath(target.transform,root.transform),sprite=name.stringValue,binding=script.GetType().Name,active=target!=null&&Active(target.transform,root.transform)});
                }
            }
        }
        File.WriteAllText(Output+"/inventory.json",JsonUtility.ToJson(report,true));
        Debug.Log("Currency inventory: "+report.entries.Count);
    }

    private static bool IsCurrency(string name,string path)
    {
        string text=name.ToLowerInvariant();
        return path.Contains("WzTexture_Money") || text.Contains("coin") || text.Contains("cash") || text.Contains("money") || text.Contains("wealth") || text=="gold" || text=="pilegold";
    }
    private static bool Active(Transform node,Transform root)
    {
        for(var t=node;t!=null;t=t.parent) { if(!t.gameObject.activeSelf)return false;if(t==root)break; }
        return true;
    }

    private const string MoneyRoot="Assets/BizzaWZ/Final/Real/GameAssets/WzTexture_Money/";
    private static readonly string[] Families={"PieceMoney","StackMoney","PileMoney","PileWealth","PileGold","HundredMoney","AbundanceWealth","MoneyEnhancement","GoldCoin"};

    public static void ApplyBatch()
    {
        var inventory=JsonUtility.FromJson<Inventory>(File.ReadAllText(Output+"/inventory.json"));
        var paths=inventory.entries.Select(x=>x.prefab).Distinct().OrderBy(p=>AssetDatabase.GetDependencies(p).Length).ToArray();
        // Save every original before changing nested prefab sources.
        foreach(var path in paths) Backup(path);
        int count=0;
        foreach(var path in paths)
        {
            // This unused imported legacy prefab contains missing scripts. Its coin reference and
            // binding were patched in YAML so the unrelated legacy components remain untouched.
            if(path.EndsWith("/common/prefab/CommonCoinBtn.prefab",StringComparison.Ordinal))continue;
            var root=PrefabUtility.LoadPrefabContents(path);
            bool changed=false;
            try
            {
                foreach(var amend in root.GetComponentsInChildren<WzIconAmend>(true))
                    if(string.IsNullOrEmpty(WzCurrencySprites.RoleKey(amend.iconType)))
                    {UnityEngine.Object.DestroyImmediate(amend);changed=true;}
                // Remove currency entries from the delayed artwork loader: the regional binding owns these images.
                foreach(var script in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if(script==null||!script.GetType().Name.StartsWith("Orchard"))continue;
                    var data=new SerializedObject(script);
                    var bindings=data.FindProperty("bindings");
                    if(bindings==null||!bindings.isArray)continue;
                    for(int i=bindings.arraySize-1;i>=0;i--)
                    {
                        var item=bindings.GetArrayElementAtIndex(i);
                        var name=item.FindPropertyRelative("sprite");
                        var image=item.FindPropertyRelative("image");
                        if(name==null||image==null||!TryRole(name.stringValue,out var role))continue;
                        var target=image.objectReferenceValue as Image;
                        if(target==null)throw new InvalidOperationException("Missing currency target: "+path);
                        Bind(target,role); target.enabled=true;
                        bindings.DeleteArrayElementAtIndex(i);changed=true;
                    }
                    data.ApplyModifiedPropertiesWithoutUndo();
                }
                foreach(var image in root.GetComponentsInChildren<Image>(true))
                {
                    var amend=image.GetComponent<WzIconAmend>();
                    E_WzIconType role;
                    if(amend!=null)
                    {
                        role=amend.iconType;
                        if(string.IsNullOrEmpty(WzCurrencySprites.RoleKey(role)))
                        {
                            // The leaf-shaped bonus frame has no BubbleCoin regional asset.
                            UnityEngine.Object.DestroyImmediate(amend);changed=true;continue;
                        }
                    }
                    else if(image.sprite==null||!TryRole(image.sprite.name,out role))continue;
                    string node=AnimationUtility.CalculateTransformPath(image.transform,root.transform);
                    if(path.EndsWith("RealWithdrawPanel.prefab")&&(node.EndsWith("CoinInfo/CurrentCount/Image")||node.EndsWith("CoinInfo/RateCount/Image")))role=E_WzIconType.GoldCoin;
                    // Reel symbols are owned by SlotMachineManager while spinning, not by a fixed Image binding.
                    if(image.GetComponentInParent<SlotMachineManager>(true)!=null)
                    {
                        if(amend!=null)UnityEngine.Object.DestroyImmediate(amend);
                        image.sprite=SpriteFor(role,AccountModule.E_CountryType.US,false);
                    }
                    else Bind(image,role);
                    count++;changed=true;
                }
                foreach(var manager in root.GetComponentsInChildren<SlotMachineManager>(true))
                {
                    var data=new SerializedObject(manager);
                    var entries=data.FindProperty("slotEntryss");
                    for(int i=0;i<entries.arraySize;i++)
                    {
                        var entry=entries.GetArrayElementAtIndex(i);
                        if(entry.FindPropertyRelative("e_SlotType").enumValueIndex!=(int)E_SlotType.Coin)continue;
                        entry.FindPropertyRelative("sprite").objectReferenceValue=SpriteFor(E_WzIconType.GoldCoin,AccountModule.E_CountryType.US,false);
                    }
                    data.ApplyModifiedPropertiesWithoutUndo();changed=true;
                }
                if(path.EndsWith("ExchangeRatePanel.prefab"))
                {
                    var visual=root.GetComponent<OrchardRateVisual>();
                    var data=new SerializedObject(visual);
                    data.FindProperty("useRegionalHero").boolValue=true;
                    var hero=(Image)data.FindProperty("heroImage").objectReferenceValue;
                    Bind(hero,E_WzIconType.MoneyEnhancement);hero.enabled=true;
                    hero.rectTransform.sizeDelta=new Vector2(250,220);
                    var bindings=data.FindProperty("bindings");
                    for(int i=bindings.arraySize-1;i>=0;i--)
                    {
                        var name=bindings.GetArrayElementAtIndex(i).FindPropertyRelative("sprite").stringValue;
                        if(name=="Before"||name=="Now")bindings.DeleteArrayElementAtIndex(i);
                    }
                    data.ApplyModifiedPropertiesWithoutUndo();
                    foreach(string state in new[]{"BeforeState","NowState"})
                    {
                        var bg=root.transform.Find("Content (1)/"+state+"/bg").GetComponent<Image>();
                        bg.sprite=NamedSprite("Assets/OrchardUI/Art/FidelityControls.png",state=="BeforeState"?"Card":"Inset");
                        bg.type=Image.Type.Sliced;bg.pixelsPerUnitMultiplier=2;bg.enabled=true;
                        var coin=root.transform.Find("Content (1)/"+state+"/Icon").GetComponent<Image>();
                        Bind(coin,E_WzIconType.PileGold);coin.enabled=true;coin.transform.SetAsLastSibling();
                    }
                    changed=true;
                }
                if(changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(root,path,out bool saved);
                    if(!saved)throw new InvalidOperationException("Currency prefab was not saved: "+path);
                }
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        AssetDatabase.SaveAssets();
        Validate();
        File.WriteAllText(Output+"/applied.txt","Configured currency images: "+count+"\nPrefabs: "+paths.Length+"\nFramework regional coin and banknote families.\n");
        Preview();
        Debug.Log("Currency authoring complete: "+count+" images.");
    }

    private static void Backup(string path)
    {
        string to=Output+"/Before/"+path;
        Directory.CreateDirectory(Path.GetDirectoryName(to));
        if(!File.Exists(to))File.Copy(path,to);
    }
    private static bool TryRole(string name,out E_WzIconType role)
    {
        foreach(E_WzIconType candidate in Enum.GetValues(typeof(E_WzIconType)))
        {
            string key=WzCurrencySprites.RoleKey(candidate);
            if(key.Length>0&&(name==key||name.StartsWith(key+"_",StringComparison.Ordinal))){role=candidate;return true;}
        }
        switch(name)
        {
            case "Cash": case "PinkCash": case "CashSmall":role=E_WzIconType.StackMoney;return true;
            case "CashLarge":role=E_WzIconType.HundredMoney;return true;
            case "CoinStack":role=E_WzIconType.PileGold;return true;
            case "Coin":case "BigCoin":case "CommonCoinBtn":case "SproutCoin":case "Gold":role=E_WzIconType.GoldCoin;return true;
            default:role=default;return false;
        }
    }
    private static void Bind(Image image,E_WzIconType role)
    {
        var amend=image.GetComponent<WzIconAmend>()??image.gameObject.AddComponent<WzIconAmend>();
        amend.image=image;amend.iconType=role;amend.isNativeSize=false;amend.enabled=true;
        image.overrideSprite=null;image.sprite=SpriteFor(role,AccountModule.E_CountryType.US,false);
        image.type=Image.Type.Simple;image.preserveAspect=true;
    }
    private static Sprite SpriteFor(E_WzIconType role,AccountModule.E_CountryType country,bool single)
    {
        string address=WzCurrencySprites.Address(WzCurrencySprites.RoleKey(role),country,single);
        var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(MoneyRoot+address+".png");
        if(sprite==null)throw new InvalidOperationException("Missing regional currency: "+address);
        return sprite;
    }
    private static Sprite NamedSprite(string path,string name)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().First(s=>s.name==name);
    }
    public static void Validate()
    {
        int checks=0;
        foreach(var country in new[]{AccountModule.E_CountryType.US,AccountModule.E_CountryType.BR,AccountModule.E_CountryType.ID})
        foreach(bool single in new[]{false,true})
        foreach(string role in Families)
        {
            string address=WzCurrencySprites.Address(role,country,single);
            string path=MoneyRoot+address+".png";
            var entry=AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(path));
            if(entry==null||entry.address!=address||AssetDatabase.LoadAssetAtPath<Sprite>(path)==null)throw new InvalidOperationException("Unresolvable currency address: "+address);
            checks++;
        }
        if(WzCurrencySprites.Address("GoldCoin",AccountModule.E_CountryType.None,false)!="")throw new InvalidOperationException("Uninitialized country must not select a currency.");
        File.WriteAllText(Output+"/validation.txt",checks+" regional address / single-currency checks passed.\nUnknown country does not load a mismatched asset.\n");
    }
    public static void Preview()
    {
        foreach(var country in new[]{AccountModule.E_CountryType.US,AccountModule.E_CountryType.BR,AccountModule.E_CountryType.ID})
        foreach(string page in new[]{"SlotsPanel/SlotPanel/SlotPanel","SlotsPanel/SlotFQAPanel/SlotFQAPanel","ExchangeRatePanel/ExchangeRatePanel","FakeWithdrawPanel/FakeWithdrawPanel","GetRewardPanel/GetRewardPanel"})
        {
            string path="Assets/BizzaWZ/Final/Real/UI/"+page+".prefab";
            var preview=OrchardSkinValidation.PreviewPrefab(path,Output+"/"+Path.GetFileName(page)+"-"+WzCurrencySprites.CountryKey(country)+".png",root=>
            {
                if(root.TryGetComponent<OrchardReferenceLayout>(out var layout))layout.RefreshLayout();
                ApplyPreviewArtwork(root);
                foreach(var amend in root.GetComponentsInChildren<WzIconAmend>(true))amend.image.sprite=SpriteFor(amend.iconType,country,false);
                foreach(var manager in root.GetComponentsInChildren<SlotMachineManager>(true))
                foreach(var image in manager.GetComponentsInChildren<Image>(true))
                    if(image.sprite!=null&&image.sprite.name.StartsWith("GoldCoin_",StringComparison.Ordinal))image.sprite=SpriteFor(E_WzIconType.GoldCoin,country,false);
                ConfigureSampleText(root,country);
                // Static artwork fixture only: account data and reward actions are not initialized here.
            },720,1280);
            if(!string.IsNullOrEmpty(preview.error))throw new InvalidOperationException(preview.error);
        }
    }
    private static void ApplyPreviewArtwork(GameObject root)
    {
        if(root.TryGetComponent<OrchardCashVisual>(out var cash))cash.ApplyArtwork(Resources.LoadAll<Sprite>("OrchardUI/CashReferenceControls"));
        if(root.TryGetComponent<OrchardLuckyHelpVisual>(out var help))help.ApplyArtwork(Resources.LoadAll<Sprite>("OrchardUI/LuckyHelpReferenceControls"));
        if(root.TryGetComponent<OrchardRateVisual>(out var rate))rate.ApplyArtwork(Resources.LoadAll<Sprite>("OrchardUI/RateReferenceControls"),null);
    }

    private static void ConfigureSampleText(GameObject root,AccountModule.E_CountryType country)
    {
        string token=country==AccountModule.E_CountryType.BR?"R$":country==AccountModule.E_CountryType.ID?"Rp":"$";
        foreach(var label in root.GetComponentsInChildren<OrchardLocalizedLabel>(true))
        {
            var data=new SerializedObject(label);
            var target=data.FindProperty("target").objectReferenceValue as TMP_Text;
            if(target!=null)target.text=data.FindProperty("english").stringValue;
        }
        if(root.TryGetComponent<SlotPanel>(out var slot))
        {
            root.transform.Find("Top/IconInfo/IconTxt").GetComponent<TMP_Text>().text="2";
            root.transform.Find("Top/DollarInfo/DollarTxt").GetComponent<TMP_Text>().text=token+"123.71";
            root.transform.Find("Content/ApprovedFreeCount").GetComponent<TMP_Text>().text="0 FREE SPIN";
            slot.slotRewardPanel.gameObject.SetActive(false);
        }
        if(root.TryGetComponent<ExchangeRatePanel>(out var rate))
        {
            rate.beforeBlanceText.text=rate.nowBlanceText.text="10,000 coins";
            rate.beforeClashText.text=token+"2.50";rate.nowClashText.text=token+"5.00";
        }
        if(root.TryGetComponent<GetRewardPanel>(out var reward))
        {
            reward.itemATxt.text="+1,000";reward.itemBTxt.text=token+"22.70";reward.levelTxt.text="Level 2";
            reward.rewardText.text="Claim ×2";
            reward.noThanksText.spriteAsset=AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(MoneyRoot+"WzMoneyIcon_Common.asset");
            reward.noThanksText.text="<sprite="+WzCurrencySprites.InlineSpriteIndex(country,true)+"> "+token+"11.35";
            reward.bonusRate.gameObject.SetActive(false);
            var video=root.GetComponentInChildren<WathAdProgress>(true);
            video.hintText.text="Watch videos to increase rewards";video.startText.text="+0%";video.endText.text="+5%";
            video.progressText.text="4 / 10";video.progressBar.fillAmount=.4f;
            root.GetComponentInChildren<Real_AdWatchProgress>(true).gameObject.SetActive(false);
            reward.progress.gameObject.SetActive(true);reward.progress.progressTxt.text="Earn "+token+"59.24 more to withdraw "+token+"800";reward.progress.progressImg.fillAmount=.74f;
        }
    }

    public static void RuntimeCheckBatch()
    {
        // Isolated icon tests: no account initialization, HTTP requests, gameplay or save changes.
        var previousConfig=ChannelConfig.Instance;
        var previousCountry=AccountModule.CountryType;
        var fixture=new ChannelConfig();
        var target=new GameObject("Currency icon test",typeof(RectTransform),typeof(Image));
        target.SetActive(false);
        int checks=0;
        try
        {
            foreach(var country in new[]{AccountModule.E_CountryType.US,AccountModule.E_CountryType.BR,AccountModule.E_CountryType.ID,AccountModule.E_CountryType.US})
            foreach(bool single in new[]{false,true})
            {
                AccountModule.CountryType=country;
                fixture.real_CustomConfig.singleCurrencyMode=single;
                foreach(string role in Families)
                {
                    string expected=WzCurrencySprites.Address(role,country,single);
                    var loaded=WzCurrencySprites.Load(role);
                    if(loaded==null||loaded.name!=expected)throw new InvalidOperationException("Runtime load failed: "+expected);
                    var image=target.GetComponent<Image>();
                    WzCurrencySprites.Apply(image,role);
                    if(image.sprite!=loaded||target.activeSelf)throw new InvalidOperationException("Icon binding changed hidden-page visibility.");
                    checks++;
                }
                if(ItemUtils.GetItemIcon(E_ItemType.Gold)!=WzCurrencySprites.Load(E_WzIconType.GoldCoin) || ItemUtils.GetItemIcon(E_ItemType.Dollar)!=WzCurrencySprites.Load(E_WzIconType.StackMoney))throw new InvalidOperationException("Collection effects use different artwork.");
            }
            File.WriteAllText(Output+"/runtime-validation.txt",checks+" actual regional sprite loads and hidden-image checks passed; US → BR → ID → US; both currency modes.\nItem collection icons match regional balances. No account or save changes.\n");
            CheckReelSwitching(fixture);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(target);
            ChannelConfig.Instance=previousConfig;AccountModule.CountryType=previousCountry;
        }
        AuditAfter();
        Debug.Log("Currency runtime checks passed: "+checks);
    }

    private static void CheckReelSwitching(ChannelConfig fixture)
    {
        var root=PrefabUtility.LoadPrefabContents("Assets/BizzaWZ/Final/Real/UI/SlotsPanel/SlotPanel/SlotPanel.prefab");
        try
        {
            var manager=root.GetComponentInChildren<SlotMachineManager>(true);
            var fruit=manager.SlotEntryss.First(x=>x.e_SlotType!=E_SlotType.Coin).sprite;
            var fruitImage=manager.slotEntries[0].img1;
            fruitImage.sprite=fruit;
            foreach(var country in new[]{AccountModule.E_CountryType.US,AccountModule.E_CountryType.BR,AccountModule.E_CountryType.ID,AccountModule.E_CountryType.US})
            {
                AccountModule.CountryType=country;fixture.real_CustomConfig.singleCurrencyMode=false;
                manager.RefreshCurrencyArtwork();
                if(manager.SlotEntryss.First(x=>x.e_SlotType==E_SlotType.Coin).sprite!=WzCurrencySprites.Load(E_WzIconType.GoldCoin))throw new InvalidOperationException("Stale spinning coin symbol.");
                if(fruitImage.sprite!=fruit)throw new InvalidOperationException("Currency switch replaced a fruit symbol.");
            }
            File.AppendAllText(Output+"/runtime-validation.txt","Reel table and current coin images switch country without replacing fruit symbols.\n");
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }

    private static void AuditAfter()
    {
        var report=new System.Text.StringBuilder();
        foreach(string guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/BizzaWZ/Final/Real/UI","Assets/BizzaWZ/Final/MenuSystem","Assets/FruitsHarvest/Resources"}))
        {
            string path=AssetDatabase.GUIDToAssetPath(guid);
            var root=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            foreach(var amend in root.GetComponentsInChildren<WzIconAmend>(true))
                if(amend.image==null||amend.image.gameObject!=amend.gameObject||string.IsNullOrEmpty(WzCurrencySprites.RoleKey(amend.iconType)))throw new InvalidOperationException("Incomplete currency binding: "+path);
            foreach(var renderer in root.GetComponentsInChildren<ParticleSystemRenderer>(true))
                if(path.Contains("Slot"))report.AppendLine(path+" | "+AnimationUtility.CalculateTransformPath(renderer.transform,root.transform)+" | "+(renderer.sharedMaterial==null?"null":AssetDatabase.GetAssetPath(renderer.sharedMaterial)));
        }
        File.WriteAllText(Output+"/particle-audit.txt",report.ToString());
    }
}
#endif
