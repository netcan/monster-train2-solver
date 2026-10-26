# Lazarus League Spike with Juice

Campaign19 Ring4, LazarusLeague/LunaCoven, seed68586509, Covenant0.
Default release200 cannot plan its deployment because a newly upgraded
LazarusLeagueSpike carries ordered Consume1, PlayedCost/ThisTurn undying
scaling1 and Juice2. Card26 has exactly one permanent TraitJuice upgrade.

`player-lazarus-juice-spike-deployment-refusal-calibration.mt2f` retains the
original rejected deployment input and its actual native successor. The
known exact card signature allows that deployment to replay, with full
state/RNG checks and ten modified-signature rejections. The initial
signature-only commit retained a separate actual-cast guard.
The separate `player-lazarus-juice-spike-cast-calibration.mt2f` records six
legal prefix inputs and the actual card26 cast. On turn1 it spends4 ember,
scales the zero-base undying amount to4, doubles to8 and applies it to unit5
on room0. The card exhausts once and records its paid cost and play count.
All seven native successors/full forecasts/RNG and four immutable parallel
cast branches pass. Actual casting is now enabled for this exact signature;
the independent native replay now passes too. The original unupgraded
LazarusLeagueSpike three-action native fixture still passes. This component
pair enables the default fourth-battle campaign acceptance, not a battle win.
