# Small Box copies before expensive-card relic modifiers

The tenth natural Banished/Wurmkin sample's sixth battle exposed a guarded
branch at input30: Small Box19 opens three templates while the original
`ApplyDazedOnAscendOrDescend` relic is owned. Skipping this branch removed
two upgraded Just Causes and Hosting Kin from all candidate routes.

Native `CardEffectUnboxSmallBoxUnitCard.ApplyEffect` constructs a card without
starting upgrades, copies permanent and temporary upgrades, then calls
`RelicManager.ApplyCardStateModifiers(resetTempCardModifiers:false)` before
adding the cached card to hand. This differs from ordinary AddCardImpl,
whose relic application precedes copied upgrades. The cached-copy path now
uses the native order and preserves temporary modifiers. Other relic
modifier families and nested boxed templates remain guarded.

Two isolated original-input processes retain the old rejection and the
corrected forecast. All30 native roots/successors and RNG agree. New cards
32/33/34 enter hand in34/33/32 order. The two Just Causes retain respectively
Corrupt and Corrupt/ReduceCostAddConsume/Juice; Hosting Kin has no copied
upgrade. All three are below the relic's threshold and gain no Permafrost.
The natural capture therefore does not claim a high-cost unboxing cast.

The checker also composes copied cost2/3 and retained temporary damage with
the separately native-calibrated original relic. These are model controls,
not additional native observations. Eight parallel unboxing branches retain
their parent. Existing full-hand shuffle, capacity truncation, copied
temporary upgrades, Conduit and original expensive-card relic checks pass.

The floor0 tactical route completes66 inputs,55 plays/11 EndTurns,in two
native processes with Pyre82. The actual10000ms/two-worker default also
completes54 inputs,43 plays/11 EndTurns,in two native processes with Pyre2.
Full forecasts,state and RNG agree; every failure counter is zero. Normal
rewards and maps reach separate Ring7 checkpoints,each at Pyre120. The six
default victories pass their uninterrupted checkpoint chain; the refined
six-battle chain is separately marked mixed. Ring7 is blocked by the newly
purchased AddWurmScaleDamage room-modifier upgrade. No full random campaign
is claimed.

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --unbox-relic-only `
  tests/fixtures/combat/rooms/player-unbox-relic-calibration.mt2f `
  tests/fixtures/combat/rooms/player-unbox-relic-replay-calibration.mt2f
```
