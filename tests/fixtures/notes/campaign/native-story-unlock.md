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
