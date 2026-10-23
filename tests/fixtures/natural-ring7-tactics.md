# Natural seventh-battle tactics

Latest default-budget follow-up: the original six-win checkpoint now also
has a native player victory and independent frozen replay with actual defaults
(three requested turns/3000ms, tactical option off). Both execute 60 inputs,
win at Pyre62, exit normally and retain zero failure counters. Shorter windows
and atomic soft-deadline overruns remain explicit. See
[default strategy acceptance](default-advice-strategy.md); earlier default
losses and the longer-budget comparisons below remain historical evidence.

Seed 424244 uses the unchanged natural checkpoint
`.probe-runs/campaign-ring8-20261022-072727-d98c7abf-before-ring7` at distance 6.
Deck, upgrades, health, enemy definitions and spawn queues are not edited.

`full-battle-natural-ring7-tactical-win.mt2f` is the fresh native replay from
`full-battle-units-spells-and-junk-20261022-104702-550e4157`. All 67 frozen
decisions (57 plays and ten EndTurns) match complete state and RNG; original
Rax is defeated and Pyre health is 46. CaptureFailures, Mismatches, Unsupported
and Pending are zero, terminal effects settle, the process exits successfully
and protected originals remain unchanged. Independent offline/native comparison
also verifies every unchanged frozen input and forecast. This first route uses
an explicitly selected deployment followed by the experimentally improved policy.

The general tactical seed now scores equipment by its captured stat/trigger
payload, actual ability ownership, front position and the oldest equipment it
would replace. Permanent, temporary and merged equipment upgrades participate.
Cooldown reduction therefore stays with an ability user instead of being
overwritten by a small health bonus. Silence has a cost for triggers/abilities;
retaliation and healing benefit the front unit. No card names or encounter IDs
are used by these rules.

Pure targeted debuffs favor enemy threats and bosses; damage still favors
low-health kills. Moving a friendly unit to the front favors health/armor and
OnHit triggers. A champion ability that would move it ahead of its existing tank
is deferred while a boss is in the room. These are optional heuristic candidates,
evaluated and independently replayed by the bounded planner.

The production policy wins from the original deployment root without manually
chosen inputs: 67 decisions, ten EndTurns and Pyre62. Bounded planning takes
72 nodes and about nine seconds; independent full-state/RNG replay, immutable
root, cancellation and shared node-budget controls pass. The existing natural
AllyBuffer planner still wins in 50 inputs. Model success does not replace the
actual player publication and independent native replay acceptance.

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --tactical-target-only
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --natural-ring7-planner-only `
  tests/fixtures/full-battle-natural-ring7-copy-on-play.mt2f
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --natural-ring7-tactics-only `
  tests/fixtures/full-battle-natural-ring7-tactical-win.mt2f
```

Natural progression to Ring8 is now retained separately. Fragile player capture
remains guarded pending its own natural victory and player acceptance.
No full regression or Mac build is used for this batch.

## Game-runtime budgets and natural continuation

The first real-player trial exhausted its 30-second budget before finishing
the tactical route. Its empty-tower fallback chose EndTurn during deployment.
The optional tactical seed now starts first and caches a complete forecast
after its first input on every planning turn. This retains a legal deployment
when the full seed times out, while the advice verifier still rejects incomplete
forecasts. One-node, sixty-node and post-deployment controls independently
replay the bounded fallbacks without changing their parents.

The seed also skips pure Dragon Hoard gold abilities that award zero gold.
This reduces the natural Ring7 winning route from 67 to 60 inputs, with Pyre62
preserved. The latest bounded model gate uses 66 nodes; the actual player
runtime has produced that full route in 27.6 seconds.

The 67-input production route has meanwhile passed a fresh native frozen
replay with Pyre62, every unchanged input/forecast and complete state/RNG.
Native campaign continuation then reached the natural Fragile encounter.
`campaign-progressed-ring8-fragile.zip` retains seven preceding native wins,
the save/choice history and that actual seventh-battle replay. All thirteen
manifest members pass restore/hash validation. Its SHA256 is
`6fb9f47594a138cbac6f759a41e62dbf5d7ff8354a93bc5d95690a4997494961`.

The campaign driver previously reopened an enhancer offer after its deck
picker had no selectable cards. It now records that declined offer for the
current distance and continues through native UI. A resumed native run declined
four such offers, reached Ring8 and preserved protected original files.

## Actual player publications

The real player assembly completes all 60 inputs (50 plays/ten EndTurns),
defeats original Rax and preserves Pyre62. Source
`full-battle-units-spells-and-junk-20261022-111153-0a3f70fa` records 62
publications and 683 pure captures, with zero capture/mismatch/unsupported/
pending counters and settled terminal effects. The visible panel reports
victory with Pyre62 and an executable next input. Pause/resume, hide/cancel,
show/refresh, raycast and drag acceptance also pass.

The first consumed publication times out with a complete losing forecast
that still deploys a legal unit. The second finds a complete victory; all
subsequent forecasts remain winning. Fifty-seven later inputs retain that
winning continuation; one later fresh search also preserves victory. Median/
P95 response is 2.50/3.94 seconds; the first complete winning search takes
31.88 seconds plus verification. The configured 30-second budget is checked
between atomic expansions, so it is not a hard response-time guarantee.

The source writes its complete binary and `DEPTH-PASS` before the outer runner
hits its 900-second deadline during game exit. Its normal exit and final
protected-file comparison are therefore **not verified**. This does not erase
the captured native victory, but is not a clean runner pass. The runner now
allows one additional sixty-second exit period after a captured terminal
result, retaining all normal exit and failure checks. Its shared process waiter
now passes seven runtime child-process controls: ordinary exit, win/loss grace,
nonzero terminal exit, an unterminated run, a stuck terminal run and immediate
isolated-card rejection. The stuck case receives exactly one bounded extension
before it is killed; a nonzero exit remains nonzero. These controls exercise
the production wait function, without reclassifying this earlier source as a
clean Unity exit or reproducing a long Unity export/teardown stall.

```powershell
./scripts/Check-NativeBattleProcess.ps1
```

The exact before-battle input is retained in
`campaign-progressed-ring7-tactical.zip`: six preceding native wins, six
hash-verified restore members, SHA256
`0a3d04974085b328b5540a0a81ce3eebab973c26bbdbc1993349c434d18fe277`.
The native save hash is
`812c73153880f32805f9e7a4ac77698023db97cb7d4bf615350643799407a47c`.
Independent `full-battle-units-spells-and-junk-20261022-112730-577004a6`
replays all 60 unchanged frozen inputs from this restored archive, winning with
Pyre62 in 504.36 seconds. It settles terminal effects, records `DEPTH-PASS`,
exits normally with code zero and preserves protected originals. All four
failure counters are zero, with 206 pure player captures and no publications
or search nodes. It deliberately does not repeat the source's UI interaction
test. The two captures are retained as
`full-battle-player-natural-ring7-{search,replay}.mt2f`.
The dedicated pair check passes every input, root, successor, complete forecast
and RNG in both captures, including the deployment fallback and later replan.

All three loaded player payloads match between the independent processes:

| DLL | SHA256 |
| --- | --- |
| MonsterTrain2Poju.Advisor.dll | `4de3bf483000921d34f67fc262a13ea8f39fff8446eb75c43fa894970d897862` |
| Model.dll | `97a9f3ae80217201bf8619903e430d8d49c402b97f96e5e8307d259ea87e3ec7` |
| FixtureArchive.dll | `db2815c453fc43396974fc9c693373d132e0944a82f5427df2f793083cf3dea0` |

The focused acceptance command is:

```powershell
./scripts/Check-PlayerAdvisor.ps1 -Case NaturalRing7 -SkipBuild
```

## Clean restart and actual default budget · 2026-10-23

Two new source/replay pairs start from the same hash-verified six-win checkpoint
above. No authored cards, health changes or encounter replacements are added.
The original timeout record remains historical; the new tactical source and
replay both exit normally, preserve protected originals, record DEPTH-PASS and
have zero capture/mismatch/unsupported/pending counters.

| Player settings | Source / replay suffix | Inputs / plays / EndTurns | Outcome | Native seconds source / replay |
| --- | --- | --- | --- | --- |
| 12 turns / 30000ms, tactical seed on | `212331-dfc907db` / `213848-31afa20d` | 60 / 50 / 10 | win, Pyre62 | 865.80 / 591.12 |
| Actual defaults: 3 turns / 3000ms, tactical seed off | `212452-5e22e425` / `213238-a521cadf` | 16 / 8 / 8 | loss | 299.12 / 157.69 |

All profile suffixes use prefix `full-battle-units-spells-and-junk-20261023-`.
The tactical run finds the full winning line in its first consumed publication;
the remaining 59 decisions reuse its verified suffix. Each fresh replay checks
all native roots, inputs, successors, complete forecasts and RNG. The default
pair verifies supported execution and its loss; it does not establish adequate
default-budget strategy for this advanced run. Preferred deployment is floor2
(zero based) in both presets. The ordinary seventh battle's clean-exit gap is
closed by the new records, while default strategy quality remains open.

The new registered archives are
`full-battle-player-natural-ring7-{clean,default}-{search,replay}.mt2f`; their
original profile names, sizes, hashes and node counts are in `manifest.tsv`.
Dedicated checker commands are `--player-natural-ring7-clean-pair-only` and
`--player-natural-ring7-default-pair-only`, each followed by its source/replay
paths. To reproduce either native pair:

```powershell
./scripts/Run-NaturalRing7PlayerPair.ps1 -InputProfile <restored-before-ring7-profile> -Preset Clean -NativeWorker 2
./scripts/Run-NaturalRing7PlayerPair.ps1 -InputProfile <restored-before-ring7-profile> -Preset Default -NativeWorker 3
```

The runner verifies the fixed original checkpoint hash, normal exits, zero
failure counters, unchanged protected files and independent focused gates.
`-SourceProfile <accepted-source-profile>` reuses an already accepted source
without repeating its search. No full regression is involved.

## Same-turn continuation follow-up · 2026-10-23

After ordinary advice continuation and the room-attachment birth-order fix,
another actual-default pair completes 25 inputs (15 plays/abilities, ten
EndTurns), with both processes losing. Source/replay profiles
`221919-5f432b65` / `222608-79e8cfd6` exit normally in 384.49 / 226.51 seconds,
preserve protected originals, record DEPTH-PASS and all four failure counters
are zero. The same original checkpoint hash and encounter/settings above are
verified by `Run-NaturalRing7PlayerPair.ps1 -Preset Default`.

Ten ordinary continuations take zero search work; nine post-EndTurn
boundaries replan. Every full native forecast/root/successor/RNG and all
20 train phases match in the independent process. The 27 source publications
all have fresh full-root checks. This is additional continuation correctness
evidence; the changed route and outcome do not establish strategy quality or
a controlled performance speedup. See [same-turn advice continuation](player-ordinary-continuation.md).
Archives use `full-battle-player-natural-ring7-ordinary-{search,replay}.mt2f`.
