# The Strangest Flea Market — NYC 1985

Unity 6 third-person browser multiplayer prototype, version 0.2.

Repository: [bralynnb/UO](https://github.com/bralynnb/UO).

**Delivery status:** source project with working and tested Node multiplayer server. The Unity client has not been compiled or visually playtested in this environment. No browser build or public play URL is included. Open in an activated Unity Editor to build it. The visuals are procedural prototype geometry, not photorealistic character models.

## This revision

- A complete walkable loop around one fictional downtown NYC block, using **1 Unity unit = 1 metre**. Play area 114 × 70 m; central building footprints approximately 77 × 32 m. It is not a surveyed reconstruction of a particular street.
- Third-person camera: right-drag orbit, 2.2–16 m zoom, obstruction handling, camera-relative walking at 1.65 m/s and jogging at 3.8 m/s.
- Continuous visible roadwork hoardings prevent leaving the block. Solid building, stall, parked-car and street-fixture footprints are shared with the server.
- Eight boxy period-inspired sedans, including two taxis; fire escapes, window air conditioners, rooftop water tanks, payphone, newspaper box, hydrants, milk crates, dumpster, crosswalks, drains, and streetlights.
- Eleven interactions: Exchange Machine, Work Board, Mushroom Merchant, Fox Origami, Guard Bag, Bodega Clerk, Elderly Infobot, Vinyl Record Cyclops, Sentient Trash Can, Sentient Bus Bench, and Construction Notice.
- Twelve ambient shoppers and rooftop watcher birds. Shopkeepers have procedural creature silhouettes. Shoppers and birds are local ambient visuals; connected players are real shared network entities.
- Twenty seeded street pickups: newspaper, glass bottle, cassette and coffee cup. Each guest can collect each once; progress is persisted. Litter can be fed to the trash can for reputation.
- Persistent, scrollable inventory; scarcity tokens, trading, a repeatable delivery job, guard-bag follower, real-time player movement and street chat.
- `?` help/options: controls, camera sensitivity, invert Y, shadows, fullscreen, reset camera.

## Open and play locally

1. In Unity Hub, install **Unity 6000.0.62f1** with **Web Build Support** and activate your Unity license.
2. Add this folder as a Unity project and let it import. The empty startup scene is prepared automatically; runtime scripts generate the block. If needed, use **TSFM → Prepare city block**.
3. Install Node.js 24, then run `npm ci` followed by `npm start` in this folder.
4. Press **Play** in Unity. Enter a guest name; the Editor connects to the local multiplayer server.
5. Choose **TSFM → Build browser game**. After it succeeds, open **http://localhost:8080**. For a second player, use another browser or choose **New guest** in another tab.

The server deliberately shows **Build required** until actual Unity WebGL output exists. There is no substitute JavaScript game pretending to be Unity.

## Controls

| Action | Control |
| --- | --- |
| Walk relative to camera | WASD / arrows |
| Jog | Hold Shift |
| Orbit | Hold right mouse button and drag |
| Zoom | Mouse wheel |
| Walk to ground location | Left click |
| Talk / collect nearest item | E / interaction card |
| Inventory | I / Your bag |
| Chat | Enter / chat field |
| Help and options | ? / F1 / ? button |
| Close panels | Escape |

Start by exchanging the seed packet for eight scarcity tokens. Take a delivery from the work board, collect it at the mushroom stall on the north sidewalk, and deliver it to the bodega on the south sidewalk. Walk around either end of the central buildings. The guide button calculates a walking route.

## Multiplayer and hosting

The Node server validates movement speed, collision, proximity, prices, pickup uniqueness and quest rewards. SQLite saves guest progress. The default capacity is 64 connections, not a production-scale MMO guarantee. Guest access is intended for this prototype; production moderation, account recovery, sharding and broader abuse protection are not implemented.

Build the Unity client and deploy it with the Node server behind HTTPS. A static GitHub Pages site alone cannot run this multiplayer server. See [deployment instructions](docs/DEPLOYMENT.md). The repository includes automatic server checks and a manual Unity build workflow. The Unity workflow requires activation secrets before it can produce the browser game.

## Validation

- `npm run check`: all 11 interaction points, all 20 pickups and all four street sides reachable.
- `npm test`: **22 passing** server and actual WebSocket integration tests.
- `npm run test:load`: 64 local connections, 60 ticks in three seconds; synthetic smoke test only.
- All 13 C# files parsed for syntax. Unity API compilation, WebGL compilation, rendered camera/UI behavior and browser performance remain unverified.

See [validation details](docs/VALIDATION.md) and [lore sources](docs/LORE-SOURCES.md).

## Boundaries of this prototype

Cars are parked, not a traffic simulation. Buildings are exterior-only. No combat, off-world portal travel, player-to-player trade, crafting, mobile touch controls or production account system. Ground pickups are instanced per guest so one player cannot exhaust them for everyone. Model and animation fidelity are intentionally simple.
