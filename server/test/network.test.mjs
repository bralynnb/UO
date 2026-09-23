import test from 'node:test';
import assert from 'node:assert/strict';
import { once } from 'node:events';
import { WebSocket } from 'ws';
import { createMarketServer } from '../src/index.mjs';
async function fixture(t) {
  const app=createMarketServer({database:':memory:'});app.server.listen(0,'127.0.0.1');await once(app.server,'listening');
  t.after(()=>app.close());const port=app.server.address().port;
  return {...app,http:`http://127.0.0.1:${port}`,ws:`ws://127.0.0.1:${port}/ws`};
}
async function connect(url,name='Test') {
  const ws=new WebSocket(url);await once(ws,'open');
  const history=[];const listeners=new Set();ws.on('message',data=>{const m=JSON.parse(data.toString());history.push(m);for(const resolve of listeners)resolve(m);});
  const wait=(type,predicate=()=>true)=>new Promise((resolve,reject)=>{
    const prior=history.find(m=>m.type===type&&predicate(m));if(prior){resolve(prior);return;}
    const timer=setTimeout(()=>{listeners.delete(listener);reject(new Error('Timed out waiting for '+type));},2500);
    function listener(m){if(m.type===type&&predicate(m)){clearTimeout(timer);listeners.delete(listener);resolve(m);}}
    listeners.add(listener);
  });
  ws.send(JSON.stringify({type:'join',name,color:2,token:''}));const welcome=await wait('welcome');
  return {ws,wait,history,welcome};
}
test('two real WebSocket clients see each other, movement and chat',async t=>{
  const app=await fixture(t),a=await connect(app.ws,'Alice'),b=await connect(app.ws,'Bob');
  const state=await a.wait('state',m=>m.players.length===2);assert.equal(state.players[1].name,'Bob');
  a.ws.send(JSON.stringify({type:'input',dx:1,dz:0,seq:1}));
  const moved=await b.wait('state',m=>m.players.some(p=>p.id===a.welcome.id&&p.x>a.welcome.profile.x));
  assert.equal(moved.players.length,2);
  a.ws.send(JSON.stringify({type:'chat',text:'Hello market'}));assert.equal((await b.wait('chat')).text,'Hello market');
  a.ws.close();await b.wait('state',m=>m.players.length===1&&m.players[0].id===b.welcome.id);
});
test('health reports missing Unity build honestly and root is not marked playable',async t=>{
  const app=await fixture(t);const health=await (await fetch(app.http+'/healthz')).json();assert.equal(health.ok,true);assert.equal(health.clientReady,false);
  assert.equal((await fetch(app.http+'/readyz')).status,503);
  const page=await fetch(app.http);assert.equal(page.status,503);assert.match(await page.text(),/not a playable game/);
});
test('foreign browser origin is rejected at WebSocket upgrade',async t=>{
  const app=await fixture(t);const ws=new WebSocket(app.ws,{origin:'https://evil.example'});
  const [error]=await once(ws,'error');assert.match(error.message,/403/);
});
test('invalid JSON yields a controlled error, and oversized frames close',async t=>{
  const app=await fixture(t);const a=await connect(app.ws);a.ws.send('{bad');
  assert.equal((await a.wait('error')).text,'Invalid message.');a.ws.send('x'.repeat(4096));
  const [code]=await once(a.ws,'close');assert.equal(code,1009);
});
test('guests cannot act before joining and HTTP traversal is refused',async t=>{
  const app=await fixture(t);const ws=new WebSocket(app.ws);await once(ws,'open');
  const response=once(ws,'message');ws.send(JSON.stringify({type:'action',vendor:'exchange',action:'exchange'}));
  assert.match(JSON.parse(String((await response)[0])).text,/Join/);ws.close();
  const traversal=await fetch(app.http+'/%2e%2e%2fpackage.json');assert.equal(traversal.status,403);
});
