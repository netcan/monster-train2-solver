# Spikes retaliation and Undying removal order · 0.2.117

Random12 Hellhorned/LazarusLeague final battle, input78 (turn9 EndTurn),
exposes an early physical removal in nested Spikes damage. A lethal enemy hit
leaves the champion temporarily at0HP with Undying. His Spikes kill that enemy.
Native completes the outer damage coroutine and revives the champion before
removing the enemy and refreshing the other enchanters' targets.

The model removed the enemy immediately, while the champion still had0HP.
The boss's debuff aura withdrew and reapplied, replacing four remaining stacks
with five. Subsequent relentless exchanges left boss HP115 instead of109.
The first exchange also retained a wrong LastAttackDamageDealt621 versus611.

Retaliation deaths now retain the pending reviving actor. Trigger queues still
drain, while physical removal waits until that actor is alive. Ordinary deaths,
damage without Undying, and other removal queues keep their existing gates.

The original native archive preserves78 actual input successors and both
individual exchange snapshots. The focused check reproduces all78 successors,
both complete room states including aura maps/statistics/RNG, and four parallel
immutable branches per exchange. The original incorrect forecast stays intact.
This partial archive is a counterexample, not a victory or accepted full plan.

```powershell
./scripts/Check-Models.ps1 -Fixture player-retaliation-revival-calibration.mt2f -SkipBuild
```
