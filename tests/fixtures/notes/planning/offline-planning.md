# Offline candidates and native replay

## One decision with current player defaults

`plan-root` directly searches one recorded `RollingDecisions[index].Root`, or
the `Root` of a single-position capture. The index defaults to zero. It uses the
Windows player's whole-battle settings: 256 shared nodes, beam8, 12 actions per
turn, top preference, tactical option off, 256 steps, early stopping and honest
shorter complete windows. Workers default to two (one is optional), and the
explicit 100..30000ms budget can reproduce the user's setting. No native game
or intermediate request export is needed. `plan-battle` below remains an
explicit fixed-horizon experiment; it does not reproduce whole-battle defaults.

```powershell
dotnet src/FixtureTools/bin/Release/net8.0/FixtureTools.dll plan-root <capture.mt2f> .probe-runs/root-candidate.mt2f 3000
dotnet src/FixtureTools/bin/Release/net8.0/FixtureTools.dll plan-root <capture.mt2f> .probe-runs/later-root.mt2f 10000 12 2
dotnet src/FixtureTools/bin/Release/net8.0/FixtureTools.dll verify-root-plan .probe-runs/root-candidate.mt2f
```

Each result records its source archive and model DLL hashes, selected decision,
exact options, full root, plan, worker metrics and limit flags. Before creating
the file, it independently replays all steps, complete state/RNG, score and
terminal/partial labeling and checks the immutable parent. `verify-root-plan`
repeats that verification in a new process. Existing outputs and invalid input
bounds are rejected; these artifacts explicitly require native validation and
are not added to the native evidence inventory. Search time excludes capture,
archive serialization and independent replay, and remains a soft budget.

An optional final `[shared-nodes]` argument accepts1..4096, default256. This is
an offline diagnostic experiment: it does not change the player's node budget.
The archive explicitly records RequestedMaxNodes and verification requires
Options.MaxNodes and ExpandedNodes to stay within it. Older archives without
this field retain the fixed256-node validation. Time/workers and every other
player option remain unchanged, and full-state/RNG replay is still required.

```powershell
dotnet src/FixtureTools/bin/Release/net8.0/FixtureTools.dll plan-root <capture.mt2f> .probe-runs/root512.mt2f 30000 0 2 512
```

The natural random05 Ring7 repaired0.2.60 root remains a40-input/7-EndTurn
model defeat:256 nodes/14.18s,512 nodes/20.65s, and a1024-node cap reaching
270 nodes at the30.82s soft limit. The512 route is exactly the256 route;
neither larger cap establishes a win. These individual timings are not a
scaling benchmark. Both new results pass separate-process full-state/RNG
replay, and the older fixed256 archive still verifies. Node counts0/4097,
an altered node declaration and output overwrite are rejected; the existing
prediction hash stays unchanged. No player default or native evidence changes.

A complete terminal root plan can be exported directly for native execution:

```powershell
dotnet src/FixtureTools/bin/Release/net8.0/FixtureTools.dll freeze-root-plan .probe-runs/root-candidate.mt2f .probe-runs/frozen-root-candidate.mt2f
```

The exporter verifies the root plan, replays every input, builds complete frozen
successor/forecast records, then independently verifies the resulting archive.
Partial horizons, malformed Action/EndTurn pairs and existing outputs are
rejected. The file retains its source hash, ModelPrediction kind and
NativeValidationRequired flag; export does not establish native acceptance.
The exported SourceArchive/SourceSHA256 retain the native input binding for
`refine-prediction`; RootPlanArchive/RootPlanSHA256 separately bind the verified
root-search result. Existing prediction files are never rewritten.
After actual native execution and independent replay, use `verify-offline-native`
to bind the prediction to each observed trace, and ModelChecks
`--random-refined-campaign-pair-only <source> <replay> <seed> <rank> <distance>
<primary> <secondary> <input-count>` to check the two complete frozen native
state/forecast/RNG chains. Search settings in a frozen process are metadata,
so such wins do not count as default-budget search acceptance.

Focused checks on2026-10-25 reproduced a partial Angel Duo deployment and a
nonzero recorded decision, plus the live moon root's terminal defeat with one
and two workers. Every result passed a second process's full replay. Altered
scores, invalid time/index/worker requests and attempted output overwrites were
rejected; the existing result hash was unchanged. These runs did not establish
an Angel Duo or moon victory, and no extra native battle was spent on them.

## Whole-battle rolling experiments

`FixtureTools plan-battle` runs the existing bounded rolling planner entirely
in the model, starting from one deployment snapshot in a settled native trace
with zero capture/mismatch/unsupported/pending counters. Each decision applies
only the first planned step, then replans from its own successor. It records
complete states/RNG and frozen actions in a separate `ModelPrediction` archive.
These predictions are not native coverage evidence.

Add `--player-continuation` to use the actual `BattleAdviceSession` instead of
searching again after every card. This enables the player's verified same-turn
and winning continuations, explicitly allowed shorter complete windows and
ordinary adaptive deployment. It keeps the requested horizon, shared search
limits and the selected tactical/floor settings. Search and continuation counts
are printed separately; the flag is opt-in so existing strict experiments retain
their behavior. The current session retains any verified forecast suffix across
turns, including partial and defeat forecasts, until the actual state diverges
or the suffix is exhausted. Older archives can predate this reuse behavior.

```powershell
dotnet build src/FixtureTools/FixtureTools.csproj -c Release --no-restore
dotnet src/FixtureTools/bin/Release/net8.0/FixtureTools.dll plan-battle <native.mt2f> .probe-runs/candidate.mt2f 3 1000 2
dotnet src/FixtureTools/bin/Release/net8.0/FixtureTools.dll plan-battle <native.mt2f> .probe-runs/player-candidate.mt2f 3 3000 2 --player-continuation
dotnet src/FixtureTools/bin/Release/net8.0/FixtureTools.dll verify-offline-plan .probe-runs/candidate.mt2f
# Replay the frozen candidate from the same native input profile, without search:
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -InputProfile <profile> -BattleDistance 7 -RollingAdvice -RollingInput .probe-runs/candidate.mt2f -OuterBossBirth -OuterBossTurns -PhysicalSpawnPoints -SettleDeathDissolves -SkipBuild
dotnet src/FixtureTools/bin/Release/net8.0/FixtureTools.dll verify-offline-native .probe-runs/candidate.mt2f <fresh-native.mt2f>
```

The explicit CLI horizon accepts 1..16 turns, budgets 1..60,000 ms per decision
and deployment rooms 0..2. Node/beam/action bounds remain 256/8/12, and the whole
battle stops at 256 decisions. Unsupported candidate reasons remain visible.
Existing live-advisor defaults are unchanged. Output files must be new.

`verify-offline-plan` independently replays every decision's entire forecast
as well as its consumed successor. It requires the consumed action to equal
the first forecast step, valid action/EndTurn pairs, no steps after a terminal
outcome, matching final states/RNG and an immutable root. This also accepts
the existing one-step `policy-battle`/`freeze-actions` artifacts. A verified
model victory still requires fresh native execution; .NET and Unity can select
different routes under the same soft time budget.

Focused 2026-10-24 validation from the unchanged Fragile adaptive-budget-loss
root completes a model victory in 54 inputs/Pyre2: seven searches, 31 ordinary
and sixteen winning continuations, zero unsupported reasons. Planning plus
full independent verification takes 91.34 seconds on this host. All 54 complete
forecasts/successors and the immutable root pass; an altered forecast Energy
is rejected even though the consumed route is unchanged. The old 44-input
Angel Duo fixed-policy artifact also passes the stronger verifier as a loss.
These are local observations, not a speed guarantee or new native coverage.

The optional `--tactical` argument adds a deterministic long-horizon seed before
the existing strength/fixed-policy/beam seeds. It preserves tower attack by
skipping negative tower-status cards, concentrates units on the preferred floor,
prioritizes equipment/armor, then spends X buffs and unit abilities. Targets are
ranked by visible stats with oldest-unit identity breaking ties; no encounter or
card asset names enter this policy. The seed shares all node/time/action and
cancellation limits. The first complete winning seed may return immediately;
this is a bounded recommendation without an optimality claim. A seed that does
not win continues through the existing planner within the remaining budget.
When player advice permits shorter windows, the initial deployment fallback
ends after one complete turn. This reserves the rest of the shared budget for
the tactical card sequence. Strict callers keep their full-horizon fallback.

```powershell
dotnet src/FixtureTools/bin/Release/net8.0/FixtureTools.dll plan-battle tests/fixtures/campaign/progression/full-battle-progressed-ring8-gold-no-cards.mt2f .probe-runs/tactical-candidate.mt2f 12 10000 2 --tactical
dotnet src/FixtureTools/bin/Release/net8.0/FixtureTools.dll policy-battle tests/fixtures/campaign/progression/full-battle-progressed-ring8-gold-no-cards.mt2f .probe-runs/policy-candidate.mt2f 2
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --tactical-planner-only tests/fixtures/campaign/progression/full-battle-progressed-ring8-gold-frozen-win.mt2f
```

`policy-battle` freezes the seed's actions without search; its output is still a
model prediction. Its optional `--strength` selects the existing visible-strength
policy instead of the tactical policy, making deployment comparisons cheap.
All three whole-battle commands use the first `RollingDecisions.Root` when
present. Player capture and development capture have separate instance-ID
namespaces; taking `Actions[0].Before` from a player trace cannot be replayed by
the player driver. Non-rolling traces retain the action/turn-root fallback.
Replay player-root candidates with `-PlayerAdvisor`. A fresh capture is needed
when older snapshots omit newly captured definition metadata; replay comparisons
remain strict rather than filling those fields from assumptions.
The planner's rolling 12-turn/10,000-ms candidate completed
50 decisions with zero unsupported reasons and model Pyre21 in 163.84 seconds,
including independent replay of all forecasts. The subsequent live rolling
source also wins natively with Pyre21 after the same 50 decisions; its accepted
trace is `full-battle-progressed-ring8-gold-search.mt2f`. The earlier external
frozen route's Pyre4 victory remains separate evidence.

For native rolling acceptance, `Run-FullBattleProbe.ps1` and
`Run-RollingAdvisorBenchmark.ps1` accept `-PlannerTacticalSeed` and explicit
`-PlannerHorizon 12`. `Run-BattleAdvisor.ps1` exposes the same opt-in setting as
`-TacticalSeed -Horizon 12 -TimeLimitMs 30000`. Outer-boss benchmarks also require
`-OuterBossBirth -OuterBossTurns`; the interactive launcher combines these as
`-OuterBoss`. Manual and automated defaults
for fixed-horizon experiments request three turns and 3,000 ms; the Windows
player defaults to whole-battle search, and these launchers require explicit
whole-battle/worker flags to select that mode. The native benchmark may allow
30,000 ms for a longer continuation. Evidence records the actual setting.

`-DotNetPlanner` on the full-battle, rolling-benchmark and interactive launchers
optionally moves search plus independent full-path replay into a .NET 8 process.
The game exports one immutable binary request under its isolated profile. The
worker checks shared bounds, replays every returned step, checks full state/RNG,
score and root immutability, then writes a new verified binary result. The game
compares every captured request value and every decoded final-state value across
the process boundary before publication. Concrete CLR collection types are not
part of this wire comparison; all model properties and ordered values are.
Cancellation kills only the owned planner process; generation checks still reject
obsolete publications, and failed verification exposes no executable advice.

Requests/results remain in the profile as `advisor-*.input.mt2f` and
`advisor-*.output.mt2f`. `FixtureTools plan-once <request> <new-result>` reproduces
one request independently. A native request calibration took 7,014 ms for search
and 2,490 ms for its separate replay. Initial live publications took about 12
seconds, versus 45–48 seconds in the earlier in-process trial. The completed
natural eighth-ring source takes 1,075.27 seconds including native actions,
captures and final export. Its 50 executed publications have capture-to-UI
median 11,089.0 ms, P95 12,868.8 ms and range 4,615.3–13,385.2 ms. All four
failure counters and 930 capture-purity checks are zero, with original files
unchanged. These are local observations without a latency guarantee. External
publication `SearchMs` includes process startup, search, independent replay and
result decoding; each result retains separate `ModelSearchMs` and
`ModelVerificationMs`. The usual native capture guard, driver and replay
acceptance remain in force, and the backend is opt-in.

`freeze-actions <native.mt2f> <actions.json or -> <output.mt2f>` imports a bounded
external policy's action sequence and independently applies every legal action
from that same native deployment root. Input items contain `Action` and
`IsEndTurn`; `-` reads JSON from standard input. The sequence must reach a
terminal outcome. The same model-only verification and fresh native replay
requirements apply. This supports cheap policy exploration without authoring
game state or repeating slow game startup for every candidate.

The first end-to-end validation used the legal gold-preserving eighth-ring
checkpoint: 25 decisions in 31.52 seconds of model planning, then 149.93 seconds
of fresh native execution. All frozen roots/actions/predictions/plans and the
terminal loss were preserved; all four native failure counters were zero and
protected game files unchanged. This validates the workflow, not a victory.
Longer horizons alone did not win the tested input. Native victory candidates
are validated separately.

The tactical seed now considers a champion's shift ability immediately before
a front-rearrangement card when both actions are legal and the card restores
the original tank and leaves the champion alive after both actions. Missing,
unaffordable or lethal restoration retains the champion protection guard.
This uses captured effects and unit roles, without card or
encounter names. The natural Fragile deployment root produces a 54-input,
nine-EndTurn model victory with Pyre2; model-only reproduction is:

```powershell
dotnet src/FixtureTools/bin/Release/net8.0/FixtureTools.dll policy-battle tests/fixtures/bosses/fragile/full-battle-player-boss-fragile-bounded-search.mt2f .probe-runs/fragile-shift-candidate.mt2f 0
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --tactical-shift-only .probe-runs/fragile-shift-candidate.mt2f
```

The focused checker covers both legal restoration counterexamples, independent
winning full-state/RNG replay, and immutable roots. The full battle now wins in
55 of 56 allowed nodes; a two-node control retains an explicitly shorter complete
deployment turn. A separate actual player source and fresh native replay now
both win at Pyre2 with all four counters zero; see [native winning evidence](../bosses/player-fragile-winning.md).

In0.2.100, the native Random10 Ring8 loss adds a lethal restoration control.
At input10, the champion's ability shifts it ahead of its tank and equipment
self-damage reduces its health45 to15. Input11 restores the tank, but a second
shift kills the champion. The optional seed previously accepted this because
it checked only the tank's final position. It now also requires the champion
to survive the restoration. The complete original successors/RNG remain
replayable; three deployment preferences avoid this setup, and full search
retains the legal ability. The original safe54-input Fragile win and bounded
winning/shorter-horizon controls pass without changing their rules.

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --tactical-shift-safety-only tests/fixtures/campaign/random/full-battle-random-campaign-10-ring8-release099-budget10000-loss.mt2f
```

This fixes a specific seed decision. Three model-only deployments from the
same Random10 Ring8 entry still lose; it is not a new native campaign victory.

`extract-unsupported-branch <recorded.mt2f> <new-output.mt2f> <exact-reason>`
accepts either recorded forecast roots or a pure native deployment observation,
which it continues with at most256 legal tactical inputs. It extracts a supported prefix and its
rejected next action. It independently verifies the prefix and immutable root,
and exports `ModelUnsupportedBranch` without an assumed successor. The scoped
native `-UnsupportedBranchInput` driver replays that prefix from the original
checkpoint and stops after observing the rejected candidate. It rejects
scenario controls and pauses background search. This short observation mode
closed the [Endless spell routing gap](../cards/endless-spell-routing.md) with two native
processes in 140.08/123.97 seconds; it does not claim terminal battle acceptance.

`refine-policy <native.mt2f> <new-winning-output.mt2f> <milliseconds>
[start-decision] [last-decision]` compares local alternatives to an accepted
native battle without launching a new game for every candidate. The inclusive
decision interval permits separate opening and late-battle batches. It changes
the recorded card's target/position or ends the turn instead; at a recorded
EndTurn it tries any remaining legal input. Later choices use the tactical
policy on the living champion's floor. Each attempt retains the full legal
prefix, RNG and immutable parent; an independent replay verifies a complete
model win before creating the requested output. No win means no winning file,
and does not prove the encounter is unwinnable.100..600000ms is a soft search
budget; atomic transitions and successful export/verification take extra time.
Search and export/verification times are reported separately for a win.

The first rejected alternative is independently reproduced and exported as
`<output>.unsupported.mt2f`, using the existing `ModelUnsupportedBranch` format
and original native deployment root. `-UnsupportedBranchInput` can then observe
that exact prefix/candidate in a fresh isolated game. The tool reports unique
rejection reasons and never invents a successor for an unsupported action.
Run the separate native source checker before interpreting these predictions;
raw UI preview allocator values are not model-prefix equality evidence.

The Banished/Umbra Ring7 counterexample produced69 opening/early alternatives
and88 later alternatives, all terminal model defeats. Four omitted choices in
the early batch share `Missing dead unit spawner card routing.`; two independent
native21-input observations now calibrate the feeding spell's already-eaten
carrier routing, with full-state/RNG agreement. A separate
Pyreborne/Banished control produces a47-input model win and passes independent
full-state/RNG replay. These are diagnostic results, not new native victories.

After the feeding repair, the Banished/Umbra opening's10,000ms/two-worker
whole-battle search still predicts a37-step defeat and passes independent replay.
The preserved feeding counterexample first diverges at recorded decision0;
rechecking only that opening produces three complete local-policy defeats with
zero unsupported attempts in4.04s. This closes that focused diagnostic guard,
without rerunning a native defeat or claiming all remaining local alternatives
were rechecked or that no winning route exists.

```powershell
dotnet src/FixtureTools/bin/Release/net8.0/FixtureTools.dll refine-policy tests/fixtures/campaign/random/full-battle-random-campaign-01-ring7-budget10000-loss.mt2f .probe-runs/ring7-local-candidate.mt2f 180000 0 16
```

`refine-prediction <native.mt2f> <verified-prediction.mt2f>
<new-winning-output.mt2f> <milliseconds> [start-decision] [last-decision]`
uses the same bounded exploration before spending time on a complete native
defeat. It first independently replays the frozen model baseline, requires its
source SHA256 and full deployment root to match the exact pure/accepted native
input, and then replays its recorded outcomes/RNG. Prediction baselines do not
become native evidence. Winning candidates are frozen from the original native
input, so the existing fresh-game replay requirement stays intact.

`improve-policy <native.mt2f> <new-output.mt2f> <milliseconds> [passes] [start-decision]`
retains the first strictly better complete route between sweeps, allowing
several useful changes to compose. The default is four sweeps (1..16 allowed)
under a100..600000ms soft search budget. Each branch preserves its prefix,
parent and RNG; a separate full replay verifies the exported route. Existing
outputs are rejected. Complete losing improvements are explicitly exported as
`PlayerDefeated`; their diagnostic boss-health ordering does not change the
production win/Pyre objective or prove optimality. Export/verification can
extend the soft budget.

`improve-prediction <native.mt2f> <verified-prediction.mt2f>
<new-output.mt2f> <milliseconds> [passes] [start-decision]` resumes these sweeps from an
independently verified frozen improvement. It requires the exact original
native input SHA256 and complete deployment root, including RNG, before using
any predicted input. The new export stays bound to that original native
source. This avoids discarding useful losing improvements between bounded
search runs; neither the baseline nor its continuation becomes native evidence.
The optional zero-based starting decision defaults to0. Later sweeps leave
that earlier prefix unchanged while still independently replaying the entire
baseline and exported route. This avoids repeatedly exploring accepted early
choices when composing improvements in later turns; it does not declare the
skipped alternatives optimal. Invalid starting indices create no output.

Loss ordering includes bosses in captured future waves and queued outer/final
bosses at full health. In random09 Ring7, a historical24-input loss at turn5,
before Rax's arrival, previously displaced a36-input turn9 loss with Rax187HP.
The corrected diagnostic metric ranks the latter higher:1000000016991.19
versus1000000099995.25. Both artifacts remain independently verified terminal
defeats bound to the same native root; the production evaluator is unchanged.

`improve-order <native.mt2f> <verified-prediction.mt2f> <new-output.mt2f>
<milliseconds> <passes> <start-decision>` additionally considers other hand
cards and unit abilities before the recorded input. The existing commands
retain their narrower same-card neighborhood. All candidates still require
legal complete continuations,immutable parents,independent replay,and exact
native source/hash binding; an exported loss remains explicitly a loss.
This does not change the production planner or a player's saved settings.

`improve-suffix <native.mt2f> <verified-prediction.mt2f> <new-output.mt2f>
<milliseconds> <passes> <start-decision>` also reapplies the recorded suffix
after each alternative. Inputs are aligned by turn and revalidated against
the new model-owned state; recorded snapshots are never spliced. An early
EndTurn skips the old turn's remaining inputs. A deferred EndTurn first uses
the tactical continuation to complete that turn. The existing18-play/256-input
bounds remain. Invalidated targets, recursion selections or consumed cards
are skipped and counted separately from missing legal model branches.

This diagnostic mode can retain useful changes on both sides of a new edit,
instead of replacing the entire later route with a fixed policy. On random09
Ring8, six improvements among142 complete candidates reduce the boss from1344
to925HP in a40-input verified defeat. The final implementation then finds two
more improvements among113 complete candidates, leaving831HP. Both exports
pass independent complete state/forecast/RNG replay and remain explicitly
model losses. A separate60-second control completes50 of51 candidates,
reapplies1056 suffix inputs, skips432 stale inputs and has zero unsupported
legal branches. Different native input hashes and existing output/observation
paths are rejected without overwriting their files.

The first reproducible rejection from a generated legal alternative or tactical
continuation is exported as`<output>.unsupported.mt2f` after independently
replaying its prefix from the native deployment root. Stale recorded selections
are never exported as model coverage gaps. Native execution remains required
before any predicted win counts toward campaign acceptance.

On continuous random09 Ring7,the36-input late baseline left Rax179HP.
The narrow decision6..end sweep completed147 candidates without improvement;
a30000ms/4096-node root search at decision24 also predicted defeat.
The order mode starts at decision20 and changes card15 to card16 there.
Four retained improvements among96 complete candidates find a37-input model
win at Pyre55 in91.58s; full export/verification finishes in102.45s.
The native source,root and first20 inputs/full successor states stay identical.
A different native deployment is rejected before output creation. Native
execution and independent replay both subsequently win at Pyre55:37 inputs,
27 plays and10 EndTurns,with every full state/forecast/RNG matching and four
zero failure counters. Each native archive also binds exactly to the unchanged
frozen model prediction. The continuous first7-ring mixed receipt chain passes
without simulations; it remains explicitly distinct from default-budget
acceptance and an8-ring campaign completion.
Focused controls reject a different native source, a separately replayable
prediction whose source hash was rebound while its root stayed different, and
an existing output without overwriting it. The natural Covenant3 AngelDuo
counterexample exercises valid continuation from a59-input verified defeat.

On the preserved random Umbra/Stygian Ring3 defeat, three improvements among
eight complete candidates found a61-input model win (Pyre24) in22.68s of
search. The retained-metagame Banished/Pyreborne Ring3 defeat produced a
41-input model win (Pyre37), with two improvements among28 complete candidates
in24.94s. Both pass independent full-state/RNG replay. Native validation exposed
an unboxed-card Conduit mismatch in the first route and a terminal spell-kill
statistics mismatch in the second. Both boundaries are now repaired and both
complete routes pass independent native winning pairs, at Pyre24 and37
respectively. The original default-budget defeats stay separate; see
[random campaign validation](../../../../docs/random-campaign-validation.md)
and [retained metagame continuation](../campaign/native-story-unlock.md).

The repaired random Pyreborne/Banished Ring5 input produced72 early local
alternatives (decisions0..18), all terminal model defeats with zero unsupported
attempts. Decisions19..47 used240000ms:183 attempts,182 terminal defeats,
one incomplete at the soft limit. The follow-up48..65 batch completed70
terminal defeats with no timeout, and a separate47..47 batch completed all4
alternatives as defeats. All batches had zero unsupported attempts. Counts
include repeated alternatives at the resumed decision and are not a unique
or exhaustive search-space total. This is a diagnostic result, not an
impossibility proof.
The suffix-preserving improver subsequently found a48-input model win in the
Wurmkin/Hellhorned random Ring8 counterexample. Both independent native
processes execute the39 card plays/nine EndTurns unchanged with zero search,
Pyre1 and matching full forecasts/state/RNG. Native rewards reach an eight-win
Standard settlement; the continuous mixed-route chain and completed-save
archive/restore pass. This is the first complete random campaign acceptance,
not an all-default-budget result. The natural Covenant3 Ring6 counterexample
improved from61 to49 inputs across two suffix batches but still predicts
defeat; no additional native games were spent on those losing routes.
Three fixed-floor baselines and the10000ms default root search also predict
defeat. The subsequent actual0.2.28 native59-input defeat confirms the root
can run without a model mismatch, not that no winning policy exists.
