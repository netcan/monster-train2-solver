# Native regression fixtures

These fixed inputs reproduce the independently modeled battle and calibration
checks. Run `pwsh -NoProfile -File scripts/Check-Models.ps1` from the repository
root. The script reads the curated fixture list in this directory.

Current inventory is 192 archives: 162 accepted battles and 30 component
calibrations. The initial Incant model's complete 180-archive regression passes.
The threshold model's combined181 regression has also completed successfully.
The relic model's combined181 regression has also completed successfully.
The corrected checker's combined182 regression has also completed successfully.
The extended trigger-count model's complete183 regression has completed:159
battles,155 paid policy chains,four no-more-card chains and24 calibrations pass.
The original spawn-status model's complete185 regression has completed:161
battles,157 paid policy chains,four no-more-card chains and24 calibrations pass.
Subsequent ability-count and relic-birth scheduling changes require their own
regression; the frozen185 baseline does not include those changes.

`full-battle-spawn-status-relics-clones.mt2f` retains schema110 with the three
original spawn-status artifacts and unchanged ExtraSpawnTrigger. It adds13 clone
operations and22 complete birth observations:13 standalone settlements and nine
deferred phases, including exact prior/remaining queues and dispatched payloads
across the whole train. Both native recordings and independent complete battles
pass; all28 pre/post-policy states repeat exactly. The final scheduling model's
complete186 regression also passes:162 battles (158 paid/four no-more-card
chains) and24 component calibrations, with every input and frozen binary hash
audited. This baseline precedes further card-upgrade-mask model work.

`card-upgrade-mask-calibration.mt2f` retains schema2 with62 original upgrade/card
filters against668 raw card definitions,15 owned cards, one live character and
null sources:42,470 native matrix queries plus58 controlled native boundary
queries. The independent component and32 immutable parallel replays pass. The
native capture preserves full context, units, gameplay/test RNG and frame and
its complete recorded battle also passes independent policy checks. This archive
is34,127 bytes/3,087 nodes, with no JSON dependency. It calibrates eligibility;
applying temporary relic upgrades in battle remains unfinished. The full187
regression is separate from the completed preceding-model186 baseline.

`card-modifier-overflow-calibration.mt2f` retains2,240 schema1 native arithmetic
queries:1,344 numeric results and896 original minimum-integer exceptions across
eight statistics, both floor settings and offset/upgrade placements. Independent
values/exception types and32 immutable replays pass. The corrected shared
modifier function also passes all pure checks, original256 numeric calculations,
the upgrade-mask calibration and this recording's complete paid battle. The
archive is9,282 bytes/2,949 nodes with no JSON dependency. Its correction is later
than the preserved frozen187 run and requires a separate full188 regression.

The frozen187 and188 runs now have complete output audits: every162 battle
passes (158 paid/four no-more-card chains), with25 and26 ordered calibrations
respectively. All input and retained binary hashes match. Their processes are
gone; interruption discarded the terminal handles, so original exit codes
cannot be verified. These are complete check-output audits, not preserved
terminal-exit-zero evidence. The complete186 baseline retains that evidence.

The later frozen189 and190 regressions retain successful terminal status and
complete output audits:162 battles (158 paid/four no-more-card chains), with27
and28 ordered calibrations respectively. Every input's manifest size/hash and
the frozen model/checker hashes match. Durable exit codes are zero, with terminal
timestamps2026-10-10T13:14:05.0335854Z and2026-10-10T13:21:27.8389773Z.
These validate the trait and status composition versions respectively; they
precede the owned-card mask and temporary-upgrade lifecycle additions.

The new trait composition model separately matches2,134 native forced-refresh
observations:668 raw card trait seeds,15 isolated owned-card copies and384
controlled combinations, each refreshed twice with persistent results carried.
It preserves ordered first-base replacements, parameter resets, the previous
combined list's insertion bound, temporary-original-type deduplication, removed
types, temporary replacement markers and Exhaust/SelfPurge suppression. Exact
base/temporary/created instance origins and32 immutable parallel replays pass.
The muted Instant capture exits zero in97.15 seconds without changing original
files, complete context, gameplay/test RNG or frame. Its separate complete
21-play/seven-EndTurn battle passes independent initial/mid-battle policies,
ending at Pyre73; all pure checks and the existing mask/overflow calibrations
also pass. Frozen validation binaries are retained under
.probe-runs/trait-composition-final-checks:Model SHA256
60795214D56C2FD301E5CC28656A69267225FB8FE706E3AB94B6DE78817CFA73,
ModelChecks SHA256
CC85004B4D78B5B2CB01E8AEE68F387893F69B3C66336E914DF5D8052B7237BD.
This calibrates trait composition inputs/outputs, not trait callbacks, permanent
upgrade installation or temporary relic upgrade lifecycle integration. It is
later than the audited frozen188 baseline; a combined regression is separate.

`card-trait-composition-calibration.mt2f` is the byte-identical schema1 native
trait refresh capture described above:31,261 bytes/4,744 nodes, SHA256
d22122720e4f265e8f036d82ed76017e332d7629310340095e5e6124710f9680.
It has no JSON source/companion and is included in the curated binary inventory.

The subsequent card status composition model separately matches1,039 native
ordered queries:15 owned CardState.TryGetStatusEffects results and1,024
controlled original-helper two-stage merges. Groups retain both status ID and
fromPermanentUpgrade, clamp after each unchecked Int32 addition, discard zero
groups after each stage and preserve their resulting order. Purify removes
starting statuses, not upgrades; card queries have no unit9999-stack cap.
Exact source flags, ordered counts and32 immutable parallel replays pass.
The muted Instant capture exits zero in64.56 seconds with complete context,
RNG/frame and original files unchanged. Its independent complete battle passes
21 paid plays/seven EndTurns, Pyre73 and initial/mid-battle branch policies;
all pure checks and mask/overflow/trait calibrations also pass. Frozen binaries
are retained in .probe-runs/card-status-composition-final-checks:Model SHA256
077E8D1778C05A20FB302D3232E151ADD931C0E324C380FC1DAA78661D56F884,
ModelChecks SHA256
5713EEBEA689CCD398FD3E9E89CFD44F345F39E0DE388BF67133888085BFF429.
This models card queries with applyDuality=false; Duality and live unit spawning
remain separately scoped. The full189 trait-version regression does not cover
this later status-source metadata/model change. Branch-derived owned mask views
and complete temporary relic upgrade lifecycles remain unfinished.

`card-status-composition-calibration.mt2f` is the byte-identical schema1 native
status query capture described above:11,026 bytes/3,316 nodes, SHA256
f2c2a6d4da1bb317b68e5778a66f9db8e6a761da4c5744e87033962191ac5582.
It has no JSON source/companion and joins the curated binary regression list.

The subsequent owned-card mask view model derives current cost/size, two-stage
status groups, trait names/parameter effects, visible upgrades, duplicate upgrade
IDs and unit ability/graft flags from immutable branch values. It carries native
lazy trait cache state:own dirty flag, both modifier counters and both saved
counters. Repeated clean queries preserve pending replacement order instead of
forcing another refresh. Native CardTraitData.None placeholders retain null
trait names; their replacement uses the definition's removable flag separately
from the runtime dummy flag. DefinitionRemovable is optional for older captures;
the original None definition supplies its known default when absent.

Exact native acceptance covers416 carried states/views:15 isolated owned copies,
192 controlled unit/spell combinations and a None-definition replacement edge,
each queried twice. All25,792 outcomes across62 unchanged original masks and
permanent-only costs match, with32 immutable branches. The muted Instant capture
exits zero in103.08 seconds with complete context, RNG/frame and original files
unchanged. Its separate21-play/seven-EndTurn battle matches independent initial/
mid-battle policies, Pyre73; all pure checks and the mask/overflow/trait/status
calibrations pass. Frozen checker binaries are retained under
.probe-runs/owned-card-mask-final-checks:Model SHA256
8714E08890A269EA7B6C2E19EEE65B68B124552D235C27B4C3297986DA51C285,
ModelChecks SHA256
32598863275AC0AC62E1BE0A987901F6DEE5EF1AF3808EA20DCC7B71BFF2D672.
This is the branch-query model; the battle adapter still needs to own and update
its descriptor through upgrade/trait/trigger lifecycles. Temporary relic effects
remain refused until that complete integration is verified. The earlier frozen
189/190 regressions do not establish this later model's combined result.

The frozen owned-view complete191 regression now retains terminal exit0 at
2026-10-10T13:57:56.6806960Z. Its audited output verifies all162 battles (158
paid/four no-more-card chains) and29 ordered calibrations, with no failed or
unsupported core transitions. Every input and frozen model/checker hash matches.
This baseline precedes temporary-upgrade lifecycles and branch-owned battle
descriptors; it does not establish either later version's full regression.

`card-owned-mask-calibration.mt2f` retains the schema1 native branch-query
acceptance described above:13,129 bytes/1,709 nodes, SHA256
845dfedbef2552e647dd8c4ee8a7cebcaf85670ea557e160fe4501156d7b0c9f.
The curated archive is byte-identical to native capture with no JSON dependency.

The subsequent temporary-card-upgrade lifecycle model matches212 native
operations with predicted state carried through each sequence:88 operations
using all11 original collectible relic upgrade payloads on unit/spell copies,
112 controlled operations and12 Moon multiplier query/reset operations. Exact
raw post-operation and post-query states match across36 sequences and32 immutable
parallel replays. Replays carry predicted state rather than recorded outcomes.
Coverage includes temporary-only uniqueness and empty IDs, shared upgrade
instances, ID/index removal cache differences, replacement asset/ID ordering,
AvoidClobbering/TraitsModified, tagged trigger installation/removal, discarded
modifier removal retaining triggers, reset/trait-only reset and magic rescaling.
Reset clears both the standby override flag and its pile value toNone. The two
native magic multiplier overrides include Moon's saved float factor with signed
nearest-even rounding; Moon resource callbacks remain separately scoped.

The muted Instant capture exits zero in63.65 seconds with original files,
complete context, gameplay/test RNG and frame unchanged. Its independent complete
battle matches21 paid plays/seven EndTurns, Pyre73 and initial/mid-battle policies.
All pure checks and the existing mask/overflow/trait/status/owned-view calibrations
pass. Frozen validation binaries are in
.probe-runs/card-upgrade-lifecycle-final-checks:Model SHA256
79C4850AE40800EF3D9E55C83318EC12AAEC634AC8725BE21B8168967142A8DD,
ModelChecks SHA256
4998DF3A9041770DE53C296C6BF03FF8ECDEA3257703980C3F727A8A8A83780A.
This models CardState upgrade operations and installed trigger definitions, not
trigger firing, live unit updates or RelicEffectAddTempUpgrade battle integration.
Those effects remain refused. The production battle adapter must still own/update
the query descriptor through ordinary upgrades, clones, new cards and resets,
then implement native relic ordering, applicability and notifications.

The native sampler now retains the state captured immediately after
StopCombatLoop, then waits for the original card animation callbacks before
exporting and exiting. This preserves the battle boundary while later game
cleanup resets Moon/resources/phase. The hand-upgrade validation completes23
paid plays/five EndTurns, Pyre80, with zero pending card movements at export;
the independent complete battle and original decision states match. The terminal
wait does not replace the recorded state with a later post-battle cleanup state.

`card-upgrade-lifecycle-calibration.mt2f` retains the schema1 native lifecycle
acceptance described above:8,370 bytes/1,334 nodes, SHA256
22024931ace427c1aa883c3389f5001f64ed0d605429a944daafd93ea0122552.
The curated binary is byte-identical to native capture without a JSON source or
companion. It joins the regression inventory; the complete192 run is separate
from the preceding frozen191 owned-view version.

`full-battle-incant-relic.mt2f` retains schema108 with the original obtainable
ExtraSpellCastTrigger acquired through SaveManager. The relic asset is unchanged;
authored unit/spell triggers exercise count2 player Incant and count1 enemy Incant,
once/silence/Purify and status children. All21 paid plays, seven EndTurns, complete
contexts/callbacks and initial/mid-battle parallel policies match the native game.

`full-battle-incant-relics-combined.mt2f` adds the original ExtraSpawnTrigger and
ExtraDeathTrigger artifacts. The unchanged production model matches all21 plays,
seven EndTurns, five actual player deaths,160 native fire-count queries and complete
parallel policies. The completed183 run retains its original frozen inventory;
that run cannot establish the subsequent inventory's complete result.

`full-battle-spawn-status-relics.mt2f` retains schema109 with three unchanged
original artifacts:SpawnWithArmor,FirstUnitGainDamageShield and FrostbiteOnEnemies.
All18 plays,six EndTurns,final Pyre80,12 native birth phases and170 exact FIFO
status callbacks pass. Shield triggers once on each of three different turns,
with two same-turn skips. Complete initial/mid-battle policies repeat in16
branches; original birth phases in32. Two native recordings retain identical
complete pre/post states for every one of the18 actions. The schema110 companion
above covers ordinary copied/cardless births. Grafted/aura births and further
relic effects still need native acceptance.

`full-battle-ability-incant.mt2f` retains schema107 with an isolated authored
relic using the original native ability-Incant effect. It is not an obtainable
artifact in the 2.2.1 asset catalog. Under the explicit card-animation settlement
protocol, two native recordings have identical complete decision states; all17
plays, five EndTurns, callbacks and initial/mid-battle parallel policies pass.
The earlier recording with outstanding animation callbacks remains excluded.

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

The schema 97 coverage above proves the direct Horde APIs. Schema 98 adds the
primary Bump card paths described below; broader simulator/search work remains.

### Primary Bump cards (schema 98)

`full-battle-bump.mt2f` records eighteen actual Bump card plays, including
up/down/zero/clamped quantities, an ignored authored integer range, rooting,
immobility priority, looping, simultaneous enemy targets, player Pyre blocking,
enemy Pyre entry, full/partially blocked rooms and both-team cross-room Horde
merges. Complete raw train/context/retained-target states and dispatch payloads
are independently compared in 32 branches. Merged unit cards remain in Standby
without death/Harvest/Rally/re-spawn callbacks.

All 38 paid-card records compare individually, including the eighteen authored
plays marked `ScenarioAction=true`. The subsequent ordinary policy consists of
20 card plays and five EndTurns, wins at Pyre 74, and matches from initial/middle
policy roots in 16 parallel branches. Native recording uses paid Steward hosts,
native observer/filler births and gold callbacks with the original Boss/waves.
It preserves raw effect boundaries and stable decisions after preview refresh.
Later rearrangement plays also verify missing Shift/Sentry reward callbacks.

Native capture is muted and Instant, takes 55.49 seconds and has zero failures,
differences, unsupported/pending records; original save/log signatures match.
The archive has 7,189 graph nodes and is 37,157 bytes. Manifest size/hash and
regression inventory now cover 146 binary archives: 137 battles and nine
calibrations, without any source JSON dependency.

The full 146-archive regression exits zero with all 137 battles and nine
calibrations passing and no unsupported transitions. The audited inventory
retains nineteen physical-position suites, fourteen queued-summon suites, ten
equipment suites, six direct summon-effect suites, eight decision-reference
suites, standalone/two summon revival suites, Horde/Bump suites and all 262
native summon damage phases. All archive sizes and SHA-256 hashes match.

Actual Bump previews, teleportation, disabled/destroyed floors, special Boss
movement/destruction and movement callbacks involving damage/revival still need
coverage, as do ordinary cloning and wider relic/room/card mechanics. The full
simulator and optimal search remain unfinished.

### Ordinary unit clone APIs (schema 99)

`full-battle-unit-clone.mt2f` preserves thirteen native clone operations and 44
full intermediate train/context boundaries. It exercises front/back/selected
placement, excluded permanent/temporary source upgrades and their status
contributions, wounded/buffed/negative actors, non-grafted equipment, natural/
replacement skills and configured cooldowns, clone-of-clone and source-free
births. Null/invalid-room/selected-boundary gates allocate nothing; a full room
allocates the copied card but no unit. Horde cloning allocates neither.

Copied source/equipment references are detached rather than owned pile cards.
Clone flags precede spawn callbacks. Raw observation pauses only background
queue runs while intrinsic birth/gear-upgrade queues execute; the subsequent
explicit drain retains complete callback payloads and order. All states and
queues compare in 32 branches without parent mutation.

The complete subsequent fourteen-play/five-EndTurn policy wins at Pyre 80 and
matches from initial/middle roots in sixteen branches. Native capture is muted
and Instant, takes 64.12 seconds and reports zero failures, differences,
unsupported/pending records, with original save/log signatures intact. The
30,687-byte native binary preserves 4,972 graph nodes. Manifest inventory now
contains 147 archives: 138 battles and nine calibrations, without source JSON.

The complete 147-archive regression exits zero with all 138 battle fixtures and
nine calibrations passing, including 134 independent policy chains. The final
audit checks all required native suites and 262 summon damage phases, with zero
unsupported transitions. Every archive matches its size/SHA-256 manifest, and
the curated clone archive is byte-identical to the native binary capture.

Paid copy-spell integration, enemy/grafted/preview cloning and room/relic or
destruction/revival interactions still require their own coverage. Broader
simulator and optimal-search work remains unfinished.

### Paid unit copy spells (schema 100)

`full-battle-unit-copy.mt2f` preserves fourteen real paid `CardEffectCopyUnits`
effects and all 28 paid card results. Coverage includes signed/ignored-range
counts, targeted and room-wide copies, sequential births, clone-of-clone,
incoming status/ability callbacks, non-grafted equipment, cardless sources,
full/partial room failures, selected-last gates and Horde without a new actor.
The immutable definition catalog resolves each clone from its current actor
and card references. Raw train state, incoming/outgoing queues and all intrinsic
dispatch payloads/order match independently in 32 branches without mutation.

The subsequent fourteen-play/five-EndTurn policy wins at Pyre 80 and reproduces
initial/middle roots in sixteen branches. The first policy turn has no legal
play and is independently recomputed. Every ascent input is empty after combat;
physical checks verify zero cross-room moves, fourteen removals and complete
position states in 32 branches. Native recording uses paid hosts, an ordinary
third-floor clone, native fillers and authored gold callbacks while retaining
the original Boss/waves. Muted Instant capture takes 89.85 seconds; all native
failure/difference/unsupported/pending gates are zero and original files match.

The 35,606-byte binary has 6,511 unique nodes and matches the native archive
exactly. Inventory is 148 archives: 139 battles and nine calibrations. Complete
regression and audit pass, including 135 independent policy chains, all nine
calibrations, 21 physical-position and ten decision-reference suites, and all
262 summon damage phases. All archive sizes/SHA-256 hashes match; unsupported
transitions are zero. Enemy/mixed-team copying is covered below; grafted/preview copying, broader room/relic/
destruction/revival interactions, special Boss mechanics and complete optimal
search remain unfinished.

### Enemy and mixed-team copy spells (schema 101)

`full-battle-hero-copy.mt2f` preserves sixteen real paid copy effects: raw versus
live stats, repeated/clone-of-copy births, incoming status and replacement-skill
queues, optional detached equipment, room targets, last/full/partial physical
groups, Horde and both mixed-mask source teams. Exact Heroes masks create
enemies; mixed masks use monster cloning even from an enemy source. Enemy
births retain natural skills, clear IsSpawning before OnSpawn, drain global
setup/child queues before that phase and enchant only the newborn. Their
counted effect spawn total stays zero; copying Horde stats preserves its
separate Rally. Initial hero compaction is distinct from effect-end compaction.

All raw train states, queued callbacks and nine-field intrinsic dispatches
compare independently in 32 branches. All seventeen paid actions and the
subsequent first-turn loss compare, including a real ordinary Champion birth's
player-wide enchant phase before Rally. Initial and later policy roots reach
the same Pyre-zero result in sixteen branches. Twenty room contexts, twenty-one
cross-room moves and 275 decision-reference mappings pass; no ordinary unit
dies. Pyre death preserves raw occupancy and ordinary death statistics, and
the terminal transition skips new waves without changing phase/cache/RNG.

Native setup uses two paid player hosts, three enemy Shield Steward hosts,
authored skills/gold callbacks/equipment and native fillers. Original Boss
definitions and waves stay intact. Muted Instant capture takes 43.30 seconds;
all failure/difference/unsupported/pending gates are zero and original files
match. The 24,418-byte binary contains 4,755 unique nodes and is byte-identical
to native output, with no source JSON. This verifies the observed primary
copying and loss paths; grafted/substituted/Boss/preview births, broader effects,
room/relic/covenant interactions and birth destruction/revival remain work.

Inventory is now 149 binary archives: 140 battles and nine calibrations.
Complete regression and audit pass, including 136 independent policy chains,
22 physical-position and eleven decision-reference suites, all fourteen
queued-summon, ten triggered-equipment and six direct summon-effect suites,
and all 262 summon damage phases. Sizes/SHA-256 hashes and native/original-file
gates match; errors and unsupported transitions are zero.

### Spawn enchant lifetime and Horde children (schema 102)

`full-battle-spawn-enchant.mt2f` records ordinary one-unit Shield Steward and
repeated two-unit Sword Steward paid births, player-wide Self Horde/armor
enchant and authored spawn/unscaled/not-from-card/status/Rally callbacks.
Original Boss definitions and waves stay intact. Newborn IsSpawning stays true
until enchant and its child queue settle, then clears before paid Rally. Source
card room caches exclude that newborn while enchant effects execute.

Fifteen enchant phases, seven complete births and 66 exact nine-field child
callbacks compare in 32 isolated branches. Native observations include newborn
and older actors, initial and later Horde growth, raw/canonical room caches,
two detached source clones, ten paid Rally team phases and 27 Rally dispatches.
All fifteen paid actions, 38 room stages and five EndTurns match. Independent
initial/mid-battle policy roots win at Pyre 80 in sixteen branches. Seventy-six
room contexts, fifteen removals, eight cross-room moves and 370 reference
mappings verify physical state. The old model fails this archive's first action
because it incorrectly marks the newborn's paid Rally trigger as fired.

Muted Instant capture takes 47.39 seconds, with zero failures/differences/
unsupported/pending records and unchanged original files. The 30,572-byte
binary contains 4,966 unique nodes and exactly matches native output. No source
JSON is retained. Wider enchant effects, grafting, birth destruction/revival,
room/relic/covenant interactions and special Boss mechanics remain unfinished.

Inventory is now 150 binary archives: 141 battles and nine calibrations.
Complete regression and audit pass, including 137 independent policy chains,
23 physical-position and twelve decision-reference suites, all fourteen
queued-summon, ten triggered-equipment and six direct summon-effect suites,
and 262 summon damage phases. All archive sizes/SHA-256 hashes and native/
original-file gates match; errors and unsupported transitions are zero.

`enchantment-lifecycle-calibration.mt2f` is a direct native schema-1 calibration
of persistent CardEffectEnchant at the status API request boundary. It records
1,460 ordered primary/preview-map and two-stream RNG transitions, 644 complete
requests with map snapshots at API entry, a 1,152-case actor gate matrix,
192 signed-count/Duality cases, preview key retention and Setup/binding gates.
Independent output-carrying chains cover 34 initial and 72 random steps;
all cases and chains run in 32 branches without mutating roots. Native status
calls are suppressed for this calibration, so automatic combat updates and
actual status/callback integration remain unfinished and explicitly unsupported.
The native full battle after restoration and its independent seven-turn policy
both win. Muted Instant capture takes 47.92 seconds; capture/native/original-file
gates pass. The binary is byte-identical to native output, 19,551 bytes/4,856
nodes, SHA-256 `2b7bdc7a17da201945b3fd35e3d2cd21ad0edd720825b9d34b6d8d523c76c618`.
Complete regression and audit pass for 151 archives: 141 battles and ten
calibrations, 137 independent policy chains, 23 physical-position and twelve
decision-reference suites, fourteen queued-summon, ten triggered-equipment,
six direct summon-effect suites and 262 damage phases. All sizes/hashes and
native provenance/original-file gates pass; errors and unsupported transitions
are zero. Persistent aura integration into the combat engine remains open.

`enchantment-combat-calibration.mt2f` captures actual status mutations and drained
child callbacks for persistent CardEffectEnchant on two real paid units. No
native gameplay/status API is suppressed. Thirty-two exact steps include global
allow/reentry guards, direct nested global updates, muting/silencing, signed/zero/
Duality-overflow counts, poison and sixteen pooled random steps. All 35 API calls,
48 complete nine-field callback payloads, per-effect maps, train/context states,
explicit UI preview preparation events and drained gold effects match in 32
isolated branches. Live captured train/context, original primary states/triggers/
identity maps, RNG and update flags restore; the queue is empty afterwards.
The isolated profile's run-stat/UI side effects are outside that restoration claim.

The following native and independent ordinary battle win after 21 paid actions,
seven EndTurns, Pyre 73; original profile/log file gates pass. Muted Instant native
capture takes 51.21 seconds. The 9,760-byte/1,776-node archive is byte-identical to
native output, SHA-256 a68ec95cafa6b9fd202b8201a0c5d892f97fd2cce8fa28c673497b87e58600da.
There is no source JSON. Generic combat still rejects persistent CardEffectEnchant
until automatic birth/movement/death/preview hooks and mutating child callbacks
are integrated. This is a real-status update kernel, not full aura battle support.

Inventory is now 152 binary archives: 141 battles and eleven calibrations. Complete
regression/audit pass with 137 policy, 23 physical-position, twelve decision-reference,
fourteen queued-summon, ten triggered-equipment, six direct summon-effect suites
and 262 damage phases. Native provenance, all archive hashes and original-file gates
pass. Final explicit refusal/binding capture fixes build, and both enchantment
calibrations plus unsupported-boundary checks pass independently afterwards.

`enchantment-world-calibration.mt2f` adds 32 real AddStatusEffect/RemoveStatusEffect
calls on the same isolated paid hosts, including silenced/muted, zero/repeated
adds, zero/all removals, ordinary armor/dormant writes, Spark with dormancy,
disabled updates and sixteen consecutive random-pool steps. These calls invoke
native global aura updates automatically; the calibration does not call
UpdateEnchantments itself or suppress gameplay APIs. Complete train/context/aura
state, both RNG streams, 76 callback payloads and drained gold effects match;
24 aura status calls are observed and retained for diagnostics. All cases repeat
in 32 isolated branches, and the random chain carries model outputs forward.
UI preview preparation is recorded explicitly. Shared world serialization and
live/retained identity checks also pass.

The accepted native capture takes 60.38 muted Instant seconds. Its 8,428-byte,
1,470-node binary is byte-identical to native output, SHA-256
`3a2b4af9f7f1c94d3d0b36cee560c0fc5e056a271a054d08ee641daf58132d51`.
There is no text source/JSON companion. Captured live state, bindings/trigger
identities, gold and RNG are restored before the subsequent ordinary paid battle
wins after 21 actions/seven EndTurns, Pyre 73; original-file signature gates pass.
Isolated run-stat/UI effects remain outside the restoration claim. This verifies
automatic control-status updates, not birth/movement/death/preview scheduling or
actor/card-changing children. Generic persistent CardEffectEnchant combat remains
refused until those paths are integrated and verified.

Complete regression and audit pass for 153 archives: 141 battles/twelve
calibrations, 137 policy, 23 physical-position, twelve decision-reference,
fourteen queued-summon, ten triggered-equipment, six direct summon-effect suites
and 262 damage phases. Binary hashes/provenance and native original-file gates
pass. Final shared-train refusal and both real-status/control-status calibrations
also pass independently after the full run.

`enchantment-source-order-calibration.mt2f` records the actual native manager list
collector: 172 ordinary lists across the real status matrix/restored paid battle,
and 32 read-only incoming-list stress cases with lengths 0/1/15/16/17/31/33/64.
All 204 arrays match independently in 32 branches, covering both teams, equal
physical positions across floors, deduplication during room collection and repeated
sorting after empty rooms. Native team values sort enemies before players; same-
team sorting uses physical IndexInRoom, correcting the earlier floor-first account.
Multiple aura-source floors explicitly require captured physical points.

The byte-identical native archive is 2,681 bytes/581 nodes, SHA-256
`b9011555d2a5cd35f410b917166240a64c83d56de74de4766a5e77581bfe5804`,
with no text source/JSON companion. The 74.43-second muted Instant run passes/wins,
has zero errors/unsupported/pending records, preserves original-file signatures
and independently wins the restored ordinary battle in 21 actions/seven turns,
Pyre 73, initial/middle roots and 16 branches. Final control/real-status/lifecycle
suites pass on the corrected collector, including missing-point refusal.

Inventory is 154 archives: 141 battles/thirteen calibrations. All manifest hashes
pass. Regression evidence is the preceding complete 153-archive run/audit plus
the new 204-list suite and final affected aura suites; a second complete
154-archive run was not performed. Generic persistent-aura combat and the full
simulator remain incomplete.

`full-battle-persistent-enchantment.mt2f` keeps paid Steward singleton armor auras
bound throughout a real game 2.2.1 battle, targeting both teams and draining
status-change gold children. The original Boss/waves remain intact. Schema 103
captures the context-owned aura world, preview maps/cache, retained destroyed
targets, physical positions and update guards at every ordinary action boundary.
The muted Instant native run takes 53.17 seconds and wins in 15 actions/five
EndTurns, Pyre 79, with zero differences/failures/unsupported/pending records and
unchanged original profile/log signatures. Independent checks reproduce all
42 room stages, nine train phases, seven spawns, actions/turns, initial/middle
policy roots and 16 branches with unchanged parents. Coverage rejects an ordinary
battle without persistent auras or without native preview/retained-target evidence.

The byte-identical native binary has 26,528 bytes and 4,444 unique nodes, with no
text source/JSON companion. The SHA-256 is in manifest.tsv. Inventory is now 155
archives: 142 battle fixtures and thirteen calibrations. Whole-battle random pools,
source death/revival, actor/card-changing aura children and broader combat effects
remain outside this fixture's proof; the full simulator remains incomplete.

Complete regression and audit pass for all 155 archives, including 138 policy
chains, 24 physical-position and thirteen decision-reference suites. All archive
sizes/hashes, native provenance and original-file gates pass, with no errors or
unsupported transitions and no tracked source JSON fixtures.

`full-battle-persistent-enchantment-deaths.mt2f` adds actual OnTurnBegin source
deaths and OnDeath gold callbacks to the paid singleton armor policy. Two formerly
bound sources withdraw statuses from live targets, retain skipped corpse map
entries and release their bindings when Unity destruction completes. Later UI
previews leave those source caches unchanged. Native and independent simulation
win in 21 actions/seven EndTurns, Pyre 65, with initial/middle roots and sixteen
branches. All 61 room stages, fourteen train phases, eleven spawns and actions/
turns match. Muted Instant native capture takes 63.43 seconds and has zero errors/
differences/unsupported/pending records and unchanged original-file signatures.

The byte-identical native archive has 32,431 bytes / 5,518 nodes and no text source
or JSON companion. Inventory is 156 archives: 143 battles/thirteen calibrations.
All manifest hashes pass. Validation combines the preceding complete 155-archive
run with the new battle, three affected historical battles, all thirteen
calibrations and pure checks; the complete 156-archive run was not repeated.
Aura-source revival and wider aura/combat mechanics remain unverified.

`full-battle-persistent-enchantment-revivals.mt2f` captures two paid bound sources
consuming their first/last Undying stacks in the ordinary OnTurnBegin FIFO before
normal death. Four natural revival APIs, 24 accepted callback payloads and 24
callback phases match independently in sixteen branches, retaining binding and
exact primary/preview maps and cached status. There are no setup operations or
queue deferrals. The full policy also matches from initial/middle roots and
16 branches, with 21 actions/seven EndTurns, final Pyre 68. Native capture takes
64.85 muted Instant seconds, has zero failures/differences/unsupported/pending
records and preserves original profile/log signatures.

The byte-identical native binary has 34,456 bytes / 5,860 nodes, no text source
or JSON companion. Inventory is 157 archives: 144 battles/thirteen calibrations;
all sizes/hashes pass. The new battle, seven affected historical battles, all
thirteen calibrations and pure checks pass. Evidence builds on the preceding
complete 155-archive run and source-death checks; a complete 157-archive run was
not repeated. Whole-battle random pools, changing aura children and broader
combat mechanics still require implementation and native coverage.

`full-battle-persistent-enchantment-random.mt2f` and
`full-battle-persistent-enchantment-random-deaths.mt2f` use the paid native
armor 2 / regen 1 / buff 1 aura pool on both teams. The ordinary scene wins at
Pyre 80 after fifteen actions/five EndTurns in 51.55 native seconds; the source
death scene wins at Pyre 65 after twenty-one actions/seven EndTurns in 64.72 seconds.
Both muted Instant captures have zero differences/failures/unsupported/pending
records and preserve the original profile/log signatures. Each continuous policy
matches initial/middle roots and sixteen independent parallel branches, including
complete primary/preview caches, both RNG streams and retained identities.

Preview RNG isolation includes the post-preview primary room-order update and its
callbacks. All three cached selections, actual preview draws and whole-scope RNG
restoration are mandatory coverage. Spawn/card/rearrangement/turn order updates and
cross-floor treasure cache propagation now follow the native cadence. These two
byte-identical archives have 29,412 bytes / 4,818 nodes and 33,542 bytes / 5,642 nodes,
without text sources or JSON companions. Inventory is 159: 146 battles and thirteen
calibrations. Random source revival remains unverified because native death dissolve
completion can defer Unity destruction; rejected revival/stage captures are not
curated. Broader aura effects and the complete simulator/optimal solver remain work.

The complete 158-archive run (145 battles/thirteen calibrations) exits zero,
including 141 continuous policies. The final rebuilt model passes nine affected
battles, including both new random-pool archives, plus all thirteen calibrations;
no errors or unsupported transitions occur. All 159 current archive sizes and
SHA-256 values match the manifest and regression inventory. These results combine
the complete prior158 run with the new source-death coverage; a single combined
159-archive regression was not repeated. Rejected destruction-stage changes are
excluded from the verified model.

character-removal-calibration.mt2f observes 55 original removal API transitions,
ten actual destructions and twelve manager queues, without changing gameplay or
timing. The independent model checks all three destruction stages, weak-reference
survival until OnDestroy, primary/preview/temporary references, live attacker
cleanup, numeric/status preservation, pending actors excluded from manager queues,
minimum-ten-frame callbacks and 32 immutable parallel branches. It has 4,282 bytes
and 816 nodes, matches the native binary exactly and has no text source.

The source diagnostic battle still has four whole-decision differences and is
not curated. Passing API calibration does not prove automatic whole-battle
removal scheduling. Inventory is now 160 archives: 146 battles/fourteen
calibrations; existing persistent-aura battles and policies pass separately.

All fourteen calibrations and pure checks pass with the rebuilt model. All 160
archive sizes/hashes and regression-list entries pass; a combined 160-archive
regression was not repeated for the independent removal API model.

full-battle-persistent-enchantment-random-revivals.mt2f enables the explicit native
death-dissolve settlement protocol. The original callbacks complete before the
game's existing turn-end removal flush, preserving their duration and ten-frame
minimum. This resolves the late removed enemy's attacker reference without
overwriting native gameplay, queues or RNG. Arbitrary unsynchronized frame timing
remains outside this stable decision protocol.

The scene wins at Pyre 72 after 21 paid plays/seven EndTurns, including four natural
revivals/24 exact callback phases, 60 room stages, fourteen train phases, eleven
spawns and thirteen card cycles. The initial 65.46-second capture and 63.67-second
repeat are muted Instant runs with zero differences/failures/unsupported/pending
capture records and unchanged original profile/log signatures. Each has six
nonterminal flushes, twelve callbacks and four extra awaited frames. Terminal
visual callbacks may outlive StopCombatLoop; they do not trigger a new flush.
Continuous policies match from initial/middle roots and sixteen immutable branches.

The curated native schema104 binary has 35,337 bytes / 5,846 nodes, no text source,
and the manifest hash matches its raw native provenance. Inventory is now 161
archives: 147 battles/fourteen calibrations. The new settled battle and all
fourteen calibrations/pure checks pass with the final rebuild. A combined161 run is
not claimed; the preceding full158 and affected historical battles provide the
baseline. Rejected unsynchronized revival battles/prototypes remain excluded.

full-battle-persistent-enchantment-upgrades.mt2f and
full-battle-persistent-enchantment-random-upgrades.mt2f cover four actors whose
aura status-change callbacks add the same temporary stat/status/trigger upgrade
twice, remove both copies, then add one permanent upgrade with status-change and
OnHit children. Full states prove actor stats, attributed trigger removal,
monotonic trigger IDs, retained zero-stack definitions and source-card lifetimes.
No setup operations run after preparation; fifteen paid plays/five EndTurns win
at Pyre 80 with 39 room stages, nine train phases, seven spawns and nine cycles.
Initial/middle policies and sixteen immutable branches match. The random scene
selects all three aura pool entries and records 58 isolated native previews.

The model now retains completed queued spell deaths in its shared aura world,
then performs the native once-per-removal-batch aura update and drains its child
callbacks through the existing queue. Complete comparisons include destruction
flags and both Battle/BattleTest streams; the previously missing two random draws
at the lethal spell are restored. Successful muted Instant captures take
61.22/56.38 seconds with zero differences/failures/unsupported/pending records and
unchanged original profile/log signatures. Rejected diagnostics are excluded.

The byte-identical native archives have 27,301 bytes/4,541 nodes and
30,314 bytes/4,970 nodes, without text sources or JSON companions. Inventory is
163 archives: 149 battles/fourteen calibrations. New and historical aura policies,
all fourteen calibrations and pure checks pass with the rebuilt model; the
complete 163-archive regression exits zero with 149 battle suites, fourteen
calibrations and 145 continuous paid policy chains. All inventory sizes/hashes,
byte-identical native provenance, PowerShell syntax and whitespace checks pass.

Four enchantment-summon[-fresh|-random|-random-fresh]-calibration.mt2f archives
observe original queued SpawnMonster applications whose new units carry persistent
auras. Each contains two batches/four births/51 full callback payloads and matches
complete room/world/source-card/physical/RNG state in 32 immutable branches. The
copied variants additionally compare four detached source-card clones each; fresh
variants use the matching child fallback definition. The model now performs the
original final room-order aura update after a triggered summon batch, before
queued OnSpawn callbacks drain.

These are component calibrations with WholeBattleVerified=false, not accepted
new full-battle policies. Preview-only births retained by aura dictionaries still
expose incomplete actor retention, source-cache restoration, synthetic identities
and preview physical references. Rejected diagnostic battles are not curated.
The four binaries have 7,258/6,772/7,721/7,124 bytes and 1,140/1,027/1,168/1,049
nodes respectively, match native bytes exactly and have no JSON companions or
text sources. Inventory is now 167: 149 battles and eighteen calibrations.

Validation includes all 22 historical triggered-summon/persistent-aura battles,
their continuous policies, all fourteen historical calibrations/pure checks and
the four new curated calibrations/pure checks. All 167 hashes/sizes, regression
entries and PowerShell syntax pass. A combined167 run is not claimed; the prior
complete163 run remains the baseline. The failed new whole-battle diagnostics
are excluded.

preview-reference-calibration.mt2f observes nineteen original native combat-preview
actor restorations and one temporary birth before EnableCombatPreviews. Independent
checks compare complete states, the cleared preview-overwritten first-born cache,
a preserved real-unit cache, primary once flags and aura dictionaries retaining
the temporary target. Thirty-two immutable parallel branches pass. The native
birth's primary origin has attack 8/HP 30 and no applied stat upgrades or statuses;
its temporary point is room 0/index 2 and original removal reaches stage 3.

The model now restores summon and aura effect state in the same shared world,
preventing a later refresh from reintroducing the old summon cache. The archive is
a byte-identical, source-free native binary with 3,787 bytes/580 nodes. It explicitly
keeps FullPreviewSimulationVerified=false: complete preview-born actor retention,
physical position planes and identity advancement remain open, and rejected
whole-battle diagnostics are excluded. Inventory is now 168 archives: 149 battles
and nineteen calibrations.

The rebuilt model passes thirty affected historical battle suites, 28 continuous
paid policy chains, all nineteen calibrations and pure checks. All 168 inventory
hashes/sizes and regression entries match, and the new binary equals its raw
native source. Probe has zero build warnings/errors; the checker keeps fourteen
existing nullable warnings with no errors. A full168 run is not claimed; the
earlier complete163 regression remains the full baseline.

Four preview-birth[-fresh|-random|-random-fresh]-calibration.mt2f archives observe
the original lifecycle of 9/9/18/17 preview-only aura summons. Independent checks
initialize each primary origin from its character/card/ability inputs, then
restore from the observed preview actor and compare complete original On and
OnDestroy states in 32 immutable branches. Native attack 8/HP 30 restores from
preview attack 9/HP 32; preview upgrades/statuses vanish, shared endless immunity
and aura maps remain, and destruction bypasses ordinary death/listener flags.

The model retains these primary origins internally, carries referenced newborns,
their detached source cards and observed identity counters through automatic aura
previews, and restores card room caches against the surviving primary actors.
The observer assigns birth/source identities before subsequent room captures.
Raw temporary position planes and a late removed actor's attacker reference still
prevent acceptance of the new whole battles. The latest fixed diagnostic chain
matches nine paid actions and two EndTurns before that remaining difference.

All four lifecycle captures have zero errors and WholeBattleVerified=false;
their entire rejected battle traces are excluded. The muted Instant native runs
preserve original profile/log signatures. Native binary copies are byte-identical,
have no text source or JSON companions, and measure 3,193/3,151/4,067/4,068 bytes
with 396/396/572/566 nodes. Inventory is 172: 149 accepted battles/23 calibrations.

The final build passes thirty affected historical battles, 28 continuous paid
policy chains, the earlier nineteen calibrations/pure checks and four new
lifecycle calibrations. All 172 manifest hashes/sizes and regression entries
match. Probe builds without warnings/errors; ModelChecks keeps fourteen existing
nullable warnings and no errors. A combined172 run is not claimed; the previous
complete163 regression remains the full baseline.

physical-plane-calibration.mt2f captures 100 original SetSpawnPoint assignments and
40 original copied-list compactions. Complete before/after worlds match in 32
immutable branches, including ten distinct copied lists, cross-plane ownership,
preview-born detachment, retained point pointers and native pivot counts. The model
keys positions by room/team/PreviewCopyId/index; null denotes the primary layout.
The observer preserves historical copied points and all three actor-state planes.
Copied-slot actor closure is observed before freezing inputs, correcting a rejected
prototype whose last copied group retained actor 27 outside its unit inventory.

This byte-identical native binary has 4,032 bytes/825 nodes, no text source or JSON
companion, zero incomplete/differing samples and WholeBattleVerified=false. Inventory
is 173: 149 accepted battles and 24 component calibrations. The final muted Instant
native run preserves original profile/log signatures, removes all ten former
physical capture failures and independently verifies 370 raw/canonical decisions
with 11,835 primary/preview/temporary mappings. Original dissolve settlement resolves
the third-turn attacker gap; terminal aura/preview/cache differences still reject
the full battle, which is excluded from the accepted inventory.

All 31 affected historical battles/29 paid policies, 23 earlier calibrations/pure
checks, the new calibration, binary provenance, all 173 manifest entries and script
syntax pass. A combined173 regression is not claimed; complete163 remains the full
baseline.

The latest temporary Boss-preview model preserves newborn preview states, shared
card-room targets and last-spawned references, skips primary spawn statistics in
preview, and cancels retained preview births when terminal ClearCards runs.
An opt-in schema105 card-animation settlement protocol waits for original
temporary-pool movements and discard completion before previews/policy input.
It changes preview start timing; arbitrary unmodified frame timing remains open.

Two new diagnostic battles independently match fifteen paid actions, five EndTurns
and complete canonical terminal states, including sixteen immutable parallel
branches. The latest muted Instant run takes 162.27 seconds, preserves original
profile/log signatures, and has zero capture failures/unsupported effects/pending
records. Its 370 raw/canonical decision mappings and 11,785 position-plane mappings
pass. Three intermediate copied-group/callback-boundary differences still reject
full-battle verification, so these traces remain outside the curated inventory.

The final model passes 32 affected historical battles, 30 continuous paid policies,
all 24 calibrations and pure checks. Inventory remains 173 binary archives:
149 accepted battles and 24 component calibrations. A combined173 regression was
not repeated; complete163 remains the full regression baseline.

`full-battle-persistent-enchantment-summons.mt2f` now accepts the complete battle
under schema105 and explicit original death/card callback settlement. All fifteen
paid actions, five EndTurns, 41 room stages, nine train phases and seven spawns
match independently, including the terminal Pyre80 state, a mid-battle root and
sixteen immutable parallel branches. Four nested aura births and six bound
sources retain detached source cards, shared weak targets, native copied-list
allocation counters and selected/primary position planes. All 154 Before/Actual
contexts have complete registries and counters; 370 complete raw/canonical
decisions and 11,785 position-plane mappings pass.

The final muted Instant native recording takes 162.55 seconds and has zero
differences, capture failures, unsupported paths or pending records; original
profile/log signatures remain unchanged. The archive is a byte-identical native
binary, has no text source or fixture JSON companion, and contains 39,816 bytes
and 7,342 unique nodes. Card metadata records 24 original movements scheduled and
none pending. This acceptance applies to the explicitly settled protocol;
arbitrary unmodified frame timing and broader combat mechanics remain open.

`full-battle-persistent-enchantment-summons-fresh.mt2f` independently covers the
fresh-source variant of the same aura battle. Four nested child births create
new source cards instead of copying the previous source's runtime state. All
fifteen paid actions, five EndTurns, 41 room stages, 154 closed contexts and the
complete initial/mid-battle policies match, with sixteen immutable branches and
Pyre80. The muted Instant native run takes 181.73 seconds, has zero capture
failures/differences/unsupported/pending records and preserves original profile
signatures. Its byte-identical source-free native binary is 39,470 bytes with
7,242 nodes.

The completed historical baseline checks every one of its 173 archives:
149 complete battle suites, 145 continuous paid policies, four complete
no-more-card chains and all 24 calibrations. No room, card-cycle, train, spawn,
EndTurn or action operation is skipped as unsupported. This run uses Model.dll
SHA-256 59153b6d1959e90bd39ee4de0ba729863a628c616f8a0cd6408ee1900e8f2ffd;
the later retained-preview callback model is being verified separately.

`full-battle-persistent-enchantment-random-summons.mt2f` accepts schema105 with
three randomly selected aura statuses, four nested child births, six bound
sources and retained preview actors outside the selected room list. Local aura
callbacks use the current retained state; temporary Boss previews preserve
complete states for actors outside their restore list, including withdrawn
statuses and once flags. All fifteen paid actions, five EndTurns, 41 room stages,
nine train phases, seven spawns and 154 closed contexts match independently.
The complete initial/mid-battle policies match Pyre80 in sixteen branches;
370 raw/canonical decisions and 14,385 position-plane mappings also pass.

The muted Instant native run takes 211.59 seconds, completes all 23 scheduled
original card-movement callbacks, has zero capture failures, differences,
unsupported operations or pending records, and preserves original profile/log
signatures. Its byte-identical source-free binary is 47,789 bytes with 8,710
nodes. The final model also passes all 58 affected historical archives:
34 battle suites, 32 paid policies, two no-more-card chains and 24 calibrations.
This is an explicitly settled recording; broader native timing and combat
coverage remain work. The three previously rejected random diagnostics are
outside the curated inventory.

`full-battle-persistent-enchantment-random-summons-fresh.mt2f` independently covers
random aura statuses with four fresh child source cards and no source copies.
The current model preserves explicit retained callbacks while restricting
ordinary attacks, team/unit/post-combat callbacks, status decay and relentless
continuation to actual room members. All 154 closed contexts, fifteen paid
actions, five EndTurns and complete intermediate/terminal states match, including
the initial/mid-battle policies and sixteen immutable parallel branches ending
with Pyre80. All 370 raw/canonical decisions and 14,385 position-plane mappings
also pass.

The muted Instant native run takes 218.97 seconds, has zero capture failures,
differences, unsupported operations or pending records, completes all 23 original
card-movement callbacks and preserves original profile/log signatures. Its
byte-identical source-free binary is 47,591 bytes with 8,608 unique nodes. The
current model separately passes 59 affected archives: 35 complete battles,
33 paid policies, two no-more-card chains and all 24 calibrations. The complete
battle simulator and optimal solver remain in progress.

The completed room-membership baseline now passes all 177 binary archives:
153 complete battle suites, 149 continuous paid policies, four complete
no-more-card chains and all 24 component calibrations. Each complete policy
checks independent initial/mid-battle roots and sixteen parallel branches.
No recorded room/card-cycle/train/spawn/EndTurn/action operation is unsupported
or skipped, and all manifest sizes and SHA-256 hashes are verified. The isolated
runner uses Model.dll SHA-256
d46c6961737888c3b84bfb883c5cdbfd85741536ea3dee8400b6cb91c6a68cbb;
the new Purify work is being validated separately.

`full-battle-purify.mt2f` records ordered starting statuses, active Purify blocking
positive/zero/negative additions, zero-stack clearing, explicit removal and
reapplication, native trigger admission and six real paid room spells. Accepted
callbacks survive later purification, while newly forbidden status/combat
callbacks are absent from the queue. All 159 FIFO dispatches and eighteen status
boundaries match independently, including exact dictionary/zero definitions,
source statistics and eight explicit removal effects.

The muted Instant native battle takes 86.30 seconds and preserves the original
Boss/waves and profile/log signatures, with zero capture failures, differences,
unsupported operations or pending records. All 61 room stages, 21 paid actions,
seven EndTurns, 228 closed contexts and complete initial/mid-battle policies match
in sixteen immutable branches ending with Pyre65. Its source-free binary is
42,703 bytes with 7,162 nodes, copied byte-identically from native capture.
The final Purify model separately passes 27 historical battles and all 24
calibrations; the completed177 baseline remains tied to the earlier model.

The -PurifyQueues native probe extends the Purify scene with ordinary birth,
AfterSpawnEnchant, Rally, Sentry, Harvest and death rewards. The final muted
Instant battle takes 81.00 seconds with zero differences, capture failures,
unsupported operations or pending records. Complete independent replay passes
21 paid actions, seven EndTurns, 61 room stages, 228 closed contexts, both policies
and sixteen parallel branches ending with Pyre65. All 759 character-overload queue
requests match captured rules and original queue-count deltas, including purified
birth/Rally/Sentry/Harvest/death rejection. No queue-data overload is observed.

Removal entries preserve physical death/spawner cleanup while gating the actual
OnDeath request at removal time. Accepted batches survive later purification and
branch-local retained actor updates. Pure tests and two deliberately broken models
verify these admission/death boundaries. Final Model.dll SHA-256 is
fb6341d949d6b98027472cf3735bd5dda0ed9540351b22221b0cf9d61c432a3f;
its final regression passes 29 complete battles, 28 continuous paid-policy chains,
one no-more-card chain and all 24 curated component calibrations. The incidental new removal
calibration lacks its required nonempty pending-dissolve queue observation and is
not curated; existing calibration gates remain strict. Horde/aura births with
Purify and the complete simulator remain open.

The status-Purify version has completed all 178 required archives: 154 accepted
battles, 150 continuous paid-policy chains, four no-more-card chains and all 24
component calibrations. No recorded room/card-cycle/train/spawn/EndTurn/action
operation is unsupported or skipped. All input identities, archive sizes and
manifest SHA-256 hashes pass. This complete178 baseline uses Model.dll SHA-256
e546dcf00cbda02d12431a4289be8b24b864a0f8cc7062bbaa7180530854ff0b;
the newer removal/admission version is validated separately.

full-battle-purify-queues.mt2f is a byte-identical native archive: 49,314 bytes,
8,697 nodes and SHA-256
1b852c9c28642bbbc254cee356c143e8112ae3d1336a023e6b81665a83f6fc20, without a text source.
The manifest and required list contain 179 archives: 155 accepted battles and 24
component calibrations. The complete178 baseline belongs to the preceding model;
the new queue/removal model's combined179 regression has now completed: 155
accepted battles, 151 continuous paid-policy chains, four no-more-card chains and
all 24 strict component calibrations, with no unsupported or skipped native
operation. All 179 required identities, archive sizes and manifest SHA-256 hashes
are independently rechecked. This baseline uses Model.dll SHA-256
fb6341d949d6b98027472cf3735bd5dda0ed9540351b22221b0cf9d61c432a3f;
the subsequent Incant changes require separate validation.

The Incant model captures native card types and ability flags, retains original
room actor identities, admits/drains players before enemies, and supports empty
and summon-effect spells through the ordinary CardSpellPlayed queue. Equipment,
monster and default unit/room ability classifications are checked in the pure
suite; the native scene records paid ordinary spells, natural units, status
children, silence removal and exact Purify admission.

The final muted Instant Incant recording takes 87.29 seconds with original profile
and log signatures preserved. It independently matches 32 team phases, 18 actor
dispatches, 51 original admission requests with 12 Purify rejections, eleven paid
empty-effect spells, 139 FIFO status callbacks and 23 removal-effect boundaries.
Thirty-two independent branches retain their parents. Two deliberately incorrect
current-source models fail on native empty-spell routing and cached actor scope.
The same model also passes 29 historical battles, 28 paid policies, one no-more-card
chain and all 24 strict component calibrations (53 unique inputs). Model.dll SHA-256:
e26db6f690855eadc4adc90ec5dd2feaf5570de1a1a9695ab826195e5f5b5cea.
The preceding complete179 baseline is not a combined regression of this Incant
version. That version refused positive Incant thresholds; relic-triggered ability Incant and the whole
simulator remain unfinished.

`full-battle-incant.mt2f` is the byte-identical final native Incant recording:
49,010 bytes, 8,274 unique nodes and no text-source provenance. SHA-256:
01812c137f392da5b24408df6999705c26b45d2b243cb871784713c1e5409d4c.
The required list and manifest now contain 180 archives: 156 accepted battles and
24 strict component calibrations. The completed179 baseline belongs to the prior
Purify queue model; the Incant model's combined180 regression is pending.

The threshold Incant version now accepts positive CardSpellPlayed thresholds while
preserving the native default-zero card-play argument. Its -IncantThresholds
recording observes a skipped positive threshold, fired zero/negative thresholds
and spent negative-once callbacks. The muted Instant native battle takes 97.95
seconds, with the original Boss/waves and profile/log signatures preserved and no
capture failure, mismatch, unsupported operation or pending record.

All 21 paid plays/seven EndTurns, 32 team phases, 18 actor dispatches, 51 native
admissions with 12 Purify rejections, eleven paid empty spells, 139 FIFO status
callbacks and 23 explicit removals match independently. Thirty-two parallel
component branches retain their parents; sixteen complete branches end at
Pyre61. An intentionally incorrect current-source model loses the positive
observer's untouched HasTriggered flag. Equality/above-threshold arguments and
zero/negative repeat batches are covered by pure/source-derived checks, not by
this ordinary-card native recording, whose argument/count are always zero/one.

The final threshold model also passes 30 historical battles, 29 paid-policy chains,
one no-more-card chain and all 24 strict component calibrations (54 unique inputs).
Model.dll SHA-256:
ab9e8f6fe6b86fb4d2b66075621d6c3719c385616f51363dbcc798020528db96.
The earlier combined180 run belongs to the initial Incant model e26db6f6. Further
relic/ability/card/trigger combinations and the complete simulator remain open.

`full-battle-incant-thresholds.mt2f` is the byte-identical final native recording:
49,221 bytes, 8,328 unique nodes, no text-source provenance and SHA-256
8cb07e36205d0f19db5a5f1fdc40cab8489b082eb12eb956762dae396f31d13f.
The manifest and required list now contain 181 archives: 157 accepted battles and
24 strict component calibrations. The threshold model's combined181 regression is
pending; the earlier combined180 run remains bound to the initial Incant model.


### Complete initial Incant regression baseline

The isolated initial Incant model's combined180 run has completed successfully:
156 accepted complete battles, 152 paid policies, four no-more-card chains and
24 strict component calibrations. Every recorded room, card-cycle, train, spawn,
turn and paid action is modeled, with zero unsupported or skipped operations.
The 180 explicit input identities are unique; their archive sizes and SHA-256
hashes match the manifest. Native calibration output includes all 24 required
suites, including the four aura-summon and four preview-birth variants.

This baseline belongs to Model.dll SHA-256
E26DB6F690855EADC4ADC90EC5DD2FEAF5570DE1A1A9695AB826195E5F5B5CEA,
retained in .probe-runs/incant-verified-checks. Evidence is the terminal-zero
.probe-runs/incant-complete-regression.log and its explicit
.probe-runs/incant-complete-inputs.txt inventory. The subsequent threshold
model's combined181 run remains separate and running; ongoing relic work is
not covered by this initial Incant baseline. The whole battle model remains
incomplete for the explicitly refused mechanics.


### Complete Incant threshold regression baseline

The isolated threshold model's combined181 run has completed with terminal exit
zero: 157 accepted complete battles, 153 paid policies, four no-more-card chains
and 24 strict component calibrations. No recorded room, card-cycle, train, spawn,
turn or paid action is unsupported or skipped. All 181 unique required archive
identities, sizes and SHA-256 hashes match the complete manifest. The final
calibration output includes all 24 required suites.

This baseline belongs to Model.dll SHA-256
AB9E8F6FE6B86FB4D2B66075621D6C3719C385616F51363DBCC798020528DB96,
retained in .probe-runs/incant-threshold-final-checks. Evidence is
.probe-runs/incant-threshold-complete-regression.log and its explicit
.probe-runs/incant-threshold-complete-inputs.txt inventory. The initial Incant
model's completed180 baseline remains separately bound to e26db6f6. Ongoing
relic/combined-prefix work requires its own final-binary native and historical
validation; neither completed baseline establishes that new model or completion
of the whole battle simulation.


### Relic-enabled ability Incant and combined card-play queues

The model now retains ordered active relic identities and runtime effect types,
including the complete list searched by RelicManager.GetRelicEffect: hero
blessings, collected artifacts, covenants, mutators, Pyre artifacts and souls.
All production context copies preserve this immutable list. Only the native
RelicEffectIncantTriggeredByUnitAbilities marker is modeled here; other active
effect types are explicitly refused by the model and captures. Duplicate marker
instances remain a boolean presence query, without multiplying Incant.

Unit skills cache the selected room after PreOwn and before effects, keep their
resolving flag and paid-cost queries through both card-play manager phases,
and queue Own then Incant per actor in creation order. The entire player batch
is admitted before any callback runs; enemies are admitted after the player
batch drains. Existing accepted effect callbacks stay at the head of this same
batch. Ordinary spells and Rally also retain those prefixes; draining them
separately can change Purify admission, status children and trigger flags.

The game 2.2.1 asset catalog contains no collectable relic using this effect
class. The -AbilityIncant scenario therefore creates an isolated test artifact
PojuNativeAbilityIncantRelic (id c2f6ed7f-18ce-4070-b65f-7dd9f5160068), registers
it through the native SaveManager.AddRelic pipeline and runs the original native
effect class. Its setup builds on the actual shared-skill activation scene,
including that scene's modified Boss ability, and preserves the natural wave
pattern. It adds Incant observers to owned Stewards and naturally scheduled
ordinary enemies. This is an authored test asset, not a naturally obtainable
artifact in this game build.

The final muted Instant native recording takes 106.41 seconds and retains schema
106: 17 plays, five EndTurns, two actual activators sharing one skill, 20 manager
phases, 25 Incant dispatches, 242 status callbacks and 780 original queue-admission
requests. Final Pyre is 80. Native capture reports zero failures, discrepancies,
unsupported stages or pending records, and original profile/log files are
unchanged. An ordinary cooldown spell exposes 34 accepted prefix callbacks.
Independent exact action/component checks and 32 phase/dispatch branches pass.

Evidence is .probe-runs/ability-incant-native-final.log and
.probe-runs/full-battle-units-spells-and-junk-20261009-170051-2fe13390/full-battle.mt2f.
The --ability-incant-only check passes against that recording in
.probe-runs/ability-incant-component-final.log. Pure checks cover absent,
duplicate and unknown relics, missing card classification, post-PreOwn lethal
cache admission, skill allocation, per-actor Own/Incant order, accepted prefixes,
Purify and parent isolation. Source-mutation negatives that ignore the marker
or omit the prefix fail on native gold or trigger flags respectively.

The final model SHA-256 is
F418B74A91DF12254ECBA6A1EF2A9AC3EA88FAA11D84DD5ED687590CB984B7FF,
retained in .probe-runs/ability-incant-final-checks. Its 37-input targeted
historical run has completed: 13 affected battles/paid policies and all 24 strict
calibrations, with archive identities, sizes and hashes verified. Evidence is
.probe-runs/ability-incant-prefix-historical.log and its explicit input list.
The separately completed 181-input threshold baseline belongs to ab9e8f6f;
it does not establish a complete regression of this relic model.

The new recording is NOT accepted into the curated inventory yet. The complete
independent policy fails after action 2 because native floor selection performs
an extra interface preview between plays: previous action's next copied-list
id is 41, next native action starts at 49 and finishes at 57, while the continuous
model finishes at 49. All isolated plays and EndTurns match, but the missing
selection transition prevents claiming complete replay from the initial root.
The original full-policy and physical-reference comparisons remain strict;
no oracle state or recorded actions are injected into the model to hide this
difference. Room-selection preview modeling, further relic effects, unmodeled
card/trigger/effect combinations and the whole battle simulation remain work.
