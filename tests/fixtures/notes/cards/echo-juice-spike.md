# Triple Juice Spike retained by Echo

Campaign18 Ring8, seed1171280461, Covenant0, release200. The original
deployment search rejects a new combination after45 supported inputs:
JuiceSpike with triple Juice, HealPyrePerEmber and one TitanCurse.

`player-echo-juice-spike-copy-calibration.mt2f` records46 native inputs.
Turn9 spends5 ember and remembers original22 as copy45, retaining its raw
valor effect, scaling query, both permanent upgrades and temporary curse.
The preview fires OnCast too: it heals preview Pyre HP from the live tower
HP even when live healing is disabled. This rule is limited to the captured
top-floor Echo with an otherwise empty, untriggered Pyre room above it.

`player-echo-juice-spike-endturn-calibration.mt2f` extends the same route
through actual pre-combat:47 inputs. Its original model boundary is guarded.
Native pre-combat increases the target's valor152 to212 and heals Pyre27 to32,
recording copied card45 after original22. Actual combat reaches turn10 with
Pyre4. The earlier draft used the main effect list again when resolving
OnCast; explicitly selecting OnCast avoids duplicating valor and skipping
the heal. This draft failure was caught before enabling the real callback.

Focused checks compare complete successors and RNG, exercise four parallel
immutable cast and EndTurn branches and reject ten mutated card/relic
signatures. The old WaxenSpike pair passes. Independent recast replay is the
next acceptance boundary; this partial route is not a battle win.
