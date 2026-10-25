# Pyre card payment and the enchantment world · 0.2.69

Random sample8 Umbra/Stygian seed1347120220 reached Ring2 through normal rewards.
With PyreHealthForEmberCardCost, playing StarterIceSpear at zero Ember spent
one Pyre HP natively,75→74. The model initially paid correctly on live rooms,
but left the enchantment world's copy at75. A later room update restored that
old HP. The source stopped after12 inputs; it is neither a win nor a defeat.

Use the existing immutable unit replacement/rebase operation when paying,
before the queued OnHit callback and spell effects. It preserves both physical
and enchantment-world HP. No cost, eligibility, callback or target rule changes.

The original and fixed independent native observations replay the same12
inputs; the first11 match. At the boundary both processes spend one HP and
keep Ember zero. The checker preserves the original expected75 HP and permits
only the lost live/world HP difference; all other fields, RNG, input identities
and parent immutability must match. Both native runs preserve real files.

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll `
  --pyre-payment-world-pair-only `
  tests/fixtures/relics/modifiers/player-pyre-payment-world-calibration.mt2f `
  tests/fixtures/relics/modifiers/player-pyre-payment-world-replay-calibration.mt2f
```

The existing original native Pyre-payment battle (31 actions,6 turn checks),
25-card policy chain and pure affordability, X-cost, owner stacking, callback,
preview, lethal-payment and isolation checks pass. This closes the combination
mismatch. The committed0.2.69 payload also passes sample8 Ring2 actual advice
and independent frozen native replay:34 inputs,29 plays,5 EndTurns,Pyre71,
36 publications,3 searches,1296 independent mask contexts and10 train checks
per process. Roots,successors,forecasts and RNG match; all four failure counts
are zero. Full regression was not repeated.

The separate observer assembly must enable Conduit capture from the explicit
player-probe environment flag as well as its assembly-local player-session
flag. The first full trial won but recorded58 unsupported observer stages;
it remains rejected. The corrected capture passes the complete independent
pair above without changing the player's Conduit rules.
