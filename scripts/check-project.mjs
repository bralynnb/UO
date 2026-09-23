import assert from 'node:assert/strict';
import { readFileSync,existsSync,readdirSync } from 'node:fs';
import { resolve } from 'node:path';
import { world,walkable,vendors } from '../server/src/world.mjs';
const root=resolve(import.meta.dirname,'..');
for(const file of ['Assets/TSFM/Scripts/MarketGame.cs','Assets/TSFM/Scripts/MarketHud.cs','Assets/TSFM/Scripts/MarketWorld.cs','Assets/TSFM/Scripts/MarketConnection.cs','Assets/TSFM/Editor/BuildGame.cs','Assets/TSFM/Plugins/WebGL/MarketSocket.jslib','Assets/WebGLTemplates/TSFM/index.html','ProjectSettings/ProjectVersion.txt','Packages/manifest.json','Dockerfile'])assert.ok(existsSync(resolve(root,file)),file);
assert.equal(world.vendors.length,11);assert.equal(new Set(world.vendors.map(v=>v.id)).size,world.vendors.length);
const visited=new Set(),queue=[[Math.round(world.spawnX),Math.round(world.spawnZ)]];
while(queue.length){const [x,z]=queue.shift(),id=x+','+z;if(visited.has(id)||!walkable(x,z))continue;visited.add(id);for(const [dx,dz] of [[1,0],[-1,0],[0,1],[0,-1]])queue.push([x+dx,z+dz]);}
for(const vendor of [...vendors.values(),...world.pickups])assert.ok(visited.has(Math.round(vendor.x)+','+Math.round(vendor.z)),vendor.id+' must be reachable from spawn');
for(const [x,z] of [[-48,0],[48,0],[0,30],[0,-30]])assert.ok(visited.has(x+','+z),'All four streets connect');
const unityProtocol=readFileSync(resolve(root,'Assets/TSFM/Scripts/Protocol.cs'),'utf8');
for(const field of ['worldVersion','cooldownUntil','deliveries','inventory','rotation','players'])assert.ok(unityProtocol.includes(field),field);
const template=readFileSync(resolve(root,'Assets/WebGLTemplates/TSFM/index.html'),'utf8');
for(const placeholder of ['LOADER_FILENAME','DATA_FILENAME','FRAMEWORK_FILENAME','CODE_FILENAME'])assert.ok(template.includes('{{{ '+placeholder+' }}}'));
// Parse the plugin as JavaScript without running browser APIs.
const plugin=readFileSync(resolve(root,'Assets/TSFM/Plugins/WebGL/MarketSocket.jslib'),'utf8');new Function(plugin);
console.log(`Project structure and protocol checked. All ${vendors.size} vendor stations reachable across ${visited.size} walkable grid cells.`);
console.log('Unity compilation and rendered gameplay require an activated Unity Editor; this check does not replace them.');
