# Guaranteed status chance in ordinary preview

The fifth random campaign (Stygian/LazarusLeague, seed972508374) avoided
playing units because Tethys's 100% OnAttacking poison15 was classified as
randomized status preview. All twelve examined opening alternatives hit
that guard; seven EndTurn inputs then lost the second battle.

Fixed chances >=100 with one fixed status and a deterministic target are
now accepted in ordinary preview. The unused native BattleTest chance draw
does not change the live Battle stream. Actual combat still draws its
per-target chance, and the explicit isolated preview protocol still mirrors
its draws. Fractional chances, ranged values, status pools and random
targets retain their existing guards.

Two independent native processes reproduce the short four-input sequence
through turn1 EndTurn. Both match every full successor, RNG and immutable
parent; neither enables UI or preview RNG isolation. The repaired raw trace
has four zero failure counters. Pure 100/200% controls compare against the
zero-chance deterministic case, while fractional and isolated-preview
controls preserve their original behavior. This component closes a search
coverage gap; it does not establish a campaign victory.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -SkipBuild -Fixture player-guaranteed-status-preview-calibration.mt2f
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --triggered-status-controls-only tests/fixtures/combat/status/full-battle-triggered-status.mt2f tests/fixtures/combat/status/full-battle-triggered-status-instant.mt2f
```
