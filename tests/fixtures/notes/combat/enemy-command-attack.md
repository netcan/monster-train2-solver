# Commanding an enemy to attack

A local alternative in the natural Remnant/Awoken sixth battle uses
Steelsinger unit16's ability card31 on enemy13. Original CardEffectAttackWithUnit
always passes MonsterManager as attacker manager and HeroManager as target
manager. Target selection therefore remains on the enemy side even when
the commanded actor is an enemy. The front heavy hits itself: HP120 to114;
the friendly caster stays at HP60 and receives cooldown3.

Unit turns now accept an explicit target team for this effect, preserving
live target collection before every strike. Ordinary combat and
AttackTargetsWithSelfUnit keep their existing targeting. A fixed override
list would incorrectly freeze targets across strikes and removals.

Original rejection and independently repaired native process reproduce
all26 inputs with full successor/RNG equality and immutable parents. The
repaired raw trace has four zero failure counters. Existing unit-turn,
equipment, dazed, selected-room, remote-actor, Pyreshot and true-final
command controls pass. The native witness is the front enemy's nonlethal
self-hit; other combinations remain subject to the existing effect guards.
No full battle win is implied by the component.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -SkipBuild -Fixture player-enemy-command-attack-calibration.mt2f
```
