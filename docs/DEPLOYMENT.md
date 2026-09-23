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

The **Build Unity browser game** workflow runs on relevant pushes to `main` and manually from the Actions tab. Follow the [activation guide](ACTIVATION.md) to configure `UNITY_LICENSE`, `UNITY_EMAIL`, and `UNITY_PASSWORD` for Personal, or `UNITY_SERIAL` with the same email/password secrets for Pro. Choose one license method. Enter secrets directly in the repository's Actions settings. If configuration is missing, the run summary lists the missing secret names and stops before compilation. After adding them, choose **Re-run failed jobs**.

The workflow compiles the Unity client, verifies its referenced runtime files and WebAssembly header, and checks that the Docker image builds. It uploads `tsfm-unity-browser-and-server`, containing `tsfm-deploy.tar.gz` and its SHA-256 checksum. Download and extract that archive on your host. It includes the browser files, Node server, catalog, and deployment configuration. It does not provision or charge for a hosting account.

Reference: [GameCI custom build methods](https://game.ci/docs/github/builder/). The editor build method uses its own default output directory; no custom CI environment forwarding is required.

## 3. Verify before public hosting

With the compiled files present:

```sh
npm ci
npm run check:build
npm start
```

Start or restart the server after compiling; build readiness is checked at process startup. Open `http://localhost:8080` in two separate browser profiles. Confirm that each can join, see the other player, move, chat, complete a delivery, spend tokens, and reconnect with the same progress. The guest pass is stored by Unity in that browser's site storage. Clearing site storage loses access to that guest; there is no account recovery or cross-device login in this version.

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

### Railway

Railway can host the compiled Unity client and Node server together on one HTTPS domain, including `/ws`. Use the compiled deployment bundle, because the GitHub source does not contain the Unity output. Connecting Railway directly to the uncompiled repository will fail the Docker build check.

Prepare an empty service called `tsfm-market` in your Railway project:

| Setting | Value |
| --- | --- |
| Deploy source | Local upload of the extracted `tsfm-deploy.tar.gz` bundle |
| Persistent volume mount | `/app/data` |
| `DATABASE_PATH` | `/app/data/market.sqlite` |
| `MAX_PLAYERS` | `64` |
| `NODE_ENV` | `production` |
| `RAILWAY_RUN_UID` | `0` (Railway's documented workaround for root-owned mounted volumes) |
| Replicas | `1`; one shared market process |
| Health check | `/readyz` |
| Public domain target port | `8080` (or the service's configured `PORT`) |

`railway.json` supplies the Docker builder, one replica, readiness path, and bounded failure retries. The volume, variables and public domain must be configured in the hosting account. Keep Railway's existing plan and usage limits unless you explicitly approve a change; this configuration does not buy a plan or guarantee free continuous hosting.

Download the successful GitHub build artifact and extract the ZIP, then:

```sh
sha256sum -c tsfm-deploy.tar.gz.sha256
mkdir tsfm-deploy
tar -xzf tsfm-deploy.tar.gz -C tsfm-deploy
cd tsfm-deploy
npm ci --omit=dev --ignore-scripts
npm run check:build
railway login
railway link
railway up --service tsfm-market
```

Install the [Railway CLI](https://docs.railway.com/cli) first. Run this upload from the extracted bundle, not the Unity project folder; the bundle deliberately omits local credentials, Editor caches and the source repository's rules that ignore generated game files. Keep GitHub source autodeploys disabled for this service. Re-upload the new compiled bundle for each release.

After a healthy deployment, generate a Railway public domain in **Settings → Networking**, set `PUBLIC_ORIGIN` to that exact `https://...` origin, and apply the variable change. Railway provides TLS and WebSocket routing, so the Caddy service is not needed there. Verify `/readyz`, play with two separate guests, then restart the service and confirm the same guest's inventory and wallet survive. Keep the volume attached when redeploying.

References: [public networking](https://docs.railway.com/networking/public-networking/specs-and-limits), [persistent volumes and UID permissions](https://docs.railway.com/volumes/reference), [config as code](https://docs.railway.com/config-as-code/reference).

### Host Node directly

Set `PORT`, `DATABASE_PATH`, and `PUBLIC_ORIGIN` for the public hostname. Run `npm ci --omit=dev`, then `npm start` under the host's process supervisor. Use the host's HTTPS reverse proxy with WebSocket upgrade support. Keep the database on persistent storage. Make the compiled browser files available under `server/public/game`.

| Setting | Default | Meaning |
| --- | --- | --- |
| `PORT` | 8080 | HTTP and WebSocket port |
| `DATABASE_PATH` | `data/market.sqlite` | Persistent player database |
| `PUBLIC_ORIGIN` | Same host as request | Expected browser origin, e.g. your HTTPS hostname |
| `MAX_PLAYERS` | 64 | Maximum simultaneous guests on this single block |

`GET /healthz` confirms process health and reports whether the client passed structural build checks. `GET /readyz` returns success only when the generated entry file, referenced loader/framework/data files, and valid WebAssembly header were present at server startup. Empty, incomplete, or unprocessed template output stays unavailable. Readiness is not a substitute for a browser playtest.

## Persistence and operations

Back up SQLite with its online backup support, or stop the process cleanly before copying the database and WAL files. Do not discard the data volume when redeploying. The game saves transactions immediately, positions every five seconds, and connected players on graceful shutdown. Unexpected termination can lose the last few seconds of movement.

Before promoting beyond a small prototype, add moderator tools, reports/mutes, abuse monitoring, backups, account recovery, production load testing, and browser/device testing. A real multi-block MMO additionally needs shard routing, shared database transactions, presence across instances, and deployment operations.
