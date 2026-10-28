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
after the boundary. Both frames retained allocator 28 and preview point 27;
two captures within each frame matched. The later decision had allocator 27
and no such point. Every prefix and successor matches the uninstrumented native
capture, including comparable gameplay RNG. These samples do not establish
whether the production overlay can capture the transient state, or whether
repeated capture itself affects subsequent cleanup.

The five `player-triple-horde-phase-boundary-*` archives preserve the original
rejection, experimental successor, phase counterexample and independent frame
samples. The focused check replays the supported prefix, compares independent
observed successors and frame states, and requires the production model to
reject the boundary. This does not establish support or a battle victory.
