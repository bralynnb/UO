import { closeSync, openSync, readFileSync, readSync, statSync } from 'node:fs';
import { resolve } from 'node:path';

// Structural deployment check, not a substitute for loading the game in a browser.
// Compression is disabled in BuildGame.cs, so these are the actual served files.
export function inspectClientBuild(directory) {
  try {
    const html = readFileSync(resolve(directory, 'index.html'), 'utf8');
    if (html.includes('{{{') || !html.includes('createUnityInstance')) {
      throw new Error('index.html is not a generated Unity player.');
    }
    const assets = [];
    for (const suffix of ['loader.js', 'framework.js', 'data', 'wasm']) {
      const references = [...html.matchAll(/["']\/?([A-Za-z0-9_.-]+)["']/g)]
        .map(match => match[1]).filter(name => name.endsWith('.' + suffix));
      const names = [...new Set(references)];
      if (names.length !== 1) throw new Error(`Missing or ambiguous ${suffix} reference.`);
      const asset = 'Build/' + names[0];
      const file = resolve(directory, asset);
      const info = statSync(file);
      if (!info.isFile() || info.size === 0) throw new Error(`Empty ${asset}.`);
      if (suffix === 'wasm') {
        const header = Buffer.alloc(8);
        const handle = openSync(file, 'r');
        try { readSync(handle, header, 0, 8, 0); } finally { closeSync(handle); }
        if (!header.equals(Buffer.from([0, 97, 115, 109, 1, 0, 0, 0]))) {
          throw new Error('The WebAssembly binary is missing its valid header.');
        }
      }
      assets.push(asset);
    }
    return { ready: true, assets };
  } catch (error) {
    return { ready: false, assets: [], reason: error.code === 'ENOENT' ? 'Unity build files are missing.' : error.message };
  }
}
