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
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --player-native-covenant-campaign-pair-only tests/fixtures/full-battle-player-native-covenant-campaign-02-ring2-search.mt2f tests/fixtures/full-battle-player-native-covenant-campaign-02-ring2-replay.mt2f 2 1
```

`Run-NativeCovenantPlayerPairs.ps1 -Battle Campaign` accepts a single rank,
native InputProfile and BattleDistance. It checks the actual native difficulty,
seed, complete prior victory history and BattleIntro before launch, validates
both loaded save hashes and final native acceptance summaries, then runs the
focused checker. Wrong-rank and wrong-ring preflights reject before launching.
The existing controlled modes retain their defaults. Longer searches use
`-PlannerHorizon` and `-PlannerTimeLimitMs` explicitly.

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
first recommendation: `Level6AngelDuo_RallyAttackIncantHealth` is rejected by
the outer/queued-boss capture guard. This is an open integration gap, not an
accepted battle archive. The five prior victories and the failed sixth battle
are kept distinct; no cumulative campaign completion is claimed.

The same Ring6 input now has accepted original Angel Duo birth and ordinary
no-card battle coverage, with complete state/RNG and zero native failures.
Original cross-floor Incant health mutation and Slay attack buffs also now have
accepted native fixed-policy coverage. The forced-Relentless global primitive
also matches five native full-state controls, including scoring counters and
full restoration before the accepted no-card battle continues. The same input
also has accepted original inner/outer death controls and complete surviving-boss
continuations. Player capture remains guarded for both-live companion
Relentless/Pyre phases and actual player replay; see [Angel Duo](angel-duo.md).
