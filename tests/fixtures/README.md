# Native regression fixtures

These fixed inputs reproduce the independently modeled battle and calibration
checks. Run `pwsh -NoProfile -File scripts/Check-Models.ps1` from the repository
root. The script reads the curated fixture list in this directory.

The retained inputs are `.mt2f` binary archives. They store typed, deduplicated
value graphs, not JSON documents. Regression reads those graphs and constructs
model states directly. Every captured value is preserved; identical repeated
states share storage. See `src/FixtureArchive/FORMAT.md` for the versioned layout.

`manifest.tsv` records the original capture length/SHA-256 and each archive's
length/SHA-256. The regression script verifies its complete curated inventory
and archive hashes before running. `SIMULATION.md` records native coverage and
capture versions; historical JSON/gzip paths there describe the original inputs
before the binary migration. Import verification compares all values, exact
number lexemes, property order and duplicate keys; JSON whitespace and escape
spelling are not part of the retained representation.

Direct native archives use a `native:` source label and zero text-source
length/SHA-256, because no source JSON document exists. Their binary payload and
complete archive hashes still protect integrity. The capture records the game
version and module MVID; optional diagnostic JSON is used only for independent
value comparison.

`results/` is ignored and holds local logs, catalogs and benchmark outputs.
Isolated native probes use `.probe-runs/`, which is also ignored. Successful
captures become fixed inputs here after native and independent comparisons pass.

`full-battle-harvest-triggers.mt2f` retains schema 70 with four actual physical
deaths across both teams: lethal maximum-health loss and lethal Horde damage.
It records complete operation states and 31 OnDeath/Harvest room/actor/dying
dispatches, including death children, player/enemy group order, Horde repetition,
silence, once flags and required dying statuses. Native and independent checks
pass; operations and dispatches repeat in 32 branches, and the complete later
policy matches initial/mid-battle roots in 16 branches. The curated inventory
contains 101 archives: 92 battles and nine calibration suites.

`full-battle-horde-statuses.mt2f` retains schema 69 with eight actual Horde
status/damage/maximum-health operations on a real summoned Steward and a second
Steward observing rally/harvest queues. It preserves accepted queue contents and
actual dispatch order independently of queue calls, plus complete states before
the operation, after it and after draining. Zero/negative additions, troop
thresholds, simulated deaths and spawning cooldown gates compare in 32 branches;
the complete six-play, five-EndTurn policy compares from initial and mid-battle
states in 16 branches. Preview updates are suppressed only during controlled
setup and restored for the full battle. The separate removal fixture below covers
final-stack removal; runtime Horde casualty upgrades, Rally, merging and cloning
remain work.

`full-battle-horde-removal.mt2f` retains schema 71 with nine native direct API,
effect and running-trigger operations, 63 independently compared death/Harvest
phases and a real paid removal spell affecting both teams. It distinguishes raw
zero-HP status changes without physical death signals from sacrifice, preserves
orphan standby cards and responsible sacrifice cards, and checks exact before,
effect-return and drained states, accepted queue counts and dispatch order.
All operations, phases and the paid spell repeat in 32 branches; the later whole
battle also compares from initial/mid-battle roots in 16 branches. Direct effects
and actual card play preserve their different standby settlement boundaries.

`full-battle-direct-unit-upgrades.mt2f` retains schema 53 with 21 actual
CharacterState API operations before the first recorded player decision.
They cover repeated definition IDs, single-copy removal using caller stats,
fresh versus identical anonymous objects, unique/clone/capacity gates, negative
modifiers, equipment-limit caps, attributed maximum HP and partial lethal
application. The source card remains independent. The complete subsequent
policy and 16 parallel branches match native combat; the API operations also
match independently in 32 parallel branches. This establishes the attribute
primitive used by the equipment lifecycle fixture below.

`full-battle-equipment.mt2f` retains schema 54 with 25 actual attachment,
oldest-first replacement and reverse removal operations. Equipment carries
base, permanent and aggregated temporary upgrades; permanent anonymous upgrade
objects retain explicit identity when removed. Card links and standby closures
record the current host separately from the original host: removed equipment
continues waiting until the original host dies. Global return-to-hand checks
occur before ordinary next-turn draws. All operations compare complete native
room/context states independently in 32 parallel branches; the subsequent
19-play, seven-EndTurn policy matches from initial and mid-battle inputs in
16 parallel branches. Equipment-granted triggers are covered separately below;
grafted equipment, equipment-granted abilities and special return overrides
remain unsupported.

`full-battle-equipment-exhausted.mt2f` uses the same attachment/removal setup
with ordinary equipment whose destination is Exhausted. It captures 15 native
equipment operations and a complete 14-play, seven-EndTurn battle, including
attached death returns and global returns of already-detached equipment. Exact
pile order, standby holes, current card links and live TimesExhausted statistics
match independently. Equipment API cases repeat in 32 parallel branches; the
policy matches initial and mid-battle roots in 16 parallel branches. This
fixture has no exhaustion listeners; their callbacks remain separate work.

`full-battle-equipment-overflow.mt2f` retains schema 55. Setup fills the native
hand to its ten-card limit, kills an equipped host through the native direct
upgrade API, then requests a zero-card draw. Attached equipment returns to the
draw pile during death; replaced equipment returns there during the global
check before DrawHand's full-hand exit. Raw death snapshots retain native
equipment references, while spawning declares canonical references matching
decision inputs. Its 30 equipment operations and one lethal direct upgrade
compare independently in 32 parallel branches; the full 21-play, seven-EndTurn
policy matches from initial and mid-battle roots in 16 parallel branches.

`full-battle-equipment-triggers.mt2f` retains schema 58. Base equipment upgrades
add once-only attachment, repeated attachment-to-any and post-combat triggers;
an anonymous permanent equipment upgrade adds another repeated trigger. Every
trigger records the raw native upgrade ID separately from its bound equipment
instance. CardState.GetID() returns a definition ID, so copies share the same
removal key: removing one copy can revoke matching triggers on another copy
while preserving ordinary base triggers. A base equipped-only removal trigger
also retains its condition after the final equipment disappears. Its 25 actual
equipment operations compare in 32 parallel branches; the complete 19-play,
seven-EndTurn policy compares from initial and mid-battle roots with 16 parallel
branches. Its refreshed capture includes persistent trigger IDs and allocation
cursors across equipment addition/removal. Immediate moon phase triggers and
equipment-granted abilities remain separate work.

`full-battle-trigger-mutation.mt2f` retains schema 57. Three native PreCombat
API cases exercise appended triggers firing in the same iteration, removal of
an earlier trigger skipping the next shifted item, and a self-removed trigger
finishing its remaining effects while its removed sibling never fires. These
deployment-turn tests author the balance timing list to permit PreCombat;
the game's original Boss and enemy waves remain intact. Raw upgrade IDs
also distinguish direct API keys from unit-effect upgrade definition IDs.
All three room/context states compare independently in 32 parallel branches;
the subsequent 20-play, seven-EndTurn policy compares from initial and actual
mid-battle roots in 16 parallel branches. Internal immutable trigger identity
is preserved through copies without entering the binary fixture schema.

`full-battle-detached-bonus-draw.mt2f` retains schema 58. Per-unit persistent
trigger IDs and allocation cursors distinguish callbacks from removed triggers
after list shifting and re-addition of the same upgrade definition. Four native
API transitions retain five separate pending counters/listeners, including an
effect that runs after its trigger removes itself. The next actual hand is
capped and receives the callbacks' temporary card upgrades before clearing
their counters. All transitions compare in 32 parallel branches; the complete
17-play, six-EndTurn policy matches initial and actual mid-battle roots in 16
parallel branches. Older captures retain their legacy index keys; detached
execution requires a captured persistent ID.

`full-battle-conditional-triggers.mt2f` retains schema 59. Trigger conditions
require positive status presence, match IDs case-insensitively, and ignore the
configured required stack counts. Self requirements must all pass; dying-target
requirements are bypassed when that separate queue payload is null. Failed
conditions do not consume once flags, and later triggers see status changes
from earlier triggers in the same phase. Eight controlled native cases and six
natural dispatches compare complete room, actor and dying-target states in 32
parallel branches. The natural cases include two actual Slay callbacks with
dead targets. The complete 19-play, six-EndTurn policy matches initial and
actual mid-battle roots in 16 parallel branches, ending at Pyre 80. Relentless
transitions are covered separately below.

`full-battle-companion-boss.mt2f` and `full-battle-companion-boss-actions.mt2f`
retain schema 60. A single companion Boss moves before gaining relentless
after wave exhaustion, then selectively removes flagged triggers before the
status callbacks drain. The original Boss keeps its attack/health and ordinary
enemies keep their waves; the Boss container moves to the second wave so its
forced return from floor two is observed. Loop/PostAscension/OnShift rewards
total 75 before removal, and the surviving status callback adds 55 afterward.
Trigger order and allocation cursors remain intact. Ten no-card phases and
eight actual-card phases compare independently in 32 parallel branches; both
complete policies compare from their initial roots in 16 parallel branches,
with an actual mid-battle root for the card policy. Raw before/after states and
pending Unity destruction IDs are retained separately from canonical phase
boundaries. Independent checks verify that their only projected changes are
destroyed attacker/equipment references. Paired companions and outer/final
Boss state machines remain unsupported.

`full-battle-sentry.mt2f` and `full-battle-sentry-lethal.mt2f` retain schema 61.
Both record two native OnSentry dispatches in physical guard order against the
original Boss. Explicit moved targets bypass LastAttackedCharacter team filters;
once flags, silence and zero-argument threshold gates match complete room,
actor and target states. The lethal case preserves the killed Boss as the
later guard's dead target without applying damage to an old living copy.
Complete movement queues include PostAscension/OnShift, deferred hit/death
callbacks and retained earlier-floor events at terminal combat. The authored
Stewards gain 50 health and reduced size, with one silenced copy; the original
Boss loses initial relentless and gains looping, retaining its stats/spawn wave.
Both whole policies match initial and actual mid-battle roots in 16 parallel
branches: 25 plays/eight EndTurns and 21 plays/seven EndTurns, both Pyre 80.
Native dispatches repeat in 32 isolated branches. Additional unsupported effect
targets continue to reject transitions explicitly.

`full-battle-room-capacity.mt2f` and `full-battle-room-capacity-lethal.mt2f`
retain schema 52 battles with live capacities used by subsequent summons and
restricted size upgrades. Their 220 exact effect contexts and 2,050 native
casting/runtime tests cover occupied-room shrinking, 1/30 bounds, signed wrap,
ignored quantity ranges, exact team flags, conditional cancellation, ordered
paid-cost scaling and unit PreCombat effects. Both policies match from initial
and mid-battle roots in 16 parallel branches; the lethal fixture also verifies
that subsequent capacity effects skip after the spell kills the Boss.

`full-battle-bonus-draw.mt2f` and `full-battle-bonus-draw-lethal.mt2f` retain
schema 51 battles with 125 exact future-draw contexts. They cover signed/ranged
amounts, optional upgrades, actual ordered listeners sharing private effect
counters, ordinary zero draws clearing callbacks while keeping the pending draw
count, and capped hands. Both complete policies match from initial and
mid-battle states in 16 parallel branches; the lethal fixture kills the Boss
with a spell and verifies subsequent-effect gates.

`full-battle-x-cost.mt2f` and `full-battle-x-cost-lethal.mt2f` retain schema 50
native battles with 39 X casts, including 16 zero and 23 positive payments,
96 exact paid-cost damage callbacks and 38 exact subsequent energy contexts.
The lethal fixture finishes with an X spell killing the Boss. Both complete
policies match from initial and mid-battle inputs with 16 parallel branches.

`full-battle-status-callbacks.mt2f` retains the complete schema 46 native battle
and 481 ordered status callback dispatches, including zero additions, actual
removals, silence loss and dying actors. These checks compare captured native
unit/context states independently of the predictions stored in the capture.

`full-battle-status-callback-actions.mt2f` retains a complete schema 48 battle
with nested damage, healing, status, upgrade and source-copy callbacks. It checks
390 status and 419 other character dispatches, 95 nested queue payloads, both
source-card writeback boundaries, and continuous simulation from initial and
mid-battle roots with 16 isolated parallel branches.

`full-battle-energy-effects.mt2f` and `full-battle-energy-effects-lethal.mt2f`
capture current, next-turn and persistent energy changes from spells and unit
triggers. They include phase gates, signed/ranged/zero amounts, native caps,
late end-turn accounting, carried combat gains, cancelled ranges and post-boss
gates. Both complete policies match from initial/mid-battle states in 16 parallel
branches; all 448 observed energy applications also compare complete contexts.

`full-battle-ability-cooldown.mt2f` retains schema 62. Two native Steward summons
carry a real unit ability, its configured activation/spawn cooldowns and the
original common ability triggers. Additional damage-card effects exercise
relative/absolute adjustments, zero/negative values, units without abilities,
spawn/activation reset modes, excess-stack retention and deferred available /
unavailable callbacks. Its 89 native effect states compare independently in 32
branches; the complete 15-play, five-EndTurn battle compares from initial and
actual mid-battle roots in 16 parallel branches, finishing at Pyre 80. Complete
room and action states also verify cooldown decay and available-marker changes.
Captured Mono status dictionary slots/free lists preserve LIFO hole reuse after
cooldown cleanup, including later armor/valor insertion. Stewards gain 50 health
and reduced size; all original Boss stats and spawn waves stay intact. Skill
activation, shared cached skill-card state, assignment/replacement/removal,
equipment abilities and Horde re-spawn interactions remain separate work.

`full-battle-ability-cache.mt2f` retains schema 63 and starts before any ability
card exists. Two natural Steward summons share one detached cached skill card;
OnSpawn generation verifies that cache allocation precedes ordinary generated
cards. A distinct skill on the original Boss exercises enemy cache creation.
Four native creation/reuse contexts compare independently in 32 branches, with
starting upgrades, exact identity, unchanged ownership/statistic membership and
complete context preservation. The 17-play / five-EndTurn policy matches initial
and mid-battle roots in 16 parallel branches. Skill activation/payment and ability
assignment/replacement/removal remain separate work; live cache Clear is not
sampled here. The accepted archive has no JSON source dependency.

`full-battle-ability-activation.mt2f`, `full-battle-ability-activation-x.mt2f`
and `full-battle-ability-activation-lethal.mt2f` retain schema 64. Each records
two real units activating one shared detached skill card. Complete states verify
fixed/X/zero payment, pre-own/own callbacks, self healing through an enemy-only
team mask, activator damage attribution, cooldown/marker changes and global
history without ordinary ownership/discard. The lethal fixture ends when the
second skill kills the Boss; ordinary piles clear while skill identity/history
and permanent-deck statistic fallback remain. Whole policies match initial and
mid-battle roots with 16 parallel branches, and each activation compares in 32
branches. These archives use direct native binary capture with no source JSON.
Ability assignment/replacement/removal and equipment skills remain separate work.

`full-battle-ability-lifecycle.mt2f` retains schema 65 and 21 native assignment,
replacement and removal API operations. Equipment restores the raw activation
cooldown, preserves the original across repeated replacements and loses that
history after direct assignment. Permanent disable IDs preserve order and
repetitions; explicit assignment remains legal, while later natural player and
enemy births omit the disabled skill and its common triggers. Removal's yielding
callback boundary preserves native available markers and fresh trigger flags.
All complete API states compare in 32 branches; the subsequent 16-play,
five-EndTurn policy matches initial and mid-battle roots in 16 branches and wins
at Pyre 80. The 26,797-byte binary archive has no JSON dependency. Card-effect and
equipment attachment integration of these primitives remains separate work.

`full-battle-ability-effects.mt2f` retains schema 66 and integrates native
SetUnitAbility/RemoveAbility into real card and character-trigger execution.
Seventeen complete effect states and 17 queued dispatches, including 35 generated
payloads, compare independently in 32 branches. Coverage includes reverse
multi-target assignment, sticky last-target follow-ups, an inactive relic gate,
pre-own replacement inside the running queue, cached skill self replacement /
removal, and exact ability/common-trigger allocation. Pre-own replacement changes
the current skill without changing the card already selected for this cast;
permanent removal disables that current replacement and preserves duplicate IDs.
The 17-play, five-EndTurn policy matches initial and actual mid-battle roots in
16 parallel branches and wins at Pyre 80. The 29,332-byte direct binary archive
has no JSON dependency. Equipment attachment grants and global relic mechanics
remain separate work.

`full-battle-equipment-abilities.mt2f` retains schema 67 and integrates ability
upgrades with initial summons, direct unit upgrades and real equipment cards.
Permanent B then temporary C selection, keep-existing B preserving base A,
and two naturally disabled upgraded C births are observed. Seventeen native
equipment operations include repeated replacements and restoration of original
C with its raw activation cooldown 6. Seven direct upgrade cases cover matching
removal, skipped/nonmatching removal, grants to empty slots and direct assignment
clearing equipment history. Two actual equipment skill casts exercise damage,
self-heal and shared detached card state. Twenty-two queued character callbacks
and 12 generated payloads compare completely, with 32 isolated branches for
initial summon, direct upgrades, equipment and callbacks. The complete 17-action,
five-EndTurn policy also matches initial and actual mid-battle roots in 16
parallel branches and wins at Pyre 80. This 31,439-byte direct binary archive has
5,000 unique nodes and no source JSON. The first-battle Boss and original waves
remain intact. Broader quiet-callback death, Horde, relic and room ability
interactions remain separate work.

`horde-stat-calibration.mt2f` is a direct native schema 1 numerical calibration,
with 560 exact raw-stat steps and 175 casualty threshold/count boundaries.
It covers first/later additions, independent HP/max HP buckets, signed overflow,
99999 HP caps, 9999 stack caps, lethal HP and the separate threshold gate.
Temporary definition/map/raw-field substitutions are restored before an ordinary
steward-once battle, whose complete chain still matches in 16 branches. The
calibration recomputes all samples independently in 32 branches and records an
unchanged complete live actor/context. The 4,625-byte archive contains 1,051 unique
nodes and no source JSON. UI calls and actual status removal are intercepted
during this numerical oracle; it does not prove Horde status callbacks, simulated
deaths, merging or complete Horde battles. Those remain explicit integration work.

`full-battle-trigger-repeats.mt2f` retains native schema 68 and adds eight complete
queue batches and 26 independently compared conditional trigger dispatches.
The original queue count multiplies the trigger's configured count after its
conditions and once flag are checked. A once trigger executes every repetition
in its first batch; later batches skip it. Zero and negative counts mark flags
without executing effects. The first three-repeat batch queues three separate
armor children, which drain after the parent and share their own once flag.
Silence and explicit fire permission retain hidden-trigger behavior. Independent
checks compare whole batches and each dispatch in 32 branches, then the seven-turn
battle in 16 branches, winning at Pyre 68. The 26,319-byte archive contains 4,135
unique nodes and no source JSON. Complete Horde status lifecycle remains pending.


`full-battle-dying-horde-upgrades.mt2f` is a native schema-74 battle with five
complete dying Horde operations, seven self-upgrade/removal effects and 45 exact
death/Harvest phases. It covers ordinary/unhealed/attributed maximum-health loss
on corpses, troop casualties, partial additions/removals, accepted queue counts,
retained-card writes and positive follow-ups without revival. Mechanism checks
repeat in 32 branches, and the complete 19-play/seven-EndTurn policy wins at Pyre
53 from initial and actual mid-battle roots in 16 branches. The 36,888-byte archive
contains 6,137 unique nodes and no source JSON. Pending-original-death-listener
upgrades and broader Horde summoning/merging/cloning remain separate work.

`full-battle-rally-triggers.mt2f` is a native schema-75 battle with five exact
Horde growth/birth/removal operations, ten post-play player/enemy phases and twelve
Rally dispatches. It records last-spawned reference timing, explicit overrides,
cached original-room actors, other-floor/new-summon exclusion, repeated/once-only
rewards, silence, required armor and an enemy override that bypasses a player-only
effect filter. Complete operation/phase/actor states and accepted queue counts
match independently in 32 branches. The complete 13-play/five-EndTurn policy wins
at Pyre 80 from initial and actual mid-battle roots in 16 branches. The 30,867-byte
archive contains 4,937 unique nodes and no source JSON. Ordinary/cardless summons,
retained/dead references, lethal Rally and Horde merging/cloning remain separate
native integration work.

`full-battle-rally-lethal.mt2f` is a native schema-76 battle with twelve paid team
phases and six Rally dispatches. An ordinary summon triggers the original Boss's
self-death, then receives permanent attack/health and unit-death armor upgrades
before queued OnDeath/Harvest rewards settle. Complete states preserve the
finished-but-not-yet-removing Boss, live last-spawned unit, resolving card history,
cleared costs and fresh terminal Standby allocation. Mechanisms match in 32
branches; the complete thirteen-play/four-EndTurn policy matches initial and
actual mid-battle roots in 16 parallel branches, winning at Pyre 80. The
21,543-byte archive has 3,333 unique nodes and no source JSON. Original Boss
stats and waves remain intact. Cardless summons, retained/dead targets, Rally
movement and broader Horde merging/cloning/revival remain separate work.

`full-battle-unit-identities.mt2f` and `full-battle-unit-identities-lethal.mt2f`
are native schema-77 recordings of the ordinary and lethal Rally scenarios.
The shared unit allocation counter agrees with every room and outer spawn state
at 50/48 spawn/decision boundaries, including ten/thirteen births across recorded
actions and turns. Existing summon, callback, death and terminal-card states
compare completely; the counter advances at births and survives removals and
terminal clearing. Thirteen-play policies finish after five/four EndTurns at
Pyre 80, matching initial and actual mid-battle roots in 16 parallel branches.
The 30,824/21,475-byte archives have 4,954/3,345 unique nodes and no source JSON.
These prove shared identity state and ordinary births; cardless/nested creation
effects remain separate native integration work.


`full-battle-multi-summon-upgrade.mt2f`,
`full-battle-multi-summon-upgrade-unique.mt2f` and
`full-battle-multi-summon-upgrade-restricted.mt2f` retain schema 79. They record
22 extra spawn-upgrade applications at separate direct-character and source-card
write boundaries, including five unique duplicate source rejections and four
capacity-rejected unit changes followed by successful source writes. Births,
copies, upgrades and Rally states compare independently in 32 branches. Complete
15/15/14-play policies from initial and mid-battle roots match in 16 branches,
with five EndTurns and Pyre 80. The original Boss/waves remain intact, captures
are direct binary archives, and original profile signatures are unchanged.
The curated inventory now contains 113 archives: 104 battles and nine
calibration suites. Fresh fallback sources and lethal/removed birth targets
remain separate native integration work.




`full-battle-multi-summon-fresh.mt2f` and
`full-battle-multi-summon-fresh-deaths.mt2f` retain schema 80 with fourteen actual
fresh fallback source setups and 66 global standby checks. Each source starts
without copied resolving-card history/upgrades or an earlier birth's temporary
upgrades. Source presence and the cardless marker remain independent. The death
scene uses a real paid friendly-damage spell and records two original unit cards
still in Standby after their differently sourced hosts die; the global check
returns both to Exhausted with exact statistics and dictionary free slots.
Complete states compare in 32 mechanism branches, and 15/21-play policies match
initial and mid-battle roots in 16 branches with five/seven EndTurns. The original
Boss/waves and profile signatures are preserved. Both captures are direct binary
archives. The curated inventory now contains 115 archives: 106 battles and nine
calibration suites. Null-source, pool/replacement and broader room/relic creation
remain separate native integration work.

`full-battle-multi-summon-additional.mt2f` and
`full-battle-multi-summon-additional-fresh.mt2f` retain schema 81 mixed summons.
Each original four-plus-four request caps to seven positions before choosing
three TrainStewardBig and four TrainStewardSmall units. Ordinary mode records
twelve detached copies and mismatched source-character upgrades; fresh mode
records fourteen matching source setups with no copies. Both retain seventeen
complete births and fourteen explicit extra upgrades, including source writes
that still succeed for mismatched characters. Self-growth skips that source
write. Full birth/copy/upgrade/Rally states match in 32 branches, and initial
and mid-battle 15-play/five-EndTurn policies match in 16 branches at Pyre 80.
Original Boss/waves and profile signatures are preserved. Both captures are
direct binary archives. The curated inventory now contains 117 archives:
108 battles and nine calibration suites. Pool selection, spawn-count relics,
replacement and broader room/relic creation remain separate native work.

The five schema-82 archives `full-battle-multi-summon-pool-additional.mt2f`,
`full-battle-multi-summon-pool-additional-fresh.mt2f`,
`full-battle-multi-summon-pool-singleton-additional.mt2f`,
`full-battle-multi-summon-pool.mt2f` and
`full-battle-multi-summon-pool-no-primary.mt2f` record actual pooled unit choices.
Native pool order and duplicate weights are retained. Every birth consumes a
Battle RNG draw, even for a singleton or a subsequent additional-character
override. Copied/fresh source selection, extra upgrades and complete callback
states are checked independently in 32 branches; initial/mid-battle complete
policies pass in 16 branches, with fifteen plays, five EndTurns and Pyre 80.
The no-primary scene captures base size zero and the native paid-card Rally
gate while preserving cardless birth Rally. Its earlier mismatching exploratory
recording is excluded. All five accepted captures have zero failures,
mismatches, unsupported transitions and pending observations. Original
Boss/waves and profile signatures are preserved; all are direct binary captures.
The inventory contains 122 archives: 113 battles and nine calibration suites.
Missing fresh sources, spawn-count relics, removed birth targets/replacement
and broader room/relic creation remain separate native integration work.

The full 122-archive regression exits zero with 113 battle passes, nine
calibration passes and all five pooled summon checks. Unsupported transition
counts remain zero, and every archive matches the inventory/SHA-256 manifest.

The four schema-83 archives `full-battle-multi-summon-pool-missing-fresh.mt2f`,
`full-battle-multi-summon-pool-additional-missing-fresh.mt2f`,
`full-battle-multi-summon-pool-no-primary-missing-fresh.mt2f` and
`full-battle-multi-summon-pool-no-primary-missing-fresh-deaths.mt2f` record actual
births after a completed no-match fallback lookup. The first two mix 3/11
source-free births with 4/3 matching fresh sources; the latter two record seven
source-free births and no fresh setup. A missing choice cannot reuse the
primary fallback or allocate a card identity. The first source-free birth is
cardless, triggers `OnSpawnNotFromCard`, and skips only the source write of an
extra unit upgrade. The original paid card retains its first-host binding.
The death scene records actual delayed returns during a global Standby check.
Complete mechanisms pass in 32 branches; initial/mid-battle policies pass in
16 branches. Ordinary scenes win at Pyre 80 after 14/15 plays and five EndTurns;
the death scene wins at Pyre 49 after 21 plays and seven EndTurns. The Boss,
waves and original profile signatures are preserved. All four captures are
direct binary archives with zero failures, mismatches, unsupported transitions
and pending observations. The inventory contains 126 archives: 117 battles
and nine calibration suites. Parentless trigger creation, spawn-count relics,
removed birth targets/replacement and broader creation remain separate work.

The full 126-archive regression exits zero with 117 battle passes, nine
calibration suites, four missing-source checks and nine pooled summon checks.
Unsupported transition counts remain zero; all archives match the inventory,
sizes and SHA-256 manifest.

The schema-84 `full-battle-spawn-points.mt2f` archive records 21 real native
position operations and 63 current/allow-last-known queries over three
source-free Stewards. It preserves slot occupants, independent unit pointers,
last-known references, holes, invalid-index no-ops, pivot shifts, cross-room
moves, zero-HP/undying occupancy, preview-born removal and the destroyed-detached
assignment guard. These are controlled API setups, rather than real death,
revival or preview-mode lifecycle tests. Actor-owned points are restored before
ordinary play. Complete operations match in 32 branches; the initial and
mid-battle policies match in 16 branches, winning at Pyre 80 after 18 plays and
six EndTurns. The Instant run takes 45.18 seconds; Boss/waves and original
profile signatures stay intact. All native capture gates are zero. The direct
archive is 25,202 bytes with 4,058 unique nodes. The inventory contains
127 archives: 118 battles and nine calibration suites. Integrating physical
point state throughout ordinary battle transitions, unit-trigger summons and
death replacement remains separate work.

The full 127-archive regression exits zero: all 118 battle archives and nine
calibration suites pass, including one physical spawn-point check, four
missing-source checks and nine pooled summon checks. Unsupported transition
counts remain zero. The complete archive inventory, sizes and SHA-256 values
match the curated manifest.

The schema-85 `full-battle-physical-spawn-points.mt2f` and
`full-battle-physical-spawn-points-no-cards.mt2f` archives carry physical group
occupants and retained current/last-known references in every complete battle
context. Independent checks cover 204 complete room contexts, 30 physical
removals and 17 cross-room moves, plus full roots in 16 branches and complete
rooms in 32 branches. Player/enemy/treasure births, front/back rearrangement,
death/queued completion, treasure despawn, same-room hole filling, ascension
and post-room centering update the shared position state. Retained identities
survive removals; malformed identities/references reject without partial children.

The first archive records 18 plays/six EndTurns/Pyre 80, 254 native compaction
boundaries, 26,962 bytes/4,408 nodes and a 43.66-second Instant run. The no-card
archive records seven EndTurns/Pyre 49, 233 compaction boundaries,
18,096 bytes/2,735 nodes and a 35.44-second run. All native capture gates are
zero, with muted audio and intact original Boss/waves/profile signatures.
The inventory contains 129 archives: 120 battles and nine calibration suites.
Physical capture remains opt-in; general triggered births, removed-target
replacement/equipment transfer, revival, Horde merging, spell movement and
special Boss paths still require further native integration.

The full 129-archive regression exits zero: all 120 battle archives and nine
calibration suites pass, including two integrated battle-position checks, two
standalone spawn-point checks, four missing-source checks and nine pooled
summon checks. Unsupported transition counts remain zero. The complete archive
inventory, sizes and SHA-256 values match the curated manifest.


The schema-86 triggered summon family contains four direct binary archives:
`full-battle-triggered-summons.mt2f`, `full-battle-triggered-summons-fresh.mt2f`,
`full-battle-triggered-summons-death.mt2f` and
`full-battle-triggered-summons-death-fresh.mt2f`. Real player unit definitions
receive native summon, spawn/heal gold and Rally triggers; an extra upgrade
adds one damage, two health and one armor. Death variants apply native lethal
Tower damage using the real rearrangement spell. Boss/waves stay original.

The two live variants each win at Pyre 80 after 15 plays and five EndTurns;
each records ten summon applications, four births and six retained zero-birth
caches. The death variants each win at Pyre 49 after 21 plays and seven
EndTurns, recording two dying-source applications/births. Copied and fresh
source paths remain separate. The archives preserve flat ID-based definitions,
physical positions, first-born effect caches and queued spawn/healing/Rally
boundaries, including actual shared weak-reference changes during previews.

Forty complete native damage steps include two cross-room pending-death
boundaries. Independent checks compare every complete train state and the
zero-HP victim's flags in sixteen branches, including death-statistic listener
removal, pending callbacks, source return and physical centering. Duplicate
deaths reject without partial children or parent changes. Full policy and
position checks remain active. All four native capture gates are zero, with
muted audio and unchanged original profile signatures.

Sizes/nodes are 24,791/3,941; 24,661/3,941; 32,000/5,308; and 32,021/5,308
in the archive order above. Instant sampling times are 43.59, 44.02, 44.63 and
45.73 seconds. The inventory contains 133 archives: 124 battles and nine
calibration suites. Equipment transfer, revival, Horde merging, additional
trigger summon pool/multi-birth combinations and broader special mechanics
remain incomplete; these archives establish the recorded paths only.


The full 133-archive regression exits zero: all 124 battle archives and nine
calibration suites pass. Four summon suites independently compare all forty
native damage steps, along with six integrated position checks, two standalone
position checks, four missing-source checks and nine pooled summon checks.
Unsupported transition counts remain zero. All binary sizes and SHA-256 values
match the complete curated inventory and manifest.

Schema 87 adds four triggered summon equipment archives:
`full-battle-triggered-summons-equipment.mt2f`,
`full-battle-triggered-summons-equipment-fresh.mt2f`,
`full-battle-triggered-summons-equipment-death.mt2f` and
`full-battle-triggered-summons-equipment-death-fresh.mt2f`.
Native setup plays a real Steward and attaches two ordinary cards and one
ReturnToHand card. Real definitions receive equipment upgrades, temporary
offsets, permanent anonymous spikes and equipment triggers. Two-birth summons
exercise the child's one-slot replacement path. Death variants retain the
native Tower lethal spell; original Boss and waves remain intact.

Copied sources duplicate attached equipment for live actors and reuse originals
after native actor removal. ReturnToHand and IgnoreCardUpgrades exclusions,
non-permanent ownership, standby host changes, displaced gear and outer-queue
callback timing are checked independently. Seventeen clone boundaries and
forty-six attachment/removal operations compare full native contexts. Sixteen
damage phases include four cross-room pending deaths. Full policies and
physical state checks run in sixteen/thirty-two branches; missing attachment
definitions reject without partial children or parent changes. The death
variant also verifies that final death-born equipment misses the preceding
TimesPlayed event while receiving later events normally.

In the archive order above, sizes/nodes are 28,661/4,694; 26,359/4,244;
35,034/5,982; and 33,340/5,650. Native Instant times are 51.58, 50.78, 54.33
and 50.41 seconds. Live variants finish after sixteen plays/five EndTurns at
Pyre 80; death variants finish after twenty-three plays/seven EndTurns at
Pyre 49. Every native capture passes with zero failures, mismatches,
unsupported transitions and pending observations, muted audio and intact
original profile signatures. The inventory contains 137 archives: 128 battles
and nine calibration suites. Sizes and SHA-256 values are in `manifest.tsv`.

The full 137-archive regression exits zero: 128 battles and nine calibration
suites pass, including eight queued-summon suites, four equipment suites,
fifty-six independently compared native damage phases and ten integrated
position suites. Unsupported transition counts remain zero; complete archive
sizes and SHA-256 values match the curated inventory and manifest.


Schema 88 adds full-battle-revival.mt2f (35,564 bytes / 5,941 nodes). The native
fixture keeps the original Boss and waves, plays two real Stewards, adds two
ordinary enemy observers and authors both-team Harvest/reanimated/death gold
callbacks. It covers first/last-stack revival, a zero-stack direct API call,
final removal, lethal max-health sacrifice and two revivals inside a running
self-damage trigger queue. One-time statistics listeners and sacrifice flags
are distinct from the retained source reference. Eleven complete revival
boundaries, seven direct/removal operations, ninety callback phases, complete
policies and physical state are independently recomputed with parallel branches.

Standalone setup defers automatic UI queue runners during the captured API,
then drains through the native caller; natural combat has no deferrals. The
muted Instant run wins at Pyre 53 after nineteen plays/seven EndTurns in 50.62
seconds, with zero capture failures, differences, unsupported or pending records
and unchanged original profile/log signatures. The inventory contains 138
binary archives: 129 battles and nine calibrations. Sizes/SHA-256 values are
in manifest.tsv; no source JSON is required for regression.

The complete Check-Models.ps1 regression passes all 138 archives, including
129 battle suites and all nine calibrations, with zero unsupported transitions.
Archive inventory, sizes and SHA-256 values match the curated manifest.

Schema 89 adds four equipment-owned summon archives:
`full-battle-triggered-summons-equipment-owned.mt2f`,
`full-battle-triggered-summons-equipment-owned-fresh.mt2f`,
`full-battle-triggered-summons-equipment-owned-death.mt2f` and
`full-battle-triggered-summons-equipment-owned-death-fresh.mt2f`.
Real return-to-hand equipment receives once-only OnTurnBegin and OnDeath
summons. Native card plays attach it to the ordinary host, and the policy can
re-attach the same equipment after it returns. The original Boss and waves
remain intact. Effects use the host's spawner card; the equipment binding is
separate and is excluded from child transfer. Copied/fresh sources exercise
both inheritance paths, including sources retained during death.

The archives record fifty complete summon effects, twenty-eight equipment-owned
birth applications (eighteen from dying hosts), sixty native damage phases and
1,330 source cache writes. Independent effect checks compare complete room/actor states,
first-birth caches and accepted callback FIFO in sixteen branches. Complete
policies run from original/middle roots; physical state checks use thirty-two
branches. Raw effect/death point references remain intact. An explicit stable
decision protocol exports 2,165 raw/canonical mappings, verifies the clearing
of 1,290 removed-unit reference records and preserves live positions and
physical groups.
These counts include repeated snapshots of the same actors.

In the archive order above, sizes/nodes are 31,013/5,105; 27,905/4,507;
53,404/9,150; and 47,865/8,209. Muted Instant native runs take 134.37, 83.83,
78.06 and 80.17 seconds. Live variants play sixteen cards/five EndTurns and win
at Pyre 80; death variants play twenty-five cards/five EndTurns and win at Pyre
79. Native and independent checks pass with zero failures, mismatches,
unsupported or pending observations and intact original profile/log signatures.
The inventory contains 142 binary archives: 133 battles and nine calibrations.
Sizes and SHA-256 values are recorded in manifest.tsv; no JSON is required.

The full 142-archive regression exits zero: all 133 battle suites and nine
calibrations pass, with zero unsupported transitions. It includes fifteen
integrated physical-position suites, twelve queued-summon suites, eight
triggered-equipment suites, four direct effect suites, four raw/canonical
decision mapping suites and 116 independently compared summon damage phases.
The complete binary inventory matches the curated sizes and SHA-256 manifest.

Revival with equipment/child summons, Horde merging/cloning, broader relic/room
effects, special Boss/Pyre behavior and complete optimal search remain unfinished.

Two new equipment-owned revival archives add complete ordinary card/combat
paths, preserving the original Boss and waves. Hosts start with two undying
stacks and children with one; OnReanimated precedes OnDeath, whose live source
copies equipment while keeping originals attached. There are no standalone
setup revival operations or natural queue deferrals.

`full-battle-triggered-summons-equipment-owned-revival.mt2f` uses schema 91,
85,318 bytes / 15,420 nodes, and records 32 plays / seven EndTurns, final Pyre
76, 41 revival boundaries and 60 callback phases in 113.50 native seconds.
`full-battle-triggered-summons-equipment-owned-revival-fresh.mt2f` uses schema
92, 73,284 bytes / 12,902 nodes, and records 31 plays / seven EndTurns, final
Pyre 75, 43 revivals and 61 phases in 110.98 seconds. Both muted Instant native
runs and independent complete policies pass, with zero native failures,
differences, unsupported or pending observations and intact original profile
and log signatures. Timings measure instrumented capture, not search speed.

Raw card room-cache membership is distinct from the alive-only query view;
zero-HP members become visible again after revival. The archives preserve 534
restored cache observations, 92,140 independently checked raw/canonical cache
mappings, 47 complete summon effects and 146 additional native damage phases.
Schema 92 also retains a zero-HP attacker snapshot omitted from visible units
while its physical slot remains occupied. Independent checks preserve
attribution and physical references without inserting that attacker into the
visible unit list, reject missing cache/reference data and compare states and
accepted FIFO in parallel branches. Counts include repeated snapshots, not
distinct actors.

The inventory now contains 144 binary archives: 135 battles and nine
calibrations. Sizes and SHA-256 values are in manifest.tsv; regression has no
source JSON dependency. The complete Check-Models.ps1 regression exits zero:
all 135 battles and nine calibrations pass, including seventeen integrated
position suites, fourteen queued-summon suites, ten equipment suites, six direct
effect suites, six decision reference suites, the standalone revival suite and
both new revival/summon suites. All 262 native summon damage phases match, with
zero unsupported transitions and intact complete inventory/size/SHA-256 checks.

Horde merging/cloning, broader relic/room effects, special Boss/Pyre behavior
and complete optimal search remain unfinished.

`full-battle-horde-merge.mt2f` adds schema 97 native Horde cloning/merging with
17 API operations, four temporary previews and five manager-order selections.
Exact API/drained room/context/source states, complete primary restoration,
accepted/dequeued callbacks and evolving once flags match in 32 branches.
Coverage includes both teams, different definitions, self/cross-team/null/
non-Horde/immunity gates, a removed actor's pending Rally entry, mixed attached
gear destinations, orphan unit Standby cards and opposing-team physical gaps.
Preview merges retain physical points and equipment; primary restoration
preserves and resolves native shared raw weak room caches.

Its complete 17-play/five-EndTurn battle wins at Pyre 80; initial/middle policies
match in 16 branches. Native failures, differences, unsupported/pending records
are zero, and the original profile/log signatures are intact. The capture uses
real paid hosts and native equipment/enemy API preparation; original Boss/waves
are unchanged. The 31,631-byte archive retains 5,159 unique nodes. The curated
inventory now contains 145 binary archives: 136 battles and nine calibrations.
Sizes and hashes are in manifest.tsv; no source JSON dependency is introduced.

The complete 145-archive regression exits zero: all 136 battles and nine
calibrations pass. It retains eighteen physical-position suites, fourteen
queued-summon suites, ten equipment suites, six direct summon-effect suites,
seven decision reference suites, all 262 summon damage phases and the new Horde
API/preview suite, with no unsupported transitions. Both restored raw weak cache
memberships match; complete inventory/size/SHA-256 checks pass.

The direct Horde APIs are covered. Real Bump movement/card integration,
ordinary unit cloning, wider relic/room/Boss/Pyre behavior, broader card coverage
and complete optimal search remain unfinished.
