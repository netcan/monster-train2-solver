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

Arkion PushAttack and SiphonRage remain guarded until their own native boundaries
are calibrated. No arbitrary higher-Covenant or natural-victory claim is made.
