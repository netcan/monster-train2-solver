# Inactive moon movement in original rooms

The original EnchantedChamber, Infectory, SpellSanctum, TrapChute and
ZoneOfSilence each affect enemies through a character upgrade. An isolated
no-cards battle attaches each unchanged original room to floor 0 and moves a
cardless first-wave MageT1_Junker between floors 1 and 0. Moon tracking stays
off. Each room contributes six Bumps: Full entry/exit, stale New entry/exit,
stale New exit after a Full reset, and New return. The 30 captured transitions
include the original ZoneOfSilence silence status and full room context.

Two separate muted Instant game processes produced byte-identical archives
(SHA-256 `5ccd7128759688cfb02af9ca9cd830f4e04616a77b0cd316e1328523ae905931`).
The focused checker compares complete native and model train states after each
action, confirms unchanged gameplay RNG, runs four immutable parallel replays,
and checks unknown room identity, card spawner, preview and moon callbacks.
The model's movement allowance is limited to the five room and character
upgrade signatures, a cardless enemy and no moon callbacks. This component
does not fire room death effects, spell upgrades or abilities, and does not
exercise natural EndTurn ascension.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture room-moon-inactive-room-batch-calibration.mt2f
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --inactive-room-batch-only tests/fixtures/combat/rooms/room-moon-inactive-room-batch-calibration.mt2f
```
