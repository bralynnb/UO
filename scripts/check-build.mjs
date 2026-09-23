import { resolve } from 'node:path';
import { inspectClientBuild } from '../server/src/client-build.mjs';

const build = inspectClientBuild(resolve(process.argv[2] || 'server/public/game'));
if (!build.ready) {
  console.error('Unity build is not ready: ' + build.reason);
  process.exitCode = 1;
} else {
  console.log('Unity browser files verified: index.html, ' + build.assets.join(', '));
}
