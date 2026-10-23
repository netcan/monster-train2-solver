# Natural Fragile winning player acceptance

The later [three-turn/3000ms native pair](player-fragile-short-budget.md) also
wins with bottom preference and no extra tactical seed, then completes native
Standard settlement. This page retains the original longer-budget evidence.

`full-battle-player-boss-fragile-{search,replay}.mt2f` retain source
`full-battle-units-spells-and-junk-20261023-034629-462214c1` and fresh replay
`full-battle-units-spells-and-junk-20261023-040317-cc91e0af`.

Each restores the same natural Ring8 checkpoint, with seven preceding native
wins, twelve hash-verified payload files and a separate manifest. The original
Fragile has 3,000 HP/30 attack, the Pyre starts at 62/120 and the natural
Banished/Hellhorned deck is unchanged. No encounter, deck, HP or phase controls
are applied. Both load the actual separate player Advisor assembly.

The source executes 45 plays and nine EndTurns, winning at Pyre2. A fresh
native process executes the same 54 inputs and wins at Pyre2. All roots,
complete published forecasts, successors and RNG match. Both pass
`DEPTH-PASS`, settle terminal effects and exit normally with protected game
files unchanged; capture/mismatch/unsupported/pending counters are all zero.
Both casting and relentless phases, original aura, unit abilities, Titanite,
shield and the natural grafted Firebrand participate in the complete route.

Player pointer routing, pause/resume, hide/cancel/show, refresh and panel
interaction checks pass. The source records 56 publications and 603 pure
captures; replay records zero publications and 125 pure captures. The first
consumed publication searches 55 nodes and predicts the complete victory;
all 53 following publications retain its exact winning suffix with zero
search nodes/time. Historical plan provenance is retained by frozen replay.

Settings are twelve turns, a 30,000-ms search soft budget, bottom deployment
and tactical candidates. The first consumed total response is 51.09 seconds,
including verification; retained response median/nearest-rank P95 is
2.38/3.96 seconds. This proves the configured route; it does not imply that
the default three-turn/three-second budget will win, or that every choice of
deck, Covenant, refresh or deviation can recover a victory.

The route exposed two separately calibrated model differences: Titanite
rounds before melee/spell weakness, and temporary boss sweep previews hold
callbacks while still physically removing dead units. Their original
diagnostic captures remain separate; the failed three-mismatch native win
was not promoted to acceptance. See [damage ordering](titanite-combinations.md)
and [held preview callbacks](boss-preview-sweep.md).

Focused entry: `scripts/Check-PlayerAdvisor.ps1 -Case Fragile -SkipBuild`.
The gate requires two actual native wins, unchanged natural root attributes,
every publication predicting victory, exact suffix/state/RNG replay and
the tactical restoration/budget counterexamples. The older bounded loss pair
remains registered and continues to require losses. No full regression or
Mac build is performed.
