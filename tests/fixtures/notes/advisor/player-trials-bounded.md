# Original Level8 Trial player advice and independent replay

All three exact Level8 Titan Trial definitions now have actual player-assembly
publication, UI and independent fresh replay evidence. This complements the
[native birth/controller/Relentless calibration](../bosses/outer-boss-trials.md).
The player gate admits all six calibrated Level8 definitions with exact
controller/payload checks. Uncalibrated Covenant birth effects, other applied
hero substitution mappings and Level9 true-final-boss encounters remain guarded.

Each process selects the original Ring8 scene and uses the original trial UI
toggle, with an explicit isolated eligibility control. The original sin performs
native hero substitution. Every source and replay deployment root must contain
that original sin, the resolved trial boss, unchanged original stats and Pyre80.
No encounter/character asset or starting deck is authored. UI refresh may consume
presentation `NonDeterministic`; every gameplay RNG stream remains unchanged at
the control, and complete subsequent forecasts/RNG match in native replay.
This is not natural campaign progression.

All sources use three-turn/three-second soft search with tactical seed. All six
processes finish with native losses, normal exit zero, protected originals
unchanged, settled terminal effects and zero capture/mismatch/unsupported/pending
counters. Replay consumes the original RollingDecisions directly, preserving
each historical root, action, forecast, successor and metadata with zero search
nodes. No model-generated replacement forecasts are substituted into replay.

| Trial | Exact inputs | Original payload witness | Source publications / pure captures | Response median / P95 |
| --- | --- | --- | --- | --- |
| AllyBuffer | 9 plays / 4 EndTurns = 13 | Queued 35/3800 boss, live debuff10 aura and new Scourge source traits | 15 / 201 | 4.43s / 5.62s |
| Fragile | 12 plays / 4 EndTurns = 16 | Queued 30/3000 boss, live enemy buff6 aura | 18 / 254 | 4.74s / 5.53s |
| Corrupter | 18 plays / 6 EndTurns = 24 | Queued 20/2800 boss, corruption4 turn-begin definition and live corruption | 26 / 349 | 4.77s / 5.78s |

The three pairs contain 53 exact native inputs and 85 live Casting snapshots.
The 59 source publications perform real search, with no retained winning
continuations. All sources pass panel rendering, pointer routing, drag/clamp,
pause/resume, hide/cancel/show and refresh checks, with zero UI or capture-purity
failures. Search soft limits are not total response deadlines. Relentless and
the exact multiplication/floor/Pyre mechanisms have separate native controls;
these bounded player losses do not establish natural or winning acceptance.

Profiles under `.probe-runs/`:

| Trial | Source | Fresh replay |
| --- | --- | --- |
| Corrupter | `full-battle-units-spells-and-junk-20261022-181725-4c83d00b` (272.43s) | `...-20261022-182232-64b9cda6` (94.90s) |
| Fragile | `full-battle-units-spells-and-junk-20261022-181725-d06c5051` (181.87s) | `...-20261022-182428-4f8b7b75` (59.15s) |
| AllyBuffer | `full-battle-units-spells-and-junk-20261022-182117-dc8892d3` (159.39s) | `...-20261022-182624-370c37c1` (70.11s) |

Six native archives are registered. Run selected pairs without a full regression:

```powershell
scripts/Check-PlayerAdvisor.ps1 -Case TrialAllyBuffer,TrialFragile,TrialCorrupter -SkipBuild
```

The existing ordinary winning-pair gates still require native victories and
both boss phases. No Mac build or full fixture regression is performed.
