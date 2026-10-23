# Adaptive ordinary deployment · 2026-10-24

The interactive default's ordinary seed now compares alternative opening
floors when its first-combat forecast loses an already deployed unit.
It compares complete two-turn windows before extending the first improvement
to the requested horizon. Alternatives use unchanged legal cards and the same
comparison horizon,
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
fallback and the safer bottom-room champion deployment. The accepted natural
Ring7 root still wins at Pyre62 in sixty
inputs/eight searches; explicit middle-floor deployment, positional plateau,
ordinary timeout, cancellation and tiny-node controls also pass.

```powershell
scripts/Check-DefaultAdvice.ps1 -Case FragileStrategy -SkipBuild
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --natural-ring7-default-planner-only tests/fixtures/full-battle-player-natural-ring7-ordinary-search.mt2f
```

The first Unity attempt `072553-16b60184` under
`full-battle-units-spells-and-junk-20261024-` exposed a scheduling gap in the
earlier three-turn deployment comparison. Its first consumed search expanded
only sixteen nodes, selected another complete route and eventually lost at
Pyre0 after 53 inputs/9 EndTurns. It exited normally in 748.85 seconds with
protected originals unchanged and all four failure counters zero. The
curated `full-battle-player-boss-fragile-adaptive-budget-loss.mt2f` is negative
strategy evidence, not a winning default pair. This is why comparison now
happens at the first combat and the sixteen-node control checks actual
champion placement as well as forecast completeness.

These results are model strategy evidence. A fresh actual-default player
source and independent native replay remain required before closing the
native Fragile quality gap. The time limit remains soft; Unity scheduling can
select a different bounded route. No full regression or Mac package is used.
