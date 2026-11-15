# 固定版本求解器阶段验收

基线为游戏 2.3.1、build 26650011、MVID
`bf93f060-beda-46b1-b808-95ebf1e9a99d`。
`tests/compatibility/game-2.3.1/solver-stage.tsv` 固定八份已有原生胜局及其字节数／
SHA-256：原版受罚法术、单位、技能、Spite 装备、选择召唤、失血光环，以及
Relentless 放弃／继续两种策略。复用原生整局归档，不重新启动游戏录制。
它是第一批阶段输入，包含明确标记的隔离控制，不代表所有自然战局或全部内容。

2026-11-15 首次完整执行为 **8/8 通过**：190 次出牌、52 次 EndTurn、481 个
房间阶段及 98 个列车阶段匹配，录制动作完整候选前沿零未支持。每局的完整
连续后继／终局和 16 条不可变分支通过，根搜索返回计划及串并行结果一致。
原始报告为 `results/solver-stage-20261115-180736-10f9eb97/report.tsv`。
八项合计 565.753 秒；该次子进程限定两个逻辑 CPU，部分时间与重复召唤压力
同时运行，不是默认配置的独占性能基准。没有运行所有归档或新版全部内容。

每项先要求版本与终局已结算、原生胜局及四项失败计数为零，再检查：

- 完整上下文引用、全部录制房间和列车阶段。
- 从原生初始状态开始连续执行出牌和 EndTurn，逐项比较原生后继和终局；
  同一起点另做 16 个不可变分支。
- 每个录制出牌点展开完整合法候选，原生实际动作必须存在，缓存后继必须与
  原生实际结果一致，候选未支持计数必须为零。
- 一个完整根决策的串／并行搜索、返回计划独立复放、完整状态及父状态不变。

每项根搜索只展开一个决策，避免八份归档重复运行同一种深搜压力。重复召唤的
128 节点压力使用独立入口，不能把单步阶段通过说成任意深度通过，也不能把
节点预算截断的结果认证为最低战损／最优。阶段通过与整个战斗模拟完成分开。

```powershell
# 阶段运行全部八项
pwsh -NoProfile -File scripts/Check-Game231SolverStage.ps1
# 日常只重跑受影响的一项
pwsh -NoProfile -File scripts/Check-Game231SolverStage.ps1 -SkipBuild -Case summon-choice
# 新选择搜索的快速检查仍用机制自己的入口
pwsh -NoProfile -File scripts/Check-Game231SummonChoiceBattle.ps1 -SkipBuild -SelectionSearchOnly
# 深层重复召唤另验；不依赖新游戏进程
pwsh -NoProfile -File scripts/Check-Game231SummonChoiceBattle.ps1 -SkipBuild -RepeatedSearchOnly
```

结果逐项写入 `results/solver-stage-<timestamp>-<id>/report.tsv`，包含通过／失败、
耗时、具体日志及被测检查器／模型哈希。单项失败后继续运行余项，最后汇总并
返回失败，便于形成有限的阻塞清单；`-Case` 子集不能计作八项整体通过。
`-ModelChecker <ModelChecks.dll>` 配合 `-SkipBuild` 可验隔离构建，运行中不替换
正在被测的二进制。报告和日志不加入 Git，二进制原生证据继续由清单引用。

报告包装器另用原生 Relentless 放弃胜局定向复验通过；损坏检查器的两个故障
输入均记录失败，第二项照常执行，最后正确返回失败，包含原生命令错误偏好
开启的情况。原始八项执行时尚未添加报告的二进制哈希列，最终包装器单项报告
保留完整哈希；标准与隔离构建检查器／模型字节相同，无需为增加报告列重跑八项。
