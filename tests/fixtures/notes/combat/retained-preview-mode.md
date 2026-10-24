# Retained actor mode during temporary boss previews

The shared pure suite exposed a regression in `AutomaticRetainedAuraCallback(true)`:
the living actor retained outside room membership kept armor2/buff1 and its
once trigger stayed false. Its local-preview counterpart still passed. This
failure is separate from moon spell/relic compatibility.

The temporary boss-preview actor scope introduced for remote Angel Duo
callbacks incorrectly replaced every actor's Preview flag with membership
of the current collected temporary list. An earlier preview birth retained
outside that list already has native `PreviewMode=true`; it must stay true.
`CombatManager.SetTemporaryStateEnabled` assigns only its collected characters,
and `CharacterState.TemporaryPreviewsEnabled` sets those actors' PreviewMode.
`CardEffectEnchant` compares each target's PreviewMode with the save mode.
These are the captured game's 2.2.1 source contracts, not a new invented rule.

The model keeps the exact collected scope for ordinary live room actors and
preserves existing preview mode for retained actors while also honoring any
explicitly collected retained actor. This restores the two aura withdrawals
and once callback without placing that retained actor back into the room,
changing its identity or awarding primary gold from a preview callback.

Validation:

- The formerly failing pure case now passes unchanged, including 32 immutable
  branches; local preview, retained regeneration/poison/combat-phase exclusion,
  last defender death, self/room targeting and nested payload checks also pass.
- Original native Angel Duo Incant/Slay capture passes five remote +30 heals,
  five excluded outer bosses, one remote +3 Slay increment, all 28 inputs,
  seven EndTurns, 64 room stages and sixteen immutable complete state/RNG chains.
- Original enchantment world/combat calibration passes 32/41 real transitions,
  complete aura maps, 76/135 callback payloads, RNG and 32 immutable branches.
- The historical sweep-preview negative control retains its original three
  failed counters while all 86 room / 17 train stages now replay exactly,
  including eight immutable exchange branches. Its old counters stay unchanged.
- The complete shared pure suite (no archive arguments) passes once after the
  fix in 4.08s. The full curated native fixture regression was not rerun.

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --retained-callbacks-only
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --angel-duo-incant-only tests/fixtures/bosses/angel-duo/full-battle-angel-duo-incant.mt2f
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --enchantment-world-only tests/fixtures/combat/core/enchantment-world-calibration.mt2f
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --enchantment-combat-only tests/fixtures/combat/core/enchantment-combat-calibration.mt2f
```

The native archives are existing unchanged evidence. This is not a claim that
a newly authored native retained-birth scenario or all shared regression
fixtures were rerun. The focused entry point avoids a complete suite just to
reproduce this failure.
