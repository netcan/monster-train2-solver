# Live moon continuation after a partial forecast

The real Windows game on 2026-10-24 exhausted its retained two-turn forecast
at native turn 4. A new search expanded 32 nodes but completed no turns:
`CardMoonUpgradeModel.RefreshHand` rejected the original Covenant 10 prefix
and its active relic composition when Silver Strike entered the hand.
This was an obsolete evidence guard, unrelated to game-version compatibility.

`player-live-moon-continuation.mt2f` preserves that actual root, the earlier
turn-2 root, original partial forecast, and unavailable diagnostics. Under
the unchanged 256-node / two-worker / 3000ms whole-battle settings, both roots
now yield terminal forecasts (three and five further turns, respectively).
The focused checker independently replays every step and complete state/RNG,
checks parent immutability, and rejects ten malformed composition controls.
The observed default-budget forecast is a defeat, not proof that no winning
line exists. Whole-battle mode remains bounded search, not exhaustive search.

The composition rule follows the native callback boundaries:

- Preserve exact ordered cumulative Covenant 1-10 definitions. Startup,
  reward, merchant and wave-selection effects are already in the capture;
  enemy birth modifiers retain their separate modeled handlers.
- Native sealed starting-hand balance and draft-rarity effects have no moon
  hand-update callback. Their identity alone does not restrict composition.
- FreezeReduceCost keeps its exact original trait-filtered, end-of-turn cost
  handler. Monster-only acquisition upgrades, including PyreHeartSavagery,
  cannot apply again when a Spell manages its moon upgrade.
- Unknown moon callbacks and changed definitions remain rejected. Errors now
  distinguish missing phase/selection, Covenant signatures and the relic name.

The isolated native witness
`full-battle-original-moon-card-upgrades-live-relics-covenant10.mt2f` starts
through native Covenant 10 run setup and acquires unchanged Enemy_LowerDraw,
FutureDraftsAreRares, FreezeReduceCost and PyreHeartSavagery definitions.
It retains 15 original spell/control plays, five EndTurns, seven spawn
boundaries, full moon-hand callback contexts and sixteen immutable battle
replays. Capture failures, mismatches, unsupported and pending are all zero;
the native game wins and the real player's files remain unchanged.
Durable witness units and controlled hands establish mechanism ordering,
not natural deck win rates or complete campaign coverage.

This witness also exposed a genuine birth-order bug: native
`CharacterState.Setup` establishes moon understanding before
`PlayerManager.OnCharacterSpawned` applies Covenant relic effects. Enemy
spawning now follows that order, preserving `relentless, fullmoon, buff`
status registration. Historical live scroll captures are unchanged; their
older model-generated forecast inputs are replayed under the corrected model
instead of treating an archived prediction as a future native observation.

Focused commands (no full regression):

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --live-moon-continuation-only tests/fixtures/player-live-moon-continuation.mt2f
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --live-moon-composition-only tests/fixtures/full-battle-original-moon-card-upgrades-live-relics-covenant10.mt2f
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --live-floor-scroll-only tests/fixtures/player-live-conduit-floor-scroll.mt2f
./scripts/Run-FullBattleProbe.ps1 -NativeWorker 2 -Seed 424242 -OriginalMoonCardUpgradesLiveRelics -StartingCovenantLevel 10 -SkipBuild -TimeoutSeconds 240
./scripts/Check-FixtureCatalog.ps1
```
