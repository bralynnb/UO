#!/bin/sh
set -eu
ROOT=$(CDPATH= cd -- "$(dirname "$0")/.." && pwd)
export SCUMMC_ROOT="${SCUMMC_ROOT:-$ROOT/.toolchain/scummc}"
if [ ! -d "$SCUMMC_ROOT" ]; then
 mkdir -p "$(dirname "$SCUMMC_ROOT")"
 git clone https://github.com/AlbanBedel/scummc.git "$SCUMMC_ROOT"
 (cd "$SCUMMC_ROOT"; git checkout 2f61ce0f7b5051e920876dc4b71935dc99f38fff; sed -i 's@man/%\.xml@man/tools/%.xml@g' Makefile.target; ./configure --disable-gtk --disable-sdl --disable-freetype --disable-readline; make -j2)
fi
mkdir -p "$ROOT/web/plugins" "$ROOT/web/data"
for file in scummvm.js scummvm.wasm; do curl --fail --location --retry 3 "https://scummvm.kuendig.io/$file" -o "$ROOT/web/$file"; done
curl --fail --location --retry 3 https://scummvm.kuendig.io/data/plugins/libscumm.so -o "$ROOT/web/plugins/libscumm.so"
# This runtime embeds detection support. libdetection.so does not exist and is not requested.
curl --fail --location --retry 3 https://scummvm.kuendig.io/data/scummclassic.zip -o "$ROOT/web/data/scummclassic.zip"
printf '{"scummclassic.zip":16594}\n' > "$ROOT/web/data/index.json"
printf '[scummvm]\npluginspath=/plugins\ngui_theme=builtin\nshow_splash=false\n' > "$ROOT/web/scummvm.ini"
python3 "$ROOT/tools/build_game.py"
cp "$ROOT/project.json" "$ROOT/web/project.json"
