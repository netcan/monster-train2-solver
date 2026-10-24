# Default-budget strategy · 2026-10-23

The default three-turn/3000ms planner now tries useful card choices before
the empty-tower/no-card forecast. Its ordinary seed uses the existing general
equipment-replacement, ability-ownership, tank-placement and debuff targeting
rules. The separate long-horizon tactical option remains off by default.
No encounter IDs, card names or archived winning actions select a move.

A first deployment candidate retains a complete one-turn fallback when the
player allows shorter forecasts. Later ordinary continuations remain limited
to the current turn, then replan the requested three turns. Every candidate
still uses the shared node/time budgets, legality model and full replay before
publication. Tiny post-deployment node limits retain complete EndTurn windows;
pre-cancellation and an incomplete one-node deployment remain unpublishable.

## Model evidence

The original accepted six-win Ring7 deployment is taken from
`full-battle-player-natural-ring7-ordinary-search.mt2f`, whose native player
route lost. The new default planner wins in 60 inputs with Pyre62, using eight
searches and 260 nodes total. Each search requests three turns/3000ms with
the extra tactical option off; same-turn suffixes are followed as in the
player. Each forecast and the complete result pass independent full-state/RNG
replay, with an immutable original root. Timings were 3.05–3.44 seconds per
search; atomic expansions can exceed the soft deadline.

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --natural-ring7-default-planner-only tests/fixtures/full-battle-player-natural-ring7-ordinary-search.mt2f
```

Existing main/on-cast debuff targets, explicit shorter-window and atomic
deadline controls, the score plateau and both long-horizon natural Ring7 and
AllyBuffer victories pass focused checks. The old visible-strength-only policy
still loses the captured root in 54 inputs; it remains a diagnostic baseline.

## Native default-budget victory

`full-battle-player-natural-ring7-short-budget-{search,replay}.mt2f` retain
source `224740-2aad80ab` and independent replay `230338-09b7071d`, with profile
prefix `full-battle-units-spells-and-junk-20261023-`. Both load the untouched
seed424244 six-win checkpoint and original Level7BattleHordeSilence. Requested
settings remain three turns, 3000ms, preferred floor3 and tactical seed off.
No cards, health, encounter, prior wins or phase controls are authored.

Both complete 60 inputs (50 plays/abilities and ten EndTurns), defeat Rax and
win at Pyre62. Every root, input, complete published forecast, successor and
RNG matches between independent native processes, including nineteen train
phases per process. Native durations are 946.86/584.70 seconds. Both exit
normally, preserve protected original files, settle terminal effects, record
DEPTH-PASS and have zero capture/mismatch/unsupported/pending counters.

The source consumes ten searches, with nine fresh new-turn forecasts;
47 ordinary and three winning continuations skip search. There are 62
publications, each with a fresh full-root check, and 520 pure captures.
Eighteen consumed forecasts explicitly use complete shorter windows, with
one or two turns instead of the requested three. Consumed searches take
3.00–5.64 seconds: the configured deadline remains soft and a single atomic
expansion can exceed it. This acceptance does not promise three-second
end-to-end latency or an unchanged three-turn horizon after a timeout.

The source uses the earlier state-key implementation; replay loads the
optimized getters/hex encoding and the tiny-node fallback guard. These
changes preserve this source's complete states/RNG and native replay. The
separate [optimized player pair](state-key-performance.md) also exercises new
search/publication, ordinary continuation and floor browsing in Unity/Mono.

The original default losses remain registered. This closes the default-budget
native-victory gap for this specific natural Ring7 checkpoint, not every
advanced deck, boss or Covenant combination. Natural Fragile now also has a
[separate short-budget native victory](player-fragile-short-budget.md) with
bottom preference. Its configured top-floor default now also wins after
[adaptive opening comparison and independent native acceptance](adaptive-deployment.md).

Focused gate: `--player-natural-ring7-default-win-pair-only` followed by the
two archives. `Check-DefaultAdvice.ps1` selects Strategy, Keys or Native
checks; archive hashes and curated registrations are also checked separately.
No full regression or Mac package is used.
