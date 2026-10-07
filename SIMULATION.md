# Full battle simulation

Fixed native regression inputs live in `tests/fixtures/` and are read by
`scripts/Check-Models.ps1`. Historical `results/` paths below refer to local
investigation logs, catalogs and benchmarks; that output directory is ignored.

The objective is an independent simulator for a complete Monster Train 2 battle
from a captured player decision state. The objective is still in progress.
Running the native game to a terminal result is an oracle, not proof that the
independent simulator can finish a battle.

## Current implementation and evidence

The pure `src/Model` library has no Unity or game assembly dependency. Its inputs
are copied immutable values; independent child states can run on worker threads.

| Component | Implementation | Verification |
| --- | --- | --- |
| Initial simple unit play | `SimpleUnitPlayModel` | Native Steward summon, turn zero only |
| Unit and no-target card actions | `BattleActionModel` | Eleven native plays across decision turns and rooms, selected placement, capacity and self-purge |
| Targeted spell actions | `CardSpellModel` | Native damage, last-target follow-ups, valor, pyregel and floor rearrange; independent immunity checks |
| Room spell targeting and effect tests | `CardTargetModel` and `CardSpellModel` | Native multi-unit damage/upgrades/healing, front/back/weakest, sticky last groups and strongest-last; empty targets, mandatory casting checks and runtime cancellation |
| Random room targets | `CardTargetModel` and `UnityRng` | Native enemy/both-team/friendly selection, zero heals, empty follow-ups, last-target identity and complete Battle RNG states |
| Random status application | `CardSpellModel` | Native effect-wide status pools, per-target chances in reverse order, immunity, empty pools and post-kill RNG |
| Random spell quantities | `CardEffectRange` and `CardSpellModel` | Native per-effect quantities, damage casting/runtime test draws, upgraded endpoints, status pool/chance ordering and complete RNG state, with explicit UI RNG isolation |
| Train spell execution | `CardSpellModel` | A single effect chain carries all rooms, shared card/statistic/RNG state and global target references; dead-unit movement and spawner routing are updated across rooms |
| Cross-room targeting | `CardTargetModel` and `CardSpellModel` | Native tower/front/above/global HP/random selections, exact live target IDs, deferred death positions and status/death focus changes |
| Target filtering | `CardTargetFilters` and `CardTargetModel` | Native health, required/excluded status, subtype and boss masks before selection; drop/last/physical-front bypasses, subtype precedence, filtered random legality and exact live target lists |
| Integer RNG and shuffle | `UnityRng` | 768 native integer draws, seed initialization and complete four-word states |
| Basic draw/discard cycle | `CardCycleModel` | 13 consecutive native operations including reshuffle |
| Spell draws and hand cycling | `CardCycleModel.DrawCards` and `CardSpellModel` | Signed/zero/max counts, full-hand cast timing, resolving-card exclusion, reshuffle RNG, draw statistics and live membership for later hand upgrades; ranged tests require explicit UI RNG isolation |
| Spell hand discard and consumption | `HandRemovalModel` and `CardSpellModel` | Forward order, resolving-card exclusion, retained buffer aliases, discard-only upgrade removal, double exhaustion statistics, per-effect counters and nested dead-spawner returns |
| Modified card generation | `CardGenerationModel`, `CardSpellModel` and `CombatTrigger` | Five destinations, pool/full-hand/duplicate timing, initial upgrades, modifier copies/exclusions, one-shot upgrades and fresh card effect state |
| Room attack exchange | `RoomCombatModel.Exchange` | Ordered initiative, target selection, retargeting, shield/armor and retaliation checks |
| Entire room resolution | `RoomCombatModel.Resolve` | Native normal exchanges, post-combat effects and multiple rounds of boss/Pyre relentless combat |
| Train combat phase | `TrainCombatModel.ResolveCombat` | Top-to-bottom native phase comparison |
| Enemy movement phase | `TrainCombatModel.Ascend` | Native movement and immediate Pyre combat, including the terminal boss fight |
| Unit effects | `CombatTrigger` and `CombatContext` | Generated cards, Battle RNG, treasure escape; gold and once-only trigger checks |
| Gold rewards | `GoldRewardModel` | 2,200 native calculations, reward minimums, integer/float boundaries, ties to even and preview exclusion |
| Card statistics and preview | `BattleStatistics` and `BattlePreviewModel` | Native per-card/Any counters, turn rollover, spawn subtypes, death/exhaust attribution and preview damage statistic |
| Statistic queries | `StatisticQueryModel` | 17,604 native queries across 40 kinds, three durations and seven card types; membership refresh, type/subtype pile counts, aggregates, costs and resource snapshots |
| Statistic-driven damage traits | `DamageScalingModel` and `RoomCombatModel` | 63 native callbacks with complete refreshed contexts; trait order, replacement/addition, explicit-source upgrades, per-hit statistics and Boss/Pyre kill previews |
| Dynamic statistic inputs | `CombatContext.QueryFrame` and turn/card transitions | 255 native damage callbacks and 48 decision boundaries; payment, combat, rollover, previews and settled terminal resources, plus multi-turn and parallel branches |
| Card instance modifiers | `CardModifierModel` | Permanent/temporary ordered numeric upgrades, unit starting statuses, discard removal, play history and 256 native scalar calculations |
| Retained card references | `CombatContext.CardRegistry` | Observed card identities survive pile clearing; detached spawner upgrades/removal preserve ownership and parent isolation |
| Standby dictionary allocation | `CardPileModel` | Captured entry slots and free-list order preserve native hole reuse after unit death; malformed layouts, distinct futures, terminal clear and parallel branches |
| Shared secondary card piles | `CombatContext.OtherPiles` | Room and spell resolution carry standby/exhausted/eaten/purged/buffer state, immediate and deferred spawner returns, terminal clearing and immutable parallel branches |
| Runtime unit upgrades | `UnitModifierModel` | Native permanent, battle and unit-death lifetimes, duplicate removal, unique upgrades, restricted size, unhealed health and lethal max-health loss |
| Hand upgrade spells | `HandUpgradeModel` | Native targeted and targetless sequences, current-hand membership, permanent/temporary groups, uniqueness and paid-card exclusion |
| Basic healing | `HealingModel` and `CardSpellModel` | Native targeted spells, modifier group clamps, maximum health, multiplier/immunity, regen and lifesteal; independent healability checks |
| Attack buff/debuff spells | `UnitAttackModel` and `CardSpellModel` | Native raw negative balances/recovery, zero-attack and incapable targets, global/random targets, source-card ownership and later unit upgrades |
| Maximum-health buff/debuff spells | `UnitHealthModel` and `CardSpellModel` | Signed temporary source-card offsets, battle/unit-death lifetimes, multiplier/immunity, suppressed OnHeal, direct lethal loss and post-boss effect chains |
| Healing triggers | `CombatTrigger` and `RoomCombatModel` | Native repeated/once rewards and silence; zero/full/immune healing, deployment timing, preview flags and child isolation checks |
| Healing effects on unit triggers | `CombatEffect.Action`, `HealingModel` and `RoomCombatModel` | Native self/room/healable/random targets, per-group ranges, negative/zero amounts, empty-room sampling, source-card independence and deferred OnHeal upgrades |
| Damage effects on unit triggers | `CombatEffect.Action` and `RoomCombatModel` | Native quantity tests/samples, source-card attribution, status multipliers, defenses, death FIFO and deferred spawner exhaustion |
| Unit post-combat healing | `RoomCombatModel.ApplyUnitPostCombat` and `UnitHealerModel` | Native per-actor healing/ordinary order, attack/trigger prevention, once/silence, changing healer quantities and complete phase/decision states |
| Terminal spell resolution | `CardSpellModel` and `BattleActionModel` | Settled native boss kill continues live effects, detached spawner upgrades/removal and healing; effect gates skip/cancel, then played/discard callbacks complete |
| Enemy waves and treasure | `EnemySpawningModel` | Native group cache, spawn order, phase, slots and treasure floor selection |
| EndTurn to next decision | `BattleTurnModel.EndTurn` | Seven native consecutive transitions in each supported fixture |
| Battle to terminal result | `BattleSimulator.Resolve` and `ResolveNoMoreCards` | Independent card policies and seven-turn chains, mid-battle inputs and 16 parallel branches |
| Additional spells, abilities, relics | Not complete | Unsupported interactions explicitly reject the model transition |

`CardEffectAddBattleCard` uses one shared immutable generation model for spells
and unit triggers. Card pools and recursively reachable card definitions are
captured at the starting decision; the model selects each card using its own
Battle stream. It does not read a native execution's later selections. Generation
uses at least one attempt even for zero or negative counts, and ignores integer
range fields. Pool selection occurs before full-hand and duplicate refusals;
refused attempts consume selection RNG but allocate no identity or upgrade.
An empty filtered pool consumes neither selection nor placement RNG.

Hand and discard additions prepend, deck-bottom additions prepend to the draw
pile, and deck-top additions append. Random deck insertion uses the native
exclusive upper bound of the existing deck count. Native identities are registered
when a card is created, so observing a reversed pile cannot reverse the assigned
identities. Generated hand cards do not increment ordinary draw statistics.

New card instances initialize their own starting upgrades, play history and
effect counters. Optional upgrades precede source modifier copying. Copying
replaces raw modifier offsets, merges upgrade lists, excludes clone-disabled
upgrades, suppresses duplicate starting upgrades when copying the same card
definition, and can omit temporary modifiers. Unit triggers copy from their
spawner card. Discard generation also applies matching and unconditional
source-dependent balance upgrades before copying. A pending next-added upgrade
is applied only to the first successfully created card; an effect-chain boundary
clears any remaining pending upgrades even when the chain creates no card.
Casting reads the original hand for required-space tests, and runtime reads the
hand after removal of the resolving card. Generation is gated during preview and
after boss death. Unknown setup traits, upgrade internals and external callbacks
reject the transition instead of returning a partial child.

Schema 20 captures pending generated-card upgrades and complete generation
rules for both spells and unit triggers. Two byte-preserving compressed native
oracles retain controlled battles with a full initial hand, starting and optional
upgrades, excluded source upgrades, copied offsets and newly initialized effect
counters. Natural enemy waves remain intact. The ordinary fixture adds a
once-only Steward post-combat generator; the lethal fixture adds conditional
discard upgrades and a lethal spell followed by a gated generation effect.

| Native oracle | Exact generation effects | Created cards | Full card plays | EndTurns | Final Pyre |
| --- | ---: | ---: | ---: | ---: | ---: |
| `full-battle-generation.json.gz` | 97, including 3 unit triggers | 112 | 17 | 5 | 80 |
| `full-battle-generation-lethal.json.gz` | 91, including 22 matching discard upgrades | 118 | 15 | 4 | 80 |

The ordinary fixture also verifies 13 refused attempts, 13 empty pools, 42
modifier-copy operations, one pending-upgrade consumption and 42 fresh card
effect states. The lethal fixture verifies 10 refusals, 11 empty pools, 43 copy
operations, one pending-upgrade consumption and 46 fresh card effect states.
All five destinations occur in both. Their 623,324,864-byte and 622,795,487-byte
raw native traces match the archives byte for byte after decompression.
Independent simulations from the starting decision, a mid-battle suffix and
16 parallel branches match the complete settled native result. Original profile
files are unchanged, and capture failures, differences, unsupported transitions
and pending records are zero. Pure checks additionally exercise conditional
and absent-source copies, unused ranges, preview/post-boss gates, incomplete
instance-state rejection, effect-chain cleanup and 32 parallel branches.

Legacy captures omit the new pending-upgrade field and richer generation rules;
they retain their original plain-generation behavior. Additional creation traits,
Infusion/Crafted Spike merging, grafting, magic-power scaling and relic/room
generation callbacks remain unsupported. These controlled fixtures do not verify
every card-generation effect in the game.

`CardEffectDraw` runs through the same full card-action pipeline as other spells.
Casting tests read the original hand; queued effects read the hand after the paid
card has been removed. Fixed positive counts draw up to the actual hand limit,
zero and other negative counts draw nothing, and `-1` fills to the maximum hand
size. The native method's starting-hand description does not match that maximum
size calculation. A draw-only spell may still pass its test with an empty deck.

Spell drawing preserves the pending start-of-turn draw modifier. Deck selection
pops from the end and prepends to the hand, excludes the resolving card, and only
reshuffles when the draw pile is empty. Native selection occurs before the final
full-hand refusal, so a reserved resolving-card slot can still cause a reshuffle
without adding a card. Each successful draw updates per-card and aggregate draw
statistics; subsequent hand upgrades use the newly changed membership. These
operations preserve card modifiers, ownership, registry references and parent
states, and advance only the appropriate CardDraw stream.

Ranged drawing samples Battle during its initial test, runtime test and application.
Quantity testing precedes the post-boss and preview gates; a gated draw does not apply or draw
cards. Its original UI highlight tests also consume Battle, so full simulation
requires the same explicit UI RNG isolation as ranged damage. Draw-type filters,
draw-or-generate effects, next-drawn upgrades, Magnetic/IgnoreDraw traits and other
draw callbacks remain unsupported rather than being treated as ordinary draws.

`tests/fixtures/full-battle-drawing-ui-isolated.json.gz` retains the unaltered
274,339,566-byte native trace from a controlled starting battle with a full hand,
an ensured drawing spell, one additional deployment energy and a pending draw
modifier of two. Natural enemy waves remain intact. All 24 card plays, seven
EndTurns, 55 room stages, 82 card cycles, 13 train phases and 11 spawns match.
Its 69 spell draw operations add 50 cards and include 14 zero-count no-ops,
13 negative-count no-ops, 21 full-hand refusals, four reshuffles and two actual
draws that preserve the nonzero pending modifier. All 267 native range samples
match quantity and complete RNG state; exactly ten initial/runtime/application
sequences are observed. The explicit UI guard preserves 1,009 original query
results and restores 323 Battle-consuming queries. Independent root-only play,
a mid-battle suffix and 16 parallel full simulations reach native victory at
Pyre 80. The original user profile is unchanged; capture failures, differences,
unsupported transitions and pending records are all zero.

`CardEffectDiscardHand` also uses the full action pipeline. Its integer parameter
selects discard or consume; range bounds and the range multiplier are unused.
It snapshots the hand in forward order and excludes the resolving card. Empty
hands and unknown modes remove nothing, reset this effect instance's consumed
counter to zero and do not drain the death queue. Preview and post-boss gates
prevent application. Separate immutable counters preserve each effect index
across repeated plays and source-card modifier changes.

A spell-triggered ordinary discard increments TimesDiscarded and removes only
temporary upgrades marked RemoveOnDiscard. It retains play history and adds a
buffer reference alongside the actual discard pile membership. That reference
survives draws, consumption and EndTurns; a later natural play removes its own
reference. The buffer is therefore an alias list, and other pile memberships
remain unique. Captures and validation retain both the aliases and the complete
native standby dictionary entry slots and free-list order.

Consumption keeps those temporary upgrades and increments TimesExhausted twice,
once on entering standby and once on returning to exhausted, while its effect
counter increments only once per consumed card. OnExhausted character callbacks
can drain a previously finished death before the consuming card's standby entry
is removed. This recursive return changes exhausted ordering and the free list,
even when the final standby pile is empty. The full spell engine defers a damage
victim's exhaustion statistic until the relevant standby return and carries its
updated shared context into the next target. Empty consumption preserves pending
death references until another operation drains the queue.

Removal callback definitions include future wave-generated cards in the initial
rules. Unknown discard/exhaust card triggers, Salvage, special discard routing,
grafted trait state, relics and room modifiers remain explicit unsupported
interactions. The new fixtures use fixed damage and ordinary UI queries; their
unused discard ranges require no UI RNG isolation.

`tests/fixtures/full-battle-hand-removal.json.gz` preserves the unaltered 113,820,105-byte
native trace. All 11 plays, six EndTurns, 45 room stages, 41 card cycles, 11 train
phases and nine spawns match. Its 36 removal effects include 14 discards,
11 consumed cards, 13 empty consumes, six ignored modes, five discarded upgrade
removals, five consumed upgrade retentions and one transient dictionary extension.
Independent root-only, mid-battle and 16 parallel simulations reach victory at
Pyre 80.

`tests/fixtures/full-battle-hand-removal-lethal.json.gz` preserves the unaltered
162,998,905-byte trace with friendly damage immediately followed by consumption.
All ten plays, seven EndTurns, 55 room stages, 48 card cycles, 14 train phases and
11 spawns match. Its 49 effects include two discards, 17 consumed cards,
20 empty consumes, seven ignored modes, two removed upgrades, eight retained
upgrades, one dictionary extension and one dead-spawner return inside a consume
callback. The same independent, mid-battle and 16 parallel checks reach native
victory at Pyre 49. Both captures finish with zero failures, differences,
unsupported transitions or pending records and leave the original profile
unchanged. These controlled starting-battle fixtures add mechanism coverage;
they do not establish support for every card or encounter.

The game oracle is build 2.2.1, Assembly-CSharp MVID
`8fb07b96-f4db-4d2b-884d-c00536d6ccf4`.

Gameplay RNG snapshots exclude native `BattleTest`, as well as `Chatter` and
`NonDeterministic`. `TargetHelper` uses `BattleTest` for legality/UI preview target
selection and `Battle` when live effects apply. `GameEffectHelper.TestEffect`
restores the temporary target list after testing. Numeric testing has a separate
rule: `CardEffectDamage.TestEffect` and `CardEffectDraw.TestEffect` call `GetIntInRange`, which consumes `Battle`
outside `SaveManager.PreviewMode`. In particular, `CardUI.IsPlayableAndAffectsState`
calls those tests while refreshing hand highlights without enabling preview mode.
The number of queries during card draw depends on UI animation/frame scheduling.

Solver experiments can opt into `MT2_PROBE_ISOLATE_UI_RNG=1`. The Harmony guard
executes the original highlight query, preserves its return value and exceptions,
then restores the complete state and seed of both `Battle` and `BattleTest` in
a finalizer. It does not wrap actual `PlayAnyCard` tests or effect execution.
This changes the native UI's RNG side effects; it is an explicit experiment mode,
not evidence of unchanged vanilla RNG behavior. Schema 16 carries `UiRngIsolated`
through every decision state so the model can distinguish the two environments.
The guard is disabled by default.

`tests/fixtures/ui-rng-isolation-calibration.json.gz` preserves 185 native query records,
including 53 that advance Battle inside the original UI method. Every query
restores all four words and the seed of both streams, and returns an original
boolean result. This fixture tests successful queries; no forced exception was
introduced. Auxiliary target/UI draws are excluded from independent gameplay
state; legacy snapshots retain their recorded test stream for compatibility.

`CardEffectRange` captures raw integer bounds and a single-precision multiplier.
Damage and healing upgrade each endpoint independently, with the permanent and
temporary groups' separate floors, before sampling. The result is the native
float product rounded down; equal bounds consume no RNG, while reversed bounds
retain Unity's descending convention. Nonfinite multipliers and out-of-domain
float-to-int conversions are rejected explicitly.

Ranged damage and drawing consume a quantity draw during each enabled initial casting test,
another during its runtime test, and another when it applies. Runtime tests draw
before a post-boss gate or missing-drop-target failure. The initial casting test
returns its child RNG without mutating the parent, and the full card action passes
that child RNG into execution. Other supported quantity effects sample once after
live target selection, sharing the result across their complete collection. Status
effects choose the shared pool entry before sampling their shared chance, then
draw per-target chances in reverse target order; a sampled zero skips those chance
draws. Valid empty collections still sample quantities. Signed attack/health
effects retain their native no-op or spawner-offset behavior, negative healing
does nothing, and negative applied damage is clamped before target defenses.

`tests/fixtures/full-battle-numeric-ranges-ui-isolated.json.gz` preserves the unaltered
81,893,428-byte native trace from an explicitly UI-isolated experiment. All 15
plays, five EndTurns, 40 room stages, nine card cycles, nine train phases and seven
spawns match. It contains 320 native quantity samples, including 77 equal bounds,
78 signed values, 302 fractional multipliers and 266 upgraded endpoints. The
independent policy reaches victory at Pyre 80, including a mid-battle suffix and
16 parallel full simulations.

`tests/fixtures/full-battle-numeric-ranges-lethal-ui-isolated.json.gz` preserves the
unaltered 67,858,469-byte native trace with a global boss-killing ranged effect.
All 12 plays, four EndTurns, 30 room stages, eight card cycles, eight train phases
and seven spawns match. Its 400 quantity samples include 14 applications after a
global lethal damage effect; two occur after the boss's death, covering empty
status collections and retained-spawner health offsets. The same independent/mid-battle/parallel
checks reach native victory at Pyre 80. Both original user profiles remain
unchanged, and both native runs have zero capture failures, differences,
unsupported transitions and pending records.

The full battle model rejects ranged damage/draw definitions when `UiRngIsolated` is
false: the captured logical decision state cannot reproduce vanilla hand animation
and frame-dependent highlight RNG consumption. These fixtures prove the explicit
solver environment with the UI guard, rather than complete vanilla UI timing.
The suite now contains 28 full battle fixtures; these controlled starting-battle
variations do not establish complete coverage of the game's other encounters.

Schema 17 carries optional `CombatUnit.IsBoss`, copied from native `IsAnyBoss`,
separately from `EndsBattleOnDeath`. Spawn definitions, unit modifications,
combat snapshots, room movement and card copies preserve it. Older oracles lack
this metadata; boss-filtering modes reject unknown identities instead of
inferring them from a unit's terminal death behavior.

`CardTargetFilters` captures damaged/undamaged health, ANDed required statuses,
excluded statuses, required/excluded subtypes and boss exclusion. Normal room,
tower, random and HP/position selectors filter the candidate pool before selecting.
Room-and-above fronts select the first filtered candidate per room and team.
Physical fronts in all rooms bypass these masks, as do direct last-target modes;
strongest-last-room instead filters candidates in the first remembered room.
Drop overrides apply subtype/team/boss checks while bypassing health/status masks.
A required subtype short-circuits excluded subtypes in normal collection; drop
overrides check both lists. Random attack legality inspects the filtered pool,
and an empty filtered pool consumes no gameplay target draw.

`tests/fixtures/full-battle-target-filters.json.gz` retains an unaltered native trace with
13 spells and 103 exact live effect target collections, captured after drag/drop
override and before effect application. It verifies five drop bypasses, seven
physical-front bypasses, 12 last-target bypasses, seven required-subtype precedence
cases, two boss exclusions and two empty random pools. All 18 plays, six EndTurns,
52 room stages, 11 card cycles, 11 train phases and nine spawns match native
victory at Pyre 80. Root-only policy execution, a mid-battle suffix and 16 parallel
simulations also match. Native capture failures, differences, unsupported and
pending records are zero, and the original user profile is unchanged. Pure checks
add contradictory masks, unknown boss identities, subtype ordering, filtered
incapable attackers, parent isolation and 32 parallel branches. Relic-driven
all-subtype rules, equipment conditions and additional target modes remain
unsupported; these checks do not prove those interactions.

`tests/fixtures/full-battle-steward-once.json` records a seven-decision-turn natural
`Level1BattleJunker` battle, starting with one Steward play. The native game won
with Pyre health 49/80. The independent model matches all 55 room stages, 13
card cycling operations, 14 train phases, 11 wave spawning operations, and seven
EndTurns. Some matched stages are empty rooms; the fixture also includes armor,
spikes, unit death, card generation, treasure escape, and the terminal Pyre boss
fight. There are zero unsupported transitions in this fixture.

`tests/fixtures/full-battle-no-cards.json` is a no-card control with 52 room stages and
the same counts for the other phases. It also reaches native victory with Pyre
health 49/80, with zero differences or unsupported transitions.

The full-chain check supplies the simulator only the first EndTurn input. Every
following decision state and the stopping point come from the independent model;
it never injects later native states, recorded predictions, or a native terminal
turn count. The checker then compares all model decisions and terminal state to
the oracle, and repeats each complete simulation in 16 parallel branches. The Steward fixture
starts after its initial summon has completed; later actions are all EndTurn.

`tests/fixtures/full-battle-units-and-junk.json` adds 11 card plays and 61 room stages.
The policy summons multiple units on different floors, inserts at selected player
positions, and plays generated self-purging junk. It reaches victory at Pyre 60/80
with zero unsupported stages or differences. Its independent chain starts before
the first play and selects every following action from its own state, without
injecting recorded actions or later native states. Every action and EndTurn is
compared to the oracle, with 16 parallel runs and a separate mid-battle root.

`tests/fixtures/full-battle-units-spells-and-junk.json` adds targeted spells to the policy.
All 21 plays, 60 room stages, 13 card-cycle operations, 14 train phases, 11 spawns
and seven EndTurns match the game with zero unsupported stages. The independent
policy reaches native victory at Pyre 73/80, including complete intermediate
comparisons, a mid-battle suffix and 16 parallel branches. It applies the two
starting spells' complete effect sequences: damage followed by pyregel, and
moving the target to the front followed by valor. A killed last target receives
no follow-up status. Valor grants attack immediately and replenishes front armor
after the whole room resolves, once per room resolution; deployment skips the
armor effect. Unit death card routing follows actual combat event order.

`tests/fixtures/full-battle-statistics.json.gz` repeats that policy with schema 5 and
the native card-statistics state included in every room, train, spawn, action
and EndTurn comparison. All 21 plays, 60 room stages, 13 card cycles, 14 train
phases, 11 spawns and seven EndTurns match, with zero unsupported transitions.
The root-only independent policy, mid-battle suffix and 16 parallel branches
also match every decision and the terminal Pyre 73/80. The gzip contains the
unaltered native trace; the checker reads compressed and ordinary JSON fixtures.

Statistics include per-card values for ThisTurn, PreviousTurn and ThisBattle,
ordered played-card history, live played-cost entries, floor/subtype spawn
counts, player deaths, end-turn energy, starting gold and last attack damage.
Capture reads private dictionaries without calling the native statistic getters
that insert zero entries or refresh deck membership. Zero-valued dictionary
entries are canonicalized away; tracked membership includes current owned cards
because the game refreshes this membership before every statistic operation.
Generated cards join that membership and therefore receive later Any counters.

Two native details matter for matching this installed build. Mapped Any counters
increment every tracked card once and the event's source card a second time,
regardless of the original increment amount. UI battle previews also write
LastAttackDamageDealt even though they leave death/exhaust counters alone. The
model simulates preview rooms on independent copies, transfers only that
statistic, and retains the gameplay board, resources, card piles and RNG. The
native probe now waits for PreviewMode to end and pending battle preview changes
to clear before capturing a decision or choosing its next action. Earlier gates
could sample immediately after a spell, while that statistic was still stale.

`tests/fixtures/full-battle-numeric-upgrades.json.gz` adds schema 6 card instance state.
An isolated native scenario gives two otherwise identical Stewards different
permanent and temporary upgrades, and changes one damage spell. Permanent scalar
offsets remain distinct from ordered upgrade lists; temporary upgrades marked
RemoveOnDiscard disappear on play or hand discard, while temporary offsets
persist. Spawned units retain the stats and armor/spikes they received before
the source card's temporary upgrade was removed. Every card retains its own
last played cost, forge history and scenario play count across piles.

The native policy wins at Pyre 80/80 after 19 plays and six EndTurns. All 51 room
stages, 11 card cycles, 11 train phases and nine spawns match, with zero capture
failures, unsupported stages or differences. The independent root-only policy,
mid-battle suffix and 16 parallel branches reproduce the full state including
card statistics and modifiers. The compressed fixture preserves the native
trace bytes unchanged.

`tests/fixtures/card-modifier-calibration.json.gz` contains 256 native calculations
across all eight scalar fields and both floor modes. The native calculation
applies offsets before upgrade lists, immediately clamps additions whose
magnitude is at least 99, and clamps once more at the end. Unit stats and cost
combine the permanent and temporary groups in one calculation; spell damage
clamps each group separately. The model preserves these differences. Trait,
trigger and ability upgrades, merged/boxed cards, grafted equipment, persistent
health remain explicitly unsupported. Initial numeric card upgrades and runtime
unit upgrades use different native calculations; the runtime model additionally
handles unhealed health and attack buffs.
Generated cards with starting upgrades are rejected until their initialization
is modeled.

`tests/fixtures/full-battle-dynamic-upgrades.json.gz` changes the runtime effect list of
the owned floor-rearranging spell inside an isolated native process. Its effects
compose permanent upgrades, repeated temporary upgrades and removal, a unique
upgrade, until-death unhealed health, and room-capacity restrictions before the
original rearrange/valor effects. The starting Boss and waves remain native.
All 18 plays, five EndTurns, 44 room stages, ten card cycles, ten train phases and
nine spawns match, finishing at Pyre 80/80. Four plays actually execute the modified
spell; coverage requires both a play and observed native upgrade state.

`tests/fixtures/full-battle-sacrifice-upgrades.json.gz` adds a max-health reduction that
kills the selected friendly unit. Five modified-spell plays exercise death
triggers and source-card routing. All 21 plays, seven EndTurns, 53 room stages,
13 card cycles, 14 train phases and 11 spawns match, finishing at Pyre 56/80.
Both fixtures pass independent root-only simulation, a mid-battle suffix and
16 parallel branches. Compressed artifacts preserve the original trace bytes.

Unit upgrade lifetimes determine whether the source card receives a permanent
upgrade, a temporary battle upgrade, or no upgrade. Removing repeated upgrades
uses the native template's inverse values and removes all matching temporary
source entries. Raw size is preserved behind the displayed 1..6 clamp. Capacity
rejection leaves both unit and source card unchanged; removal remains legal in
a full room. Positive max-health upgrades heal, unhealed-health upgrades only
increase the maximum, and removing a health upgrade clips current health to the
new maximum. Unique and clone-exclusion rules, attack buffs and parallel parent
isolation also have focused checks. Statistic-driven scaling upgrade instances,
their shared identity, and nonempty max-health upgrade maps remain unsupported.

The dynamic fixture finishes when a damage spell kills the Boss. Kill-camera
setup clears runtime cards. The resolving spell still completes its played and
discard callbacks before the combat loop stops: its card instance returns to
discard, its play/discard counters advance and its live played cost is cleared.
Statistics first refresh from the permanent deck, then from the restored owned
card. The model requires captured permanent deck membership for this sequence.

`tests/fixtures/full-battle-hand-upgrades.json.gz` modifies the owned floor-rearranging
spell into a targetless hand-upgrade spell. `tests/fixtures/full-battle-targeted-hand-upgrades.json.gz`
retains its original unit-target effects, applies the same hand upgrades, and
then damages the unit occupying the original drop point. Each native fixture
actually plays the modified spell seven times and matches all 23 plays, five
EndTurns, 43 room stages, ten card cycles, ten train phases and nine spawns,
finishing at Pyre 80/80. Both also pass independent root-only policies,
mid-battle suffixes and 16 parallel branches. The fixture Boss and waves are native;
only the isolated player spell and numeric starting upgrades are modified.

Direct play removes the paid card from the hand before queued effects execute.
Hand upgrades therefore affect the remaining hand, including cards with
unsupported play effects when their upgrade callbacks are modeled. Draw,
discard and the resolving card retain their existing upgrades. Unique upgrades
are checked separately within permanent and temporary groups. Newly applied
temporary hand upgrades last until battle end; preexisting RemoveOnDiscard
upgrades still use their captured discard lifecycle. Upgrade filters and unknown
trait callbacks reject the transition. The model re-reads source-card modifiers
for each subsequent effect, preserving already paid cost.

Repeated DropTargetCharacter effects refer to the originally selected spawn
point, which can have a different occupant after rearrangement. Each drop effect
also replaces the LastTargetedCharacters collection; those follow-ups retain
the selected unit identity and skip a dead unit. The mixed native fixture
exercises a rearrange followed by a drop effect hitting the new occupant.
Room and LastTargeted group sequences are covered by the room-spell fixture below.

`BattleActionModel` checks instance identity, energy, enabled floors, Pyre exclusion,
summon capacity and slots, and insertion position. Child states carry spawned
unit/card relationships and all piles into EndTurn. Illegal plays and unsupported
rules have different rejection codes and neither returns a partial child state.
`EnumerateSupportedPlays` enumerates implemented actions; callers must not treat
this list as a certificate that every game action has been covered.

The comparable decision state includes unit identities, health, attack, statuses
and trigger state, unit size and status immunities in newer fixtures; enemy movement and wave cache; every captured gameplay RNG
stream except Chatter and NonDeterministic; hand/draw/discard/standby/exhausted/
purged/eaten/buffer piles; energy, gold, forge points, hoard, and moon phase.
Schema 5 additionally captures unit subtypes, card statistics and whether battle
previews are enabled. Older fixtures omit statistics and do not verify them.
Schema 6 also captures every owned card's permanent/temporary modifiers
and instance play history. Schema 7 adds unit upgrade ledgers, raw size, equipment
limit, healability and source-definition matching. Schema 8 captures permanent
deck membership for terminal card statistics. Older fixtures do not verify
fields absent from their schema. Schema 9 additionally captures separate card
upgrade-callback interactions in play definitions. Schema 10 also includes
deployment restrictions for each unit trigger. Legacy inputs without that
metadata cannot model OnHeal. Schema 11 captures effect testing and cancellation
flags. Schema 12 samples terminal state after `StopCombatLoop` has finished and
records an assertion that `cardEffectResolving` is false. A
despawn counter <= 1 is canonicalized to 1 because each value despawns at the
next application. Native UI previews can decrement that private counter below
zero. Longer despawn countdowns are rejected by the native definition capture
until preview effects are modeled.

`results/rule-catalog.json` exports wave candidates, enemy/unit definitions, and
starter/generated card effects, traits and triggers without selecting future
groups. Capture checks that the gameplay RNG streams remain unchanged. The model
inputs embed the resolved wave candidates and rules needed by these fixtures.
Newer inputs also include card play definitions, room capacity/spawn-slot rules,
and armor/valor/pyregel status definitions. `relentless` grants rooted immunity;
live immunity state is preserved when copying, summoning and moving units.

`tests/fixtures/rng-calibration.json` captures six seeds and both RNG values and states,
including signed bounds, the full signed integer span, equal bounds, and reversed
bounds. Sampling restored Unity's process RNG and did not advance the game's
HadesRNG streams. Integer compatibility is verified for this installed build;
floating-point draws are not implemented yet.

`tests/fixtures/gold-reward-calibration.json.gz` captures 2,200 calls to the native
gold adjustment function. It covers reward and ordinary balance changes, negative
and zero values, four rounding increments and single-precision boundaries.
For a supported battle's standard rules, positive rewards are floored through
single precision, rounded to the nearest multiple of five with ties to even,
and raised to at least five. Ordinary balance changes are unchanged. Preview
effects leave gold unchanged. Unit effect values retain their unadjusted reward;
only generated-card effects retain a pile destination. Relic and mutator reward
modifiers remain explicitly unsupported.

`tests/fixtures/full-battle-healing.json.gz` adds a controlled healing scenario to the
same natural first battle. The isolated process appends healing and unhealed
maximum-health effects to the owned rearrangement spell and assigns different
healing statuses to two Stewards. The native healing state classes exist in this
build; missing multiplier/immunity definitions are registered only in the
fixture's in-memory database, with multiplier parameter 2. Boss and wave rules
remain those of the original battle.

All 18 card plays (including seven healing spell plays), six EndTurns, 53 room
stages, 11 card cycles, 11 train phases and nine spawns match the native game.
Checks require observed health restoration and multiplier, immunity, regen and
lifesteal coverage. The same root-only policy, mid-battle suffix and 16 parallel
branches are checked. Basic healing clips at maximum health, uses the multiplier
parameter independently of stacks, and respects healability. Maximum-health
upgrade healing bypasses immunity but still respects the multiplier and
healability. Overheal and specialized healing effects remain unsupported.

`tests/fixtures/full-battle-healing-triggers.json.gz` adds repeated, once-only and
ignored-silence OnHeal gold rewards to the same fixture. One Steward has both
healing immunity and silence. All 18 card plays, six EndTurns, 53 room stages,
11 card cycles, 11 train phases and nine spawns match the native game. Coverage
checks require three blocked heals to award 15 gold, a consumed once-only trigger,
and an ignored-silence trigger firing while ordinary triggers remain blocked.
The root-only policy, mid-battle suffix and 16 parallel branches match all
intermediate decisions and the terminal state.

Eligible healing fires OnHeal even at full health or when immunity reduces the
amount to zero. Unhealable units and negative modified healing skip it;
maximum-health upgrade healing deliberately does not fire it. Trigger state
preserves once-only, silence, fire count and deployment restrictions. Each native
preview resets its own once-only flags independently of the real state; the
model resets these only on preview copies. Pure checks also cover generated-card
identity/RNG and despawn follow-up targeting through the existing effect engine.
Conditional thresholds, equipment requirements, purification and trigger removal
on relentless changes remain explicit unsupported interactions.

`tests/fixtures/full-battle-room-spells.json.gz` replaces the owned rearrangement spell's
effects only in the isolated process. Boss and wave definitions stay native.
The sequence combines enemy room damage, friendly room upgrades, enemy last-target
statuses, strongest-last/back/last damage and friendly room/front/weakest healing.
The verification policy prefers rooms with targets for an area spell. Coverage
requires actual damage to multiple enemies and maximum-health upgrades/healing
of multiple friendly units in a single play; a definition alone is insufficient.
All 18 plays, five EndTurns, 48 room stages, 10 card cycles, 10 train phases and
nine spawns match the game. Eight area spell plays include eight multi-unit
heals, one multi-enemy damage play and five surviving enemy follow-up collections.
The independent root policy, mid-battle suffix and 16 parallel branches match
every decision and terminal Pyre health 80/80. This oracle was recaptured after
post-kill continuation expanded the verification policy's legal action choices.

Same-room collections process enemies before players, preserving each team's
front-to-back order and the first target on health ties. Spells can target stealth
but exclude untouchable units. Healing collections include full-health units if
healable. The first effect records the last-target collection; subsequent ordinary
room selectors preserve it, and a new drop selector replaces it. Last-target
follow-ups remove dead units and filter teams. Strongest-last ignores team filters
and retains dead references with zero HP, which are skipped when applying effects.

Native effect-test metadata is captured with schema 11. Casting tests inspect the
unchanged pre-cast state: any tested success permits casting unless a mandatory
effect fails. Runtime tests run again after preceding effects, and a configured
failure can stop the rest of the sequence. Unknown target modes, additional target
filters and untested-first effects requiring uncaptured prior
target history remain unsupported. Rearrangement still requires a drop target.

Legacy oracles sampled the entry to native `StopCombat`, before its
`StopCombatLoop` waited for the resolving card effect. A controlled room-spell
run exposed an intermediate terminal snapshot with only part of a post-kill
damage effect executed. New captures wait for that loop to stop, before
encounter-complete rewards and out-of-battle cleanup. Terminal spells continue
their fixed current target collection and subsequent effects, retaining the
terminal outcome. Each subsequent effect is tested again, including its native
`CanPlayAfterBossDead` flag: hand upgrades cannot run, and their configured test
failure may skip that effect or cancel the rest of the sequence. Live source
card modifiers remain available after ownership is cleared. Damage, healing and
unit upgrades process targets forwards; status addition uses native reverse order.
Terminal spell destinations other than discard remain explicitly unsupported.

`tests/fixtures/full-battle-terminal-spells.json.gz` changes the owned rearrangement
spell to one lethal enemy-room damage effect in the isolated process. Native
boss and wave definitions remain intact. It verifies a spell-driven victory
after the resolving effect and played/discard callbacks finish. Its completed
terminal state includes the spell's restored instance and discard membership,
updated statistics and cleared played-cost entry. The dynamic and two hand
upgrade oracles are recaptured at this same settled boundary; their prior
entry-point terminal snapshots are retained in Git history.
The lethal-spell fixture matches all 13 plays, four EndTurns, 30 room stages,
eight card cycles, eight train phases and seven spawns. The independent root
policy, mid-battle suffix and 16 parallel branches reach the same terminal Pyre
health 80/80 and complete callback state.
Focused checks also cover a generated spell absent from the fallback permanent
deck: its played history remains, while its missing played-statistic entry cannot
increment; discard creates a new entry for the restored card. This generated
terminal branch has pure checks and native source evidence, but no dedicated
native fixture yet.

Schema 13 captures an identity registry of all cards observed in the battle.
It is separate from `CardInstances`, which describes current native ownership.
The registry retains the exact modifier and play state of cards removed by
`ClearCards`; living units can still reference and upgrade their spawner cards.
Owned-card updates and newly generated cards update the registry, while lookup
and upgrade of a detached spawner never add it to a pile or ownership list.
New captures also include the native all-bosses-dead gate and each card effect's
permission to execute after that gate is set. Missing legacy fields remain null.

`tests/fixtures/full-battle-post-kill-spells.json.gz` replaces the owned rearrangement
spell with a controlled effect chain, retaining the natural boss and wave data.
The final play kills the boss at the back, damages the remaining room targets,
kills the guard, adds and consumes armor, applies a permanent source-card upgrade,
adds/removes a battle upgrade and heals both surviving friendly units. Their
health changes from 41/41 and 26/26 to 45/45 and 30/30; their unowned spawner
cards retain the permanent upgrades in the registry. A prohibited hand effect
skips without cancellation, followed by one that cancels a labeled tail upgrade.
The native oracle matches 13 plays, four EndTurns, 30 room stages, eight card
cycles, eight train phases and seven spawns. Root-only policy, a mid-battle suffix
and 16 parallel branches finish with exactly the same complete terminal state.
Pure checks additionally cover a boss killed before remaining targets in one
damage effect, prohibited generation from boss death and post-kill OnHeal,
allowed post-kill gold rewards, and fallback-deck statistic membership when
the source is a generated card. These trigger cases lack a dedicated native
post-kill fixture.

`tests/fixtures/full-battle-random-spells.json.gz` replaces the owned rearrangement spell
with enemy, mixed-team and friendly `RandomInRoom` selections, a zero heal,
status addition and last-target follow-ups. It ends with lethal enemy room damage
and an empty random enemy collection. All 13 plays, four EndTurns, 30 room stages,
eight card cycles, eight train phases and seven spawns match native execution,
including complete gameplay RNG states. Seven random spell plays include three
rooms with multiple enemy candidates and seven empty follow-ups; empty selection
does not consume a live draw. A single candidate still consumes one draw.
The independent root policy, mid-battle suffix and 16 parallel branches finish
with exactly the same terminal state and Pyre health 80/80.

Supported casting/runtime tests depend on whether a target exists. They inspect
random collections without consuming Battle RNG; only an effect that passes its
runtime test performs live random selection. First-effect last-target history is
replaced with the actual chosen unit. A mandatory last-target team test after a
mixed-team random first effect can depend on the auxiliary test stream; that
case remains explicitly unsupported. A skipped first random effect that leaves
test-selected last-target history for later effects also rejects the transition.
Repeated legality tests and 32 parallel spell branches preserve their parent.

`tests/fixtures/full-battle-random-status.json.gz` changes the owned rearrangement spell
to controlled armor/regen/pyregel pools and probability parameters 0, 50 and 100.
One owned Steward has a permanent immune status; boss and wave data remain native.
The sequence kills remaining enemies, then performs an empty status-pool selection
and a post-kill friendly chance effect. All 13 plays, four EndTurns, 30 room stages,
eight card cycles, eight train phases and seven spawns match the game. Seven
modified spell plays include five live immune targets. Exact gameplay RNG states,
statistics, status outcomes, root-only/mid-battle policies and 16 parallel branches
match, with final Pyre health 80/80.

Native status application chooses one entry for the whole effect, even with no
targets. Multiple pool entries consume one Battle draw; one entry consumes none.
A zero probability parameter applies unconditionally with no chance draws;
every nonzero parameter consumes one 0..99 draw per target in reverse collection
order, including immune targets and a guaranteed 100-percent effect. Status
selection follows random target selection and precedes chance draws. Pure checks
cover hit/miss ordering, shared pool choice, empty collections, composed draws
and 32 isolated parallel branches. Nonstackable status casting rules, subtype
conditions, range/scaling parameters and additional status triggers remain
explicitly unsupported.

`tests/fixtures/full-battle-cross-room-spells.json.gz` and
`tests/fixtures/full-battle-cross-room-targets.json.gz` replace the owned rearrangement
spell with controlled cross-room chains, preserving the natural boss and waves.
They cover `Tower`, `FrontInAllRooms`, `FrontInRoomAndRoomAbove`,
`WeakestAllRooms`, `StrongestAllRooms`, `RandomFromAnyRoom` and
`StrongestLastTargetedCharactersRoom`. The first oracle matches 12 plays, four
EndTurns, 30 room stages, eight card cycles, eight train phases and seven spawns;
the second preserves surviving enemies and matches 15 plays, five EndTurns,
42 room stages, nine card cycles, nine train phases and seven spawns. Both
independent root policies, mid-battle suffixes and 16 parallel branches match
the complete terminal state, with Pyre health 80/80.

Schema 14 captures every live effect's target IDs and last-reference room/death
state for these two scenarios. The checker recomputes the collections from each
play's input and compares them in order, including effects with empty collections
and runtime tests that skip execution. This detects intermediate target changes
even when subsequent lethal damage or healing makes final unit states identical.
The lethal fixture contains 122 exact collections and 16 remote friendly
heal/upgrade outcomes; the surviving-enemy fixture contains 100 collections and
13 remote outcomes. Both include enemies on multiple floors.

Native `RoomState.AddCharactersToList` re-sorts the accumulated list by team and
physical position every time it adds a room. Tower, all-front and global HP
selectors preserve these repeated sorts; health ties also depend on room
insertion order. Global random selection uses that candidate order and consumes
one Battle draw for a nonempty collection, including a single candidate.
Static Pyre room definitions bound front ranges even after Pyre removal.

Damage leaves its last dead victim's spawn point available until the next native
trigger queue drain. Strongest-last-room can therefore select a living unit in
that reference's room before removal and yields an empty collection afterward.
The engine tracks these pending positions independently of living room units.
Native status triggers and death cleanup can change the UI's selected floor,
which `FrontInRoomAndRoomAbove` reads during resolution. Status-trigger VFX
metadata captures focus behavior for each team's facing; missing metadata
rejects a dependent room-and-above transition. OnHeal trigger notification
focus, rearrangement with pending death positions and mandatory room tests that
depend on the auxiliary random stream remain explicitly unsupported.
Pure checks additionally cover per-target room capacity, global HP ties,
single/empty RNG draws, pending-reference cleanup, armor focus, malformed inputs
and 32 parallel branches with unchanged parents.

Attack changes use the unit's raw damage buff independently of its base attack
and source card upgrades. Displayed attack is clamped at zero; a negative raw
balance must first be offset by later buffs. A zero-attack unit can still receive
these effects when `CanAttack` is true. Incapable targets remain in last-target
history but do not pass effect tests or receive changes. Zero and negative
effect amounts do nothing. Mixed-capability random tests and mandatory
last-target capability tests that depend on the auxiliary test RNG reject the
entire transition explicitly. Range/scaling parameters, trait callbacks and
Pyre-targeted attack changes remain unsupported.

`tests/fixtures/full-battle-attack-buffs.json.gz` replaces the owned rearrangement spell
with global enemy buffs and last-target debuff/recovery, global friendly
debuff/recovery, one random friendly buff, zero/negative no-ops and an until-death
unit upgrade. It retains the natural boss and waves. Pure checks additionally
cover casting capability, runtime cancellation, tested and untested drop-target
history, unit upgrade/removal with a raw deficit and 32 isolated branches.
All 21 plays, seven EndTurns, 56 room stages, 13 card cycles, 14 train phases
and 11 spawns match the native game. Six modified spell plays cover 28 raw
balance recoveries, seven remote friendly changes, one incapable target and
two zero-attack recoveries. Source unit cards remain unchanged and every random
friendly buff consumes exactly one Battle draw. The independent root policy,
mid-battle suffix and 16 parallel branches match the complete terminal state,
with Pyre health 32/80 and zero unsupported transitions or differences.

Maximum-health buffs increase current maximum health, heal with the native
multiplier while bypassing heal immunity, and suppress OnHeal unit triggers.
Unhealable units still gain maximum health. The battle lifetime also adds the
raw signed amount to the spawner card's temporary health offset, including
negative amounts that do not change the live unit. Unit-death lifetime and
preview do not write that source-card offset. Debuffs subtract both maximum
and current health directly; zero/negative amounts do nothing. A lethal debuff
sacrifices the unit, applies death effects/statistics and routes its spawner card
without ordinary armor/damage or damage-card attribution. Supported effect
chains continue after a boss sacrifice using retained source-card references.
The native base casting test also permits these effects with empty collections.

`tests/fixtures/full-battle-max-health-spells.json.gz` adds global unhealed health,
positive/negative/zero maximum-health buffs with both lifetimes and direct
enemy/friendly maximum-health debuffs to the owned rearrangement spell. Two
owned Stewards carry healing multiplier/immunity and a once-only OnHeal reward
that these buffs must leave unfired. The natural boss and waves are retained.
All 18 plays, six EndTurns, 60 room stages, 11 card cycles, 11 train phases and
nine spawns match the game. Seven modified plays cover 12 remote friendly
changes, three sacrifices, seven immune heals and five multiplier heals. The
independent root policy, mid-battle suffix and 16 parallel branches reach the
matching terminal Pyre health 80/80.

`tests/fixtures/full-battle-max-health-lethal.json.gz` uses lethal enemy maximum-health
loss and another battle-lifetime friendly buff afterward. All 12 plays, four
EndTurns, 30 room stages, eight card cycles, eight train phases and seven spawns
match the game. Seven modified plays cover 14 remote changes, seven sacrifices,
seven immune heals, seven multiplier heals and five detached source cards after
the boss dies. The independent root policy, mid-battle suffix and 16 parallel
branches match the settled terminal state at Pyre 80/80, including the resolving
spell's played/discard callbacks.

Pure checks cover signed offsets, detached and mismatched spawners, no-heal
capability, the 99,999 health ceiling, preview ownership, death rewards/counters,
post-boss continuation, later summon definitions, empty casting and 32 independent parallel branches.
Life-link, horde, conditional sacrifice/OnHealed effects, additional scaling rules,
Pyre targeting and room-and-above selection after uncaptured sacrifice focus
remain explicitly unsupported.

Run the saved native oracles without the game:

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1
```

The checker recomputes each supported model stage and compares it to the captured
native `Actual` state. It does not use the recorded model `Predicted` state as
an oracle. Run a fresh isolated full native battle with:

```powershell
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -Policy steward-once
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -Policy no-cards
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -Policy units-and-junk
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -Policy units-spells-and-junk
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -Policy units-spells-and-junk -NumericUpgrades
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -Policy units-spells-and-junk -DynamicUpgrades
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -Policy units-spells-and-junk -SacrificeUpgrades
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -Policy units-spells-and-junk -HandUpgrades
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -Policy units-spells-and-junk -TargetedHandUpgrades
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -Policy units-spells-and-junk -Healing
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -Policy units-spells-and-junk -HealingTriggers
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -Policy units-spells-and-junk -RoomSpells
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -Policy units-spells-and-junk -TerminalSpells
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -Policy units-spells-and-junk -PostKillSpells
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -Policy units-spells-and-junk -RandomSpells
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -Policy units-spells-and-junk -RandomStatus
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -Policy units-spells-and-junk -CrossRoomSpells
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -Policy units-spells-and-junk -CrossRoomTargets
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -Policy units-spells-and-junk -AttackBuffs
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -Policy units-spells-and-junk -MaxHealthSpells
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -Policy units-spells-and-junk -MaxHealthLethal
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -Policy units-spells-and-junk -NumericRanges
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -Policy units-spells-and-junk -NumericRangesLethal
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -Policy units-spells-and-junk -TargetFilters
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -Policy units-spells-and-junk -Generation
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -Policy units-spells-and-junk -GenerationLethal
```

The runner checks the explicit probe success marker, capture failures, pending
records, and differences; a game's process exit code alone does not prove the
probe passed. It hashes original profile files before and after the run. The
checked native runs left those files unchanged. Game copies, isolated profiles,
and locally decompiled reference material are ignored by Git.

## Requirements for completion

Schema 15 captures the Standby dictionary's physical entry slots and free-list
order. Unit death frees its card's slot; the next summon reuses the most recently
freed slot before extending the entry array. These slots affect enumeration
order and cannot be reconstructed from the visible cards alone. Action and
EndTurn routing preserve this immutable allocation state, including terminal
clearing. `standby-routing-calibration.json.gz` retains a native summon action
that reused a deleted entry; the checker compares its complete native state
and 32 independent parallel branches. Older captures lack this allocation
metadata and retain their historical list behavior; they do not verify hole reuse.

Spell execution now uses a complete train state internally. The existing room
API delegates to the same engine for local checks. Each applied target updates
its own room, then shares the resulting context with every other room; fixed
target collections and last-target identities span the train. Capacity-limited
unit upgrades consult the target room's definition. Dead movement and standby
spawner routing use all rooms. The 36 saved native battle oracles exercise this
engine through the normal card-action path, including cross-room target modes.

`StatisticQueryModel` implements the counter and definition queries used by
scaling traits. Every call refreshes statistic membership from all owned cards,
including buffer-only references, or the permanent deck when every pile is
empty. Per-card queries ignore type mismatches as the native code does. Global
Any counters sum the corresponding ordinary counters with type/subtype filters;
the stored Any entries are scaling notifications and cannot replace those sums.
Current/battle monster-death totals ignore filters, but PreviousTurn falls
through to the filtered per-card aggregate. Spawn totals/top-floor queries fall
through to local counters for PreviousTurn; subtype spawn counts return zero.

TypeInDeck excludes exhausted/eaten/purged membership. SubtypeInDeck includes
those owned cards and ignores the requested card type. Specific-card counts use
the permanent deck rather than current ownership. PlayedCost adds the source's
intrinsic cost and X-cost modifiers to the stored paid amount, then floors the
total at zero; UnmodifiedPlayedCost preserves signed stored costs. A variable
card without a recorded cost reads current energy only in an active battle.
Gold uses turn-start gold while the combat loop runs and live gold after it
stops. Dynamic resource inputs are immutable query frames in the shared combat
context; transitions advance these values as the branch progresses. An explicit
query frame overrides that shared frame for isolated calibration queries.

`tests/fixtures/statistic-query-calibration.json.gz` preserves 17,604 raw native
observations across six batches: live, distributed and empty piles, each with
and without stored paid costs. Calibration uses isolated inactive native
components, synthetic counters and a synthetic variable-cost source. The
counter inputs include detached entries and signed integer overflow; all seven
card type filters and all three durations are queried. It verifies membership
refresh as well as values, and checks that the live combat context is unchanged.
The independent reader recomputes every query. Pure checks cover retained buffer
aliases, exhausted subtype counts, missing inputs and 64 isolated parallel
branches. The query API supports static definition masks and no cardFilter.
Room/status/magic/corruption/capacity, last-ability-activator and CurrentCost
queries remain explicit unsupported results. Legacy captures without dynamic
frames still reject resource-dependent queries whose boundary state is missing.

Schema 23 captures energy, combat-loop state, raw turn count, forge points,
Dragon's Hoard, native moon flags and the Pyre resurrection query flag in every
shared context. Moon values are New=1 and Full=2. The resurrection value is
whether an existing resurrection relic is no longer allowed to activate, not
a resurrection count; capturing that query does not implement resurrection.
Modern decision inputs must agree with their outer resources and turn state.
Contradictory or inactive decision inputs reject the transition.

Casting tests retain the original energy; queued card effects see the paid
balance. EndTurn records the remaining energy before removing it, and combat,
movement and ordinary spawning see zero energy. After these phases, the moon
flips and the turn increments. Turn-one initial spawning precedes replenishment;
normal hand drawing and UI previews see replenished energy. The native statistic
rollover clears EnergyRemainingEndOfTurn for the next decision. Terminal damage
and effect tails keep the live-loop query state until settlement; the final
decision has RunningCombat=false and remains in the InBattle save sequence.
Generation, gold rewards, modifiers, card routing and terminal pile clearing all
preserve the frame. Branches retain their parent's original values.
An exact relentless cycle keeps RunningCombat=true; cycle detection does not
invent a native victory/defeat settlement.

`tests/fixtures/full-battle-dynamic-statistics.json.gz` retains an unaltered native
trace from a controlled starting decision with forge=7, hoard=9 and eight
energy per normal turn. Live gold deliberately differs from deployment-turn
start gold. Reduced spell scaling lets enemies reach combat, so the trace
actually queries nonzero end-turn energy. Natural boss and wave definitions
are unchanged. All 19 card plays, 5 EndTurns, 38 room stages, 9 card cycles,
9 train phases and 7 spawns match, ending in victory with Pyre health 80.

All 255 scaling callbacks match damage and complete refreshed contexts: 210
spell callbacks and 45 combat callbacks, including 27 direct attacks and 18
spikes callbacks. Eleven query kinds occur across raw turns 1, 2 and 4; both
native moon flags occur. Every combat callback sees current energy zero and
remaining-end-turn energy three. Twenty-eight callbacks have live gold that
differs from the recorded turn-start gold. The resurrection query flag is zero
in this fixture; it does not verify activation of a resurrection relic.

The archive decompresses byte-for-byte to the 264,983,231-byte native JSON
(SHA-256 `89f8485a94a350689f11f8a0e7b702ea0668d133e1af0d2d8e15463d4880bce9`).
The independent reader recomputes every callback and all 48 decision boundaries,
then simulates the complete policy from initial/mid-battle roots and 16 parallel
branches. Pure checks also verify two successive turns, variable-query energy
fallback, explicit-frame precedence, contradictory-input rejection, terminal
gold/energy/moon timing, exact cycles and 32 isolated branches. Capture failures,
differences, unsupported transitions and pending records are zero. Original
profile files are unchanged. Additional resource-changing card/relic effects
and variable-cost card actions remain outside this verified scope.

Schema 25 captures mixed ordered `CardTraitScalingUpgradeUnitAttack` and
`CardTraitScalingUpgradeUnitHealth` descriptors on immutable card instances and
creation rules, plus the upgrade definition's `MagicPowerTraitScalingOnly` flag.
Unit-upgrade effects create a fresh upgrade for each target, then run source-card
traits in order. Queries use the associated trait owner's identity. Integer
products and additions wrap without adding the source card's numeric modifiers.
Restriction 1 allows an ordinary cast with no character trigger, and allows
`OnSpawn`; it skips other character triggers. Other restriction values follow the
native unrestricted path. A magic-only upgrade skips these traits and their
queries. Clone exclusion precedes the callbacks; uniqueness and capacity checks
follow them. Rejected applications still retain any query membership refresh.

Scaled upgrades are retained on the unit and its spawner's applicable modifier
group. Creation, discard, hand/unit upgrades and health updates preserve the
source trait descriptors. Native definition-based removal finds and removes
stored upgrade entries by ID, but deducts the base-valued upgrade passed by the
removal effect. It does not re-run the scaling traits or deduct the stored scaled
values. Consequently a residual attack/health bonus can remain on the unit after
the corresponding unit/spawner modifier entry is removed; the model preserves
this behavior.

Unit-upgrade application and removal in preview mode change the simulated unit
but retain both permanent and temporary spawner-card modifiers. This follows the
native effects' explicit `!PreviewMode` source-card gates. Pure checks cover all
three lifetimes and removal from an already upgraded unit; these preview cases
are checked against native source behavior rather than a new runtime capture.

`tests/fixtures/full-battle-unit-upgrade-scaling.json.gz` retains 245 live callbacks and
30 isolated callbacks, 18 plays, 6 EndTurns, 53 room stages, 11 card cycles, 11
train phases and 9 spawns. It wins the natural battle with Pyre health 80. The
controlled owned spell exercises both stat types, signed bonuses, local played
history, turn/moon/forge queries, permanent/temporary/unique upgrades, repeated
removal, and capacity and magic-only gates. The callback calibration covers
null/OnSpawn/OnHeal restrictions. It temporarily gives the trait owner a distinct
local counter while supplying another card as the callback argument, then
restores the original native statistic entries and verifies the full live
context is unchanged. Independent checks recompute complete upgrade/context
outputs and the full battle from initial/mid-battle roots with parallel branches.
The retained native JSON is 219,987,508 bytes, with SHA-256
`64c8be1e716b2263b967e1a610f47a263bb244c7f58425a8b0e9a7fb4ae1df4c`.
Capture failures, mismatches, unsupported stages and pending records are zero;
the original profile files remain unchanged. Single-instance accumulation and
upgrade-scaling classes beyond these two remain to be modeled.

Schema 26 adds an immutable unit-upgrade effect descriptor to character triggers.
`OnHeal`, `PostCombat` and living room targets of `OnDeath` can apply ordinary or
temporary unit upgrades and remove temporary upgrades. The existing upgrade
rules receive the triggering unit's spawner card and actual trigger kind. Every
target gets a fresh scaled upgrade. Lifetimes, clone/unique gates, base-valued
removal and source-card updates follow the same paths as spell upgrades.

Trigger preflight and per-effect tests distinguish whole-trigger refusal from
canceling subsequent effects. Native marks a trigger as fired before its effects;
the model preserves this ordering for nested deaths. Application updates the
current working unit in place, preserving the attacker reference and preview
once-only flags. Max-health healing does not recursively fire `OnHeal`. Random
target tests do not consume RNG; successful applications do. Self targets bypass
team, health/status/subtype and untouchable filters, retaining the native boss
filter. Room targets use the existing collector. Relentless cycle signatures now
include changing attack/max-health/size, unit upgrades and spawner modifiers.

`tests/fixtures/full-battle-unit-trigger-upgrades.json.gz` preserves 72 native callbacks:
54 `OnHeal` and 18 `PostCombat`, including 24 restricted trait skips and 9
magic-only callbacks. The controlled Steward fixture covers continuous temporary
application/removal, a permanent once-only upgrade, repeated until-death upgrades,
silence and ignored-silence triggers. Eight captured silenced-unit states have
the ignored-silence once upgrade consumed. All 15 plays, 5 EndTurns, 42 room
stages, 9 card cycles, 9 train phases and 7 spawns match; the natural battle wins
with Pyre health 80. Independent checks recompute complete callback outputs,
decision transitions and initial/mid-battle full simulations with parallel
branches. Pure checks also cover room/random/self targets, mandatory and cancel
tests, later multistrike attacks reading upgraded stats, nested lethal upgrades,
preview once flags and 32 isolated branches.

The retained JSON is 134,230,436 bytes, SHA-256
`5027333e3b3efd9e795cc1fc26346f74e4374d0c5f03fd7ab610eaa1c650785c`.
Capture failures, mismatches, unsupported steps and pending records are zero;
original profile files are unchanged. Dead-self upgrade
routing, trigger upgrades that require room capacity, cross-room or remembered
targets, and single-instance accumulation remain explicit unsupported cases.

Schema 27 executes `OnSpawn`, then `OnUnscaledSpawn`, then
`OnSpawnNotFromCard` for cardless entrants. A player summon enters the room and
updates floor/subtype spawn counters before its triggers; callbacks read the
paid cost and remaining energy while the source card is in the discard buffer.
Unit upgrades, their source-card lifetimes and generated cards settle before
the played/discarded callbacks. A summon removed during its own triggers still
allocates and frees its standby slot, then exhausts its source once. Its unit
identity remains consumed even when no quiet snapshot can observe it alive.

Enemy waves create and enter the complete selected group before any member's
spawn triggers execute, preserving creation order. Room targets consequently
include later group members. Treasures use the same cardless trigger phases;
removed entrants leave no movement record. Pure checks cover paid/spawn-scaled
upgrades, generation, deployment skips, death/despawn routing, enemy batches,
treasures and 32 parallel branches without changing their parent states.

`tests/fixtures/full-battle-spawn-triggers.json.gz` retains 24 native scaling callbacks
(16 ordinary spawn, 8 unscaled), 8 exact generation effects including 2 cards
generated by player summons, 20 plays, 6 EndTurns, 55 room stages, 11 card cycles,
11 train phases and 9 enemy spawns. It wins with Pyre health 70. The lethal
variant retains 24 ordinary spawn callbacks, 8 generation effects, 23 plays,
7 EndTurns, 58 room stages, 13 card cycles, 14 train phases and 11 spawns; it
wins with Pyre health 45. It also checks removed summons, source exhaustion and
standby free-slot history. Both independently recompute complete callback and
generation outputs and initial/mid-battle policies with 16 parallel branches.
The 37 prior full-battle fixtures and 7 native calibrations still pass.

The retained JSON bytes have SHA-256:
`a78232fad98034a58c8a990fb4a9971129b8f70fa5ac4ce6189cd553b176faf7`
(ordinary, 200,218,764 bytes), and
`08da780458768b91354482c7519a0e4f1db6083303f9dd330f71df49d3b95918`
(lethal, 233,834,673 bytes). Capture failures, mismatches, unsupported steps and
pending records are zero; original profile files are unchanged. Native natural
waves and boss are retained. Other unit-added notifications, enchantments,
attachments, cardless player spawning and battle-ending summon routing remain
outside this verified subset and require additional modeling.

Schema 28 executes `OnTurnBegin` once per unit turn, including each relentless
round. Attack/trigger prevention is evaluated first. Active daze reports its
prevention before this phase and disables ordinary triggers; its stacks follow
the captured triggered/end-of-combat removal rules. Ignored-silence triggers can
still run but do not restore the current turn's attack. Silence and deployment
rules retain their existing gates. Attack conditions and the multistrike count
are evaluated after turn triggers, so a zero-attack unit may gain an attack and
an incapable unit can trigger without attacking. A turn trigger is not repeated
for each multistrike hit. Empty opposing teams still allow turn triggers.

Pure checks cover zero/incapable attacks, enemy turns, empty targets, dazed and
silenced actors, ignored-silence exceptions, deployment skips, repeated and
once-only upgrades, source routing after despawn, preview gold exclusion and
32 parallel branches. Other attack-prevention statuses and status-trigger
interactions remain outside the supported status definitions.

`RoomCombatModel.ApplyUnitTurn` also exposes this phase for independent native
comparison. The probe records the full room before/after each actual unit turn
and direct-attack target IDs, excluding native preview attacks. These records
participate in the trace's mismatch, unsupported and pending totals.
`tests/fixtures/full-battle-unit-turn-begin.json.gz` retains 60 scaling callbacks,
including 20 restricted skips and 20 positive attack-scaling callbacks, 47 exact
unit turns and 37 direct attacks. Independent checks compare complete per-turn
states and attack target sequences, covering 2 zero-attack recoveries,
1 dazed/ignored turn without an attack, 4 silenced/ignored turns and 11 upgraded
enemy turns. The native daze stack persists until end-of-combat removal, as its
captured definition specifies.

All 15 plays, 5 EndTurns, 42 room stages, 9 card cycles, 9 train phases and
7 spawns match; the natural battle wins with Pyre health 69. Initial/mid-battle
policies and 16 parallel branches reproduce the terminal result. All 39 prior
battles and 8 calibrations still pass. The retained JSON is 169,395,775 bytes,
SHA-256
`86757ad020525276c197ddfcc8ff05d101ab90652fcb891ee40d362eed0326dd`.
Capture failures, mismatches, unsupported steps and pending records are zero;
original profile files are unchanged. Attack and turn-entry triggers
beyond the supported kinds still require additional modeling.

Schema 29 adds `OnTeamTurnBegin`. An exchange first runs ambush units, then all
enemy team-start triggers before enemy unit turns, then all player team-start
triggers before regular player unit turns. Ambush units still receive their
team-start effects, but do not attack again in the regular player phase. Each
team phase processes a snapshot of its living actors in physical room order;
effects from every actor settle before the team attacks. Unit-turn daze
prevention is evaluated later and does not disable the earlier team phase.

`RoomCombatModel.ApplyTeamTurnBegin` supports separate phase comparisons.
The native probe retains complete team-phase room states and a shared sequence
for team phases and individual unit turns, allowing ambush/enemy/player ordering
to be checked. Pure checks cover whole-team room buffs before attacks, phase
decomposition, repeated/once triggers, silence and ignored-silence exceptions,
deployment, removal during a team snapshot, preview and 32 parallel branches.

`tests/fixtures/full-battle-team-turn-begin.json.gz` retains 38 exact team phases
(19 enemy, 19 player), 40 exact unit turns with 28 direct attacks, and 90 scaling
callbacks (42 team-start, 48 unit-start). Independent checks compare complete
phase states, callback outputs and attack target sequences, with 6 native
ambush-before-enemy-before-player order records. The natural battle matches all
15 plays, 5 EndTurns, 40 room stages, 9 card cycles, 9 train phases and 7 spawns,
winning with Pyre health 69. Initial/mid-battle policies and 16 parallel branches
reproduce the terminal result; all 40 prior battles and 8 calibrations still pass.

The retained native JSON is 202,662,856 bytes, SHA-256
`974235394293b987d86dd48d2f029daf3d2533178995f8bd4d40e84ae6df7cba`.
Capture failures, mismatches, unsupported stages and pending records are zero;
original profile files are unchanged. Global pre-combat phases,
attack/hit/kill triggers and broader status/room/relic interactions remain to be
modeled.

Schema 30 adds the global `EndTurnPreHandDiscard` character phase. Native active
character lists append at creation and retain that order across floor and
physical position changes. The projected unit identities are assigned at native
creation; the model processes each team's snapshot in that order, including
the Pyre in the player list. The player queue finishes before the enemy queue.
Effects observe the current hand and incoming energy statistic before discard
and energy removal. Newly generated hand cards are discarded in the same turn.

Deaths update counters and return standby spawners immediately. Their `OnDeath`
effects append to the running trigger queue, after the remaining team actors;
nested deaths append in turn. This distinction preserves generated-card
identities, creation upgrades and subsequent statistics. Unknown effects
produce no usable child state. UI battle preview runs only the player
pre-discard phase once, before previewing the rooms. Buffs affect those copied
units; live units, card piles, gold and source upgrades remain unchanged.

`TrainCombatModel.EndTurnPreHandDiscard` and its room entry point support
independent phase comparisons. Native records retain both teams' creation
orders and complete train states. Pure checks cover cross-floor order, Pyre
participation, existing energy and earlier exhaustion, once/repeat triggers,
silence/daze/deployment, queued death effects and generation, preview and 32
parallel branches. The isolated fixtures enable this trigger during deployment
and seed its incoming energy statistic to seven, captured in the starting
state, to make the later energy-snapshot boundary observable.

`tests/fixtures/full-battle-pre-hand-discard.json.gz` retains 10 exact character phases,
14 scaling callbacks and 13 generation callbacks. Independent checks compare
complete phases, both teams' active order, generated-hand discard and incoming
energy. All 15 plays, 5 EndTurns, 42 room stages, 9 card cycles, 9 train phases
and 7 spawns match; the natural battle is won with Pyre health 80. The complete
policy also matches from initial/mid-battle roots with 16 parallel branches.
The native JSON is 182,243,837 bytes, SHA-256
`46fab2069ea7b672e1aab04206422b8563e228d101f149ba70f9e72f6616faed`.

`tests/fixtures/full-battle-pre-hand-discard-lethal.json.gz` retains 14 exact phases,
10 scaling callbacks, 2 nested unit deaths and 4 hand generations. The death
cards carry a distinct native temporary upgrade; their identities are allocated
after both actors' ordinary generation, independently verifying the queue order.
All 21 plays, 7 EndTurns, 60 room stages, 13 card cycles, 13 train phases and
11 spawns match, winning with Pyre health 74. Complete policies from initial
and mid-battle roots reproduce the terminal state with 16 parallel branches.
The native JSON is 275,900,270 bytes, SHA-256
`847b15f886f6d1ebe1e849b441f602759a6ddd3b1a374c42cceadc141a07354a`.
Both fixtures have zero capture failures, mismatches, unsupported stages and
pending records; original profile files are unchanged. The 41 earlier battles
and 8 calibrations also pass; there are now 43 verified complete battle fixtures.

The broader objective remains open: pre-combat, other character/card triggers,
relic and hand-retention effects, boss actions, equipment and room mechanics
still need their own native validation.

Schema 31 captures immutable `CloneDamageBase` and `CloneHealBase` on upgrades.
Native cloning first copies permanent and optional temporary upgrades, then
refreshes all existing destination upgrades. Upgrade data whose positive
damage/heal is not marked as scaled by a non-magic-power trait resets that
value to its base times the new card's magic-power multiplier. The supported
creation traits have multiplier one; multiplier-changing traits remain explicit
external interactions. Anonymous upgrades, nonpositive data bases and upgrades
marked for non-magic-power scaling preserve their current values. Other
statistics, source offsets and the source itself remain unchanged.

This refresh also applies to destination optional upgrades when copying source
temporary modifiers is disabled. Pending next-added temporary upgrades arrive
after the refresh. Missing copy sources take the native early return without
refreshing the destination. Unit scaling preserves the immutable base fields.
Earlier captures omit these nullable descriptors and retain their observed
copy behavior; they do not establish coverage of this newly captured refresh.

`tests/fixtures/full-battle-clone-upgrade-refresh.json.gz` retains 10 exact generations
including two death copies. Each copy resets a scaled permanent damage value
from 8 to 1 and a temporary heal value from 8 to 2, while preserving a damage
value of 9 whose data is marked for non-magic-power scaling. Independent checks
compare full contexts and verify that both sources stay unchanged. The battle
matches all 21 plays, 7 EndTurns, 60 room stages, 13 card cycles, 13 train phases,
11 spawns, 14 pre-discard phases and 10 scaling callbacks, winning with Pyre
health 74. Complete initial/mid-battle policies and 16 parallel branches match
the terminal state. Pure checks also cover optional/pending ordering, ignored
temporary modifiers, no-copy/missing-source behavior, retained descriptors and
32 parallel branches.

The native JSON is 325,689,980 bytes, SHA-256
`81bb57bb4a103f482b3b97ac1c85e1ba2b964b7f3efd38c19f4b9d4b35d69982`.
Capture failures, mismatches, unsupported stages and pending records are zero;
original profile files are unchanged. All 43 earlier battles and 8 calibrations
pass; there are now 44 verified complete battle fixtures. The full objective
still includes the broader mechanics listed above.

Schema 32 adds native per-team `PreCombat` records and pure train/room phase
entry points. The new turn resets statistics, increments the turn counter and
spawns its initial enemies before resetting Spawning RNG and running these
queues. The player queue completes before the enemy queue, both in character
creation order including the Pyre. Energy remains zero until after both phases;
opening/ordinary hand draws follow them. Cards generated into the hand coexist
with that draw and use the remaining hand capacity. Room combat and battle
preview do not repeat this once-per-new-turn phase.

The shared character-phase engine preserves deployment, silence/ignored-silence,
daze, once/repeat, unit/source upgrades, immediate death counters and standby
routing, and deferred FIFO `OnDeath` callbacks. Independent pure checks verify
creation order across floors, Pyre/team participation, earlier exhaustion
statistics, nested generation, unknown-effect rejection, immutable parents and
32 parallel branches.

`tests/fixtures/full-battle-pre-combat.json.gz` retains 8 exact complete phases (4 per
team), 8 generated hand cards, 12 scaling callbacks (6 current-turn and 6 reset
hand-draw queries), 4 subsequent draw boundaries and 2 initial enemies that
participate in the first phase. All 15 plays, 5 EndTurns, 44 room stages,
9 card cycles, 9 train phases and 7 spawns match, winning with Pyre health 79.
The independent policy finishes from initial and mid-battle states, compares
every action/decision, and reproduces the terminal state in 16 parallel branches.

The native JSON is 173,469,684 bytes, SHA-256
`ab1efd077ecdff0c7f61cb985484fed02dadddae1f03b1435f826e15f6550d72`.
Capture failures, mismatches, unsupported stages and pending records are zero;
original profile files are unchanged. All 44 earlier battles and 8 calibrations
pass; there are now 45 verified complete battle fixtures.

An additional isolated lethal experiment matches all 8 character phases,
including queued death effects, but finds a separate terminal statistic-cache
membership gap. Its full battle is deliberately not retained as a passing
fixture: one newly generated card is owned but absent from native `deckStats`,
and must disappear from projected tracking when terminal piles clear. The
earlier statistics model merged those two memberships. The following independent
schema 33 change resolves that gap with its own retained native regression. Other triggers, relics,
equipment, room mechanics, boss actions/companions/final bosses, resurrection
and the broader rules listed below still leave the overall objective open.

Schema 33 captures immutable `BattleStatistics.StoredCards`, the actual native
`deckStats` keys, separately from `TrackedCards`, the projection's union of cache
keys and current owned cards. Generation changes ownership without necessarily
creating a cache entry. Each context normalizes that union from its copied card
instances and stored keys. Consequently, terminal clearing drops an uncached
generated card while keeping genuine cache entries and retained card identities.

Counter events, statistic queries and turn rollover materialize cache entries.
Resource/last-attack setters preserve the cache; detached source counters do not
invent entries. Empty-pile queries refresh from the permanent deck. Cache
membership participates in the room state signature. Nullable metadata preserves
the behavior of earlier captures, which do not prove this newly modeled boundary.
Pure checks verify these distinct futures, source/global zero events, legacy
states, immutable parents and 32 parallel branches.

`tests/fixtures/full-battle-statistic-cache.json.gz` is the fresh native regression for
the previously failing lethal pre-combat experiment. It matches all 10 complete
generations with uncached births, two terminal removal boundaries and the nine
cached generated cards retained at each boundary. The two killed player units
return their standby cards immediately, while their marked death cards are
allocated after the remaining player phase effects. All 8 character phases,
10 scaling callbacks, 15 plays, 5 EndTurns, 43 room stages, 9 card cycles,
9 train phases and 7 spawns match, winning with Pyre health 77. Initial/mid-battle
independent policies and 16 parallel branches reproduce every decision and the
terminal state.

The native JSON is 177,665,942 bytes, SHA-256
`835fcd37b9bd3011cb00db3e1fb971a83a85098f65eac14294fa19c6dae06981`.
Capture failures, mismatches, unsupported stages and pending records are zero;
original profile files are unchanged. All 45 previous battles and 8 calibrations
pass; there are now 46 verified complete battle fixtures. Other character/card
triggers, relics, equipment, room effects, additional statuses, boss actions and
companions/final bosses, resurrection and the broader objective remain open.

Schema 34 captures healing effects on character triggers, both on living units
and in definitions for future summons/waves. Ordinary character effects have no
parent card: its heal upgrades do not change their quantities. Native getters
still floor scalar values and range endpoints at zero. A negative range
multiplier can produce a negative request after sampling. Target collection
precedes one sample for the entire group; an empty `Room` passes the effect test
and still samples. Random preflight tests leave Battle RNG unchanged.

Triggered healing uses the existing healability, multiplier, immunity and health
clipping rules. Zero, clipped and immune heals still queue OnHeal. The shared
character queue now carries OnHeal and OnDeath in FIFO order: callbacks append
after already queued team actors, and newly nested callbacks append at the tail.
Ordinary room triggers also drain a local FIFO instead of executing nested
callbacks recursively. Queued actors use current live state; a later death
suppresses its pending healing effects. Queue continuation preserves preview
once-only flags. Tests distinguish deferred healing upgrades from immediate
application and retain immutable parents across 32 parallel branches.

`tests/fixtures/full-battle-triggered-healing.json.gz` preserves an unchanged
native trace with 15 plays, 5 EndTurns, 44 room stages, 7 spawns, 8 pre-combat
phases, 69 triggered healing effects and 16 unit-upgrade scaling callbacks. It
wins with Pyre health 79. The fixture includes self/team bypass, room/healable/
random selection, empty ranges, scalar clamping, negative/zero/immune requests,
source-card heal upgrades and two newly fired deferred OnHeal upgrades. Initially
full units finish the first phase exactly five health below their new maximum:
the later heals ran before the queued unhealed-health upgrades. Per-effect native
sampling and request observations are independently recalculated, as are full
phase and decision states, initial/mid-battle policies and 16 parallel branches.

The raw JSON is 208,862,740 bytes, SHA-256
`fb5bc6e58d9b2661e4f377f3a14f506d273170ed96267eee7abd0d592702a482`.
Capture failures, mismatches, unsupported stages and pending records are zero;
the original profile is unchanged. All 46 previous complete battle fixtures and
8 calibrations pass; there are now 47 retained complete battle fixtures.
Cross-room/sticky trigger targets, sacrifice
healing traits, other trigger effects and post-combat healing phases still need
modeling, alongside the wider outstanding battle mechanics.

Schema 35 adds unit `PostCombatHealing` before ordinary `PostCombat`, for each
enemy then each player in room order. Each actor/phase finishes its nested queue
before the next phase. Healing prevention blocks the entire healing phase,
including ignored-silence effects; trigger prevention still permits those
effects in ordinary PostCombat. Attack capability alone does not prevent
healing. Each exchange resets its prevention sets. They remain effective after
status clearing and through the final exchange of relentless combat; unit
post-combat phases run once after the exchange loop.

Native unit attack upgrades also adjust the scalar of its first PostCombatHealing
heal effect, with a zero floor. Removing each matching upgrade subtracts its
attack amount from that running value. Later healer effects and range endpoints
are unchanged; previews leave healing quantities unchanged. This update precedes
the upgrade's health changes. A trigger sequence reads its current effect state,
so an earlier upgrade changes the subsequent heal and remains in the child state.
Effect scalar values participate in the room signature. Pure checks cover
continuous removal, clamping, ranged scalars, preview isolation, per-actor order,
post-clear daze, deployment/once/silence, final relentless gating and 32 parallel
branches.

`tests/fixtures/full-battle-post-combat-healing.json.gz` retains a complete native
trace with 18 exact post-combat phases, 8 pre-combat phases, 89 observed healing
effects (19 in PostCombatHealing), 24 upgrade callbacks, 15 plays, 5 EndTurns,
43 room stages and 7 spawns. Two phases preserve attack/healing prevention and
leave ignored healing once flags unconsumed. Four ordinary-post callbacks and
four within-healing callbacks independently verify scaled upgrades; the latter
change a heal later in the same effect sequence. Initial/mid-battle independent
policies and 16 parallel branches reproduce victory with Pyre health 79.

The unchanged raw JSON is 237,928,863 bytes, SHA-256
`ac77459194552a97c56668e3bb832d7743090d772b65583c0377e285dbba2de7`.
Capture failures, mismatches, unsupported phases and pending records are zero;
the original profile is unchanged. All 47 previous battles and 8 calibration
fixtures pass; there are now 48 retained complete battles. Cross-room/sticky
character effects, sacrifice healing, additional triggers/statuses, equipment,
room/relic effects, boss actions/companions/final bosses and resurrection still
leave the complete-battle objective open.

Schema 36 adds ordinary character-trigger `CardEffectDamage` descriptors to live
units, future player summons and future enemy definitions. Quantity getters
clamp raw damage/range endpoints without parent-card modifiers. Runtime damage
still passes the triggering unit as attacker and its spawner as the explicit
played card. This preserves responsible-card traits, per-hit statistics and
kill attribution; numeric source upgrades enter through applicable damage
traits rather than changing the effect-state quantity itself.

Damage preflight and runtime tests each sample a range, even for empty target
sets. An application samples once after actual target collection, then applies
one quantity to its ordered group. Self bypasses ordinary team filters; random
target tests consume no Battle draws. Negative samples and zero maximum ranges
fail before application, with casting and subsequent-effect cancellation flags
preserved. Status multipliers read the actor's stacks and wrap integer products.
The shared damage engine applies Default damage defenses without direct-attack
melee weakness, lifesteal or spikes.

OnDeath callbacks remain in the shared FIFO. Damage deaths update death counters
inline, but their spawner return/exhaustion waits until the complete trigger
queue ends and uses active character creation order. Later generated cards
therefore receive the exhaustion event. Upgrade deaths retain their previously
verified inline settlement. Pure checks cover these differences, late generation,
source traits/offsets, defenses, test gates, empty/random groups, integer wrapping,
parent isolation and 32 parallel branches.

`tests/fixtures/full-battle-triggered-damage.json.gz` preserves the complete
native trace unchanged: 18 plays, 6 EndTurns, 48 room stages, 9 spawns, 10
pre-combat phases, 9 generation effects and 12 upgrade callbacks. Its 198 damage
quantity observations include 138 tests and 60 applications: 9 failed negative
tests, 9 empty ranged applications, 13 groups, 9 random selections, 6 positive
status multipliers and 8 death effects. Independent hit checks verify 70 damage
requests, 2 shield blocks, 16 armor observations and explicit spawner source
identities. Two phase snapshots show newly generated cards receiving the late
exhaustion counter. All phase and initial/mid-battle policy comparisons match,
including 16 parallel branches, victory and final Pyre health 80.

The captured game is 2.2.1, module MVID
`8fb07b96-f4db-4d2b-884d-c00536d6ccf4`. Raw JSON is 249,677,993 bytes,
SHA-256 `9e2c759d5cf5715e59803e1728ea64a41eb27bfd26886935c9940c6be27978ed`;
the byte-identical gzip is 7,875,744 bytes. Capture failures, mismatches,
unsupported phases and pending records are zero, and the original profile is
unchanged. All 48 earlier complete battles and 8 calibrations pass; there are now
49 retained complete battle fixtures. Cross-room/sticky character targets,
combat-event and other triggers/statuses, specialized sacrifice effects,
equipment, room/relic effects, boss actions/companions/final bosses and
resurrection still leave the full objective open.

Schema 24 captures ordered `CardTraitScalingAddStatusEffect` descriptors on
immutable card instances and generated-card rules, plus the native stackability
of status definitions. A status application's immunity check precedes its source
traits. Each zero-only trait checks the running incoming amount after preceding
traits, rather than the target's existing count. List, propagatable and Horde-only
filters preserve native query short-circuits; propagation uses captured status
definitions with separate Hero/Monster exclusion masks. Integer products and
running bonuses wrap as native integers do; these traits do not apply numeric
card upgrades, floating multipliers or preview-only Juice/relic modifiers.

The application clamps the final count to 0..9999 for stackable statuses, or
0..1 for nonstackable statuses. Only an actual positive delta attributed to a
source card updates `AnyStatusEffectStacksAdded`, and only outside preview.
Negative additions do not count as added stacks or invoke removed-stack
attribution. Subsequent reverse-order targets query the preceding target's
updated statistics. Creation, discard, hand/unit upgrades and health changes
retain trait descriptors; unsupported queries do not return usable child states.

`tests/fixtures/full-battle-status-scaling.json.gz` preserves the original native JSON
bytes: 312 trait callbacks, 75 applications, 15 plays, 5 EndTurns, 43 room stages,
9 card cycles, 9 train phases and 7 spawns. It wins the natural battle with Pyre
health 80. The controlled owned spell exercises ordered zero gates, positive and
negative scaling, 10 observed 9999-stack caps, 13 decreases, source-counter feedback,
20 immune applications, both target
teams, and native turns 1..4 across both moon phases. Independent checks recompute
callback bonuses, complete before/after application states and the entire battle
from initial/mid-battle inputs with parallel branches. The isolated run reports
zero capture failures, mismatches, unsupported stages and pending records; the
original profile files are unchanged. Removed-stack scaling remains unsupported
until its source-attribution paths are modeled. Horde filter matching and the
nonstackable application cap have focused model checks; Horde combat mechanics
and nonstackable spell casting legality remain outside the full-battle fixture.
The retained native JSON is 322,446,069 bytes, with SHA-256
`1427b643a923638379f2522266671384a07a3f514b0fe3cf3424a49db8f471a9`.

Statistic updates also preserve the native signed integer boundary. Source-card
and global event counters, floor/subtype spawn dictionaries and death totals
wrap at the 32-bit boundary. Negative nonzero counters remain present and survive
native turn/battle duration rollover; they are not saturated or discarded. A
subsequent status-scaling target therefore reads the wrapped count and can add
zero stacks after the preceding target reached the stack cap.
`tests/fixtures/statistic-overflow-calibration.json.gz` retains 60 native source/global
counter updates and 5 floor/subtype updates, initialized at positive and negative
integer boundaries, including 20 positive-to-negative wraps. It runs on isolated native components and verifies that the
live battle context is unchanged. Independent checks compare complete statistics
and exercise cross-target status feedback and 32 parallel branches.
The original calibration JSON is 1,049,950 bytes, with SHA-256
`53668df023e9c950374779119c7cd19598202d68c72af2a087b04a71e956f0c5`.

An increment with amount zero still runs native event bookkeeping. The six
mapped event families update their `Any` counters once for each tracked card
and once more for the tracked source, while the source quantity stays unchanged.
`TimesPlayed` also appends the source to play history. A detached positive source
identity contributes to global events without gaining ownership or a local
counter. Previous-turn values stay unchanged; event counters preserve signed
wrapping. Stack-count increments have no mapped global event.

`tests/fixtures/statistic-zero-increment-calibration.json.gz` retains 48 native
zero-amount updates across six event families and two stack counters, with
24 detached sources, zero/seeded/boundary totals and 6 positive-to-negative
wraps. The isolated native component leaves the full live context unchanged.
Independent checks compare complete statistics and exercise 32 parallel
branches. `Run-FullBattleProbe.ps1 -StatisticOverflow` captures both the original
65 boundary samples and these additional 48 zero samples. The retained JSON is
924,095 bytes, SHA-256
`09d7cd4deb7ce74f75696af41d57908e1b9f3ffca30aea8dbcc8bca469cdee37`.
The accompanying native full battle matches all 21 plays, 7 EndTurns, 60 room
stages, 13 card cycles, 14 train phases and 11 spawns, winning with Pyre health
73. Capture failures, mismatches, unsupported stages and pending records are
zero; original profile files are unchanged. All 39 prior battles and 7 prior
calibrations also pass; the zero calibration is the eighth retained calibration.

Schema 22 captures ordered `CardTraitScalingAddDamage` descriptors on immutable
card instances and generated-card creation rules. Each hit queries the current
statistics using the associated trait owner's identity. Its integer product
uses native unchecked arithmetic before single-precision multiplication and
flooring. Replacement/additive modes and multiple traits preserve native order.
Each callback applies the explicit damage-source card's permanent and temporary
damage upgrades to zero, with a floor between groups; an ordinary unit attack
or spikes retaliation supplies no explicit card and adds no such upgrade term.
The final bonus damage is clamped to zero before defensive statuses. The magic
power-in-target-room special case returns zero without querying statistics.

A naturally played card enters DiscardBuffer while its effects execute. This
keeps the source owned during membership refresh and preserves its history.
Damage traits and status focus run before the previous victim's queued standby
return; the resulting damage is retained instead of querying again after that
return. In a Boss/Pyre room, each live attack first performs the native single
hit kill preview on copies. Character state is restored, but refreshed statistic
membership and last-attack damage survive, including nested spikes. A trait
reading last-attack damage therefore sees that preview's result before the real
hit. Full-room UI previews remain a separate existing transition.

`tests/fixtures/full-battle-damage-scaling.json.gz` preserves a complete unmodified
native JSON trace: 15 plays, 5 EndTurns, 41 room stages and 63 damage callbacks;
the battle is won with Pyre health 80. The native scaling oracle exercises ordered replacement/additive traits, signed
numeric upgrades, fractional multipliers, repeated tower targets, multistrike,
spikes and relentless. Independent checks recompute callback damage and complete
refreshed contexts, then run the complete policy from initial/mid-battle inputs
with parallel branches. Creation/discard/modification copies retain descriptors.
Queries with unmodeled status-counter update paths, missing dynamic resources,
or an unverified float-to-integer overflow domain return explicit unsupported
results; sacrifice/ability responsibility and other damage-trait classes remain
to be modeled.

Schema 21 captures the secondary piles in the shared combat context as well as
the outer decision state. A room death or post-combat despawn updates standby
slots and exhaustion before the context reaches another room. Damage spells
keep the final victim's spawner in standby until the existing death queue drains;
hand removal preserves nested return ordering. The terminal clear resets all
secondary piles and the standby dictionary layout. Older fixtures leave this
field null and retain their historical outer-state routing; they do not verify
native shared-pile state at room boundaries. The checker also starts each older
oracle with a shared copy of its captured outer piles and independently runs its
complete policy. All originally captured decision fields, including the outer
piles, stay in that comparison; only the newly added shared copy is omitted.

`tests/fixtures/full-battle-shared-piles.json.gz` and
`tests/fixtures/full-battle-shared-piles-lethal.json.gz` preserve complete native traces
without changing their JSON bytes. Independent checks compare room and decision
contexts, then recompute the complete policy from the initial and mid-battle
states with 16 parallel branches. The lethal fixture also verifies hand
consumption, nested dead-spawner returns and transient standby slot reuse.

1. Capture a self-contained starting battle state and its static rule definitions.
   Include card instance identities, permanent/temporary modifications, card
   statistics, units, statuses, trigger counters, equipment, room state and
   attachments, resources, spawn pattern, boss state, relic internals, delayed
   effects, and every gameplay RNG stream. Do not derive hidden future outcomes
   from a native execution of the candidate being modeled.
2. Apply legal unit plays, targeted spells, equipment, room cards, abilities, and
   follow-up choices to independent child states. Unsupported effects must be
   explicit; a partially applied transition must never become a search node.
3. Implement the complete native EndTurn sequence: pre-discard triggers, hand
   discard and energy handling, room combat, post-combat triggers and status
   clearing, enemy ascension/Pyre encounters, spawning, boss actions, turn
   advancement, energy replenishment, draw, and the next quiet decision point.
4. Continue from that state through the entire battle to victory or defeat,
   including relentless, conditional final bosses, and Pyre destruction or
   resurrection. Distinguish an exact state cycle from an unsupported state.
5. Compare complete decision states and terminal outcomes against the native
   game, using multiple action sequences and mid-run inputs. Check card piles
   and gameplay RNG as well as units, room state and final Pyre health.
6. Verify immutable parent/child isolation and parallel branch determinism.
   Only after state coverage is complete may exact transposition pruning rely
   on the state representation.

The supported initial battle now runs independently to a verified terminal result.
The full objective remains open: additional legal card/ability actions,
additional upgrade internals, additional statistic-driven effects and specialized sacrifice mechanics, relics, equipment, room effects, additional statuses and
triggers, boss actions/companions/final bosses, and resurrection are not complete.
Actions now share a state with turn transitions and work at arbitrary decision
turns within the supported rules. Trait/trigger upgrades, statistic-driven effects, equipment
and additional effects are the next steps. Exact pruning still requires
broader state coverage; these fixtures do not establish a simulator for every
Monster Train 2 battle.
