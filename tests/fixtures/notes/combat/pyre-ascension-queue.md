# Pyre defeat stops lower-floor ascension callbacks

Random09 Ring8,Wurmkin/Hellhorned,seed771950956,Covenant0,unchanged checkpoint
`campaign-ring8-20261026-001033-9de97b88`. The29-input source preserves the
old forecast from the turn5 EndTurn: enemy21/22 health308/100 instead of native
311/103. Both have already physically moved from room0 to room1, but their
PostAscension damage3 never executes after Pyre death.

Native HeroManager.DoAscension first reserves every destination, then handles
each destination from the top down. It drains the Pyre-room movement queue
and performs tower combat before processing lower destinations. A stopped
combat ends that coroutine. The model previously dispatched every destination
before the Pyre combat, applying two extra damage3 effects on this terminal
branch. Version0.2.88 defers lower-floor movement callbacks until tower combat
has completed and preserves the already reserved physical locations on defeat.

The independent corrected process repeats the original29-input request.
All complete states, forecasts, RNG, immutable parents and terminal outcome
PlayerDefeated match. The original wrong forecast remains preserved. Normal
ascension with Titanskin5 still takes piercing3 in the11-input independent
pair; the older22-operation movement controls also pass. The preceding37-step
seventh-battle frozen native pair passes with the changed movement ordering.
This pair validates a defeat boundary, not a complete random campaign victory
or a new default-budget player acceptance.

```powershell
./scripts/Check-Models.ps1 -Fixture player-pyre-ascension-queue-calibration.mt2f -SkipBuild
```
