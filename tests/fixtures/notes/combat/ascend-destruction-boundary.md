# Ascend component destruction boundary

Sample20 (Underlegion/LunaCoven, seed1043013037) reaches a natural third-battle
win in50 inputs under release0.2.212. Player decisions and complete turn-end
successors match, but Ascend component13 spans Unity's deferred OnDestroy:
old friendly weak references7/36/37 clear while enemy51 remains. The old raw
trace remains a failed component comparison and is not accepted as a campaign
ring. Removing all absent actors would incorrectly remove that enemy too.

Release0.2.214 changes only the explicit isolated death-settlement protocol.
Before capturing Ascend's input, it awaits original OnDestroy for known actors
already marked IsDestroyed whose native weak reference is still non-null.
It records their IDs and the frame count, bounds the wait to two frames, and
fails capture if destruction does not finish. It never destroys objects,
flushes removal queues, filters comparison fields, or changes player search.
The new complete battle and independent replay remain pending validation.
