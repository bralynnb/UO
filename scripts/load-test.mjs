import assert from 'node:assert/strict';
import { once } from 'node:events';
import { WebSocket } from 'ws';
import { walkable } from '../server/src/world.mjs';
import { createMarketServer } from '../server/src/index.mjs';
const app=createMarketServer({database:':memory:'});app.server.listen(0,'127.0.0.1');await once(app.server,'listening');
const clients=[];let snapshots=0;
try {
  for(let i=0;i<64;i++) {
    const ws=new WebSocket(`ws://127.0.0.1:${app.server.address().port}/ws`);await once(ws,'open');
    const welcomed=new Promise((resolve,reject)=>{const timeout=setTimeout(()=>reject(new Error('Join timeout')),4000);ws.on('message',buffer=>{const m=JSON.parse(String(buffer));if(m.type==='welcome'){clearTimeout(timeout);resolve();}if(m.type==='state')snapshots++;});});
    ws.send(JSON.stringify({type:'join',name:'Visitor '+i,color:i%6,token:''}));await welcomed;clients.push(ws);
  }
  assert.equal(app.market.clients.size,64);
  const start=app.market.tickNumber;let seq=0;
  const interval=setInterval(()=>{for(const [i,ws] of clients.entries())ws.send(JSON.stringify({type:'input',seq:++seq,dx:i%2?1:-1,dz:0}));},50);
  await new Promise(resolve=>setTimeout(resolve,3000));clearInterval(interval);
  const ticks=app.market.tickNumber-start;assert.ok(ticks>=45,`Expected >=45 ticks, saw ${ticks}`);
  for(const client of app.market.clients.values()){assert.ok(walkable(client.profile.x,client.profile.z));}
  console.log(JSON.stringify({simultaneousClients:64,durationSeconds:3,ticks,snapshots,result:'passed',scope:'local synthetic smoke test; not a production capacity guarantee'},null,2));
}finally{await app.close();}
