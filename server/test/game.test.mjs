import test from 'node:test';
import assert from 'node:assert/strict';
import { mkdtempSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { PlayerStore } from '../src/store.mjs';
import { Market } from '../src/game.mjs';
import { world,walkable,move,vendors } from '../src/world.mjs';
function setup(t,options={}) {
  let time=100000;const store=new PlayerStore(':memory:');t.after(()=>store.close());
  const market=new Market(store,{now:()=>time,...options});let id=0;
  const client=(name='Visitor',token='')=>{const c={id:String(++id),out:[],send(m){this.out.push(structuredClone(m));}};market.handle(c,{type:'join',name,token,color:1});return c;};
  const action=(c,vendor,action,item)=>{time+=400;market.handle(c,{type:'action',vendor,action,item});};
  const at=(c,id)=>{Object.assign(c.profile,{x:vendors.get(id).x,z:vendors.get(id).z});};
  return {market,store,client,action,at,advance(n){time+=n;}};
}
test('join assigns distinct guests and snapshots never reveal resume tokens',t=>{
  const {client}=setup(t);const a=client('Alice'),b=client('Bob');
  assert.notEqual(a.profile.id,b.profile.id);
  assert.equal(a.out[0].token.length,64);
  const states=a.out.filter(m=>m.type==='state');
  assert.equal(states.at(-1).players.length,2);
  assert.equal(JSON.stringify(states).includes(a.out[0].token),false);
  assert.deepEqual(Object.keys(states.at(-1).players[0]).sort(),['bag','color','id','name','rotation','x','z']);
});
test('all vendor interactions and spawn are walkable',()=>{
  assert.ok(walkable(world.spawnX,world.spawnZ));
  for(const vendor of vendors.values())assert.ok(walkable(vendor.x,vendor.z),vendor.id);
});
test('movement is capped, diagonal normalized, and client position ignored',t=>{
  const {market,client}=setup(t);const a=client();a.profile.x=0;a.profile.z=-30;
  market.handle(a,{type:'input',dx:999,dz:999,seq:1,x:10000,z:10000});market.tick(.05);
  assert.ok(Math.abs(Math.hypot(a.profile.x,a.profile.z+30)-world.speed*.05)<1e-8);
  market.handle(a,{type:'input',dx:-1,dz:0,seq:0});assert.equal(a.dx,1);
});
test('stale movement stops after loss of input',t=>{
  const {market,client,advance}=setup(t);const a=client();const x=a.profile.x;
  market.handle(a,{type:'input',dx:1,dz:0,seq:1});advance(251);market.tick();assert.equal(a.profile.x,x);
});
test('buildings, cars, stalls and construction fences stop movement',()=>{
  for(const o of world.obstacles) {
    assert.equal(walkable(o.x,o.z),false,o.id+' center is solid');
    const p={x:o.x-o.w/2-world.radius-.05,z:o.z};
    if(walkable(p.x,p.z)) {
      move(p,1,0,1,true);
      assert.ok(p.x<=o.x-o.w/2-world.radius,o.id+' blocks approach');
    }
  }
  const p={x:world.maxX-1,z:0};move(p,1,0,10,true);assert.ok(p.x<world.maxX-.3);
  assert.equal(walkable(NaN,0),false);assert.equal(walkable(Infinity,0),false);
});
test('actions require proximity and exchange cannot be claimed twice',t=>{
  const {client,action,at}=setup(t);const a=client();
  assert.throws(()=>action(a,'exchange','exchange'),/closer/);at(a,'exchange');action(a,'exchange','exchange');
  assert.equal(a.profile.tokens,8);assert.throws(()=>action(a,'exchange','exchange'),/already/);assert.equal(a.profile.tokens,8);
});
test('full delivery pays once and cannot skip steps',t=>{
  const {client,action,at}=setup(t);const a=client();
  at(a,'bodega');assert.throws(()=>action(a,'bodega','deliver'),/not carrying/);
  at(a,'mushroom');assert.throws(()=>action(a,'mushroom','pickup'),/first/);
  at(a,'board');action(a,'board','job');assert.equal(a.profile.job,1);
  at(a,'mushroom');action(a,'mushroom','pickup');assert.equal(a.profile.job,2);
  at(a,'bodega');action(a,'bodega','deliver');assert.equal(a.profile.job,0);assert.equal(a.profile.tokens,10);assert.equal(a.profile.xp,20);
  assert.throws(()=>action(a,'bodega','deliver'),/not carrying/);at(a,'board');assert.throws(()=>action(a,'board','job'),/packed/);
});
test('purchases use server prices, require correct vendor, and bag cannot duplicate',t=>{
  const {market,client,action,at,advance}=setup(t);const a=client();a.profile.tokens=30;
  at(a,'bags');advance(400);market.handle(a,{type:'action',vendor:'bags',action:'buy',item:'guard-bag',price:0});
  assert.equal(a.profile.tokens,18);assert.equal(a.profile.bag,true);
  assert.throws(()=>action(a,'bags','buy','guard-bag'),/already/);
  assert.throws(()=>action(a,'bags','buy','paper-bird'),/not sold/);
  at(a,'origami');action(a,'origami','buy','paper-bird');assert.equal(a.profile.tokens,12);
  a.profile.tokens=0;assert.throws(()=>action(a,'origami','buy','paper-bird'),/Not enough/);
});
test('resume restores same identity, inventory, and progress',t=>{
  const {market,client,action,at}=setup(t);const a=client();at(a,'exchange');action(a,'exchange','exchange');
  const token=a.out[0].token;market.leave(a);const b=client('New name',token);
  assert.equal(b.profile.id,a.profile.id);assert.equal(b.profile.tokens,8);assert.equal(b.profile.exchanged,true);
});
test('duplicate active session and invalid token are rejected',t=>{
  const {client}=setup(t);const a=client();
  assert.throws(()=>client('Intruder',a.out[0].token),/already playing/);
  assert.throws(()=>client('Other','a'.repeat(64)),/no longer valid/);
  assert.throws(()=>client('Other','made-up'),/Invalid guest/);
});
test('capacity is enforced, while leaving frees a slot',t=>{
  const {market,client}=setup(t,{capacity:1});const a=client();assert.throws(()=>client(),/full/);market.leave(a);assert.doesNotThrow(()=>client());
});
test('chat is bounded, sanitized, and throttled',t=>{
  const {market,client}=setup(t);const a=client('<i>Alice</i>');market.handle(a,{type:'chat',text:'<b>Hello</b>\0'+'x'.repeat(250)});
  const chat=a.out.at(-1);assert.equal(chat.type,'chat');assert.equal(chat.text.length,180);assert.equal(chat.text.includes('<'),false);assert.equal(chat.text.includes('\0'),false);
  assert.throws(()=>market.handle(a,{type:'chat',text:'Again'}),/moment/);
});
test('invalid messages and movement types cannot crash simulation',t=>{
  const {market,client}=setup(t);const a=client();
  for(const bad of [null,[],{type:'input',dx:'1',dz:0,seq:1},{type:'input',dx:NaN,dz:0,seq:1},{type:'teleport'}])assert.throws(()=>market.handle(a,bad));
  assert.doesNotThrow(()=>market.tick());
});
test('disk persistence survives server store restart and stores only a hashed token',()=>{
  const dir=mkdtempSync(join(tmpdir(),'tsfm-store-'));const file=join(dir,'market.sqlite');
  let store=new PlayerStore(file);
  try {
    const {profile,token}=store.join('','Alice',2);profile.tokens=47;profile.inventory=[{id:'paper-bird',name:'Living paper bird',count:1}];store.save(profile);
    const secret=store.db.prepare('SELECT secret FROM players').get().secret;assert.notEqual(secret,token);
    store.close();store=new PlayerStore(file);const resumed=store.join(token,'Ignored',0).profile;
    assert.equal(resumed.tokens,47);assert.equal(resumed.inventory[0].count,1);assert.equal(resumed.id,profile.id);
  }finally{store.close();rmSync(dir,{recursive:true,force:true});}
});

test('jogging has a separate server-controlled speed limit',t=>{
  const {market,client}=setup(t);const a=client();a.profile.x=0;a.profile.z=-30;
  market.handle(a,{type:'input',dx:99,dz:0,seq:1,jog:true,speed:999});market.tick(.05);
  assert.ok(Math.abs(a.profile.x-world.jogSpeed*.05)<1e-8);
});
test('street pickups enforce distance, valid ID and one collection per guest',t=>{
  const {client,action,advance,market}=setup(t);const a=client(),item=world.pickups[0];
  assert.throws(()=>action(a,'','collect',item.id),/closer/);
  Object.assign(a.profile,{x:item.x,z:item.z});action(a,'','collect',item.id);
  assert.equal(a.profile.inventory.find(i=>i.id===item.kind).count,1);
  assert.throws(()=>action(a,'','collect',item.id),/already/);
  assert.throws(()=>action(a,'','collect','invented-item'),/closer/);
  const token=a.out[0].token;market.leave(a);const restored=client('Return',token);
  assert.ok(restored.profile.collected.includes(item.id));
  assert.throws(()=>action(restored,'','collect',item.id),/already/);
});
test('trash can consumes only actual litter and cannot mint currency',t=>{
  const {client,action,at}=setup(t);const a=client();at(a,'trash');
  assert.throws(()=>action(a,'trash','recycle'),/Find/);
  a.profile.inventory=[{id:'cassette',name:'Cassette',count:1},{id:'bottle',name:'Bottle',count:1}];
  action(a,'trash','recycle');assert.equal(a.profile.tokens,0);assert.equal(a.profile.xp,1);
  assert.deepEqual(a.profile.inventory.map(i=>i.id),['cassette']);
  assert.throws(()=>action(a,'trash','recycle'),/Find/);
});
