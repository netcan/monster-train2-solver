# Held sweep callbacks in temporary boss previews

`boss-preview-sweep-calibration.mt2f` is the unchanged native diagnostic
`full-battle-units-spells-and-junk-20261023-033259-5b670c40` (352.24 seconds).
It replays all 54 player inputs on the natural Fragile checkpoint and wins
with Pyre2. Its historical three mismatch counters remain intact: two room
stages and their enclosing train phase. The enclosing runner rejects this
capture; it is a negative control, not accepted winning-player evidence.

At turn5, a sweeping enemy hits three allies while an Avenger queues melee
weakness and HP-scaled retaliation. Native `RunBossKillPreview` shares the
already running sweep queue. `RunTriggerQueue` defers its callbacks, while
`RemoveDeadCharacters` still completes physical removals. Ninety-four read-only
observations record temporary character flags and the original aura maps;
the three temporary sweep hits do not run the queued aura-changing retaliation.
A separate temporary Pyre hit retains the physical-death removal witness.
No death listener or native scheduling behavior is changed.

The old model drained the temporary callbacks unconditionally. It retained
`NextAction=2` on three living remote targets where native still held zero.
Simply skipping the combined drain/removal function instead broke temporary
Pyre death cleanup. The corrected temporary engine holds callbacks while
settling dead characters, preserving both boundaries.

The focused checker preserves the old mismatch counters, verifies the native
observation witnesses, recomputes all 86 room stages and 17 train phases from
their native inputs, and compares complete units/context/RNG against the
captured native states. Eight independent exchange branches also leave their
parent immutable. A fresh player search and independent winning replay are
separate acceptance requirements.

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll `
  --boss-preview-sweep-only tests/fixtures/bosses/outer/boss-preview-sweep-calibration.mt2f
```
