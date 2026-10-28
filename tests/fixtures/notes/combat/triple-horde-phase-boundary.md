# Triple horde combat phase boundary

Random campaign 25 yields a rejected EndTurn after 20 inputs, with a champion,
three original companions, a three-horde combat birth and a retired Titanskin
source. The original and an independent experimental native capture agree on
the final decision successor. Both record the real birth and two preview writes.

An experimental guard expansion made the decision successor match but did not
match the intermediate EndTurn capture: the model had NextUnitId 27, while the
native phase had 28 and an additional unplaced preview spawn point 27. A later
decision capture had 27 and no such point. The experimental expansion was removed.

Two additional independent native processes sampled the first two ready frames
after the boundary using the development trace. Both frames had allocator 28
and preview point 27; two captures within each frame matched. The later player
decision used the separately loaded player trace, with allocator 27 and no such
point. Because these are distinct identity maps, these samples alone do not
establish that either map underwent a 28-to-27 cleanup. Every player prefix and
successor matches the uninstrumented native capture, including comparable RNG.
These earlier samples did not determine what the player trace saw in those frames.

Two more independent native processes captured both collectors twice per frame
in the same two frames and again at the next frame. Within each collector the
comparable states were unchanged across these samples. The development trace
kept preview point27 and allocator28, while the player trace consistently had
allocator27 and no point27. After removing only that known point and allocator
from the comparison, the first remaining mismatch is the second-floor third
unit's `SpawnerCardId` (development5, player6). The checker preserves this
counterexample without asserting that it is the only remaining difference.
Every input and player successor still matches the uninstrumented native
process. The live-model boundary remains rejected until these identities and
their behavioral effects are independently established.

The five `player-triple-horde-phase-boundary-*` archives preserve the original
rejection, experimental successor, phase counterexample and independent frame
samples. The focused check replays the supported prefix, compares independent
observed successors and frame states, and requires the production model to
reject the boundary. This does not establish support or a battle victory.
