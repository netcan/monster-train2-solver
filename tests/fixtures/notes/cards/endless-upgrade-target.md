# Prefer a new recipient for pure Endless upgrades

The native Remnant Pact Echo copy observation103940-cc228ec3 records card11
adding a second Endless stack to25attack Avenger50 while12attack Dreg51 lacks
Endless. `StatusEffectEndlessState.OnTriggered` sets the spawner card's return
pile once; it does not multiply returns by stack count. The native later Echo
also buffs enemy attackers through their Incant callback, so blindly repeating
the spell can spend ember and help enemies.

The strength and tactical candidate seeds now defer a pure single-target
Endless upgrade on a recipient with persistent Endless. They keep their usual
ranking among new recipients. The complete action frontier still contains the
legal repeated cast for callback combinations. Combat, card payment and Echo
execution semantics are unchanged. Mixed stat upgrades, on-cast effects,
scaling, expiring statuses and trait/trigger upgrades retain their old ranking.

Focused control `--endless-upgrade-target-only` uses the same complete native
successor, then isolates the real card in hand. Both seeds and all three floor
preferences choose Dreg51, preserve the repeated-cast branch and immutable
parent/RNG; three scope controls and four parallel choices pass. The original
48/49/49-input Pact Echo native pair remains a separate execution check.
This is strategy control evidence; a new campaign victory still needs native
execution and independent replay.
