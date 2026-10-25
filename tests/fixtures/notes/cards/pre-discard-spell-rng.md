# Preserve pre-discard relic RNG during spell cast tests

The retained metagame campaign's sixth battle,Banished/Pyreborne424245,
Covenant0,has a Battle RNG mismatch at input15. Card23 is SpikeOfTheBanished,
an X-cost Valor spell targeting champion2. Four energy is paid and Valor
rises9->29; the spell is consumed. ChanceToNotConsume performs a failed50%
roll before spell cast testing. The previous model overwrote that advanced
RNG with the earlier casting-context snapshot.

0.2.75 keeps the pre-play ownership and hand snapshot required by cast tests,
but supplies the current Battle RNG after the pre-discard callback. The
historical forecast differs only in Context.BattleRng and the matching
Battle RNG stream: one Range(0,100) call from
1104874222,3905334759,1589918500,890083637 to
3905334759,1589918500,890083637,2887280896.

The original and repaired independent native15-input observations reproduce
every root,successor,full state and RNG without mutating parents. The original
rewound forecast is retained and specifically checked. Existing suppression
controls cover49/50,sticky Test,body/manager reset,preview,exclusions and16
immutable branches. The independent repaired observation uses SuperUltra;
an initial Instant attempt stopped at input7 because an equipment attachment
was captured before settling and is not counted as an accepted observation.

Curated pair:cards/traits/player-pre-discard-spell-rng[-replay]-calibration.mt2f.
This closes one RNG mismatch,not the sixth battle or natural story unlock.

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --pre-discard-spell-rng-only tests/fixtures/cards/traits/player-pre-discard-spell-rng-calibration.mt2f tests/fixtures/cards/traits/player-pre-discard-spell-rng-replay-calibration.mt2f
```
