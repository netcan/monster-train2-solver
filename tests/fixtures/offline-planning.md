# Offline candidates and native replay

`FixtureTools plan-battle` runs the existing bounded rolling planner entirely
in the model, starting from one deployment snapshot in a settled native trace
with zero capture/mismatch/unsupported/pending counters. Each decision applies
only the first planned step, then replans from its own successor. It records
complete states/RNG and frozen actions in a separate `ModelPrediction` archive.
These predictions are not native coverage evidence.

```powershell
dotnet build src/FixtureTools/FixtureTools.csproj -c Release --no-restore
dotnet src/FixtureTools/bin/Release/net8.0/FixtureTools.dll plan-battle <native.mt2f> .probe-runs/candidate.mt2f 3 1000 2
dotnet src/FixtureTools/bin/Release/net8.0/FixtureTools.dll verify-offline-plan .probe-runs/candidate.mt2f
# Replay the frozen candidate from the same native input profile, without search:
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -InputProfile <profile> -BattleDistance 7 -RollingAdvice -RollingInput .probe-runs/candidate.mt2f -OuterBossBirth -OuterBossTurns -PhysicalSpawnPoints -SettleDeathDissolves -SkipBuild
dotnet src/FixtureTools/bin/Release/net8.0/FixtureTools.dll verify-offline-native .probe-runs/candidate.mt2f <fresh-native.mt2f>
```

The explicit CLI horizon accepts 1..16 turns, budgets 1..60,000 ms per decision
and deployment rooms 0..2. Node/beam/action bounds remain 256/8/12, and the whole
battle stops at 256 decisions. Unsupported candidate reasons remain visible.
Existing live-advisor defaults are unchanged. Output files must be new.

The optional `--tactical` argument adds a deterministic long-horizon seed before
the existing strength/fixed-policy/beam seeds. It preserves tower attack by
skipping negative tower-status cards, concentrates units on the preferred floor,
prioritizes equipment/armor, then spends X buffs and unit abilities. Targets are
ranked by visible stats with oldest-unit identity breaking ties; no encounter or
card asset names enter this policy. The seed shares all node/time/action and
cancellation limits. The first complete winning seed may return immediately;
this is a bounded recommendation without an optimality claim. A seed that does
not win continues through the existing planner within the remaining budget.

```powershell
dotnet src/FixtureTools/bin/Release/net8.0/FixtureTools.dll plan-battle tests/fixtures/full-battle-progressed-ring8-gold-no-cards.mt2f .probe-runs/tactical-candidate.mt2f 12 10000 2 --tactical
dotnet src/FixtureTools/bin/Release/net8.0/FixtureTools.dll policy-battle tests/fixtures/full-battle-progressed-ring8-gold-no-cards.mt2f .probe-runs/policy-candidate.mt2f 2
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --tactical-planner-only tests/fixtures/full-battle-progressed-ring8-gold-frozen-win.mt2f
```

`policy-battle` freezes the seed's actions without search; its output is still a
model prediction. The planner's rolling 12-turn/10,000-ms candidate completed
50 decisions with zero unsupported reasons and model Pyre21 in 163.84 seconds,
including independent replay of all forecasts. This is separate from the
accepted external frozen route's natural native Pyre4 victory.

For native rolling acceptance, `Run-FullBattleProbe.ps1` and
`Run-RollingAdvisorBenchmark.ps1` accept `-PlannerTacticalSeed` and explicit
`-PlannerHorizon 12`. `Run-BattleAdvisor.ps1` exposes the same opt-in setting as
`-TacticalSeed -Horizon 12 -TimeLimitMs 30000`. Outer-boss benchmarks also require
`-OuterBossBirth -OuterBossTurns`; the interactive launcher combines these as
`-OuterBoss`. Manual and automated defaults
remain a three-turn horizon and 3,000 ms; the native benchmark may explicitly
allow 30,000 ms for its longer continuation. Evidence records this setting.

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
seconds, versus 45–48 seconds in the earlier in-process trial. These are initial
observations, not whole-battle acceptance or a latency guarantee. External
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
