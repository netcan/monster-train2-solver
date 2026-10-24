# Natural Fragile with a short planning budget

`full-battle-player-boss-fragile-short-budget-{search,replay}.mt2f` retain actual player
source `full-battle-units-spells-and-junk-20261024-000704-1b1412d8` (729.51
seconds). It restores the unchanged seed424244 natural Ring8 checkpoint with
seven prior native wins, original Fragile 3000 HP/30 attack and Pyre62/120.
The input save SHA256 is
`07884dc51c1354ee4668e277c87e9b733cd8156d49625d7f41848b5d3da5d449`.
No encounter, deck, health, phase or Covenant controls are applied.

The separate player Advisor uses three turns/3000ms, bottom preference (room0,
the first floor counting from the bottom), ordinary same-turn continuation and
no extra tactical seed. The source wins at Pyre2 after 45 plays and nine
EndTurns. All 54 native inputs match, terminal effects settle, the process
exits normally, protected originals remain unchanged and capture/mismatch/
unsupported/pending counters are zero.

The player records 56 publications, 56 fresh publication-root checks and 463
pure captures. There are 37 ordinary same-turn continuations and five winning
continuations with zero search nodes/time; twenty consumed forecasts explicitly
use a shorter complete window. The first consumed response is 6.49 seconds;
all 42 retained responses have median/nearest-rank P95 2.36/3.30 seconds.
The search budget is a soft deadline, not a three-second total response bound.

## Complete prediction at the previous failure boundary

The previous native attempt matched 48 inputs but could not publish at
turn8/Pyre7: five selected legal actions exhausted the deadline before any
complete EndTurn. Its exact unavailable root and provenance remain in
`advisor-ordinary-budget-failure.mt2f`; that incomplete native run is not
accepted as a completed battle.

The ordinary seed now finishes that prefix with one atomic EndTurn when no
complete forecast exists and shorter windows are allowed. Cancellation and
the hard node limit remain binding. In the new native source, the turn8 search
uses six nodes/3477.4ms and publishes a complete six-input, one-EndTurn victory.
The following five inputs retain its verified winning suffix. The focused
failure-root check also independently replays expiration after one/two legal
actions and preserves strict-window, hard-node and cancellation rejection.

## Independent replay and campaign settlement

Fresh process `campaign-ring9-20261024-001925-f3ae7fc9` executes the same 54
inputs and wins at Pyre2. All roots, successors, complete forecasts and RNG
match; each process has seventeen matched train phases, settled terminal
effects and four zero failure counters. Replay searches zero nodes and the
source retains eight fresh new-turn searches. The focused native recovery
check also requires the source's complete turn8 root to equal the original
failure root, including every state field and RNG, without normalization.

That replay process continues through the native EndGame flow and records a
Standard victory with all eight native battle wins, no cheat flag, normal
exit and protected originals unchanged. The dedicated runner validates source
acceptance, frozen inputs and settled-save hashes before writing
`player-pair-accepted.json`; the original input save remains unchanged.
The [accepted pair summary](player-fragile-short-budget-accepted.json) retains
both archive hashes and profiles.

`campaign-completed-fragile.zip` bundles the completed native save, final replay,
audit chain and native story snapshot. Its SHA256 is
`f60f40223a547c6add8b899fdc85afad59714faa2e8fde2ea35b2830108c23d3`.
A fresh isolated restoration verifies all 26 payload files plus the manifest.
The after-EndGame story snapshot still reports zero of nine extra Pyre Hearts,
story index0 and true-final feature locked; Standard settlement does not
establish natural true-final unlock.

```powershell
scripts/Run-FragileShortBudgetPlayerPair.ps1 -InputProfile <restored-ring8-profile> -NativeWorker 2 -SkipBuild
scripts/Check-PlayerAdvisor.ps1 -Case FragileShortBudget -SkipBuild
```

Accepted source/replay profiles can be supplied to the runner to repeat only
their gates rather than another battle. The prior twelve-turn/30000ms Fragile
win remains separate evidence. This result changes only the configured floor
preference: the shipped top-floor default remains unchanged, because the
natural Ring7 default-budget route loses with bottom preference. This does not
establish arbitrary decks or higher Covenant acceptance. A later
[adaptive ordinary deployment pair](adaptive-deployment.md) independently wins
with the shipped top-floor configuration, keeping this explicit-bottom pair
as separate evidence. No full regression or Mac build is required.
