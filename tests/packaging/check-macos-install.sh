#!/bin/bash
set -euo pipefail
repo=$(CDPATH= cd -- "$(dirname -- "$0")/../.." && pwd -P)
source "$repo/packaging/macos/poju-common.sh"
test_root=$(mktemp -d "${TMPDIR:-/tmp}/poju-installer-test.XXXXXX")
cleanup() {
    case "$test_root" in */poju-installer-test.??????) rm -rf -- "$test_root" ;; esac
}
trap cleanup EXIT
package="$test_root/package"
game="$test_root/游戏目录 with spaces"
plugin="$game/BepInEx/plugins/PojuAdvisor"
mkdir -p "$package/BepInEx/plugins/PojuAdvisor" "$game"
for dll in MonsterTrain2Poju.Advisor.dll Model.dll FixtureArchive.dll; do
    printf 'new %s\n' "$dll" > "$package/BepInEx/plugins/PojuAdvisor/$dll"
done
printf 'new launcher\n' > "$package/Start-Poju.command"
poju_check_existing_runtime "$game"
poju_install_plugin "$package" "$game"
for dll in MonsterTrain2Poju.Advisor.dll Model.dll FixtureArchive.dll; do
    cmp "$package/BepInEx/plugins/PojuAdvisor/$dll" "$plugin/$dll"
done
printf 'personal file\n' > "$plugin/extra.txt"
mkdir -p "$game/BepInEx/config"
printf 'user config\n' > "$game/BepInEx/config/netcan.monstertrain2.advisor.cfg"
poju_install_plugin "$package" "$game"
[ "$(cat "$plugin/extra.txt")" = 'personal file' ]
[ "$(cat "$game/BepInEx/config/netcan.monstertrain2.advisor.cfg")" = 'user config' ]

# Fail after the plugin folder has been replaced, while committing the launcher.
# A command stub injects I/O failure without adding a test bypass to the product.
for dll in MonsterTrain2Poju.Advisor.dll Model.dll FixtureArchive.dll; do
    printf 'old %s\n' "$dll" > "$plugin/$dll"
done
printf 'old launcher\n' > "$game/Start-Poju.command"
mv() {
    if [[ "${!#}" = "$game/Start-Poju.command" && "$*" = *".poju-install."* ]]; then return 23; fi
    command mv "$@"
}
set +e
(set -e; poju_install_plugin "$package" "$game")
result=$?
set -e
unset -f mv
[ "$result" -ne 0 ]
for dll in MonsterTrain2Poju.Advisor.dll Model.dll FixtureArchive.dll; do
    [ "$(cat "$plugin/$dll")" = "old $dll" ]
done
[ "$(cat "$game/Start-Poju.command")" = 'old launcher' ]
[ "$(cat "$plugin/extra.txt")" = 'personal file' ]
[ -z "$(find "$game" -maxdepth 1 -name '.poju-install.*' -print)" ]

printf 'partial\n' > "$game/run_bepinex.sh"
if poju_check_existing_runtime "$game"; then exit 1; fi
mkdir -p "$game/BepInEx/core"
for path in BepInEx.dll BepInEx.Preloader.dll 0Harmony.dll; do
    printf 'runtime\n' > "$game/BepInEx/core/$path"
done
printf 'loader\n' > "$game/libdoorstop.dylib"
poju_check_existing_runtime "$game"
printf 'v6\n' > "$game/BepInEx/core/BepInEx.Core.dll"
if poju_check_existing_runtime "$game"; then exit 1; fi
rm "$game/BepInEx/core/BepInEx.Core.dll"
printf 'netcan.monstertrain2.isolationprobe\n' > "$game/BepInEx/plugins/renamed-development.dll"
if poju_check_existing_runtime "$game"; then exit 1; fi
printf 'MACOS-INSTALLER PASS: fresh install, preserving upgrade, mid-update rollback, spaces/Unicode, incomplete runtime, v6 and development probe rejection.\n'
