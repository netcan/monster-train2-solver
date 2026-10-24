# Pyre-health early stopping · Windows 0.2.20

Player search defaults to `Search/StopAtBestPyreHealth=true`. A complete
`BattleWon` forecast stops both workers as soon as its final Pyre health equals
a conservatively established upper bound. The panel identifies this reason;
`POJU-ADVICE` logs `stoppedEarly` and `pyreHealthBound`. The default public model
API remains opt-in so existing fixed-budget consumers keep their behavior.

The objective remains victory first, then surviving Pyre health. This proves
that objective cannot improve within the modeled root, not global optimality
across secondary positional preferences, hidden information or unsupported
mechanics. A damaged winning forecast below the bound keeps searching.

The upper-bound inspection runs once on the background coordinator over the
complete immutable graph, including future waves, summoned units and generated
card definitions. Without train healing, current HP is the bound. Train healing
requires a captured, validated native resource maximum, which becomes the bound.
Ordinary unit healing cannot restore the captured native unhealable Pyre.

Unbounded permanent maximum-health changes, resurrection, direct health
replacement/stat swaps, unknown direct Pyre operations or ambiguous Pyre state
disable early stopping. Potential healing without captured resource limits also
disables it. Search then retains its usual time/node/step limits.

Workers share only an atomic success signal; this does not cancel the returned
winning plan. The existing independent full-state/RNG replay still runs before
publication. A sibling can finish its current atomic simulation before observing
the signal. User cancellation remains cancellation, and the UI never joins a
search worker. Verified continuations preserve early-stop provenance.

## Focused validation

```powershell
dotnet build src/ModelChecks/ModelChecks.csproj -c Release --no-restore
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --early-stop-only `
  tests/fixtures/full-battle-player-conduit-search.mt2f `
  tests/fixtures/full-battle-train-heal.mt2f `
  tests/fixtures/full-battle-player-cross-turn-scroll-search.mt2f
```

The native winning control reaches the same 80-HP score with 3 expanded nodes
instead of 43 in serial search; two workers jointly use 4–5 nodes (scheduling
dependent). Both returned routes pass independent complete state/RNG replay.
The checker also covers a lower-HP win that must keep searching, native PayOff
healing from 70/80 to 74/80, future-wave maximum growth, resurrection, missing
resource limits, custom evaluation, expiry, user cancellation, sibling success,
continuation metadata and root immutability. These are targeted checks; no full
fixture regression is required for this delivery.

## Native Windows acceptance

Actual player 0.2.20 in isolated profile
`.probe-runs/full-battle-units-spells-and-junk-20261024-221343-553b1e0d`
searches the native turn-four position. Three searches (initial and UI refresh
controls) stop in 388.4, 379.6 and 369.9 ms under a 3000-ms budget, each with two
worker IDs distinct from Unity's thread. Its two-step route is executed by the
native game and wins at the 80-HP bound. The remaining one-step publication
retains early-stop metadata and performs zero search work.

The process exits normally after 73.39 seconds: 19 total plays, five EndTurns,
42 compared stages, all four failure counters zero, terminal effects settled
and protected original files unchanged. This scenario authors Steward/equipment
carriers while retaining original relics, waves and boss; it is a focused
functional acceptance rather than a natural-deck performance benchmark.

The permanent archive is `full-battle-player-early-stop-search.mt2f`. Its dedicated
checker independently replays every frozen step against complete native states
and RNG, and checks the player entry point, bound, worker identities, below-budget
searches and zero-search continuation. It is registered in `Check-Models.ps1`;
the catalog gate validates all 1361 archive hashes/registrations without battle
simulation.

```powershell
./scripts/Run-FullBattleProbe.ps1 -NativeWorker 2 -Policy units-spells-and-junk `
  -OriginalConduitRelics -PhysicalSpawnPoints -PlayerAdvisor -Planner `
  -PlannerTurn 4 -PlannerWholeBattle -PlannerSearchWorkers 2 -SkipBuild `
  -TimeoutSeconds 300
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --early-stop-native-only `
  tests/fixtures/full-battle-player-early-stop-search.mt2f
```

Two earlier complete player runs with bottom/middle deployment
(`20261024-221002-e515edbd`, `20261024-221154-a2c48c3f`) win at 79/80 and 74/80.
Neither falsely reports early stopping. Both exit normally with zero native
failure counters and unchanged protected originals. The existing parallel
planner and rapid-input/floor-selection checks also pass. The exact installed
Model DLL passes the focused objective-bound checker through the offline harness.
No full fixture simulation regression was run.

## Windows delivery

Installed at 2026-10-24 22:16:44 from `d11e2b65` to
`F:/SteamLibrary/steamapps/common/Monster Train 2/BepInEx/plugins/PojuAdvisor`.
All three installed DLL hashes match the native-accepted build; 28 other loader,
plugin and configuration files remain unchanged. Previous and accepted payloads
plus `installation.json` are under `BepInEx/PojuBackups/EarlyStop-20261024-221643`.
The Windows package is
`results/mod-dist/PojuAdvisor-0.2.20-Windows-20261024-221643.zip`.
No Mac build or real player input/restart was performed. The running real player
PID74796 loaded 0.2.19 and requires a user restart to load 0.2.20.
