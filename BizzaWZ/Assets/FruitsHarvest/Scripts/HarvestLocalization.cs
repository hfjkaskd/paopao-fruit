using System.Collections.Generic;
public static class HarvestLocalization
{
    private static readonly Dictionary<EDMAJFIKJLE,string> Languages = new Dictionary<EDMAJFIKJLE,string>
    {
        { EDMAJFIKJLE.en, "en-US" },
        { EDMAJFIKJLE.ar, "ar-SA" },
        { EDMAJFIKJLE.de, "de-DE" },
        { EDMAJFIKJLE.es, "es-ES" },
        { EDMAJFIKJLE.fr, "fr-FR" },
        { EDMAJFIKJLE.hu, "hu-HU" },
        { EDMAJFIKJLE.id, "id-ID" },
        { EDMAJFIKJLE.it, "it-IT" },
        { EDMAJFIKJLE.ja, "ja-JP" },
        { EDMAJFIKJLE.ko, "ko-KR" },
        { EDMAJFIKJLE.pt, "pt-BR" },
        { EDMAJFIKJLE.ro, "ro-RO" },
        { EDMAJFIKJLE.ru, "ru-RU" },
        { EDMAJFIKJLE.th, "th-TH" },
        { EDMAJFIKJLE.vi, "vi-VN" },
        { EDMAJFIKJLE.ms, "ms-MY" },
        { EDMAJFIKJLE.fil, "fil-PH" },
        { EDMAJFIKJLE.sv, "sv-SE" },
        { EDMAJFIKJLE.da, "da-DK" },
        { EDMAJFIKJLE.fi, "fi-FI" },
        { EDMAJFIKJLE.no, "no-NO" },
        { EDMAJFIKJLE.pl, "pl-PL" },
        { EDMAJFIKJLE.hi, "hi-IN" },
        { EDMAJFIKJLE.nl, "nl-NL" },
        { EDMAJFIKJLE.fa, "fa-IR" },
        { EDMAJFIKJLE.tr, "tr-TR" },
        { EDMAJFIKJLE.el, "el-GR" },
        { EDMAJFIKJLE.cs, "cs-CZ" },
        { EDMAJFIKJLE.bg, "bg-BG" },
        { EDMAJFIKJLE.he, "he-IL" },
        { EDMAJFIKJLE.zh_cn, "zh-CN" },
        { EDMAJFIKJLE.zh_tw, "zh-TW" },
    };
    public static EDMAJFIKJLE Current
    {
        get { foreach(var pair in Languages) if(pair.Value==LanguageUtils.SelectedLanguage) return pair.Key; return EDMAJFIKJLE.en; }
    }
    public static void Install()
    {
        var table=TableUtils.Tables.TblLanguage;
        Add(table,"LosePanelRevive_Desc","lose_pop_content");
        Add(table,"LosePanel_Desc","lose_pop_title");
        Add(table,"LosePanel_confirm","lose_pop_restart");
        Add(table,"Harvest_Lose_Title","lose_pop_title");
    }
    private static void Add(cfg.TblLanguage table,string target,string source)
    {
        var translations=OJEEJGGLNPC.Instance.allLanDict[source];
        var row=new LanguageConfig { Id=target, Dict=new Dictionary<string,string>() };
        foreach(var pair in Languages) row.Dict[pair.Value]=translations[pair.Key];
        if(table.DataMap.TryGetValue(target,out var existing)) { foreach(var pair in row.Dict) existing.Dict[pair.Key]=pair.Value; }
        else { table.DataMap.Add(target,row); table.DataList.Add(row); }
    }
}
