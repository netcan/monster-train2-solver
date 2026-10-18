# Original card definition catalog

`card-catalog.mt2f` is a definition inventory, separate from the curated battle
and component fixtures. It does not increase their 511-archive acceptance count.

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

Source: `.probe-runs/full-battle-no-cards-20261019-040724-5506b27b/card-catalog.mt2f`.
The committed archive is byte-identical: 95,766 bytes, SHA-256
`68a26f84ebdcd4f848ebc8e67223a0ca4eddcaf8565e9201b769f8b18ae231f4`.
The projection reflects revision `f813b49`. All 668 existing raw definitions
match the previous snapshot, including all ordered status lists and Dig Deep's
pile-status `horde` mask.
This field captures native removal and List-filter status traits; Pyreborne
Brainspike retains `cooldown` removal and `avarice` addition separately.
Only Dante's Inferno's specific-card damage trait has this non-null field:
`913694e9-bace-4480-8691-24f699ae583e` (Dante's Candle). This identity is captured
from the actual native trait data. Frame 11,974/11,974, decision and RNG are unchanged.
The surrounding native battle won with all four capture counters zero and
the protected original files unchanged. Positive Corrupt Restricted hand
spells now retain their calibrated selected-room cost; source-room abilities
and unit/room ability activation keep explicit guards.
Original Wracking Pain now retains its calibrated target-debuff stack damage;
attacker/list/buff modes and added/replaced source traits remain uncalibrated.
Original Dante's Inferno retains its calibrated permanent-card count damage,
source damage upgrades, piercing and Consume. Attacker mutation remains guarded.
Original GainXEnergy retains its calibrated current-turn played-discard gain
and unplayed no-op; next-turn routes and added/replaced traits retain guards.
Original Pyreborne Brainspike retains its calibrated X-scaled cooldown removal,
List-filter Avarice addition and Consume, including zero-X and clamped removal.
Missing or changed source status lists remain uncalibrated.
Original Moonbeam retains its calibrated X applications of fixed front-enemy
damage, including zero X and lethal retargeting. Other targeting and range
modes and added/replaced source traits remain guarded.
Original Mulch retains its calibrated visible permanent-deck future draw and
Consume. Original Unearth Treasure retains its calibrated exhausted-pile
threshold, random spell recursion, exact source-family filter, Consume and
Distinct marker. Other pile-scaling routes retain explicit projection guards.
Original Pyre Plating retains its calibrated X-scaled Pyre attack buff,
List-filter armor and Consume; other buff targets and changed source traits
remain guarded.
Original Dig Deep retains its visible permanent-deck Horde scaling and
new/existing-unit routes. Missing pile-status lists and relic stack-modifier
combinations remain guarded.
Original Prism Retrieval retains X-scaled next-draw attack/cost upgrades,
typed Monster draws, queue clearing, Consume and its display-only target marker.
Changed payloads and multiple/filtered queued upgrades remain guarded.
Original Lunacy retains X-scaled moon advances before Conduit addition,
phase parity, ordinary discard and exact Float-one/PlayedCost/List metadata.
Uncalibrated moon card-upgrade/relic callbacks retain guards.
Original Awoken's Rail Spike retains X-scaled played-discard draw, deck draw
blocking and next-turn reset with exact source metadata. Missing draw-block
state, changed source traits, OnCast and active relic combinations retain guards.
The new `MoonPhaseUpgrade` source field retains all seven original upgrade
payloads across Celestial Storm, Gravity, Heavenfall, Silver Strike and Starstruck;
the independent root list records the card/trait-index mapping. Other source
traits retain null. New/Full hand upgrades, OnCast effects and discard/reset
routes now project; missing private state, living units and room/relic combinations
retain guards. Native detached-card controls preserve live decision/frame/RNG.
Original Symphony of the Soul adds direct battle evidence for Corrupt five,
Positive-category multiplication and friendly-only armor multiplication,
including the zero-armor chance draw. These witnesses use the existing
projections; the frozen catalog does not need regeneration.
Original Unplayable markers retain
their native rejection behavior. Scrap Metal retains Persistent discard guards
and its calibrated single-base Refine 3 smelt path. A separate native legality
index records original rejected attempts without increasing direct-play counts.
Original Ice and Pyre retains its
calibrated Limited Range restriction. Titan Corruption retains Monster-only
costs and deployment marker source provenance: its false definition draw flag
is separate from the runtime class's positive eligibility. Native queries do
not establish initial deployment draw ordering or upgraded eligibility.
Siren Song and Trap Chute still have their separate room payload guards.
Titan Bane retains four ordered
native hand cost type filters. Runtime and source descriptors require the same
filters; missing metadata is not inferred. The original hand cost witness also
refreshes Engine Upgrade's complete nested source provenance.
Original Unpurgable cards now retain
their calibrated battle marker, including the positive temporary SelfPurge route.
Native Battering Ram/Bramble Lash
queries retain the Monsters armor/spikes masks and use live selected-room units
for zero and positive two-unit sums. Source trait descriptors retain the ordered
status IDs and query team; older missing masks stay unverified. The catalog
operation preserved decision state, frame 13,853 and every deterministic RNG
stream. All 668 shared raw definitions match the previous snapshot, excluding
Rule/ProjectionError; no raw source field changed.

Original Horn Break, Branding Rite
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

Source trait capture now shares its parameter serializer with this catalog.
The independent metadata baseline
`.probe-runs/full-battle-no-cards-20261019-003014-166d4764` passed all capture
counters and retained native state/RNG/frame; all 668 shared raw definitions
match this frozen catalog exactly. CardState.Setup provenance is attached to
new instance descriptors, separately from mutable runtime traits. Missing old
provenance remains null and cannot establish direct X-scaling source matches.
The latest snapshot also retains that provenance in nested creation rules.
Older Fused Weaponry/Goldy Whelp captures lacked this non-null nested metadata.
Fresh captures on 2026-10-19 retain it, with all native failure counters zero,
protected original profiles unchanged and sixteen independent full chains each:
Fused Weaponry `.probe-runs/full-battle-units-spells-and-junk-20261019-005937-67e20938`,
Goldmance `.probe-runs/full-battle-units-spells-and-junk-20261019-010124-aa9450e0`.
Their source identities match again without inferring missing fields. The new
Battering Ram/Bramble Lash sources match their exact query masks as well.
Both refreshes also pass bounded search: seven native decisions each, zero
unsupported attempts, equal serial/parallel best results and 128 explored
states. Root action counts are 114 for Fused Weaponry and sixteen for Goldmance.

Recreate with an otherwise unmodified sandbox scenario:

```powershell
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -Policy no-cards -CardCatalog -BinaryCapture
```

This command performs one isolated native capture, not the fixture regression
suite. The native catalog is the input to the per-card evidence inventory.
