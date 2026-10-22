# Natural event-grafted equipment

Native MVID: `8fb07b96-f4db-4d2b-884d-c00536d6ccf4`.

`full-battle-event-grafted-equipment.mt2f` was captured from the naturally
progressed seed 424244 checkpoint before Ring 5:
`.probe-runs/campaign-ring8-20261022-072727-d98c7abf-before-ring5`.
The original event grafted ordinary `MeatShield` onto `Firebrand`; the host
also has naturally acquired Titanite and Endless upgrades. No deck, card,
unit, boss, health or spawn-queue values were edited for this battle.

Capture:

```powershell
./scripts/Run-FullBattleProbe.ps1 -Policy units-spells-and-junk `
  -InputProfile .probe-runs/campaign-ring8-20261022-072727-d98c7abf-before-ring5 `
  -Seed 424244 -BattleDistance 4 -NativeWorker 2 -PhysicalSpawnPoints `
  -BranchCardMasks -SettleDeathDissolves -SettleCardAnimations `
  -TitaniteCalibration -SkipBuild -TimeoutSeconds 360
```

Source: `.probe-runs/full-battle-units-spells-and-junk-20261022-080753-00d525ed`.
The native battle won with 27 Pyre HP: 33 plays, nine EndTurns, 855 complete
decision contexts, 359 physical compactions and thirteen equipment operations.
Capture/mismatch/unsupported/pending counters are all zero; terminal effects
settled and protected native files remained unchanged.

The focused check requires two births of the grafted host, one initial
equipment attachment and retained severed-equipment snapshots. It compares
the temporary `CardTraitGraftedEquipment`, equipment condition/ownership,
instance-owned graft definition and physical birth, then replays the complete
battle from one immutable root in sixteen independent branches. All native
decisions, full state and RNG match. Rebirth does not reattach severed gear.

`titanite-calibration.mt2f` contains 24 detached original
`StatusEffectDamageReductionState.TestTrigger` calls: odd/even, zero/negative,
float-rounding boundaries through `int.MaxValue`, and piercing-card controls.
Native stage is `OnPreAttackedDamageShield`. Live decision, RNG and frame are
unchanged. The full battle supplies the subsequent real-combat witness.
Titanite combined with Damage Shield or Titanskin remains explicitly rejected
pending an ordering calibration.

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --event-graft-only `
  tests/fixtures/full-battle-event-grafted-equipment.mt2f
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --titanite-only `
  tests/fixtures/titanite-calibration.mt2f
```

Scope: this exact original Firebrand/MeatShield pair and original five built-in
grafts. Other event host/equipment pairs, graft-copy interactions, combined
Titanite shields and actual player-advisor natural late-game victories require
their own native evidence. This fixed-policy victory is not player-advisor
acceptance or a Fragile terminal-boss victory.
