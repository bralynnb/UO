import { createServer } from 'node:http';
import { readFile, stat } from 'node:fs/promises';
import { existsSync } from 'node:fs';
import { resolve, extname, sep } from 'node:path';
import { fileURLToPath } from 'node:url';
import { randomUUID } from 'node:crypto';
import { WebSocketServer, WebSocket } from 'ws';
import { PlayerStore } from './store.mjs';
import { Market } from './game.mjs';
const root = fileURLToPath(new URL('../public',import.meta.url));
const types={'.html':'text/html; charset=utf-8','.js':'application/javascript','.json':'application/json','.wasm':'application/wasm','.data':'application/octet-stream','.css':'text/css','.png':'image/png','.svg':'image/svg+xml'};
export function createMarketServer({database=process.env.DATABASE_PATH||'data/market.sqlite',capacity=Number(process.env.MAX_PLAYERS)||64,publicOrigin=process.env.PUBLIC_ORIGIN,publicDir=root}={}) {
  const store=new PlayerStore(database), market=new Market(store,{capacity});
  const server=createServer(async(req,res)=>{
    res.setHeader('X-Content-Type-Options','nosniff');
    res.setHeader('Referrer-Policy','same-origin');
    res.setHeader('X-Frame-Options','SAMEORIGIN');
    if (!['GET','HEAD'].includes(req.method)) {res.writeHead(405);res.end();return;}
    let path;
    try {path=decodeURIComponent(new URL(req.url,'http://local').pathname);} catch {res.writeHead(400);res.end();return;}
    const built=existsSync(resolve(publicDir,'game/index.html'));
    if(path==='/healthz') {res.setHeader('Cache-Control','no-store');res.setHeader('Content-Type','application/json');res.end(JSON.stringify({ok:true,clientReady:built,players:market.clients.size,capacity,version:'0.1.0'}));return;}
    if(path==='/readyz') {res.writeHead(built?200:503,{'Content-Type':'application/json','Cache-Control':'no-store'});res.end(JSON.stringify({ready:built}));return;}
    if(path==='/') {
      if(built) {res.writeHead(302,{Location:'/game/'});res.end();return;}
      path='/index.html'; res.statusCode=503;
    }
    if(path.endsWith('/')) path+='index.html';
    const target=resolve(publicDir,'.'+path);
    if (!target.startsWith(resolve(publicDir)+sep) || path.split('/').some(p=>p.startsWith('.')) || path.includes('\0')) {res.writeHead(403);res.end();return;}
    try {
      const info=await stat(target);
      if(!info.isFile()) throw new Error();
      res.setHeader('Content-Type',types[extname(target)]||'application/octet-stream');
      res.setHeader('Cache-Control','no-cache');
      res.setHeader('Content-Length',info.size);
      if(req.method==='HEAD') res.end(); else res.end(await readFile(target));
    }catch {res.writeHead(404,{'Content-Type':'text/plain'});res.end('Not found');}
  });
  const sockets=new WebSocketServer({noServer:true,maxPayload:2048,perMessageDeflate:false});
  server.on('upgrade',(req,socket,head)=>{
    let path;try{path=new URL(req.url,'http://local').pathname;}catch{socket.destroy();return;}
    let sameOrigin=true;
    if(req.headers.origin) {
      try {sameOrigin=publicOrigin ? new URL(req.headers.origin).origin===new URL(publicOrigin).origin : new URL(req.headers.origin).host===req.headers.host;}catch{sameOrigin=false;}
    }
    if(path!=='/ws'||!sameOrigin||sockets.clients.size>=capacity+32) {socket.end('HTTP/1.1 403 Forbidden\r\nConnection: close\r\n\r\n');return;}
    sockets.handleUpgrade(req,socket,head,ws=>sockets.emit('connection',ws));
  });
  sockets.on('connection',ws=>{
    const client={id:randomUUID(),send:message=>{
      if(ws.readyState!==WebSocket.OPEN)return;
      if(ws.bufferedAmount>256*1024){ws.close(1013,'Connection too slow');return;}
      ws.send(JSON.stringify(message));
    }};
    let windowStart=Date.now(),messages=0,alive=true,joinAttempts=0;
    const deadline=setTimeout(()=>{if(!client.profile) ws.close(1008,'Join timeout');},10000);
    ws.on('pong',()=>{alive=true;});
    ws.checkHeartbeat=()=>{if(!alive){ws.terminate();return;}alive=false;ws.ping();};
    ws.on('message',(buffer,isBinary)=>{
      if(Date.now()-windowStart>=1000){windowStart=Date.now();messages=0;}
      if(++messages>70||isBinary){ws.close(1008,'Message limit');return;}
      try {
        const message=JSON.parse(buffer.toString());
        if(message?.type==='join' && ++joinAttempts>3){ws.close(1008,'Join limit');return;}
        market.handle(client,message);
        if(client.profile)clearTimeout(deadline);
      }catch(error){client.send({type:'error',text:error instanceof SyntaxError?'Invalid message.':error.message});}
    });
    ws.on('close',()=>{clearTimeout(deadline);market.leave(client);});
    ws.on('error',()=>{});
  });
  const tick=setInterval(()=>market.tick(.05),50);
  const flush=setInterval(()=>market.saveAll(),5000);
  const heartbeat=setInterval(()=>{for(const ws of sockets.clients)ws.checkHeartbeat();},20000);
  return {server,market,store,async close(){
    clearInterval(tick);clearInterval(flush);clearInterval(heartbeat);market.saveAll();
    for(const ws of sockets.clients)ws.terminate();
    await new Promise(resolveClose=>sockets.close(resolveClose));
    await new Promise(resolveClose=>server.close(resolveClose));store.close();
  }};
}
if(process.argv[1] && resolve(process.argv[1])===fileURLToPath(import.meta.url)) {
  const app=createMarketServer();
  const port=Number(process.env.PORT)||8080;
  app.server.listen(port,'0.0.0.0',()=>console.log(`TSFM server listening on ${port}; capacity ${app.market.capacity}.`));
  let closing=false;
  for(const signal of ['SIGINT','SIGTERM'])process.on(signal,async()=>{if(closing)return;closing=true;await app.close();process.exit(0);});
}
