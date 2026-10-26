# Combat victory statistic membership

Fixed random sequence item19, LazarusLeague/LunaCoven seed68586509, reaches a
native first-battle victory using37 default10000ms/two-worker inputs. The final
EndTurn at turn4 trips three duplicate terminal comparisons: the old prediction
retains424 values/24 tracked cards, native has293/15. Preserve the failed trace;
it is not an accepted campaign pair or release validation.

Native UpdateDeckStats falls back to the permanent deck when CardManager owns
no live battle cards, deleting entries for nine generated infusions17..25. The
victory killcam has already cleared every battle pile. Release0.2.195 applies
the existing terminal-card statistic membership refresh to this combat victory
boundary. It retains every permanent statistic and changes no comparison or
normalization rules. Defeat, stalemate and nonterminal turns retain their rules.

All37 actual inputs/full successors/RNG and four detached EndTurns pass against
the preserved native counterexample. Historical nonterminal forecasts still
match; the entire old statistic mismatch is exactly the nine removed entries.
The accepted57-input Ring6 win/replay and26-input Ring7 defeat from sample18
also pass the targeted campaign checks, preserving complete state/forecast/RNG
and original budget classification.
Independent repaired native observation and a fresh default campaign pair remain
required before accepting this as a released whole-battle victory.
