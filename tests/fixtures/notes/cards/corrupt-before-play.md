# Corrupt card pre-play unit callbacks

The natural Hellhorned/Wurmkin fifth-battle alternative deploys upgraded
Corrupt Train Steward card3 into room1, which already contains Shardsoul
Carver unit4. Its pre-play corruption must fire the existing unit's
CorruptionAdded trigger before the summoned unit is created. Native attack
changes 20 to 23, armor becomes 3, and room corruption changes 0 to 1.

Corrupt traits now use the shared corruption effect queue, with captured
card provenance and player-action phase. This preserves callback ordering
before corruption is committed and before Corrupt Restricted runs. The
caller retains the entire train successor, auxiliary piles, allocator and
play rules. Summoning rebuilds the existing player roster after callbacks;
reusing its pre-callback snapshot would erase Carver's upgrade and armor.

Original rejection and independent repaired process both replay all four
inputs with complete state/RNG equality and immutable parents. The repaired
raw trace has zero capture failures, mismatches, unsupported and pending.
Existing Carver, restricted-corruption and tower-corruption controls pass.
This calibrates the upgraded Corrupt-before-summon composition. Nested
corruption, unknown effects and uncaptured source metadata retain their
guards; no fifth-battle win is inferred from this component.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -SkipBuild -Fixture player-corrupt-before-play-calibration.mt2f
```
