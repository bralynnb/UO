# Architecture — 0.2

`Assets/TSFM/Resources/block.json` defines all metre-scale bounds, obstacle footprints, vendors and pickup positions. Unity and Node read the same versioned file (world version 2).

`MarketWorld` generates the exterior city and procedural props. `MarketGame` controls guest entry, camera-relative movement, interaction selection, pathfinding and world pickup visibility. `ThirdPersonCamera` follows the rendered player with orbit/zoom and obstruction tests against the shared obstacles. `MarketHud` uses Unity UI for the guest screen, chat, inventory, help and vendor panels. `WalkAnimation` gives the simple player models a procedural gait.

`MarketConnection` uses a browser WebSocket bridge in WebGL and ClientWebSocket in the desktop Editor. Commands are normalized movement input, chat and actions, not arbitrary client positions or inventory writes. The server runs at 20 Hz. World version mismatches are rejected by the client.

`server/src/game.mjs` validates each action and returns the private profile to that guest. Public snapshots contain only player identity, name, appearance, position, rotation and bag presence. Resume secrets are hashed in SQLite and never included in public snapshots. Pickups are one-time per guest, persisted in `collected`; they do not represent globally scarce shared ground inventory. Trade and rewards remain server-owned.

Ambient shoppers, rooftop birds and vendors are deterministic local scene content; they are not synchronized AI agents. Cars are static obstacle-backed props. Buildings are not enterable. The full loop is on a flat ground plane, with no jump or vertical navigation.

`BuildGame` prepares an empty boot scene and a referenced Standard material, then builds actual Unity WebGL output into `server/public/game`. The Node server reports readiness only when that output exists. The manual GitHub Actions workflow packages the client and server, but it requires the owner's valid Unity activation configuration and a chosen repository. Nothing has been published by this revision.
