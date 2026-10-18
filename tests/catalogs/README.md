# Original card definition catalog

`card-catalog.mt2f` is a definition inventory, separate from the curated battle
and component fixtures. It does not increase their 466-archive acceptance count.

The isolated `no-cards` run on 2026-10-18 inspected all 668 unique
`AllGameData.GetAllCardData()` definitions from game module
`8fb07b96-f4db-4d2b-884d-c00536d6ccf4`. Native predicates identify 563 logbook
entries, 536 mastery entries, 91 unit abilities and four room abilities. These
predicates are recorded separately; their counts are not an additive partition.
The remaining definitions include non-logbook/generated/event data. Do not use
668 as the denominator for collectible player-card completion.

The archive retains native identity, type, clan, role predicates, main effects,
traits, card triggers, upgrade-mask facts and the current model projection.
Branch card metadata is enabled explicitly, so the catalog does not mistake a
disabled capture feature for missing Consume, Permafrost or magic-power support.
The runner restricts this mode to the unmodified `no-cards` baseline.
Definition projection errors are recorded per card, without dropping the card.
Projection success does not prove runtime playability or complete interactions.
Native decision state, every deterministic RNG stream (including BattleTest),
and frame stayed unchanged during inspection. No card was played by the catalog
operation. The surrounding sandbox run passed its usual capture checks and
verified the protected original profile afterward.

Source: `.probe-runs/full-battle-no-cards-20261018-224537-f9dff065/card-catalog.mt2f`.
The committed archive is byte-identical: 94,402 bytes, SHA-256
`e32f5a735705fd2ae3c517d16657c7da9949ec7d4167b19f75142287d2ab5d99`.
The projection reflects revision `396abde`: Fused Weaponry ordered equipment
merging and Eclipse Chamber room phase understanding/post-combat growth join
the calibrated primitives. Nested/trait-bearing merge sources, permanent
offsets, changed copy flags, room phase callbacks and friendly floor movement
remain runtime guards. Recall equipment/ability callbacks and stateful or
repeated trigger history are also guarded. Saved boxed-card contents remain separate from owned
identities, and creation descriptors retain original starting-upgrade metadata.
Projection support alone does not establish
direct original-card coverage or remove runtime guards. All 668 raw card
definitions are unchanged from the previous catalog, comparing every field
except Rule and ProjectionError.
All 668 raw upgrade-mask definitions still match the accepted native calibration. Original
card facts and projection guards remain separate from runtime evidence.

Recreate with an otherwise unmodified sandbox scenario:

```powershell
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -Policy no-cards -CardCatalog -BinaryCapture
```

This command performs one isolated native capture, not the fixture regression
suite. The native catalog is the input to the per-card evidence inventory.
