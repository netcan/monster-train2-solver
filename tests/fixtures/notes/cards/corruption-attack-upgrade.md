# Merchant corruption attack on unit-card birth

The tenth natural Banished/Wurmkin sample's Ring7 opening contains First of
Kin5 with merchant upgrade `AddWurmScaleDamage`,DataId
`1b2642b2-3ae9-46e6-b7cf-f0fd3c4a53f8`. Its room-modifier interaction was
guarded; resolving the opening hand aborted background search.

An isolated native cast confirms the unit inherits one
`RoomStateSelfDamagePerCorruptionModifier`,coefficient5,divisor1. The upgrade
does not enter the character's applied-upgrade list: only its existing
ValorOnTurnStart appears there. Card birth now appends this known rule to
the unit's self-attack rules and preserves the applied-upgrade list. The
existing physical-room corruption evaluator computes its attack contribution.

Original and independent corrected processes take22.01/26.91s and preserve
the old rejection and repaired forecast. Their entire birth states and RNG
match,including First of Kin2 with base attack45 and health75. Eight parallel
branches leave their parent unchanged. The birth room has zero corruption;
this pair does not assert a native nonzero-corruption attack query.

The probe checks the original upgrade's one room-modifier definition,class
and coefficient. Changed definitions gain an additional guard; changed ID,
asset,statistics,traits or trigger signatures remain unsupported. The raw
`Upgrade room modifiers` marker remains captured so direct live-unit
upgrade/removal APIs retain their protection. This calibration opens only
the original unit-card birth path. Ring7 full-battle advice is being verified
from both continuous checkpoints; no complete random campaign is claimed.

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --card-corruption-attack-upgrade-only `
  tests/fixtures/cards/upgrades/player-corruption-attack-upgrade-calibration.mt2f `
  tests/fixtures/cards/upgrades/player-corruption-attack-upgrade-replay-calibration.mt2f
```
