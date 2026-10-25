# Acquired Pyre maximum health in battle · 2026-10-25

The seventh random campaign's third battle refused every continuation because
PyreHeartFather (`fe768ab5-3f58-4f96-9afc-48f0f30c7b21`) owned
RelicEffectModifyTowerMaxHealth. Its native acquisition had already increased
the Pyre from80/80 to105/105 through normal campaign rewards.

The sealed native effect implements IRelicAcquireRelicEffect and
IModifyTowerMaxHealthRelicEffect. Acquisition adjusts positive HP and refreshes
the maximum; removal is empty. It has no combat callback. The battle model now
accepts that exact effect type and retains captured HP/max HP, without replaying
acquisition in search branches. Other unknown effects still reject the owner.

Two independent native processes executed the first end turn from the same
Ring3 checkpoint. The original23.53-second observation preserves its null
prediction and rejection; the repaired35.25-second observation has a matching
prediction, complete successor/RNG and pure parent state, retaining105/105.
The old capture projected its global rejection into eight room diagnostic
lists (four live and four aura-world rooms). Only that exact string is removed
in memory when comparing the two physical native executions. No original
archive, health, card, unit, RNG or other diagnostic is changed. The repaired
request uses a fresh capture, since the old request's diagnostics intentionally
no longer match; an interrupted attempt stopped before executing input.

The two fixtures live in `relics/modifiers/player-pyre-max-health-*.mt2f`.
The focused checker also retains the unknown-combat-callback guard:

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll `
  --pyre-max-health-pair-only `
  tests/fixtures/relics/modifiers/player-pyre-max-health-calibration.mt2f `
  tests/fixtures/relics/modifiers/player-pyre-max-health-replay-calibration.mt2f
```

The existing campaign draft calibration also passes:840 native draft selections,
three Pyre acquisition/removal transitions, starting upgrades and exact RNG.
This component acceptance does not establish a winning third-battle route.
