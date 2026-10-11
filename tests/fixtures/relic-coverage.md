# Original relic coverage evidence

As of 2026-10-11, **32 distinct original collectable relics / 224 catalog entries
(14.29%)** have accepted native full-battle evidence. This counts unchanged relic
definitions whose effects actually ran, including their original filters and
conditions. Test cards, unit triggers and carrier subtypes may be authored to
exercise those effects; the claim covers the recorded paths, not every possible
combination. Catalog enumeration and other owners of a supported effect class
do not count as validated relics. `RelicEffectAddTrigger` appears on 26 catalog
entries; nine now have accepted original-owner effect evidence.
`RelicEffectAddStatusEffectOnStatusApplied` appears on eight catalog entries;
four now have accepted original-owner evidence. Unknown status families and
other effects on the remaining owners still require modeling and native proof.

All ten archives below report game `2.2.1`, module MVID
`8fb07b96-f4db-4d2b-884d-c00536d6ccf4`, `NativeWon=true`, and zero
`CaptureFailures`, `Mismatches`, `Unsupported` and `Pending`. Their current byte
lengths and SHA-256 hashes were checked against [manifest.tsv](manifest.tsv).
The newest batch includes native sampling, independent replay and focused
regression; the older evidence was retained without a new full-inventory run.

| AssetKey | DataId | Accepted archive | Actual effect witness |
| --- | --- | --- | --- |
| `ExtraSpellCastTrigger` | `410ba540-7c4f-4dc5-a84f-b1d8af508891` | [combined trigger relics][counts] | `IncantTriggers`: 65 player `CardSpellPlayed` queries have `FireCount=2`; five enemy queries remain 1. Full callbacks and the 21-play policy match. |
| `ExtraSpawnTrigger` | `9e0deb69-6196-44a6-8220-85bd0df25f77` | [combined trigger relics][counts] | 26 player `OnSpawn` queries have `FireCount=2`; five enemy queries remain 1. The battle contains new paid unit births. |
| `ExtraDeathTrigger` | `a5d67620-a9ec-4257-91b5-305336e11987` | [combined trigger relics][counts] | 26 player `OnDeath` queries have `FireCount=2`; five enemy queries remain 1. Final `MonstersDeadThisBattle=5` confirms actual deaths. |
| `SpawnWithArmor` | `68ef2523-5c2e-4660-b96d-00b1c0485f54` | [spawn status relics][statuses] | All five player birth records add 5 Armor. Example: `RelicSpawnStatuses[0]`, unit 2, Armor absent → 5. |
| `FirstUnitGainDamageShield` | `60a2a8a3-5f7a-4a9d-b427-5f261145fa1f` | [spawn status relics][statuses] | The first player birth receives 2 Damage Shield on three different turns; two same-turn births skip it. Captured condition counters and turn resets match. |
| `FrostbiteOnEnemies` | `270356af-16bd-4433-a0b2-3e5bb94ef890` | [spawn status relics][statuses] | All seven enemy births receive 2 Frostbite, serialized by the native game as status ID `poison`; player births do not. Example: record 3, unit 5. |
| `ReduceStarterCost` | `d892106c-dbbc-40ac-bf0b-902882cdc69d` | [starter cost relic][cost] | 124 exact manager calls and 49 notifications cover filtering, unique repeat rejection, reset and generated cards. `Actions[0]` actually plays `JustCause` for 1 Ember with base cost 2 and the original `ReduceStarterCost_Upgrade` cost offset −1. |
| `PyreHeartSon` | `8503360a-26b2-4b71-9334-4c6014ad397f` | [cardless relic upgrades][cardless] | Five ordinary cardless births gain +5 attack/+12 health; five cardless clones skip the upgrade. Example: record 8, unit 10, attack 0 → 5 and max health 1 → 13; record 9's clone remains 0/1. |
| `MoltenManifold` | `f141d334-b503-48ff-ac21-5f2ad27105dd` | [original relic batch][batch] | Original `TinkerersToolbelt_Upgrade` contributes `XCost=2`. Sixteen X-cost plays include nine positive payments; 20 damage-scaling witnesses match native values and differ when the relic upgrade is removed. Manager calls also exercise repeat stacking, reset and non-X rejection. |
| `SpawnWithRage` | `55ca34e9-047b-4b93-b390-d8d228a43261` | [original relic batch][batch] | All five player births gain 3 Rage (`buff`). Example: record 0, unit 2, absent → 3. |
| `UnitsGainLifesteal` | `a6b0b657-8893-4cc8-bc62-ec09544c4306` | [original relic batch][batch] | All five player births gain 2 Lifesteal (`lifesteal`). Example: record 0, unit 2, absent → 2. |
| `ContingencyPlans` | `0278309e-d15c-4041-a8dc-b76f360f306b` | [original relic batch][batch] | Two Monstrosity births gain 7 Spikes; the other three player births do not. Records 0–1 accept the authored Monstrosity carrier; record 2 rejects the other carrier. The relic and subtype filter are unchanged. |
| `ImpDamageBuff` | `94595f3a-4624-4408-977d-b166a98f23d2` | [original unit relic batch][units] | Original unique Demon-filtered upgrade adds Multistrike 1. Twenty-seven native exchange observations contain at least two attacks by a Demon carrier, with complete combat state matching. |
| `StewardSacrificeDamage` | `f6a4e405-98b9-4eeb-bae1-50cb204d12ed` | [original unit relic batch][units] | Six original `StewardSacrificeDamageAbility` activations remove their own units; three damage enemies. Three paid source cards exhaust and three cardless source copies retire from ownership. All queued callbacks and physical position changes match. |
| `MorselsGetDamageShield` | `d1daea7e-3346-47df-a756-c839b044e5f3` | [original unit relic batch][units] | Three paid Morsel births retain their starting shield without repeating the spawn effect; three cardless births gain one additional shield. One native exchange consumes a shield while preserving its owner's health. Both original effects and filters remain unchanged. |
| `ValorTransferOnAngelDeath` | `8c493cd7-8048-4598-bdd5-6cc2801cd842` | [original unit relic batch][units] | Twenty-eight native Shift queries cover player 2, enemy body 1, original enemy equipment 2 and excluded trigger 1. Eight player shifts and one equipped-enemy shift double the moved unit's gold callback and match other same-team callbacks. |
| `EquippedUnitsGainArmor` | `9af0364b-8e05-45d9-be46-7b7dd1e721e7` | [original trigger relics][triggers] | Original OnEquipmentAdded grants 10 Armor and permits count modifiers. An authored native +1 count carrier produces fire count 2; two player attachments give two positive effect counterfactuals. |
| `DamageShieldOnEquip` | `0c50313f-1f07-40c4-8a3e-54748b57981f` | [original trigger relics][triggers] | Each player attachment adds one shield. Its original exclusion flag keeps fire count 1 with the same +1 equipment modifier; two positive effect counterfactuals. |
| `ManyBuffonAngelEquip` | `c97500a9-b0a4-4c18-b203-b9f4447c84c3` | [original trigger relics][triggers] | Original source team 255 admits both teams, filtered to Angels. Two player and one enemy attachments each add 3 Valor, 3 Rage and 3 Regen; complete states and three effect counterfactuals match. |
| `SurgeonsStitcher` | `8f22145d-5c41-45da-8abf-95bc5f369648` | [original trigger relics][triggers] | Original 999999 Self heal restores a wounded 68/80 player to 80/80 on attachment. Removing its installed trigger loses that heal. |
| `AngelsSpawnWithShiftyValor` | `3d4705ec-ee6b-433b-88c8-1b4ce0ed7cc9` | [original trigger relics][triggers] | Original Angel OnShift adds 2 Valor with fire count 1 despite the original +1 Shift relic. Ten Bump plays provide six positive effect counterfactuals; physical positions and other same-team callbacks also match. |
| `RageOnHymnistDeath` | `ec46f782-87ad-4cbd-aacb-023701417283` | [stationary trigger relic deaths][triggerDeaths] | Original player OnDeath targets eligible Angels on its floor with 7 Rage. Two combat counterfactuals change actual unit/status results. This effect has no positive witness in the separate physical Shift archive. |
| `WhelpsGainExtinguishHealPyre` | `e6a167d5-f57e-4a02-a0f0-b8cf9373c838` | [original trigger relics][triggers] | Its actual payload adds 5 Pyregel to enemies on a Whelp's floor. The original exclusion flag prevents the +1 Death relic from doubling it. Four death/action/combat counterfactuals change unit/status results. |
| `DragonsGainStrikeAddPyregel` | `1fdf2fde-a5e7-4eae-86eb-d432145c4621` | [original trigger relics][triggers] | Original Dragon Strike adds 5 Pyregel to LastAttackedCharacter. Five action/combat counterfactuals change unit/status results. |
| `WhelpStrikeAddPyregel_DH` | `588825ab-8517-48db-8447-31b48e931aaa` | [original trigger relics][triggers] | Original Whelp Strike adds 3 Pyregel to LastAttackedCharacter. Six action/combat counterfactuals change unit/status results. |
| `ApplyExtraPyregel_DH` | `f3e1c306-964c-4373-96c3-61e606379f43` | [original status relics][statusAdders] | The unchanged None-team positive-stack rule adds 1 Pyregel. Four distinct real native query cases lose that extra stack when only this relic is removed. |
| `IncreasePyregelStacks` | `f4c76f11-37d9-4bb5-867a-4d99b7cfa270` | [original status relics][statusAdders] | The unchanged Heroes-team first-application rule adds 6 Pyregel. A real first application is 1 → 8 with both original Pyregel relics; repeated application is 1 → 2. Removing this relic changes the first case by exactly 6. |
| `ExtraValorOnValor` | `74c025fa-ddcb-494b-a790-dcadc5b5772b` | [original status relics][statusAdders] | The unchanged Monsters-team unconditional rule adds 1 Valor, including zero input. Eight distinct real native query cases lose that stack when only this relic is removed. Starting, spell and combat-trigger status paths replay exactly. |
| `IncreaseArmorStacks` | `ba26070b-4f6b-4af0-a1ec-2af024b6af87` | [original status relics][statusAdders] | The unchanged Monsters-team positive-stack rule adds 4 Armor. Five distinct real native query cases lose exactly 4 stacks when only this relic is removed. Starting, spell, equipment-upgrade and combat status paths replay exactly. |
| `IncreaseRageDamage` | `b9bddf11-acaa-40fb-bc40-eee83e478424` | [original magnitude relics][magnitudes] | The unchanged Monsters-team buff magnitude adds 1 attack per future added/removed Rage stack. Removing only this owner preserves captured starting attack history but changes subsequent status attack in 21 action/combat witnesses. |
| `StrongerSpikes` | `aa94ad34-ee09-464e-89ff-2a2c2867c097` | [original magnitude relics][magnitudes] | The unchanged Monsters-team spikes magnitude adds 1 damage per stack at retaliation. Twelve player-owned Spikes event witnesses deal less real damage after removing only this owner. |
| `AwokenRegen` | `1fd244de-7d7b-4345-974b-4e5bf287a3df` | [original magnitude relics][magnitudes] | The unchanged Monsters-team regen magnitude adds 1 healing per stack at post-combat. Fourteen player-owned Regen event witnesses restore less actual health after removing only this owner. |

Record indices above are zero-based. Full-battle acceptance and independent
effect checks are documented in [README.md](README.md); the table's ten source
archives contain these observed battle sizes:

| Archive | Schema | Card plays | EndTurns | Matched stages | Independent relic observations |
| --- | ---: | ---: | ---: | ---: | --- |
| [combined trigger relics][counts] | 108 | 21 | 7 | 62 | 160 trigger-count queries |
| [spawn status relics][statuses] | 109 | 18 | 6 | 52 | 12 birth phases |
| [starter cost relic][cost] | 105 | 18 | 5 | 42 | 124 modifier calls |
| [cardless relic upgrades][cardless] | 111 | 1 | 5 | 42 | 22 birth phases |
| [original relic batch][batch] | 106 | 21 | 5 | 46 | 30 modifier calls, 12 birth phases |
| [original unit relic batch][units] | 106 | 39 | 7 | 56 | 40 modifier calls, 28 Shift queries, 6 original ability activations |
| [stationary trigger relic deaths][triggerDeaths] | 103 | 13 | 5 | 35 | 14 births, eight required original effect witnesses |
| [original trigger relics][triggers] | 106 | 28 | 7 | 54 | 14 births, three attachments including one enemy, ten Bump plays, eight required effect witnesses |
| [original status relics][statusAdders] | 113 | 26 | 5 | 39 | 27 distinct real queries, 192 boundary queries, four positive original effects |
| [original magnitude relics][magnitudes] | 114 | 18 | 5 | 39 | 20 matrix, seven live and four preview getter cases; three positive original effects |

The checks that enforce these observations are
[TriggerCountChecks.Native](../../src/ModelChecks/TriggerCountChecks.cs),
[RelicSpawnStatusChecks.Native](../../src/ModelChecks/RelicSpawnStatusChecks.cs),
[RelicCardModifierChecks.Native](../../src/ModelChecks/RelicCardModifierChecks.cs),
[OriginalRelicBatchChecks.Native](../../src/ModelChecks/OriginalRelicBatchChecks.cs),
[OriginalUnitRelicBatchChecks.Native](../../src/ModelChecks/OriginalUnitRelicBatchChecks.cs),
[OriginalTriggerRelicBatchChecks.Native/Combined](../../src/ModelChecks/OriginalTriggerRelicBatchChecks.cs),
[OriginalStatusRelicBatchChecks.Native](../../src/ModelChecks/OriginalStatusRelicBatchChecks.cs),
and [OriginalMagnitudeRelicBatchChecks.Native](../../src/ModelChecks/OriginalMagnitudeRelicBatchChecks.cs).
Each new trigger archive has eight positive owner effects; their union covers
all nine distinct originals. Removing only one original's installed callbacks
must change health, attack, membership or status stacks, beyond trigger metadata.
Each archive also compares full native states, all nine installation rules,
negative team/subtype gates, count admission and 32 immutable birth branches.
The cardless archive uses the clone-birth path in `RelicSpawnStatusChecks.Native`
to compare the complete train, callback queue and dispatched callbacks;
[RelicCardlessUpgradeChecks](../../src/ModelChecks/RelicCardlessUpgradeChecks.cs)
adds focused synthetic checks for its application gates.

No additional original relic is counted for the status, piercing, SelfPurge,
unit-ability, OnKill or conditional modifier fixtures. Their preparation in
[RelicCardModifierScenario](../../src/Probe/RelicCardModifierScenario.cs) replaces
`ReduceStarterCost`'s runtime upgrade or adds a condition. In particular,
`StingBuffPiercing`, `StewardSacrificeDamage_Upgrade` and
`PyreHeartSavagery_UpgradeData` are useful original **payload** evidence, not
validation of their complete original owner relics in those earlier archives.
The new unit relic batch independently validates the unchanged Steward owner.
The ability-Incant fixture's
`PojuNativeAbilityIncantRelic` is authored and is also excluded. The always-present
`PyreHeartStarter_ArtifactData` with an empty effect list adds no effect witness.

The original batch was accepted in commit `63d4f07` with focused regression.
The preceding 210-archive milestone used a passing prefix and a fixed passing
suffix; it is not a fresh full regression on the final batch implementation.
The latest magnitude batch passed its complete fixture checks, independent
full-battle/mid-battle/16-branch replay and three affected old battle fixtures.
All 217 archive names, sizes and hashes match; a full 217-archive replay remains
deferred. Mageblade still requires room spell power and its cached coefficient,
and is excluded from accepted magnitude consumers and original-owner coverage.
The older `6/224` passage in README describes an earlier milestone; it does not
include the starter-cost, cardless or four-artifact batch evidence above.

[counts]: full-battle-incant-relics-combined.mt2f
[statuses]: full-battle-spawn-status-relics.mt2f
[cost]: full-battle-relic-card-modifiers.mt2f
[cardless]: full-battle-cardless-relic-upgrades.mt2f
[batch]: full-battle-original-relic-batch.mt2f
[units]: full-battle-original-unit-relics.mt2f
[triggerDeaths]: full-battle-original-trigger-relic-deaths.mt2f
[triggers]: full-battle-original-trigger-relics.mt2f
[statusAdders]: full-battle-original-status-relics.mt2f
[magnitudes]: full-battle-original-magnitude-relics.mt2f
