# Ascend component destruction boundary

Sample20 (Underlegion/LunaCoven, seed1043013037) reaches a natural third-battle
win in50 inputs under release0.2.212. Player decisions and complete turn-end
successors match, but Ascend component13 spans Unity's deferred OnDestroy:
old friendly weak references7/36/37 clear while enemy51 remains. The old raw
trace remains a failed component comparison and is not accepted as a campaign
ring. Removing all absent actors would incorrectly remove that enemy too.

The corrected release0.2.215 changes only the explicit isolated death-settlement protocol.
Before capturing Ascend's input, it awaits original OnDestroy for known actors
already at DestroyedState.Destroyed whose native weak reference is still non-null.
It records their IDs and the frame count, bounds the wait to two frames, and
fails capture if destruction does not finish. It never destroys objects,
flushes removal queues, filters comparison fields, or changes player search.
The repaired complete battle and independent replay are validated below.

The initial214 protocol used IsDestroyed, which also includes BeginDestroy and
InRemoveList. A focused third-battle rerun exposes that error on turn1: the
native removal queue has not flushed yet, so waiting cannot complete. The wait
fails capture instead of changing game removals. Release215 narrows the input
to the exact Destroyed enum state, where UnityEngine.Object.Destroy has actually
been called. Earlier removal/animation stages proceed normally.

Pinned215 source103716-56cea771 now wins in48 inputs/40plays/8EndTurns with
Pyre80. Ascend13 records one native frame for pending destroyed IDs7/36/37/62/65;
its two raw-cache snapshots agree. The earlier turn1 Ascend records zero waits,
so BeginDestroy/InRemoveList still proceed to their own queue flush. All15 train
phases strictly match. Four immutable component branches and the original50-input
counterexample (including enemy51's surviving weak reference) pass focused checks.

Independent normal-reward replay104353-7c691d82 matches all48 complete player
states/forecasts/RNG and all15 train phases, with four zero counters. Normal66
events advance the same campaign to Ring4; the first three default-budget save
links pass without simulations. This validates the isolated sampling protocol;
later battles and a full campaign clear remain pending.
