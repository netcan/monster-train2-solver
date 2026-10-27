# Paid Relentless outer-boss Bump destroys passed floors

Sample22 LunaCoven/Remnant seed591468325/Covenant0 native Ring3 run
140135-6feeba03 reaches input39/turn6. Card21 adds spell weakness2 to
Fleshweaver Corruption9 and moves it from floor0 to1. Native additionally
disables floor0 and sacrifices enemies60/61. The original prediction moves
the boss correctly but leaves both enemies and the enabled departure floor.
This single original mismatch is retained; the run is not a native battle win.

Independent component140744-26b051c6 keeps all38 preceding inputs and the
original mismatching forecast. CardEffectBump.Bump destroys the passed rooms
for a Relentless target before its outer-boss callback skip. The model had
skipped the entire follow-up loop for every forced boss move, omitting that
destruction. Release231 performs paid-card destruction before that skip.
Boss controller actions retain their own phase-specific destruction and do
not enter this paid-card path.

Both the original failure root and complete39-input native component now
match full state, forecasts and RNG, including the two sacrifices and room
Enabled flag. Four parallel branches preserve the parent. Prior real20/22
Bump effect controls and their32 immutable branches pass; those old archives
retain their separately documented stale Enabled-snapshot limitation. No
Enabled field is excluded from this new39-input comparison. The paired boss
counterexample also passes its53rd automatic and63rd paid-card boundaries,
with eight immutable full-state/RNG branches.

Independent repaired39-input replay and fresh default-budget Ring3 acceptance
are pending. This component does not add a campaign or battle victory.

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --relentless-outer-bump-counterexample-only <original-failure.mt2f>
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --relentless-outer-bump-only <source.mt2f> <replay.mt2f>
```
