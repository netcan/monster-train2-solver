# Inactive full-moon Basher movement

Random28 ring 7 offline refinement found a guarded Hidden Passage move of the
original MonsterBasher from the lower to middle room. Moon tracking is inactive,
the effective phase is full moon, and native changes its understanding mask from
5 to 6. Version 434 adds only the observed unit and its captured attack, health,
size and phase signature; other unit signatures remain guarded.

Two original and two repaired independent native observations replay all 17
inputs and compare complete states and RNG. Four concurrent branches preserve
their parent, and an unknown unit is rejected. This component observation is
not a battle or campaign victory.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture player-random28-ring7-fullmoon-basher-original.mt2f
```
