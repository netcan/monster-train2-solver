# Original Corrupter native calibration

The ordinary `Boss_Level8_Corrupter` now has original birth, casting, attack
callbacks, Relentless, floor destruction and Pyre calibration. Its original
20 attack/2800 health and two unit triggers are unchanged. The sealed
`RelicEffectOnlyTooltips` only supplies a character for the scene's tooltip;
it no longer blocks otherwise supported combat. Other unknown effects on the
same relic remain guarded.

The disposable development profile explicitly selects the original Ring8
`Level8FinalBossCorrupter` scenario from the run's own scenario pool. The
control retains previous/selected scenario indices and names, generated wave
boss IDs and equal RNG before/after selection. No scenario, character, deck
or relic definition is rewritten. This is an isolated route selection, not
natural campaign progression or player Advisor acceptance.

`full-battle-outer-boss-corrupter-casting.mt2f` comes from
`full-battle-units-spells-and-junk-20261022-172832-f4d35399` (85.93 seconds).
The original starting deck plays 18 cards and six EndTurns before losing.
The complete chain from one deployment root matches native state and RNG.
Its companion birth, phase and controller fixtures cover initial physical
placement without an aura, nine settled phase boundaries, four moves,
twelve real controller operations and 1152 detached native transitions.

Two captured original `OnAttacking` effects double existing corruption from
two to four. One victim is alive and the other already has zero HP. The
independent effect checker retains the dying override target separately from
the room's pre-effect physical occupants; no expected native successor is
injected into prediction. Actor, target, full context and RNG match. The
implementation admits only the calibrated fixed corruption/factor-two attack
signature; randomized, different-status and different-factor variants reject.
Zero-stack/overflow boundaries, dying targets, source immutability and tooltip
effects mixed with unknown effects have separate model controls.

`full-battle-outer-boss-corrupter-casting-replay.mt2f` comes from a fresh
process, `full-battle-units-spells-and-junk-20261022-173302-69662475`
(85.49 seconds). It consumes the 24 frozen inputs with zero search nodes.
Every original/fresh native successor and gameplay RNG matches, with the same
loss. Both processes exit normally, record DEPTH-PASS, preserve protected
original files, settle terminal effects and have four zero failure counters.

The separate `outer-boss-corrupter-relentless-{turn,phase}-calibration.mt2f`
pair comes from `full-battle-units-spells-and-junk-20261022-173231-0593b54d`
(103.15 seconds). At turn two it explicitly empties the spawn queue and sets
Pyre health/max health from 80 to 10000. The dedicated checker requires
exactly the resulting one mid-turn mismatch and zero capture, unsupported or
pending failures. The generic harness correctly reports DEPTH-FAIL for this
controlled battle; it is not an ordinary full-battle acceptance fixture.
Eight phase boundaries, the Casting-to-Relentless transition and three floor
destructions match. Nine paid actions and three EndTurns from one post-control
root, plus four concurrent immutable branches, reach the captured controlled
victory with Pyre8745. This is not a natural win.

Run the focused suite without a full fixture regression:

```powershell
scripts/Check-OuterBossCorrupter.ps1 -SkipBuild
```

The real player publication/UI and independent 24-input replay now also pass;
see [bounded player evidence](player-corrupter-bounded.md). The player gate
admits all six calibrated Level8 definitions after the independent
[Trial player pairs](../advisor/player-trials-bounded.md). Level9 and uncalibrated Covenant
birth effects retain their guards. No Mac package is built.
