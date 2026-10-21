#!/bin/bash
set -euo pipefail

package_root=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd -P)
check_only=0
if [ "${1:-}" = "--check-only" ]; then check_only=1; shift; fi
game_root=${1:-"$HOME/Library/Application Support/Steam/steamapps/common/Monster Train 2"}

fail() { printf '安装未完成：%s\n' "$*" >&2; exit 1; }
[ "$(uname -s)" = Darwin ] || fail '请在 Mac 上运行此安装器。'
[ -d "$game_root" ] || fail "找不到游戏目录：$game_root。将游戏目录作为参数传入。"
game_root=$(CDPATH= cd -- "$game_root" && pwd -P)
if [[ "$game_root" = *.app ]]; then app=$game_root; game_root=$(dirname -- "$app")
else
    shopt -s nullglob
    apps=("$game_root"/*.app)
    [ "${#apps[@]}" -eq 1 ] || fail '游戏目录中应只有一个游戏 .app；也可以直接传入游戏 .app 路径。'
    app=${apps[0]}
fi
managed=$(find "$app/Contents" -type f -name Assembly-CSharp.dll -print -quit)
[ -n "$managed" ] || fail '未找到 Mono 游戏程序集 Assembly-CSharp.dll。此包不支持 IL2CPP，请保留原状并反馈此结果。'
binary_name=$(/usr/libexec/PlistBuddy -c 'Print :CFBundleExecutable' "$app/Contents/Info.plist")
binary="$app/Contents/MacOS/$binary_name"
[ -f "$binary" ] || fail '游戏 .app 内找不到可执行文件。'
architectures=$(lipo -archs "$binary")
case "$architectures" in
    *arm64*|*x86_64*) ;;
    *) fail "不支持的游戏架构：$architectures" ;;
esac
printf '游戏：%s\n处理器：%s\n' "$app" "$(uname -m)"
file "$binary"
printf '游戏架构：%s\n' "$architectures"
printf 'Mono 程序集：%s\n' "$managed"
(cd "$package_root" && shasum -a 256 -c SHA256SUMS) || fail '安装包校验失败，请重新获取。'
source "$package_root/poju-common.sh"
poju_check_existing_runtime "$game_root" || exit 1
if [ "$check_only" -eq 1 ]; then
    printf '\n环境检查通过，尚未写入游戏目录。实际加载和建议效果仍需游戏内确认。\n'
    exit 0
fi

if [ ! -f "$game_root/BepInEx/core/BepInEx.dll" ]; then
    unzip -n -q "$package_root/runtime/BepInEx_macos_universal_5.4.23.5.zip" -d "$game_root"
fi
poju_check_existing_runtime "$game_root" || exit 1
poju_install_plugin "$package_root" "$game_root"
chmod u+x "$game_root/run_bepinex.sh"
printf '\n安装完成。在 Steam 游戏属性 → 启动选项填入：\n"%s/Start-Poju.command" %%command%%\n' "$game_root"
printf '\n进入战斗后显示中文建议；Fn+F8 显示/隐藏。日志：%s/BepInEx/LogOutput.log\n' "$game_root"
printf '卸载：移走 BepInEx/plugins/PojuAdvisor，清除上述 Steam 启动选项。\n'
