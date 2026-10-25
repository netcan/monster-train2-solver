# Physical references after an ability attack kills a front unit

The natural Remnant/Awoken Ring4 checkpoint (seed295400568,rank0) exposes
Steelsinger3 using WrathAbility25 in room1 after26 native inputs. The one-sided
AttackWithUnit kills enemy13; enemy14 survives and the actor keeps41HP.
Native RoomUI.CenterCharacters calls CommonSelectionBehavior.OnCharactersRemoved,
which compacts the physical references from [13,14] to [14,0]. The original model
left [0,14]. Unit stats, card piles and gameplay RNG already matched.

Stable physical ability decisions now compact both teams in rooms that lost a
unit during AttackWithUnit/AttackTargetsWithSelfUnit resolution. Raw effect and
isolated preview captures retain their distinct timing; ordinary card behavior
is unchanged. The original mismatching observation remains immutable evidence.

The calibration pair contains the same27 native inputs in independent processes:
`full-battle-units-spells-and-junk-20261025-085449-76346cbd` and
`full-battle-units-spells-and-junk-20261025-090304-53aaccd2`.
The focused check independently replays every successor and RNG, verifies parent
immutability, and proves the original discrepancy is confined to SpawnPoints.
The second process also records an exact repaired forecast. Ability pure controls
and the30-input dead-caster pair pass. These observations do not establish a
terminal Ring4 win or a full campaign clear.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture player-ability-attack-compaction-calibration.mt2f -SkipBuild
```
