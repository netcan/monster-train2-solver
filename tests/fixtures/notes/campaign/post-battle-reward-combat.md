# Acquired post-battle reward relic in combat · 0.2.115

Random12 Hellhorned/LazarusLeague seed861773648 reaches the natural final
battle with VoidPortal (5cad6ad0-769b-46d3-9464-514b8e74190a). Version0.2.114
rejects its sole RelicEffectAddPostBattleReward before the first input.

The original native class implements only IRelicEffect. Its AddPostBattleReward
method is called by RelicManager.ApplyPostBattleRelicRewards after battle;
it gathers reward references, appends them and sends a relic notification.
It has no combat callback. The battle validator now accepts this exact effect
type while continuing to validate every other effect on the owner. The
different RelicEffectAddPostBattleRewardOnBossDeath remains guarded.

The existing campaign-post-battle-calibration contains108 actual native
manager collections across the original VoidPortal and MothersRemnant2Relic,
including reversed order and duplicate reward prefixes. It passes unchanged;
that evidence covers collection, not reward acceptance/grant.

Two isolated original-checkpoint processes observe the first deployment
EndTurn. The retained original has a null prediction and the exact refusal;
the repaired process has a full prediction matching native state and RNG.
The checker replays both observations, verifies immutable parents, rejects
unknown companion effects in either order and keeps other effect types guarded.
Only52 copies of the exact obsolete diagnostic are projected out of the old
request/state for comparison. Equality with a fresh native deployment is
required before replay; production has no historical-input bypass.

Fixtures: campaign/progression/player-post-battle-reward-combat[-replay]-calibration.mt2f.
Native source:20261026-101424-5c6f85c2; repaired process:20261026-101658-e63cb55c,
clean revision0d96b072. This component acceptance does not establish a final
battle victory or a Windows installation of0.2.115.

```powershell
./scripts/Check-Models.ps1 -Fixture player-post-battle-reward-combat-calibration.mt2f -SkipBuild
```
