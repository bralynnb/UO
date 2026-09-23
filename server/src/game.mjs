import { world, move, nearby, vendors, catalog, walkable } from './world.mjs';
const publicPlayer = p => ({id:p.id,name:p.name,color:p.color,x:p.x,z:p.z,rotation:p.rotation,bag:p.bag});
const clean = (s, n) => typeof s === 'string' ? s.replace(/[<>\u0000-\u001f\u007f]/g, '').trim().slice(0,n) : '';
export class Market {
  constructor(store, {capacity=64, now=Date.now}={}) {
    this.store=store; this.capacity=capacity; this.now=now; this.clients=new Map(); this.tickNumber=0;
  }
  send(client, message) { client.send(message); }
  broadcast(message) { for (const c of this.clients.values()) this.send(c,message); }
  join(client, message) {
    if (this.clients.size >= this.capacity) throw new Error('This block is full. Please try again shortly.');
    const name=clean(message.name,20).replace(/[^a-zA-Z0-9 _.-]/g,'') || 'Visitor';
    const {profile,token}=this.store.join(message.token,name,Number.isInteger(message.color)?Math.max(0,Math.min(5,message.color)):0);
    if ([...this.clients.values()].some(c=>c.profile.id===profile.id)) throw new Error('This guest is already playing in another tab. Use New guest for a second player.');
    if (!walkable(profile.x, profile.z)) {profile.x=world.spawnX;profile.z=world.spawnZ;}
    profile.collected ??= [];
    Object.assign(client,{profile,dx:0,dz:0,jog:false,lastInput:this.now(),lastChat:-Infinity,lastAction:-Infinity,seq:-1});
    this.clients.set(client.id,client);
    this.send(client,{type:'welcome',id:profile.id,token,profile,capacity:this.capacity,worldVersion:world.version});
    this.broadcast({type:'notice',text:`${profile.name} entered the market.`});
    this.snapshot();
  }
  handle(client, message) {
    if (!message || typeof message !== 'object' || Array.isArray(message)) throw new Error('Invalid message.');
    if (message.type==='join') {
      if (client.profile) throw new Error('Already joined.');
      return this.join(client,message);
    }
    if (!client.profile) throw new Error('Join the market first.');
    if (message.type==='input') {
      if (!Number.isFinite(message.dx)||!Number.isFinite(message.dz)||!Number.isSafeInteger(message.seq)) throw new Error('Invalid movement.');
      if (message.seq<=client.seq) return;
      client.seq=message.seq;client.jog=message.jog===true;
      client.dx=Math.max(-1,Math.min(1,message.dx));client.dz=Math.max(-1,Math.min(1,message.dz));client.lastInput=this.now();
    } else if (message.type==='chat') {
      if (this.now()-client.lastChat<1000) throw new Error('Give the street a moment between messages.');
      const text=clean(message.text,180);
      if (!text) return;
      client.lastChat=this.now();
      this.broadcast({type:'chat',id:client.profile.id,name:client.profile.name,text});
    } else if (message.type==='action') {
      if (this.now()-client.lastAction<350) throw new Error('One thing at a time.');
      client.lastAction=this.now(); this.action(client,message);
    } else if (message.type==='ping') this.send(client,{type:'pong'});
    else throw new Error('Unknown message type.');
  }
  action(client, message) {
    const p=client.profile;
    if (message.action==='collect') {
      const item=world.pickups.find(i=>i.id===message.item);
      if (!item || Math.hypot(p.x-item.x,p.z-item.z)>2.2) throw new Error('Walk closer to the item.');
      if (p.collected.includes(item.id)) throw new Error('You have already collected this item.');
      const owned=p.inventory.find(i=>i.id===item.kind);
      if (owned?.count>=99) throw new Error('Your pockets are full.');
      if (owned) owned.count++; else p.inventory.push({id:item.kind,name:item.name,count:1});
      p.collected.push(item.id);this.store.save(p);
      this.send(client,{type:'profile',profile:p,text:`Picked up ${item.name.toLowerCase()}.`});return;
    }
    const vendor=vendors.get(message.vendor);
    if (!nearby(p,vendor)) throw new Error('Walk closer to the vendor.');
    let text='';
    if (message.action==='recycle' && vendor.id==='trash') {
      const item=p.inventory.find(i=>['newspaper','bottle','coffee-cup'].includes(i.id)&&i.count>0);
      if (!item) throw new Error('Find a discarded newspaper, bottle, or coffee cup first.');
      item.count--;p.inventory=p.inventory.filter(i=>i.count>0);p.xp+=1;text='The trash can eats your litter. +1 reputation.';
    } else if (message.action==='exchange' && vendor.id==='exchange') {
      if (p.exchanged) throw new Error('Your seed packet has already been exchanged.');
      p.exchanged=true;p.tokens+=8;p.xp+=5;text='Earth seeds exchanged for 8 scarcity tokens.';
    } else if (message.action==='job' && vendor.id==='board') {
      if (p.job!==0) throw new Error('Finish the current delivery first.');
      if (this.now()<p.cooldownUntil) throw new Error('The next delivery is still being packed. Try again in a moment.');
      p.job=1;text='Job accepted. Pick up the order at the mushroom stall.';
    } else if (message.action==='pickup' && vendor.id==='mushroom') {
      if (p.job!==1) throw new Error('Take a delivery job from the work board first.');
      p.job=2;text='Order collected. Carry it across the street to the bodega.';
    } else if (message.action==='deliver' && vendor.id==='bodega') {
      if (p.job!==2) throw new Error('You are not carrying a delivery.');
      p.job=0;p.tokens+=10;p.xp+=20;p.deliveries++;p.cooldownUntil=this.now()+15000;
      text='Delivery complete. Earned 10 scarcity tokens and 20 reputation.';
    } else if (message.action==='buy') {
      const item=catalog.get(message.item);
      if (!item || item.id!==vendor.id) throw new Error('That item is not sold here.');
      if (p.tokens<item.price) throw new Error('Not enough scarcity tokens.');
      if (item.item==='guard-bag' && p.bag) throw new Error('Your guard bag is already on duty.');
      let owned=p.inventory.find(i=>i.id===item.item);
      if (owned && owned.count>=99) throw new Error('Your pockets are full.');
      p.tokens-=item.price;
      if (owned) owned.count++; else p.inventory.push({id:item.item,name:item.itemLabel,count:1});
      if (item.item==='guard-bag') p.bag=true;
      text=item.item==='guard-bag'?'Guard bag hired. It will follow you around the market.':`Bought ${item.itemLabel.toLowerCase()}.`;
    } else throw new Error('That action is not available here.');
    this.store.save(p);
    this.send(client,{type:'profile',profile:p,text});
  }
  tick(dt=.05) {
    dt=Math.min(.1,Math.max(0,dt));
    for (const c of this.clients.values()) {
      if (this.now()-c.lastInput>250) {c.dx=0;c.dz=0;}
      move(c.profile,c.dx,c.dz,dt,c.jog);
    }
    this.tickNumber++;this.snapshot();
  }
  snapshot() { this.broadcast({type:'state',tick:this.tickNumber,players:[...this.clients.values()].map(c=>publicPlayer(c.profile))}); }
  leave(client) {
    if (!this.clients.has(client.id)) return;
    this.store.save(client.profile);this.clients.delete(client.id);
    this.broadcast({type:'notice',text:`${client.profile.name} left the market.`});this.snapshot();
  }
  saveAll() {for (const c of this.clients.values()) this.store.save(c.profile);}
}
