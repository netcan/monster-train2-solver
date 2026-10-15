# Inventory validation, 2026-10-15

## Campaign draft and Pyre health extension

Current inventory: 346 archives (313 battles, 33 calibrations), 113/224 original
owners (50.45%): 104 battle owners and nine campaign-interface owners.

Native component source:
`.probe-runs/full-battle-steward-once-20261015-200933-3c29997e/campaign-draft-calibration.mt2f`.
The host battle wins with 55 stages, one play, seven EndTurns and all four error
counters zero; Steam files remain unchanged. The component is schema 1, game
2.2.1, module MVID `8fb07b96-f4db-4d2b-884d-c00536d6ccf4`, 5239 bytes,
1137 unique nodes, SHA-256
`06cfd88a3aab87d6f2d76f3bbc0a18a5c687bd5dd62ee04705a6687f270134d4`.

Three unchanged originals are acquired and removed through SaveManager. Actual
DraftRewardData.GrantReward executes 840 candidate selections over five owner
phases, seven class masks, three rarity floors, two bypass gates and two seeds.
The model matches exact candidate IDs and Rewards RNG after each call. Positive
owner ablations produce 48 allied-class changes and 56 Rare changes; 168 explicit
class controls and 168 ignore-rarity controls pass. Native neutral fallback is
retained even without the NonClass flag. PyreHeartFather adds 25 maximum/current
health at low/near-full/full health; removal preserves raw HP while the visible
getter clamps to the restored maximum.

The authored ordered pool contains unchanged unlocked/DLC-eligible original
cards; rarity weights are flattened. The bounded claim excludes UI choice,
chosen-card grant and run rarity tickets. Scheduling of subsequent loading/UI
tasks is suppressed only after actual native candidate selection. Recorded
relic/deck IDs, raw/visible/max HP, frame and deterministic RNG match before/after;
native isolated-save run-stat side effects are outside this restoration claim.

Five affected fixtures pass general/focused gates, including the new component,
prior campaign and gold consumers, Steward battle and equipment growth battle:
`.probe-runs/model-checks-20261015-201404-b9f42f96/summary.json`.
All three workers pass with exit 0; this is not a complete 346-archive rerun.

| Frozen runtime binary | SHA-256 |
| --- | --- |
| Model | `2DD4DE35F34A8926F704350C2F147504AD888003A817FD3A9A829602AD6E03F8` |
| FixtureArchive | `5DFDD5BB6A9143F8B9E08CCB8C5D071FE0FA018251D47BD92AC9A63CD501A51E` |
| Checker | `2019AB2967F54D79E3008C34952BB11A40242345C6D4A53F41A2BB3C8699DDDC` |

## Equipment growth extension

Current inventory: 345 archives (313 battles, 32 calibrations), 110/224 original
owners (49.11%): 104 battle owners and six campaign-interface owners.

Native source:
`.probe-runs/full-battle-units-spells-and-junk-20261015-200021-84134976/full-battle.mt2f`.
Three unchanged original owners install +2 equipped slay, +4 equipped death and
front-position Deathwish Valor triggers. Fourteen complete birth/train/queue
transitions include seven friendly and seven enemy controls. Five births retain
exclusive trigger-instance IDs, including creation-time Deathwish registration.
Positive owner/installed-trigger ablations remove seven slay gear witnesses,
one equipped sacrifice gear witness and nine native PreCombat Valor-2 effects.
Two real entries and four losses install/remove the original unique upgrade;
original rearrangement spells provide those position changes. The authored
damage/heal carriers do not define Deathwish eligibility.

The battle wins with 42 stages, 24 plays, five EndTurns, Pyre 80 and all four
error counters zero. Independent root/mid-battle/16-branch policy replay matches.
Pure controls cover two equipment copies, parent equipment binding, permanent
card versus live host trait scaling, preview and parent immutability. Scaled
triggered equipment buffs remain rejected. AdvancedGrowth campaign run-stat
counters are outside this battle claim; isolated Steam files remain unchanged.

Earlier birth diagnostics without exclusive queue capture are excluded. A
whole-manager PreCombat diagnostic
(`full-battle-units-spells-and-junk-20261015-195706-fcb53e2d`) also observed
asynchronous destroyed-attacker and UI-selection changes and is excluded. The
accepted sample records the individual native status effects, matching complete
room/context state, alongside the complete decision/EndTurn replay.

Seven affected fixtures pass all general/focused gates and bounded search with
three workers and no reruns:
`.probe-runs/model-checks-20261015-200235-e73dae52/summary.json`.
This is an affected regression, not a complete 345-archive rerun.

| Frozen runtime binary | SHA-256 |
| --- | --- |
| Model | `B7D1C8719B6659683525EFD8FE2E760AD77FAE81F4EA09441A9D9CC9EA154255` |
| FixtureArchive | `EBEF13EB78D1C36028C992480DB080115CD212DA0ED2D0F2D4A1AB3AB2197983` |
| Checker | `3515938422D46BA42316A58132279FE8637D314A0ECA6079E97AF86A424FE973` |

## Conduit extension

Current inventory: 344 archives (312 battles, 32 calibrations), 107/224 original
owners (47.77%): 101 battle owners and six campaign-interface owners.

Native source:
`.probe-runs/full-battle-units-spells-and-junk-20261015-193707-ffc54f39/full-battle.mt2f`.
Three unchanged originals add Conduit through first-summon, slay and equipment
callbacks. Fourteen complete birth/train/queue transitions include four first
summons, three later player summons and seven enemy controls. Nine slay-stage
and two equipment witnesses disappear when removing the corresponding owner
and installed triggers. Twelve card plays carry actual Conduit upgrades.
The battle wins with 42 stages, 21 plays, five EndTurns, Pyre 80 and zero error
counters; Steam files remain unchanged. Independent root/mid-battle/16-branch
policy replay matches.

The model follows native per-unit card tracking, temporary upgrade damage/heal
values, trait multipliers, draw/discard, selected-room refresh and reorder.
Pure controls cover unselected-room stack-removal reapplication, preview,
death/removal, missing eligibility and immutable parallel branches. Physical
Conduit movement is rejected pending independent calibration. Snapshot-only
normalization replaces random tracker GUIDs with canonical unit IDs without
changing native getters, comparisons or effects. The first general regression
identified a raw GUID left in the independent mask view; that capture and failed
run `.probe-runs/model-checks-20261015-193435-7197d683` are excluded. The accepted
recapture includes the independent native mask view normalization.

Six affected fixtures pass all general/focused gates and bounded search with
three workers and no reruns:
`.probe-runs/model-checks-20261015-193908-0abe8803/summary.json`.
This is an affected regression, not a complete 344-archive rerun.

| Frozen runtime binary | SHA-256 |
| --- | --- |
| Model | `1817D0F44F9462B41812D69B4635F9F769726C125C4596CEA2CA8EFE358703DD` |
| FixtureArchive | `7044FB864F59407B22DB7579AFAE4B6D737A9D422430C928FDFC7FDC3219C30F` |
| Checker | `2F0FE2A12C17864FFB9251CD645C45B596875B9316F8D8DC895D64F9A1539399` |

## Shroom Decay extension

Current inventory: 343 archives (311 battles, 32 calibrations), 104/224 original
owners (46.43%): 98 battle owners and six campaign-interface owners. Six affected
fixtures pass general/focused gates with three workers and no reruns:
`.probe-runs/model-checks-20261015-190850-7109024f/summary.json`.
This is an affected regression, not a complete inventory rerun.

Native source:
`.probe-runs/full-battle-units-spells-and-junk-20261015-190619-2a1be001/full-battle.mt2f`.
Unchanged `ShroomsGainSapDecay` installs its original Shroom-only summon trigger.
Fourteen complete birth/train/queue transitions include seven enemy and four
non-Shroom controls. Five native enemy status witnesses disappear when removing
the original owner and its installed triggers. Three later combat stages deal
actual Decay damage. The policy retains Shrooms until enemies occupy a legal
room; independent root/mid-battle/16-branch replay uses that same documented
selection rule rather than recorded choices. The battle wins with 57 stages,
19 plays, five EndTurns, Pyre 79 and all four error counters zero; Steam files
remain unchanged. Bounded serial/parallel search passes.

Native status data fixes Decay at three damage per stack in the post-combat
poison stage, followed by one-stack end-turn removal. Deployment damages without
natural stack removal. Pure checks cover armor and immutable parent state.
Earlier diagnostic battles with empty summon targets are excluded despite their
zero differential counters. The proposed two Conduit owners remain pending.

| Frozen runtime binary | SHA-256 |
| --- | --- |
| Model | `DCF914880DB75AAAC5266099E9439218F00CE42E5781562B1884A2D2D3CA1FDA` |
| FixtureArchive | `3A063EDA51EAE850E1C80687D4E299CBEF500F0348E62F0766AE7CC5998C6473` |
| Checker | `515B04931F729DC879ED40B9E82093EAAE6FE3887A3BD67FA3E282D98D2AF9A5` |

## Spell-play threshold extension

Current inventory: 342 archives (310 battles, 32 calibrations), 103/224 original
owners (45.98%): 97 battle owners and six campaign-interface owners. Eight
affected fixtures pass all general/focused gates with three workers and no
reruns: `.probe-runs/model-checks-20261015-184842-12b4aaf1/summary.json`.
This is an affected regression rather than a complete inventory rerun.

Native source:
`.probe-runs/full-battle-units-spells-and-junk-20261015-184639-1ba6990c/full-battle.mt2f`.
Original `IncreaseMagicPowerOnSpellsPlayed` and `DrawCardWhenConsumePlayed`
definitions are unchanged, including the latter's required Exhaust trait. Three
zero-cost authored spells provide two non-consuming controls and one Consume
carrier. Five third-spell activations include deployment; 65 owned spell damage/
heal increments, one real Consume draw and 15 non-Consume controls match. The
natural battle wins with 49 stages, 26 plays, six EndTurns, Pyre 80 and all four
error counters zero. Root/mid-battle/16-branch policy replay and bounded serial/
parallel search match. Steam files remain unchanged.

Native `RelicState.OnCharacterAdded` and `OnCharacterStatusEffectApplied` evaluate
the card-play effects' conditions even when their specialized callback is a no-op.
A temporary native call-stack trace identified the latter refreshing deckStats
after generated Junk, during Valor's Armor application. The model now preserves
this cache transition without recording or triggering the relic. Pure controls
also cover failed conditions before preview gating and immutable parent state.
The diagnostic patch is removed from the accepted capture. Earlier diagnostic
captures with nonzero mismatch counters are excluded.

Catalog schema 6 adds required card traits to all 662 definitions; enumeration
keeps native frame/RNG unchanged. The exact 224-owner inventory uses the catalog
beside the accepted battle. Schema 5 omitted traits and must not establish a
trait-filter acceptance claim.

| Frozen runtime binary | SHA-256 |
| --- | --- |
| Model | `1F9FE0D930721D353BD5EE7BEC10CCDFD135D6EA43A227F187AF0887B0D63BCE` |
| FixtureArchive | `74B29A7EAFE7818B45EE899E000ACE6F7ACB8A33744350890D1F83E5F81BF779` |
| Checker | `09CC7F0867EE3787856EB85D10771F47704E1E4639AD19777563203F9F8FF077` |

## Opening corruption extension

Current inventory: 341 archives (309 battles, 32 calibrations), 101/224 original
owners (45.09%): 95 battle owners and six campaign-interface owners. Seven
affected archives pass all general/focused gates with three workers and no
reruns: `.probe-runs/model-checks-20261015-183147-ab29f41a/summary.json`.
This is an affected regression rather than a full inventory rerun.

Native source:
`.probe-runs/full-battle-units-spells-and-junk-20261015-182923-965483a5/full-battle.mt2f`.
Unchanged `IncreaseMaxCorruption`, `UnremovableCorruptionTop` and
`CorruptionBattleStart` run naturally on deployment. The first two callbacks
are start-battle effects; the last is post-opening-draw. The ordered initial
manager chain and each individual callback's four-room corruption state match.
All room maxima become 5, top-floor current/permanent becomes 3/1, and the
remaining rooms become 2/0. Only permanent corruption notifies once. There are
no native unit CorruptionAdded triggers at these initial boundaries; unsupported
callback combinations remain guarded. Pure tests cover phase/preview skips,
owner ablations and immutable sources. All seven EndTurns retain the resulting
room values, proving no extra first-combat or later-turn application.

The game wins: 60 stages, 21 plays, seven EndTurns, zero four error counters,
unchanged Steam files. Direct binary: 33,232 bytes, 5,332 nodes, source length
zero, SHA-256 `e63d17b24ebaf007c72a52ecb98ca373afafafa93cf0ca88c39fe9d93fe98226`.
The unified start-battle dispatcher keeps energy and corruption in native
owner/effect order; the original first-combat energy archive remains exact.

| Frozen runtime binary | SHA-256 |
| --- | --- |
| Model | `4A671013164A4DD7E6C5A7A2C11DC2B24700A743B0F58D740F2D33486325E002` |
| FixtureArchive | `DFD2B89DA6B0B9924474B95422E097699E63C3E805A1B3ECCD26AAF3D12F9767` |
| Checker | `6A1CDA2BF52C7B282A78D6D553CD453E8822731F990AEE0D26B78EDA7FE63BB0` |

## Campaign consumer extension

Previous inventory: 340 archives (308 battles, 32 calibrations), 98/224 original
owners (43.75%): 92 battle owners and six campaign consumer owners. Four
affected archives pass with three workers, all exit codes zero and no reruns:
`.probe-runs/model-checks-20261015-182410-a2e07d5d/summary.json`.
The selection is the new campaign calibration, old gold reward calibration,
original forge conservation and dragon-hoard battle; this is not a full suite.

Native source:
`.probe-runs/full-battle-steward-once-20261015-182118-3767d576/campaign-relic-calibration.mt2f`.
Its host battle wins with zero four counters and unchanged original Steam
files. All six original owners are acquired and removed through SaveManager;
their definition IDs, effect classes, parameters, None subtypes and lack of
conditions are independently checked. The actual consumer observations are:

- 80 `BalanceData.GetUpgradeSlots` queries: baseline, Spell only, both owners,
  Monster only, removal; Monster/Spell/Equipment/Blight and modifiers -5/0/1/20.
- 108 `MerchantGoodState.GetCost` prices: original -0.25 all-cost modifier,
  purchase schedules, two reward classes, 0/10/50% merchant discounts, Purge
  exclusion and final rounding, with baseline/removal controls.
- 66 actual `GoldRewardData.RewardValue` and `GrantReward` observations:
  original +50% trial modifier, trial flag on/off, positive/nonpositive amounts,
  baseline/removal, and exact displayed/granted gold after reward rounding.
- Six real hoard-cap acquisition/removal transitions: both original +3/+2
  owners at starting caps 0/max-1/max, upper bounds and amount truncation.

Recorded relic IDs, gold, cap, hoard amount, deck IDs, frame and RNG are equal
before/after. Actual reward grants execute native run-stat side effects within
the isolated save; complete save restoration is not claimed. Campaign consumer
evidence is distinguished from full-battle effect integration; these classes
are not silently permitted in battle branch validation.

The direct binary archive is 3,514 bytes, 507 unique nodes, source length zero,
SHA-256 `e0a9b3e9e51ff2d6a4e6ddbb24f8ab871cc304b9e5c177b063d03e3fdf210378`.
The refreshed 224-owner inventory separates 92 accepted-native-battle, six
accepted-native-campaign and 126 pending entries.

| Frozen runtime binary | SHA-256 |
| --- | --- |
| Model | `4781018AF50FB211D0012A6F9B5D0E4906E3735B518F55CF351848DA709AE279` |
| FixtureArchive | `E5248285D936F1C42E251D9FDB09F73EC3E35F692E1C3065039EEBA22A053C2C` |
| Checker | `68D351D8D963BA2611A45BCA566F7543B1D08769EFC978D10C7BF4DB0896E1FD` |

## Forge conservation extension

Previous inventory: 339 archives (308 battles, 31 calibrations), 92/224 original
owners (41.07%). Five affected archives pass with three workers and no reruns:
`.probe-runs/model-checks-20261015-181708-5484b7be/summary.json`. This is not a
complete 339-archive rerun.

Native source:
`.probe-runs/full-battle-units-spells-and-junk-20261015-181515-5141e8c9/full-battle.mt2f`.
The original `SteelStockpile` flat cap remains 10. Four actual
`PlayerManager.OnCombatEnd` calls exercise 0/5/10/17; removing the owner resets
17→0, then reacquiring it leaves the actual battle with 17. All seven EndTurns
retain 17, including the terminal battle snapshot. Export waits for the natural
post-victory player cleanup, where 17→10 with one relic notification. All six
complete callback contexts, moon reset, notification counts and input isolation
match. Pure cases additionally check percentage initialization, unknown modes,
first eligible owner, conditional notification/consumption, preview and bounds.
The game wins with Pyre health 66, 60 stages, 21 plays, seven EndTurns, all four
error counters zero, and unchanged Steam files. Direct binary archive:
33,171 bytes, 5,314 nodes, source length zero, SHA-256
`118fea00a50e61e3b1ee64e951d06ff938970093afe4780e0f4b3286bcdd68f9`.

| Frozen runtime binary | SHA-256 |
| --- | --- |
| Model | `93DB0E1157715CB22E7B57048793FB24873CCBDD3A5C5C79DE398488B5007AF9` |
| FixtureArchive | `6F2BDC5131050F0353E983F5094E2F19129813D17E4F6E3D55E640F646918D83` |
| Checker | `47BB19394B2A0473E1A7617768CC913D1FAAB46E251AC7F28219C2C54946D375` |

## Healing and first-combat energy extension

Previous inventory: 338 archives (307 battles, 31 calibrations), with 91/224
original collectible owners (40.63%). Two new battles and six affected older
archives pass the current general/focused gates, independent complete policy
replay, mid-battle roots, immutable branches and bounded serial/parallel search.
This is an affected-fixture regression, not a rerun of all 338 archives.
Summary: `.probe-runs/model-checks-20261015-181031-83a8154d/summary.json`.

Native healing source:
`.probe-runs/full-battle-units-spells-and-junk-20261015-180525-27638795/full-battle.mt2f`.
The unmodified `IncreaseSpellHealing` owner adds healing to Spell cards without
checking `CanScaleWithMagicPower`. Twenty-two manager refreshes exactly match
card state, dispatch returns/order, relic state and statistics. Detached original
scalable/non-scalable Spells and Monster/Blight controls, an isolated stronger
trait carrier, reset/repeat and owner ablations pass. A synthetic damage-then-heal
carrier supplies actual healing evidence without modifying the original relic.
Later policy plays also exercise a damage-killed target and an empty room.
The game wins: 62 stages, 24 plays, seven EndTurns, zero four error counters.
Archive: 35,541 bytes, 5,785 nodes, SHA-256
`81ecefc25b3d3bc5d21bbd69eaca2ab9563198685d846ce34477d6dc4b8051e0`.

Native first-combat energy source:
`.probe-runs/full-battle-units-spells-and-junk-20261015-180756-b70c6dc2/full-battle.mt2f`.
The unmodified `OneEmberFirstTurn_DH` has native amount +2 and disallows
deployment. Acquisition does not add energy. Its single natural callback runs
on turn one with zero notifications. Model integration follows initial enemy
spawn and precedes the Spawning RNG reset, PreCombat and normal energy income.
Deployment/owner-ablation controls and all seven complete EndTurns pass.
The game wins: 61 stages, 23 plays, seven EndTurns, zero four error counters.
Archive: 33,624 bytes, 5,474 nodes, SHA-256
`9389f51971af3275850362950c39673c768ce3d02ac8c7f23e8acc6b0b832836`.
Both archives have direct binary provenance, source length zero, build 2.2.1,
the catalog's native module MVID, and unchanged original Steam files.

Frozen binaries for the eight-fixture run:

| Runtime binary | SHA-256 |
| --- | --- |
| Model | `DD14198A27CF9BA991E645DEEDAFA3A4DA2D5E4C9B15DEDBDB57662B490EB7FD` |
| FixtureArchive | `99DEA27564B943461E8BACFC10C3F3664452C85000A1D5382D04F666664FBE7E` |
| Checker | `38B1EF7132476CC6E41B96749A1A6D097744834317D95D7A66C00F34B7E64C4F` |

## Upgrade-count relic extension

The previous inventory was 336 archives (305 battles and 31 calibrations), with
89/224 original collectible relic owners (39.73%). This extension validates the
new upgrade-count battle and six affected older archives with the current
model/checker; the complete 335-archive baseline below used the earlier model.
It is not a claim that all 336 archives were rerun after this extension.

Native source:
`.probe-runs/full-battle-units-spells-and-junk-20261015-174325-66c763b4/full-battle.mt2f`.
The unchanged `GiveSpellStatsForUpgrades_DH` and `GiveUnitStatsForUpgrades_DH`
definitions retain their original parameters and the spell owner's complete
`DevilishDetails` exclusion mask. The isolated game wins with Pyre health 80:
52 stages, 20 card plays, six EndTurns, 44 exact manager refreshes and zero
`CaptureFailures`, `Mismatches`, `Unsupported` and `Pending`. The launcher checks
that original Steam files remain unchanged.

The decompiled `RelicEffectModifyCardAttack`, `RelicEffectModifyCardHealth` and
`CardUpgradeState.ShouldShowIcon` implementations determine the rules. Permanent
visible upgrades give +3 each; hidden/no-icon upgrades and temporary upgrades do
not count. Visible region upgrades count here, unlike the separate upgrade-mask
counter. Counts 0/1/2, nonmatching card types, native excluded-pool rejection,
reset/repeat and individual owner ablations pass. The upgraded original Train
Steward and damage spell are played, with a later spell redraw retaining +6.
Cardless source-card scaling has pure boundary checks rather than a native
cardless witness in this capture.

Only the initial two witness plays are marked as scenario preparation. All
remaining 18 policy plays and six EndTurns run as one independent chain, with
a second mid-battle root and 16 immutable branches. Every native manager call
compares complete card state, dispatch/filter order, relic state and statistics.
The standard selected-fixture gates also exercise bounded serial/parallel search.

Curated archive `full-battle-original-upgrade-count-relics.mt2f`: 33,297 bytes,
5,417 unique nodes, direct-binary source length zero, SHA-256
`a601b943b86b373aa03c7e8a50b28ae3915684dc82a42bf37f3302285d11e7ec`.
All 336 archive lengths and hashes match the manifest; the owner table has 89
distinct entries. Regression evidence is
`.probe-runs/model-checks-20261015-174540-cec0ed4d/summary.json` (seven selected
fixtures, three workers).

| Runtime binary | SHA-256 |
| --- | --- |
| Model | `B97CCAE8E43F6AA5A34B80B098364F0400E5A363388476BA6AC6185E821678C1` |
| FixtureArchive | `9C875F79538C33A30367EA1E6A29694E868460FC109FE9341A72D6AD45AB3168` |
| Checker | `11E302F2FCD76A0C16BBC9212062B7689644753092D78AA674A654B211C02410` |

## Previous complete 335-archive baseline

All 335 baseline archives pass their curated general/focused regression gates:
304 full battles and 31 component calibrations. All 30 historical recordings
that played on disabled floors have fresh native replacements, preserving
their original scenarios and capture flags. Each replacement has fresh decision
snapshots, `NativeWon=true`, zero `CaptureFailures`/`Mismatches`/`Unsupported`/
`Pending`, and direct binary provenance without a JSON source. Its complete
independent policy and 16 immutable branches pass. The native workers verify
that original Steam files remain unchanged.

The final local audit checks all 335 archive lengths and SHA-256 values against
the manifest, the exact disjoint union of passed worker selections, terminal
exit records, no whole-policy skips, and all 30 replacements' freshness,
node counts and zero counters. It finishes with `COMPLETE-INVENTORY PASS` in
`.probe-runs/complete-inventory-20261015.json`.

| Accepted fixtures | Workers | Local evidence |
| --- | --- | --- |
| 306 | 11 | `.probe-runs/final-306-passed-shards-20261015.json` |
| 2 | 2 | `.probe-runs/model-checks-20261015-162633-4ece3f16/summary.json` |
| 27 | 8 | `.probe-runs/model-checks-20261015-165149-9b0688bf/summary.json` |

The first entry contains only the eleven fully passed shards of
`.probe-runs/model-checks-20261015-160823-d3d350bf/summary.json`.
That original 333-fixture run remains **Failed**: worker 10 rejected the fresh
original-resource recording because its specialized checker compared retired
terminal UI card views (six predicted definitions versus one native definition).
All 27 fixtures in that shard are rerun in the third entry, including the
resource recording. The two final recaptures in the second entry were excluded
from the original bulk selection. These three accepted selections have no gaps
or duplicates.

Commit `7983022` changes only `OriginalResourceRelicBatchChecks.cs` within the
model/archive/checker sources since model commit `d526e7c`. Its terminal
comparisons now use the same projection as the generic action/turn checker:
retired terminal `PlayRules` are excluded, while combat, resources, piles and RNG
remain compared. Nonterminal decision snapshots remain strict. The fix passes
the eight unchanged original owners/all 20 effects, eight real acquisitions,
48 energy bodies, and independent root/mid-battle policy replay. The audit
also reads the scenario of every reused old-checker fixture and rejects any
that could enter this changed resource-specific path.

| Runtime binary | SHA-256 |
| --- | --- |
| Model, identical in all accepted runs | `14984F85129B9A510DA25E4C08F298769A88E3484F412AAA323EF4E4C177F1A6` |
| FixtureArchive, identical in all accepted runs | `D131285A256F9182847716720122DCB844067F42C05CAC3F25CBC1FD6268F79F` |
| Checker used for the 306 + 2 unaffected fixtures | `3A23AA92DBC2259B465C063BC3448B7F3097454BEA9FA2AC2F6197FDDDCF9771` |
| Corrected checker used for all 27 rerun fixtures | `8F5E50238C146CB7CF3E7BDE0455F25CD1F863283D40BD745CD26E6210E52822` |

The parallel runner still invalidates binary changes during a run. The checker
switch occurs only after the original workers exit; only the checker is
replaced, retaining the exact tested model/archive binaries. The combined audit
explicitly permits this reviewed resource-only checker delta. It does not claim
that all 335 fixtures ran under one checker binary.

To repeat the complete curated regression from source:

```powershell
./scripts/Check-Models.ps1 -Workers 8
```

Historical archives may exclude the known-stale room `Enabled` field, while
fresh decision snapshots retain that comparison. Whole-policy legacy skips are
removed. This validates the recorded paths; unchanged original relic-owner
coverage remains 87/224 (38.84%), and full game/optimal solver coverage remains
unfinished.
