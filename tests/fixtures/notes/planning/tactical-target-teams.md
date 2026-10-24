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
This is model validation; it does not establish a new natural Ring7 native pair.

The actual player 0.2.24 payload subsequently passed the isolated native run
`full-battle-units-spells-and-junk-20261025-014005-38b5292b`: 21 plays and seven
EndTurns, victory, normal exit in141.20 seconds, unchanged original player files,
and zero capture/mismatch/unsupported/pending counters. Full native state/RNG,
30 publications with fresh complete roots, seven zero-capture/zero-search floor
selections, 27 reused publications, six collapsed cross-turn resets and complete
background history counts/drop0 all pass. The accepted trace SHA256 is
`e0fbe036a696c59749bcbf5fe80a431b11a1d648643175c04e696fed2682d235`.
This is a focused player smoke and model/native comparison, not a claim that the
remaining natural Angel Duo or higher Covenant strategy gaps are closed.

```powershell
./scripts/Check-DefaultAdvice.ps1 -Case Targets -SkipBuild
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --natural-ring7-planner-only tests/fixtures/campaign/progression/full-battle-natural-ring7-copy-on-play.mt2f
```
