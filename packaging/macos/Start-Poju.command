#!/bin/bash
set -euo pipefail
game_root=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd -P)
cd "$game_root"
if [ "$#" -gt 0 ]; then
    exec /bin/sh "$game_root/run_bepinex.sh" "$@"
fi
shopt -s nullglob
apps=("$game_root"/*.app)
if [ "${#apps[@]}" -ne 1 ]; then
    printf '找不到唯一的游戏 .app，请从 Steam 启动。\n' >&2
    exit 1
fi
exec /bin/sh "$game_root/run_bepinex.sh" "${apps[0]}"
