# Player floor browsing · 2026-10-23

Selecting a floor used to invalidate advice immediately, cancel its worker and
restart search. The overlay now retains its displayed suggestion during a pure
browse and observes the settled native root, including while search runs.
Only the known selected floor and next unused preview-copy allocator can be
normalized for reuse. Costs, card identities, resources, histories, existing
physical/preview references and every RNG remain compared. Normal full-state
search keys are unchanged.

The worker independently replays the retained line from the new full root and
requires the same complete outcome/state before publishing. A differing cost
or outcome requests fresh search and hides the previous forecast. Real plays,
EndTurn, undo, restart, refresh and pause retain their invalidation behavior.
The overlay says `沿用当前建议`; it does not claim a new search or an immediate
zero-cost capture. Rebased plans expose zero search nodes/time.

Actual player defaults (three turns, 3000ms, tactical seed off), seed 424242,
original Level1BattleJunker:

| Evidence | Source | Fresh replay |
| --- | --- | --- |
| Profile suffix, prefix `full-battle-units-spells-and-junk-20261023-` | `213919-42f9c530` | `214501-1253740b` |
| Native seconds | 226.26 | 94.92 |
| Inputs / plays / EndTurns | 28 / 21 / 7 | 28 / 21 / 7 |
| Outcome / final Pyre | win / 80 | win / 80 |
| Native user floor switches | 7 | 7 frozen UI inputs |

One switch occurs during actual search; six follow published advice. All seven
keep generation 9, reuse the verified line and launch zero new searches.
The six ready-switch checks take 1684–2022ms (median 1787ms), including native
frames, capture and background verification; the existing advice remains
visible throughout that work. This does not promise instant full-state capture.
The frozen process repeats the seven UI inputs before executing the advice;
every action root, successor, full forecast and RNG matches. All 26 native
train phases match. Both processes exit normally, retain protected originals,
record DEPTH-PASS and have zero capture/mismatch/unsupported/pending counters.

Source records 36 publications, 245 pure captures and seven floor reuses.
Hashes and original profile names are registered in `manifest.tsv` alongside
`full-battle-player-floor-browse-{search,replay}.mt2f`.

Run `scripts/Check-FloorBrowseAdvice.ps1 -SkipBuild` for the archived pair and
focused ready/in-flight/nonwinning/resource/cost/RNG/input controls. Native
reproduction adds `-PlayerAdvisor -RollingAdvice -AdvisorFloorBrowse` to
`Run-FullBattleProbe.ps1`; the flag requires both player and rolling drivers.
No full regression or Mac packaging is involved.

An additional historical check,
`--winning-advice-only full-battle-player-boss-search.mt2f`, fails its initial
complete-plan reproduction on both this version and an isolated build of
baseline `74f9272c`. It remains an existing calibration/fixture issue; the new
browsing tests neither accept that old plan nor weaken full-state verification.
