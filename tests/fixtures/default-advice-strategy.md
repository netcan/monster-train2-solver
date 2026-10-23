# Default-budget strategy · 2026-10-23

The default three-turn/3000ms planner now tries useful card choices before
the empty-tower/no-card forecast. Its ordinary seed uses the existing general
equipment-replacement, ability-ownership, tank-placement and debuff targeting
rules. The separate long-horizon tactical option remains off by default.
No encounter IDs, card names or archived winning actions select a move.

A first deployment candidate retains a complete one-turn fallback when the
player allows shorter forecasts. Later ordinary continuations remain limited
to the current turn, then replan the requested three turns. Every candidate
still uses the shared node/time budgets, legality model and full replay before
publication. Tiny post-deployment node limits retain complete EndTurn windows;
pre-cancellation and an incomplete one-node deployment remain unpublishable.

## Model evidence

The original accepted six-win Ring7 deployment is taken from
`full-battle-player-natural-ring7-ordinary-search.mt2f`, whose native player
route lost. The new default planner wins in 60 inputs with Pyre62, using eight
searches and 260 nodes total. Each search requests three turns/3000ms with
the extra tactical option off; same-turn suffixes are followed as in the
player. Each forecast and the complete result pass independent full-state/RNG
replay, with an immutable original root. Timings were 3.05–3.44 seconds per
search; atomic expansions can exceed the soft deadline.

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --natural-ring7-default-planner-only tests/fixtures/full-battle-player-natural-ring7-ordinary-search.mt2f
```

Existing main/on-cast debuff targets, explicit shorter-window and atomic
deadline controls, the score plateau and both long-horizon natural Ring7 and
AllyBuffer victories pass focused checks. The old visible-strength-only policy
still loses the captured root in 54 inputs; it remains a diagnostic baseline.

This section is model evidence only. Actual player publication and independent
native replay of the new default route are still required before closing the
Ring7 default-budget victory goal. No full regression or Mac package is used.
