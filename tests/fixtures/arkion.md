# Original Arkion variants

## Conditional Pyre damage

Source `full-battle-no-cards-20261023-185417-24df2aa4` explicitly selects the
original `Level3BattleHeavyHitter_PyreDamage` scenario from the native seed424242
Covenant2 checkpoint after two original victories. Authored scenario/boss/deck
assets and route RNG remain unchanged. This is selected-route calibration.

The original boss is 4/425 with one multistrike. Native Covenant01+02 add zero
attack/+200 HP at birth, producing 4/625. Its sole empty boss action uses
`RandomBossActionBehavior` and the authored None team parameter, unlike the
occupied-player-room controller used by the other calibrated ordinary bosses.
Complete native creation, birth, physical placement and controller/RNG match.
Multistrike is accepted only on this exact newly calibrated boss identity.

Original PostCombat `CardEffectPyreDamageIfNoPlayerUnits` deals five remote Pyre
damage when its current room contains no player units. Four positive native room
stages retain that exact damage. The pre-existing ordinary conditional-damage
model still tests the caster's room and uses the captured remote Pyre.

The native no-card loss contains five EndTurns,40 room stages and10 train phases.
All individual stages and sixteen independent immutable full-battle branches
match complete state/RNG. The process exits normally in46.97 seconds, preserves
protected originals and records all four failure counters zero.

| Archive | Bytes | Nodes | SHA256 |
| --- | ---: | ---: | --- |
| full-battle-arkion-pyre.mt2f | 34863 | 4809 | 8a5e4132a319d448e19c0fadb8e1587506c4f7f7f71691f19efc37a1c357fcb0 |
| outer-boss-birth-calibration-arkion-pyre.mt2f | 13964 | 1738 | 867dbe8842bd47ed85bbe21539c924bb9411c0b9fd33aeff5f7ec87419b518f1 |

Focused checks: `--arkion-pyre-birth-only` and `--arkion-pyre-baseline-only`.
Actual player advice/UI and fresh native replay now pass; the pair uses
`Run-NativeCovenantPlayerPairs.ps1 -Battle Campaign -Rank 2 -InputProfile <Ring3-checkpoint>
-BattleDistance 2 -EarlyOuterBossVariant ArkionPyre -PlannerHorizon 2
-PlannerTimeLimitMs 1500 -TimeoutSeconds 1200 -SkipBuild` and
`--player-arkion-pyre-pair-only`.

Source `full-battle-units-spells-and-junk-20261023-185636-d31b3318` and replay
`full-battle-units-spells-and-junk-20261023-190512-2d51b94e` both win. Each executes
43 inputs,34 card/ability plays and9 EndTurns, with69 room stages and17 train
phases. All45 source publications have fresh complete-root checks;373 captures
have zero purity/publication failures. Actual player UI and every full forecast,
root, successor and RNG match across independent processes. Both exit normally
(513.82s/315.10s), preserve protected originals and report four zero counters.
These selected original encounters retain their two-win checkpoint history and
the disclosed2-turn/1500ms soft budget; they are not natural-route victory claims.

| Archive | Bytes | Nodes | SHA256 |
| --- | ---: | ---: | --- |
| full-battle-player-arkion-pyre-search.mt2f | 90513 | 14057 | bd139aa8127dd18b8db618f2f4f8d3ae6953e4d85123b4b1797a40888c76f556 |
| full-battle-player-arkion-pyre-replay.mt2f | 87072 | 13829 | 9da3315cd4ee7798a4bd5c0ba1c132b9c2e52ec56aa6821bf8219b26a9286748 |

## Siphon Rage and Push Attack

Both remaining original variants now pass rank-two birth, complete no-card
battles and legal native fixed-spell victories from the same two-win checkpoint.
The original scenario selection preserves authored assets and route RNG.
SiphonRage is raw7/425 and native7/625. Its original enemy relic installs
`OnAnyHeroDeathOnFloor`, granting self Rage2 per same-floor enemy death. One
positive native combat Harvest matches exactly before ordinary room-end decay.
PushAttack is raw13/425 and native13/625. Its attack-before-damage trigger moves
the original attacked player to the back, retaining that actor as the damage
target. Three surviving original-victim back/hit witnesses match. The native
FloorRearrange iterator ignores its stale authored dazed editor field; capture
and validation preserve that behavior only for the calibrated trigger signature.

| Variant / native source | Outcome | Plays / EndTurns / Rooms / Train | Seconds |
| --- | --- | --- | ---: |
| Rage baseline `190200-a0875091` | loss | 0 / 5 / 41 / 10 | 56.88 |
| Rage fixed spells `190316-1667938d` | win, Pyre47 | 26 / 8 / 65 / 15 | 247.16 |
| Push baseline `191306-ee2fe498` | loss | 0 / 5 / 41 / 10 | 56.10 |
| Push fixed spells `191306-db575296` | win, Pyre37 | 26 / 8 / 70 / 15 | 235.29 |

All source profiles have the prefix `full-battle-no-cards-20261023-` for
baselines or `full-battle-units-spells-and-junk-20261023-` for fixed spells.
Each process exits normally, preserves protected originals and records four
zero failure counters. Every native stage, full policy, mid-battle suffix and
sixteen independent immutable full chains match complete state/RNG.
Focused checks are `--arkion-{rage,push}-{birth,baseline,growth}-only`.
The Push actual player-source/replay pair now also passes, as recorded below.

Rage player source `full-battle-units-spells-and-junk-20261023-191122-701b1c19`
and replay `full-battle-units-spells-and-junk-20261023-192114-a474fc70` both win.
Each executes44 inputs,35 plays and9 EndTurns, with68 room stages and17 train
phases. All46 source publications have fresh complete-root checks;381 captures
have zero purity/publication failures. UI and every forecast/root/successor/RNG
match across independent processes. Both exit normally (589.09s/344.09s), retain
protected originals and record four zero counters. The pair retains the native
two-win history and disclosed2-turn/1500ms soft search budget.

| Archive | Bytes | Nodes | SHA256 |
| --- | ---: | ---: | --- |
| full-battle-player-arkion-rage-search.mt2f | 109250 | 15992 | a45abc392316c696e23738ddc71aa1d72958f1f7fb65e50de35bfed44e12b745 |
| full-battle-player-arkion-rage-replay.mt2f | 105491 | 15755 | c9c5552d02a3077914bd345cdf19178d6a68f7a420a3cf70c0a0de9e0352216f |

| Archive | Bytes | Nodes | SHA256 |
| --- | ---: | ---: | --- |
| full-battle-arkion-rage-baseline.mt2f | 49816 | 6161 | 4b2c9dcb37b66f1446f35cd94a2fe415bb1df008403ae70edba685d291a91d00 |
| outer-boss-birth-calibration-arkion-rage.mt2f | 29465 | 3098 | c461c741d5d8853f3095e97ff9d59214fb21b98a234e5996407f2fe964fd4d6c |
| full-battle-arkion-rage-native-spells.mt2f | 83573 | 12673 | 62b6fb959ba04c9be9ca2147b0cd633721ffaabbaa7deb655151cf4d69cdb947 |
| full-battle-arkion-push-baseline.mt2f | 34702 | 4801 | 659322418a2e24d81fc275b6a29a7b01c73a04c727bf7bdd8235a803b34c4965 |
| outer-boss-birth-calibration-arkion-push.mt2f | 14020 | 1736 | 4e9956402c187410df18ef55e2e32d555256df07cc79d6750f50ce35218f0db1 |
| full-battle-arkion-push-native-spells.mt2f | 71599 | 11974 | 4df47ab49d4913eb57f122ef0fbbe098a170463573bbe735aae8da26ef23de79 |

Push source `full-battle-units-spells-and-junk-20261023-193227-208b639c` and
fresh replay `full-battle-units-spells-and-junk-20261023-194020-5463f741` both win.
Each executes39 inputs,30 plays and9 EndTurns, with71 room stages and17 train
phases. All41 source publications have fresh full-root checks;342 captures
have zero purity/publication failures. Actual UI and every forecast/root/
successor/RNG match across independent processes. Both exit normally
(469.65s/264.97s), retain protected originals and record four zero counters.
The pair uses the same two-win checkpoint and2-turn/1500ms soft search budget.
Triggered rearrangement now dispatches native OnShift/OnSentry callbacks for
the original victim and displaced actors, in the observed movement order.
The earlier source `192545-26f1802f` stopped at the uncalibrated callback guard
and is retained only as a diagnostic, not accepted evidence.

| Archive | Bytes | Nodes | SHA256 |
| --- | ---: | ---: | --- |
| full-battle-player-arkion-push-search.mt2f | 91331 | 14330 | a82951d30b3738e855d094ca0c2a3de593bea47331b1814519bebf73ab48a058 |
| full-battle-player-arkion-push-replay.mt2f | 88143 | 14116 | 513392861bf31cf05578d3d672bbe34388f2436a872b3398e5c167ab21c4508c |

No arbitrary higher-Covenant or natural-victory claim is made.
