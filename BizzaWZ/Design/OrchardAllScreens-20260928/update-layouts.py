from pathlib import Path
import json
root=Path(__file__).resolve().parents[2]
base=Path(__file__).parent
data=json.loads((base/'layouts-before.json').read_text(encoding='utf-8-sig'))
pages={p['name']:p for p in data['pages']}
for p in data['pages']:
    if p['name'] in ('withdraw-main','loading','game-hud','tutorial'): continue
    for b in p.get('boxes',[]):
        if b.get('font',0)>0:b['font']=round(b['font']*1.15,1)
    for c in p.get('captions',[]):c['font']=round(c['font']*(1.20 if c.get('title') else 1.14),1)
    if p['name'] in ('level-complete','new-booster'):
        p['backdrop']=True;p['backdropResource']='OrchardUI/RewardBackdrop'
    if p['name'] in ('revive','get-booster','rate-up','welcome-gift','daily-mission','lucky-help','service-topics'):
        # These overlays retain the live game or parent page beneath them.
        p['backdrop']=False
    if p['name'] in ('revive','get-booster','new-booster','rate-up','welcome-gift','daily-mission','lucky-help'):
        for b in p.get('boxes',[])+p.get('decorations',[]):
            if b.get('role')=='Title':b['role']='detail:WoodTitle'
def cap(page,path,en,pt):
    for c in pages[page].get('captions',[]):
        if c['path']==path:c['en'],c['pt']=en,pt
cap('cash-withdraw','ButtomGroup/Title','Cash\nwithdrawal','Saque em\ndinheiro')
cap('service','Title/Title','Support','Atendimento')
cap('daily-tasks','ApprovedSubtitle','Enjoy your time in the orchard.','Aproveite seu tempo no pomar.')
for c in pages['settings']['captions']:
    if c['path']=='BG (1)/Text (TMP)':c['font']=72;c['w']=490;c['x']=180
for c in pages['level-complete']['captions']:
    if c['path']=='BG/Text (TMP)':c['font']=108;c['x']=178;c['y']=228;c['w']=498;c['h']=119
for p in ('rating','faq','history'):
    for c in pages[p]['captions']:
        if 'ApprovedSection' in c['path'] or 'ApprovedQuestion' in c['path']:c['font']*=1.08
def box(page,path,**values):
    for b in pages[page].get('boxes',[])+pages[page].get('captions',[])+pages[page].get('decorations',[]):
        if b['path']==path:b.update(values)
c='Content/Scroll View/Viewport/Content/'
for path in ('Content/Scroll View','Content/Scroll View/Viewport','Content/Scroll View/Viewport/Content'):
    box('withdraw-milestones',path,x=34,y=255,w=784,h=1216)
for suffix in ('WithdrawInfo','WithdrawInfo/bg'):box('withdraw-milestones',c+suffix,x=52,y=281,w=746,h=200,**({'role':'Input'} if suffix.endswith('/bg') else {}))
box('withdraw-milestones',c+'WithdrawInfo/Title',x=259,y=315,w=367,h=57,font=44)
box('withdraw-milestones',c+'WithdrawInfo/CoinInfo/RealCurrent/Image',x=78,y=310,w=156,h=152)
box('withdraw-milestones',c+'WithdrawInfo/CoinInfo/RealCurrent/Text (TMP)',x=259,y=369,w=295,h=87,font=96)
for suffix in ('WithdrawInfo/WithdrawBtn','WithdrawInfo/WithdrawBtn/Btn'):box('withdraw-milestones',c+suffix,x=580,y=341,w=210,h=97)
box('withdraw-milestones',c+'WithdrawInfo/WithdrawBtn/Btn/Text (TMP)',x=593,y=354,w=184,h=70,font=48)
for suffix in ('WithdrawalInstruction','WithdrawalInstruction/bg'):box('withdraw-milestones',c+suffix,x=52,y=502,w=746,h=134,**({'role':'Input'} if suffix.endswith('/bg') else {}))
box('withdraw-milestones',c+'WithdrawalInstruction/Title/DailyText',x=77,y=527,w=171,h=51,font=43)
for suffix in ('WithdrawalInstruction/Progress','WithdrawalInstruction/Progress/bg'):box('withdraw-milestones',c+suffix,x=257,y=528,w=516,h=53)
box('withdraw-milestones',c+'WithdrawalInstruction/Progress/real',x=261,y=532,w=508,h=45)
box('withdraw-milestones',c+'WithdrawalInstruction/Progress/progressText',x=276,y=530,w=470,h=49,font=42)
box('withdraw-milestones',c+'WithdrawalInstruction/Hint',x=273,y=588,w=496,h=38,font=33)
for suffix in ('WithdrawDan','WithdrawDan/bg'):box('withdraw-milestones',c+suffix,x=33,y=655,w=786,h=816)
for suffix in ('WithdrawDan/bg/WithdrawMode','WithdrawDan/bg/WithdrawMode/Scroll View','WithdrawDan/bg/WithdrawMode/Scroll View/Viewport'):box('withdraw-milestones',c+suffix,x=61,y=746,w=724,h=699)
box('withdraw-milestones','ButtomGroup/CloseBtn',x=42,y=103,w=99,h=102)
box('withdraw-milestones','ButtomGroup/FQA',x=707,y=103,w=99,h=102)
box('withdraw-milestones','Title/bg (1)',x=171,y=94,w=508,h=121)
box('withdraw-milestones','Title/Text',x=195,y=111,w=460,h=87,font=65)
box('faq','ApprovedSupport',role='ButtonBlue')
(root/'Design/OrchardImplementation-20260928/layouts.json').write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
print('Updated '+str(len(pages))+' screen layout definitions from the preserved baseline.')
