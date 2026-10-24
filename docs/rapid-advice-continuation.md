# Rapid advice continuation · Windows 0.2.18

The 0.2.17 live log contains a card-play invalidation followed by an EndTurn
invalidation without a publication between them, then a new 10-second search.
That sequence also occurs later in the same player session. It identifies an
input/publication overlap; the log alone cannot prove every input followed the
published route.

A deterministic regression using the captured native Conduit fixture reproduces
an independent defect in that overlap. Starting verification removed the last
published candidate and drained its deferred floor callbacks. A second input
cancelled that worker before publication, leaving only the old prefix hashes.
Even when those hashes matched again, the reused forecast could contain a stale
preview allocator and fail independent full-state replay.

The session now retains the published anchor and an ordered history of floor
callbacks and immutable captured checkpoints until a result is accepted. An
obsolete/cancelled worker consumes neither. Matching checks each checkpoint
against the original planned prefix, applies intervening selections in order,
and verifies the full remaining forecast from the latest exact native root.
Only the accepted worker's history prefix is removed; later views survive.
Ordinary inputs without floor callbacks keep the existing prefix-hash fast path.
Browsing itself still records O(1) events without capture, hashing or replay.

The focused regression deliberately makes multiple advised inputs, including
EndTurn, before polling any new publication. It covers no browsing, preceding
browsing, and browsing between inputs. All cases require one search, the correct
remaining step count, a clean published checkpoint, and independent exact suffix
replay. Existing full-state/RNG divergence, explicit refresh, in-flight browsing
and native selection callback checks remain applicable.
An additional off-route resource checkpoint must force fresh search even if a
later root converges to the old route. A floor callback arriving after worker
start must survive that worker's publication.

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --floor-browse-selection-only tests/fixtures/advisor/live/player-live-conduit-floor-scroll.mt2f
```

Actual divergence, exhaustion of the available forecast, and manual reset retain
their existing fresh-search behavior. This fix does not change the planner's
search budget, battle model or native input handling.

## Acceptance

The three rapid-input variants, intermediate-divergence rejection, late-view
retention, and existing native Conduit selection controls pass with the exact
production Model.dll in `.probe-runs/rapid-continuation-check-20261024-214200`.
Existing floor ready/in-flight/cost/resource/RNG controls and both live moon
continuation roots also pass. No full regression was run.

The isolated native player run is
`.probe-runs/full-battle-units-spells-and-junk-20261024-213952-efcb3ddb`:

```powershell
./scripts/Run-FullBattleProbe.ps1 -NativeWorker 2 -Policy units-spells-and-junk `
  -OriginalConduitRelics -PlayerAdvisor -RollingAdvice -AdvisorPresentation `
  -AdvisorFloorBrowse -PlannerWholeBattle -PlannerSearchWorkers 2 -SkipBuild `
  -TimeoutSeconds 300
```

The actual 0.2.18 player wins with Pyre74 after four plays and seven EndTurns,
53 room stages, 13 publications and ten zero-search reused suffixes. All four
native failure counters are zero, protected player files remain unchanged and
the process exits normally in 90.65 seconds. Seven initial native wheel inputs
include one during search; four further wheels with a deployed Conduit add zero
captures, publications or searches and preserve the panel. This native driver
waits between publications; the rapid overlap itself is the deterministic
regression above, not an unrecorded claim about this driver's timing.

Accepted production SHA256 values:

| File | SHA256 |
| --- | --- |
| MonsterTrain2Poju.Advisor.dll | EF22FB467AB2470E133BCF899546DE1B4CD5DA767062C44F9379270BFEB6FB8C |
| Model.dll | B221DEB8C38EA217F638C49E79EF72B3D3987B1790345BD866D82C9727EC7935 |
| FixtureArchive.dll | 35E5AC6BBBB0E3878C330AB7724B955A933095DE69956A94053F37D4253FC5A8 |

Later checker builds may produce different assembly informational commit
metadata; delivery uses the exact accepted player payload above.

## Windows delivery

Installed at 2026-10-24 21:42:57 from `8af71be3`, with equality of all three
accepted/installed SHA256 values, to
`F:/SteamLibrary/steamapps/common/Monster Train 2/BepInEx/plugins/PojuAdvisor`.
Previous and accepted payloads plus installation.json are backed up in
`BepInEx/PojuBackups/RapidAdviceContinuation-20261024-214256`.
The package is `results/mod-dist/PojuAdvisor-0.2.18-Windows-20261024-214256.zip`.
Twenty-eight other loader/plugin/configuration files retain their hashes.
No real player inputs, process restart or Mac build were performed.

At delivery PID142908, started at 21:22:45, still runs 0.2.17. The installed
0.2.18 takes effect when the user restarts the game.
