# Tactical seed target teams

On2026-10-25 the same preference was extended to targeted unit/room abilities.
Previously their seed path took the first legal enumerated activation without
the hand-card target check. The full enumerator still retains self-targeted
damage for deliberate sacrifice combinations; only the seed's preference changes.
Non-targeted abilities keep their existing order.

The Ring5 native dead-caster observation supplies the control root. After four
legal model hand plays, the seed selects champion2's ability against enemy10,
preserves the champion, and still exposes the legal self-kill to the full search.
This control verifies full parent immutability and is registered with the native
observation pair. Four synthetic ability controls separately cover damage/buffs
and absent legal preferred targets, including the formerly first-listed allied
damage target. The16 existing hand-card target controls, pure ability controls
and60-step natural Ring7 model victory pass. This is a model policy check, not a
new native campaign victory.

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --tactical-ability-target-only tests/fixtures/combat/units/player-dead-caster-status-calibration.mt2f
```

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

Windows 0.2.24 was installed offline at2026-10-25 01:43:57 from committed
`ca506ccd`, using the exact three accepted DLLs. All28 other loader/plugin/config
hashes are unchanged; F10 and the user's10000ms budget are preserved. The next
game launch loads this version. Backup and installation receipt:
`BepInEx/PojuBackups/TacticalTargets-20261025-014355`.
Package: `results/mod-dist/PojuAdvisor-0.2.24-Windows-20261025-014355.zip`, SHA256
`159388d8c952fca7a68a65637501b7778545f300acae870f5d89bde6687807c3`.

```powershell
./scripts/Check-DefaultAdvice.ps1 -Case Targets -SkipBuild
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --natural-ring7-planner-only tests/fixtures/campaign/progression/full-battle-natural-ring7-copy-on-play.mt2f
```
