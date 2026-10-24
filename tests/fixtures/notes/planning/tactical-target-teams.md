# Tactical seed target teams

The bounded planner's tactical seed previously assigned an unfavorable team a
low finite rank, then cast at that target anyway after preferred targets were
absent or illegal. A legal negative-status spell could debuff an ally; a positive
status spell could buff a boss. Both main and on-cast effects reproduced this.

The seed now defers these cards when no preferred target is legal. The complete
action enumerator still retains cross-team casts for sacrifice, Incant and other
combinations; this is a seed choice, not a legality rule or a pruning proof.

Only effects on the selected actor determine the preferred team. Enemy room
damage accompanying an allied-target buff does not turn that buff into an enemy
target. Direct and scaled targeted damage retain enemy preference.

Sixteen focused cases cover positive/negative status, damage, mixed room damage,
main/on-cast effects and absent preferred targets. The missing-target and mixed
cases first failed against the prior implementation. All now check the exact
chosen successor/state/RNG and parent immutability, while preserving legal
cross-team branches. The archived natural Ring7 tactical deployment still yields
a fully replayable 60-input victory, with cancellation and node-bound controls.
This is model validation; a new native player run is recorded separately.

```powershell
./scripts/Check-DefaultAdvice.ps1 -Case Targets -SkipBuild
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --natural-ring7-planner-only tests/fixtures/campaign/progression/full-battle-natural-ring7-copy-on-play.mt2f
```
