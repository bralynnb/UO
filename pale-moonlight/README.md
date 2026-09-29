# In the Pale Moonlight — Chapter 1

A small original fan-adventure prototype inspired by the broad story of the DS9 episode. Play as Sisko. Dialogue and puzzles are newly written, not a transcription. Chapter 1 is implemented; Chapters 2–10 are outlined in `docs/CHAPTERS.md` and `project.json`.

## Play

1. Install a current [ScummVM](https://www.scummvm.org/downloads/) release. This project is tested with **ScummVM 2026.3.0**, using its real SLUDGE engine. Older releases may detect the file without including a playable SLUDGE engine.
2. Extract this entire folder. In ScummVM, choose **Add Game**, select this project's **game** folder, and accept the detected SLUDGE game.
3. Start the game. Since this is a new, unregistered fan game, ScummVM may display an unknown/unsupported-game warning. Choose **Start anyway** for this project.

Windows users with ScummVM installed in its standard location can use **Play-Windows.bat**. On Linux/macOS with ScummVM on PATH:

```sh
scummvm --path=game sludge:sludge
```

The actual game is `game/chapter1.slg`. It includes its images, font, script, and floor geometry; no commercial game data is required. ScummVM itself is not included. This release is a desktop download, not a hosted browser game.

## Controls

- Click the floor to walk. Choose a verb, then an object. Right-click examines.
- To combine items, choose **Use**, select an inventory item, then click its target.
- Click or press Space during speech to advance it.
- **F1**: contextual help and puzzle hint.
- **F5**: save; **F7**: load. Chapter completion also saves automatically.
- **F2**: quick placement editor. Click a prop to select it, then click to place its top-left corner. Arrow keys nudge it. **S** saves the placement; **F** toggles the walkable-area outline. F2 returns to play. Right-click cancels selection.

ScummVM's normal quit/menu controls remain available. No combat, recorded voices, music, video, network loop, physics engine, or continuous browser rendering loop is used. The game targets 640×480 with nearest-neighbor pixel presentation. Performance depends on the device; no universal “zero lag” guarantee is possible.

## Project Studio

The visual studio is included at `editor/`. It edits objects, dimensions, rotation, flip, tint, opacity, render layer, interaction approach points, exits, walkable polygons, dialogue, inventory pickup flags, puzzle rules, and game settings. It is a **local companion editor**, not an overlay with all those controls inside ScummVM. The in-game F2 editor provides quick placement and floor visualization only.

Prerequisites: Python 3.10+, [Pillow](https://pillow.readthedocs.io/), and the [SLUDGE compiler](https://opensludge.github.io/download.html). On Ubuntu:

```sh
sudo apt-get install python3-pil sludge-compiler
python3 tools/studio.py
```

On Windows, install Python and the SLUDGE development kit, run `py -m pip install Pillow`, and set `SLUDGE_COMPILER` to your compiler executable if it is not on PATH. Then run `py tools/studio.py`.

Open **http://127.0.0.1:8765**. Move props by dragging, resize with the lower-right handle, or enter exact values. In Walkable Area mode, drag vertices, double-click near an edge to insert a vertex, or right-click a vertex to remove it. Save & build validates the project and compiles a new `.slg`. Close and restart the game to load that build.

**Separate editing scopes:** Project Studio changes `project.json` and rebuilds the distributable. In-game placement changes are local ScummVM save data. They are not exported into the project. To discard local placement overrides, remove `layout-v1-x.dat` and `layout-v1-y.dat` from your ScummVM save directory. Keep backups of saved games before replacing a build; altered scripts may make old saves incompatible.

## Build

```sh
python3 tools/build.py
```

If needed, set `SLUDGE_COMPILER` to an absolute executable path. `assets/` contains source PNGs and runtime sprite/font banks. `tools/art.py` reproduces the original code-drawn artwork on systems with the DejaVu Sans Mono font installed. Do not run `tools/seed.py` over edited data; it resets the initial project.

The authoring server binds only to localhost. It does not need an account, paid service, database, or internet connection. A failed validation/compilation leaves the current project intact. `project.previous.json` retains the last successful project's data.

## GitHub

This package is repository-ready, with an automated compilation workflow. No GitHub repository has been created by this download. Once an empty repository named `pale-moonlight-scummvm` exists in your account, use the normal Git push flow. The workflow builds the `.slg` and uploads it as an Actions artifact; it does not host a browser game.

## Scope and rights

This is an unofficial fan project, unaffiliated with Star Trek's owners, LucasArts, or ScummVM. Character names and the setting remain their respective owners' properties. It contains no extracted episode footage, music, dialogue transcript, or commercial LucasArts artwork. The interface follows the nine-verb adventure convention; it does not use the original LucasArts SCUMM engine or its source assets.

The visual art is an initial prototype, not a pixel-for-pixel recreation of Monkey Island, Fate of Atlantis, or a DS9 set. See `docs/TESTING.md` for verified behavior and remaining limitations. Font licensing is in `docs/FONT-LICENSE.txt`.

## Browser edition
`web/index.html` launches genuine ScummVM WebAssembly with only the SLUDGE engine plugin preloaded. Browser saves use IndexedDB. On the unknown-game notice choose Start anyway. F2 edits object positions in the running game; F5 saves and F7 loads.

The hosted scene editor saves local browser drafts and exports project.json. It does not compile in the browser: import the exported project into the downloadable Python studio, build, and replace web/game/chapter1.slg. Only Chapter 1 is playable.

ScummVM browser runtime is distributed under GPL-3.0-or-later, downloaded from https://scummvm.kuendig.io/ with corresponding upstream source at https://github.com/chkuendig/scummvm-demo and https://github.com/scummvm/scummvm. Runtime wasm is split into two transport chunks to meet static hosting limits, then reassembled unchanged before execution.

Native game playthrough verified; browser runtime startup still needs an actual browser check in this environment.
