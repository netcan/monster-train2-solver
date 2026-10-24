# Pyre-health early stopping · Windows 0.2.20

Player search defaults to `Search/StopAtBestPyreHealth=true`. A complete
`BattleWon` forecast stops both workers as soon as its final Pyre health equals
a conservatively established upper bound. The panel identifies this reason;
`POJU-ADVICE` logs `stoppedEarly` and `pyreHealthBound`. The default public model
API remains opt-in so existing fixed-budget consumers keep their behavior.

The objective remains victory first, then surviving Pyre health. This proves
that objective cannot improve within the modeled root, not global optimality
across secondary positional preferences, hidden information or unsupported
mechanics. A damaged winning forecast below the bound keeps searching.

The upper-bound inspection runs once on the background coordinator over the
complete immutable graph, including future waves, summoned units and generated
card definitions. Without train healing, current HP is the bound. Train healing
requires a captured, validated native resource maximum, which becomes the bound.
Ordinary unit healing cannot restore the captured native unhealable Pyre.

Unbounded permanent maximum-health changes, resurrection, direct health
replacement/stat swaps, unknown direct Pyre operations or ambiguous Pyre state
disable early stopping. Potential healing without captured resource limits also
disables it. Search then retains its usual time/node/step limits.

Workers share only an atomic success signal; this does not cancel the returned
winning plan. The existing independent full-state/RNG replay still runs before
publication. A sibling can finish its current atomic simulation before observing
the signal. User cancellation remains cancellation, and the UI never joins a
search worker. Verified continuations preserve early-stop provenance.

## Focused validation

```powershell
dotnet build src/ModelChecks/ModelChecks.csproj -c Release --no-restore
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --early-stop-only `
  tests/fixtures/full-battle-player-conduit-search.mt2f `
  tests/fixtures/full-battle-train-heal.mt2f `
  tests/fixtures/full-battle-player-cross-turn-scroll-search.mt2f
```

The native winning control reaches the same 80-HP score with 3 expanded nodes
instead of 43 in serial search; two workers jointly use 4–5 nodes (scheduling
dependent). Both returned routes pass independent complete state/RNG replay.
The checker also covers a lower-HP win that must keep searching, native PayOff
healing from 70/80 to 74/80, future-wave maximum growth, resurrection, missing
resource limits, custom evaluation, expiry, user cancellation, sibling success,
continuation metadata and root immutability. These are targeted checks; no full
fixture regression is required for this delivery.
