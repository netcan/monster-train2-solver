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
These player pairs are still pending and do not contribute to the accepted count.
