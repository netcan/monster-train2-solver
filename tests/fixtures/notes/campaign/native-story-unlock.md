# Natural true-final unlock requirements · 2026-10-24

The completed natural Fragile bundle `campaign-completed-fragile.zip`
contains the native after-EndGame `native-story-progression.json`. Its
SHA256 is `f60f40223a547c6add8b899fdc85afad59714faa2e8fde2ea35b2830108c23d3`.
Reading that archived observation confirms story index0, the true-final
feature locked and none of the nine preceding additional hearts unlocked.
These are this profile's cumulative values, not combined totals from separate
isolated profiles or claims about a player's own save.

| Original heart | Native tracked value | Current | Required | Remaining |
| --- | --- | ---: | ---: | ---: |
| Pact | BattlesWonWithTrial | 0 | 7 | 7 |
| Life | PyreDamage | 120 | 475 | 355 |
| Malicka | NumChallengesWon | 0 | 1 | 1 |
| Wyngh | UnitsMoved | 155 | 225 | 70 |
| Fhyra | CoinsSpents | 1410 | 20000 | 18590 |
| Aquath | MagicPowerBonusDamage | 330 | 12000 | 11670 |
| Bogworm | DebuffsApplied | 116 | 3200 | 3084 |
| Time | CardsFrozen | 0 | 25 | 25 |
| Herzal | DamageDoneByTrainStewards | 1100 | 10000 | 8900 |

In the matching native source (MVID
`8fb07b96-f4db-4d2b-884d-c00536d6ccf4`),
`SaveManager.CheckForNewlyUnlockedPyreHearts` tests each original unlock
criterion and awards qualifying hearts. `TryToAdvanceStory` additionally
requires the current dialogue to have completed and the next story's original
condition to be met. `CoreDialogue_100_SeraphReunion_StoryProgressionData`
requires nine unlocked additional hearts and rewards `UnlockTrueFinalBoss`.
Earlier story rewards unlock challenge runs at two hearts and crew at six.
The three `DefeatedTrueFinalBoss` hearts are subsequent rewards, outside this
nine-heart prerequisite.

Natural acceptance must keep one profile's metagame progression between new
runs, complete the corresponding native activities and claim the original
story dialogues/rewards. Replaying one pre-battle checkpoint in fresh profiles
does not accumulate these nine metrics. Directly setting tracked values or
temporarily overriding the feature unlock remains controlled-mechanism evidence
only. A Standard victory does not by itself satisfy these thresholds.

The bundle already passed restoration of all 26 payload files; the table
above is a metadata read, with no battle rerun. For a fresh current native
observation use `Run-CampaignProgress.ps1 -StoryProgressionOnly -InputProfile`
with a restored profile. Natural unlock and subsequent true-final victory
remain open acceptance goals; existing controlled true-final model/player
pairs retain their separate scope.

## Retaining one profile across new runs · 2026-10-25

`Run-CampaignProgress.ps1 -NewRunFromProfile <idle-profile> -StopBeforeRing 1`
now clones the native post-EndGame profile, then starts a new run through the
original UI. It rejects active campaigns and combinations with resume, replay
or story-only observation. `Run-RandomCampaign.ps1` accepts the same option;
`-InputProfile` continues to mean resuming an unfinished campaign.

The restored completed Fragile profile was used to start native seed424245.
The driver follows the original SkipDialogue input and confirmation, allowing
the game's coroutine to mark the dialogue completed and grant any reward.
All16 previously recorded cumulative tracked values remained identical,
completed dialogues increased0→1 and the additional hearts remained0/9.
The source profile and player's real files remained unchanged. This proves
the cross-run entry works, not that the unlock thresholds or another campaign
have been completed.

`saves/campaign/campaign-metagame-new-run.zip` contains the new Ring1 entry,
44 hash-verified native payload/history files and the source-profile bindings.
SHA256 `248f0e98702a4b8feadd3a6f63a05fe1f6db7cee242a92ddf4eae5fdcfd162fa`.
The checker restores both archives, verifies the native new-run audit, idle
source hashes, preserved cumulative values, hearts, features and dialogues,
with no battle simulations:

```powershell
pwsh -NoProfile -File scripts/Check-NativeMetagameContinuation.ps1 `
  -CompletedArchive tests/fixtures/saves/campaign/campaign-completed-fragile.zip `
  -OpeningArchive tests/fixtures/saves/campaign/campaign-metagame-new-run.zip
```

### First natural battle after the retained-profile opening · 0.2.68

After fixing the lost card-type sequence reset, seed424245 Banished/Pyreborne
won Ring1 using the actual10000ms/two-worker player advice and an independent
frozen native replay:21 inputs,16 card plays,5 EndTurns, Pyre80. Both processes
match all state/forecast/RNG and9 train stages, with four failure counters zero.
The source used64.33s,23 publications,3 search metric samples,754 independent
card-mask contexts and181 pure captures. This is the retained-profile campaign,
not sample8 of the randomized clan sequence and not a whole-campaign clear.

Normal native rewards advance to Ring2, saveSHA256
`edf23a1fe8d61e2a8cef0ddc73c72d523718c0a92dbb397f21c6fe4cf3399188`.
`saves/campaign/campaign-metagame-ring2.zip` contains48 hash-verified files,
SHA256 `3521b609d5d49deb9322d639c0c5951fdfa16e61340fdb2fd415dc47d489c8d9`.
The native cumulative values survive and9 increase; UnitsMoved155→162,
StewardDamage1100→1174 and CoinsSpent1410→1465. Additional hearts remain0/9.

Append `-ProgressArchive <ordered checkpoint zip array>` to the command above
to verify each adjacent battle's input save/metagame hashes, unchanged seed,
clans, rank, complete native victory history and independent replay receipt.
The same check compares cumulative counters, hearts, features and dialogues
without battle simulations. Skipped/repeated boundaries are rejected. Actual
battle states remain covered by the separate two curated `.mt2f` archives in
`campaign/progression/full-battle-metagame-campaign-ring1-budget10000-*`.

Ring2 also passed actual10000ms advice and independent native replay:35 inputs,
28 plays,7 EndTurns, Pyre56,14 train stages per process; state/forecast/RNG and
four zero failure counters match. Native runtime132.65s,37 publications,
3 search metric samples,1498 independent mask contexts and293 pure captures.
The next native save is
`44e6ef19e0139783f648523fd4e70c004ab6dcdea55cb0c1dd3441e970aa3412`.
`campaign-metagame-ring3.zip` has52 hash-verified files, SHA256
`0ae5d64843e63088be6a9d89076155a07be860e5c9c7bf51e310a6f719c3941e`.
Checking Ring2 and Ring3 archives in order confirms UnitsMoved172,
StewardDamage1326, CoinsSpent1510, Debuffs120, MagicPower350 and PyreDamage144;
all previous counters, hearts, features and dialogues are retained. Hearts0/9.

Ring3 actual default advice loses after44 inputs,35 plays and9 EndTurns.
Its complete native state/forecast/RNG,18 train stages and four zero failure
counters pass; runtime197.13s,46 publications,4 search metric samples,
1798 independent mask contexts and365 pure captures. The source defeat remains
curated separately from the two earlier winning pairs; the pre-battle Ring3
checkpoint is preserved for offline strategy improvement, with no Ring4 claim.
An independent30s/4096-node-cap root search also predicts defeat after44 steps
(1250 expanded nodes); its complete state/RNG replay passes. This is a bounded
strategy counterexample, not a proof that the battle has no winning route.

### Retained-profile Ring3 improvement · 0.2.72

The iterative offline tool composes two improvements among28 complete
candidates, finding a41-input model win (Pyre37). Native validation exposed
the [masked outer-boss death statistics](../combat/outer-boss-death-statistics.md)
boundary. After that repair, the full source and independent frozen native
replay both win:33 plays,8 EndTurns,16 train stages, all roots/successors/
forecasts/RNG and four zero failure counters. Source runtime101.89s,155
independent card-mask contexts and83 pure captures. This is an offline policy
win, not default10000ms strategy acceptance; the44-input default loss stays.

The curated pair is
`campaign/progression/full-battle-metagame-campaign-ring3-refined-*`.
Native rewards/merchant/map continuation enters Ring4, with input save SHA256
`0ff1758a0468b8d49e00c434cc6e200528bfee13170ea5bbfb7356fa402908ed`.
`campaign-metagame-ring4.zip` contains56 hash-verified payload/history files,
SHA256`e72c2addd953e2f2b18de215b5ee8d1dd371872eed8dce7a0ff8c1fecdcd8e4a`.
Checking Ring2, Ring3 and Ring4 checkpoints in order preserves all16 earlier
counters, hearts, features and dialogues, with no battle simulations. Latest
UnitsMoved183,StewardDamage1408,CoinsSpent1660,Debuffs127,MagicPower410,
PyreDamage163. This profile resumes actual10000ms/two-worker advice at Ring4;
no natural true-final victory is claimed.

Ring4 actual10000ms advice produces a51-input native defeat (42 plays,
9 EndTurns): all complete states/forecasts/RNG,18 train stages and four zero
failure counters pass. Native runtime218.24s,53 publications,4 search metric
samples,1705 independent mask contexts and421 pure captures. The unchanged
pre-battle checkpoint starts at Pyre87 after normal recovery/rewards; the
loss is archived separately as `full-battle-metagame-campaign-ring4-budget10000-loss`.
Offline improvement needs only3 complete candidates and5.86s of search to
find a53-input model win at Pyre17; full independent export/replay completes
in16.52s. The complete source and independent native replay now both win:
44 plays,9 EndTurns,Pyre17,18 train stages,all roots/successors/forecasts/RNG
and four zero failure counters. Source runtime141.36s,201 independent masks,
107 pure captures,zero searches/publications; the53-input prediction is
separately bound to both native traces. The51-input default defeat stays.

Normal rewards/merchant/map continuation enters Ring5 at save SHA256
`190167d488c3e45f2773635449dc11fa2e2065a987f98ef83bf367322ef248f6`.
`campaign-metagame-ring5.zip` contains60 verified files,SHA256
`fe1f7eed827302f046fbfdf4c49767b68eca842171344789cea55cadeb7000e4`.
The ordered Ring2..5 checkpoint check passes without battle simulation and
preserves all16 old counters,hearts,features and dialogues. Extra hearts
remain0/9; actual10000ms/two-worker advice continues at Ring5.

Ring5 actual10000ms advice loses after58 inputs (49 plays,9 EndTurns).
Complete states/forecasts/RNG,18 train stages and four zero failure counters
pass. Runtime233.35s,60 publications,4 search metric samples,1532 independent
masks and477 pure captures. The curated default defeat remains separate.
The iterative tool finds a48-input model win at Pyre8 after three improvements
among9 complete candidates,zero unsupported:13.06s search,19.60s including
independent export/replay. The complete source and independent native replay
now both win:40 plays,8 EndTurns,Pyre8,15 train stages,all roots/successors/
forecasts/RNG and four zero failure counters. Source118.84s,183 independent
masks,97 pure captures,zero new searches/publications. The48-input prediction
is separately bound to both native traces; the58-input default loss stays.

Normal reward/merchant/map continuation enters Ring6 at save SHA256
`b90d38a7460c6a999a5c9f8c6cd3432450261942cd57e257689f97fa6a43ae2e`.
`campaign-metagame-ring6.zip` contains64 verified files,SHA256
`f63265c35364d9f1b461cacc43ea76fb7fd7a1a32c760d81851db5b52dc64a76`.
The ordered Ring2..6 checkpoint check preserves all16 earlier counters,hearts,
features and dialogues with zero battle simulations. Extra hearts remain0/9;
actual10000ms/two-worker advice resumes at Ring6. Curated Ring4 and5 refined
pairs use their frozen-policy gates,not actual-default search gates.

After the pre-discard RNG correction,actual0.2.75 Ring6 advice still loses
with62inputs/51plays/11EndTurns. Full state/forecasts/RNG,22train stages
and all four failure counters pass. The loss remains curated separately.
Offline improvementv2 completes58/58 candidates with four losing improvements,
442 unsupported alternative attempts and97.08s total. Resuming it asv3
completes59 of60 candidates with one further61-input losing improvement,
187 unsupported alternative attempts and106.75s total under a90s soft limit.
Both exported routes independently replay fully. These local improvements
do not prove impossibility; no losing model route is sent through redundant
native replays. A separate late30..60 batch completes99/99 continuations,
29 unsupported attempts and53.79s,no win. Its cross-floor spell/Harvest
boundary now passes two independent35-input native observations in0.2.79;
see [deferred Harvest](../bosses/spell-harvest-tower.md). Ring6 remains
unfinished and extra hearts remain0/9.

After that correction,another90s local search completes64 of65 candidates,
seven unsupported attempts and no improvement. Three inexpensive alternative
deployments under0.2.80 (floor0,floor1,and strength-first floor2) independently
replay to complete model defeats with62,44,and61 inputs respectively. None
is sent through a redundant native battle; these outcomes do not prove that
Ring6 is unwinnable.
