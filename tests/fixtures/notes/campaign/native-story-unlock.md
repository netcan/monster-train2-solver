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
