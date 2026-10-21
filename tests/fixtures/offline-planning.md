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
