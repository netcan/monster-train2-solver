# 随机组合整局验收

`scripts/Run-RandomCampaign.ps1` 从原生第一战开始，执行实际玩家建议，
每场获胜后在独立游戏进程冻结复放全部输入，再经原生奖励、商店和地图进入下一战。
只有游戏自身 EndGame 给出八战全胜的 `Standard` 结算、完整档案打包和恢复校验
通过，才记为整局通关。模型预测胜利和单场获胜均不算整局通关。

使用发布版默认配置：整场搜索、两个后台线程、3000ms、256 步上限、上层优先，
额外战术种子关闭。3000ms 是搜索软预算；每场原生进程另有超时。
你的 Windows 配置目前为 10000ms，本验收不修改该配置。

```powershell
pwsh -NoProfile -File scripts/Run-RandomCampaign.ps1 `
  -PrimaryClan Banished -SecondaryClan Umbra -Seed 1970936330 -NativeWorker 2
```

开始前顺序构建 Probe、Advisor、ModelChecks；已经构建时可加 `-SkipBuild`。
指定 `-InputProfile .probe-runs/<checkpoint>` 可继续同一种子、难度和组合的
未完成隔离检查点。失败记录和各战接受记录写入独立的
`.probe-runs/random-campaign-*/campaign-result.json` 与 `accepted-rings.jsonl`。
失败保留具体战次和输入，不将后续战次记为通过。胜局的模型检查还要求源局和复放
完整初始局面、每步后继、预测及 RNG 相同；复放必须零搜索。

采样范围为已支持的十一种基础／旧版氏族，排除需要单独 DLC 可用性检查的 Railforged。
只在隔离档案解锁被抽中的氏族及旧族所需 Crew，原生起始牌组、冠军、遗物、
首领生成和后续奖励均不改写。因此这是这些氏族的隔离自然普通通关验收，
不代表档案自然解锁，也不代表真终局或高 Covenant 保证获胜。

2026-10-25 首批用 `.NET Random(20261025)` 依序抽取主族、不同副族、游戏种子，
按下列顺序运行 Covenant0。氏族池顺序为 Banished、Pyreborne、LunaCoven、
Underlegion、LazarusLeague、Hellhorned、Awoken、Stygian、Umbra、Remnant、Wurmkin。

| 样本 | 主族／副族 | 原生种子 | 本次状态 |
| --- | --- | ---: | --- |
| 1 | Banished / Umbra | 1970936330 | 前两战源局及独立复放通过，第三战源局胜；整局仍在验收 |
| 2 | Pyreborne / Banished | 1862997127 | 首战源局及独立复放通过；第二战发现原生商店 Permafrost 升级采集保护缺口，正在修复并从原检查点继续 |

两组首战各自的源局／独立复放已归档在 `tests/fixtures/campaign/random/`，
以 `full-battle-random-campaign-01-ring1-*` 与 `02-ring1-*` 命名。
定向选择任意一侧归档都会校验整对，重复选择不会重复执行。
首批同时确认错误种子、难度和交换的主／副族均被拒绝；未运行全量回归。

定向检查入口：

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll `
  --random-campaign-pair-only <search.mt2f> <replay.mt2f> <seed> <rank> <distance> <primary> <secondary>
```

完整败局使用 `--random-campaign-source-only <source.mt2f> <seed> <rank> <distance>
<primary> <secondary>`，仍检查原生逐步状态、预测、RNG 与面板证据。
机制不支持或不完整档案不能通过此入口冒充一个已验证的策略败局。
