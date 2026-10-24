# Deferred floor browsing · Windows 0.2.17

Changing the viewed floor used to capture the entire native battle, hash it,
replay the remaining forecast and publish another advice object. Even a reused
route dimmed the panel while checking, and native preview loading could schedule
yet another capture.

The overlay now records selection callbacks in order and keeps the current
publication, text, status, color and scroll position. A view event does no native
capture, detached snapshot hashing, model replay or route search. Selection-only
preview/loading frames preserve that publication too. Views during snapshot
preparation or an in-flight search do not cancel that work.

After an actual card/turn/mutation boundary, the normal complete native capture
runs. The session reproduces the recorded selection callbacks from the prior
published root, matches a full decision/RNG prefix, then verifies the remaining
forecast from the actual new root. Replaying from that root retains the actual
preview allocator in the forecast. Conduit/room spell modifiers, private tracker
histories, costs, physical identities and RNG remain checked. Manual reset clears
the view history and starts a new search.

Cheap resource/pile/turn changes and real input/mutation hooks still wake the
ordinary capture path. The selected-room stamp alone does not. The existing
native callback calibration is documented in [live-scroll-debug.md](live-scroll-debug.md).

Focused checks extend the captured Conduit wheel fixture with seven deferred
view notifications, exact publication identity, unchanged generation/search
count, a following advised input and complete suffix replay. Energy/RNG changes,
manual reset and views during an in-flight search are also checked. Existing
ready/in-flight floor rebasing and resource/cost/RNG rejection checks remain.

The isolated player acceptance asserts zero overlay captures on all seven native
wheel inputs, including one during search. For an already published route it
also checks the advice object, publication count, text, status, color and scroll
position before taking its own explicit diagnostic capture. That independent
capture compares the complete native selection transition and RNG with the
recorded callbacks; it is not part of the player's browsing path.

## Native acceptance

The accepted run is
`.probe-runs/full-battle-units-spells-and-junk-20261024-211810-9c3644f9`.
It loads the actual 0.2.17 player DLL, whole-battle/two-worker/3000ms settings,
and the existing isolated original Conduit relic scenario. The scenario authors
Steward/equipment carriers and keeps the three native relic definitions unchanged.

```powershell
./scripts/Run-FullBattleProbe.ps1 -NativeWorker 2 -Policy units-spells-and-junk `
  -OriginalConduitRelics -PlayerAdvisor -RollingAdvice -AdvisorPresentation `
  -AdvisorFloorBrowse -PlannerWholeBattle -PlannerSearchWorkers 2 -SkipBuild `
  -TimeoutSeconds 300
```

Seven initial wheel inputs, including one during search, add zero player captures
and zero searches. Once a living Conduit contributor is deployed, four additional
native wheel inputs add zero captures, publications and searches, retain the
same text/status/color/scroll, and match independently captured full native
selection callbacks/RNG. Those four inputs include leaving and returning to the
contributor's floor. Global BrowseSelections also counts selection callbacks
caused by ordinary native actions; it is not the explicit wheel-input count.

The driver then completes four card plays and seven EndTurns, wins, and records
53 room stages, 13 publications and 10 reused suffixes. CaptureFailures,
Mismatches, Unsupported and Pending are all zero. Presentation acceptance checks
50 retained input notifications, six non-top scroll positions and four live
duration changes. The process exits normally in 105.91 seconds and protected
player files remain unchanged.

The captured Conduit fixture also passes with the exact production Model.dll
in a separate checker harness. Existing floor concurrency/divergence and live
moon continuation controls pass. The 1360-fixture catalog hash/registration
check is separate from these focused behavior checks; no full regression or
Mac build is required for this change. These counters establish removal of the
player's browsing work in the covered native cases, not an FPS benchmark.

## Windows delivery

Installed at 2026-10-24 21:20:59 from `2ec8eb5a`, with source/installed SHA256
equality for all three DLLs:

| File | SHA256 |
| --- | --- |
| MonsterTrain2Poju.Advisor.dll | 2565149E84D533720B80A8210EB9818022E3E37D7865F5E9F70F0F88EA623804 |
| Model.dll | C359515E6444775197322A5017C6059CF8624BEB49EBA896526425853270E519 |
| FixtureArchive.dll | B7E5CC6BE4849FE21CCB001D10C1D5519E134FA2C71365C4645749EED12CE4A6 |

The target is `F:/SteamLibrary/steamapps/common/Monster Train 2/BepInEx/plugins/PojuAdvisor`.
Previous and accepted payloads plus installation.json are backed up under
`BepInEx/PojuBackups/DeferredFloorBrowsing-20261024-212058`.
The distributable is
`results/mod-dist/PojuAdvisor-0.2.17-Windows-20261024-212058.zip`.
Twenty-eight other loader/plugin/configuration files retain their original
hashes. No real player input, process restart or Mac build was performed.

The current player PID33172, started at 20:52:47, still has 0.2.16 loaded.
Installed 0.2.17 takes effect after the user restarts the game.
