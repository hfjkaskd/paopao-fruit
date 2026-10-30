const {chromium}=require('C:/Users/pc/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const path=require('path'),assert=require('assert'),{pathToFileURL}=require('url');
(async()=>{
 const browser=await chromium.launch({executablePath:'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe',headless:true,args:['--enable-unsafe-swiftshader']});
 try{
  for(const size of [{width:390,height:844},{width:1308,height:936}]){
   const p=await browser.newPage({viewport:size});
   await p.goto(pathToFileURL(path.resolve(__dirname,'../deliverables/index.html')).href);
   await p.waitForFunction(()=>window.OrchardPlayable?.ready);
   await p.evaluate(()=>{const g=window.OrchardPlayable;g.all[0].find(f=>f.type===39).node.emit('click');});
   await p.waitForFunction(()=>!window.OrchardPlayable.busy);
   const geometry=await p.evaluate(async()=>{const cc=await System.import('cc'),g=window.OrchardPlayable,f=g.basket[0],tray=g.node.getChildByName('Basket'),a=f.node.worldPosition,b=tray.worldPosition;return {fruitY:a.y,trayY:b.y,fruitX:a.x,trayX:b.x,scale:tray.scale.x,landscape:g.landscape};});
   assert(Math.abs(geometry.fruitY-geometry.trayY-36)<1,'Collected fruit must match authored FirstPos');
   assert.strictEqual(geometry.landscape,false);
   await p.screenshot({path:path.resolve(__dirname,`qa/geometry_${size.width}.png`)});
   console.log('Basket alignment and portrait composition PASS',size,geometry);
   await p.close();
  }
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
