const fs=require('fs'),path=require('path');
const {minify}=require('F:/Cocos/Creator/3.8.3/resources/resources/3d/engine/node_modules/terser');
const parser=require('F:/Cocos/Creator/3.8.3/resources/resources/3d/engine/node_modules/@babel/parser');
const work=__dirname,build=path.join(work,'build/web'),out=path.join(work,'../deliverables');
fs.mkdirSync(out,{recursive:true});
const locales=JSON.parse(fs.readFileSync(path.join(work,'locales.json'),'utf8')); 
function walk(d){return fs.readdirSync(d,{withFileTypes:true}).flatMap(x=>x.isDirectory()?walk(path.join(d,x.name)):[path.join(d,x.name)]);}
const local={};
for(const file of walk(path.join(build,'assets'))){
    const key=path.relative(build,file).replace(/\\/g,'/');
    if(file.endsWith('.json'))local[key]=JSON.parse(fs.readFileSync(file,'utf8'));
    else if(!file.endsWith('.js')){
        const ext=path.extname(file),mime={'.png':'image/png','.jpg':'image/jpeg','.ttf':'font/ttf','.bin':'application/octet-stream'}[ext]||'application/octet-stream';
        local[key]='data:'+mime+';base64,'+fs.readFileSync(file).toString('base64');
    }
}
const settings=JSON.parse(fs.readFileSync(path.join(build,'src/settings.json'),'utf8'));
settings.splashScreen={totalTime:0,logo:{type:'none'},background:{type:'none'},watermarkLocation:'none',autoFit:true};
settings.screen.exactFitScreen=true;
settings.engine.debug=false;
const bridge=fs.readFileSync(path.join(work,'bridge.js'),'utf8');
const sounds={};for(const name of ['tap','clear','win'])sounds[name]='data:audio/mpeg;base64,'+fs.readFileSync(path.join(work,'sounds',name+'.mp3')).toString('base64');
const soundCode=`(function(){var sources=${JSON.stringify(sounds)},voices={},tasks=[];Object.keys(sources).forEach(function(name){var a=new Audio();a.preload='auto';voices[name]=a;tasks.push(new Promise(function(resolve){a.addEventListener('canplaythrough',resolve,{once:true});a.addEventListener('error',resolve,{once:true});a.src=sources[name];a.load();}));});window.OrchardAudio={ready:Promise.all(tasks),play:function(name){var a=voices[name];if(a){a.currentTime=0;var p=a.play();if(p)p.catch(function(){});}},mute:function(value){Object.keys(voices).forEach(function(k){voices[k].muted=value;});},stop:function(){Object.keys(voices).forEach(function(k){voices[k].pause();});}};})();`;
const assetLoader=`
window.OrchardInline={install:function(cc){
    function key(url){var i=url.indexOf('assets/');return (i>=0?url.slice(i):url).split('?')[0];}
    function json(url,options,done){var k=key(url);if(!Object.prototype.hasOwnProperty.call(INLINE_ASSETS,k))return done(new Error('Missing inline resource '+k));done(null,INLINE_ASSETS[k]);}
    function image(url,options,done){var img=new Image(),k=key(url);img.onload=function(){done(null,img)};img.onerror=function(){done(new Error('Invalid inline image '+k))};img.src=INLINE_ASSETS[k];}
    function script(url,options,done){done(null);}
    function bundle(name,options,done){var n=name.split('/').pop(),base='assets/'+n+'/',cfg=Object.assign({},INLINE_ASSETS[base+'config.json']);cfg.base=base;done(null,cfg);}
    function font(url,options,done){var source=INLINE_ASSETS[key(url)],name='OrchardPlayableFont';if(typeof FontFace==='function'){var ff=new FontFace(name,'url('+source+')');ff.load().then(function(f){document.fonts.add(f);done(null,name);},function(e){done(e);});}else{var style=document.createElement('style');style.textContent='@font-face{font-family:'+name+';src:url('+source+')}';document.head.appendChild(style);var c=document.createElement('canvas').getContext('2d'),text='Orchard Trio 0123456789';c.font='40px monospace';var original=c.measureText(text).width,count=0;function check(){c.font='40px '+name+',monospace';if(c.measureText(text).width!==original||++count>125)done(null,name);else setTimeout(check,16);}check();}}
    cc.assetManager.downloader.register({'.json':json,'.png':image,'.jpg':image,'.jpeg':image,'.ttf':font,'.js':script,bundle:bundle});
    cc.assetManager.downloader.downloadScript=script;
    cc.assetManager.downloader._downloadJson=json;
}};`;
function escapeScript(s){return s.replace(/<\/script/gi,'<\\/script');}
(async()=>{
    let app=fs.readFileSync(path.join(build,'application.js'),'utf8');
    app=app.replace("this.settingsPath = 'src/settings.json';","this.settingsPath = '';");
    app=app.replace('cc = engine;','cc = engine; window.OrchardInline.install(cc);');
    // Settings are provided directly: Cocos has no settings-file request.
    const a=app.indexOf('overrideSettings: {'),b=app.indexOf('\n            }).then',a);
    app=app.slice(0,a)+'overrideSettings: INLINE_SETTINGS'+app.slice(b);
    let scripts=fs.readFileSync(path.join(build,'src/polyfills.bundle.js'),'utf8')+'\n'+fs.readFileSync(path.join(build,'src/system.bundle.js'),'utf8');
    scripts+=`\nvar INLINE_LOCATION=location.href.split(/[?#]/)[0];var INLINE_BASE=INLINE_LOCATION.slice(0,INLINE_LOCATION.lastIndexOf('/')+1);var INLINE_REGISTRY={};var INLINE_REGISTER=System.register.bind(System);System.register=function(name,deps,declare){if(typeof name==='string'){INLINE_REGISTRY[name]=[deps,declare];INLINE_REGISTER(name,deps,declare);}else INLINE_REGISTER(name,deps);};System.constructor.prototype.instantiate=function(url){if(INLINE_REGISTRY[url])return INLINE_REGISTRY[url];throw Error('Missing inline module '+url);};\n`;
    const jsfiles=['cocos-js/cc.js','application.js','src/chunks/bundle.js','assets/internal/index.js','assets/main/index.js','index.js'];
    for(const rel of jsfiles){
        let code=rel==='application.js'?app:fs.readFileSync(path.join(build,rel),'utf8');
        if(rel==='assets/main/index.js'){
            const ast=parser.parse(code);for(const statement of ast.program.body){const call=statement.expression;if(call&&call.type==='CallExpression'&&call.arguments[0]&&call.arguments[0].value==='chunks:///_virtual/debug-view-runtime-control.ts')code=code.slice(0,statement.start)+'System.register("chunks:///_virtual/debug-view-runtime-control.ts",[],function(){return{execute:function(){}}});'+code.slice(statement.end);}
        }
        code=code.replace(/System\.register\(\s*\[/g,'System.register(INLINE_BASE+'+JSON.stringify(rel)+',[');
        scripts+='\n'+code;
    }
    scripts+='\nvar INLINE_RESOLVE=System.resolve.bind(System);System.constructor.prototype.resolve=function(id,parent){if(id==="cc")return INLINE_BASE+"cocos-js/cc.js";if(INLINE_REGISTRY[id])return id;return INLINE_RESOLVE(id,parent);};System.import(INLINE_BASE+"index.js").catch(function(e){console.error(e);});';
    const compressed=(await minify(scripts,{compress:{passes:2},mangle:true,format:{comments:/copyright|permission|license/i}})).code;
    const boot=(await minify('var INLINE_ASSETS='+JSON.stringify(local)+';var INLINE_SETTINGS='+JSON.stringify(settings)+';'+assetLoader,{compress:false,mangle:false})).code;
    const css='html,body{margin:0;padding:0;width:100%;height:100%;overflow:hidden;background:#0f2c16;touch-action:none}body{position:absolute;inset:0}body,canvas,div{outline:none;-webkit-tap-highlight-color:transparent;user-select:none;-webkit-user-select:none}#GameDiv,#Cocos3dGameContainer,#GameCanvas{width:100%;height:100%;display:block}#GameDiv{position:fixed;inset:0;width:100vw!important;height:100vh!important;overflow:hidden}';
    for(const [country,locale] of Object.entries(locales))for(const channel of ['mtg','unity']){
        const html='<!DOCTYPE html><html lang="'+locale.lang+'"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,user-scalable=no,initial-scale=1,minimum-scale=1,maximum-scale=1"><link rel="icon" href="data:,"><title>Orchard Trio</title><style>'+css+'</style></head><body><div id="GameDiv" cc_exact_fit_screen="true"><div id="Cocos3dGameContainer"><canvas id="GameCanvas" tabindex="0"></canvas></div></div><script>window.ORCHARD_CHANNEL='+JSON.stringify(channel)+';'+'window.ORCHARD_LOCALIZATION='+JSON.stringify(locale)+';'+escapeScript(soundCode)+escapeScript(bridge)+'</script><script>'+escapeScript(boot)+'</script><script>'+escapeScript(compressed)+'</script></body></html>';
        const name=channel==='mtg'?'OrchardTrioMTG.html':'index.html';const dest=path.join(out,country);fs.mkdirSync(dest,{recursive:true});fs.writeFileSync(path.join(dest,name),html);
        console.log(channel,Buffer.byteLength(html),'bytes');
        if(Buffer.byteLength(html)>=5000000)throw Error('Playable exceeds conservative 5 MB limit');
    }
})();
