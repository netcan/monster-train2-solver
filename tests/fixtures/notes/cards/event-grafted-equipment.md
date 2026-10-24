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
Titanite combined with Damage Shield/Titanskin now has 1,536 original native
stage controls. Shield insertion order and the distinct card/unit piercing
rules are modeled; Damage Shield combinations require captured order and at
most sixteen active statuses. See [combination calibration](../combat/titanite-combinations.md).

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --event-graft-only `
  tests/fixtures/cards/equipment/full-battle-event-grafted-equipment.mt2f
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --titanite-only `
  tests/fixtures/combat/core/titanite-calibration.mt2f
```

## Actual player advice and independent replay

Search profile `full-battle-units-spells-and-junk-20261022-082738-90f25d85`
loads the real player assembly on the same natural before-Ring5 checkpoint.
It wins with Pyre15 after 38 plays and nine EndTurns (47 inputs). The panel
publishes 49 times; pause/resume, hide/cancel, show/refresh and actual pointer
drag/raycast controls pass. The first search takes 10.71 seconds; all 46 later
inputs retain the verified winning suffix with zero search nodes, median/P95
response 1.57/2.07 seconds. This uses horizon12, 30,000-ms tactical search and
deployment floor2, not default-budget performance.

Independent frozen replay `full-battle-units-spells-and-junk-20261022-083546-9b98f087`
loads identical player/model/archive DLLs and wins with the same Pyre15.
All 47 inputs, roots, successors, forecasts and RNG match. Both runs have
zero capture/mismatch/unsupported/pending counts and settle terminal effects.
The focused pair check also requires real temporary graft attachment and
enemy physical orders that differ from their native creation/identity order.

```powershell
./scripts/Check-PlayerAdvisor.ps1 -Case EventGraft -SkipBuild
```

Scope: this exact original Firebrand/MeatShield pair and original five built-in
grafts. Other event host/equipment pairs and graft-copy interactions need
their own evidence. The natural Ring7 fixed-policy
replay matches but loses; its actual-player scout also only found losing
plans. Neither is a Ring7 or Fragile terminal-boss player victory.
