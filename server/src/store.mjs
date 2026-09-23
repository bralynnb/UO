import { DatabaseSync } from 'node:sqlite';
import { mkdirSync } from 'node:fs';
import { dirname } from 'node:path';
import { createHash, randomBytes, randomUUID } from 'node:crypto';
import { world } from './world.mjs';
const hash = token => createHash('sha256').update(token).digest('hex');
export class PlayerStore {
  constructor(filename) {
    if (filename !== ':memory:') mkdirSync(dirname(filename), {recursive:true});
    this.db = new DatabaseSync(filename);
    this.db.exec('PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL; CREATE TABLE IF NOT EXISTS players (id TEXT PRIMARY KEY, secret TEXT UNIQUE NOT NULL, profile TEXT NOT NULL, updated INTEGER NOT NULL);');
    this.read = this.db.prepare('SELECT profile FROM players WHERE secret = ?');
    this.insert = this.db.prepare('INSERT INTO players VALUES (?, ?, ?, ?)');
    this.write = this.db.prepare('UPDATE players SET profile = ?, updated = ? WHERE id = ?');
  }
  join(token, name, color) {
    if (typeof token === 'string' && /^[a-f0-9]{64}$/.test(token)) {
      const row = this.read.get(hash(token));
      if (row) return {profile: JSON.parse(row.profile), token};
      throw new Error('This guest pass is no longer valid. Use New guest to start again.');
    }
    if (token) throw new Error('Invalid guest pass.');
    const secret = randomBytes(32).toString('hex');
    const profile = {id:randomUUID(), name, color, x:world.spawnX, z:world.spawnZ, rotation:90,
      tokens:0, xp:0, job:0, deliveries:0, cooldownUntil:0, exchanged:false, bag:false, inventory:[], collected:[]};
    this.insert.run(profile.id, hash(secret), JSON.stringify(profile), Date.now());
    return {profile, token:secret};
  }
  save(profile) { this.write.run(JSON.stringify(profile), Date.now(), profile.id); }
  close() { this.db.close(); }
}
