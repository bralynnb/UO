import test from 'node:test';
import assert from 'node:assert/strict';
import { mkdtempSync, mkdirSync, writeFileSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { once } from 'node:events';
import { inspectClientBuild } from '../src/client-build.mjs';
import { createMarketServer } from '../src/index.mjs';

// Temporary structural fixtures only; these are not playable Unity builds.
function fixture(t) {
  const root = mkdtempSync(join(tmpdir(), 'tsfm-build-test-'));
  t.after(() => rmSync(root, { recursive: true, force: true }));
  const game = join(root, 'game');
  mkdirSync(join(game, 'Build'), { recursive: true });
  writeFileSync(join(game, 'index.html'), `<script>
    const buildUrl='Build'; script.src=buildUrl+'/market.loader.js';
    createUnityInstance(canvas, {dataUrl:buildUrl+'/market.data',
      frameworkUrl:buildUrl+'/market.framework.js',codeUrl:buildUrl+'/market.wasm'});
  </script>`);
  for (const name of ['market.loader.js', 'market.framework.js', 'market.data']) {
    writeFileSync(join(game, 'Build', name), 'test fixture');
  }
  writeFileSync(join(game, 'Build', 'market.wasm'), Buffer.from([0, 97, 115, 109, 1, 0, 0, 0]));
  return { root, game };
}

test('Unity readiness requires every referenced asset, not just index.html', t => {
  const { game } = fixture(t);
  assert.equal(inspectClientBuild(game).ready, true);
  rmSync(join(game, 'Build', 'market.data'));
  assert.equal(inspectClientBuild(game).ready, false);
});

test('Unity readiness rejects an unprocessed template and invalid WebAssembly', t => {
  const { game } = fixture(t);
  writeFileSync(join(game, 'Build', 'market.wasm'), '<html>download error</html>');
  assert.match(inspectClientBuild(game).reason, /WebAssembly/);
  writeFileSync(join(game, 'index.html'), 'createUnityInstance {{{ LOADER_FILENAME }}}');
  assert.equal(inspectClientBuild(game).ready, false);
});

test('readiness stays unavailable when an index exists without the Unity binary', async t => {
  const { root, game } = fixture(t);
  rmSync(join(game, 'Build', 'market.wasm'));
  const app = createMarketServer({ database: ':memory:', publicDir: root });
  app.server.listen(0, '127.0.0.1'); await once(app.server, 'listening');
  t.after(() => app.close());
  const base = `http://127.0.0.1:${app.server.address().port}`;
  assert.equal((await fetch(base + '/readyz')).status, 503);
  assert.equal((await (await fetch(base + '/healthz')).json()).clientReady, false);
});

test('a complete structural build is served with WebAssembly MIME and root redirect', async t => {
  const { root } = fixture(t);
  const app = createMarketServer({ database: ':memory:', publicDir: root });
  app.server.listen(0, '127.0.0.1'); await once(app.server, 'listening');
  t.after(() => app.close());
  const base = `http://127.0.0.1:${app.server.address().port}`;
  assert.equal((await fetch(base + '/readyz')).status, 200);
  const redirect = await fetch(base, { redirect: 'manual' });
  assert.equal(redirect.status, 302); assert.equal(redirect.headers.get('location'), '/game/');
  const wasm = await fetch(base + '/game/Build/market.wasm', { method: 'HEAD' });
  assert.equal(wasm.status, 200); assert.equal(wasm.headers.get('content-type'), 'application/wasm');
});
