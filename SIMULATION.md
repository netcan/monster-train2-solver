# Full battle simulation

Fixed native regression inputs live in `tests/fixtures/` and are read by
`scripts/Check-Models.ps1`. Historical `results/` paths below refer to local
investigation logs, catalogs and benchmarks; that output directory is ignored.

The objective is an independent simulator for a complete Monster Train 2 battle
from a captured player decision state. The objective is still in progress.
Running the native game to a terminal result is an oracle, not proof that the
independent simulator can finish a battle.

## Native scenario construction

`Run-FullBattleProbe.ps1` launches the sandbox game with a fresh copy of the
isolated profile. The probe redirects the save root before game initialization
and verifies the original profile's file signatures afterward. Audio is muted
and the native game uses Instant timing. `NativeReplayScenario` sets seed
424242, starts a new run, and enters the first battle through the game's own
map and battle-intro flow.

Baseline captures keep the ordinary battle. Mechanism fixtures then modify
runtime CardData, upgrades or character triggers at deployment turn zero,
reinitialize affected card instances, and keep the original Boss and waves.
Companion-Boss fixtures additionally move the same Boss container to the second
wave, preserving its attack/health and every ordinary enemy's original wave.
Sentry fixtures change the original Boss's looping/relentless flags and add
movement/arrival callbacks, plus Steward health/size/status upgrades; Boss stats
and all spawn waves stay intact.
The automated policy selects legal actions and invokes native card play and
EndTurn. The probe observes actual state and callback boundaries, including
private counters and the native ordered signal listeners. Specialized setup
may invoke native effects directly to reach otherwise rare starting states;
for example, applying one future-draw effect twice creates two real callbacks
that share its private counter.

The recording is state/event data, rather than a video. Accepted `.mt2f`
archives preserve definitions, starting and intermediate states, actions,
RNG and observed results. Independent checks recompute the same operations
and compare actual captured values, without trusting embedded predictions.
Curated archives require native and independent checks to pass. These authored
fixtures prove the observed paths, not every original deck, relic or Boss.

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
| Future draws and bonus upgrades | `BonusDrawModel` and `CardCycleModel` | 125 exact native contexts, shared counters and ordered duplicate listeners, signed/ranged quantities, zero-draw cancellation, capped hands and complete ordinary/lethal policies with parallel branches |
| Dynamic room capacities | `RoomCapacityModel`, `BattleActionModel` and `UnitModifierModel` | 220 exact contexts and 2,050 native tests; both team groups, occupied shrinking, bounds/wrap, ignored ranges, statistic-driven quantities, unit triggers, live summons/restricted upgrades and complete policies with parallel branches |
| Spell draws and hand cycling | `CardCycleModel.DrawCards` and `CardSpellModel` | Signed/zero/max counts, full-hand cast timing, resolving-card exclusion, reshuffle RNG, draw statistics and live membership for later hand upgrades; ranged tests require explicit UI RNG isolation |
| Spell hand discard and consumption | `HandRemovalModel` and `CardSpellModel` | Forward order, resolving-card exclusion, retained buffer aliases, discard-only upgrade removal, double exhaustion statistics, per-effect counters and nested dead-spawner returns |
| Modified card generation | `CardGenerationModel`, `CardSpellModel` and `CombatTrigger` | Five destinations, pool/full-hand/duplicate timing, initial upgrades, modifier copies/exclusions, one-shot upgrades and fresh card effect state |
| Room attack exchange | `RoomCombatModel.Exchange` | Ordered initiative, target selection, retargeting, shield/armor and retaliation checks |
| Entire room resolution | `RoomCombatModel.Resolve` | Native normal exchanges, post-combat effects and multiple rounds of boss/Pyre relentless combat |
| Train combat phase | `TrainCombatModel.ResolveCombat` | Top-to-bottom native phase comparison |
| Enemy movement phase | `TrainCombatModel.Ascend` | Native movement and immediate Pyre combat, including the terminal boss fight |
| Bump card movement | `BumpModel`, `CardSpellModel` and `HordeMergeModel.MergeAcrossRooms` | Eighteen real paid-card effect/queue comparisons, both-team cross-room Horde merges, signed/clamped/fixed-range quantities, immobility/rooting, loops, simultaneous targets, Pyre and full/partially blocked rooms |
| Ordinary unit clone APIs | `UnitCloneModel`, `CharacterCopyModel` and `UnitBirthModel` | Thirteen native operations, 44 exact copied-card/birth/gear/stats/ability boundaries, raw/drained train states and callback payload/order; detached/cardless sources, failed allocations and 32 branches |
| Paid unit copy spells | `UnitCopyModel`, `UnitCloneModel` and `CardSpellModel` | Fourteen real paid effects, complete raw state and incoming/outgoing queue payload/order, sequential births, full/partial rooms, copied equipment/skills, cardless/Horde sources and 32 branches |
| Enemy and mixed-team copy spells | `HeroUnitBirthModel`, `UnitCopyModel` and `CardSpellModel` | Sixteen real paid effects, raw/live stats, natural skills, optional equipment, initial hero compaction, full/partial rooms, incoming global callbacks and 32 branches; subsequent paid summon and complete first-turn defeat |
| Spawn enchant callbacks | `BattleActionModel`, `UnitBirthModel` and `RoomCombatModel` | Ordinary/repeated paid births retain IsSpawning through player-wide enchant and child queues; fifteen native Horde/armor phases, complete source caches and nine-field child payloads in 32 branches |
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
| Current and future energy effects | `EnergyModel`, `CardSpellModel` and `RoomCombatModel` | Native spell/unit-trigger applications, caps, signed/ranged quantities, phase gates, current/next/persistent income and post-boss gates; complete policies with parallel branches |
| X-cost payments | `BattleActionModel` and `CardPlayRule.CostType` | Zero/current-energy payment, independent fixed/X modifiers, 96 exact paid-cost scaling callbacks, redrawn cards, post-payment gains and terminal Boss kill; complete policies with parallel branches |
| Healing triggers | `CombatTrigger` and `RoomCombatModel` | Native repeated/once rewards and silence; zero/full/immune healing, deployment timing, preview flags and child isolation checks |
| Healing effects on unit triggers | `CombatEffect.Action`, `HealingModel` and `RoomCombatModel` | Native self/room/healable/random targets, per-group ranges, negative/zero amounts, empty-room sampling, source-card independence and deferred OnHeal upgrades |
| Damage effects on unit triggers | `CombatEffect.Action` and `RoomCombatModel` | Native quantity tests/samples, source-card attribution, status multipliers, defenses, death FIFO and deferred spawner exhaustion |
| Revenge and Slay triggers | `CombatTrigger.TriggerAtThreshold` and `RoomCombatModel` | Native blocked/lethal HP damage, thresholds, once/silence, dead-boss suppression, sweep callback timing and complete unit turns |
| Unit post-combat healing | `RoomCombatModel.ApplyUnitPostCombat` and `UnitHealerModel` | Native per-actor healing/ordinary order, attack/trigger prevention, once/silence, changing healer quantities and complete phase/decision states |
| Terminal spell resolution | `CardSpellModel` and `BattleActionModel` | Settled native boss kill continues live effects, detached spawner upgrades/removal and healing; effect gates skip/cancel, then played/discard callbacks complete |
| Enemy waves and treasure | `EnemySpawningModel` | Native group cache, spawn order, phase, slots and treasure floor selection |
| EndTurn to next decision | `BattleTurnModel.EndTurn` | Seven native consecutive transitions in each supported fixture |
| Battle to terminal result | `BattleSimulator.Resolve` and `ResolveNoMoreCards` | Independent card policies and seven-turn chains, mid-battle inputs and 16 parallel branches |
| Single companion Boss transition | `CompanionBossModel` and `TrainCombatModel` | Wave exhaustion, forced looping, ordered movement/status/removal callbacks, raw/canonical phase boundaries and both no-card/card policies |
| Unit arrival Sentry queues | `TrainCombatModel` and `RoomCombatModel` | Physical guard order, moved-target overrides, retained dead victims, once/silence/threshold gates and terminal hit/death queues |
| Unit ability cooldown state | `AbilityCooldownModel`, `CardSpellModel` and `RoomCombatModel` | Native natural-ability spawn, separate configured/current cooldowns, signed/absolute updates, resets, status decay and availability-marker callbacks; complete battle and parallel branches |
| Shared ability card cache | `AbilityCardModel` and `CombatContext.AbilityCardCache` | Native player and enemy ability creation, shared unit references, detached card statistics, starting upgrades and allocation before OnSpawn generation; complete battle and parallel branches |
| Unit ability activation | `UnitAbilityModel` and `BattleActionModel` | Native fixed/X/zero payments, shared detached history, self targets, damage attribution, pre-own/own callbacks, cooldown and direct Boss-kill settlement; complete policies and parallel branches |
| Mono status dictionary slot reuse | `StatusDictionaryState` | Captured slot/free-list state, LIFO reuse after cleanup, exact later status enumeration and independent branch isolation |
| Unit ability lifecycle primitives | `AbilityLifecycleModel` and spawn transitions | 21 native assignment/removal API cases, raw cooldown restoration, equipment overlay history, permanent disable order and disabled player/enemy births; complete subsequent policy and parallel branches |
| Ability assignment/removal effects | `CardSpellModel`, `RoomCombatModel` and `AbilityLifecycleModel` | 17 native effect states and 17 queued dispatches with 35 payloads; multi-target and last-target spells, pre-own replacement, cached self replacement/removal, exact current disabled IDs and complete policies with parallel branches |
| Ability upgrades and equipment grants | `CardUpgradeModifier`, `UnitModifierModel` and spawn transitions | Permanent/temporary initial selection, keep-existing and matching-removal gates, raw restoration after repeated equipment replacement, direct assignment clearing history, disabled upgraded births and real equipment skill casts; complete policy and parallel branches |
| Horde numerical primitives | `HordeStatModel` | 560 native raw-stat steps and 175 casualty boundaries, signed overflow, HP/stack caps and 32 branches |
| Persistent enchantment lifecycle primitive | `EnchantmentLifecycleModel` | 1,460 native target-map/RNG transitions and 644 exact status API requests, primary/preview retention and 32 branches; status mutation, reentry and automatic combat integration remain open |
| Horde status changes and casualties | `HordeStatusModel`, status/spawn/damage/health transitions | Eight exact native operations, accepted queues and complete drains, zero/negative notifications, simulated deaths, troop thresholds and spawning cooldown gates; complete subsequent battle and parallel branches |
| Horde runtime unit upgrades | `UnitModifierModel` and `HordeStatusModel` | 16 exact API/queued operations, ordered healed/unhealed/attributed HP casualties, first-stack reset, raw final removal and lethal sacrifice; 40 death/Harvest phases, four paid spell chains and complete policies with parallel branches |
| Dying Horde unit upgrades | `UnitModifierModel` and `HordeStatusModel` | Five complete deaths, seven dying upgrade/removal effects, 45 exact death/Harvest phases, accepted callback counts and source-card writes; ordinary/unhealed/attributed HP, early exits and 32 branches |
| Paid summon and Horde Rally | `CardPlayedTriggerModel`, `BattleActionModel` and `RoomCombatModel` | Five exact Horde operations, ten post-play team phases, twelve repeated dispatches, cached room membership, last-spawned timing/overrides, silence/once/conditions and complete policies with parallel branches |
| Terminal summon and lethal Rally | `BattleActionModel` and `RoomCombatModel` | Twelve native team phases and six dispatches, deferred Boss self-death, post-kill permanent/unit-death upgrades, rewards, a surviving last-spawned reference and fresh Standby routing; complete policy with parallel branches |
| Shared unit identity allocation | `UnitIdentityModel` and `CombatContext.NextUnitId` | Player/enemy/treasure allocation, all context copies, stable death/terminal counters, 98 native spawn/decision boundaries and complete policies with parallel branches |
| Repeated paid summons and detached sources | `UnitBirthModel`, `UnitSummonModel` and `CardGenerationModel.CloneDetached` | Ten complete native births, five detached card copies, ten paid phases and 26 Rally dispatches; live source upgrades, retained clone flags, cardless/source separation, physical slot limits, capacity overflow and parallel branches |
| Fresh and additional summon sources | `UnitSummonRule`, `UnitSummonChoice` and `UnitStandbyModel` | Native fresh setup, original-card bindings, delayed returns, mixed unit templates, odd capped splits and source-mismatch upgrade routing; complete policies and parallel branches |
| Pooled unit summons | `UnitSummonRule.Pool` and `UnitSummonModel` | Ordered duplicate weights, singleton RNG draws, additional overrides, fresh/copied sources and no-primary paid Rally gates; five native scenes and parallel branches |
| Missing summon sources | `FallbackLookupComplete`, `UnitBirthModel` and `UnitStandbyModel` | Confirmed no-match fallbacks, first-birth cardless markers, no-source spawn triggers/upgrades, mixed sources and delayed original-card returns; four native scenes and parallel branches |
| Physical spawn points | `SpawnPointModel` | 21 exact native operations and 63 current/last-known queries, holes, stale occupant references, zero-HP/undying occupancy, preview-born removal, cross-room moves and 32 branches; integrated battle paths are listed below |
| Physical positions in complete battle states | `CombatContext.SpawnPoints`, `BattleSpawnPointModel` and battle transitions | Two native full battles, 204 room contexts, 30 removals and 17 cross-room moves; complete roots in 16 branches and rooms in 32 branches |
| Status removal and Horde sacrifice | `StatusRemovalModel`, `CardSpellModel` and `RoomCombatModel` | Nine native API/effect/trigger operations, exact room/retained actor states, accepted queue counts, ordered death/Harvest dispatches, a real paid spell removing both teams and parallel branches; raw zero HP preserves orphan standby cards while sacrifice signals physical death and retains its responsible card |
| Reentrant death signals and queued player sacrifice | `UnitDeathState`, `StatusRemovalModel` and `RoomCombatModel` | Three native operations, 45 exact death/Harvest phase states and complete dispatch order, 95 effect/retained-target states, pending versus cleared statistics listeners, spawner timing and parallel branches |
| Physical death Harvest | `HarvestModel` and `RoomCombatModel` | Four native physical deaths and 31 exact dispatches; own-death children precede player/enemy groups, Hero/Monster/Unit kinds, Horde repetition, required dying statuses, silence and once flags; complete subsequent battle and parallel branches |
| Undying revival | `RoomCombatModel.ApplyRevival` and damage/sacrifice settlement | Eleven native revival boundaries, seven direct/removal operations, ninety callback phases, both teams, last/zero stacks, queued self damage, one-time statistics listeners and complete policies with parallel branches |
| Equipment-owned summons | `RoomCombatModel`, `UnitBirthModel` and `BattlePreviewModel` | Native equipment binding is separate from the host source card; individual summon effects compare complete states, first-birth caches and accepted callback FIFO, including physical Rally order and retained death sources |
| Queued trigger repetition | `RoomCombatModel` | Eight complete native batches and 26 dispatches, once flags, zero/negative counts, ordered child callbacks, silence/fire permissions and 32 branches; Horde status callers now preserve rally/harvest repetition payloads |
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

Ordinary callbacks drain the shared FIFO before damage deaths enter unit removal.
Damage deaths update death counters inline, but their OnDeath effects and spawner
return/exhaustion wait for that removal stage. Later generated cards therefore
receive the exhaustion event. Upgrade deaths retain their previously verified
inline settlement. Pure checks cover these differences, late generation,
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

Schema 37 verifies the separate damage-death removal stage with
`-DamageDeathQueue`. A PreCombat effect kills a one-health enemy, then heals its
actor for zero. The queued OnHeal grants seven armor before the enemy's OnDeath
deals three damage. Native requests independently witness seven then four armor
blocking the damage with no health loss. Treating OnDeath as an ordinary queued
callback instead costs three health in the first phase.

Removal snapshots enemies before players, each in active creation order, and
marks the complete initial batch before running any of its callbacks. Newly
killed units during OnDeath are removed by the nested queue before the current
parent's spawner returns; already-marked units stay in the original batch.
Both the room engine and train dispatcher implement this staging. Pure checks
cover ordinary callback ordering, reversed physical target order, nested return
order, death-generated cards receiving exhaustion, and 32 isolated parallel
branches. Boss terminal OnDeath damage is kept separate here; the exploratory
kill-camera clearing discrepancy is covered by the Schema 38 fixture below.

`tests/fixtures/full-battle-damage-death-queue.json.gz` retains the unmodified
complete native battle: 15 plays, 5 EndTurns, 41 room stages, 7 spawns, 8
pre-combat phases, 8 generation effects, 16 upgrade callbacks, 8 zero-heal
observations and 39 damage quantity observations (26 tests, 13 applications,
17 requests and 5 OnDeath applications). The game is 2.2.1 with module MVID
`8fb07b96-f4db-4d2b-884d-c00536d6ccf4`. Raw JSON is 183,673,530 bytes,
SHA-256 `e2cd0c7ceaa0d1bfad28fd4c5491c01a108cb9819788301ef4ecbb5bcaabeb7f`;
the byte-identical gzip is 6,072,874 bytes. Native capture failures, mismatches,
unsupported phases and pending records are zero; the original profile is
unchanged and Pyre health at victory is 80.
Independent phase/hit checks, complete initial and mid-battle policies and 16
parallel branches pass. All 49 earlier complete battles and 8 calibration
fixtures also pass; the curated corpus now retains 50 complete battles.

Schema 38 adds the captured `KillCamActivated` gate and `-TerminalDeathDamage`.
Native boss-kill preview can start the kill camera before the real ordinary
attack. Other damage enters the same camera through CheckForDeath before death
signals. The camera clears active cards and all piles once, while retaining
card references and existing statistic cache entries. Subsequent death
statistics run before OnDeath effects. A null responsible card has no native
IncrementStat call and must not refresh the cache; an attributed death after
clearing refreshes it against the permanent deck before incrementing source
and global counters. Detached spawners no longer present in standby do not
return or produce a TimesExhausted/AnyExhausted event.

The model handles both camera entry points and carries its gate through card,
room and turn transitions. The dying actor still resolves traits through its
retained spawner reference. Older captures that omitted the identity store
retain observed source-card metadata while the current operation settles,
without adding unobserved registry fields to their output. Pure checks cover
death-statistic scaling inside OnDeath, permanent/generated kill attribution,
unattributed cache preservation, detached spawners, one-shot clearing, preview
isolation and 32 parallel branches.

`tests/fixtures/full-battle-terminal-death-damage.json.gz` retains the complete
unchanged native battle. Its two camera observations show preview activation
clearing 23 owned cards with no statistic change, then the real death calling
the already-active camera. Nine exact death-signal observations include the
terminal boss and a friendly unit killed by its OnDeath damage. That friendly
death refreshes cached membership from 23 to 15 permanent cards, increments
death counters, and produces no spawner exhaustion. Independent checks compare
every captured death statistic and verify damage runs after cards clear.
The complete battle has 15 plays, 5 EndTurns, 41 room stages, 7 spawns, 8
pre-combat phases, 8 card generations, 16 upgrade callbacks, 8 zero heals and
42 damage samples (28 tests, 14 applications, 19 requests, 6 OnDeath effects).
Initial and mid-battle policies, including 16 parallel branches, reproduce
victory with final Pyre health 80.

The game is 2.2.1, module MVID `8fb07b96-f4db-4d2b-884d-c00536d6ccf4`.
Raw JSON is 186,797,283 bytes, SHA-256
`8ce969f2d5a651dd10bb8dcf9d6cccc0391c6fb80591fb47da175a3363fce411`;
the byte-identical gzip is 6,192,686 bytes. Capture failures, mismatches,
unsupported phases and pending records are zero, and the original profile
is unchanged.
The complete curated check script passes all 51 battle fixtures and 8
calibrations. Combat-event triggers, equipment/room/relic effects, specialized
boss behavior and resurrection still leave the full objective open.

Schema 39 adds `-HitKill`, captured trigger thresholds and native OnHit/OnKill
queue/execution observations. Armor and shield blocking still queue OnHit;
the threshold receives damage after blocking, which can be zero. A request
with neither positive damage nor blocking queues no OnHit. Positive thresholds
gate that argument; zero and negative thresholds are inactive. Ordinary dying
characters can fire OnHit and OnKill, while dead minibosses/outer bosses skip
both. Healing an actor at zero HP does not revive it.

OnKill is queued before lifesteal and spikes for damage attributed to a
character attacker. A sweep fixes its target list and postpones all callbacks
until every target has been hit. Pending callbacks use the actor's current
health and once flags, even when it has died in the meantime. Ordinary queue
entries drain before the deferred death-removal batch. Boss-kill previews
preserve the running-queue gate during sweep, so preview callbacks do not heal
or damage between targets. Input states remain immutable and worker branches
carry independent trigger queues.

Pure checks include blocked/zero/lethal hits, HP-damage thresholds, ordinary
Slay/lifesteal/spikes ordering, dying Slay actors, deferred sweep healing,
global phase dispatch, once/silence/deployment/preview gates and 32 parallel
branches. Required-status/equipment trigger gates remain explicitly
unsupported. At schema 39, self upgrades on OnHit/OnKill/OnDeath still reject
dying-unit settlement; schema 40 below closes that gap. Positive thresholds
on other character trigger kinds still reject the transition.

`tests/fixtures/full-battle-hit-kill.json.gz` retains the unchanged native trace:
18 plays, 6 EndTurns, 50 room stages, 9 spawns, 11 train phases, 11 card cycles,
56 independently reproduced unit turns and 10 observed multi-target sweeps.
Its 162 queue/fire observations include 81 compared trigger dispatches: 17 blocked
and 8 lethal ordinary OnHit callbacks (6 rewarded), 9 OnKill callbacks (5 rewarded), 10 positive
threshold passes, and one dead-boss OnHit that fires no effects. Reward deltas
and every once flag are independently recomputed from captured native inputs.
The entire policy from initial and mid-battle states, including 16 parallel
branches, reproduces victory with final Pyre health 80.

The game is 2.2.1, module MVID `8fb07b96-f4db-4d2b-884d-c00536d6ccf4`.
Raw JSON is 228,467,200 bytes, SHA-256
`e9f6666a51a1894ffa8f7d8b7d832861a88d307998a82cbbdf787dee43f46cf3`;
the byte-identical gzip is 7,306,978 bytes. Native capture failures, mismatches,
unsupported phases and pending records are zero; original profile files are
unchanged. The isolated game is automatically muted during this capture.
The complete curated check script passes all 52 battle fixtures and 8
calibrations. Additional combat triggers, dying-unit upgrades, equipment,
relics, room effects, specialized bosses and resurrection remain open.

Schema 40 adds `-DyingUpgrades` and native upgrade snapshots that retain the
zero-HP target alongside the living room. OnHit, OnKill and OnDeath self
upgrades now use the existing actor and queue rather than starting a new room
engine. Public room inputs still require living units; the internal exception
is restricted to the dying target of that one upgrade application.

Native upgrades change their ledger, attack, size and healer quantity before
health. Positive health changes can increase a dying actor's maximum HP and
continue to status/source-card updates, but never revive it. A negative health
or unhealed-health stage stops if the actor is dead: preceding changes remain,
while later stages and source-card additions are skipped. Already-dead actors
never repeat sacrifice, death counters or source-card exhaustion. Removal
still processes every matching unit copy and clears all matching temporary
source-card upgrades, even when individual removals exit at their HP stage.
Permanent, battle and unit-only lifetimes, unique no-ops, clone exclusions,
preview suppression and retained source cards after terminal clearing keep
their native behavior.

The native sample also corrects the old assumption that a sweep attacker at
zero HP deals zero damage to later targets. Native combat checks destruction,
which is postponed until the sweep group finishes. The dying actor retains
its attack and consumes remaining lifesteal stacks without healing; spikes
require a living attacker and no longer retaliate. Queued callbacks then see
the current dying actor. This affects damage, kills, gold and the shared last
attack-damage statistic in live combat and previews.

`tests/fixtures/full-battle-dying-upgrades.json.gz` retains the unchanged
native battle: 21 plays, 7 EndTurns, 57 room stages, 11 spawns, 14 train phases,
13 card cycles, 47 independently reproduced unit turns and 2 multi-target
sweeps. Of 53 upgrade observations, 17 capture zero-HP targets: 5 Slay,
4 Revenge and 8 death applications, including 2 duplicate removals,
2 failed negative-health stages, 2 failed negative-unhealed stages and
2 unit-only upgrades. Health scaling is removed from this authored scenario
so the negative-health case stays negative; attack scaling and the natural
waves/boss are retained. The independent checks compare full dying-unit and
source-card contexts. Initial/mid-battle policies and 16 parallel branches
reproduce victory with final Pyre health 73; pure checks include 32 independent
branches, terminal retained spawners and global queued callbacks.

The game is 2.2.1, module MVID `8fb07b96-f4db-4d2b-884d-c00536d6ccf4`.
Raw JSON is 368,944,511 bytes, SHA-256
`5fcac7be693f6169edb939d0fb848440252a2c635ca8f1cc5560301e14754085`;
the byte-identical gzip is 10,340,268 bytes. Native capture failures,
mismatches, unsupported phases and pending records are zero; original
profile files are unchanged and the isolated game is automatically muted.
The complete curated check script passes all 53 battle fixtures and 8
calibrations.
Other combat triggers, equipment, relics, room effects, specialized bosses
and resurrection still leave the full objective open.

Schema 41 adds `-AttackTriggers`, ordered native attacking observations and
nullable `CombatUnit.LastAttackerId` relationship state. Direct attacks queue
OnAttackingBeforeDamage after defenses and the resulting HP are computed,
but before the HP update. Pre-damage armor cannot retroactively block that
hit, pre-damage healing is overwritten by the cached HP, and attack changes
affect later attacks. OnAttacking runs after HP changes, before Slay,
lifesteal, retaliation and the victim's OnHit/death checks. Default effect
damage and spikes do not recursively fire attacking triggers. Splash and
trample generation remain unmodeled, although native uses these trigger kinds
for those damage types too.

Each queued attack callback retains its own victim override, including across
sweep and dying victims. LastAttackedCharacter applies that override without
team, health, status, subtype or boss filters. The separate trigger preflight
has no override and finds all allowed-team units in the room whose recorded
last attacker is the actor. These fallback victims also bypass other filters.
Zero denotes known absence; null preserves legacy captures that lack this
state and rejects fallback queries when they would need it. Damage updates
the relationship, with card-only damage and Pyre attackers clearing it; unit
copies, upgrades, spells and ascent preserve the relationship; new spawns
start with no attacker.

Ordinary dying attackers can mark a matching once flag, then abort the whole
trigger dispatch before applying effects; subsequent triggers in that dispatch
stay unmarked. Dead bosses skip the dispatch outright. The new attacking kinds
supply threshold argument zero, so positive thresholds never pass. Native
ProcessRemovals destroys removed character objects after next-turn statistics
and before advancing the turn, clearing retained attacker references. Terminal
combat skips this phase and retains those references. Room stages keep them
until that actual destruction boundary.

`tests/fixtures/full-battle-attack-triggers.json.gz` retains the unchanged
native battle: 21 plays, 7 EndTurns, 53 room stages, 11 spawns, 13 train phases,
13 card cycles, 46 independently reproduced unit turns and 2 sweeps. Its
104 queue/fire observations contain 52 compared dispatches, split 26 before
and 26 after damage, with 10 queued target overrides, 10 dying aborts,
5 positive-threshold skips, 11 once skips and 3 silence gates. All 85 upgrade
snapshots complete; 17 dying target/source contexts are independently compared.
Initial/mid-battle policies and 16 parallel branches reproduce victory with
final Pyre health 80. Pure checks include 32 parallel branches, cached damage
and HP, late armor/healing, nested damage, target filter bypass, victim history,
cross-floor live references, destruction, dying victims/actors and Pyre routing.

The game is 2.2.1, module MVID `8fb07b96-f4db-4d2b-884d-c00536d6ccf4`.
Raw JSON is 439,091,767 bytes, SHA-256
`6d4e7d32a46c97642b4d57e9cc093a900f16ccf5bdce60d34badc12f7211706b`;
the byte-identical gzip is 11,959,789 bytes. Native capture failures,
mismatches, unsupported phases and pending records are zero; original
profile files are unchanged and the isolated game is automatically muted.
The complete curated check script passes all 54 battle fixtures and 8
calibrations.
Further trigger kinds, statuses, equipment, relics, room effects, specialized
bosses and resurrection still leave the full objective open.

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


Schema 42 adds `-TriggeredStatus`, runtime/definition capture of
`CardEffectAddStatusEffect` on character triggers, `TriggeredStatuses` effect
observations, and captured room/team magic power in the shared combat context.
All context transitions preserve that power snapshot, including terminal card
clearing. Missing or duplicate power inputs are explicit unsupported results.

The trigger engine now applies a single selected status to the effect's fixed
collection in reverse order. It chooses the pool entry even for an empty
collection, then samples the chance range once. Chance zero bypasses per-target
rolls; negative, 100 and larger values still consume a roll for every target,
including an immune target. Tests for stackable pools use only collection
legality; randomized nonstackable strict legality remains unsupported because
it depends on the native BattleTest stream. Single nonstackable strict effects
check existing positive stacks, the effect's subtype and boss filter.

The stack multiplier sums the triggering character's selected status count,
the first collected target's missing health, and that target team's room magic
power, then multiplies the selected incoming stack count once for the entire
collection. Source-card status traits run afterwards per target, following
immunity checks; positive applied deltas update source-card statistics. Dying
actors and targets stay available through native callback settlement without
revival, and zero/negative resulting stacks are omitted from the projected
positive-status view.

This regression also exposed two independent corrections, committed separately:
`45fcde9` makes unit piercing conditional on a missing source damage card, as
native DamageHelper requires; `8b97f05` preserves current floor/front-to-back
movement metadata when new waves coexist with surviving enemies upstairs.
The first exploratory capture recomputes to all seven exact EndTurns after
those fixes. It is not part of the retained fixture list.

Nonzero room magic power, per-target statistic feedback, strict legality,
probability boundaries, retained dying objects, preview statistics, malformed
power inputs, and 32 parallel immutable branches are covered by pure checks.
The native fixture's supported rooms have zero magic power; this does not prove
room-modifier or relic mechanics. Duality remains unsupported through status
validation, and status-change callbacks such as OnArmorAdded/OnSilence and their
removal paths remain unimplemented. They must be added before full battle
simulation can be considered complete.


Random trigger effects also make the native preview's leaked attack statistic
history-dependent: vanilla previews consume BattleTest without starting from a
fresh gameplay-state copy. Schema 42 therefore explicitly records
`IsolatedBattlePreview` for the opt-in solver protocol. The native probe seeds
BattleTest from Battle for each whole battle preview and boss-kill preview,
then restores both original streams. Whole battle previews share their
transient stream across floors, including random test targets and both status
pool preflight tests. The pure model preserves the native attack-statistic
result while returning the unchanged live RNG and units. Independent checks
verify the native scope records, actual test-stream consumption and restoration
of both streams. This protocol is enabled by `-TriggeredStatus`; existing
captures retain their original preview semantics. Raw vanilla UI-driven preview
statistics for randomized unit triggers are not established by this protocol.


`tests/fixtures/full-battle-triggered-status.json.gz` retains the unchanged
native JSON from
`.probe-runs/full-battle-units-spells-and-junk-20261007-125305-01a92e59`:
620,302,405 raw bytes, 18,625,866 gzip bytes, SHA-256
`b2b9ec32e4545ea3b2763c6d8a85860b5181ba6839034aaef373b8152c7824bf`.
The battle has 21 plays, 7 EndTurns, 69 room stages, 11 spawns, 14 train phases,
13 card cycles and 77 individually verified unit turns. Its 157 exact status
effects include 6 pools, 6 empty collections, 4 ranges, 10 area collections,
10 retained dying observations, 2 combined multipliers, 39 attributed source
cards, 29 immune observations and 3 strict nonstackable applications, across
OnSpawn, PreCombat, OnAttacking, OnHit, OnDeath and OnTurnBegin. The 32 battle
preview and 46 boss-kill preview scopes restore both streams; 43 scopes consume
the isolated test stream. Native victory has Pyre 72, all capture failures,
mismatches, unsupported stages and pending observations are zero, and the
original game files remain unchanged. The debug game is automatically muted.
Independent effect/turn/action and complete policy-chain checks pass, including
a mid-battle root and 16 parallel policy branches.

The final schema-42 regression exits successfully with all 55 battle fixtures
and all eight calibration fixtures. Randomized triggered-status previews
without the explicit isolated RNG protocol now return Unsupported, preventing
an implicit approximation of vanilla UI preview history.

The retained corpus now uses version-one `.mt2f` binary value graphs. All 63
captures (55 battles and eight calibrations) preserve their complete captured
values, exact numeric lexemes, object property order, duplicate keys, array order
and nulls. Identical subtrees share immutable nodes; a string table and typed
numeric encodings avoid repeating state dumps. The binary reader constructs
model objects directly through cached constructor plans, without parsing or
reconstructing a JSON document. Native probe JSON remains available only as a
local import/diagnostic format in ignored output directories.

Migration independently compared every original value with its decoded binary
counterpart across 9,690,793,008 raw capture bytes. The previously retained
JSON/gzip inputs totaled 293,389,234 bytes; the 63 binary archives total
1,327,446 bytes, a 99.55% reduction. The triggered-status archive is 42,927 bytes
and retains the same source SHA-256 recorded above. `tests/fixtures/manifest.tsv`
records source provenance and full binary archive hashes, which the regression
script checks before running. Historical JSON/gzip paths and hashes above refer
to the original captures before migration; native capture schemas are unchanged.
Formatting whitespace and string escape spelling are not retained. The binary
format, numeric precision rules, import commands and corruption checks are
documented in `src/FixtureArchive/FORMAT.md`.

The complete regression using only the 63 binary fixture paths exits zero:
all 55 battle fixtures and all eight calibration fixtures pass, including the
157 triggered-status observations and mid-battle parallel policy branches.
Binary-format checks cover integer boundaries, exact decimal/exponent lexemes,
Unicode, duplicate properties, defaults, deterministic output, 32 parallel
hydrations and rejection of corrupt/truncated/unknown-version/cyclic archives.

Isolated full-battle probes now default to the game's native `Instant` timing
table. `-GameSpeed Normal` retains a baseline; Fast, Ultra and SuperUltra are
also selectable. `MT2_PROBE_FAST_REPLAY` only accelerates replayed prefixes and
does not accelerate directly driven battle sampling. The new
`MT2_PROBE_GAME_SPEED` override intercepts `SaveManager.GetActiveGameSpeed`,
preserves native preview/undo/replay Instant behavior, and does not write player
preferences or change `Time.timeScale`. Schema 43 records the requested speed
and the number of live overrides. All debug games remain muted.

A fresh Normal/Instant pair using `-Policy units-spells-and-junk
-TriggeredStatus` captures the same 21 plays, seven EndTurns and native victory
with Pyre 72. The native process elapsed time decreases from 155.79 seconds to
82.34 seconds, a 1.89x speedup on this scenario. These measurements include game
startup, sampling, export and shutdown, but exclude launcher postprocessing.
`FixtureTools compare-speeds` compares all 41 complete gameplay sections,
including snapshots, RNG streams, effects, callback order and checkpoints.
Frame-dependent UI query/preview observation counts are checked independently
for restoration and model equivalence rather than requiring identical counts.
The native captures are
`.probe-runs/full-battle-units-spells-and-junk-20261007-133518-be33d8ca` (Normal)
and `.probe-runs/full-battle-units-spells-and-junk-20261007-133253-1dd16f32`
(Instant). Both exit zero with no capture failures, mismatches, unsupported
stages or pending observations, and original game files remain unchanged.

The initial `tests/fixtures/full-battle-triggered-status-instant.mt2f` preserved
all captured values from the 620,310,073-byte Instant source (source SHA-256
`d4e68baf103d6562e84dbf7235def6c9241949ffbf63aea7ca9892434eae7dcd`). The archive
is 43,288 bytes with 7,146 unique nodes; its hash is recorded in the manifest.
The independently modeled native actions, turns and all 157 triggered-status
effects pass, including 36 battle/46 boss-kill preview scopes with 47 consuming
the isolated test stream. The complete binary regression now exits zero with
56 battle fixtures and eight calibration fixtures. Supplying a Normal capture
as the accelerated input to the speed comparator is also rejected.

Full-battle export now runs once in the scenario tick after the terminal native
coroutine has returned. Previously it also ran inside the stop wrapper, writing
the same large capture twice. Json.NET now streams compact UTF-8 to a temporary
file, then publishes the completed file, rather than allocating a complete
indented string. All capture fields remain present; schema 43 and the binary
fixture format are unchanged. The export log records duration, bytes and pending
observations.

The same triggered-status scenario now takes 74.00 seconds for the native
process, compared with 82.34 seconds before the export change and 155.79 seconds
at Normal speed. This is a further 10.1% reduction, or 2.11x compared with the
Normal capture on this scenario. The single export takes 4.927 seconds and
writes 237,681,273 bytes instead of approximately 620 MB of indented text. Total
launcher elapsed time is 102.36 seconds including build and postprocessing;
the previous native-process times must not be compared against this total.
The capture is
`.probe-runs/full-battle-units-spells-and-junk-20261007-134628-58167246`.
Complete comparison with the fresh Normal capture again passes all 41 gameplay
sections. Independent checks pass all 157 status effects, 77 unit turns,
33 battle/46 boss-kill preview scopes (45 consumed), the complete action/turn
chain and 16 parallel branches.

The retained Instant fixture now contains this streamed capture: 43,400 bytes,
7,146 unique nodes, source SHA-256
`750f6fc21fbdcf831158ad795a5e5ad59e6094361a53eb54bc90a7cde818365c`.
The manifest records its updated source length/hash and binary length/hash.
Independent import verification compares every captured value with the binary
archive. The original indented Instant capture remains available locally under
the earlier profile path.

An additional native `-TerminalDeathDamage` run at
`.probe-runs/full-battle-units-spells-and-junk-20261007-134825-c6338e02`
verifies the later export boundary against pending terminal effects: 15 plays,
five EndTurns, Pyre 80, one terminal-clear signal, one later death, one cache
drop and one lethal post-clear hit. Independent exact effect, death-queue,
terminal-signal, action/turn and parallel-policy checks pass. Its single export
takes 1.581 seconds with zero pending observations. Both native runs exit zero
without capture failures, mismatches or unsupported stages; original game files
remain unchanged and audio stays muted.

The final regression with the updated streamed binary fixture exits zero:
all 56 battle fixtures and all eight calibration fixtures pass. The curated
inventory and complete archive SHA-256 hashes are verified before model checks.

Schema 44 captures each unit's complete native status dictionary as
`StatusRegistry`, preserving insertion order and zero-stack entries. `Statuses`
remains the positive-stack combat view. Definitions include native visibility
and display category. Native presence queries count dictionary entries even
when their stacks are zero, so treating the active list as the dictionary would
produce incorrect arguments for `OnNewStatusEffectAdded`. Legacy captures have
a null registry; presence queries return unknown rather than approximating them
from active statuses.

Status application, damage consumption, upgrades, unit/card cloning, summons,
enemy creation, train movement and attacker-reference cleanup preserve registry
entries. Readding a zero-stack status reuses its original rule and insertion
position. Native end-of-turn cleanup removes a definition only when that status
is selected for decay and reaches zero; ordinary consumption retains it. The
relentless-cycle signature also includes retained definitions and their order,
preventing presence changes from being mistaken for an identical combat state.
Independent adversarial checks cover zero additions, consumed shields, hidden
and persistent definitions, zero-entry cleanup/readdition, missing legacy state
and 32 parallel branches without parent mutation.

The retained `tests/fixtures/full-battle-status-registry.mt2f` comes from
`.probe-runs/full-battle-units-spells-and-junk-20261007-140231-8021bb2d`.
The 249,937,719-byte complete capture becomes a 45,315-byte binary archive with
7,403 unique nodes; source SHA-256 is
`2df29089404a322c37120c124a1abf872ace8df3f0213c20beb495887352b35b`.
The manifest records both source and archive hashes, and import verification
compares every captured value. All 21 plays, seven EndTurns, 69 room stages,
77 unit turns and 157 triggered-status effects match the native game, ending
with victory and Pyre 72. Direct native query calibration verifies 128 distinct
unit/dictionary snapshots, 51 with zero entries, including 64 visible zero
entries and six hidden definitions. The 34 battle/46 boss-kill preview scopes
restore both streams, with 46 consuming the isolated test stream.

An additional `-DyingUpgrades` capture at
`.probe-runs/full-battle-units-spells-and-junk-20261007-140501-125877f3`
independently verifies 17 dying unit/source contexts, including five Slay,
four Revenge and eight death effects, two removals, both failed HP stages and
two unit-only upgrade lifetimes. All 21 plays, seven EndTurns and 47 unit turns
match, ending at Pyre 73. Its 78 native presence queries include 49 zero-entry
snapshots, 58 visible zero entries and 23 hidden definitions. Both native runs
exit zero with no capture failures, mismatches, unsupported stages or pending
observations; original game files remain unchanged and audio stays muted.

The complete binary regression exits zero with 57 battle fixtures and eight
calibration fixtures. This completes the status-presence prerequisite; queued
status-change callbacks and their payloads still need implementation before the
whole battle simulator is complete.

Full-battle sampling now exports a typed `.mt2f` graph directly by default.
It uses the same Json.NET contracts and scalar spellings as the diagnostic
export, but visits shared objects once, caches integer/string nodes and builds
property plans once per captured type. The archive library targets both Unity's
netstandard2.1 runtime and the net8.0 independent tools. Explicit null hashing
handles Mono's different `HashCode.Add(value, comparer)` behavior. Export errors
terminate the probe with a diagnostic instead of retrying every game frame.
The launcher reads and inspects the binary graph directly, preserving shared
containers; model checks continue constructing fresh immutable model states.

The verified final capture is
`.probe-runs/full-battle-units-spells-and-junk-20261007-150757-9acaa467`.
Its complete diagnostic JSON is 249,938,988 bytes; the native binary is 44,494
bytes with 7,405 unique nodes. Binary export takes 3.411 seconds (3.105 graph
construction and 0.302 archive writing), versus 4.964 seconds for JSON export
from the same settled snapshot. This reduces this export phase by 31.3%.
The launcher reads and expands the binary in 0.060 seconds. Reading and
`ConvertFrom-Json` on the same JSON, measured separately with no simultaneous
model regression, takes 27.145 seconds. These are individual phase timings;
the 76.76-second native process includes optional JSON comparison export and
must not be described as a pure binary-process benchmark. Full-process timings
vary with native startup and frame scheduling.

The complete native run exits zero with 21 card plays, seven EndTurns, 69 room
stages, victory and Pyre 72, zero capture failures/mismatches/unsupported/pending
observations, unchanged original game files and muted audio. Independent
comparison verifies every binary/JSON value, property order and numeric lexeme.
The diagnostic JSON is optional (`-CaptureJson`); `-BinaryCapture:$false`
selects the legacy path. An offline `FixtureTools benchmark-capture` command
measures export/import on a retained graph and verifies its complete values;
those .NET timings are separate from Unity timings.

The retained `tests/fixtures/full-battle-native-binary.mt2f` stores this complete
native binary, SHA-256
`7f363ab290e21dfb3c4398e596d6579f7de8c42853288af89e5a5c526efa5c13`.
Its text-source metadata is zero because it was not imported from JSON; binary
payload and manifest integrity checks remain active. The source includes empty
callback-observer arrays from schema 45; these do not establish callback model
coverage. Capture-format version one is unchanged and prior archives remain
readable.

The isolated checkout of the staged capture change builds the probe without
warnings and completes the full regression with exit zero: 58 battle fixtures
and eight calibration fixtures. All 66 archives match the curated inventory and
SHA-256 manifest. The new native binary also passes independent action/turn
chains, 16 parallel branches, all 157 triggered-status effects, 77 unit turns,
both isolated preview streams and all 128 native registry queries.

## Status callbacks and ordered dispatch

`Run-FullBattleProbe.ps1 -Policy units-spells-and-junk -StatusCallbacks` captures
the seven modeled status callbacks: `OnStatusEffectChanged`, `OnArmorAdded`,
`OnPyregelAdded`, `OnValiant`, `OnSilence`, `OnSilenceLost` and
`OnNewStatusEffectAdded`. It extends the triggered-status scenario with native
gold rewards, threshold/once/silence gates, enemy initialization and a temporary
silence upgrade whose saturated count is removed before combat. Schema 46
records both enqueue payloads and completed dispatches with native unit/context
snapshots. Preview dispatches are excluded; incomplete observations fail the
capture gate. Archives are exported directly as binary graphs.

Status additions retain native no-op behavior: zero and negative additions
still enqueue a change callback and the status-specific addition callback.
Actual removals enqueue the negative delta and can enqueue `OnSilenceLost`;
a negative addition does not use that removal callback. Visible status counts
include retained zero entries. New-status thresholds require complete captured
visibility/category definitions. Immunity prevents both mutation and callbacks.
Unit upgrades use the native stack limit before constructing callback payloads.

The model carries deferred callbacks through spell effects, triggered effects,
unit upgrades, consumption, scheduled cleanup and initial status applications.
It preserves FIFO ordering, including initial enemy callbacks already queued
when the first `OnSpawn` phase is added and callbacks appended by earlier
callbacks. A dying nonboss can mark its once flag before aborting the effects;
dead bosses skip these callbacks. Valor still invokes an armor addition at zero
delta when existing armor exceeds its target. Source traits and flat pyregel
damage apply before melee-weakness multiplication.

The retained `tests/fixtures/full-battle-status-callbacks.mt2f` comes from game
2.2.1, module MVID `8fb07b96-f4db-4d2b-884d-c00536d6ccf4`. The complete native
run takes 95.04 seconds at Instant timing and passes with 21 plays, seven
EndTurns, 69 room stages, victory/Pyre 72, zero capture failures, mismatches,
unsupported or pending observations, unchanged original files and muted audio.
The binary is 61,174 bytes with 10,109 unique nodes; SHA-256 is
`63187b1e1fff393c0e3b0f7b8f95980e4911481707d38f4797bf9c32f797450f`.

Independent regression checks all 481 native enqueue/dispatch payloads in
order and compares each dispatch's unit and complete context. Coverage includes
all seven kinds, 71 rewarded dispatches, one zero delta, 100 negative deltas,
32 dying actors, 79 enemy dispatches, a rewarded silence loss, a rewarded
same-count armor addition, 147 once skips and nine silence gates. The same
fixture also checks 159 triggered status effects, 77 unit turns and both
restored preview RNG streams. Core checks cover nested initial queue ordering,
nonstackable upgrade limits, cleanup and 32 isolated parallel branches.

The full regression exits zero for 59 battle fixtures and eight calibrations.
All 67 binary archives match the curated inventory and SHA-256 manifest. The
probe builds with zero warnings and errors; no JSON fixture is tracked.

This covers status callbacks inside the existing modeled effect set. Relic
handlers, unit abilities, horde and other unmodeled status-specific callbacks
still require separate modeling and native evidence; their interactions must
remain unsupported.

## Unit upgrade callback source-card boundary

Native `CharacterState.ApplyCardUpgradeImpl` runs `CombatManager.RunTriggerQueue`
before `CardEffectAddCardUpgradeToUnits` writes the upgrade to the spawner card.
The owning room engine now drains a standalone addition at that boundary, using
its existing unit references and shared context. When a trigger queue is already
running, child callbacks remain deferred and observe the completed card write.
Removal retains its later drain because native `RemoveCardUpgrade` does not run
the queue before its source-card cleanup.

`UnitUpgradeCallbackChecks` verifies that a source-copy callback on a standalone
addition receives the original card modifiers, while the same addition inside
a running `PreCombat` queue copies the new upgrade. Both cases retain the final
source-card upgrade. The standalone case fails on the previous model; the new
model passes the core suite and 32 isolated parallel branches.

## Retained callback actors and generated statistic entries

A queued status callback can retain a living enemy before it occupies a floor.
The room engine now keeps that actor as an effect reference without adding it
to room target collections or the resulting room state. Self effects still
apply to the retained actor. Native callbacks 63 and 371 in the extended action
probe confirmed that room healing must not heal that unplaced enemy, while
room damage still targets the actual floor occupants. A core check verifies
this distinction and immutable parent state.

Generating a card with supported scaling traits also refreshes native
`deckStats`: card setup and upgrade text call `GetStatValue`, which runs
`UpdateDeckStats` even for a zero result. Active-battle generation now refreshes
owned statistic membership after placement for those traits. Plain generated
cards still leave the new entry uncached. Native source-copy callbacks exposed
five missing zero-valued entries; the independent callback context comparison
now matches them. Core checks cover both generation paths and parent isolation.

## Signed upgrade status stacks

Native `AddStatusEffectStacks` skips a zero stack entry and sends negative
entries through status removal. This differs from calling `AddStatusEffect`
directly, which can enqueue callbacks even for zero or negative additions.
Unit upgrades now retain the modifier while skipping zero status entries, and
negative entries use removal callbacks, including `OnSilenceLost`.

The extended native action probe's zero-armor upgrade generated no callbacks;
the old model incorrectly generated status-changed and armor-added callbacks.
Core checks cover that no-op, final source-card writeback, and a negative silence
upgrade whose removal callback grants gold. Direct zero/signed addition checks
retain their existing callback expectations.

Removing an upgrade passes its signed count to native `RemoveStatusEffect`:
`-1` clears all existing stacks, while smaller counts increase the stack count
without firing addition callbacks. Core checks verify both resulting counts and
the removal-only reward behavior.

Nested damage and healing queue payloads also preserve native `FireTriggersData`
defaults: `OnHit` and `OnHeal` carry an empty parameter string. Native action
dispatches verify the generated queue payloads without replacing empty strings
with nulls. Core checks cover room-target damage and retained self healing.

## Merged status applications when summoning upgraded cards

Native `SetupStartingStatusEffects` merges authored stacks with permanent
upgrades, then with temporary upgrades, before installing them. Each merge
clamps the running sum to zero after every entry and drops empty entries after
the group. Summons now enqueue one initial application per merged status rather
than one callback per contributing upgrade. Enemy definitions without a source
card still retain their individual authored applications.

The extended native action probe's play 10 had starting armor 3 plus upgrade
armor 1. Native queued a single armor-4 application; the old model rewarded
both additions and overcounted gold by 5. The corrected play matches the native
state. Core checks verify the single reward and signed entry/group boundaries.
Definitions using native `fromPermanentUpgrade` application groups remain
explicitly unsupported until that separate grouping metadata is modeled.

## Combat cancellation after a deferred boss removal

`HeroManager.RemoveCharacter` notifies `GameScreen` after the final boss's
death callback. `GameScreen.EndCombat` starts `StopCombatLoop`, canceling the
main combat coroutine. Later characters already marked as being removed in
that batch never reach their `OnDeath` callbacks. A card effect still resolving
holds the stop operation until its effects finish.

Room combat and unit turns now stop the current deferred removal batch after
the winning boss has been removed. Nested deaths created by the boss's own
callback still settle before that cancellation; ordinary removal and played
card effects retain their existing behavior. In the extended native action
probe, enemy 14 and boss 15 were removed, but sweep attacker 11 had no removal
or `OnDeath` dispatch. The previous model overcounted its death reward by 5.
The corrected room, unit turn and full EndTurn states match the native capture.
Core checks cover both combat entry points and retain the existing nested
terminal-death and detached-source upgrade checks.

## Destroyed attacker references at the next decision

Native character destruction is deferred until the end of a Unity frame.
When an EndTurn callback completes, a dying object's `IsDestroyed` state can
already be final while Unity's null comparison still returns its old reference.
All decision captures now write that attacker reference as zero, including
card-action before/after states and EndTurn boundaries. Room stages and
in-flight effect/callback captures keep their references until their own native
boundaries. An initial EndTurn-only implementation passed individual steps but
failed the continuous policy at action 9: its previous decision cleared attacker
9, while the subsequent native card snapshot retained it. Continuous policy
checks therefore also enforce this normalization across adjacent decisions.

## Native status callback action regression

`Run-FullBattleProbe.ps1 -Policy units-spells-and-junk -StatusCallbackActions`
extends the status callback scenario with self/room damage and healing, status
additions, permanent upgrades and source-card copies. Its played pyregel upgrade
observes the source before writeback; an armor callback upgrades and copies the
source while the native queue is already running. A zero-stack upgrade remains
in the modifier list without enqueuing a status addition. Enemy callbacks add
then directly re-add zero armor to exercise rewarded same-count additions.

Schema 48 captures generated queue payloads, ordinary character dispatches,
override targets and native activity/dying/removal flags. Temporary removal
observations are scoped to this isolated action scenario. The observers wrap
native enumerators and preserve parent callback scopes through nested execution.

The retained `tests/fixtures/full-battle-status-callback-actions.mt2f` comes from
game 2.2.1, module MVID `8fb07b96-f4db-4d2b-884d-c00536d6ccf4`. The native run
takes 223.86 seconds at Instant timing with muted audio, 15 card plays, five
EndTurns, 35 room stages, nine train phases, seven spawn observations and victory
at Pyre 80. Capture failures, mismatches, unsupported and pending observations
are all zero; the original game files are unchanged. The archive has 9,898 unique
nodes in 65,121 bytes, no source JSON, and SHA-256
`b9953779ab30f2f410952e8b53cc170b6eead2a16d3e14e86369da5f932b566f`.

Independent checks match all 390 status dispatches and their full room scopes,
95 generated queue payloads and 419 ordinary character actor/context states.
Coverage includes 113 rewards, eight zero and 63 negative changes, 35 dying
actors, 99 enemy dispatches, two rewarded silence losses, six same-armor rewards,
251 once skips and 33 silence gates. Actual once-only action effects cover ten
damage, 14 healing, seven upgrade and five source-copy executions, including one
standalone and four running-queue source-copy boundaries. Two captured false
can-fire gates have no matching non-ignoring effects; fixtures needing that
queued permission flag remain rejected rather than inferred.

The same binary checks 116 triggered status effects, 32 unit turns, 168 native
status queries and both restored preview streams. Continuous simulation matches
every action and EndTurn from the initial state, the complete suffix from an
actual mid-battle state, and 16 isolated parallel branches. The curated inventory
now contains 60 battle fixtures and eight calibration archives.

The complete 68-archive manifest, integrity and model regression passes after
these changes. The native probe Release build also succeeds with zero warnings
and zero errors.

## Canonical attacker references at quiet decisions

Current decision snapshots explicitly set `CanonicalDecisionReferences`.
Attacker references to dead or destroyed characters become zero at quiet card,
EndTurn and terminal boundaries; live attackers and in-flight callback/room
references remain intact. The simulator applies the same rule to completed
decisions. Older snapshots retain their original convention through the default
false flag. Independent checks cover both conventions, terminal boss removal,
live references and 32 parallel branches. A new native energy scenario exposed
the terminal case: the prior model retained removed attacker 13 after the native
quiet decision had cleared it.

## Current, next-turn and persistent energy

`EnergyModel` implements `CardEffectGainEnergy`, `CardEffectAdjustEnergy`,
`CardEffectGainEnergyNextTurn` and `CardEffectGainEnergyEveryTurn` for spells and
unit triggers. GainEnergy's monster-turn-only mode tests the actual combat
phase; AdjustEnergy changes current energy during MonsterTurn and queues a
signed change during other phases. Positive gains use the captured native cap,
removals floor at zero, and the native integer additions wrap before clamping.
Nonpositive GainEnergy/NextTurn/EveryTurn quantities are no-ops. A failed phase
gate consumes no quantity RNG and respects subsequent-effect cancellation.

Schema 49 adds immutable `CombatContext.EnergyState`: maximum energy, current
phase, pending next-turn and persistent modifications, and whether the Pyre is
alive. Current energy stays in the shared query frame. All context transitions
preserve these fields, including card generation, payment, spawner returns and
terminal clearing. Missing limits or phase inputs reject energy effects.
Pre-discard triggers can change energy before its end-turn statistic is stored;
end-turn handling then removes it before combat. Unrestricted gains during combat or PreCombat remain
available when positive next-turn income is added. Nonpositive income preserves
those gains, and the one-turn modifier resets after replenishment. The settled
terminal capture occurs after StopCombatLoop, before StopCombat advances its
phase to EndOfCombat, so it retains the last live combat phase.

Two binary-only native fixtures preserve the original Boss and waves:
`full-battle-energy-effects.mt2f` takes 47.04 seconds at muted Instant timing,
with 28 card plays, six EndTurns, 49 room stages and 256 independently matched
energy contexts. `full-battle-energy-effects-lethal.mt2f` takes 41.87 seconds,
with 20 plays, four EndTurns, 30 room stages and 192 matching energy contexts.
Both finish at Pyre 80, with zero capture failures, mismatches, unsupported or
pending observations and unchanged original game files. Their archives contain
5,458 nodes in 31,204 bytes and 4,043 nodes in 24,200 bytes, respectively.

The 448 exact context comparisons cover five modes, four phases, 52 ranged
samples, 84 zero and 128 negative samples, and 48 capped results. The lethal
fixture actually kills the Boss with a spell and verifies that all four later
energy effects skip without changing income or consuming their quantity RNG.
Both complete policies match every action/EndTurn from initial and actual
mid-battle inputs, including 16 isolated parallel branches. Pure checks also
cover late end-turn energy at a terminal boundary, no-positive-income turns,
signed wrap, stopped/Pyre-dead tests and immutable branches. Status registry
queries continue to compare exact native results; dedicated triggered-status
fixtures retain their stronger zero/hidden-definition coverage requirements.

The complete curated regression passes all 70 binary archives: 62 battle
fixtures and eight calibrations, with inventory and SHA-256 integrity checks.
The final native probe Release build succeeds with zero warnings and errors.

## X-cost payment and effect quantities

Schema 50 captures the definition's `CostType`. Existing archives use Default;
their capture boundaries explicitly rejected variable-cost instances. Card
modifier resolution preserves the type. ConsumeRemainingEnergy pays the current
decision's entire energy, including zero, independently of the upgraded fixed
cost. Native IsAffordable and CanPlayHandCard validate the recorded plays before
the game actually executes them. Unknown and NonPlayable types remain rejected
until their associated legality and callbacks are modeled.

The paid amount enters both the card's LastPlayedCost and the live payment
statistic before effects run; end-of-play callbacks clear the latter and retain
the former on owned cards and detached registry references. UnmodifiedPlayedCost
reads that exact payment. PlayedCost additionally reads the raw base cost and
the separate permanent/temporary X-cost modifiers. Its bonuses change effect
quantities without changing the amount paid. Effects can gain new energy after
payment; a Boss-killing spell skips that subsequent gain. Pure checks also cover
a redrawn card paying the next turn's fresh energy and 32 immutable branches.

Two binary-only fixtures preserve the original Boss and waves on game 2.2.1,
module MVID `8fb07b96-f4db-4d2b-884d-c00536d6ccf4`:

- `full-battle-x-cost.mt2f`: 78.07 seconds at muted Instant timing, 28 card plays,
  six EndTurns and 52 room stages. Its 23 X casts include nine zero and 14
  positive payments, with 12 ignored fixed-cost reductions, 56 exact damage
  callbacks and 23 exact energy contexts. The archive has 4,263 nodes in 24,633
  bytes, SHA-256 `0e7b766b784abee966e5cf0041a992f829c1dc1fde5c1c9f606f2212c09afe3c`.
- `full-battle-x-cost-lethal.mt2f`: 62.46 seconds, 21 plays, five EndTurns and
  44 room stages. Its 16 X casts include seven zero and nine positive payments,
  eight ignored fixed-cost reductions and the winning Boss cast, with 40 exact
  damage callbacks and 15 exact energy contexts. The archive has 3,768 nodes in
  22,305 bytes, SHA-256 `be50597271d31879b60ae2cf07cb72048c0be7fdb3772429479e5f0f0fcf1156`.

Both end at Pyre 80 with zero capture failures, mismatches, unsupported or
pending observations and unchanged original game files. Independent policies
reproduce every action, EndTurn and terminal state from initial and actual
mid-battle inputs, including 16 parallel branches. Every recorded damage and
energy context is recomputed independently. Dedicated energy fixtures retain
their full phase/range/cap coverage contract; other scenarios still compare all
their observed energy effects exactly. Relic/room cost modifiers and dynamic
trait costs remain explicit unmodeled interactions.

The existing 70-archive regression passes after the payment changes. Both new
native fixtures and the expanded pure checks also pass; the curated inventory
now contains 64 battle fixtures and eight calibration archives.
Its complete 72-archive inventory and SHA-256 integrity check passes, and the
final native probe Release build succeeds with zero warnings and errors.

## Future draw counts and bonus-card upgrades

Schema 51 adds nullable `CardCycleState.BonusDraw`; null identifies older inputs
that did not capture this mechanism. Counters are keyed by card/effect or
unit/trigger/effect identity. Native capture reads the actual private effect
counters and signal invocation list, including duplicate listeners and detached
sources. The immutable model preserves callback order and shared counters
across card plays, hand cycling, generation and terminal clearing.

`CardEffectDrawAdditionalNextTurn` applies signed amounts, sampling a range
only once. An optional upgrade increments the effect's private counter and
registers a fresh listener for a nonzero amount. Bonus upgrades use the actual
resulting hand size compared with the base hand size; duplicate listeners can
upgrade the same card more than once while consuming their shared counter.
DrawCards dispatches its final null/reset event even for zero or negative draws,
clearing listeners and their counters while preserving the future draw count.
DrawHand's empty-pool and full-hand early exits retain pending state; normal
completion resets the count. Quantity additions use native signed wrap.

The ordinary binary fixture takes 51.14 seconds at muted Instant timing: 21
card plays, seven EndTurns, 64 room stages and 67 exact future-draw contexts,
ending at Pyre 73. Its archive has 5,031 nodes in 31,018 bytes. The lethal
fixture takes 44.51 seconds: 17 plays, five EndTurns, 44 room stages and 58 exact
contexts, ending at Pyre 80 after a spell kills the original Boss. Its archive
has 3,790 nodes in 23,898 bytes. Both have zero capture failures, mismatches,
unsupported or pending observations and unchanged original game files.

The 125 independently recomputed contexts include 22 negative and 23 zero
amounts, 19 ranges, 106 upgraded and 18 unit effects, six duplicate-listener
contexts, eight ordinary zero-draw cancellations and 11 capped hand draws.
Every complete policy matches from initial and actual mid-battle inputs,
including 16 isolated parallel branches. Pure checks also cover existing hand
thresholds, empty/full exits, integer wrap and 32 parallel branches. Filtered
upgrades, card-trigger owners and unknown signal listeners remain explicitly
unsupported. Summoning applies only positive merged starting upgrades to a new
status registry, while retaining captured historical zero-stack definitions.

The complete curated regression passes all 74 binary archives, including
inventory and SHA-256 checks. The native probe Release build succeeds with
zero warnings and errors.

## Dynamic room capacity effects

Schema 52 captures immutable `CombatContext.RoomCapacities` for both native
spawn-point groups in every room. Later card plays, summon legality and
restricted size upgrades read the current shared capacity rather than the
initial play rules. Existing archives retain null and their original static
capacity inputs. Capacity state survives all card, room, train and terminal
transitions; capacity-scaling traits survive payment, discard, upgrades and
card creation. Missing capacity inputs reject the new effect.

`CardEffectAdjustRoomCapacity` reads its integer parameter directly: configured
integer ranges and multipliers do not sample RNG. Ordered
`CardTraitScalingAdjustCapacity` traits query the existing live statistic model
and add signed integer products before adjustment. Native RoomState bounds
reject further decreases at one and increases at 30; accepted amounts clip to
those limits using native signed wrap. Shrinking an occupied room retains its
characters and can leave their total size above capacity. Exact Heroes targets
the enemy group; None and combined flags select the player group. The
OnlyTriggerIfNoEnemies gate checks native hero/enemy presence, including dying
characters that have not been removed. Preview, Pyre-room and post-Boss gates
skip effects; failed tests retain cancellation behavior and consume no quantity
RNG. Unit triggers apply these rules with no parent-card scaling.

The ordinary binary fixture runs at muted Instant timing in 107.81 seconds
while the old regression runs concurrently. It finishes at Pyre 67 after 24
plays, seven EndTurns and 66 room stages. Its archive has 6,070 nodes in 33,777
bytes. The lethal fixture takes 65.00 seconds, finishes at Pyre 80 after 15
plays, four EndTurns and 30 stages, and actually kills the original Boss with a
spell before the later capacity effects. Its archive has 3,686 nodes in 21,865
bytes. Both retain the original Boss and waves, unchanged original game files,
and zero capture failures, differences, unsupported or pending observations.

Independent checks recompute 220 complete effect contexts, 410 native bounds
calls with 30 refusals, and 2,050 casting/runtime tests with 186 failed gates.
They include 15 ignored ranges, 11 changed trait quantities, 24 unit effects,
103 observed occupied oversize states and 15 signed wraps. Restricted size
upgrades following PreCombat capacity changes are included in the complete
native phase/decision comparisons. Both complete policies match from initial
and actual mid-battle roots with 16 parallel branches. Pure checks additionally
prove live summon legality, precedence over stale explicit upgrade limits,
dying-enemy/Pyre/preview gates and 32 immutable parallel branches.

The existing 74-archive regression and both new fixtures pass. The expanded
76-archive inventory and SHA-256 integrity check also passes; no text JSON is
required. The native probe Release build has zero warnings and errors. Relics,
attachments, corruption and other conditional room modifiers remain explicit
unsupported interactions until their separate execution paths are modeled.

## Direct unit upgrades for equipment

`UnitModifierModel.ApplyDirect` models CharacterState.ApplyCardUpgrade and
RemoveCardUpgrade separately from card effects that maintain a unit's spawner
modifiers. A direct operation changes the live unit and applied-upgrade list;
it neither scales through the responsible card nor writes a temporary upgrade
back to the spawner. Clone exclusion is a card-effect gate; the native direct
API ignores that flag. Unique definitions and live restricted-size checks
still gate an addition.

A named removal removes the first list entry with the supplied definition ID
and reverses the supplied descriptor's stats, which need not match that entry.
The card-effect removal API retains its separate all-copies behavior. Anonymous
removal uses object identity: a fresh descriptor reverses stats while retaining
the applied entry; an explicit captured list index represents the same applied
object and removes that entry. The temporary aggregate constructed during
equipment attachment/removal therefore cannot be treated as a named upgrade.
Removal also retracts maximum HP attributed to its upgrade ID. Additions run
queued callbacks; removals expose pending callbacks for their caller to drain.
Lethal HP steps retain already-applied changes and stop later status steps.
Attributed-health ledgers are currently allowed only on the direct API target;
ordinary room validation continues to reject their unmodeled producers.

`full-battle-direct-unit-upgrades.mt2f` records 21 native operations on a
naturally played Steward before the initial decision capture. Setup marks that
unit as a clone through the native API. The attributed-health case uses native
BuffMaxHP and seeds its private attribution ledger to exercise removal by key.
The fixture observes duplicate IDs with different values, caller-based removal,
fresh/same anonymous objects, unique and capacity no-ops, clone exclusion,
positive/negative attributes, the equipment-limit cap and its negative removal
result, keyed health and a lethal partial application. Checks recompute complete
room/context snapshots from actual before/after data, preserve parent states,
and repeat the 21 operations in 32 independent parallel branches.

The muted Instant native run takes 49.77 seconds and retains the original Boss
and waves. Its subsequent policy finishes at Pyre 60 after 20 plays, seven
EndTurns and 57 room stages, with zero capture failures, differences,
unsupported or pending records and unchanged original files. The complete
policy matches from the initial and actual mid-battle roots in 16 parallel
branches. The direct binary archive stores 4,658 nodes in 27,403 bytes, with
schema 53, game 2.2.1 and module MVID
8fb07b96-f4db-4d2b-884d-c00536d6ccf4; no source JSON document is needed.

The existing 76-archive regression and the new fixture's final independent
checks pass. The expanded 77-archive inventory and SHA-256 integrity check
passes. The native probe Release build has zero warnings and errors;
ModelChecks retains its 12 existing nullable warnings and has no build errors.

This supplies the numeric/status upgrade primitive for equipment. Attachment,
oldest-first replacement, reverse removal, source-equipment trigger provenance,
equipped versus standby-host relationships, grafting, card returns and unit
death still need their own modeled lifecycle and native fixture coverage.

## Equipment attachment and original-host returns

`RoomCombatModel.ApplyEquipment` now models native attachment, oldest-first
replacement and reverse-order remove-all with base definition upgrades,
permanent card upgrades and the native temporary HP/damage/status aggregate.
The equipment list changes before attachment upgrades and after removal
upgrades. Capacity is reread after each removal. Ordinary equipment callbacks
include OnEquipmentAddedToAny in native enemy-then-player room order,
OnEquipmentAdded on the host and deferred OnEquipmentRemoved callbacks.

Permanent upgrade objects require explicit source-card/index identity in the
unit's applied-upgrade list. Native attachment reuses those objects and native
removal removes the identical object. Definition upgrades and temporary
aggregates are freshly created objects; anonymous aggregate removal therefore
reverses attributes while retaining its anonymous applied-list entry. These
paths share the direct attribute primitive without writing to a unit's spawner.

Units carry ordered equipment card IDs and cards carry current equipped-unit
IDs. Standby entries separately carry their original host and return-to-hand
trait. Removing equipment clears its current card link but does not release
the original-host standby condition. Attached equipment returns in unit-list
order on player death; previously detached equipment waits for the global
standby check. Global checks occur after card resolution and before ordinary
next-turn draws, in native standby dictionary order. Return-to-hand prepends
cards, with full-hand routing to the draw pile; ordinary exhaustion increments
the live statistic. Destroyed host references normalize at decision boundaries.
Grafting, special return overrides, child-unit handoffs, equipment-granted
triggers/abilities and last-equipped target history remain unsupported.
Raw API attachment of an already-attached equipment object also fails closed;
the ordinary card effect separately preserves its native already-attached no-op.

The authored setup converts owned targeted spells into zero-cost equipment,
gives a naturally played Steward two equipment slots, attaches three cards
through CharacterState.AddEquipment and supplies actual native standby
callbacks. It additionally supplies temporary and permanent modifiers,
equipment callbacks and a room-wide removal spell. The original Boss and waves
remain native. The retained fixture specifically covers return-to-hand,
including detached equipment waiting on its original host and returns before
later draws; specialized exhaustion listeners and full-hand return overflow
still require dedicated native coverage.

`tests/fixtures/full-battle-equipment.mt2f` records 25 native API operations,
19 subsequent card plays, seven EndTurns and 61 room stages. The muted Instant
native run takes 72.35 seconds, wins at Pyre 61 and has zero capture failures,
differences, unsupported or pending records, with unchanged original files.
The schema-54 binary archive stores 4,904 nodes in 28,801 bytes on game 2.2.1,
module MVID 8fb07b96-f4db-4d2b-884d-c00536d6ccf4. Independent checks recompute
every operation and repeat them in 32 parallel branches; the complete policy
matches from initial and actual mid-battle roots in 16 parallel branches.

The existing 77 binary archives pass their full regression. The final equipment
archive passes its independent checks, including the repeated-raw-attachment
unsupported guard, and all 78 archives match the curated SHA-256 inventory.
The native probe Release build has zero warnings and errors; ModelChecks has
no errors and retains its 12 existing nullable warnings. No source JSON fixture
is tracked.

## Ordinary equipment exhaustion

`-EquipmentExhausted` constructs the same native attachment/replacement and
reverse-removal scenario without CardTraitReturnToHandEquipment. The retained
`tests/fixtures/full-battle-equipment-exhausted.mt2f` verifies attached equipment
returning during the host's death and detached equipment returning during the
global standby check. Complete contexts compare exact exhausted-pile order,
dictionary free-slot history, card links and live TimesExhausted/AnyExhausted
statistics. This scenario has no exhaustion listeners, so it does not establish
their selected-room callback ordering.

The muted Instant native run takes 49.28 seconds, wins at Pyre 61 and records
15 equipment API operations, 14 policy plays, seven EndTurns and 61 room stages.
Capture failures, differences, unsupported and pending records are all zero;
original files are unchanged. All API transitions compare independently in
32 parallel branches. The complete policy matches from the initial and actual
mid-battle roots in 16 parallel branches. The schema-54 archive contains
4,411 nodes in 26,491 bytes on game 2.2.1 and the unchanged module MVID.
Both equipment fixtures pass the final checker and all 79 archives match the
curated SHA-256 inventory. The probe builds without warnings/errors; the
checker retains its 12 existing nullable warnings. This increment adds native
evidence and scenario coverage without changing the battle model.

## Full-hand equipment returns and spawning reference boundaries

`-EquipmentOverflow` fills the actual native hand to ten cards after the three
setup attachments. A direct negative-HP upgrade kills the original host;
attached return-to-hand equipment instead appends to the draw pile. The replaced
first equipment card still waits in standby until a subsequent native
DrawHand(0). The global standby check runs before the full-hand early exit and
also routes that card to the draw pile. Exact room/context comparisons cover
the lethal direct API boundary, and exact draw comparisons cover this zero-draw
boundary. The source unit's spawner is exhausted and equipment links retain
their original objects through the raw API snapshot.

Native Unity objects can remain referenced after their logical unit removal
and compare null only after later cleanup. Raw API recordings preserve this
in-flight convention. Enemy spawning observations now explicitly declare
canonical decision references, using the same capture convention as player
decisions. `EnemySpawnState.CanonicalDecisionReferences` defaults false for
legacy captures and is preserved through action/turn constructors. Spawning
normalizes absent-host card links and absent-attacker unit links only at a
declared canonical output boundary; live links and immutable parents remain
intact. Pure checks compare both conventions and 32 parallel branches. This
avoids making simulated semantic relationships depend on Unity frame cleanup.

`tests/fixtures/full-battle-equipment-overflow.mt2f` stores 4,940 nodes in
29,900 bytes with schema 55 on game 2.2.1 and the unchanged module MVID.
The muted Instant native run takes 47.18 seconds and wins at Pyre 52 after
21 policy plays, seven EndTurns and 56 room stages. Its 30 equipment API
operations, one lethal direct upgrade, full-hand draw and complete policy have
zero capture failures, differences, unsupported or pending records; original
files are unchanged. Equipment and lethal API cases compare independently in
32 parallel branches, and the entire policy matches initial and actual
mid-battle roots with 16 parallel branches. The ordinary hand-return and
exhaustion fixtures also pass the final independent checker.

Equipment-granted triggers/abilities, grafting and special return overrides
remain separate work. The full-hand fixture supplies targeted equipment return
coverage; the ordinary fixtures retain their stronger reverse-removal and
permanent-upgrade identity requirements.

All 79 existing binary archives pass the full regression. All three equipment
fixtures pass the final checker, including canonical flag propagation and the
lethal API's 32 isolated repeats. The expanded 80-archive inventory and SHA-256
check passes. Probe builds with zero warnings/errors; ModelChecks has no errors
and its 12 existing nullable warnings. No source JSON fixture is needed.

## Equipment-granted trigger origins and shared removal IDs

`-EquipmentTriggers` adds actual native character triggers through equipment
definition upgrades and an anonymous permanent equipment upgrade. The captured
`CombatTriggerOrigin` keeps the upgrade ID, bound equipment instance,
IsFromEquipment flag and OnlyIfEquipped condition separately. Once flags,
effect counters, healer adjustments and preview resets preserve the origin.
Upgrade application adds triggers after both maximum-HP steps and before
status callbacks; partial lethal applications skip that addition. Native
preview application/removal leaves trigger membership unchanged. Spawner
permanent/temporary card upgrades initialize ordinary unbound triggers.

The native removal key is CardState.GetID(), which returns the definition ID,
not the instance ID. Two copies therefore bind their effects to different
equipment objects while sharing an upgrade ID. RemoveTrigger iterates backward
and deletes all matching trigger kinds and IDs; an empty ID deletes every
matching kind, including base triggers. The native fixture demonstrates removal
of one equipment copy deleting matching triggers from the remaining copy while
preserving unattributed base triggers. EquipmentDefinition now records this raw
native upgrade ID. Legacy definitions retain their recorded key convention;
new captures also preserve raw maximum-HP ledger keys without arbitrarily
mapping a shared definition ID to the first card instance.

OnlyIfEquipped tests current equipment membership before marking the once flag
or executing effects. The native scenario retains an equipped-only base removal
trigger after the final equipment disappears. Pure checks also cover disabled
execution with an empty equipment list, named versus empty-ID removal,
preview exclusion, inherited spawner triggers, immutable parents and 32
parallel branches. Upgrade trigger descriptors share the ordinary character
trigger projection, with bounded recursion explicitly reported as unsupported.

`tests/fixtures/full-battle-equipment-triggers.mt2f` contains 4,958 nodes in
29,238 bytes, schema 56 on game 2.2.1 and module MVID
8fb07b96-f4db-4d2b-884d-c00536d6ccf4. The muted Instant native run takes 45.25
seconds, wins at Pyre 61 after 19 plays and seven EndTurns, and compares 61 room
stages, 13 card cycles, 14 train phases, 11 spawns and 25 equipment operations.
All capture failures, differences, unsupported and pending counts are zero;
original files are unchanged. The independent checker compares the complete
native operations in 32 parallel branches and the whole policy from initial
and actual mid-battle inputs with 16 parallel branches. All three previous
equipment fixtures pass the same checker.

Equipment-granted unit abilities, grafting, immediate moon/deathwish effects,
status-conditioned triggers, empty equipment-trigger source capture, and
upgrade effects mutating a trigger list while it is firing remain separate
work. Running-list mutations fail closed until retained trigger identity is
modeled; this increment does not treat them as ordinary immutable effects.

The complete regression passes all 80 existing binary archives, including all
eight calibration suites. The new native fixture and the three previous
equipment fixtures pass the final independent checker. The expanded 81-file
inventory and SHA-256 verification pass, as does probe-script parsing. Probe
builds with zero warnings/errors; ModelChecks retains its 12 existing nullable
warnings and has no errors. No JSON source fixture is introduced.

## Retained trigger execution during live list mutation

Native CharacterState.FireTriggers iterates the mutable trigger list by index
while retaining the current CharacterTriggerState locally. A newly appended
matching trigger can fire during the same iteration. Removing an earlier entry
shifts the list and the next increment can skip a trigger. Removing the current
trigger does not cancel its remaining effects; removed siblings do not fire.

CombatTrigger now carries a private immutable identity token. Fired flags,
preview resets and healer effect updates preserve that token, while newly
granted triggers receive fresh identities. The engine finds the retained
trigger after immutable unit copies and list changes rather than reusing its
old index. Removed triggers continue their local effects without writing their
state onto a different list entry. The outer index still advances exactly as
in the game. Tokens are not serialized and introduce no mutable state shared
between parallel branches.

The native unit upgrade effects pass their upgrade definition ID to both
ApplyCardUpgrade and RemoveCardUpgrade. UnitModifierModel now uses that ID for
effect-driven trigger additions/removals while preserving caller-supplied keys
for the direct API and definition IDs for equipment. The previous blanket
rejection of running-list mutation is removed. A detached running bonus-draw
effect still fails closed because its counter projection needs persistent
effect identity, separately from the trigger identity modeled here.

`-TriggerMutation` authors three direct PreCombat cases on a normally summoned
Steward: a parent grants a same-phase child, a running trigger removes an
earlier OnHeal entry, and a running trigger removes itself plus its sibling but
then awards gold. An acyclic removal descriptor shares the installed upgrade's
definition ID, avoiding synthetic cyclic source data. Deployment-turn API
testing explicitly permits PreCombat in the in-memory balance timing list;
the original Boss and waves are unchanged. Every before/after state is
captured from the native coroutine and independently compared.

`tests/fixtures/full-battle-trigger-mutation.mt2f` stores 4,595 nodes in 27,669
bytes, schema 57 on game 2.2.1 with the unchanged module MVID. The muted Instant
native run takes 45.56 seconds and wins at Pyre 73 after 20 plays and seven
EndTurns. All 60 room stages, 13 card cycles, 14 train phases, 11 spawns and
three mutation cases have zero capture failures, differences, unsupported or
pending records; original files are unchanged. Independent mutation cases
repeat in 32 parallel branches, and the complete policy matches initial and
actual mid-battle roots with 16 parallel branches. Equipment-trigger,
effect-driven unit-upgrade and pre-combat historical fixtures also pass the
targeted independent checker.

The complete regression passes all 81 existing binary archives and all eight
calibration suites. The new fixture passes the final independent checker,
including 32 parallel mutation branches and the 16-branch complete policy.
All 82 curated archives match the SHA-256 inventory, and probe-script parsing
passes. Probe builds with zero warnings/errors; ModelChecks retains its 12
existing nullable warnings and has no errors. No JSON source fixture is
introduced.

## Persistent bonus-draw effects after trigger removal

Native CardEffectDrawAdditionalNextTurn owns its private upgradeApplyCount.
The registered signal closure retains the CardEffectState and parameters even
after CharacterState removes the source trigger. Trigger-list indices are
therefore not effect identities: shifting a live trigger or installing another
copy at an old index must not merge its counter with a removed effect.

CombatTrigger.StateId and CombatUnit.NextTriggerId now capture a per-unit
monotonic allocation. Trigger removals preserve the allocation cursor. Granted
unit/equipment triggers and inherited spawner triggers receive fresh IDs;
immutable health, attack, status, equipment, ascension and engine snapshots
preserve them. Fired/preview/healer copies retain both their internal identity
and captured ID. Missing IDs remain nullable for legacy archives, and malformed
captured allocations fail closed rather than guessing an effect owner.

Native trigger allocation is observed when CharacterState adds a trigger,
including additions/removals between snapshots. The trace retains removed
trigger objects and their counters. Bonus-draw keys use the captured ID plus
the unchanged effect position, while legacy live triggers keep their previous
index keys. A self-removed running trigger with a persistent ID can finish its
draw effect. The pending counters/listeners remain entirely in immutable card
cycle state, independent of the removed unit's live trigger list and of other
parallel branches.

`-DetachedBonusDraw` authors four direct PreCombat transitions: a live trigger
removes an earlier trigger whose draw upgrade is pending; a running trigger
removes itself and its sibling before scheduling another upgrade; and the same
upgrade definition is installed, fired and removed twice. The five actual
listeners retain distinct counters totaling eight additional draws. The next
ordinary hand caps at ten, applies the native callbacks' temporary upgrades,
and clears their counters/listeners. The original Boss/waves are unchanged.

`tests/fixtures/full-battle-detached-bonus-draw.mt2f` stores 4,087 nodes in
24,981 bytes, schema 58 on game 2.2.1 with module MVID
`8fb07b96-f4db-4d2b-884d-c00536d6ccf4`. The muted Instant native run takes
44.45 seconds and wins at Pyre 80 after 17 plays and six EndTurns. All 49 room
stages, 11 card cycles, 11 train phases, nine spawns, six bonus-draw effects and
four mutation cases have zero capture failures, differences, unsupported or
pending records; original files are unchanged. Independent mutation cases
repeat in 32 parallel branches, and the complete policy matches initial and
actual mid-battle roots in 16 parallel branches.

The equipment-trigger archive is refreshed with persistent allocations using
the unchanged scenario: 4,975 nodes, 29,380 bytes, schema 58. Its muted Instant
native run takes 50.83 seconds with 19 plays, seven EndTurns and final Pyre 61.
All captures match with zero failures/unsupported/pending records; original
files are unchanged. All 25 actual equipment transitions compare independently
in 32 parallel branches, and both complete-policy roots match with 16 parallel
branches. This verifies allocation through real equipment-trigger additions
and shared-definition removals as well as the new detached callback cases.

The complete regression passes all 82 pre-existing archives, including the
legacy equipment-trigger capture and all eight calibration suites. The new
detached-draw and refreshed equipment captures separately pass the final
independent checker, including their 32-branch local and 16-branch complete
policies. The resulting 83-file inventory and SHA-256 validation pass.
Probe builds with zero warnings/errors; ModelChecks retains its 12 existing
nullable warnings and has no errors. Probe-script parsing passes, and no JSON
source fixture is introduced.

## Status-conditioned triggers and separate dying targets

Native CharacterState.FireTriggers checks each required self status with
HasStatusEffect, which accepts a positive count and compares status IDs without
case sensitivity. The count configured on each requirement is ignored. Dying
requirements are checked only when a non-null dyingCharacter is supplied.
These gates run before effect preflight and before consuming the once flag;
later triggers read live statuses changed by earlier effects in the same phase.

CombatTriggerConditions copies both requirement lists into immutable values.
Trigger copies and newly granted unit/equipment triggers preserve these gates.
Character queues carry a separate dying-character payload, independent of the
attack/override target, through local and external dispatch, sweep callbacks
and retained removed units. Explicit queue fire permission is also preserved.
Natural Slay dispatches now supply the actual victim for status tests, including
its zero health and retained status registry after death. Legacy archives with
no conditions keep their existing behavior.

`-ConditionalTriggers` normally summons two initial Stewards on separate floors;
each occupies three of the ordinary five capacity. Native direct upgrades
install eight controlled PreCombat cases covering missing self requirements,
case-insensitive presence, configured counts of 999 with actual counts of one,
missing/positive/zero dying status, null-dying bypass and same-phase changes.
Deployment-turn API testing permits PreCombat in the in-memory timing list.
Four subsequent natural PreCombat dispatches and two actual Slay callbacks
exercise the same conditions during the ordinary battle. The Boss and enemy
waves remain unchanged. Each raw native phase captures complete room, actor
and dying-target states, independently of its embedded prediction.

`tests/fixtures/full-battle-conditional-triggers.mt2f` stores 4,441 nodes in
27,430 bytes, schema 59 on game 2.2.1 with module MVID
`8fb07b96-f4db-4d2b-884d-c00536d6ccf4`. The muted Instant native run takes
48.21 seconds and wins at Pyre 80 after 19 plays and six EndTurns. Its 53 room
stages, 12 card cycles, 12 train phases, 11 spawns and 14 conditional dispatches
have zero capture failures, differences, unsupported or pending records;
original files are unchanged. All conditional phases compare independently in
32 parallel branches. The complete policy matches initial and actual
mid-battle roots in 16 parallel branches. Pure checks additionally cover local
sweep victims, retained external targets, explicit fire permission and parent
immutability.

All 83 pre-existing binary archives, including eight calibration suites, pass
the complete regression. The new archive separately passes the independent
checker. The expanded 84-file inventory and SHA-256 checks pass, along with
probe-script parsing. Probe builds with zero warnings/errors; ModelChecks has
its 12 existing nullable warnings and no errors. No source JSON fixture is
introduced. Relentless-transition trigger removal, immediate moon/deathwish
effects, equipment-granted abilities, grafts, relics, room attachments and
specialized Boss interactions remain separate work.

## Companion Boss transition and selective trigger removal

Native HeroManager.PerformCompanionBossAction runs only at BossActionPreCombat
after the wave queue is exhausted. It selects a living companion in tower
order, runs DoAscension with only that character and forceLoop enabled, adds
relentless, then calls RemoveTriggersOnRelentlessChange. Force-loop still
requires the character's intrinsic looping flag. The trigger removal iterates
backward and deletes only flagged entries, preserving all surviving entries,
fired flags and the persistent trigger allocation cursor. It does not run
ordinary upgrade-removal effects or add its own callbacks. Relentless adds
rooted immunity; its queued status callbacks drain after the removal through
the native RemoveDeadCharacters phase.

CombatTrigger captures the removal flag through immutable copies and granted
unit/equipment triggers. EnemyDefinition and EnemyMovement carry companion
membership independently of ordinary movement flags. BattleTurnModel invokes
the companion phase after both teams' PreCombat and before energy/draw.
Train movement dispatches OnTrainRoomLoop, PostAscension and OnShift in the
native group order and retains their events for subsequent death/card routing.
This removes the previous blanket rejection of relentless-removal triggers.
Paired companions and outer/final Boss state machines remain explicit
unsupported interactions. Movement Sentry queues are covered in the next section.

Phase comparisons declare stable character/equipment references, using the
same canonical boundary as player decisions. Unity may finish destruction
between coroutine yields, so a marked-Destroyed object can temporarily retain
its raw attacker ID. The capture stores RawBefore, RawAfter and RawActual,
including pending destruction IDs, alongside the native canonical snapshots.
Independent checks verify the raw-to-canonical mapping as well as every
complete phase result. The model rejects phase inputs with unsettled detached
attacker references instead of predicting animation frame timing. Retained
dying-character payloads inside trigger queues remain separately modeled.

`-CompanionBoss` moves the original first-battle Boss container to the second
wave, sets companion/looping membership and removes its starting relentless.
Attack and health remain 7/125, and all ordinary enemy waves remain intact.
Six authored triggers make the actual forced return from floor two observable:
three movement rewards sum to 75 before removal, four flagged triggers are
removed, and the surviving status callback adds 55 afterward. Both archives
record one real native selective-removal operation without changing queued
callbacks or the allocation cursor.

Both captures use schema 60, game 2.2.1 and module MVID
`8fb07b96-f4db-4d2b-884d-c00536d6ccf4`:

| Archive | Bytes / nodes | Native time | Plays / EndTurns | Final Pyre | Compared Boss phases |
| --- | ---: | ---: | ---: | ---: | ---: |
| `full-battle-companion-boss.mt2f` | 17,734 / 2,662 | 44.92 s | 0 / 7 | 49 | 10 |
| `full-battle-companion-boss-actions.mt2f` | 25,474 / 4,197 | 64.33 s | 18 / 6 | 75 | 8 |

Muted Instant runs win with zero capture failures, differences, unsupported or
pending records, and unchanged original files. Complete Boss phases repeat in
32 isolated branches; both whole battles match from their initial roots in 16
parallel branches, with an actual mid-battle root for the card policy. The
card policy includes six observed death returns and raw destroyed-reference
projection coverage. All 84 pre-existing archives, including eight calibration
suites, pass full regression. Both new captures and the historical damage-death
and detached-draw fixtures pass the final independent checker. The expanded
86-file inventory and SHA-256 validation pass, as does probe-script parsing.
Probe builds with zero warnings/errors; ModelChecks retains 12 existing nullable
warnings and no errors. No JSON source fixture is introduced. Equipment-granted
abilities, grafts, moon/deathwish, relics, room attachments and broader Boss
interactions remain work toward the complete simulator.

## Arrival Sentry queues and retained moved targets

Native HeroManager.PostAscensionCharacterTriggers queues PostAscension and
OnShift for each moved character, then queues OnSentry for opposing characters
in the destination room's physical order, with the moved character as an explicit
target. The complete group drains afterward. Looping groups first queue their
OnTrainRoomLoop callbacks; ordinary destination groups run from top to bottom.
Stationary characters do not receive arrival callbacks. OnSentry supplies a zero
integer argument, so positive threshold triggers are accepted but never fire.

TrainCombatModel now builds that complete queue for ascension and exposes a
standalone Sentry transition. The ordinary unit-trigger engine applies supported
damage, status, upgrade and resource effects with the native moved-target
override. LastAttackedCharacter overrides bypass team/status/health/subtype/Boss
filters, while trigger preflight still has no override. Once and silence gates
retain their existing semantics. Queued override targets now retain their latest
health, trigger flags and death state across engines and deferred callbacks;
later guards cannot damage a stale living snapshot of an already killed victim.
Terminal movement retains all preceding floor events for death/card routing.
Effect types and target modes outside the modeled rules remain unsupported.

The native fixtures add repeated, once, ignored-silence and threshold OnSentry
triggers to Stewards. Permanent upgrades add 50 health and reduce size by one;
one Steward starts silenced. The original first-battle Boss retains attack 7,
health 125 and its spawn wave, gains looping and loses starting relentless.
Ordinary waves stay intact. Enemy movement/hit/death gold callbacks make the
shared queue observable. The native guard order is IDs 3 then 2, demonstrating
physical rather than creation order. Normal damage changes the incoming Boss
from 85 to 64 then 57 HP, including native source-card modifiers. The lethal
case changes 85 to zero, then observes the later guard with the same zero-HP
target. Both compare complete room, actor, target and shared resource states.

Both captures use schema 61, game 2.2.1 and module MVID
`8fb07b96-f4db-4d2b-884d-c00536d6ccf4`:

| Archive | Bytes / nodes | Native time | Plays / EndTurns | Final Pyre | Native Sentry callbacks |
| --- | ---: | ---: | ---: | ---: | ---: |
| `full-battle-sentry.mt2f` | 29,354 / 4,772 | 49.88 s | 25 / 8 | 80 | 2 |
| `full-battle-sentry-lethal.mt2f` | 26,440 / 4,313 | 40.06 s | 21 / 7 | 80 | 2 |

Muted Instant native runs win with zero capture failures, differences,
unsupported or pending records, and unchanged original files. Both policies
match independently from initial and actual mid-battle roots in 16 parallel
branches. Four complete Sentry dispatches repeat in 32 isolated branches. Pure
checks additionally cover multiple arrivals, once counters, terminal preservation
of earlier floor events, stationary/opposing-team gates and parent immutability.
The first development recording produced no Sentry callbacks; coverage gates
reject it and it is excluded from regression inputs.

All 86 pre-existing archives pass the complete regression. Both new fixtures and
historical damage-death, attack-trigger and companion-Boss fixtures pass the final
independent checker. The 88-file inventory/SHA-256 and PowerShell parsing checks
pass. Probe builds with zero warnings/errors; ModelChecks retains its 12 existing
nullable warnings and no errors. No source JSON fixture is introduced. The full
simulator still needs equipment abilities, grafts, moon/deathwish, relics, room
attachments and specialized Boss interactions.

## Unit ability cooldowns and native status dictionary slots

Native skills have configured cooldown-after-activation and cooldown-at-spawn
fields, separate from the unit's current `cooldown` status stacks. A relative
adjustment uses the configured duration; an absolute adjustment replaces it.
The duration clamps to at least one. When the remaining stacks exceed the raw
unclamped result, native removal reduces them using that raw result, so negative
or zero adjustments can clear current cooldown while the configured duration
remains one. Nonpositive relative adjustments skip units with a zero duration;
absolute or positive adjustments can retain a duration on units without any
ability. Such a duration does not make the unit have an ability.

Reset uses the spawn or activation duration, only adds a positive shortfall,
never reduces excess stacks, bypasses immunity and retains source-card status
statistics. Native queues OnUnitAbilityUnavailable for zero-to-positive cooldown
and OnUnitAbilityAvailable for positive-to-zero cooldown. Common triggers prepend
before authored triggers at creation. They add the available marker on spawn,
reset spawn cooldown, reset activation cooldown after own activation, add the
marker on availability and remove it on unavailability. The implemented callback
queue carries complete immutable units and defers child callbacks in native FIFO
order. Actual active-skill actions remain work.

Cooldown cleanup exposed a separate native dictionary rule: enumeration follows
Mono entry slots, and new keys reuse the most recently freed slot. In the first
modeled recording, later armor/valor entries appeared in the opposite order even
though all stacks, fields and resources matched. Captures now retain allocated
slots and the free-list chain. Cleanup pushes removed slots onto that chain;
zero-stack dictionary entries stay allocated until an actual cleanup removes
them. New entries consume free slots before extending the array. Every immutable
unit copy carries this state; legacy archives keep their uncaptured null state.
Combat cycle detection includes ability fields and captured dictionary layout.

`full-battle-ability-cooldown.mt2f` uses schema 62, game 2.2.1 and module MVID
`8fb07b96-f4db-4d2b-884d-c00536d6ccf4`. Its 26,367-byte binary archive has 4,303
unique nodes and SHA-256
`76e3b3a179f77adf0f5995f3c435159ea24e41f467934f4227debcd47daffec4`.
The muted Instant native run took 49.98 seconds, won at Pyre 80 and recorded 15
plays / five EndTurns, 45 room stages, nine card cycles, nine train phases and
seven spawns. There were zero capture failures, differences, unsupported or
pending records; original files were unchanged. Two real natural-ability
summons execute the game's unmodified common ability trigger definitions. Their
Stewards gain 50 health and reduced size; Boss stats and ordinary waves remain
unchanged. Five damage-card plays each carry the ordered cooldown-effect chain.

Independent checks reproduce all 89 native cooldown / available-marker effect
states with 32 isolated branches, and the complete policy from its initial and
actual mid-battle roots with 16 parallel branches. Pure checks cover duration /
stack separation, immunity bypass, excess-stack retention, no-ability fields,
LIFO dictionary reuse and parent isolation. Native capture requires actual reset,
adjustment, marker removal and ability-bearing units, preventing a policy that
skips unsupported summons from passing as mechanism coverage.

Skill activation/payment, shared cached ability-card identity, ability lifecycle
assignment/replacement/removal, equipment-granted skills and Horde re-spawn gates
are not covered by this increment. Grafts, moon/deathwish, relics, room attachments
and specialized Boss interactions also remain necessary for the complete goal.

The final `scripts/Check-Models.ps1` run passes the complete 89-file curated
inventory/SHA-256 gate, all pure checks and every native archive, including the
eight calibration suites, with exit code zero. Both historical status-callback
fixtures retain their original seven-kind coverage gate; the new ability fixture
separately requires actual common available/unavailable triggers to have fired.
Probe builds with zero warnings/errors; ModelChecks keeps 12 existing nullable
warnings and no errors. PowerShell parsing and `git diff --check` pass. Failed
development captures are excluded; the accepted archive has no JSON dependency.

## Shared native ability card cache

`UnitOrRoomAbilityCardStateCache.Get` caches a single CardState per definition ID.
Different units using that skill share modifiers, effect counters and play/cost
history on the same instance. The cache is separate from all ordinary card piles
and CardManager.GetAllCards: its references do not become owned cards or entries
in deckStats. Capture observes the private dictionary without requesting new
cards. First native cache accesses register their returned identity immediately,
including access from presentation code; the model owns no mutable global cache.

`CombatContext.AbilityCardCache` records definition-to-instance membership, while
CardRegistry retains the detached card's complete immutable state. Creation uses
the definition's starting upgrades and initialized traits/counters, allocates one
identity, leaves owned piles/statistics unchanged and does not consume pending
next-added upgrades or either gameplay RNG stream. Subsequent accesses return the
same child instance. Native unit definitions and captured ability fields retain
a creation blueprint from the decision input. Natural player summons and enemy
wave spawning ensure the cache before spawn callbacks; OnSpawn card generation
then allocates later identities. All context copies, terminal clearing and combat
cycle signatures retain cache membership. Legacy archives keep their uncaptured
null cache and blueprint fields.

`full-battle-ability-cache.mt2f` uses schema 63, game 2.2.1 and module MVID
`8fb07b96-f4db-4d2b-884d-c00536d6ccf4`. It has 28,676 bytes / 4,730 unique nodes,
with SHA-256
`30bc4353a639e87f879914ff783b103c9243518414f554b15f359bc550b1db91`.
It starts with an empty cache and captures two natural Steward summons sharing
skill card 16. Creation advances NextCardId from 16 to 17 while ordinary owned
card count stays 15. Two OnSpawn generators allocate later ordinary cards. A
second skill definition on the original Boss allocates detached card 22 during
native enemy spawning; Boss attack/health and all original waves stay intact.
Both skill definitions have real starting damage upgrades. The controlled
cooldown effect chain remains on damage cards, providing 97 additional native
cooldown/marker transitions.

The accepted muted Instant native run takes 47.28 seconds, wins at Pyre 80 and
records 17 plays / five EndTurns, 45 room stages, nine card cycles, nine train
phases and seven spawns. Capture failures, differences, unsupported and pending
records are zero, and original profile files are unchanged. Four native cache
contexts independently compare creation/reuse for both definitions in 32
branches. The complete policy matches from initial and actual mid-battle roots
in 16 parallel branches. Coverage requires actual shared unit references,
distinct player/enemy skills, starting upgrades and an initially empty cache;
pre-created cache-only inputs cannot pass this fixture. Dictionary grouping in
the PowerShell gate uses key expressions because the binary reader returns
Dictionary objects, whose keys are not ordinary PowerShell properties.

Pure checks additionally cover an owned card with the same definition, distinct
skill allocation, retained references after cache clear/recreation, invalid
ownership aliases, unchanged statistic membership, unknown setup rejection, unused pending upgrades,
fresh effect counters and parent isolation. Cache clear/recreation is checked
against the native implementation contract, but has no live native Clear sample
in this fixture. Skill activation/payment, ability assignment/replacement/removal,
equipment-granted abilities and Horde re-spawn gates remain necessary work.
This increment does not complete grafts, moon/deathwish, relics, room attachments
or specialized Boss interactions.

The final scripts/Check-Models.ps1 run passes all 90 curated binary archives,
complete inventory/SHA-256 validation, every pure check and all eight native
calibration suites, with exit code zero. Probe builds with zero warnings/errors;
ModelChecks keeps its 12 existing nullable warnings and no errors. PowerShell
parsing and git diff --check pass. The accepted capture is native binary and
introduces no JSON fixture dependency. Development runs excluded from the
curated inventory include the first player-only capture and the enemy capture
that the original dictionary property-name grouping incorrectly rejected.

## Unit ability activation and terminal skills

`PlayCardAction.ActivatorUnitId` identifies a unit skill independently of hand
membership. `UnitAbilityModel` verifies availability, configured floor/hand
restrictions and casting, obtains the shared detached skill card, pays fixed or
remaining-energy cost, and supplies the actor to the spell effect chain. Payment
precedes the player's pre-own callback. The resolving flag surrounds the effects
and is cleared before the own callback queue drains. Common callbacks reset the
actor's current cooldown and remove its available marker. Skill activations are
included in supported action enumeration; sibling units retain their own
cooldowns while sharing the same card modifiers and last paid cost.

Self targeting follows the native exception: it ignores team, health, status
and subtype filters, and only the ignore-Boss mask applies. Spell damage carries
the activator into damage/death handling, preserving LastAttackerId and kill
attribution. `CombatContext.LastAbilityActivatorUnitId` captures the native
retained reference, survives every context copy, and clears on an ordinary card
play. Both draw scheduling and energy effect verifiers preserve the cache and
actor reference when reconstructing complete contexts.

Skill plays update global played history and owned cards' AnyCardPlayed
counters, but do not become owned deck statistics, discard, or increment local
CardState play count. A winning skill clears ordinary piles without restoring
its cached card as a resolving hand card. Cached identities and playback
history remain; native statistics fall back to the permanent deck. A separate
terminal skill verifier checks these invariants while the historical terminal
spell verifier continues requiring ordinary cast/discard restoration.

Three schema-64 native archives use game 2.2.1 and module MVID
`8fb07b96-f4db-4d2b-884d-c00536d6ccf4`. Preparation extends the cache scenario:
two natural Stewards share skill card 16 with its real starting damage upgrade,
and the original Boss retains a distinct skill. Stewards gain 50 HP and lose one
size only once; Boss attack/health and every original spawn wave stay intact.
The player skill chains front-enemy damage, self healing and a future draw.
Its pre-own trigger grants gold and deals three self damage; the heal deliberately
uses an enemy-only team mask. Coverage accounts for armor before requiring real
self healing, plus actual pre-own/own callbacks and shared card identity.

| Archive | Bytes / nodes | Actions / EndTurns | Skill activations | Native seconds |
| --- | --- | --- | --- | --- |
| `full-battle-ability-activation.mt2f` | 28,207 / 4,605 | 17 / 5 | Two fixed-cost plays | 50.89 |
| `full-battle-ability-activation-x.mt2f` | 26,952 / 4,343 | 16 / 5 | One positive X payment and one zero payment | 47.57 |
| `full-battle-ability-activation-lethal.mt2f` | 26,047 / 4,213 | 16 / 4 | Two fixed-cost plays; the second kills the Boss | 46.02 |

All three muted Instant runs win at Pyre 80, preserve original profile signatures,
and have zero capture failures, differences, unsupported or pending records.
Independent recomputation matches all six complete activation states in 32
branches, 203 cooldown effect records, and each whole policy from initial and
actual mid-battle roots with 16 parallel branches. The lethal fixture explicitly
requires a skill to finish the battle. It also verifies the later future-draw
effect does not run after Boss death.

Pure checks cover capped pre-own energy gains after payment, zero/current X
payments, fresh cache allocation, cooldown/resolving/silence/muted/deployment/
hand/cost/identity/floor gates, self-filter bypass, ordinary-card actor reset,
terminal clearing, search enumeration and parent isolation. General muted
trigger suppression is not modeled by this increment: an affected actor cannot
activate, and other muted contexts remain unsupported. Cross-room spell damage
actors, retained self after removal, non-spell skill pipelines, assignment/
replacement/removal, equipment-granted skills and Horde spawning also need
coverage. Grafts, moon/deathwish, relics, room attachments and specialized Boss
interactions remain necessary for the complete battle goal.

The accepted archives are direct native binary captures with no JSON source
dependency. SHA-256 and source metadata live in the curated manifest.

The final `scripts/Check-Models.ps1` run passes all 93 curated archives,
including 85 battle captures and eight calibration suites, with exit code zero.
Complete inventory/SHA-256 validation, pure checks and every native comparison
pass. Probe builds with zero warnings/errors; ModelChecks retains its 12 existing
nullable warnings and introduces none. PowerShell parsing and git diff --check
pass. Failed development coverage-gate captures remain excluded from the curated
inventory; the accepted archives retain complete native and independent proof.

## Unit ability lifecycle primitives and permanent spawn suppression

`AbilityLifecycleModel` models the native CharacterState assignment/removal APIs.
Unit states now retain the original and remembered ability definitions separately
from runtime cooldown fields. Equipment replacement keeps the original ability;
direct replacement clears that history. Removing an equipment ability restores
the original using its raw activation cooldown, including a zero-spawn-cooldown
original. A changed runtime duration is not used for restoration.

Assignment and removal update the native status registry, dictionary holes,
common trigger origins and persistent trigger allocation cursor. Removal yields
its first common-trigger operation. At a quiet direct-API boundary, the main
combat loop processes queued status callbacks during that yield, before the
remaining old common triggers disappear or replacement triggers are appended.
The available marker may survive removal even after the unit no longer has an
ability. A zero-spawn assignment subsequently removes one available marker.
These are observed native behaviors; collapsing the operation into an atomic
trigger replacement changes the resulting flags and statuses. The primitive
can return deferred callbacks for integration inside a running trigger queue;
that integration still requires its own native coverage.

The probe's quiet boundary checks both IsRunningTriggerQueue and the actual
pending queue count. A false running flag alone was insufficient: callbacks
could remain queued and contaminate the next API record. No-op cases now
compare only after the preceding operation fully settles.

Permanent removal appends the removed definition ID to the run disable list,
preserving order and duplicates. Removing an equipment ability disables that
ability and still restores the original. Explicit assignment can grant a
previously disabled definition. Natural player summons and enemy spawning
consult the list before adding ability markers/common triggers or allocating a
cached ability card. Authored starting statuses remain separate from the
synthetic ability marker; card upgrades apply afterward.

`full-battle-ability-lifecycle.mt2f` uses schema 65, game 2.2.1 and module MVID
`8fb07b96-f4db-4d2b-884d-c00536d6ccf4`. Its source is
`.probe-runs/full-battle-units-spells-and-junk-20261008-003754-fddea59f/full-battle.mt2f`.
The direct binary archive contains 26,797 bytes and 4,398 unique nodes; SHA-256
is `112a5069c421463b7c4939b7c59cea5e0270b6b816a5df6ceb8e6e4b90d7882e`.
There is no source JSON dependency. The native run was muted, used Instant,
finished in 42.21 seconds, exited zero and left original profile files unchanged.
Capture failures, differences, unsupported and pending records are all zero.

Before the first recorded policy decision, one natural Steward summon is followed
by 21 native API operations: ordinary/null/no-ability no-ops, repeated assignment,
zero-spawn cooldown, repeated equipment replacement, original restoration,
direct replacement, equipment restrictions, restoration after runtime cooldown
9 versus raw cooldown 3, permanent removal/restoration and duplicate disable
entries. A regular enemy definition is given the same authored skill to verify
later disabled enemy births; its stats and original wave placement remain intact.
The original Boss keeps its distinct skill, stats and spawn wave.

All 21 complete room/context transitions compare independently in 32 branches.
Eight native cache contexts and 16 common cooldown-effect snapshots also compare
in 32 branches. The subsequent policy has 16 plays, five EndTurns, 45 room stages,
nine card cycles, nine train phases and seven spawn phases. It matches from its
initial post-API state and actual mid-battle roots in 16 parallel branches,
winning at Pyre 80. Both player and enemy birth suppression have required native
coverage gates. The historical cache/cooldown suites keep their complete coverage
requirements; unrelated lifecycle captures compare every observed operation
without requiring an empty initial cache or a different suite's effect matrix.

These primitives do not yet enable SetUnitAbility/RemoveAbility card effects or
equipment attachment grants in the search action pipeline. Those integrations,
inside-queue lifecycle changes, Horde, grafts, moon/deathwish, relics, room
attachments and specialized Boss interactions remain part of the full objective.

The final scripts/Check-Models.ps1 run passes all 94 curated archives: 86 battle
archives and eight calibration suites, with exit code zero. Complete inventory
and SHA-256 validation, pure checks and every native comparison pass. Probe has
zero warnings/errors; ModelChecks retains its 12 existing nullable warnings and
adds none. PowerShell parsing and git diff --check pass. The full battle
simulation objective remains active.

## Ability card effects and running-queue replacement (2026-10-08)

`CardSpellModel` and `RoomCombatModel` now execute native SetUnitAbility and
RemoveAbility through `AbilityLifecycleModel`. Both iterate a fixed target
collection in reverse, ignore unused integer/range parameters, and preserve
metadata through modifier and trigger-effect copies. Their native base tests
allow empty target collections; null/non-ability assignment remains a no-op.
Removal honors its effective skip-if-relic-active gate. This gate does not add
general relic support: active global relic interactions still require modeling.
Card/trigger execution defers availability callbacks to the existing queue,
unlike the quiet direct API's yielding boundary covered by the previous fixture.
The reachable root catalog now recursively includes abilities assigned by card,
summoned-unit and enemy trigger effects before any assignment or activation.

`AbilityEffectsScenario` prepares real Steward skills and one ordinary spell
at deployment turn zero. A multi-target spell assigns a skill to enemies,
then removes/reassigns the sticky initial last-target group. A pre-own character
trigger assigns a different skill inside the running queue. The already selected
cached skill still executes: the first skill replaces itself, and the second
removes the current pre-own replacement permanently. Therefore the disabled IDs
are the queued replacement's ID, twice in order, rather than the cached played
card's ID. Original Boss stats and ordinary spawn waves stay intact. The removal
gate uses a cloned concrete relic definition with a fresh ID and no collected
state. Unity cannot instantiate the abstract RelicData base; the failed initial
setup capture is excluded from the curated inventory.

`AbilityEffectProbe` observes complete before/after room/context states for every
actual effect; `StatusCallbackProbe` additionally records pre-own, own, available
and unavailable dispatches and their generated queue payloads. Independent
checks recompute all 17 effect transitions and 17 dispatches, including all 35
generated payloads, in 32 isolated branches. Complete room and retained actor
states, payload order/values, cached identity, raw cooldowns, once flags and
trigger allocation match. Pure checks cover reverse permanent-disable order,
null/empty/relic gates, no RNG consumption, shared cache, missing definition
rejection, deferred running-queue callbacks and immutable parallel branches.

The accepted native run is
`full-battle-units-spells-and-junk-20261008-010935-17c85a23`, muted with Instant
timing and isolated save files. It wins at Pyre 80 with 17 card plays (four skill
activations), five EndTurns, 48 room stages, nine card cycles, nine train phases
and seven spawn phases. Capture failures, differences, unsupported and pending
counts are zero, exit code is zero, and original files are unchanged. Its schema
66 archive `tests/fixtures/full-battle-ability-effects.mt2f` contains 4,734 unique
nodes in 29,332 bytes, with SHA-256
`d0989e70226ecd4115098f7faf7f543eddd83184a2c4eb389fe05dc1a08aa8c1` and no JSON
source dependency. The entire policy matches independently from initial and
actual mid-battle roots in 16 parallel branches. Native capture gates require
the actual multi-target assignment, queued grant, inactive relic gate and both
cached self replacement/removal paths; fallback ordinary card play cannot pass.

Equipment attachment grants, Horde interactions, grafts, moon/deathwish, global
relics, room attachments and specialized Boss state machines remain necessary
for the full battle simulation objective.

The final `scripts/Check-Models.ps1` run passes all 95 curated binary archives:
87 battle archives and eight calibration suites, with exit code zero. Inventory
and SHA-256 checks, pure checks, all independent native comparisons and parallel
branches pass. Probe builds with zero warnings/errors; ModelChecks adds no
warnings beyond its 12 existing nullable warnings. PowerShell parsing and
`git diff --check` pass. The complete simulation objective remains active.

## Unit ability upgrades and equipment integration

`CardUpgradeModifier` now captures the upgrade's incoming ability definition,
common triggers and do-not-replace flag. Equipment, scaled-stat and clone-refresh
copies preserve this metadata. Native upgrade capture continues to reject room
abilities, card-trigger changes and unsupported trait modifications.

Live unit upgrades install/remove upgraded character triggers before changing
the skill, then process upgraded statuses. Keep-existing skips a grant when the
unit already has a skill. Removal changes the ability only when the incoming
upgrade definition matches the current raw skill ID; removing an upgrade that
was skipped does not revoke a different skill. Quiet direct API and equipment
setup retain the native yielding boundary: pending callbacks can drain after
the first common trigger is removed. Playing equipment through a card, or
applying an upgrade inside the running trigger queue, defers those callbacks.
The outer room engine also retains changes that these quiet callbacks make to
other room actors.

Equipment grants use the same overlay history as native SetUnitAbility: repeated
replacements remember the original skill, and removing the current overlay
restores that original with its raw activation cooldown. Direct non-equipment
upgrades clear the remembered original. The fixture upgrades equipment with an
OnUnitAbilityAvailable gold trigger before the skill changes, so incorrect
trigger-installation order or premature callback draining changes the compared
context and flags.

Initial player summons select the base skill, apply permanent ability upgrades
in order, then apply temporary ability upgrades in order. Keep-existing checks
the selected reference before consulting the run's permanent disable list.
Suppression occurs after selection and does not fall back to the base skill.
The selected skill supplies marker/status dictionary slots, common trigger IDs,
its raw definition and spawn cooldown before the ordinary OnSpawn path runs.
Malformed upgrades referencing a card that is not a unit ability reject this
transition explicitly; its native invalid raw-reference setup is not modeled.
The starting rule catalog also follows abilities referenced by owned card
upgrades, spell upgrade descriptors and equipment definitions.

`-EquipmentAbilities` constructs this scenario in the isolated sandbox at turn
zero. A first big Steward has permanent B and temporary C upgrades, so it is
born with C (activation cooldown 6, spawn cooldown 2). C is permanently disabled
and explicitly reassigned before equipment setup. Another big Steward retains
base A under a keep-existing B upgrade. Small Stewards have base A plus a C
upgrade: both later natural summons omit the disabled selected C and all common
skill triggers. Equipment B/C/D replacements, an explicit removal/restoration
and seven direct upgrade API cases execute before the ordinary policy. Two
later equipped units actually activate skill B; its front-enemy damage and
self-heal run through the native card pipeline. Boss stats and all original
spawn waves remain intact.

The muted Instant native run takes 50.55 seconds, wins at Pyre 80 and exits zero.
Capture failures, mismatches, unsupported transitions and pending records are
all zero; original profile files remain unchanged. Schema 67 records 17 actions,
five EndTurns, 47 room stages, nine card cycles, nine train phases and seven
enemy spawn phases. Independent checks compare all 17 equipment operations,
seven direct upgrades, the initial upgraded summon and 22 queued character
dispatches with 12 generated payloads. Those boundaries repeat in 32 isolated
branches; the complete policy matches initial and actual mid-battle roots in
16 parallel branches. Capture and independent checks require the actual
keep-existing birth, disabled upgraded births, raw cooldown restoration, both
quiet/card callback contexts and real equipment skill activation.

`tests/fixtures/full-battle-equipment-abilities.mt2f` contains 5,000 unique nodes
in 31,439 bytes, SHA-256
`3bbbeae20c27930358f8df2e4058c7e0008be9d93e4c98b3d00f6d2b860162b2`.
It is a direct native binary archive with no text source or JSON dependency.
The curated inventory is now 96 archives: 88 battles and eight calibrations.

Broader native coverage remains necessary for ability upgrades in generated
cards and nested upgrade-granted triggers, damage/death during quiet lifecycle
yields, Horde interactions, grafts, moon/deathwish, global relics, room
attachments and specialized Boss state machines. The complete battle simulation
objective remains active.

The final `scripts/Check-Models.ps1` run passes all 96 curated binary archives
with exit code zero, including inventory/SHA-256 verification, pure checks,
all independent native comparisons and parallel branches. Probe builds with
zero warnings/errors; ModelChecks retains its 12 existing nullable warnings.
Both changed PowerShell scripts parse successfully and `git diff --check` passes.

## Horde numerical primitives and native calibration

`HordeStatModel` implements the numerical step in native
StatusEffectHordeState.OnStacksChangedUpdateCharacterStats and the separate
health-based casualty test/count. The definitions retain authored attack and
health per troop, distinct from the unit's current raw fields. First additions
replace those fields with authored stats times added stacks. Later additions
add to the existing fields. Removals always subtract authored attack, but assess
current HP and max HP separately using native single-precision division and
ceiling: a field already in the remaining troop-count bucket is retained.
The casualty count preserves at least one troop for the later physical death.

Native integer multiplication/addition wrap. The HP setters independently cap
at 99999, retaining negative growth results and HP above max HP; the attack setter
does not lower-clamp raw damage. The casualty threshold uses its own signed
integer product, so a positive computed removal count is not sufficient to
dispatch it. Native status stack setters cap at 9999 before those checks.
The model keeps the threshold decision and computed count separately.

`-HordeStats` invokes the actual private native numerical methods synchronously
at the first decision. It temporarily substitutes a cloned character definition
and a copied status dictionary on the live Pyre actor. UI updates are skipped
only for that actor during calibration. The casualty method's call to remove
Horde is intercepted to record the requested count, avoiding status callbacks,
simulated death statistics and trigger queues. All original definition/map
references, raw attack, HP/max HP and the dirty flag are restored in finally;
complete actor and context captures must match afterward. Thus this is a
numerical oracle, not an end-to-end Horde status or battle fixture.

On game 2.2.1, MVID `8fb07b96-f4db-4d2b-884d-c00536d6ccf4`, all 560 numerical
steps and 175 casualty boundaries match with zero differences. The matrix
includes first/later additions, removals across separate HP buckets, zero
private-method steps, exact damage thresholds, lethal HP, overflow and caps.
Four actual steps retain HP above max HP and 19 produce negative growth HP.
Independent checks recompute all samples in 32 branches and preserve parent
snapshots. The subsequent ordinary steward-once battle also matches all 55 room
stages, 13 card cycles, 14 train phases, 11 enemy spawns and seven EndTurns,
including the entire chain in 16 parallel branches; it wins at Pyre 49.
The muted Instant run takes 36.46 seconds, exits zero and leaves original files
unchanged. All battle capture failure/mismatch/unsupported/pending counts are zero.

`tests/fixtures/horde-stat-calibration.mt2f` is a direct native schema 1 archive:
4,625 bytes, 1,051 unique nodes, SHA-256
`1705f0c276404b5f0129f8c97ed5eb3d9be0271ab31c219c4a54bfd326bd2ccb`.
It has no text source or JSON dependency. The inventory is now 97 archives:
88 battles and nine calibration suites.

Horde remains explicitly unsupported by ordinary room validation until this
numerical foundation is connected to starting/runtime status additions and
removals, OnHit/max-HP casualty settlement, simulated deaths, repeated rally /
harvest callbacks and OnTroopAdded/Removed payloads. Repeated spawns must also
retain native IsSpawning cooldown gates; summoning, movement merging and clone
behavior need complete native state comparisons. These requirements remain
part of the complete battle simulation objective, together with grafts,
moon/deathwish, global relics, room attachments and specialized Boss state machines.

The complete 97-archive regression exits zero, including inventory/SHA-256
verification, pure checks, all independent native comparisons and parallel
branches. Probe builds with zero warnings/errors; ModelChecks retains its 12
existing nullable warnings. Both changed PowerShell scripts parse successfully
and `git diff --check` passes.

## Queued trigger repetition for Horde lifecycle

Native CombatManager.QueueTrigger retains a signed triggerCount on one queue
record. CharacterState.FireTriggers tests conditions and marks each accepted
trigger before multiplying its native fire count by that record count with
unchecked integer arithmetic. A once trigger therefore executes all repetitions
in the first batch; it is skipped in later records. Even zero and negative
counts mark the flag before doing no effects. Splitting a repeated record into
multiple single-count records would lose this behavior.

QueuedCharacterTrigger and the local/external room queue paths now retain that
count, defaulting to one for ordinary callbacks and older archives. Conditions,
silence, explicit canFireTriggers and deployment gates still run at their native
positions. Effects may enqueue children during each repetition, but those
children drain after the parent batch finishes. This is required for Horde's
repeated rally/harvest callbacks; Horde status callers are not connected yet.

`-TriggerRepeats` extends the conditional fixture with native OnUnscaledSpawn,
OnSpawnNotFromCard and OnShift queue records after real summons and upgrades.
Eight batches exercise first/spent once triggers, silence and explicit fire
permission, zero and negative counts. The first three-repeat batch gives 180
gold before its three armor children drain for 20, 5 and 5 gold, proving both
queue order and the child's once flag. Pure checks additionally verify native
fire-count multiplication and signed product overflow. No native overflow case
is claimed. Immediate drawing via trigger-stackable effects remains unsupported
and needs its native aggregate application semantics when added.

Game 2.2.1, MVID `8fb07b96-f4db-4d2b-884d-c00536d6ccf4`: eight complete batch
states and 26 complete room/actor/dying dispatches independently match in 32
branches. The subsequent battle matches 62 room stages, 13 card cycles, 14 train
phases, 11 spawn phases, seven plays and seven EndTurns; it wins at Pyre 68.
The complete policy from initial and mid-battle roots matches in 16 branches.
The muted Instant native run takes 44.80 seconds and exits zero, with capture
failure/mismatch/unsupported/pending counts all zero and original files unchanged.

`tests/fixtures/full-battle-trigger-repeats.mt2f` is direct native schema 68,
26,319 bytes / 4,135 nodes, SHA-256
`7e3ef1f226b4aae1db5f73cf5337f11a2f3a7cfecdc805c5da74162cc6735a86`.
It has no JSON dependency. The curated inventory is 98 archives: 89 battles and
nine calibration suites. Horde status additions/removals, simulated deaths,
casualty settlement, spawning cooldown gates, merging and cloning remain required
before Horde is supported by ordinary battle transitions.

Current native source tracing locates merge decisions and settlement in
CardEffectBump, rather than same-floor FloorRearrange. A move can select an
occupied recipient spawn point when the destination contains another same-team
Horde; CanMergeCharactersInRoom takes the first such actor without requiring a
matching character definition. Merge adds the incoming troop count using the
recipient's authored stats, then despawns/removes the incoming actor with
death=false. The Bump effect type suppresses Horde's re-spawn/rally callbacks,
while ordinary OnStatusEffectChanged and OnTroopAdded are still queued. The
non-death removal skips InnerCharacterDeath's death and harvest work, but standby
card/equipment returns still need native observations. These are source-derived
integration requirements, not verified end-to-end merge model behavior.

The complete 98-archive regression exits zero, including manifest/SHA-256
verification, pure checks, all independent native comparisons and parallel
branches. Probe builds with zero warnings/errors; ModelChecks retains its 12
existing nullable warnings. Both changed PowerShell scripts parse successfully
and `git diff --check` passes.

## Horde status changes, casualties and spawn cooldown gates

Schema 69 captures each unit's authored troop attack/health and native
IsSpawning flag. Immutable unit copies preserve those fields through spells,
upgrades, equipment, combat, movement and ability changes. `HordeStatusModel`
connects the calibrated numerical primitive to status additions/removals,
OnHit settlement and nonlethal maximum-health debuffs. Starting Horde statuses
replace the upgraded starting attack/health with authored troop totals, while
independent attack-buff and upgrade bookkeeping remains intact.

Positive additions queue the native re-spawn phases for an existing Horde and
enemy-first rally records for other room actors, including untouchable actors.
Those records retain their troop repetition count and last-spawned override.
Removed troops increment simulated death statistics and queue enemy-first
harvest records with the retained dying actor and removed count. Ordinary status
notifications follow those callbacks. OnTroopAdded/OnTroopRemoved use the native
total/delta payloads and threshold gates. Zero and negative additions still
notify troops; negative additions change the status count without changing raw
Horde attack or HP. Ordinary HP damage keeps current HP during casualty removal
when it already lies in the surviving troops' health bucket.

`-HordeStatuses -Policy units-and-junk` uses two actual Steward card plays in
floor zero, keeping the original Boss and waves. They have authored attack 8,
health 25 and two starting troops, plus a real unit ability with spawn cooldown
2 and activation cooldown 3. Their permanent +50 HP upgrade does not overwrite
the initial Horde total of 50 HP. The second Steward observes rally/harvest
queues while the first undergoes eight actual native operations:

| Operation | Troops | Raw attack | HP / maximum HP | Simulated player deaths |
| --- | --- | --- | --- | --- |
| Remove all current cooldown | 2 | 16 | 50 / 50 | 0 |
| Add two troops | 4 | 32 | 100 / 100 | 0 |
| Add zero troops | 4 | 32 | 100 / 100 | 0 |
| Add negative one troop | 3 | 32 | 100 / 100 | 0 |
| Remove one troop | 2 | 24 | 75 / 75 | 1 |
| Apply 51 actual card-attributed damage | 1 | 16 | 24 / 50 | 2 |
| Add two troops again | 3 | 32 | 74 / 100 | 2 |
| Debuff maximum HP by 30 | 2 | 24 | 44 / 45 | 3 |

The cooldown remains zero after both positive additions. Two actual OnSpawn
ResetCooldown effects run while IsSpawning is false and preserve that zero;
the two initial summons run the same effect while IsSpawning is true and set
cooldown 2. Independent effect checks preserve the source trigger kind when
recomputing this native gate.

Direct API calls lack the ordinary card-resolution protection against UI
preview updates. The scenario waits for native preview/queues to become quiet,
temporarily uses the game's SuppressCombatPreviewUpdates property while these
operations yield, and restores it before the complete battle. An earlier
unaccepted sample exposed preview switching queues during a status operation.
The accepted recording preserves actual accepted queue entries and actual
dispatch order separately from QueueTrigger calls, with zero remaining entries
and no active preview at each boundary. Independent checks compare complete
operation/drain room/context states and all queued/dispatched scalar payloads
in 32 branches, including children generated by the availability callback.

The muted Instant native run takes 41.31 seconds, wins at Pyre 80, and records
43 room stages, nine card cycles, nine train phases, seven spawns, six policy
plays and five EndTurns. Capture failures, mismatches, unsupported and pending
counts are zero; original profile signatures are unchanged. The independent
complete policy also matches from initial and mid-battle inputs in 16 branches.

`tests/fixtures/full-battle-horde-statuses.mt2f` contains 23,825 bytes and 3,548
unique nodes, with SHA-256
`71c0ff606fa826dec2bd82236a3f958a4a4995913177cabb764b751d17fe1ad1`.
It is a direct native binary archive with no JSON source. The curated inventory
is now 99 archives: 90 battles and nine calibration suites.

This establishes Horde status/casualty transitions within the verified rules.
Ordinary validation still rejects actors with unmodeled rally/harvest effect
triggers, and last-spawned target execution remains separate work. Final-stack
status removal explicitly rejects a transition until physical-death settlement
is integrated; runtime upgrades that need ordered Horde status/health casualty
work also reject. Bump merging, cloning, additional statuses and physical
harvest routing remain necessary before broader Horde battle support.

The complete 99-archive regression exits zero, including all 90 native battle inputs, nine calibrations, immutable parallel branches and manifest/SHA-256 checks. Probe builds with zero warnings/errors; both changed PowerShell scripts parse and git diff --check passes.

## Physical death Harvest routing

`HarvestModel` connects physical removal to the ordinary character trigger
engine. A unit finishes OnDeath and its queued children before Harvest. For
enemy deaths it dispatches OnAnyHeroDeathOnFloor; for player deaths it dispatches
OnAnyMonsterDeathOnFloor. OnAnyUnitDeathOnFloor follows. Each kind snapshots the
surviving players, queues and drains that group, then snapshots and drains the
enemies. Removed actors do not observe their own death. Non-death despawns skip
physical Harvest. Local room combat and the external train/card queue use the
same group stages, retaining the current dying object across death children.

The dying Horde's status snapshot determines repetition: Monster and Unit
Harvest repeat for its remaining troops, while Hero Harvest remains one. This
differs from simulated troop deaths, whose existing queues remain enemy-first
and interleave Monster/Unit per observer. Lethal ordinary damage settles Horde
casualties on OnHit first; lethal maximum-health loss preserves the Horde count
until physical removal. Required dying statuses see changes made by OnDeath
children, and existing once/silence/fire-count semantics apply to Harvest.

`-HarvestTriggers -Policy units-and-junk` uses two actual Steward plays plus
three native SpawnHeroInRoom calls with an isolated copy of an ordinary enemy
definition. Both teams carry authored Harvest rewards, once rewards and
required-dying armor/Horde conditions. A visible Horde condition reward is
blocked on the silenced second Steward. Death triggers add armor before
Harvest, and the resulting armor child retains the dead actor. The original
Boss, ordinary enemy definitions and spawn waves remain unchanged.

Four actual API operations kill an enemy and a player by maximum-health loss,
then an enemy and a player by damage. Whole settled room/context states match
independently, and observed dispatch order proves own-death/child/kind/team
ordering for both maximum-health deaths. All 31 native OnDeath/Harvest
dispatches independently compare complete room, actor and dying states.
Both checks repeat in 32 branches. Pure checks also compare local/external
queues and prove ordinary versus Horde physical repetition and dead exclusion.

The muted Instant native run takes 45.34 seconds, wins at Pyre 49, and records
55 room stages, 13 card cycles, 14 train phases, 11 spawns, nine policy plays
and seven EndTurns. Capture failures, mismatches, unsupported transitions and
pending records are zero; original profile signatures remain unchanged.
The complete subsequent policy matches from initial and mid-battle inputs in
16 branches.

`tests/fixtures/full-battle-harvest-triggers.mt2f` is direct native schema 70,
28,694 bytes / 4,567 nodes, SHA-256
`b042c3d62aac10e7095981abd096f3fe6b34dfe378eea8ef951a094ea6e2deb7`.
It has no JSON dependency. The curated inventory now contains 100 archives:
91 battles and nine calibrations. Final Horde-stack status removal and its
physical sacrifice, runtime Horde casualty upgrades, Rally, Bump merging,
cloning and additional statuses remain separate incomplete mechanisms.

The complete 100-archive regression exits zero, including all 91 native battle
inputs, nine calibrations, immutable parallel branches and manifest/SHA-256
checks. Additional pure checks compare immediate and deferred external removal
queues with local Harvest, and the final focused native check verifies exact
reward, remaining-troop and death-child boundaries. Probe builds with zero
warnings/errors; ModelChecks retains its 12 existing nullable warnings. Both
changed PowerShell scripts parse and `git diff --check` passes.

## Status removal and final Horde sacrifice, schema 71

`StatusRemovalModel` separates the raw CharacterState API from
CardEffectRemoveStatusEffect. Raw removal lowercases the ID, uses unchecked
subtraction with native stack caps, and treats -1 as removal of every stack.
Zero or negative removed counts do not change Horde numerics or emit removal
notifications. The effect skips missing/zero statuses, clamps its request to the
current count and applies its subtype test independently of target collection.

Removing every Horde stack through the raw API sets HP to zero and reports
simulated troop deaths. It does not signal physical death, execute OnDeath or
return the spawner. Its orphan standby card remains represented. The card effect
then explicitly sacrifices the zero-stack actor: physical death statistics and
LastSacrificedMonsterStats use the actual source, and retained death callbacks
preserve SacrificeCardId across immutable copies. Standby settlement remains a
separate boundary, rather than being inserted into the raw status operation.
The effect probe records native card-playing state so direct setup calls and
actual card effects retain their observed settlement boundaries.

Raw removal also retains actors that were already at zero HP when passed in a
retained trigger room. Lethal damage must still settle the OnHit Horde casualty
and then physical death; filtering that actor before casualty processing would
break its queued lookup. The existing four-death Harvest archive and an explicit
lethal-damage check verify this distinction from a fresh final-stack removal.

Outside a running trigger queue, the sacrifice drains own-death children before
physical Harvest groups. Inside an existing queue, native removal queues the
physical Harvest groups immediately; own-death children append later. Both
paths retain the same dying actor as its status and once flags change.

`-HordeRemoval -Policy units-spells-and-junk` authors two initial Horde Stewards
and four ordinary-enemy observers while preserving the original Boss and waves.
Nine actual operations cover zero, negative, partial and missing removal, raw
final removal on both teams, effect final removal on both teams and a PreCombat
enemy self-removal. A further Steward is created with the native AddNewCard API
and actually played. An ordinary owned spell is drawn natively if absent from
the deployment hand; its real paid play removes the remaining enemy and this
new player Horde, then executes its later healing effect and card callbacks.
No later native result is used to choose a simulated action or target.

Independent checks compare complete operation rooms and retained actors both at
effect return and after the additional queue/standby boundary, exact accepted
queue counts, dispatch order, 63 death/Harvest phase room/actor/dying states,
28 cooldown/removal effect states and the complete paid spell. All mechanism
comparisons repeat in 32 independent branches. The subsequent 19-play,
seven-EndTurn policy matches from initial and mid-battle inputs in 16 branches.

The final muted Instant native run takes 53.32 seconds, wins at Pyre 49 and
records 56 room stages, 13 card cycles, 14 train phases and 11 spawn stages.
Capture failures, mismatches, unsupported transitions and pending records are
zero, and original profile signatures remain unchanged. Its direct native
archive `tests/fixtures/full-battle-horde-removal.mt2f` has schema 71,
35,970 bytes / 6,109 nodes and SHA-256
`c9a25d986f7dc3279c3aafc70e1033a2da4d594739fcb52b72f977cba622cd89`.
The curated inventory contains 101 archives: 92 battles and nine calibrations,
with no source JSON dependency.

The final complete 101-archive regression exits zero after the retained zero-HP
actor correction, including all 92 native battle inputs and nine calibrations.
The old physical Harvest and new removal archives also pass a focused check.
The final Probe Release build has zero warnings/errors; ModelChecks retains
12 existing nullable warnings. Both changed PowerShell scripts parse and the
staged Git diff passes whitespace checks.

Already dying actors, player sacrifice inside an existing queue, raw removal
of a terminal Boss and equipment/signal interactions beyond these captures need
further native coverage. Runtime Horde casualty upgrades, Rally, merging,
cloning and additional statuses remain incomplete; this increment does not
establish arbitrary-card or arbitrary-Boss battle completeness.

## Reentrant death signals and queued player sacrifice, schema 72

Death completion, physical removal and the statistics callback are distinct
native states. `UnitDeathState` records HasFinishedDying, IsBeingRemoved, the
presence of the one-shot CardStatistics death listener and the source of any
pending statistics invocation. The probe observes the actual CheckForDeath and
OnCharacterDeath coroutines; it does not derive these fields from a later
recorded result. Immutable unit copies, generated spawn definitions and retained
actors preserve the state. Legacy archives keep a null lifecycle descriptor.

An existing queue changes player sacrifice settlement. The native manager
accepts OnDeath and physical Harvest callbacks, removes the unit and returns its
standby card before those accepted callbacks execute. The model now returns the
player card at this boundary; outside a running queue it keeps the separately
verified later settlement boundary.

A death callback may remove the dying actor's remaining Horde stacks and invoke
Sacrifice again. IsBeingRemoved prevents another physical removal, OnDeath and
physical Harvest batch. This does not suppress the death signal itself: a
remaining CardStatistics listener can increment physical-death and sacrifice
statistics again. Cleared listeners cannot. The original statistics invocation
can also remain pending when the OnDeath observation begins. Independent checks
settle it at the observed phase/effect yield boundary and preserve its original
source. For the player case, two simulated Horde deaths and two death-signal
notifications produce four additional monster-death counts, while only one
OnDeath dispatch and one physical Harvest batch occur.

Room results expose retained actors and executed trigger dispatches internally
for independent verification. The cooldown/removal effect probe now captures
target actors before and after execution, including corpses absent from the
room projection. Checks compare these identities and lifecycle fields alongside
full room/context state, and prioritize updated actor state over old accepted
callback snapshots.

`-HordeDeath -Policy units-spells-and-junk` uses two actual Steward plays and
three ordinary-enemy Horde observers. One player receives a PreCombat final-stack
removal; a player and an enemy receive an additional OnDeath self-removal and
are killed through native DebuffMaxHP. The original Boss and spawn waves remain
intact. Three complete operations compare settled rooms and corpses, exact
accepted dispatch order and the absence of duplicate OnDeath. Forty-five
native death/Harvest phases and 95 cooldown/removal effects compare complete
states and retained targets. Mechanism checks repeat in 32 independent branches;
the subsequent 19-play/seven-EndTurn battle policy matches from initial and
mid-battle inputs in 16 branches.

This scenario also exposed cooldown decay in a room with no player units.
RoomIsInRelentlessPhase depends on a surviving enemy Relentless unit, without
requiring both teams. Cooldown removal now queries that condition at each clear
boundary. An enemy Boss with another enemy preserves both cooldowns; an ordinary
room and player-only Relentless still decay them. This correction was committed
separately as `f78d369`.

The final muted Instant run takes 53.30 seconds, wins at Pyre 51, and records
56 room stages, 13 card cycles, 14 train phases and 11 spawn stages. Capture
failures, mismatches, unsupported transitions and pending records are zero;
original profile signatures remain unchanged. The direct native archive
`tests/fixtures/full-battle-horde-death.mt2f` has schema 72, 36,353 bytes and
6,019 unique nodes, with no source JSON. Its SHA-256 is
`0d4f5138ea96491cfb876e4b6580db546b93e2c71233af1646d74cab6ea7eb7e`.
The curated inventory now contains 102 archives: 93 battles and nine calibrations.

These captures verify both-team repeated Horde sacrifice and queued player card
return. Runtime Horde casualty upgrades, Rally, Bump merging, cloning, revival,
additional statuses and broader death/equipment/relic signal callbacks remain
incomplete. Raw final-stack removal on a terminal Boss still needs separate
native coverage; this does not establish arbitrary-battle completeness.

The complete 102-archive regression exits zero: all 93 native battle inputs,
nine calibrations, parallel branch checks, typed archive hydration and complete
inventory/SHA-256 verification pass. The final native archive also passes a
focused independent check. Probe builds with zero warnings/errors; ModelChecks
retains its 12 pre-existing nullable warnings. Both changed PowerShell scripts
parse, and Git whitespace checks pass.


## Signed raw attack before status buffs

Native GetAttackDamage combines signed AttackDamage and DamageBuff with status
buffs before its final zero clamp. The captured BaseAttack display has already
been clamped and cannot reconstruct a deficit. CombatUnit and the room engine
now use the raw modifier fields when present, with BaseAttack as the legacy
fallback. A raw Horde attack of -3 with four Valor deals one damage, rather than
four. Likewise an ordinary raw/buff balance of -12 plus a 13-point status buff
recovers only one attack. Pure checks cover both deficits and actual exchange
damage. The new native Horde-upgrade battle and the existing attack-buff battle
match complete initial/mid-battle policies and 16 parallel branches after this
correction. Horde runtime upgrade integration and its archive are separate work.


## Horde runtime unit upgrades, schema 73

Unit upgrades now settle each nonlethal maximum-health reduction in native
order: ordinary additional HP, unhealed HP, and an attributed-health removal.
Each step updates troop counts, raw attack, simulated-death statistics and its
accepted callbacks before the next step. New trigger and ability upgrades follow
these numeric steps; authored status upgrades run last. A first Horde addition
resets current attack/HP/max HP to the source troop definition, while retaining
the independently updated attack-from-upgrades field. Growing an existing Horde
adds its source troop stats. Zero status additions are ignored; negative additions
and upgrade removals retain the raw status API's distinct semantics.

An ordinary/unhealed HP step that kills the actor exits the native upgrade before
its later statuses and source-card writeback. That path records nil-source
Sacrifice and physical death. Removing all Horde stacks through an upgrade can
instead leave HP zero without dispatching a death signal; the model retains that
actor's unfinished lifecycle and attached statistics listener. Direct upgrade
results retain corpses and dispatch diagnostics for independent verification.

`-HordeUpgrades -Policy units-spells-and-junk` uses two real Steward plays, a cloned
ordinary enemy observer and the original Boss/waves. Fifteen direct API operations
and one queued PreCombat upgrade cover health before troop changes, two separate
casualties before later growth, healed versus unhealed reversal, associated-health
casualties, negative/zero troop upgrades, first installation, raw final removal,
and a lethal unhealed step that skips a nine-troop addition. The queue case also
verifies actual source-card writeback. Four real paid Rally spells each execute
Horde growth, two-step casualty upgrades and both removals before their ordinary
floor rearrangement. The subsequent 19-play/seven-EndTurn policy matches from
initial and actual mid-battle inputs in 16 parallel branches.

Standalone API calibration temporarily holds the background ProcessEffectsQueue
only while its operation is running, mirroring the occupied background loop of a
normal card effect. Otherwise idle RemoveDeadCharacters drains accepted callbacks
between the yielded HP steps, producing scheduling-dependent results. Gameplay
upgrade, status, death and trigger implementations are unchanged by this probe
isolation. Actual queued-trigger and paid-card cases execute the normal native
queue. Capture comparisons include API-boundary rooms and queue counts, complete
settled rooms and retained actors, exact dispatched order, 40 complete native
death/Harvest phases and 107 retained-target cooldown/removal effect states.
Mechanism checks repeat in 32 independent branches and preserve their parents.

The final muted Instant run takes 57.60 seconds, wins at Pyre 49 and records
58 room stages, 13 card cycles, 14 train phases and 11 spawn stages. Capture
failures, mismatches, unsupported transitions and pending records are zero;
original profile signatures remain unchanged. The native schema-73 archive
`tests/fixtures/full-battle-horde-upgrades.mt2f` is 40,760 bytes with 6,624 unique
nodes, no source JSON and SHA-256
`b44e36e43490dba2f7472a03b3b8143babaac5145ff88b551937cdd56e2cbeed`.
The curated inventory now contains 103 archives: 94 battles and nine calibrations.

This establishes runtime Horde upgrades for the recorded API, queued trigger and
paid effect paths. Horde Rally/summon integration, Bump merging, cloning, revival,
additional statuses and broader equipment/relic/death callbacks remain incomplete.
Specialized room modifiers, Duality, upgrade accumulation and external interactions
still require their own state capture and native coverage. Negative-HP upgrades
applied to retained dying Horde actors also need separate native coverage.


The complete 103-archive regression exits zero: all 94 native battle inputs,
nine calibrations, typed binary hydration, manifest/SHA-256 integrity, immutable
parallel branches and independent room/card/train/turn/action comparisons pass.
The final archive also passes its focused independent API/phase/effect and
complete-policy checks. Probe builds with zero warnings/errors; ModelChecks has
its 12 pre-existing nullable warnings. Both changed PowerShell scripts parse and
git diff --check passes. The separate raw-attack correction is commit `c0e4c95`.


## Dying Horde unit upgrades, schema 74

A negative maximum-health upgrade on an already-dead Horde actor still settles
its troop casualties. Native DebuffMaxHP distinguishes the actor's original life
state: newly lethal loss invokes Sacrifice; an actor that was already dead takes
the Horde settlement path instead. An additional HP step then exits the upgrade
because its actor is dead. It keeps the partially installed numeric/applied-upgrade
changes, skips later unhealed HP, triggers, abilities and statuses, and does not
write a failed addition to the source card. The same early exit during upgrade
removal preserves later unit fields, while the effect still clears matching
source-card temporary upgrades.

Attributed maximum-health ledgers now survive ordinary room, damage and effect
transitions. Their nonnegative amounts and unique keys remain validated. Removing
an upgrade by its ID consumes the associated HP in both direct API and effect
paths, settles any troop casualty and clears that ledger entry before the dead
actor's early exit. Previously only direct API removal admitted this state.

`-DyingHordeUpgrades -Policy units-spells-and-junk` plays two real Stewards and
spawns three copies of an ordinary enemy observer. It installs native self-targeted
OnDeath upgrade effects, then applies five lethal native maximum-health debuffs.
The cases cover ordinary negative HP, positive HP followed by negative unhealed
HP, positive-HP upgrade removal, unhealed-HP removal and attributed-HP removal.
Every dead two-troop actor settles to one troop without another physical death or
revival. Two positive follow-ups also verify that the failed effect does not cancel
the subsequent effect chain: the permanent follow-up writes to the retained
player card, while a unit-death lifetime remains local to its corpse.

Independent checks compare complete operation rooms/contexts, retained corpses,
source-card metadata, actual dispatch order and each accepted callback count.
They also compare seven effect boundaries and all 45 death/Harvest phase states,
repeat those checks in 32 independent branches and preserve parent states. The
background effect loop is held only during standalone calibration, as in schema
73; the native death, status, upgrade and trigger implementations run normally.

The final muted Instant run takes 69.48 seconds, wins at Pyre 53 and captures
56 room stages, 13 card cycles, 14 train phases, 11 spawn stages, 19 card plays and
seven EndTurns. Capture failures, mismatches, unsupported transitions and pending
records are zero; original profile signatures remain unchanged. Complete initial
and actual mid-battle policies match independently in 16 parallel branches. The
accepted native archive `tests/fixtures/full-battle-dying-horde-upgrades.mt2f` is
36,888 bytes / 6,137 unique nodes, has no source JSON, and has SHA-256
`9539d9f350724766e138e717b86465585def7a07d7a84ad862ad09595e4f6dc8`.

These recorded death callbacks start after the original statistics listener has
settled. Upgrades executed while that original death signal is still pending
need their own native coverage. Horde Rally/summon integration, merging, cloning,
revival, additional statuses and broader equipment/relic/Boss callbacks remain
in progress.

The full 104-archive regression exits zero: 95 native battle archives and nine
calibrations pass, including typed binary hydration, complete manifest/SHA-256
integrity and independent action/room/train/turn/complete-policy comparisons.
The affected seven-fixture regression and the new archive's focused checks also
pass. Probe builds with zero warnings/errors; ModelChecks retains its 12 existing
nullable warnings. Both changed PowerShell scripts parse, and git diff --check
passes.

## Paid summon and Horde Rally, schema 75

The battle context now captures the native last-spawned player-unit reference.
All context copies preserve it, MonsterTurn clears it, and stable removal phases
clear a destroyed reference. Legacy archives keep their uncaptured null field.
LastSpawnedCharacter targeting uses a queued override when present, otherwise
this context reference. It requires the target to be in the effect's room and
bypasses ordinary team and target filters. In the native fixture, a player-only
upgrade therefore correctly affects an enemy Horde supplied by the override.

Horde birth and paid-card Rally happen at different times. Starting troops queue
both-team observers with the troop count before the global last-spawned reference
changes. A second birth consequently sees the previously spawned player unit.
Existing Horde growth instead carries its own unit as an explicit override and
repeats each observer's trigger by the added count. Zero growth and removal issue
their ordinary status/troop or death notifications without adding Rally dispatches.

After the unit card's effects, the native played callback increments TimesPlayed
before draining player Rally and then enemy Rally. The resolving card retains its
buffer membership and paid cost until discard. The model now follows that order
and uses the original living, non-spawning room members cached when play began.
New summons and other-floor observers are excluded. Original actors that move
remain eligible in their current rooms; dead actors are excluded. Movement and
death during a paid summon still need dedicated native coverage.

`-RallyTriggers -Policy units-spells-and-junk` plays two real Stewards, with native
Horde starting statuses and authored Rally callbacks, alongside same-floor and
other-floor ordinary enemy observers. Five native raw Horde additions/removals
cover positive, zero, first-enemy birth and enemy growth. Repeated rewards,
once-only rewards, visible triggers under silence, required armor and last-spawned
armor upgrades make the trigger timing observable. The original Boss and waves
remain intact. The isolated background queue hold is limited to the five raw
calibration operations; actual status, summon, card and trigger implementations
execute normally.

Independent checks recompute five complete API/queue/drain room states, ten paid
team phases and all twelve actor/room dispatch states. They verify actual dispatch
order, accepted queue sizes, distinct override/global references, exact rewards,
enemy filter bypass, once/silence behavior and parent isolation in 32 branches.
The complete initial and actual mid-battle policies also match in 16 branches.

The final muted Instant run takes 57.25 seconds, wins at Pyre 80 and captures
41 room stages, nine card cycles, nine train phases, seven spawn stages, 13 card
plays and five EndTurns. Capture failures, mismatches, unsupported transitions
and pending records are zero; original profile signatures remain unchanged.
`tests/fixtures/full-battle-rally-triggers.mt2f` is 30,867 bytes / 4,937 unique nodes,
contains no source JSON, and has SHA-256
`ac1158939bc57d98e13e419343d2c1d58407c017cfd4f0c82d618833c286c1b6`.

A fresh schema-75 terminal-spell run also passes native and independent complete
policy checks: 13 card plays, four EndTurns, Pyre 80 and 16 parallel branches.
Its captured last-spawned references survive ordinary unit/spell actions and
reset at MonsterTurn; the Boss-killing spell clears cards with an explicitly
empty reference. A nonempty reference during terminal clearing still needs
dedicated lethal summon/Rally coverage. The muted Instant run takes 40.52 seconds,
has zero capture failures/mismatches/unsupported/pending records, and preserves
the original profile signatures.

Ordinary and cardless player summons, retained/dead last-spawned targets, lethal
Rally chains, Horde merging/cloning/revival and broader equipment/relic/Boss
callbacks still require separate native coverage and implementation.

The full 105-archive regression exits zero: 96 native battle archives and nine
calibrations pass, with complete manifest/SHA-256 integrity, typed binary
hydration and independent room/card/train/turn/action/complete-policy checks.
The seven affected old fixtures and both fresh Rally/terminal-spell runs also
pass their focused checks. Probe builds with zero warnings/errors; ModelChecks
retains its twelve existing nullable warnings. Both changed PowerShell scripts
parse, and git diff --check passes.

## Terminal summon and lethal Rally, schema 76

An ordinary paid unit summon can end the battle inside its post-play Rally queue.
The model now settles a SpawnMonster card whose destination is Standby through
that terminal boundary. Boss death clears the primary and secondary card piles,
but the resolving card is restored to a fresh Standby allocation when its play
finishes. Its living unit and last-spawned reference remain. Other unverified
terminal card destinations still reject the transition explicitly.

TimesPlayed is incremented before Rally, so even a generated resolving card
retains that increment after the Boss kill. Its PlayCount and TimesDiscarded
also complete, while the played-cost dictionary clears. Subsequent effects in
the same Boss trigger still apply a permanent attack/health upgrade to the new
summon's card, a temporary-until-unit-death armor upgrade to the unit, and gold.
Queued Boss OnDeath and player Harvest rewards also settle before the terminal
decision. Complete native card, unit, statistics and pile states are compared.

Self damage inside a running character queue finishes the Boss's death signal
and removes its statistics listener before physical removal begins. At the end
of that parent Rally trigger, HasFinishedDying is true and IsBeingRemoved is
false. Deferred removal marks the removal batch later, before OnDeath and
Harvest dispatches. The model preserves this distinction instead of marking
every death as being removed immediately. Stable decision capture also clears
a dead/destroyed last-spawned reference; raw trigger observations retain the
in-flight reference for exact callback comparisons.

`-LethalRally -Policy units-spells-and-junk` reduces the ordinary large Steward's
size to one and adds Rally/Harvest gold observers. The original Boss receives
an OnSpawn trigger that generates that same Steward into the hand, plus the
lethal Rally effect chain and OnDeath reward. Its original attack/health and all
spawn waves remain intact. The probe selects a legal Boss-room summon through
the native CanPlayHandCard API to reach the terminal path. Independent complete
policy checks choose a supported Boss-room summon from the current model state;
they do not read future native decisions or reuse captured predictions.

Independent checks recompute all twelve team phases, all six actor/room
dispatches, the complete thirteen-play/four-EndTurn policy, and its actual
mid-battle root. Mechanism checks repeat in 32 branches, and complete policies
repeat in 16 parallel branches with parent isolation. The shared-pile checker
counts slot reuse only for nonterminal actions: terminal clearing resets the
allocation, so the pre-clear free-slot index cannot describe the restored card.

The final muted Instant native run takes 39.39 seconds, wins at Pyre 80, and
captures 28 room stages, eight card cycles, eight train phases and seven spawn
stages. Capture failures, mismatches, unsupported transitions and pending records
are zero; original profile signatures remain unchanged.
`tests/fixtures/full-battle-rally-lethal.mt2f` is 21,543 bytes / 3,333 unique nodes,
contains no source JSON, and has SHA-256
`a583073ce01d40551f8d63947ea6fbee318ea61a5893d988421095d033fc7857`.

This supplies the ordinary paid summon, lethal Rally and nonempty terminal
reference coverage left open by schema 75. Cardless summons, retained/dead
last-spawned targets, movement during Rally, Horde merging/cloning/revival and
broader equipment/relic/Boss callbacks still need native coverage and integration.

The full 106-archive regression exits zero: 97 native battle archives and nine
calibrations pass, with complete manifest/SHA-256 integrity, typed binary
hydration and independent room/card/train/turn/action/complete-policy checks.
The new archive also passes its separate focused regression, and eight affected
older fixtures pass their focused checks. Probe builds with zero warnings/errors;
ModelChecks retains its twelve existing nullable warnings. Both changed
PowerShell scripts parse, and git diff --check passes.

## Shared unit identity allocation, schema 77

Player and enemy births previously allocated identities through the outer
EnemySpawnState only. Nested card or character effects cannot access that state;
inferring a new ID from living units would reuse an identity after its last
holder dies. CombatContext now carries an optional shared NextUnitId, and all
context copies preserve it through card generation, statistics, abilities,
upgrades, status callbacks, death and terminal clearing. Ordinary card plays,
enemy groups and treasure births use the same immutable allocation model.
Outer spawn/decision outputs take the completed shared counter, including any
future nested allocations. A birth advances the counter before initialization
and OnSpawn callbacks; removing units never reduces it.

Native capture observes the existing probe-local reference identity counter,
whose creation hooks assign IDs when characters are born. This is independent
of unit position and native team-specific cheat IDs. Every modern room context
and outer decision/spawn state retains the same value. Missing legacy fields
remain null: existing unit plays/waves use their original outer counter, while
context-only creation explicitly rejects an uncaptured counter. Inconsistent
outer/shared values, existing-unit collisions and exhausted allocation bounds
are explicit unsupported results. They are not silently repaired by guessing
from current units.

Independent checks start from a counter above all living IDs, summon a unit,
kill it, summon again and create an enemy group. The second summon keeps the
next identity, and both enemy births continue the same sequence. Complete child
states match in 32 branches without mutating the parent. Invalid, conflicting,
missing and exhausted metadata cases also reject explicitly, and legacy captures
retain their previous state representation.

Two fresh muted Instant native runs preserve the original Boss/waves and repeat
the schema-75 Rally and schema-76 lethal-Rally scenarios. The ordinary run takes
48.97 seconds, wins at Pyre 80 and compares 50 complete spawn/decision boundaries,
including ten births across recorded actions/turns. The lethal run takes 39.81
seconds, also wins at Pyre 80, and compares 48 boundaries with thirteen births.
Both have zero capture failures, mismatches, unsupported transitions and pending
records, and original profile signatures remain unchanged. Independent complete
policies match all thirteen plays and five/four EndTurns from initial and actual
mid-battle roots in 16 parallel branches. Mechanism checks also repeat in 32
branches, preserving the new counter at each captured callback boundary.

`tests/fixtures/full-battle-unit-identities.mt2f` is 30,824 bytes / 4,954 unique
nodes, with SHA-256
`229dd8ed2603d2b2f07f00dbeaebcf134f6d4518574c1ef2c0a5668ec9713823`.
`tests/fixtures/full-battle-unit-identities-lethal.mt2f` is 21,475 bytes / 3,345
unique nodes, with SHA-256
`ac7353c87345a229588dd185a83887fce36afc7f35c89a6c36549e38f8401f76`.
Both are direct native binary captures with no source JSON.

This supplies the allocation prerequisite for cardless and nested births;
it does not implement their creation effects. CardEffectSpawnMonster integration
still needs its own native coverage for placement, fresh/copied spawner cards,
multiple births, extra upgrades, cardless Rally overrides, death replacement,
equipment transfer and room/relic spawn modifiers. The whole-battle objective
remains open.

Source inspection also distinguishes the cardless flag from a null spawner
card: fallback creation can supply a card while the unit remains cardless.
OnSpawnNotFromCard uses the null-card condition, whereas cardless Rally uses
the separate flag. Runtime integration must verify both independently.

The full 108-archive regression exits zero: 99 native battle archives and nine
calibrations pass, including manifest/SHA-256 integrity, typed hydration and
independent room/card/train/turn/action/complete-policy comparisons. Both new
archives also pass their separate focused checks. Probe builds with zero
warnings/errors; ModelChecks retains its twelve existing nullable warnings.
The changed regression script parses, and git diff --check passes.

## Repeated paid summons and detached source cards, schema 78

`CardEffectSpawnMonster` now supports repeated ordinary primary-unit births from
one paid card. `UnitSummonRule` captures the attempt count and source-card
creation definition. The first unit retains the paid card; later units receive
fresh detached copies of its current state before each birth. Those copies
consume card identities but never enter owned cards or piles, never register
new tracked/stored/deck cards, and never consume pending generation upgrades or
selection RNG. Each has fresh play/payment/effect history and room cache.

Native `CopyCardState` retains both permanent and temporary upgrades even when
`ExcludeFromClones` is true. Unlike `AddCardImpl` copies, this path passes no
exclusion list. Upgrade values receive the existing clone refresh, and later
copies read the live source after earlier Rally callbacks have changed it.

`UnitBirthModel` handles insertion, shared unit identity allocation, initial
statistics/statuses, OnSpawn and OnUnscaledSpawn, the last-spawned reference and
cardless Rally. Extra births carry the cardless status and Endless immunity;
their copied source remains non-null, so OnSpawnNotFromCard does not run. The
newborn remains IsSpawning while player-only Rally dispatches on the same floor
run with an explicit newborn override. Previously born units join later birth
queues. The flag clears when that complete birth returns.

`CardInstanceState.PlayedRoomUnitIds` records native eligible cached membership.
Character effects refresh their source card's cache while the newest unit is
still spawning. The normal paid player/enemy Rally phases then use the current
cache separately for each team. This matters because player Rally can refresh
the cache again before the enemy phase. Card modifier/history transformations
preserve it, and dead/destroyed actors are removed from eligible membership.
Legacy archives keep this optional metadata null.
Cache refresh occurs inside each positive trigger repetition. A zero repetition
still marks the trigger but executes no effects and leaves the cache unchanged.
The companion multi-summon-zero scene verifies this using a fresh detached
cardless observer on floor two, created before the first captured decision.
Native QueueAndRunTrigger dispatches its Rally with triggerCount zero; both the
complete retained actor and room/context states are independently compared.
The subsequent paid battle uses the normal trigger counts.

The paid card's initial size test checks one unit. Remaining births use physical
spawn slots, even when their total size exceeds room capacity. The native scene
asks for four size-one Stewards: the first card creates four; a later card
inserts at position one and creates only the three remaining slots, resulting
in seven units on a capacity-five floor. Detached cardless spawners are allowed
through EndTurn without fake Standby entries and keep existing detached death
routing.

`MultiSummonScenario` changes only isolated Steward definitions and card
upgrades. It adds separate birth/unscaled/no-card sentinel rewards, Rally armor
on the newest unit, and a temporary attack/HP upgrade on each Rally actor. The
latter writes the source card, so later copies must observe the changing
upgrade list. Both permanent and temporary source upgrades also carry clone
exclusion flags, verifying that this native copy path retains them. The original
125-HP, attack-seven Boss and all ordinary wave definitions remain unchanged.

`UnitBirthProbe` records every native creation before/after, and records native
CopyCardState calls independently. `UnitSummonChecks` reconstructs each result
from its own before state and definition, compares complete actual native room
and shared context states, checks the source/cardless and allocation invariants,
and repeats births, copies and both Rally phase kinds in 32 isolated branches.
Complete policies are also recomputed from initial and actual mid-battle states
in 16 parallel branches.

The curated archive is `tests/fixtures/full-battle-multi-summon.mt2f`, a direct
binary capture with no JSON provenance: 24,180 bytes, 3,792 unique graph nodes,
SHA-256 `7814b455c3298c84ebe57712c8e6d7c1c6477ba8a7304b695640a8847417f7c6`.
The muted Instant native run took 55.84 seconds, completed 15 card plays and five
EndTurns, and won at Pyre 80. Capture failures, differences, unsupported stages
and pending work are zero; original profile signatures are unchanged.

The second direct binary archive is
`tests/fixtures/full-battle-multi-summon-zero.mt2f`: 24,643 bytes, 3,842 unique
nodes, SHA-256 `1320a8647847ad4f000c53831f14824d2fcc9f9bd6f4d231f81ff9e386cdbddb`.
It contains eleven complete births, six detached copies, ten paid phases and
28 Rally dispatches, including the separate zero-count operation. Its muted
Instant native run took 73.97 seconds and completed 15 plays and five EndTurns,
winning at Pyre 80 with all four differential/error/pending counters zero and
unchanged original profiles.

This increment covers repeated ordinary primary units with copied sources.
Additional-character/pool selection, null or fresh fallback sources, ignored
card upgrades, death replacement, equipment transfer,
active AfterSpawnEnchant effects and room/relic spawn modifiers remain outside
this verified scope. Unsupported variants still reject transitions. The
whole-battle objective remains open.

The final full 110-archive regression exits zero: all 101 native battle archives
and nine calibration archives pass. Both new files pass complete initial and
mid-battle policies with 16 parallel branches; their births, detached copies
and Rally phases pass independent exact comparisons in 32 branches. The full
curated inventory and SHA-256 manifest match. Probe builds with zero warnings
and errors; ModelChecks keeps its twelve existing nullable warnings. Both
changed PowerShell scripts parse, both summon selectors return one exact
scenario value, and git diff --check passes.


## Extra spawn upgrades after birth, schema 79

`CardEffectSpawnMonster` awaits `CreateMonsterState` and its birth/Rally
callbacks before applying `ParamCardUpgradeData` through the direct character
API. It then adds the same descriptor to the source card's temporary modifiers
outside preview, including when the unit API rejects a unique duplicate or a
capacity-restricted size increase. The direct API does not perform this source
write itself. Later births copy the original resolving card's current state,
so they inherit its first extra upgrade before receiving their own extra
upgrade. Their detached copy's subsequent source write does not modify the
original card.

`SpawnUpgradeModel` represents those two boundaries using the existing direct
unit-upgrade engine and a registry-aware temporary source write. Repeated
summons invoke it after each completed birth and before copying the next source.
The source write preserves history, room cache, traits and ownership. It uses
native unique-ID suppression independently of the unit's rejection result.
Unknown fresh/null source creation is still rejected by the repeated-summon
entry point rather than being approximated.

The probe records full states before direct application, after the direct API
returns, and immediately after `CardStateModifiers.AddUpgrade` returns. The
observer excludes `fromSpawn` applications, so inherited starting upgrades are
not mistaken for the effect's later extra application. Independent checks
recompute both complete transitions without consulting later native states,
require the actual source insertion/rejection result, preserve the parent and
repeat all birth/copy/extra/Rally mechanisms in 32 parallel branches. Entire
policies also compare from initial and actual mid-battle roots in 16 branches.

All three scenes retain the original Boss (125 HP / 7 attack) and waves, two
real upgraded Steward cards, four intended births per card and the previous
live source growth, clone flags and cardless callbacks. Their extra upgrade
adds 2 attack, 3 health and 2 armor. The unique variant sets `IsUnique`; the
restricted variant also adds one size and restricts that increase to room
capacity. Ordinary and unique scenes still exercise seven-slot truncation.
Restricted growth makes the second real card use a different floor, so that
scene proves capacity failure rather than slot truncation; the other fixtures
retain the slot requirement.

| Native archive | Bytes / unique nodes | Extra applications / successful source writes | Plays / EndTurns | Native seconds |
| --- | --- | --- | --- | --- |
| `full-battle-multi-summon-upgrade.mt2f` | 25,049 / 3,911 | 7 / 7 | 15 / 5 | 49.14 |
| `full-battle-multi-summon-upgrade-unique.mt2f` | 24,729 / 3,857 | 7 / 2 | 15 / 5 | 50.74 |
| `full-battle-multi-summon-upgrade-restricted.mt2f` | 23,757 / 3,672 | 8 / 8 | 14 / 5 | 50.51 |

The restricted scene records four accepted unit size increases and four
rejected increases whose source writes still succeed. The unique scene records
five duplicate source rejections after later births inherit the unique upgrade.
All three win at Pyre 80, with CaptureFailures/Mismatches/Unsupported/Pending
all zero. They are direct binary captures from game 2.2.1, module MVID
`8fb07b96-f4db-4d2b-884d-c00536d6ccf4`, run muted at Instant speed in fresh
isolated profiles. Original profile signatures remain unchanged. Their archive
SHA-256 values are recorded in `tests/fixtures/manifest.tsv`; no source JSON is
needed. The curated inventory now has 113 archives (104 battles plus nine
calibrations).

This extends the previous ordinary copied-source summon path. Fresh fallback
sources, pool/additional-character selection, lethal/removed birth targets,
death replacement, equipment transfer, active AfterSpawnEnchant and room/relic
spawn modifiers still require native integration. Broader battle coverage,
complete state-based pruning and a universal optimal solver remain open.




The final full 113-archive regression exits zero: all 104 battle archives and
nine calibrations pass, with no unsupported state-transition checks. The
inventory and archive SHA-256 manifest agree. The three new complete policies
and their 32-branch birth/copy/extra/Rally checks also pass within this full run.
Probe builds with zero warnings/errors; ModelChecks retains its twelve existing
nullable warnings. Both changed PowerShell scripts parse, all three extra-upgrade
selectors produce one exact scenario value, and git diff --check passes.


## Fresh fallback spawners and delayed unit-card returns, schema 80

When `CardEffectSpawnMonster.ParamBool` ignores the resolving card's upgrades,
native code creates a new `CardState` for every birth. It selects the first
spawner definition in `AllGameData.GetAllCardData()` order whose character ID
matches the selected unit, then runs `CardState.Setup`. It does not copy the
resolving card or an earlier newborn's source. The first successful birth still
has a non-null source and is not cardless; subsequent births receive the
cardless marker independently of their new source reference. OnSpawnNotFromCard
therefore does not fire in this path.

`CardGenerationModel.CreateDetached` retains starting modifiers and fresh
history/counters/traits while allocating only a registry reference. It changes
neither ownership/piles, gameplay RNG, statistics nor pending generation
upgrades. The summon descriptor captures the native-order fallback definition
at the root. Each birth uses that descriptor, then applies its extra upgrade
and updates its own new source. The original paid card retains its own upgrades
and history; later sources do not inherit the earlier source's extra upgrades
or Rally growth. Missing fallback definitions or missing unit-binding metadata
remain explicit unsupported results.

Native DiscardCard binds the original resolving card to the first summoned
character. That character can have a different spawner card. A death-local
standby check only looks for the character's own source, so it does not return
the original card. A later global CheckStandByConditions during DiscardCard or
DrawHand evaluates the original binding and returns the card to Exhausted.
`CardPileState.UnitConditions` preserves the host and ready state separately
from the unit's spawner reference, plus the existing physical dictionary slots
and free chain. Room/death queues mark readiness; global boundaries perform the
return and its exhaustion statistic. Legacy archives retain null metadata.
CardPile add/remove/clear and equipment binding preserve this new state.

The spell death queue now treats known unowned registry references like the
room engine: they have no card to move between piles. This lets actual paid
friendly damage kill both ordinary fresh-source and cardless units while the
original cards wait for the global check. It does not relax missing-card errors
for unknown references.

The probe observes real CardState.Setup within the fallback effect iterator,
full birth/extra-upgrade states, the native unit-card standby closures and every
global standby check. Independent checks compare the complete setup and global
return states, preserve parent inputs, distinguish source presence from the
cardless marker, and repeat setups, births, upgrades, global checks and Rally
mechanisms in 32 parallel branches. Entire policies also match from initial and
actual mid-battle roots in 16 branches.

| Native archive | Bytes / unique nodes | Fresh setups / global checks | Plays / EndTurns | Native seconds |
| --- | --- | --- | --- | --- |
| `full-battle-multi-summon-fresh.mt2f` | 25,497 / 4,001 | 7 / 27 | 15 / 5 | 52.25 |
| `full-battle-multi-summon-fresh-deaths.mt2f` | 32,468 / 5,340 | 7 / 39 | 21 / 7 | 54.18 |

Both scenes retain the original Boss (125 HP / 7 attack) and waves, original
resolving-card upgrades, four intended one-size births per card, birth/Rally
rewards and the extra 2 attack / 3 health / 2 armor upgrade. The death scene
changes the existing rearrangement spell to actual lethal friendly Tower
damage. It records original card 1 bound to unit 2 with source 16, and original
card 2 bound to unit 6 with source 20. Both original cards are still in Standby
with ready conditions immediately before the global check, then return to
Exhausted. Independent replay matches all source, pile, statistic, death and
terminal states. Exploratory captures that did not exercise those deaths were
not retained.

These are direct binary recordings from game 2.2.1, module MVID
`8fb07b96-f4db-4d2b-884d-c00536d6ccf4`. Muted Instant runs use fresh isolated
profiles; original profile signatures remain unchanged. CaptureFailures,
Mismatches, Unsupported and Pending are zero. Archive SHA-256 hashes are in the
manifest, and no source JSON is required. The inventory grows to 115 archives:
106 battles and nine calibrations.

This verifies the native primary-unit fallback selected in these scenes.
No-match/null sources, pool/additional-character selection, active relic/room
spawn modifiers, removed birth targets, death replacement, equipment transfer,
active AfterSpawnEnchant and broader revival/Horde creation still require
native integration. The full battle objective remains open.

The final full 115-archive regression exits zero: all 106 battle archives and
nine calibration suites pass, with no nonzero unsupported transition counts.
The binary inventory and SHA-256 manifest agree. Both fresh-source policies
match initial and mid-battle roots in 16 branches, and their setup, birth,
extra-upgrade, global standby and Rally checks pass in 32 branches. The death
scene also preserves three observed card returns and two Standby slot reuses.

## Additional unit selection and mismatched sources, schema 81

`CardEffectSpawnMonster` independently requests at least one primary and one
additional unit for each `ParamInt` count. Without active spawn-count relics,
the requested total is twice `max(ParamInt, 1)`. Native code caps that total by
the remaining physical positions before choosing which character to spawn:
successful birth indices at or beyond integer `cappedTotal / 2` use the
additional character. The insertion-index loop has its own final-position
limit; it does not change the halfway point. Seven available positions therefore
produce three primary and four additional units for a four-plus-four request.
Additional selection alone consumes no gameplay RNG; pooled selection remains
a separate path.

`UnitSummonChoice` retains the additional raw character template and its
native-order fallback source definition. Each birth initializes that selected
template with the live source upgrades, starting statuses, ability state and
callbacks. Ordinary source mode still uses the resolving card for the first
birth and live detached copies of that same card for subsequent births, even
when the additional character differs. `SpawnerMatchesDefinition` is captured
from actual character/source identity and preserved through modifier resolution.
This makes ordinary self-growth upgrade the mismatched live unit without writing
the battle-lifetime upgrade back to its source. The explicit extra spawn upgrade
still applies directly after birth callbacks and writes the source's temporary
modifiers, independent of that match flag.

Fresh source mode chooses a new matching fallback for every selected character.
It therefore switches source CardData when the additional character changes,
retains fresh histories and applies neither the resolving card's upgrades nor
the previous birth's source modifications. The first birth remains non-cardless;
later births receive the marker independently of their selected kind. Original
paid-card standby bindings continue to point at the first host rather than its
fallback source.

The birth probe now captures the actual `MonsterManager.CreateMonsterState`
character argument; the spawner card alone cannot identify a mixed birth. Root
rule closure includes generated-card pools, ability changes and natural ability
cards from both templates. Independent mechanism checks compare complete births,
copies, extra upgrades and Rally phases/dispatches in 32 branches. They separately
require the odd split and both kinds, correct fresh source selection, a mismatched
self-growth without source write and an explicit extra upgrade with source write.
Complete policies compare initial and actual mid-battle roots in 16 branches.

| Native archive | Bytes / unique nodes | Births / detached copies / fresh setups | Rally dispatches | Plays / EndTurns | Native seconds |
| --- | --- | --- | --- | --- | --- |
| `full-battle-multi-summon-additional.mt2f` | 27,610 / 4,474 | 17 / 12 / 0 | 55 | 15 / 5 | 73.23 |
| `full-battle-multi-summon-additional-fresh.mt2f` | 28,056 / 4,491 | 17 / 0 / 14 | 43 | 15 / 5 | 70.31 |

Each scene pays two actual upgraded Steward cards for four primary plus four
additional one-size Stewards. Each occupied floor shows three TrainStewardBig
and four TrainStewardSmall units. Fourteen extra upgrades run in each scene;
seventeen recorded births include three other ordinary unit-card plays. The
original Boss (125 HP / 7 attack) and waves remain intact. Both battles win at
Pyre 80 with CaptureFailures/Mismatches/Unsupported/Pending all zero. Direct
binary captures are from game 2.2.1, module MVID
`8fb07b96-f4db-4d2b-884d-c00536d6ccf4`; original profile signatures are unchanged.
The native game is muted at Instant speed. Only this scenario's observation
deadline grows to 180 seconds after actual progressing seven-birth recording
exceeded the old 30-second limit. Script parameters now reject unknown names;
both new selectors have one exact scenario value and were verified against the
actual archives, so an ordinary baseline cannot count as mixed-unit coverage.

Both focused independent checks exit zero. The curated binary inventory grows
to 117 archives: 108 battles plus nine calibrations. Their SHA-256 values and
node counts are recorded in the manifest; no source JSON is required.
Character pools, active spawn-count relics/room modifiers, missing fallback
sources, removed birth targets, death replacement/equipment transfer and broader
revival/Horde creation still require native integration. Full battle coverage,
complete state-based pruning and a universal optimal solver remain open.

The final full 117-archive regression exits zero: all 108 battle archives and
nine calibration suites pass with no nonzero unsupported transition counts.
The curated binary inventory and SHA-256 manifest agree. Both mixed-source
policies and all 32-branch mechanism checks pass within this complete run.

## Pooled unit selection and no-primary cards, schema 82

`UnitSummonRule.Pool` preserves native pool order and duplicate entries rather
than treating the pool as a set. Every birth draws once from the Battle stream,
including a singleton pool and a draw later replaced by an additional character.
Ordinary mode clones the live resolving source before sampling. The sampled
character is then overridden at the capped total's halfway point, if applicable.
Fresh mode creates its matching fallback source after that selection. Birth
callbacks and the explicit extra unit/source upgrade follow in native order.
`SpawnerMatchesDefinition` uses actual source-character identity, including
the selected fallback, rather than assuming that every fresh source matches.

The scoped pool probe observes the real `RandomElement<CharacterData>` call
inside the native spawn iterator. It records ordered pool entries, the chosen
character and complete before/after contexts. Independent checks recompute
each draw from the captured decision state, verify every born kind/source and
the final Battle RNG, and compare complete sampling boundaries. Singleton
sampling is required to advance RNG. Additional scenes require an actual
sampled result to be overridden; merely configuring an additional character
cannot satisfy that coverage check.

A pooled monster card can have no primary `ParamCharacterData`. Its legality
uses captured `NativeBaseSize` zero, followed by actual card size upgrades and
the native 1..6 clamp. It needs no placeholder unit template. The native
`CanCardTriggerRally` gate rejects the paid card when `GetSpawnCharacterData`
is null, so `TriggersPaidRally` suppresses that post-play phase. Later cardless
births still trigger their own Rally. The initial exploratory no-primary
capture exposed two full-action mismatches from the missing paid-card gate;
that archive is not curated. The corrected native recording passes completely.

| Native archive suffix after `full-battle-multi-summon-` | Bytes / unique nodes | Pool draws / births / copies / fresh setups | Paid phases / Rally dispatches | Native seconds |
| --- | --- | --- | --- | --- |
| `pool-additional.mt2f` | 28,550 / 4,577 | 14 / 17 / 12 / 0 | 10 / 55 | 67.38 |
| `pool-additional-fresh.mt2f` | 28,790 / 4,559 | 14 / 17 / 0 / 14 | 10 / 43 | 80.16 |
| `pool-singleton-additional.mt2f` | 28,327 / 4,541 | 14 / 17 / 12 / 0 | 10 / 55 | 73.68 |
| `pool.mt2f` | 25,476 / 3,933 | 7 / 10 / 5 / 0 | 10 / 26 | 49.55 |
| `pool-no-primary.mt2f` | 24,421 / 3,772 | 7 / 10 / 5 / 0 | 6 / 17 | 47.26 |

Weighted scenes use `[TrainStewardBig, TrainStewardSmall, TrainStewardBig]`;
the singleton scene uses `[TrainStewardBig]`. Additional scenes request four
plus four births and cap to seven before splitting. Fresh mode additionally
records 27 complete global Standby checks. Each scene applies one explicit
extra upgrade per pool draw and finishes after fifteen plays and five EndTurns
at Pyre 80. The original Boss (125 HP / 7 attack) and waves remain intact.
CaptureFailures, Mismatches, Unsupported and Pending are all zero. All five
focused independent checks pass, including complete initial/mid-battle policies
in 16 parallel branches and complete mechanism states in 32 branches.

These are direct binary captures from game 2.2.1, module MVID
`8fb07b96-f4db-4d2b-884d-c00536d6ccf4`. The game is muted at Instant speed;
original profile signatures remain unchanged. All five script selectors are
explicit parameters and were checked against actual scenario metadata.
The curated inventory grows to 122 archives: 113 battles plus nine calibrations,
with sizes, SHA-256 values and unique node counts in the manifest and no source
JSON dependency.

Missing/null fresh sources, active spawn-count relics/room modifiers, removed
birth targets and physical slot holes, death replacement/equipment transfer,
and broader revival/Horde creation still need native integration. Full battle
coverage, complete state-based pruning and a universal optimal solver remain open.

The final full 122-archive regression exits zero: all 113 battle archives,
all nine calibration suites and all five pooled summon checks pass. Complete
policies and parallel mechanism branches pass within this run. There are no
nonzero unsupported transition counts, and archive sizes/SHA-256 values match
the curated manifest. Legacy summon rules retain their constructor defaults.

## Confirmed missing fallback sources, schema 83

Native `CardEffectSpawnMonster` searches the ordered card catalog when it has
no source card. If no matching spawner definition exists, it still creates the
selected character with a null source. `UnitSummonRule` and each
`UnitSummonChoice` now capture `FallbackLookupComplete`: a null creation rule
with this flag records a completed no-match lookup. An absent rule without the
flag remains unsupported. Independent negative checks remove that metadata from
actual decision states and require rejection without a partial child or parent
mutation. Existing positive fallback definitions retain their constructor defaults.

The model creates a detached source only for a matching selected fallback.
A missing additional or pooled fallback cannot fall back to the primary
character's source definition. Source-free births allocate a unit identity but
no card identity, inherit no resolving-card upgrades and use source ID zero.
Their first birth is also cardless, with native Endless immunity, unlike the
first birth with a matching fresh source. `OnSpawnNotFromCard` follows actual
source absence independently of the cardless marker. Existing units receive
the first source-free birth's cardless Rally as well as subsequent births.
Every pool draw still precedes additional override and source lookup, preserving
the complete Battle RNG. Extra upgrades apply directly to the unit; only the
source-card write is omitted when that source is null.

The native birth probe accepts null spawners and uses `fromPlayedCard` only
to capture static definitions. It records source ID zero, actual cardless flags
and complete before/after states. Extra-upgrade observations close at the direct
API boundary for source-free units rather than waiting for a source write that
does not exist. Independent checks compare complete births, upgrades, sampling
and Rally states in 32 branches. They also check fresh setups, card identity
allocation, absence of copied paid-card upgrades and global Standby returns.
Complete policies recompute the initial and actual mid-battle roots in 16 branches.

The original paid card still binds to its first born host, even when that host
has no source. A death-local source check cannot return that card. The recorded
death scene kills source-free hosts with a real paid friendly-damage spell,
marks the original bindings ready, then returns them in a real global Standby
check with exact pile allocation and statistics. Paid Rally remains governed
by the original card's primary spawn definition; no-primary variants suppress
that phase while keeping source-free birth Rally.

| Native archive suffix after `full-battle-multi-summon-` | Bytes / unique nodes | Pool draws / births / missing sources / fresh setups | Global checks | Plays / EndTurns / final Pyre | Native seconds |
| --- | --- | --- | --- | --- | --- | --- |
| `pool-missing-fresh.mt2f` | 25,909 / 4,002 | 7 / 10 / 3 / 4 | 27 | 15 / 5 / 80 | 51.81 |
| `pool-additional-missing-fresh.mt2f` | 27,503 / 4,275 | 14 / 16 / 11 / 3 | 27 | 14 / 5 / 80 | 62.49 |
| `pool-no-primary-missing-fresh.mt2f` | 25,038 / 3,868 | 7 / 10 / 7 / 0 | 27 | 15 / 5 / 80 | 46.67 |
| `pool-no-primary-missing-fresh-deaths.mt2f` | 31,983 / 5,189 | 7 / 10 / 7 / 0 | 39 | 21 / 7 / 49 | 49.93 |

Each scene copies actual Steward `CharacterData` in memory, gives the copy a
distinct fixed ID and verifies that the ordered spawner catalog contains no
matching definition. The original prefab/stats are retained, with one-size
births and explicit spawn/unscaled/no-source/Rally callbacks. Mixed pools keep
one real matched kind; no-primary pools contain only unmatched copies. No
fallback card or future selection is fabricated. The original Boss (125 HP /
7 attack), waves and profile signatures are unchanged. The game is muted at
Instant speed. All four direct binary captures are from game 2.2.1, module MVID
`8fb07b96-f4db-4d2b-884d-c00536d6ccf4`, with CaptureFailures, Mismatches,
Unsupported and Pending all zero. All four focused independent checks pass.

The curated inventory grows to 126 binary archives: 117 battles plus nine
calibrations, with actual sizes, SHA-256 values and node counts in the manifest.
Parentless unit-trigger creation, active spawn-count relics/room modifiers,
removed birth targets and physical holes, death replacement/equipment transfer
and broader revival/Horde creation still require native integration. Full battle
coverage, complete state-based pruning and a universal optimal solver remain open.

The next spawn integration also needs current/last-known physical spawn points.
`CharacterState.ApplyEffects` passes the caster as `selfTarget`, so the native
`IsEffectFromMonsterDeath` helper enters its pivot/compaction path for live
monster-trigger casters as well as dying ones. The loop separates physical
attempt indices from successful-birth counts. Each effect retains its first
born weak reference and does not reset it on a zero-success execution. Those
references and layouts need capture before general trigger/replacement branches
can be treated as complete states.

The final full 126-archive regression exits zero: all 117 battle archives,
all nine calibration suites, all four missing-source checks and all nine pooled
summon checks pass. Initial/mid-battle policies and parallel mechanism branches
pass within this complete run. Unsupported transition counts remain zero, and
the archive inventory, sizes and SHA-256 values match the curated manifest.

## Physical spawn point operations and retained references

The schema-84 `full-battle-spawn-points.mt2f` archive captures the actual slot
occupants independently from each unit's current and last-known slot references.
These can temporarily disagree: setting a slot overwrites its occupant without
clearing the displaced unit's pointer. Moving that displaced unit only clears
its old slot if it still owns that slot. Direct removal retains the old
last-known reference; an explicit remember operation or compaction refreshes it.
Allow-last-known lookup uses that reference only at nonpositive HP.

`SpawnPointModel` separately models backward insertion shifts, adjacent
rearrangement, cross-room assignments and the native compaction loop with its
12-pass limit and pivot movement count. Compaction remembers removed occupants,
retains undying occupants at zero HP, removes preview-born units in a live world
and finally refreshes occupied inner points. Remaining-point counts include
ordinary dead occupants and exclude outer-train Boss units. Group point counts,
SaveManager inner counts and the actual outside-point flags remain distinct.

The native setup creates three source-free Stewards and runs 21 observed native
operations. It deliberately produces holes, stale references, zero HP,
undying occupancy, a preview-born flag and a destroyed detached actor. The
final setup restores healthy actors to owned points before ordinary play.
These controlled flags and raw health edits establish position API behavior;
they do not establish a real death, revival or preview-mode lifecycle.

Independent checks recompute every complete before/after world, all 63 native
current/allow-last-known queries, pivot movements and remaining-point counts.
They verify parent isolation, reject malformed identities/references without
partial children, and repeat the operations in 32 parallel branches. The
subsequent policy also matches from initial and actual mid-battle roots in
16 branches: 18 card plays and six EndTurns, victory at Pyre 80. The native
Instant run takes 45.18 seconds with muted audio, original Boss/waves and
unchanged original profile signatures. Capture failures, mismatches,
unsupported transitions and pending observations are all zero.

The direct archive is 25,202 bytes with 4,058 unique nodes. It was captured on
game 2.2.1, MVID `8fb07b96-f4db-4d2b-884d-c00536d6ccf4`. The inventory now
contains 127 archives: 118 battles and nine calibration suites.

This is an independently checked position component. The ordinary full battle
state does not yet carry this physical world through every transition. General
unit-trigger summons, retained first-born effect references, removed birth
targets, death replacement and equipment transfer still require that integration.

The full 127-archive regression exits zero: all 118 battle archives and nine
calibration suites pass, including one physical spawn-point check, four
missing-source checks and nine pooled summon checks. Unsupported transition
counts remain zero. The complete archive inventory, sizes and SHA-256 values
match the curated manifest.

## Physical positions throughout complete battle transitions

`CombatContext.SpawnPoints` now carries immutable physical groups and retained
unit position references through every existing context-copy path. Position
ownership is separate from health/status inputs: the current operation supplies
its actual combat actors to the physical model. A selected group cannot shift
an occupied point whose retained actor's combat state is unavailable. Invalid
identities, unknown retained points and missing shared counters reject transitions
without partial children. Removing a unit retains its identity and last-known
reference while clearing its current owned point.

Player births use backward insertion, assignment and the native setup's
compaction; enemy and treasure births allocate their actual first empty points.
Enemy ascension reserves destinations from the top down and carries same-room
hole filling and cross-room assignments before subsequent callbacks. Floor
rearrangement updates physical references as well as room unit ordering.
Immediate death, queued completion and treasure despawn clear their points at
their own boundaries. Train combat includes the centering step after each
nonterminal room; an individual room capture still precedes that step. This
preserves the observed temporary holes instead of flattening every capture.

The probe's optional `-PhysicalSpawnPoints` switch records primary native
points and each observed unit's primary current/last-known references, including
units retained after removal. Preview and temporary-preview groups do not pollute
those position identities. Captured compaction records remain diagnostic native
boundaries; complete room/train/action/turn checks independently recompute the
full contexts, including this new state, rather than trusting recorded predictions.

The schema-85 `full-battle-physical-spawn-points.mt2f` archive starts after the
21-operation position setup and completes 18 card plays and six EndTurns at
Pyre 80. Its 102 complete room contexts contain 17 physical removals and five
cross-room moves. The 43.66-second native Instant run records 254 compaction
boundaries. Initial and actual mid-battle roots pass in 16 parallel branches;
complete room position comparisons pass in 32 branches. The archive is 26,962
bytes with 4,408 unique nodes.

`full-battle-physical-spawn-points-no-cards.mt2f` keeps the ordinary starting
battle and plays no cards. Seven EndTurns finish at Pyre 49. Its 104 room
contexts contain 13 physical removals and twelve cross-room moves, including
the Pyre path; 233 native compaction boundaries are retained. The independent
initial root matches in 16 branches and complete rooms match in 32 branches.
The native Instant run takes 35.44 seconds. This archive is 18,096 bytes with
2,735 unique nodes.

Both native runs finish with zero failures, mismatches, unsupported transitions
and pending observations. Audio is muted, original Boss/waves stay intact and
original profile signatures are unchanged. Captures use game 2.2.1, MVID
`8fb07b96-f4db-4d2b-884d-c00536d6ccf4`. The inventory contains 129 archives:
120 battles and nine calibration suites.

Native position capture is opt-in while the remaining physical paths are
expanded. General unit-trigger spawning and retained first-born effect caches,
death replacement/equipment transfer, revival, Horde merging and broader
relic/room effects remain incomplete. Spell movement and special Boss paths
still need position-aware native fixtures before broad physical coverage can
be claimed. These two full battles establish the integrated paths above.

The full 129-archive regression exits zero: all 120 battle archives and nine
calibration suites pass, including two integrated battle-position checks, two
standalone spawn-point checks, four missing-source checks and nine pooled
summon checks. Unsupported transition counts remain zero. The complete archive
inventory, sizes and SHA-256 values match the curated manifest.


## Triggered summons and sequential damage deaths

Schema 86 adds a finite `TriggeredSummonCatalog` to the shared context. Unit,
card and room definitions use IDs; summon effects keep ID references rather
than recursively embedding their descendants. Capturing a unit that summons
itself therefore does not recursively expand the fixture. Every context-copy
path preserves the immutable catalog.

Character `CardEffectSpawnMonster` applications now create detached copied or
fresh sources, mark cardless births, use actual physical insertion points and
retain the native first-born cache. An application that has no free points
preserves its previous cache. OnSpawn, OnUnscaledSpawn, AfterSpawnEnchant,
cardless Rally and extra-upgrade healing callbacks join the existing outer
character queue. The extra upgrade reaches both the new unit and its source
before that queue resumes. Live friendly actors and zero-HP actors use the
native current/last-known source point rules and centering steps.

A physical spell damage step keeps the current zero-HP victim unfinished while
processing previously finished deaths throughout the train. This prevents a
later victim from incorrectly receiving Rally from an earlier death summon.
The global death queue shares statistics, card routing, source caches and
position state across floors; the last removal in a team batch centers its
room. CheckForDeath removes the victim's death-statistic listener immediately,
while its OnDeath callback remains pending for the next queue run. Unity frame
completion clears removed actors' primary current and last-known pointers;
card effects in the same frame can still use those retained points.

Native UI previews also mutate shared summon weak references. The model
observes actual preview births and clears only the primary effect caches they
overwrote; it also reproduces the resulting cleared last-spawned reference.
It does not replace these observed side effects with idealized preview behavior.

Four binary archives record live/death hosts with copied/fresh sources:

| Archive | Plays / EndTurns / Pyre | Bytes / unique nodes | Native Instant seconds |
| --- | --- | --- | --- |
| `full-battle-triggered-summons.mt2f` | 15 / 5 / 80 | 24,791 / 3,941 | 43.59 |
| `full-battle-triggered-summons-fresh.mt2f` | 15 / 5 / 80 | 24,661 / 3,941 | 44.02 |
| `full-battle-triggered-summons-death.mt2f` | 21 / 7 / 49 | 32,000 / 5,308 | 44.63 |
| `full-battle-triggered-summons-death-fresh.mt2f` | 21 / 7 / 49 | 32,021 / 5,308 | 45.73 |

The setup adds native summon/gold triggers to real player unit definitions and
an extra +1 damage/+2 health/+1 armor upgrade. Death variants turn the real
rearrangement spell into a native Tower-targeted lethal damage effect. Original
Boss data, waves and profile signatures remain intact. All four captures finish
with zero failures, mismatches, unsupported transitions and pending observations.
Audio is muted. The installed game is 2.2.1, MVID
`8fb07b96-f4db-4d2b-884d-c00536d6ccf4`.

Together the captures contain 24 queued summon applications, twelve births,
twelve zero-birth cache retentions and four dying sources. Forty complete
native damage steps include two cross-room pending-death boundaries. The
checker independently recomputes complete train state and retained victim
flags in sixteen branches per damage step, rejects duplicate pending deaths,
and checks parent isolation. Existing complete room/train/card-action/turn,
physical-position and full-policy checks also recompute their native oracles.

Use `Run-FullBattleProbe.ps1 -Policy units-spells-and-junk -TriggeredSummons`,
adding `-TriggeredSummonsFresh`, `-TriggeredSummonsDeath`, or both to select
the other variants. These switches enable physical capture and their own
mandatory summon/death coverage gates. Curated data stays binary; sizes and
SHA-256 values are recorded in `tests/fixtures/manifest.tsv`.

This establishes the recorded trigger and death paths. Equipment transfer from
an actor to its replacement still rejects; revival, Horde merging, additional
summon pool/multi-birth trigger combinations, wider relic/room effects and
special Boss position paths still need native integration. Full battle
simulation and a complete solver remain unfinished.


The full 133-archive regression exits zero: all 124 battle archives and nine
calibration suites pass. Four summon suites independently compare all forty
native damage steps, along with six integrated position checks, two standalone
position checks, four missing-source checks and nine pooled summon checks.
Unsupported transition counts remain zero. All binary sizes and SHA-256 values
match the complete curated inventory and manifest.

## Triggered summon equipment transfer

Schema 87 captures equipment definitions in the finite summon catalog and
whether an attachment API runs inside the native character queue. A friendly
summon that copies its actor's source also inherits the actor's equipment in
original order, excluding ReturnToHand equipment. IgnoreCardUpgrades births
inherit none. A living actor forces equipment copies; a removed dying actor
can reuse its original cards. Each later birth tests current attachment state
again, including equipment already displaced by an earlier replacement.

Copies reset card history, preserve modifiers and join non-permanent deck
ownership through the native AddNonPermanentCardToDeck path. This differs from
ordinary generated cards and detached summon sources. After attachment,
MoveToStandByPile or ChangeStandbyCondition binds the new host. Replaced gear
keeps its condition until that host dies; transferred conditions no longer
return the same card on the original actor's death. Attachment healing,
equipment-added and equipment-removed callbacks remain behind the enclosing
summon effect rather than draining a nested queue.

The recording registers CopyCardState results at the actual allocation
boundary. Previously, observing owned equipment before detached unit sources
could reverse their assigned IDs. Independent checks compare complete clone
contexts and actual histories, ownership, relationships and modifier state.

Equipment copied by the last pending death also exposed the natural cast
boundary: native TimesPlayed runs before the remaining removal queue. Card
actions now carry that queue through the spell result and drain it after the
played statistic, before discard. Ability activation carries pending callbacks
into its own callback queue. The final physical death in each team performs
native centering. Direct effect callers still request a settled effect result.

Four binary fixtures retain the original Boss and waves. Setup changes real
player definitions, plays the host through native CardManager, and attaches
two ordinary equipment cards plus one ReturnToHand exclusion. The host has
three equipment slots; summoned children have one, exercising ordered
replacement. Ordinary gear includes temporary damage/health offsets,
permanent anonymous spikes and a once-only equipment trigger. Each summon
requests two births, bounded by physical slots. Death variants use real native
Tower damage. Audio is muted, Instant timing is used and original profile
signatures remain unchanged.

| Archive | Plays / EndTurns / final Pyre | Bytes / nodes | Native seconds |
| --- | --- | --- | --- |
| `full-battle-triggered-summons-equipment.mt2f` | 16 / 5 / 80 | 28,661 / 4,694 | 51.58 |
| `full-battle-triggered-summons-equipment-fresh.mt2f` | 16 / 5 / 80 | 26,359 / 4,244 | 50.78 |
| `full-battle-triggered-summons-equipment-death.mt2f` | 23 / 7 / 49 | 35,034 / 5,982 | 54.33 |
| `full-battle-triggered-summons-equipment-death-fresh.mt2f` | 23 / 7 / 49 | 33,340 / 5,650 | 50.41 |

All four native captures pass with zero failures, mismatches, unsupported
transitions and pending observations. The copied variants retain seventeen
complete clone boundaries and ten queued attachments. Across the four scenes,
independent checks observe five final transferred attachments, four of them
owned copies, one reused original and eleven ReturnToHand exclusions. All forty-six
attachment/removal operations compare complete native room/context state.
Sixteen native damage steps include four cross-room pending-death boundaries.
Initial and mid-battle policies run in sixteen branches; complete physical
room comparisons run in thirty-two. Missing equipment definitions reject in
sixteen branches without exposing a partial child or changing the parent.

Use `Run-FullBattleProbe.ps1 -Policy units-spells-and-junk
-TriggeredSummonsEquipment`, optionally adding `-TriggeredSummonsDeath` and/or
`-TriggeredSummonsFresh`. Dedicated native coverage requires an equipped source
that actually births children, queued attachment in copied variants and no
queued attachment in fresh variants. The curated inventory now contains 137
archives: 128 battles and nine calibration suites. Revival, Horde merging,
additional trigger summon pool combinations, wider relic/room effects,
grafted equipment and special Boss position paths still require integration.
Full battle simulation and the complete solver remain unfinished.

The full 137-archive regression exits zero: all 128 battle archives and nine
calibration suites pass. Eight queued-summon suites include four equipment
suites; six damage suites independently compare fifty-six native steps. Ten
integrated battle-position suites, four missing-source suites and nine pool
summon suites pass. Unsupported transition counts remain zero. The complete
binary inventory, sizes and SHA-256 values match the curated manifest.


## Undying revival and death listener lifetime

Schema 88 captures IsSacrifice separately from the retained SacrificeCard
reference, and distinguishes a permanent death statistics listener from a
one-time listener. These fields are now captured for every native scenario;
older archives retain their unknown nullable metadata. New birth templates
carry the same fields through player and enemy allocation. Undying is included
in the finite status definitions. A revival with unknown listener lifetime
rejects rather than guessing whether a later death can increment statistics.

Native revival restores HP to one, raises zero maximum HP to one, removes one
undying stack and clears HasFinishedDying/IsSacrifice. It preserves the unit's
identity, physical spawn point and its retained source reference. The living
unit participates in its own Harvest callbacks. Both team groups for the
specific death kind, then both groups for AnyUnit death, are queued before
OnReanimated and OnDeath. The latter carries paramInt one, allowing native
OnDeath threshold gates. An alive revival callback no longer marks its actor
physically removed or clears its surviving signal listeners.

Death statistics still receive the death signal, but the game's ordinary
CardStatistics listener is registered once. The first revival consumes it;
later revivals and final physical death do not increment it again. This is
separate from character Harvest, reanimated and death trigger dispatch. Model
revival preserves that distinction and retains the sacrifice source even after
its active sacrifice flag is cleared. The direct ApplyRevival API exposes its
pending callbacks; damage and sacrifice callers determine when their queue
runs. A queue already running defers both revival batches from a two-effect
self-damage trigger until that trigger returns.

The fixture keeps the original Boss and waves. Setup changes real Steward
definitions to two undying stacks and authors gold callbacks for both team
Harvest kinds, OnReanimated and OnDeath, with once/silence/threshold gates. Two
native card plays place the observers; two additional enemy observers copy a
real ordinary enemy definition. Direct native operations cover the first and
last stack, direct revival without a stack, final physical removal and a lethal
maximum-health debuff that revives at 1/1 HP. A surviving Steward receives two
extra stacks and 50/50 HP before the independent battle root, and its two-effect
OnTurnBegin self damage exercises revival inside the native running queue.

The recording includes the zero-HP actor before the revival API, although the
ordinary live-room projection excludes it. Standalone setup coroutines also
hold off automatic UI queue runners during that API boundary. The native
caller drains the queue immediately afterward. The guard records 24 deferred
attempts and makes no deferrals in natural combat; the two queued natural
revivals remain unmodified. Independent checks compare the accepted callback
FIFO, complete room/context/actor values, seven composite native operations
and all ninety callback phase states in sixteen parallel branches. Full policy
checks start both at the original and a middle decision, with sixteen branches;
physical state checks compare 112 contexts in thirty-two branches.

A last-target status follow-up also exposed a missing centering boundary:
CardSpellModel's internal death drain removed the defeated front enemy without
moving the surviving rear enemy forward. The last physical death of a team now
runs native centering at that internal queue exit, as the outer card action
already did. This fixes the same natural card action in the complete fixture.

full-battle-revival.mt2f contains 35,564 bytes and 5,941 unique nodes, SHA-256
9d3b2e1baf13e34384c925a64d021273028dde8c5af72596622fe343d395bf2a.
The muted Instant run takes 50.62 seconds, plays nineteen cards, ends seven
turns, wins at Pyre 53 and preserves the original profile/log signatures.
CaptureFailures, Mismatches, Unsupported and Pending are all zero. Eleven
revival boundaries include two inside a running queue; the recording contains
299 physical compactions. The archive and manifest have no JSON dependency.

Run Run-FullBattleProbe.ps1 -Policy units-spells-and-junk -Revival. The curated
inventory contains 138 archives: 129 battles and nine calibrations. These paths
verify ordinary player/enemy revival and the recorded integrations. Revival
with equipment/child summons, Horde merging, wider relic/room effects, special
Boss/Pyre revival and complete optimal search still require native integration.
Full battle simulation and the complete solver remain unfinished.


A fresh ordinary battle with the schema 88 snapshot also passes native and
independent checks: twenty-one plays/seven EndTurns, final Pyre 73, complete
policy and middle-root comparisons with sixteen parallel branches. It leaves
original files unchanged and has zero capture failures, differences,
unsupported or pending records. The listener/source fields are therefore
checked outside the authored revival setup as well.

The complete Check-Models.ps1 regression exits successfully across all 138
binary archives (129 battles and nine calibrations), with zero unsupported
transitions. It includes eleven integrated physical-position suites, eight
queued-summon suites, four triggered-equipment suites, fifty-six native damage
phases and the new full revival suite. Fixture sizes and SHA-256 values match
the curated manifest.

## Equipment-owned summons and decision reference lifetimes

Schema 89 records each triggered summon's actual source card, separate equipment
binding, persistent trigger state ID, effect index and accepted child callbacks.
CharacterState.ApplyEffects passes the host's spawner card even for effects
bound to equipment. The source card's room membership cache therefore refreshes
on the host card, including retained OnDeath actors using the last-known room.
New triggered-summon recordings include these room caches on every card;
older archives retain their optional unknown fields.

ApplyNativeTriggeredSummon resumes one effect after native trigger preflight,
once marking and source caching. It returns a complete child and pending
callbacks without draining them, or rejects missing definitions/cursors/source
state. Independent checks recompute each effect from its captured input, compare
the complete room, retained actor, first-birth cache and accepted callback FIFO,
then repeat in sixteen branches while checking parent isolation. Complete battle
policies still start from the original or a middle decision and generate all
subsequent states themselves.

This exposed an ordering error that whole-state comparisons did not detect:
cardless birth Rally uses MonsterManager.AddCharactersInRoomToList and therefore
physical room order. It does not sort the actors by identity. The simulator now
keeps that order and refreshes its working roster after a physical birth before
equipment-added callbacks snapshot the room.

Native UI battle previews also write source card caches. The preview model carries
those writes back to original card instances, filters out temporary preview-born
references and preserves the raw cached identities of original units until
their normal state is restored. A terminal Boss or Pyre removal stops the preview
coroutine, including subsequent unit turns, post-combat work and lower rooms.
The simulator follows that boundary rather than overwriting a later actor's cache
after the native preview has stopped.

CanonicalPhysicalReferences explicitly identifies new stable decision inputs.
At those boundaries, logically removed actors have no current/last-known physical
point, independent of the corpse dissolve's Unity destruction timing. Raw effect,
damage, death and compaction boundaries keep the original references. The recorder
exports both raw and canonical physical layouts plus live actor IDs; independent
checks verify that only removed object references change and that live positions,
physical groups, identity coverage and Boss/preview flags remain exact. The flag
defaults to false for old inputs and follows card, ability and turn transitions.
The existing canonical weak-reference rule also clears a removed first-birth
reference at a decision; raw effect boundaries retain it until actual destruction.

The authored scene adds once-only OnTurnBegin and OnDeath summons to one real
return-to-hand equipment definition. The host keeps its ordinary summon triggers,
and the original Boss and waves remain intact. Two other equipment cards transfer
to children, while the summoning equipment itself returns to hand. The ordinary
policy can attach it again, exposing repeated live and dying sources, changing
equipment memberships and later uses of the same card. Copied and fresh-source
variants exercise both upgrade-inheritance paths. Native runs remain muted,
use Instant timing and verify original profile/log signatures.

The four accepted archives have the following native and independent results.
Names below share the prefix full-battle-triggered-summons-equipment-owned.

| Archive suffix | Bytes / nodes | Plays / EndTurns | Final Pyre | Equipment birth applications: live / dying | Instant seconds |
| --- | --- | --- | --- | --- | --- |
| .mt2f | 31,013 / 5,105 | 16 / 5 | 80 | 1 / 0 | 134.37 |
| -fresh.mt2f | 27,905 / 4,507 | 16 / 5 | 80 | 1 / 0 | 83.83 |
| -death.mt2f | 53,404 / 9,150 | 25 / 5 | 79 | 4 / 9 | 78.06 |
| -death-fresh.mt2f | 47,865 / 8,209 | 25 / 5 | 79 | 4 / 9 | 80.17 |

All four native runs win with zero capture failures, mismatches, unsupported
transitions and pending records, and unchanged original profile/log signatures.
Their independent checks exit zero, including original and middle decision
roots, sixteen parallel policies and thirty-two physical-position branches.
Fifty individual summon effect boundaries include twenty-eight equipment-owned
applications that produce units, eighteen from retained dying hosts. Sixty
additional native damage phases compare pending deaths and zero-HP actors.
The four recordings preserve
1,330 source-cache writes and 2,165 raw/canonical decision mappings; the mapping
checks prove that 1,290 removed-unit reference records are cleared while live
actors and physical groups remain unchanged. Counts cover repeated boundary
snapshots, not distinct removed units.
The timings are native capture wall times with instrumentation, not independent
model or search performance measurements.

Run Run-FullBattleProbe.ps1 -Policy units-spells-and-junk
-TriggeredSummonsEquipmentOwned, adding -TriggeredSummonsFresh and/or
-TriggeredSummonsDeath for the other variants. The inventory now contains 142
binary archives: 133 battles and nine calibrations. Sizes and SHA-256 values
are recorded in tests/fixtures/manifest.tsv; regression has no source JSON
dependency.

The complete Check-Models.ps1 regression exits zero across all 142 archives,
including all 133 battle suites and nine calibrations. It checks fifteen
integrated physical-position suites, twelve queued-summon suites, eight
triggered-equipment suites, four direct summon-effect suites and four decision
reference mapping suites. All 116 captured summon damage phases match
independently. Unsupported transition counts remain zero, and the complete
binary inventory matches its sizes and SHA-256 manifest.

These integrations do not finish the whole battle simulator. Revival combined
with equipment/child summons, Horde merging/cloning, further relic and room
effects, special Boss/Pyre mechanics and broader card-effect coverage remain.

## Revival, child summons and retained card room membership

Schema 91 distinguishes CardInstanceState.RawPlayedRoomUnitIds from the current
PlayedRoomUnitIds query view. Native CardState keeps weak references in its
cached room list; CharacterInRoomAtTimeOfCardPlay masks dead/destroyed actors
without deleting those entries. A zero-HP actor can therefore disappear from
the query and reappear when ReviveFromUndyingStatus restores one HP. The room
model now recomputes this view from retained membership after life changes.
Modifier, equipment, discard, upgrade and resource transitions preserve both
lists; generated/cloned/ability cards begin with empty captured caches.
Revival rejects an input that captured a query view but omitted its raw cache,
because that view alone cannot recover masked references.

Battle previews carry raw membership back to restored original units and remove
temporary preview-born references. Stable decision captures also clear removed
members, using the same explicit logical reference lifetime as physical points.
DecisionRoomCaches records native raw memberships, captured memberships and
independent living-unit IDs so the projection can be checked. Effect and revival
API inputs keep the raw zero-HP references needed for restoration.

The new authored scenario extends real equipment-owned summons: the host starts
with two undying stacks, children with one, and both receive reanimated rewards.
The existing real Tower damage card and normal policy cause the deaths; no
standalone revival setup operations run. Native revival restores HP before its
OnReanimated and OnDeath callbacks, so the latter can copy a living host's
equipment while leaving the originals attached. Copied/fresh source variants
use Run-FullBattleProbe.ps1 -Policy units-spells-and-junk
-TriggeredSummonsRevival, optionally adding -TriggeredSummonsFresh.

Queued death recording now resolves an actor's last-known room after physical
removal. Falling back to floor zero hid child births on another floor and gave
the standalone phase model an incorrect input; an actor without either room
is now a capture failure. Complete train/card/turn checks remain independent
of these additional phase recordings.

Schema 92 additionally captures each revival's original attacker. The native
fresh-source recording includes a zero-HP attacker omitted from the alive-only
room snapshot while it still occupies its physical slot. ApplyRevival accepts
an explicit retained attacker snapshot without inserting it into the visible
unit list or changing its physical slot; missing, mismatched or duplicate
references reject. The same reference preserves sacrifice-card attribution.
Independent checks compare native room/actor states and accepted callback FIFO,
reject missing raw memberships and attacker snapshots, and verify immutable
parents across sixteen native and thirty-two isolated model branches.

Two new direct binary archives cover this integration:

| Fixture | Schema | Bytes / nodes | Plays / EndTurns | Final Pyre | Revivals / callback phases | Native capture seconds |
| --- | --- | --- | --- | --- | --- | --- |
| full-battle-triggered-summons-equipment-owned-revival.mt2f | 91 | 85,318 / 15,420 | 32 / 7 | 76 | 41 / 60 | 113.50 |
| full-battle-triggered-summons-equipment-owned-revival-fresh.mt2f | 92 | 73,284 / 12,902 | 31 / 7 | 75 | 43 / 61 | 110.98 |

Both muted Instant native runs win with zero capture failures, differences,
unsupported transitions or pending records, and preserve original profile/log
signatures. Independent complete policies also match from original/middle roots
in sixteen branches; physical states compare in thirty-two branches. There are
twelve equipped-host and sixty-four cardless-child revival boundaries, ten
equipment-owned OnDeath birth applications from revived live hosts, forty-seven
summon-effect boundaries and 146 additional independently checked damage phases.
The two archives retain 534 restored room-cache membership observations and
92,140 raw/canonical cache mappings; these counts include repeated snapshots.
SHA-256 values and sizes are in tests/fixtures/manifest.tsv. Timings are native
capture wall times with instrumentation, not model or search benchmarks.

The complete Check-Models.ps1 regression exits zero across all 144 binary
archives: 135 battles and nine calibrations. It includes seventeen integrated
physical-position suites, fourteen queued-summon suites, ten triggered-equipment
suites, six direct summon-effect suites, six decision reference mapping suites,
one standalone revival suite and both new revival/summon suites. All 262
captured summon damage phases match independently. There are no nonzero
unsupported transition counts, and the complete archive inventory matches its
sizes and SHA-256 manifest, without source JSON dependencies.

Horde merging/cloning, wider relic and room effects, special Boss/Pyre mechanics
and broader card-effect coverage remain unfinished; the complete battle
simulator is still in progress.

## Horde merge/clone APIs and temporary previews (schema 97)

`HordeMergeModel` now models the native Horde branch of `CloneMonsterState`
and `StatusEffectHordeState.MergeCharactersInRoom`. Cloning adds one troop to
the existing actor without allocating a unit/card, choosing a spawn point or
consuming RNG. Selection chooses the first other Horde actor in native
same-team manager order; authored character definitions need not match.
Selection and the direct API have distinct gates: the latter also accepts
self-merges and cross-team actors.

Merge growth uses the recipient's authored troop attack/HP. A real merge
despawns and destroys the incoming actor while retaining its positive HP,
equipment references, death-listener state and unfinished death flags. It
does not emit death/Harvest notifications or increment physical death counters.
Removal clears its physical point and centers both teams, including gaps on
the opposing team. Attached ordinary gear returns to Exhausted, return-to-hand
gear enters Hand, and the incoming unit's own card remains in Standby because
its death condition is unfinished. Passing `CardEffectBump` suppresses Horde
re-spawn/Rally callbacks while preserving status-change and troop-added records.
This API-level effect-type test does not implement Bump movement/card play.

Optional despawn/destruction fields distinguish removed positive-HP objects
from live actors. Queue engines retain those objects, their callback entries
and evolving trigger flags across phases. Ordinary callbacks on them abort
their effects at the native lifecycle gate. Self-merging first grows the same
object, then removes it; immunity blocks recipient growth but still removes
the source. Missing lifecycle/physical inputs return an unsupported result.

Temporary previews retain physical positions, equipment piles and destruction
state, while their source becomes logically despawned in the temporary actor
state. Preview restoration returns the complete primary actor/save/context
state and preserves native shared weak room caches, resolving visibility again
against the restored primary actors. Raw cache references are captured rather
than inferred. Trigger capture reads the active primary/preview once flag;
the scenario includes once-only re-spawn triggers and verifies their restored
primary flags.

`tests/fixtures/full-battle-horde-merge.mt2f` records 17 native API operations,
including four temporary previews, and five manager-order selections. It
covers both teams, different definitions, self/cross-team/null/non-Horde/immunity
boundaries, a removed actor still present in the Rally queue, both equipment
destinations and opposing-team physical centering. All before/API-return/drained
room/context/source states and accepted/dequeued callback payloads compare
independently in 32 branches; all four complete restored primary states match.
Two hosts are summoned through real paid plays. Ordinary equipment is played
from hand; the returning item uses native attachment/standby preparation when
absent from the starting hand. Five enemy observers use the native spawn API
with authored definitions derived from an ordinary wave template, without
changing the original Boss or subsequent wave definitions.

The muted Instant native capture wins at Pyre 80 after 17 recorded card plays
and five EndTurns, with zero failures, differences, unsupported transitions or
pending records and unchanged original profile/log signatures. Complete later
policies also match from initial/middle roots in 16 parallel branches. The
archive is 31,631 bytes with 5,159 unique graph nodes; SHA-256 is in manifest.tsv.
Instrumented native capture takes 48.05 seconds, not a model/search benchmark.
The curated inventory contains 145 binary archives: 136 battles and nine
calibrations, with complete size/hash inventory checks and no source JSON.

The complete `Check-Models.ps1` regression exits zero across all 145 archives.
All 136 battles and nine calibrations pass, including eighteen physical-position
suites, fourteen queued-summon suites, ten equipment suites, six direct summon
effect suites, seven decision reference suites and the new Horde API/preview
suite. All 262 native summon damage phases still match; no transition is
unsupported. Two raw cache memberships become visible again after the native
preview merge is rolled back, and both match the independent restoration.

At schema 97, real Bump movement/card integration remained open. The following
schema 98 increment adds primary card movement; the remaining work still includes
ordinary unit cloning, wider relic/room effects, special Boss/Pyre mechanics,
broader card coverage and complete optimal search.

## Primary Bump cards and cross-room Horde merges (schema 98)

`BumpModel` resolves the native target snapshot as one train-wide effect. Its
first pass determines legal destinations and relocates ordinary actors; Horde
actors stay on their original physical point until the later merge. It then
updates departure/destination positions and dispatches movement callbacks in
native order. Each Shift, Sentry and enemy post-movement/attempt phase drains
the queue before the next actor, so these boundaries are not a single final
batch. Previously queued callbacks follow their actor's current room after
movement, including the loop callback accepted before relocation.

Cross-room Horde growth/status callbacks run in the recipient's room, while
source removal, both-team centering and attached gear settlement run in the
source's room. The removed positive-HP actor keeps its unfinished death state
and source card in Standby. Spell results retain this lifecycle snapshot through
paid-card settlement; generic dead-spawner routing no longer exhausts a merged
unit's card. Actual Bump suppresses Horde re-spawn/Rally callbacks.

Captured `UnitBumpRules` distinguish looping, companion and outer-Boss actors.
Unit births, upgrades, ability changes, trigger mutations, combat, revival and
status changes preserve these optional immutable values. Legacy captures without
them remain usable for their existing operations, but Bump rejects missing
movement inputs. Primary ordinary movement supports up/down/zero and clamped
amounts, rooting removal, immobility priority, looping to the bottom, first-empty
points, partial movement before a blocked floor, player Pyre restrictions and
enemy entry into Pyre. Native Bump reads the fixed authored integer; configured
integer ranges affect its tooltip but are not sampled by the effect.

The same native run exposed missing floor-rearrangement callbacks. That effect
now visits its drop actor and every crossed actor in native order, fires Shift
only for actors that move, and runs Sentry even for a drop actor already at the
requested position. Each phase drains before the next actor/effect. Five later
native rearrangement plays verify the rewards of these callbacks.

`full-battle-bump.mt2f` contains eighteen authored Bump plays using native
`CanPlayHandCard`/`PlayCard`, two paid Steward hosts and native observer/filler
births. Observers carry gold callbacks to make dispatch ordering and flags
observable; the original Boss and wave definitions remain intact. Card
reconfiguration explicitly invalidates captured definitions. Raw effect return
states are separate from stable decisions, which include the ordinary combat
preview refresh. Authored plays have `ScenarioAction=true`; all are independently
recomputed, while the subsequent policy starts after that preparation.

All eighteen effect states, retained targets and complete dispatch payloads
match in 32 independent branches. All 38 card actions, five EndTurns and later
combat/spawn/card-cycle phases match independently. The ordinary 20-play policy
reaches the same win at Pyre 74 from initial/middle policy roots in 16 branches.
The muted Instant native capture takes 55.49 seconds; capture failures,
differences, unsupported/pending records are zero, and original profile/log
signatures are intact. The binary archive is 37,157 bytes and 7,189 graph nodes;
the curated inventory contains 146 archives: 137 battles and nine calibrations.

The complete 146-archive regression exits zero: all 137 battles and nine
calibrations pass, with no unsupported transitions. The audit verifies nineteen
physical-position suites, fourteen queued-summon suites, ten equipment suites,
six direct summon-effect suites, eight decision-reference suites, the standalone
revival and two revival/summon suites, Horde and Bump suites, and all 262 native
summon damage phases. Every curated archive matches its size/SHA-256 manifest;
there are no tracked source JSON fixtures.

This validates the recorded primary movement and gold callbacks. Actual Bump
preview movement/rollback, teleportation, destroyed-floor handling, companion/
outer/relentless Boss movement and room destruction, damage/revival interactions
during card movement, relic/room modifiers, ordinary unit cloning and broader
card coverage still need native fixtures and implementation. The full simulator
and optimal search are not complete.

## Ordinary unit clone APIs (schema 99)

`UnitCloneModel.Apply` implements ordinary `CloneMonsterState` from captured
source and birth definitions. It follows the native order: collect excluded
status contributions, copy the detached source card, strip permanent/temporary
excluded upgrades in reverse native removal order, create and mark the clone,
copy non-grafted equipment, copy live statistics/statuses, then copy the skill
and configured cooldown. `UnitCloneModel.Drain` resolves the remaining queue.

`CharacterCopyModel` preserves native status dictionary order and separates
source additions from recipient removals. It excludes hidden/grafted/silenced
source statuses and equipment-owned status kinds, subtracts excluded card
upgrade contributions, preserves recipient cardless/equipment statuses, copies
current/max HP without healing and applies only positive source damage buffs.
Copied equipment keeps detached ownership and its own upgrade/source identities.
Birth sets the clone flag before spawn callbacks. Ability replacement carries
existing status callbacks into its raw lifecycle transition; the final explicit
drain observes the newly installed triggers.

Null and invalid-room gates allocate nothing. A selected source on the last
physical point exits before copying its card. By contrast, a full destination
still allocates a detached card before birth fails, without allocating a unit.
Source-free ordinary clones allocate a unit without a source card. The existing
Horde gate grows the original actor and allocates neither card nor unit.
EndTurn accepts verified detached clone references that are present in the
complete registry and absent from owned instances; owned unit sources retain
their Standby requirement.

`full-battle-unit-clone.mt2f` records thirteen API operations and 44 intermediate
train/context boundaries. A real paid Steward supplies the source. The fixture
authors excluded permanent/temporary upgrades, a natural and replacement skill,
gold callbacks, equipment, wounds/buffs and native cardless/filler births, while
preserving the original Boss and waves. During raw API observation the scoped
probe pauses automatic queue runs, preserving intrinsic birth and gear-upgrade
queues. An explicit native drain records complete callback parameters and order.
This separates coroutine scheduling from the rules being tested.

All thirteen operations and every boundary, queue payload and final dispatch
match in 32 branches without root mutation. The independent subsequent policy
reproduces all fourteen plays and five EndTurns, wins at Pyre 80, and matches
from initial/middle policy roots in sixteen parallel branches. Native capture
uses muted Instant timing and takes 64.12 seconds; failures, differences,
unsupported/pending records are zero and original profile/log signatures match.
The native binary is 30,687 bytes with 4,972 unique nodes. The curated inventory
contains 147 archives: 138 battle scenes and nine calibrations.

The complete 147-archive regression exits zero: all 138 native battle fixtures
and nine calibrations pass, including 134 independent policy chains. The audit
confirms twenty physical-position suites, nine decision-reference suites,
fourteen queued-summon suites, ten equipment suites, six direct summon-effect
suites, standalone and two summon/revival suites, Horde/Bump/ordinary-clone
suites and all 262 native summon damage phases. Unsupported transitions remain
zero. All archive sizes and SHA-256 hashes match; the new curated clone archive
is byte-identical to its native capture and no source JSON fixtures are tracked.

This increment covers the observed ordinary APIs. Actual `CardEffectCopyUnits`
paid-card integration, enemy copying, grafted birth/room/relic modifiers, clone
previews and birth/movement callbacks involving destruction or revival still
need native fixtures and implementation. Wider cards, special Boss mechanics
and complete optimal search remain open; the full simulator is unfinished.

## Paid unit copying (schema 100)

`UnitCopyModel` integrates primary player-unit `CardEffectCopyUnits` with paid
card resolution. An immutable catalog contains birth, card, equipment and skill
definitions. Each copy resolves its rule from the current source actor and card
registry; no recorded future action or result supplies the simulated transition.
The effect retains each target's original physical point across its copies,
keeps the original room actor snapshot for the final counted Rally, and compacts
players after multiple successful births. Zero/negative counts and authored
integer ranges follow the native fixed-count behavior without extra RNG draws.

Birth drains carry the incoming global queue through spawn/status/skill phases
and preserve child callback order. Equipment copies retain detached card
ownership. Full-room failures can allocate copied cards without allocating a
unit; selected-last gates allocate neither. Horde grows the source without a
new actor or counted final Rally. Raw dispatch observations preserve all nine
callback fields, including inert callbacks, separately from stable card results.

`full-battle-unit-copy.mt2f` records fourteen actual paid casts: targeted and
room-wide copying, successive copies, clone-of-clone, status/ability effects
before copying, equipment, cardless sources, full/partial allocation failures
and Horde. Native setup uses two paid Shield Steward hosts and an ordinary
clone for the third floor, native fillers, authored upgrades/skills and gold
callbacks. The original Boss and waves remain intact. Scoped observation
retains incoming callbacks while allowing intrinsic birth/equipment queue runs.
Copied card identities are captured when native `CopyCardState` creates them,
including detached cards from failed births.

All fourteen raw train states, queued payloads and intrinsic dispatch sequences
match independently in 32 branches. All 28 paid actions, five EndTurns and
recorded combat/spawn/card-cycle phases match. The subsequent fourteen-play
policy wins at Pyre 80 from initial/middle roots and sixteen parallel branches.
Its initial turn has no ordinary card play; independent verification begins
before that first EndTurn. All four ascent inputs are empty after combat, so
physical checks verify that no cross-room move occurs, alongside fourteen
physical removals and every complete room position state in 32 branches.

Muted Instant native recording takes 89.85 seconds, with zero capture failures,
differences, unsupported/pending records and unchanged original profile/log
signatures. The 35,606-byte binary contains 6,511 unique nodes and is curated
byte-for-byte from the native archive. The inventory now contains 148 binary
archives: 139 battle fixtures and nine calibrations. The complete regression
exits zero, including 135 independent policy chains and all nine calibrations.
The final audit verifies 21 physical-position suites, ten decision-reference
suites, fourteen queued-summon suites, ten equipment suites, six direct summon
effect suites, standalone/two summon revival suites, Horde/Bump/clone/copy
suites and all 262 summon damage phases. No unsupported transitions occur;
every archive matches its size and SHA-256 manifest, and the curated copy
archive is byte-identical to the native capture. The pure Probe build has zero
warnings/errors.

Enemy and mixed-team copying are covered by the next increment below.
Grafted/preview copying, room/relic modifiers, destruction/revival
during birth, wider cards and special Boss mechanics need further native
coverage and implementation. The full simulator and optimal search remain open.

## Enemy and mixed-team copying (schema 101)

The declared effect team selects the birth path. An exact Heroes mask creates
an enemy through `HeroUnitBirthModel`; a mixed Heroes/Monsters mask uses ordinary
monster cloning even when its target is an enemy. The catalog captures separate
raw enemy definitions, including natural skills and spawn interactions. Rules
are resolved from the current source actor and immutable card registry.

Enemy birth compacts its physical group before finding the first free point.
A full group allocates neither card nor unit. Setup and incoming global callbacks,
including their children, drain before OnSpawn; the new enemy clears IsSpawning
before that phase. OnSpawn, Unscaled, NotFromCard and only the newborn's
AfterSpawnEnchant then run in native order. Optional copying applies detached
non-grafted equipment and live character statistics/statuses, while preserving
the natural skill and configured cooldown. It creates no source card, player
spawn statistic, clone/cardless flag or MonsterManager last-spawned update.

Native `SpawnHeroInRoom` receives its output actor by value, so the effect's
counted spawn total remains zero despite actual births. There is no final
counted effect Rally or compaction. Copying Horde statistics can separately
queue Rally over existing room actors; its count and payload remain distinct.

`full-battle-hero-copy.mt2f` records sixteen paid casts: zero/negative counts,
raw/live stats, repeated and clone-of-copy births, status/ability callbacks
already queued, both equipment-copy settings, room targeting, a last-point
Horde source, full/partial hero groups and both mixed-mask source teams. Two
paid player hosts and three native enemy hosts use authored Shield Steward
definitions, natural/replacement skills and gold callbacks. Native fillers
create exact capacity boundaries. Original Boss definitions and waves stay
intact. The recorded original-actor queues and all nine dispatch fields match
independently in 32 branches without changing parents.

The subsequent ordinary Champion play verifies player-wide AfterSpawnEnchant
before paid Rally. The battle then naturally loses on its first EndTurn at
Pyre zero. All seventeen paid actions, ten room stages, two train phases and
the EndTurn match. Independent policy runs from the initial decision and the
later state after the ordinary play agree in sixteen parallel branches. Physical
checks cover twenty room contexts, twenty-one cross-room moves and 275 raw/
canonical reference mappings in 32 branches. No ordinary unit dies; the model
preserves the Pyre's raw physical occupancy and skips ordinary unit death
statistics/removal. No new wave is spawned after defeat; phase/cache/RNG stay
unchanged. This fixture verifies a complete loss, not a second multi-turn win.

Muted Instant native capture takes 43.30 seconds. Capture failures, differences,
unsupported and pending records are zero, and original profile/log signatures
match. The accepted binary is 24,418 bytes with 4,755 unique nodes, SHA-256
`a6bd22ac6f407c026166b1645edfdc45807cd12750965abb7976bc930b980336`.
The curated archive is byte-identical to native output and has no source JSON.

The complete 149-archive regression and audit pass: 140 battles, nine
calibrations, 136 independent policy chains, 22 physical-position and eleven
decision-reference suites. All fourteen queued-summon, ten triggered-equipment,
six direct summon-effect suites and 262 summon damage phases remain passing.
Every archive matches its size and SHA-256 manifest; there are no errors or
unsupported transitions. Native capture and original-file signature gates pass.

This covers the observed primary enemy/mixed-copy paths. Boss registration,
grafted/substituted births, room/relic/covenant interactions, actual previews,
destruction/revival during birth and wider effects require additional native
inputs, implementation and fixtures. The whole simulator and optimal search
remain unfinished.

## Spawn enchant and paid birth lifetime (schema 102)

Ordinary paid player births now keep IsSpawning true through the room's
AfterSpawnEnchant queue and all its children. The flag clears after that queue
settles, before paid Rally. This matches MonsterManager.CreateMonsterState and
the existing repeated/clone birth path. Enemy creation keeps its separate native
timing. Legacy archives without this optional flag retain their earlier shape.

This timing affects behavior: trigger effects refresh their source card's
room-membership cache while excluding in-progress births. Clearing the flag
early included the newborn in its own subsequent paid Rally. The new native
archive rejects the old model at the first card: the newborn's CardMonsterPlayed
HasTriggered flag was true instead of the native false. The corrected model
reproduces that complete action and the remaining battle independently.

`full-battle-spawn-enchant.mt2f` records ordinary one-unit Shield Steward cards
and repeated two-unit Sword Steward cards. Authored player-wide enchant phases
add Horde and armor to their actors, with spawn/unscaled/not-from-card/status/
Rally gold callbacks. Boss definitions and waves stay intact. This covers both
newborn and older-unit enchant, initial Horde application and later troop growth,
incoming queue order, status children and Horde's separate spawn/Rally children.
Raw and canonical card caches are captured, including detached source cards.

Fifteen native enchant phases, seven complete births and 66 child callbacks
match in 32 independent branches. All nine callback fields, complete room/context
states and parent isolation compare. Two detached card clones, ten paid Rally
team phases and 27 actor dispatches are recorded and independently checked.
All fifteen paid actions, 38 room stages, nine train phases and five EndTurns
match; independent initial/mid-battle policy roots finish the same native win at
Pyre 80 in sixteen branches. Physical checks cover 76 room contexts, fifteen
removals, eight cross-room moves and 370 raw/canonical reference mappings.

Muted Instant native recording takes 47.39 seconds. Native capture failures,
differences, unsupported and pending records are zero, with unchanged original
profile/log signatures. The curated binary is byte-identical to native output:
30,572 bytes, 4,966 unique nodes, SHA-256
`27aadd9bf013e6cb2ea5d538578e5a19a411421c14f6a4fa1cef249da00b06dc`.
It has no source JSON. Complete regression and audit pass for 150 archives:
141 battles, nine calibrations, 137 independent policy chains, 23 physical-
position and twelve decision-reference suites. All fourteen queued-summon,
ten triggered-equipment, six direct summon-effect suites and 262 summon damage
phases pass. Sizes/SHA-256 hashes, native capture and original-file gates match;
errors and unsupported transitions are zero.

This verifies the observed status/Horde enchant paths;
grafted and room/relic/covenant birth modifiers, destruction/revival during
birth, wider enchant effects and special Boss mechanics still need native
coverage and implementation. The whole battle simulator remains unfinished.

## Persistent enchantment lifecycle primitive (calibration schema 1)

`EnchantmentLifecycleModel` models the persistent `CardEffectEnchant` state
machine up to its status API requests. This differs from schema 102's ordinary
AfterSpawnEnchant AddStatus callbacks. Each effect retains an ordered primary
map, an ordered preview map, the pending preview-sync flag and its last selected
status. Inputs include captured retained actors and the ordered output of the
separate target-collection phase. Constructors copy caller-owned collections;
all transition and intermediate request states are immutable.

Native updates select from the status pool every time, including duplicate
updates and removals. They append new non-self targets and keep old keys. They
compute add/remove decisions for the whole selected map before issuing requests
in map order. Source mute, silence, dormancy/spark, death/despawn/destruction and
both actors' current room gate validity. Destroyed recipients, dead recipients
without Undying and mismatched preview recipients are skipped and retain their
pending action and applied flag. The applied flag changes before a status API
call; non-skipped entries clear their pending action afterward.

Removal uses the currently selected status/count and the source's current
Duality, rather than the status/count used for the original application. Armor
and Positive/Negative categories can double; other captured categories retain
their count. Signed multiplication wraps. Add requests attribute sourceIsHero
to the recipient's team and have no source card, relic or triggering actor.
Configured actor-status, missing-health and magic scaling are ignored because
Enchant calls the selector with an empty target list and no actor. Ordinary
AddStatus chance/range and IgnoreDualism fields also do not apply.

The first native run corrected an earlier RNG assumption. The status selector
requests Battle even in preview, but RandomManager converts that request to
BattleTest while SaveManager.PreviewMode is true. The model now carries both
streams: primary random updates advance Battle; preview updates advance only
BattleTest. Singleton pools consume neither. Any solver preview protocol must
also restore these streams outside this primitive.

Preparing a preview sets a sync flag. The next valid preview update overwrites
or appends primary entries in the preview map without clearing preview-only
keys or changing existing key positions. Primary state is unaffected. Setup
clears both maps but retains the sync flag and cached status. Missing bound
managers, source or effect state make an update a complete no-op.

`Run-FullBattleProbe.ps1 -Policy units-spells-and-junk -EnchantmentLifecycle`
creates four isolated native CharacterState objects. Their room references use
real native spawn points without putting these actors into the train. The
calibration controls raw HP/despawn/destroyed/preview/status gates, retained maps
and the manager's temporary LastSpawned reference, then invokes the unmodified
native enchant update. Harmony observes native target collection and captures
status requests, including the complete map state at each API entry. It
suppresses those status calls and visual movement only during the calibration.
This is an explicit status API boundary oracle; it does not verify actual status
mutation, callbacks, removal signals or automatic train update scheduling.

The accepted native archive contains 1,460 exact transitions and 644 requests:
a 1,152-case source/recipient/preview/applied/pending-action gate matrix,
192 signed-count/status/Duality cases, first/repeated/empty/self target
collections, ordered multi-target calls, source/recipient state changes,
preview resynchronization with stale keys, Setup and three missing-binding
cases. A 64-update random pool selects armor, poison and cardless. Independent
checks compare every native map, cached status, both RNG streams, every request
field and intermediate state. They also carry model outputs through 34 initial
and 72 random-chain steps instead of restarting from native snapshots. All
checks and chains repeat in 32 branches with unchanged parents. Combat still
explicitly rejects CardEffectEnchant until its lifecycle is integrated.

After calibration, the full native and independent seven-turn battle still
wins. Native recording uses muted Instant timing and takes 47.92 seconds.
The train/context, primary save/preview reference, LastSpawned reference,
Battle/BattleTest and Unity global RNG are restored. Native capture failures,
differences, unsupported and pending records are zero, and original profile/log
file signatures remain unchanged. The curated binary is byte-identical to the
native archive: 19,551 bytes, 4,856 nodes, SHA-256
`2b7bdc7a17da201945b3fd35e3d2cd21ad0edd720825b9d34b6d8d523c76c618`.
It has no source JSON.

The next step is to store this state in each live CombatEffect, independently
collect targets through the existing target model, apply each status request
with its child callbacks, and reproduce automatic birth/status/movement/death
updates, removed-source signals and preview rollback. Reentry and broader
room/relic modifiers require real status-mutating native fixtures. Special Boss
and the remaining battle mechanics also remain open; the full simulator is
not complete.

The complete regression and audit pass for 151 archives: 141 battle fixtures
and ten calibrations, including 137 independent policy chains, 23 physical-
position suites, twelve decision-reference suites, fourteen queued-summon
suites, ten triggered-equipment suites, six direct summon-effect suites and
262 summon damage phases. All required native suites and ten final calibration
outputs are present. Every archive matches its manifest size/SHA-256; native
binary provenance and original-file signature gates pass. Errors and unsupported
transitions are zero. This broad regression does not extend the primitive's
status API boundary into automatic persistent-enchantment combat support.

### Persistent aura updates with real status mutations

`EnchantmentCombatModel` adds a full-train update kernel. Persistent target maps,
preview synchronization flags, cached status definitions and binding metadata now
belong to each `CombatEffect.Enchantment`; effect copies and live/template capture
preserve them. The staged lifecycle plans the whole map before applying individual
status calls. Each call marks its entry before entering the status engine, then
refreshes the effect and recipient before continuing. The kernel collects its own
targets, uses Battle/BattleTest in native order, and performs actual modeled status
addition/removal and ordered callback generation.

The room manager's allow/reentry guards apply to the entire train. Source ordering
now reproduces each native manager's repeated whole-list sort, as detailed below.
Direct effect calls can enter a nested global pass;
updates already inside a global pass cannot. Parentless aura targets can include
the Pyre. Actor identities retained outside the live train have an explicit store;
source removal and the automatic combat hooks still require separate integration.

The accepted native calibration uses two real paid Steward units, installs runtime
aura and callback triggers temporarily, and calls the actual native
RoomManager.UpdateEnchantments / CardEffectEnchant.OnUpdateEnchantments.
AddStatusEffect, RemoveStatusEffect and the trigger queue execute without gameplay
suppression. Thirty-two observed steps cover duplicate updates, source muting and
silencing, disabled/re-enabled global updates, direct/global reentry, zero/negative
and overflowing Duality counts, poison removal and sixteen pooled random steps.
All 35 status calls, 48 nine-field callback payloads and resulting status/effect/
train/context states match independently. Gold callbacks also run through the
existing character queue and match the native output. Thirty-two parallel copies
preserve their parent frames.

Frame-driven UI preview preparation can occur while the coroutine drains. These
are explicit native PrepareEnchantmentsForPreview events, recorded in order and
applied through EnchantmentCombatModel.PrepareForPreview. They are neither
suppressed nor inferred from expected output. This calibration tests their
synchronization flags; it does not claim a complete real-status preview rollback.

The calibration restores native primary state objects, original trigger lists and
trigger identity maps, enchanter flags, gold, RNG streams and global update flags.
The captured live train/context and empty queue match their original values.
Run-stat/UI side effects in the isolated profile are outside this restoration
claim. The subsequent ordinary battle and independent initial/middle policy roots
win after 21 paid actions and seven EndTurns, Pyre 73; original profile/log file
signatures are unchanged. Muted Instant capture takes 51.21 seconds.

The curated binary is a direct native schema-1 archive: 9,760 bytes/1,776 nodes,
SHA-256 a68ec95cafa6b9fd202b8201a0c5d892f97fd2cce8fa28c673497b87e58600da.
It has no source JSON. The first native experiment was rejected for an unmodeled
preview preparation during queue draining and was not curated.

This kernel is not yet wired into generic combat birth, movement, source death,
removed-source signals, revival or trigger mutation. Generic RoomCombatModel still
rejects CardEffectEnchant. Child callbacks that mutate actors/cards are explicitly
rejected by this kernel until those automatic hooks are integrated; only observed
gold callback effects currently drain. Horde aura births/casualties, room/relic modifiers, Purify and Deathwish
also remain unsupported here. The full battle simulator remains incomplete.

The complete 152-archive regression and audit pass: 141 battles and eleven
calibrations, 137 policy chains, 23 physical-position and twelve decision-reference
suites, fourteen queued-summon, ten triggered-equipment, six direct summon-effect
suites and 262 summon damage phases. Sizes, SHA-256 hashes, byte-identical native
provenance and original-file gates pass. The final small refusal/binding-capture
fixes build successfully and the real-status calibration, explicit unsupported
boundaries and previous 1,460-step lifecycle matrix pass again independently.
Probe has zero warnings/errors; ModelChecks retains its fourteen existing nullable
warnings. Generic persistent-aura combat integration and the full simulator remain
incomplete.

The automatic-control increment's complete 153-archive regression and audit pass:
141 battles, twelve calibrations, 137 policy chains, 23 physical-position and
twelve decision-reference suites, fourteen queued-summon, ten triggered-equipment,
six direct summon-effect suites and 262 damage phases. Binary integrity/provenance
and original-file gates pass. The final shared-train refusal is checked even on a
room without an aura source; the new control-status and previous real-status
calibrations pass on that final source. Probe/Model build without warnings/errors;
ModelChecks retains fourteen existing nullable warnings.

### Automatic aura updates from ordinary status APIs

`CombatContext.Enchantments` now owns an immutable train snapshot, retained actors,
enchanter identities, room-manager guards and the BattleTest stream. World rooms
have no context back-reference, so capturing/copying this state cannot recurse
through Context -> World -> Room -> Context. Card play, card cycles, generation,
equipment, ability caching, gold and all context-copy methods preserve the world.
Room completion synchronizes changed actors and retained corpses; the train queue
refreshes each room from the latest shared world before applying its next callback.
Identity checks cover a moved actor without duplicate live/retained entries and
updated corpse health. These checks do not verify native movement or death hooks.

Ordinary AddStatusEffect/RemoveStatusEffect model paths now request the native
global aura update for silenced, muted, and spark on a dormant actor. Addition
includes zero/repeated calls; removal only requests an update after an actual
stack decrease. Other status writes synchronize the actor without requesting a
global update. Existing global/direct reentry and source order remain in the
kernel. Its status calls use a context without a world to avoid applying these
automatic hooks twice. Status callbacks precede aura-generated callbacks.

`Run-FullBattleProbe.ps1 -Policy units-spells-and-junk -EnchantmentWorld` extends
the real-status calibration with 32 actual native control-status API transitions.
It covers muting, silencing, zero/repeated additions, zero/all removals, armor and
dormant writes without global updates, zero/positive Spark with dormancy, disabled
updates and sixteen consecutive random-pool control steps. No gameplay status API
or queue effect is suppressed. Complete train/context/effect maps, both RNG streams,
76 nine-field callback payloads and drained gold results match independently.
Twenty-four automatic aura status calls are observed; their request records are
retained as diagnostics, rather than claimed as an independent request-field check.
The sixteen random steps also carry model outputs forward; all cases repeat in
32 branches with unchanged parents. UI preview preparation events remain explicit.

The accepted native run takes 60.38 seconds with muted Instant timing. Native
differences, capture failures, unsupported and pending records are zero. Native
primary state objects, triggers/identities, enchanter flags, gold and RNG are
restored before the subsequent ordinary seven-turn/21-action battle wins; original
profile/log file signatures remain unchanged. Isolated run-stat/UI side effects
remain outside the restoration claim. The directly captured binary has 8,428 bytes,
1,470 unique nodes, SHA-256
`3a2b4af9f7f1c94d3d0b36cee560c0fc5e056a271a054d08ee641daf58132d51`;
it has no text source or JSON companion.

This increment verifies automatic control-status updates and shared callback state.
Birth binding, per-movement scheduling, source death/removal, revival and full
preview rollback are still pending. Actor/card-changing aura children, Horde aura
casualties, Deathwish, Purify and room/relic interactions remain refused. Generic
RoomCombatModel still rejects CardEffectEnchant; the restored ordinary battle is
not evidence of a persistent-aura multi-turn battle. The full simulator remains
incomplete.

### Native enchantment source collection order

Native RoomState.AddCharactersToList sorts the entire accumulated list after each
floor call, including empty floors. Team.Heroes=1 and Team.Monsters=2 put enemies
before players; same-team comparisons use actual physical IndexInRoom across
floors. It skips actors already present while adding a room, but preserves
duplicates in an incoming list. Thus walking MonsterManager before HeroManager
does not imply that player sources run first, and a floor traversal alone does
not reproduce the final order. EnchantmentCombatModel now uses the independent
collector. Multiple source floors require captured physical points; the model
explicitly refuses missing point data rather than guessing a cross-floor order.

The new native collector calibration observes 172 ordinary manager lists across
the real status matrix and subsequent paid battle, plus 32 read-only stress lists.
Initial lengths 0, 1, 15, 16, 17, 31, 33 and 64 exercise duplicate incoming actors,
equal physical indices, both teams, empty-floor calls and sorting size thresholds.
All 204 native arrays match independently in .NET and repeat in 32 branches without
parent changes. The first independent test exposed missing collection deduplication;
the corrected collector passes the same captured native inputs. Gameplay/status
APIs are never suppressed by this observer.

The accepted muted Instant native run takes 74.43 seconds, with passed/won true,
zero capture failures/differences/unsupported/pending records and unchanged original
profile/log signatures. The restored ordinary battle also passes independent
initial/middle-root policy chains, 21 actions/seven EndTurns, Pyre 73 and 16 branches.
Both existing real-status calibrations and the 1,460-step lifecycle suite pass on
the corrected source, including the missing-physical-point refusal.

The new native binary has 2,681 bytes/581 unique nodes, SHA-256
`b9011555d2a5cd35f410b917166240a64c83d56de74de4766a5e77581bfe5804`.
It has no text source/JSON companion and is byte-identical to native output.
The curated inventory now contains 154 archives (141 battles/thirteen calibrations).
All hashes pass; validation consists of the preceding complete 153-archive
regression/audit plus this new collector suite and final affected aura suites,
rather than a second complete 154-archive run. Automatic birth/movement/death,
changing children, full preview rollback and the rest of the full simulator remain
open; generic persistent-aura combat is still refused.

### Shared-world direct effect routing

EnchantmentWorldModel.UpdateEffect invokes only the selected actor/trigger/effect,
including direct calls while the manager update gate is disabled. It preserves
the effect binding gate and refuses missing actors or invalid effect indices.
A character trigger must use this route instead of updating every aura owned by
that character, which would repeat status calls and random draws.

All 32 existing real-status native samples now independently check both the
standalone kernel and the context-owned shared-world route. Exact train/status
state, all 48 callback payloads, drained child gold, ordered preview preparations,
effect maps, both RNG streams and unchanged parents match. The comparisons repeat
in 32 branches; no new native archive is needed. This validates effect routing,
not automatic birth/movement/death scheduling or a persistent-aura paid battle.

### Persistent aura in a complete paid battle (schema 103)

AutomaticLifecycle worlds now schedule native aura updates during birth binding,
direct trigger effects, ascension, death/removal and both UI and Boss-hit previews.
Birth preserves starting-status ordering before authored immunities; enemy OnSpawn
binds after spawn setup. Ascension disables manager updates while reserving moves,
then enables updates before Pyre combat and after the movement batch. Death runs
direct source updates before Slay and at removal notification, followed by one
global room-order update after physical removals unless terminal combat interrupts
that boundary. Cross-room status children share the same FIFO and latest world.

Generic UI preview and temporary Boss-hit preview have distinct preparation rules.
Both restore primary actor state while carrying native effect-map/cache changes;
temporary Boss previews prepare again on disable and drain their trigger queue.
UI preview does not prepare on disable. A primary aura status change after UI
preview requests another preview. Destroyed targets remain while effect maps
reference them; frame completion clears their physical points and attacker weak
references, while stopped terminal snapshots retain objects not yet Unity-flushed.

`Run-FullBattleProbe.ps1 -Policy units-spells-and-junk -PersistentEnchantments`
uses paid Steward cards with an OnSpawn singleton armor aura targeting both teams
and status-change gold children. Sources remain bound throughout the actual battle;
there is no administrative removal or status-API suppression. The launcher requires
captured physical points, isolated preview RNG, actual preparation and nonempty
bound aura maps, so an accidentally unprepared ordinary battle cannot pass.
The original Boss/waves remain intact, and the isolated game is muted at Instant.

The accepted game 2.2.1 capture takes 53.17 seconds, wins after 15 paid actions and
five EndTurns with Pyre 79, and has zero differences/capture failures/unsupported/
pending records. Original profile/log signatures remain unchanged. Independent
.NET checks reproduce 42 room stages, nine train phases, seven spawns, nine card
cycles, all actions and EndTurns including complete shared world state. Initial
and middle-root policy chains finish without injecting recorded intermediate
states; 16 branches agree and preserve parents. Physical-position checks cover
13 removals, eight cross-room moves and one empty ascent. Coverage requires both
teams, multiple bound sources, preview removals/preparations, retained destroyed
targets and drained status children.

`tests/fixtures/full-battle-persistent-enchantment.mt2f` is the byte-identical
native binary: 26,528 bytes, 4,444 unique nodes, no text source or JSON companion.
Its SHA-256 is recorded in manifest.tsv. Inventory is now 155 archives: 142 battles
and thirteen calibrations.

This proves one persistent singleton-armor policy, including ordinary target
death and movement. Random-pool whole-battle scheduling, actual source death and
revival, actor/card-changing aura children, preview-born identities, trigger
replacement, Horde aura casualties, Deathwish/Purify, room/relic interactions and
broader special Boss/card mechanics still require implementation and native
coverage. The complete battle simulator and optimal solver remain unfinished.

The complete 155-archive regression and audit exit zero: 142 battles/thirteen
calibrations, 138 independent policy chains, 24 physical-position and thirteen
decision-reference suites, fourteen queued-summon, ten triggered-equipment,
six direct summon-effect suites and 262 damage phases. All binary sizes/hashes,
native provenance and original-file gates pass; no errors or unsupported
transitions occur. Probe builds without warnings/errors; ModelChecks retains
fourteen existing nullable warnings. Both PowerShell scripts parse and staged
whitespace checks pass. There are no tracked source JSON fixtures or results.

### Actual aura source death and destroyed-reference release

`-PersistentEnchantmentDeaths` adds native OnTurnBegin self damage and OnDeath
gold callbacks to paid aura sources. Two sources survive the first combat, then
die through the ordinary turn/queue path. Sources are not removed by scenario
administration. Native binding remains available for the removal signal and
becomes false only after Unity destruction at the stable frame boundary. The
model now releases that binding during CompleteFrameRemovals; stopped terminal
frames retain their existing destruction boundary. Subsequent general UI preview
prepares only live room actors, not retained destroyed enchanters.

Source removal withdraws statuses from living targets. Dead/destroyed target
entries can retain IsEnchanted=true with NextAction=Remove because native skips
their status API call; their exact primary/preview caches must remain intact.
This fixture requires formerly bound sources, destroyed-reference release,
withdrawn live targets, skipped corpse entries and no later preview preparation.

The accepted muted Instant run takes 63.43 seconds, wins after 21 paid actions
and seven EndTurns with Pyre 65, and has zero differences/capture failures/
unsupported/pending records. Original profile/log signatures remain unchanged.
Independent checks reproduce 61 room stages, fourteen train phases, eleven spawns,
thirteen card cycles, all actions/turns and initial/middle-root policy chains in
16 branches with unchanged parents. Physical checks cover 23 removals, ten
cross-room moves and one empty ascent. The first native recording rejected the
previous model's preparation of dead source caches; it is not curated.

`full-battle-persistent-enchantment-deaths.mt2f` is byte-identical to the accepted
native capture: 32,431 bytes / 5,518 nodes, no text source or JSON companion.
Inventory is 156 archives: 143 battles/thirteen calibrations. All inventory
sizes/hashes pass. Regression evidence is the preceding complete 155-archive
run/audit plus this new paid battle, the previous persistent-aura, spawn-enchant
and revival battles, all thirteen calibrations and the complete pure check suite.
A second complete 156-archive run was not performed. Probe builds with zero
warnings/errors; ModelChecks retains fourteen existing nullable warnings.

Actual persistent-aura source death is now verified for this singleton armor
policy. Aura-source revival, whole-battle random pools, actor/card-changing aura
children and the remaining combat mechanics are still open; the full simulator
and optimal solver remain incomplete.

### Natural revival of persistent aura sources

`-PersistentEnchantmentRevivals` gives paid aura sources two Undying stacks and
two real OnTurnBegin self-damage effects. Reanimated, harvest and death gold
children run in the native FIFO. Each of two sources consumes its first and last
stack inside the ordinary trigger queue, then dies normally later. No setup
revival API or queue deferral is used. Revival retains the source binding and
exact primary/preview effect maps and cached status; source death still follows
the previously verified destruction boundary.

RoomCombatModel.Revive now preserves explicit IsDespawned/IsDestroyed values.
Dropping these values changed false into unknown and later prevented destroyed
source binding release. The first native capture rejected four affected room/
train/turn transitions and is not curated. Unknown values in historical inputs
retain their existing representation.

The accepted muted Instant capture takes 64.85 seconds and wins after 21 paid
actions/seven EndTurns with Pyre 68. Differences, failures, unsupported and pending
records are zero, and original profile/log signatures remain unchanged. Four
natural revival boundaries include 24 accepted callback payloads and 24 actual
callback phases. Native API room/actor/world/RNG states, first/last stacks,
reanimated/death order, binding/maps and parent states match in 16 branches.
Independent whole-battle checks reproduce 59 room stages, fourteen train phases,
eleven spawns, thirteen card cycles, all actions/turns, initial/middle roots and
16 policy branches. Physical checks cover 23 removals/eight moves and 530 canonical
decision mappings.

`full-battle-persistent-enchantment-revivals.mt2f` is the byte-identical native
binary: 34,456 bytes / 5,860 nodes, no text source or JSON companion. Inventory is
157 archives: 144 battles/thirteen calibrations, with all sizes/hashes verified.
The new battle and seven affected historical battles pass: persistent aura,
aura source death, standalone revival, Horde merge, Bump and both owned equipment
revival variants. All thirteen calibrations and pure checks pass. This evidence
extends the preceding complete 155-archive run and the source-death increment;
a second complete 157-archive run was not performed. Probe builds without
warnings/errors; ModelChecks retains fourteen existing nullable warnings.

Persistent singleton-armor sources now have verified birth, target movement,
source/target death, natural first/last-stack revival and preview behavior.
Whole-battle random pools, actor/card-changing aura children, Horde aura effects,
broader control/room/relic interactions and remaining special Boss/card mechanics
are still pending. The full battle simulator and optimal solver remain incomplete.

### Whole-battle random aura pools and complete preview isolation

`-PersistentEnchantmentRandomPools` gives the paid aura sources the native pool
armor 2 / regen 1 / buff 1, targeting both teams. Combining the flag with
`-PersistentEnchantmentDeaths` exercises natural source death with the same pool.
The original Boss, waves, paid policy and native status/trigger APIs remain in use.

The native pool is selected on every aura update, including removals and updates
which do not change any target. The cached selection is shared across primary and
preview maps. Whole-battle integration now includes the distinct order update at
SpawnMonster completion, CardManager.OnCardPlayed completion, FloorRearrange
completion, turn-counter rollover and the first initial enemy spawn. Enemy spawning
also imports aura cache changes on other floors; treasure creation previously
advanced RNG correctly but overwrote those other-floor caches with stale units.

The opt-in isolated preview protocol now spans the primary room-order update and
its drained callbacks after combat preview restoration. Previously its scope ended
at SetCharacterPreviewState.On, allowing this final update to consume real Battle
RNG on each extra UI refresh. Native captures proved individually matching actions
and turns could then diverge in a continuous policy. The extended scope preserves
Battle and BattleTest across the whole refresh, while observing both streams before
restoration and retaining the actual aura cache/status/statistic writes. The native
policy waits for that scope to finish before capturing a stable decision. The model
restores RNG before any requested subsequent preview, preserving the same protocol.
This protocol is explicitly enabled; arbitrary unisolated random UI refresh timing
is not modeled as a deterministic decision.

| Oracle | Paid actions / EndTurns | Native seconds | Final Pyre | Preview scopes | Bytes / nodes |
| --- | --- | --- | --- | --- | --- |
| full-battle-persistent-enchantment-random.mt2f | 15 / 5 | 51.55 | 80 | 64 | 29,412 / 4,818 |
| full-battle-persistent-enchantment-random-deaths.mt2f | 21 / 7 | 64.72 | 65 | 68 | 33,542 / 5,642 |

Both muted Instant runs win naturally with zero capture failures, differences,
unsupported transitions and pending records; original profile/log signatures are
unchanged. The ordinary pool oracle reproduces 41 room stages, nine train phases,
seven spawns and nine card cycles; the source-death oracle reproduces 61 room stages,
fourteen train phases, eleven spawns and thirteen card cycles. Independent policies
start from initial and middle roots, reproduce every paid action/EndTurn through
terminal combat, and repeat in sixteen parallel branches without changing parents.
All three cached selections, real Battle advancement, preview draws, restoration
through primary order completion, both teams and retained target/source maps are
required coverage, not optional observations.

The archives are byte-identical native binaries without text sources or JSON
companions. Their SHA-256 values are
8696555b9b71d7a975bb71762d5f317e097d186fb3f659291690d02e6135194e and
8d8b7f4326df89920e8dcbe5b3170d6f46ff2e63fd3dcd576719a7d86c0a8a37.
Inventory is 159 archives: 146 battles and thirteen calibrations.

Random source revival remains an exploratory probe mode. Its native capture reveals
four decision differences involving a retained enemy's live last-attacker reference.
CharacterState.IsDestroyed includes BeginDestroy and InRemoveList, while the death
dissolve callback asynchronously adds the actor to the actual removal list. Its
Unity object can survive until a later ProcessRemovals, so unconditional reference
clearing at a decision boundary is incomplete. An observed destruction-stage
prototype exposed additional room/spawn timing differences and was not retained
in the verified model. Rejected captures are not curated and comparisons remain
complete. Native removal scheduling, actor/card-changing aura children, Horde aura
effects, broader control/room/relic interactions and special Boss/card mechanics
remain work; the full battle simulator and optimal solver are incomplete.

Validation: the complete 158-archive regression (145 battles and thirteen
calibrations, including the ordinary random-pool oracle) exits zero, with 141
continuous policy checks and no errors or unsupported transitions. A final
rebuild of the verified model also passes nine affected battles, including the
new random source-death oracle, and all thirteen calibrations. Both new policies
pass initial/middle roots and sixteen parallel branches. All 159 current archive
sizes/hashes match the manifest and regression inventory; a single combined
159-archive regression was not repeated. The final probe build has zero warnings
and errors, and PowerShell parsing and whitespace checks pass. The rejected
destruction-stage prototype is excluded from these changes and results.

### Native character removal API model

CharacterRemovalModel now separates BeginDestroy, InRemoveList, scheduled
Destroyed and actual OnDestroy. ProcessQueue schedules only actors actually in
the manager's queue; an actor still waiting for its minimum-ten-frame dissolve
callback remains pending. Scheduling preserves the weak reference and all state
information. Actual destruction clears primary/preview/temporary position,
attacker, feeder, linked-unit, equipment, applied-upgrade and signal references,
upgrade HP maps and manager references while preserving numeric/status fields.

The opt-in -CharacterRemoval probe observes original SetDestroyedState,
HeroManager/MonsterManager.ProcessRemovals and CharacterState.OnDestroy without
suppressing gameplay or changing queues/timing. Its binary API calibration has
55 exact transitions, ten real destructions and twelve manager calls. It proves
pending dissolves survive a nonempty queue, delayed callbacks take at least ten
frames and primary/preview/temporary information exists. Independent comparisons
and parent checks pass in 32 parallel branches. The five historical persistent
aura battles and their continuous policies also pass with the rebuilt model.

character-removal-calibration.mt2f is a byte-identical native 4,282-byte/816-node
archive without a text source, SHA-256
5d13e0243419eef35e259181b7d5da424bb6d3187f31b8c57bcd0cf31b38f67c.
Current inventory is 160 archives: 146 battles and fourteen calibrations. This
calibration comes from the random-revival diagnostic battle, which still has
four complete-decision differences; only the independently passing API
calibration is curated, and that battle is excluded. API event inputs are not
future policy inputs. Automatic whole-battle removal scheduling remains open.

Validation: all fourteen calibrations and pure model checks pass with the final
rebuild, as do the five persistent-aura battles and their complete initial/middle
policies and sixteen branches. Probe builds without warnings/errors. All 160
inventory sizes/hashes, PowerShell syntax and whitespace checks pass; the complete
160-archive regression was not repeated for this separate API model.

### Stable native removal flush and random aura revival

The explicitly enabled -SettleDeathDissolves solver protocol wraps the original
RunEndMonsterTurn coroutine. After its gameplay work returns, it waits for the
original DoDeathDissolve callbacks to populate the manager removal queues, then
allows RunCombat to execute its original ProcessRemovals. Duration, the ten-frame
minimum, damage, revival APIs, callbacks, queues and RNG are not overwritten.
Actual Unity destruction still occurs at its native frame boundary. This makes
the existing canonical decision contract reproducible for the observed late
death; it does not model arbitrary unsynchronized rendering frame timing.

full-battle-persistent-enchantment-random-revivals.mt2f now proves the complete
paid random armor 2 / regen 1 / buff 1 aura policy with natural first/last Undying
revivals. It wins with 21 paid card plays/seven EndTurns and final Pyre 72. All
four revival boundaries and 24 callback phases match, with bound sources surviving
revival and naturally releasing on death. Sixty room stages, fourteen train
phases, eleven spawns and thirteen card cycles match without unsupported paths.
Independent policies reproduce the initial and middle roots and sixteen parallel
branches through the terminal win, preserving their parents. Native initial and
repeat captures have zero differences/failures/unsupported/pending capture records
and preserve original profile/log signatures, taking 65.46 and 63.67 muted Instant
seconds respectively. The repeat disables additional character-removal observation.

Both captures observe six original nonterminal removal flushes, twelve original
death callbacks and four additional awaited frames for retained enemy 6. That
actor's existing callback completes its ten-frame delay and joins the queue before
the original flush; its attacker reference clears only on real destruction. The
recorded completed callbacks respect their unchanged minimum frame counts, and
every pending actor at a barrier completes before the barrier returns. The final
Boss's visual callback may remain pending after the native terminal StopCombatLoop;
terminal combat does not gain an artificial removal flush. The two captures have
67/66 isolated combat previews, respectively.

The curated schema104 archive is byte-identical to the first native binary,
35,337 bytes / 5,846 nodes, without a text source or JSON companion. SHA-256 is
4cfc37af046868c088f53a433c6a25c5acc242f8d1eb4dbb5bb6ee202effcd2e.
Inventory is 161 archives: 147 battles and fourteen calibrations. The settled
random-revival battle and all fourteen calibrations/pure checks pass with the
final rebuild. A combined 161-archive regression is not claimed; the preceding
full158 regression and affected historical aura policies remain the baseline.
The four-difference unsynchronized captures and destruction-stage prototype stay
excluded. Broader aura children, Horde effects, room/relic/control interactions,
special Boss/card mechanics and the complete simulator/optimal solver remain work.
