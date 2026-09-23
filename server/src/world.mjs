import { readFileSync } from 'node:fs';
export const world = JSON.parse(readFileSync(new URL('../../Assets/TSFM/Resources/block.json', import.meta.url)));
export const vendors = new Map(world.vendors.map(v => [v.id, v]));
export const catalog = new Map(world.vendors.filter(v => v.item).map(v => [v.item, v]));
export function walkable(x, z) {
  if (!Number.isFinite(x) || !Number.isFinite(z) || x < world.minX || x > world.maxX || z < world.minZ || z > world.maxZ) return false;
  return !world.obstacles.some(o => Math.abs(x-o.x) < o.w/2+world.radius && Math.abs(z-o.z) < o.d/2+world.radius);
}
export function move(player, dx, dz, dt, jog=false) {
  const length = Math.hypot(dx, dz);
  if (length > 1) { dx /= length; dz /= length; }
  const distance = (jog ? world.jogSpeed : world.speed) * dt;
  const steps = Math.max(1, Math.ceil(distance / 0.15));
  for (let s=0; s<steps; s++) {
    const x = player.x + dx*distance/steps;
    const z = player.z + dz*distance/steps;
    if (walkable(x, player.z)) player.x = x;
    if (walkable(player.x, z)) player.z = z;
  }
  if (length > .01) player.rotation = Math.atan2(dx, dz)*180/Math.PI;
}
export function nearby(player, vendor) { return !!vendor && Math.hypot(player.x-vendor.x, player.z-vendor.z) <= 3.1; }
