# Original Corrupter player advice and independent replay

`full-battle-player-boss-corrupter-bounded-{search,replay}.mt2f` retains source
`full-battle-units-spells-and-junk-20261022-174039-3a5e705e` (246.21 seconds)
and fresh process `full-battle-units-spells-and-junk-20261022-174556-73258462`
(86.28 seconds). Both load the separate actual Advisor assembly. The explicit
original-scene selector chooses `Level8FinalBossCorrupter` from the run's own
Ring8 pool without consuming RNG or changing encounter/character assets.
This is a controlled route selection, not natural campaign progression.

The original boss retains attack20/HP2800, its two native triggers and zero
auras. The original deck and Pyre80 remain unchanged. Both processes finish
with a native loss after 18 card actions and six EndTurns. All 24 inputs,
deployment roots, native successors, complete published forecasts and RNG
match. Replay consumes the original RollingDecisions directly with zero search
nodes. Both exit normally with code zero, settled terminal effects, protected
originals unchanged and zero capture/mismatch/unsupported/pending counters.

The source uses a three-turn, three-second soft search budget with tactical
seed, recording 26 publications and 345 pure captures. Panel render, pointer,
drag/clamp, pause/resume, hide/cancel/show and refresh checks pass. Actual
response median/P95 is 4.59/5.42 seconds including capture and verification.
This pair exercises Casting and original corruption on live player units;
the separate [native calibration](outer-boss-corrupter.md) covers the attack
multiplier, Relentless, floor destruction and Pyre combat.

The player gate now admits the three calibrated ordinary Level8 definitions
with the existing controller and payload validation. Titan Trial and Entropy
definitions retain their guards. This bounded loss pair does not grant winning
or natural-progression acceptance; the existing winning checker still requires
two native victories. No full fixture regression or Mac build is performed.

```powershell
scripts/Check-PlayerAdvisor.ps1 -Case CorrupterBounded -SkipBuild
```
