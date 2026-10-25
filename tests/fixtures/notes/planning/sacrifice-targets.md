# Preserve the champion in resource-sacrifice seeds

Random06 Hellhorned/LazarusLeague, seed1848503146 Covenant0 Ring5,
10000ms/two workers/whole battle, failed natively on0.2.64 after49 inputs
(40 plays/9 EndTurns), Pyre0. All native successors/forecasts/RNG and four
failure counters passed; the original defeat is retained unchanged.

At turn5 the first input plays ImpProvisation14 on champion2. Native
CardEffectSacrifice only applies its ParamSubtype filter when recollecting a
RandomInRoom target; a dropped target can therefore sacrifice the champion.
The resource/draw seed ranked this spell as a buff and chose the strongest
champion (+10000 target preference). The native successor removes it before
the boss arrives. This is a strategy error, not a simulation mismatch.

0.2.65 classifies targeted Sacrifice/SacrificeOrFeed separately in both hand
candidate seeds. It defers a champion/Pyre target and prefers lower surviving
board strength among ordinary allies. This is conservative candidate ordering;
the complete action enumerator still permits champion sacrifice, including a
potential winning sacrifice/recursion combination. No card or encounter IDs
enter the ranking. Tactical targeted abilities use the same ranking path.

The native failure root now chooses another ally in both seeds, preserves the
champion and matches direct full-state/RNG replay without modifying its parent.
Four main/on-cast and weak-target/only-champion controls pass; original16
target-team, four ability-team and front-damage controls also pass.
The old49-input terminal defeat still passes its independent native checker.

An initial offline root search finds a53-input/9-EndTurn win with Pyre94,
117 nodes/7.99s (requested30s/512 nodes), verified in a separate process.
This is a model prediction; actual default-budget native acceptance follows
separately, and no new native victory is claimed by this component check.

Subsequent committed0.2.65 actual10000ms/two-worker advice and independent
native replay both win Ring5:41 inputs (32 plays/9 EndTurns), Pyre94,
full states/forecasts/RNG and zero failure counters. This differs from the
offline53-input route. The source takes238.59s with43 publications and six
search metric samples. Normal native rewards advance the same save to Ring6.
See `full-battle-random-campaign-06-ring5-budget10000-{search,replay}.mt2f`.

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --sacrifice-policy-only tests/fixtures/campaign/random/full-battle-random-campaign-06-ring5-budget10000-loss.mt2f
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --tactical-target-only
```
