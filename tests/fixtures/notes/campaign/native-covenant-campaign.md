# Native cumulative Covenant campaign

Seed424242's original rank-two startup was resumed through native rewards,
drafts, map choices and merchant purchases. Difficulty was originally selected
by native RunSetup in a disposable profile; no earned-unlock claim is made.
Progression does not jump distances, substitute encounters or author rewards.

`campaign-native-covenant-02-ring2.zip` preserves one original victory, Pyre52,
23 cards, original cumulative owners and `wasDevCheatUsed=false` at BattleIntro.
Its SHA256 is `95df49709d90284046c17ea966092977ca906f74257d8d198b784797cf4334a2`.
All six payloads verify on restore, including native choice/hash ancestry.
Source: `campaign-ring2-20261023-134638-2ebf1cc3-before-ring2`.

The actual player advisor completes natural `Level2BattleJunker` with 27 paid
plays and seven EndTurns, winning with Pyre52. Its source profile is
`full-battle-units-spells-and-junk-20261023-134733-76da328b` (341.14 seconds).
The independent fresh player replay is
`full-battle-units-spells-and-junk-20261023-135351-bc70fc9d` (133.59 seconds).
Both processes exit normally with unchanged protected files and zero capture,
mismatch, unsupported and pending counters. All 34 inputs, complete published
forecasts, full state/RNG, 55 room stages and thirteen train stages match.
The source has 37 publications and451 pure captures; the replay searches no
nodes, publishes no suggestions and has115 pure captures. Native UI passes.
The configuration uses six turns, a10000ms soft search budget and tactical
candidates; after finding a victory it retains the verified suffix.

The focused checker verifies prior native victories, the complete ordered
cumulative owner definitions at every input/successor, an unmodified resumed
scenario, actual player UI and independent winning full-state/RNG replay.
The fresh replay additionally observes native difficulty and original starting
level; the earlier source predates these additive read-only fields.

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --player-native-covenant-campaign-pair-only tests/fixtures/campaign/covenants/full-battle-player-native-covenant-campaign-02-ring2-search.mt2f tests/fixtures/campaign/covenants/full-battle-player-native-covenant-campaign-02-ring2-replay.mt2f 2 1
```

`Run-NativeCovenantPlayerPairs.ps1 -Battle Campaign` accepts a single rank,
native InputProfile and BattleDistance. It checks the actual native difficulty,
seed, complete prior victory history and BattleIntro before launch, validates
both loaded save hashes and final native acceptance summaries, then runs the
focused checker. Wrong-rank and wrong-ring preflights reject before launching.
The existing controlled modes retain their defaults. Longer searches use
`-PlannerHorizon` and `-PlannerTimeLimitMs` explicitly.

`-DefaultAdvice` selects the actual ordinary-campaign defaults: three turns,
3000ms, top-floor preference and tactical seed off. It rejects other budgets
and controlled encounter modes, and its separate
`--player-native-covenant-campaign-default-pair-only` gate verifies the recorded
settings before accepting the original campaign history, both victories,
player UI, complete forecasts/state/RNG and independent frozen native replay.
Wrong-rank inputs reject before launch; older extended-budget/tactical pairs
reject as default evidence. The soft budget remains distinct from total response
time and can produce explicitly marked complete shorter windows.
The default runner retains a losing source as diagnostics and stops before
spending another native process on its replay.

A separate fixed-policy branch from the same Ring2 input won Rings2–5 with
Pyre47/47/67/59, then lost Ring6. The driver therefore correctly reports its
overall checkpoint request failed, despite normal exit and unchanged protected
files. Its retained pre-Ring6 checkpoint contains all five original wins and
34 cards. `campaign-native-covenant-02-ring6.zip` preserves that input and all
nine payloads verify on restore; SHA256 is
`997deb4337b717015349d31c4866005161e36f8934b5a964dc0d128a89eb4b37`.
Source: `campaign-ring8-20261023-134854-d165977b-before-ring6`.

The first actual player attempt from that unmodified checkpoint,
`full-battle-units-spells-and-junk-20261023-135449-65294df5`, stops before its
first recommendation: `Level6AngelDuo_RallyAttackIncantHealth` was rejected by
the outer/queued-boss capture guard. That guard is now calibrated for the exact
original definition; the initial failed attempt is not an accepted archive.
The five prior victories and the failed sixth battle
are kept distinct; no cumulative campaign completion is claimed.

The same Ring6 input now has accepted original Angel Duo birth and ordinary
no-card battle coverage, with complete state/RNG and zero native failures.
Original cross-floor Incant health mutation and Slay attack buffs also now have
accepted native fixed-policy coverage. The forced-Relentless global primitive
also matches five native full-state controls, including scoring counters and
full restoration before the accepted no-card battle continues. The same input
also has accepted original inner/outer death controls and complete surviving-boss
continuations. Both-live companion Relentless/Pyre phases now also have an
accepted full native control, complete branches and dedicated component checks.
Actual player advice and fresh frozen replay also now pass from that same
unmodified checkpoint: 44 inputs, 36 card/ability actions, eight EndTurns, 71 room
and 16 train stages per process. Full forecasts/state/RNG, actual UI and all five
original prior victories match; both processes exit normally with protected
files unchanged and all four failure counters zero. The three-turn/three-second
budget produces two losses, not natural campaign completion. The archives are
`full-battle-player-angel-duo-search.mt2f` and
`full-battle-player-angel-duo-replay.mt2f`; see [Angel Duo](../bosses/angel-duo.md).

## Natural rank-three Ring2 with actual defaults

The native rank-three startup victory resumes through original rewards, draft,
map and merchant choices to the second battle, without changing difficulty,
encounters, cards, counters or prior history. This extends the original
RunSetup-selected difficulty; it makes no earned-unlock claim.
`campaign-native-covenant-03-ring2.zip` preserves the one-win, 23-card input;
SHA-256 is `549419999abd1ad1cd620b68a07c637fad2337bcc305b03bd07aac966ee26252`.
All six payloads verify on restore. The native save SHA-256 is
`347e208be03388532a524c15a01b74e2de18b21a82330dfe5a5059383a607360`.

Source `full-battle-units-spells-and-junk-20261024-083220-494fd62b` and replay
`full-battle-units-spells-and-junk-20261024-083618-e4010bd0` both win original
`Level2BattleJunker` at Pyre52 after 28 inputs: 21 plays/abilities and seven
EndTurns. Native durations are 236.47/118.49 seconds. Both processes exit
normally, preserve protected originals and have all four failure counters zero.
All inputs, roots, complete forecasts, successors, state/RNG, 56 room stages
and thirteen train phases per process match. All three cumulative original
Covenant owners and the preceding native victory remain intact.

Settings are the actual three-turn/3000ms/top-floor defaults, tactical seed off;
every consumed forecast completes all three requested turns or a terminal win,
without a shortened window. The source consumes five searches and reuses
sixteen ordinary/seven winning continuations. Thirty publications each have
a fresh full-root check; captures are 240/96 with no purity failures. Replay
searches/publishes nothing. The deadline remains soft, not an end-to-end promise.
This establishes this natural higher-rank battle's default victory, not a
completed rank-three campaign.

Curated archives use `full-battle-player-native-covenant-campaign-03-ring2-default-`
with `search.mt2f` and `replay.mt2f`. Their independent gate is:

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --player-native-covenant-campaign-default-pair-only tests/fixtures/campaign/covenants/full-battle-player-native-covenant-campaign-03-ring2-default-search.mt2f tests/fixtures/campaign/covenants/full-battle-player-native-covenant-campaign-03-ring2-default-replay.mt2f 3 1
```

Reproduction uses `Run-NativeCovenantPlayerPairs.ps1 -Rank 3 -Battle Campaign
-BattleDistance 1 -InputProfile <restored-profile> -DefaultAdvice -SkipBuild`.
Wrong-rank native input and the older tactical/extended-budget pair both reject
before they can be called default evidence. No full regression or Mac package
is run for this batch.
`Check-PlayerAdvisor.ps1 -Case NativeCovenantCampaign03Default -SkipBuild`
checks both registered archive hashes and this focused pair gate.

## Natural rank-three Ring3 with actual defaults

Original rewards/map progression from the accepted Ring2 replay preserves
rank3, seed424242, two original wins and27 cards at the unmodified
`Level3BattleHeavyHitter_SiphonRage` encounter. The first no-advice guard and
the narrow native birth calibration are documented in [Arkion](../bosses/arkion.md#natural-cumulative-covenant3-rage-birth).
`campaign-native-covenant-03-ring3.zip` restores all nine native payloads.

After that repair, source `full-battle-units-spells-and-junk-20261024-084734-c8a4d32b`
and replay `full-battle-units-spells-and-junk-20261024-085335-04143890` both win
at Pyre52:36 inputs,27 card/ability plays and nine EndTurns. Native durations
are359.24/251.35 seconds. Both exit normally, preserve protected originals and
record four zero failure counters. All inputs, roots, complete forecasts,
successors, RNG,67 room stages and17 train phases per process match across
independent native processes. Native campaign input and cumulative owners
remain unchanged; no selected-route, stat, boss/death or Covenant control is used.

Both use actual three-turn/3000ms/top-floor defaults with tactical seed off
and no shortened forecast. The source consumes seven searches and reuses24
ordinary/five winning continuations. All38 publications have fresh full-root
checks; captures are315/123 with zero purity failures. The replay's36 inputs
consume zero planning nodes and publish no advice; this historical replay
still starts36 unused background searches, a test-driver inefficiency distinct
from input selection. No end-to-end three-second response guarantee is made.
This is a third natural rank-three victory, not completed campaign acceptance.

Archives use `full-battle-player-native-covenant-campaign-03-ring3-default-`
with `search.mt2f` and `replay.mt2f`; registered hashes and the pair gate run via
`Check-PlayerAdvisor.ps1 -Case NativeCovenantCampaign03Ring3Default -SkipBuild`.
Reproduction uses `Run-NativeCovenantPlayerPairs.ps1 -Rank 3 -Battle Campaign
-BattleDistance 2 -InputProfile <restored-Ring3-profile> -DefaultAdvice -SkipBuild`.

## Natural rank-three Ring4 with actual defaults

Original rewards/map continuation from the accepted Ring3 replay reaches
`Level4BattleHunterAttackers` at seed424242/distance3, three preceding wins,
all three cumulative native owners and Pyre72. The input checkpoint restores
all12 payloads. Its aura/birth rejection and separate27-input native component
are documented in [Harvester birth](../combat/harvester-birth.md).

Source `full-battle-units-spells-and-junk-20261024-092829-f142671b` and replay
`full-battle-units-spells-and-junk-20261024-093534-c7fa900f` both win at Pyre72:
35 inputs,27 card/ability plays and eight EndTurns. Native durations are
422.38/167.91 seconds. Both exit normally, preserve protected originals and
record all four failure counters zero. Complete independent inputs, roots,
forecasts, successors, RNG,68 room stages and15 train phases per process match.
This is the fourth natural rank-three victory, not completed campaign acceptance.

Actual defaults remain three turns/3000ms/top-floor preference, tactical seed
off, with no shortened forecasts. The source consumes six searches and reuses
21 ordinary/eight winning continuations; all37 publications have fresh complete
root checks. Captures are306/71 with zero purity failures. The frozen replay
starts one initial search before pausing, then starts zero further searches,
publishes zero advice and has no final background advice. Every replay input
consumes zero planning nodes. The deadline remains soft, not an end-to-end
response-time guarantee. The full player victories have no surviving enemy on
an upper floor at the birth boundary; actual cross-floor movement is established
by the separate seven-target native component, not inferred from these wins.

Archives use `full-battle-player-native-covenant-campaign-03-ring4-default-`
with `search.mt2f` and `replay.mt2f`, plus the accepted pair receipt.
`Check-PlayerAdvisor.ps1 -Case NativeCovenantCampaign03Ring4Default -SkipBuild`
checks registered hashes and the independent default pair gate. Reproduction
uses `Run-NativeCovenantPlayerPairs.ps1 -Rank 3 -Battle Campaign -BattleDistance 3
-InputProfile <restored-Ring4-profile> -DefaultAdvice -SkipBuild`.

## Natural rank-three Ring5 with actual defaults

Original rewards/map continuation reaches `Level5BattleHeavyHarvester` with
rank3,seed424242,four prior native wins and Pyre72. The immutable input save
SHA256 is `fdd901bbf0860bd7430493cbba99fe25d8ee0b56264a4b2fc4a0a25cc4b72390`.
`campaign-native-covenant-03-ring5.zip` has SHA256
`61e38ca09213d825a0a66d09a60401607b87f8d4ff7523daa185193aaa0f6192`;
all15 native payloads verify on restore.

Source `full-battle-units-spells-and-junk-20261024-094028-17a7824d` and replay
`full-battle-units-spells-and-junk-20261024-094856-87d8b344` both win at Pyre72:
43 inputs,34 card/ability plays and nine EndTurns. Native durations are
506.08/227.70 seconds. Both exit normally,preserve protected originals and
record all four failure counters zero. All independent inputs,roots,complete
forecasts,successors,RNG,74 room stages and17 train phases per process match.
This is the fifth natural rank-three victory,not completed campaign acceptance.

Actual defaults remain three turns/3000ms/top-floor preference,tactical seed
off. Fifteen inputs use explicitly marked shorter complete forecasts; a
three-second end-to-end response is not guaranteed. Eight consumed searches
serve43 inputs,with30 ordinary/five winning continuations avoiding search.
All45 publications have fresh full-root checks; captures are373/87 with no
purity failures. Frozen replay starts one initial search before pausing,then
starts zero further searches,publishes zero advice and has no final advice.
This pair started before seven-switch browsing was added to future default
sources; it establishes no additional browsing observation.

The continuous runner journals the accepted pair before progression,restores
its checkpoint and advances the replay through original rewards,shops and
map choices to native Ring6 with five prior wins. This is the first accepted
pair/restore/continuation integration of that runner; later failures retain it.
Continuation exits normally,protects originals and records69 native events.

Archives use `full-battle-player-native-covenant-campaign-03-ring5-default-`
with `search.mt2f` and `replay.mt2f`,plus its accepted receipt.
`Check-PlayerAdvisor.ps1 -Case NativeCovenantCampaign03Ring5Default -SkipBuild`
checks registered hashes and the independent default pair gate. Reproduction
uses `Run-NativeCovenantPlayerPairs.ps1 -Rank 3 -Battle Campaign -BattleDistance 4
-InputProfile <restored-Ring5-profile> -DefaultAdvice -SkipBuild`; new source
runs additionally exercise seven floor switches,which the frozen run repeats.
