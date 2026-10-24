# Retired aura sources and per-ember Pyre healing

Native MVID: `8fb07b96-f4db-4d2b-884d-c00536d6ccf4`.

## Card kills and deferred destruction

`full-battle-retired-aura-card-kill.mt2f` uses the unchanged naturally progressed
checkpoint `.probe-runs/campaign-ring7-20261022-075901-c305fc5f-before-ring7`.
That route skips merchant purchases before Ring 7; no deck/save edits are used.

```powershell
./scripts/Run-FullBattleProbe.ps1 -Policy units-spells-and-junk `
  -InputProfile .probe-runs/campaign-ring7-20261022-075901-c305fc5f-before-ring7 `
  -Seed 424244 -BattleDistance 6 -NativeWorker 2 -PhysicalSpawnPoints `
  -BranchCardMasks -SettleDeathDissolves -SettleCardAnimations `
  -SkipBuild -TimeoutSeconds 480
```

Source: `.probe-runs/full-battle-units-spells-and-junk-20261022-081853-28a3007c`.
Thirty-six plays and eight EndTurns reach a native loss with all four failure
counters zero, terminal effects settled and protected native files unchanged.
The focused check observes two removed-but-retained native sources, three bound
aura snapshots after card kills and eleven unbound snapshots after turn end.
Full state/RNG and sixteen immutable complete replays match.

`CharacterState.IsDestroyed` includes `BeginDestroy` and `InRemoveList`.
`MonsterManager.ProcessRemovals` / `HeroManager.ProcessRemovals` schedule actual
Unity destruction later, at the combat loop's turn-end removal boundary. Waiting
for every removed aura source before accepting another card caused an indefinite
wait. The player now waits only for an actually scheduled Unity destruction;
the model preserves mid-turn bindings and releases them at turn end. Animation
movement/discard checks remain active. This fixed-policy loss establishes the
mechanism, not a Ring 7 player-advisor victory.

## Healing from the original card cost

`full-battle-train-heal-per-ember.mt2f` captures original
`TraitHealPyrePerEmber` / `CardEffectHealPyrePerEmberCost`.

```powershell
./scripts/Run-FullBattleProbe.ps1 -TrainHealPerEmber -Seed 424242 `
  -NativeWorker 1 -SettleDeathDissolves -SettleCardAnimations `
  -SkipBuild -TimeoutSeconds 240
```

Source: `.probe-runs/full-battle-units-spells-and-junk-20261022-082256-8e86f2c4`.
Explicit controls add three original Train Stewards and one MeatShield, each
carrying the unchanged original healing upgrade. Three carriers receive an
explicit cost-reduction upgrade; the third card's raw cost is set to three.
The Pyre starts five HP below its native maximum. No unit stats are edited.

| Carrier | Raw cost | Paid cost | Native healing |
| --- | ---: | ---: | ---: |
| Train Steward | 1 | 1 | 1 |
| Discounted Train Steward | 1 | 0 | 1 |
| Raw-cost control Train Steward | 3 | 2 | 3 |
| Discounted MeatShield at full HP | 1 | 0 | 0 |

The native battle wins with 80 Pyre HP: 23 plays, seven EndTurns, all four failure
counters zero and settled terminal effects. Complete state/RNG and sixteen
independent immutable branches match. Resource capture now includes card-trigger
healing upgrades after their owner leaves hand. Negative healing remains
explicitly rejected pending native calibration.

## Healing from paid X cost

`full-battle-train-heal-per-ember-x.mt2f` adds two original Train Stewards and
two original Vent cards, all carrying the unchanged original healing upgrade.
The second Steward has an explicit one-ember discount. Both Vent carriers have
an explicit +2 X upgrade and Freeze so they remain in hand through deployment.
The Pyre starts twelve HP below its original maximum; unit stats, original
Vent damage and native waves remain unchanged. This is an authored mechanism
calibration, separate from natural deck or player-entry acceptance.

| Carrier | Paid cost | Extra X | Native healing |
| --- | ---: | ---: | ---: |
| Train Steward | 1 | 0 | 1 |
| Discounted Train Steward | 0 | 0 | 1 |
| Vent | 3 | 2 | 3 |
| Vent | 0 | 2 | 0 |

The native effect reads GetCardPlayedCost with allowModifications false during
resolution. The model reuses its UnmodifiedPlayedCost query, including the
native energy fallback when a paid-cost entry is absent. Settled decisions
clear transient played-cost entries, so the oracle is actual energy spent and
native HP changes, followed by complete action/turn state and RNG comparison.

Source `full-battle-units-spells-and-junk-20261022-124156-a51e568d` wins with
Pyre75, 20 plays/seven EndTurns, 58 matching room stages and all four failure
counters zero. It records DEPTH-PASS and exits normally after 107.99 seconds;
protected originals remain unchanged. Sixteen independent immutable full-battle
branches match the native chain. The fixed-cost/capped controls still pass.
Archive: 47,602 bytes/7,567 nodes; SHA-256
`56488383b845d5e9e8b2c6dc7b96bf832d2d5ba850d77b8c0457e427a97919cb`.

```powershell
./scripts/Run-FullBattleProbe.ps1 -TrainHealPerEmberX -NativeWorker 1 `
  -SettleDeathDissolves -SettleCardAnimations -SkipBuild -TimeoutSeconds 240
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --ember-train-heal-x-only `
  tests/fixtures/combat/damage/full-battle-train-heal-per-ember-x.mt2f
```

Focused checks:

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --retired-aura-only `
  tests/fixtures/combat/core/full-battle-retired-aura-card-kill.mt2f
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --ember-train-heal-only `
  tests/fixtures/combat/damage/full-battle-train-heal-per-ember.mt2f
```
