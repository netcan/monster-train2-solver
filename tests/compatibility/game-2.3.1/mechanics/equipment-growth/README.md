# Game 2.3.1 equipment growth evidence

`full-battle.mt2f` is the untouched binary native capture from
`.probe-runs/full-battle-units-spells-and-junk-20261114-125708-7cfaaf97`.
`manifest.tsv` fixes its size and SHA256. The game module is
`bf93f060-beda-46b1-b808-95ebf1e9a99d`; native exit status, DEPTH-PASS,
unchanged player files and all four zero capture counters passed.

The dedicated checker validates the four original relic owners and payloads,
14 births, owner ablations, Vanguard gain/loss, and three permanent-health
encounter callbacks at +8. It also replays 24 card plays and five EndTurns from
the root and a middle decision, including 16 independent immutable branches.
The matching 2.2.1 fixture retains its +6 expectation.

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --original-equipment-growth-only tests/compatibility/game-2.3.1/mechanics/equipment-growth/full-battle.mt2f
```

This accepts these four original owners on 2.3.1. New half-health Deathwish,
new-statistic trait callbacks and other changed/new owners remain separate.
