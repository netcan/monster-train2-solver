# 怪物火车 2 · 破局助手

在游戏内显示本回合剩余的出牌/技能、目标、楼层及结束回合步骤、预计薪火血量和损失，并可展开完整方案。
Windows 玩家版默认以整场战斗结束为搜索目标，最多两个低优先级后台线程共用
节点与时间预算；每条路线默认最多256步，预算不足时明确显示部分方案。
找到薪火血量达到可证明上界的完整胜局时会[提前结束搜索](docs/early-stopping.md)。
按已验证胜局操作可跨回合复用后续步骤；局面偏离时重新搜索。
默认使用已读取的牌序和随机状态，未保证全局最优。
部分方案明确显示预测窗口，窗口内零损不代表整场零损；技能标出来源楼层和费用。
最近的建议与已接受操作有[容量受限的后台记录](docs/advice-forecast-history.md)，便于核对战损变化。
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
[剩余验收目标](docs/remaining-goals.md) 区分玩家版已验证行为和待完成的自然对局支持。
