# Friendly-trigger relic damage through shields

The natural Remnant/Awoken sixth battle reaches a shielded HeavyT2_Basic23
when Dreg21 dies at input40. DamageOnBurnoutDeath's original
`RelicEffectDamageEnemyOnFriendlyTrigger` deals5 through the shield. After
the room's later feeding/boss callbacks, native health is115 and shield4;
the old model incorrectly blocked the damage, leaving health120 and shield3.

Native `DamageHelper.IsPiercingDamage` tests the damage relic's effect types
against the original balance list. `RelicEffectDamageEnemyOnFriendlyTrigger`
passes its relic state to `ApplyDamageToTarget`. The model now retains piercing
for this already validated effect family in the actual room, preview and
standalone application paths. Other relic damage keeps its existing behavior.

The original40-input observation preserves the counterexample. The focused
checker compares every native input/root/full successor/RNG and immutable
parent. Its historical projection permits only Heavy23's health and shield
stack values, including the corresponding enchantment-world caches.
An independent repaired observation must preserve the same complete native
prefix and final result. A component pass does not count as a campaign win.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -SkipBuild -Fixture player-friendly-relic-piercing-calibration.mt2f
```
