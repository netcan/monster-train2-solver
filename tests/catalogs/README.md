# Original card definition catalog

`card-catalog.mt2f` is a definition inventory, separate from the curated battle
and component fixtures. It does not increase their 482-archive acceptance count.

The isolated `no-cards` run on 2026-10-19 inspected all 668 unique
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

Source: `.probe-runs/full-battle-no-cards-20261019-001202-b8e6c17f/card-catalog.mt2f`.
The committed archive is byte-identical: 95,096 bytes, SHA-256
`5048468d039d9471d4e74b53ddbe50bd11c51b60fd53096f594af38176f94404`.
The projection reflects revision `b1704af`: original Horn Break, Branding Rite
and Inferno piercing have native controls for all four protection stages,
including ordinary Spell Shield precedence. Inferno Room pre-combat damage
includes lethal cleanup, preview writes, VFX room focus, replacement and
restoration. Original Grow Room propagatable
status growth and Mushy Room enemy Sap callbacks have isolated full train
comparisons, positive restoration and floor/team controls. Raw room metadata
now retains deployment restrictions and nested effect filters: Grow Room skips
deployment and filters propagation, while Mushy Room allows deployment.
Zone of Silence enemy status and
trigger suppression, Memorial of the Melted Endless death/redraw, and Planetarium
Conduit contributors/tracker replacement have native witnesses. Room character
synchronization retains shared context changes. Original Engine Upgrade delayed
energy and Stewards Quarters subtype/Multistrike upgrades join Fight Club attack
and Spell Sanctum cost attachments. Native witnesses
also cover five original forge spells, three propagation spells and Great Decay
across card piles. Card modifier resolution preserves propagation filters.
Fused Weaponry ordered equipment merging and Eclipse Chamber room phase
understanding/post-combat growth remain supported. Nested/trait-bearing merge sources, permanent
offsets, changed copy flags, room phase callbacks and friendly floor movement
remain runtime guards. Recall equipment/ability callbacks and stateful or
repeated trigger history are also guarded. Saved boxed-card contents remain separate from owned
identities, and creation descriptors retain original starting-upgrade metadata.
Projection support alone does not establish
direct original-card coverage or remove runtime guards. All shared raw fields
of all 668 card definitions match the previous catalog, excluding only Rule and
ProjectionError.
The new room energy/filter fields make some older captures insufficient for
exact definition matching; they remain mechanism evidence. The matcher does
not infer absent non-null metadata from a shared DataId.
All 668 raw upgrade-mask definitions still match the accepted native calibration. Original
card facts and projection guards remain separate from runtime evidence.

Recreate with an otherwise unmodified sandbox scenario:

```powershell
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -Policy no-cards -CardCatalog -BinaryCapture
```

This command performs one isolated native capture, not the fixture regression
suite. The native catalog is the input to the per-card evidence inventory.
