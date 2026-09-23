# Build and publish City Block 01

The public play URL will exist only after both the Unity browser build and the game server have been deployed. This source package does not contain a compiled Unity game.

## 1. Get the project

The project is maintained in [bralynnb/UO](https://github.com/bralynnb/UO). Clone it, or download its ZIP from GitHub:

```sh
git clone https://github.com/bralynnb/UO.git
cd UO
```

Use GitHub's normal authentication. The ignore rules exclude license files, player data, credentials, and generated Unity output. No open-source license has been assigned to the user's worldbuilding or project.

## 2. Produce the Unity Web build

### Build on your computer

Install Unity 6000.0.62f1 and Web Build Support through Unity Hub, activate your license, and open the project. Choose **TSFM → Build browser game**. A successful build writes `server/public/game/index.html` and its `Build` directory.

Windows PowerShell alternative:

```powershell
.\scripts\build-webgl.ps1
```

macOS/Linux alternative:

```sh
UNITY_EDITOR='/absolute/path/to/Unity' bash scripts/build-webgl.sh
```

Close any other Editor instance using this project before a batch build. Review `unity-build.log` if it fails.

### Build with GitHub Actions

The **Build Unity browser game** workflow runs manually from the Actions tab. It expects the Personal license activation secrets described in [GameCI's current activation instructions](https://game.ci/docs/github/activation/): `UNITY_LICENSE`, `UNITY_EMAIL`, and `UNITY_PASSWORD`. Enter secrets directly in the repository's Actions settings, not in a commit or chat. If you have a different license type, follow the corresponding GameCI activation path and adapt the workflow rather than mixing license types.

The workflow compiles the Unity client and uploads `tsfm-unity-browser-and-server`, containing `tsfm-deploy.tar.gz`. Download and extract that archive on your host. It includes the browser files, Node server, catalog, and deployment configuration. It does not automatically provision or charge for a hosting account.

Reference: [GameCI custom build methods](https://game.ci/docs/github/builder/). The editor build method uses its own default output directory; no custom CI environment forwarding is required.

## 3. Verify before public hosting

With the compiled files present:

```sh
npm ci
npm start
```

Open `http://localhost:8080` in two separate browser profiles. Confirm that each can join, see the other player, move, chat, complete a delivery, spend tokens, and reconnect with the same progress. The guest pass is stored by Unity in that browser's site storage. Clearing site storage loses access to that guest; there is no account recovery or cross-device login in this version.

## 4. Host the game and server

Use a host capable of a long-running Node 24 process or Docker container, WebSockets, HTTPS, and a persistent disk. Choose a host you control and its cost plan before provisioning it. The setup is one process for one block; do not deploy multiple replicas against separate SQLite files.

### Docker on a server with a domain

After a successful build, create a `.env` file in the project directory:

```dotenv
DOMAIN=your-game-hostname.example
```

Replace the example with a hostname you own. Point its DNS to your server and open ports 80 and 443. Then:

```sh
docker compose up -d --build
```

Caddy serves HTTPS and proxies both game assets and WebSockets to the game process. The public address is `https://YOUR_ACTUAL_HOSTNAME/`. This is a placeholder pattern, not an issued URL. The `market_data` volume preserves players across container restarts.

The Docker build refuses to proceed without the Unity build, and the HTTPS proxy waits for the game's readiness check. That prevents deploying the setup page as if it were the game.

### Host Node directly

Set `PORT`, `DATABASE_PATH`, and `PUBLIC_ORIGIN` for the public hostname. Run `npm ci --omit=dev`, then `npm start` under the host's process supervisor. Use the host's HTTPS reverse proxy with WebSocket upgrade support. Keep the database on persistent storage. Make the compiled browser files available under `server/public/game`.

| Setting | Default | Meaning |
| --- | --- | --- |
| `PORT` | 8080 | HTTP and WebSocket port |
| `DATABASE_PATH` | `data/market.sqlite` | Persistent player database |
| `PUBLIC_ORIGIN` | Same host as request | Expected browser origin, e.g. your HTTPS hostname |
| `MAX_PLAYERS` | 64 | Maximum simultaneous guests on this single block |

`GET /healthz` confirms process health and reports whether the client build exists. `GET /readyz` returns success only when the browser entry file exists. Readiness is not a substitute for a browser playtest.

## Persistence and operations

Back up SQLite with its online backup support, or stop the process cleanly before copying the database and WAL files. Do not discard the data volume when redeploying. The game saves transactions immediately, positions every five seconds, and connected players on graceful shutdown. Unexpected termination can lose the last few seconds of movement.

Before promoting beyond a small prototype, add moderator tools, reports/mutes, abuse monitoring, backups, account recovery, production load testing, and browser/device testing. A real multi-block MMO additionally needs shard routing, shared database transactions, presence across instances, and deployment operations.
