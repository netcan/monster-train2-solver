# Player forecasts under a small budget

`player-fragile-budget-boundary.mt2f` preserves the deployment root and original
unavailable root from native profile
`full-battle-units-spells-and-junk-20261022-163019-b63a687f`. The real player
assembly executed fifteen cards/two EndTurns with exact successors, zero
capture/mismatch/unsupported/pending counters, nineteen publications and 214
pure captures. On turn two its three-turn, three-second tactical search stopped
after two turns and three nodes. The player correctly refused that incomplete
requested horizon. This source run did not finish; the archive explicitly
retains its error and `SourceRunCompleted=false`, and is not a full-battle pass.

Player search now retains a complete shorter turn window when its time or node
budget expires before completing the requested horizon. It uses the same search
and budget. The first step remains an independently replayed legal input; the
whole shorter forecast must reproduce its state and RNG. The result keeps
`CompletedHorizon=false` and marks `UsedShorterHorizon=true`, with separate
requested/completed turn counts. The panel displays those counts and
`预算内缩短`. Neither an unfinished deployment action nor cancellation qualifies.
Strict planner callers retain their previous behavior, and a complete requested
forecast always takes precedence over a shorter one. Winning continuations
still require an actual verified victory.

The focused check uses deterministic one/two-node budgets on the captured live
root to verify complete one/two-turn windows for a three-turn request, full
state/RNG replay and immutable roots. It also checks strict rejection, zero-turn
deployment rejection, cancellation, tampered full state, a complete requested
window and background player publication. Existing advice-session and retained
winning-continuation checks remain separate. Full native player acceptance is
recorded with the Fragile source/replay pair, rather than inferred from this
failed source or the model-only budget controls.

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --shorter-advice-budget-only tests/fixtures/bosses/fragile/player-fragile-budget-boundary.mt2f
```
