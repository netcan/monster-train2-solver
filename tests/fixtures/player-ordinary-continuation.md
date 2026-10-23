# Same-turn advice continuation · 2026-10-23

Following an ordinary, nonwinning forecast no longer searches again after every
card. The complete forecast is replayed once before publication; an exact
settled successor can retain its immutable suffix until this turn's EndTurn.
All state, histories, costs, resources and RNG remain keyed. Only the next
unused preview-copy allocator can be normalized. Divergence, undo, explicit
refresh, cancellation and backward moves require a fresh search. Defeat and
stalemate forecasts are not retained.

The next turn starts a new forecast with the original configured lookahead,
instead of reusing a shrinking ordinary horizon. Complete winning proofs can
still continue across turns. Shorter-horizon and budget flags remain visible;
the panel labels ordinary reuse `沿用已验证步骤`. A fresh full-state observation
still guards every publication. Capture/verification work remains, so this is
not a promise of zero input-to-panel latency.

Actual player defaults: three turns, 3000ms, tactical seed off; native seed
424242 and original Level1BattleJunker, with seven floor switches including one
while searching:

| Evidence | Source | Independent frozen replay |
| --- | --- | --- |
| Profile suffix, prefix `full-battle-units-spells-and-junk-20261023-` | `220306-9194bc2d` | `220629-b5df5ab1` |
| Native seconds | 154.28 | 73.95 |
| Inputs / plays / EndTurns | 24 / 18 / 6 | 24 / 18 / 6 |
| Outcome / final Pyre | win / 76 | win / 76 |

The source consumes nine ordinary continuations and eleven winning
continuations, all with zero search nodes/time. Three post-EndTurn ordinary
boundaries start fresh searches, each retaining the full three-turn request.
Ordinary retained responses take 456–838ms, median 634ms, including capture and
verification. Seven floor switches still add zero searches. All 32 source
publications have fresh full-root checks; 223 captures retain zero purity
failures. Both processes exit normally, preserve protected original files,
record DEPTH-PASS and have zero capture/mismatch/unsupported/pending counters.
Every input, full forecast, successor and RNG matches in the independent
native replay, together with 11 train phases per process.

The previous per-input-search run used 28 inputs and finished with Pyre 80.
These paths differ, so their wall times are not a controlled speedup or
strategy-quality comparison. The measured reduction here is the elimination
of search work on nine verified same-turn continuations.

`Check-OrdinaryAdvice.ps1 -SkipBuild` verifies archive hashes, focused
continuation/horizon/concurrency controls, the native pair and floor browsing.
`--winning-advice-only full-battle-player-boss-search.mt2f` also passes all
50 native steps. That historical test now calibrates its final unused preview
allocator against an independent model replay before strict publication
verification; the production full-state verifier is unchanged. No full
regression or Mac build is involved.

A concurrent natural Ring7 default-budget experiment
`full-battle-units-spells-and-junk-20261023-220306-98231be1` reaches a later
Endless TrainSteward resummon and records an action mismatch: the native unit
receives its StewardsQuarters multistrike, while the modeled unit omits it.
That partial trace is a diagnostic observation, not accepted native evidence.
