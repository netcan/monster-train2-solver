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

2026-11-21 在 `c810073e` 的八场阶段重放再次全部通过。190 次出牌与 52 次 EndTurn，
累计 512.413 秒。该次包含回合顺序、开局容量、金币法强及 Showtime 的新模型，报告为
`results/solver-stage-20261121-122608-d4022791/report.tsv`。

2026-11-21 在 `5f0771b5` 的八场阶段重放全部通过，累计 511.384 秒。
190 次出牌和 52 次 EndTurn 的完整后继、各场 16 条分支、录制候选、
串并行搜索与计划复放一致。报告为
`results/solver-stage-20261121-132614-cb678c21/report.tsv`。

2026-11-21 Thik T3 接入后的 `6cf408ab` 上，固定八场全部通过。累计 559.358 秒，
190 次出牌和 52 次 EndTurn，分支、录制候选与串并行复放一致。报告为
`results/solver-stage-20261121-143609-31b5c527/report.tsv`。

2026-11-21 Heavenfall 修复后的 `3881f41a` 上，固定八场全部通过。累计 543.500 秒，
190 次出牌和 52 次 EndTurn。报告为 `results/solver-stage-20261121-153614-848ee65e/report.tsv`。

同日加入留存授予、满手复制、满月法强及新版衰亡目标后，固定八场再次全部通过。
累计 629.362 秒，190 次出牌和 52 次 EndTurn。检查器与模型哈希见
`results/solver-stage-20261121-161817-ed77694b/report.tsv`。

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

清单要求八个不同的归档路径和哈希。检查器逐项核对归档内的场景与布置标记，
例如将继续的归档传给放弃项会在搜索前失败。
结果逐项写入 `results/solver-stage-<timestamp>-<id>/report.tsv`，包含通过／失败、
耗时、具体日志及归档／检查器／模型哈希。单项失败后继续运行余项，最后汇总并
返回失败，便于形成有限的阻塞清单；`-Case` 子集不能计作八项整体通过。
`-ModelChecker <ModelChecks.dll>` 配合 `-SkipBuild` 可验隔离构建，运行中不替换
正在被测的二进制。报告和日志不加入 Git，二进制原生证据继续由清单引用。

报告包装器另用原生 Relentless 放弃胜局定向复验通过；损坏检查器的两个故障
输入均记录失败，第二项照常执行，最后正确返回失败，包含原生命令错误偏好
开启的情况。原始八项执行时尚未添加报告的二进制哈希列，最终包装器单项报告
保留完整哈希；标准与隔离构建检查器／模型字节相同，无需为增加报告列重跑八项。

## 已录制战斗批量复放

`pwsh -NoProfile -File scripts/Check-Game231RecordedChains.ps1 -SkipBuild` 从各版本清单读取
当前版本已结算的整局归档，校验文件哈希及四个采集计数，再单次复放录制的出牌与
EndTurn。`-Archive` 可指定清单内的相对路径。报告保留首处分歧及归档、检查器和模型
哈希。八场阶段验收另有 16 条分支、合法候选和串并行搜索检查。

`-Workers 4` 可并行复放独立归档，同一场战斗内部仍依输入顺序执行。在同一检查器和模型的
155 场对照中，1 个工作线程用时 224.4 秒，4 个用时 102.1 秒。报告按清单顺序输出，
除计时列外逐行一致。对照报告位于 `results/recorded-chain-parallel-20261122/`。
复放遇到序列化后完全相同的状态时直接通过，存在编码或状态差异时沿用逐字段检查。
155 场串行对照的结果和首处分歧一致。

对指定的归档加 `-CheckCardFrontiers`，可在同一份报告中检查每个录制出牌点的候选，
同时校验实际出牌对应的后继。缺少状态导致的候选拒绝会报告决策编号和首个动作。
日常可用 `-Archive` 选出受影响的战斗，按需执行这项检查。
`-CheckEndTurnFrontiers` 对录制的结束回合执行同样的检查，包含 Relentless 选择，
达到枚举预算或遇到未支持后继时报告失败。无出牌基线以及 Relentless 的继续和放弃
三份原版记录均通过，合计 21 个结束回合决策点。

2026-11-21 检查 168 项登记，复放 106 场，合计 1,014 次出牌与 584 次 EndTurn，
累计 72.471 秒。92 场连续通过，14 场在已确认的测试布置前后分别通过，零失败。
这 14 场包括两场友军触发伤害和 12 场 LifeLink。工具保留原始首处分歧，根据归档中的
注入敌方编号或布置前后快照确认第一回合边界，再分别验证部署及后续完整输入。
同一归档换成未知路径的故障控制仍然失败，后续两个用例继续执行。报告在
`results/recorded-chain-triage-segments-current-20261121/report.tsv`。

同一 `3881f41a` 模型复放 116 场，覆盖 1,072 次出牌和 634 次 EndTurn，累计约 70 秒。
102 场连续通过，14 场在已确认的布置边界前后分段通过。报告为
`results/current-replay-2f07e3a9/report.tsv`。随后登记的基础冠军胜局另用同一构建
验证出牌、回合结束、合法候选和串并行搜索。

当前版本后续批量复放通过 122 场，107 场连续、15 场分段，覆盖 1,168 次出牌和 672 次
EndTurn，累计约 103 秒。新增分段为满月法强，核对六处场景插牌前后的原版状态。
报告为 `results/current-replay-ac9a6716/report.tsv`。衰亡新版目标归档随后登记，已通过
完整链、分支、合法候选和串并行计划的独立验证。

串行搜索节点预算修正后，固定八场全部通过，共 190 次出牌及 52 次 EndTurn，
用时 649.217 秒。报告为 `results/solver-stage-20261121-175043-123014a3/report.tsv`。
Fleshweaver 出生 Mass 修正后，批量复放 127 场，112 场连续、15 场分段，零失败。
报告为 `results/current-mass-replay.tsv`。

两处敌方附魔与部队光环顺序修正后，从 214 项登记中复放 144 场。
129 场连续通过，15 场分段通过，覆盖 1,690 次出牌和 838 次 EndTurn，零失败。
报告为 `results/recorded-chain-triage-20261121-203042-4f746b87/report.tsv`。
较早一次固定阶段验收八场均通过，报告为
`results/solver-stage-20261121-200236-e6b5c31c/report.tsv`。

收入第六环延长路线及两场 Relentless 原生按钮复放后，复放通过 147 场。
132 场连续、15 场分段，覆盖 1,744 次出牌或技能与 863 次 EndTurn，零失败。
报告为 `results/recorded-chain-triage-20261121-205812-0db2fbfb/report.tsv`。
附魔修正后的固定阶段验收八项均通过，报告为
`results/solver-stage-20261121-204107-2c6612d1/report.tsv`。

收入 Mollis 与生命循环的原版胜局后，复放通过 148 场。
133 场连续、15 场分段，覆盖 1,757 次出牌或技能及 868 次 EndTurn，零失败。
报告为 `results/recorded-chain-triage-20261121-212314-d73a6feb/report.tsv`。

原版 Inferno 开场挂件胜局已纳入。复放通过 149 场，134 场连续、15 场分段，
共 1,777 次出牌或技能及 874 次 EndTurn，零失败。报告为
`results/recorded-chain-triage-20261121-214400-f64111b3/report.tsv`。
同一模型源码的八项固定验收也全部通过，报告为
`results/solver-stage-20261121-214802-288fb822/report.tsv`。

Soul Savior 第四环的 Spurned ArmorPerDebuff 变体使用原版 TestScenario 加载。
原版战败归档有 32 次出牌、7 次 EndTurn、67 个房间阶段和 14 个列车阶段，
四项采集计数均为零。整战复放同时验证原版角色的出生、选层、Relentless
转相及到达薪火层。原版攻击挂载与 Witherbloom 邻位伤害另有独立组件归档。
入口为 `ModelChecks --native-spurned-battle <archive.mt2f>`，归档位于
`tests/compatibility/game-2.3.1/mechanics/spurned/`。
纳入该归档后的整战回归通过 150 场，135 场连续、15 场分段，零失败。
报告为 `results/recorded-chain-triage-20261121-223320-56a50f84/report.tsv`。

WitherbloomOnAction 变体使用另一原版 TestScenario。原版进入 Relentless 时移除两条
固有触发。随后由装备获得的触发继续按装备来源处理。常规路线有 32 次出牌和
7 次 EndTurn，败局激活击杀镜头并清空牌堆。第二条原版路线的前 20 次出牌相同，
从第 4 回合起直接结束回合。该败局未激活击杀镜头，保留抽牌堆 3 张、弃牌堆 10 张、
消耗堆 12 张和 Standby 的 10 个空槽。两条路线的完整后继、随机流、16 条分支、
候选枚举与串并行方案复放均通过，四项采集计数为零。

纳入两条 WitherbloomOnAction 路线后，2.3.1 已登记的整战回归通过 152 场，
137 场连续、15 场分段，共 1,861 次出牌或技能及 895 次 EndTurn，零失败。
报告为 `results/recorded-chain-triage-20261121-230244-38d5934b/report.tsv`。

DualismAt50 变体使用第三个原版 TestScenario。独立组件对照了 601、600、599/1200
生命时的原版回调和目标。高于半血登记 0 层 Malaise，等于和低于半血施加 1 层
Duality。单次效果测试推进一次 BattleTest，阈值效果实际施加时两条随机流保持原值。
首轮整战还录到出牌之间的预演随机流漂移。归档整战明确启用既有的房间预演和界面
随机流隔离，完整复放 32 次出牌、7 次 EndTurn、65 个战斗阶段、14 个列车阶段，
四项采集计数为零。16 条独立分支、录制根的一步候选及串并行方案复放通过。
组件和整战分别为 `duality-health-threshold.mt2f` 和 `duality-battle.mt2f`。
纳入此整战后，登记回归通过 153 场，138 场连续、15 场分段，共 1,893 次出牌或技能
及 902 次 EndTurn，零失败。报告为
`results/recorded-chain-triage-20261121-spurned-duality/report.tsv`。

第 3 环 Fleshweaver Spikes 使用原版战前检查点和泰坦试炼开关。Boss 受击后的原版出生回调
确认，试炼遗物把召出的尖刺小怪替换为试炼版本。顾问采集保留效果的原版召唤定义身份和
实际出生单位。专项回归逐项通过 30 次出牌、9 次 EndTurn、76 个战斗阶段及 18 个列车阶段，
四项采集计数为零，原版终局判负。组件和整战归档位于
`tests/compatibility/game-2.3.1/mechanics/fleshweaver-titan-trial/`。复放报告为
`results/fleshweaver-spike-trial/recorded-chain-registered.tsv`。

另两种 Fleshweaver 试炼使用同一战前检查点分别启用原版 Corruption 和 More Mass 遗物。
短采样验证召唤前后的定义身份、有序遗物效果和放置模板。正式整战中 Corruption 对照通过
26 次出牌及 8 次 EndTurn，More Mass 对照通过 29 次出牌及 9 次 EndTurn。独立复放覆盖
71 和 74 个战斗阶段、双方完整的录制动作候选、各 16 条分支和串并行方案复放。
原版 Corruption 交战记录中，试炼小怪由 5 血降到死亡，击杀它的 Train Steward 从 0
变为 2 层 Corruption。独立复放匹配了击杀和触发双方的单位 ID。
两场原版均判负，四项采集计数均为零。整战归档与 Spikes 位于同一个目录。

纳入第 3 环尖刺试炼及第三种第 6 环奖励路线后，登记回归通过 155 场，140 场连续、
15 场分段，共 1,963 次出牌或技能及 919 次 EndTurn，零失败。报告为
`results/recorded-chain-triage-20261122-trial-stage/report.tsv`。

加入 Corruption 和 More Mass 后，四工作线程批量回归通过 157 场，142 场连续、15 场分段，
共 2,018 次出牌或技能及 936 次 EndTurn，零失败。报告为
`results/recorded-chain-triage-20261122-003750-21698e7f/report.tsv`。

第 6 环零盟约 Cael 护盾泰坦试炼使用原版战前存档及试炼开关。二进制短测分别验证
初始波次 Feed 和外层 Cael 的原版替换，独立复放完整出生和放置。整战记录确认
9 个波次替换体、14 次进食和 26 次双护盾升级。37 次出牌、9 次 EndTurn、94 个战斗阶段、
18 个列车阶段及 37 个录制出牌根的候选均通过，四项采集计数均为零。16 条独立分支、
串并行搜索及计划复放也通过。原版结局为败局。测试采用现有局内顾问的查询随机流规则，
并采集原版月相掩码用于推移候选。归档位于
`tests/compatibility/game-2.3.1/mechanics/cael-shield-titan-trial/`。

加入第 6 环的 70 步续局和 Cael 护盾试炼后，四工作线程批量回归通过 159 场，
144 场连续、15 场分段，共 2,114 次出牌或技能及 956 次 EndTurn，零失败。报告为
`results/recorded-chain-triage-20261122-013803-877730ac/report.tsv`。

Cael 零盟约试炼的攻击与生命、吸血变体分别通过 26 次出牌和 6 次 EndTurn、17 次出牌和
4 次 EndTurn。原版对照分别覆盖 5 和 3 个波次替换体、8 和 5 次进食、13 和 8 次试炼升级。
两场的录制出牌候选、各 16 条独立分支、串并行搜索与计划复放均通过。原版结局均为败局，
四项采集计数均为零。两个整战与对应出生侧档分别位于 `cael-atk-hp-titan-trial/` 和
`cael-lifesteal-titan-trial/`。加入两场后，四工作线程批量回归通过 161 场，其中 146 场
连续、15 场分段，合计 2,157 次出牌或技能、966 次 EndTurn，零失败。报告为
`results/recorded-chain-triage-20261122-020219-dcf98cab/report.tsv`。

契约 2 的 Cael 护盾试炼出生已独立通过。2.3.1 隔离实例载入 2.2.1 的原版五胜存档，
显式选择原版 Cael 护盾场景与试炼事件。原版外层 Cael 计入契约的 700 点生命加成，
波次 Feed 保持 60 点生命。替换、放置后的完整战斗状态与随机流均匹配。
归档在 `tests/compatibility/game-2.3.1/mechanics/cael-shield-rank-two-titan-trial/`，
使用 `--native-cael-shield-rank-two-trial substitution.mt2f birth.mt2f` 复核。
同一路线的整战补录通过 26 次出牌、6 次 EndTurn、59 个房间阶段及 12 个列车阶段。
首次整战发现，未死亡的 Feed 被进食后，模型提前刷新了预演光环。修复后四处差异消失，
五个波次替换体、九次进食、十五次试炼升级均有原版记录。原版终局判负，四项采集计数均为零。
录制出牌候选、16 条独立分支、串并行搜索及计划复放均通过。整战使用 `battle.mt2f` 和
`--native-cael-shield-rank-two-trial-battle` 复核。
六个录制回合根也通过独立结束回合候选检查。纳入这一场后，批量回归通过 162 场，147 场
连续及 15 场分段，合计 2,183 次出牌或技能、972 次 EndTurn，零失败。报告为
`results/recorded-chain-triage-20261122-023728-b5f56383/report.tsv`。

2.3.1 载入原有 2.2.1 契约 10 第三环战前存档。Arkion 火炉路线用两线程、每次搜索十秒，
完成 36 次出牌和 9 次 EndTurn，以 22 点薪火获胜。全新原版进程固定复放同样的 45 个输入，
完整前后状态、预测和随机流逐项相同。两场均正常退出，四项采集计数均为零。
归档为 `migrated-covenant10-arkion-pyre.mt2f` 和 `migrated-covenant10-arkion-pyre-frozen.mt2f`。
该路线从 45 个实战局面枚举了 602 个出牌或技能候选和 45 个结束回合后继，零未支持、零截断。
加入两场归档后，批量复放通过 166 场，150 场连续、16 场分段，共 2,303 次出牌或技能
及 1,001 次结束回合，零失败。报告为 `results/recorded-chain-triage-20261122-042812-67ad8f8d/report.tsv`。

契约 10 第四环的原版对照确认，Shardtail 部署后同层小鬼费用 1→0，另一层和非小鬼对照
保持原费用。实际打出小鬼支付 0 余烬，支付前后战斗随机流吻合。
后续实战在第 15 个输入发现 HotHead 技能触发 Avarice 时遗漏 3 金币。匹配激活单位和原版
技能缓存卡后，`--observed-successors-only` 回放已采集的 15 个输入与四个列车阶段均通过。
两份对照归档分别位于 `mechanics/shardtail-imp-cost/` 和 `mechanics/avarice-unit-skill/`。

同一存档第 27 个输入结束第五回合。原版 Harvester 出场时先给楼上的四个敌人增加一层连击，
随后把它们移到底层。顾问复用本次回调已更新的楼层状态，并按原版顺序先登记房间附加触发器，
再登记契约触发器。`--observed-successors-only` 核对了归档内 27 个原版输入的完整后继和随机流，
以及 12 个列车结算阶段。归档为 `mechanics/covenant-harvester-spawn/ring4-observed.mt2f`，
记录结束于这次输入，第四环胜负需由整场复放验证。

新一轮实机经过上述出场结算，在第 33 个输入观察到炼金助手死亡使龙之宝藏由 0 增至 1。
原版和模型的战斗上下文均结算为 1，回合结果现在也读取该值。使用 `--observed-successors-only`
核对 `mechanics/end-turn-dragon-hoard/ring4-observed.mt2f`，33 个原版输入和 16 个列车阶段通过。

使用 `-ResumeRollingAdvice` 从同一战前档固定复放这 33 个输入，期间顾问未启动搜索。
原版结算与采集一致后，顾问从当前状态生成新建议，继续完成第 34 至 37 步。第四环实际结果为败局，
四项采集计数均为零。`--rolling-advice-only` 独立复核了 28 次出牌或技能、9 次结束回合及终局。
整场归档为 `mechanics/player-advisor-rolling/migrated-covenant10-ring4-defeat-resumed.mt2f`。
另一原版进程固定复放全部 37 步并原版结算败局。`--rolling-prefix-resume-pair-only` 逐项匹配
输入、起点、完整后继、预测与随机流。第二份归档为 `migrated-covenant10-ring4-defeat-frozen.mt2f`。

2.3.1 的进食组件对照使用两次原版结算。隔离实例用原版接口发放测试用牌，再通过出牌
与 EndTurn 触发结算。两次进食均将 Morsel Miner 的来源牌移入 Eaten，Primordium
存活并减少一层 Eatmany。普通进食在房间结算前保留空位，Feast 结算后填补空位。
两条完整后继各通过 16 条独立分支。使用 `--native-source-backed-feeding` 复核
`tests/compatibility/game-2.3.1/mechanics/original-source-backed-feeding/source-backed-feeding.mt2f`。
原版录制入口为 `Run-FullBattleProbe.ps1 -OriginalSourceBackedFeeding`。
此专项修复后的批量回归仍是 162 场通过，报告为
`results/recorded-chain-triage-20261122-024257-c2d5e98a/report.tsv`。

用 2.3.1 载入 2.2.1 原版第三环战前存档，隔离顾问在两线程、单次搜索三秒的配置下
完成 24 次出牌和 9 次 EndTurn，以 80 点薪火生命获胜。进程正常退出，四项采集计数为零。
同一归档另通过 33 次滚动建议与预测复放、完整链及 16 条独立分支。
独立原生进程从同一存档复放固定方案，仍以 80 点薪火生命获胜。两场的 33 个输入、
完整前后状态、预测和随机流逐项一致。两份归档位于
`tests/compatibility/game-2.3.1/mechanics/player-advisor-rolling/`，用
`--rolling-replay-pair-only migrated-ring3.mt2f migrated-ring3-frozen.mt2f` 核对。

另用 2.3.1 原生前两场胜利后的未修改存档测试第三环，隔离顾问完成 24 次出牌、
7 次 EndTurn，以 76 点薪火生命获胜。进程正常退出，四项采集计数为零。
`current-ring3.mt2f` 另通过完整链、16 条独立分支和 31 次滚动建议预测复放。
独立战役进程复放固定方案后写入第四环原版战前存档。两份归档的 31 个输入、完整
状态、预测和随机流逐项一致。复放归档为 `current-ring3-frozen.mt2f`。

从该战役第四环战前存档继续，顾问记录了 37 次出牌和 9 次 EndTurn，以 78 点
薪火生命获胜。四项采集计数为零，完整链、16 条独立分支和 46 次滚动建议预测复放
均通过。独立战役进程从同一存档执行固定方案，46 个输入、完整状态、预测与随机流
逐项一致，并写入第五环原版战前存档。归档为 `current-ring4.mt2f` 和
`current-ring4-frozen.mt2f`。

同一原版战役的第二环也通过顾问搜索和独立冻结复放。两次均以 80 点薪火生命获胜，
26 次出牌、7 次 EndTurn 的完整状态、预测和随机流逐项一致。归档为
`current-ring2.mt2f` 和 `current-ring2-frozen.mt2f`。

第五环原版存档种子为 `20261115`。顾问记录了 45 次出牌和 9 次 EndTurn，以
19 点薪火生命获胜。进程正常退出，四项采集计数为零，完整链、16 条独立分支和
54 次滚动建议预测复放通过。独立战役进程逐项匹配 54 个输入、完整状态、预测与
随机流，写入第六环原版战前存档。归档为 `current-ring5.mt2f` 和
`current-ring5-frozen.mt2f`。

从相同早期决策和随机状态重搜第五环最后一回合，前 49 个输入保持一致，后续四次
出牌采用另一条路线。独立原版进程复放全部 54 步后，以 51 点薪火生命获胜，
比已归档的建议多保留 32 点。四项采集计数为零，完整链和 16 条独立分支通过。
归档为 `current-ring5-refined.mt2f`。

使用保留 51 点薪火生命的路线写入第六环原版战前存档后，顾问在第六环完成 35 次
出牌、8 次 EndTurn，原版判负。四项采集计数为零，完整链、16 条独立分支及 43 次
滚动建议预测复放通过。归档 `current-ring6-defeat.mt2f` 记录该预算下的败局。

同一检查点把单次搜索预算从 3 秒提高到 10 秒，原版仍在回合 7 判负。
四项采集计数均为零，43 个决定中四次重新展开搜索，另外 39 次使用已验证的后续方案。
从该原版轨迹第一、二回合的起点分别离线重搜后，两次得到同一条 69 步完整预测。
这条路线在回合 9 开始时剩 13 点薪火生命，回合结束时判负。完整状态和随机流通过
离线复放，路线仍待原版复放。输出位于 `results/ring6-retained-check-203140/`。

从回合 8 的模型后继再搜得到 63 步路线。原版独立进程从同一未修改检查点复放了
52 次出牌或技能、11 次 EndTurn，回合 10 的结算仍判负。四项采集计数均为零，
共 112 个房间阶段、22 个列车阶段。完整后继、16 条独立分支、全部固定预测与
随机流逐项匹配。52 个录制的出牌或技能起点上，候选枚举均无未支持项。
原版归档为 `current-ring6-turn10-defeat.mt2f`。

同一第五环 51 血胜局另由原版战役选择 Shield of Wyngh 和 Soulsmelter，经过 59 项
奖励及地图事件到达独立的第六环战前检查点。10 秒搜索预算的首个原版测试在回合 7
判负。35 次出牌或技能、8 次 EndTurn、16 条独立分支、滚动预测及四项采集计数通过。
2026-11-23 从已有隔离录档登记，当前模型连续复放及两类候选通过。43 个录制局面共枚举
1,074 个出牌或技能候选与 43 个结束回合后继，零未支持、零截断。
[`current-ring6-shield-wyngh-soulsmelter-defeat.mt2f`](../tests/compatibility/game-2.3.1/mechanics/player-advisor-rolling/current-ring6-shield-wyngh-soulsmelter-defeat.mt2f)

同一第五环胜局按原生卡牌选择领取 PunkrockReveler 和 DragonsBreath，复放 54 个固定
输入后以 51 点薪火生命获胜并抵达第六环战前检查点。第六环沿用两线程、单次搜索 10 秒，
在第 7 回合结束时原版判负。40 次出牌或技能、8 次 EndTurn 和全部 48 次滚动预测复放，
四项采集计数均为零。48 个录制决策根枚举 1,201 个出牌或技能候选及 48 个 EndTurn
后继，零未支持、零截断。归档为 `current-ring6-third-rewards-defeat.mt2f`。

第三种奖励路线从同一原版检查点离线重搜，从第 27 个输入改为顶层部署盾卫。独立原版实例
固定复放了 59 次出牌或技能及 11 次 EndTurn，完整预测、实际后继和随机流逐项一致。
结算第 10 回合时判负，终态回合计数为 11，Cael 剩余 881/1800 生命。四项采集计数均为零。
登记复放及 70 个录制决策根均通过，枚举 1,591 个出牌或技能候选和 70 个 EndTurn 后继，
零未支持、零截断。原版归档为 `current-ring6-third-rewards-frozen-turn11-defeat.mt2f`。

在 Shield of Wyngh 和 Soulsmelter 路线的 70 步离线计划中交换早期三个动作，共检查 143 个候选，
其中 77 个为三步组合，零未支持。新路线从第 1 回合第 6 个输入开始变化，
完整状态和随机流通过独立离线复放。仍在第 11 回合判负，Cael 剩余 934/1800
生命，比前一条离线路线少 2 点。新动作序列尚无原版复放。
结果位于 `results/ring6-shield-early-triples-independent/`。

离线 `plan-root` 另记录束搜索实际展开的状态数。第六环两个局面分别用 3 秒和
15 秒对照，默认四次均在束搜索前达到预算。预留一半时间及节点后各展开
2、13、2、9 个状态，其中三次可复放预测变短。八份预测的状态和随机流均通过
独立复放。计数器已纳入工具，预留预算的试验保留在隔离工作树。
日志位于 `results/ring6-beam-reservation-compare/`。

另一组双线程离线对照让辅助线程在首次完成一回合后进入束搜索。第 6 环试炼的
第 33 个局面从零次增至三次束展开，两个输出仍是相同的 23 步、四个完整回合和
28 点薪火。搜索耗时为 10.14 和 10.88 秒。Arkion 第 40 个局面的两组结果均以
22 点薪火获胜，动作、完整状态和随机流逐项相同。两例各运行一次，实验保留在
隔离分支。证据位于 `results/first-tactical-turn-beam-evidence/`。

两条第六环路线的全部 106 个滚动决策根均通过候选诊断，共枚举 2,648 个出牌或
技能候选与 106 个 EndTurn 后继。录制的实际输入、完整后继及随机流逐项匹配，
零未支持、零截断。入口为 `ModelChecks --rolling-frontier-only <archive.mt2f>`，
仅枚举录制根的一步候选。该入口也接受带 Relentless 对话的归档，并在选择预算耗尽
时报告截断。

另一份 2.2.1 原版六胜存档在 2.3.1 启动第七环，完成 21 次出牌和 8 次 EndTurn，
原版判负。四项采集计数为零，完整链、16 条独立分支和 29 次预测复放通过。
归档为 `migrated-ring7-defeat.mt2f`。

另一份 2.2.1 原版七胜存档在 2.3.1 完成第八环的 25 次出牌和 7 次 EndTurn，
原版判负。四项采集计数为零，完整链、16 条独立分支和全部 32 次预测复放通过。
归档为 `migrated-ring8-defeat.mt2f`。

带 Judge’s Scales 的另一份原版第八环存档在部署时弹出弃牌选择。隔离驱动通过
原生完成按钮确认零弃牌，两次均保留同序的 10 张手牌和抽牌、战斗随机流。搜索与
独立冻结复放各有 24 次出牌、8 次 EndTurn，32 个输入、完整状态、预测与随机流
逐项一致，两场原版均判负。归档为 `migrated-ring8-judges-scales.mt2f` 和
`migrated-ring8-judges-scales-frozen.mt2f`。

第七、八环的四份既有滚动归档共通过 125 个录制决策根的候选检查。模型枚举了 2,437 个
出牌或技能候选和 125 个 EndTurn 后继，未出现未支持或截断，原版实际输入的后继及随机流
均匹配。其中两份 Judge’s Scales 归档来自同一条路线的独立原版运行。

从同一 Judge’s Scales 存档的第 17 个决策开始离线重搜，第 18 个实际输入首次分叉，
出了一张牌，将结束回合延后。新路线以 42 次出牌或技能和 9 次 EndTurn 完成原版败局。
`verify-offline-native` 核对了冻结方案与实机输入，`--rolling-advice-only` 独立复算了
51 个原版实际后继、随机流及每步预测。四项采集计数为零。归档为
`migrated-ring8-judges-scales-alternate-frozen.mt2f`。
纳入后，批量回归通过 163 场，148 场连续及 15 场分段，合计 2,225 次出牌或技能、
981 次 EndTurn，零失败。报告为 `results/recorded-chain-triage-20261122-030100-608385d0/report.tsv`。
在这条新路线的第 20 个录制局面，候选检查暴露了两条涉及部队升级与附魔来源顺序的预演分支。
实际执行的 51 步均匹配。新路线的一步枚举共得到 927 个受支持的出牌或技能候选，
51 个结束回合后继，另有上述两条未支持候选，零截断。
可在 `--rolling-frontier-only <archive> 64` 后附加从零开始的决策索引。指定 `19`
只检查该局面并输出两个具体输入，省去复跑其他 50 个录制根。
两条候选分别用实机复放同一段 19 步前缀。第 20 步的 Wracking Pain 和 Fight Club 均由原版接受，
复用原有部队光环和预演清理路径后，模型逐项对齐实际前后状态、升级顺序和随机流。
复测 51 个录制局面的 929 个出牌或技能候选、51 个 EndTurn 后继，未支持和截断均为零。
分支归档在 `mechanics/troop-aura-enchantment/`，由 `--third-troop-meat-shield-preview-only` 复核。

新版优先遗物清单 25 件均已有整局在场、效果列表有序匹配。最后五件在同一场
隔离胜局取得，使用原版卡与遗物定义，三张防守卡实例增加测试耐久。原版出牌令
锻造点 0→2→0，七次出牌、五次 EndTurn、完整链、16 条分支及合法候选匹配。
128 节点串并行计划与复放一致，搜索达到节点与回合预算。归档为
`tests/compatibility/game-2.3.1/mechanics/priority-relic-battle/battle.mt2f`，
录制入口为 `Run-FullBattleProbe.ps1 -OriginalPriorityRelicBattle -BinaryCapture`。

2026-11-22 的 2.3.1 整场复放验收通过 183 场，167 场连续通过，16 场分段通过。
合计 2,741 次出牌或技能、1,132 次 EndTurn，零失败，另有 100 份证据档跳过整场检查。
报告为 `results/recorded-chain-triage-20261122-121026-5a641be0/report.tsv`。
第 6 环试炼另对 53 个录制局面枚举了 961 个出牌或技能候选和 53 个结束回合，零未支持、零截断。

加入 Shelter 腐化倍率、复制牌月相升级、四件状态强化遗物的混合月相和新增机制采集后，
同日再跑阶段回归，183 场全部通过，167 场连续、16 场分段，另有 110 份证据档跳过整场检查。
报告为 `results/recorded-chain-triage-20261122-142913-219f9224/report.tsv`。

达到 200 场归档后执行阶段回归，184 场连续、16 场分段，零失败。共复放 3,063 次
出牌或技能与 1,238 次结束回合，四个工作线程耗时约 123 秒。另有 110 份组件或诊断档
按清单跳过整场复放。报告为 `results/recorded-chain-triage-20261122-162023-b6f7f0c8/report.tsv`。

达到 220 场归档后执行阶段回归，204 场连续、16 场分段，零失败。共复放 3,501 次
出牌或技能与 1,345 次结束回合，四个工作线程耗时约 125 秒，跳过 110 份组件或诊断档。
报告为 `results/recorded-chain-triage-20261122-180735-ff3f0428/report.tsv`。

达到 240 场归档后，阶段检查额外枚举了录制决策的出牌和结束回合候选。221 场连续通过，
17 场分段通过。另两场录制链通过，候选检查分别发现旧档缺少移动前月相掩码和红冠的装备池。
共检查 3,822 次出牌或技能、1,453 次结束回合，跳过 112 份组件或诊断档。四个工作线程与实机
采样并行时，耗时约 21 分钟。后续用定向复验处理这两场和新改动。
报告为 `results/recorded-chain-triage-20261122-195541-b6ad0cc0/report.tsv`。

两条路线补采后均通过完整后继和录制候选检查。装备与死亡路线记录 23 次出牌、5 次结束回合，
第 5 环左路红冠补采记录 22 次出牌、9 次结束回合。旧录像仍提供原来的实际输入与结算证据。
[装备与死亡补采](../tests/compatibility/game-2.3.1/mechanics/controlled-original-trigger-relics/equipment-and-death-with-moon.mt2f)
[第 5 环红冠复放](../tests/compatibility/game-2.3.1/mechanics/player-advisor-rolling/unlocked-covenant0-ring5-left-red-crown.mt2f)

达到 270 场归档后执行阶段复放，253 场连续通过，17 场分段通过，零失败。
共复放 4,248 次出牌或技能、1,617 次结束回合，跳过 113 份组件或诊断档。
四个工作线程耗时约 137 秒。新增出牌和结束回合候选按受影响场次定向检查。
报告为 `results/recorded-chain-triage-20261122-220339-7dcc4c41/report.tsv`。

达到 285 场归档后，阶段复放 267 场连续通过、18 场分段通过，零失败。
共复放 4,417 次出牌或技能、1,699 次结束回合，跳过 117 份组件或诊断档。
四个工作线程耗时约 150 秒。新增候选继续按受影响场次定向检查。
报告为 `results/recorded-chain-triage-20261122-230529-a0617a94/report.tsv`。

达到 300 场归档后，阶段复放 281 场连续通过、19 场分段通过，零失败。
共复放 4,613 次出牌或技能、1,782 次结束回合，跳过 117 份组件或诊断档。
四个工作线程耗时约 145 秒。新增两类候选按受影响场次定向检查。
报告为 `results/recorded-chain-triage-20261122-234313-4379ffc4/report.tsv`。

2026-11-23，模型在持有卡牌集合相同时复用不可变战斗统计。第 6 环原版快照的固定 32 节点
.NET 离线对照中，开局分配量下降 19% 至 20%，后期下降 27% 至 30%。单线程和双线程的
18 次运行中，同配置的动作、评分、随机流和结算状态逐项一致。原版右路与召回归档的完整回放、
出牌及结束回合候选也已定向通过。游戏内耗时尚待测量。
离线基准为 `results/ring6-owned-statistics/fixed-budget.log`。

同日用更新后的顾问运行一场 2.3.1 隔离首战。21 次出牌、7 次结束回合、30 次建议发布，
原版以 76 点薪火生命获胜。面板的楼层浏览、候选保留、手牌变化、重置及预算切换均通过。
独立复放逐项核对 28 步，出牌及结束回合候选也通过。
[`current-ring1-presentation-after-allocation.mt2f`](../tests/compatibility/game-2.3.1/mechanics/player-advisor-rolling/current-ring1-presentation-after-allocation.mt2f)

随后将八条高频的状态更新改为复用已冻结的卡牌集合。相对上一版分配优化，固定 32 节点下，
开局进一步减少约 8% 分配量，后期减少约 10% 至 12%。18 次对照的动作、评分、完整状态和随机流
一致。首战面板、第 6 环右路以及 Colonel 与远层光环三场的两类候选均通过。
离线基准为 `results/ring6-context-scalar/fixed-budget-allocators.log`。

同一构建定向补查了第 5 环的三条原版战役路线，以及第 6 环第 10 回合败局。共核对
183 个实机录制局面、3,683 个出牌或技能候选及 183 个结束回合后继，未支持和截断均为零。
[`第 6 环第 10 回合败局`](../tests/compatibility/game-2.3.1/mechanics/player-advisor-rolling/current-ring6-turn10-defeat.mt2f)

原版第 6 环三个录制局面中，按不可变卡牌实例缓存卡牌自身的校验结果，将固定 32 节点
.NET 离线搜索的分配量再降 37% 至 42%。单线程和双线程共六组，每组各两次，动作、评分、
完整状态和随机流均匹配。房间相关的合法性判断继续按各自局面执行。
第 6 环右路、留存授予、原版装备与合成、原版接枝四场通过定向复放和两类候选检查。
镜厅接枝及弃牌前随机流的单机制检查也通过。离线对照在
`results/card-validation-cache/fixed-budget.log`，四场报告在
`results/recorded-chain-triage-20261123-020425-2789eb8a/report.tsv`。

319 场归档的阶段复放中，299 场连续通过，19 场按已验证的准备边界分段通过，另有
118 份组件或诊断档按清单跳过。第 5 环左路红冠暴露一处结算后自动切层。原版
Gravity 把一个敌人移动到中层，随即选中中层并清除底层临时导魔。候选检查和连续复放
现在从模型后继复放该录制切层，并比较同一完整状态。红冠和第 6 环左路定向复验通过，
合计 81 次出牌或技能、20 次结束回合和两类录制候选检查。
阶段报告为 `results/recorded-chain-triage-20261123-022849-68aee405/report.tsv`，
修正后的定向报告为 `results/recorded-chain-triage-20261123-023432-15ea7113/report.tsv`。

终局施法清理与技能攻击补位修复后，2.3.1 的阶段复放通过 335 场。316 场连续通过，
19 场在已验证的准备边界分段通过。共匹配 5,121 次出牌或技能和 1,988 次结束回合，
失败为零，118 份组件或诊断档按清单跳过。阶段运行核对录制动作的连续后继与终局，
新增录像的两类候选由各自的登记检查覆盖。
报告为 `results/recorded-chain-triage-20261123-042910-b6c4d6e1/report.tsv`。

本轮阶段复放通过 358 场，其中 339 场连续，19 场按已验证的准备边界分段。
共匹配 5,509 次出牌或技能和 2,127 次结束回合，失败为零，118 份组件或诊断档按清单跳过。
同一模型的技能与楼层生命光环定向搜索通过，两场的候选、串并行结果及计划独立复放一致。
报告分别为 `results/recorded-chain-triage-20261123-061255-37443f78/report.tsv` 与
`results/solver-stage-20261123-061101-488f05c8/report.tsv`。

2.3.1 的下一次阶段复放核对 375 场，其中 356 场连续通过，19 场在已验证的准备边界分段通过。
共匹配 5,771 次出牌或技能和 2,229 次结束回合，失败为零，119 份组件或诊断档按清单跳过。
此阶段新增归档逐场通过出牌和结束回合候选检查。报告为
`results/recorded-chain-triage-20261123-073523-2b5237d6/report.tsv`。

隔离 2.3.1 原版中的双线程顾问在搜索中完成 7 次楼层切换，额外搜索次数为零。
该胜局的 26 次建议输入及 449 条出牌候选和 26 条结束回合候选均通过独立复放。
[原版录像](../tests/compatibility/game-2.3.1/mechanics/player-advisor-rolling/current-two-worker-floor-browse.mt2f)

375 场阶段复放后新增 27 场。按 Git 差异选中这 27 场，逐场回放并枚举两类候选，全部通过。
这批共核对 220 次出牌或技能和 147 次结束回合。报告为
`results/recorded-chain-triage-20261123-084814-478a1818/report.tsv`。

2.3.1 的 402 场已结算录像中，591/601 张收藏卡有结算定义匹配的出牌记录，
168/236 件收藏遗物有在场且有序效果列表匹配的记录。版本变更优先清单达到 52/52 张卡
和 25/25 件遗物。组合覆盖与触发效果分别以机制差分和阶段报告为准。

2.3.1 的 601 张可出牌收藏卡均取得定义匹配的原版出牌记录。最后七场新增录像的定向复放
通过，共 56 次出牌或技能及 35 次结束回合，并核对了两类候选。随后整批复放通过 409 场，
其中 390 场连续、19 场按已验证的准备边界分段。共核对 6,047 次出牌或技能和 2,411 次
结束回合，119 份组件或诊断档按清单跳过，失败为零。四工作线程耗时约 157 秒。
报告分别为 `results/recorded-chain-triage-20261123-090831-3848db24/report.tsv` 和
`results/recorded-chain-triage-20261123-090902-4ba67528/report.tsv`。

同批录像中的遗物在场证据为 168/236 件，版本变更优先项为 25/25 件。卡牌统计衡量实际
出牌和定义的一致性，组合、技能和遗物触发仍按各自的实机证据与差分检查验收。

开局被动遗物模式复用原版取得入口，让六件遗物一起进入首战。完整胜局的 22 次操作、
6 次结束回合及两类候选通过。新增三件整局在场证据，遗物累计 171/236 件。
同一入口的组件模式另行确认六件遗物的八次原版修正回调，仍只需采集到部署阶段。
[原版整局](../tests/compatibility/game-2.3.1/mechanics/opening-passive-relics/native-battle.mt2f)

验收后新增五场只跑定向回归。121 次操作、32 次结束回合及两类候选全部通过。
其中两个正常战前存档在隔离 2.3.1 中由双线程顾问驱动，第 1 环胜局复放 22 次建议，
第 6 环败局复放 62 次建议到原版终局。414 场归档的遗物整局在场证据达到 179/236。
报告为 `results/recorded-chain-triage-20261123-093044-fa984afb/report.tsv`。

另一份第 6 环原版存档的 48 次顾问输入均复放至败局结算。楼层重排回调现在保留全列车的
移动记录。三组首张法术复制和一组未出牌费用变化分别在隔离原版执行备选输入，复放核对了完整
后继及 RNG。该整战通过 953 个出牌候选和 48 个结束回合候选，未支持与截断均为零。
[原版整局](../tests/compatibility/game-2.3.1/mechanics/player-advisor-rolling/native-moon-remnant-sixth-battle-defeat.mt2f)

以 409 场的阶段提交为基线，11 场新增整局在同一构建上完成定向复放和两类候选检查，
累计 177 次操作与 68 次结束回合。420 场已结算归档覆盖了 186/236 件遗物的在场阵容。
定向结果为 `results/recorded-chain-triage-20261123-105302-92e1186c/report.tsv`。

2.3.1 的 601 张可玩收藏卡均有实际出牌及定义匹配记录，236 件收藏遗物均有在场及有序效果
匹配记录。最后一批的六份战役组件在同一隔离进程中采集，全部通过新版及旧版专项对照。
收藏遗物的在场统计仍以整战快照为依据，各项实际触发以对应的原版差分为依据。

阶段复放通过 435 场，包含 416 场连续复放、19 场按已验证准备边界分段复放。共核对
6,521 次出牌或技能、2,577 次结束回合，另有 129 份组件或诊断档按清单跳过。固定求解器
八项全部通过，每项均检查录制候选、串并行一致性、计划复放及独立分支。结果为
`results/recorded-chain-triage-20261123-121554-67b89461/report.tsv` 和
`results/solver-stage-20261123-121856-598a5349/report.tsv`。

阶段复放之后新增一场 2.3.1 Windows 发布版双线程顾问胜局。21 次出牌、7 次结束回合及两类
候选检查通过，切换楼层 7 次，未新增搜索。该发行包的三个托管 DLL 还通过了独立的 Windows
界面启动检查，使用了复制的玩家配置和隔离存档。macOS 的加载和实战尚待目标机器验证。
[发布版顾问整局](../tests/compatibility/game-2.3.1/mechanics/player-advisor-rolling/current-stage-two-worker-presentation.mt2f)

定向复查的 16 场后期战斗通过 627 次出牌或技能、141 次结束回合及两类候选检查。
报告为 `results/recorded-chain-triage-20261123-122444-e64c21b5/report.tsv`。

第 9 环的发布版实测在隔离原版中直接进入最终首领。首次结束回合时，五个首领部位现在保留
原版满月掩码。两个原版进程独立完成 10 次出牌和 6 次结束回合并结算为败局，核对了实际后继、
顾问预测、随机流和结束回合候选。随后在三个原版分支中核对 Valor、Pyregel 与 Sap，修正友军
变化后的 LifeLink 与预演校验。同一场的 528 个出牌候选和 16 个结束回合候选全部通过。
[顾问运行](../tests/compatibility/game-2.3.1/mechanics/player-advisor-rolling/current-ring9-original-true-final-boss.mt2f) ·
[独立复放](../tests/compatibility/game-2.3.1/mechanics/player-advisor-rolling/current-ring9-original-true-final-boss-frozen.mt2f)

三个备选出牌后均在隔离原版继续到终局，分别执行 10、10 和 15 个输入。随后用两工作线程和
3 秒单次搜索预算运行发布版顾问，原版败局的 15 次出牌、6 次结束回合和两类候选全部通过。
六份新增第 9 环整战归档通过同一构建的定向检查。总计 442 场已结算归档。旧版 2.2.1 的
17 个实际后继仍通过，旧版完整建议复放在一个重复攻击触发标记上有已有差异。
[双线程实战](../tests/compatibility/game-2.3.1/mechanics/player-advisor-rolling/current-ring9-two-worker-advisor.mt2f)
· [定向报告](../results/recorded-chain-triage-20261123-131627-e746f3d0/report.tsv)

第 9 环阶段验收后执行一次全量整战复放。442 场全部通过，423 场连续复放，19 场按已验证的
准备边界分段复放。共核对 6,594 次出牌或技能与 2,620 次结束回合，共 132 份组件或诊断档
按清单跳过。卡牌和遗物的原版记录分别保持 601/601 和 236/236。
[阶段复放报告](../results/recorded-chain-triage-20261123-131730-28290707/report.tsv)

第 3 环沿原版战斗前 35 个输入继续搜索。8 秒配置下，隔离原版双线程顾问找到
并执行了剩余七步，以 37 点薪火获胜。独立冻结路线也在原版以 37 点获胜。两场均
核对了 33 次出牌、9 次结束回合、完整后继和随机流。采用新的终局分支顺序后，同一
快照在离线 24 节点回归中找到五步、37 点薪火的胜局。原版 3 秒配置复测仍以 35 点
获胜，首次重新求解耗时 3.38 秒，建议端到端耗时 4.68 秒。
[独立原版复放](../tests/compatibility/game-2.3.1/mechanics/player-advisor-rolling/current-ring3-endgame-pyre37.mt2f) ·
[8 秒配置顾问实战](../tests/compatibility/game-2.3.1/mechanics/player-advisor-rolling/current-ring3-endgame-parallel-resumed.mt2f)

第 9 环第 5 回合的一次双线程搜索在 3 秒配置下耗时 6.70 秒。分线程计时确认固定策略
在时限后仍模拟多批候选。将时限检查接入该策略后，同一原版快照的搜索耗时为 3.36 秒，
端到端耗时从 7.64 秒降至 4.26 秒，实际模拟的候选从 140 个降到 73 个。复测执行 15 次
出牌和 6 次结束回合，原版败局已结算。逐项重放、随机流和两类候选检查均通过。
[实战归档](../tests/compatibility/game-2.3.1/mechanics/player-advisor-rolling/current-ring9-advisor-deadline.mt2f)

三份新增整战的定向回归共核对 81 次出牌、24 次结束回合及两类候选。2.3.1 的已结算
归档达到 445 场，卡牌定义匹配的出牌记录为 601/601，遗物有序效果匹配的在场记录为
236/236。第 3 环后续优化将离线找到 37 点路线所需的回合结算从五次减至三次，3 秒配置
原版复测仍以 35 点获胜，42 个输入的实际后继及随机流逐项匹配。
[定向报告](../results/recorded-chain-triage-20261123-144302-936253aa/report.tsv)

`scripts/Check-SolverCompletion.ps1 -SkipBuild` 使用已有构建运行求解器专项，包括多回合深搜、
预算中断、独立计划重放，并刷新当前卡牌和遗物证据。日常录制回归仍可用
`Check-Game231RecordedChains.ps1 -ChangedFrom <revision> -SkipBuild` 选择新增或变更的归档。

2.3.1 发布版顾问从 Relentless 首个选择前生成整战建议。隔离原版执行六次建议的结束回合，
期间按顺序选择九次继续、一次放弃，以 53 点薪火获胜。独立重放逐项核对原版后继和随机流，
整场一次出牌、七次结束回合及两类候选均通过。启动器按顾问给出的回合、楼层、交战轮数和
选择核对整场记录，并拒绝被篡改的一条选择。第二个隔离原版进程直接复放冻结方案，从相同
起点执行十次选择，再次以 53 点薪火获胜。配对检查通过完整后继、终局和随机流。
[原版顾问归档](../tests/compatibility/game-2.3.1/mechanics/relentless-advisor/mixed-policy.mt2f) ·
[独立原版复放](../tests/compatibility/game-2.3.1/mechanics/relentless-advisor/mixed-policy-replay.mt2f)

本轮阶段回归通过 447 场已结算整战，其中 428 场连续复放，19 场按已验证的准备边界分段。
共核对 6,677 次出牌或技能和 2,658 次结束回合。136 份组件或诊断档由各自的专项检查核对。
[阶段复放报告](../results/recorded-chain-triage-20261123-173528-ffa0815d/report.tsv)

第 3 环复用同一份战前档和前 35 个原版输入。终局线程和技能候选在截止后停止安排新模拟，
3 秒配置下首次搜索由 4.33 秒降到 3.28 秒，建议端到端由 5.96 秒降到 4.04 秒。
两场都保留 35 点薪火，已有 37 点的原版路线仍是限时搜索的对照。
[修复前日志](../.probe-runs/full-battle-units-spells-and-junk-20261123-162527-04eaf1a4/unity-scenario.log) ·
[修复后日志](../.probe-runs/full-battle-units-spells-and-junk-20261123-163106-b79665b6/unity-scenario.log)

终局搜索优先尝试能保住前排的治疗后，同一战前档在 3 秒配置下找到五步原版胜路，
以 37 点薪火结算。首次搜索耗时 3.31 秒，端到端 3.96 秒。40 个原版输入及随机流
通过独立复放，其中 35 个来自固定前缀，随后五步来自顾问。
[原版复放归档](../tests/compatibility/game-2.3.1/mechanics/player-advisor-rolling/current-ring3-endgame-three-second.mt2f)

状态键编码复用 UTF-8 缓冲区，并直接写入 SHA-256 摘要。以相同第 7 环快照做七组交替离线对照，
20 次计算新状态键的中位时间从 466.6 毫秒降至 308.8 毫秒，中位托管分配从 87.6 MB 降至
74.2 MB。三种状态键模式均与独立编码器一致。2.3.1 Windows 原版随后执行双线程顾问的
28 步胜局，21 次出牌、7 次结束回合及两类候选检查均通过。
[原版归档](../tests/compatibility/game-2.3.1/mechanics/player-advisor-rolling/current-state-key-buffered-advisor.mt2f)

隔离的 2.3.1 Windows 发布版顾问在部署建议显示后，通过原版出牌入口打出另一张合法单位牌。
旧建议立即失效，顾问额外搜索一次并发布 27 步新方案。新建议起点与独立采集的原版状态及
随机流一致。专项从该起点复放新方案的全部步骤，并重算终局与评分。
[偏离建议专项](../tests/compatibility/game-2.3.1/mechanics/player-advisor-rolling/current-player-advisor-off-route.mt2f)

旧方案的匹配只复放到实机快照所在回合。同一原版开局再次偏离建议，匹配旧方案从 4.62 秒降至
0.68 秒，随后分别耗时 1.18 和 1.10 秒执行重新搜索。两次都发布了经独立复放验证的 27 步新方案。
快速连续出牌、跨回合与换楼层的复用专项均通过。原版在两次采样之间分配的预览编号只在比较这两个采样时归一化。
[限回合复放后的原版复测](../tests/compatibility/game-2.3.1/mechanics/player-advisor-rolling/current-player-advisor-off-route-bounded.mt2f)

第 8 环从同一份原版战前存档开战，先复放 33 个录制输入。恢复后的第一条建议施放 Broken Memories，
在原版消耗堆界面显示「选择 虫裔道钉」，并用原版确认操作选中第 25 号实例。发布版顾问继续到败局结算。
31 次出牌、7 次结束回合、完整状态、随机流和两类候选均通过独立复放。同名提示另有离线测试及手牌界面对照。
[第 8 环原版选牌整局](../tests/compatibility/game-2.3.1/mechanics/player-advisor-rolling/current-ring8-visible-deck-choice.mt2f)

同一份第 8 环战前存档也在 2.3.1 上复放了旧版的 48 步胜路。当前模型从原版部署快照重新生成
全部后继，独立原版进程按原输入复放，最终保住 1 点薪火。39 次出牌、9 次结束回合、完整状态、
随机流和两类候选均通过。该胜局使用冻结输入，3 秒顾问的记录仍为败局。
[第 8 环原版 1 点薪火胜局](../tests/compatibility/game-2.3.1/mechanics/player-advisor-rolling/current-ring8-historic-one-pyre-route.mt2f)

同一第 8 环结局局面中，第二个搜索线程对普通伤害法术多尝试一次异层首领目标。离线 3 秒、96 节点
配置从此前两回合的部分方案改为找到 11 步胜路。随后从原版前 39 个输入恢复 3 秒顾问，找到并执行
最后 11 步，以 5 点薪火结算。首次搜索耗时 3.21 秒，端到端 5.86 秒。41 次出牌、9 次结束回合及两类
候选检查全部通过。另两个第 8 环局面及第 3 环 37 点薪火对照仍返回相同的操作序列和完整终态。
[第 8 环 3 秒顾问实战](../tests/compatibility/game-2.3.1/mechanics/player-advisor-rolling/current-ring8-endgame-boss-target-three-second.mt2f)

本阶段复放通过 454 场 2.3.1 已结算战斗，其中 435 场连续复放，19 场按已验证的准备边界分段。
共核对 6,886 次出牌或技能和 2,711 次结束回合，失败为零。另有 141 份组件或诊断归档。两类候选检查
在新增整战各自登记时执行。 [阶段复放报告](../results/recorded-chain-triage-20261123-195536-7cfe4e68/report.tsv)

11 月 24 日的阶段回放通过 462 场已结算战斗，含 443 场连续回放和 19 场分段回放。
核对了 7,072 次出牌或技能和 2,767 次结束回合，失败为零。登记后补跑的 SniperAdvance 和
Dutiful Burst 两场各自通过完整连续回放、16 条独立分支和两类候选检查。
[阶段复放报告](../results/recorded-chain-triage-20261124-000240-52465205/report.tsv) ·
[SniperAdvance](../tests/compatibility/game-2.3.1/mechanics/unruly-sniper/native-battle.mt2f) ·
[Dutiful Burst](../tests/compatibility/game-2.3.1/mechanics/dutiful-burst/native-battle.mt2f)

随后定向新增 Dutiful AscendDescend、Dutiful Armor 和 Lifemother Reanimate 三场原版整战。
三场共 68 次出牌或技能、13 次结束回合，均通过连续回放、16 条独立分支和两类候选。
加上前述两场定向复验，已有 467 场通过，合计 7,197 次出牌或技能和 2,791 次结束回合。
Soul Savior 原版遭遇已有 10／15 场整战证据。
[首领覆盖清单](../tests/fixtures/inventory/boss-coverage.md)

Lifemother Debuffs 追加 11 次出牌、4 次结束回合及两条实际付费的替代分支。
整战两类候选通过，累计已有 468 场、7,208 次出牌或技能及 2,795 次结束回合通过。
Soul Savior 原版遭遇已有 11／15 场整战证据。
[Debuffs 整战](../tests/compatibility/game-2.3.1/mechanics/lifemother-debuffs/native-battle.mt2f)

Indulgent Titanskin 追加 8 次出牌、4 次结束回合，两类候选通过。累计已有 469 场原版
战斗通过，合计 7,216 次出牌或技能和 2,799 次结束回合。Soul Savior 已有 12／15 场
整战证据。 [Titanskin 整战](../tests/compatibility/game-2.3.1/mechanics/indulgent-titanskin/native-battle.mt2f)
