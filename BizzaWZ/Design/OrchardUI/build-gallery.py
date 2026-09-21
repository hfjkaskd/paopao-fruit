"""Build a local, dependency-free gallery from completed Unity validation output.

Run after the final preview queue completes:
    python Design/OrchardUI/build-gallery.py
No screenshots are generated, edited, or synthesized by this script.
"""
from __future__ import annotations

import argparse
import json
import os
import re
from datetime import datetime, timezone
from pathlib import Path
from urllib.parse import quote


FRIENDLY = {
    "LosePanel": "失败与再试一次", "WhiteWinPanel": "胜利结算", "UIItem": "通用物品",
    "TransitionBlock": "转场遮罩", "LoadingPanel": "加载界面", "UIRedPoint": "红点提示",
    "BG": "共用背景", "RealGamePanel": "游戏主界面", "UIPropEntry": "道具入口",
    "PausePanel": "设置与暂停", "UITeachFingerMovePage": "引导手势",
    "UITeachMaskFocusPage": "引导聚焦", "UITeachMaskPage": "引导遮罩", "UITeachTipsPage": "引导提示",
    "CommonConfirmTipsPanel": "通用确认弹窗", "CurrencyBar": "货币状态栏",
    "UIActivityTaskElement": "活动任务条目", "UIDailyTaskElement": "每日任务条目",
    "UIDailyTaskPage": "每日任务", "BroadCastBar": "奖励广播栏", "AddPropPanel": "获取道具",
    "ItemForCountry": "地区奖励条目", "DailyMissionPanel": "每日现金任务",
    "DailyWithdrawPanel": "每日提现", "ExchangeRatePanel": "兑换率提升", "FAQPanel": "帮助与常见问题",
    "FakeWithdrawPanel": "现金提现", "WithdrawAmountItem": "提现金额卡",
    "GameUiWidget": "游戏HUD", "UIBizzaAAA": "游戏奖励入口", "GetRewardPanel": "领取奖励",
    "Real_WithdrawProgress": "提现奖励进度", "NewbieGiftPage": "新手礼包", "BonusRate": "加成倍率",
    "DailyBonus": "每日奖励", "DailyBonusItem": "每日奖励条目", "RealWithdrawPanel": "金币兑换与提现",
    "WithdrawInfo": "提现说明", "WithdrawLevel": "提现等级列表", "WithdrawLevelItem": "提现等级卡",
    "WithdrawWay": "支付方式", "ChatElement": "客服聊天气泡", "ServiceBtn": "客服入口",
    "ServicePanel": "客服会话", "ServiceSelectPanel": "客服快捷问题", "SlotEnter": "老虎机入口",
    "SlotFQAPanel": "老虎机说明", "SlotPanel": "老虎机奖励", "StarRatingPopup": "游戏评分",
    "UIWithdrawalConfirmPanel": "提现确认", "UIWithdrawalPendingPanel": "提现处理中",
    "WithdrawDanItem": "提现段位条目", "WithdrawDanPanel": "提现段位",
    "WithdrawFillPanel": "填写提现账户", "UIWithdrawalPanel": "填写提现账户",
    "WithdrawHistory": "提现记录", "WithdrawHistoryItem": "提现记录条目",
}
PAGE_NAMES = {
    "LosePanel", "WhiteWinPanel", "LoadingPanel", "RealGamePanel", "PausePanel",
    "CommonConfirmTipsPanel", "UIDailyTaskPage", "UI_DailyTaskPage", "AddPropPanel",
    "DailyMissionPanel", "DailyWithdrawPanel", "ExchangeRatePanel", "FAQPanel",
    "FakeWithdrawPanel", "GetRewardPanel", "NewbieGiftPage", "RealWithdrawPanel",
    "ServicePanel", "ServiceSelectPanel", "SlotFQAPanel", "SlotPanel", "StarRatingPopup",
    "UIWithdrawalConfirmPanel", "UIWithdrawalPendingPanel", "WithdrawDanPanel",
    "WithdrawFillPanel", "UIWithdrawalPanel", "WithdrawHistory",
}
LAYER_ORDER = {"BaseLayer": 0, "PopupLayer": 1, "TopLayer": 2, "SystemLayer": 3, "TeachLayer": 4}
TOKEN = re.compile(r"^(.+?)(?:\(Clone\))?\[(\d+)\]$")


def read_json(path: Path, required: bool = True) -> dict:
    if not path.exists():
        if required:
            raise SystemExit(f"缺少报告：{path}")
        return {}
    try:
        return json.loads(path.read_text(encoding="utf-8-sig"))
    except (OSError, json.JSONDecodeError) as exc:
        raise SystemExit(f"报告不可读，可能正在重写，请稍后再运行：{path}\n{exc}") from exc


def relative_url(path: Path, base: Path) -> str:
    return quote(Path(os.path.relpath(path, base)).as_posix(), safe="/")


def resolve_report_image(value: str, base: Path) -> Path:
    image_path = Path(value)
    if image_path.is_absolute() and image_path.exists():
        return image_path
    # Reports may have been copied together with their Previews folder.
    filename = value.replace("\\", "/").rsplit("/", 1)[-1]
    return base / "Previews" / filename


def identify_runtime(snapshot: dict) -> tuple[str, list[str]]:
    """Infer the frontmost known page from actual serialized hierarchy layer/sibling order.

    Snapshot v1 does not record an explicit opening timestamp. Do not claim that
    alphabetical JSON order equals opening order, or infer names from PNG content.
    """
    candidates: dict[str, tuple[int, int, int]] = {}
    for collection in ("images", "texts", "buttons"):
        for item in snapshot.get(collection, []):
            layer = -1
            for depth, segment in enumerate(item.get("path", "").split("/")):
                match = TOKEN.match(segment)
                if not match:
                    continue
                name = match.group(1).removesuffix("(Clone)")
                sibling = int(match.group(2))
                if name in LAYER_ORDER:
                    layer = LAYER_ORDER[name]
                if name in PAGE_NAMES:
                    rank = (layer, sibling, depth)
                    candidates[name] = max(candidates.get(name, (-1, -1, -1)), rank)
    explicit = snapshot.get("lastOpenedPage") or snapshot.get("pageId")
    if explicit:
        candidates[explicit] = (999, 999, 999)
    ordered = sorted(candidates, key=lambda name: candidates[name], reverse=True)
    return (ordered[0] if ordered else ""), ordered


def collect(base: Path) -> tuple[dict, dict[Path, tuple[int, int]]]:
    state = read_json(base / "state.json", required=False)
    if state.get("busy") or state.get("compiling") or state.get("updating"):
        raise SystemExit("Unity 验证仍在进行，请等待最终 apply / audit / preview 完成后再生成画廊。")
    manifest = read_json(base / "prefab-manifest.json")
    preview = read_json(base / "preview-report.json")
    expected = set(manifest.get("prefabs", []))
    reported = {entry.get("prefab") for entry in preview.get("pages", [])}
    if reported != expected or len(preview.get("pages", [])) != len(expected):
        raise SystemExit("静态预览报告尚不完整或与 manifest 不一致，请完成全部预览后再生成画廊。")
    tracked: dict[Path, tuple[int, int]] = {}

    def track(path: Path) -> None:
        stat = path.stat()
        tracked[path] = (stat.st_size, stat.st_mtime_ns)

    track(base / "preview-report.json")
    cards = []
    missing = []
    for index, entry in enumerate(preview["pages"], 1):
        path = resolve_report_image(entry.get("image", ""), base)
        if not path.is_file():
            missing.append(str(path))
            continue
        track(path)
        name = Path(entry["prefab"]).stem
        cards.append({
            "kind": "static", "id": name, "name": FRIENDLY.get(name, name), "index": index,
            "image": relative_url(path, base), "prefab": entry["prefab"], "error": entry.get("error", ""),
            "detail": "静态 Prefab 默认值 · 业务脚本与动画已禁用",
            "notes": entry.get("notes", []), "time": preview.get("generatedUtc", ""), "diagnostic": "",
            "search": " ".join((name, FRIENDLY.get(name, name), entry["prefab"], "静态 默认值")),
        })
    if missing:
        raise SystemExit("预览图缺失，未生成画廊：\n" + "\n".join(missing))

    runtime_cards = []
    for path in sorted((base / "RuntimeCaptures").glob("*.png"), reverse=True):
        track(path)
        diagnostic = path.with_suffix(".json")
        snapshot = read_json(diagnostic, required=False)
        if diagnostic.exists():
            track(diagnostic)
        page, candidates = identify_runtime(snapshot)
        name = FRIENDLY.get(page, page) if page else "运行截图（页面未识别）"
        detail = "实际运行画面 · " + ("页面名根据运行 JSON 层级识别" if page else "缺少可识别的页面诊断数据")
        runtime_cards.append({
            "kind": "runtime", "id": page or path.stem, "name": name, "index": len(runtime_cards) + 1,
            "image": relative_url(path, base), "prefab": "", "error": "", "detail": detail,
            "notes": ["快照内可见页面：" + "、".join(FRIENDLY.get(item, item) for item in candidates)] if candidates else [],
            "time": snapshot.get("generatedUtc", path.stem.removeprefix("runtime-")),
            "diagnostic": relative_url(diagnostic, base) if diagnostic.exists() else "",
            "search": " ".join((name, page, path.name, " ".join(candidates), "运行 实际截图")),
        })
    audit = read_json(base / "audit-current.json", required=False)
    payload = {
        "generated": datetime.now(timezone.utc).isoformat(timespec="seconds"),
        "staticCount": len(cards), "runtimeCount": len(runtime_cards), "cards": runtime_cards + cards,
        "previewUtc": preview.get("generatedUtc", ""), "auditUtc": audit.get("generatedUtc", ""),
        "newErrors": audit.get("newErrorCount"), "existingIssues": audit.get("existingIssueCount"),
        "resolvedIssues": audit.get("resolvedIssueCount"), "buttonCount": audit.get("buttonCount"),
    }
    return payload, tracked


TEMPLATE = r'''<!doctype html>
<html lang="zh-CN"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
<title>Orchard UI · 界面换皮画廊</title>
<style>
:root{font-family:Inter,"Microsoft YaHei",system-ui,sans-serif;color:#174f7d;background:#fffaf0;font-synthesis:none}*{box-sizing:border-box}body{margin:0}a{color:inherit;text-decoration-thickness:1px;text-underline-offset:4px}button,input{font:inherit}header,main,footer{max-width:1440px;margin:auto;padding:0 36px}header{padding-top:48px;padding-bottom:28px}.eyebrow{font-size:12px;letter-spacing:.2em;color:#a7773d;font-weight:800}h1{font-size:clamp(26px,4vw,44px);line-height:1.25;margin:12px 0 14px;color:#173f64}header p{max-width:820px;color:#5d7382;line-height:1.9;margin:0}.header-links{display:flex;gap:24px;margin-top:20px;font-size:14px}.metrics{display:flex;gap:12px;flex-wrap:wrap;margin-top:28px}.metric{background:#fff;border:1px solid #e9dec8;border-radius:16px;padding:13px 18px;min-width:160px;font-size:13px;color:#70808b}.metric strong{display:block;color:#174f7d;font-size:23px;margin-bottom:4px}.notice{margin:0 0 24px;padding:16px 18px;border-left:4px solid #dcb979;background:#f7efdE;border-radius:3px 12px 12px 3px;color:#6f6e58;font-size:13px;line-height:1.8}.toolbar{display:flex;justify-content:space-between;gap:20px;align-items:center;position:sticky;top:0;z-index:5;padding:16px 0;background:#fffaf0f5;backdrop-filter:blur(12px);border-bottom:1px solid #e9dfce;margin-bottom:20px}.filters{display:flex;gap:8px;flex-wrap:wrap}.filter{border:1px solid #dfd4bd;background:#fff;padding:10px 16px;border-radius:999px;color:#4f6b7f;cursor:pointer;font-size:14px}.filter.active{background:#174f7d;border-color:#174f7d;color:#fff}.search{max-width:340px;flex:1;min-width:150px;border:1px solid #d7dddc;background:white;padding:12px 16px;border-radius:12px;outline-offset:3px}.result-line{display:flex;justify-content:space-between;gap:12px;color:#718391;font-size:12px;margin-bottom:18px}.grid{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:24px}.card{background:#fff;border:1px solid #eadfcf;border-radius:20px;overflow:hidden;box-shadow:0 6px 20px #6d501006}.thumb{display:block;background:repeating-conic-gradient(#f1f2ed 0% 25%,#f8f8f4 0% 50%) 50%/18px 18px;aspect-ratio:9/16;overflow:hidden;border-bottom:1px solid #ece7dc}.thumb img{display:block;width:100%;height:100%;object-fit:contain;transition:transform .2s}.thumb:hover img{transform:scale(1.015)}.meta{padding:16px}.tag{font-size:10px;font-weight:800;letter-spacing:.05em;display:inline-block;padding:5px 8px;border-radius:7px;background:#edf3f8;color:#467395}.tag.runtime{background:#e6f4e6;color:#27844b}.tag.error{background:#fde9e1;color:#b15034;margin-left:6px}.card h2{font-size:17px;margin:11px 0 6px;color:#163f61;line-height:1.4}.asset-id{font-family:ui-monospace,Consolas,monospace;overflow-wrap:anywhere;color:#8a98a0;font-size:11px;line-height:1.5}.description{color:#71818b;font-size:11px;line-height:1.7;margin:10px 0}.card-links{display:flex;gap:16px;margin-top:12px;font-size:12px;font-weight:700}.empty{display:none;text-align:center;border:1px dashed #d9c8aa;border-radius:18px;padding:60px 20px;color:#7b8b96}footer{padding-top:32px;padding-bottom:40px;font-size:12px;color:#87959d;line-height:1.8}.error-copy{color:#ae4d31}.small{font-size:11px;color:#8b98a0}.hidden{display:none!important}@media(max-width:1150px){.grid{grid-template-columns:repeat(3,minmax(0,1fr))}}@media(max-width:820px){header,main,footer{padding-left:20px;padding-right:20px}.grid{grid-template-columns:repeat(2,minmax(0,1fr));gap:16px}.toolbar{align-items:stretch;flex-direction:column;gap:12px}.search{max-width:none;flex:auto}.metric{min-width:130px}}@media(max-width:450px){.grid{gap:12px}.meta{padding:12px}.card h2{font-size:14px}.filter{font-size:12px;padding:9px 12px}.card{border-radius:14px}.header-links{gap:16px;font-size:12px}}
</style></head><body>
<header><div class="eyebrow">ORCHARD UI · ART REVIEW</div><h1>阳光田野 · 界面换皮画廊</h1>
<p>以无大树的蓝天田野、温暖木框、奶油内板、蓝色导航和绿色主按钮统一界面。这里收录 Unity 实际生成的原图，点击缩略图可查看完整尺寸。</p>
<div class="header-links"><a href="locked-reference.png" target="_blank" rel="noopener">锁定美术参考 ↗</a><a href="README.md" target="_blank" rel="noopener">制作与验收说明 ↗</a><a href="audit-current.json" target="_blank" rel="noopener">最新结构审计 ↗</a></div>
<div class="metrics"><div class="metric"><strong id="runtime-count">—</strong>实际运行截图</div><div class="metric"><strong id="static-count">—</strong>静态页面与组件预览</div><div class="metric"><strong id="button-count">—</strong>已审计标准 Button</div></div></header>
<main><div class="notice"><b>两种图片对应不同的验证范围。</b>“实际运行”来自已启动游戏的 Game 画面；“静态预览”显示 Prefab 的序列化默认值，禁用了业务脚本和动画，可能包含默认语言、占位金额或默认状态，不代表账号数据与业务验证。此画廊不替代 Android / iOS 真机验收。</div>
<div class="toolbar"><div class="filters" role="group" aria-label="筛选图片"><button class="filter active" data-filter="all">全部</button><button class="filter" data-filter="runtime">实际运行</button><button class="filter" data-filter="static">静态预览</button></div><input id="search" class="search" type="search" placeholder="搜索页面、组件或文件名…" aria-label="搜索页面"></div>
<div class="result-line"><span id="result-count"></span><span>点击图片打开原图 ↗</span></div><div id="grid" class="grid"></div><div id="empty" class="empty">没有匹配的图片，请更换关键词或筛选条件。</div></main>
<footer><div id="audit-summary"></div><div id="build-time"></div><div>图片均链接到本次验证生成的本地 PNG；未生成替代图片。运行页面名称从同名 JSON 的页面层级识别，缺少诊断时明确标注。</div></footer>
<script id="gallery-data" type="application/json">__DATA__</script>
<script>
const data=JSON.parse(document.getElementById('gallery-data').textContent);
let currentFilter='all';let query='';
const grid=document.getElementById('grid');
const add=(tag,className,text)=>{const node=document.createElement(tag);if(className)node.className=className;if(text!==undefined)node.textContent=text;return node};
const link=(text,url)=>{const node=add('a','',text);node.href=url;node.target='_blank';node.rel='noopener';return node};
document.getElementById('runtime-count').textContent=data.runtimeCount;
document.getElementById('static-count').textContent=data.staticCount;
document.getElementById('button-count').textContent=data.buttonCount??'待审计';
document.getElementById('build-time').textContent='画廊生成时间：'+data.generated+' · 静态预览报告：'+data.previewUtc;
document.getElementById('audit-summary').textContent=data.newErrors==null?'最终审计结果请查看审计文件。':'结构审计记录：新增错误 '+data.newErrors+'，既存问题 '+data.existingIssues+'，已解决 '+data.resolvedIssues+'。该统计不等同于视觉或真机通过。';
function render(){grid.replaceChildren();const entries=data.cards.filter(item=>(currentFilter==='all'||item.kind===currentFilter)&&item.search.toLocaleLowerCase().includes(query));
for(const item of entries){const card=add('article','card');const thumb=link('',item.image);thumb.className='thumb';const image=add('img');image.src=item.image;image.alt=item.name+' · '+(item.kind==='runtime'?'实际运行':'静态预览');image.loading='lazy';image.decoding='async';thumb.append(image);card.append(thumb);
const meta=add('div','meta');meta.append(add('span','tag '+item.kind,item.kind==='runtime'?'实际运行':'静态预览'));if(item.error)meta.append(add('span','tag error','需检查'));meta.append(add('h2','',item.name));meta.append(add('div','asset-id',item.id));meta.append(add('p','description',item.detail));if(item.error)meta.append(add('p','description error-copy',item.error));
if(item.kind==='runtime'){meta.append(add('div','small',item.time));if(item.notes.length)meta.append(add('p','description',item.notes.join(' · ')));}else meta.title=item.prefab;
const links=add('div','card-links');links.append(link('查看原图 ↗',item.image));if(item.diagnostic)links.append(link('运行诊断 ↗',item.diagnostic));meta.append(links);card.append(meta);grid.append(card)}
document.getElementById('result-count').textContent='显示 '+entries.length+' / '+data.cards.length+' 张图片';document.getElementById('empty').style.display=entries.length?'none':'block';}
document.querySelectorAll('[data-filter]').forEach(button=>button.addEventListener('click',()=>{currentFilter=button.dataset.filter;document.querySelectorAll('[data-filter]').forEach(item=>item.classList.toggle('active',item===button));render()}));
document.getElementById('search').addEventListener('input',event=>{query=event.target.value.trim().toLocaleLowerCase();render()});render();
</script></body></html>'''


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--directory", type=Path, default=Path(__file__).resolve().parent)
    args = parser.parse_args()
    base = args.directory.resolve()
    payload, tracked = collect(base)
    serialized = json.dumps(payload, ensure_ascii=False, separators=(",", ":")).replace("<", "\\u003c")
    page = TEMPLATE.replace("__DATA__", serialized)
    for path, original in tracked.items():
        stat = path.stat()
        if original != (stat.st_size, stat.st_mtime_ns):
            raise SystemExit(f"报告或图片正在重写，未生成画廊，请等验证完成：{path}")
    output = base / "gallery.html"
    output.write_text(page, encoding="utf-8")
    print(f"已生成：{output}")
    print(f"静态预览 {payload['staticCount']}，实际运行 {payload['runtimeCount']}，总计 {len(payload['cards'])} 张。")


if __name__ == "__main__":
    main()
