# Natural Hydra equipment catalog

Stygian/LazarusLeague seed972508374, Covenant0, champion-preserving Ring7.
The0.2.58 default10000ms source actually loses after43 inputs/7 EndTurns.
All four failure counters,45 publications, seven search samples,1314 independent
card-mask contexts,357 pure captures and14 train phases pass. This is the
curated native source-only defeat, not an eight-battle clear.

Search excludes HydraArmedHorror at deployment: its authored OnEquipmentAdded
trigger grants Multistrike and removes its equipment. The equipment definitions
live in SummonCatalog, previously captured only when triggered summoning was
enabled. Hydra's native29/35 deployment actually succeeds with an empty equipped
list, while the old model rejects it before spending energy.

0.2.59 captures this catalog when a native authored unit trigger contains
CardEffectRemoveEquipment, independently of summoned units. The original
one-input observation keeps its null forecast and refusal. A separate process
with the repaired projection matches the complete successor and RNG. Removing
only SummonCatalog from every repeated context makes both initial and resulting
states exactly equal to the old observation. The missing-catalog rejection
control remains. Actual MeatShield equipment definitions are present.

The pre-existing original equipment-consumption witness also passes: seven
plays, five EndTurns and16 independent immutable chains include a positive
Hydra conversion/removal. This natural deployment pair alone does not claim
that a generated equipment was consumed or that Ring7 can be won.

Further local refinement after the deployment fix finds one missing generated
equipment definition (TwistedAssistantsBack). Its31-input native observation
is retained separately for the next calibration; it is not marked supported
by this change. Before the catalog fix,14 local candidates all lose; after it,
21 local candidates all lose with this one remaining unsupported signature.

```powershell
./scripts/Check-Models.ps1 -SkipBuild -Fixture @(
  'player-natural-hydra-catalog-deployment-calibration.mt2f')
```

The diagnostic environment's Overlay.Seed label is424242. The actual campaign
seed is972508374, bound by the unchanged native source SHA256
`9ee75d83b164a11ec9c93c2ee73d634315a609ebc99d333079bb227f2f851512`
and complete initial state/RNG; it must not be inferred from that label.
