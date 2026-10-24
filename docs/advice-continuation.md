# Advice continuation · Windows0.2.12

The [panel continuity follow-up](advisor-presentation.md) keeps the old forecast
readable, dimmed and marked pending through card/turn/native loading work. After
the full current-root check, a verified suffix updates in place and preserves
the reading position. Keeping this text does not preserve a valid Current
publication: outstanding capture, selection or preparation still blocks ReadyFor.
Genuine new search, errors and explicit reset clear the obsolete forecast.

The [live Conduit follow-up](live-scroll-debug.md) fixes an omitted case in
the0.2.10 starter acceptance: selecting another room applies/removes hidden
Conduit upgrades and updates private card trackers. Floor reuse now validates
that entire modeled selection transition against the native full state/RNG,
retaining those fields in the comparison. Seven wheel inputs in the user's
running battle keep search count and generation unchanged; the exact release
model passes the captured live fixture and complete remaining forecast replay.

Following a verified plan advances to its remaining steps without searching
again. This includes EndTurn in a nonwinning/partial forecast and a forecast
whose terminal outcome is a loss. Remaining turn counts decrease when an
EndTurn is consumed; budget/shorter-horizon flags and the original forecast
remain intact. The panel does not claim a partial forecast is a full battle.

Every consumed prefix must match the captured complete state and RNG. Only
the existing next-unused preview-copy allocator normalization is permitted.
Inputs immediately invalidate obsolete publications, but retain the immutable
proof and clearly marked pending text until a settled capture can check the
successor. An unchanged root after an
extra animation boundary also reuses the proof without consuming a step.

Floor scrolling preserves the current candidate and running search while the
camera/preview is busy. Wheel events coalesce for120ms before capture. A cheap
room-only observation wakes this same path if the selection hook is missed;
it never replaces the full publication check. New selection-sensitive roots
still replay the remaining forecast on a background worker before reuse.
Each suffix keeps at most eight exact calibrated roots; an identical root
can reuse that result. Different preview allocators require strict calibration
again, so zero searches does not mean zero capture/verification work.

Fresh search is required when the settled gameplay state/RNG diverges from
the verified route, the available forecast runs out, or the user clicks
refresh. Undo/restart, a different battle and pause/hide reset their existing
session boundaries. Cosmetic preview/animation alone does not cancel a search.
Publication still requires a fresh complete native capture and state/RNG
purity checks. The Unity thread never waits for model search or replay.

Focused model checks cover11 following inputs across three ordinary turns
with one search; unchanged busy roots; horizon exhaustion; shorter horizons;
seven followed terminal-loss steps; explicit refresh; backward moves; changed
resources/costs/RNG; cancellation and obsolete background preparations.
No full regression is needed for this focused workflow.

The real0.2.10 player source and a fresh frozen native process both win with73
Pyre HP after19 plays/seven EndTurns (26 inputs). All four failure counters are
zero, both exits are normal and protected originals are unchanged. Every input,
root, successor, complete forecast and RNG matches, with14 native train phases
per process. There are22 ordinary continuations with zero search, including four
nonwinning EndTurns, plus one winning continuation. Only the two exhausted
three-turn windows trigger fresh route searches. Deliberate UI pause/hide/refresh
controls account for the remaining search setup; the source records seven
searches in total. It publishes34 plans with254 pure captures. Frozen replay
pauses advice, performs53 pure captures and zero searches/publications after
pause. Seven selections use the actual native scroll-wheel handler, including
one during search, without changing its generation or adding searches. Settled
idle performs zero captures/publications/searches for two seconds.

This focused source deliberately disables whole-battle search to exercise
nonwinning three-turn forecasts at the usual3000ms/two-worker budget. It does
not change the installed whole-battle default. Both isolated processes run
below normal priority; total runtimes are90.61s/54.58s including startup and UI
checks, not a search-only or FPS benchmark. Ready wheel switches settle in
298-780ms in this sample, including camera/preview and background validation;
the claim is zero new searches, not instantaneous switching.

Evidence profiles under `.probe-runs`:
`full-battle-units-spells-and-junk-20261024-171423-2aa4c670` and
`full-battle-units-spells-and-junk-20261024-171626-aab3a4e3`.
The complete pair is curated and registered as
`full-battle-player-cross-turn-scroll-{search,replay}.mt2f`.

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --player-ordinary-advice-pair-only tests/fixtures/full-battle-player-cross-turn-scroll-search.mt2f tests/fixtures/full-battle-player-cross-turn-scroll-replay.mt2f
```
