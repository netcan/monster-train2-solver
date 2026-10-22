# Player Mod validation · 2026-10-21

The player entry point is `src/Advisor/AdvisorPlugin.cs`. The 0.2.0 baseline below
is followed by the 0.2.1, 0.2.2 and 0.2.3 focused results.
It compiles against Mono game 2.2.1, module
`8fb07b96-f4db-4d2b-884d-c00536d6ccf4`, as an AnyCPU netstandard2.1 assembly.
The assembly excludes `ProbePlugin` and `NativeReplayScenario`. Startup selects
eleven observation/invalidation patches explicitly, including spawn-point copy
identity tracking, original source-trait capture for generated cards and shared
reference tracking for queued next-draw upgrades and native trigger creation
order. It does
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
`-PrimaryClan Hellhorned -SecondaryClan Banished` selects both clans explicitly.
Legacy clan selection also unlocks the native Crew feature in the disposable
profile; DLC availability checks remain active. Neither clan selector can be
combined with a resumed checkpoint, and the two requested clans must differ.
The player acceptance bridge reports an unavailable plan immediately instead
of waiting for timeout.

The refreshed 0.2.2 search profile
`full-battle-units-spells-and-junk-20261021-201209-120850e8`
wins in 304.33 seconds with 80 Pyre HP: 38 decisions, 31 card plays, seven EndTurns,
40 publications and 486 pure captures. The UI checks pass (340 vertices,
85,699 visible pixels). Fresh profile
`full-battle-units-spells-and-junk-20261021-203819-580f52ed` wins in 83.65 seconds
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

Player capture now also waits for native retired-character destruction at the
frame boundary. A dead character can have `IsDestroyed == true` while its Unity
reference and aura binding remain live until the frame ends. Reading that
intermediate state caused a full-state discrepancy; the observer waits without
changing the native cleanup. The fresh moon replay above uses this settlement
gate and still matches all 38 inputs, full roots, successors, forecasts and RNG.

The player now installs the read-only next-draw queue identity observer too.
A final-boss diagnostic previously left `NextUpgradeReferenceId` at 1 after
Soldier of Fortune while the model expected 2. Frozen diagnostic profile
`full-battle-units-spells-and-junk-20261021-203527-1128a495` reaches all 12 recorded
inputs and matches that successor at 2; it then stops because the diagnostic
input ends before combat completion. This is a focused failure reproduction,
not a completed battle acceptance. The original Soldier of Fortune model
fixture also passes its paid/unaffordable draw, shared-upgrade and full-chain
checks.

The frozen driver waits for the separately loaded overlay to establish its
native battle identity space before issuing inputs. Deep player UI checks also
receive time for all three publications and their full forecast verification.
The source plan's configured budget is unchanged. Frozen root errors include
the first differing field, and a diagnostic source's old error is cleared before
the new observation. Both player acceptance checkers explicitly require the
player assembly in source and replay processes.

This establishes the natural Luna Coven starter battle in the real player
entry point. It does not establish every later moon deck, room/relic combination,
final boss or macOS runtime.

## Delivered 0.2.3 Hellhorned/Banished check

The requested main Hellhorned / secondary Banished pair was selected through
native setup in a fresh disposable profile, seed `20261021`. The three Advisor
build-output DLLs were temporarily replaced with the original delivered 0.2.3
package payload before running, then restored afterward. The archived loaded
DLLs match all three package files by SHA256.

```powershell
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 `
  -PlayerAdvisor -RollingAdvice -PrimaryClan Hellhorned -SecondaryClan Banished `
  -Seed 20261021 -PlannerHorizon 3 -PlannerTimeLimitMs 3000 `
  -BinaryCapture -SkipBuild -TimeoutSeconds 360
```

Profile `full-battle-units-spells-and-junk-20261021-232913-afdc977e` completes
the natural `Level1BattleHorde` encounter in 252.73 seconds, winning with 80 Pyre
HP. All 28 inputs (21 card plays, seven EndTurns) consume fresh player advice;
30 publications and 363 pure captures pass UI and stale-result checks. The
first rendered recommendation deploys a Shield Steward on floor one, position
one, after a 3.11-second search. All four native failure counters are zero,
terminal effects settle, protected files remain unchanged, and the independent
`--rolling-advice-only` checker passes every input and published continuation.

The initial attempt stopped before battle because the blank profile lacked the
native Crew unlock. Only the isolated selection driver was updated to prepare
that prerequisite; it is excluded from the player assembly. Duplicate clan
requests and clan selection with resumed checkpoints are rejected before launch.
This Windows first-battle check does not reproduce the reported macOS failure
or establish later decks or both main/secondary orientations. No Mac package
was rebuilt and no full regression was run.

## Efficient complete-window player validation

The isolated frozen-window driver waits for player capture initialization before
any native input. A searched window still verifies the real player UI and a new
publication after the first action. It then pauses unused background planning
through the player's existing pause button while executing that original plan.
A frozen replay pauses planning before its first input. The separate player
assembly, native observation and every complete-state/RNG comparison stay active.
This mode verifies one complete publication; it does not establish rolling
replanning at every subsequent decision. The rolling acceptance mode is unchanged.

The driver also skips the unused fixed-policy candidate while a player plan owns
the decision. Ordinary policy execution before and after the window is retained.
These driver changes are excluded from the player assembly and package.

Player probes load both shared dependencies from the Advisor build output and
archive all three loaded DLLs. This keeps the native replay and package byte
identical even if an intervening checker build refreshes another output tree.

## Natural final-boss player plan

Player capture accepts the original `Boss_Level8_AllyBuffer` definition through
the same calibrated queued-birth, action-controller, physical-anchor and
enchantment-world projections as the native model. It does not enable the
development boss-forcing patches. Other boss definitions and unsupported
payloads retain their existing refusal.

The input is the unmodified naturally progressed checkpoint
`campaign-ring8-20261021-140958-4b20355b-before-ring8`, after seven native wins.
The real in-process player uses 12 turns, a 30,000 ms budget, preferred room 2
and tactical seed. Its third initial publication finds a complete 50-input win
in 24,784.5 ms; full forecast verification takes another 14,738.2 ms. The four
actual publications take 37.2–47.1 seconds from capture to publication.
The configured search budget is not a bound on total response latency.

Source profile `full-battle-units-spells-and-junk-20261021-205527-92aaa977`
wins in 606.83 seconds with 21 Pyre HP, 39 card actions and 11 EndTurns.
The player records four publications, 279 pure captures, 296 text vertices and
85,699 visible pixels. Native UI checks, action-triggered refresh and every
full-state/RNG successor pass. Terminal effects settle, protected files stay
unchanged, and capture/mismatch/unsupported/pending counters are all zero.

Fresh replay profile `full-battle-units-spells-and-junk-20261021-210553-12f4d7bf`
wins in 246.13 seconds with the same 39 card actions, 11 EndTurns and 21 Pyre HP.
Its planner record has zero search nodes; no new recommendation selects an input.
Every input, root, successor, complete forecast and gameplay RNG matches the
source. Terminal effects settle, protected files remain unchanged and all four
failure counters are zero. The three archived player DLL hashes equal the
Advisor output used for packaging. Both complete archives are curated as
`full-battle-player-boss-search.mt2f` and `full-battle-player-boss-replay.mt2f`.

This is one complete real player publication, followed by frozen execution after
checking the first action's new publication. It is not an acceptance of winning
rolling replans under a 30-second budget. An earlier rolling diagnostic matched
its first five inputs but was stopped before completion; shorter 10-second
searches returned losing forecasts. Neither diagnostic is curated as a win.

The pair checker independently replays every source and frozen input and checks
the queued boss at turn zero, both Casting/Relentless phases, newly generated
Entropy Scourges with captured source traits, and native unit-ability creation
and activation. Run
`scripts/Check-PlayerAdvisor.ps1 -Case OuterBoss -SkipBuild` for the manifest-
verified focused gate, or omit `-Case` to check moon, boss and Conduit pairs.
No full fixture regression is needed for this player-capture increment.

## Delivered 0.2.2 trial package

`results/mod-dist/0.2.2-validated/PojuAdvisor-0.2.2-macOS.zip` is 2,919,431 bytes,
packaged from clean source `a1c96934370209dd01961bc287d49b381cb618cb` using the
native-validated binaries. ZIP SHA256 is
`224237d214c88293d34f8451534d22face1742c043148dbfb3782a63fe8a27e6`.
The package checker passes all 12 files, complete checksums, LF shell scripts,
AnyCPU IL metadata, the player entry point and exclusion of the test bridge.
All three ZIP DLLs exactly match the fresh final-boss replay payload:

| DLL | SHA256 |
| --- | --- |
| MonsterTrain2Poju.Advisor.dll | `0e257b05ff04e5992e15c496ed38fe053ab9fe25b4626f66d2d0ad16dec6abe9` |
| Model.dll | `de2ccc1553f50b23f89356a0ceddc85471d39bb8adad1ce6a5f2aa83911df5b2` |
| FixtureArchive.dll | `9da684f8af93795b507e5ddd3aed1b29d23def54054611fc73298856ab93147d` |

The ZIP and its `.sha256` file are copied to `/tank` share root,
`\\intel-n100.local\tank` (this workstation's `Z:`). Both network copies match
their local SHA256 values. The previous 0.2.1 package remains available.
Mac runtime acceptance remains unverified and the package retains its trial label.

## Version 0.2.3: physical Conduit capture

The player captures primary Conduit trackers, including dead or detached
contributors, together with physical spawn points. An explicitly selected
read-only `AddTrigger` observer preserves native trigger creation identities
when relic callbacks prepend a new trigger to the visible list.

The initial player diagnostic matched nine inputs, then stopped publishing:
the native game retained a destroyed ordinary enemy as a live Unity object.
Decision readiness now waits only for a retired, still-bound enchantment source
whose Unity destruction changes the captured aura binding. Ordinary corpses do
not block advice. The existing retired-unit destruction fixture still matches
all six EndTurns, upgrade cleanup, aura unbinding and gameplay RNG. Failed
diagnostics are not curated as accepted battles.

Source profile `full-battle-units-spells-and-junk-20261021-221018-514570eb`
completes 30 rolling decisions (24 card actions, six EndTurns) in 265.04 seconds
and wins with 80 Pyre HP. The real player records 32 publications, 398 pure
captures, 300 panel vertices and 85,699 visible pixels. Native UI interactions
pass, 275 compactions and 2,765 complete physical decision captures are recorded,
terminal effects settle, original files remain unchanged and all four failure
counters are zero.

Fresh profile `full-battle-units-spells-and-junk-20261021-221504-75c1e557`
repeats all 30 inputs with zero planner nodes in 93.11 seconds, again winning
with 80 HP. It records 70 pure captures, no publications and 550 physical
decision captures; all counters remain zero and terminal effects settle.
The focused pair checker matches every root, input, successor, forecast and
gameplay RNG, independently verifies 14 relic births and observes 96 contributor
snapshots, 37 rooms with multiple contributors and 12 upgraded card plays.
Original relic definitions are unchanged, but carriers are authored controls;
this does not establish a natural Conduit deck or arbitrary combinations.

Both archives are curated as `full-battle-player-conduit-{search,replay}.mt2f`.
Run `scripts/Check-PlayerAdvisor.ps1 -Case Conduit -SkipBuild` for this focused gate.

## Delivered 0.2.3 trial package

`results/mod-dist/0.2.3-validated/PojuAdvisor-0.2.3-macOS.zip` is 2,927,620 bytes,
from clean source `e8499e053edad73807dbdc679beaf77ecf7fd2a9`. ZIP SHA256:
`8e0d2c0ce1b61a8664fc8ce4e19040d7a599c61fd25bb158399952885440794e`.
All 12 files pass package checks. All three DLLs exactly match the fresh
Conduit replay payload:

| DLL | SHA256 |
| --- | --- |
| MonsterTrain2Poju.Advisor.dll | `841e2b3e33df115e8050232c7a1c1b0b2e8ce2827b4f450cf87ced98441e0981` |
| Model.dll | `54b693d24c310c19fa292899bcb21a606991ab72843d8fca070f5c7432fe788b` |
| FixtureArchive.dll | `41d034870b67b28e6202d76b735ae1577c1fbe1bdb44aacdf1530c972f14f53e` |

The ZIP and checksum are copied to `/tank` root with network hashes verified;
0.2.2 remains available. Mac runtime acceptance is still outstanding.

## Room moon masks with original Full/New callbacks

The actual player entry now passes a complete authored Eclipse encounter with
original Selene and Nightingale callback payloads. Source profile
`full-battle-units-spells-and-junk-20261021-230548-15477baa` completes 23 rolling
decisions (18 plays, five EndTurns) in 178.82 seconds, winning with Pyre80.
It records 25 publications, 246 pure captures, 352 panel vertices and 85,699
visible pixels, with native pause/hide/refresh/pointer interaction checks passing.

Fresh profile `full-battle-units-spells-and-junk-20261021-230904-1abf02f5` repeats
all 23 frozen inputs with zero search nodes in 84.21 seconds and wins with Pyre80.
It records 55 pure captures and no publications. Complete roots, successors,
forecasts and gameplay RNG match. Both settle terminal effects, preserve
protected files and retain zero capture/mismatch/unsupported/pending counters.
All three player DLL hashes match between profiles.

The pair checker independently reproduces seven native spawn boundaries in each
process and observes both phases, five callback actors, mixed masks and four
enemy births under an active mask. Heroes initialize their phase off-train;
the extra room phase enters on a later refresh. Earlier diagnostics exposing
four spawn-mask mismatches were not curated. Authored carriers establish this
combination, not arbitrary late-game natural decks, enemy moon callbacks,
preview callbacks or friendly movement through masked rooms.

Archives: `full-battle-player-room-moon-{search,replay}.mt2f`. Run
`scripts/Check-PlayerAdvisor.ps1 -Case RoomMoon -SkipBuild` for the manifest-
verified pair gate. Existing Moon and Conduit player gates also pass.
Per the user's request, no further Mac packages are built.

## Retaining verified winning continuations

The in-process player session now retains one immutable winning line after its
complete independent model replay. At the next settled input boundary it hashes
the captured state on the worker and can publish the remaining suffix under a
new generation. Every gameplay property, RNG stream, physical reference and
selected room must match a forward position on the proven line. The next unused
UI preview-copy number is normalized only while the allocator is present and
valid; existing copy identities remain in the key. Ordinary search and
publication keys still include that number.

Input and animation boundaries immediately hide old advice. A divergent state
requests fresh search; explicit refresh, undo, restart, pause, hide and a new
battle clear the retained line. Cancelled or obsolete workers cannot install
one. A reused suffix reports zero search nodes/time and displays
`沿用已验证胜局`; it does not claim to have searched again. Custom external
verifiers retain their existing behavior and do not create these proofs.

`--winning-advice-only full-battle-player-boss-search.mt2f` checks all 49 native
successor roots of the archived 50-input win. It also checks resource changes,
RNG seed and word changes, forward jumps, backward moves, explicit reset, forged
publication keys, cancelled work, immutable roots and the preview-allocator
boundary. Existing advice-session cancellation/coalescing/failure checks pass.

Source profile `full-battle-units-spells-and-junk-20261021-234726-00a8770f`
completes 50 rolling decisions (39 plays, eleven EndTurns) in 774.50 seconds,
winning with Pyre21. All 50 consumed publications predict victory: the initial decision
uses real search, and the following 49 publish the exact remaining winning
suffix with zero new search nodes. Retained capture-to-publication latency is
2.18 seconds median, 3.64 seconds P95 and 4.24 seconds maximum. The initial
consumed publication still takes 42.91 seconds; the search budget does not
bound capture, verification or total response time.

The player records 52 publications, 554 pure captures, 296 text vertices and
85,699 visible pixels. Native UI interactions pass, terminal effects settle,
protected files remain unchanged, `DEPTH-PASS` is present and all four failure
counters are zero. The source archive retains all 3,010 complete decision
contexts and is curated as `full-battle-player-boss-rolling-search.mt2f`.

Fresh profile `full-battle-units-spells-and-junk-20261022-000038-aa55e6f7`
repeats all 50 frozen inputs with zero search nodes in 339.36 seconds, again
winning with Pyre21. It records 120 pure captures and no publications. Terminal
effects settle, protected files remain unchanged, `DEPTH-PASS` is present and
all four failure counters are zero. The frozen record preserves the historical
source labels; it does not run a winning-continuation cache or a new search.
Its archive is `full-battle-player-boss-rolling-replay.mt2f`.

The pair checker independently replays every input and every published suffix,
compares full state/RNG between native processes and checks each retained root
with only the unused allocator normalized. It also requires exact remaining
steps, unchanged terminal forecasts/scores, fresh generations, zero new search
work and correct source/timing labels. Queued boss birth, Casting/Relentless,
generated Entropy Scourge source traits and three native ability activations
remain covered. Run `scripts/Check-PlayerAdvisor.ps1 -Case OuterBossRolling -SkipBuild`.

Both runs resume the natural ring-eight checkpoint
`campaign-ring8-20261021-140958-4b20355b-before-ring8`, distance seven, with the
12-turn/30,000-ms tactical configuration and deployment room two. This validates
following a verified winning line; it does not establish a fresh winning search
at every input or sufficient performance under the default three-turn budget.
All three loaded player DLL hashes match between the two processes:

| DLL | SHA256 |
| --- | --- |
| MonsterTrain2Poju.Advisor.dll | `9df82bedd1ee9b7f5325fffda37621659301478c01d1af4523fbdb22f6f7ad34` |
| Model.dll | `7bf1fc6f38ba504598151455fee922a030e4f5bf254c73e91479aaff25a1042f` |
| FixtureArchive.dll | `dd0c12238c09b68252f0d0e65425337d3ecf324df3257cc85cdd084ddba0d6f4` |

Only focused checks and the coverage metadata refresh are run. No full
regression or Mac package build is performed.

### Player advice after friendly movement through moon rooms

On 2026-10-22, actual player publications execute original Hidden Passage and
Dripfall through an original Eclipse mask with authored callback carriers.
Source `full-battle-units-spells-and-junk-20261022-003336-e5a057dd` completes
thirty decisions (twenty-five plays/five EndTurns), winning with Pyre80 in
211.32 seconds. It records 32 publications, 285 pure captures, 1,620 complete
contexts and a panel with 300 vertices/85,699 pixels. Five actual moves change
original New callback upgrades; one ordinary champion move changes its mask
without adding that upgrade. Twenty-nine later publications retain the
verified winning suffix; median/P95 response is 0.93/1.32 seconds, with the
first consumed response taking 5.98 seconds.

Fresh `full-battle-units-spells-and-junk-20261022-004005-67a70205` repeats all
thirty frozen inputs in 111.34 seconds, winning with Pyre80, 69 pure captures,
520 complete contexts and zero publications/search nodes. Complete states,
forecasts, gameplay RNG and all seven native spawn boundaries match. Both
runs settle terminal effects, keep protected originals unchanged, contain
`DEPTH-PASS` and have all four failure counters zero. Their three loaded DLL
hashes match exactly:

| DLL | SHA256 |
| --- | --- |
| MonsterTrain2Poju.Advisor.dll | `e9a9735246edf23e063b687e9498334c5a21144ac0168cd1473d72db7b00d332` |
| Model.dll | `ab5a32f9a6716d631fdab93bfbfb11ee27c3f4552cf87d2c58ca708b08ff7f41` |
| FixtureArchive.dll | `4d9b8e584aa6441a490a150f3c769c16266a3d8c022090c0104c97991abd1a95` |

Run `scripts/Check-PlayerAdvisor.ps1 -Case RoomMoonMovement -SkipBuild`.
The harness orders an authored hand, retains four owned movement cards with
temporary Freeze, and uses a six-turn/10,000-ms tactical budget. It does not
establish a natural late-game deck, mixed-mask enemy births in this case or
default-budget performance. The earlier RoomMoon birth gate remains intact.
No full regression or Mac package build is performed.

### Natural event-graft player victory

On 2026-10-22, actual player search on the natural seed424244 before-Ring5
checkpoint wins with Pyre15: 38 plays, nine EndTurns, 47 consumed inputs and
49 publications. Source `full-battle-units-spells-and-junk-20261022-082738-90f25d85`
takes 472.33 seconds; independent frozen replay
`full-battle-units-spells-and-junk-20261022-083546-9b98f087` takes 214.89 seconds.
The dedicated pair check passes every input, root, successor, forecast and RNG;
both native runs have zero failure counters and settled terminal effects.
Actual temporary event graft attachment and reversed enemy physical/identity
orders are required. Enemy IDs are now observed at original creation, before
physical front/back arrangement, using a readonly callback wrapper.

The first search takes 10.71 seconds. All 46 later inputs retain the winning
continuation with zero search nodes; median/P95 response is 1.57/2.07 seconds.
The original pure-capture, pause/resume, hide/cancel, show/refresh, raycast and
drag checks also pass. The panel visibly reports victory with Pyre15.

Both native processes load these exact player payloads:

| DLL | SHA256 |
| --- | --- |
| MonsterTrain2Poju.Advisor.dll | `bac9886556735f4c6aec68a49aabb23acb98fdaac5c678fd50f5a18f26c37a79` |
| Model.dll | `5ffd517565621b635dabec5f534799148f97e95d5f7f893ae84dcd514cb6bc50` |
| FixtureArchive.dll | `de42e87278baaf2129fae646fae58b3105b4b19eff92d561c74c9725c23d919e` |

Run `scripts/Check-PlayerAdvisor.ps1 -Case EventGraft -SkipBuild`.
This uses horizon12/30,000-ms tactical search and floor2. It establishes natural
Ring5 acceptance for the exact Firebrand/MeatShield event pair, not default
budget performance, Ring7 victory or Fragile player acceptance. The weaker
Ring7 scout only published losing plans and was stopped; it is not curated
as a complete acceptance trace. No full regression or Mac build was run.
