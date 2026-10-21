# Player Mod validation · 2026-10-21

The player entry point is `src/Advisor/AdvisorPlugin.cs`. The 0.2.0 baseline below
is followed by the 0.2.1 focused results.
It compiles against Mono game 2.2.1, module
`8fb07b96-f4db-4d2b-884d-c00536d6ccf4`, as an AnyCPU netstandard2.1 assembly.
The assembly excludes `ProbePlugin` and `NativeReplayScenario`. Startup selects
nine observation/invalidation patches explicitly, including spawn-point copy
identity tracking and original source-trait capture for generated cards. It does
not install every patch in its assembly.

## Focused results

- Both Advisor and the existing Probe Release builds succeed. The ordinary
  unsuppressed first build retains existing nullable warnings in reused code.
- `--battle-advice-only full-battle-planner-starter-search.mt2f` passes stale
  result rejection, latest-request coalescing, single-worker cancellation,
  replay-verified budget fallback, failures and nonblocking disposal.
- Both macOS `.command` files pass Bash syntax checking. Package validation
  checks exact payload membership, all internal SHA256 values, LF scripts,
  AnyCPU IL, absence of development entry points and a clean source revision.
- The unmodified official macOS BepInEx 5.4.23.5 ZIP matches GitHub's release
  asset digest `01c2ae782eb016dfd6c345a18dbd2dcafffb3d9d318449d6486689f426b4a323`.

## Windows native smoke

Temporary player DLLs were loaded alongside the isolated development driver in
the copied Windows game. The original save/log signatures remain unchanged.
The developer driver supplies game inputs; the player Mod only observes and
publishes recommendations. These inputs do not establish a separate native
replay of the player Mod's recommendations.

The first run omitted `-Planner`; the fast driver completed before advice was
published, so its advisor acceptance gate failed. This is not counted as a
successful advisor test. The corrected focused run used:

```powershell
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 `
  -Policy units-spells-and-junk -Planner -Advisor `
  -PlannerHorizon 3 -PlannerTimeLimitMs 3000 -BinaryCapture -SkipBuild
```

Source profile: `.probe-runs/full-battle-units-spells-and-junk-20261021-190926-e274fc76`.
The existing gate passes. The battle finishes in 80.70 seconds, with a native
win, 21 card actions, seven EndTurns, settled terminal effects and zero capture,
mismatch, unsupported and pending counters. The player Mod logs `POJU-READY`
and two `POJU-ADVICE` publications (12/11 steps, 3064.4/3162.5 ms), both with
`capturePurityFailures=0`. No player capture/overlay error is present.
The native panel render assertion passes for the development overlay; it is
not a separate render assertion for the player's canvas.

No full fixture regression was run for packaging. macOS loader startup,
Apple Silicon runtime compatibility, font rendering and actual Mac battles
remain unverified. `docs/macOS.md` and the package's `BUILD.json` preserve that
boundary; the installation preflight must not be reported as runtime acceptance.

## Version 0.2.1

The panel now has a persistent mouse toggle, a draggable title constrained to
the canvas, and configurable `PauseWhenHidden` (default true). Hiding cancels
the active advice request; showing invalidates its old snapshot before capture
resumes. Dynamic fonts are reused across canvas recreation and released on
disposal; player timing history retains only the latest 128 publications.

Both Release builds and the focused advice-session check pass. One development
advisor native run validates the shared updated overlay and existing planner:
`.probe-runs/full-battle-units-spells-and-junk-20261021-192259-9c8d2b52`.
It finishes in 76.80 seconds with a native win, 21 actions, seven EndTurns,
settled terminal effects, protected files unchanged and four zero counters.
Two advice publications have 12/11 steps and zero capture-purity failures.
The rendered panel has 352 text vertices and 85,699 nonbackground pixels;
visual inspection confirms the new mouse toggle and the recommendation text.
Mouse clicking/dragging and Mac font/loader behavior still require interactive
validation; this run does not assert those interactions.

`tests/packaging/check-macos-install.sh` runs against temporary file fixtures
under Git Bash, without a game or macOS runtime. It verifies first install,
upgrade preservation of config/extra files, Unicode/space paths, rollback after
an injected launcher replacement failure, temporary-directory cleanup, and
rejection of incomplete BepInEx, BepInEx 6 and renamed development probe DLLs.
All three product shell files pass Bash syntax checks. This verifies portable
file operations and failure handling, not actual macOS loader compatibility.

The installer performs existing-runtime checks even with `--check-only`, stages
replacement DLLs on the game's volume and retains the previous complete plugin
folder/launcher. Build version is read from `Advisor.csproj`; the package now
contains the shared `poju-common.sh` helper in its checksum coverage. No full
fixture regression was run for this increment.

## Real player assembly acceptance

`Run-FullBattleProbe.ps1 -PlayerAdvisor` loads the actual standalone player DLL
with the isolated native input driver, without creating a development overlay.
The driver reads the player's publication, checks its full root key against a
fresh pure native capture, and executes its frozen steps through the game UI.
Player and driver allocate separate capture IDs; native object references map
chosen card/target/activator IDs between them. Expected and actual planner
states stay in the player's identity space. The independent development trace
continues to check native effects in its own identity space. Choice-card ID
translation is deliberately rejected by this focused acceptance driver.

The harness dispatches Unity pointer events to the actual buttons after checking
the foremost raycast hit. It checks pause/resume, preservation of manual pause
across hiding/showing, cancellation while searching, no captures/publications
while paused or hidden, new generations after showing/refreshing, and dragging
to both canvas boundaries. This checks Unity input routing and callbacks; it
does not simulate hardware mouse input or establish Mac input compatibility.
It also renders the actual player canvas. The acceptance bridge is excluded
from the player assembly and rejected by package validation if present.

```powershell
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 `
  -Policy units-spells-and-junk -PlayerAdvisor `
  -PlannerHorizon 3 -PlannerTimeLimitMs 3000 -BinaryCapture -SkipBuild
```

Successful source profile:
`.probe-runs/full-battle-units-spells-and-junk-20261021-193549-f77c2b1f`.
The native battle wins in 106.29 seconds with 21 card actions, seven EndTurns,
settled terminal effects, protected files unchanged and four zero counters.
The player's 12-step, three-turn plan completes every native comparison;
score is -79931.1 against the no-card baseline -73884.4. Four publications have
zero capture-purity failures. The player's rendered canvas has 352 text vertices
and 85,699 nonbackground pixels. `PLAYER-ADVISOR-UI-PASS` and
`PLAYER-ADVISOR-PASS` both pass. This supersedes the earlier Windows player
recommendation/render/Unity pointer-event gaps; macOS acceptance remains open.

Two initial harness attempts correctly refused consumption: the first had
different branch-mask capture settings, and the second exposed separate unit-ID
allocation between assemblies. Neither is counted as successful evidence.
Temporary player DLLs/config are archived inside the isolated run profile;
the worker's prior config is restored even when acceptance fails.

### Independent frozen-window replay

The next fresh process uses the recorded player sequence without consuming new
search results:

```powershell
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 `
  -Policy units-spells-and-junk -PlayerAdvisor -BinaryCapture -SkipBuild `
  -PlannerInput .probe-runs/full-battle-units-spells-and-junk-20261021-193549-f77c2b1f/full-battle.mt2f
dotnet run --project src/ModelChecks/ModelChecks.csproj -c Release --no-build -- `
  --planner-pair-only `
  .probe-runs/full-battle-units-spells-and-junk-20261021-193549-f77c2b1f/full-battle.mt2f `
  .probe-runs/full-battle-units-spells-and-junk-20261021-193815-14a08f1f/full-battle.mt2f
```

Replay profile: `full-battle-units-spells-and-junk-20261021-193815-14a08f1f`.
It wins in 72.20 seconds, with 21 actions, seven EndTurns, settled terminal
effects, protected files unchanged and four zero counters. The independent
model checker passes all 12 steps in both processes, identical roots, frozen
inputs, final full state and RNG; the replay record has zero search nodes.
The observing player worker can still run in this replay process, but none of
its newly computed recommendations chooses an input.

### Whole-battle player advice

`-PlayerAdvisor -RollingAdvice` consumes only the first step of each new real
player publication, then requires a new matching publication for the next input.
Its native terminal capture occurs after `StopCombatLoop`, before encounter
cleanup can reset player capture identities. This capture hook exists only in
the isolated development driver.

```powershell
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 `
  -PlayerAdvisor -RollingAdvice -PlannerHorizon 3 -PlannerTimeLimitMs 3000 `
  -BinaryCapture -SkipBuild -TimeoutSeconds 300
```

Source profile: `full-battle-units-spells-and-junk-20261021-194138-1f85eb5c`.
The unmodified natural encounter wins in 220.43 seconds. All 27 inputs (20 card
actions and seven EndTurns) come from player advice; 29 publications include
the initial UI checks. Terminal effects settle, protected files remain unchanged,
and capture/mismatch/unsupported/pending counters are all zero. The independent
`--rolling-advice-only` checker replays every chosen action and every published
continuation, verifies their states/RNG and confirms the final native victory.

`--rolling-replay-pair-only <search> <replay>` checks the two native sequences
without requiring a fixed-policy baseline or claiming an improvement over one.
The existing development ring-one search/replay control passes this checker too.
Only focused checks are run; no full fixture regression is needed for this
acceptance-driver increment.

The fresh whole-battle replay profile is
`full-battle-units-spells-and-junk-20261021-194538-e963d99e` (64.85 seconds).
It executes the same 27 frozen inputs with zero search nodes in every replay
record, wins with 80 Pyre HP, settles terminal effects and preserves protected
files; all four failure counters are zero. `--rolling-replay-pair-only` passes
every input, root, successor, complete published forecast and RNG comparison
across the two native processes. The source player overlay reports 350 pure
captures and 29 publications. This covers a natural first battle, not arbitrary
decks, later encounters or macOS execution.

The refreshed trial package is
`results/mod-dist/0.2.1-validated/PojuAdvisor-0.2.1-macOS.zip`, 2,918,539 bytes,
from clean source `e12ce8ef27cff5d749bf860b819e59cd3378b4a2`.
ZIP SHA256:
`20576252143c348118f4fd694ad48559b6c83c858729d8e9f1ea2e5e307f80ac`.
Package validation passes all 12 files, checksums, AnyCPU metadata and exclusion
of the player acceptance bridge. All three packaged DLL hashes equal the files
used by the successful fresh native replay; the archived player DLL SHA256 is
`5fe13c5448d2d128678d8908b717818bd73817921c175c89f2e9ef1586b4a00d`.
The product version remains 0.2.1; this increment strengthens validation and
does not establish Mac runtime acceptance.

## Natural Luna Coven player battle

Player capture now checks native moon-effect/trait signatures and the shared
moon transition validator instead of requiring a development scenario switch.
It captures unit moon masks only while the native deck activates moon tracking.
Unknown effects and model restrictions remain explicit refusals, including
Conduit with physical movement and uncalibrated room-mask callback combinations.

`Run-FullBattleProbe.ps1 -PlayerAdvisor -RollingAdvice -PrimaryClan LunaCoven`
selects the clan through the native run-setup handler. Only the disposable
profile's clan unlock and moon tutorial marker are prepared; the game generates
the starting deck, champion, relics and encounter without card/stat edits.
`PrimaryClan` cannot be combined with a resumed checkpoint. The player acceptance
bridge now reports an unavailable plan immediately instead of waiting for timeout.

Search profile `full-battle-units-spells-and-junk-20261021-200103-eb0de862`
wins in 282.64 seconds with 80 Pyre HP: 38 decisions, 31 card plays, seven EndTurns,
40 publications and 483 pure captures. The UI checks pass (340 vertices,
85,699 visible pixels). Fresh profile
`full-battle-units-spells-and-junk-20261021-200558-4a5085d8` wins in 73.30 seconds
using the same 38 frozen inputs with zero search nodes. Both runs settle terminal
effects, preserve protected files and have zero capture/mismatch/unsupported/pending
counters. Full roots, successors, published continuations and RNG match.

The complete archives are committed as
`tests/fixtures/full-battle-player-moon-search.mt2f` and
`tests/fixtures/full-battle-player-moon-replay.mt2f`. Run
`ModelChecks --player-moon-pair-only <search> <replay>` to verify both native
sequences, both moon phases, repeated shifts, the retained native mask bit and
Witchweave casts in each phase. The existing set/advance/unit-callback fixtures
and pure transition controls also pass. No full regression was run.

This establishes the natural Luna Coven starter battle in the real player
entry point. It does not establish every later moon deck, room/relic combination,
final boss or macOS runtime.
