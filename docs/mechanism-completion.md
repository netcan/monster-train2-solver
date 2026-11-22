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

Rage Room 实机核对房间触发器、楼层与队伍范围，以及友军获得 Rage 的正例。
5 次出牌、5 次结束回合、16 条独立分支和候选检查通过。
[`rage-room.mt2f`](../tests/compatibility/game-2.3.1/mechanics/controlled-original-room-abilities/rage-room.mt2f)

原版法术和单位升级计数遗物在 2.3.1 中各使用实际取得的定义。整战记录 44 次修饰回调和
26 次原版筛选，覆盖 0、1、2 个可见升级、重置、重复及逐件对照。
23 次出牌、7 次结束回合及候选检查通过。
[`battle.mt2f`](../tests/compatibility/game-2.3.1/mechanics/original-upgrade-count-relics/battle.mt2f)

腐化遗物批次观察到原版为楼层敌军增加两层 Corrupt Poison。部署结束后的两张零费测试牌
单独核对，其余状态和随机流逐项比较。19 次出牌、6 次结束回合及候选检查分段通过。
[`battle.mt2f`](../tests/compatibility/game-2.3.1/mechanics/corruption-relic-trigger/battle.mt2f)

镜厅组件记录的付费接枝出生和复制体各有独立装备。复制体的来源卡保留本体装备引用，
实际绑定使用独立的装备实例。专项核对物理槽位、随机流和原版结算状态下的结束回合。
该复制分支的出牌前预测仍会拒绝。
[`observation.mt2f`](../tests/compatibility/game-2.3.1/mechanics/hall-grafted-birth/observation.mt2f)

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
二进制 53,985 字节。Warborne 抽牌缓存已由上条另行补齐，前置回合顺序仍待整合。

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
源码；可找到归一化入口和继承实现，避免按类名漏搜后重写。当前 431 类有
模型直接／继承／状态 ID 代码链接，465 类有探针链接，390 类有检查器链接；
均是导航证据，不能计作支持或完成。当前三十二项人工审查区分战前输入标记、
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
