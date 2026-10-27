# Positive attack buffs with a damage cost

Both cheap search seeds formerly classified every targeted damage composition
as an enemy spell. In the retained native original-spell-compositions fixture,
Dark Deal5 grants Rage7 then deals5 damage to ally2: attack+14 and health-5.
Its complete recorded native successor and RNG replay exactly. Starting from
that native pre-cast root and making seven legal model seed plays, the prior
strength seed instead chooses enemy4. This check fails before the repair.

Release230 recognizes the narrow captured composition of positive Rage and
direct targeted damage, using effect definitions rather than card names. Both
seeds now select the surviving ally2; the full enumerator retains enemy and
lethal sacrifice branches. Ranking considers attack gain across strikes. The
actual simulated successor must retain the ally and improve its attack output,
so selected-floor magic power, shields and callbacks are not approximated by
subtracting only the printed damage.

Both complete chosen successors/RNG and eight parallel immutable choices pass.
Four additional model controls reject a lethal100ATK ally, choose a surviving
10->24ATK/10->5HP ally or defer if none exists, while preserving all legal lethal
and enemy inputs. The existing16 target-team, four ability and front-damage
controls pass. These are native-root strategy controls plus an existing native
positive effect witness; they do not establish a new native battle win or a
complete default-budget campaign. Other mixed statuses remain unchanged.

```powershell
./scripts/Check-DefaultAdvice.ps1 -Case Targets -SkipBuild
```
