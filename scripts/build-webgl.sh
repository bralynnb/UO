#!/usr/bin/env bash
set -euo pipefail
: "${UNITY_EDITOR:?Set UNITY_EDITOR to the path of your activated Unity 6000.0.62f1 editor}"
project_path="$(cd -- "$(dirname -- "$0")/.." && pwd)"
"$UNITY_EDITOR" -batchmode -quit -projectPath "$project_path" -buildTarget WebGL -executeMethod TSFM.Editor.BuildGame.WebGL -logFile "$project_path/unity-build.log"
