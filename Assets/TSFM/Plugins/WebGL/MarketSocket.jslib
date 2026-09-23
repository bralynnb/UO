mergeInto(LibraryManager.library, {
  TSFM_Connect: function(targetPtr) {
    var target=UTF8ToString(targetPtr);
    if(window.tsfmSocket){window.tsfmSocket.onclose=null;window.tsfmSocket.onmessage=null;window.tsfmSocket.onopen=null;window.tsfmSocket.close();}
    var ws=new WebSocket((location.protocol==='https:'?'wss://':'ws://')+location.host+'/ws');
    window.tsfmSocket=ws;
    ws.onopen=function(){SendMessage(target,'OnSocketOpen','');};
    ws.onmessage=function(event){if(typeof event.data==='string' && event.data.length<131072)SendMessage(target,'OnSocketMessage',event.data);};
    ws.onclose=function(){SendMessage(target,'OnSocketClosed','Connection closed');};
    ws.onerror=function(){};
  },
  TSFM_Send: function(jsonPtr) {
    var ws=window.tsfmSocket;
    if(ws&&ws.readyState===1&&ws.bufferedAmount<32768)ws.send(UTF8ToString(jsonPtr));
  },
  TSFM_Close: function() {
    if(window.tsfmSocket){window.tsfmSocket.onclose=null;window.tsfmSocket.close();window.tsfmSocket=null;}
  }
});
