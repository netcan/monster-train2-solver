# Replacing a start-of-combat attachment

The natural Hellhorned/Wurmkin fifth-battle alternative plays FightClub
card9 on the top floor at turn3. It replaces the ordinary FightClub slot
created once by the starting relic. The current attachment's identity
changes from relic upgrade f93ac9f7 to card6bc6afc8, while its +15 attack
payload remains and the existing Train Steward stays at attack41.

Relic validation previously required the initial attachment to remain
forever, rejecting this legal replacement. It now accepts the captured
positive per-floor card-attachment creation ordinal and one current
card-owned ordinary attachment. Missing initial metadata, missing source
card provenance, duplicate slots and malformed original payloads retain
their guards. Current attachment payload validation remains in the shared
room model.

Both native processes reproduce all20 inputs, full successors/RNG and
immutable parents; the original rejection is retained and the repaired
raw trace has four zero failure counters. All three original floor relic
controls pass. This is a replacement component, not a fifth-battle victory.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -SkipBuild -Fixture player-relic-attachment-replacement-calibration.mt2f
```
