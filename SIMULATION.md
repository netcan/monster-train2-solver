# Full battle simulation

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
| Train spell execution | `CardSpellModel` | A single effect chain carries all rooms, shared card/statistic/RNG state and global target references; dead-unit movement and spawner routing are updated across rooms |
| Cross-room targeting | `CardTargetModel` and `CardSpellModel` | Native tower/front/above/global HP/random selections, exact live target IDs, deferred death positions and status/death focus changes |
| Integer RNG and shuffle | `UnityRng` | 768 native integer draws, seed initialization and complete four-word states |
| Basic draw/discard cycle | `CardCycleModel` | 13 consecutive native operations including reshuffle |
| Room attack exchange | `RoomCombatModel.Exchange` | Ordered initiative, target selection, retargeting, shield/armor and retaliation checks |
| Entire room resolution | `RoomCombatModel.Resolve` | Native normal exchanges, post-combat effects and multiple rounds of boss/Pyre relentless combat |
| Train combat phase | `TrainCombatModel.ResolveCombat` | Top-to-bottom native phase comparison |
| Enemy movement phase | `TrainCombatModel.Ascend` | Native movement and immediate Pyre combat, including the terminal boss fight |
| Unit effects | `CombatTrigger` and `CombatContext` | Generated cards, Battle RNG, treasure escape; gold and once-only trigger checks |
| Gold rewards | `GoldRewardModel` | 2,200 native calculations, reward minimums, integer/float boundaries, ties to even and preview exclusion |
| Card statistics and preview | `BattleStatistics` and `BattlePreviewModel` | Native per-card/Any counters, turn rollover, spawn subtypes, death/exhaust attribution and preview damage statistic |
| Card instance modifiers | `CardModifierModel` | Permanent/temporary ordered numeric upgrades, unit starting statuses, discard removal, play history and 256 native scalar calculations |
| Retained card references | `CombatContext.CardRegistry` | Observed card identities survive pile clearing; detached spawner upgrades/removal preserve ownership and parent isolation |
| Standby dictionary allocation | `CardPileModel` | Captured entry slots and free-list order preserve native hole reuse after unit death; malformed layouts, distinct futures, terminal clear and parallel branches |
| Runtime unit upgrades | `UnitModifierModel` | Native permanent, battle and unit-death lifetimes, duplicate removal, unique upgrades, restricted size, unhealed health and lethal max-health loss |
| Hand upgrade spells | `HandUpgradeModel` | Native targeted and targetless sequences, current-hand membership, permanent/temporary groups, uniqueness and paid-card exclusion |
| Basic healing | `HealingModel` and `CardSpellModel` | Native targeted spells, modifier group clamps, maximum health, multiplier/immunity, regen and lifesteal; independent healability checks |
| Attack buff/debuff spells | `UnitAttackModel` and `CardSpellModel` | Native raw negative balances/recovery, zero-attack and incapable targets, global/random targets, source-card ownership and later unit upgrades |
| Maximum-health buff/debuff spells | `UnitHealthModel` and `CardSpellModel` | Signed temporary source-card offsets, battle/unit-death lifetimes, multiplier/immunity, suppressed OnHeal, direct lethal loss and post-boss effect chains |
| Healing triggers | `CombatTrigger` and `RoomCombatModel` | Native repeated/once rewards and silence; zero/full/immune healing, deployment timing, preview flags and child isolation checks |
| Terminal spell resolution | `CardSpellModel` and `BattleActionModel` | Settled native boss kill continues live effects, detached spawner upgrades/removal and healing; effect gates skip/cancel, then played/discard callbacks complete |
| Enemy waves and treasure | `EnemySpawningModel` | Native group cache, spawn order, phase, slots and treasure floor selection |
| EndTurn to next decision | `BattleTurnModel.EndTurn` | Seven native consecutive transitions in each supported fixture |
| Battle to terminal result | `BattleSimulator.Resolve` and `ResolveNoMoreCards` | Independent card policies and seven-turn chains, mid-battle inputs and 16 parallel branches |
| Additional spells, abilities, relics | Not complete | Unsupported interactions explicitly reject the model transition |

The game oracle is build 2.2.1, Assembly-CSharp MVID
`8fb07b96-f4db-4d2b-884d-c00536d6ccf4`.

Gameplay RNG snapshots exclude native `BattleTest`, as well as `Chatter` and
`NonDeterministic`. `TargetHelper` uses `BattleTest` for legality/UI preview target
selection and `Battle` when live effects apply. `GameEffectHelper.TestEffect`
restores the temporary target list after testing. Numeric testing has a separate
rule: `CardEffectDamage.TestEffect` calls `GetIntInRange`, which consumes `Battle`
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

`results/ui-rng-isolation-calibration.json.gz` preserves 185 native query records,
including 53 that advance Battle inside the original UI method. Every query
restores all four words and the seed of both streams, and returns an original
boolean result. This fixture tests successful queries; no forced exception was
introduced. Auxiliary target/UI draws are excluded from independent gameplay
state; legacy snapshots retain their recorded test stream for compatibility.

`results/full-battle-steward-once.json` records a seven-decision-turn natural
`Level1BattleJunker` battle, starting with one Steward play. The native game won
with Pyre health 49/80. The independent model matches all 55 room stages, 13
card cycling operations, 14 train phases, 11 wave spawning operations, and seven
EndTurns. Some matched stages are empty rooms; the fixture also includes armor,
spikes, unit death, card generation, treasure escape, and the terminal Pyre boss
fight. There are zero unsupported transitions in this fixture.

`results/full-battle-no-cards.json` is a no-card control with 52 room stages and
the same counts for the other phases. It also reaches native victory with Pyre
health 49/80, with zero differences or unsupported transitions.

The full-chain check supplies the simulator only the first EndTurn input. Every
following decision state and the stopping point come from the independent model;
it never injects later native states, recorded predictions, or a native terminal
turn count. The checker then compares all model decisions and terminal state to
the oracle, and repeats each complete simulation in 16 parallel branches. The Steward fixture
starts after its initial summon has completed; later actions are all EndTurn.

`results/full-battle-units-and-junk.json` adds 11 card plays and 61 room stages.
The policy summons multiple units on different floors, inserts at selected player
positions, and plays generated self-purging junk. It reaches victory at Pyre 60/80
with zero unsupported stages or differences. Its independent chain starts before
the first play and selects every following action from its own state, without
injecting recorded actions or later native states. Every action and EndTurn is
compared to the oracle, with 16 parallel runs and a separate mid-battle root.

`results/full-battle-units-spells-and-junk.json` adds targeted spells to the policy.
All 21 plays, 60 room stages, 13 card-cycle operations, 14 train phases, 11 spawns
and seven EndTurns match the game with zero unsupported stages. The independent
policy reaches native victory at Pyre 73/80, including complete intermediate
comparisons, a mid-battle suffix and 16 parallel branches. It applies the two
starting spells' complete effect sequences: damage followed by pyregel, and
moving the target to the front followed by valor. A killed last target receives
no follow-up status. Valor grants attack immediately and replenishes front armor
after the whole room resolves, once per room resolution; deployment skips the
armor effect. Unit death card routing follows actual combat event order.

`results/full-battle-statistics.json.gz` repeats that policy with schema 5 and
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

`results/full-battle-numeric-upgrades.json.gz` adds schema 6 card instance state.
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

`results/card-modifier-calibration.json.gz` contains 256 native calculations
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

`results/full-battle-dynamic-upgrades.json.gz` changes the runtime effect list of
the owned floor-rearranging spell inside an isolated native process. Its effects
compose permanent upgrades, repeated temporary upgrades and removal, a unique
upgrade, until-death unhealed health, and room-capacity restrictions before the
original rearrange/valor effects. The starting Boss and waves remain native.
All 18 plays, five EndTurns, 44 room stages, ten card cycles, ten train phases and
nine spawns match, finishing at Pyre 80/80. Four plays actually execute the modified
spell; coverage requires both a play and observed native upgrade state.

`results/full-battle-sacrifice-upgrades.json.gz` adds a max-health reduction that
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

`results/full-battle-hand-upgrades.json.gz` modifies the owned floor-rearranging
spell into a targetless hand-upgrade spell. `results/full-battle-targeted-hand-upgrades.json.gz`
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

`results/rng-calibration.json` captures six seeds and both RNG values and states,
including signed bounds, the full signed integer span, equal bounds, and reversed
bounds. Sampling restored Unity's process RNG and did not advance the game's
HadesRNG streams. Integer compatibility is verified for this installed build;
floating-point draws are not implemented yet.

`results/gold-reward-calibration.json.gz` captures 2,200 calls to the native
gold adjustment function. It covers reward and ordinary balance changes, negative
and zero values, four rounding increments and single-precision boundaries.
For a supported battle's standard rules, positive rewards are floored through
single precision, rounded to the nearest multiple of five with ties to even,
and raised to at least five. Ordinary balance changes are unchanged. Preview
effects leave gold unchanged. Unit effect values retain their unadjusted reward;
only generated-card effects retain a pile destination. Relic and mutator reward
modifiers remain explicitly unsupported.

`results/full-battle-healing.json.gz` adds a controlled healing scenario to the
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

`results/full-battle-healing-triggers.json.gz` adds repeated, once-only and
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

`results/full-battle-room-spells.json.gz` replaces the owned rearrangement spell's
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

`results/full-battle-terminal-spells.json.gz` changes the owned rearrangement
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

`results/full-battle-post-kill-spells.json.gz` replaces the owned rearrangement
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

`results/full-battle-random-spells.json.gz` replaces the owned rearrangement spell
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

`results/full-battle-random-status.json.gz` changes the owned rearrangement spell
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

`results/full-battle-cross-room-spells.json.gz` and
`results/full-battle-cross-room-targets.json.gz` replace the owned rearrangement
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

`results/full-battle-attack-buffs.json.gz` replaces the owned rearrangement spell
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

`results/full-battle-max-health-spells.json.gz` adds global unhealed health,
positive/negative/zero maximum-health buffs with both lifetimes and direct
enemy/friendly maximum-health debuffs to the owned rearrangement spell. Two
owned Stewards carry healing multiplier/immunity and a once-only OnHeal reward
that these buffs must leave unfired. The natural boss and waves are retained.
All 18 plays, six EndTurns, 60 room stages, 11 card cycles, 11 train phases and
nine spawns match the game. Seven modified plays cover 12 remote friendly
changes, three sacrifices, seven immune heals and five multiplier heals. The
independent root policy, mid-battle suffix and 16 parallel branches reach the
matching terminal Pyre health 80/80.

`results/full-battle-max-health-lethal.json.gz` uses lethal enemy maximum-health
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
Life-link, horde, conditional sacrifice/OnHealed effects, range/scaling rules,
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
spawner routing use all rooms. The 22 saved native battle oracles exercise this
engine through the normal card-action path, including cross-room target modes.

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
additional upgrade internals, statistic-driven scaling and specialized sacrifice mechanics, relics, equipment, room effects, additional statuses and
triggers, boss actions/companions/final bosses, and resurrection are not complete.
Actions now share a state with turn transitions and work at arbitrary decision
turns within the supported rules. Trait/trigger upgrades, statistic-driven effects, equipment
and additional effects are the next steps. Exact pruning still requires
broader state coverage; these fixtures do not establish a simulator for every
Monster Train 2 battle.
