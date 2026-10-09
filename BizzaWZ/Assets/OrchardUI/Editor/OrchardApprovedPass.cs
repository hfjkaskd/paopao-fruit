using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Explicit asset authoring and diagnostics for the September approved concepts.</summary>
[InitializeOnLoad]
public static partial class OrchardApprovedPass
{
    public const string Output = "Design/OrchardImplementation-20260928/";
    [Serializable] public sealed class Manifest { public string[] prefabs; }
    [Serializable] public sealed class Node
    {
        public string path, type, sprite, text;
        public bool active;
        public Vector2 position, size, min, max, pivot;
        public Vector3 scale;
        public float fontSize;
        public List<string> fields = new List<string>();
    }
    [Serializable] public sealed class Hierarchy { public string prefab; public List<Node> nodes = new List<Node>(); }
    static OrchardApprovedPass() { EditorApplication.update += Tick; }
    private static void Tick()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        string file = Output + "command.txt";
        if (!File.Exists(file)) return;
        string command;
        try { command=File.ReadAllText(file).Trim(); File.Delete(file); }
        catch(IOException) { return; }
        try
        {
            if (command == "dump") DumpAll();
            else if(command=="fidelity-import") ImportFidelityArt();
            else if(command=="details-import") ImportReferenceDetails();
            else if(command=="booster-import") ImportBoosterReference();
            else if(command=="cash-import") ImportCashReference();
            else if(command=="account-import") ImportAccountReference();
            else if(command=="confirm-import") ImportConfirmReference();
            else if(command=="history-import") ImportHistoryReference();
            else if(command=="reminder-import") ImportReminderReference();
            else if(command=="rate-import") ImportRateReference();
            else if(command=="service-import") ImportServiceReference();
            else if(command=="daily-import") ImportDailyReference();
            else if(command=="spin-import") ImportSpinReference();
            else if(command=="lucky-help-import") ImportLuckyHelpReference();
            else if(command=="size-reference") UnityEditor.PlayModeWindow.SetCustomRenderingResolution(852,1846,"Orchard Reference 852x1846");
            else if(command=="size-short") UnityEditor.PlayModeWindow.SetCustomRenderingResolution(1080,1920,"Orchard Short 1080x1920");
            else if (command == "import") ImportApprovedArt();
            else if (command.StartsWith("apply:", StringComparison.Ordinal)) ApplyLayouts(command.Substring(6));
            else if (command == "components") AuthorComponents();
            else if (command.StartsWith("preview:", StringComparison.Ordinal))
            {
                string name = command.Substring(8);
                foreach (var spec in ReadLayouts()) if (spec.name == name || name == "all")
                {
                    Vector2 size=LayoutReferenceSize(spec);
                    OrchardSkinValidation.PreviewPrefab(spec.prefab, Output + "Previews/" + spec.name + ".png",
                        root => ConfigureReferencePreview(root, spec.name), (int)size.x, (int)size.y,
                        root => SaveGeometry(root, spec.name),PreviewContext(spec.name));
                }
            }
            else if (command == "audit") File.WriteAllText(Output + "audit-current.json", JsonUtility.ToJson(OrchardSkinValidation.Audit("implementation-current"), true));
            else if (command == "enter-play")
            {
                if (!EditorApplication.isPlaying)
                {
                    EditorSceneManager.OpenScene("Assets/Game/Resources/Scenes/InitWZ.unity");
                    EditorApplication.isPlaying = true;
                }
            }
            else if (command == "stop-play") EditorApplication.isPlaying = false;
            else throw new ArgumentException("Unknown authoring command: " + command);
            File.WriteAllText(Output + "result.txt", DateTime.UtcNow.ToString("O") + " SUCCESS " + command);
        }
        catch (Exception e) { File.WriteAllText(Output + "result.txt", "FAILED " + command + "\n" + e); Debug.LogException(e); }
    }
    private static void ConfigureReferencePreview(GameObject root, string name)
    {
        var layout = root.GetComponent<OrchardReferenceLayout>(); if (layout != null) layout.RefreshLayout();
        // Disposable visual fixtures only; business scripts remain disabled. These values never enter an asset or account.
        if (name == "settings")
        {
            var p = root.GetComponent<PausePanel>(); p.musicOnIm.gameObject.SetActive(true); p.musicOffIm.gameObject.SetActive(false);
            p.soundOnIm.gameObject.SetActive(true); p.soundOffIm.gameObject.SetActive(false);
            p.LibOnIm.gameObject.SetActive(false); p.LibOffIm.gameObject.SetActive(true);
            p.languageDropdown.ClearOptions(); p.languageDropdown.AddOptions(new List<string> { "Português" });
        }
#if BIZZA_REAL_WITHDRAW
        if(name=="withdraw-confirm")
        {
            var p=root.GetComponent<UIWithdrawalConfirmPanel>();p.PaymentValueText.text="R$0,03";p.NameText.text="Maria Silva";p.CPF_CNPJText.text="***.***.***-**";p.EmailText.text="m***@example.com";p.paymentImage.sprite=p.paymentList.GetSpriteByType(E_PayeeAccountType.Pagbank);
        }
        if (name == "withdraw-account")
        {
            var p = root.GetComponent<UIWithdrawalPanel>();
            foreach (Transform t in p.InputRoot.Find("InfoContent")) t.gameObject.SetActive(false);
            foreach (var go in p.PagBankList) go.SetActive(true);
            p.PlatformRoot.SetActive(false); p.PlatformIconRoot.SetActive(true);
            p.paymentImage.sprite = p.paymentList.GetSpriteByType(E_PayeeAccountType.Pagbank);
            var amount=root.GetComponentInChildren<OrchardKeyboardFormScroll>(true) != null
                ? root.transform.Find("Root/FillRoot/FormViewport/FormContent/ApprovedAmount").GetComponent<TMP_Text>()
                : root.transform.Find("Root/FillRoot/ApprovedAmount").GetComponent<TMP_Text>();
            amount.text="R$0,03";amount.transform.SetAsLastSibling();
        }
        if (name == "withdraw-pending")
        {
            var p = root.GetComponent<UIWithdrawalPendingPanel>(); p.amountText.text = "R$0,03";
            p.paymentImage.sprite = p.paymentList.GetSpriteByType(E_PayeeAccountType.Pagbank); p.paymentImage.color = Color.white;
            p.progressFill.fillAmount = .6f; p.progressText.text = "12 / 20"; p.hintText.text = "Verificando solicitação...";
        }
        if(name=="withdraw-main")
        {
            var p=root.GetComponent<RealWithdrawPanel>();p.balanceTxt.text="0,95";p.rateTxt.text="100 ≈ R$3,00";p.clashTxt.text="≈ R$0,03";p.passLevelText.text="Nível 84";p.hintTxt.text="<color=#007DA3>PagBank:</color> Você pode sacar <color=#005D24>R$0,03.</color>";
            p.fingerObj.SetActive(false);p.canWithdrawFingerHint.SetActive(false);
            p.progressObj.SetActive(false);p.progressHintTxt.gameObject.SetActive(false);p.completeHintTxt.gameObject.SetActive(true);p.completeHintTxt.text="Valor a receber: <color=#005629>R$0,03</color>";
            var footer=p.completeHintTxt.transform.parent.GetComponent<LayoutElement>();if(footer!=null){footer.preferredHeight=70;footer.preferredWidth=428;}
            var cards=p.withdrawLevelRoot.GetComponentsInChildren<WithdrawLevelItem>(true);
            if(cards.Length<6&&cards.Length>0){UnityEngine.Object.Instantiate(cards[0],cards[0].transform.parent);cards=p.withdrawLevelRoot.GetComponentsInChildren<WithdrawLevelItem>(true);}
            for(int i=0;i<cards.Length;i++){cards[i].levelTxt.text="Nível "+new[]{1,50,120,250,500,1000}[i];cards[i].rateText.text=(1+i*.2f).ToString("0.0",System.Globalization.CultureInfo.InvariantCulture)+"X";cards[i].balanceText.text=i==0?"R$0,02":i<3?"R$0,03":i<5?"R$0,04":"R$0,05";cards[i].shadowObj.SetActive(false);cards[i].selectedObj.SetActive(false);cards[i].selectObj.SetActive(i==1);cards[i].transform.Find("ApprovedSelectionBorder").gameObject.SetActive(i==1);}
            var ways=p.withdrawWayRoot.GetComponentsInChildren<WithdrawWay>(true);var cfg=AssetDatabase.LoadAssetAtPath<PaymentConfig>("Assets/BizzaWZ/Final/Real/Config/PaymentConfig.asset");
            for(int i=0;i<ways.Length;i++){ways[i].transform.Find("Frame").GetComponent<Image>().sprite=cfg.GetSpriteByType(i==0?E_PayeeAccountType.Pagbank:E_PayeeAccountType.PIX);ways[i].transform.Find("Select").gameObject.SetActive(i==0);}
        }
        if(name=="level-complete")
        {
            var p=root.GetComponent<GetRewardPanel>();p.itemATxt.text="+1.000";p.itemBTxt.gameObject.SetActive(false);p.levelTxt.text="NÍVEL 24 COMPLETO";p.rewardText.text="Assistir e receber";p.noThanksText.text="Continuar";
            var w=root.GetComponentInChildren<WathAdProgress>(true);w.hintText.text="Progresso do bônus";w.progressText.text="6 / 10";p.bonusRate.bonusRateTxt.text="+5%";
        }
        if(name=="get-booster")
        {
            var p=root.GetComponent<AddPropPanel>();p.propName.text="Desfaça sua última jogada.";p.limitTxt.text="Usado nesta fase: 0 / 3";
            foreach(var tx in p.adBuyBtn.GetComponentsInChildren<TMP_Text>(true))tx.text="Assistir e ganhar 1";
        }
        if(name=="daily-mission")
        {
            var p=root.GetComponent<DailyMissionPanel>();p.GoObj.SetActive(true);p.WithdrawObj.SetActive(false);p.ClaimedObj.SetActive(false);p.claimedHint.gameObject.SetActive(false);
            p.hintsTxt.text="Assista a 30 vídeos para receber";root.transform.Find("Content/ApprovedRewardValue").GetComponent<TMP_Text>().text="R$0,20";root.transform.Find("Content/ApprovedProgressValue").GetComponent<TMP_Text>().text="8 / 30";
        }
#endif
        if(name=="revive")
        {var p=root.GetComponent<LosePanel>();foreach(var go in p.reviveObjs)go.SetActive(true);foreach(var go in p.loseObjs)go.SetActive(false);}
        if(name=="lucky-spin")
        {
            var p=root.GetComponent<SlotPanel>();p.canClickObj.SetActive(true);p.notCanClickObj.SetActive(false);
            root.transform.Find("Content/ApprovedFreeCount").GetComponent<TMP_Text>().text="1";
            root.transform.Find("Content/ApprovedSlotProgress").GetComponent<TMP_Text>().text="3 / 5";
            p.slotHintTxt.text="Complete fases para ganhar um giro.";
        }
        ConfigureDetailedReferencePreview(root, name);
    }
    private static string PreviewContext(string name)
    {
        if(name=="lucky-help")return "Design/OrchardLuckySpinRefinement-20260928/Runtime/00-lucky-spin.png";
        if(name=="service-topics")return Output+"Runtime/05-ServicePanel.png";
        if(name=="lucky-help")return Output+"Runtime/07-SlotPanel.png";
        if(name=="revive"||name=="get-booster"||name=="rate-up"||name=="welcome-gift"||name=="daily-mission"||name=="tutorial")return Output+"Runtime/gameplay.png";
        return null;
    }
    private static void SaveGeometry(GameObject root, string name)
    {
        var report = new Hierarchy { prefab = name };
        foreach (var r in root.GetComponentsInChildren<RectTransform>(true))
        {
            Vector3 center = root.transform.InverseTransformPoint(r.TransformPoint(r.rect.center));
            var node = new Node { path = AnimationUtility.CalculateTransformPath(r, root.transform), active = r.gameObject.activeInHierarchy,
                position = new Vector2(center.x + ((RectTransform)root.transform).rect.width*.5f, ((RectTransform)root.transform).rect.height*.5f - center.y), size = r.rect.size, min = r.anchorMin, max = r.anchorMax, scale = r.localScale, pivot = r.pivot };
            if (r.TryGetComponent<Image>(out var image)) { node.sprite = image.sprite != null ? image.sprite.name : ""; node.type = "Image:" + image.enabled + ":" + image.color.a; }
            if (r.TryGetComponent<TMP_Text>(out var text)) { node.text = text.text; node.fontSize = text.fontSize; node.type = "Text:" + text.enabled; }
            report.nodes.Add(node);
        }
        File.WriteAllText(Output + "Previews/" + name + ".json", JsonUtility.ToJson(report, true));
    }
    public static void DumpAll()
    {
        var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText("Design/OrchardUI/prefab-manifest.json"));
        var paths = new HashSet<string>(manifest.prefabs, StringComparer.Ordinal);
        paths.Add("Assets/FruitsHarvest/Resources/Original/res/local/coreplay/CorePlayUI.prefab");
        paths.Add("Assets/FruitsHarvest/Resources/Original/res/local/pops/newitempop/NewItemPop.prefab");
        Directory.CreateDirectory(Output + "Hierarchy");
        foreach (string path in paths)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var report = new Hierarchy { prefab = path };
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    var n = new Node { path = AnimationUtility.CalculateTransformPath(t, root.transform), active = t.gameObject.activeSelf, scale = t.localScale };
                    if (t is RectTransform r) { n.position = r.anchoredPosition; n.size = r.sizeDelta; n.min = r.anchorMin; n.max = r.anchorMax; n.pivot = r.pivot; }
                    foreach (Component c in t.GetComponents<Component>())
                    {
                        if (c == null) { n.type += " MISSING"; continue; }
                        n.type += " " + c.GetType().Name;
                        if (c is Image im) n.sprite = im.sprite == null ? "" : AssetDatabase.GetAssetPath(im.sprite) + "#" + im.sprite.name;
                        if (c is TMP_Text tx) { n.text = tx.text; n.fontSize = tx.fontSize; }
                        if (!(c is MonoBehaviour) || c is Graphic) continue;
                        var so = new SerializedObject(c); var p = so.GetIterator();
                        while (p.NextVisible(true))
                        {
                            if (p.propertyType != SerializedPropertyType.ObjectReference || p.objectReferenceValue == null || p.name == "m_Script") continue;
                            var value = p.objectReferenceValue;
                            Transform target = value is Component comp ? comp.transform : value is GameObject go ? go.transform : null;
                            string valuePath = target != null && (target == root.transform || target.IsChildOf(root.transform)) ? AnimationUtility.CalculateTransformPath(target, root.transform) : AssetDatabase.GetAssetPath(value);
                            n.fields.Add(c.GetType().Name + "." + p.propertyPath + " = " + valuePath);
                        }
                    }
                    report.nodes.Add(n);
                }
                File.WriteAllText(Output + "Hierarchy/" + Path.GetFileNameWithoutExtension(path) + ".json", JsonUtility.ToJson(report, true));
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}
