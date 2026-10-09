
"use strict";
const ARTWORKS = [
  {
    "id": "00",
    "slug": "00-approved-withdrawal",
    "title": "已确认提现主界面",
    "english": "Sacar",
    "category": "approved",
    "note": "已确认的视觉基准：明亮果园、奶油金边、绿色主按钮、双列卡片。",
    "badge": "已确认样稿",
    "image": "images/00-approved-withdrawal.png",
    "prompt": "prompts/00-approved-withdrawal.md"
  },
  {
    "id": "06",
    "slug": "06-settings",
    "title": "设置与暂停",
    "english": "Configurações",
    "category": "system",
    "note": "音乐、音效、振动、语言与继续/重开，葡语界面。",
    "badge": "",
    "image": "images/06-settings.png",
    "prompt": "prompts/06-settings.md"
  },
  {
    "id": "09",
    "slug": "09-daily-tasks",
    "title": "每日任务（框架附录）",
    "english": "Tarefas diárias",
    "category": "system",
    "note": "保留原有在线时长任务，当前未启用的框架页面视觉稿。",
    "badge": "框架附录 / 现有分支",
    "image": "images/09-daily-tasks.png",
    "prompt": "prompts/09-daily-tasks.md"
  },
  {
    "id": "10",
    "slug": "10-rating",
    "title": "游戏评价",
    "english": "Sua opinião",
    "category": "system",
    "note": "五颗星与确认操作，四星选中状态示例。",
    "badge": "",
    "image": "images/10-rating.png",
    "prompt": "prompts/10-rating.md"
  },
  {
    "id": "11",
    "slug": "11-cash-withdrawal",
    "title": "现金提现（框架分支）",
    "english": "Saque em dinheiro",
    "category": "system",
    "note": "现有现金业务分支，余额不足时主操作呈禁用状态。",
    "badge": "框架附录 / 现有分支",
    "image": "images/11-cash-withdrawal.png",
    "prompt": "prompts/11-cash-withdrawal.md"
  },
  {
    "id": "12",
    "slug": "12-payout-account",
    "title": "收款账户",
    "english": "Conta de saque",
    "category": "withdrawal",
    "note": "PagBank 巴西账户表单：姓名、CPF/CNPJ 与邮箱。",
    "badge": "",
    "image": "images/12-payout-account.png",
    "prompt": "prompts/12-payout-account.md"
  },
  {
    "id": "13",
    "slug": "13-withdraw-confirm",
    "title": "提现确认",
    "english": "Confirmar saque",
    "category": "withdrawal",
    "note": "提交前核对金额、方法和收款账户。",
    "badge": "",
    "image": "images/13-withdraw-confirm.png",
    "prompt": "prompts/13-withdraw-confirm.md"
  },
  {
    "id": "14",
    "slug": "14-withdraw-pending",
    "title": "提现处理中",
    "english": "Processando",
    "category": "withdrawal",
    "note": "显示请求处理中状态，等待服务端结果。",
    "badge": "",
    "image": "images/14-withdraw-pending.png",
    "prompt": "prompts/14-withdraw-pending.md"
  },
  {
    "id": "15",
    "slug": "15-withdraw-history",
    "title": "提现记录",
    "english": "Histórico",
    "category": "withdrawal",
    "note": "在一页里呈现处理中、完成与未完成的记录状态。",
    "badge": "",
    "image": "images/15-withdraw-history.png",
    "prompt": "prompts/15-withdraw-history.md"
  },
  {
    "id": "16",
    "slug": "16-withdraw-milestones",
    "title": "段位奖励（框架分支）",
    "english": "Minhas conquistas",
    "category": "system",
    "note": "现有段位奖励分支，展示已领取、可领取与锁定状态。",
    "badge": "框架附录 / 现有分支",
    "image": "images/16-withdraw-milestones.png",
    "prompt": "prompts/16-withdraw-milestones.md"
  },
  {
    "id": "17",
    "slug": "17-withdraw-reminder",
    "title": "可提现提醒",
    "english": "Saldo disponível",
    "category": "system",
    "note": "余额与可提现金额提醒，进入主提现页面。",
    "badge": "框架附录 / 现有分支",
    "image": "images/17-withdraw-reminder.png",
    "prompt": "prompts/17-withdraw-reminder.md"
  },
  {
    "id": "19",
    "slug": "19-faq",
    "title": "常见问题",
    "english": "Ajuda",
    "category": "withdrawal",
    "note": "围绕提现与记录的简短帮助内容。",
    "badge": "",
    "image": "images/19-faq.png",
    "prompt": "prompts/19-faq.md"
  },
  {
    "id": "20",
    "slug": "20-support-chat",
    "title": "客服对话",
    "english": "Atendimento",
    "category": "withdrawal",
    "note": "客服聊天、快捷问题入口和输入区。",
    "badge": "",
    "image": "images/20-support-chat.png",
    "prompt": "prompts/20-support-chat.md"
  },
  {
    "id": "21",
    "slug": "21-support-topics",
    "title": "客服快捷问题",
    "english": "Perguntas rápidas",
    "category": "withdrawal",
    "note": "七条快捷问题与自定义消息入口。",
    "badge": "",
    "image": "images/21-support-topics-v2.png",
    "prompt": "prompts/21-support-topics.md"
  },
  {
    "id": "26",
    "slug": "26-notification",
    "title": "通用提示",
    "english": "Aviso",
    "category": "system",
    "note": "通用通知样式，以网络暂停为示例。",
    "badge": "",
    "image": "images/26-notification.png",
    "prompt": "prompts/26-notification.md"
  }
];
const CATEGORIES = [{"id": "all", "label": "全部界面"}, {"id": "approved", "label": "已确认样稿"}, {"id": "withdrawal", "label": "提现服务"}, {"id": "system", "label": "系统与附录"}];
const gallery=document.getElementById("gallery");
const filters=document.querySelector(".filters");
const lightbox=document.getElementById("lightbox");
const viewerImage=document.getElementById("viewer-image");
const viewerError=document.getElementById("viewer-error");
let activeCategory="all", visibleEntries=ARTWORKS, viewerIndex=0, previousFocus=null;
function node(tag,className,text){const el=document.createElement(tag);if(className)el.className=className;if(text!==undefined)el.textContent=text;return el;}
function renderFilters(){
  filters.replaceChildren();
  CATEGORIES.forEach(category=>{
    const count=category.id==="all"?ARTWORKS.length:ARTWORKS.filter(item=>item.category===category.id).length;
    const button=node("button","filter",category.label);button.type="button";button.setAttribute("aria-pressed",String(activeCategory===category.id));
    button.append(node("span","filter-count",String(count)));
    button.addEventListener("click",()=>{activeCategory=category.id;renderGallery();renderFilters();const buttons=filters.querySelectorAll(".filter");buttons[CATEGORIES.findIndex(c=>c.id===category.id)].focus();});
    filters.append(button);
  });
  const counter=node("span","result-count",visibleEntries.length+" 个审阅条目");counter.setAttribute("aria-live","polite");filters.append(counter);
}
function renderGallery(){
  visibleEntries=activeCategory==="all"?ARTWORKS:ARTWORKS.filter(item=>item.category===activeCategory);
  gallery.replaceChildren();
  visibleEntries.forEach((item,index)=>{
    const card=node("article","art-card");card.dataset.id=item.id;
    const button=node("button","image-button");button.type="button";button.setAttribute("aria-label","放大查看 "+item.id+" "+item.title);
    const img=node("img");img.src=item.image;img.alt=item.title+" · "+item.english+" 竖屏效果图";img.loading="lazy";img.decoding="async";
    img.addEventListener("error",()=>button.classList.add("is-missing"));
    img.addEventListener("load",()=>button.classList.remove("is-missing"));
    const missing=node("div","missing");missing.append(node("strong","",item.title),node("span","","图片文件准备中"));
    button.append(img,missing,node("span","zoom-hint","放大查看 ↗"));button.addEventListener("click",()=>openViewer(index,button));card.append(button);
    const head=node("div","card-head");head.append(node("span","card-number",item.id),node("h2","",item.title));card.append(head,node("p","english",item.english));
    if(item.badge)card.append(node("span","badge",item.badge));
    card.append(node("p","card-note",item.note));
    const links=node("div","card-links");
    const original=node("a","","原 PNG ↗");original.href=item.image;original.target="_blank";original.rel="noopener";
    const prompt=node("a","","提示词 ↗");prompt.href=item.prompt;prompt.target="_blank";prompt.rel="noopener";
    links.append(original,prompt);card.append(links);gallery.append(card);
  });
}
function showViewerImage(){
  const item=visibleEntries[viewerIndex];
  document.getElementById("viewer-title").replaceChildren(node("span","",item.id),document.createTextNode(item.title+" · "+item.english));
  document.getElementById("viewer-original").href=item.image;
  document.getElementById("viewer-note").textContent=(item.badge?item.badge+"。 ":"")+item.note;
  document.getElementById("viewer-position").textContent=(viewerIndex+1)+" / "+visibleEntries.length+"　← → 浏览　Esc 关闭";
  viewerError.hidden=true;viewerImage.style.visibility="visible";viewerImage.alt=item.title+" · 放大效果图";viewerImage.src=item.image;
}
function openViewer(index,opener){viewerIndex=index;previousFocus=opener;showViewerImage();lightbox.showModal();document.body.style.overflow="hidden";document.getElementById("viewer-close").focus();}
function moveViewer(delta){viewerIndex=(viewerIndex+delta+visibleEntries.length)%visibleEntries.length;showViewerImage();}
function closeViewer(){lightbox.close();}
viewerImage.addEventListener("error",()=>{viewerImage.style.visibility="hidden";viewerError.hidden=false;});
viewerImage.addEventListener("load",()=>{viewerImage.style.visibility="visible";viewerError.hidden=true;});
document.getElementById("viewer-close").addEventListener("click",closeViewer);
document.getElementById("viewer-prev").addEventListener("click",()=>moveViewer(-1));
document.getElementById("viewer-next").addEventListener("click",()=>moveViewer(1));
lightbox.addEventListener("close",()=>{document.body.style.overflow="";if(previousFocus)previousFocus.focus();});
lightbox.addEventListener("keydown",event=>{if(event.key==="ArrowLeft"){event.preventDefault();moveViewer(-1);}else if(event.key==="ArrowRight"){event.preventDefault();moveViewer(1);}});
renderGallery();renderFilters();
