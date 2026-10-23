# Ordinary unplayed-card Pyre preview and terminal stop

The native cumulative rank-two Corrupter actual player source/replay pair
retains the original deck and scenario. After its fifth combat/ascension,
Pyre16 and four original ScourgeT3 cards coexist. Each original negative
unplayed trigger deals five damage. Native UI previews these triggers before
any room fight; twenty damage exhausts preview HP and stops the preview
coroutine, preserving last-attack damage45 from the prior native Pyre attack.
The old ordinary model skipped these effects and incorrectly wrote three
from a later room preview. The shared preview now runs the calibrated fixed
negative effects in ordinary battles too and stops at Pyre death.

The next actual EndTurn also dies to those four cards. The native stop occurs
before CheckForDeath/ShowKillCam resumes: the Pyre leaves the live roster,
the turn remains five and all five hand cards remain, with KillCam=false.
The previous stop-before-kill-camera condition was limited to life-linked
true-final-boss battles. Both ordinary and final-boss unplayed damage now use
the captured HeroTurn boundary. Played cards and other combat phases retain
their existing cleanup behavior.

`UnplayedPyrePreviewChecks` requires both boundaries in the accepted actual
player source, matches prior native Ascend statistics, ties preview and real
death to the same hand/turn, and replays sixteen immutable full-turn branches
per boundary with complete native state/RNG. Its focused entry is:

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --unplayed-pyre-preview-only tests/fixtures/full-battle-player-native-covenant-outer-boss-corrupter-02-search.mt2f
```

The regular rank-two Corrupter pair check also requires this witness. Both
fresh processes have normal exit, protected originals unchanged and four
zero native/purity failure counters. Full forecasts, UI and independent
frozen native replay pass; see [pair evidence](native-covenant-outer-bosses.md).
The existing rank-ten Corrupter, rank-nine startup and rank-two final-boss
player pairs also pass after the preview repair. No full archive regression
was run. Failed source profiles remain diagnostics and are not curated.

The five-original-card regression was rerun in native profile
`full-battle-units-spells-and-junk-20261023-134115-b3f8f547`: seven turns,
victory with Pyre15, normal exit, unchanged protected files and all four
failure counters zero. Its final acceptance summary is persisted. This also
repairs the dedicated trigger scenario's schema161 selection and both final
animation guards; their completion and pending/error assertions remain strict.
Two earlier runs stopped at those schema guards and are not accepted evidence.
The existing canonical fixture remains the card-effect baseline; no duplicate
archive or full-suite regression was added.
