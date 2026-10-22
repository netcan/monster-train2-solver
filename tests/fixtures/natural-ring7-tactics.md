# Natural seventh-battle tactics

Seed 424244 uses the unchanged natural checkpoint
`.probe-runs/campaign-ring8-20261022-072727-d98c7abf-before-ring7` at distance 6.
Deck, upgrades, health, enemy definitions and spawn queues are not edited.

`full-battle-natural-ring7-tactical-win.mt2f` is the fresh native replay from
`full-battle-units-spells-and-junk-20261022-104702-550e4157`. All 67 frozen
decisions (57 plays and ten EndTurns) match complete state and RNG; original
Rax is defeated and Pyre health is 46. CaptureFailures, Mismatches, Unsupported
and Pending are zero, terminal effects settle, the process exits successfully
and protected originals remain unchanged. Independent offline/native comparison
also verifies every unchanged frozen input and forecast. This first route uses
an explicitly selected deployment followed by the experimentally improved policy.

The general tactical seed now scores equipment by its captured stat/trigger
payload, actual ability ownership, front position and the oldest equipment it
would replace. Permanent, temporary and merged equipment upgrades participate.
Cooldown reduction therefore stays with an ability user instead of being
overwritten by a small health bonus. Silence has a cost for triggers/abilities;
retaliation and healing benefit the front unit. No card names or encounter IDs
are used by these rules.

Pure targeted debuffs favor enemy threats and bosses; damage still favors
low-health kills. Moving a friendly unit to the front favors health/armor and
OnHit triggers. A champion ability that would move it ahead of its existing tank
is deferred while a boss is in the room. These are optional heuristic candidates,
evaluated and independently replayed by the bounded planner.

The production policy wins from the original deployment root without manually
chosen inputs: 67 decisions, ten EndTurns and Pyre62. Bounded planning takes
72 nodes and about nine seconds; independent full-state/RNG replay, immutable
root, cancellation and shared node-budget controls pass. The existing natural
AllyBuffer planner still wins in 50 inputs. Model success does not replace the
actual player publication and independent native replay acceptance.

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --tactical-target-only
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --natural-ring7-planner-only `
  tests/fixtures/full-battle-natural-ring7-copy-on-play.mt2f
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --natural-ring7-tactics-only `
  tests/fixtures/full-battle-natural-ring7-tactical-win.mt2f
```

The actual player run and natural progression to Ring8 are being checked
separately. Fragile player capture remains guarded pending that acceptance.
No full regression or Mac build is used for this batch.

## Game-runtime budgets and natural continuation

The first real-player trial exhausted its 30-second budget before finishing
the tactical route. Its empty-tower fallback chose EndTurn during deployment.
The optional tactical seed now starts first and caches a complete forecast
after its first input on every planning turn. This retains a legal deployment
when the full seed times out, while the advice verifier still rejects incomplete
forecasts. One-node, sixty-node and post-deployment controls independently
replay the bounded fallbacks without changing their parents.

The seed also skips pure Dragon Hoard gold abilities that award zero gold.
This reduces the natural Ring7 winning route from 67 to 60 inputs, with Pyre62
preserved. The latest bounded model gate uses 66 nodes; the actual player
runtime has produced that full route in 27.6 seconds. Whole-battle player and
independent replay acceptance remain separate until both runs finish.

The 67-input production route has meanwhile passed a fresh native frozen
replay with Pyre62, every unchanged input/forecast and complete state/RNG.
Native campaign continuation then reached the natural Fragile encounter.
`campaign-progressed-ring8-fragile.zip` retains seven preceding native wins,
the save/choice history and that actual seventh-battle replay. All thirteen
manifest members pass restore/hash validation. Its SHA256 is
`6fb9f47594a138cbac6f759a41e62dbf5d7ff8354a93bc5d95690a4997494961`.

The campaign driver previously reopened an enhancer offer after its deck
picker had no selectable cards. It now records that declined offer for the
current distance and continues through native UI. A resumed native run declined
four such offers, reached Ring8 and preserved protected original files.
