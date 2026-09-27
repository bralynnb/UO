# TSFM — Night Train

An original three-car atmospheric subway prototype. Actual SCUMM v6 game resources are compiled with ScummC and executed by ScummVM WebAssembly. The JavaScript shell only loads the engine, handles hosting and edits scene JSON; it does not simulate the game.

320×200 indexed pixel presentation. Traveler is 56 pixels tall in an 80-pixel interior. The carriage wall and glass mask hide the actor between windows. Click the edges to switch between three cars. Nine native verbs; wallet, ten gold tokens and subway card. No passengers or objectives.

## Run

Use `docker build -t tsfm-subway .` and `docker run --rm -p 8000:8000 tsfm-subway`. Open localhost:8000. Alternatively install Python 3.12, the requirements, a C compiler, bison, flex, libpng-dev and git; run `sh tools/bootstrap.sh`, set SCUMMC_ROOT to this project's `.toolchain/scummc`, then `python serve.py`.

Edit opens the scene JSON with controls for scale, walking, tunnel timing and tokens; advanced JSON edits palette, windows and item responses. Applying an edit recompiles actual SCUMM resources in an isolated temporary folder and restarts the ride. Export JSON for backup. The current editor is a property editor, not a full drag-and-drop art/logic editor. Artwork and scripts are editable in tools/build_game.py; generated layers and SCUMM sources are in web/source after compilation. No arbitrary uploaded code is executed.

F5: native ScummVM menu. Space: pause. Runtime saves use browser IndexedDB; persistence depends on browser permissions. Browser tests must pass before a release is described as verified. Tests use physical pointer inputs and retain screenshots and engine logs.

The loader preloads the SCUMM engine module and original game data into the Emscripten filesystem. It does not request the nonexistent libdetection.so, and only removes the loading overlay when an actual room-entry message is emitted. Failed downloads show an error and retry instead of silently hanging.

## Attribution

Authoring compiler and common verb interface derive from Alban Bedel's ScummC, GPL-2.0-or-later, pinned at 2f61ce0f7b5051e920876dc4b71935dc99f38fff. Original game code/art in this project: GPL-2.0-or-later. ScummVM is a separate GPL-3.0-or-later runtime from its own developers. Browser binaries are fetched from scummvm.kuendig.io; upstream source is scummvm/scummvm on GitHub. No LucasArts game data, characters or backgrounds are included. The legacy tentacle filename is used only for SCUMM v6 runtime selection.

This is an initial art pass, not a finished commercial game. No audio track has been added yet. All changes are isolated to tsfm-subway-scummvm; main is untouched.
