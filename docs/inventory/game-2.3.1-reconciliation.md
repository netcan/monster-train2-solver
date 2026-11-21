# 固定版本机制与现有代码对账

固定 2.3.1 / build 26650011，MVID `bf93f060-beda-46b1-b808-95ebf1e9a99d`。由 `scripts/Get-MechanismReconciliation.ps1` 生成。

扫描编译后的模型、采集探针与检查器 IL，索引实际字符串和原生类型操作数；状态类同时读取原生 `const StatusId`。非公共根类的继承引用标为 `base-*`，用于查共享实现，不能据此确认派生类行为。PDB 必须与程序集匹配，工作区源码校验和必须与 PDB 一致，两个消费者的模型字节必须一致。注释、未使用常量和文档不参与代码匹配。

这是有限的源码审查队列，不是剩余未实现数量或完成率。一个方法中出现类型／状态名不能证明该类已实现：可能只是保护、快照、构造测试或归一化入口。状态 ID 引用同样不证明完整生命周期；探针包含生产采集与隔离测试代码，不能直接当作执行模型。

| 待办分类 | 类数 | 含目录引用 | 含类型声明 |
| --- | ---: | ---: | ---: |
| known-gap | 1 | 1 | 1 |
| inspect-no-code-link | 65 | 65 | 65 |
| inspect-inherited-code | 13 | 13 | 13 |
| inspect-projection-normalization | 59 | 59 | 59 |
| inspect-guard-links | 49 | 49 | 49 |
| review-existing-code | 233 | 233 | 233 |
| resolve-reference-only | 7 | 7 | 0 |
| resolve-entry | 286 | 0 | 0 |
| scoped-review-recorded | 21 | 21 | 21 |

总计 734 个具体类，448 类有目录引用；102 个状态类取得原生 StatusId。已记录 23 项有边界的人工审查，仍未将任何类自动判为完整完成。

`known-gap` 是源码审查确认的有边界缺口；`inspect-no-code-link` 优先核对入口与别名；`inspect-inherited-code` 只有父类的模型链接；`inspect-projection-normalization` 核对采集端是否已转为通用模型动作；`inspect-guard-links` 仅提示所有直接模型引用都落在按名称识别的保护方法，需人工确认。无链接不等于未实现。`resolve-entry` 没有捕获目录引用，仍需查动态入口和未捕获定义。

`DefinitionOwners` 只计目录中的类型声明／添加特性；过滤器条件、通用参数默认值和投影描述分别保留在原生清单。仅有这些引用的类进入 `resolve-reference-only`，先确认执行入口，再决定是否补实现；已有 known-gap 审查原样保留，不据此认定该类未使用或已经完成。含声明也不证明实战会执行。

共享机制审查一次记录原生类、模型消费者、采集入口、定向检查器和明确边界，后续按真实反例修复；不为同一实现的每张卡各造一场战斗。`input-only` 记录战前取得的输入或定义标记，不认证其战役消费者已实现。清单 TSV 按审查类别、类型声明引用数、全部引用数排序，源码例子最多各列两个方法。

## 已有源码审查结论

| 原生类 | 结论 | 边界 |
| --- | --- | --- |
| CardEffectAddStatusEffectNextMonster | known-gap | Native Apply queues AddSpawningStatusEffect for a later monster; the model has no pending-spawn status action. All five pinned catalog references are requiredCardEffects mask conditions, with zero captured type declarations. Resolve a real execution entry before prioritizing queue capture, consumption timing and search state. The latent implementation gap remains; parent AddStatus links do not implement it, and reference-only does not prove the class unused. |
| CardTraitJuice4 | known-gap | Shared 2/3/4 multiplier and ID-independent simple status-spell contract now have five original upgrades, 180 native callback pairs, nine original soul-upgrade casts and complete native loss/16 branches/frontiers/root search. Mollis OnCast armor/poison use the existing dispatcher. Source OnCardBeingUpgraded and next-draw manager dispatch now have five original definitions, 540 relation controls/repeats and 300 ordered pairs; six native two-card draws verify deep copies, filtered shared originals and callback short circuit, with complete successors/16 branches/frontiers/root search. Parameter-upgrade cast effects and other mixed ordered scaling remain guarded; other consumers are not certified. No class-wide lifecycle completion. |
| CardEffectDamage | shared-path | Native Damage normalizes to the shared Damage action; current PunishedSpike battle exercises repeated damage, armor, target death and full successors. Existing target/scaling/callback guards remain; not an audit of every contextual branch. |
| RelicEffectAddRelicStartOfRun | input-only | Sealed native class implements StartOfRun ApplyEffect, which adds the configured relic through SaveManager. Battle input captures the resulting relic roster. No combat callback override in this class; start-of-run campaign handling and carried-owner validation remain separate. |
| RelicEffectAddUnitUpgradePerPile | shared-path | Shared ID-independent birth callback captures initialized pile/threshold/team/subtype/upgrade, snapshots visible count once, preserves repeated direct API calls with empty upgradeId and existing queue policy. Four original definitions/52 configurations/3744 native parent-coroutine upgrade requests with controlled counts; children are not executed in this matrix. Nine unmodified native pile queries; unchanged deck-size mutator full win, three births with individual applied records, 16 branches/frontiers/serial-parallel root search. Five model placement/Covenant/condition/threshold/identity controls. Soul attachment lifecycle, complex upgrade callback combinations and other owner combinations remain outside this integration; other spawn-status scaling is separate. |
| RelicEffectAdditionalDrawDuringRelentless | shared-path | Shared initialized mutable cache refreshes at start/before draw, tests both teams physical status membership without alive filtering, excludes Pyre and preserves false Test/no notification/no signal. Two original sources/10 configurations/640 native callbacks with controlled status queries and real independent MutableRules integer queries, including float precision and Int32 endpoints. Original T2/T3 full native wins; positive/reset cache, combined occupied draw, T3 tenth play draws two through existing consumer; full successors/16 branches/frontiers/serial-parallel root search. Raw actor/case/death boundaries are source/model controls; production BattleTurn now dispatches the modeled before-draw callbacks in owner/effect order; the focused legacy resource API retains its guard; complex lifecycle, outer-boss anchors, mixed Covenants and full hover remain outside certification. |
| RelicEffectAdditionalDrawForEnemyOccupiedFloors | shared-path | Shared installed mutable draw value retains stale queries, per-room EndTurn/after-draw Test refresh and MonsterTurn-only enemy birth/killed callbacks with distinct manager gates. Three original sources/2304 detached native callbacks with controlled floor/team/phase queries and intercepted signal requests; same immutable successors and 16 parallel numeric checks. Physical dead references retained until removal; first PostCombat Despawn filter, silenced and duplicate source controls. Unchanged original WarborneT1 native win, actual kill cache1->0, full successors/16 branches/frontiers/serial-parallel root search. Higher-tier Relentless/draw-on-play companions, actual MonsterTurn enemy birth, outer-boss anchors, complex competition, mixed Covenants and full hover are not certified. |
| RelicEffectApplyExtraPyreDamage | shared-path | Shared ID-independent ordered post-defense Pyre damage rule captures initialized extraDamage, preserves per-effect signed overflow and nonpositive reset, and records RoomState notifications even in preview except Covenant owners. Four original definition parameters/40 callbacks and 650 native manager cases; unchanged +3 mutator native win, seven authored damage/armor/shield controls and natural attacks, full successors/16 branches/frontiers/serial-parallel root search. Other soul companion effects, mixed Covenants, condition combinations and other tower-damage interfaces are not certified; synchronous GetDamageToTarget and direct HP payments are separate paths. |
| RelicEffectCardDrawOnCardPlay | shared-path | Existing ID-independent ordered consumer reuses card type/subtype/trait and statistic conditions, native draw pipeline and condition records. This review adds unchanged original WarborneT3 carried-owner integration with ten same-turn plays and an actual two-card tenth-play draw; all successors/RNG, 16 branches/frontiers and serial-parallel root search. Other original owners have separate component checks; their complete lifecycle, other condition mixtures, mixed Covenants and all card combinations are not certified here. |
| RelicEffectCardUpgrade | input-only | Sealed native class only overrides editor descriptions and implements the empty ICardUpgradeRelicEffect marker. Its 148 catalog references include upgrade definitions; already acquired card upgrades are battle inputs, not 148 missing live battle handlers. Acquisition consumers are not certified by this review. |
| RelicEffectHealStartOfTurn | shared-path | Existing Champion-only timed full-heal consumer accepts exact owner/effect selection without filtering the passive roster. Two unchanged original relic orders prove native heal incoming energy4 versus3 and full healing across three authored wounded floors; complete native battles/16 branches/frontiers/serial-parallel root search and legacy champion heal regression. Other healing behaviors, all conditional branches and complex callback competition are not certified by these two controls. |
| RelicEffectModifyCharacterAttackOrHealthPercentage | shared-path | Shared ID-independent birth/query rules capture initialized native float, team/subtype/required-mutator filters and ordered raw attack/health setters. Six original owners/56 effects, 138 configurations and 7728 query/birth pairs; unchanged double-health mutator full native win, 16 branches/frontiers/root search. Other owners companion effects, mixed Covenant projections and conditional effects remain outside this integration. A later shared late-attack integration adds nonneutral percentage birth matrices and a combined unchanged two-mutator full native battle. |
| RelicEffectModifyDragonsHoardEndOfBattle | campaign-only | Sealed combat callbacks only remember private Pyre damage; campaign rewards have current OnPunish and legacy OnHit calibration. Natural seed20261115 Ring1 carried original owner across 21 inputs/42 full states, default3s budget, Pyre80 native win and independent forecasts/state/RNG. No independent frozen native pair or later rings claimed. |
| RelicEffectModifyEnergyReducingEveryTurn | shared-path | Shared initialized rule preserves cached display query, actual Test/Apply refresh even when zero, deployment gate, unchecked arithmetic and positive-only AddEnergy/one notification. Combined six original sources/90 configurations/2643 native calls and 63 expected overflow controls; detached batch intercepts energy/notification requests. Unchanged original SolarisT1/WormgutT3 plus capacity artifact native full win, zero-bonus cache refresh, size20 threshold, full successors/16 branches/frontiers/serial-parallel root search. Other tier lifecycles, mixed Covenants, prior before-draw effect ordering and complete hover remain outside integration. |
| RelicEffectModifyEnergyWhenDamagedUnitOnEachFloor | shared-path | Existing physical wounded-floor predicate now executes in full-owner/effect order in the battle turn pipeline. Unchanged EmberForDamagedPerFloor and ChampsHealBeginningOfTurn acquired in both orders: three authored injured champions heal fully, actual first normal income4 versus3. Both full native wins/successors/16 branches/frontiers/serial-parallel root search; two model orders/16 parallel pairs and same-owner selection. Focused legacy energy API retains its restricted ordering guard. Complex nested callbacks and all condition/owner permutations remain outside certification. |
| RelicEffectModifyEnergyWhenFloorsOccupied | shared-path | Shared initialized rule distinguishes registered actors from living capacity/GetSize clamp, excludes Pyre, exposes only base query, and preserves separate base/threshold energy clamps including RemoveEnergy(0) and one notification. Combined six sources/90 configurations/2643 native calls; native Int32.MinValue faults explicitly unsupported. Original WormgutT3 plus SolarisT1 and capacity artifact acquired before room setup, actual size20 threshold and full native win/16 branches/frontiers/root search. Other tier lifecycles, mixed Covenants, prior before-draw ordering and enemy-occupied draw cache remain outside integration. |
| RelicEffectModifyGoldEndOfBattle | campaign-only | Sealed native combat callbacks only remember private Pyre damage and return false; rewards use the separate campaign consumer. 864 current and 756 legacy component rows; natural carried-owner battle integration remains separate. |
| RelicEffectModifyMagicPowerPerGold | shared-path | Original Echoes mutator observes exact permanent-deck membership and effect-owned card identity. Native 100 gold yields +1 damage/heal. Original Smelted Treasure pays 50 during the turn while +1 stays installed. Next ordered BeforeDraw refresh yields +0. Same-definition generated card excluded. Full native win, exact successors, sixteen immutable branches, recorded frontiers and serial/parallel search agree. Mixed mutators and other permanent-deck replacement paths remain outside this evidence. |
| RelicEffectModifyRandomRoomCapacity | input-only | Original mutator is captured after native deployment draw settles the turn-zero capacity callback. A native win proves one selected floor 5 to 2, one Battle RNG advance, later false Tests, exact capacity-based rejection and alternate-floor placement. Complete successors, 16 branches, frontiers and serial/parallel plans agree. The solver consumes settled room capacities, and pending opening mutations are rejected. Campaign carryover and other random-capacity owners remain separate. |
| RelicEffectModifyRandomRoomCapacityAfterBattle | input-only | Capture settled native capacities after the opening callback. Original Bogwurm is naturally selected and the unchanged catalog Life artifact is attached in an isolated control. Capacity bonuses +2/0/+1 and the Life static -1 enter actual deployment legality, with an oversized legal summon and unchanged-floor denial. Original wins, exact successors, 16 branches, frontiers and serial/parallel plans pass. Campaign reward mutation remains native. |
| RelicEffectMultiplyAttackLate | shared-path | Shared parameter-driven late multiplier preserves manager order, champion filtering, captured native Pyre exemption and raw snapshots. Pinned one original owner; 640 native queries, 80 products, 640 actual attacks plus 7728 percentage query/birth pairs with nonneutral late. Unchanged half-attack plus double-enemy-health native win; full successors, 16 branches/frontiers and serial/parallel root search. Actual status callback interactions, all other owner combinations and mixed Covenants are not fully certified. |
| RelicEffectScaleStatusApplicationsOnSpawn | shared-path | Shared initialized birth rule preserves actual enemy-only/inverted required-mutator gate, original-definition positive starting additions, ordered exact-type runtime trigger scaling and immutable source/target metadata; enchant capture reads runtime parameters. Four original sources/54 configurations/2592 native callbacks in detached objects; starting AddStatus requests intercepted, not applied there. Unchanged original endless RunDistance4 owner acquired before Pyre setup, authored enemy status/trigger and defender controls; native full win/four zero, 16 branches/frontiers/serial-parallel root search. Other source companions, complex status callback competition, mixed Covenants, scaled enchant gameplay lifecycle and complete hover query remain outside certification. |
| RelicEffectShowtime | shared-path | Three unchanged original souls apply their hidden floor attachment once before deployment. Settled capacities 5 to 8/8/10 admit a size-six pair and route the original front-only upgrade after room combat. Two/two/four positive repetitions, rear unit unaffected, and full-capacity no-op agree across full native wins, exact successors, 16 branches, frontiers and serial/parallel plans. Native RNG selects the opening room. Other floor seeds and mixed room attachments remain outside this evidence. |
## 第一批需核对的代码入口

| 原生类 | 队列 | 声明引用 | 全部引用 | 模型／探针示例 |
| --- | --- | ---: | ---: | --- |
| RelicEffectCardDuplicator | inspect-no-code-link | 5 | 5 | 无直接链接 |
| RelicEffectGetCardCopiesOnDeck | inspect-no-code-link | 5 | 5 | 无直接链接 |
| RelicEffectUpgradeCardOnPurchasingUpgrade | inspect-no-code-link | 4 | 4 | 无直接链接 |
| CardTraitDrawOnPlay | inspect-no-code-link | 3 | 3 | 无直接链接 |
| CardTraitScalingRewardGold | inspect-no-code-link | 3 | 3 | 无直接链接 |
| RelicEffectAddAlliedChampionToDeckAndRandomUpgradeItAtLightforge | inspect-no-code-link | 3 | 3 | 无直接链接 |
| RelicEffectAddPyreAttackAndHealthPerCardInDeck | inspect-no-code-link | 3 | 3 | 无直接链接 |
| RelicEffectBuffPyreForBossKill | inspect-no-code-link | 3 | 3 | 无直接链接 |
| RelicEffectCardUpgradeRandomCardsInDeck | inspect-no-code-link | 3 | 3 | 无直接链接 |
| RelicEffectGrantRewardStartOfRun | inspect-no-code-link | 3 | 3 | 无直接链接 |
| RelicEffectPurgeCardsUntilDeckLimitMet | inspect-no-code-link | 3 | 3 | 无直接链接 |
| RelicEffectPurgeStarterDeckMatchingFilter | inspect-no-code-link | 3 | 3 | 无直接链接 |
| RelicEffectAddStartingUpgradeToUnitCards | inspect-no-code-link | 2 | 2 | 无直接链接 |
| RelicEffectBumpOnTurnEnd | inspect-no-code-link | 2 | 2 | 无直接链接 |
| RelicEffectChangeGoldReward | inspect-no-code-link | 2 | 2 | 无直接链接 |

## 可重建输入

- 原版 DLL SHA-256: `49c71329eb27ae77462e6e59e1fd300d6a249dba95af5b22f9aea766432e27b9`。
- 原生类清单 SHA-256: `cd905d8ab02624ad9411a8138d0043360ebcdccecb9bafc5ce1f439eb5c9d7ab`。
- 人工审查索引 SHA-256: `0656e9f382fe6132ffaf7993329ba4ab77551819822e7a338440bce743c46f75`。
- Model: PDB 校验过的项目源码集合 `25e7bbf9496ce9b3e102a4c75ca30ca03a4e7ec48acec8eaa0b4dc11868dd2e4`。
- Probe: PDB 校验过的项目源码集合 `cff3a2e7ea25f211e3619b1bb347a41f639f4bce5f333b24958871f2e7931176`。
- ModelChecks: PDB 校验过的项目源码集合 `d7f0206ac5a2e9dbc5263ccaeb2dd46bd20639abf6eebecb033f659cf4af970e`。

源码集合摘要排除 obj/bin 生成文件；这些文件仍校验 PDB，但 Git 提交号写入 AssemblyInfo 不会使源码审查清单反复失效。运行时继续严格比较消费者 DLL 的真实字节，绝不以源码摘要替代依赖一致性检查。

`-Check` 要求生成内容逐字一致，源码、构建或审查声明变化后必须重建。检查器链接只证明存在，仍需运行相应检查器确认当前通过；旧版本证据不能据此升级为新版整场证据。
