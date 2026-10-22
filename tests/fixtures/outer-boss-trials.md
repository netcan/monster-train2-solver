# Original Level8 Titan Trial calibration

The three exact `Boss_Level8_{AllyBuffer,Fragile,Corrupter}_TitanTrial`
definitions have native birth, casting, full paid chains, independent replay,
Relentless, floor destruction and Pyre combat evidence. Separate actual-player
pairs now also cover 53 inputs, full forecasts and UI checks; see
[bounded player evidence](player-trials-bounded.md). The player gate admits all
six calibrated Level8 definitions.
Entropy Level9 and uncalibrated outer-boss Covenant effects remain guarded.

The isolated driver selects the original Ring8 scenario from the run's own
pool. Titan Trials use the original scenario sin's ordered
`RelicEffectOnSpawnHeroSubstitution` lookup at `HeroManager.CreateHeroState`;
the ordinary/harder spawn-pattern flag alone does not select these definitions.
Capture now resolves the three exact original-to-trial mappings before
serializing queued/wave definitions, preserving the native effect order.
Other applied mappings add an unsupported external interaction. The sealed
substitution effect has no combat callbacks; all other effects on each sin
retain their existing independent validators.

`BattleIntroScreen` resets the saved trial flag during setup. The driver waits
for the fight button, explicitly enables isolated trial eligibility and invokes
the original UI toggle before entering combat. Both the original trial sin and
the substituted boss must appear in the first deployment root. Route selection
preserves every RNG stream. UI refresh may consume `NonDeterministic` for
presentation; all gameplay streams remain unchanged and both snapshots are
retained. No scenario, character asset, starting deck or Pyre80 is rewritten.
This is controlled original-scene selection, not natural progression.

| Trial | Original stats | Strengthened native payload | Source inputs | Fresh replay inputs |
| --- | --- | --- | --- | --- |
| AllyBuffer | 35 attack / 3800 HP | Moving debuff10; generated Scourge titanskin10 | 20 plays / 6 EndTurns | 26 |
| Fragile | 30 attack / 3000 HP | Enemy buff6 aura and full original spawn-status sin | 12 plays / 4 EndTurns | 16 |
| Corrupter | 20 attack / 2800 HP | Turn-begin corruption4; attack multiplies corruption by two | 18 plays / 6 EndTurns | 24 |

All six ordinary casting/replay processes lose, exit normally with code zero,
settle terminal effects, preserve protected originals and have zero
capture/mismatch/unsupported/pending counters. Sixty-six inputs, complete
successors and RNG match across the three independent pairs; all replay nodes
are zero and the frozen model-prediction artifacts retain their original
forecasts. The three controller captures contain 32 actual operations and 3456
detached transitions. Aura binding and initial-action latches match. Corrupter
also has two isolated native multiplications, including a retained dying target.

Source/fresh profiles, all under `.probe-runs/`:

| Trial | Source | Fresh |
| --- | --- | --- |
| Corrupter | `full-battle-units-spells-and-junk-20261022-180223-31d42059` (95.32s) | `full-battle-units-spells-and-junk-20261022-180728-0b210226` (79.09s) |
| Fragile | `full-battle-units-spells-and-junk-20261022-180223-a70dde94` (73.27s) | `full-battle-units-spells-and-junk-20261022-180652-9c074067` (59.42s) |
| AllyBuffer | `full-battle-units-spells-and-junk-20261022-180412-d23362d7` (108.62s) | `full-battle-units-spells-and-junk-20261022-181113-c65cafac` (77.75s) |

Each separate Relentless calibration explicitly clears the spawn queue and
changes Pyre80 to Pyre10000 at turn two. Exactly that EndTurn has one expected
harness mismatch; generic `NativePassed`/`DEPTH-PASS` is false. Dedicated gates
require zero capture/unsupported/pending counters, only that recorded mismatch,
eight exact phase boundaries, three destroyed floors and an uninterrupted
post-control action chain with four concurrent immutable branches. The three
controlled wins have Pyre7105/7961/8745 for AllyBuffer/Fragile/Corrupter,
respectively. They do not establish natural winning acceptance.

Control profiles are `full-battle-units-spells-and-junk-20261022-180823-23d747f2`
(AllyBuffer, 106.77s), `...-20261022-180916-34297e08` (Fragile, 87.12s) and
`...-20261022-180505-78c3654a` (Corrupter, 92.67s). Earlier setup diagnostics
that still entered ordinary bosses or failed the UI gate are not curated.

Twenty-one native archives are registered. Run the focused checks:

```powershell
scripts/Check-OuterBossTrials.ps1 -SkipBuild
```

`scripts/Freeze-NativeBattleInputs.ps1` freezes chronological non-player inputs
and verifies the complete model chain before native replay. It refuses rolling
or player sources, which must replay original publications directly. No full
fixture regression or Mac build is performed.
