const {chromium}=require('C:/Users/pc/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const fs=require('fs'),path=require('path'),assert=require('assert'),{pathToFileURL}=require('url');
const out=path.resolve(__dirname,'../deliverables'),qa=path.join(__dirname,'qa');
async function idle(p){await p.waitForFunction(()=>!window.OrchardPlayable.busy);}
async function emitFruit(p,type){const id=await p.evaluate(type=>{const g=window.OrchardPlayable;return g.all[g.current].find(f=>f.type===type&&f.button.interactable)?.tile.tileID;},type);assert(id!==undefined);await p.evaluate(id=>{const g=window.OrchardPlayable;g.all[g.current].find(f=>f.tile.tileID===id).node.emit('click');},id);await idle(p);}
async function bounds(p){return p.evaluate(async()=>{const cc=await System.import('cc'),g=window.OrchardPlayable,cam=g.camera,cv=document.getElementById('GameCanvas'),r=cv.getBoundingClientRect();const nodes=g.all[g.current].filter(f=>f.state===0).map(f=>({node:f.node,w:f.w,h:f.h}));nodes.push({node:g.installButton,w:450,h:64});return {canvas:[cv.width,cv.height],css:[r.width,r.height],screen:[innerWidth,innerHeight],bounds:nodes.map(f=>{const p=cam.worldToScreen(f.node.worldPosition);return [p.x/cv.width*innerWidth,(cv.height-p.y)/cv.height*innerHeight];})};});}
(async()=>{
 const browser=await chromium.launch({executablePath:'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe',headless:true,args:['--enable-unsafe-swiftshader']});
 try{
  const errors=[],network=[];
  const context=await browser.newContext({viewport:{width:390,height:844},isMobile:true,hasTouch:true,userAgent:'Mozilla/5.0 (Linux; Android 12; Pixel 5) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Mobile Safari/537.36'});
  const unity=await context.newPage();unity.on('pageerror',e=>errors.push(e.message));unity.on('request',r=>{if(/^https?:/.test(r.url()))network.push(r.url());});
  await unity.addInitScript(()=>{window.hostCalls=[];window.hostListeners={};window.hostVisible=false;window.mraid={getState:()=> 'loading',isViewable:()=>window.hostVisible,addEventListener:(n,f)=>window.hostListeners[n]=f,open:u=>window.hostCalls.push(['open',u])};});
  await unity.goto(pathToFileURL(path.join(out,'index.html')).href);await unity.waitForFunction(()=>window.hostListeners.ready);
  assert.strictEqual(await unity.evaluate(()=>window.OrchardPlayable.ready),false);
  await unity.evaluate(()=>window.hostListeners.ready());assert.strictEqual(await unity.evaluate(()=>window.OrchardPlayable.ready),false);
  await unity.evaluate(()=>{window.hostVisible=true;window.hostListeners.viewableChange(true);});await unity.waitForFunction(()=>window.OrchardPlayable.ready);
  assert.deepStrictEqual(await unity.evaluate(()=>window.hostCalls),[]);
  await unity.waitForTimeout(150);
  // Real touch events verify that engine Buttons work in Android emulation.
  for(let k=0;k<3;k++){
    const point=await unity.evaluate(async()=>{const cc=await System.import('cc'),g=window.OrchardPlayable,f=g.all[0].find(f=>f.type===39&&f.button.interactable);const pt=g.camera.worldToScreen(f.node.worldPosition),cv=document.getElementById('GameCanvas'),r=cv.getBoundingClientRect();return{id:f.tile.tileID,x:r.x+pt.x/cv.width*r.width,y:r.y+(cv.height-pt.y)/cv.height*r.height};});
    await unity.touchscreen.tap(point.x,point.y);await unity.waitForFunction(id=>window.OrchardPlayable.all[0].find(f=>f.tile.tileID===id).state!==0,point.id,{timeout:2000});await idle(unity);
  }
  assert.strictEqual(await unity.evaluate(()=>window.OrchardPlayable.guideActive),false);
  await unity.evaluate(()=>{window.hostVisible=false;window.hostListeners.viewableChange(false);});
  assert.strictEqual(await unity.evaluate(()=>window.OrchardPlayable.ready),false);
  assert.strictEqual(await unity.evaluate(()=>window.OrchardPlayable.all[0].some(f=>f.button.interactable)),false);
  await unity.evaluate(()=>{window.hostVisible=true;window.hostListeners.viewableChange(true);});
  for(const size of [{width:844,height:390},{width:1200,height:628},{width:800,height:600},{width:360,height:640},{width:430,height:932}]){
    await unity.setViewportSize(size);await unity.waitForTimeout(300);
    const b=await bounds(unity);assert.deepStrictEqual(b.css,b.screen,'Canvas does not fill frame');
    for(const [x,y] of b.bounds)assert(x>=0&&x<=size.width&&y>=0&&y<=size.height,JSON.stringify({size,b}));
    await unity.screenshot({path:path.join(qa,'responsive_'+size.width+'_'+size.height+'.png')});
    console.log('RESPONSIVE',size,b.canvas);
  }
  console.log('UNITY MRAID hidden/start/pause/resume, local file and touch PASS');
  const mtg=await context.newPage();mtg.on('pageerror',e=>errors.push(e.message));mtg.on('request',r=>{if(/^https?:/.test(r.url()))network.push(r.url());});
  await mtg.addInitScript(()=>{window.hostCalls=[];window.gameReady=()=>window.hostCalls.push(['ready']);window.gameEnd=()=>window.hostCalls.push(['end']);window.gameRetry=()=>window.hostCalls.push(['retry']);window.install=()=>window.hostCalls.push(['install']);});
  await mtg.goto(pathToFileURL(path.join(out,'OrchardTrioMTG.html')).href);await mtg.waitForFunction(()=>window.hostCalls.some(x=>x[0]==='ready'));
  assert.strictEqual(await mtg.evaluate(()=>window.OrchardPlayable.ready),false);assert.strictEqual(await mtg.evaluate(()=>window.OrchardPlayable.all[0].some(f=>f.button.interactable)),false);
  await mtg.evaluate(()=>window.gameStart());await mtg.waitForFunction(()=>window.OrchardPlayable.ready);
  await mtg.waitForTimeout(150);await mtg.setViewportSize({width:1200,height:628});await mtg.waitForTimeout(300);
  await mtg.screenshot({path:path.join(out,'MTG_Main_1200x628.png')});
  await mtg.setViewportSize({width:390,height:844});await mtg.waitForTimeout(300);
  await mtg.screenshot({path:path.join(qa,'mtg_start.png')});
  await mtg.evaluate(()=>window.OrchardPlayable.node.getChildByName('Header').getChildByName('SettingsButton').emit('click'));
  assert.strictEqual(await mtg.evaluate(()=>window.OrchardPlayable.settingsOpen),true);
  assert.strictEqual(await mtg.evaluate(()=>window.OrchardPlayable.all[0].some(f=>f.button.interactable)),false);
  await mtg.evaluate(()=>window.OrchardPlayable.settingsPanel.getChildByName('Sound').emit('click'));
  assert.strictEqual(await mtg.evaluate(()=>window.OrchardPlayable.muted),true);
  await mtg.evaluate(()=>window.OrchardPlayable.settingsPanel.getChildByName('Resume').emit('click'));
  assert.strictEqual(await mtg.evaluate(()=>window.OrchardPlayable.settingsOpen),false);
  for(let i=0;i<3;i++)await emitFruit(mtg,39);
  await emitFruit(mtg,33);
  assert.strictEqual(await mtg.evaluate(()=>window.OrchardPlayable.basket.length),1);
  await mtg.evaluate(()=>window.OrchardPlayable.undoButton.emit('click'));
  assert.strictEqual(await mtg.evaluate(()=>window.OrchardPlayable.basket.length),0);assert.strictEqual(await mtg.evaluate(()=>window.OrchardPlayable.undoLeft),0);
  const before=await mtg.evaluate(()=>window.OrchardPlayable.all[0].filter(f=>f.state===0).map(f=>({type:f.type,x:f.home.x,y:f.home.y})));
  await mtg.evaluate(()=>window.OrchardPlayable.shuffleButton.emit('click'));
  const after=await mtg.evaluate(()=>window.OrchardPlayable.all[0].filter(f=>f.state===0).map(f=>({type:f.type,x:f.home.x,y:f.home.y})));
  assert.deepStrictEqual(before.map(f=>[f.x,f.y]),after.map(f=>[f.x,f.y]));assert.deepStrictEqual(before.map(f=>f.type).sort(),after.map(f=>f.type).sort());
  // The current first five boards have only three types; a full seven-slot loss is mathematically impossible.
  // Exercise the loss branch with a QA-only three-slot capacity, without changing the delivered configuration.
  await mtg.evaluate(()=>{window.OrchardPlayable.cfg.capacity=3;window.OrchardPlayable.load(1);});
  const types=await mtg.evaluate(()=>[...new Set(window.OrchardPlayable.all[1].map(f=>f.type))]);
  for(const type of types)await emitFruit(mtg,type);
  assert.strictEqual(await mtg.evaluate(()=>window.OrchardPlayable.ended),true);
  assert.strictEqual(await mtg.evaluate(()=>window.hostCalls.filter(x=>x[0]==='end').length),1);
  await mtg.evaluate(()=>window.OrchardPlayable.retryButton.emit('click'));
  assert.strictEqual(await mtg.evaluate(()=>window.hostCalls.filter(x=>x[0]==='retry').length),1);
  assert.strictEqual(await mtg.evaluate(()=>window.OrchardPlayable.guideActive),false);
  await mtg.evaluate(()=>{window.OrchardPlayable.cfg.capacity=7;window.OrchardPlayable.installButton.emit('click');});
  assert.strictEqual(await mtg.evaluate(()=>window.hostCalls.filter(x=>x[0]==='install').length),1);
  await mtg.evaluate(()=>window.gameClose());assert.strictEqual(await mtg.evaluate(()=>window.OrchardPlayable.ready),false);
  await mtg.evaluate(()=>window.OrchardPlayable.installButton.emit('click'));assert.strictEqual(await mtg.evaluate(()=>window.hostCalls.filter(x=>x[0]==='install').length),1);
  assert.deepStrictEqual(errors,[]);assert.deepStrictEqual(network,[]);
  console.log('MTG ready/start/end/retry/close/install, undo, shuffle PASS; NO NETWORK REQUESTS');
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
