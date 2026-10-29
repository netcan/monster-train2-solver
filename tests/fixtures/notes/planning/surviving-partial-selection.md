# Bounded whole-battle forecast selection

The source is a native ring 6 deployment for Pyreborne/LunaCoven, seed
1332512341, after a separately refined ring 5 win and rank 2 reward choices.
Both retained archives are model-only predictions from the identical captured
root and RNG. The 3-second search produced a four-turn surviving prefix; the
30-second search reached its 256-node cap after about seven seconds and
returned an eleven-turn defeat. A 30-second 2048-node experiment also returned
a defeat after 702 nodes. None of these proves the battle unwinnable.

Before 0.2.404, a whole-battle worker selected its best terminal defeat even
when the bounded search had already found a living complete-turn prefix.
When time, node, or step limits stop a whole-battle search, it now prefers a
living prefix over a known defeat, while retaining terminal wins as the first
choice. The prefix remains explicitly incomplete, ends at an EndTurn, and is
independently verified before publication. Strict whole-battle callers and
finite-horizon behavior retain their original completion requirements.

The focused test replays both historical predictions from the same native
root, checks the repaired parallel search and strict caller, validates the
selected full state/RNG with immutable input, and checks advice publication.
This tests selection semantics, not a victory or an optimality guarantee.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture surviving-prefix-root-original.mt2f
```
