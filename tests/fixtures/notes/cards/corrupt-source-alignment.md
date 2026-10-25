# Positive Corrupt Restricted with installed Holdover

The natural Hellhorned/Wurmkin fifth-battle alternative plays Echo Infusion
card16 after its permanent TraitRetain shop upgrade. Native card-data source
parameters still contain one authored Corrupt Restricted trait, while the
live base-state trait list also contains installed Holdover. Requiring equal
list lengths incorrectly rejected the available requirement1 provenance.

Requirement lookup now matches the single authored Corrupt Restricted source
by type and requires exactly one unchanged positive base trait. Missing or
duplicate source definitions, mismatched values, source-room restrictions,
upgraded/replaced Corrupt requirements and uncalibrated mixed traits remain
guarded. Unrelated traits do not change this requirement's provenance.

Original and repaired native processes reproduce all20 inputs with full
successor/RNG equality and immutable parents. At the boundary, corruption
3 becomes2 before the target heavy's HP120 becomes135. Targeting an enemy
is legal but is not a tactical recommendation. The repaired raw trace has
four zero failure counters. The original threshold, missing-provenance and
source-floor controls also pass. No fifth-battle victory is inferred.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -SkipBuild -Fixture player-corrupt-source-alignment-calibration.mt2f
```
