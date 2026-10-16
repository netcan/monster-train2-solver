# Original relic coverage evidence

As of 2026-10-16, **142 distinct original collectable relics / 224 catalog entries
(63.39%)** have accepted native evidence: 126 have full-battle effect evidence,
fifteen have campaign consumer interfaces validated, and one is an explicit
native no-op with seeded baseline equivalence. Except for that no-op, this counts
unchanged relic definitions whose effects actually ran, including original filters and
conditions. Test cards, unit triggers and carrier subtypes may be authored to
exercise those effects; the claim covers the recorded paths, not every possible
combination. Catalog enumeration and other owners of a supported effect class
do not count as validated relics. `RelicEffectAddTrigger` appears on 26 catalog
entries; 23 now have accepted original-owner effect evidence.
`RelicEffectAddStatusEffectOnStatusApplied` appears on eight catalog entries;
all eight now have accepted original-owner evidence. Both catalog owners of
`RelicEffectPreventStatusEffectDecay` have accepted evidence for their original
Rage and Frostbite consumers. All four catalog owners of
`RelicEffectModifyStatusMagnitude` now have accepted evidence. Unknown status
families and other effects on the remaining owners still require modeling and
native proof.

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
The latest battle validates FirstRoomCardOnAllFloors: an ordinary-card control
does not trigger it, the first native capacity attachment is copied to all
non-Pyre floors, and a later attachment is blocked by the unchanged once-per-
battle condition. Native room and unit snapshots also match attachment upgrade
propagation. The accepted inventory is 363 archives: 325 battles and 38 component
calibrations, with no JSON companions.
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
included in the 103/224 owner count. The `AllUnitsMultistrike` archive
validates a mutator, not a collectible relic, and is also excluded from that
owner count.

| AssetKey | DataId | Accepted archive | Actual effect witness |
| --- | --- | --- | --- |
| `GoldOnDeath` | `265d9fb6-934f-4b7d-ba12-d3a06c48b610` | [unit resource relics][unitResources] | Original +5 reward gold on actual friendly death: two positive native callbacks. Both Tomb and ordinary sacrifice qualify; six enemy deaths do not. Gold's living-death override is preserved separately from Ember's strict dead-only gate. |
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

## Campaign interface evidence

These fifteen unchanged original owners are validated by their actual native
campaign consumers, with baseline and removed-owner controls. This table is
separate from the 126 battle owners. It does not claim whole-battle integration
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
| [normal smelt][smelt] | 103 | 22 | 7 | 55 | Original +1 Ember on a cost-2 smelt |
| [zero-cost smelt][smeltZero] | 103 | 21 | 7 | 56 | Original +1 Ember with zero base smelt cost |
| [bulk smelt][smeltBulk] | 103 | 1 | 7 | 52 | Four consumed cards, 3 Forge and four extra Ember |
| [original Tinkermaster Mix][tinkermasterMix] | 103 | 21 | 7 | 62 | Four native Mix drafts with three candidates each across two Mixology casts |
| [original Progress Mix][progressMix] | 103 | 21 | 7 | 60 | Six native Mix drafts, two conditional follow-up rounds across separate turns |
| [original card-type cost][cardTypeCost] | 103 | 24 | 6 | 50 | Ten modifier calls, two original zero-Ember self-purge plays, generated Junk |
| [original Ember conservation][conserveEmber] | 103 | 21 | 7 | 62 | Seven conservation queries, two banked turns, later positive payments and terminal Ember retention |

The checks that enforce these observations are
[TriggerCountChecks.Native](../../src/ModelChecks/TriggerCountChecks.cs),
[RelicSpawnStatusChecks.Native](../../src/ModelChecks/RelicSpawnStatusChecks.cs),
[RelicCardModifierChecks.Native](../../src/ModelChecks/RelicCardModifierChecks.cs),
[OriginalCardTypeCostChecks.Native](../../src/ModelChecks/OriginalCardTypeCostChecks.cs),
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
[openingDraw]: full-battle-original-opening-draw-relics.mt2f
[turnDraw]: full-battle-original-turn-draw-relics.mt2f
[purgeGold]: campaign-purge-gold-calibration.mt2f
[phaseUpgrades]: full-battle-original-unit-phase-upgrade-relics.mt2f
[deckCopy]: campaign-deck-copy-calibration.mt2f
[nullRelic]: null-relic-equivalence-calibration.mt2f
[relicCountAttack]: full-battle-original-relic-count-attack.mt2f
[moreSpikes]: full-battle-original-more-spikes.mt2f
[stealthSlay]: full-battle-original-stealth-slay.mt2f
