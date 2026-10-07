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
