# 破局助手 0.2.3 · Mac 试用版

本项目已经具备游戏内出牌建议的可用基础。Mod 显示下一张牌/技能、目标、
楼层和预计火炉血量；正常操作后自动更新，默认在后台搜索未来 3 回合，预算
3 秒。面板可重新计算、暂停或恢复。它读取已知牌序和随机状态，不保证全局
最优，也不是未知抽牌的概率策略。未支持的局面会停止给出建议，可继续手动玩。

当前验证来自 Windows Mono 游戏和归档原生对照，Mac 加载与实际对局尚未验证。
这是供 Mac 实机验收的版本，不能将成功编译等同于 Mac 已可用。推荐先在普通
单人战斗中试用。游戏更新或其他 Mod 更改规则后，可能无法给出建议。

0.2.3 增加已校准的导流采集与物理移动，并修复普通敌人死亡后建议停止更新的
问题。保留月巫族自然开局与 `Boss_Level8_AllyBuffer` 终局 Boss 支持；敌方导流
物理移动、未知 Boss 变体、复杂房间月相回调仍会停止建议。导流验证使用原生
遗物和人工安排的携带单位，不代表所有自然牌组组合均已验证。

## 安装原生 macOS Steam 版

1. 退出游戏，解压 `PojuAdvisor-0.2.3-macOS.zip`。
2. 打开终端，进入解压后的目录。先检查（不写入游戏目录）：

   ```bash
   bash install.command --check-only
   ```

3. 检查通过后安装：

   ```bash
   bash install.command
   ```

   默认位置是 `~/Library/Application Support/Steam/steamapps/common/Monster Train 2`。
   若 Steam 游戏位于外置盘，将目录或 `.app` 作为参数传入，例如：

   ```bash
   bash install.command --check-only "/Volumes/Games/steamapps/common/Monster Train 2"
   bash install.command "/Volumes/Games/steamapps/common/Monster Train 2"
   ```

4. 安装器会打印 Steam 启动选项，将整行复制到游戏的「属性 → 通用 → 启动选项」。
   从 Steam 启动游戏，进入一场战斗。面板应显示「怪物火车 · 战局建议」，随后
   发布下一步建议。右上角的按钮可显示/隐藏面板，拖动面板标题可调整位置。
   也可用 `F8` 显示/隐藏，Mac 键盘通常需要 `Fn+F8`。

包内提供官方 BepInEx 5.4.23.5 macOS universal 运行时；没有安装时才解压，
保留已有 BepInEx 和配置。已安装 BepInEx 的用户需确认是 **5.x Mono**。
环境检查会确认 `.app`、处理器架构、Mono 程序集、包内文件校验值和已有
BepInEx 的必要文件，同时检测误装的开发探针；检查通过
不代表实机加载成功。若游戏为 IL2CPP，安装器会停止，此包不能直接适用。
不需要安装 .NET SDK 或 Trainworks Reloaded。

## 设置和卸载

首次成功加载后，配置位于：

`BepInEx/config/netcan.monstertrain2.advisor.cfg`

- `Enabled = false`：停用建议。
- `ToggleKey = F8`：面板按键，可改成游戏和系统未占用的键。
- `PauseWhenHidden = true`：隐藏面板时取消后台计算；重新显示后按最新局面计算。
- `Horizon = 3`：规划未来回合数，范围 2–16。
- `TimeLimitMs = 3000`：每次后台搜索预算，范围 100–30000 毫秒。
- `PreferredFloor = 3`：优先部署层，1 是底层。
- `TacticalSeed = false`：额外战术候选，启用可能更慢。

配置改完后重启游戏。搜寻复杂时会超过预算少许，以完成当前不可分割的展开或
复验。默认隐藏面板会暂停计算，也可点击「暂停建议」手动暂停。若配置
`PauseWhenHidden = false`，隐藏时会继续计算。

终局 Boss 的完整计划验收使用 `Horizon = 12`、`TimeLimitMs = 30000`、
`PreferredFloor = 3`、`TacticalSeed = true`。这一复杂战斗中发布建议还需约
13–16 秒复验，不能把 30 秒搜索预算理解为总响应时间。默认的 3 回合/3 秒
设置仍适用于较短规划；上述终局配置尚不代表每步重新规划都能稳定保留胜局。

卸载时只需移走 `BepInEx/plugins/PojuAdvisor`，并清除本安装器打印的 Steam
启动选项。更新前的整个助手目录和启动器保存在 `BepInEx/PojuBackups`；
助手替换失败时自动恢复旧版，保留配置和原目录中的其他文件。存档与游戏自身
保存方式不作改动。开发用 `Probe.dll` 不应与玩家版一起安装。

## 无面板或无建议

查看 `BepInEx/LogOutput.log`：`POJU-READY` 表示助手已加载，`POJU-ADVICE`
表示已发布建议。`ADVISOR-CAPTURE-FAIL`、`ADVISOR-FAIL` 或程序集加载错误
可以帮助定位游戏版本/机制兼容性。如果连日志都没有，先检查 Steam 启动选项
及 BepInEx 加载。Apple 芯片上加载器仍可能遇到上游架构问题，请反馈原始日志。

若通过 CrossOver / Whisky 玩 **Windows 版**，需要瓶内 Windows BepInEx 5，
不能使用本包的 macOS 启动器和 `.dylib`。同一组 `BepInEx/plugins/PojuAdvisor`
DLL 可复制进 Windows 游戏目录。该路径也尚未进行 Mac 实机验收。

## 开发构建

在具有游戏 Mono 程序集和 BepInEx 5 的开发机上运行：

```powershell
pwsh -NoProfile -File scripts/Build-AdvisorMod.ps1
```

可用 `-GameDir` 指定开发机游戏目录，`-GameManagedDir` 指定 Mac 的 Managed
目录。发行包只携带助手 DLL 和官方运行时，不包含游戏程序集、存档或探针入口。
脚本输出 ZIP、SHA256 和构建来源。安装器不执行重新签名或系统安全设置变更。

安装依据：[BepInEx 官方发行版](https://github.com/BepInEx/BepInEx/releases/tag/v5.4.23.5)、
[Steam 启动说明](https://docs.bepinex.dev/articles/advanced/steam_interop.html)、
[MT2 社区 Mod 安装说明](https://github.com/Monster-Train-2-Modding-Group/Trainworks-Reloaded/wiki/Installing-Mods)。
