(function(){
  if(window.__aiGenBound) return;
  window.__outbox = window.__outbox || [];
  function send(o){ try { window.__outbox.push(o); } catch(e){} }
  try { console.log('[ai-gen] script 注入'); } catch(_e){}
  send({type:'log',msg:'script 注入开始'});

  var SVG = {
    chat:'<svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"/></svg>',
    task:'<svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="4 17 10 11 4 5"/><line x1="12" y1="19" x2="20" y2="19"/></svg>',
    mini:'<svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect x="3" y="3" width="18" height="18" rx="2"/><rect x="12" y="12" width="7" height="7" rx="1"/></svg>',
    folder:'<svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z"/></svg>',
    bug:'<svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="9"/><line x1="12" y1="8" x2="12" y2="12"/><line x1="12" y1="16" x2="12.01" y2="16"/></svg>',
    set:'<svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="3"/><path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 1 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 1 1-4 0v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 1 1-2.83-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 1 1 0-4h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 1 1 2.83-2.83l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 1 1 4 0v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 1 1 2.83 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 1 1 0 4h-.09a1.65 1.65 0 0 0-1.51 1z"/></svg>',
    image:'<svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect x="3" y="3" width="18" height="18" rx="2"/><circle cx="8.5" cy="8.5" r="1.5"/><polyline points="21 15 16 10 5 21"/></svg>',
    video:'<svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polygon points="23 7 16 12 23 17 23 7"/><rect x="1" y="5" width="15" height="14" rx="2"/></svg>',
    music:'<svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M9 18V5l12-2v13"/><circle cx="6" cy="18" r="3"/><circle cx="18" cy="16" r="3"/></svg>',
    pdf:'<svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/></svg>',
    file:'<svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M13 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V9z"/><polyline points="13 2 13 9 20 9"/></svg>',
    proj:'<svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polygon points="12 2 2 7 12 12 22 7 12 2"/><polyline points="2 17 12 22 22 17"/><polyline points="2 12 12 17 22 12"/></svg>'
  };

  window.__aiGenFindInput = function(){
    var sels=['textarea#chat-input','textarea[placeholder]',
              'div[contenteditable="true"]','[role="textbox"]'];
    for(var i=0;i<sels.length;i++){
      var e=document.querySelector(sels[i]);
      if(e&&e.offsetParent!==null) return e;
    }
    return null;
  };

  window.__aiGenFindSend = function(){
    var inp=window.__aiGenFindInput();
    if(!inp) return null;
    var ir=inp.getBoundingClientRect();
    var scope=inp.parentElement;
    for(var i=0;i<5&&scope&&scope.parentElement;i++) scope=scope.parentElement;
    var best=null, bs=-1e9;
    if(scope){
      scope.querySelectorAll('[role="button"], button').forEach(function(el){
        if(el.offsetParent===null||el.disabled) return;
        if(!el.querySelector('svg')) return;
        var r=el.getBoundingClientRect();
        if(r.width<14||r.width>90||r.height<14||r.height>90) return;
        var dy=Math.abs((r.top+r.height/2)-(ir.top+ir.height/2));
        if(dy>Math.max(40,ir.height)) return;
        if(r.left<ir.left) return;
        var sc=r.left-dy;
        if(sc>bs){bs=sc;best=el;}
      });
    }
    if(best) return best;
    var sels=['button[type="submit"]',
              'button[aria-label*="发送"]','button[aria-label*="Send"]',
              '[data-testid*="send"]'];
    for(var i=0;i<sels.length;i++){
      var b=document.querySelector(sels[i]);
      if(b&&b.offsetParent!==null&&!b.disabled) return b;
    }
    return null;
  };

  window.__aiGenGetInputText = function(){
    var inp = window.__aiGenFindInput();
    if(!inp) return '';
    if(inp.tagName==='TEXTAREA'||inp.tagName==='INPUT')
      return (inp.value||'').trim();
    return (inp.innerText||'').trim();
  };

  window.__aiGenSend = async function(text){
    var input=window.__aiGenFindInput();
    if(!input) throw new Error('找不到输入框');
    input.focus(); input.click();
    if(input.tagName==='TEXTAREA'||input.tagName==='INPUT'){
      var proto=input.tagName==='TEXTAREA'?window.HTMLTextAreaElement.prototype:window.HTMLInputElement.prototype;
      var setter=Object.getOwnPropertyDescriptor(proto,'value').set;
      setter.call(input, text);
      input.dispatchEvent(new Event('input',{bubbles:true}));
      input.dispatchEvent(new Event('change',{bubbles:true}));
    }else{
      var sel=window.getSelection(); var rg=document.createRange();
      rg.selectNodeContents(input); sel.removeAllRanges(); sel.addRange(rg);
      document.execCommand('insertText',false,text);
      input.dispatchEvent(new InputEvent('input',{bubbles:true,data:text}));
    }
    await new Promise(function(r){setTimeout(r,400);});
    var b=window.__aiGenFindSend();
    if(b){
      try{
        b.click();
        send({type:'sent',how:'button'});
        return 'button';
      }catch(e){}
    }
    var mk=function(type){
      input.dispatchEvent(new KeyboardEvent(type,{key:'Enter',code:'Enter',
        keyCode:13,which:13,bubbles:true,cancelable:true}));
    };
    mk('keydown'); mk('keypress'); mk('keyup');
    send({type:'sent',how:'enter'});
    return 'enter';
  };

  window.__aiGenRenderSvg = async function(svgText,w,h){
    var blob=new Blob([svgText],{type:'image/svg+xml;charset=utf-8'});
    var url=URL.createObjectURL(blob);
    try{
      var img=new Image();
      await new Promise(function(res,rej){
        img.onload=res; img.onerror=function(){rej(new Error('SVG 加载失败'));};
        img.src=url;
      });
      var c=document.createElement('canvas');c.width=w;c.height=h;
      var ctx=c.getContext('2d');ctx.drawImage(img,0,0,w,h);
      return c.toDataURL('image/png');
    }finally{URL.revokeObjectURL(url);}
  };

  var lastContinue=0;
  setInterval(function(){
    if(window.__aiGenAutoContinue===false) return;
    var now=Date.now();
    if(now-lastContinue<3000) return;
    var kws=['继续生成','继续输出','继续回答','继续','Continue'];
    var all=document.querySelectorAll('button, [role="button"], div, span, a, p');
    var cands=[];
    for(var i=0;i<all.length;i++){
      var el=all[i];
      if(el.children.length>4) continue;
      var r=el.getBoundingClientRect();
      if(r.width<10||r.height<10||r.width>400||r.height>100) continue;
      var tx='';
      for(var j=0;j<el.childNodes.length;j++) if(el.childNodes[j].nodeType===3) tx+=el.childNodes[j].textContent;
      tx=tx.trim();
      if(!tx){tx=(el.textContent||'').trim();if(tx.length>12) continue;}
      if(!tx||tx.length>15) continue;
      var m=false;
      for(var k=0;k<kws.length;k++) if(tx===kws[k]||tx===kws[k]+'...'){m=true;break;}
      if(!m&&tx.indexOf('继续')>=0&&tx.length<=6) m=true;
      if(!m) continue;
      cands.push({el:el,text:tx,rect:r});
    }
    if(!cands.length) return;
    cands.sort(function(a,b){
      if(Math.abs(a.rect.bottom-b.rect.bottom)>20) return b.rect.bottom-a.rect.bottom;
      return b.rect.right-a.rect.right;
    });
    var tg=cands[0], cl=tg.el, cur=cl;
    for(var d=0;d<5;d++){
      if(!cur||cur===document.body) break;
      if(cur.tagName==='BUTTON'||cur.getAttribute('role')==='button'){cl=cur;break;}
      cur=cur.parentElement;
    }
    try{
      cl.click();
      lastContinue=now;
      send({type:'log',msg:'已自动点击继续生成：'+tg.text});
    }catch(e){}
  },500);

  var END_RE=/<!--\s*END\s*-->/i;
  var seen={};

  function isGen(){
    var sels=['button[aria-label*="停止"]','button[aria-label*="Stop"]'];
    for(var i=0;i<sels.length;i++){
      var e=document.querySelector(sels[i]);
      if(e&&e.offsetParent!==null) return true;
    }
    return false;
  }

  function findTh(){
    var th=false,dn=false;
    var all=document.querySelectorAll('div, span, p');
    for(var i=0;i<all.length;i++){
      var e=all[i];if(!e.offsetParent) continue;
      var t=(e.textContent||'').trim();
      if(!t||t.length>40) continue;
      if(t==='正在思考'||t==='正在深度思考'||t==='Thinking') th=true;
      if(t.indexOf('已思考')===0||t.indexOf('已深度思考')===0) dn=true;
    }
    return {thinking:th,done:dn};
  }

  function scan(){
    var out=[];
    document.querySelectorAll('pre').forEach(function(pre){
      var ce=pre.querySelector('code');
      var raw=(ce?ce.innerText:pre.innerText||'').replace(/\u200b/g,'').trim();
      if(!raw) return;
      var lang='';
      var m=raw.match(/^```([a-zA-Z0-9_]*)/);
      if(m) lang=m[1].toLowerCase();
      var ct=raw;
      if(raw.indexOf('```')===0)
        ct=raw.replace(/^```[a-zA-Z0-9_]*\s*/,'').replace(/```\s*$/,'').trim();
      var he=END_RE.test(ct);
      if(he) ct=ct.replace(END_RE,'').replace(/```\s*$/,'').trim();
      var lo=ct.toLowerCase();
      var svg=lo.indexOf('<svg')>=0;
      var fr=/^\s*---FRAME---\s*$/m.test(ct);
      var js=ct.trim().charAt(0)==='{'&&ct.trim().charAt(ct.trim().length-1)==='}';
      var fl='';
      if(lang==='photocode'||lang==='videocode'||lang==='musiccode'||
         lang==='pdfcode'||lang==='filecode'||lang==='projectcode') fl=lang;
      else if(svg&&fr) fl='videocode';
      else if(svg) fl='photocode';
      else if(js){
        var lo2=ct.toLowerCase();
        if(/"filename"\s*:/.test(lo2)&&/"content"\s*:/.test(lo2)) fl='filecode';
        else if(/"tracks"\s*:/.test(lo2)) fl='musiccode';
        else if(/"paragraphs"\s*:/.test(lo2)) fl='pdfcode';
        else if(/"files"\s*:/.test(lo2)&&/"name"\s*:/.test(lo2)) fl='projectcode';
        else fl='musiccode';
      }
      var comp=false;
      if(fl==='musiccode'||fl==='pdfcode'||fl==='filecode'||fl==='projectcode')
        comp=he||js;
      else comp=he||/<\/svg>\s*$/i.test(ct);
      if(fl&&ct&&comp) out.push({lang:fl,code:ct,has_end:he});
    });
    return out;
  }

  function sh(s){var h=0;for(var i=0;i<s.length;i++) h=((h<<5)-h+s.charCodeAt(i))|0;return h.toString(16);}
  var ph='',phh=0;
  window.__aiGenMarkRequestStart=function(){ph='';phh=0;};

  setInterval(function(){
    var t=findTh(), g=isGen(), b=scan();
    if(!b.length) return;
    var tg=null;
    for(var i=0;i<b.length;i++) if(b[i].has_end){tg=b[i];break;}
    if(!tg) tg=b[0];
    var h=sh(tg.lang+'|'+tg.code);
    if(seen[h]) return;
    if(t.thinking&&!t.done) return;
    if(tg.has_end){seen[h]=true;send({type:'scan',lang:tg.lang,code:tg.code});return;}
    if(g) return;
    if(h===ph){phh++;if(phh>=2){seen[h]=true;send({type:'scan',lang:tg.lang,code:tg.code});}}
    else{ph=h;phh=1;}
  },1500);

  // ============ 注入式工具栏 ============
  (function(){
    if(document.getElementById('ai-gen-bar')) return;

    if(!document.getElementById('ai-gen-style')){
      var st=document.createElement('style');
      st.id='ai-gen-style';
      st.textContent=
        '#ai-gen-bar,#ai-gen-midbar{position:fixed;z-index:2147483646;display:none;' +
        'gap:6px;align-items:center;padding:6px 10px;border-radius:12px;' +
        'background:rgba(30,30,30,0.85);border:1px solid rgba(255,255,255,0.14);' +
        'box-shadow:0 4px 16px rgba(0,0,0,.28);backdrop-filter:blur(8px);' +
        'font-family:-apple-system,"Microsoft YaHei",sans-serif;user-select:none}' +
        '#ai-gen-bar{top:12px;right:12px}' +
        '#ai-gen-midbar{left:-9999px;top:-9999px}' +
        '#ai-gen-bar.on,#ai-gen-midbar.on{display:flex}' +
        '#ai-gen-bar .gbtn,#ai-gen-midbar .gbtn{display:inline-flex;align-items:center;' +
        'gap:5px;padding:5px 10px;border-radius:14px;background:transparent;' +
        'color:#fff;border:1px solid rgba(255,255,255,0.28);font-size:12px;' +
        'font-weight:500;cursor:pointer;line-height:1;white-space:nowrap}' +
        '#ai-gen-bar .gbtn:hover,#ai-gen-midbar .gbtn:hover{' +
        'background:rgba(255,255,255,0.15);border-color:rgba(255,255,255,0.75)}' +
        '#ai-gen-bar .gbtn svg,#ai-gen-midbar .gbtn svg{flex-shrink:0;display:block}';
      document.head.appendChild(st);
    }

    function bind(bar){
      bar.addEventListener('click',function(e){
        var b=e.target.closest('.gbtn');if(!b)return;
        e.stopPropagation();e.preventDefault();
        var a=b.dataset.act;
        try { console.log('[ai-gen] click act=' + a); } catch(_e){}
        send({type:'log',msg:'点击按钮 act=' + a});
        if(a==='switch') send({type:'toolbar_switch',to:b.dataset.to});
        else if(a==='gen') {
          var txt = window.__aiGenGetInputText ? window.__aiGenGetInputText() : '';
          send({type:'toolbar_gen',kind:b.dataset.kind,text:txt});
        }
        else if(a==='files') send({type:'toolbar_files'});
        else if(a==='feedback') send({type:'toolbar_feedback'});
        else if(a==='settings') send({type:'toolbar_settings'});
        else if(a==='mini') send({type:'toolbar_mini'});
      });
    }

    var topBar=document.createElement('div');
    topBar.id='ai-gen-bar';
    topBar.innerHTML=
      '<div class="gbtn" data-act="switch" data-to="chat">' + SVG.chat + '<span>聊天</span></div>' +
      '<div class="gbtn" data-act="switch" data-to="agent">' + SVG.task + '<span>任务</span></div>' +
      '<div class="gbtn" data-act="mini">' + SVG.mini + '<span>小窗</span></div>' +
      '<div class="gbtn" data-act="files">' + SVG.folder + '<span>文件</span></div>' +
      '<div class="gbtn" data-act="feedback">' + SVG.bug + '<span>反馈</span></div>' +
      '<div class="gbtn" data-act="settings">' + SVG.set + '<span>设置</span></div>';

    var midBar=document.createElement('div');
    midBar.id='ai-gen-midbar';
    midBar.innerHTML=
      '<div class="gbtn" data-act="gen" data-kind="image">' + SVG.image + '<span>图片</span></div>' +
      '<div class="gbtn" data-act="gen" data-kind="video">' + SVG.video + '<span>视频</span></div>' +
      '<div class="gbtn" data-act="gen" data-kind="music">' + SVG.music + '<span>音乐</span></div>' +
      '<div class="gbtn" data-act="gen" data-kind="pdf">' + SVG.pdf + '<span>PDF</span></div>' +
      '<div class="gbtn" data-act="gen" data-kind="file">' + SVG.file + '<span>文件</span></div>' +
      '<div class="gbtn" data-act="gen" data-kind="project">' + SVG.proj + '<span>项目</span></div>';

    bind(topBar); bind(midBar);
    document.body.appendChild(topBar);
    document.body.appendChild(midBar);

    window.__aiGenSetToolbar=function(on){
      if(on){topBar.classList.add('on');midBar.classList.add('on');}
      else{topBar.classList.remove('on');midBar.classList.remove('on');}
      send({type:'log',msg:'工具栏显隐=' + on});
    };

    // ============ 中间按钮跟随"深度思考/智能搜索"锚点 ============
    if(!window.__aiGenMidPosLoopStarted){
      window.__aiGenMidPosLoopStarted = true;
      (function startMidPosLoop(){
        function findAnchor(){
          var tg = ['深度思考','智能搜索','DeepThink','联网搜索','深度搜索'];
          var all = document.querySelectorAll('div,button,span,a');
          var best = null, br = -1;
          for(var i=0;i<all.length;i++){
            var e = all[i]; if(!e.offsetParent) continue;
            var t = (e.textContent||'').trim();
            if(t.length===0||t.length>12) continue;
            var h = false;
            for(var k=0;k<tg.length;k++){
              if(t===tg[k]||t.replace(/\s+/g,'')===tg[k]){h=true;break;}
            }
            if(!h) continue;
            var r = e.getBoundingClientRect();
            if(r.width<2||r.width>220||r.height>60) continue;
            if(r.right>br){br=r.right;best=e;}
          }
          return best;
        }
        var state = '';
        function posMid(){
          var m = document.getElementById('ai-gen-midbar');
          if(!m || !m.classList.contains('on')) return;
          var a = findAnchor();
          if(!a){
            if(state !== 'fallback'){
              m.style.left = 'auto';
              m.style.top = '56px';
              m.style.right = '12px';
              state = 'fallback';
            }
            return;
          }
          var r = a.getBoundingClientRect();
          if(r.width<2||r.bottom<0||r.top>window.innerHeight){
            if(state !== 'fallback'){
              m.style.left = 'auto';
              m.style.top = '56px';
              m.style.right = '12px';
              state = 'fallback';
            }
            return;
          }
          var cw = m.offsetWidth||460, ch = m.offsetHeight||32;
          var left = r.right+8, top = r.top+(r.height-ch)/2;
          if(left+cw > window.innerWidth-10){
            left = Math.max(10, Math.min(r.left, window.innerWidth-cw-10));
            top = r.bottom+6;
          }
          if(top<10) top=10;
          if(top+ch>window.innerHeight-10) top=window.innerHeight-ch-10;
          m.style.left = left+'px';
          m.style.top = top+'px';
          m.style.right = 'auto';
          state = 'anchored';
        }
        function loop(){ try{posMid();}catch(e){} requestAnimationFrame(loop); }
        loop();
      })();
    }

    send({type:'log',msg:'工具栏已创建'});
  })();

  window.__aiGenInstalled=true;
  send({type:'log',msg:'页面交互脚本已就绪 v0.7.32'});
})();