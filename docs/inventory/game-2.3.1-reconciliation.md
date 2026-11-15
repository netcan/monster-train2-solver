# 固定版本机制与现有代码对账

固定 2.3.1 / build 26650011，MVID `bf93f060-beda-46b1-b808-95ebf1e9a99d`。由 `scripts/Get-MechanismReconciliation.ps1` 生成。

扫描编译后的模型、采集探针与检查器 IL，索引实际字符串和原生类型操作数；状态类同时读取原生 `const StatusId`。非公共根类的继承引用标为 `base-*`，用于查共享实现，不能据此确认派生类行为。PDB 必须与程序集匹配，工作区源码校验和必须与 PDB 一致，两个消费者的模型字节必须一致。注释、未使用常量和文档不参与代码匹配。

这是有限的源码审查队列，不是剩余未实现数量或完成率。一个方法中出现类型／状态名不能证明该类已实现：可能只是保护、快照、构造测试或归一化入口。状态 ID 引用同样不证明完整生命周期；探针包含生产采集与隔离测试代码，不能直接当作执行模型。

| 待办分类 | 类数 | 含捕获定义引用 |
| --- | ---: | ---: |
| known-gap | 3 | 3 |
| inspect-no-code-link | 80 | 80 |
| inspect-inherited-code | 15 | 15 |
| inspect-projection-normalization | 60 | 60 |
| inspect-guard-links | 51 | 51 |
| review-existing-code | 234 | 234 |
| resolve-entry | 286 | 0 |
| scoped-review-recorded | 5 | 5 |

总计 734 个具体类，448 类有目录引用；102 个状态类取得原生 StatusId。已记录 8 项有边界的人工审查，仍未将任何类自动判为完整完成。

`known-gap` 是源码审查确认的有边界缺口；`inspect-no-code-link` 优先核对入口与别名；`inspect-inherited-code` 只有父类的模型链接；`inspect-projection-normalization` 核对采集端是否已转为通用模型动作；`inspect-guard-links` 仅提示所有直接模型引用都落在按名称识别的保护方法，需人工确认。无链接不等于未实现。`resolve-entry` 没有捕获目录引用，仍需查动态入口和未捕获定义。

共享机制审查一次记录原生类、模型消费者、采集入口、定向检查器和明确边界，后续按真实反例修复；不为同一实现的每张卡各造一场战斗。`input-only` 记录战前取得的输入或定义标记，不认证其战役消费者已实现。清单 TSV 按审查类别、目录引用数排序，源码例子最多各列两个方法。

## 已有源码审查结论

| 原生类 | 结论 | 边界 |
| --- | --- | --- |
| CardEffectAddStatusEffectNextMonster | known-gap | Native Apply queues AddSpawningStatusEffect for a later monster, rather than modifying current targets. The direct spell effect allowlist has no subtype entry and model has no mapped pending-spawn status action. Requires pending queue capture, consumption timing and search state; parent AddStatus links do not implement it. |
| CardTraitJuice4 | known-gap | Shared 2/3/4 multiplier and ID-independent simple status-spell contract now have five original upgrades, 180 native callback pairs, nine original soul-upgrade casts and complete native loss/16 branches/frontiers/root search. Mollis OnCast armor/poison use the existing dispatcher. Actual OnCardBeingUpgraded self-upgrade dispatch and ordered scaling combinations remain guarded; query evidence does not certify that lifecycle. |
| RelicEffectModifyCharacterAttackOrHealthPercentage | known-gap | Native OnCharacterAdded rounds percentage attack/health, with team/subtype/required-mutator filters and an additional stat-query path. No dedicated captured modifier or accepted runtime effect type exists in current relic validation; preserve rejection until shared birth/stat projection is modeled and compared. |
| CardEffectDamage | shared-path | Native Damage normalizes to the shared Damage action; current PunishedSpike battle exercises repeated damage, armor, target death and full successors. Existing target/scaling/callback guards remain; not an audit of every contextual branch. |
| RelicEffectAddRelicStartOfRun | input-only | Sealed native class implements StartOfRun ApplyEffect, which adds the configured relic through SaveManager. Battle input captures the resulting relic roster. No combat callback override in this class; start-of-run campaign handling and carried-owner validation remain separate. |
| RelicEffectCardUpgrade | input-only | Sealed native class only overrides editor descriptions and implements the empty ICardUpgradeRelicEffect marker. Its 148 catalog references include upgrade definitions; already acquired card upgrades are battle inputs, not 148 missing live battle handlers. Acquisition consumers are not certified by this review. |
| RelicEffectModifyDragonsHoardEndOfBattle | campaign-only | Sealed combat callbacks only remember private Pyre damage; campaign rewards have current OnPunish and legacy OnHit calibration. Natural seed20261115 Ring1 carried original owner across 21 inputs/42 full states, default3s budget, Pyre80 native win and independent forecasts/state/RNG. No independent frozen native pair or later rings claimed. |
| RelicEffectModifyGoldEndOfBattle | campaign-only | Sealed native combat callbacks only remember private Pyre damage and return false; rewards use the separate campaign consumer. 864 current and 756 legacy component rows; natural carried-owner battle integration remains separate. |
## 第一批需核对的代码入口

| 原生类 | 队列 | 目录引用 | 模型／探针示例 |
| --- | --- | ---: | --- |
| CardTraitAddMagicPowerToAttackDamage | inspect-no-code-link | 5 | 无直接链接 |
| RelicEffectCardDuplicator | inspect-no-code-link | 5 | 无直接链接 |
| RelicEffectGetCardCopiesOnDeck | inspect-no-code-link | 5 | 无直接链接 |
| CardEffectDamageByPercentRemainingHealth | inspect-no-code-link | 4 | 无直接链接 |
| RelicEffectAddUnitUpgradePerPile | inspect-no-code-link | 4 | 无直接链接 |
| RelicEffectApplyExtraPyreDamage | inspect-no-code-link | 4 | 无直接链接 |
| RelicEffectScaleStatusApplicationsOnSpawn | inspect-no-code-link | 4 | 无直接链接 |
| RelicEffectUpgradeCardOnPurchasingUpgrade | inspect-no-code-link | 4 | 无直接链接 |
| CardTraitDrawOnPlay | inspect-no-code-link | 3 | 无直接链接 |
| CardTraitScalingRewardGold | inspect-no-code-link | 3 | 无直接链接 |
| RelicEffectAddAlliedChampionToDeckAndRandomUpgradeItAtLightforge | inspect-no-code-link | 3 | 无直接链接 |
| RelicEffectAddPyreAttackAndHealthPerCardInDeck | inspect-no-code-link | 3 | 无直接链接 |
| RelicEffectAdditionalDrawForEnemyOccupiedFloors | inspect-no-code-link | 3 | 无直接链接 |
| RelicEffectBuffPyreForBossKill | inspect-no-code-link | 3 | 无直接链接 |
| RelicEffectCardUpgradeRandomCardsInDeck | inspect-no-code-link | 3 | 无直接链接 |

## 可重建输入

- 原版 DLL SHA-256: `49c71329eb27ae77462e6e59e1fd300d6a249dba95af5b22f9aea766432e27b9`。
- 原生类清单 SHA-256: `f5eb4fb077a3b2a7c1260b696fb7711774e1b84418e609996f8e40898552fb45`。
- 人工审查索引 SHA-256: `e791708b4c80e57aa2c8a9389204cee2520d624f0c699558e400136060dce7f3`。
- Model: PDB 校验过的项目源码集合 `83b294ad76d815345735ac88fce591d601aa181f0b6ef9f3a0531ab579171518`。
- Probe: PDB 校验过的项目源码集合 `f417c98936a246604d5d7edc860c9b1562e0cd9f12cec611e93b0f7a8efa8217`。
- ModelChecks: PDB 校验过的项目源码集合 `7275b8272578527364c5ad3f0ba646fc2123a575cc676c68a83c18df44b649d3`。

源码集合摘要排除 obj/bin 生成文件；这些文件仍校验 PDB，但 Git 提交号写入 AssemblyInfo 不会使源码审查清单反复失效。运行时继续严格比较消费者 DLL 的真实字节，绝不以源码摘要替代依赖一致性检查。

`-Check` 要求生成内容逐字一致，源码、构建或审查声明变化后必须重建。检查器链接只证明存在，仍需运行相应检查器确认当前通过；旧版本证据不能据此升级为新版整场证据。
