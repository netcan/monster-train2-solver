#!/bin/bash

poju_error() { printf '安装未完成：%s\n' "$*" >&2; return 1; }

poju_check_existing_runtime() {
    local root=$1 path dll
    if [ -L "$root/BepInEx/plugins/PojuAdvisor" ] || [ -L "$root/Start-Poju.command" ]; then
        poju_error '助手目录或启动器是符号链接，请先移走该链接再安装。'; return 1
    fi
    if [ -e "$root/BepInEx/plugins/PojuAdvisor" ] && [ ! -d "$root/BepInEx/plugins/PojuAdvisor" ]; then
        poju_error 'PojuAdvisor 应为目录。'; return 1
    fi
    if [ -e "$root/BepInEx/core" ] || [ -e "$root/libdoorstop.dylib" ] || [ -e "$root/run_bepinex.sh" ]; then
        if [ -f "$root/BepInEx/core/BepInEx.Core.dll" ]; then
            poju_error '检测到 BepInEx 6；此包需要 BepInEx 5 Mono。'; return 1
        fi
        for path in BepInEx/core/BepInEx.dll BepInEx/core/BepInEx.Preloader.dll BepInEx/core/0Harmony.dll libdoorstop.dylib run_bepinex.sh; do
            if [ ! -f "$root/$path" ]; then
                poju_error "现有 BepInEx 不完整，缺少 $path。请先整理现有安装。"; return 1
            fi
        done
    fi
    if [ -d "$root/BepInEx/plugins" ]; then
        while IFS= read -r -d '' dll; do
            if LC_ALL=C grep -a -F -q 'netcan.monstertrain2.isolationprobe' "$dll"; then
                poju_error "检测到开发用隔离探针：$dll。请先移走该 DLL。"; return 1
            fi
        done < <(find "$root/BepInEx/plugins" -type f -name '*.dll' -print0)
    fi
}

# Keep all replacement files on the game's volume. A failed update restores the
# previous plugin folder and launcher; the installer never edits saves/config.
poju_install_plugin() (
    set -euo pipefail
    local package_root=$1 game_root=$2 stage plugin_dir backup_dir=''
    local moved_old=0 installed_new=0 installed_launcher=0 committed=0 code
    [[ "$game_root" = /* ]] && [ "$game_root" != / ] || { poju_error '安装目标必须为游戏的绝对路径。'; exit 1; }
    plugin_dir="$game_root/BepInEx/plugins/PojuAdvisor"
    stage=$(mktemp -d "$game_root/.poju-install.XXXXXX")
    poju_finish() {
        code=$?
        trap - EXIT
        if [ "$committed" -eq 0 ]; then
            if [ "$installed_new" -eq 1 ]; then
                mv "$plugin_dir" "$stage/rejected-plugin" || printf '请从 %s 手动恢复助手。\n' "$backup_dir" >&2
            fi
            if [ "$moved_old" -eq 1 ] && [ ! -e "$plugin_dir" ]; then
                mv "$backup_dir/PojuAdvisor" "$plugin_dir" || printf '旧版保留在 %s。\n' "$backup_dir" >&2
            fi
            if [ -n "$backup_dir" ] && [ -f "$backup_dir/Start-Poju.command" ]; then
                cp -p "$backup_dir/Start-Poju.command" "$game_root/Start-Poju.command" || printf '启动器备份：%s\n' "$backup_dir" >&2
            elif [ "$installed_launcher" -eq 1 ]; then
                rm -f "$game_root/Start-Poju.command"
            fi
        fi
        case "$stage" in
            "$game_root"/.poju-install.??????)
                if [ -d "$stage" ] && [ ! -L "$stage" ]; then rm -rf -- "$stage"; fi ;;
        esac
        exit "$code"
    }
    trap poju_finish EXIT
    if [ -d "$plugin_dir" ]; then cp -R "$plugin_dir" "$stage/PojuAdvisor"
    else mkdir "$stage/PojuAdvisor"; fi
    for dll in MonsterTrain2Poju.Advisor.dll Model.dll FixtureArchive.dll; do
        # A copied symlink must not make a staged overwrite follow its target.
        rm -f "$stage/PojuAdvisor/$dll"
        cp "$package_root/BepInEx/plugins/PojuAdvisor/$dll" "$stage/PojuAdvisor/$dll"
    done
    cp "$package_root/Start-Poju.command" "$stage/Start-Poju.command"
    chmod u+x "$stage/Start-Poju.command"
    mkdir -p "$game_root/BepInEx/plugins"
    if [ -d "$plugin_dir" ] || [ -f "$game_root/Start-Poju.command" ]; then
        mkdir -p "$game_root/BepInEx/PojuBackups"
        backup_dir=$(mktemp -d "$game_root/BepInEx/PojuBackups/$(date +%Y%m%d-%H%M%S).XXXXXX")
        if [ -f "$game_root/Start-Poju.command" ]; then
            cp -p "$game_root/Start-Poju.command" "$backup_dir/Start-Poju.command"
        fi
        if [ -d "$plugin_dir" ]; then
            mv "$plugin_dir" "$backup_dir/PojuAdvisor"
            moved_old=1
        fi
    fi
    mv "$stage/PojuAdvisor" "$plugin_dir"
    installed_new=1
    mv "$stage/Start-Poju.command" "$game_root/Start-Poju.command"
    installed_launcher=1
    committed=1
    if [ -n "$backup_dir" ]; then printf '旧版备份：%s\n' "$backup_dir"; fi
)
