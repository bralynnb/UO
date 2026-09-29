# Browser edition

Hosted game: https://pale-moonlight-chapter-one.bralynn.chatgpt.site

Chapter-1-Studio.zip contains the complete compiled SLUDGE game, sprites, fonts, editable project and local Python studio. Extract it before building. The text source files here track the browser additions and editor source. Only Chapter 1 is implemented.

The web loader uses genuine ScummVM WASM and libsludge.so from https://scummvm.kuendig.io/. Download scummvm.js, scummvm.wasm and data/plugins/libsludge.so from that runtime. Split the wasm file at byte 20000000 into scummvm.wasm.0 and scummvm.wasm.1, place the plugin at web/plugins/libsludge.so, and compiled game at web/game/chapter1.slg. Serve web over HTTP. Include assets and project.json for the editor. Use GPL-compliant ScummVM distribution and provide upstream source links.

The hosted editor saves drafts locally and exports JSON. Rebuild those drafts using the local studio; browser compilation is not implemented. Native gameplay was tested; actual browser startup has not been verified in this execution environment.
