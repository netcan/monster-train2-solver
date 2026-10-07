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
