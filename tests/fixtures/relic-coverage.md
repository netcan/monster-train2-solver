# Original relic coverage evidence

As of 2026-10-18, **184 distinct original collectable relics / 224 catalog entries
(82.14%)** have accepted native evidence: 167 have full-battle effect evidence,
sixteen have campaign consumer interfaces validated, and one is an explicit
native no-op with seeded baseline equivalence. Except for that no-op, this counts
unchanged relic definitions whose effects actually ran, including original filters and
conditions. Test cards, unit triggers and carrier subtypes may be authored to
exercise those effects; the claim covers the recorded paths, not every possible
combination. Catalog enumeration and other owners of a supported effect class
do not count as validated relics. `RelicEffectAddTrigger` appears on 26 catalog
entries; 25 now have accepted original-owner effect evidence.
`RelicEffectAddStatusEffectOnStatusApplied` appears on eight catalog entries;
all eight now have accepted original-owner evidence. Both catalog owners of
`RelicEffectPreventStatusEffectDecay` have accepted evidence for their original
Rage and Frostbite consumers. All four catalog owners of
`RelicEffectModifyStatusMagnitude` now have accepted evidence. Unknown status
families and other effects on the remaining owners still require modeling and
native proof.
The original `ChanceForSpellWeakness` owner now has native 50% AfterSpawnEnchant
evidence for its unchanged enemy-Hero Spell Weakness effect, including eligible
hit/miss rolls, gated callbacks and the full nested effect/status definition.
`UpgradedDrafts` now has an unchanged-owner native manager probe on a spell,
covering the ordered 50-position enhancer pool, 85 exact native filter outcomes,
three supported selected pool positions, complete card-state application and
Rewards RNG. External-interaction or replacement upgrades, other target cards,
preview execution and the later draft UI choice/grant sequence are not claimed.
`2ChargesOnCardConsumed` now has an unchanged-owner positive Exhaust-carrier
play and a non-Exhaust control: corruption rises by exactly one only on the
selected floor. CorruptionAdded callback floors and already-over-maximum values
are not covered by this witness.
`ChanceForSpellWeakness` now has seven native eligible 50% AfterSpawnEnchant
rolls (four hits, three misses) and four exact Spell Weakness applications;
496 gated tests preserve Battle RNG. The winning replay and independent
128-state search pass. Other owner identities and nested effect shapes are not
generalized.
`AddSnackOnKill` now has three native enemy-Hero OnDeath witnesses that add six
cards from its exact 19-entry weighted filtered pool, plus 312 gated callbacks.
The 55-stage replay and one model-selected action have zero differential
counters; independent 128-state search has serial/parallel best equality. The
native run had no full-hand death; a synthetic full-hand control confirms both
Battle RNG selections and the single manager notification without allocating a
card. Generated-card play effects and multi-action terminal cleanup are outside
this narrow support boundary.
`FinalEndingArtifact` now has four natural post-draw champion additions and two
gated callbacks over its unchanged ordered 36-entry pool. Exact N-1 Battle RNG
shuffling, selected grant upgrades and full-hand/champion-in-hand gates match;
the 55-stage replay and one-action policy pass with zero differential counters,
and independent 128-state search has serial/parallel best equality. This deck's
champion is not in that pool, so native deck-excluded count is zero; a synthetic
filtered-pool control verifies exclusions. Eight candidates with external grant
upgrade interactions are explicitly unsupported, and generated-card play is
outside the policy boundary.
`RelicEffectModifyForgeUpgrade` appears on two catalog owners; BrokenRail and
HephsBlueprints now both have accepted native battle witnesses.
`RebalanceValor` now has an unchanged owner with native callback evidence for
its unchanged +0 Attack and +1 Armor per Valor stack. The schema-206 battle
matches 51 stages and six actions; all three tracked native Valor callbacks
match exact per-stack magnitudes and target Armor. Pure model controls cover the
vanilla/relic target, existing Armor, front-only application, turn-zero and
deployment gates, and malformed definitions; independent 128-state search
passes.
`UpgradeMaxHpFrontUnit` now has schema-207 native evidence for repeated +15
max/current HP upgrades on each populated floor's front Monster. Two floors have
back-unit controls, two floors stay empty, and each source card retains the
temporary upgrade; a retained dying front target remains unsupported.

## ChampsHealBeginningOfTurn

| AssetKey | DataId | Fixture | Evidence |
| --- | --- | --- | --- |
| `ChampsHealBeginningOfTurn` | `dbd5aec6-5046-4df2-ae61-4b646b8d1702` | `full-battle-original-champion-start-turn-heal.mt2f` | The unchanged `RelicEffectHealStartOfTurn` resets a seeded counter from 4 to 0 and runs six before-draw callbacks. Native `TargetMode.Tower` heals every wounded living Champion to full across all floors, not only the selected floor; a wounded non-Champion control is unchanged. The 70-stage battle has six actions, seven EndTurns and zero capture failures, mismatches, unsupported transitions or pending records. Dedicated native checks pass callback replay, two-floor targeting, placement exclusion, one-turn cap and owner ablation; independent search matches 128/128 states with serial/parallel best equality. |

The accepted payload boundary is `HealToFull`, Monsters team, Tower targeting,
the original Champion subtype, no conditions and deployment-phase exclusion.
Other healing behaviors, target teams, modes, subtypes or condition sets remain
unsupported. The 31,365-byte archive has 4,892 unique nodes and SHA-256
`a408d82e5159b2a2ab71543a61c3e57eee4738d939dcc6a86f66b8e2db84a6f4`; no JSON
companion is tracked. Inventory is 404 archives: 366 full battles and 38
component calibrations. Original relic evidence is 184/224 (82.14%): 167 battle,
16 campaign and one native no-op owner.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-original-champion-start-turn-heal.mt2f
dotnet run --project src/ModelChecks -c Release -- --original-champion-start-turn-heal-only tests/fixtures/full-battle-original-champion-start-turn-heal.mt2f
dotnet run --project src/ModelChecks -c Release -- --battle-search-only tests/fixtures/full-battle-original-champion-start-turn-heal.mt2f
```
The native source and callback effect for `RageOnBurnoutStackLoss` now have one
accepted battle witness for a controlled friendly Ephemeral removal.
`PyreRevengeAddPyregel` now has four live native OnHit witnesses whose complete
Tower target sequences and +2 Pyregel results match across floors.
`ExtractForEmberAndEchos` now has ordered native +1 Ember and +1 selected-room
corruption witnesses for a zero-debit Corrupt Restricted carrier; the original
owner definition is unchanged and its full battle plus 128-state search pass.
`CopyFirstConsumePlayed` now has a native direct-play Consume Spell witness: its
permanent copy enters hand and deck statistics, and its same-battle replay is
blocked by the original once-per-battle condition. The unchanged owner, full
replay and 128-state search pass.
`CopyConsumedCard` now has a native direct-play Consume Spell witness: the
unchanged owner creates two non-permanent copies, and replaying one is blocked
by the original once-per-battle condition. Its full replay and 128-state search
pass with zero error counters.
`SingleToTeamMonsterSpellTargeting` now has one native direct Monster Spell
witness: the unchanged owner copies fixed 1 Damage/DropTarget to the other
eligible Monster in the same non-Pyre room, excluding the original target and
Untouchable units. The 62-stage winning replay has zero error counters; focused
checks validate the tracked ThisTurn condition and controls. Independent search
visits 128/128 states with serial/parallel best equality, while unsupported
alternatives remain outside the explicitly narrow card/effect boundary.
`MoonPhaseAddArmorToSilent` now has six native Full/New callback witnesses: three
Full callbacks leave state unchanged, and three New callbacks add 10 Armor to
all eligible friendly Silent targets across three multi-room positive cases.
The 45-stage replay, four controlled card actions, complete-state callback
replays and 128-state independent search pass with zero native error counters.
`UncontrolledGroup` now has direct native evidence for its unchanged ordered
`RelicEffectRebalanceUnstable` / `RelicEffectNull` payload: eligible nonlethal
units on either team gain +5 Unstable, with Blast Resistant as a no-growth
control. The 41-stage battle and 5-to-10 callback replay have zero differential
counters; independent search checks 128/128 states with serial/parallel best
equality. Lethal pre-growth sacrifice and other unstable-rebalance payloads
remain unsupported.
`DamageOnFloorTransition` now has exact +3 native enemy damage after both
PostAscension and PostDescension; rooted, immobile and player movement controls
remain unchanged. Its full replay and independent battle search pass.
`PurgeToExhaust` now has exact native SelfPurge-to-Exhaust replacement evidence
for the original ordered Spell and Equipment filters, including a played Spell
that reaches Exhausted with native private trait state. Its full replay and
independent 128-state search pass.
`PyregelOnAscend_DH` now applies exactly 2 Pyregel after an enemy's native
PostAscension callback. Descension, player movement, rooted and immobile controls
remain unchanged; all 22 Bump records replay exactly and bounded battle search
passes.
`TrainRoomInstallFloor3AtStartOfCombat` now installs its unchanged cardless,
Monsters-only `FightClub` attachment on room index 2. The native upgrade is
exactly +15 attack with no other payload; its initial room is empty, and later
snapshots show the upgrade on one impacted Monster in the attached room. Focused
model lifecycle checks verify that it is removed when the unit leaves. The
60-stage, 21-action winning battle has zero error counters;
the dedicated checker confirms one impacted-unit witness and 32 attachment
snapshots, and independent search passes 128/128 states.

The battle archives cited below report game `2.2.1`, module MVID
`8fb07b96-f4db-4d2b-884d-c00536d6ccf4`, `NativeWon=true`, and zero
`CaptureFailures`, `Mismatches`, `Unsupported` and `Pending`. Archive provenance,
byte lengths and SHA-256 hashes are recorded in [manifest.tsv](manifest.tsv).
The MooncycleRestoreFriendlyHealth battle validates the unchanged owner through
seven native Full/New callbacks. Six restore exactly the lowest-health damaged
friendly unit across all rooms, capped at 10 and maximum HP; two callbacks have
eligible candidates on multiple floors, and one has no eligible target. The
original Selene Acolyte, two controlled wounds and two original Fae
Meddler phase advances provide positive controls. The winning battle and native
callback capture have zero capture failures, mismatches, unsupported transitions
or pending records. Five setup plays replay exactly; independent search matches
24 decisions across 128/128 states and serial/parallel best results. Original
CarriageJack's separate room-capacity behavior remains covered by its own archive.
The schema-168 battle validates TrainRoomInstallFloor1AtStartOfCombat: the unchanged
cardless FuelCar attachment is added to floor one at combat start for both teams.
Six Heroes and two Monsters retain its GUID-attributed OnDeath trigger in native
snapshots; Ascend removes the old-room effect in live state and model. The 60-stage
battle and independent 128/128-state search pass. TrainRoomInstallFloor2AtStartOfCombat
is separately validated with a cardless SpellSanctum attachment on room index 1:
a native 1-cost hand Spell costs 0 there, and its upgrade is removed from units
after they leave that room. The 57-stage winning replay and 128/128-state search
pass with zero error counters. The separate FirstRoomCardOnAllFloors
battle validates an ordinary-card control
does not trigger it, the first native capacity attachment is copied to all
non-Pyre floors, and a later attachment is blocked by the unchanged once-per-
battle condition. Native room and unit snapshots also match attachment upgrade
propagation. The accepted inventory is 370 archives: 332 battles and 38 component
calibrations, with no JSON companions. The new EmberOnEarlyDiscard battle starts
at zero Ember and confirms +1 per card-triggered card discard across five gaining
steps; 18 consume controls do not gain Ember. Its pure checks cover energy cap,
placement and preview gates. All 45 battle stages match, with zero capture
failures, mismatches, unsupported transitions or pending records. The dedicated
checker validates 36 hand-removal effects, and independent search matches
128/128 states, two replayed steps, and serial/parallel best results. The
ReduceSpellCostOfCheaper battle applies the unchanged FreeSpellBlessing only to
filtered spells with strictly lower cost; equal-cost, zero-cost and X-cost
controls are unchanged. Playing the upgraded 1-cost target clears its tracked
upgrade, while the 2-cost spell still in hand is cleared during
EndTurnPreHandDiscard. Two scenario actions, all 52 stages, and independent
128/128-state replay/search match with zero error counters.
A recent calibration bundles two
complete winning native battles for StarterWurmRelic: identical 21-play/seven-
EndTurn policies and all decision state except ownership, with independent root,
mid-battle and parallel replay. Actual acquisition/removal/reacquisition leaves
recorded deck/resources/frame/RNG unchanged. Base/editor interfaces alone do
not imply an activation; ownership remains present. Three affected archives pass.
The previous component proves the original
random permanent deck-copy owner through 72 native end-battle manager calls.
Sixteen fresh copies preserve permanent modifiers and flags, discard temporary
upgrades, and match exact Rewards RNG. Empty/champion-only/removed controls and
four affected archives pass. Animation and pending-animation bookkeeping are
suppressed after the actual deck mutation. An earlier battle proves two original
death/moon upgrade owners: one spawner-retained death upgrade, five moon additions
and four removals across all four callbacks. 3025 native Test filters include
enemy/subtype controls; five positive owner ablations and complete independent
policy replay pass. Six affected archives pass, including prior moon callbacks.
The previous battle proves original
full-health and first-combat draw owners through 13 actual mutating Test
callbacks and one positive natural post-opening draw. Complete independent
25-play/five-EndTurn replay and five affected archives pass. An earlier
component proves Canio through 144 actual purge-manager calls, including 24
positive rewards, six card types and combat/removed controls. Four affected
archives pass. This is callback evidence, excluding removal/payment UI.
The previous battle proves three original
pre-opening draw owners through five exact callbacks: three natural primary
draws and two controlled native secondary-filter fallbacks. Temporary upgrades,
draw-count expansion and CardDraw RNG match. Independent policy replay and five
affected archives pass; three full-hand cases are pure model controls only.
The previous battle proves three original
unit resource owners through 505 exact manager contexts, actual Tomb/ordinary
sacrifices and two equipment additions. Gold, Ember and twice-recorded trigger
conditions match; enemy/subtype/repeat controls and four positive ablations pass.
The previous battle proves all four
after-draw card-generation owners through 16 actual native Test/Apply callbacks,
including one full hand and four temporary Ephemeral upgrades. All candidate
definitions are captured before selection, with exact Battle RNG and complete
independent policy replay. An earlier component proves 756 actual end-battle
resource manager calls, including controlled Pyre damage, reset and capacity
boundaries. Four affected archives pass regression. The previous component proves 108 actual post-battle
reward-list calls, preserving original reward references, prefixes, duplicates
and owner order with unchanged RNG. Four affected archives pass regression.
The previous component proves actual draft
candidate selection and exact Rewards RNG over 840 GrantReward calls, plus
three Pyre max-health acquisition/removal transitions. Five affected archives
pass general/focused regression. An earlier battle proves three original
equipment growth owners through slay/death buffs, front-position Deathwish
entry/loss and nine actual PreCombat Valor applications. Fourteen exact births
include both-team and friendly-only controls and five exclusive trigger queues.
Seven affected archives pass the general/focused gates and bounded search.
The previous battle proves three Conduit owners through first-summon, slay and equipment callbacks, native temporary
upgrades, room selection and independent policy replay. The previous battle proves
Shroom summon Decay with positive damage and team/subtype controls.
Six affected archives pass the Conduit extension's general/focused gates and bounded search.
The previous two battles add original
spell healing and first-combat energy; eight affected archives pass current
general/focused regression, independent policy replay and bounded search.
The complete previous 335-archive general/focused regression passes; this
extension has a focused regression over its new battle and six affected older
archives. [Validation provenance](validation-20261015.md) distinguishes
the failed original shard and its corrected-checker rerun. An independent-chain
preflight of 97 legacy archives passed
67 and identified 30 old captures with card plays on disabled floors; all 30 now
have fresh replacements and complete independent policy replay.
Whole-policy-chain legacy skips have been
removed. The remaining compatibility rule is explicitly labelled and applies
only to the historical room `Enabled` field; fresh archives remain strict.

The Egg transformation archive now proves two native
OnHatched sacrifice/summon chains after the already counted `EggHatchRemoval`
effect. The original `KinhostVessel` card now has a separate native Hatch 8
play/sacrifice/Pupa witness, including a separate two-combat path with permanent
Corruption 4. Original Glugsider, Shardsoul Carver and Keeper of Echoes now have
separate `CorruptionAdded` callback battles; none adds a relic owner. The original
`DamageOnCorruptPlay` battle adds one relic owner: its two ordered RandomInRoom
effects each add one Corrupt Poison stack to an enemy on corruption increase.
The Mageblade status/cache/attack path has an accepted
native battle archive, but Mageblade is not a collectable relic and is not
included in the 151/224 owner count. The `AllUnitsMultistrike` archive
validates a mutator, not a collectible relic, and is also excluded from that
owner count.

| AssetKey | DataId | Accepted archive | Actual effect witness |
| --- | --- | --- | --- |
| `GoldOnDeath` | `265d9fb6-934f-4b7d-ba12-d3a06c48b610` | [unit resource relics][unitResources] | Original +5 reward gold on actual friendly death: two positive native callbacks. Both Tomb and ordinary sacrifice qualify; six enemy deaths do not. Gold's living-death override is preserved separately from Ember's strict dead-only gate. |
| `BigDamageFromManyMorsels` | `15f3c641-14ba-4305-8c9f-cdb2ebb4e572` | [Big Damage from Many Morsels][bigDamage] | Six actual complete-train births replay the unchanged threshold effect; native checks cover 19/20/21 Snacks, player/Pyre/enemy gates, five Tower targets and seven room/character notifications. Its 113-stage winning battle has zero differential counters. Bounded search visits 128 states and replays three steps with serial/parallel best equality; unsupported branches remain excluded. |
| `EmberOnEarlyDiscard` | `44149a09-df55-4864-b5d9-f30c4e659999` | [early discard energy][earlyDiscardEnergy] | The unchanged +1 effect triggers for card-caused discard of each card, but not Consume; five native steps gain Ember and 18 consume controls gain none. Pure checks cover the energy clamp, placement and preview gates. Full battle and independent 128/128-state replay/search match. |
| `ReduceSpellCostOfCheaper` | `9cc85dbb-d0ae-409b-8f26-41d2202eb1e0` | [cheaper spell cost][cheaperSpellCost] | Unchanged FreeSpellBlessing applies only to filtered spells with strictly lower cost; equal-cost, zero-cost and X-cost controls are unchanged. Played-card and end-turn discard clean up tracked upgrades. Full battle and independent 128/128-state replay/search match. |
| `ReduceFirstSpellCostPerDeckSize` | `64d9943e-02e7-469d-b516-c483a3ad8ccf` | [first spell cost per deck size][firstCardCost] | The unchanged 15-card DeckPile threshold applies cost/X-cost reduction to eligible Spells, clears hand upgrades after the first Spell, and re-arms through the per-room EndOfTurn callback. The schema-205 battle matches 52 stages and three scripted actions; dedicated checks and 128-state serial/parallel search pass with zero unsupported attempts. Other owners and card/pile variants are not claimed. |
| `RebalanceValor` | `76511782-d2c1-43d1-9878-e465758017d8` | [RebalanceValor][rebalanceValor] | The unchanged native owner keeps Valor attack at +0 and increases Armor to +2 per stack (base +1 plus relic +1). Three tracked callbacks match their native per-stack getters and target four Armor at two stacks; turn zero/deployment are excluded. The schema-206 battle matches 51 stages and six actions; dedicated checks and 128-state serial/parallel search pass with all error counters zero. Other owners and status-stack variants are not claimed. |
| `UpgradeMaxHpFrontUnit` | `cb2c9d18-9a05-4523-acdc-6e4af73152aa` | `full-battle-original-front-room-health-upgrade.mt2f` | Four native start-of-turn callbacks apply exactly +15 max/current HP and one matching upgrade to the first player Monster on floors 0/1; each second unit and empty floors 2/3 are controls. Source-card temporary modifiers track only the front targets. The schema-207 battle has 54 stages, four actions, five EndTurns, and zero error counters; dedicated native/model ablation and 128-state search pass. Retained dying front targets remain unsupported. |
| `NefariousOverflow` | `b0575799-1511-489d-a05d-b5b2e6bf84c2` | [NefariousOverflow][nefariousOverflow] | The unchanged both-team payload adds 1 explosion damage per Unstable stack: the five-stack carrier's death kills the Mage and reduces the Guard's Armor 17→7. Proc-damage and growth hooks remain unsupported when nonzero. Full battle and independent 128/128-state replay/search match. |
| `UncontrolledGroup` | `4c737e1e-7e5c-40ee-87a3-944859784283` | [UncontrolledGroup][uncontrolledGroup] | The unchanged ordered two-effect owner adds +5 Unstable to eligible nonlethal units on both teams; Blast Resistant suppresses the callback. Native growth callback and full 41-stage replay match, with independent 128/128-state search. Lethal pre-growth sacrifice and sibling payloads are not claimed. |
| `CopyFirstSpellPlayed` | `0f4a9e72-cd34-4667-95f6-6a0ca69832f8` | [CopyFirstSpellPlayed][copyFirstSpell] | The unchanged once-per-turn owner copies the first direct Spell with temporary Ephemeral; its generated copy plays again the same turn without retriggering. Exact original owner only; sibling copy payloads are not generalized. |
| `CopyFirstConsumePlayed` | `b7207406-3c55-4771-b9c9-ed99dd9e1f9b` | [CopyFirstConsumePlayed][copyFirstConsume] | The unchanged original owner copies the first direct-play Spell carrying the required Consume trait as a permanent hand card, and the copy enters deck statistics. Playing that copy in the same battle does not retrigger the ThisBattle condition. Native replay and independent 128/128-state search pass; sibling copy payloads are not generalized. |
| `CopyConsumedCard` | `51368ce4-b037-49ef-801c-545405b78800` | [CopyConsumedCard][copyConsumedCard] | The unchanged owner copies a direct-play card with the required Consume trait twice, irrespective of card type, as non-permanent hand cards. Playing one copy in the same battle creates no third copy. The captured Spell path passes native replay and independent 128/128-state search; other card types are not claimed. |
| `SingleToTeamMonsterSpellTargeting` | `070c87f3-a48f-49d6-a288-04854ab3b0cf` | [Monster Spell targeting][monsterSpellTargeting] | One direct unupgraded, traitless 1-Damage DropTarget Spell targets Monsters; the unchanged owner copies the effect to the other eligible same-room Monster, excluding the original and Untouchable units. The ThisTurn condition and full native replay match. Other effect payloads, upgrades, traits, OnCast, Pyre-room and mixed-effect cases remain unsupported. |
| `GarfieldBoxRandomCardPerTurn` | `d537c198-899f-4385-9552-f35f58826be4` | [GarfieldBox random card][garfieldBoxRandomCard] | Six unchanged after-draw callbacks replay the native partial shuffle over the full 348-entry pool, selecting only the two active player clans (58 eligible) before adding one non-permanent card to hand. The winning 61-stage replay and independent 128-state search pass. Placement/preview and synthetic full-hand RNG-only controls pass; other clan selections and broader candidate pools are not claimed. |
| `MoonPhaseAddArmorToSilent` | `e013b17d-753b-47b8-a837-02af85addb4b` | [moon-phase Silent armor][moonPhaseArmor] | The unchanged New-only owner adds 10 Armor to each eligible friendly Silent. Three Full callbacks are no-ops; three New callbacks target both witnesses across separate rooms, with complete train state matching. Pure controls cover phase, team/subtype, Pyre/dead/Untouchable/immunity/Purify exclusions, status metadata, preview and unsupported target callbacks. The winning replay and independent 128/128-state search pass; other moon-phase status owners and callback families are not claimed. |
| `StartOfTurnAddFunguysPerDeckSize` | `e5d7c9d0-c0a3-4b0c-97cf-6def6ad287e9` | [Funguy deck scaling][funguyDeckScaling] | Six original callbacks at 40 visible cards apply two cardless Horde effects on each live room: three Funguys spawn and Horde rises by six total. Relentless room destruction is respected; support is limited to this original payload and captured positive path. |
| `CardDrawOnHeal` | `9a38370b-150c-45bd-9e3e-93a688a04850` | [CardDrawOnHeal][cardDrawOnHeal] | Four allied OnHeal callbacks draw exactly one card after native ThisTurn reset; a second same-turn heal is suppressed after the two native notifications, and an enemy-heal control does not trigger the Monsters-only owner. Placement/preview gates are pure controls; targeted test-card setup is excluded from full-policy replay. |
| `BrokenRail` | `1699b5c7-8c34-471c-8e5a-c9830e2bc482` | [BrokenRail forge upgrade][brokenRail] | The unchanged +2 HP forge upgrade scales to +6 at three forge points on one eligible ordinary monster summon. Native direct-play and full-battle state match; bounded 128-state search matches with serial/parallel best equality. Tracking/toggle/eligibility controls are pure; room-specific replacements and non-monster plays remain unsupported. |
| `RageOnBurnoutStackLoss` | `85667d60-8053-4204-8c02-ba101dab7945` | [Ephemeral-loss Rage][rageBurnout] | The unchanged Monsters-team owner adds exactly 3 Rage when one friendly Ephemeral stack is removed. The native action and full 41-stage battle match with zero error counters; every recorded action and turn transition replays. Pure controls cover raw requested counts, absent/zero status callbacks, the all-stacks sentinel, team/status gates, exclusions, preview and immutable branches. The controlled scripted action is outside generic policy replay; this does not claim a bounded-search result or natural burnout lifecycle coverage. |
| `PyreRevengeAddPyregel` | `82b22203-9da1-4f83-9e84-826fb8d1d280` | [Pyre Revenge Pyregel][pyreRevenge] | The unchanged Pyre-only OnHit trigger applies exactly +2 Pyregel to all eligible Heroes. Four live native callbacks match complete-train Tower target IDs and order, including remote-floor state writeback; the winning 60-stage replay has zero error counters. Pure controls cover Pyre-only installation, preview exclusion, cross-floor persistence and immutable branches. This does not claim bounded-search coverage. |
| `ExtractForEmberAndEchos` | `4d237444-5beb-43df-a545-c68a7ae86ba6` | [ExtractForEmberAndEchos][extractForEmberAndEchos] | The unchanged ordered callbacks grant +1 Ember then +1 corruption to the selected room for a zero-debit Corrupt Restricted carrier. Native callback values and full 61-stage replay match with zero error counters; energy cap, nonmatching-card, preview, placement and immutable controls pass. Positive pre-play debit and conditional effects remain unsupported; 128-state search passes. |
| `EmberOnTombDeath` | `4a5b1613-568c-4bf6-8bf7-15f8bf013345` | [unit resource relics][unitResources] | Original +5 Ember after actual Tomb sacrifice; ordinary friendly sacrifice and enemy deaths do not qualify. Complete manager contexts and exact resource/RNG state match. |
| `Frenzystesia` | `56316fc2-b41a-41ae-aba8-e83fb668e8e1` | [unit resource relics][unitResources] | Original +1 Ember on the first equipment callback this turn. Two actual equipment additions prove the first-only condition, with notification count recorded once by the effect and again by RelicManager. Full 22-play/five-EndTurn policy matches, including native duration resets. |
| `RandomEquipmentCardFirstTurn` | `7a1d5b5d-cd08-4935-b629-d3d441905100` | [original opening draw][openingDraw] | Natural Equipment primary draw plus controlled native fallback to an Equipment with the deployment-draw trait. Original temporary -99 cost, count expansion and CardDraw RNG match. |
| `DrawToMaxStartOfCombat` | `2058bafa-a15c-4787-9b9e-4dc7fdf09b74` | [original turn draw][turnDraw] | One positive natural turn-one post-opening callback. Draws maximum hand size minus starting draw size, independently of current hand count; Int=10 is unused. Complete policy and bounded search replay match. |
| `PyrePiping` | `44aae53e-ee70-4b2c-8d25-a02f977349ee` | [original turn draw][turnDraw] | Native start-battle/before-draw Test updates the registered +1/0 mutable value while returning false. Thirteen callbacks include controlled full/partial/zero health and natural full-health draws; the branch model refreshes after combat damage. |
| `EnhanceAttackOnConsume` | `52c41ba0-f720-4b7e-9dd0-bd318be6acae` | [original death/moon upgrades][phaseUpgrades] | Actual friendly OnDeath adds original +10 attack upgrade and retains it on the spawner's temporary card modifiers until battle end. An actual sacrifice proves the gain; owner removal removes exactly that future upgrade. The upgrade's attacking-unit filter is not consulted by the direct native unit API. |
| `SeleneandSilentBuff` | `e54e6cb7-f424-44be-a7d2-869c6fb7bb15` | [original death/moon upgrades][phaseUpgrades] | Four original subtype-filtered callbacks add/remove unique +20 attack for Selene in Full and Silent in New. Five additions/four removals, initial birth callback, ordinary/enemy controls and four positive next-turn ablations match. Unit-death lifetime upgrades do not enter spawner modifiers. |
| `RandomTrainRoomCardFirstTurn` | `d245f9d5-e966-4a8e-a125-4aa5b48f1e7c` | [original opening draw][openingDraw] | Natural room-card primary draw plus controlled native fallback to a room card with the deployment-draw trait. Both original masks, temporary -99 cost and count expansion match; the inert room carrier is not played. |
| `DrawSpellOnDeployment` | `69d88251-cfed-4fd0-a355-cba2e10e09d4` | [original opening draw][openingDraw] | Natural original-mask Spell draw and temporary -1 cost/+1 X-cost reduction, without draw-count expansion. Full native contexts, preview/nondeployment/removed-owner controls and independent 19-play policy match. |
| `GiveSpellStatsForUpgrades_DH` | `4200307a-616d-4cbb-a54e-527c5dc0f492` | [upgrade-count relics][upgradeCounts] | Original +3 damage per permanent upgrade with a visible icon: 0/1/2 yield 0/3/6. Hidden, no-icon and temporary upgrades are excluded; a visible region upgrade counts. Original `DevilishDetails` exclusion rejects the upgraded spell. The damage spell is played and redrawn with its bonus intact. |
| `IncreaseSpellHealing` | `03613302-e55a-4e6c-99a1-c198c8d3175c` | [original healing power][healingPower] | Unchanged +10 healing to every Spell, independently of native magic-power eligibility. Non-scalable Spell, Monster/Blight rejection, fivefold trait multiplier, reset/repeat and owner ablations pass all 22 manager calls. A controlled damage-then-heal spell actually restores health; damage remains unchanged. |
| `OneEmberFirstTurn_DH` | `c5a68f6d-671d-4db3-999f-120b5febf41b` | [original first-combat energy][firstCombatEnergy] | Original amount is +2 despite the asset name. Acquisition does not add energy; the natural first-combat callback runs once on turn one after initial enemy spawn and before regular income, with zero notifications. Deployment/owner-ablation controls and all seven EndTurns match. |
| `SteelStockpile` | `b2868935-103c-4d27-ae48-a96d947c05ed` | [original forge conservation][forgeConservation] | Original flat cap 10 is applied by actual `PlayerManager.OnCombatEnd`: 0/5/10/17 retain 0/5/10/10, including one notification at zero. Removed-owner control resets 17→0. Natural post-victory cleanup retains 17→10; all seven in-battle EndTurns keep 17. All six full callback contexts match. |
| `IncreaseMaxCorruption` | `3a28026e-e44b-4456-b535-7134e7525240` | [original opening corruption][openingCorruption] | Natural start-battle callback raises all four room maxima 4→5, including Pyre room. Original +1, placement restriction and no notification remain unchanged; no later-turn repeat. |
| `UnremovableCorruptionTop` | `a66188b9-845d-475e-b6e6-7ea5335ec00b` | [original opening corruption][openingCorruption] | Native floor index `(int)2f` applies permanent +1 only to top floor and adjusts current by one. Original cap/clamp semantics and single notification match, before post-draw effects. |
| `CorruptionBattleStart` | `a528a1b2-0d14-4fc5-9b8f-4b1f23722422` | [original opening corruption][openingCorruption] | The effect runs after deployment's opening draw, adding 2 to each room after the preceding two owners. Final max=5 everywhere; top current/permanent=3/1, others=2/0. Zero notifications; all seven EndTurns keep these values. |
| `GiveUnitStatsForUpgrades_DH` | `fc1f6104-b3c1-4714-ac0f-9c76a6fb1a77` | [upgrade-count relics][upgradeCounts] | Ordered original health then attack effects give 0/3/6 to both temporary offsets for 0/1/2 visible permanent upgrades. A real Train Steward summons with +6 attack/+6 max health. All 44 manager refreshes match, including reset/repeat, card-type controls and independent owner ablations. |
| `ExtraSpellCastTrigger` | `410ba540-7c4f-4dc5-a84f-b1d8af508891` | [combined trigger relics][counts] | `IncantTriggers`: 65 player `CardSpellPlayed` queries have `FireCount=2`; five enemy queries remain 1. Full callbacks and the 21-play policy match. |
| `ExtraSpawnTrigger` | `9e0deb69-6196-44a6-8220-85bd0df25f77` | [combined trigger relics][counts] | 26 player `OnSpawn` queries have `FireCount=2`; five enemy queries remain 1. The battle contains new paid unit births. |
| `ExtraDeathTrigger` | `a5d67620-a9ec-4257-91b5-305336e11987` | [combined trigger relics][counts] | 26 player `OnDeath` queries have `FireCount=2`; five enemy queries remain 1. Final `MonstersDeadThisBattle=5` confirms actual deaths. |
| `SpawnWithArmor` | `68ef2523-5c2e-4660-b96d-00b1c0485f54` | [spawn status relics][statuses] | All five player birth records add 5 Armor. Example: `RelicSpawnStatuses[0]`, unit 2, Armor absent → 5. |
| `FirstUnitGainDamageShield` | `60a2a8a3-5f7a-4a9d-b427-5f261145fa1f` | [spawn status relics][statuses] | The first player birth receives 2 Damage Shield on three different turns; two same-turn births skip it. Captured condition counters and turn resets match. |
| `FrostbiteOnEnemies` | `270356af-16bd-4433-a0b2-3e5bb94ef890` | [spawn status relics][statuses] | All seven enemy births receive 2 Frostbite, serialized by the native game as status ID `poison`; player births do not. Example: record 3, unit 5. |
| `EggHatchRemoval` | `8c8a2a2c-500c-4b77-90fd-0d309b94bb2c` | [Egg Hatch battle][eggHatch] | Its unchanged first effect removes 3 Hatch from two authored Egg-subtype births; the second adds 30 Armor, yielding 35 with the separate original +5 Armor relic. The 12 exact birth phases and full battle replay match. Natural Egg transformation remains untested. |
| `StealthWaxers` | `22f5ff29-69be-4043-9fe1-245392ea3c95` | [StealthWaxers battle][stealthWaxers] | Four player births satisfy the unchanged `SubtypeInDeck >= 1` condition and gain one Endless; each condition count advances once. The other births do not receive Endless. |
| `HymnistsApplyValorOnSummon` | `55cf29f3-bc0d-4b5f-929e-07d4629141b3` | [team-wide spawn status][teamSpawnStatus] | The unchanged Hymnist-filtered effect adds 5 Valor to all living same-team units in the room. A same-room clone battle records 33 existing teammate witnesses across 22 exact birth phases; all 13 clone operations and 32 branches match. |
| `TunedGear` | `f12924bc-0600-4de0-ae2a-93534f251eb1` | [TunedGear Burst battle][tunedGear] | Five player births gain one Burst; seven enemy births do not. The 12 native birth phases, Burst attack count/consumption, 51 full-battle stages and 32 focused branches match. |
| `BalatroEventChicot` | `92d66578-b148-49f8-9880-edb67f8b18de` | [Chicot Mute battle][chicot] | One native BossAll birth gains 3 Muted; the schema-156 replay matches all 52 battle stages and 32 isolated birth branches. |
| `DantesCandelabra` | `a3dde0d6-0aa3-44e5-a6a4-0e993a5d9a15` | [Pyre spawn-status battle][pyreSpawn] | Acquired before battle, its unchanged Pyre-only spawn effect adds 2 Titan Skin during the game's natural `CreatePyreHeart` path. The first native birth state and subsequent battle replay match. |
| `PressurizedBoiler` | `fda361aa-fecc-4adf-b19c-f92af04e0e19` | [Pyre spawn-status battle][pyreSpawn] | Acquired before battle, its unchanged Pyre-only spawn effect adds 4 Burst in the same natural Pyre birth. The model matches the birth state, 52 room stages and 18 actions. |
| `ReduceStarterCost` | `d892106c-dbbc-40ac-bf0b-902882cdc69d` | [starter cost relic][cost] | 124 exact manager calls and 49 notifications cover filtering, unique repeat rejection, reset and generated cards. `Actions[0]` actually plays `JustCause` for 1 Ember with base cost 2 and the original `ReduceStarterCost_Upgrade` cost offset −1. |
| `ExtraToughTombs` | `61fdeafd-21c5-4009-a448-a5c286ac3fc0` | [ExtraToughTombs battle][extraToughTombs] | The original `OnlyTombs` filter adds 30 health and its unique `AddBurnoutToTombs` upgrade. The played Tomb carrier summons at 55/55 with one `ephemeral` stack; post-combat burns it out and all 59 room stages match. |
| `PyreHeartSon` | `8503360a-26b2-4b71-9334-4c6014ad397f` | [cardless relic upgrades][cardless] | Five ordinary cardless births gain +5 attack/+12 health; five cardless clones skip the upgrade. Example: record 8, unit 10, attack 0 → 5 and max health 1 → 13; record 9's clone remains 0/1. |
| `MoltenManifold` | `f141d334-b503-48ff-ac21-5f2ad27105dd` | [original relic batch][batch] | Original `TinkerersToolbelt_Upgrade` contributes `XCost=2`. Sixteen X-cost plays include nine positive payments; 20 damage-scaling witnesses match native values and differ when the relic upgrade is removed. Manager calls also exercise repeat stacking, reset and non-X rejection. |
| `HerzalsAnvil` | `4e0f1e86-f122-4b20-82cf-7c997e4ee84a` | [original Forge Points relic][forge] | Its unchanged `RelicEffectAdditionalForgePoints` adds 1 to positive adjustments. Native battle state changes; independent replay matches and owner ablation removes exactly that point. |
| `SpawnWithRage` | `55ca34e9-047b-4b93-b390-d8d228a43261` | [original relic batch][batch] | All five player births gain 3 Rage (`buff`). Example: record 0, unit 2, absent → 3. |
| `UnitsGainLifesteal` | `a6b0b657-8893-4cc8-bc62-ec09544c4306` | [original relic batch][batch] | All five player births gain 2 Lifesteal (`lifesteal`). Example: record 0, unit 2, absent → 2. |
| `ContingencyPlans` | `0278309e-d15c-4041-a8dc-b76f360f306b` | [original relic batch][batch] | Two Monstrosity births gain 7 Spikes; the other three player births do not. Records 0–1 accept the authored Monstrosity carrier; record 2 rejects the other carrier. The relic and subtype filter are unchanged. |
| `StingBuffPower` | `0f6d3edc-d212-4e3a-877f-e9eb2ac78339` | [original Sting relics][sting] | Its unchanged unique `OnlyStings` upgrade adds 20 damage. Three played Sting cards retain the upgrade; all 53 combined native manager calls match repeat/reset/filter behavior and 32 immutable branches. |
| `StingBuffPiercing` | `2d367bde-7616-4804-a6c9-910060ae5fd7` | [original Sting relics][sting] | Its unchanged unique `OnlyStings` upgrade adds 10 damage and IgnoreArmor. The same three plays resolve to +30 damage with IgnoreArmor; its `RelicEffectAddCardsNull` companion is tooltip-only. |
| `NonBannerUnitsHaveDeployable` | `fb6332bb-3e1c-44ba-951b-39fbc8d43601` | [original deployment-phase relic][deployable] | Its unchanged temporary upgrade adds `CardTraitDrawInDeploymentPhase`; two eligible non-Banner units enter the first decision hand and both are independently played. All 48 native manager calls match the non-unique repeat, reset and filter behavior. |
| `ImpDamageBuff` | `94595f3a-4624-4408-977d-b166a98f23d2` | [original unit relic batch][units] | Original unique Demon-filtered upgrade adds Multistrike 1. Twenty-seven native exchange observations contain at least two attacks by a Demon carrier, with complete combat state matching. |
| `StewardSacrificeDamage` | `f6a4e405-98b9-4eeb-bae1-50cb204d12ed` | [original unit relic batch][units] | Six original `StewardSacrificeDamageAbility` activations remove their own units; three damage enemies. Three paid source cards exhaust and three cardless source copies retire from ownership. All queued callbacks and physical position changes match. |
| `AddAttackToFunguy` | `25166141-810b-46ae-ac48-b5560083355a` | [unit-upgrade trigger relic][unitUpgradeTrigger] | Four unchanged native `OnTroopAdded` callbacks apply +4 attack for two Horde stacks (2 → 6); four `OnTroopRemoved` callbacks include one observed removal of a +2 upgrade (5 → 3). All 23 card actions match. |
| `MorselsGetDamageShield` | `d1daea7e-3346-47df-a756-c839b044e5f3` | [original unit relic batch][units] | Three paid Morsel births retain their starting shield without repeating the spawn effect; three cardless births gain one additional shield. One native exchange consumes a shield while preserving its owner's health. Both original effects and filters remain unchanged. |
| `ValorTransferOnAngelDeath` | `8c493cd7-8048-4598-bdd5-6cc2801cd842` | [original unit relic batch][units] | Twenty-eight native Shift queries cover player 2, enemy body 1, original enemy equipment 2 and excluded trigger 1. Eight player shifts and one equipped-enemy shift double the moved unit's gold callback and match other same-team callbacks. |
| `EquippedUnitsGainArmor` | `9af0364b-8e05-45d9-be46-7b7dd1e721e7` | [original trigger relics][triggers] | Original OnEquipmentAdded grants 10 Armor and permits count modifiers. An authored native +1 count carrier produces fire count 2; two player attachments give two positive effect counterfactuals. |
| `DamageShieldOnEquip` | `0c50313f-1f07-40c4-8a3e-54748b57981f` | [original trigger relics][triggers] | Each player attachment adds one shield. Its original exclusion flag keeps fire count 1 with the same +1 equipment modifier; two positive effect counterfactuals. |
| `ManyBuffonAngelEquip` | `c97500a9-b0a4-4c18-b203-b9f4447c84c3` | [original trigger relics][triggers] | Original source team 255 admits both teams, filtered to Angels. Two player and one enemy attachments each add 3 Valor, 3 Rage and 3 Regen; complete states and three effect counterfactuals match. |
| `SurgeonsStitcher` | `8f22145d-5c41-45da-8abf-95bc5f369648` | [original trigger relics][triggers] | Original 999999 Self heal restores a wounded 68/80 player to 80/80 on attachment. Removing its installed trigger loses that heal. |
| `AngelsSpawnWithShiftyValor` | `3d4705ec-ee6b-433b-88c8-1b4ce0ed7cc9` | [original trigger relics][triggers] | Original Angel OnShift adds 2 Valor with fire count 1 despite the original +1 Shift relic. Ten Bump plays provide six positive effect counterfactuals; physical positions and other same-team callbacks also match. |
| `ExtinguishSpreadPyregel` | `9142aaa7-072e-43ca-bc5e-2c9995061174` | [stationary trigger relic deaths][triggerDeaths] and [physical trigger relics][triggers] | The unchanged no-subtype OnDeath trigger installs on both teams except the Pyre. It transfers explicit Pyregel stacks to every eligible opposing Hero on the source floor, ceil-divided per target, then removes the source stacks. Owner ablation changes actual status state four times in the stationary battle and seven times in the physical battle. |
| `FusedPyreshard` | `eae7f78f-fe81-4418-a491-d75413dbb55d` | [Fused Pyreshard][fusedPyreshard] | The unchanged Monsters-source, Pyre-subtype OnKill trigger adds 5 to the saved Pyre attack modifier. Native `RelicState.OnCharacterAdded` installs exactly one trigger on the live Pyre; the battle raises the modifier 0 → 10 on two Pyre kills. Five non-Pyre player-kill controls leave it unchanged; two trigger ablations remove the effect. |
| `RageOnHymnistDeath` | `ec46f782-87ad-4cbd-aacb-023701417283` | [stationary trigger relic deaths][triggerDeaths] | Original player OnDeath targets eligible Angels on its floor with 7 Rage. Two combat counterfactuals change actual unit/status results. This effect has no positive witness in the separate physical Shift archive. |
| `WhelpsGainExtinguishHealPyre` | `e6a167d5-f57e-4a02-a0f0-b8cf9373c838` | [original trigger relics][triggers] | Its actual payload adds 5 Pyregel to enemies on a Whelp's floor. The original exclusion flag prevents the +1 Death relic from doubling it. Four death/action/combat counterfactuals change unit/status results. |
| `DragonsGainStrikeAddPyregel` | `1fdf2fde-a5e7-4eae-86eb-d432145c4621` | [original trigger relics][triggers] | Original Dragon Strike adds 5 Pyregel to LastAttackedCharacter. Five action/combat counterfactuals change unit/status results. |
| `WhelpStrikeAddPyregel_DH` | `588825ab-8517-48db-8447-31b48e931aaa` | [original trigger relics][triggers] | Original Whelp Strike adds 3 Pyregel to LastAttackedCharacter. Six action/combat counterfactuals change unit/status results. |
| `ApplyExtraPyregel_DH` | `f3e1c306-964c-4373-96c3-61e606379f43` | [original status relics][statusAdders] | The unchanged None-team positive-stack rule adds 1 Pyregel. Four distinct real native query cases lose that extra stack when only this relic is removed. |
| `IncreasePyregelStacks` | `f4c76f11-37d9-4bb5-867a-4d99b7cfa270` | [original status relics][statusAdders] | The unchanged Heroes-team first-application rule adds 6 Pyregel. A real first application is 1 → 8 with both original Pyregel relics; repeated application is 1 → 2. Removing this relic changes the first case by exactly 6. |
| `ExtraValorOnValor` | `74c025fa-ddcb-494b-a790-dcadc5b5772b` | [original status relics][statusAdders] | The unchanged Monsters-team unconditional rule adds 1 Valor, including zero input. Eight distinct real native query cases lose that stack when only this relic is removed. Starting, spell and combat-trigger status paths replay exactly. |
| `IncreaseArmorStacks` | `ba26070b-4f6b-4af0-a1ec-2af024b6af87` | [original status relics][statusAdders] | The unchanged Monsters-team positive-stack rule adds 4 Armor. Five distinct real native query cases lose exactly 4 stacks when only this relic is removed. Starting, spell, equipment-upgrade and combat status paths replay exactly. |
| `ExtraSpellWeakness` | `b1d918d8-1fa6-4e6d-b523-5f7f339e5af8` | [original status relics][statusAdders] | The unchanged Heroes-team positive-stack rule adds 1 Spell Weakness on enemies. Two real native query cases lose that layer when only this relic is removed. |
| `IncreaseDecaySlimeStacks` | `55c06c99-7cee-4d7e-ad17-f893722d8071` | [original status relics][statusAdders] | The unchanged None-team rule adds 1 Corrupt Poison stack; two real applications lose exactly 1 when only this relic is removed. Its native `OnAlmostPostRoomCombat` stage matches the model, including corruption-scaled damage, the turn-zero gate and Pyregel interaction in the 39-stage battle. |
| `DamageOnCorruptPlay` | `f302d4a1-13ba-4907-a26d-f0d8eead6f2e` | [corruption relic battle](full-battle-corruption-relic-trigger.mt2f) | Original Unearthed Remains raises room Corruption 0 to 1 during MonsterTurn. The unchanged relic's two ordered RandomInRoom effects each add 1 Corrupt Poison to an enemy, producing two positive enemy-status changes; Echoes of the Past removes Corruption without repeating them. All 50 native stages, 19 actions and bounded serial/parallel searches match. |
| `IncreaseBurnoutStacks` | `dc32326b-0952-4b3b-8bc8-cec909476c5f` | [original burnout stacks][burnoutStacks] | The unchanged Monsters-team positive-stack rule adds 3 Ephemeral. A real player application is 1 → 4; removing only this relic returns it to 1. The complete 41-stage battle and 336-query Ephemeral boundary matrix match. |
| `IncreaseRageDamage` | `b9bddf11-acaa-40fb-bc40-eee83e478424` | [original magnitude relics][magnitudes] | The unchanged Monsters-team buff magnitude adds 1 attack per future added/removed Rage stack. Removing only this owner preserves captured starting attack history but changes subsequent status attack in 19 action/combat witnesses in the refreshed archive. |
| `StrongerSpikes` | `aa94ad34-ee09-464e-89ff-2a2c2867c097` | [original magnitude relics][magnitudes] | The unchanged Monsters-team spikes magnitude adds 1 damage per stack at retaliation. Ten player-owned Spikes event witnesses in the refreshed archive deal less real damage after removing only this owner. |
| `AwokenRegen` | `1fd244de-7d7b-4345-974b-4e5bf287a3df` | [original magnitude relics][magnitudes] | The unchanged Monsters-team regen magnitude adds 1 healing per stack at post-combat. Twelve player-owned Regen event witnesses in the refreshed archive restore less actual health after removing only this owner. |
| `DoubleMagebladeDamage` | `622860d3-b2ca-45b0-991a-b7745e1dcd5c` | [original Mageblade magnitude][magebladeMagnitude] | Its unchanged Monsters-team Mageblade magnitude adds 1 stack. A positive native query and all four team masks match; removing only this owner removes the bonus, and two starting stacks produce the native cached coefficient 4. |
| `IncreaseArmorStacks_DH` | `c9b18f6f-20d1-4228-bfab-b740609db8dc` | [original lifecycle relics][lifecycles] | Original player OnOwnAbilityActivated adds 3 Armor to player units on its floor. Six real ability activations provide 24 positive unit-state witnesses after removing only this owner's installed callback and context relic. The original exclusion flag keeps fire count 1. |
| `AvariceOnSummon` | `644749c2-6ee3-4ce6-91ed-420077ee0193` | [original lifecycle relics][lifecycles] | Its actual OnOwnAbilityActivated payload applies original `Plus5Attack` (`069b201b-ee72-4daf-99d8-914d56f1bad5`), non-unique and `TemporaryUntilEndOfBattle`, to player units on its floor. Twenty-four positive witnesses lose future +5 attack gains when only this owner is removed; existing applied upgrades, raw attack and shared ability-card caches remain intact. |
| `ShroomsGainRegen` | `bbe03be9-dae9-40bb-bb46-ab7553a17bd9` | [original lifecycle relics][lifecycles] | The unchanged Shroom installation and target-subtype filters grant Regen 1 on OnAnyUnitDeathOnFloor. Sixteen positive future Regen witnesses differ under owner removal; three Shroom births accept the trigger, four other player births and seven enemy births reject it. |
| `DamageOnGoingDownFloor` | `19147050-9859-4cf1-b826-f258f6c48c42` | [original lifecycle relics][lifecycles] | Its actual OnDeath payload adds 5 Armor to FrontInRoom on the player team, with original action value 100. A real Undying death/revival gives the front Shroom Armor 5; removing only this owner loses that gain in one positive witness. |
| `LifeInsurer` | `c1ae061c-5f95-4f1d-a8f0-6e027636a81e` | [original lifecycle relics][lifecycles] | One natural Undying revival consumes a stack and restores health 0 → 1, then original OnReanimated grants its actor Armor 14. One positive owner ablation loses that Armor. The native queue preserves Harvest callbacks followed by OnReanimated and OnDeath with parameter 1. |
| `RagePreservedOnFriendlies` | `b7822614-96ec-4ace-9029-6efc8adef374` | [original decay relics][decays] | The unchanged Monsters-team buff decay rule gives 14 actual player Rage-preservation witnesses. Enemy Rage still decays as a negative team case. Removing only this owner changes future preserved stacks while retaining captured starting attack history. |
| `IncreaseFrostbiteStacks` | `9cb2b9b2-0308-4f71-92e8-e96a30b87416` | [original decay relics][decays] | Original None-team poison+2 turns real card applications from 1 into 3 stacks on both teams; its second effect prevents natural poison decay. Owner removal gives 25 combined preservation and real-application witnesses, preserving prior attack caches and leaving the card's buff2 and armor30 effects unchanged. |
| `SilverIncreasedDraw` | `4a51a457-9c50-4623-b8f5-e1bd9aac561a` | [original resource relics][resources] | Original starting-hand rule adds 1 draw. Six real empty-hand draws produce eight cards with the full batch and seven when only this owner is removed. |
| `SilverIncreasedEmber` | `1a69fbff-9f1d-4c98-92ff-a63e0ac520c5` | [original resource relics][resources] | Original +1 ordinary and +1 deployment callbacks have seven positive future energy witnesses; native signed callback bodies and notification rules match. |
| `Inkbound_LeakyPen` | `19c5ec66-4dde-4161-8fcf-68caf38a293c` | [original resource relics][resources] | Original -1 ordinary energy and +2 starting-hand rules have 12 combined energy/draw witnesses. Owner removal increases actual energy by 1 and changes real batch draws from eight to six cards. |
| `LessDeployEmberMoreOtherEmber` | `7bc61ac1-f295-4bf5-a978-1c538c8f2e51` | [original resource relics][resources] | Original -3 deployment and +1 ordinary energy have seven positive callback witnesses. The deployment subtraction is observed before ordinary callbacks, preserving its actual negative contribution. |
| `MoreDeploymentPhaseEmber` | `83116c76-f5cc-4912-8a56-872731393975` | [original resource relics][resources] | One actual first-deployment callback contributes +3 energy; its owner ablation lowers that intermediate native energy boundary by 3. It is not inferred from a display getter. |
| `BossEmber` | `780f977e-4007-45ae-900f-c8929b2bf000` | [original resource relics][resources] | All four original effects have positive witnesses: ordinary/deployment +1 energy, native acquisition +20 HP and table-index-0 Pyre attack +30. Fourteen combined witnesses include actual Pyre combat; catalog Int30/10 are not substituted for the initialized HP/attack amounts. |
| `BossEmber2` | `6eef1e2d-3bd5-4f04-bd88-d0c24d5eb691` | [original resource relics][resources] | All four original effects have positive witnesses: ordinary/deployment +1 energy, acquisition +20 HP and table-index-1 Pyre attack +55. Fourteen combined witnesses use its initialized lookup amount rather than catalog Int10. |
| `BossEmberDH` | `50e516a6-f27a-4878-9586-c0efce046206` | [original resource relics][resources] | All four original effects have positive witnesses: ordinary/deployment +1 energy, acquisition +20 HP and table-index-1 Pyre attack +55. Fourteen combined witnesses cover actual acquisition, timing and Pyre combat with unchanged original payloads. |
| `BossIncreaseTurnDraw` | `f9cc17ee-b7e5-45de-a02f-bd271e2dcd1a` | [Pyre refresh relics][pyreRefresh] | All three original effects have positive witnesses: six real draw8 → draw7 owner ablations, one acquisition +20 HP/max and six actual Pyre combat attack witnesses for its table-index-0 +30 attack. Thirteen combined witnesses. |
| `BossIncreaseTurnDrawDH` | `401f30f6-6206-4c1c-b014-a8047c01597e` | [Pyre refresh relics][pyreRefresh] | All three original effects have positive witnesses: six real draw8 → draw7 owner ablations, acquisition +20 HP/max and six Pyre combat witnesses for table-index-1 +55 attack. Thirteen combined witnesses retain native upgrade/status history. |
| `BossIncreaseTurnDraw2` | `341001b3-3fa9-47a5-9d88-eb04390b82fa` | [Pyre refresh relics][pyreRefresh] | All three original effects have positive witnesses: six real +1 draw applications, acquisition +20 HP/max and six actual combat witnesses for table-index-1 +55 attack. Thirteen combined witnesses; catalog Int10/30 are not substituted for initialized lookup amounts. |
| `PurgeChampionReward` | `be25af24-9516-4ed2-9cf1-c756a381a928` | [floor capacity relics][floorCapacity] | All four original effects have positive witnesses: +1 ordinary/deployment Ember, +1 draw and +2 floor capacity. Six real energy callbacks, four draw6 applications and a capacity-15 native room; removing only this relic reduces a new room to 13. |
| `BossCapacity` | `9291142a-dc6f-4834-89f1-ceda778e06ec` | [floor capacity relics][floorCapacity] | All three original effects have positive witnesses: +2 capacity and its initialized Pyre health/attack table entry. Removing only this relic reduces a new room from 15 to 13. |
| `BossCapacity2` | `2d313e11-aa80-4116-b359-7560d3307544` | [floor capacity relics][floorCapacity] | All three original effects have positive witnesses: +2 capacity and its initialized Pyre health/attack table entry. Removing only this relic reduces a new room from 15 to 13. |
| `BossCapacityDH` | `f7b0c251-7f96-4115-897a-d5b4a40a36ef` | [floor capacity relics][floorCapacity] | All three original effects have positive witnesses: +2 capacity and its initialized Pyre health/attack table entry. Removing only this relic reduces a new room from 15 to 13. |
| `SilverIncreasedCapacity` | `aab4971f-05c6-430a-8a6a-dfb654a41b0c` | [floor capacity relics][floorCapacity] | Its original +2 capacity effect has positive native query and owner-removal witnesses; the full room fits sizes 6+6 and rejects the next size-2 summon. |
| `MoreRares_Relic` | `374995fd-7619-4244-9d1f-269429258584` | [Endless return][endless] | Its unchanged Monsters-team `AddEndlessToNonMorsels` upgrade gives the carrier Endless; native death sends the spawner to Draw, preserves the temporary upgrade and does not increment `TimesExhausted`. |
| `MagicPowerWhenCardConsumed` | `a4c87eb7-1c4e-4f63-af08-79f910e5a2e8` | [original card-play magic power][cardPlayMagicPower] | Unchanged power-1 effect requires `CardTraitExhaustState` and has no conditions. Two matching carrier plays produce 110 native-matched spell offset updates; the authored test carrier satisfies the original trait filter. |
| `RootBeforePyre` | `4778bb46-27e4-49ae-aa5d-750813ceb6d0` | [room-entry status relics][roomEntry] | Its unchanged room-2 Heroes effect adds 1 Rooted. Two distinct native Heroes have the exact status after entry. |
| `IncreaseMagicPowerOnSpellsPlayed` | `76166da7-d9aa-4c68-9a78-64914ec49f81` | [original spell-play thresholds][spellThresholds] | Five third-spell activations, including deployment, produce 65 exact damage/heal offset increments. Once-per-turn conditions reset correctly; passive status/birth condition queries retain the native card-statistic cache semantics. |
| `DrawCardWhenConsumePlayed` | `4fbb0cca-26cd-4510-af02-66694c7edfd3` | [original spell-play thresholds][spellThresholds] | The original Spell and Exhaust trait filters yield one actual deployment draw, with 15 non-consuming controls. Its tracked condition limits activation to once per turn; trait requirements survive condition recording. |
| `ShroomsGainSapDecay` | `3accd42e-3f82-4bf1-9026-29a624d3dde5` | [original Shroom Decay][shroomDecay] | Fourteen exact births include seven enemy and four non-Shroom controls; three Shroom births install the unchanged summon trigger. Five enemy status witnesses disappear on owner ablation; three combat stages deal actual Decay damage. Full native state and independent targeted-policy replay match. |
| `Inkbound_Clepsydranium` | `b0972732-ea21-42d0-844d-05fb5ff028e6` | [original Conduit relics][conduit] | Original player OnKill adds Conduit 1; nine positive stage witnesses disappear under removal of this owner and its installed triggers. Full room power and temporary hand upgrades match. |
| `EquipmentBoon_DH` | `394a559b-0c45-4bac-880a-522b04f1780c` | [original Conduit relics][conduit] | Original OnEquipmentAdded adds Conduit 4 with IgnoreDualism; two real equipment attachments have positive removed-owner controls. No original owner definition is modified. |
| `AddConduitOnSpawn` | `357c7265-74d0-4567-b793-d2fbac464d82` | [original Conduit relics][conduit] | Original tracked once-per-turn condition gives Conduit 4 on four first summons; three later player births and seven enemy births are controls. Fourteen full birth transitions and 12 Conduit-upgraded card plays match. |
| `UpgradeEquipmentOnDeath` | `b3e7888f-8f5e-4082-89b0-078d80ce4a9b` | [original equipment growth][equipmentGrowth] | Original equipped-only player OnKill adds +2 attack permanently to equipment and to its host. Seven positive stage witnesses disappear under owner/installed-trigger removal. |
| `AdvancedGrowth` | `449f6464-8164-48ad-bf66-6107b10f7f5c` | [original equipment growth][equipmentGrowth] | Original both-team OnDeath grants equipment +4 attack. An actual equipped sacrifice and returned/replayed gear prove the persistent change; seven enemy births receive this trigger while rejecting the other two owners. Its unused effect status parameter is not applied. Campaign run-stat counters are outside this battle claim. |
| `FriendlyUnitsGainDeathwishValor` | `19f0d8e3-a3f8-4cac-b73d-46dc189a70ac` | [original equipment growth][equipmentGrowth] | Original front-position Deathwish installs/removes unique ValorOnTurnStart. Two actual entries, four losses and nine subsequent PreCombat Valor-2 applications match; removing owner/installed triggers eliminates all nine status witnesses. Birth registration preserves exclusive trigger-instance queue identity. |
| `HairOfTheHybrid` | `ac208c49-610a-43e7-80cc-e9680cf84fcd` | [original health choice][healthChoice] | Friendly-only OnEquipmentAdded applies armor 10 below ceil(50% max HP), otherwise Rage 5. Four exact native callbacks include natural 7/13, 6/13 and healed 13/13 boundaries, positive owner/trigger ablations and unchanged Battle RNG. Seven friendly/seven enemy births, full policy replay and inverted/preview/retained controls pass. |
| `AddImpToHand` | `91211832-747e-4995-b4cb-7ca23c000f10` | [original after-draw generation][afterDrawGeneration] | Four natural post-draw additions from the unchanged filtered original Imp pool. TestEffect selection, exact Battle RNG, fresh identities, hand insertion and complete context match. |
| `ChanceToVineGrasp` | `00063533-9277-485c-b100-13637a228f9a` | [original after-draw generation][afterDrawGeneration] | Four actual generated cards receive unchanged AddEphemeral as a temporary upgrade. Original name does not imply a probabilistic activation gate; its actual conditions are empty. |
| `AddSnackToHand` | `81958958-6481-4847-afe1-cb651a041f6c` | [original after-draw generation][afterDrawGeneration] | Four actual post-draw hand additions, exact native selection/state and original filtered pool. Deployment and preview gates have pure controls. |
| `AddSnackOnKill` | `b6548209-3ccf-4deb-9896-f07d5df3dab9` | [AddSnackOnKill][addSnackOnKill] | Three exact native enemy-Hero OnDeath callbacks add six cards from the original weighted pool; 312 callbacks are gated. Synthetic full-hand control verifies two RNG selections and one manager notification without card allocation. Generated-card play effects and native full-hand deaths are not claimed. |
| `FinalEndingArtifact` | `6bb39014-9b03-4620-b088-f618a7e680b7` | [FinalEndingArtifact][finalEndingArtifact] | Four natural post-draw champions from the unchanged 36-entry pool match exact Fisher-Yates Battle RNG selection and grant upgrades; two native gate callbacks consume no RNG. Full-hand/champion-hand and filtered-pool controls pass. The deck's champion is absent from this pool; eight externally interactive grant upgrades are explicitly unsupported, and generated-card play is outside the policy boundary. |
| `AddStingToHand` | `5e5d863b-514c-4026-a9b0-ffe56af2a3eb` | [original after-draw generation][afterDrawGeneration] | Three actual hand additions plus one native full-hand rejection. Selection still consumes Battle RNG before the failed insertion; rejection allocates no card identity. All 16 combined consumers and full policy replay match. |
| `SpellWeaknessBelowPyre` | `5a96d89f-d7c0-49fa-88d1-8ec75a8f3140` | [room-entry status relics][roomEntry] | Its unchanged room-2 Heroes effect adds 2 Spell Weakness. Three distinct native Heroes have the exact status; spell damage and post-card removal match through the full battle. |
| `PyregelOnEnterTopFloor` | `e4be072c-e389-4ae7-acc6-0c8aa4cf0078` | [room-entry status relics][roomEntry] | Its unchanged room-2 Heroes effect adds 10 Pyregel. Three distinct native Heroes have the exact status after entry. |
| `MoonPhaseModifyEmber` | `70096a91-220a-4ab3-b8d0-b8fe8f3a819c` | [original moon-phase energy relic][moonPhase] | Three Full callbacks add 1 Ember and notify; three New callbacks change nothing. Original once-per-turn condition and exact hidden moon markers match. |
| `MooncycleRestoreFriendlyHealth` | `6d977136-5aac-44fd-9843-54cf2a0bd01d` | [original moon-phase healing relic][moonHeal] | Seven unchanged-owner callbacks across Full and New phases select only the weakest damaged friendly across rooms, including two multi-floor candidate sets; healing is capped at max HP and the no-target control is unchanged. |
| `WhelpDragonAttack_DH` | `72706202-526e-4377-9745-7df40a26ba7d` | [original card-attack relic][cardAttack] | Two unchanged +8 effects match Whelp and Dragon spawn subtypes in order. Four native manager observations cover both subtype outcomes and reset/reapply; dynamic scaling and cardless callbacks remain separately bounded. |
| `CardDrawOnCardPlay` | `f3f07b9b-1349-41d0-abab-bd5ec04d81e4` | [original card-draw-on-play][cardDraw] | The unchanged one-card effect has `AnyCardPlayed >= 3`; it skips deployment and draws on the third play in five active turns. All five native activations and draws match. |
| `MoreOrLessArtifactsRelic` | `79122f7a-6895-443a-830c-2db23027776c` | [relic-count attack scaling][relicCountAttack] | Both unchanged ordered effects match 64 actual manager calls. The negative attack scales by currently held collected relics while excluding boss, Soul Savior and active-clan starters; monster/champion/spell, foreign/active starter, boss, remove, reset/repeat and unknown-new-owner controls pass. Positive effect ablation and 16 immutable parallel branches pass. |
| `MoreSpikes` | `a6c957cc-e9f5-49b7-a6a3-cd7e95393289` | [dynamic status attack][moreSpikes] | Its unchanged team/status/multiplier query adds one attack per friendly Spike. An authored 4-Spike monster carries 26 native attack through 15 exact plays; 27 positive owner observations and 21 enemy controls match. Status-scaled attack remains separate from base/status buffs and applies to combat attacks. |
| `UnitsGainStealthOnSlayPlusAttack` | `4687feca-a053-48ed-b208-81dc6dfc6e72` | [stealth on slay][stealthSlay] | Its unchanged +3-per-Stealth query and ordered OnKill/OnSpawn triggers match. Five friendly births gain one Stealth; five native OnKill ablations add another stack. Fifteen exact plays, five EndTurns and 16 parallel policy branches match, with enemy controls and zero differential errors. |
| `DrawOnSnackPlayed` | `8b00c46d-374e-4afb-aa27-526873e242d1` | [original card-draw-on-play][cardDraw] | Its unchanged Monster / Snack filter and tracked `ThisTurn` condition draw once on deployment. A synthetic Train Steward carrier with the Snack subtype provides the native positive witness without invoking its unrelated `OnEaten` behavior. |
| `EmberFoundryCrucible` | `a034e00f-3a12-4c19-b626-4b8fd17c412a` | [normal smelt][smelt], [zero-cost smelt][smeltZero], [bulk smelt][smeltBulk] | Original +1 Ember per consumed card: cost-2 Starter Scrap choice changes Ember 4→6 after paying 1; zero-cost Sting changes 4→4; Foundry Line consumes four cards for 3 Forge and Ember 4→10. All three complete battles and action/search replays match. |
| `TinkermasterTools` | `db2193eb-a476-4cc4-9a26-2d0e35f46b74` | [original Tinkermaster Mix][tinkermasterMix] | Its unchanged +1 amount changes both original Mixology drafts from two candidates to three. Two Mixology casts in separate turns, four native draft screens and their full action/search replays match. |
| `ProgressPacemaker` | `4446fd0c-f7c3-4a9d-96c3-bd12e4426a76` | [original Progress Mix][progressMix] | Its unchanged once-per-turn condition adds one follow-up Mix with two candidates. Both original Mixology casts yield three ordered drafts; the condition triggers after each play and resets before the next turn. Six native draft screens and full action/search replays match. |
| `CheaperBlightScourge` | `a19cac7c-b2e0-42bd-bc1e-d180e17a1c15` | [original card-type cost][cardTypeCost] | Its unchanged ordered Blight/Junk -99 effects match ten native modifier calls, including repeated application, reset, a nonmatching card and enemy-generated Junk. Original Blight and Junk play for zero Ember and self-purge; full battle and search replay match. |
| `ConserveEmber` | `3e200d1f-7ba9-4a6e-a09d-5625f25b01ac` | [original Ember conservation][conserveEmber] | Its unchanged all-energy effect conserves positive balances on all seven EndTurns. Initial banking produces 4 → 7 → 10 Ember, followed by 21 paid plays and a winning terminal balance of 1 Ember with Pyre health 58. Seven exact native queries, complete policy replay and bounded serial/parallel search match. |
| `CarriageJack` | `19e15240-37ca-460d-8230-693626fc7f7e` | [card-play room capacity][carriageJack] | Its unchanged +1 effect matches only `TrainRoomAttachment` plays and adjusts the selected player floor after the native attachment's +4. A non-attachment control leaves capacity unchanged. Unit-upgrade propagation from the attached room modifier is outside this fixture. |
| `FirstRoomCardOnAllFloors` | `e55235f7-1d84-43f0-b8f5-037b64b8f582` | [first-room attachment replication][firstRoomAttachment] | The unchanged once-per-battle condition copies the first native `IncreaseRoomCapacity` attachment to every non-Pyre floor. An ordinary-card control is inert; a second attachment increments only its source-room counter and does not replicate. Full native state replay includes live and later unit upgrade propagation. |
| `TrainRoomInstallFloor1AtStartOfCombat` | `2c97e1b3-d4bb-4133-9572-e582805c106f` | [start-room attachment][startRoomAttachment] | Its unchanged cardless Primary FuelCar attachment targets both teams on floor one. Six Heroes and two Monsters carry exactly one OnDeath trigger attributed to the native attachment GUID; source-room removal on Ascend matches live snapshots. |
| `TrainRoomInstallFloor2AtStartOfCombat` | `b037ec04-6164-4cd8-8b9d-3bc420c29351` | [start-room SpellSanctum attachment][startRoomSpellSanctum] | Its unchanged cardless SpellSanctum attachment targets both teams on room index 1. Native cost reads show the same 1-cost hand Spell costs 0 there; eight impacted-unit and 31 room-state witnesses match, and the unit upgrade is removed after leaving the room. No X-cost Spell appeared in the captured hand, so the PlayedCost branch is not battle-witnessed. |
| `TrainRoomInstallFloor3AtStartOfCombat` | `c474dd00-3725-46d6-b074-0be7f7687cdd` | [start-room FightClub attachment][startRoomFightClub] | Its unchanged cardless FightClub attachment targets Monsters on room index 2. The native +15 attack upgrade is absent before units enter; one impacted-unit and 32 room-state witnesses match, and room-exit removal is covered by focused model lifecycle checks. |
| `PartFactory` | `623ce968-aae5-4b07-b0c8-071f315031b7` | [PartFactory][partFactory] | The unchanged start-of-battle owner adds the native SetupRun-selected Pufferflesh to hand. Both weighted entries for that DataId match complete card context; the card later moves to Draw and is never played. Pool selection is a captured external result, not modeled SetupRun RNG. Grafted-equipment unit state and deploying/playing this card remain unsupported. |
| `DamageOnFloorTransition` | `269a6f43-9982-4683-ac39-1a862e481898` | [DamageOnFloorTransition][damageOnFloorTransition] | The unchanged owner applies exactly 3 damage after an enemy's native PostAscension and PostDescension callbacks; rooted, immobile and player movement are controls. All 22 native Bump states and 42 full-battle actions match, and independent search passes 74/74 states with no unsupported attempts. |
| `AddStatsToSoloUnits` | `724a1c19-192f-48c1-8099-8ae3ecb9c1e5` | [AddStatsToSoloUnits][soloUnitUpgrade] | Each start-of-turn callback gives the sole player Monster +5 Damage/+5 Health. Two repeated applications remain after a second Monster enters that floor; a two-Monster floor and two empty floors are controls. The owner is unconditional, non-removable, and captured on four upgradable floors only. |
| `PurgeToExhaust` | `10648aee-8577-49b6-8659-318a7f922d5a` | [PurgeToExhaust][purgeToExhaust] | The unchanged ordered Spell/Equipment filters replace SelfPurge with Exhaust. Six native source/filter controls and 24 repeated manager calls match; a converted Spell is actually played into Exhausted with native private trait state. Full battle and independent 128-state search pass. |
| `PyregelOnAscend_DH` | `394fb724-046f-448f-9531-f9d6caadae77` | [enemy ascend Pyregel][pyregelOnAscend] | The unchanged owner adds 2 Pyregel after enemy PostAscension; descend, player, rooted and immobile controls do not gain it. All 22 Bump replays, 40 full-battle stages and 42 actions match; independent search passes 74/74 states with serial/parallel best equality. |
| `BalatroEventYorick` | `2bd14ebc-1589-4652-a002-54013f0eace4` | [schema-194 Balatro battle](full-battle-original-balatro-event-yorick.mt2f) | Its unchanged exhaust look-ahead matches 28 native Test calls and six natural Consume callbacks, including full train state and adjusted reward gold; non-Consume, threshold, preview and multiplier controls pass. Independent replay/search match. |
| `BalatroEventCavendish` | `24c4ad26-f2b3-4a95-bddc-caab94bd4767` | [schema-204 Balatro battle](full-battle-original-balatro-event-cavendish.mt2f) | Its unchanged ordered attack, 1% end-combat destruction and banana-shake effects pass ordinary/champion/spell and cardless controls, deterministic miss/hit undo, and independent search. |
| `2ChargesOnCardConsumed` | `217d553b-cddf-4a8c-a4c9-cfdb3c99389c` | [2Charges battle](full-battle-original-two-charges-on-card-consumed.mt2f) | Unchanged +1 corruption owner: an Exhaust carrier changes only its selected floor; a non-Exhaust play and all other floors remain unchanged. Five card actions replay; callback-bearing floors and above-maximum behavior are outside this capture. |
| `ChanceForSpellWeakness` | `98989cbb-c729-4f87-8240-08f448db96d8` | [ChanceForSpellWeakness battle](full-battle-original-chance-for-spell-weakness.mt2f) | Unchanged 50% AfterSpawnEnchant effect adds one Spell Weakness to eligible enemy Heroes: seven exact rolls (four hits/three misses), four applications, and 496 gated tests with unchanged Battle RNG. Other owner identities and nested payload shapes remain unsupported. |

## Campaign interface evidence

These sixteen unchanged original owners are validated by their actual native
campaign consumers, with baseline and removed-owner controls. This table is
separate from the 163 battle owners. It does not claim whole-battle integration
for these effect classes. The first capture's recorded relic IDs, gold, hoard cap and
amount, deck IDs, frame and RNG match before/after; reward grants still execute
their native run-stat side effects in the isolated save. Catalog enumeration
and inert presence do not count. Evidence: [campaign calibration][campaign].
The [draft calibration][campaignDrafts] records exact original candidate IDs and
Rewards RNG from actual GrantReward selection, using an authored ordered pool
of unlocked original cards and flattened Common..Rare weights. UI choice,
chosen-card grant and run rarity tickets are outside this bounded consumer.
Recorded relic/deck IDs, raw/visible/max Pyre health, frame and deterministic RNG
are restored; this does not claim restoration of every isolated-save run statistic.
The [post-battle calibration][postBattleRewards] validates collection of fixed
reward offers, including prefix duplicates and reversed owner order. Accepting
or granting rewards, UI choice and pool-based rewards are outside this claim.
The [battle-resource calibration][battleResources] exercises actual end-battle
gold/hoard consumers and controlled native character-trigger callbacks. It does
not claim natural incoming-damage integration into battle branches.

| Original owner | Data ID | Native evidence | Accepted path |
| --- | --- | --- | --- |
| `ExtraSpellUpgrades` | `60a7f580-0788-42f2-8896-cb8d22e849a5` | [campaign calibration][campaign] | +1 Spell upgrade slot through `BalanceData.GetUpgradeSlots`, type controls, lower/upper clamps and spell-only/both/removed phases. |
| `ExtraUnitUpgrades` | `3203c2d3-58f3-4f38-888e-886d8faae758` | [campaign calibration][campaign] | +1 Monster slot with original None subtype; unit-only/both/removed phases. The combined 80-query matrix includes Spell, Monster, Equipment and Blight with four card modifiers. |
| `CheaperMerchants` | `ffcb6931-e45e-4e27-bacf-4c649779c2be` | [campaign calibration][campaign] | Original -0.25 additional cost multiplier; 108 actual `MerchantGoodState.GetCost` queries cover purchase indices, rounding, merchant discounts, Purge discount exclusion, baseline and removal. |
| `ApplyUnitUpgradeWhenRich` | `07bdefdb-03b8-4159-b74b-be85f68c4c56` | [campaign calibration][campaign] | Actual effect is +50% trial gold, despite asset name. All 66 `GoldRewardData.RewardValue` displays and `GrantReward` balance deltas match optional trial gating, minimum/rounding, nonpositive rewards, baseline and removal. |
| `ModifyDragonsHoardCap` | `00a8f443-cdea-437a-98bf-0ebe0ead00b0` | [campaign calibration][campaign] | Original +3 on actual acquisition; removal subtracts 3 and truncates amount to cap. Starting caps 0/max-1/max exercise both clamps and capped-acquisition loss on removal. |
| `ModifyDragonsHoardCap_DH` | `c3f91e0e-629f-4d3f-b422-ce1653147293` | [campaign calibration][campaign] | Original +2 acquisition/removal through SaveManager, including amount truncation and upper cap; three independent starting-cap cases. |
| `AlliedClassRelic` | `656861c9-a96d-4c99-988c-390b75e5d8d4` | [draft calibration][campaignDrafts] | Replaces the main class flag with the allied class; 48 positive changes, all seven class masks and explicit-class bypass controls. Native neutral fallback remains available. |
| `FutureDraftsAreRares` | `8f46fde9-0c75-49ba-9f58-3063864f81bf` | [draft calibration][campaignDrafts] | Sets both minimum rarity and exact Rare filter; 56 positive changes, three authored rarity floors and ignore-rarity bypass controls. All 840 combined selection results and Rewards RNG match. |
| `UpgradedDrafts` | `9e0e5d4e-6d16-43f1-8cd4-cc4c2b431afd` | [draft calibration][campaignDrafts] | Unchanged spell draft owner over an ordered 50-entry EnhancerPool; 85 exact native filter outcomes and three selected positions match full card application and Rewards RNG. Replacement/external interactions, other targets, preview and later UI grant are outside this boundary. |
| `PyreHeartFather` | `fe768ab5-3f58-4f96-9afc-48f0f30c7b21` | [draft calibration][campaignDrafts] | Actual acquisition adds 25 maximum and current health at low/near-full/full HP. Removal drops dynamic maximum, preserves raw HP and clamps the visible getter. |
| `VoidPortal` | `5cad6ad0-769b-46d3-9464-514b8e74190a` | [post-battle calibration][postBattleRewards] | Appends the original PurgeReward object through ApplyPostBattleRelicRewards; existing offers and duplicates remain intact. Int=8 and Int2=2 are unused by this consumer. |
| `MothersRemnant2Relic` | `202a7153-bfe9-4da9-be3e-ddcf8c195354` | [post-battle calibration][postBattleRewards] | Appends the original CardDuplicatorReward enhancer offer. Combined and reversed owner order, repeated calls and removed-owner controls match all 108 native calls with unchanged RNG. |
| `Inkbound_KwillingCatcher` | `28772b0a-6f5c-44a7-98dd-260a851dd391` | [battle-resource calibration][battleResources] | Actual end-battle +100 reward gold, unaffected by Pyre damage: 378 increases, repeated calls and removed-owner/order controls. |
| `ModifyDragonsHoardBattleEnd` | `26c9bc93-f5a6-439d-8239-14c22cbeb02e` | [battle-resource calibration][battleResources] | Actual +3 hoard capped at capacity, 144 increases, 54 positive-Pyre-OnHit damage gates and 24 recoveries after native start-battle reset. Zero/negative/wrong-trigger/null-character controls do not mark damage. |
| `BalatroEventCanio` | `d8d1e069-938e-4141-b6ac-e20f4578163a` | [purge gold calibration][purgeGold] | Actual campaign purge callback grants +75 adjusted reward gold and one notification for each of six card types. All 144 manager calls match, including 24 positive rewards, combat no-op and removed-owner controls, completion and unchanged RNG. Deck removal/payment UI is outside this interface claim. |
| `MothersRemnant1Relic` | `8ee399d2-4ee4-409d-af95-319bcd6accc4` | [permanent copy calibration][deckCopy] | Seventy-two actual end-battle manager calls prove 16 fresh permanent copies with original champion-excluding mask, ordered candidate selection and Rewards RNG. Four modified/purified/event-cached witnesses retain permanent offsets/upgrades and drop temporary upgrades. Native cached-duplicate setter updates the cached flag; its duplicate getter remains false. Presentation bookkeeping is suppressed after deck mutation. |

## Native no-op evidence

This category proves unchanged definitions without a gameplay effect interface;
it does not invent an activation or remove ownership from other relics' counts.

| Original owner | Data ID | Native evidence | Accepted path |
| --- | --- | --- | --- |
| `StarterWurmRelic` | `815a25e7-2fed-4cb2-a568-79af12cd7116` | [native no-op equivalence][nullRelic] | Only RelicEffectNull with base/editor interfaces. Actual acquisition/removal/reacquisition preserves recorded deck/resources/frame/RNG. Two same-seed native winning policies match all 21 actions/seven EndTurns and complete decision contexts except the retained owner. Both independently replay, including sixteen parallel branches. |

## Recorded battle sizes

Record indices in the battle table are zero-based. Full-battle acceptance and independent
effect checks are documented in [README.md](README.md); the source
archives contain these observed battle sizes:

| Archive | Schema | Card plays | EndTurns | Matched stages | Independent relic observations |
| --- | ---: | ---: | ---: | ---: | --- |
| [combined trigger relics][counts] | 108 | 21 | 7 | 62 | 160 trigger-count queries |
| [spawn status relics][statuses] | 109 | 18 | 6 | 52 | 12 birth phases |
| [starter cost relic][cost] | 105 | 18 | 5 | 42 | 124 modifier calls |
| [cardless relic upgrades][cardless] | 111 | 1 | 5 | 42 | 22 birth phases |
| [original relic batch][batch] | 106 | 21 | 5 | 46 | 30 modifier calls, 12 birth phases |
| [original unit relic batch][units] | 103 | 40 | 7 | 55 | 40 modifier calls, 28 Shift queries, 6 original ability activations |
| [stationary trigger relic deaths][triggerDeaths] | 103 | 13 | 5 | 35 | 14 births, eight required original effect witnesses |
| [original trigger relics][triggers] | 106 | 28 | 7 | 54 | 14 births, three attachments including one enemy, ten Bump plays, eight required effect witnesses |
| [original status relics][statusAdders] | 113 | 26 | 5 | 39 | 29 distinct real queries, 240 boundary queries, five positive original effects |
| [original burnout stacks][burnoutStacks] | 142 | 26 | 5 | 41 | 35 actual / 336 boundary status queries; one positive IncreaseBurnoutStacks owner witness |
| [original magnitude relics][magnitudes] | 114 | 24 | 5 | 38 | Refreshed native capture: 20 matrix, seven live and four preview getter cases; three positive original effects |
| [original lifecycle relics][lifecycles] | 115 | 24 | 5 | 35 | 14 exact birth/train/queue phases, six ability activations, one natural revival, 17 callback phases; five positive original effects |
| [original decay relics][decays] | 116 | 21 | 5 | 42 | 16 matrix, 11 live and ten preview decay queries; 239 stack queries; two positive original effects |
| [original resource relics][resources] | 117 | 42 | 7 | 50 | Eight real acquisitions, 48 native energy bodies, six empty-hand draw8 applications and six positive Pyre combat stages; all 20 original effects |
| [Pyre refresh relics][pyreRefresh] | 118 | 1 | 7 | 50 | Three real acquisitions, six actual draw8 applications, Gold0/+5/-5 and two nested refreshes with three notifications each, six positive Pyre combat stages; all nine original effects |
| [floor capacity relics][floorCapacity] | 119 | 26 | 5 | 51 | Five real acquisitions, all 14 original effects, 21 capacity queries, four room initializations, capacity 15 and legal 6/6/2/1 native deployment |
| [Endless return][endless] | 126 | 23 | 7 | 62 | Original Endless upgrade survives one actual carrier death; returned to Draw without Exhausted or a `TimesExhausted` increment |
| [original card-play magic power][cardPlayMagicPower] | 128 | 11 | 7 | 61 | Two matching carrier plays and 110 spell offset updates for unchanged `MagicPowerWhenCardConsumed` |
| [room-entry status relics][roomEntry] | 132 | 21 | 7 | 55 | Three unchanged originals, 13 train phases and actual room-2 Heroes status witnesses |
| [original moon-phase energy relic][moonPhase] | 146 | 24 | 7 | 57 | Three Full +1/notification callbacks and three New no-op callbacks with exact unit markers |
| [original card-attack relic][cardAttack] | 147 | 21 | 7 | 60 | Four native manager observations; Whelp/Dragon subtype dispatch, +8 offset and reset/reapply |
| [original card-draw-on-play][cardDraw] | 160 | 20 | 6 | 52 | Five threshold draws plus one deployment-turn Snack activation/draw |
| [first-room attachment replication][firstRoomAttachment] | 167 | 7 | 7 | 60 | Three controlled plays: ordinary negative, first attachment copied to all non-Pyre rooms, then once-per-battle gate control |
| [start-room attachment][startRoomAttachment] | 168 | 23 | 7 | 60 | Both-team cardless FuelCar attachment, eight GUID-attributed OnDeath witnesses and source-room removal on Ascend |
| [start-room SpellSanctum attachment][startRoomSpellSanctum] | 186 | 27 | 7 | 57 | Both-team cardless attachment, native 1-to-0 Spell cost witness and room-exit upgrade removal |
| [start-room FightClub attachment][startRoomFightClub] | 187 | 21 | 7 | 60 | Monsters-only cardless +15 attack attachment on room 2; one impacted-unit and 32 room-state witnesses |
| [Monster Spell targeting][monsterSpellTargeting] | 188 | 13 | 7 | 62 | One copied 1-Damage DropTarget effect; original-target, Untouchable and team controls |
| [normal smelt][smelt] | 103 | 22 | 7 | 55 | Original +1 Ember on a cost-2 smelt |
| [zero-cost smelt][smeltZero] | 103 | 21 | 7 | 56 | Original +1 Ember with zero base smelt cost |
| [bulk smelt][smeltBulk] | 103 | 1 | 7 | 52 | Four consumed cards, 3 Forge and four extra Ember |
| [original Tinkermaster Mix][tinkermasterMix] | 103 | 21 | 7 | 62 | Four native Mix drafts with three candidates each across two Mixology casts |
| [original Progress Mix][progressMix] | 103 | 21 | 7 | 60 | Six native Mix drafts, two conditional follow-up rounds across separate turns |
| [original card-type cost][cardTypeCost] | 103 | 24 | 6 | 50 | Ten modifier calls, two original zero-Ember self-purge plays, generated Junk |
| [original Ember conservation][conserveEmber] | 103 | 21 | 7 | 62 | Seven conservation queries, two banked turns, later positive payments and terminal Ember retention |
| [PurgeToExhaust][purgeToExhaust] | 184 | 21 | 7 | 61 | 24 exact manager calls, six source/filter controls and one played converted Spell |
| [enemy ascend Pyregel][pyregelOnAscend] | 185 | 42 | 5 | 40 | 22 Bump records, exact enemy PostAscension +2 Pyregel and descend/player/rooted/immobile controls |
| [Big Damage from Many Morsels][bigDamage] | 169 | 6 | 7 | 113 | Six native train-birth replays, exact 19/20/21 Snack thresholds and Tower/Pyre/enemy controls |
| [original early-discard energy][earlyDiscardEnergy] | 170 | 11 | 6 | 45 | Five Ember-gaining card discards, 18 no-gain consume controls |
| [cheaper spell cost][cheaperSpellCost] | 171 | 2 | 7 | 52 | Two ordered spell plays; played-card and end-turn discard cleanup witnesses |
| [NefariousOverflow][nefariousOverflow] | 172 | 2 | 7 | 59 | One unchanged relic payload; five-stack Unstable death adds 5 explosion damage, killing the Mage and reducing Guard Armor 17→7 |
| [CopyFirstSpellPlayed][copyFirstSpell] | 173 | 2 | 6 | 52 | First direct Spell receives an Ephemeral hand copy; a second same-turn direct play leaves the one-shot condition unchanged |
| [Funguy deck scaling][funguyDeckScaling] | 174 | 0 | 7 | 67 | Six callbacks at 40 visible cards apply twice per live room; three cardless Funguys spawn with six total Horde stacks |
| [BrokenRail forge upgrade][brokenRail] | 176 | 16 | 5 | 39 | Original +2 HP forge payload scales to +6 at three points; native full-battle and bounded search match |
| [Ephemeral-loss Rage][rageBurnout] | 177 | 18 | 5 | 41 | Original +3 Rage payload on one controlled friendly Ephemeral removal; all recorded actions and turns replay |
| [2ChargesOnCardConsumed](full-battle-original-two-charges-on-card-consumed.mt2f) | 195 | 5 | 7 | 61 | Exact +1 corruption on selected floor, with non-Exhaust and cross-floor controls |
| [ChanceForSpellWeakness](full-battle-original-chance-for-spell-weakness.mt2f) | 196 | 5 | 7 | 61 | Seven eligible 50% rolls (four hits/three misses), four applications and 496 gated tests |
| [SummonWithAvarice](full-battle-original-summon-with-avarice.mt2f) | 197 | 5 | 7 | 61 | Five eligible player births gain two Avarice; ten combat stages isolate positive native gold |
| [ChanceToCleanse](full-battle-original-chance-to-cleanse.mt2f) | 198 | 5 | 7 | 63 | Seven eligible 50% rolls (four hits/three misses), four applications and 510 gated tests |
| [ExtraFeedTrigger](full-battle-original-extra-feed-trigger.mt2f) | 199 | 5 | 7 | 61 | Original Monsters OnFeed +1; live Pyre native query 2 vs OnDeath control 1 |
| [HephsBlueprints](full-battle-original-hephs-blueprints.mt2f) | 200 | 17 | 5 | 39 | Equipment-only +2 damage/+2 Armor forge upgrade scales to +6/+6 at three Forge; Monster filter control |
| [ModifyDragonsHoardBattleStart][startCombatHoard] | 201 | 21 | 7 | 60 | Deployment +1 Hoard, one card and monster resource refresh each |
| [BalatroEventCavendish](full-battle-original-balatro-event-cavendish.mt2f) | 204 | 25 | 6 | 48 | Ordinary Monster +2x base attack; cardless positive/Champion exclusion; deterministic 1% miss/hit and exact deck undo |

The checks that enforce these observations are
[TriggerCountChecks.Native](../../src/ModelChecks/TriggerCountChecks.cs),
[RelicSpawnStatusChecks.Native](../../src/ModelChecks/RelicSpawnStatusChecks.cs),
[RelicCardModifierChecks.Native](../../src/ModelChecks/RelicCardModifierChecks.cs),
[OriginalCardTypeCostChecks.Native](../../src/ModelChecks/OriginalCardTypeCostChecks.cs),
[OriginalPurgeToExhaustChecks.Native](../../src/ModelChecks/OriginalPurgeToExhaustChecks.cs),
[ConserveEmberChecks.Native](../../src/ModelChecks/ConserveEmberChecks.cs),
[OriginalRelicBatchChecks.Native](../../src/ModelChecks/OriginalRelicBatchChecks.cs),
[OriginalUnitRelicBatchChecks.Native](../../src/ModelChecks/OriginalUnitRelicBatchChecks.cs),
[OriginalTriggerRelicBatchChecks.Native/Combined](../../src/ModelChecks/OriginalTriggerRelicBatchChecks.cs),
[OriginalStatusRelicBatchChecks.Native](../../src/ModelChecks/OriginalStatusRelicBatchChecks.cs),
[OriginalMagnitudeRelicBatchChecks.Native](../../src/ModelChecks/OriginalMagnitudeRelicBatchChecks.cs),
[OriginalLifecycleRelicBatchChecks.Native](../../src/ModelChecks/OriginalLifecycleRelicBatchChecks.cs),
[OriginalDecayRelicBatchChecks.Native](../../src/ModelChecks/OriginalDecayRelicBatchChecks.cs),
[OriginalResourceRelicBatchChecks.Native](../../src/ModelChecks/OriginalResourceRelicBatchChecks.cs),
[PyreResourceRefreshChecks.Native](../../src/ModelChecks/PyreResourceRefreshChecks.cs),
and [OriginalFloorCapacityRelicChecks.Native](../../src/ModelChecks/OriginalFloorCapacityRelicChecks.cs),
with [EndlessReturnChecks.Native](../../src/ModelChecks/EndlessReturnChecks.cs)
checking the original relic payload and its death-route witness, and
[RelicCardPlayMagicPowerChecks.Native](../../src/ModelChecks/RelicCardPlayMagicPowerChecks.cs)
checking the original trait filter and spell offset updates, and
[OriginalRoomEntryStatusRelicChecks.Native](../../src/ModelChecks/OriginalRoomEntryStatusRelicChecks.cs)
checking the three unchanged room-entry payloads and actual Heroes witnesses,
and [OriginalMoonPhaseEnergyRelicChecks.Native](../../src/ModelChecks/OriginalMoonPhaseEnergyRelicChecks.cs)
checking the unchanged moon-phase payload, both phase outcomes and unit markers
across all recorded decisions, and [RelicCardAttackChecks.Native](../../src/ModelChecks/RelicCardAttackChecks.cs)
checking the two unchanged attack payloads, ordered subtype matches and native
temporary offsets. Its pure checks also cover cardless births and reject dynamic
attack scaling, which is not included in this original-owner evidence.
The native `ExtraFeedTrigger` owner, registration cache, direct Pyre count
queries and shared count model are checked by
[OriginalExtraFeedTriggerChecks.Native](../../src/ModelChecks/OriginalExtraFeedTriggerChecks.cs).
The original BrokenRail payload and its three-point direct-monster forge
transition are checked by [OriginalBrokenRailChecks.Native](../../src/ModelChecks/OriginalBrokenRailChecks.cs).
The original status-removal callback and native status transition are checked by
[OriginalRageOnBurnoutStackLossChecks.Native](../../src/ModelChecks/OriginalRageOnBurnoutStackLossChecks.cs).
Each new trigger archive has eight positive owner effects; their union covers
all nine distinct originals. Removing only one original's installed callbacks
must change health, attack, membership or status stacks, beyond trigger metadata.
Each archive also compares full native states, all nine installation rules,
negative team/subtype gates, count admission and 32 immutable birth branches.
The cardless archive uses the clone-birth path in `RelicSpawnStatusChecks.Native`
to compare the complete train, callback queue and dispatched callbacks;
[RelicCardlessUpgradeChecks](../../src/ModelChecks/RelicCardlessUpgradeChecks.cs)
adds focused synthetic checks for its application gates.

The lifecycle archive requires a positive actual future effect for all five
original owners, with counts 24, 24, 16, 1 and 1 in table order. Each owner
counterfactual removes only its matching installed callbacks and context relic;
existing unit stats, previously applied upgrades, other callbacks, trigger-count
caches and shared ability-card caches are preserved. Full native action/combat
states, team/subtype rejections, installation order, complete birth callback
queues and dispatches match, with 32 immutable birth branches. Its independent
policy replay covers the complete battle, a mid-battle continuation and 16
parallel branches, ending with Pyre health 80.

The decay archive requires real friendly Rage preservation, enemy Rage decay,
both-team Frostbite preservation and actual card poison3/1 owner ablations.
Its 14 Rage witnesses and 25 combined Frostbite witnesses compare complete
native action/combat states, retaining captured starting accumulators. Live
getter evidence is required separately from the matrix. Full-battle and
mid-battle policy replay, 16 parallel policy branches and 32 immutable getter
branches pass, ending with Pyre health 77.

The resource archive requires positive actual effects for all 20 original
effects on its eight owners, with owner witness totals 6/7/12/7/1/14/14/14 in
table order. Fifty-six native resource operations contain eight normal
acquisitions before battle and 48 ordered energy callback bodies. Captured
Balance is base energy 3, deployment extra 1, base draw 5, maximum energy 99
and maximum hand size 10. Deployment energy is 4 → 8, then ordinary callbacks
produce 12; later ordinary turns produce 7. No native boundary hits the energy
cap. Six real empty-hand draws produce eight cards, with positive owner
ablations. Pyre upgrade indices 0/1/1 resolve attack amounts 30/55/55 and health
amounts 20/20/20 from the selected Pyre tables. Acquisition, initialized birth
and actual Pyre combat match. Getter checks preserve 32 immutable branches;
full-battle and mid-battle replay each preserve 16 independent chooser branches,
ending at Pyre 123. These are policy replay checks, not optimal search results.

The Pyre refresh archive adds three unchanged Boss draw owners with 13 positive
witnesses each, requiring every one of their nine effects to run. Three native
acquisitions establish HP/max increments and initialized table values. Six real
draw8 applications and six positive Pyre combat stages supply draw and attack
owner ablations. A single native spell applies +7/-10 temporary attack upgrades,
Buff11 and Rage2, then Gold0/+5, another -3 upgrade and Gold-5. Gold0 does not
refresh; each nonzero gold body restores the relic-derived raw base while
preserving upgrade/status history and sends three attack-effect notifications.
Full captured trains, private cache boundaries and final-model replays match.
The 61-second battle finishes at Pyre 123 with one action, seven EndTurns and
50 stages. Whole-battle/mid-battle policies each pass 16 parallel branches,
and 32 operation branches preserve their immutable parents and RNG.

No additional original relic is counted for the status, SelfPurge, unit-ability,
OnKill or conditional modifier fixtures. Their preparation in
[RelicCardModifierScenario](../../src/Probe/RelicCardModifierScenario.cs) replaces
`ReduceStarterCost`'s runtime upgrade or adds a condition. In particular,
`StingBuffPiercing` in the earlier piercing archive remains payload evidence,
not owner evidence; the dedicated archive above now validates both unchanged
Sting owners. `StewardSacrificeDamage_Upgrade` and `PyreHeartSavagery_UpgradeData`
remain original payload evidence only.
The new unit relic batch independently validates the unchanged Steward owner.
The ability-Incant fixture's
`PojuNativeAbilityIncantRelic` is authored and is also excluded. The always-present
`PyreHeartStarter_ArtifactData` with an empty effect list adds no effect witness.

The original batch was accepted in commit `63d4f07` with focused regression.
The preceding 210-archive milestone used a passing prefix and a fixed passing
suffix; it is not a fresh full regression on the final batch implementation.
The preceding magnitude batch passed its complete fixture checks, independent
full-battle/mid-battle/16-branch replay and three affected old battle fixtures.
At that milestone, all 217 archive names, sizes and hashes matched; a full
217-archive replay remained deferred. At the lifecycle milestone, that batch
passed strict native acceptance, complete-fixture checks and focused regression;
all 218 archives' integrity checks passed. Its 24 actions comprise 18 ordinary
plays and six unit ability activations. A full 218-archive replay remained
deferred at that milestone.
The current magnitude archive was recaptured after fixing a status-definition
validation dummy that lacked an attack cache and incorrectly rejected
`JustCause` (FloorRearrange plus Valor 2). The original generic policy now plays
that legal card; no historical card exclusion was added. The refreshed battle
has 24 actions, five EndTurns and 38 stages, with positive owner witnesses
19/10/12 and Pyre 80. It replaces the same archive name without increasing the
inventory. At the decay milestone, the new archive brought the inventory to
219; strict and complete-fixture checks passed for both captures. A full
219-archive replay remained deferred.
At the resource milestone, accepted inventory reached 220 and owner coverage
reached 47/224. Its native, pure, strict and complete-fixture checks passed, as did focused
EnergyEffects (49 stages), TypedDraw (35 stages) and NoCards (52 stages)
regressions. Card checks matched 32,515 native views across 845 decision contexts.
A full 220-archive replay remained deferred at that milestone. The following
Pyre refresh batch independently validates live gold/cache-refresh interactions;
nonzero saved modified Pyre base attack remains guarded. The resource batch
does not establish support for all owners of these five effect classes.
The Pyre refresh batch brings inventory to 221 and owner coverage to 50/224.
Its final frozen pure, strict and complete-fixture checks pass, together with
five directly affected old-fixture regressions. Sampling and final model hashes
are distinct after a Room-only API correction; acceptance uses final-model
replay against the captured native states. Full 221-archive replay remains
deferred, and unsupported tower room modifiers and uncaptured Setup-time Gold
range amounts remain guarded.
Mageblade magnitude rules, its captured per-unit cache, and attacks using captured
room magic power are now modeled. `full-battle-mageblade.mt2f` verifies 39 native
status/cache/room-power observations and passes all 45 battle stages, eight paid
actions, six EndTurns and 16 whole-battle/mid-battle policy branches. The test
injects +15 room power as a controlled input; the natural source and preview-copy
lifecycle of room power remain outside this evidence. Mageblade is not part of
the original-relic owner count.
The schema-121 mixed cardless-order fixture places `SpawnWithArmor`'s status
payload before `PyreHeartSon`'s cardless upgrade in a single test relic. Native
and independent replay verify five non-clone applications and five clone skips.
This combines two existing payloads for ordering coverage and does not add an
original relic owner to the count.
The older `6/224` passage in README describes an earlier milestone; it does not
include the starter-cost, cardless or four-artifact batch evidence above.

The native Horde-triggered unit-upgrade archive brought accepted original-owner
coverage to 64/224. The subsequent status-stack relic archive adds
`ExtraSpellWeakness`, bringing the total at that milestone to 65/224 (29.02%). The
refreshed schema-136 status-stack archive now also accepts `IncreaseDecaySlimeStacks`,
bringing coverage to 66/224 (29.46%); its 39 battle stages have zero
mismatches, capture failures, unsupported interactions or pending records. At
that checkpoint, the 246-archive replay attempt had not completed and a full
replay of the then-current 251 archives was deferred. The
`HerzalsAnvil` Forge Points battle then brings accepted original-owner coverage
to 67/224 (29.91%); its positive adjustment, `+1` payload and owner ablation pass
with zero native mismatches, capture failures, unsupported interactions or pending records.

The schema-135 `full-battle-relic-card-ephemeral-upgrades.mt2f` archive verifies
that relic-granted temporary Ephemeral traits purge cards on play and at
EndTurn. Its test authors a replacement upgrade payload on the original
`ReduceStarterCost` relic, so it adds battle-mechanism coverage only and does
not change the accepted 67/224 original-relic count.

The schema-137 `full-battle-relic-extra-tough-tombs.mt2f` archive accepts the
unchanged original `ExtraToughTombs` relic (DataId
`61fdeafd-21c5-4009-a448-a5c286ac3fc0`). Its native `OnlyTombs` filter accepts
the pre-battle carrier, applies +30 health and the unique `AddBurnoutToTombs`
upgrade, and the card is played. The summoned Train Steward is 55/55 with one
`ephemeral` stack. At `OnPostCombatPoison`, that status is consumed, `OnBurnout`
fires, and the unit is sacrificed. The muted Instant battle won with 22 actions,
seven EndTurns and 59 stages; native capture has zero failures, mismatches,
unsupported transitions or pending records. Independent model checks match all
59 room stages and 22 actions; search matches 22 decisions and 128 states.
Coverage is now 68/224 (30.36%).

The archive is 33,356 bytes with 5,388 unique nodes and SHA-256
`1551a614cb42645e926c6637629d2d4a33320cdc7afdf52ea10b6920bb2a8ad2`; there is
no JSON companion. The inventory is 253 archives: 222 battles and 31 component
calibrations. Full-inventory replay remains deferred.

The schema-146 `full-battle-original-moon-phase-energy-relic.mt2f` archive
accepts the unchanged `MoonPhaseModifyEmber` owner (DataId
`70096a91-220a-4ab3-b8d0-b8fe8f3a819c`). Three Full callbacks each add one Ember
and notify once; three New callbacks leave Ember unchanged and do not notify.
The original once-per-turn condition remains intact. Hidden `newmoon`/`fullmoon`
markers match every action and EndTurn state, including newly spawned units. The
muted Instant battle wins with 57 matched stages, 24 card plays and seven
EndTurns; capture failures, mismatches, unsupported transitions and pending
records are zero. Independent replay matches all transitions and the whole-battle,
mid-battle and 16-branch policy checks. Coverage is now 73/224 (32.59%).

The binary is 34,311 bytes with 5,595 unique nodes and SHA-256
`e04a9a7be21b1e82270b1d4182db0ba9eccd7ceda96171b84d6946d2a3e19764`; it has no
JSON companion. The inventory is 259 archives: 228 battles and 31 component
calibrations. Full-inventory replay remains deferred.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-original-moon-phase-energy-relic.mt2f
dotnet run --project src/ModelChecks -c Release -- --original-moon-phase-energy-only tests/fixtures/full-battle-original-moon-phase-energy-relic.mt2f
```

The schema-166 `full-battle-original-moon-phase-heal-relic.mt2f` archive accepts
unchanged `MooncycleRestoreFriendlyHealth` (DataId
`6d977136-5aac-44fd-9843-54cf2a0bd01d`): `Monsters`, `WeakestAllRooms`, 10 HP,
all moon phases, no subtype or conditions. Seven actual callbacks span Full and
New phases; six restore the selected lowest-HP damaged friendly, including
cross-room choices in two callback snapshots, with healing capped at both the
configured amount and max HP.
The remaining callback has no damaged friendly and leaves all HP unchanged.
The capture includes team, room, target order, targetability and before/after HP
for every unit, including enemy and Pyre controls. Five authored scenario plays
replay exactly; the full winning battle records 52 stages, 24 card plays and six
EndTurns, with zero capture failures, mismatches, unsupported transitions or
pending records. Independent search matches 24 decisions, 128/128 states, four
replayed steps and serial/parallel best results. Other target modes, conditioned
definitions and `OnHeal` callbacks remain explicitly unsupported by this model.
Coverage is now 140/224 (62.50%).

The binary is 35,543 bytes with 5,814 unique nodes and SHA-256
`2814a44aba7f03b2a730b4396ef5e7034acf507484bf35d127ee6991ced1f3c1`; it has no
JSON companion. The inventory is 362 archives: 324 battles and 38 component
calibrations.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-original-moon-phase-heal-relic.mt2f
dotnet run --project src/ModelChecks -c Release -- --original-moon-phase-heal-only tests/fixtures/full-battle-original-moon-phase-heal-relic.mt2f
```

The schema-147 `full-battle-original-card-attack-relic.mt2f` archive accepts the
unchanged `WhelpDragonAttack_DH` owner. Its original effect data has `CardType=Spell`,
but native code checks the spawned subtype first when one is configured. Two
controlled Monster card states therefore exercise the same subtype branch: Whelp
returns effect 0 and Dragon returns effect 1, each with a +8 temporary attack
offset. Both non-reset and reset manager calls agree. The battle won with 60
matched stages, 21 plays and seven EndTurns; all failure, mismatch, unsupported
and pending counts are zero. The archive does not include a live cardless birth;
the pure model check covers that branch. Dynamic multipliers, upgrade/relic-count
scaling, name/card filters and champion/starter exclusions remain rejected.
Coverage is now 74/224 (33.04%).

The binary is 32,904 bytes with 5,327 unique nodes and SHA-256
`051f518a1e7dae9e2818a5bf940651dce35afde5cbc86943e4ebcf36bc7f3208`; it has no
JSON companion. Inventory is 260 archives: 229 battles and 31 component
calibrations.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-original-card-attack-relic.mt2f
dotnet run --project src/ModelChecks -c Release -- --original-card-attack-only tests/fixtures/full-battle-original-card-attack-relic.mt2f
```

The schema-168 `full-battle-original-start-room-attachment.mt2f` archive accepts
the unchanged `TrainRoomInstallFloor1AtStartOfCombat` owner. Its primary, cardless
`FuelCar_Upgrade` attachment targets both Heroes and Monsters in room 0 without
incrementing the card-attachment counter. Native traces show six Heroes and two
Monsters with exactly one `OnDeath` trigger whose origin is the attachment GUID
and with the applied FuelCar upgrade recorded. Ascending enemies lose the source-
room upgrade and trigger immediately; the movement model matches this state.
The complete battle wins with 60 matched stages, 23 plays and seven EndTurns, all
failure/mismatch/unsupported/pending counters zero. Independent search matches 23
decisions across 128/128 states with serial and parallel best results equal.
Other room-attachment lifecycle paths are not claimed by this fixture. Coverage is
now 143/224 (63.84%).

The binary is 34,617 bytes with 5,544 unique nodes and SHA-256
`409f538660dbc7e13f1c708f0f1d26903c1e4b81381dd722fe6d815a47daa65b`; it has no
JSON companion. Inventory is 364 archives: 326 battles and 38 component
calibrations.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-original-start-room-attachment.mt2f
dotnet run --project src/ModelChecks -c Release -- --original-start-room-attachment-only tests/fixtures/full-battle-original-start-room-attachment.mt2f
```

## Fused Pyreshard

The schema-106 `full-battle-original-fused-pyreshard.mt2f` archive validates the
unchanged owner and its Monsters-source `SubtypesData_Pyre` OnKill +5 effect.
Because the isolated setup acquires the relic after creating the Pyre, it invokes
the original `RelicState.OnCharacterAdded` callback on that live Pyre and captures
the resulting single installed trigger; natural acquisition-before-Pyre-birth
ordering is not claimed. The native battle wins with 53 matched room stages, 28
actions and seven EndTurns, with all four failure counters at zero. Two Pyre
kills raise the persisted attack modifier from 0 to 10; five non-Pyre player-kill
controls remain unchanged, and two owner-trigger ablations remove the increase.
Independent policy replay covers 650 decision contexts and 16,610 exact native
card views, with 16 immutable branches. The model now accepts nonzero saved Pyre
attack in room combat and applies it once in live attack queries.
Native SaveManager preview mode suppresses the persistent adjustment; the pure
preview check confirms no saved-state change.

The binary is 69,639 bytes with 10,692 unique nodes and SHA-256
`d375c45659ebbafc694a012f45b5dab8717fff52c20423443270fdfbcaf2d748`; it has no
JSON companion. Inventory is 365 archives: 327 battles and 38 component
calibrations. Original coverage is 144/224 (64.29%), including 128 battle owners.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-original-fused-pyreshard.mt2f
dotnet run --project src/ModelChecks -c Release -- --gold-pyre-pure
```

[counts]: full-battle-incant-relics-combined.mt2f
[statuses]: full-battle-spawn-status-relics.mt2f
[cost]: full-battle-relic-card-modifiers.mt2f
[cardless]: full-battle-cardless-relic-upgrades.mt2f
[batch]: full-battle-original-relic-batch.mt2f
[forge]: full-battle-original-forge-points-relic.mt2f
[units]: full-battle-original-unit-relics.mt2f
[triggerDeaths]: full-battle-original-trigger-relic-deaths.mt2f
[triggers]: full-battle-original-trigger-relics.mt2f
[statusAdders]: full-battle-original-status-relics.mt2f
[burnoutStacks]: full-battle-original-burnout-stacks.mt2f
[magebladeMagnitude]: full-battle-original-mageblade-magnitude.mt2f
[magnitudes]: full-battle-original-magnitude-relics.mt2f
[lifecycles]: full-battle-original-lifecycle-relics.mt2f
[decays]: full-battle-original-decay-relics.mt2f
[resources]: full-battle-original-resource-relics.mt2f
[pyreRefresh]: full-battle-pyre-resource-refresh.mt2f
[fusedPyreshard]: full-battle-original-fused-pyreshard.mt2f
[floorCapacity]: full-battle-original-floor-capacity-relics.mt2f
[endless]: full-battle-original-endless-return.mt2f
[cardPlayMagicPower]: full-battle-original-card-play-magic-power.mt2f
[roomEntry]: full-battle-original-room-entry-status-relics.mt2f
[moonPhase]: full-battle-original-moon-phase-energy-relic.mt2f
[moonHeal]: full-battle-original-moon-phase-heal-relic.mt2f
[cardAttack]: full-battle-original-card-attack-relic.mt2f
[cardDraw]: full-battle-original-card-draw-on-card-play.mt2f
[carriageJack]: full-battle-original-carriage-jack.mt2f
[firstRoomAttachment]: full-battle-original-first-room-attachment.mt2f
[startRoomAttachment]: full-battle-original-start-room-attachment.mt2f
[startRoomSpellSanctum]: full-battle-original-start-room-attachment-floor2.mt2f
[startRoomFightClub]: full-battle-original-start-room-attachment-floor3.mt2f
[partFactory]: full-battle-original-part-factory.mt2f
[unitUpgradeTrigger]: full-battle-original-unit-upgrade-trigger-relic.mt2f
[extraToughTombs]: full-battle-relic-extra-tough-tombs.mt2f
[stealthWaxers]: full-battle-original-stealth-waxers.mt2f
[teamSpawnStatus]: full-battle-original-team-spawn-status.mt2f
[tunedGear]: full-battle-spawn-status-relics-tuned-gear.mt2f
[chicot]: full-battle-spawn-status-relics-boss-mute.mt2f
[pyreSpawn]: full-battle-spawn-status-relics-pyre.mt2f
[eggHatch]: full-battle-spawn-status-relics-egg-hatch.mt2f
[sting]: full-battle-original-sting-relics.mt2f
[deployable]: full-battle-original-deployable-relic.mt2f
[smelt]: full-battle-original-smelt-ember.mt2f
[smeltZero]: full-battle-original-smelt-ember-zero.mt2f
[smeltBulk]: full-battle-original-smelt-ember-bulk.mt2f
[tinkermasterMix]: full-battle-original-tinkermaster-mix.mt2f
[progressMix]: full-battle-original-progress-pacemaker-mix.mt2f
[cardTypeCost]: full-battle-original-card-type-cost-relic.mt2f
[conserveEmber]: full-battle-original-conserve-ember.mt2f
[upgradeCounts]: full-battle-original-upgrade-count-relics.mt2f
[healingPower]: full-battle-original-healing-power-relic.mt2f
[firstCombatEnergy]: full-battle-original-start-combat-energy-relic.mt2f
[startCombatHoard]: full-battle-original-start-combat-hoard-relic.mt2f
[earlyDiscardEnergy]: full-battle-original-early-discard-energy.mt2f
[cheaperSpellCost]: full-battle-original-cheaper-spell-cost.mt2f
[firstCardCost]: full-battle-original-first-card-cost.mt2f
[rebalanceValor]: full-battle-original-rebalance-valor.mt2f
[nefariousOverflow]: full-battle-original-nefarious-overflow.mt2f
[copyFirstSpell]: full-battle-original-copy-first-spell-played.mt2f
[funguyDeckScaling]: full-battle-original-funguy-deck-scaling.mt2f
[forgeConservation]: full-battle-original-forge-conservation-relic.mt2f
[campaign]: campaign-relic-calibration.mt2f
[campaignDrafts]: campaign-draft-calibration.mt2f
[postBattleRewards]: campaign-post-battle-calibration.mt2f
[battleResources]: campaign-battle-resource-calibration.mt2f
[unitResources]: full-battle-original-unit-resource-relics.mt2f
[openingCorruption]: full-battle-original-opening-corruption-relics.mt2f
[spellThresholds]: full-battle-original-spell-play-threshold-relics.mt2f
[shroomDecay]: full-battle-original-shroom-decay-relic.mt2f
[conduit]: full-battle-original-conduit-relics.mt2f
[equipmentGrowth]: full-battle-original-equipment-growth-relics.mt2f
[healthChoice]: full-battle-original-health-choice-relic.mt2f
[afterDrawGeneration]: full-battle-original-after-draw-generation-relics.mt2f
[addSnackOnKill]: full-battle-original-add-snack-on-kill.mt2f
[finalEndingArtifact]: full-battle-original-final-ending-artifact.mt2f
[openingDraw]: full-battle-original-opening-draw-relics.mt2f
[turnDraw]: full-battle-original-turn-draw-relics.mt2f
[purgeGold]: campaign-purge-gold-calibration.mt2f
[phaseUpgrades]: full-battle-original-unit-phase-upgrade-relics.mt2f
[deckCopy]: campaign-deck-copy-calibration.mt2f
[nullRelic]: null-relic-equivalence-calibration.mt2f
[relicCountAttack]: full-battle-original-relic-count-attack.mt2f
[bigDamage]: full-battle-original-big-damage-many-morsels.mt2f
[moreSpikes]: full-battle-original-more-spikes.mt2f
[stealthSlay]: full-battle-original-stealth-slay.mt2f
[cardDrawOnHeal]: full-battle-original-card-draw-on-heal.mt2f
[brokenRail]: full-battle-original-broken-rail.mt2f
[rageBurnout]: full-battle-original-rage-on-burnout-stack-loss.mt2f
[pyreRevenge]: full-battle-original-pyre-revenge.mt2f
[extractForEmberAndEchos]: full-battle-original-extract-for-ember-and-echos.mt2f
[copyFirstConsume]: full-battle-original-copy-first-consume-played.mt2f
[copyConsumedCard]: full-battle-original-copy-consumed-card.mt2f
[monsterSpellTargeting]: full-battle-original-monster-spell-targeting.mt2f
[garfieldBoxRandomCard]: full-battle-original-garfield-box-random-card.mt2f
[moonPhaseArmor]: full-battle-original-moon-phase-status-relic.mt2f
[damageOnFloorTransition]: full-battle-original-damage-on-floor-transition.mt2f
[soloUnitUpgrade]: full-battle-original-add-stats-to-solo-units.mt2f
[purgeToExhaust]: full-battle-original-purge-to-exhaust.mt2f
[pyregelOnAscend]: full-battle-original-pyregel-on-ascend.mt2f
[uncontrolledGroup]: full-battle-original-uncontrolled-group.mt2f

## AddStatsToSoloUnits

The schema-183 `AddStatsToSoloUnits` fixture captures the unchanged original
owner, its `RelicEffectAddTempSoloUnitUpgradeStartOfTurn` callback and the
non-unique +5 Damage/+5 Health payload. Native calls reject the start-of-battle
phase, accept each player-turn phase, and repeat the buff on a sole player
Monster. A second floor-0 Monster clears tracking without removing the two
already-applied upgrades; floor 1 stays multi-unit and floors 2/3 stay empty.
Focused replay verifies owner-present and removed-owner branches, exact tracker
reset and all relic-state clone paths. The full battle wins with 46 matched
stages, four actions, five EndTurns and zero capture failures, mismatches,
unsupported transitions or pending records; independent search matches 128
states with serial/parallel best equality.

The direct binary archive is 24,606 bytes with 3,502 nodes and SHA-256
`7b4ac92dcb8a37b56538f1fe1bf64a3e970d6627400351f1ff1a686455ee5911`.

## PurgeToExhaust

The schema-184 `PurgeToExhaust` fixture captures the unchanged original owner
and its ordered `OnlySpells` then `OnlyEquipment` `RelicEffectModifyTrait`
filters. Six native source/filter controls include SelfPurge and no-trait
Spell/Equipment cases plus a SelfPurge Blight control. Twenty-four repeated
manager calls match the captured descriptors and filter dispatches. The
converted Spell is played in the winning 61-stage battle and reaches the
Exhausted pile with its native private Exhaust trait state; all error counters
are zero. Focused replay and independent 128/128-state search pass with
serial/parallel best equality. Pure controls cover source-absent persistent
metadata, repeat/reset idempotence, owner validation and immutable branches.

The direct binary archive is 36,125 bytes with 5,754 nodes and SHA-256
`f4efc2e1383f0783cab729a2b371c5438ebf0ec01aabd85ad296ca2a4e22f8c8`.
The inventory is 383 archives: 345 full battles and 38 component calibrations.
Original relic evidence is 162/224 (72.32%): 146 battle, 15 campaign and one
native no-op owner.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-original-purge-to-exhaust.mt2f
dotnet run --project src/ModelChecks -c Release -- --original-purge-to-exhaust-pure
```

## PyregelOnAscend_DH

The schema-185 fixture captures the unchanged `PyregelOnAscend_DH` owner,
`RelicEffectAddStatusOnUnitMovedUpDown`, source team Heroes, restriction mask 5,
no excluded subtypes and a +2 Pyregel payload. The captured rooms have
`Deployment=true`; the original owner is not disallowed during placement, so an
enemy PostAscension still applies the status. Descension, player movement,
rooted and immobile controls remain unchanged. All 22 native Bump operations
replay exactly, including callback order. The winning battle matches 40 stages
and 42 actions with zero capture failures, mismatches, unsupported transitions
or pending records; the native probe also leaves original game files unchanged.
Independent battle search matches 42 decisions and checks 74/74 states, with
zero unsupported attempts, three replayed steps and equal serial/parallel best
results.

The direct binary archive is 50,842 bytes with 9,814 nodes and SHA-256
`e4c1cf757d2b3550af6c1387f40720541d971abd633eb564436bba71b180c91d`.
The inventory is 383 archives: 345 full battles and 38 component calibrations.
Original relic evidence is 162/224 (72.32%): 146 battle, 15 campaign and one
native no-op owner.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-original-pyregel-on-ascend.mt2f
dotnet run --project src/ModelChecks -c Release -- --pyregel-on-ascend-pure
```

## TrainRoomInstallFloor2AtStartOfCombat

The schema-186 `full-battle-original-start-room-attachment-floor2.mt2f` archive
validates the unchanged `TrainRoomInstallFloor2AtStartOfCombat` owner and its
cardless primary `SpellSanctum_Upgrade` attachment on room index 1. The native
upgrade contains exactly one `RoomStateSpellCostModifier(-1)` and targets both
Heroes and Monsters without incrementing the card-attachment counter. A positive-
cost hand Spell reads cost 1 on the control floor and 0 in the attached room;
the original card state is not played or mutated. Eight impacted-unit snapshots
retain the upgrade only while in the attached room, and 31 room-state snapshots
preserve its GUID and modifier. No X-cost Spell was present in the captured hand,
so the +1 PlayedCost branch is not claimed as battle-witnessed.

The complete battle wins with 57 matched stages, 27 actions and seven EndTurns;
capture failures, mismatches, unsupported transitions and pending records are
zero, and original game files are unchanged. Independent search matches 27
decisions across 128/128 states, with zero unsupported attempts and equal
serial/parallel best results. The archive has 34,481 bytes and 5,514 unique
nodes, SHA-256
`4d81d3d5fc6983acda69fa01ab84e924f41e93cabe499a95dfe7a5102ed75dbe`; no JSON
companion is tracked. Inventory is 385 archives: 347 full battles and 38
component calibrations. Original relic evidence is 164/224 (73.21%): 148
battle, 15 campaign and one native no-op owner.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-original-start-room-attachment-floor2.mt2f
dotnet run --project src/ModelChecks -c Release -- --original-start-room-attachment-only tests/fixtures/full-battle-original-start-room-attachment-floor2.mt2f
```

## TrainRoomInstallFloor3AtStartOfCombat

The schema-187 `full-battle-original-start-room-attachment-floor3.mt2f` archive
validates the unchanged `TrainRoomInstallFloor3AtStartOfCombat` owner and its
cardless primary `FightClub` attachment on room index 2. The native owner targets
Monsters only; FightClub contains exactly +15 attack and no other unit or room
payload. The target room starts empty, then one impacted player Monster carries
the native upgrade while in that room. Thirty-two room-state snapshots preserve
the attachment GUID and payload.

The complete battle wins with 60 matched stages, 21 actions and seven EndTurns;
capture failures, mismatches, unsupported transitions and pending records are
zero, and original game files are unchanged. Independent search matches all 21
recorded decisions across 128/128 states, with zero unsupported attempts and
equal serial/parallel best results. Focused model checks cover +15 application
when entering the room and removal when leaving. The archive has 34,269 bytes
and 5,409 unique nodes, SHA-256
`af52ad4508492a8711630fdd7aad6fa381813a6d86687594c64ea303966319cf`; no JSON
companion is tracked. Inventory is 385 archives: 347 full battles and 38
component calibrations. Original relic evidence is 164/224 (73.21%): 148
battle, 15 campaign and one native no-op owner.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-original-start-room-attachment-floor3.mt2f
dotnet run --project src/ModelChecks -c Release -- --original-start-room-attachment-only tests/fixtures/full-battle-original-start-room-attachment-floor3.mt2f
```

## SingleToTeamMonsterSpellTargeting

The schema-188 archive captures the unchanged original owner and its tracked
ThisTurn condition. A direct unupgraded Spell with no traits targets Monsters
and applies fixed 1 Damage via DropTarget. The owner applies that effect to the
other eligible Monster in the same non-Pyre room, excluding the selected target
and Untouchable units. Native team, no-target and eligibility controls pass;
the full 62-stage winning replay has 13 card cycles, 20 actions, seven EndTurns
and zero capture failures, mismatches, unsupported transitions or pending
records. The focused checker verifies the condition update and once-per-turn
reset.

Independent search visits 128/128 states with serial/parallel best equality;
it records 91 unsupported alternatives while matching recorded decisions and
21 unsupported candidate moves in its bounded search. This is not generic
coverage for other spell effects, upgrades, traits, OnCast behavior, Pyre rooms
or mixed-effect spells. The 35,464-byte binary has 5,580 unique nodes and SHA-256
`a14ea15a21c34c58ca672d3d28e0b8b1eec0db68f57e642fa5ed43272707b584`; no JSON
companion is tracked. The inventory is 386 archives: 348 battles and 38
component calibrations. Original relic coverage is 165/224 (73.66%): 149 battle,
15 campaign and one native no-op owner.

## MoonPhaseAddArmorToSilent

The schema-190 archive captures the unchanged New-only owner applying 10 Armor
to every eligible friendly Silent in Tower-targeted rooms. Three Full callbacks
are no-ops; three New callbacks add Armor to two Silent witnesses across
separate rooms. All six callbacks match complete-state replay. The winning
45-stage battle has 17 actions, five EndTurns, and zero capture failures,
mismatches, unsupported transitions or pending records. Independent search
matches all 17 decisions across 128/128 states with serial/parallel best
equality and zero unsupported attempts. Pure controls cover phase, owner
filters, status metadata, preview behavior, rejected target callbacks and
immutable branches. Support is limited to this owner and targets without status
application callbacks; other moon-phase status relics remain pending. The
34,017-byte binary has 5,272 unique nodes and SHA-256
`6f8cf7873e739ec4189ca03c13db71db7f74f3919c7453191c164200e7356bb6`; no JSON
companion is tracked. Inventory is 388 archives: 350 full battles and 38
component calibrations. Original relic coverage is 167/224 (74.55%): 151
battle, 15 campaign and one native no-op owner.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-original-moon-phase-status-relic.mt2f
dotnet run --project src/ModelChecks -c Release -- --original-moon-phase-status-only tests/fixtures/full-battle-original-moon-phase-status-relic.mt2f
dotnet run --project src/ModelChecks -c Release -- --battle-search-only tests/fixtures/full-battle-original-moon-phase-status-relic.mt2f
```

## UncontrolledGroup

The schema-191 archive captures the unchanged ordered
`RelicEffectRebalanceUnstable` / `RelicEffectNull` owner. Its native
`OnMonsterTeamTurnBegin` callback grows the nonlethal carrier from 5 to 10
Unstable; Blast Resistant suppresses growth. The 41-stage winning replay has
zero capture failures, mismatches, unsupported transitions or pending records.
The focused checks cover both teams, exact payload guards, the nonzero-floor
callback queue, and the lethal pre-growth rejection boundary. Independent search
matches all three recorded decisions over 128/128 states, with serial/parallel
best equality and zero unsupported attempts. Lethal pre-growth sacrifice and
sibling `RelicEffectRebalanceUnstable` payloads remain outside the support
boundary. The 25,130-byte binary has 3,471 unique nodes and SHA-256
`c0bb3b5babf93a34eaef97e4afd763de925a17415a592891e3aee121d75f84b8`; no JSON
companion is tracked. Inventory is 389 archives: 351 full battles and 38
component calibrations. Original relic coverage is 168/224 (75.00%): 152 battle,
15 campaign and one native no-op owner.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-original-uncontrolled-group.mt2f
dotnet run --project src/ModelChecks -c Release -- --original-uncontrolled-group-only tests/fixtures/full-battle-original-uncontrolled-group.mt2f
dotnet run --project src/ModelChecks -c Release -- --battle-search-only tests/fixtures/full-battle-original-uncontrolled-group.mt2f
```

## AddSnackOnKill

The schema-192 archive captures the unchanged owner, exact ordered 19-entry
weighted Class5Food pool, and native enemy-Hero OnDeath behavior. Three
qualifying callbacks add six non-permanent cards; 312 callbacks are gated.
Complete callback contexts match, including Battle RNG consumption and both
effect/manager notifications. The winning 55-stage replay and one model-selected
action have zero differential counters. Independent search matches the action
and visits 128/128 states with serial/parallel best equality.

The native run had no full-hand death. A synthetic control fills the hand from
captured card identities and confirms that both selection attempts still consume
Battle RNG while no card identity is allocated and only one manager notification
is recorded. The policy stops after one model-selected action and never plays
cards generated by this relic; generated-card play effects and multi-action
terminal cleanup are outside this support boundary. The 30,100-byte archive has
4,327 unique nodes and SHA-256
`47996ad0a1d10f9b5f6720b0b6d347d1f967eb88fc025ce369ffa3273789dbb`; no JSON
companion is tracked. Inventory is 390 archives: 352 battles and 38 component
calibrations. Accepted original-owner coverage is 169/224 (75.45%): 153 battle,
15 campaign and one native no-op.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-original-add-snack-on-kill.mt2f
dotnet run --project src/ModelChecks -c Release -- --original-add-snack-on-kill-only tests/fixtures/full-battle-original-add-snack-on-kill.mt2f
dotnet run --project src/ModelChecks -c Release -- --battle-search-only tests/fixtures/full-battle-original-add-snack-on-kill.mt2f
```

## FinalEndingArtifact

The schema-193 archive captures the unchanged owner and its ordered 36-entry
champion pool, including each candidate's grant upgrades and the native deck
eligibility mask. Four natural callbacks add one non-permanent champion to hand;
two hand/champion gates leave context and Battle RNG unchanged. The selected
candidate, reverse Fisher-Yates N-1 shuffle, upgrade identity and complete
generated-card context match native state. Full-hand and champion-in-hand
controls allocate no card or consume no RNG. A synthetic candidate mask tests
that excluded entries cannot be selected; this run's own deck champion is not
in the random pool, so no native entry was excluded.

The 55-stage battle and one model-selected action have zero capture failures,
mismatches, unsupported transitions or pending records. Independent search
matches the decision and visits 128/128 states with serial/parallel best
equality. Eight pool entries carry external grant-upgrade interactions and are
explicitly refused by the model; generated-card play is not exercised by the
one-action policy. The 33,994-byte archive has 4,642 unique nodes and SHA-256
`910a8b3888f646383d58d1714db9fd0a1d019a63eaf0c18803a3a5a0d0c2390f`; no JSON
companion is tracked. Inventory is 391 archives: 353 full battles and 38
component calibrations. Original relic evidence is 170/224 (75.89%): 154 battle,
15 campaign and one native no-op owner.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-original-final-ending-artifact.mt2f
dotnet run --project src/ModelChecks -c Release -- --original-final-ending-artifact-only tests/fixtures/full-battle-original-final-ending-artifact.mt2f
dotnet run --project src/ModelChecks -c Release -- --battle-search-only tests/fixtures/full-battle-original-final-ending-artifact.mt2f
```

## 2ChargesOnCardConsumed

The schema-195 archive captures unchanged owner `217d553b-cddf-4a8c-a4c9-cfdb3c99389c`
(`RelicEffectModifyCorruptionOnCardPlayed`, `ParamInt=1`, no condition, required
`CardTraitExhaustState`). A non-Exhaust control leaves corruption unchanged. An
eligible Exhaust carrier adds exactly one corruption to its selected floor and
leaves every other floor unchanged; the carrier enters Exhausted. The replay
also confirms the original owner is unchanged.

The winning replay has 61 stages, 13 card cycles, 14 train phases, 11 spawns,
five card actions and seven EndTurns, with zero capture failures, mismatches,
unsupported transitions or pending records. Independent search matches all five
recorded decisions and visits 128/128 states with serial/parallel best equality;
four steps replay. The generic Consume lifecycle regression now clears stale
unit-condition bindings when an already-Standby consumed card enters the
Exhausted alias while preserving Standby and Exhausted pile identity.
`CorruptionAdded` callbacks and corruption already above its maximum are outside
this witness. The 28,759-byte archive has 4,274 unique nodes and SHA-256
`f3b0f017b30ae1c22f8f367210fe7989b1410630218673332503b25c3f62ecdf`; no JSON
companion is tracked. Inventory is 393 archives: 355 full battles and 38
component calibrations. Original relic evidence is 173/224 (77.23%): 156 battle,
16 campaign and one native no-op owner.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-original-two-charges-on-card-consumed.mt2f
dotnet run --project src/ModelChecks -c Release -- --original-two-charges-only tests/fixtures/full-battle-original-two-charges-on-card-consumed.mt2f
dotnet run --project src/ModelChecks -c Release -- --battle-search-only tests/fixtures/full-battle-original-two-charges-on-card-consumed.mt2f
```

## ChanceForSpellWeakness

The schema-196 archive captures unchanged owner
`98989cbb-c729-4f87-8240-08f448db96d8`, its 50% enemy-Hero
`AfterSpawnEnchant` filter and one self-targeted Spell Weakness +1 effect.
Captured nested metadata includes the native `SubtypesData_None` key,
`TargetCardType=Spell`, `ParamMultiplier=0`, and the complete Spell Weakness
status definition. Seven eligible tests match one native 0..100 Battle RNG draw
each (four hits, three misses), while 496 gated tests preserve RNG. Four native
Apply coroutines add exactly one stack and consume no additional RNG.

The winning replay has 61 stages, 13 card cycles, 14 train phases, 11 spawns,
five card actions and seven EndTurns, with zero capture failures, mismatches,
unsupported transitions or pending records. Independent search matches all five
decisions and visits 128/128 states with serial/parallel best equality; four
steps replay. Focused controls cover preview, destroyed/Untouchable units, wrong
trigger/team, removed owner, incomplete payload and all 25 immutable relic copy
updates. The changed spawn-trigger ordering also passes existing room-entry and
spawn-trigger fixture replays. Other owner identities and nested effect shapes
are not generalized. The 31,208-byte archive has 4,728 unique nodes and SHA-256
`c4981c58612f035f5f69df78f100ba78c8aa7d42a197b5293ac596e729e56692`; no JSON
companion is tracked. Inventory is 394 archives: 356 battles and 38 component
calibrations. Original relic evidence is 174/224 (77.68%): 157 battle, 16
campaign and one native no-op owner.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-original-chance-for-spell-weakness.mt2f
dotnet run --project src/ModelChecks -c Release -- --original-chance-for-spell-weakness-only tests/fixtures/full-battle-original-chance-for-spell-weakness.mt2f
dotnet run --project src/ModelChecks -c Release -- --battle-search-only tests/fixtures/full-battle-original-chance-for-spell-weakness.mt2f
```

## ChanceToCleanse

| `ChanceToCleanse` | `1e972d81-fc2f-4d4a-ade9-46edeb246dbd` | Native acquired owner and exact nested cleanse payload |

The schema-198 archive captures the unchanged `RelicEffectChanceAddEffectAfterSpawn`
owner: Heroes team, original None subtype, 50% chance, no room restriction,
exclusions or conditions. Its one self-targeted nested effect is
`CardEffectRemoveAllStatusEffects`, category 0 (visible Positive), with
remove-half disabled. Seven eligible `AfterSpawnEnchant` tests consume one
matching native 0..100 Battle RNG draw each (four hits, three misses); 510
gated tests preserve RNG. All four successful native applications complete
without additional RNG, and two visible Positive status stacks are removed.

Focused model controls verify exact Positive-only removal, preservation of
Negative statuses on a hit, status preservation on a miss, and wrong trigger,
team, preview, Untouchable, destroyed-actor and unknown-owner gates. Every
recorded combat stage independently replays with zero differences. The winning
battle has 63 stages, 13 card cycles, 14 train phases, 11 spawns, five card
actions and seven EndTurns, with zero capture failures, mismatches, unsupported
transitions or pending records. Independent search matches all five decisions,
visits 128/128 states with serial/parallel best equality and replays four steps.
The evidence covers this original owner and nested effect payload only. The
31,639-byte archive has 4,826 unique nodes and SHA-256
`0755bc75824ceef1b822a15624eeb50ba4d8f3f6df0514f867143ac021328074`; no JSON
companion is tracked. Inventory is 396 archives: 358 full battles and 38
component calibrations. Original relic evidence is 176/224 (78.57%): 159
battle, 16 campaign and one native no-op owner.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-original-chance-to-cleanse.mt2f
dotnet run --project src/ModelChecks -c Release -- --original-chance-to-cleanse-only tests/fixtures/full-battle-original-chance-to-cleanse.mt2f
dotnet run --project src/ModelChecks -c Release -- --battle-search-only tests/fixtures/full-battle-original-chance-to-cleanse.mt2f
```

## SummonWithAvarice

| `SummonWithAvarice` | `a9670728-2ad2-4776-be80-e1a08864f18c` | Native acquired owner and exact spawn payload |

The schema-197 archive captures the unchanged `RelicEffectAddStatusEffectOnSpawn`
owner. Its Monster-target rule requires the original None subtype, has no
exclusions, conditions, room restriction, graft or character filter, and applies
two `avarice` stacks. Five eligible player births gain exactly those two stacks;
enemy births remain unchanged. Each captured birth independently replays with
the native Battle RNG advance.

Native source shows Avarice grants gold after positive damage from a DirectAttack
or Trample hit, or from a source card marked as a UnitAbility. Focused controls
cover a fully blocked attack, preview combat and ordinary Spell damage; a
UnitAbility hit does grant gold. The captured reward multiplier and native
minimum-one/nearest-increment rounding are applied per hit. The native replay
contains ten combat stages with a positive, independently ablated Avarice-only
gold contribution and zero battle differences.

The winning replay has 61 stages, 13 card cycles, 14 train phases, 11 spawns,
five card actions and seven EndTurns, with zero capture failures, mismatches,
unsupported transitions or pending records. Independent search matches all
five decisions, visits 128/128 states with serial/parallel best equality and
replays four steps. This evidence is limited to the captured original owner,
its two-stack birth payload and the tested Avarice damage sources; other owners
and status-trigger combinations are not generalized. The 29,926-byte archive
has 4,443 unique nodes and SHA-256
`9475c27591a83feaee0d2e1c9fd67e50dfe50d1851d6b97a48dd6fe4b7ca4ee5`; no JSON
companion is tracked. Inventory is 395 archives: 357 full battles and 38
component calibrations. Original relic evidence is 175/224 (78.12%): 158
battle, 16 campaign and one native no-op owner.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-original-summon-with-avarice.mt2f
dotnet run --project src/ModelChecks -c Release -- --original-summon-with-avarice-only tests/fixtures/full-battle-original-summon-with-avarice.mt2f
dotnet run --project src/ModelChecks -c Release -- --battle-search-only tests/fixtures/full-battle-original-summon-with-avarice.mt2f
```

## ExtraFeedTrigger

| `ExtraFeedTrigger` | `b6613fe4-13c6-4462-8a0b-9af42850a851` | Native acquired owner and exact trigger-count payload |

The schema-199 archive captures the unchanged `RelicEffectModifyTriggerCount`
owner: Monsters team, original None subtype, `OnFeed` +1, no all-trigger mode,
Hero eligibility, excluded card triggers or conditions. On deployment turn,
the live Monsters Pyre's native `GetTriggerFireCount` returns 2 for `OnFeed`
and 1 for `OnDeath`; the captured registration cache contains only the exact
`OnFeed` modifier. Focused model controls verify the Monsters positive, enemy,
other-trigger and `NoCountModifiersAllowed` paths, plus immutable parallel
queries.

No natural `OnFeed` dispatch occurred in this battle, so the evidence does not
claim a feed callback or its downstream effects were exercised. It combines
the direct native count query with the exact cache captured in battle contexts,
independent replay of all 61 native stages, and search across all five
decisions. Search visits 128/128 states with serial/parallel best equality and
replays four steps. The battle won with zero capture failures, mismatches,
unsupported transitions or pending records. The 44,070-byte archive has 5,640
unique nodes and SHA-256
`3cf10ba1b2ecaee524e957bdd7821e5fd247ab6b33f37a80029fc3d572ccf8b0`; no JSON
companion is tracked. Inventory is 397 archives: 359 full battles and 38
component calibrations. Original relic evidence is 177/224 (79.02%): 160 battle,
16 campaign and one native no-op owner.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-original-extra-feed-trigger.mt2f
dotnet run --project src/ModelChecks -c Release -- --original-extra-feed-trigger-only tests/fixtures/full-battle-original-extra-feed-trigger.mt2f
dotnet run --project src/ModelChecks -c Release -- --battle-search-only tests/fixtures/full-battle-original-extra-feed-trigger.mt2f
```

## HephsBlueprints

| `HephsBlueprints` | `5152b7d0-4f8a-4b7b-8871-06c25bb1a5af` | Native acquired owner, original Equipment filter and scaled forge payload |

The schema-200 archive captures the unchanged `RelicEffectModifyForgeUpgrade`
owner and its native `OnlyEquipmentNoEnemyTargetted` mask. At three Forge
points, the original +2 attack/+2 Armor payload is added to the eligible
Equipment card as +6/+6; the original type filter rejects a Monster control,
which receives only the default forge upgrade. The equipment is then attached
to the controlled Monster. Native source folds all temporary equipment-card
upgrades into one anonymous host upgrade, so the checker verifies that host
aggregate against every temporary upgrade on the equipment card rather than
expecting the original `DataId` to survive attachment.

Both scenario actions match the native game and shared action model. The
winning 39-stage replay has 17 card actions, five EndTurns and zero capture
failures, mismatches, unsupported transitions or pending records. Focused
checks cover the Equipment type filter, scaling, Monster/default-forge control,
inactive tracking, zero points, ineligible cards and immutable input.
Independent search matches all 17 recorded decisions, visits 128/128 states,
replays three steps and has serial/parallel best equality. The witness is
limited to this owner payload and the captured ordinary-equipment path; other
equipment effect shapes are not generalized. The 30,033-byte archive has 4,507
unique nodes and SHA-256
`55d6974cbff32d9e17db3a5878648301688e0120c37fb51fbd434b6dc152980a`; no JSON
companion is tracked. Inventory is 398 archives: 360 full battles and 38
component calibrations. Original relic evidence is 178/224 (79.46%): 161 battle,
16 campaign and one native no-op owner.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-original-hephs-blueprints.mt2f
dotnet run --project src/ModelChecks -c Release -- --original-hephs-blueprints-only tests/fixtures/full-battle-original-hephs-blueprints.mt2f
dotnet run --project src/ModelChecks -c Release -- --battle-search-only tests/fixtures/full-battle-original-hephs-blueprints.mt2f
```

## ModifyDragonsHoardBattleStart

| `ModifyDragonsHoardBattleStart` | `3b6780ab-a1e8-443d-9ddb-5caedbd87775` | Native acquired owner and +1 deployment callback |

The schema-201 battle archive captures the unchanged `RelicEffectModifyDragonsHoardStartOfCombat`
owner and its +1 payload. The native deployment callback changes Hoard from 0
to 1, then invokes the card-manager and monster-manager Hoard refresh exactly
once each. Acquisition leaves resources unchanged. The model exhaustively checks
6,006 cap, amount, signed-change and preview branches, including unchecked
integer overflow and parallel parent immutability.

The winning 60-stage battle has 21 card actions, seven EndTurns and zero capture
failures, mismatches, unsupported transitions or pending records. All seven
native EndTurns replay exactly; independent search matches 21 decisions, visits
128/128 states, replays four steps and has serial/parallel best equality. This
witness covers the original +1 deployment path and dependent refresh calls; other
start-combat Hoard payloads and preview-mode manager execution are not claimed.
The 35,081-byte archive has 5,503 unique nodes and SHA-256
`150ae19ca9d3869086e4b97419b997259b5527346ea62e5d0595cb635c5091d0`; no JSON
companion is tracked. Inventory is 399 archives: 361 full battles and 38
component calibrations. Original relic evidence is 179/224 (79.91%): 162 battle,
16 campaign and one native no-op owner.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-original-start-combat-hoard-relic.mt2f
dotnet run --project src/ModelChecks -c Release -- --original-start-combat-hoard-only tests/fixtures/full-battle-original-start-combat-hoard-relic.mt2f
dotnet run --project src/ModelChecks -c Release -- --battle-search-only tests/fixtures/full-battle-original-start-combat-hoard-relic.mt2f
```

## BalatroEventCavendish

| `BalatroEventCavendish` | `24c4ad26-f2b3-4a95-bddc-caab94bd4767` | Native acquired owner, ordered card attack, cardless callback and end-combat removal/undo |

The schema-204 archive checks the unchanged three-effect owner in native order.
Both card modifier reset modes match for an ordinary Monster, a Champion and a
Spell; accumulated applications are tracked by card-instance identity so the
1% removal branch can undo the exact amount on each deck copy. The full battle
also captures natural manager resets, while the native `OnCharacterAdded`
callback is isolated on a surviving ordinary unit: its unmodified attack 46
becomes 138. A control using the actual Champion subtype remains at 46, and the
unit's temporary cardless status, attack and subtype list are restored.

The end-combat probe seeds the native Battle RNG to a miss (1) and hit (0). The
miss retains the owner and only attempts the banana-shake effect; the hit emits
the destruction trigger, undoes every tracked in-deck modifier, removes the
owner, then attempts banana shake. Reacquisition restores card modifiers. Native
terminal BattleWon stops before that end-of-combat callback; the final captured
owner, tracked contributions and Battle RNG remain unchanged. Nonterminal clear
callbacks are modeled separately.

The winning 48-stage battle has 25 card actions, six EndTurns and zero capture
failures, mismatches, unsupported transitions or pending records. Dedicated
checks and independent search match all 25 recorded decisions, visit 128/128
states, replay four steps and have serial/parallel best equality. The 33,072-byte
archive has 5,105 unique nodes and SHA-256
`73e606fc0c3753e99d8a27c8a5d537a5017712c25b65df1469a6b21018198d0e`; no JSON
companion is tracked. Inventory is 400 archives: 362 full battles and 38
component calibrations. Original relic evidence is 180/224 (80.36%): 163 battle,
16 campaign and one native no-op owner.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-original-balatro-event-cavendish.mt2f
dotnet run --project src/ModelChecks -c Release -- --original-balatro-event-cavendish-only tests/fixtures/full-battle-original-balatro-event-cavendish.mt2f
dotnet run --project src/ModelChecks -c Release -- --battle-search-only tests/fixtures/full-battle-original-balatro-event-cavendish.mt2f
```

## UpgradeMaxHpFrontUnit

The schema-207 fixture captures the unchanged owner applying its non-unique
`EmblemOfExiles_Upgrade` (+15 Health) at start of turn. Four native callbacks
increase MaxHealth and current Health by exactly 15 on the first player Monster
in each populated floor; a second Monster on each floor and the two empty floors
are controls. The matching temporary upgrade is also added to each front unit's
spawner card, but not to either back-unit card. The start-of-turn test rejects
the initially empty battle and accepts the later populated floors.

The winning 54-stage battle has four card actions, five EndTurns and zero capture
failures, mismatches, unsupported transitions or pending records. Focused native
checks compare all four callbacks and run owner-present/owner-removed model
branches; independent search visits 128/128 states, replays three steps and has
serial/parallel best equality. A retained dying front unit is explicitly
unsupported. The 25,633-byte archive has 3,774 unique nodes and SHA-256
`ca53f9d4e4816704da2498f2adb8760bcf8ad340b21ead17664be3ef2285cde1`; no JSON
companion is tracked. Inventory is 403 archives: 365 full battles and 38
component calibrations. Original relic evidence is 183/224 (81.70%): 166 battle,
16 campaign and one native no-op owner.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-original-front-room-health-upgrade.mt2f
dotnet run --project src/ModelChecks -c Release -- --original-front-room-health-upgrade-only tests/fixtures/full-battle-original-front-room-health-upgrade.mt2f
dotnet run --project src/ModelChecks -c Release -- --battle-search-only tests/fixtures/full-battle-original-front-room-health-upgrade.mt2f
```
