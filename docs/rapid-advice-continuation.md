# Rapid advice continuation · Windows 0.2.18

The 0.2.17 live log contains a card-play invalidation followed by an EndTurn
invalidation without a publication between them, then a new 10-second search.
That sequence also occurs later in the same player session. It identifies an
input/publication overlap; the log alone cannot prove every input followed the
published route.

A deterministic regression using the captured native Conduit fixture reproduces
an independent defect in that overlap. Starting verification removed the last
published candidate and drained its deferred floor callbacks. A second input
cancelled that worker before publication, leaving only the old prefix hashes.
Even when those hashes matched again, the reused forecast could contain a stale
preview allocator and fail independent full-state replay.

The session now retains the published anchor and an ordered history of floor
callbacks and immutable captured checkpoints until a result is accepted. An
obsolete/cancelled worker consumes neither. Matching checks each checkpoint
against the original planned prefix, applies intervening selections in order,
and verifies the full remaining forecast from the latest exact native root.
Only the accepted worker's history prefix is removed; later views survive.
Ordinary inputs without floor callbacks keep the existing prefix-hash fast path.
Browsing itself still records O(1) events without capture, hashing or replay.

The focused regression deliberately makes multiple advised inputs, including
EndTurn, before polling any new publication. It covers no browsing, preceding
browsing, and browsing between inputs. All cases require one search, the correct
remaining step count, a clean published checkpoint, and independent exact suffix
replay. Existing full-state/RNG divergence, explicit refresh, in-flight browsing
and native selection callback checks remain applicable.

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --floor-browse-selection-only tests/fixtures/player-live-conduit-floor-scroll.mt2f
```

Actual divergence, exhaustion of the available forecast, and manual reset retain
their existing fresh-search behavior. This fix does not change the planner's
search budget, battle model or native input handling.
