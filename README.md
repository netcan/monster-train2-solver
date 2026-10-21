# 怪物火车 2 · 破局助手

在游戏内显示下一步出牌/技能、目标、楼层和预计火炉血量。后台进行有限回合
搜索，操作后自动更新建议。默认使用已读取的牌序和随机状态，未保证全局最优。
面板可用鼠标显示/隐藏和拖动，默认隐藏时暂停后台计算。

玩家 Mod 入口位于 `src/Advisor`，开发用隔离探针位于 `src/Probe`。
两者分别构建；玩家版不安装探针的存档重定向、自动对局和校准补丁。

[Mac 安装与使用](docs/macOS.md)：提供中文面板、BepInEx 5 macOS universal
运行时、环境检查、文件校验与安装脚本。**Mac 实机加载尚未验证，当前为试用版。**

开发机具有游戏程序集和 BepInEx 5 时：

```powershell
pwsh -NoProfile -File scripts/Build-AdvisorMod.ps1
pwsh -NoProfile -File scripts/Check-AdvisorPackage.ps1 -Package <输出的ZIP路径>
```

构建产物位于 `results/mod-dist`，不包含游戏程序集、资源和存档。
运行 Mod 无需单独安装 .NET SDK 或 Trainworks Reloaded。

本次构建、游戏内冒烟和未验证项见 [Mod 验证记录](docs/mod-validation.md)。

模拟器和求解器的原生校准证据与边界见 [SIMULATION.md](SIMULATION.md)。
卡牌和遗物的证据覆盖不等于任意组合均可求解。
