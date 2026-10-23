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
Actual player advice/UI and fresh native replay are still pending; the pair uses
`Run-NativeCovenantPlayerPairs.ps1 -Battle Campaign -Rank 2 -InputProfile <Ring3-checkpoint>
-BattleDistance 2 -EarlyOuterBossVariant ArkionPyre -PlannerHorizon 2
-PlannerTimeLimitMs 1500 -TimeoutSeconds 1200 -SkipBuild` and
`--player-arkion-pyre-pair-only`.

Arkion PushAttack and SiphonRage remain guarded until their own native boundaries
are calibrated. No arbitrary higher-Covenant or natural-victory claim is made.
