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

## Windows 0.2.13 delivery

Built from `251c0e97` and installed on 2026-10-24 at 18:18 in
`F:/SteamLibrary/steamapps/common/Monster Train 2/BepInEx/plugins/PojuAdvisor`.
SHA256 values:

| File | SHA256 |
| --- | --- |
| MonsterTrain2Poju.Advisor.dll | 80cbe2e0c4b8bdf3efbe85b022789f9d7dda51ce749ffb742a37e8a0bbdf5335 |
| Model.dll | 1980626a44e6ef11e7ea0531cb36739e8b8a094dcc7b75829f83007893e8b882 |
| FixtureArchive.dll | 32b0b58698d1ed17b06ea4fd4f0aa15723a60a9c10eaca68da4f2216d70b73b7 |

The exact production Model.dll passed both live-root terminal searches,
the native composition's full state/RNG/battle-chain checks, and the real
floor-scroll fixture in an isolated ModelChecks copy. The harness retains
its net8.0 FixtureArchive serializer support; the installed archive is the
netstandard2.1 build. The native isolated witness above used the same model
implementation before the more specific diagnostic error strings were added.
No repeated full regression or macOS build was run. The fixture catalog
passes 1357 archive hashes and curated registrations.

Previous and accepted payloads are retained under
`BepInEx/PojuBackups/MoonContinuation-20261024-181833/{previous,accepted}`.
All 28 other loader/plugin/config file hashes match. F10, whole-battle mode,
two workers and 3000ms are unchanged. The player process is still PID 59976,
started at 17:22:19; replacing disk files does not reload its managed model.
The existing temporary floor/presentation fixes remain active, but this moon
continuation repair requires a normal full game restart. No player cards,
turns, save data or process lifecycle were changed for delivery.
