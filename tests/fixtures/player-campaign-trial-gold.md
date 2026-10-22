# Natural Ring7 player battle with the trial-gold relic

Seed42's native campaign generated `Level8FinalBossCorrupter` at final pool
index2. Two simple deployment variants won six battles and lost Ring7. The
higher-health original Ring7 input has Pyre120, thirty cards and six completed
native victory records, with `wasDevCheatUsed=false`. No encounter, roster,
enemy health or rewards were substituted.

`campaign-progressed-ring7-trial-gold.zip` preserves that input and the native
choice/hash ancestry. Export and restoration verify all nine payload files;
archive SHA256 is
`0efa61d2abe83c442bf0a8628a2d53d5305cb73002da02a6eeb1842f2dacbfc6`.
The restored save SHA256 is
`b843b7196256e8f3877976c1a6ecc87db6486d6ce96258e1ab8776639512f610`.

The acquired original `ApplyUnitUpgradeWhenRich` only has
`RelicEffectModifyTrialGoldRewards`. Although its +50% trial reward consumer
already passed 66 original native display/grant observations, battle validation
rejected this effect and the actual player panel could not recommend an action.
That integration guard is corrected without suppressing unknown additional
callbacks. The existing six-owner campaign matrix still passes.

The separate, unmodified player advisor searches twelve turns with a 30,000 ms
soft budget, tactical candidates and preferred bottom deployment. It finds a
winning plan and executes all fifty inputs: 42 paid plays and eight EndTurns,
winning `Level7BattleHordeSilence` with Pyre84. Every subsequent publication
retains the exact winning suffix. There are 52 publications, 527 pure captures,
zero purity failures and successful native pointer/pause/hide/refresh checks.
Measured publication response median/P95 is 2.03/2.65 seconds; the first accepted
publication takes 27.35 seconds, including 16.05 seconds of search. These
observations do not establish default short-budget winning quality.

Source: `full-battle-units-spells-and-junk-20261023-052042-7d894e7d`.
It exits normally after 553.49 seconds with `DEPTH-PASS`, all four failure
counters zero, settled terminal effects and unchanged original Steam files.
`full-battle-player-campaign-trial-gold-search.mt2f` contains the complete
fifty-input native trace and all published full forecasts.

The independent fresh player replay
`full-battle-units-spells-and-junk-20261023-053023-359aa850` also wins with
Pyre84 and all four failure counters zero, exits normally after 380.43 seconds
and leaves original Steam files unchanged. It publishes no suggestions and
performs no new search; its 172 player captures are pure. The source and fresh
replay use the identical restored native save hash above.

The native campaign continuation now passes in
`campaign-ring8-20261023-054016-2d0cf43c`: all fifty frozen player inputs replay,
the original post-battle rewards/map/merchant choices continue, and the native
Ring8 checkpoint retains seven original victories and Pyre84. Its 72 audit
events end with `complete`; the game exits normally, original Steam files are
unchanged, the injected player plugin is archived and the original worker
configuration state is restored. An additional preflight now rejects a losing
source or an earlier-ring replay with outer-boss probes before launching.
The ordinary non-replay Ring1 startup also passes in
`campaign-ring1-20261023-054820-b0b11296` with 28 audit events, a normal exit
and unchanged original files; this compatibility check runs no battle.

The initial development-only diagnostic
`campaign-ring8-20261023-053047-5333097c` failed before its first input because
the actual player and development captures have distinct card identity spaces.
`Run-CampaignProgress.ps1` now detects player-origin frozen plans, loads the
actual player assembly with its captured search configuration, keeps the
original strict full-state/RNG comparison, and restores the isolated worker's
configuration/plugin state afterward. No identity differences are ignored.
`campaign-ring8-20261023-053300-6672fed2` separately replayed all fifty inputs
and won, but export correctly failed because the caller supplied the Ring8
birth probe for Ring7. It is retained as a failed diagnostic, not accepted
campaign progress; the corrected continuation omits those probes.

Probe archives now additionally retain a read-only `NativeRunInput` observation
at their first valid checkpoint: actual loaded seed, distance/scenario, cheat
flag, initial gold/Pyre and all preceding native battle records. This does not
change native state, rewards, listeners or RNG, and is excluded from production
player captures. The earlier search trace predates that additive field; the
fresh replay supplies it and must match the search trace's full roots and RNG.

Focused acceptance passes: both complete native input chains, every full
published forecast, exact retained winning suffixes/final scores, pure player
UI observations and the fresh native six-win campaign history. The checker is
`--player-campaign-trial-gold-pair-only <search> <replay>`; the catalog verifies
all 1072 archive hashes and registrations without simulating unrelated battles.
