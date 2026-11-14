# Inactive moon movement with original room death triggers

BoilerRoom and SacrificialChamber are the remaining original room attachments
that affect enemies through a character upgrade with an OnDeath trigger. In an
isolated no-cards battle, a cardless first-wave MageT1_Junker moves in and out
of each unchanged room attachment six times. The two original OnDeath upgrades
add and remove their native trigger while moon tracking remains inactive.

Two independent muted Instant game processes produced byte-identical archives
(SHA-256 `f6b0338b8e3004494c864f16a4fec6291903d37aaff0df9beb3c0a72a5117c4b`).
The checker compares all 12 complete native/model successors and unchanged
gameplay RNG, verifies trigger counts and stale Full understanding changing
from mask 6 to 5, runs four immutable branches, and protects preview, card
spawner, unknown room, upgrade identity and moon callback boundaries. The
allowance requires the two original room and OnDeath upgrade signatures.
Neither OnDeath trigger fires in this capture, and natural EndTurn ascension
remains separate evidence.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture room-moon-inactive-trigger-rooms-calibration.mt2f
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --inactive-trigger-rooms-only tests/fixtures/combat/rooms/room-moon-inactive-trigger-rooms-calibration.mt2f
```
