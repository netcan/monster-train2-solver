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
| Runtime unit upgrades | `UnitModifierModel` | Native permanent, battle and unit-death lifetimes, duplicate removal, unique upgrades, restricted size, unhealed health and lethal max-health loss |
| Hand upgrade spells | `HandUpgradeModel` | Native targeted and targetless sequences, current-hand membership, permanent/temporary groups, uniqueness and paid-card exclusion |
| Basic healing | `HealingModel` and `CardSpellModel` | Native targeted spells, modifier group clamps, maximum health, multiplier/immunity, regen and lifesteal; independent healability checks |
| Healing triggers | `CombatTrigger` and `RoomCombatModel` | Native repeated/once rewards and silence; zero/full/immune healing, deployment timing, preview flags and child isolation checks |
| Terminal spell resolution | `BattleActionModel` | Native boss-killing spell skips later played/discard callbacks, retains played cost and prunes temporary-card statistics |
| Enemy waves and treasure | `EnemySpawningModel` | Native group cache, spawn order, phase, slots and treasure floor selection |
| EndTurn to next decision | `BattleTurnModel.EndTurn` | Seven native consecutive transitions in each supported fixture |
| Battle to terminal result | `BattleSimulator.Resolve` and `ResolveNoMoreCards` | Independent card policies and seven-turn chains, mid-battle inputs and 16 parallel branches |
| Additional spells, abilities, relics | Not complete | Unsupported interactions explicitly reject the model transition |

The game oracle is build 2.2.1, Assembly-CSharp MVID
`8fb07b96-f4db-4d2b-884d-c00536d6ccf4`.

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

The dynamic fixture finishes when a damage spell kills the Boss. Native battle
stop skips the later played/discard callbacks, retains the live played-cost
entry, and refreshes statistic membership from the permanent deck after clearing
runtime cards. The model reproduces that ordering and rejects terminal card
resolution when permanent deck membership was not captured.

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
Multi-unit Room/LastTargeted sequences remain explicitly unsupported.

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
metadata cannot model OnHeal. A
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
All 18 plays, six EndTurns, 54 room stages, 11 card cycles, 11 train phases and
nine spawns match the game. Eight area spell plays include seven multi-unit
heals, one multi-enemy damage play and five surviving enemy follow-up collections.
The independent root policy, mid-battle suffix and 16 parallel branches match
every decision and terminal Pyre health 80/80.

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
filters, random targets and untested-first effects requiring uncaptured prior
target history remain unsupported. Rearrangement still requires a drop target.

The current terminal oracle samples the entry to native `StopCombat`. Its
`StopCombatLoop` subsequently waits for the currently resolving card effect.
A controlled room-spell run exposed an intermediate terminal snapshot with only
part of a post-kill damage effect executed. Terminal spells with remaining live
targets or effective follow-ups are therefore explicitly rejected. Capturing a
settled terminal state and modeling remaining post-kill effects is separate work;
this fixture does not establish that behavior. Dead-only follow-ups are supported.

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
```

The runner checks the explicit probe success marker, capture failures, pending
records, and differences; a game's process exit code alone does not prove the
probe passed. It hashes original profile files before and after the run. The
checked native runs left those files unchanged. Game copies, isolated profiles,
and locally decompiled reference material are ignored by Git.

## Requirements for completion

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
