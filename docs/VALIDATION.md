# Validation — 0.2, 23 September 2026

## Executed in this workspace

- Node.js 24.19.0; locked `ws` dependency installed using `npm ci`.
- Project/geometry checks passed. The walkable grid includes 5,048 cells; all 11 stations and 20 pickups are reachable from spawn, with a connected route around all four sides of the central building block.
- All 22 Node tests passed, including real WebSocket joins, shared movement, chat, disconnect, guest persistence, proximity rules, speed limits, static collision, economy, pickup uniqueness, recycling and malformed-message handling.
- 64-client loopback smoke test: 60 server ticks over three seconds; no simulated player escaped walkable space. Not a browser benchmark or a production capacity certification.
- Every C# source file was parsed with the tree-sitter C# grammar. Syntax parsing is not semantic compilation against Unity APIs.

## Not executed

Unity Editor and an activated Unity license are unavailable here. No Unity client compilation, WebGL build, 3D screenshot, browser gameplay test, rendered UI review or remote deployment was performed. The included scene is generated on Editor import; only a successful Unity build can produce the actual browser game.

## Required Unity acceptance pass

- Import in Unity 6000.0.62f1, confirm zero compiler errors, start Node server and enter play mode.
- Walk and jog around the full perimeter; attempt all construction exits and approach every solid object.
- Orbit and zoom near walls/cars; confirm the camera never enters a facade and controls remain usable.
- Pick up each item kind; reload and confirm saved inventory and pickup visibility agree.
- Complete the delivery loop, buy each item, hire a bag and recycle litter.
- Open long Infobot dialogue, filled inventory and options at 1280×720 and 1920×1080; check clipping, scroll, fullscreen, chat focus and mouse-wheel behavior.
- Build WebGL, visit the local URL in two desktop browsers, and confirm two named players see movement/chat and independent inventory.
- Profile browser frame time and memory before hosting publicly; procedural facade detail and static batching are not yet performance-tested.
