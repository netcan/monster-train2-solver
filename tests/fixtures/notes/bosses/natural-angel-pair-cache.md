# Natural companion death and empty retention capture

Seed972508374, Stygian/LazarusLeague, Covenant0, champion-preserving Ring6.
The 0.2.57 default10000ms source actually loses after56 inputs and11 EndTurns.
Preserve its original three mismatches and one unsupported card-cycle capture.
These are not evidence of a calibrated defeat or a victory.

At room stage74, Turn8 room0, Savage Angel9 dies from102 health while Entropic
Angel11 survives. Native immediately clears Angel11's paired-boss reference and
marks it as ending the battle on death. The previous model updated the latter
but kept Pair9 in the shared enchantment world until the later train boundary.
0.2.58 clears both at the same death boundary. The local room and enclosing
room/train comparisons now match, including their complete context registries.

At card cycle5, copied Undying card30 has Freeze and Ephemeral. Native purges
it rather than retaining it. Captured PersistentCardIds is an explicit empty
array; this differs from missing retention identities. The latter still refuses
to predict. Old native bytes and the original refusal remain unchanged.

An independent 0.2.58 game process executes the same56 frozen inputs and still
loses. CaptureFailures, Mismatches, Unsupported and Pending are all zero. All94
room stages,22 card cycles,22 train phases, player successors and RNG match.
No advice is published by the frozen replay. This closes two modeling defects;
it does not establish that the default strategy wins Ring6 or the campaign.

```powershell
./scripts/Check-Models.ps1 -SkipBuild -Fixture @(
  'full-battle-natural-angel-pair-cache-calibration.mt2f')
```

FixtureTools `freeze-root-plan <root-plan.mt2f> <new-frozen.mt2f>` now exports a
verified complete offline root plan as replayable inputs. It replays every
immutable successor and full forecast/RNG before export and verifies the new
archive. The output remains ModelPrediction with NativeValidationRequired;
it cannot count as native acceptance or default-budget advice.
