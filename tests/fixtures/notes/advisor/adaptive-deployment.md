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
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --natural-ring7-default-planner-only tests/fixtures/campaign/progression/full-battle-player-natural-ring7-ordinary-search.mt2f
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

## Actual-default native source

Fresh source `full-battle-units-spells-and-junk-20261024-074031-4cf932a7`
now wins the unchanged natural seven-win checkpoint at Pyre2 after 54 inputs
(45 plays/abilities and nine EndTurns). Its actual player settings remain
three turns/3000ms, configured top preference (room2, third floor counting
from the bottom) and tactical seed off. The adaptive opening places the
champion on the bottom floor without changing that configuration. No deck,
boss health, encounter, Covenant, phase or prior-win controls are applied.

All consumed inputs and complete forecasts match native state/RNG. The source
exits normally in 801.41 seconds, preserves protected originals, settles all
terminal effects and records zero capture/mismatch/unsupported/pending counts.
There are 56 publications with 56 fresh full-root checks and 463 pure captures.
Eleven consumed searches produce the route; 39 ordinary and four winning
continuations reuse verified suffixes with zero search work. Twenty-three
consumed forecasts use explicitly shorter complete windows. The first search
expands sixteen nodes in 3377.7ms, with 6775.5ms total response; consumed searches
range from 3072.1 to 4054.9ms. This is a soft search deadline, not a three-second
total response guarantee.

The source archive is 135209 bytes/21221 unique nodes, SHA256
`1f8413d938969ab200f76b1c668288e79b91f257064cd096e9e454c48570fd99`.
Fresh process `campaign-ring9-20261024-075356-46e33e2f` independently executes
the same 54 inputs and wins at Pyre2. Every input, root, successor, complete
forecast and RNG matches; both processes have seventeen matched train phases,
normal exits, protected originals unchanged and four zero failure counters.
The dedicated actual-default pair gate passes. The source retains eight fresh
new-turn searches; defeat forecasts still search again after each input.

The replay also completes the original native eight-battle Standard victory,
with no cheat flag. The runner verifies the unchanged original input, frozen
source and final settled-save hashes. Final save SHA256 is
`3b872d16a998ae83dedff349b5bd58c2c897e8624d3f1d121f921a1412179846`.
See the [accepted pair summary](../../evidence/bosses/player-fragile-default-accepted.json).

Curated archives are `full-battle-player-boss-fragile-default-{search,replay}.mt2f`.
Replay is 131276 bytes/20974 nodes, SHA256
`f15b1bd4f77b6e6f7473163d50f930b452e141987812c77484b50eef993c611d`.

```powershell
scripts/Check-DefaultAdvice.ps1 -Case FragileNative -SkipBuild
```

This closes the default-budget native-victory gap for this natural Fragile
checkpoint. The earlier accepted Ring7 default victory and its current model
non-regression control remain separate evidence. Other decks, higher Covenant,
natural true-final unlock and Mac runtime acceptance remain separate goals.
No full regression or Mac package is used.
