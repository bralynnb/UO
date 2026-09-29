# Verification and current limits

## Verified

- The SLUDGE 2.2.2 compiler produces a real `.slg`, not a JavaScript simulation.
- ScummVM 2026.3.0 launches it using the SLUDGE engine.
- An automated mouse/keyboard playthrough reached the Chapter 1 completion state: terminal report, pickup, office-to-Ops transition, strategic review, PADD combination, shop transition, Garak dialogue choice, and completion save.
- F5 saved, F7 restored the completed state, and F2 entered/exited the placement editor. A prop was nudged and placement data was saved.
- Original four-direction Sisko walking sprites and all three scenes rendered in ScummVM.
- Editor DOM tests verified loading, property edits, undo, walkable polygon addition, prop creation/deletion, and the real compiler endpoint.
- The server rejected a self-intersecting polygon and preserved the last valid project.
- Python and JavaScript syntax checks passed.

The headless test environment required disabling its unavailable OS text-to-speech service. This was a test-environment workaround only; no modified ScummVM binary or preload library is part of this package. The game uses text, not speech synthesis.

## Limits

- Only Chapter 1 is playable. Chapters 2–10 are a development outline.
- Artwork and character animation are initial prototype assets, with a limited palette and simple shading. They are not production-quality LucasArts assets or exact DS9 room reproductions.
- The full visual editor runs as a local companion application. The native F2 editor supports placement and floor visualization, not full dialogue/polygon editing.
- ScummVM placement overrides and project edits have separate storage; there is no native-to-project export bridge.
- Browser visual QA could not run: the available Chromium binaries did not launch in the execution environment. Editor interaction and build tests used a DOM harness with mocked canvas drawing. Native game screenshots were visually inspected.
- This package does not contain a public browser deployment or a created GitHub repository. The GitHub Actions workflow is prepared but has not run on GitHub.
- No universal device-level performance or compatibility guarantee is asserted. The game avoids video, 3D, network polling, and large runtime libraries beyond ScummVM itself.

`chapter1-preview.png`, `chapter1-dialogue.png`, and `chapter1-complete.png` are actual screenshots captured during the ScummVM playthrough.
