# Ascend component destruction boundary

Sample20 (Underlegion/LunaCoven, seed1043013037) reaches a natural third-battle
win in50 inputs under release0.2.212. Player decisions and complete turn-end
successors match, but Ascend component13 spans Unity's deferred OnDestroy:
old friendly weak references7/36/37 clear while enemy51 remains. The old raw
trace remains a failed component comparison and is not accepted as a campaign
ring. Removing all absent actors would incorrectly remove that enemy too.

Release0.2.214 changes only the explicit isolated death-settlement protocol.
Before capturing Ascend's input, it awaits original OnDestroy for known actors
already at DestroyedState.Destroyed whose native weak reference is still non-null.
It records their IDs and the frame count, bounds the wait to two frames, and
fails capture if destruction does not finish. It never destroys objects,
flushes removal queues, filters comparison fields, or changes player search.
The new complete battle and independent replay remain pending validation.

The initial214 protocol used IsDestroyed, which also includes BeginDestroy and
InRemoveList. A focused third-battle rerun exposes that error on turn1: the
native removal queue has not flushed yet, so waiting cannot complete. The wait
fails capture instead of changing game removals. Release215 narrows the input
to the exact Destroyed enum state, where UnityEngine.Object.Destroy has actually
been called. Earlier removal/animation stages proceed normally.
