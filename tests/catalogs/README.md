# Original definition catalogs

`boss-rule-catalog.mt2f` freezes the complete native rule catalog captured on
2026-10-23 from `full-battle-no-cards-20261023-172252-3c660349`. It retains all
original characters and all outer-boss scenarios with their enemy-relic growth
callbacks. Inspection verified unchanged gameplay RNG and frame; the surrounding
native run exited normally with protected files unchanged and all four failure
counters zero. SHA256 is
`80459451514dde9b2313d31c7c27ca812ce055ea43c8c8c11fbfc0b756526f65`
(59,983 bytes). It is definition evidence, separate from runtime battle fixtures.

`ModelChecks --angel-duo-catalog-only tests/catalogs/boss-rule-catalog.mt2f`
checks all three original Angel Duo scenarios. Their six role definitions retain
identical birth/death payloads within each role. Their growth pairs differ:
PostCombat health60 / OnTrainRoomLoop attack8; CardSpellPlayed health30 /
OnKill attack3; OnAnyHeroDeathOnFloor health50 / OnHit attack1. These signatures
identify runtime work; catalog presence alone does not enable a new player route.

`scripts/Get-BossCoverageInventory.ps1` compares all fifteen ordinary base-game
Ring3/6/8 scenarios against the current model identity gate and curated native
battle/player-pair metadata. It generates `tests/fixtures/boss-coverage.md` and
`boss-inventory.tsv`; `-Check -SkipBuild` verifies their freshness without native
games or a battle regression. Other boss systems keep separate denominators.

## Card definition catalog

`card-catalog.mt2f` is a definition inventory, separate from the curated battle
and component fixtures. It does not increase their curated acceptance count.

The isolated `no-cards` run on 2026-10-21 inspected all 668 unique
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

Source: `.probe-runs/full-battle-no-cards-20261021-034102-061ebda8/card-catalog.mt2f`.
The committed archive is byte-identical: 100,225 bytes, 12,187 unique nodes,
SHA-256 `fac3bf2624c3e776d9132115a7eb1df6c6eb3534b32be434aed0e22c9810f831`.
The latest refresh captures four room ability attachments and their four skill
definitions, including native cooldown/creation and Echo Bloom's corruption
modifier. Only these eight projections change after normalizing absent/null
Ability and CorruptionModifier fields; the other 660 are identical. Runtime
activation requires its own accepted native witness in the inventory.
The preceding refresh recognizes the captured Treasure trait for Crypt Builder,
Guardian Amulet and Titanstooth. Only these three projections change; the other
665 are identical. Strict definition matching is unchanged.
The preceding refresh explicitly captures the fresh birth lifecycle of Infectory's
fallback template, matching its accepted native witness. Only this projection
changes; the other 667 are identical. Strict definition matching is unchanged.
The preceding refresh captures Apex Imp's status synergy and empty birth counter
cache. Only this projection changes after normalizing absent/null SynergizeStatuses
fields; the other 667 are identical.
The preceding refresh captures Morsel-Made's authored self-feeder effect. Only this
projection changes; the other 667 are identical.
The preceding refresh captures Hydra-Armed Horror's equipment removal and
Soulsmelter's status-quotient encounter award. Only these two projections change;
the other 666 are identical.
The preceding refresh captures Siren Song's pre-combat Dazed/movement composition.
Only this projection changes; the other 667 are identical.
The preceding refresh captures Infectory's authored room death effect and Funguy
exclusion filter. Only this projection changes after normalizing absent/null
DeathEffect fields on room attachments; the other 667 are identical.
The preceding refresh captures the authored equipment/birth rules of Sanguine
Assistant, Twisted Assistant, Pufferflesh, Erratic Assistant and Double-Edged
Bearer. Only these five projections change after normalizing absent/null Graft
fields on existing unit modifier descriptors; the other 663 are identical.
The preceding refresh captures Ethereal Seelie's queued room status modifier.
Only this projection changes; the other 667 projections are identical.
The preceding refresh captures Stasis Crystal's native status-scaled PostCombat
upgrade. Only this projection changes; the other 667 are identical.
The preceding refresh captures Copy Morsels' native unit birth-copy modifier.
Only this projection changes after normalizing absent/null SpawnCopySubtype
fields on existing unit modifier descriptors; the other 667 are identical.
The preceding refresh captures Hall of Mirrors' native room birth-copy modifier.
Only this projection changes after normalizing absent/null SpawnCopySubtype
fields on existing room attachment descriptors; the other 667 are identical.
The preceding refresh captures Total Recall's native pre-combat consumed-spell draw.
Only this projection changes after normalizing absent/null ConsumedSpellDrawCount
fields on existing pre-combat descriptors; the other 667 are identical.
The preceding refresh captures Hellfurnace's native room Pyregel stack modifier.
Only this projection changes after normalizing absent/null StatusStackBonuses
fields on existing room attachment descriptors; the other 667 are identical.
The preceding refresh captures Imprisoned Zephyr's self/front attack-triggered
floor rearrangement. Only this projection changes; the other 667 are identical.
The preceding refresh captures Weapons Cache's triggered next-draw upgrade and
typed draw. Only this projection changes; all other 667 definitions are identical.
The preceding refresh captures native room upgrade trigger definitions, filters,
traits, replacement references, unhealed health and room ability identities.
Exactly seven model projections change: Boiler Room, Awakening Thicket,
Sacrificial Chamber, Bloodsoaked Arena, Rage Room, Reanimation Lab and
Electricity Chamber. All other 661 model projections remain identical.
The preceding refresh captures Formless Child and Enlightened Infery's native
character-triggered recursion payloads. Only these two projections change;
all other 666 decoded card definitions stay identical.
The preceding refresh captures Crucible Steward's native source-attack damage
callback. Only this projection changes; the other 667 definitions are identical.
The preceding refresh captures Mooncaller's native room trigger-count modifier.
Only Mooncaller's card projection changes semantically; after removing null
RoomTriggerCounts fields, all other 667 decoded card definitions stay identical.
The preceding refresh captures Greed Dragon's Hoard upgrade and Shadeguard's
Moon-phase upgrade. Exactly these two card projections change; all other 666
decoded card definitions remain identical.
The preceding refresh captures Vinemother's native temporary Sting upgrade and
OnlyStings filter. Only Vinemother's card projection changes; the other 667
decoded card definitions remain identical.
The preceding refresh captures Deranged Brute, Shard Channeler and Lead Songbird's
native Rage, Spikes and Valor room modifiers. Exactly these three card
projections change semantically; after removing null RoomStatuses fields,
all other 665 decoded card definitions remain identical.
The preceding refresh captures Edge Prior's native healing-cost room modifier.
Only Edge Prior's card projection changes; the other 667 definitions remain
byte-for-byte equivalent in their decoded card objects.
The preceding refresh captures native unit room resource modifiers and accepts
Ember Forge and Wildwood Custodian. Exactly these two card projections change
semantically; after removing null RoomResources fields, all other card
definitions remain identical. Purified resource units remain guarded.
The preceding refresh captures discarded-copy cleanup upgrade exclusion IDs and
accepts the three native cleanup definitions for Pandemic, Spreading Spores
and Contagious. Exactly these three card projections change semantically;
all other card definitions remain identical after removing the new optional
exclusion-ID field from the comparison. CopyOnPlay sources remain guarded.
The preceding refresh clears unused shared-field guards for Forge Steward and
Salvage Machine, records Funguy in a Suit's source-status multiplier, and
accepts Making of a Morsel's card-only Hand generation target. Exactly these
four Rules change semantically. SourceStatusMultiplier is nullable in all
effect projections; all existing native definition parameters stay identical.
The preceding refresh projects Bounding Echoes and Returned Soul's temporary Corrupt
trait upgrades. Exactly those two Rules change; all 668 raw definitions match.
The preceding refresh removes Awoken's Rail Spike's obsolete hand-discard guard.
Its played-cost draw and draw blocking remain validated by CardDiscardDrawModel;
only its Rule changes and all 668 raw card definitions stay identical.
The preceding refresh removes obsolete hand-discard guards for branch-owned
Persistent and Ephemeral traits. Seven Rules changed: Scrap Metal, TFB Entropy
Scourge, TFB Pyre Attack and four non-logbook Infusion definitions (Blast,
Goliath, Madman and Enfeebling). All 668 raw card definitions stay identical.
Native witnesses distinguish unplayable retention, unplayed purge overriding
Freeze, two played Ephemeral purge references and three with SelfPurge.
The preceding refresh projects Inhale's OnKill spawn-or-status callback and
Boilerplate's permanent armor per Forge Point. Rules gain nullable OnForgeEffects;
apart from that schema field, only these two Rules changed. All 668 raw native
card definitions remain identical. The permanent-status flag stays captured;
only the calibrated original Boilerplate armor upgrade leaves its former guard.
The preceding refresh captures independent native card-trigger type, amount,
persistence and upgrade fields, plus projected original OnKill definitions.
Fanning the Flame and Subsuming Blade gain source-only permanent damage growth;
only these two Rules changed, with all 668 raw card definitions unchanged.
Inventory promotion additionally requires the native callback payload to match
the original catalog, preventing modified trigger parameters from counting.
The preceding refresh captures Silver Ink and Magic Power Hand upgrade filters and
removes the guard on unused shared fields in Death's Dancer's temporary unit
upgrade. Only these three Rules changed; all 668 raw definitions are unchanged.
The preceding refresh projects Shadesplitter's Room-targeted OnKill generator and
Revenge of the Damned's OnKill corruption. Only these two Rules changed;
all 668 raw native definitions remain identical.
The preceding refresh projects Bone Dog's fixed positive encounter-complete Pyre
maximum-health adjustment. Only its Rule changed; all 668 raw definitions are unchanged.
The preceding refresh projects Pyre Steward's fixed two-HP OnKill train healing.
Only its Rule changed; all 668 raw native definitions are unchanged.
The preceding refresh projects Dante's permanent OnSpawn card generation. Its unused
AddStatus card pool is omitted exactly as in runtime capture. Only Dante's Rule
changed; all 668 raw native card definitions remain identical.
The preceding refresh projects Goldtooth, Lady of the House and Bog Fly's contextual
self attack rules. Unit modifier descriptors gain a nullable field. Independent
native character room-modifier definitions expose all fourteen such originals
for signature-based scheduling; all 668 existing raw card definitions are unchanged.
The preceding refresh projects Frostpiercer's last-attack-damage status action.
The preceding refresh adds native HasMoonPhaseCardEffect to nested card-creation
rules so generation can activate player moon tracking. Sixty-four Rules have
the new nullable field; all 668 raw native definitions are unchanged.
The preceding refresh omits discard-copy upgrades when source modifiers are not
copied or the destination is not DiscardPile. Both native generation iterators
only inspect these upgrades inside those two conditions. Twenty-two Rules
changed, with all 668 raw native definitions unchanged. Pandemic, Contagious
and Spreading Spores retain their full discard-copy upgrade payloads and guards.
The catalog and surrounding baseline passed; earlier callback projections remain.
Original Enchant effects retain their native test flags. Binding an aura through
its spawn lifecycle does not set HasTriggered when its only effect has
ShouldTest=false. Legacy evidence without these flags retains its prior behavior.
Static unit-ability scaling queries preserve the owning Spell CardData type.
Kinhost Carapace's two UnitUpgrade traits, Pyreblooded's UnitUpgrade trait and
Railgun's StatusScaling trait retain native `CurrentCostSourceIsSpell=true`.
The 668-definition native catalog pass kept decision state, RNG and frame
unchanged; its surrounding baseline won with zero capture failures, mismatches,
unsupported transitions and pending records.
An earlier refresh captured unit-trigger `CardEffectRemoveAllStatusEffects`
actions through the existing status-removal projection. Its only `Rule`
projection changes were Green Recruit, Eel Gorgon and Cluster Colonel; native raw
fields across all 668 cards were unchanged.
Ancient Synergy retains `QueryCardType=Spell` for its native TypeInDeck filter.
Greed, Golden Touch and Bonk retain their fixed
card-owned resource kill effects.
Original Surgical Strike retains its source-card self-copy and original
temporary Ephemeral upgrade. Its source suppression trait retains `KillTargetTeam=Monsters` and
`KillTargetSubtype=SubtypesData_FunGuy`, read directly from native CardTraitData.
Null fields are omitted on other traits for legacy evidence identity.
Original Bloody Moon and Antumbra
Assault retain their captured card-owned kill triggers. Original Morsels retain
last-feeder status targets and initial zero feeder references in both native
definition templates and living food units; Void Excavator's status effect keeps its unused
upgrade field without imposing another effect type's modifier guard. Kill-trigger
metadata remains branch-owned; missing payloads and unsupported trigger routes
still reject. Existing raw fields retain ordered status lists and Dig Deep's
pile-status `horde` mask. Animus of Speed's original ability now projects the
calibrated selected-target self attack with normal prevention and cooldown;
targeted unit ability activation and enumeration retain their explicit legality
checks. Other parameter/relic/trigger combinations need separate witnesses.
This field captures native removal and List-filter status traits; Pyreborne
Brainspike retains `cooldown` removal and `avarice` addition separately.
Only Dante's Inferno's specific-card damage trait has this non-null field:
`913694e9-bace-4480-8691-24f699ae583e` (Dante's Candle). This identity is captured
from the actual native trait data. Frame 14,165/14,165, decision and RNG are unchanged.
The surrounding native battle won with all four capture counters zero and
the protected original files unchanged. Five original grafted equipment cards
retain their calibrated costs, upgrades and private Severed state. Automatic
birth grafts, child handoff, copying/merging grafts and added/replaced traits
retain explicit guards. Positive Corrupt Restricted hand
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

The Sentry Turret refresh comes from
`.probe-runs/full-battle-no-cards-20261021-040610-e53eaf4a/card-catalog.mt2f`
(100444 bytes, 12198 nodes, SHA-256
`fbb73a6ec2eb2bb511bb77333ab286e0a5194a5b2511f54369a6aedb3de7bbdf`).
After ignoring newly absent/null Sentry fields, only Sentry Turret changes among
668 definitions: its supported room modifier captures an empty target list,
false attacking flag and original movement rules. Its original direct play and positive attack are checked
separately against the native battle witness.

The Echo Vault refresh comes from
`.probe-runs/full-battle-no-cards-20261021-041950-8b750633/card-catalog.mt2f`
(100454 bytes, 12209 nodes, SHA-256
`87ab01e7204c995b2e7ab9c7f4013390c433e348f2b920dac492eba56551876a`).
Ignoring newly absent/null Echo fields, only Echo Vault changes among 668
definitions: its native empty remembered-spell state replaces the attachment
guard. Native copying, overwrite, same-floor replay and an other-floor control
have a separate unchanged-original witness.

The Squad Leader refresh comes from
`.probe-runs/full-battle-no-cards-20261021-043848-6330db70/card-catalog.mt2f`
(100592 bytes, 12213 nodes, SHA-256
`776863ce7e7fe9aca2b17c790cf8bab51ac66179d70de0f3372b890603227d00`).
Ignoring newly absent/null TroopAura fields, only Squad Leader changes among
668 definitions: its original troop upgrade, statuses, target cache and
movement rules replace the unsupported room-modifier guard. Birth, growth,
troop loss and floor departure have a separate unchanged-original witness.

The final hidden-definition refresh is from
`.probe-runs/full-battle-no-cards-20261021-061053-d0de3aa7/card-catalog.mt2f`
(110315 bytes, 13578 nodes, SHA-256
`c9ea090d631034900d459f456e0027b8aa6863bb2f57e6de31fba6e6a4027723`).
All 668 identities and native role counts remain unchanged. After normalizing
absent/null optional fields, exactly sixteen projections change: the fifteen
previously guarded hidden/internal definitions and the calibrated selected-floor
Pyreshot focus flag. The new projections cover three Corrupt Restricted skills,
four Infusions, Fetch, Savior levels two/three, Courage, Pyre Time Freeze,
Chomp, Lunar Echo and single-stage spike crafting. Decision, RNG and frame are
unchanged by inspection; this is an isolated catalog capture, without a fixture
regression run. Original skills remain unchanged in their separate native
activation witnesses; some use documented control owners.

Recreate with an otherwise unmodified sandbox scenario:

```powershell
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -Policy no-cards -CardCatalog -BinaryCapture
```

This command performs one isolated native capture, not the fixture regression
suite. The native catalog is the input to the per-card evidence inventory.
