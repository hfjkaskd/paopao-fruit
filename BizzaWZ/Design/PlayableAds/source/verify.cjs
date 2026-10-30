const {chromium}=require('C:/Users/pc/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const http=require('http'),fs=require('fs'),path=require('path'),assert=require('assert');
const out=path.resolve(__dirname,'../deliverables'),qa=path.join(__dirname,'qa');fs.mkdirSync(qa,{recursive:true});
const server=http.createServer((req,res)=>{const f=path.join(out,req.url.split('?')[0].slice(1)||'index.html');if(fs.existsSync(f)){res.setHeader('Content-Type','text/html');res.end(fs.readFileSync(f));}else {res.statusCode=404;res.end();}});
async function snapshot(page){return page.evaluate(()=>{let p=window.OrchardPlayable;return {ready:p.ready,level:p.current+1,matched:p.matched,basket:p.basket.length,guide:p.guideActive,ended:p.ended,fruits:p.all[p.current].filter(f=>f.state===0).length};});}
async function settle(page){await page.waitForFunction(()=>!window.OrchardPlayable.busy);}
async function clickFruit(page,id){
    const point=await page.evaluate(async(id)=>{const p=window.OrchardPlayable,f=p.all[p.current].find(f=>f.tile.tileID===id),cc=await System.import('cc');const camera=p.node.parent.getChildByName('Camera').getComponent(cc.Camera);const pt=camera.worldToScreen(f.node.worldPosition);const cv=document.getElementById('GameCanvas'),r=cv.getBoundingClientRect();return {x:r.x+pt.x/cv.width*r.width,y:r.y+(cv.height-pt.y)/cv.height*r.height};},id);
    await page.mouse.click(point.x,point.y);await settle(page);
}
(async()=>{
 await new Promise(r=>server.listen(18742,'127.0.0.1',r));
 const browser=await chromium.launch({executablePath:'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe',headless:true,args:['--disable-gpu-sandbox','--enable-unsafe-swiftshader']});
 try{
  const page=await browser.newPage({viewport:{width:390,height:844}}),errors=[],requests=[];
  page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(m.type()==='error')errors.push(m.text());});page.on('request',r=>{if(!r.isNavigationRequest()&&/^https?:/.test(r.url()))requests.push(r.url());});
  await page.addInitScript(()=>{window.hostCalls=[];window.hostListeners={};window.mraid={getState:()=> 'default',isViewable:()=>true,addEventListener:(n,f)=>window.hostListeners[n]=f,open:u=>window.hostCalls.push(['open',u])};});
  await page.goto('http://127.0.0.1:18742/index.html');
  try{await page.waitForFunction(()=>window.OrchardPlayable&&window.OrchardPlayable.ready,{timeout:30000});}catch(e){console.log('BOOT FAILED',errors,requests);await page.screenshot({path:path.join(qa,'failed.png')});throw e;}
  console.log('BOOT',await snapshot(page));console.log('VIEW',await page.evaluate(async()=>{const cc=await System.import('cc'),p=window.OrchardPlayable;return {scaleX:cc.view.getScaleX(),scaleY:cc.view.getScaleY(),design:cc.view.getDesignResolutionSize(),camera:p.node.parent.getChildByName('Camera').getComponent(cc.Camera).orthoHeight,canvas:[document.getElementById('GameCanvas').width,document.getElementById('GameCanvas').height]};}));await page.screenshot({path:path.join(qa,'portrait.png')});
  console.log('ERRORS',errors,'NETWORK',requests);
  if(process.argv.includes('--smoke'))return;
  let state=await snapshot(page);assert(state.guide&&state.fruits===9);
  const guide=await page.evaluate(()=>window.OrchardPlayable.all[0].filter(f=>f.type===39).map(f=>f.tile.tileID));
  for(const id of guide)await clickFruit(page,id);
  assert(!(await snapshot(page)).guide);await page.screenshot({path:path.join(qa,'after_guide.png')});
  for(let lv=1;lv<=5;lv++){
   await page.waitForTimeout(100);
   console.log('LAYOUT',await page.evaluate(()=>{const p=window.OrchardPlayable;return {level:p.current+1,id:p.cfg.levels[p.current].levelID,map:p.cfg.levels[p.current].mapID,total:p.all[p.current].length,types:[...new Set(p.all[p.current].map(f=>f.type))]};}));
   await page.screenshot({path:path.join(qa,'level_'+lv+'_start.png')});
   for(let count=0;count<80;count++){
    state=await snapshot(page);if(state.ended)break;
    const id=await page.evaluate(()=>{const p=window.OrchardPlayable,fs=p.all[p.current].filter(f=>f.button.interactable);let preferred=p.basket.length?p.basket[p.basket.length-1].type:null;return (fs.find(f=>f.type===preferred)||fs[fs.length-1])?.tile.tileID;});
    assert(id!==undefined,'No interactive fruit at level '+lv);await clickFruit(page,id);
   }
   state=await snapshot(page);console.log('LEVEL',state);assert(state.ended&&state.fruits===0&&state.basket===0);
   if(lv<5){await page.evaluate(()=>window.OrchardPlayable.nextButton.emit('click'));assert((await snapshot(page)).level===lv+1);assert(!(await snapshot(page)).guide);}
  }
  await page.screenshot({path:path.join(qa,'complete.png')});
  await page.evaluate(()=>window.OrchardPlayable.nextButton.emit('click'));
  assert.deepStrictEqual(await page.evaluate(()=>window.hostCalls),[['open','https://play.google.com/store/apps/details?id=com.wiwitsugeh.orchardtrio']]);
  await page.setViewportSize({width:844,height:390});await page.waitForTimeout(300);await page.screenshot({path:path.join(qa,'landscape_complete.png')});
  assert.deepStrictEqual(errors,[]);assert.deepStrictEqual(requests,[]);
  assert.deepStrictEqual(await page.evaluate(()=>window.OrchardPlayable.cfg.levels.map(l=>l.levelID)),['10001','10002','10003','10004','10005']);
  console.log('UNITY five levels and CTA PASS');
 }finally{await browser.close();server.close();}
})().catch(e=>{console.error(e);process.exitCode=1;server.close();});
