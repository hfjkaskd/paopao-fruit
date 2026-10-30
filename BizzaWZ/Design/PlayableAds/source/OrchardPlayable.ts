import { _decorator, Component, Node, JsonAsset, Label, Button, Sprite, SpriteFrame, Color, Vec3, tween, Tween, view, ResolutionPolicy, UITransform, game, Camera, Canvas } from 'cc';
const {ccclass, property} = _decorator;
declare global { interface Window { OrchardAd:any; OrchardPlayable:any; OrchardAudio:any; } }
type Fruit = {node:Node, button:Button, sprite:Sprite, tile:any, home:Vec3, state:number, type:number, w:number, h:number};

@ccclass('OrchardPlayable')
export class OrchardPlayable extends Component {
    @property(JsonAsset) config:JsonAsset = null!;
    @property(Node) board:Node = null!;
    @property([Node]) boards:Node[] = [];
    @property([Node]) backgrounds:Node[] = [];
    @property(Node) slots:Node = null!;
    @property(Node) levelLabel:Node = null!;
    @property(Node) progress:Node = null!;
    @property(Node) guide:Node = null!;
    @property(Node) finger:Node = null!;
    @property(Node) result:Node = null!;
    @property(Node) resultTitle:Node = null!;
    @property(Node) resultText:Node = null!;
    @property(Node) nextButton:Node = null!;
    @property(Node) nextText:Node = null!;
    @property(Node) retryButton:Node = null!;
    @property(Node) installButton:Node = null!;
    @property(Node) undoButton:Node = null!;
    @property(Node) shuffleButton:Node = null!;
    private cfg:any;
    private all:Fruit[][]=[];
    private basket:Fruit[]=[];
    private history:Fruit[]=[];
    private current=0;
    private matched=0;
    private guideActive=true;
    private guideCompleted=false;
    private busy=false;
    private ready=false;
    private ended=false;
    private undoLeft=1;
    private shuffleLeft=1;
    private landscape=false;
    private white=new Color(255,255,255,255);
    private dim=new Color(90,98,105,255);
    private locked=new Color(150,150,150,255);
    private progressLabel:Label=null!;
    private countUndo:Label=null!;
    private countShuffle:Label=null!;
    private frames=new Map<number,SpriteFrame>();
    private camera:Camera=null!;
    private cashLabel:Label=null!;
    private luckyCount:Label=null!;
    private cashTarget:Node=null!;
    private flights:Node[]=[];
    private rootTransform:UITransform=null!;
    private skipButton:Node=null!;
    private settingsPanel:Node=null!;
    private settingsOpen=false;
    private muted=false;
    private storePrompt=false;
    private previousCount=0;

    onLoad() {
        this.cfg=this.config.json;
        this.applyLocale();
        this.progressLabel=this.progress.getComponent(Label)!;
        this.countUndo=this.undoButton.getChildByName('Count')!.getComponent(Label)!;
        this.countShuffle=this.shuffleButton.getChildByName('Count')!.getComponent(Label)!;
        const header=this.node.getChildByName('Header')!;
        this.cashTarget=header.getChildByName('CashBalance')!;
        this.cashLabel=this.cashTarget.getChildByName('Amount')!.getComponent(Label)!;
        this.luckyCount=this.node.getChildByName('Props')!.getChildByName('Lucky')!.getChildByName('Count')!.getComponent(Label)!;
        this.flights=this.node.getChildByName('RewardFlights')!.children;
        this.rootTransform=this.node.getComponent(UITransform)!;
        this.skipButton=this.result.getChildByName('Skip')!;
        this.settingsPanel=this.node.getChildByName('Settings')!;
        this.skipButton.on(Button.EventType.CLICK,()=>this.next(),this);
        header.getChildByName('SettingsButton')!.on(Button.EventType.CLICK,()=>{if(!this.ready||this.ended)return;this.settingsOpen=true;this.settingsPanel.active=true;this.refresh();},this);
        this.settingsPanel.getChildByName('Resume')!.on(Button.EventType.CLICK,()=>{if(!this.ready)return;this.settingsOpen=false;this.settingsPanel.active=false;this.refresh();},this);
        this.settingsPanel.getChildByName('Sound')!.on(Button.EventType.CLICK,()=>{if(!this.ready)return;this.muted=!this.muted;if(window.OrchardAudio)window.OrchardAudio.mute(this.muted);this.settingsPanel.getChildByName('Sound')!.getChildByName('Text')!.getComponent(Label)!.string=this.muted?this.cfg.locale.soundOff:this.cfg.locale.soundOn;},this);
        for(const counter of ['CoinBalance','CashBalance'])header.getChildByName(counter)!.getChildByName('Withdraw')!.on(Button.EventType.CLICK,()=>this.showStorePrompt(),this);
        for(const entry of ['Lucky','Gift'])this.node.getChildByName('Props')!.getChildByName(entry)!.on(Button.EventType.CLICK,()=>this.showStorePrompt(),this);
        for (let i=0;i<this.boards.length;i++) {
            const tiles=this.cfg.levels[i].normalTiles.slice().sort((a:any,b:any)=>a.layerIndex-b.layerIndex||a.indexInLayer-b.indexInLayer);
            const fruits:Fruit[]=[];
            for(let j=0;j<tiles.length;j++) {
                const n=this.boards[i].children[j]; const t=tiles[j];const size=this.cfg.sizes[t.eleType];
                const f:Fruit={node:n,button:n.getComponent(Button)!,sprite:n.getComponent(Sprite)!,tile:t,home:n.position.clone(),state:0,type:t.eleType,w:size[0],h:size[1]};
                this.frames.set(f.type,f.sprite.spriteFrame!);
                n.on(Button.EventType.CLICK,()=>this.pick(f),this);fruits.push(f);
            }
            this.all.push(fruits);
        }
        this.nextButton.on(Button.EventType.CLICK,()=>this.next(),this);
        this.retryButton.on(Button.EventType.CLICK,()=>this.retry(),this);
        this.installButton.on(Button.EventType.CLICK,()=>window.OrchardAd.install(this.cfg.store),this);
        this.undoButton.on(Button.EventType.CLICK,()=>this.undo(),this);
        this.shuffleButton.on(Button.EventType.CLICK,()=>this.shuffle(),this);
        this.nextText.getComponent(Label)!.string=this.cfg.text.common_continue||'Continue';
        this.installButton.getChildByName('InstallText')!.getComponent(Label)!.string=this.cfg.text.common_download||'Download';
        window.OrchardPlayable=this;
        this.load(0);
        this.fit();window.addEventListener('resize',this.onResize);
        window.OrchardAd.attach({start:()=>{this.ready=true;game.resume();this.refresh();},pause:()=>{this.ready=false;this.refresh();game.pause();},resume:()=>{this.ready=true;game.resume();this.refresh();},close:()=>{this.ready=false;this.refresh();game.pause();}});
    }
    private applyLocale(){
        const l=(window as any).ORCHARD_LOCALIZATION||{guide:'Tap 3 matching fruits!',download:'Download',withdraw:'Withdraw',win:'Well Done!',claim:'Claim',skip:'No Thanks',retry:'Restart',settings:'Settings',soundOn:'Sound: On',soundOff:'Sound: Off',resume:'Continue',lose:'Try again!'};
        this.cfg.locale=l;this.cfg.text.common_download=l.download;this.cfg.text.common_continue=l.resume;
        const label=(n:Node,t:string)=>{n.getComponent(Label)!.string=t;};
        const h=this.node.getChildByName('Header')!;
        label(h.getChildByName('CashBalance')!.getChildByName('Withdraw')!.getChildByName('Text')!,l.withdraw);
        label(this.guide.getChildByName('GuideText')!,l.guide);
        label(this.result.getChildByName('Skip')!.getChildByName('SkipText')!,l.skip);
        label(this.retryButton.getChildByName('RetryText')!,l.retry);
        const settings=this.node.getChildByName('Settings')!;
        label(settings.getChildByName('Title')!,l.settings);
        label(settings.getChildByName('Sound')!.getChildByName('Text')!,l.soundOn);
        label(settings.getChildByName('Resume')!.getChildByName('Text')!,l.resume);
    }
    private onResize=()=>{this.fit();setTimeout(()=>this.fit(),0);};
    start(){this.fit();}
    update(){if(this.camera){const p=this.node.parent!.worldPosition,c=this.camera.node.worldPosition;if(p.x!==c.x||p.y!==c.y)this.camera.node.setWorldPosition(p.x,p.y,1000);}}
    onDestroy(){window.removeEventListener('resize',this.onResize);}
    private fit(){
        this.landscape=false;
        const aspect=window.innerWidth/window.innerHeight;
        const designH=this.cfg.portraitHeight;
        const designW=designH*aspect;
        view.setDesignResolutionSize(designW,designH,ResolutionPolicy.EXACT_FIT);
        this.node.parent!.getComponent(Canvas)!.alignCanvasWithScreen=false;
        const camera=this.node.parent!.getChildByName('Camera')!.getComponent(Camera)!;
        this.camera=camera;
        camera.orthoHeight=designH/2;
        camera.node.setWorldPosition(this.node.parent!.worldPosition.x,this.node.parent!.worldPosition.y,1000);
        const place=(name:string,x:number,y:number,s:number)=>{const n=this.node.getChildByName(name)!;n.setPosition(x,y);n.setScale(s,s,1);};
        if(this.landscape){
            place('Header',530,440,.85);place('Board',-430,-240,.7);place('Basket',-430,-435,.7);place('Slots',-430,48,.7);place('BasketLock',-108,-417,.7);
            place('Props',530,-45,.85);place('Install',530,-430,1.6);place('Guide',-430,-240,.7);
            this.result.setScale(.74,.74,1);this.settingsPanel.setScale(.85,.85,1);
        }else{
            const hudScale=Math.min(1,designW/this.cfg.portraitWidth);
            place('Header',0,1180-208*hudScale,hudScale);place('Board',0,0,1);place('Basket',0,-684,1);place('Slots',0,0,1);place('BasketLock',431,-665,1);place('Props',0,-1180+222*hudScale,hudScale);place('Install',0,-1140,hudScale);place('Guide',0,0,1);
            this.result.setScale(1,1,1);this.settingsPanel.setScale(1,1,1);
        }
        this.result.getChildByName('Shade')!.getComponent(UITransform)!.setContentSize(designW/this.result.scale.x,designH/this.result.scale.y);
        this.settingsPanel.getChildByName('Shade')!.getComponent(UITransform)!.setContentSize(designW/this.settingsPanel.scale.x,designH/this.settingsPanel.scale.y);
        const cover=1;
        this.node.getChildByName('Background')!.setScale(cover,cover,1);
        for(const n of this.backgrounds)n.setScale(cover,cover,1);
    }
    private load(index:number){
        this.current=index;this.matched=0;this.basket.length=0;this.history.length=0;this.busy=false;this.ended=false;this.storePrompt=false;this.settingsOpen=false;this.settingsPanel.active=false;
        this.guideActive=index===0&&!this.guideCompleted;this.undoLeft=1;this.shuffleLeft=1;
        this.result.active=false;
        for(let i=0;i<this.all.length;i++){
            this.boards[i].active=i===index;this.backgrounds[i].active=i===index;
            const fs=this.all[i];for(let j=0;j<fs.length;j++){
                const f=fs[j];Tween.stopAllByTarget(f.node);this.setType(f,f.tile.eleType);f.home.set(f.tile.posX,f.tile.posY+this.cfg.boardOffsetY,0);f.node.setParent(this.boards[i]);f.node.setSiblingIndex(j);f.node.position=f.home;f.node.setScale(1,1,1);f.node.active=true;f.state=0;
            }
        }
        this.levelLabel.getComponent(Label)!.string=String(index+1);
        this.previousCount=0;for(const n of this.flights){Tween.stopAllByTarget(n);n.active=false;}
        this.countUndo.string='1';this.countShuffle.string='1';this.refresh();
    }
    private occlusion(f:Fruit){
        const higher=this.all[this.current];let covered=0,total=0;
        for(let iy=0;iy<12;iy++)for(let ix=0;ix<12;ix++){
            const u=(ix+.5)/12*2-1,v=(iy+.5)/12*2-1;
            const x=f.home.x+u*f.w*.5,y=f.home.y+v*f.h*.5;
            const weight=Math.abs(u)<=.25&&Math.abs(v)<=.25?3:1;total+=weight;
            for(const other of higher){if(other===f||other.state!==0)continue;
                if(other.tile.layerIndex*10000+other.tile.indexInLayer<=f.tile.layerIndex*10000+f.tile.indexInLayer)continue;
                if(Math.abs(x-other.home.x)<=other.w*.5&&Math.abs(y-other.home.y)<=other.h*.5){covered+=weight;break;}
            }
        }return covered/total;
    }
    private refresh(){
        const fs=this.all[this.current];
        for(const f of fs){
            const blocked=f.state!==0||this.occlusion(f)>.8;
            f.button.interactable=this.ready&&!this.settingsOpen&&!this.busy&&!this.ended&&!blocked&&(!this.guideActive||f.type===this.cfg.guideType);
            f.sprite.color=this.guideActive&&f.type!==this.cfg.guideType?this.dim:blocked&&f.state===0?this.locked:this.white;
        }
        this.guide.active=this.guideActive&&!this.ended&&!this.settingsOpen;
        Tween.stopAllByTarget(this.finger);
        if(this.guideActive){
            const target=fs.find(f=>f.type===this.cfg.guideType&&f.state===0);
            if(target){const p=new Vec3(target.home.x+55,target.home.y-65,0);this.finger.position=p;tween(this.finger).to(.48,{position:new Vec3(p.x,p.y-18,0)}).to(.48,{position:p}).union().repeatForever().start();}
        }
        this.progressLabel.string=String(this.matched/3)+' / '+String(fs.length/3);
        this.cashLabel.string=(this.cfg.displayBalances[this.current]+(this.cfg.displayBalances[this.current+1]-this.cfg.displayBalances[this.current])*this.matched/fs.length).toFixed(2);
        this.luckyCount.string=String((this.matched/3)%5)+'/5';
        this.undoButton.getComponent(Button)!.interactable=this.ready&&!this.settingsOpen&&!this.busy&&!this.ended&&!this.guideActive&&this.undoLeft>0&&this.history.some(f=>f.state===1);
        this.shuffleButton.getComponent(Button)!.interactable=this.ready&&!this.settingsOpen&&!this.busy&&!this.ended&&!this.guideActive&&this.shuffleLeft>0;
    }
    private arrange(){
        for(let i=0;i<this.basket.length;i++){
            const f=this.basket[i],scale=Math.min(1,this.cfg.slotScale/Math.max(f.w,f.h));
            tween(f.node).to(this.cfg.motionSeconds,{position:new Vec3(this.cfg.slotFirstX+i*this.cfg.slotInterval,this.cfg.slotY,0),scale:new Vec3(scale,scale,1)},{easing:'quadOut'}).start();
        }
    }
    private pick(f:Fruit){
        if(!this.ready||!f.button.interactable||f.state!==0)return;
        if(window.OrchardAudio)window.OrchardAudio.play('tap');
        this.busy=true;f.state=1;this.history.push(f);
        let index=this.basket.length;
        for(let j=this.basket.length-1;j>=0;j--)if(this.basket[j].type===f.type){index=j+1;break;}
        this.basket.splice(index,0,f);f.node.setParent(this.slots,true);f.node.setScale(1,1,1);this.arrange();this.refresh();
        this.scheduleOnce(()=>this.resolve(),this.cfg.motionSeconds+.02);
    }
    private resolve(){
        let start=-1;
        for(let i=0;i<=this.basket.length-3;i++)if(this.basket[i].type===this.basket[i+1].type&&this.basket[i].type===this.basket[i+2].type){start=i;break;}
        if(start>=0){
            if(window.OrchardAudio)window.OrchardAudio.play('clear');
            const group=this.basket.splice(start,3);this.matched+=3;
            this.flyReward(group[0].node);
            if(this.guideActive&&group[0].type===this.cfg.guideType){this.guideActive=false;this.guideCompleted=true;}
            for(const f of group){f.state=2;tween(f.node).to(this.cfg.matchSeconds,{scale:new Vec3(0,0,1)}).call(()=>{f.node.active=false;}).start();}
            this.arrange();this.refresh();this.scheduleOnce(()=>this.afterMatch(),this.cfg.matchSeconds+.02);
        }else this.afterMatch();
    }
    private afterMatch(){
        this.busy=false;
        if(this.matched===this.all[this.current].length)this.finish(true);
        else if(this.basket.length>=this.cfg.capacity)this.finish(false);
        else this.refresh();
    }
    private finish(won:boolean){
        if(won&&window.OrchardAudio)window.OrchardAudio.play('win');
        this.ended=true;this.result.active=true;this.guide.active=false;
        const final=won&&this.current===this.all.length-1;
        this.storePrompt=false;
        this.resultTitle.getComponent(Label)!.string=won?this.cfg.locale.win:this.cfg.locale.lose;
        this.resultText.getComponent(Label)!.string='$ '+Number(this.cfg.levelRewards[this.current]).toFixed(2);
        this.result.getChildByName('RewardCoin')!.active=won;this.result.getChildByName('RewardCash')!.active=won;this.result.getChildByName('RewardCoinsText')!.active=won;this.resultText.active=won;
        this.nextButton.active=won;this.retryButton.active=!won||final;this.skipButton.active=won&&!final;
        this.nextText.getComponent(Label)!.string=final?(this.cfg.text.common_download||'Download'):this.cfg.locale.claim;
        if(final||!won)window.OrchardAd.end();
        Tween.stopAllByTarget(this.nextButton);
        if(final)tween(this.nextButton).to(.65,{scale:new Vec3(1.05,1.05,1)}).to(.65,{scale:new Vec3(1,1,1)}).union().repeatForever().start();
        this.refresh();
    }
    private next(){if(!this.ready||!this.ended)return;if(this.storePrompt||this.current===this.all.length-1)window.OrchardAd.install(this.cfg.store);else this.load(this.current+1);}
    private retry(){if(!this.ready||!this.ended)return;if(this.storePrompt){this.storePrompt=false;this.ended=false;this.result.active=false;this.refresh();return;}window.OrchardAd.retry();this.load(this.current);}
    private showStorePrompt(){if(!this.ready||this.ended||this.busy)return;this.storePrompt=true;this.ended=true;this.result.active=true;this.resultTitle.getComponent(Label)!.string='Orchard Trio';this.resultText.active=false;for(const name of ['RewardCoin','RewardCash','RewardCoinsText'])this.result.getChildByName(name)!.active=false;this.nextButton.active=true;this.nextText.getComponent(Label)!.string=this.cfg.locale.download;this.retryButton.active=true;this.skipButton.active=false;this.refresh();}
    private flyReward(source:Node){const start=this.rootTransform.convertToNodeSpaceAR(source.worldPosition),target=this.rootTransform.convertToNodeSpaceAR(this.cashTarget.worldPosition);for(let i=0;i<this.flights.length;i++){const n=this.flights[i];Tween.stopAllByTarget(n);n.active=true;n.position=start;n.setScale(.65,.65,1);const mid=new Vec3(start.x+(i%5-2)*80,start.y+150+(i%3)*80,0);tween(n).delay(i*.025).to(.22,{position:mid,scale:new Vec3(1,1,1)}).to(.4,{position:target,scale:new Vec3(.2,.2,1)},{easing:'quadIn'}).call(()=>{n.active=false;}).start();}}
    private undo(){
        if(!this.undoButton.getComponent(Button)!.interactable)return;
        let f:Fruit|undefined;while(this.history.length){const next=this.history.pop()!;if(next.state===1){f=next;break;}}
        if(!f)return;this.undoLeft--;this.countUndo.string=String(this.undoLeft);this.basket.splice(this.basket.indexOf(f),1);f.state=0;f.node.setParent(this.boards[this.current]);f.node.position=f.home;f.node.setScale(1,1,1);
        for(const x of this.all[this.current])if(x.state===0)x.node.setSiblingIndex(x.tile.layerIndex*10000+x.tile.indexInLayer);
        this.arrange();this.refresh();
    }
    private shuffle(){
        if(!this.shuffleButton.getComponent(Button)!.interactable)return;
        const fs=this.all[this.current].filter(f=>f.state===0),types=fs.map(f=>f.type);
        for(let i=types.length-1;i>0;i--){const j=Math.floor(Math.random()*(i+1)),t=types[i];types[i]=types[j];types[j]=t;}
        for(let i=0;i<fs.length;i++)this.setType(fs[i],types[i]);
        this.shuffleLeft--;this.countShuffle.string=String(this.shuffleLeft);this.refresh();
    }
    private setType(f:Fruit,type:number){f.type=type;f.w=this.cfg.sizes[type][0];f.h=this.cfg.sizes[type][1];f.sprite.spriteFrame=this.frames.get(type)!;f.node.getComponent(UITransform)!.setContentSize(f.w,f.h);}
}
