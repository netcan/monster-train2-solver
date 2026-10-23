# Native Covenant run startup

`StartingCovenantLevel` selects a native rank in the disposable RunSetup
screen using its original `HandleAscensionLevelChanged` callback. The game
then uses normal `SaveState`, `StartingConditions` and `StartRun` logic.
The probe neither activates Covenants after startup nor invokes the startup
deck effects. Selection records the previously unlocked maximum and makes
no claim that the disposable profile earned the requested level.

The original rank-one definition is the sole cumulative native owner.
Read-only hooks observe exactly one original `RelicEffectPurgeStarterDeck`
and one `RelicEffectAddCardSetStartOfRun`: fifteen cards become the single
champion, then twenty cards. Purging preserves all sixteen RNG streams;
the original card-set generation runs normally and its before/after RNG
is recorded. The rebuilt deck exactly equals the actual starting deck.
The production model consumes captured battle/deck state; it never replays
these campaign effects during search.

`full-battle-native-covenant-startup-01.mt2f` comes from
`full-battle-no-cards-20261023-105416-e7548b0f`. The native process exits
normally after 47.08 seconds, with protected files unchanged and all four
failure counters zero. It wins in seven EndTurns with Pyre10. All native
waves, complete turns, fourteen train stages, 57 room stages, state/RNG,
and sixteen independent immutable full chains match. Pure native stat
queries preserve their frames and all sixteen RNG streams; spoofed wave
origins remain rejected.

Run `ModelChecks --native-covenant-startup-only <archive>`. Real player
advice and independent frozen replay, higher cumulative ranks, natural
campaign completion and earned unlocks remain separate acceptance targets.

The capture also records the original Covenant catalog and effect signatures,
so subsequent batches can distinguish battle effects from campaign-only
effects without inferring behavior from display names.
