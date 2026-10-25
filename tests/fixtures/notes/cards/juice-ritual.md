# Merchant Juice on Ritual of Battle

Random campaign06, Hellhorned/LazarusLeague, seed1848503146, Covenant0,
Ring4 checkpoint `campaign-ring4-20261025-172637-bb07659b`, saveSHA256
`d0bca84e426d38252ed2a2351f166f368a04a4a264721b3cf597955e764ca3c0`.
The unchanged RitualOfBattle11 carries the original TraitJuice merchant
upgrade `0fc9f6e7-df71-4045-9867-67453769adf8`. The existing model allowed
only the previously calibrated Lifesteal/Scorch owner. It refused the new
owner during the very first deployment input, preventing complete advice.

The original one-input native observation retains that refusal and its actual
successor. Two independent positive observations replay the same19 inputs,
starting from the same deployment. The last input at turn3 plays Ritual11
on allied unit8 in room0: its authored12 Rage becomes24, adding48attack with
health unchanged and24 attributed status stacks. Every prefix root,
successor, complete context and RNG matches the model and the independent
native process; all parents remain immutable. Original save files remain
unchanged. This is a component calibration, not a complete Ring4 victory.

Native `CardTraitJuice.OnStatusEffectApplied` returns max(sourceStacks,0)
for StackMultiplier2. Existing signed/overflow boundary controls still apply.
0.2.63 adds only the exact RitualOfBattle owner
`f9f20326-dccb-44af-894b-467534afab4a` to this calibrated signature.
Additional ordered status-scaling traits, unknown owners and multiple
effective Juice traits remain guarded. The checker explicitly rejects an
unknown owner and an additional Gold-based scaling callback.

```powershell
./scripts/Check-Models.ps1 -Fixture player-juice-ritual-calibration.mt2f -SkipBuild
```
