# Original Cael variants

The three original Ring6 Cael encounters use the unmodified seed424242 native
Covenant2 checkpoint after five victories. The isolated route control explicitly
selects `Level6BattleEater{AtkHP,DamageShield,Lifesteal}`; scenario assets and
route RNG remain unchanged. This is selected-route calibration, not a natural
campaign victory or acceptance of arbitrary higher Covenant owners.

The boss is authored at 22 attack/1800 HP. Original Covenant01+02 apply the native
zero-attack/+700 HP birth adjustment, yielding 22/2500, with the original single
outer-boss identity, placement, controller and terminal death routing. Birth
checks compare complete creation/placement/controller state and RNG, including
immutable branches. These bosses have no aura.

| Variant | Original PostCombat upgrade | Positive native upgrades | EndTurns | Room stages | Train phases |
| --- | --- | ---: | ---: | ---: | ---: |
| AtkHP | +35 maxHP, +4 spikes | 8 | 4 | 31 | 8 |
| DamageShield | +1 damage shield | 12 | 5 | 48 | 10 |
| Lifesteal | +1 lifesteal | 8 | 4 | 29 | 8 |

The native upgrade targets the first enemy in the boss's room, excludes bosses
and lasts until battle end. The `AtkHP` asset name does not mean an attack buff.
Original snack OnEaten self-damage remains captured and modeled. The native
damage implementations ignore stale shared copy/filter/upgrade editor fields;
their actual damage parameters and targeting retain the existing parser checks.

All three no-card battles are losses. Both individual room/train/EndTurn replay
and sixteen independent immutable full-battle chains match complete state/RNG.
Every process exits normally, preserves protected originals and records zero
CaptureFailures/Mismatches/Unsupported/Pending in `native-accepted-summary.json`.

| Variant | Accepted source profile | Native seconds |
| --- | --- | ---: |
| AtkHP | `full-battle-no-cards-20261023-183347-b373e259` | 49.70 |
| DamageShield | `full-battle-no-cards-20261023-183618-80b41d7d` | 56.82 |
| Lifesteal | `full-battle-no-cards-20261023-183716-af684c80` | 46.78 |

Six archives preserve the three battles and their separate native births:
`full-battle-cael-{atkhp,damage-shield,lifesteal}.mt2f` and
`outer-boss-birth-calibration-cael-{atkhp,damage-shield,lifesteal}.mt2f`.
Their exact bytes/hashes and native sources are registered in `manifest.tsv`.
Focused checks are `--cael-birth-only <archive> <AtkHP|DamageShield|Lifesteal>`
and `--cael-baseline-only <archive> <variant>`.

The player gate now accepts these exact definitions after full payload validation.
Actual advice/UI and a fresh native frozen replay are separately required:
`Run-NativeCovenantPlayerPairs.ps1 -Battle Campaign -Rank 2 -InputProfile <checkpoint>
-BattleDistance 5 -EarlyOuterBossVariant CaelAtkHP -PlannerHorizon 2
-PlannerTimeLimitMs 1500 -TimeoutSeconds 1200 -SkipBuild`.
Substitute `CaelDamageShield` or `CaelLifesteal` for the other two variants.
AtkHP and DamageShield now have accepted pairs below; Lifesteal remains pending.

## Actual AtkHP player pair

Source `full-battle-units-spells-and-junk-20261023-184123-42b1c109` and fresh frozen
replay `full-battle-units-spells-and-junk-20261023-185431-c632e73e` each execute
47 inputs:38 card/ability plays and9 EndTurns, with69 room stages and16 train
phases. The source publishes49 recommendations, each with a fresh complete-root
check;408 captures have zero purity/publication failures. Actual player UI and
every full forecast, root, successor and RNG match the independent replay.
Both processes exit normally (785.63s/449.22s), preserve protected originals and
report all four failure counters zero. Both lose at the disclosed2-turn/1500ms
soft budget. There are no controlled native damage or boss HP changes.

| Archive | Bytes | Nodes | SHA256 |
| --- | ---: | ---: | --- |
| full-battle-player-cael-atkhp-search.mt2f | 122671 | 19681 | 2872fba7a33a8b8d969432e13cb1b1300f8e855aa17dcd87bca3d0e788e1b6e4 |
| full-battle-player-cael-atkhp-replay.mt2f | 118544 | 19431 | ff3cca95b8b4df3d4200cc407b3e55722014a38f7289d712a5e2be5a5e186de0 |

## Actual DamageShield player pair

Source `full-battle-units-spells-and-junk-20261023-190257-83a63cae` and fresh
replay `full-battle-units-spells-and-junk-20261023-191857-83bf943f` each execute
50 inputs:41 plays and9 EndTurns, with81 room stages and18 train phases. All52
source publications have fresh full-root checks;436 captures have zero purity
or publication failures. UI and every forecast/root/successor/RNG match across
independent processes. Both exit normally (958.48s/529.23s), preserve protected
originals and record four zero counters. Both lose with the same disclosed
2-turn/1500ms soft budget and no controlled native damage or boss HP changes.

| Archive | Bytes | Nodes | SHA256 |
| --- | ---: | ---: | --- |
| full-battle-player-cael-damage-shield-search.mt2f | 124203 | 19652 | 5bfa7b47b0decf12905cfa55b18a77169cfea1809c1b8ec9f9d7a8b0f6279305 |
| full-battle-player-cael-damage-shield-replay.mt2f | 119903 | 19381 | 0d2b46b89c633928422c76451a1093a136a3e30f9953dca6fb2582b2e16b5142 |
