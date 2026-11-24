# 按机制完成战斗模拟

当前目标仍是完成整个战斗模拟。验收基线固定为游戏 2.3.1、build 26650011、
MVID `bf93f060-beda-46b1-b808-95ebf1e9a99d`。后续游戏更新另做版本差异清单，
不在当前批次混入新增定义。下面是推进方式调整，不表示现有缺口已完成。

## 推进优先级与阶段收束

2026-11-15 重新核对调用链：`CardHoverPreviewModel.Apply` 当前只由
ModelChecks 调用，`BattleSearchModel`、实际出牌与回合切换没有调用它。
因此，不把独立鼠标悬停的全部效果组合设为求解器的前置条件。已经完成的预演
证据保留；只有实际执行／搜索依赖的临时状态路径，或真实反例证明会影响战斗
结果的预演问题，才进入当前主线。与出生、死亡等共用的管线仍按影响范围回归。
这调整工作顺序，不将尚未完成的预演、整局覆盖或整个模拟宣称为完成。

下一阶段的主线限定为固定版本中的实际出牌、回合切换与搜索后继：

1. 固定一批原版实战起点、种子和输入作为阶段验收集；先复用现有二进制归档，
   缺原生证据的输入才重新启动隔离实例。新版本内容留待独立迁移阶段。
2. 先复放和搜索该集合，收集实际未支持与完整状态差异，按同一源码机制合并，
   以阻塞场景数量和修复成本排序。目录无引用不能证明无入口，也不优先为它
   人工补一场战斗。源码已知会影响结果的缺陷仍进入主线。
3. 每组修复前正向阅读原生代码；参数分支尽量从目录生成一次批量对照，复用
   目标、伤害、升级和触发队列。纯查询函数可直接转写；依赖原生对象、管理器
   与协程的部分继续适配，不假定导入 DLL 就能得到独立、多线程的搜索状态。
4. 日常只跑新证据和受影响回归。单机制满足既定分支后即提交，未观察到反例的
   组合归入后续组合差分，不为每笔提交重新扩张验收矩阵或重复深搜。
5. 固定集合的实际后继／终局／RNG、返回计划复放和串并行隔离一致，未支持与
   差异清零后做阶段回归并结项。报告集合大小、通过／阻塞数和最主要阻塞原因；
   该阶段通过不等于整个游戏覆盖。后续阶段再扩集合和补齐源码分支审计。

原生预演仍只作对照，不能替代任意多回合分支的完整隔离状态。最终全版本完成
仍采用下列固定验收关口；阶段交付不再等待与搜索无调用关系的界面行为穷举。

第一批已固定为[八份原生整局的阶段验收](solver-stage.md)，使用归档哈希、逐项
结果与失败后继续的报告；支持日常只跑指定项，深层压力单独验收。

`scripts/Check-Game231RecordedChains.ps1` 对已登记、通过版本和结算检查的战斗各回放一次。
首次扫描 106 场，92 场通过，合计 61.923 秒。其余 14 场在下一条录制输入前出现差异。
两份友方触发伤害样本在部署后插入单位，十二份 LifeLink 样本在第一回合调试手牌或牌堆。
扫描报告保留这 14 处断点，专项回归从各自的场景准备完成点验证。
原始结果在 `results/recorded-chain-triage-current-20261121/report.tsv`。

2026-11-21 抽牌后召唤提交上的扫描覆盖 111 场，97 场连续通过，14 场分段通过，
耗时 68.966 秒。报告在 `results/recorded-chain-triage-20261121-145753-02292ccb/report.tsv`。

`scripts/Get-RelicCoverageInventory.ps1 -Current` 按 SHA-256 校验目录和已登记战斗。
111 场的 3,288 个出牌／回合前后快照中，2.3.1 的 236 件收藏遗物有 22 件在场且
与目录的有序效果列表一致，214 件暂无在场记录。报告列出归档和快照位置，证据范围
限于在场与效果列表。个别效果是否触发、触发参数和后继仍由机制检查验证。
输出为 `results/relic-observations-2.3.1.tsv`，可用 `-Current -Check -SkipBuild` 复核。

加 `-Worklist` 可同时生成 `results/relic-effect-worklist-2.3.1.tsv`。清单按原版效果类型顺序
归并待验证遗物，优先附上相同顺序的在场录像，否则给出共享效果类型及其归档、快照和遗物位置。
这些位置用于复用测试布置，待测遗物自己的参数和触发仍需原版对照。
同一命令还生成 `results/relic-priority-version-delta-2.3.1.tsv`，对照两份原版目录的
12 件新增和 13 件变化，并附当前战斗的在场证据。目录中的字段差异可直接定位复测范围。

2026-11-22 在 183 场已结算原版战斗上重生成内容证据。2,741 次出牌或技能中，
601 张符合统计条件的收藏卡有 132 张的结算定义匹配，27 张有身份记录，442 张
没有出牌记录。7,746 份决策快照中，236 件收藏遗物有 50 件的在场有序效果列表
与目录匹配。版本优先清单的 52 张卡和 25 件遗物均达到各自这项证据要求。
卡牌明细为 `results/card-evidence-2.3.1.tsv`，按 `Get-CardCoverageInventory.ps1 -Current`
重建。遗物明细仍由上面的遗物盘点生成。

复核同一批 183 场后，卡牌定义匹配修正为 145/601，另有 14 张留下出牌身份、442 张
暂无出牌记录。原版出生目录省略了两个未触发的生命周期字段，整战记录显式写入
`false`。仅归一这对默认值，两个字段任一个为 `true` 的反例仍拒绝匹配。
同一份 Cael 原版试炼复放核对了 26 次出牌、6 次回合结束和 16 条独立分支。
这项盘点修正复用了原有战斗数据。

随后对同批归档核实了两个静态触发模板，目录省略的计数许可字段与实战显式
`false` 一致。仅在这两个模板中归一该默认值，改变许可、触发次数或字段类型都
会拒绝匹配。另六张已有出牌证据通过，Arkion 原版胜局的 45 个输入通过独立复放。
随后在两条精确的 Funguy 备用召唤投影复用了同一组出生默认值，第 8 环原版
51 个输入通过复放。当前 153/601 张定义匹配、6 张已记录出牌身份、
442 张暂无出牌记录。

四件状态强化遗物共用一次约 33 秒的原版采样，覆盖 Rage、Spikes、Regen 和 Mageblade。
五次原版出牌、第一次战斗结算的完整后继与随机流逐项吻合。四件均有原版战斗回调，
逐个从查询输入移除时，对应加成归零。该短测从月相追踪关闭的原版局面开始，已登记为
[`status-magnitude/native-exchange.mt2f`](../tests/compatibility/game-2.3.1/mechanics/status-magnitude/native-exchange.mt2f)。

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --original-status-magnitude-witness-only tests/compatibility/game-2.3.1/mechanics/status-magnitude/native-exchange.mt2f
```

改用会开启月相的原版 Selene 后，四件遗物的加成在同一场战斗仍生效。重新采集
并严格复放五次出牌和一次普通结束回合，完整后继及随机流一致。Selene 的月相
掩码为 `5→6→5`，Sweep 随满月获得和移除。部署结束的完整状态和随机流也通过，
且上移阶段逐项一致。战斗中途记录仅在阶段字段及其四个房间副本看到 `Combat` 与
`MonsterTurnQueueClear` 的采样差异。普通回合和部署分开核对。
[`native-moon-verified.mt2f`](../tests/compatibility/game-2.3.1/mechanics/status-magnitude/native-moon-verified.mt2f)。
[`native-moon-deployment.mt2f`](../tests/compatibility/game-2.3.1/mechanics/status-magnitude/native-moon-deployment.mt2f)

五张原版房间状态牌共用一次约 37 秒的原版采样。Lava Flow、Gooey Flask、Echo Snare、
Jar of Frost 和 Magic Juice 合计支付 2 点余烬，分别在同房两名原版敌人身上触发正向
状态变化。五次施放和第一次战斗结算都核对了完整后继、随机流和消耗去向。
用 `ModelChecks --original-room-status-spells-only` 可在离线复查这份短测。
[`room-status-spells/native-exchange.mt2f`](../tests/compatibility/game-2.3.1/mechanics/room-status-spells/native-exchange.mt2f)

整战录制将五张原版牌放入首个决策前的牌堆，保留原版洗牌、抽牌和结算。
首回合完成五次施放后接回普通策略，约 86 秒后原版获胜，四项采集计数均为零。
独立复放通过 23 次出牌、7 次结束回合和 16 条分支，录制根的出牌与结束回合
候选检查也通过。纳入该整战后累计 184 场，卡牌定义匹配为 158/601。
[`room-status-spells/battle.mt2f`](../tests/compatibility/game-2.3.1/mechanics/room-status-spells/battle.mt2f)

第二组用同一原版牌堆准备方式录制了五张单体状态牌。Perils of Production、
Energy Siphon、Immortality Potion、Forevermore Serum 和 Spike Potion 在首回合
依次施放。约 71 秒后原版获胜。18 次出牌、6 次结束回合、16 条分支及录制根的
候选检查通过。累计 185 场整战，符合统计条件的 601 张收藏卡中，163 张的结算定义
匹配，6 张有出牌身份记录，432 张暂无出牌记录。
[`targeted-status-spells/battle.mt2f`](../tests/compatibility/game-2.3.1/mechanics/targeted-status-spells/battle.mt2f)

`Run-FullBattleProbe.ps1 -OriginalOpeningSpellIds <一至五个有序 GUID>` 复用同一原版牌堆
准备和战斗执行流程。第一组配置的五张牌在同一场战斗完成首回合施放，约 87 秒
后原版获胜。24 次出牌、7 次结束回合、16 条分支及录制根的候选检查通过。
累计 186 场整战，卡牌定义匹配为 168/601，另有 6 张有出牌身份记录、
427 张暂无出牌记录。
[`configured-opening-spells/battle.mt2f`](../tests/compatibility/game-2.3.1/mechanics/configured-opening-spells/battle.mt2f)

第二组配置新增三张牌的原版出牌见证，并验证了此前只有身份记录的 Immortal Trade
和 Liquid Courage。18 次出牌、6 次结束回合、16 条分支和候选检查通过。
[`ally-enemy-status-and-movement.mt2f`](../tests/compatibility/game-2.3.1/mechanics/configured-opening-spells/ally-enemy-status-and-movement.mt2f)

四件状态强化遗物也在单体状态牌整战中同时在场。约 66 秒后原版获胜，15 次出牌、
5 次结束回合、16 条分支及候选检查通过。原版记录正向 Rage 和 Spikes 回调，
四件遗物效果表与原版目录都一致。当前累计 188 场整战，卡牌结算定义匹配
173/601，另有 4 张出牌身份记录和 424 张暂无出牌记录。遗物整战在场证据为 54/236。
[`status-magnitude/battle.mt2f`](../tests/compatibility/game-2.3.1/mechanics/status-magnitude/battle.mt2f)

不满五张的配置保留原始牌库补齐首回合手牌。Wildwood Tome 与 Twin Shards，
以及 March of Shields 与 Twin Shards 的短牌组均完成原版胜局。两份归档合计
39 次出牌、14 次结束回合，完整复放和候选检查均通过。March of Shields 可选择
任一阵营，实测选中友方单位并施加护甲。累计 190 场整战，结算定义匹配为
175/601，另有 3 张出牌身份记录和 423 张暂无出牌记录。
[`configured-opening-spells/manifest.tsv`](../tests/compatibility/game-2.3.1/mechanics/configured-opening-spells/manifest.tsv)

Ritual of Battle 与 Cycle of Life 各用未附加额外强化的原版定义完成一场胜局。
Cycle of Life 的原版动作给友军增加 10 点当前和最大生命，再施加 8 层尖刺。
两场各有 16 次出牌、6 次结束回合，完整复放、16 条分支和录制根候选检查通过。
累计 192 场整战，结算定义匹配 177/601，另有 1 张出牌身份记录和 423 张暂无出牌记录。

原版 Spike of the Banished 的 X 费用和 Valor 增量已补齐。实机用正数余烬支付，
每点余烬增加 4 层 Valor。胜局的 19 次出牌、7 次结束回合、16 条分支与录制根
候选检查通过。累计 193 场整战，当前已有出牌记录的 178 张符合统计条件的收藏卡
均与原版结算定义匹配，423 张仍无出牌记录。
[`spike-of-the-banished.mt2f`](../tests/compatibility/game-2.3.1/mechanics/configured-opening-spells/spike-of-the-banished.mt2f)

Flash Freeze 与 Ice Spear 在同一原版胜局补齐。首回合验证直接伤害、护甲与
Frostbite，整局 21 次出牌、7 次结束回合、16 条分支和候选检查均通过。
累计 194 场整战，180/601 张收藏卡已有匹配的原版结算。
[`flash-freeze-and-ice-spear.mt2f`](../tests/compatibility/game-2.3.1/mechanics/configured-opening-spells/flash-freeze-and-ice-spear.mt2f)

`Run-FullBattleProbe.ps1 -OriginalOpeningUnitIds <一至四个有序 GUID>` 已在同一
原版胜局记录 Welder Helper、Steelworker、Follower 和 Votivary。四张留在
原有部署手牌之外，并由原版按合法楼层部署。22 次出牌、6 次结束回合、四项
采集计数、16 条分支及候选检查通过。累计 195 场整战和 184/601 张已匹配收藏卡。
[`armor-multistrike-endless.mt2f`](../tests/compatibility/game-2.3.1/mechanics/configured-opening-units/armor-multistrike-endless.mt2f)

Entombed Explosive、Overworked Assistant、Wickless Tycoon 和 Spikedriver Spider
在第二场原版胜局完成部署。首领击杀 Entombed Explosive 后，其死亡触发反杀首领。
模型在这个同步触发结束战斗时保留死亡单位的站位占用，与原版终局一致。新录制的
22 次出牌、6 次结束回合、四项采集计数、16 条分支与候选检查均通过。累计 196 场
整战和 188/601 张已匹配收藏卡。
[`death-damage-and-harvest.mt2f`](../tests/compatibility/game-2.3.1/mechanics/configured-opening-units/death-damage-and-harvest.mt2f)

第三场部署登记 Deathcapped Prophet、Embered Infery、Coldsting Siren 和 Forge Steward。
中层召唤触发使下次抽牌增量从 0 变为 1，付费后余烬从 1 升到 4，随后顺序部署顶层两张。
原版胜局的 25 次出牌、7 次结束回合、16 条分支与候选检查通过。累计 197 场整战和
192/601 张已匹配收藏卡。
[`rally-and-ember.mt2f`](../tests/compatibility/game-2.3.1/mechanics/configured-opening-units/rally-and-ember.mt2f)

从已保存的原版首战入口恢复，八张生成型法术在同一场受控原版胜局结算。探针在部署前
放置测试锚点与余烬控制，给待测实例临时加 Freeze，保留原版卡牌定义。整场 12 次出牌、
5 次结束回合、16 条独立分支与候选检查通过。累计 198 场整战和 200/601 张已匹配收藏卡。
[`generated-battle-cards.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-isolated-spells/generated-battle-cards.mt2f)

九件原版装备在另一场受控首战依次付费装到三层测试单位。同一可达定义遍历采集 Potion Kit
能力的四个原版 Mix 配方，装备前后的配方目录一致。原版胜局的 13 次出牌、5 次结束回合、
16 条分支和候选检查通过。累计 199 场整战和 209/601 张已匹配收藏卡。
[`native-equipment-and-mix-pool.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-isolated-equipment/native-equipment-and-mix-pool.mt2f)

按已有的原版首战前档恢复，Squad Leader 和 Twisted Assistant 完成原版部署，
Engulfed in Smoke 在另一场结算。各场从部署连续复放至胜利，分别为 19 次出牌与
6 次结束回合、17 次出牌与 6 次结束回合，两场的分支和候选检查均通过。
[`checkpoint-squad-leader-and-assistant.mt2f`](../tests/compatibility/game-2.3.1/mechanics/configured-opening-units/checkpoint-squad-leader-and-assistant.mt2f)
[`checkpoint-engulfed-in-smoke.mt2f`](../tests/compatibility/game-2.3.1/mechanics/configured-opening-spells/checkpoint-engulfed-in-smoke.mt2f)

原版支持法术批次复用首战前档与既有测试对照，录到治疗、过量治疗、余烬和抽牌、
腐化护甲、Hoard 火车治疗及正值与零值 X 费用容量。27 次出牌、5 次结束回合、
16 条分支和候选检查通过，为 2.3.1 增加 12 张卡牌的定义匹配记录。
[`resources-healing-and-capacity.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-isolated-spells/resources-healing-and-capacity.mt2f)

`-OriginalOpeningRelicIds` 在同一原版首战按指定顺序取得六件遗物。五件分别为五次友军
出生补上 Rage、Lifesteal、Burst、Armor、Avarice，另一件为七次敌军出生补上 Frostbite。
出生点前后的状态增量、遗物顺序、18 次出牌、6 次结束回合、分支和候选检查通过。
累计 203 场整战、224/601 张卡牌及 60/236 件遗物已有原版匹配见证。
[`spawn-statuses.mt2f`](../tests/compatibility/game-2.3.1/mechanics/configured-opening-relics/spawn-statuses.mt2f)

Chicot 在 2.3.1 原版首战补录了首领出生时获得 3 层 Muted。专项复放比较了 12 个出生阶段
与 32 条独立分支，完整胜局的 18 次出牌、6 次结束回合和两类候选检查也通过。
原生采集用 `-OriginalChicotRelic -BranchCardMasks -BinaryCapture`，包含 Devote 候选所需的手牌特性。
[`boss-mute-chicot.mt2f`](../tests/compatibility/game-2.3.1/mechanics/configured-opening-relics/boss-mute-chicot.mt2f)

EggHatchRemoval 的 2.3.1 受控胜局保留原版遗物定义，用两名配置了 Egg 子类型和 Hatch 3 的
测试单位验证原版回调顺序。两次出生均移除 Hatch 并增加 30 护甲。12 个出生阶段、32 条
独立分支、18 次出牌、6 次结束回合及两类候选复放通过。
[`egg-hatch-removal.mt2f`](../tests/compatibility/game-2.3.1/mechanics/configured-opening-relics/egg-hatch-removal.mt2f)

2.3.1 受控首战用一名配置了 Monstrosity 子类型的原版单位验证 Contingency Plans。
该单位出场增加 7 层 Spikes，另七个原版敌军出生均维持原层数。完整胜局的 1 次出牌、
7 次结束回合以及两类候选检查通过。
[`contingency-plans.mt2f`](../tests/compatibility/game-2.3.1/mechanics/configured-opening-relics/contingency-plans.mt2f)

PurgeToExhaust 的 2.3.1 胜局核对 23 次原版管理器调用和六组过滤对照。转换后的法术
实放进入耗尽堆，并保留原版耗尽状态。22 次出牌、7 次结束回合以及两类候选通过。
新版和旧版专项均通过，同一管理器检查器也通过新版 Permafrost 的定向复验。
[`purge-to-exhaust.mt2f`](../tests/compatibility/game-2.3.1/mechanics/configured-opening-relics/purge-to-exhaust.mt2f)

CopyFirstSpellPlayed 的两份 2.3.1 胜局分别覆盖同回合限制和法术直接击杀。致命施法触发终局清理后，
原版回调仍从登记表找到原牌，生成一张带 Ephemeral 的手牌。原牌随后进入弃牌堆，终局同时保留这两张牌。
两局共 46 次出牌和 13 次结束回合，完整状态、随机数与两类候选检查通过。
[`copy-first-spell.mt2f`](../tests/compatibility/game-2.3.1/mechanics/configured-opening-relics/copy-first-spell.mt2f) ·
[`copy-first-spell-terminal.mt2f`](../tests/compatibility/game-2.3.1/mechanics/configured-opening-relics/copy-first-spell-terminal.mt2f)

CopyFirstConsumePlayed 的 2.3.1 录像确认首张消耗法术产生一张永久副本，后续消耗受同场限制。
23 次出牌、7 次结束回合与两类候选检查通过。
[`copy-first-consume.mt2f`](../tests/compatibility/game-2.3.1/mechanics/configured-opening-relics/copy-first-consume.mt2f)

Corrupted Angels Artifact 与 RandomCardCostOnDraw 在两种获取顺序下均有原版实机录像。每种顺序匹配 53 条回调、
17 次自然触发与三层嵌套抽牌。成功的嵌套抽牌分别观察到 12 与 17 条弃牌时清除的费用修饰。
两局共 66 次出牌、10 次结束回合及两类候选检查通过。
[`corrupted-angels-cost-first.mt2f`](../tests/compatibility/game-2.3.1/mechanics/configured-opening-relics/corrupted-angels-cost-first.mt2f) ·
[`corrupted-angels-cost-last.mt2f`](../tests/compatibility/game-2.3.1/mechanics/configured-opening-relics/corrupted-angels-cost-last.mt2f)

MoreCapacity 在原版第三回合把中层容量从 5 提到 7，发出一次通知，随后四次检查均由回合条件拦截。
七个大小为 1 的单位成功部署，第八个被拒绝。8 次出牌、5 次结束回合与两类候选检查通过。
[`more-capacity.mt2f`](../tests/compatibility/game-2.3.1/mechanics/configured-opening-relics/more-capacity.mt2f)

第三张法术加法强与首张 Consume 法术抽牌在同一原版对局中验证。两版专项均匹配五次阈值触发、
一次实际抽牌、15 个非 Consume 对照和 65 次法术数值变化。2.3.1 的 26 次出牌、6 次结束回合与两类候选检查通过。
[`spell-play-thresholds.mt2f`](../tests/compatibility/game-2.3.1/mechanics/configured-opening-relics/spell-play-thresholds.mt2f)

ExtraSpellCastTrigger、ExtraSpawnTrigger 和 ExtraDeathTrigger 在同一原版对局中验证。
专项核对 32 个队伍阶段、160 次法术触发次数查询，以及各 31 次出生和死亡查询。
21 次出牌、7 次结束回合和两类候选检查通过。
[`incant-relics-combined.mt2f`](../tests/compatibility/game-2.3.1/mechanics/configured-opening-relics/incant-relics-combined.mt2f)

2ChargesOnCardConsumed 的原版对照核对有效消耗牌使选中楼层增加 1 点充能，其他楼层保持原值。
普通牌对照与消耗牌实际入堆均匹配。5 次出牌、7 次结束回合及两类候选检查通过。
[`two-charges-on-consume.mt2f`](../tests/compatibility/game-2.3.1/mechanics/configured-opening-relics/two-charges-on-consume.mt2f)

PropagateOnStatusEffectApplied 的原版专项核对恰好 3 层的触发、超过 3 层的反例，以及四种状态各增加 2 层且仅作用于同一单位。
18 次出牌、6 次结束回合及两类候选检查通过。
[`propagate-on-status-effect.mt2f`](../tests/compatibility/game-2.3.1/mechanics/configured-opening-relics/propagate-on-status-effect.mt2f)

CooldownOnDamage_DH 的原版对照核对冷却从 3 降到 2，以及空法术不触发。没有冷却状态或仅剩 1 层冷却的
无技能目标都会留下零层登记。原版 Spikedriver Factory 受击时也从 1 层降为 0，技能可用标记保持 0。
三局共 71 次出牌或技能、20 次结束回合及两类候选通过。
[`cooldown-on-damage.mt2f`](../tests/compatibility/game-2.3.1/mechanics/configured-opening-relics/cooldown-on-damage.mt2f) ·
[`cooldown-on-damage-no-prior-status.mt2f`](../tests/compatibility/game-2.3.1/mechanics/configured-opening-relics/cooldown-on-damage-no-prior-status.mt2f) ·
[`cooldown-on-damage-ability-zero.mt2f`](../tests/compatibility/game-2.3.1/mechanics/configured-opening-relics/cooldown-on-damage-ability-zero.mt2f)

DamageOnCardPlayed 在四遗物阵容的实战中按房间的预演状态判断是否触发。开启预演随机流隔离时，
八次此前有差异的真实出牌现已匹配原版伤害与战斗随机流。开关两种设置均使用同一批四件原版遗物，
合计 46 次出牌、12 次结束回合及两类候选检查全部通过。
[隔离预演随机流](../tests/compatibility/game-2.3.1/mechanics/configured-opening-relics/native-card-play-damage-roster-isolated-rng.mt2f) ·
[原版随机流](../tests/compatibility/game-2.3.1/mechanics/configured-opening-relics/native-card-play-damage-roster.mt2f)

PyreHealthForEmberCardCost 的实际支付也按房间的预演状态判断。开启预演随机流隔离的 2.3.1 专项录制
确认普通费用、4 点薪火支付和 X 费三项对照，并核对唯一一次薪火受击回调。录制中的准备环节额外加入
三张对照牌，专项逐项核对 31 次出牌及 6 次结束回合，并从准备后的局面复放常规操作到终局。
[薪火支付专项](../tests/fixtures/combat/damage/full-battle-original-pyre-health-card-cost-isolated-preview.mt2f)

Echoes 的友军伤害限制在隔离原版用同一变异器分别录制了伤害回调和整场战斗。只有普通友军作为
攻击者时才会把对敌伤害归零。实战中已验证带 Pyregel 的目标，以及无攻击者的伤害对照。完整胜局
的 21 次出牌、7 次结束回合和两类候选检查通过。
[原版伤害回调](../tests/compatibility/game-2.3.1/mechanics/prevent-monster-damage/adjustments.mt2f) ·
[原版整战](../tests/compatibility/game-2.3.1/mechanics/prevent-monster-damage/native-battle.mt2f)

Echoes 的留存生命效果按单位的来源卡记录。原版专项中，列车员受击后写回 24 点生命，
以 24/25 部署的单位治疗后写回 25 点，预置 26 点的单位以 26/26 部署并在死亡时清零。
部署、受击、治疗和死亡的分支回放通过。原版开局整战的 21 次出牌、7 次结束回合及两类候选检查通过。
另一次隔离实机继续走完战后奖励和地图，原版战后回调给幸存的盾牌乘务员写回 4 点生命，
另一张同名卡的留存值为 0。独立进程加载原版第 2 环存档，支付费用打出同一张牌，单位以
4 点生命入场。第 1 环的 20 次操作、7 次结束回合和两类候选，以及第 2 环的部署后继与随机流均通过。
该变异器由隔离采样程序调用原版接口加入，原版定义的 DLC 要求为 `None`。
[跨战二进制归档](../tests/compatibility/game-2.3.1/mechanics/persistent-monster-health/campaign/manifest.tsv)
[受击、部署与治疗](../tests/compatibility/game-2.3.1/mechanics/persistent-monster-health/hit-birth-heal.mt2f) ·
[提高生命上限与死亡](../tests/compatibility/game-2.3.1/mechanics/persistent-monster-health/maximum-birth-death.mt2f) ·
[原版整战](../tests/compatibility/game-2.3.1/mechanics/persistent-monster-health/native-battle.mt2f)

时光薪火沿原版出生流程加载同一张技能牌。Echoes 变异器将使用后的冷却间隔从 99 改为 2，
原版对照保持 99。两组的出生冷却和当前剩余冷却均为 0，模型出生回调通过定向核对。
实战中，带变异器的时光薪火在第 1 回合使用后进入 2 点冷却，第 3 回合再次零费使用。
无变异器时，首次使用后移除本场技能。三次使用与四次结束回合均匹配原版完整状态和随机数。
[启用变异器](../tests/compatibility/game-2.3.1/mechanics/pyre-ability-cooldown-birth/with-owner.mt2f) ·
[原版对照](../tests/compatibility/game-2.3.1/mechanics/pyre-ability-cooldown-birth/without-owner.mt2f) ·
[实战再次使用](../tests/compatibility/game-2.3.1/mechanics/pyre-ability-cooldown-birth/reuse-with-owner.mt2f) ·
[实战移除对照](../tests/compatibility/game-2.3.1/mechanics/pyre-ability-cooldown-birth/reuse-without-owner.mt2f)

随机目标变异器会在法术入手时改写手动目标和同层前排目标。原版回调核对火把、冰矛及重组，
其中重组保留原来的消耗牌堆目标。真实火把出牌从两名敌人中抽取一名，战斗随机数推进一次，
完整状态及无指定目标的候选均通过。原版放逐者与狱魔胜局的 18 次出牌和 7 次结束回合通过复放。
Just Cause 的原版专项确认，随机选中的单位换位后仍由同一个单位获得 Valor。该胜局的全部出牌和结束回合候选通过。
[随机火把专项](../tests/compatibility/game-2.3.1/mechanics/random-spell-targets/targetless-cast.mt2f) ·
[随机换位专项](../tests/compatibility/game-2.3.1/mechanics/random-spell-targets/just-cause.mt2f) ·
[原版整战](../tests/compatibility/game-2.3.1/mechanics/random-spell-targets/native-battle.mt2f)

Fhyra 在原版扣血并触发薪火受伤回调后获得 10 金币。零伤害和未触发回调的扣血对照均无金币变化。
不出牌的原版开局胜局包含 5 次有效受伤，金币从 50 升至 100，7 次结束回合及候选检查全部匹配。
[原版受伤回调](../tests/compatibility/game-2.3.1/mechanics/fhyra-gold-on-pyre-damage/tower-damage-callbacks.mt2f) ·
[原版整战](../tests/compatibility/game-2.3.1/mechanics/fhyra-gold-on-pyre-damage/native-battle.mt2f)

Pact 薪火在每回合首次打齐两个氏族时，为下一回合增加 1 次抽牌。实机两个相邻回合的七次出牌和两次结束回合
逐步匹配完整状态和随机数，三个开局手牌数依次为 5、6、6。另一场原版开局胜局的 39 次出牌、7 次结束回合
和两类候选检查均通过。
[双氏族专项](../tests/compatibility/game-2.3.1/mechanics/pact-draw-after-each-clan/paired-clans-two-rounds.mt2f) ·
[原版整战](../tests/compatibility/game-2.3.1/mechanics/pact-draw-after-each-clan/native-battle.mt2f)

Entropy 薪火读取永久牌组中的祸害。在隔离原版存档中加入两张原版自净化祸害后，首轮抽 6 张。
花费 3 点余烬打出一张祸害并永久移除后，次轮抽 5 张。出牌和两次结束回合的完整状态与随机数通过回放。
[原版祸害清除专项](../tests/compatibility/game-2.3.1/mechanics/entropy-pyre-draw/owned-blight-purge.mt2f)

`NoCombatPreviews` 激活后，原版所有录制决策均显示战斗预演关闭。整场原版胜局的 21 次出牌、
7 次结束回合及两类候选检查通过，并完成 16 条独立分支复放。
[禁用原版预演的整战](../tests/compatibility/game-2.3.1/mechanics/no-combat-previews/native-battle.mt2f)

Railforged 第三环 Unruly 在原版受控战后回调中依次测试复制和召唤。首领独处时召唤原版 Decoy，
已有 Decoy 时复制它。两种状态的效果次序、出生站位、完整后继和随机数均与模型一致。
原版测试场景的整战还通过首次出生、选层、Relentless 转相和终局。带 Conduit 跟踪的原版录档通过
33 次出牌、7 次结束回合及两类候选检查。
[原版战后回调](../tests/compatibility/game-2.3.1/mechanics/unruly-post-combat/native-post-combat.mt2f) ·
[原版出生](../tests/compatibility/game-2.3.1/mechanics/unruly-post-combat/native-birth.mt2f) ·
[原版整战](../tests/compatibility/game-2.3.1/mechanics/unruly-post-combat/native-battle.mt2f)

Unruly 的 Scourge 变体在同层敌军死亡后向手牌生成原版祸害。两次生成均与原版一致。
整战的 33 次出牌、7 次结束回合及两类候选检查通过。
[原版 Scourge 整战](../tests/compatibility/game-2.3.1/mechanics/unruly-scourge/native-battle.mt2f)

SniperAdvance 变体命中存活的后排目标后把该单位移到首位。只有一个目标时站位保持原顺序，
致死控制中首领触发未执行。原版整战的 34 次出牌、7 次结束回合和两类候选检查通过。
[原版狙击整战](../tests/compatibility/game-2.3.1/mechanics/unruly-sniper/native-battle.mt2f)

Soul Savior 第一环 Dutiful 的 Burst 变体录到三次战后发放与七次消耗。原版整战的
23 次出牌、4 次结束回合和两类候选检查通过，并核对了首领在有 Haste 时的楼层移动速度。
[原版 Dutiful 整战](../tests/compatibility/game-2.3.1/mechanics/dutiful-burst/native-battle.mt2f)

Dutiful 的 AscendDescend 变体在两次原版移位后给同层四个敌军各加 3 Rage。
23 次出牌、4 次结束回合、两类候选和 16 条独立分支通过。
[原版移位整战](../tests/compatibility/game-2.3.1/mechanics/dutiful-ascend-descend/native-battle.mt2f)

Dutiful 的 Armor 变体在三次原版战后回调中累计六次增加 12 护甲。采样沿用原版死亡结算等待，
以便光环退出后再读取回合后继。整战的 28 次出牌、5 次结束回合和两类候选检查通过。
[原版护甲整战](../tests/compatibility/game-2.3.1/mechanics/dutiful-armor/native-battle.mt2f)

Indulgent 的 Titanskin 变体已对照原版出生、楼层结算和单次交战。命中造成 5 点生命伤害，
Titanskin 吸收 3 点，状态层数保持 3。
[原版出生和受伤记录](../tests/compatibility/game-2.3.1/mechanics/indulgent-titanskin/birth-and-damage.mt2f)

Indulgent Titanskin 第一个结束回合与原版逐字段匹配。抽牌后两名波次敌人分别获得 +2 攻击，
喂食引起的状态回调在预演和正式战斗中给接收者登记 +0 攻击，三条 RNG 流在回调内保持原值。
[原版首回合与四次回调](../tests/compatibility/game-2.3.1/mechanics/indulgent-hivemind/native-first-turn.mt2f)

第二个结束回合也通过完整状态和随机流对照。四名 Hivemind 持有者分布在两层，第二次抽牌后
分别刷新为 +6 攻击。原版旧加成 +2、+2、+0 和新出生者未登记加成的四个输入均保留在归档中。
[原版第二回合](../tests/compatibility/game-2.3.1/mechanics/indulgent-hivemind/native-second-turn.mt2f)

第三个结束回合也匹配完整状态。六名持有者在三层楼各获得 +13 攻击，顶层喂食移除一名队友，
让接收者的 Hivemind 从 6 增至 9 层。记录同时保留了喂食后原版物理站位。
[原版第三回合](../tests/compatibility/game-2.3.1/mechanics/indulgent-hivemind/native-third-turn.mt2f)

Titanskin 整战验证了结算时新增和移除 Hivemind 房间修饰器，以及它们随单位上层移动。
终局的 Hunter 被薪火反击击杀，剩余 Heavy 完成最后一次交战。原版录档包含 8 次出牌、4 次
结束回合、35 次楼层或单次交战结算和 8 次整车结算，两个候选集和 16 条独立分支均通过。
[原版 Titanskin 整战](../tests/compatibility/game-2.3.1/mechanics/indulgent-titanskin/native-battle.mt2f)

Indulgent 的减容变体三次把玩家侧楼层容量减 1，敌方容量保持 5。专项回放核对三组完整减容状态，
第一组还在同一战后队列内验证先减容，再重放一次自身 OnFeed 获得 3 Rage。追加的原版整战验证了
8 次出牌、4 次结束回合与两类候选检查，实机结算为败局。
[原版减容和 OnFeed 记录](../tests/compatibility/game-2.3.1/mechanics/indulgent-capacity/native-capacity-feed.mt2f)
[原版减容整战](../tests/compatibility/game-2.3.1/mechanics/indulgent-capacity/native-battle.mt2f)

FavoredChild_2 的 2.3.1 原版施法记录覆盖 3 次战前行动和 2 次战后行动。
回放逐字段核对首领控制器、跨层移动后的物理站位、Hivemind 持有者和随机流，并检查输入状态保持原样。
这份归档的验证范围截至施法阶段。采样来自隔离档案 `20261124-033144-04e51780`，
原始整战 SHA-256 为 `19e5dd5e645bc5e51c097794942fcc7c9c25b9a1671605abbafe5dba647026e0`，
施法记录 SHA-256 为 `76a45570951e1f50dfbec2e14d1f15ed9e5079e87f29b00d57520c92ed096214`。
使用 `pwsh -File scripts/Check-Game231IndulgentCasting.ps1` 离线复验。
[原版施法记录](../tests/compatibility/game-2.3.1/mechanics/indulgent-capacity/native-casting-phases.mt2f)

Indulgent GorgeOnSlay 击杀后排入一次自身 OnFeed，获得 3 Rage，攻击从 13 升至 19。
同一段原版回放中，回调、首领状态、战斗上下文和 RNG 均一致。新的原版整战也通过 8 次出牌、
4 次结束回合和两类候选检查，以薪火耗尽结算。
[原版击杀和 OnFeed 记录](../tests/compatibility/game-2.3.1/mechanics/indulgent-gorge-on-slay/native-gorge-on-slay.mt2f)
[原版 GorgeOnSlay 整战](../tests/compatibility/game-2.3.1/mechanics/indulgent-gorge-on-slay/native-battle.mt2f)

减容和 GorgeOnSlay 的新月出生均通过原版独立回放。两个旧录档把原版控制器记录在出生事件里，
校验时将同一份控制器数据放回回合快照，核对未放置实体、初始选层、物理站位和随机流。
[减容出生](../tests/compatibility/game-2.3.1/mechanics/indulgent-capacity/native-birth.mt2f)
[GorgeOnSlay 出生](../tests/compatibility/game-2.3.1/mechanics/indulgent-gorge-on-slay/native-birth.mt2f)

Railforged 波次里的 `R1_Attacker_AscendGainAtk_Ver2` 击杀后尝试下移并获得 10 护甲。
原版底层和跨层两次交战的完整状态、物理站位和随机流均通过对照。跨层样本也覆盖伤害
预演触发目标层原生单位的 Shift 回调。
[原版两次交战](../tests/compatibility/game-2.3.1/mechanics/railforged-kill-bump/native-exchanges.mt2f)

Lifemother Reanimate 的战后效果先给玩家前排加 10 Witherbloom，再给敌方前排加 2 Undying。
六条原版回调包含一次 Witherbloom 生效、两次空目标和三次 Undying 生效，效果顺序、目标状态
和 RNG 均通过对照。暗月下移与手牌界面测试 RNG 按既有隔离入口处理后，原版败局的 17 次出牌、
4 次结束回合、两类候选和 16 条分支通过。归档中的实际复活次数为零。
[原版战后回调](../tests/compatibility/game-2.3.1/mechanics/lifemother-reanimate/post-combat.mt2f)
[原版整战](../tests/compatibility/game-2.3.1/mechanics/lifemother-reanimate/native-battle.mt2f)

Lifemother Infested 的三个同层死亡回调将首领的 Hivemind 从 0 依次加到 2、4、6 层。
每次加层时攻击保持原值，模型现有状态添加路径逐条匹配首领完整状态和三条随机流。
波次进食及楼层修正器的攻击刷新仍待整战对照。
[原版 Hivemind 加层回调](../tests/compatibility/game-2.3.1/mechanics/lifemother-infested/status-additions.mt2f)

Infested 的第一个结束回合通过完整回放，核对出生、首领控制器、物理站位和随机流。首轮抽牌后
三名波次单位各获 +3 攻击。第二和第三轮分别把 7 名持有者刷新为 +14 攻击、把 9 名持有者刷新为
+32 攻击。两个抽牌专项也核对了房间修饰器，并验证刷新过程不消耗随机数。
[原版首个结束回合](../tests/compatibility/game-2.3.1/mechanics/lifemother-infested/native-first-turn.mt2f) ·
[后两轮抽牌](../tests/compatibility/game-2.3.1/mechanics/lifemother-infested/later-draws.mt2f)

第二和第三波敌人通过四次原版出生回放，包含外层寻宝入口和内部波次入口。两次新波分别
新增 3 名和 2 名 Hivemind 持有者，物理站位、波次缓存和随机流一致。
[原版后两轮出生](../tests/compatibility/game-2.3.1/mechanics/lifemother-infested/later-spawns.mt2f)

Infested 完成原版整战验收。录制固定五次原版部署，连续核对 4 次结束回合、33 次楼层结算、
8 次整车结算和 16 条独立分支，两类候选全部通过。终局由 Heavy 的攻击耗尽薪火。
[后两轮楼层结算](../tests/compatibility/game-2.3.1/mechanics/lifemother-infested/later-combat.mt2f) ·
[原版整战](../tests/compatibility/game-2.3.1/mechanics/lifemother-infested/native-battle.mt2f)

另一条原版路线加入 Sacrificial Chamber。实际战斗中两次死亡回调各发放 10 金币，首领受伤后
继续保有 Hivemind，第三回合从 6 层增至 12 层。8 次出牌、4 次结束回合、34 次楼层结算
与两类候选均通过。
[Sacrificial Chamber 整战](../tests/compatibility/game-2.3.1/mechanics/lifemother-infested/sacrificial-chamber-battle.mt2f)

另一条出牌分支在相同六步前置之后，把 Sacrificial Chamber 放到首领所在层。原版的两个首领
触发器继续存在，房间为首领添加一个 10 金币的死亡触发，完整后继及随机流均通过检查。
[首领层的房间放置分支](../tests/compatibility/game-2.3.1/mechanics/lifemother-infested/chamber-floor2-branch.mt2f)

Fanning the Flame 和 Slam 分别击杀一名 Hivemind Horde。两场原版观测中，同层四名敌人都
增加 1 层，原持有者保留旧攻击加成，两位新持有者登记 +0 攻击。法术额外消耗一次 Battle
和两次 BattleTest，技能的两条随机流保持原值。两条分支均复放完整前置和后继。
[法术分支](../tests/compatibility/game-2.3.1/mechanics/lifemother-infested/horde-fanning-branch.mt2f) ·
[技能分支](../tests/compatibility/game-2.3.1/mechanics/lifemother-infested/horde-ability-branch.mt2f)

`Run-FullBattleProbe.ps1 -OriginalLifemotherInfestedObservation` 从隔离的原版 BattleIntro
采集待出生首领、施法控制器和同层死亡回调。该录制模式复用手牌界面测试随机流隔离，以便逐字段
对照游戏规则引发的随机消耗。首轮抽牌后首领在索引 2 的楼层，下一动作是第 0 组第 0 个动作。

Lifemother Debuffs 的原版践踏在濒死冠军和后排复仇者身上各施加 12 层 Malaise。
死亡回调把冠军身上的 12 层转给存活复仇者，叠到 24 层。四次回调的状态和随机流均通过专项回放。
两条原版 Torch 分支进一步核对 Malaise 与 Pyregel 的转移和清除顺序。反向顺序的样本中没有存活接收者。
整战的 11 次出牌、4 次结束回合、两类候选和 16 条独立分支通过，原版结算为败局。
[原版 Malaise 转移](../tests/compatibility/game-2.3.1/mechanics/lifemother-debuffs/native-component.mt2f)
[原版整战](../tests/compatibility/game-2.3.1/mechanics/lifemother-debuffs/native-battle.mt2f)

原版部署限制使底层和中层在部署阶段无法召唤，进入第 1 回合后解禁。完整结束回合后继通过对照。
同一次组合采样中，BigMonsters 使列车乘务员的占位由 3 变为 4，冠军由 2 变为 3，
部署和普通回合的结果相同。六条原版造牌记录确认先按原始占位应用突变，再应用造牌升级。
基础占位 6 的 Shadowsiege 无临时加成，应用原版 Tiny Stone 后降到 1。模型对照覆盖这六条记录的完整后继和随机数。
复制、开箱及多种占位效果叠加仍需独立对照。
[部署解禁](../tests/compatibility/game-2.3.1/mechanics/placement-mutators/placement-reopen.mt2f) ·
[原版单位占位](../tests/compatibility/game-2.3.1/mechanics/placement-mutators/placement-size.mt2f) ·
[原版造牌](../tests/compatibility/game-2.3.1/mechanics/placement-mutators/big-monsters-creation.mt2f)

两项原版自定义局突变经过同种子对照。部署仍弃牌，普通回合保留两张未出的牌，
并排在三张新抽牌之后。原版余烬在部署和首回合均为 24，支付 1 点后降至 23，
第二回合仍为 23。两组共两次出牌和四次结束回合，完整状态和随机数匹配。
同一规则下，原版未打出的 TFB_PyreAttack 从手牌移到弃牌缓冲，普通牌仍按原顺序保留。
原版未调用普通弃牌或永久移除回调，新回合完整后继和随机数匹配。
[双突变](../tests/compatibility/game-2.3.1/mechanics/hand-and-ember/combined.mt2f) ·
[普通对照](../tests/compatibility/game-2.3.1/mechanics/hand-and-ember/stock.mt2f) ·
[临时牌清理](../tests/compatibility/game-2.3.1/mechanics/hand-and-ember/ephemeral.mt2f)

原版法术伤害推层突变在火把打掉 3 点护甲时触发。敌人保留 5 点生命，从底层移到中层。
同一目标随后受到非伤害法术，站位保持不变。另一个被火把击杀的单位也未触发移动。
三次出牌后继和随机数均通过对照，范围是普通敌人的单目标直接法术。
[原版推层对照](../tests/compatibility/game-2.3.1/mechanics/spell-damage-move/component.mt2f)

ExtraFeedTrigger 的原版查询确认友军 OnFeed 次数为 2，OnDeath 次数保持 1。
61 个战斗阶段、5 次出牌、7 次结束回合与两类候选通过。实际普通进食也已录到两次 OnFeed，
合金构装体的燃料从 1 增至 3，矿工只被吃掉一次，完整状态与随机数在 16 条分支上复放一致。
[`extra-feed-trigger.mt2f`](../tests/compatibility/game-2.3.1/mechanics/configured-opening-relics/extra-feed-trigger.mt2f) ·
[`extra-feed-trigger-meal.mt2f`](../tests/compatibility/game-2.3.1/mechanics/original-source-backed-feeding/extra-feed-trigger-meal.mt2f)

Mother’s Remnant III 在原版满手时把首次合格出牌的永久副本加入牌组，同回合再次出牌受次数限制。
34 次出牌、7 次结束回合与两类候选检查通过。
[`mothers-remnant3.mt2f`](../tests/compatibility/game-2.3.1/mechanics/configured-opening-relics/mothers-remnant3.mt2f)

两个 2.3.1 原版移动遗物场景复用移动前月相掩码采集。升降层伤害场景实测敌军上移和下移
各受 3 点伤害。Pyregel 场景实测敌军上移获得 2 层 Pyregel，并核对下移、友军和移动受阻对照。
每份录像通过 22 个移动边界及 42 次出牌检查，脚本布置结束后另核对 20 次出牌、5 次结束回合和
16 条分支。两份完整录档连同各自哈希由专项入口验证。旧版控制录档的移动前掩码缺失，当前
通用移动检查在首个 `player-up` 报缺少该字段。
[移动遗物专项检查](../scripts/Check-Game231MovementRelics.ps1)

受控法术批次录到伤害、治疗、过量治疗和未来抽牌。Dark Deal 定向检查从录制动作找到
原版卡牌与目标，核对两版相同的 Rage、伤害和随机流。两版均通过专项检查，该 2.3.1
胜局另通过 10 次出牌、5 次结束回合、16 条分支和候选检查。
[`damage-healing-and-draw.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-isolated-spells/damage-healing-and-draw.mt2f)

状态单位批次用同一场原版胜局复核 11 张单位的付费出生、初始属性与状态，并录到
Steelguard 格挡、Undying 回场和 Burnout 到期。整场 21 次出牌、5 次结束回合、
16 条分支与候选检查通过。
[`birth-and-combat-statuses.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-status-units/birth-and-combat-statuses.mt2f)

18 张原版基础冠军在同一受控首战完成原版出生检查，新增 15 张收藏卡匹配。
固定采集顺序省去通用全手牌搜索后，同一机器与配置用时从 219.08 秒降到 81.92 秒。
两次的 24 个出牌输入及 29 个动作结果一致，各自通过原版整战和 16 条分支检查。
新归档另通过候选检查。累计 206 场整战和 249/601 张已匹配收藏卡。
[`base-deployment.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-base-champions/base-deployment.mt2f)

支援单位批次同样复用固定出牌顺序，实机录制 121.62 秒完成。原版对照覆盖 Rally、
治疗、受击、攻击、装备成长与死亡资源，独立检查通过 36 次出牌、5 次结束回合、
16 条分支和候选。累计 207 场整战和 260/601 张已匹配收藏卡。
[`unit-triggers-and-equipment.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-support-units/unit-triggers-and-equipment.mt2f)

六件原版状态层数遗物在同一场 2.3.1 胜局均有可归因的正向效果。专项核对 33 次
实战查询和 288 次边界查询，包括首次与重复 Pyregel，以及 2 点腐化下的 Corrupt Poison。
两版模块指纹分别校验，旧版 Burnout 对照也通过。新整战和候选检查通过。
[`status-stack-queries.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-status-relics/status-stack-queries.mt2f)

缩放伤害批次在原版胜局核对 11 张法术的死亡、消耗堆和牌库计数、腐化及 Horde 移除。
22 次出牌、5 次结束回合和候选检查通过。两个额外爆炸反应候选在离线验证中经过双方
待结算死亡与站位整理。实际出牌对照使用原版首战默认种子，目标吃到伤害后存活。
[`counters-and-horde.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-scaled-damage/counters-and-horde.mt2f)

同一场隔离对照打出十二张指定原版牌。Masterwork 令锻造点增加 8，Boilerplate 将其用完，
Excavation Eruption 和 Ice Tornado 分别命中三次，Pyreclasm 对存活目标造成 150 点伤害。
Disassemble 消灭 Dregs Tomb、获得 5 锻造点，并观测到两名单位出生。
16 次出牌、5 次结束回合、16 条独立分支和两类候选通过。
[`fixed-damage-and-forge.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-scaled-damage/fixed-damage-and-forge.mt2f)

九张原版牌同场核对献祭、出生和 X 费伤害。Inhale 击杀 4 生命的友军 Funguy，生成 8 生命的 Funguy。
Imp Provisation 献祭 Welder Helper 并补 1 余烬，Impressive 献祭 Embered Infery 并击杀敌方前排。
Moonbeam 以 X=5 付费，随后清除最后一名敌军。13 次出牌、5 次结束回合、16 条独立分支和两类候选通过。
[`sacrifice-and-x-hits.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-scaled-damage/sacrifice-and-x-hits.mt2f)

原版 X 费状态批次核对 Spike of the Legion 与 Prismal Dust 的 X=4 和 X=0，
并核对 Spike of the Hellhorned 的 X=4、目标楼层、双效果及消耗去向。
10 次出牌、5 次结束回合、16 条独立分支和两类候选通过。
[`x-cost-status.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-room-status-damage/x-cost-status.mt2f)

2.3.1 目录中 83 张卡牌的主伤害效果可按原版参数归为 64 组。已有 278 场通过整战登记后，
每组至少有一张定义匹配的原版牌实际打出。`FixtureTools card-damage-audit` 输出参数哈希、
出牌归档和动作索引。各效果的命中、失败和无目标条件由对应专项核对。

两场 2.3.1 胜局共同覆盖十件原版触发遗物。第一场观察 Hymnist 死亡后的 Rage，
第二场补采月相掩码并实际执行十次换层，观察 Angel 换层与 Whelp 攻击。两场共
58 次出牌或技能、12 次结束回合，各通过 16 条独立分支。第二场另通过候选检查。
[`equipment-and-death.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-trigger-relics/equipment-and-death.mt2f)
[`movement-and-strike.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-trigger-relics/movement-and-strike.mt2f)

补采的装备及死亡路线保留原版月相和第三件装备的冻结状态。第 1 回合实际装备一名敌方 Angel，
同场观察四次 Hymnist 死亡触发。23 次出牌、5 次结束回合、16 条独立分支和候选检查通过，
与移动路线合并仍覆盖十件遗物的正向效果。
[`equipment-and-death-with-moon.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-trigger-relics/equipment-and-death-with-moon.mt2f)

原版 DantesCandelabra 和 PressurizedBoiler 在薪火出生时分别加 2 层 Titan Skin 与 4 层 Burst。
FirstUnitGainDamageShield 在三个回合触发，另有两次同回合后续出生跳过的对照。18 次出牌、
6 次结束回合、32 条出生分支和候选检查通过。
[`pyre-and-first-unit-births.mt2f`](../tests/compatibility/game-2.3.1/mechanics/configured-opening-relics/pyre-and-first-unit-births.mt2f)

原版 HymnistsApplyValorOnSummon 在 33 个同层友方单位状态中分别加 5 层 Valor。
ExtraSpawnTrigger 实际将出生回调次数从 1 增至 2，无卡出生的升级在五个普通单位生效、五个复制体跳过。
14 次出牌、5 次结束回合、32 条出生分支和候选检查通过。
[`hymnist-team-spawn-and-duplicates.mt2f`](../tests/compatibility/game-2.3.1/mechanics/configured-opening-relics/hymnist-team-spawn-and-duplicates.mt2f)

三件原版开局腐化遗物按实际回调顺序生效，四个房间的腐化上限由 4 升至 5，顶层永久腐化为 1。
首次抽牌后四个房间各获得 2 层，顶层当前腐化为 3。21 次出牌、7 次结束回合和候选检查通过。
[`corruption-and-capacity.mt2f`](../tests/compatibility/game-2.3.1/mechanics/configured-opening-relics/corruption-and-capacity.mt2f)

原版单位遗物批次核对 41 次修饰回调、3 次正常出牌和 3 次无牌出生的零食护盾、
6 次牺牲及 35 次恶魔连击。首回合录制驱动加入两张对照牌，分段回放核对两张牌及手牌顺序，
随机流保持一致。19 次出牌、5 次结束回合及结束回合候选通过。实战位移和护盾挡伤尚待对照，
部署时两个移动候选缺前序月相快照。
[`单位遗物归档`](../tests/compatibility/game-2.3.1/mechanics/controlled-unit-relic-batch/battle.mt2f)

同种子补采从部署阶段记录月相。实机执行 7 次友军 Shift、1 次敌军 Shift 和 1 次护盾挡伤。
同一两张对照牌的布置按分段检查。40 次出牌、7 次结束回合及两类候选均通过。
[`单位遗物月相补采`](../tests/compatibility/game-2.3.1/mechanics/controlled-unit-relic-batch/moon-masks.mt2f)

两件原版衰减遗物验证友方怒气保留、敌方怒气衰减和双方霜寒保留。霜寒回调携带遗物来源时，
模型沿用该来源登记伤害归属。2.3.1 的 42 个房间阶段、23 次出牌、5 次结束回合与两类候选均匹配，
2.2.1 专项也通过。
[`decay-and-preserved-rage.mt2f`](../tests/compatibility/game-2.3.1/mechanics/configured-opening-relics/decay-and-preserved-rage.mt2f)

原版 CheaperBlightScourge 的两次指定出牌均以零余烬结算，同时核对十次费用修饰回调和
生成 Junk 的费用。24 次出牌、6 次结束回合和两类候选检查通过。
[`blight-and-junk-cost.mt2f`](../tests/compatibility/game-2.3.1/mechanics/configured-opening-relics/blight-and-junk-cost.mt2f)

原版 MoreSpikes 按友军尖刺层数增加攻击。实机对照记录 27 次友军加攻和 21 次敌军筛选，
两版专项、15 次出牌、5 次结束回合和两类候选检查均通过。
[`spikes-attack-scaling.mt2f`](../tests/compatibility/game-2.3.1/mechanics/configured-opening-relics/spikes-attack-scaling.mt2f)

原版 FirstRoomCardOnAllFloors 在首张房间附卡结算时复制到各楼层。专项核对普通牌、
每场一次条件、累计计数以及第二张附卡的筛选。7 次出牌、7 次结束回合和两类候选通过。
[`first-room-attachment.mt2f`](../tests/compatibility/game-2.3.1/mechanics/configured-opening-relics/first-room-attachment.mt2f)

原版 AddStatsToSoloUnits 的隔离对照中，单独一名友军每次获得 +5 攻击和 +5 生命。
专项同时核对多单位楼层、空层、跟踪重置和复制保留。4 次出牌、5 次结束回合、
16 条独立分支和两类候选通过。
[`solo-unit-upgrades.mt2f`](../tests/compatibility/game-2.3.1/mechanics/configured-opening-relics/solo-unit-upgrades.mt2f)

原版回合开始传播遗物实测四次全楼层回调、16 次状态层数增加 1，以及 21 次排除。
空层、双方单位、薪火及随机流均有对照。2 次出牌、5 次结束回合及两类候选通过。
[`start-turn-propagation.mt2f`](../tests/compatibility/game-2.3.1/mechanics/configured-opening-relics/start-turn-propagation.mt2f)

原版出生沉默遗物实测六名普通敌人，核对 287 次触发判定、Collector 排除和内层随机抽取。
2 次出牌、5 次结束回合及两类候选通过。
[`enemy-spawn-silence.mt2f`](../tests/compatibility/game-2.3.1/mechanics/configured-opening-relics/enemy-spawn-silence.mt2f)

三件原版单位资源遗物通过 585 条回调上下文核对。两次友军死亡各提供 5 金币，墓碑死亡提供
5 余烬，同回合首次装备提供 1 余烬。22 次出牌、5 次结束回合、16 条独立分支和候选检查通过。
[`death-and-first-equipment.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-resource-relics/death-and-first-equipment.mt2f)

两个原版出牌抽牌遗物记录六次出牌阈值触发和一次 Snack 触发。Snack 使用专门构造的测试对照牌。
23 次出牌、7 次结束回合、16 条独立分支和候选检查通过。
[`played-card-and-snack-draw.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-resource-relics/played-card-and-snack-draw.mt2f)

两件原版回合抽牌遗物验证一次开局补牌与十三次满血判定，涵盖受伤、零血、满血、预演及移除遗物对照。
25 次出牌、5 次结束回合、16 条独立分支和候选检查通过。
[`opening-and-full-health-draw.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-resource-relics/opening-and-full-health-draw.mt2f)

三件原版 Pyre 抽牌遗物各使每回合补牌增加 1 张。三件同持时连续六回合各抽 8 张，
逐件移除的对照均为 7 张。另核对五次资源与金币刷新、六个 Pyre 战斗阶段和胜利后规则列表的刷新。
1 次出牌、7 次结束回合及两类候选检查通过。
[`pyre-heart-draw-and-resources.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-resource-relics/pyre-heart-draw-and-resources.mt2f)

原版 ModifyDragonsHoardBattleStart 在部署阶段令龙之宝藏增加 1，分别刷新原版卡牌与单位资源。
两版专项均核对获得遗物、部署调用和上限。21 次出牌、7 次结束回合及两类候选通过。
[`deployment-hoard.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-resource-relics/deployment-hoard.mt2f)

原版 OneEmberFirstTurn_DH 在首个正式回合额外获得 2 余烬。两版均通过部署、取得遗物和移除遗物对照，
核对 23 次出牌、7 次结束回合及两类候选。
[`first-turn-energy.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-resource-relics/first-turn-energy.mt2f)

原版 DeckTurnDraw 遗物在牌组达到 25 张时，抽牌后额外抽 2 张。实机记录四次额外抽牌，
专项还核对门槛、重置及随机流。20 次出牌、5 次结束回合和两类候选通过。
[`deck-threshold-draw.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-resource-relics/deck-threshold-draw.mt2f)

原版 ConserveEmber 实机验证七次跨回合保留余烬，以及 21 次付费出牌和终局剩余余烬。
7 次结束回合及两类候选均通过。
[`conserve-ember.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-resource-relics/conserve-ember.mt2f)

两件原版 Sting 遗物对照 53 次卡牌修饰回调，三次付费施放均获得合计 +30 伤害与穿甲。
19 次出牌、6 次结束回合、32 条修饰器分支和候选检查通过。
[`sting-with-original-relics.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-isolated-spells/sting-with-original-relics.mt2f)

原版 Offering Token 零费抽一张牌，再弃掉选定的 Train Steward。
2 次出牌、5 次结束回合、16 条独立分支和两类候选通过。
[`offering-token.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-isolated-spells/offering-token.mt2f)

第二份原版归档选中刚抽到的乘务员，保留施放前手里的同名牌。求解器在抽牌和相关回调结算后
读取选择范围，两种弃牌后继都与各自的实机记录一致。模型还检查了施放前手中只有 Offering Token
时，刚抽到的牌仍能进入候选。
[原版弃掉新抽牌归档](../tests/compatibility/game-2.3.1/mechanics/controlled-isolated-spells/offering-token-discard-drawn.mt2f)

隔离的 Luna Coven／Awoken 对局用原版月相治疗和治疗抽牌遗物验证 Stardust Invocation。
月相变化先治疗乘务员并抽到 Bloody Moon，随后原版复制选择同时列出这张牌和手中的 Witchweave。
求解器现已枚举两张牌。实际选中 Bloody Moon 的完整后继和随机流与原版一致，另一条候选也能独立复放。
[Stardust 施放中抽牌归档](../tests/compatibility/game-2.3.1/mechanics/stardust-heal-draw/native-stardust-heal-draw.mt2f)

原版 Lazarus 冠军的三级 Devour 分别从 3 攻击的 Overworked Assistant 转移 3、6、9 点攻击。
随后牺牲消耗助手的 1 层 Undying，原单位以 1 点生命留场。测试通过原版升级接口选定技能等级，
在隔离手牌加入一张原版助手。三个等级的合法目标、结算后继和随机流均通过精确复放。
[Devour 原版归档](../tests/compatibility/game-2.3.1/mechanics/lazarus-devour/manifest.tsv)

原版 MonsterGlutton 受火把伤害后以 47/50 生命进食。原版 Morsel Miner 和 Feast 将生命结算到
59/59，同层自然出场的 Shield Guard 护甲从 25 降至 5，Miner 的来源卡进入 Eaten。三次原版操作
的完整后继和随机流一致，求解器也枚举到了这三步。测试将未修改的原版卡加入隔离手牌后付费执行。
[MonsterGlutton 进食归档](../tests/compatibility/game-2.3.1/mechanics/glutton-onfeed/native-glutton-onfeed.mt2f)

原版 Blighted Totem 在同层付费施放 Torch 后触发 `CardSpellPlayed`，给两名自然出场的敌人
各施加 2 层 Poison，友方单位未受影响。原版触发标记、完整状态、随机流和求解候选均匹配。
[Blighted Totem 施法归档](../tests/compatibility/game-2.3.1/mechanics/blighted-totem/native-blighted-totem.mt2f)

原版 Starter Frostbite Discard 零费对敌方前排施加 8 层霜寒，并从合格手牌中随机弃一张。
2 次出牌、5 次结束回合、16 条独立分支和两类候选通过。
[`starter-frostbite-discard.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-isolated-spells/starter-frostbite-discard.mt2f)

原版 Drain 付 1 余烬施加 3 层 Debuff，随后随机弃掉唯一合格的牌。
2 次出牌、5 次结束回合、16 条独立分支和两类候选通过。
[`drain.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-isolated-spells/drain.mt2f)

原版 Titan’s Gratitude 付 1 余烬攻击一名敌方前排，再从合格手牌随机弃一张。
专项核对原版 60 点伤害参数、实际伤害受剩余生命限制及法强来源。2 次出牌、5 次结束回合及两类候选通过。
[`titans-gratitude.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-isolated-spells/titans-gratitude.mt2f)

原版 Bounding Echoes 给手牌中的 Cosmic Bolt 加临时 Corrupt，随后实放该牌并核对腐化变化。
7 次出牌、5 次结束回合、16 条独立分支和两类候选通过。
[`bounding-echoes.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-isolated-spells/bounding-echoes.mt2f)

原版 Returned Soul 从弃牌堆选回 Cosmic Bolt，赋予临时 Corrupt 和 Consume，并核对再次施放。
7 次出牌、5 次结束回合、16 条独立分支和两类候选通过。
[`returned-soul.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-isolated-spells/returned-soul.mt2f)

三张原版 Scourge 对照均实际支付 1 余烬，无目标结算后自行移除。
23 次出牌、7 次结束回合、16 条独立分支及两类候选通过。
[`null-target-scourges.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-isolated-spells/null-target-scourges.mt2f)

七张原版法术与房间牌同场记录 Funguy 的出生与 Horde 增长、Morsel 生成及炼化选牌。
11 次出牌、5 次结束回合、16 条独立分支和两类候选检查通过。
[`horde-growth-and-feeding.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-isolated-spells/horde-growth-and-feeding.mt2f)

Grow Room 与空房间的锻造点、余烬原版查询在输入 0、1、2 时均返回原值。
查询前后房间、手牌与随机流一致。同层 Metallurgy 候选在原始整战快照上通过复放。
[`grow-room-smelt-costs.mt2f`](../tests/compatibility/game-2.3.1/mechanics/grow-room-smelt/grow-room-smelt-costs.mt2f)

原版 Metallurgy 在装有 Grow Room 的底层实际施放，按指定选择消耗一张零费 Morsel。
7 次出牌、5 次结束回合、16 条独立分支和两类候选检查通过。
[`grow-room-metallurgy.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-room-abilities/grow-room-metallurgy.mt2f)

八牌补采记录 Mass Production 复制原版 Morsel Miner，随后 Metallurgy 按指定选择消耗复制品。
12 次出牌、5 次结束回合、16 条独立分支和两类候选检查通过。
[`native-horde-and-hand-choices.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-isolated-spells/native-horde-and-hand-choices.mt2f)

另一组原版法术与房间牌覆盖 X 费回能、护甲、单位技能替换和战后治疗。原版 GainXEnergy 支付
11 余烬并返还 22。13 次出牌、5 次结束回合、16 条独立分支和候选检查通过。Enchanted Chamber
的伤害、治疗、Attuned 和跨层清理使用独立对照。
[`x-ember-armor-and-abilities.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-isolated-spells/x-ember-armor-and-abilities.mt2f)

三件原版 Conduit 遗物分别覆盖每回合首次出生、装备和击杀。采样保留物理位置，并核对 14 次出生、
11 次击杀和两次装备的逐件移除对照。21 次出牌、5 次结束回合、16 条独立分支和候选检查通过。
[`birth-equipment-and-slay.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-conduit-relics/birth-equipment-and-slay.mt2f)

两个原版单位升级遗物分别记录一次死亡触发的 +10 攻击、五次月相 +20 和四次移除。
实际 21 次出牌、5 次结束回合，候选检查和 16 条独立分支通过。
[`death-and-moon-upgrades.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-phase-relics/death-and-moon-upgrades.mt2f)

补采月相掩码后，Restoring Retreat 在原版先回复 10 点生命，再将该单位移到底层。
同一场受控胜局还核对两队 Witchweave、治疗与净化、毒层集中及翻倍、移除装备与加攻。
19 次出牌、5 次结束回合、16 条分支和候选检查通过。
[`healing-cleanse-and-descent.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-healing-cleanse/healing-cleanse-and-descent.mt2f)

五件原版生命周期遗物在同一场胜局均有可归因的效果。26 次出牌或技能、5 次结束回合、
35 个战斗阶段和 1 次自然 Undying 复活通过原版比对，新档另通过候选检查。复活前的
采集保留房间已有的首领关联。房间专项同步实测的选楼索引，再比较完整状态。
[`native-ability-and-revival.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-lifecycle-relics/native-ability-and-revival.mt2f)

移动法术批次记录九张原版法术，覆盖敌军上移与下移、双方同层下移、友军上移后的
顺序调整和移动后追加的状态。13 次出牌、6 次结束回合、16 条分支及候选检查通过。
[`ascend-and-descend.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-movement-spells/ascend-and-descend.mt2f)

楼层重排批次验证了伤害后的目标重排、护甲随机流、Valor 立即攻击、升层及降层伤害。
19 次出牌、5 次结束回合、16 条分支及候选检查通过。
[`position-and-movement.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-rearrange-spells/position-and-movement.mt2f)

资源遗物批次复核原版八件遗物的二十项效果、48 次余烬回调及六次八张抽牌。
42 次出牌与 7 次结束回合的原版胜局以 123 点薪火结束，专项和候选检查均通过。
[`energy-draw-and-pyre.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-resource-relics/energy-draw-and-pyre.mt2f)

友军状态批次核对十二张原版法术、单体与整层目标、实际状态层数及七条 Consume 路径。
16 次出牌、5 次结束回合、16 条分支和候选检查通过。
[`target-and-room-statuses.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-friendly-statuses/target-and-room-statuses.mt2f)

混合状态法术批次记录七张原版卡牌。专项核对友军 Valor、Shield、Burst，敌军 Rooted、
Spell Weakness、Unstable、Pyregel，以及伤害顺序和下一回合抽牌。10 次出牌、5 次
结束回合、16 条分支和候选检查通过。
[`statuses-and-draw.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-status-compositions/statuses-and-draw.mt2f)

楼层容量遗物批次核对原版五件遗物的十四项效果、21 次容量查询和 4 次实际房间初始化。
26 次出牌、5 次结束回合及候选检查通过。2.3.1 胜利末帧沿用通用终局比较。
[`capacity-and-pyre.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-floor-capacity/capacity-and-pyre.mt2f)

原版 Morsel 批次记录了五种原版召唤与进食结算，包含 Void 的升级与 Lifesteal。
8 次出牌、5 次结束回合、39 项专项对照、16 条分支和候选检查通过。
[`feeding-and-abilities.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-morsels/feeding-and-abilities.mt2f)

反应型单位批次核对 Reap、OnHit 和多次 Incant 的实际原版结算。2.3.1 的 Runebound Guardian
出生带 20 点护甲，自身和 Healing Totem 的 Incant 每次共加 5。24 次出牌、5 次结束回合、
16 条分支和候选检查通过。
[`incant-and-hit.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-reactive-units/incant-and-hit.mt2f)

单位增益批次核对八张原版卡牌的升级与状态变化、Burnout 筛选、同层与相邻层目标、
四条 Consume 路径。12 次出牌、5 次结束回合、16 条分支和候选检查通过。
[`buffs-and-consume.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-unit-buffs/buffs-and-consume.mt2f)

资源法术批次核对 Hoard 上限与薪火费用、扣资源前的伤害调整、Forge 和 X 费用的正值与零值。
13 次出牌、5 次结束回合、六条 Consume 路径、独立分支和候选检查通过。
[`hoard-forge-and-consume.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-resource-spells/hoard-forge-and-consume.mt2f)

原版 Feast 和 Shroud Spike 对照核对三次正向喂食、一次空层对照、目标存活和末次进食，
其中包括一次六倍效果和一次零费用结算。6 次出牌、5 次结束回合、16 条独立分支及两类候选通过。
[`shroud-spike-feeding.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-resource-spells/shroud-spike-feeding.mt2f)

原版 Channelsong 与 Gifts for a Guard 同场核对按类型抽牌、升级调度和拒绝条件。
两张法术及一张原来没有 Consume 的受益牌均实际打出并进入消耗堆。专项核对 57 次抽牌调用、
32 次升级调度、满手和空池。23 次出牌、5 次结束回合、16 条独立分支和两类候选通过。
[`typed-draw-and-consume.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-resource-spells/typed-draw-and-consume.mt2f)

原版 Prism Retrieval 的 X=3、0、10 实测攻击分别增加 15、0、50，费用分别降低 3、0、10。
专项核对单位抽牌、法术排除、升级队列及消耗。6 次出牌、5 次结束回合和两类候选通过。
[`prism-retrieval.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-resource-spells/prism-retrieval.mt2f)

原版 Awoken’s Rail Spike 核对 X=3、0、10，抽到的牌费用 −1，X 额外 +1，并核对禁抽及下一回合重置。
11 次出牌、5 次结束回合、16 条独立分支和两类候选通过。
[`awoken-rail-spike.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-resource-spells/awoken-rail-spike.mt2f)

持有原版 `ExtraSpellUpgrades` 时，Awoken’s Rail Spike 支付 4 余烬。Consume 先将来源牌移出弃牌堆，
抽牌回调随后只取出剩余的一张牌，牌序随机流保持原值，并设定当回合禁抽。
2.3.1 原版胜局的 5 次出牌、5 次结束回合、16 条独立分支和出牌候选通过。
[`awoken-rail-spike-spell-slot-relic.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-resource-spells/awoken-rail-spike-spell-slot-relic.mt2f)

原版 Card Draw Potion 支付 1 余烬并进入消耗牌堆。下回合原版抽 8 张牌，额外抽牌修正随后归零。
`--original-card-draw-potion-only` 核对这条机制及 16 条独立分支。3 次出牌、5 次结束回合和两类候选检查通过。
[`card-draw-potion.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-resource-spells/card-draw-potion.mt2f)

带 Consume 和 CopyOnPlay 升级的原版法术完成击杀时，原版先生成手牌副本，再结清原牌的待命条目。
原牌累计两次消耗统计，第二次结算也计入副本的 AnyExhausted。2.3.1 的 3 次出牌、4 次结束回合、
16 条独立分支与两类候选检查通过，两份 2.2.1 归档也通过同一专项复放。
[`terminal-copy-consume.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-resource-spells/terminal-copy-consume.mt2f)

原版 The Great Decay 为三个牌堆中的法术添加升级。专项验证费用折扣、Consume、
X 加成和单位筛选。6 次出牌、5 次结束回合、16 条独立分支与两类候选通过。
[`great-decay.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-resource-spells/great-decay.mt2f)

原版 Unearth Treasure 核对耗尽堆的 5 张和 10 张门槛，以及随机返回 2 张和 1 张法术时的顺序与随机流。
4 次出牌、5 次结束回合、16 条独立分支及两类候选通过。
[`unearth-treasure.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-resource-spells/unearth-treasure.mt2f)

原版 Broken Memories 零费将选中的耗尽法术放回抽牌堆，本牌进入耗尽堆，耗尽单位提供过滤对照。
7 次出牌、5 次结束回合、16 条独立分支及两类候选通过。
[`broken-memories.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-resource-spells/broken-memories.mt2f)

敌方状态批次核对十一张原版卡牌，包括单体、整层及整塔目标、Pyregel 与 Rage 的翻倍、
保证触发的随机流和腐化消耗。18 次出牌、5 次结束回合和候选检查通过。
[`hostile-status-order.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-hostile-statuses/hostile-status-order.mt2f)

腐化限制批次复核三张原版载体及其生成结算，包含十二个低于、等于及高于费用边界的
合法性查询、选中楼层、扣除与回填顺序。7 次出牌、5 次结束回合及候选检查通过。
[`paid-corruption.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-corrupt-restricted/paid-corruption.mt2f)

另一场受控装备批次在三个原版宿主上验证九件原版装备的实际付费与挂载。13 次出牌、
5 次结束回合、16 条分支与候选检查通过。
[`phase-and-combat-effects.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-isolated-equipment/phase-and-combat-effects.mt2f)

复制与弃牌批次合并记录八张原版法术。专项观察三次付费的原版宝藏牌、五次弃牌免费
结算，以及十七次结束回合的对照。12 次出牌、5 次结束回合、16 条分支和候选检查通过。
[`copy-and-treasure-discard.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-isolated-spells/copy-and-treasure-discard.mt2f)

接枝装备批次验证五件原版装备及替换、待命、拆离与移除顺序，另有十次独立原版重置对照。
14 次出牌、6 次结束回合、16 条分支和候选检查通过。
[`standby-and-severing.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-grafted-equipment/standby-and-severing.mt2f)

接枝单位的单独批次记录五个原版付费出生、各自的来源牌和装备绑定，以及七次宿主死亡返还。
9 次出牌、5 次结束回合、16 条分支和候选检查通过。
[`paid-unit-births.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-grafted-equipment/paid-unit-births.mt2f)

房间法强批次使用 Cosmic Bolt、Duality 与 RIP 的原版定义，核对 0 和 5 点房间法强、
伤害与治疗倍率、费用及楼层目标。8 次出牌、5 次结束回合、16 条分支和候选检查通过。
[`room-spell-power.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-strong-magic/room-spell-power.mt2f)

原版首战同时记录三件开战房间遗物和三件抽牌后生成遗物。三个房间附件均实际安装，
三件生成遗物各触发五次，完整比对原版生成牌、临时 Ephemeral 与随机流。37 次出牌、
6 次结束回合、16 条分支与候选检查通过。
[`room-attachments-and-post-draw.mt2f`](../tests/compatibility/game-2.3.1/mechanics/configured-opening-relics/room-attachments-and-post-draw.mt2f)

房间状态伤害批次核对 Battering Ram 与 Bramble Lash 的零值和正值，原版累加同层友军的
护甲或尖刺，并保留敌军、其他楼层和输入缺失对照。14 次出牌、5 次结束回合和候选检查通过。
[`armor-and-spikes.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-room-status-damage/armor-and-spikes.mt2f)

三场腐化触发胜局分别验证 Carver 临时增加 3 点攻击和护甲、Glugsider 回复生命、
Keeper 同时给自己和 Train Steward 增加升级。共 61 次出牌、18 次结束回合，
专项和候选检查均通过。三场归档位于 `mechanics/controlled-original-corruption-triggers/`。

Ember Forge 与 Wildwood Custodian 使用原版单位定义完成首战对照。前者在回合结算补 2 余烬，
后者存活时实际抽六张牌，死亡后恢复五张。5 次出牌、5 次结束回合、16 条独立分支和候选检查通过。
[`ember-and-draw.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-support-units/ember-and-draw.mt2f)

十单位批次覆盖原版出生、锻造、回合余烬、弃牌回收与单位技能候选。14 次出牌、5 次结束回合、
16 条独立分支和候选检查通过。Pyreblooded 技能候选使用当前楼层的实机单位状态，
这一场技能所在层的 Pyregel 为零。
[`birth-resources-and-recovery.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-support-units/birth-resources-and-recovery.mt2f)

五张原版单位同场核对 Sporelauncher 的同层衰亡、Death Eater 的灵魂、Apex Imp 的护甲和怒气、
Deathmetal Hymnist 的连击绑定及 Weldling 战后护甲。9 次出牌、5 次结束回合、16 条独立分支和两类候选通过。
[`decay-souls-and-armor.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-support-units/decay-souls-and-armor.mt2f)

同一组新增顶层 Cluster Colonel，并保留中层 Deathmetal Hymnist。原版战斗预演退出后，
顶层无牌出生的预演单位编号未进入下一决策快照。模型按原版光环引用和站位清理编号，
10 次出牌、5 次结束回合、16 条独立分支以及两类候选均匹配。随后原版 Funguy 的 Horde 实测达到 16 层。
[`colony-and-enchanter.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-support-units/colony-and-enchanter.mt2f)

Bog Chrysalis 和 Bogdeep Cocoon 的同场对照实测孵出两只 Bog Fly。顾问从原版出生配置
提前取得 Flight 技能定义，孵出的两只单位都可在候选里使用跨层飞行。6 次出牌、5 次结束回合、
16 条独立分支及两类候选通过。跨层飞行用这场的实机状态离线复放，原版实放由已有飞行专项验证。
[`bog-hatching-and-flight.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-support-units/bog-hatching-and-flight.mt2f)

开局批测复用原版导魔追踪读取器后，Fae Moon Pixie 与 Ethereal Seelie 在同一场部署到中层。
原版胜局的 20 次出牌、6 次结束回合，以及出牌和回合结束候选均通过独立复放。Fae Moon Pixie
还在逻辑站位的隔离场景单独核对了原版出生配置、4 次出牌、5 次结束回合和 16 条分支。
[原版同场部署](../tests/compatibility/game-2.3.1/mechanics/controlled-original-support-units/opening-conduit-pixie-and-seelie.mt2f)
· [单独出生校准](../tests/compatibility/game-2.3.1/mechanics/controlled-original-support-units/fae-moon-pixie-conduit.mt2f)

同一批测入口另用 Coldstrike Siren 和 Paraffin Enforcer 验证两名带攻击触发的原版单位。
分别部署到中层和顶层，23 次出牌、7 次结束回合的连续后继及两类录制候选均通过。
[原版攻击触发单位](../tests/compatibility/game-2.3.1/mechanics/controlled-original-support-units/opening-attacking-status-units.mt2f)

同一入口批录 Bog Fly、Animus of Speed 和 Spikedriver Factory 的付费部署。三名原版单位均捕获独立技能 ID，
部署结算时的技能冷却均为 2。24 次出牌、7 次结束回合及两类候选通过。各自技能施放另做专项验证。
[原版技能单位开局录像](../tests/compatibility/game-2.3.1/mechanics/controlled-original-support-units/opening-shared-ability-carriers.mt2f)

`-UseOpeningUnitAbilities` 可在开局部署后同场录制多个原版技能。Animus of Speed 的指令攻击击杀前排后，
两名敌人随原版房间回调前移，即使启用了预览随机数隔离。Spikedriver Factory 也在同场完成尖刺制作选择。
23 次出牌或技能、6 次结束回合和两类候选通过。三单位组合中，Bog Fly 的出发楼层没有腐化，Flight 仍用独立场景核对。
[同场技能录像](../tests/compatibility/game-2.3.1/mechanics/controlled-original-support-units/opening-ability-activations.mt2f)

Flight 专项用 `-OpeningUnitRoomCorruption 4` 通过原版管理器设置开局腐化，并采集实际月相掩码。
Bog Fly 从中层飞到底层，中层腐化从 4 降至 3。17 次出牌或技能、5 次结束回合和两类候选通过。
[Flight 原版施放录像](../tests/compatibility/game-2.3.1/mechanics/controlled-original-support-units/opening-bog-flight-activation.mt2f)

Alloyed Construct、Crucible Collector 和 Crucible Warden 在一次原版对局中完成付费出生。
归档保留三者的 OnFeed 定义，6 次出牌、5 次结束回合及两类候选通过。实际进食另有专项场景。
[进食单位出生录像](../tests/compatibility/game-2.3.1/mechanics/controlled-original-support-units/feeding-carrier-births.mt2f)

同场使用 Hidden Passage 将原版 Death’s Dancer 从底层移到中层。Shift 结算后攻击从 7 增至 11。
该场另录到 Divine Dancer 的付费出生。7 次出牌、5 次结束回合及两类候选通过。
[舞者实机移动录像](../tests/compatibility/game-2.3.1/mechanics/controlled-original-support-units/dancer-shift-and-birth.mt2f)

Bounty Stalker 与 Makeshift Golem 在同场完成原版付费部署。Bounty Stalker 首次死亡时，原版在对应牌上添加 +10 攻击升级。
Makeshift Golem 留场至终局清理。23 次出牌、7 次结束回合及两类候选通过。
[原版部署及死亡录像](../tests/compatibility/game-2.3.1/mechanics/controlled-original-support-units/opening-death-callback-units.mt2f)

Lunar Priestess 在战后生成四张 Dusk，第一张使原版开启月相追踪。开局单位探针复用原版月相采集和现有校验入口，覆盖追踪中途开启的连续后继。
26 次出牌、7 次结束回合及两类候选通过。
[原版战后生成与月相录像](../tests/compatibility/game-2.3.1/mechanics/controlled-original-support-units/opening-lunar-priestess.mt2f)

装入两种升级单位模板和一张法术的原版 Jack-in-the-Box 实际放出两名单位并跳过法术。
首个战后出生完成时，原版把前排空槽后的敌人前移。中间房间阶段、5 次出牌、5 次结束回合、
16 条独立分支和两类候选均匹配，旧版有内容和空盒对照也通过。
[原版盒子战后出生录像](../tests/compatibility/game-2.3.1/mechanics/controlled-original-support-units/jack-in-the-box-populated.mt2f)

Astrologer 与 Nightingale 在同一场完成付费部署。原版在存活单位参与战斗期间交替切换新月和满月，
23 次出牌、7 次结束回合及两类候选通过。
[原版月相单位对局](../tests/compatibility/game-2.3.1/mechanics/controlled-original-support-units/opening-moon-callback-units.mt2f)

Reforming Ooze 和 Hulking Mass 同场验证付费出生、Undying 和 40 Armor，以及两条未触发的 OnReanimated 定义。
5 次出牌、5 次结束回合、16 条独立分支及两类候选通过。
[原版复生单位出生录像](../tests/compatibility/game-2.3.1/mechanics/controlled-original-support-units/reanimation-carrier-births.mt2f)

Vinemother 和 Glareminder 同场触发弃牌前回调。专项核对 Vinemother 对 16 张 Sting 的三次升级及各牌堆对照，
原版底层腐化也从 0 逐回合增至 4。5 次出牌、5 次结束回合、16 条独立分支及两类候选通过。
[原版弃牌前单位回调录像](../tests/compatibility/game-2.3.1/mechanics/controlled-original-support-units/pre-discard-unit-callbacks.mt2f)

两种 Feeder 与 Morselmaker 同场完成付费出生。Morselmaker 在三次战后供食中令前排锚点各增加 9 点攻击。
两种 Feeder 自身的 OnFeed 在这场未触发。7 次出牌、5 次结束回合、16 条独立分支及两类候选通过。
[原版供食单位批录](../tests/compatibility/game-2.3.1/mechanics/controlled-original-support-units/feeder-and-morselmaker-callbacks.mt2f)

Crucible Steward 与 Conductor Steward 同场完成付费出生。对局保留技能可用状态与卡牌触发定义，
Conductor Steward 的临时增益在四个观察点均未触发。5 次出牌、5 次结束回合、16 条独立分支及两类候选通过。
[原版乘务员出生录像](../tests/compatibility/game-2.3.1/mechanics/controlled-original-support-units/card-trigger-stewards.mt2f)

Minds of Their Own 命令装备单位击杀前排后，原版将余下敌人前移。普通出牌和技能共用补位处理，
同场还核对了 Pyreshot 的薪火攻击。7 次出牌、5 次结束回合、16 条独立分支及两类候选通过，旧版两种法术和技能补位专项均通过。
[原版指令攻击录像](../tests/compatibility/game-2.3.1/mechanics/controlled-original-support-units/spell-commanded-attack-centering.mt2f)

原版 Kinhost Vessel 单独录到一次自然孵化。22 次出牌、7 次结束回合和 57 个房间阶段通过，
两类候选均无未支持项。128 状态预算内的串并行搜索返回相同结果，三步计划复放一致。
[原版 Kinhost Vessel 孵化录像](../tests/compatibility/game-2.3.1/mechanics/controlled-original-support-units/kinhost-vessel-hatch.mt2f)

Monstrosity 和 Splitter 同场完成付费出生。Monstrosity 在四个观察点均因本层单位死亡取得临时增益。
Splitter 的两个死亡召唤定义已捕获，当场保持未触发。6 次出牌、5 次结束回合、16 条独立分支和两类候选通过。
[原版本层死亡与召唤定义录像](../tests/compatibility/game-2.3.1/mechanics/controlled-original-support-units/room-death-carriers.mt2f)

原版 First of Kin 和 Sacrificial Resurrection 的同场对照核对两张手牌被消耗、单位增攻及
Draff 出生，Morel Mistress 提供排除对照。7 次出牌、5 次结束回合、16 条独立分支和两类候选通过。
[`exhausted-first-of-kin.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-support-units/exhausted-first-of-kin.mt2f)

原版 Molded 付 1 余烬选回耗尽单位，赋予 +5 攻击、+5 生命及 Ephemeral，并验证耗尽法术被排除。
5 次出牌、5 次结束回合、16 条独立分支及两类候选通过。
[`molded.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-support-units/molded.mt2f)

原版 Wicked Blaze 付 2 余烬选回耗尽单位，赋予 +15 攻击、+5 生命及 Ephemeral。
7 次出牌、5 次结束回合、16 条独立分支和两类候选通过。
[`wicked-blaze.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-support-units/wicked-blaze.mt2f)

原版 Starter Reform 从耗尽堆随机取回唯一合格的单位，赋予 +5 攻击、+5 生命、费用 −99 和 Ephemeral。
2 次出牌、5 次结束回合、16 条独立分支和两类候选通过。
[`starter-reform.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-support-units/starter-reform.mt2f)

四张原版单位同场核对出生模板与 Conduit 来源，涵盖 Magic Power Acolyte 和 Moonguard 的月相增益。
8 次出牌、5 次结束回合、16 条独立分支及两类候选通过。
[`conduit-and-moonguard.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-support-units/conduit-and-moonguard.mt2f)

原版 Magnetic Relay 召回普通无装备单位时，实机清空该单位的物理槽位，来源卡回到手牌。
Transcend Imp 随后按原有顺序重放召唤历史，被召回的单位可重新部署。7 次出牌、5 次结束回合、
16 条独立分支和两类候选通过。
[`transcend-imp-physical-recall.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-support-units/transcend-imp-physical-recall.mt2f)

加入原版 Fledgling Imp 后，复放按历史顺序完成金币、自身临时升级和同层友军的 5 层 Rage。
三次原版重放覆盖空历史、三个有序来源及其中一个来源被召回后的历史。两次有效重放保留不同楼层、
敌军和随机流的对照。8 次出牌、5 次结束回合、两类候选和并行独立复放通过。
[`transcend-imp-original-fledgling-rage.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-support-units/transcend-imp-original-fledgling-rage.mt2f)

同组追加原版 Welder Helper。两次有历史的重放只给本层原版友军前排加 20 护甲，
并核对原版必定触发仍推进 Battle RNG 的行为。8 次操作、5 次结束回合、两类候选和
并行独立复放通过。
[`transcend-imp-original-welder-front-armor.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-support-units/transcend-imp-original-welder-front-armor.mt2f)

原版 Cheery Deathcap 和 Enthusiastic Fan 在空楼层触发后都保留在召唤历史中。下一回合
在自然刷出敌人的楼层召唤 Transcend Imp，两名敌人各获得 7 层 Decay 和 3 层 Sap。
10 次操作、5 次结束回合、两类候选、完整后继及随机流通过独立复放。
[`transcend-imp-original-enemy-room-decay-sap.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-support-units/transcend-imp-original-enemy-room-decay-sap.mt2f)

追加原版 Starter Imp 与 Unstable Infery 的固定伤害回放。下一回合在自然刷出敌人的
楼层召唤超越小鬼，前排承受 16 点伤害，随后同层两名敌人各承受 10 点。前排的 25 点
护甲耗尽，生命从 5 降至 4，后排生命从 125 降至 115。10 次操作、5 次结束回合、
两类候选及 16 条独立分支通过完整状态和随机流对照。采集使用隔离工作进程的
`-TranscendImp` 模式，进程环境设置 `MT2_PROBE_TRANSCEND_DAMAGE=1`。
[`transcend-imp-original-front-room-damage.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-support-units/transcend-imp-original-front-room-damage.mt2f)

原版 Hallowed Halls 牺牲同层友军，并从预置耗尽堆重建六名升级单位。专项核对 Standby、
站位和容量。5 次出牌、6 次结束回合及两类候选通过。
[`hallowed-halls.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-support-units/hallowed-halls.mt2f)

2.3.1 隔离对照通过原版接口获取 `AlliedClassRelic` 后实际施放 Hallowed Halls。
该遗物只参与战后选牌的氏族筛选。5 次出牌、6 次结束回合、出牌候选和结束回合候选均通过完整状态和随机流对照。
专项同时用原版 Carriage Jack 作为有战斗回调的反例，确认顾问仍会拒绝该组合。
[`hallowed-halls-allied-draft-relic.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-support-units/hallowed-halls-allied-draft-relic.mt2f)

原版 Echo Vault 与 `SilverIncreasedEmber` 同场时，普通法术仍可被记忆并在房间开战前免费重施。
对照记录火炬重施打掉 3 点护甲、回响缓存清空、8 次出牌、5 次结束回合和 16 条独立分支。
改动遗物数值或加入未知战斗回调的反例继续触发保护。
[`native-echo-vault-silver.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-isolated-spells/native-echo-vault-silver.mt2f)

Pyreblooded 的后续实机对照在同层记录友军 10 层、两名敌军各 4 层 Pyregel。原版技能
消耗 0 余烬并进入 3 层冷却，自身攻击由 5 提升至 23。8 次出牌或技能、5 次结束回合、
16 条独立分支和候选检查通过。专项也复放零层、缺少楼层快照和跨楼层的对照。
[`pyreblooded-room-pyregel.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-support-units/pyreblooded-room-pyregel.mt2f)

另一场部署八张原版单位。Plague Doctor 实际触发攻击前回调，Wickless Baron 从死亡触发获得
临时攻血，四个结算快照的攻击为 15、25、25、35。12 次出牌、5 次结束回合、16 条独立分支及候选检查通过。
[`pre-attack-and-death-growth.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-support-units/pre-attack-and-death-growth.mt2f)

部署被动遗物批次保留六件原版遗物取得顺序与八组牌的原版结算、回调返回值和统计。
升级 Torch 只收到升级数量效果的 +3 伤害，两个种族效果分别命中 Royal Whelp 与 Greed Dragon。
录制时仅观察攻击回调的出入参，绕开会改变 Mono 种族判定结果的筛选插桩。离线用
`ModelChecks --original-opening-passive-relics-only <archive>` 重放该组件。
[`card-modifiers.mt2f`](../tests/compatibility/game-2.3.1/mechanics/opening-passive-relics/card-modifiers.mt2f)

另一场首战打出 11 张原版法术，覆盖抽牌、余烬、友军伤害、治疗和攻血互换。
15 次出牌、5 次结束回合、完整状态与随机流、16 条独立分支和候选检查通过。
[`resources-and-friendly-fire.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-isolated-spells/resources-and-friendly-fire.mt2f)

Shadesplitter 单独保留底层低生命敌军，确认原版击杀后从加权池生成一张 Morsel 并推进战斗随机流。
5 次出牌、5 次结束回合、16 条独立分支和候选检查通过。
[`shadesplitter-morsel.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-isolated-spells/shadesplitter-morsel.mt2f)

Stasis Crystal 实际附着后触发一次成长。移除技能后仍保留楼层和其他生成单位对照。
6 次出牌或技能、5 次结束回合、16 条独立分支和候选检查通过。
[`stasis-crystal-removal.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-isolated-equipment/stasis-crystal-removal.mt2f)

Mitosis Cap 在战斗结束时永久复制宿主来源卡。原版回调与牌组归属、牌堆和新实例标识一致。
5 次出牌、5 次结束回合、16 条独立分支和候选检查通过。
[`mitosis-cap-copy.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-isolated-equipment/mitosis-cap-copy.mt2f)

装备整组复测覆盖 Cursed Twinblade 每击减攻、Resin Blades 宿主死亡回手与 Organ Harvester
击杀后获得 Undying。原版 17 件装备、26 次出牌或技能、5 次结束回合、16 条独立分支和候选检查通过。
[`attack-and-host-death.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-isolated-equipment/attack-and-host-death.mt2f)

Loamcoat 实战核对先添加再生、后调整层数的战后顺序。Void Arms 组合用 Scrap for Parts
使宿主死亡并确认来源牌永久移除。两场共 11 次出牌、11 次结束回合，各 16 条独立分支及候选检查通过。
[`loamcoat-regeneration.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-isolated-equipment/loamcoat-regeneration.mt2f)
[`void-arms-host-purge.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-isolated-equipment/void-arms-host-purge.mt2f)

Assembly Room 复制前排单位并把复制体下移，Echo Bloom 让底层腐化从 1 增至 5。
记录三次房间技能、六次冷却变动、9 次出牌或技能、5 次结束回合，16 条独立分支和候选检查通过。
采样同时记录物理站位和移动前月相。
[`assembly-and-corruption.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-room-abilities/assembly-and-corruption.mt2f)

原版 Trap Chute 与 Steel Pulley Claw 分别验证跨两层移动加眩晕和跨层牵引。
8 次出牌或技能、6 次结束回合、两次房间技能、三次冷却变动、16 条独立分支和候选检查通过。
[`movement-and-daze.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-room-abilities/movement-and-daze.mt2f)

同层替换路线依次装入四张原版房间牌。Spell Sanctum 将后续药剂实际费用从 1 降至 0，
药剂登记下回合额外抽 3 张。Hellfurnace 在两次施放中各提高 3 层状态，Hall of Mirrors
复制一次 Dreg。Engine Upgrade 装入后友方容量由 3 增至 4，接下来三个回合各额外获得 1 点余烬。
12 次出牌、5 次结束回合、16 条独立分支和候选检查通过。
[`ordered-room-replacements.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-room-abilities/ordered-room-replacements.mt2f)

Enchanted Chamber 独立对照验证 Torch 实际伤害 18、Restore 回复最高 19，以及 Attuned 提升到 75。
跨层与房间替换移除临时附魔。10 次出牌、5 次结束回合、16 条独立分支和候选检查通过。
[`enchanted-chamber-replacement.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-room-abilities/enchanted-chamber-replacement.mt2f)

Rage Room 实机核对房间触发器、楼层与队伍范围，以及友军获得 Rage 的正例。
5 次出牌、5 次结束回合、16 条独立分支和候选检查通过。
[`rage-room.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-room-abilities/rage-room.mt2f)

房间批次记录九张指定原版牌，实际施放与结束回合逐项复放。Electricity Chamber 的出生尖刺
通过原版专项核对。13 次出牌、5 次结束回合、16 条独立分支和两类候选通过。
[`spawn-triggers-and-range.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-room-abilities/spawn-triggers-and-range.mt2f)

Imprisoned Zephyr 的两种原版首战都观察到后排 Dreg 在攻击时移到前排，其他楼层站位保持一致。
每场 6 次出牌、5 次结束回合及 16 条独立分支均通过。原档各保留三处终局协程诊断，
专项回放核对了离场敌人的旧槽位及下一决策前的清理，两个归档计入诊断清单。
[`Junker 对照`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-room-abilities/zephyr-junker-terminal-witness.mt2f)
[`Horde 对照`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-room-abilities/zephyr-horde-terminal-witness.mt2f)

Reanimation Lab 的独立对照实放原版 Dreg 并核对出生升级。6 次出牌、5 次结束回合、16 条独立分支
和两类候选检查通过。
[`reanimation-birth.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-room-abilities/reanimation-birth.mt2f)

Ice and Pyre 的独立对照在顶层命中前排敌军并造成 90 点伤害，在空 Pyre 层正常付费。
三个原版卡牌实例各检查四层的手牌和出牌范围。3 次出牌、7 次结束回合、16 条独立分支和两类候选通过。
[`limited-range-damage.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-room-abilities/limited-range-damage.mt2f)

原版 Eclipse Chamber 与 Lunacy 同场验证房间月相掩码、手牌临时升级以及 X=2、3、0 的实际付费。
20 次出牌、5 次结束回合、16 条独立分支和两类候选通过。
[`lunacy-eclipse-mask.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-room-abilities/lunacy-eclipse-mask.mt2f)

原版法术和单位升级计数遗物在 2.3.1 中各使用实际取得的定义。整战记录 44 次修饰回调和
26 次原版筛选，覆盖 0、1、2 个可见升级、重置、重复及逐件对照。
23 次出牌、7 次结束回合及候选检查通过。
[`battle.mt2f`](../tests/compatibility/game-2.3.1/mechanics/original-upgrade-count-relics/battle.mt2f)

腐化遗物批次观察到原版为楼层敌军增加两层 Corrupt Poison。部署结束后的两张零费测试牌
单独核对，其余状态和随机流逐项比较。19 次出牌、6 次结束回合及候选检查分段通过。
[`battle.mt2f`](../tests/compatibility/game-2.3.1/mechanics/corruption-relic-trigger/battle.mt2f)

镜厅组件记录的付费接枝出生和复制体各有独立装备。复制体的来源卡保留本体装备引用，
实际绑定使用独立的装备实例。专项核对物理槽位、随机流和原版结算状态下的结束回合。
[`observation.mt2f`](../tests/compatibility/game-2.3.1/mechanics/hall-grafted-birth/observation.mt2f)

原版回调补采记录了先复制来源卡、再创建复制单位、最后复制并绑定接枝装备的顺序。
两件装备的对象引用各自独立，复制单位的来源卡仍指向本体接枝。同场核对了物理槽位、
原版随机流及归档结算状态上的结束回合。
[`callback-order.mt2f`](../tests/compatibility/game-2.3.1/mechanics/hall-grafted-birth/callback-order.mt2f)

新版镜厅采样中，付费登场的原版 Twisted Assistant 携带一件未改动接枝装备，
出牌预测与完整原版结算状态逐项一致。串行和八条并行分支核对随机流、父状态和接枝引用。
普通镜厅、房间替换及五种原版接枝装备的生命周期专项也通过。生成或触发召唤仍需要各自的出生证据。
[`predicted-birth.mt2f`](../tests/compatibility/game-2.3.1/mechanics/hall-grafted-birth/predicted-birth.mt2f)

DoubleUnits 原版对照记录了付费单位、首个镜厅复制体、额外召唤的接枝单位和第二个复制体。
四件接枝装备分别绑定各自的单位，三个脱离牌组的来源牌仍引用原接枝装备。
定向复放核对完整结算、物理站位、回合触发计数及 Battle 和 CardDraw 随机流。
采样使用 `-OriginalHallExtraSpawn -BinaryCapture`，专项使用 `--native-hall-extra-spawn-only`。
其他触发召唤、状态替代和不同接枝组合仍需对应的原版差分。
[`extra-spawn.mt2f`](../tests/compatibility/game-2.3.1/mechanics/hall-grafted-birth/extra-spawn.mt2f)

普通单位对照在连续两回合使用长矛列车员和角魔战士。每回合首张付费单位生成两个，
第二张生成一个。四次完整后继核对了独立的来源卡、出生点上限和回合重置。
采样使用 `-OriginalDoubleUnitsOrdinary -BinaryCapture`，专项使用 `--native-double-units-ordinary-only`。
[`ordinary-extra-spawn.mt2f`](../tests/compatibility/game-2.3.1/mechanics/hall-grafted-birth/ordinary-extra-spawn.mt2f)

Eager Conscript 的原版对照确认，DoubleUnits 将首次状态量从 1 提到 2。首张生成一只带
2 层 Horde 的 Funguy，第二张给同一单位加 1 层。两次完整结算及随机流均与模拟一致。
采样使用 `-OriginalDoubleUnitsSpawnOrStatus -BinaryCapture`，专项使用 `--native-double-units-spawn-or-status-only`。
[`spawn-or-status.mt2f`](../tests/compatibility/game-2.3.1/mechanics/hall-grafted-birth/spawn-or-status.mt2f)

Kinhost Vessel 的原版对照在部署回合先消耗一次 DoubleUnits，随后在第一普通回合通过
自然孵化生成两只 Pupa。定向检查核对孵化回调、站位、来源牌、随机流、回合计数重置和完整
结束回合。采样使用 `-OriginalDoubleUnitsTriggeredSpawn -BinaryCapture`，专项使用
`--native-double-units-triggered-only`。
[`double-units-triggered.mt2f`](../tests/compatibility/game-2.3.1/mechanics/hall-grafted-birth/double-units-triggered.mt2f)

40 张牌的原版 Funguy 遗物在首个普通回合执行六次数量查询。DoubleUnits 只增加第一次，
顶层、中层和底层分别结算为 3、2、2 层 Horde。独立检查核对完整遗物回调与结束回合。
采样使用 `-OriginalFunguyDeckScalingDoubleUnits -BinaryCapture`，专项使用
`--native-funguy-deck-double-units-only`。
[`funguy-deck-scaling-double-units.mt2f`](../tests/compatibility/game-2.3.1/mechanics/funguy-deck-scaling/funguy-deck-scaling-double-units.mt2f)

FirstUnitGainDamageShield 的原版对照中，付费单位获得 2 层伤害护盾，额外单位只获得
Cardless。第二张付费单位维持原有触发计数。定向检查重放两次出牌、楼层切换与完整 RNG。
采样使用 `-OriginalDoubleUnitsFirstShield -BinaryCapture`，专项使用
`--native-double-units-first-shield-only`。
[`first-shield.mt2f`](../tests/compatibility/game-2.3.1/mechanics/hall-grafted-birth/first-shield.mt2f)

SpawnWithArmor 的原版对照中，首次付费召唤的两个单位都获得 5 层护甲。同回合的下一张
付费单位也获得 5 层。定向检查核对两个完整后继和随机流。
采样使用 `-OriginalDoubleUnitsSpawnArmor -BinaryCapture`，专项使用
`--native-double-units-spawn-armor-only`。
[`spawn-armor.mt2f`](../tests/compatibility/game-2.3.1/mechanics/hall-grafted-birth/spawn-armor.mt2f)

2026-11-21 用同一场新版原版胜局复测 `RootBeforePyre`、`SpellWeaknessBelowPyre` 和
`PyregelOnEnterTopFloor`。三件遗物在入场前按原版流程获得。第三层分别有 2、3、3 名
敌军出现相应状态。21 次出牌、7 次 EndTurn、59 个房间阶段、14 个列车阶段、完整链和
16 条分支、合法候选及串并行计划全部通过。两版专用原生检查都通过，归档在
`tests/compatibility/game-2.3.1/mechanics/room-entry-status-relics/`。

新版 `AddHoldoverToSpells` 的原版对照覆盖抽牌后检查与逐张进手牌的两条回调。原版
Torch 自动获得留存，打出时在弃牌堆标记待返回，下回合回手后清除标记。另有四次
原版生成 Sting 获得留存、十三次不合格卡回调、五次抽牌后回调。隔离胜局包含四次
出牌、五次 EndTurn、九次牌堆切换检查，整链、16 条分支、合法候选及串并行计划通过。
原版遗物与突变器参数在采样前后保持一致。归档位于
`tests/compatibility/game-2.3.1/mechanics/holdover-grant/`。

2.3.1 的 `CopyConsumedCard` 在手牌已满时停止复制。满手打出原版 Space Prism 后只补入
一张，抽牌堆保持原顺序，随后打出复制牌仍受每场一次的限制。零空位的模型检查也验证
不分配新卡编号、不消耗触发次数。旧版两张复制样本通过。新版胜局中 23 次出牌、7 次
EndTurn、60 个房间阶段、14 个列车阶段、录制候选及串并行计划通过。归档位于
`tests/compatibility/game-2.3.1/mechanics/copy-consumed-card/`。

满月法强遗物在 2.3.1 扩大了目标效果范围。原版 Divine Punishment 满月获得伤害和治疗
各 +10，新月的同牌和不匹配的对照牌没有获得升级。该牌用于观察授予，本场未打出；
原生 `CardEffectDamage` 的结算路径使用升级后的伤害参数。26 次出牌、7 次 EndTurn、
58 个房间阶段及 14 个列车阶段通过。七个分段各自复放，另核对六处插牌布置和原始
连续性差异。旧版专用检查通过。归档位于
`tests/compatibility/game-2.3.1/mechanics/moon-phase-hand-upgrade/`。

2.3.1 的衰亡死亡遗物会把层数分给同层可选敌军。原版验证涵盖双方死亡、跨层、不可选、
无接收者及原单位保留层数。5 层分给两名敌军时每名获得 3 层。完成 108 次原生触发
判定和三次结算对照，布置用单位在首个录制决策前清除。胜局的五次出牌、七次 EndTurn、
61 个房间阶段、14 个列车阶段以及 16 条分支、合法候选和串并行计划都通过。
2.2.1 的同阵营转移回归也通过。归档位于
`tests/compatibility/game-2.3.1/mechanics/decay-death-hero-targets/`。

2.3.1 的 HephsBlueprints 原版装备池加入 Spiteful Shackles、Zealot's Halo 和 Sineater。
三件新增项和旧版对照 Soulsmelter 花费 3 锻造点，取得通用升级及遗物额外的 6 点伤害和
6 层护甲。TrainStewardSpear 仅取得通用升级，PenitentsDeathsuit 的原版定义和拥有实例
均被遗物筛选排除。六项原生组件对照均通过，旧版整局仍通过。
归档位于 `tests/compatibility/game-2.3.1/mechanics/hephs-blueprints-equipment-pool/`。

新版高费永冻遗物的升级带有复制排除标记。原版复制筛选和模型均排除来源卡上的该升级，
满足费用条件的副本仍可独立获得升级。专项检查覆盖 7 次添加、1 次费用下降后移除升级但
保留冻结、1 次重抽、6 次查询及两次预演。四次出牌、五次 EndTurn、39 个房间阶段、
九个列车阶段通过。SuperUltra 的胜局正常退出，旧版归档也通过。
归档位于 `tests/compatibility/game-2.3.1/mechanics/expensive-card-permafrost/`。

同一轮隔离采样对照四件变更遗物。九项原生调用验证新版旗帜排除池、两张新增起始牌、
X 费筛选和 Endless 状态。五项安装升级，其中三项的升级被原版复制筛选排除。模型的
有序筛选和卡牌后继与实测一致。归档位于
`tests/compatibility/game-2.3.1/mechanics/changed-temp-upgrade-relics/`。

## 工作单位

2026-11-20 接入[回合前原生顺序分派](before-draw-order.md)：十二种已有 BeforeDraw
路径按遗物／效果索引逐项执行，保留完整被动列表；前排生命成长和类型序列
复位移至原生较早阶段。两件未改定义原版遗物交换取得顺序，三层受伤冠军
同样治满，首回合能量分别为 4／3；两场胜局、各 6 出牌／5 EndTurn／42 房间／
9 列车阶段／138 完整上下文四项零，完整链／16 分支／候选／串并行根搜索通过。
16 路并行顺序对与同一遗物多效果选择通过，二进制 73,247 字节。
多项既有机制通过定向回归。固定八场阶段验收全部通过，覆盖 190 次出牌与 52 次 EndTurn。

2026-11-20 新增[Relentless 抽牌与 Warborne 高等级组合](relentless-draw-mechanism.md)：
两个原版来源／10 配置／640 条原生回调、同量不可变后继与 16 路并行数值检查
通过，覆盖开局／抽牌前缓存、双方物理死亡占位、薪火排除与 int→float／整数
边界。原版 T2／T3 两场胜局合计 14 出牌／10 EndTurn／83 房间／18 列车阶段／
278 完整上下文四项零，T3 同回合第十次出牌实际抽两张，复用已有通用消费者。
每场完整链／16 分支／全部录制候选／串并行根搜索及计划复放通过。
原生矩阵 0.003 秒，两场 78.00／49.17 秒；三份二进制 87,926 字节。
T1/T2/T3 的基础组合已有各自原生整场证据；较晚回合前回调仍受顺序保护，
其他牌组与复杂生命周期继续扩展。

2026-11-15 新增[敌方占层抽牌缓存共用机制](occupied-floor-draw-mechanism.md)：
Warborne 三个原版来源／2,304 条原生回调及同量不可变后继、16 路并行数值检查
通过，保留逐层 EndTurn、抽牌后与玩家阶段敌方出生／死亡的不同刷新时点。
原版 T1 整场胜局、4 次出牌／5 次 EndTurn、42 房间／9 列车阶段／134 上下文
四项零，玩家阶段击杀使缓存 1→0，完整链／16 分支／全部录制候选／串并行
根搜索与计划复放通过。原生矩阵 0.013 秒、整场 43.59 秒；二进制 51,584 字节。
较高等级的基础组合已由上条另行验收，复杂组合仍继续扩展。

2026-11-15 新增[递减与占层回合能量共用机制](turn-energy-mechanism.md)：
六个原版灵魂／90 组配置／2,643 次原生调用，覆盖陈旧回合缓存、零奖励仍刷新、
物理占位与存活容量差异及两笔能量的独立截断；63 条原生溢出控制显式拒绝。
未改定义的 SolarisT1／WormgutT3 与扩容遗物整场胜局、4 次出牌／5 次 EndTurn、
41 房间／9 列车阶段四项零；容量 20 额外奖励、递减归零后的缓存、完整后继／
16 分支／候选前沿／串并行根搜索通过。原生矩阵 0.012 秒、整场 43.29 秒；
二进制 53,985 字节。Warborne 抽牌缓存已由上条另行补齐。11 月 20 日接入了
[抽牌前统一分派](before-draw-order.md)，原版交换回调顺序的两组样本分别得到 4 和 3 余烬。

2026-11-15 修正[原生清单](inventory/game-2.3.1-mechanisms.md)的引用口径：448 类有
目录引用，其中 441 类含类型声明，七类仅见于过滤器、通用参数等引用。
例如 NextMonster 状态效果的五个引用全部是升级过滤条件；尚未捕获实际执行
定义，先查入口，不优先为它人工造整局。默认 SourceTrait／TargetTrait 同样
不能当作已配置特性。[定向检查](../scripts/Check-MechanismReferences.ps1)保留
七个固定反例、四个声明正例和混合角色检查。该调整不增加支持率、不删除
源码已知的潜在缺口，也不据目录缺失认定原生类未使用。

2026-11-22 在隔离原版为效果类补做直接使用位置查询。遍历 724 个卡牌、406 个角色、
920 个升级和 676 条遗物集合条目的已配置效果列表。`CardEffectAddStatusEffectNextMonster`
在这个范围内零命中。正对照 `CardEffectGrantEquipmentFromPool` 找到 Red Crown 升级的
`OnSpawn` 效果。两次查询均在部署后首次可操作状态完成，决策、帧号及所有随机流均一致。
已登记 [两份原版归档](../tests/compatibility/game-2.3.1/mechanics/effect-owner-query/manifest.tsv)。
可用 `Run-FullBattleProbe.ps1 -EffectOwnerType <类名> -Policy no-cards -BinaryCapture`
复查，并可加载原版战前档。动态构造和晚期调用仍需要战斗记录来确认。

2026-11-24 同一原版进程批量查询三个类，并多检查 71 个灵魂升级。`CardEffectDamagePerTargetAttack`
找到 Little Fade 三档原版死亡触发升级。`CardEffectDamageWithTrample` 在已扫描的定义效果表中为零。
正对照 `CardEffectDamageByPercentRemainingHealth` 在三个 Beeziebub 灵魂与其升级目录各命中一次，
回调均指向薪火。决策状态、帧号和随机流在各次查询中保持一致。批量调用的类名用逗号分隔。
[批量查询归档](../tests/compatibility/game-2.3.1/mechanics/effect-owner-query/manifest.tsv)

同日追加原版卡牌初始升级和 26 棵冠军奖励树。三条 Little Fade 伤害记录只命中通用升级目录。
Little Fade 的原版实测升级树提供强化友军、增加自身费用及永久尖刺。原版状态效果正对照在冠军奖励树命中
69 条。首轮扫描据通用升级目录将三条伤害记录标为实战缺口，这项结论以追加的挂载查询结果更正。
[冠军奖励树归档](../tests/compatibility/game-2.3.1/mechanics/effect-owner-query/target-attack-reward-scan.mt2f)

2026-11-15 新增[出生状态层数缩放](spawn-status-scaling-mechanism.md)：
四个原版来源、54 组配置、2,592 次原生出生回调通过，约 0.343 秒；区分按
原始定义追加与运行时触发器重复缩放，并修正附魔采集丢失运行时参数的问题。
未改定义的原版无尽变异器在薪火创建前取得，受控连续胜局四项零、42 房间／
九列车阶段、16 条分支、完整候选前沿和串并行根搜索通过。其他来源伴随效果、
复杂状态回调及缩放附魔实战生命周期仍有边界，不据此推算全游戏完成率。

2026-11-15 新增[按牌堆数量授予出生升级](pile-spawn-upgrade-mechanism.md)：
四个原版定义、52 组配置、3,744 条原生升级请求及九类真实计数通过，原生矩阵
约 0.027 秒。不改定义的原版变异器胜局匹配三次出生的逐次升级记录、完整
后继、16 条分支、候选前沿和串并行根搜索；三个灵魂的携带流程与复杂升级
交互仍有边界。状态层数出生缩放已在上项单独验收，不按本组推算整体完成率。

2026-11-15 新增[薪火受伤加成](pyre-damage-taken-mechanism.md)共用路径：
四个原版定义参数、40 次回调及 650 组有序管理器对照，原生矩阵约 0.015 秒。
不改定义的 +3 变异器连续胜局验证七次伤害／护甲／护盾控制与自然敌人攻击；
完整后继、16 条分支、录制候选前沿及串并行根搜索通过。
三个灵魂的伴随效果和 Covenant 混用仍有边界，不按组件数量推算整体完成率。

2026-11-15 新增 [Juice 来源升级回调](juice-upgrade-mechanism.md)：五个原版定义，
540 组关系控制、重复调用和 300 组有序组合，1,680 次原生回调约 0.067 秒。
一个连续胜局验证六次两张牌抽取、深复制、过滤短路与共享原始对象的后续变化；
八次出牌、完整后继、16 条分支、候选前沿及串并行搜索通过。
参数升级 cast effects 与其他混合缩放仍是明确边界。

2026-11-15 新增[晚期攻击倍率](late-attack-mechanism.md)的共用适配。
一次原生批处理完成 640 组查询、80 组叠乘与 640 组实际攻击，约 0.017 秒；
另复用百分比生成器验证非中性倍率下的 7,728 对。两件不改定义的原版变异器
组合整局四零、16 条分支及串并行搜索通过。日常复用二进制证据，不按参数
重新录整局；该交付不等于全部内容与交互完成。

2026-11-15 新增[出生百分比攻血](percentage-stat-mechanism.md)的共用规则与整局接入。
自动发现六个原版定义中的 56 处效果，138 组配置、7,728 组查询／出生回调对通过；
原生矩阵 0.240 秒。原版双倍敌方生命变异器的连续胜局、七只敌方正例、16 条分支、
完整候选前沿及串并行根搜索通过。其余变异器伴随效果仍有边界，
不据组件矩阵推算整局覆盖率。

2026-11-15 已把首个旧式按内容放行的路径改为机制结构契约：
[共用状态倍率](juice-mechanism.md)用一套 2／3／4 倍规则、现有 OnCast 分派器和
一个简单状态法术契约复用不同卡牌身份。五个原版升级的 180 组回调对与九次
原版升级法术实际施放均匹配；检查还替换身份验证无需新增卡牌 ID 白名单。
后续同类适配优先移除这种重复工作，只有新回调／源码分支或实际差异才新增
逻辑。参数升级 cast effects 与额外有序缩放等契约外路径保持显式缺口，避免用放宽保护冒充完成。

按“原生实现类＋参数分支＋触发位置”推进，复用模拟器的公共目标选择、费用、
伤害／治疗、升级、出生／死亡、队列与 RNG 管线。共享同一实现的卡牌与遗物
归在一起；单张内容只保留数据差异和原版身份的证据。

已经生成[原生类清单](inventory/game-2.3.1-mechanisms.md)和
[可排序 TSV](inventory/game-2.3.1-mechanisms.tsv)。它直接读取原版 DLL 元数据与
三份固定版本的二进制目录，不启动游戏、不执行原生代码、不依赖 JSON。
734 个具体类中，448 个能关联到当前目录中的定义。它们包括大量已有实现；
这些数字是代码审查范围，**不是剩余待实现数量或完成率**。清单另列类型声明、
选择条件、参数和描述引用；含类型声明也仍需确认实际执行入口。

现有代码通过[机制对账表](inventory/game-2.3.1-reconciliation.md)与
[可排序队列](inventory/game-2.3.1-reconciliation.tsv)定位。工具读取编译后的
Model／Probe／ModelChecks IL、原生父类和状态 ID，再用匹配的 PDB 校验当前
源码；可找到归一化入口和继承实现，避免按类名漏搜后重写。当前 461 类有
模型直接／继承／状态 ID 代码链接，497 类有探针链接，414 类有检查器链接；
这些链接用于导航。当前 76 项人工审查区分战前输入标记、
共享伤害路径、两类战后消费者，以及未建模项和局部实现的剩余边界。其余类继续按队列审查，
不将 734 全部列为待实现，也不将没有类名引用的条目自动认作缺失。

```powershell
# 重建代码并生成对账；不启动游戏
pwsh -NoProfile -File scripts/Get-MechanismReconciliation.ps1
# 仅核对当前已构建的代码、源码与清单是否一致
pwsh -NoProfile -File scripts/Get-MechanismReconciliation.ps1 -SkipBuild -Check
```

人工声明位于 `tests/compatibility/game-2.3.1/mechanism-reviews.tsv`，模型／
采集方法和定向检查器必须存在，结论必须写清边界。已验证源码漂移、错配
PDB、过期消费者和失效方法四种拒绝；故障输入均恢复原字节后再次核对通过。
这一层先交付有限、可定位的审查队列；完整剩余实现数仍需逐组审查，不能把
当前 known-gap 条目数当作剩余实现总数。

各类记录原生覆盖方法、引用者数量、已捕获参数配置数和出现位置，便于先看
影响最多定义的公共实现。参数配置不等于代码分支；继承方法也需检查。
目录只捕获六种完整状态定义，所有 104 种状态实现类已列出；还需补齐状态
参数和其他引擎服务的盘点。`RuntimeEvidence=unmapped` 表示尚未关联已有证据，
不能解释为“不支持”，也不能因目录没有保护项便认定支持。

首组两类失血攻击查询已关联 `native-query-fixture` 证据，带归档路径、检查器和
未完成整合边界。[证据索引](../tests/compatibility/game-2.3.1/mechanism-evidence.tsv)
由盘点工具核对版本、SHA-256、源定义、矩阵大小和状态还原。该标记表示存在
有范围的原生查询归档；当前代码是否通过仍需执行链接检查器，不能算整类
生命周期完成或新版总覆盖率。其余已有证据仍需逐组关联。

重建／核对：

```powershell
pwsh -NoProfile -File scripts/Get-MechanismInventory.ps1
pwsh -NoProfile -File scripts/Get-MechanismInventory.ps1 -SkipBuild -Check
```

可以用 `-GameAssembly` 指向同 MVID 的隔离副本。版本不匹配直接拒绝，避免
把新版本类型和旧版本原生证据混算。

## 固定验收关口

2026-11-24 的阶段回归通过 483 场已结算战斗，含 464 场连续复放和 19 场经过验证的分段复放。
共核对 7,519 次出牌或技能和 2,884 次结束回合，差异为 0。另有 228 项登记是组件归档或未结算样本。
固定的八场搜索验收也全部通过，覆盖 190 次出牌或技能、52 次结束回合及串并行计划复放。

随后登记的六场原版战斗在同一构建下通过定向连续复放，合计 92 次操作和 36 次结束回合。
两类候选检查均通过，归档索引增至 489 场已结算战斗。2.3.1 的 601 张符合统计口径的卡牌都有
定义匹配的原版成功出牌，236 件收藏遗物都有有序效果一致的在场记录。整战回归和专项触发证据见各自入口。
[六场定向报告](../results/recorded-chain-triage-20261124-220236-2f1a671e/report.tsv)

Windows 包通过隔离验收后，阶段回放核对全部 489 场已结算战斗，其中 470 场连续通过，19 场分段通过。
共核对 7,611 次出牌或技能、2,920 次结束回合，失败为 0。另有 273 份组件及未结算归档按清单跳过。
[整战阶段报告](../results/recorded-chain-triage-20261124-232728-007d6281/report.tsv)

2026-11-25 的阶段回放在代码提交 `2cf96e48` 上通过 492 场已结算战斗，其中 473 场连续通过、
19 场分段通过。共核对 7,637 次出牌或技能和 2,935 次结束回合，失败为 0。登记的 274 份组件及
未结算归档按清单跳过。三场新增召唤回放另通过两类候选检查，共 26 次操作和 15 次结束回合。
[整战回放](../results/recorded-chain-triage-20261125-001454-8058fffd/report.tsv) ·
[候选定向回放](../results/recorded-chain-triage-20261125-001124-f776a258/report.tsv)

同批次求解验收通过。固定版本的 601 张统计范围内卡牌均有定义匹配的原版成功出牌，
236 件收藏遗物均有顺序一致的原版效果在场记录。尖刺工厂的弹窗在最新 Advisor 构建上再次通过隔离实机验收。
GrimFlight 验收程序已就绪，正向运行需要原版客户端确认 Punished DLC 已安装且本局启用。
[求解验收日志](../results/solver-completion-release-2cf96e48.log) · [GrimFlight 验收说明](summon-choice-battle.md#正版-dlc-环境中的顾问交接)

同日以 `49423b7e` 再次执行阶段验收。497 场已结算战斗全部通过，其中 478 场连续复放、
19 场分段复放，共核对 7,714 次战斗操作与 2,964 次结束回合，失败为 0。
阶段求解验收核对了 2,875 份二进制归档的注册及哈希，并确认 601 张卡牌均有定义匹配的原版出牌，
236 件收藏遗物均有按原版效果顺序匹配的在场记录。供奉及 GrimFlight 的玩家界面正向验收仍需
已安装并启用 Punished DLC 的原版战役环境。
[整战阶段报告](../results/recorded-chain-triage-20261125-022422-c0ec996b/report.tsv) ·
[阶段求解日志](../results/solver-completion-20261125-advisor-boundaries.log)

`aac6ea21` 的整战阶段复放通过 501 场，其中 482 场连续复放、19 场分段复放。
八路并行复放共核对 7,734 次战斗操作和 2,985 次结束回合，失败为 0。
同一版本的求解验收通过跨回合搜索、串并行对照及 2,875 份归档登记。覆盖清单确认
601 张收藏卡均有定义匹配的原版出牌，236 件收藏遗物均有按效果顺序匹配的原版在场记录。
[整战阶段报告](../results/recorded-chain-triage-20261125-042106-0de57402/report.tsv) ·
[阶段求解日志](../results/solver-completion-20261125-aac6ea21.log)

2.3.1 原版 `DoubleStatusEffects` 已接入共享状态层数管线。原版 SpikePotion 支付 1 余烬，
使火车乘务员获得 20 层尖刺，完整出牌后继和随机流通过复放。七项查询对照覆盖冷却、无目标、
永久升级、延迟加法和转移来源。四项原版移除调用依次将尖刺从 20 变为 18、14、10、12，
其中最后一次以卡牌升级为来源，传入 `-1`。
[原版专项归档](../tests/compatibility/game-2.3.1/mechanics/double-status-effects/native-spike-and-removal.mt2f)

同一变异器启用后，常规出牌策略在原版完成整场胜局，结算时薪火剩余 72 HP。连续复放的 20 次出牌
和 7 次结束回合均匹配，两类决策起点的候选枚举均通过严格检查。
[原版整战归档](../tests/compatibility/game-2.3.1/mechanics/double-status-effects/native-win.mt2f)

原版 `LonelyRooms` 在各楼层建立物理槽位时将玩家槽位上限设为 1。分支复用原版快照中的槽位及占用，
并按各楼层独立记录的容量规则判定出牌。原版胜局的 21 次出牌、7 次结束回合和两类候选枚举均匹配，
结算时薪火剩余 58 HP。
[原版整战归档](../tests/compatibility/game-2.3.1/mechanics/lonely-rooms/native-win.mt2f)

原版 `Echoes_DoubleFriendlyUnitHealthUnhealed` 在实体出生回调中将火车乘务员从 25/25 改为 25/50，
来源卡牌的临时生命增加 25。完整出牌后继、随机流和搜索候选均通过定向对照。
[原版出生归档](../tests/compatibility/game-2.3.1/mechanics/double-friendly-health/native-steward-birth.mt2f)

同一变异器的原版胜局中，21 次出牌、7 次结束回合和两类候选枚举均匹配，结算时薪火剩余 66 HP。
[原版整战归档](../tests/compatibility/game-2.3.1/mechanics/double-friendly-health/native-win.mt2f)

原版 `ShuffleUnits` 的部署结束回合保持原阵型。进入普通回合后，两单位对照覆盖友军和敌军换位，
三单位对照记录顶层槽位从 `[2, 3, 4]` 变为 `[4, 2, 3]`，并保留每次抽样和移动回调。
两组的部署结束与普通结束回合均通过完整后继对照，移动回调分别记录 8 和 10 次。
[双单位对照](../tests/compatibility/game-2.3.1/mechanics/shuffle-units/native-first-turn-projected.mt2f) ·
[三单位对照](../tests/compatibility/game-2.3.1/mechanics/shuffle-units/native-three-hosts.mt2f)

第 3 回合中层的原版记录显示，友军换位后也会整理敌方的空槽。模型在位移回调前整理两队槽位，
随后重新录制原版胜局。18 次出牌、6 次结束回合、49 个房间阶段和 11 个列车阶段均匹配，
两类候选枚举通过严格检查，结算时薪火剩余 80 HP。
[原版整战归档](../tests/compatibility/game-2.3.1/mechanics/shuffle-units/native-settled-win.mt2f)

原版 `BothSidesUnitsAreStronger` 的付费部署和第一波结算逐项匹配。原版敌军出生记录观察到
生命和最大生命增加 10，后续触发增加 5 点攻击。共享薪火攻击查询和刷新都从 45 得到 55。
原版胜局的 21 次出牌、7 次结束回合及两类候选枚举通过严格检查，结算时薪火剩余 56 HP。
[首波对照](../tests/compatibility/game-2.3.1/mechanics/both-sides-units/native-first-wave-projected.mt2f) ·
[原版整战归档](../tests/compatibility/game-2.3.1/mechanics/both-sides-units/native-win.mt2f)

原版 Eager Conscript 支付 1 余烬后召唤无来源卡牌的 Funguy。三个回调依次把生命从 4/4 改为 14/14，
再各加 5 点攻击，最终为 13 攻。完整出牌后继和随机流匹配。
[无卡出生对照](../tests/compatibility/game-2.3.1/mechanics/both-sides-units/native-cardless-funguy.mt2f)

原版 Pyre Steward 技能先对薪火造成 3 点伤害，再按薪火实际显示攻击对同层两名敌人各造成 45 点伤害。
两次击杀治疗将薪火生命从 77 依次恢复至 79、80。模拟在延迟死亡回调前保留跨层治疗，完整技能后继和随机流与原版一致。
[技能专项归档](../tests/compatibility/game-2.3.1/mechanics/pyre-steward-damage/native-first-activation.mt2f)

两档原版敌方首次出生复生分别赋予 1 层和 2 层复生。同一回合后续敌人跳过，抽牌后原版重置触发标记。
两档各自通过 3 个出生阶段和连续两个结束回合的完整复放。每次实际授予推进一次 Battle RNG，跳过和重置保持随机流。
[一层复生对照](../tests/compatibility/game-2.3.1/mechanics/front-reanimation/native-tier2-settled.mt2f) ·
[两层复生对照](../tests/compatibility/game-2.3.1/mechanics/front-reanimation/native-tier3-settled.mt2f)

原版 Zariel 的三档技能从 10 层 Pain 分别获得 10、20、30 点伤害。目标初始为 25 护甲、5 生命。
前两档结算后护甲分别剩 15、5，第三档结算时目标移出战场。三档部署与技能后继均通过精确复放，技能各消耗 0 余烬并进入 1 回合冷却。
[一档对照](../tests/compatibility/game-2.3.1/mechanics/zariel-pain/native-tier1-observation.mt2f) ·
[二档对照](../tests/compatibility/game-2.3.1/mechanics/zariel-pain/native-tier2-observation.mt2f) ·
[三档对照](../tests/compatibility/game-2.3.1/mechanics/zariel-pain/native-tier3-observation.mt2f)

Zariel 的 Devotion 路线在法术支付并结算后读取当前等级。原版一档由 Venerate 先升到 1 级，
再对前排造成 12 点伤害，护甲从 25 降至 13，生命保持 5，伤害来源是 Zariel 的部署卡。
部署和付费施法的完整后继、随机流均通过精确复放。三档共用同一路径，原版定义系数为 6、8、10。
[原版一档对照](../tests/compatibility/game-2.3.1/mechanics/zariel-devotion/native-tier1.mt2f)

原版 `Echoes_NoRoomOverriding` 会将安装的主房间固化。一次付费触发原版房间复制遗物，
三个普通楼层各增加 4 点容量并留下一个固化房间。完整付费后继、随机流和三个楼层的替换判定一致。
[原版房间固化对照](../tests/compatibility/game-2.3.1/mechanics/embedded-room/native-placement.mt2f)

隔离实例通过原版无尽循环入口进入第 1 轮，记录了首波三个原版敌军的出生回调。
两名重装敌人由 3 攻、90 血变为 5 攻、120 血，侍僧由 0 攻、30 血变为 0 攻、120 血。
之后从第八环原版检查点复放 48 个输入并获胜，经原版胜利按钮进入无尽。接续该存档采样后，
同样的三个出生回调、六个原版倍率和回调前后随机流逐项通过。顾问通过读取原版倍率和角色基准值，
对同一原版首波重现了完整部署结束回合，还匹配了扩大的战后伤害、增益和治疗。超出已校准的出生类型会截断预测。
同一战斗的六个已录制结束回合全部通过独立复放，共有 21 次有序出生回调。最后一波首领由 7 攻、125 生命变为 20 攻、2,250 生命，原版保留 1 层无情。
从部署一直录到战败结算，36 次出牌、9 次结束回合、16 条独立分支及两类候选枚举均通过，采样四项计数为零。
[原版胜利入口](../tests/compatibility/game-2.3.1/mechanics/endless-enemy-scaling/native-victory-transition.mt2f) ·
[原版无尽首波](../tests/compatibility/game-2.3.1/mechanics/endless-enemy-scaling/native-victory-first-wave.mt2f) ·
[原版首波预测对照](../tests/compatibility/game-2.3.1/mechanics/endless-enemy-scaling/native-victory-projected-first-wave.mt2f) ·
[原版六波预测对照](../tests/compatibility/game-2.3.1/mechanics/endless-enemy-scaling/native-victory-six-waves.mt2f) ·
[原版首战至终局](../tests/compatibility/game-2.3.1/mechanics/endless-enemy-scaling/native-victory-origin-defeat.mt2f) ·
[受控无尽首波](../tests/compatibility/game-2.3.1/mechanics/endless-enemy-scaling/native-controlled-first-wave.mt2f)

从原版胜利后的检查点受控推进至第 2 环，首波四名敌军的完整结束回合通过独立复放。
附魔师的护甲从 120 到 240，泰坦皮肤从 15 到 30，附魔向三名追猎者各授予 10 层泰坦皮肤。
遗物将后排追猎者移到前排，房间名单、物理槽位、附魔目标及随机流均与原版一致。
[第 2 环受控原版对照](../tests/compatibility/game-2.3.1/mechanics/endless-enemy-scaling/native-controlled-ring2-first-wave.mt2f)

无尽第 3 环受控首波复放覆盖三名常规敌人与外侧 Boss Arkion。Arkion 在顶层出生后结算为
40 攻击、6250 生命，推位参数保持 1。原版出生前后、初始行动、随机流和部署结束后的完整状态均匹配。
[第 3 环首波](../tests/compatibility/game-2.3.1/mechanics/endless-enemy-scaling/native-controlled-ring3-first-wave.mt2f) ·
[Arkion 出生](../tests/compatibility/game-2.3.1/mechanics/endless-enemy-scaling/native-controlled-ring3-arkion-birth.mt2f)

沿同一受控起点连续执行 4 次 EndTurn 到原版败局。完整复放核对 Arkion 行动控制器、物理位置、随机流及结算，
16 条独立分支和结束回合候选通过。该路线只使用结束回合。
[第 3 环原版败局](../tests/compatibility/game-2.3.1/mechanics/endless-enemy-scaling/native-controlled-ring3-arkion-defeat.mt2f)

主菜单的原版查询批量记录无尽前 3 轮共 24 个环位，每个环位核对攻击、生命、状态、增益、治疗和伤害六个倍率。
90 种状态中有 26 种带无尽缩放资格，查询前后确定性随机流一致。
[原版倍率归档](../tests/compatibility/game-2.3.1/mechanics/endless-enemy-scaling/native-balance-factors.mt2f)

在原版胜利后的隔离存档上调用原版换轮入口，进入受控第 2 轮首环。完整部署后继匹配五次出生。
两名 Horde 的死亡腐化从 3 层增至 15 层，重装敌人的战后自伤和增攻缩放为 200 和 20，
其 1 层多重攻击与出生回调随机流一致。
[第 2 轮首波归档](../tests/compatibility/game-2.3.1/mechanics/endless-enemy-scaling/native-controlled-loop2-first-wave.mt2f)

同一第 2 轮首环完成 14 次真实出牌和 2 次 EndTurn。第 3 回合的 Torch 击杀剩 3 生命的 Horde，
同层前排 Shardsoul Carver 获得 15 层腐化。16 个完整结算快照与模型逐项匹配。
[第 2 轮付费击杀归档](../tests/compatibility/game-2.3.1/mechanics/endless-enemy-scaling/native-controlled-loop2-horde-death.mt2f)

从部署到原版败局的整战包含 14 次出牌及 4 次 EndTurn。连续后继、终局、随机流和出牌／结束回合候选均通过。
[第 2 轮原版败局](../tests/compatibility/game-2.3.1/mechanics/endless-enemy-scaling/native-controlled-loop2-defeat.mt2f)

独立构建的玩家版在受控第 2 轮原版部署界面显示了部分方案和首步出牌建议。实机检查核对了渲染、
建议对应局面、战斗状态及随机流，采集中出牌与结束回合输入均为零。
[玩家版原版部署归档](../tests/compatibility/game-2.3.1/mechanics/endless-enemy-scaling/native-controlled-loop2-advisor.mt2f)

另一次玩家版实测接受首步出牌后立即显示临时建议。原版结算匹配预测后，顾问确认并显示剩余 16 步，
搜索次数仍为 1。该验收执行一次原版出牌，结束回合输入为零。
[玩家版出牌接续归档](../tests/compatibility/game-2.3.1/mechanics/endless-enemy-scaling/native-controlled-loop2-advisor-handoff.mt2f)

三阶原版 Silver 灵魂的薪火升级由 `SaveManager.BattleComplete` 在战斗终局快照之后调用，
同一灵魂的额外奖励在战后生成。顾问识别原版灵魂和完整有序效果列表，并在下一场读取薪火的实际数值。
Silver T1、T2、T3 分别用两份真实胜局后的隔离存档调用原版调度器。三阶各观察到一次生命分支和一次攻击分支，
分别增加 5、7、10 点当前与最大生命或持久攻击。每次有效调用消耗一次战斗随机。
[原版 Silver 调度归档](../tests/compatibility/game-2.3.1/mechanics/silver-postbattle/manifest.tsv)

含 Silver T1 的原版序章实际获胜。原版战后流程调用一次遗物，薪火从 50/50 升到 55/55，
该数值随原版战役流程保存并带入下一场战前界面。
[原版胜局交接归档](../tests/compatibility/game-2.3.1/mechanics/silver-postbattle/native-tier1-prologue-handoff.mt2f)

原版 `ChampionReplacement` 在开局换冠军，`TimeForTrials` 在战斗介绍界面锁定试炼。
同一原版种子的首战对照中，冠军由 Hornbreaker 换为 Rector Flicker。试炼开启并锁定后，
顾问从原版部署读取敌方减抽牌效果，回合抽牌由 5 降至 4。两组冠军的部署候选通过检查。
[原版开局对照](../tests/compatibility/game-2.3.1/mechanics/original-battle-setup/manifest.tsv)

原版 Dominion 修改器在永久单位牌入库时读取有序升级池。原版 `MothersRemnant3Relic`
复制付费部署的 Train Steward 后，副本获得一项永久升级，奖励随机流前进一次。
完整出牌后继、出牌候选和 16 条独立分支均通过定向复放。
[原版永久副本归档](../tests/compatibility/game-2.3.1/mechanics/dominion-deck-upgrade/native-paid-copy.mt2f)

原版 Savagery 在冠军永久入库时按原版升级池顺序应用三项升级。普通单位和法术的对照无升级，
三次入库的随机流一致。完成三项升级的 Fel 经原版付费部署，完整后继与候选通过定向复放。
在独立的 3 点失血对照中，攻击查询增加 6，战斗随机流一致。
[原版 Savagery 入库归档](../tests/compatibility/game-2.3.1/mechanics/dominion-deck-upgrade/native-savagery-deck-entry.mt2f) ·
[原版付费部署归档](../tests/compatibility/game-2.3.1/mechanics/dominion-deck-upgrade/native-savagery-paid-champion.mt2f)

原版 `DoubleGoldEarned` 和 `HalfGoldEarned` 分别把同一档 250 金奖励结算为 500 金和 125 金。
两档均按原价扣除 3 金，奖励与支出过程的确定性随机流保持一致。
[双倍归档](../tests/compatibility/game-2.3.1/mechanics/gold-reward-multiplier/native-double.mt2f) ·
[减半归档](../tests/compatibility/game-2.3.1/mechanics/gold-reward-multiplier/native-half.mt2f)

Garfield 的新版遗物使用 378 张原生有序卡池。地狱魔族和觉者共筛出 58 张，六次抽牌后
实际生成和随机状态均与模型一致。原版胜局的五次出牌、七次 EndTurn 和十六条独立分支
通过，旧版 348 张卡池也通过专项回归。隔离实例尚无法启用 Punished DLC，
选中 Punished 卡牌的分支仍缺实机对照。归档位于
`tests/compatibility/game-2.3.1/mechanics/garfield-box/`。

2.3.1 的终局统计诊断记录到击杀镜头清空牌堆后，卡面伤害文本在帧末触发了一次原版统计刷新。
专项复放在房间、交战、列车和回合四个边界重放该事件，逐项匹配永久牌组投影。
同版对照在没有该回调的终局保留临时卡统计。诊断归档保留四条原始差异，
使用 `--terminal-ui-statistics-only` 复核。归档位于
`tests/compatibility/game-2.3.1/mechanics/terminal-ui-statistics/`。

| 关口 | 有限检查范围 | 退出条件 |
| --- | --- | --- |
| 原生定义与源码 | 卡牌效果、特性、遗物、房间修饰器、状态及其有效参数／触发位置；Boss 行为与升级定义 | 每项有共享实现映射或经源码证明的无战斗效果说明；有效参数与缺失定义已补齐 |
| 引擎公共管线 | 目标、费用、身份／引用、升级掩码、队列顺序、出生／死亡、跨层、回合切换、RNG 与预演隔离 | 对应源码分支有原生对照，完整状态与父状态不变检查通过 |
| 机制差分 | 每组的正常／不生效／非法输入、参数分支、边界值和触发时机 | 批量原生与模型结果一致；未支持与差异均清零，反例可独立重放 |
| 搜索与连续状态 | 候选完备性、分支隔离、返回路径复放、随机状态、串并行及跨回合 | 固定验收用例通过；预算影响与未解决压力反例有明确结论 |
| 原版整合与交互 | 版本迁移、新内容、跨机制回调、原版身份、实际选择界面、顾问与玩家操作 | 阶段全量回归和必要实局交互通过，不用组件校准代替整局证据 |

这些关口通过后，才将固定版本的模拟标为完成。覆盖每个代码分支与公共状态
不变量，再用批量组合差分找反例；不把“枚举所有卡牌、遗物、手牌的组合”设为
完成条件。发现反例仍需修复，但归回已有机制／管线，避免每次重新扩张目标。

2.3.1 的 `RandomizeWaveOrder` 在部署前提交原版波次顺序。顾问按该顺序读取波次和原版组缓存，
并用附加契约 `Covenant02_BattlesNewEnemies` 解析出生单位。专项归档从首两波交换的原版记录
提取 11 个生成阶段，逐项核对单位、阶段、缓存和随机流。原版保持最后两波顺序。
[波次专项归档](../tests/compatibility/game-2.3.1/mechanics/randomized-waves/nontrivial-permutation.mt2f)

一次原版随机结果保留了全部波次的顺序。该胜局已完成 18 次操作、6 次结束回合和两类候选的定向复验。
[波次整战归档](../tests/compatibility/game-2.3.1/mechanics/randomized-waves/native-battle.mt2f)

Fecundity 的原版升级回调会把全额减费值改为目标卡当前的实际费用。专项归档对照了目标卡和显式来源卡
分别持有该特性的两条路径，以及两卡均无该特性的控制组。该回调已接入 `FreeFromBlessing` 的手牌升级。
原版 Train Steward 付费出牌后也生成了独立手牌副本。副本采用临时费用 +1 的后继升级，来源卡保留原升级。
专项复放核对了本次操作的完整状态、随机流和出牌候选。
[减费专项归档](../tests/compatibility/game-2.3.1/mechanics/fecundity-cost/full-cost-reduction.mt2f) ·
[付费出牌归档](../tests/compatibility/game-2.3.1/mechanics/fecundity-cost/fecundity-oncast.mt2f)

`LoseMoneyOnHit` 的原版触发器经过三次实机命中对照。每次命中扣 1 金，金币为 0 时由现有金币规则钳制。
专项复放逐项核对伤害、金币和随机流，并检查缺少原版触发器时的拒绝路径。
[受击扣金归档](../tests/compatibility/game-2.3.1/mechanics/lose-money-on-hit/three-physical-hits.mt2f)

`MoreTreasureUnits` 通过原版第二环同源对照。待生成的寻宝单位由 1 增至 2，两次出生后依次降到 1 和 0。
补齐月相和导能采集后，原版胜局的 38 次出牌、7 次结束回合、16 条独立分支和两类候选检查均通过。
专项对照匹配 11 个生成阶段，包含两次原版寻宝单位出生及对应随机流。
[原版整战归档](../tests/compatibility/game-2.3.1/mechanics/more-treasure-units/native-settled-win.mt2f)

`SkullCollectors` 在已验证的原版第 2、4、7 环路线分别替换三级寻宝单位。原版结算分别为
10 攻 41 血、25 攻 83 血和 45 攻 125 血。完整出生后继、物理位置、寻宝计数和 Spawning RNG 全部匹配。
[一级对照](../tests/compatibility/game-2.3.1/mechanics/skull-collectors/native-first-treasure.mt2f) ·
[二级对照](../tests/compatibility/game-2.3.1/mechanics/skull-collectors/native-tier2-projected.mt2f) ·
[三级对照](../tests/compatibility/game-2.3.1/mechanics/skull-collectors/native-tier3-projected.mt2f)

第二层开局突变在首个可操作决策前多结算一次移层和出生。专项对照匹配了首波移至第二层、
第二波在底层生成、寻宝单位生成及出生随机流的重置。支持范围由该开局的原版状态约束。
[第二层开局归档](../tests/compatibility/game-2.3.1/mechanics/second-floor-enemy-opening/first-decision.mt2f)

## 自动化与测试成本

从仓库根目录运行下列命令，按已登记的原版战斗生成待采集卡牌清单。

```powershell
dotnet run --project src/FixtureTools -c Release -- card-opening-worklist .
```

TSV 的 `RunnerOption` 和 `DataIds` 可用于开局批录。清单按效果与目标分组，
根据原版开局的剩余余烬及楼层容量安排出牌顺序。`RunnerOption` 为空的行保留待定向
采集的卡牌。每组仍由实机确认可出牌、效果结算和后续状态。
配置卡组时可加 `-InputProfile`，复用实验目录下原版首战前的隔离存档并直接进入战斗。

复用旧场景时，`dotnet run --project src/FixtureTools -c Release -- card-legacy-recapture-worklist .`
按连续战斗中可补录的卡牌数排列 2.2.1 归档。清单给出原采集模式、归档哈希与实际出牌位置。
这些旧档用于挑选批次，2.3.1 的覆盖统计由该版本实机结果更新。

遗物可用 `scripts/Get-RelicCoverageInventory.ps1 -Current -LegacyRecaptureWorklist` 做同类筛选。
清单按同场待补遗物数量和录制输入数排序，保留旧档哈希、实际在场位置和两版本间的定义变更。
常规遗物盘点继续使用 `-Current`。

`scripts/Run-FullBattleProbe.ps1 -OriginalOpeningRelicRosterIds <逗号分隔的原版遗物 UUID>`
可在隔离首战批量取得原版遗物。采集先核对实际部署的遗物顺序和效果列表，再走整局验收。
四件遗物的首批胜局完成了 27 次操作、7 次结束回合和两类候选检查。
[原版整局](../tests/compatibility/game-2.3.1/mechanics/configured-opening-relics/native-four-owner-roster.mt2f)
随机冻结、随机手牌升级等效果可追加 `-OriginalOpeningRelicRosterNativeRng`。
该选项按原版随机流采集出牌和结束回合，保留相同的整局差分验收。

新原版胜局可用 `scripts/Register-Game231NativeBattle.ps1` 登记到现有机制目录。
该入口校验隔离运行摘要和归档哈希，完成单场复放后再发布归档与清单。按需启用
`-CheckCardFrontiers` 与 `-CheckEndTurnFrontiers`。

优先实现可复用的机制场景生成器：从目录挑原版代表定义，按源码分支构造
最小正例、控制例、复位及边界状态，在一次隔离原生进程中批量采样，再按完整
状态差分。已有 11,232 次召唤候选校准可作为这种批处理的参考。
首组[失血攻击定义驱动生成器](room-health-query-batch.md)已完成：自动发现五个
原版升级，生成 53 组配置与 38,160 次查询，原生矩阵 2.214 秒、独立检查约
2.353 秒通过，完整状态／定义／RNG／帧／队列不变。它只验收查询组件；
三个原版冠军升级的友军光环传播、叠加、移层和死亡另已完成
[独立整局整合](room-health-attack.md)，日常生命周期检查可单独运行。
其他机制生成器及其余证据映射仍需继续，
不能把盘点或组件校准算作自动适配全部完成。异步界面、复杂引用和回调顺序
仍需专门适配与审核。

日常每笔改动只跑该机制的检查器和直接受影响的旧回归。只有跨公共管线的
变化或阶段交付跑全量。整局负责验证跨回合／队列／界面整合；纯参数分支由
批量组件对照承担，不再给每个参数各录一场完整战斗。

日常可用 `-ChangedFrom` 从指定 Git 提交筛选到当前工作区新增或修改的已登记 2.3.1
整场归档。已有二进制新登记到清单或加入阶段集时，也会自动选中。共享逻辑修改后，
用 `-Archive` 明确追加受影响的旧对照。选中零场时直接返回，
省略两个选择参数时回放全部已登记场次。

```powershell
pwsh -NoProfile -File scripts/Check-Game231RecordedChains.ps1 -SkipBuild -ChangedFrom HEAD~1 -Archive 'mechanics/punished-skills/battle.mt2f'
```

同一搜索性质只验证一次。快速检查验证目标问题所需的最小完整前沿；更深的
搜索压力检查独立保留在阶段层。例如碎片选择现在可单独检查：

```powershell
pwsh -NoProfile -File scripts/Check-Game231SummonChoiceBattle.ps1 -SkipBuild -SelectionSearchOnly
```

该项实测约 12 秒，仅展开一次决策但枚举完整根前沿，并比较串并行的选择与
返回路径。完整检查器另验连续整局、49 个录制决策的候选和基线 128 状态搜索。
减少重复计算不意味着重复召唤的深层压力搜索已验收；其独立结论见
[召唤选择边界](summon-choice-battle.md)。

每项独立机制或工具完成后单独提交 commit。原生捕获仍使用静音、隐藏的隔离
实例，证据以 `.mt2f` 与哈希清单保留，不触碰玩家实例。
