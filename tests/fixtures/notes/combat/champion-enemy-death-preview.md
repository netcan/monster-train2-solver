# Unobserved champion enemy-death fallback

Sample25 Underlegion/Pyreborne1860103187 Ring2 source231033-8ed6457e differs
at EndTurn input9. The original two-stack MonsterChampionUnderlegion1 enemy
death fallback creates an unowned Fun Guy in the subsequent combat preview.
Its temporary object is destroyed before native decision capture assigns an
identity: native NextUnitId13 and no point13, versus historical14 and an empty
SpawnedInPreview point13. All other fields match after only this normalization.

Release263 extends unobserved cardless preview retirement to that exact
original card0422f6ca-896c-4676-a01f-71d8d4b232fb, two-stack room/player/horde
signature and raw3/4 unupgraded Fun Guy. Unknown cards/assets/stack counts,
explicit summon observation and durable enchantment references keep identities.

Original native component231250-527c0331 and fixed-build independent native
component232349-1f39b742 preserve all nine complete roots/successors/forecasts/
RNG. Four immutable parallel EndTurns and four identity retention controls pass.
The previous49-input Green Recruit and45-input egg pairs remain passing.
Source revisionb1b172f1 is frozen separately from subsequent curation edits.
The following actual10000ms/two-worker default Ring2 source wins with31 inputs
and Pyre80; its normal independent reward replay is checked separately.
