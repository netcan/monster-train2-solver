# Adaptive ordinary deployment · 2026-10-24

The interactive default's ordinary seed now compares alternative opening
floors when its complete initial forecast loses an already deployed unit.
Alternatives use unchanged legal cards and the same requested horizon,
evaluator, positional ordering, time/node limits and cancellation. A partial
alternative cannot replace a complete horizon. No encounter/card identities
or archived winning inputs choose a move. Openings without that predicted
loss keep their existing preferred seed; explicit non-top preferences,
strict-horizon callers and the optional tactical seed retain their behavior.

After deployment, this ordinary seed prefers the living champion's combat
floor. This preserves the chosen formation when replanning a later turn.
Legal enumeration and the existing beam remain free to choose other actions;
this preference does not force placements or reuse an unverified forecast.

The unchanged native Fragile seven-win root now wins in a rolling model
check with the configured top preference, three turns/3000ms and tactical
seed off: 54 inputs, seven searches and Pyre2. Each complete forecast and the
consumed route pass independent full-state/RNG replay; the original root is
immutable. The hard sixteen-node opening retains a complete verified
fallback. The accepted natural Ring7 root still wins at Pyre62 in sixty
inputs/eight searches; explicit middle-floor deployment, positional plateau,
ordinary timeout, cancellation and tiny-node controls also pass.

```powershell
scripts/Check-DefaultAdvice.ps1 -Case FragileStrategy -SkipBuild
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --natural-ring7-default-planner-only tests/fixtures/full-battle-player-natural-ring7-ordinary-search.mt2f
```

These results are model strategy evidence. A fresh actual-default player
source and independent native replay remain required before closing the
native Fragile quality gap. The time limit remains soft; Unity scheduling can
select a different bounded route. No full regression or Mac package is used.
