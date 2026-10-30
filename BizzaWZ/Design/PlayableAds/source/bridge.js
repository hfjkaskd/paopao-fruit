(function(){
    'use strict';
    var hooks=null,started=false,closed=false,ended=false,visible=true;
    var channel=window.ORCHARD_CHANNEL;
    function start(){if(closed)return;started=true;visible=true;if(hooks)hooks.start();}
    function pause(){visible=false;if(window.OrchardAudio)window.OrchardAudio.stop();if(hooks)hooks.pause();}
    function resume(){if(closed)return;visible=true;if(!started)start();else if(hooks)hooks.resume();}
    function close(){closed=true;visible=false;if(window.OrchardAudio)window.OrchardAudio.stop();if(hooks)hooks.close();}
    window.OrchardAd={
        attach:function(callbacks){
            hooks=callbacks;
            function initialize(){if(channel==='mtg'){
                if(typeof window.gameReady==='function'){window.gameReady();if(started)start();else pause();}
                else start();
            }else if(window.mraid){
                var ready=function(){
                    window.mraid.addEventListener('viewableChange',function(v){if(v)resume();else pause();});
                    if(window.mraid.isViewable())resume();else pause();
                };
                if(window.mraid.getState()==='loading')window.mraid.addEventListener('ready',ready);else ready();
            }else start();}
            if(window.OrchardAudio)window.OrchardAudio.ready.then(initialize);else initialize();
        },
        install:function(url){
            if(closed||!visible)return;
            if(channel==='mtg'){if(typeof window.install==='function')window.install();}
            else if(window.mraid)window.mraid.open(url);
        },
        end:function(){if(ended)return;ended=true;if(channel==='mtg'&&typeof window.gameEnd==='function')window.gameEnd();},
        retry:function(){ended=false;if(channel==='mtg'&&typeof window.gameRetry==='function')window.gameRetry();}
    };
    if(channel==='mtg'){window.gameStart=start;window.gameClose=close;}
    document.addEventListener('visibilitychange',function(){if(document.hidden)pause();else if(started&&(!window.mraid||window.mraid.isViewable()))resume();});
})();
