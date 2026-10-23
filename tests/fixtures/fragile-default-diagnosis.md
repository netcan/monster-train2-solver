# Natural Fragile default-budget diagnosis · 2026-10-24

Natural Fragile now has an accepted three-turn/3000ms native player victory,
independent replay and Standard settlement with bottom preference and the extra
tactical seed off; see [the short-budget pair](player-fragile-short-budget.md).
The shipped top-floor preference still loses in the rolling diagnostic below.
The earlier [twelve-turn/30000ms pair](player-fragile-winning.md) remains
separate evidence and is not needed to establish the bottom-preference win.

Two focused model diagnostics now work from that unchanged captured root:

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --deployment-forecast-only tests/fixtures/full-battle-player-boss-fragile-search.mt2f
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --rolling-player-forecast-only tests/fixtures/full-battle-player-boss-fragile-search.mt2f 256 2
```

The deployment comparison plays each floor's tactical deployment, then ends
turns without additional cards until three complete EndTurns. Every branch
passes independent complete-state/RNG replay and root immutability. The
short-horizon public score is saturated at the same value on all three floors:

| Room index (bottom = 0) | Score | Unclamped positional ordering | Inputs |
| --- | --- | --- | --- |
| 0 | -61501 | 3840.5 | 8 |
| 1 | -61501 | 3851.5 | 8 |
| 2 | -61501 | 3873.2 | 8 |

The ordinary default planner still selects room2 in this diagnostic. The
table compares deployment-only continuations; its lower positional value
does not prove that changing floors will win a complete rolling battle.

The rolling diagnostic uses 256 nodes, three turns, beam8, twelve actions
per turn, a three-second soft deadline, preferred room2 and no extra tactical
seed. It verifies every complete forecast before consuming it. Ordinary
suffixes last through the current EndTurn, winning suffixes can cross turns,
and defeat forecasts consume only their first input. A final independent
replay checks all consumed inputs, the complete state/RNG and the original
root. The original per-input `--rolling-forecast-only` mode remains available.

The latest offline run loses during turn7 after 48 inputs and seventeen
searches (442 nodes, 66.20 seconds including verification). All seventeen
forecasts and the full consumed route pass state/RNG replay. An earlier run
also loses after 48 inputs/seventeen searches, with 410 nodes. Soft-budget
node counts and chosen paths can vary with runtime timing; neither run is
a native default-budget acceptance or a universal impossibility proof.

## Short budget with bottom preference

The same ordinary planner wins this captured Fragile root when only its
preferred room changes to 0: 54 inputs, seven searches and Pyre2. Two runs
pass independent verification of every forecast and the complete consumed
route. They use 219/208 nodes (host timing can change the exact count), three
turns/3000ms, same-turn reuse and no extra tactical seed. This is model
evidence; its subsequent [native pair and Standard settlement](player-fragile-short-budget.md)
now pass independently with the same short-budget configuration.

The focused winning gate now rejects a replay-correct loss:

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --fragile-short-budget-planner-only tests/fixtures/full-battle-player-boss-fragile-search.mt2f
```

Changing the global default floor is not justified by this result: the
accepted natural Ring7 root loses with the same bottom preference (56 inputs,
25 searches), while its current top preference has an accepted default-budget
native victory. The shipped preferred-floor default remains top. A dedicated
native gate, `--player-boss-fragile-short-budget-pair-only`, requires actual
three-turn/3000ms wins on the original seven-win checkpoint, bottom preference,
no extra tactical seed, full native state/RNG/forecast replay and ordinary
continuation checks. It rejects the old twelve-turn/30000ms winning pair.

These commands diagnose model strategy, not live Unity scheduling or native
acceptance. A successful diagnostic exit means replay integrity passed;
read the printed outcome separately. The bottom-preference short-budget native
pair is now accepted separately; default top-preference Fragile quality remains
open. The accepted Ring7 default victory and Fragile long-budget victory remain
separate evidence. No full regression or Mac build is required for diagnosis.

## Ordinary-prefix timeout found in native play

The first fresh bottom-preference native run reached turn8/Pyre7 with 48
matched inputs, then failed to publish advice: five legal card choices used
the soft deadline before any complete EndTurn (`turns=0 nodes=5`). The failed
run is `full-battle-units-spells-and-junk-20261023-234552-2c334d45`; it is not
a completed battle or an accepted winning pair.

`advisor-ordinary-budget-failure.mt2f` preserves that exact unavailable root,
native error and capture provenance. The ordinary seed now finishes its
selected prefix with one atomic EndTurn only when the soft deadline expires
and no complete forecast exists. Shorter windows must be enabled; cancellation
and the hard node limit still take precedence. This does not turn the soft
three-second deadline into a bound on total response time.

`--ordinary-advice-budget-only` reproduces expiration after one and two card
choices and verifies complete forecasts by independent full-state/RNG replay.
Strict-horizon, hard-node, cancellation and root-immutability controls pass.
The fixture remains diagnostic evidence from an incomplete native run. A fresh
native source/replay pair subsequently wins at Pyre2 after 54 inputs and
continues through original eight-battle Standard settlement. Its turn8 root
equals the failed root in every state field and RNG; the fresh six-node search
publishes five legal actions plus one complete EndTurn, then retains the exact
winning suffix. See [short-budget native acceptance](player-fragile-short-budget.md).
