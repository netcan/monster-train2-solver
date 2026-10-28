# Triple horde combat phase boundary

Random campaign 25 yields a rejected EndTurn after 20 inputs, with a champion,
three original companions, a three-horde combat birth and a retired Titanskin
source. The original and an independent experimental native capture agree on
the final decision successor. Both record the real birth and two preview writes.

An experimental guard expansion made the decision successor match but did not
match the intermediate EndTurn capture: the model had NextUnitId 27, while the
native phase had 28 and an additional unplaced preview spawn point 27. A later
decision capture had 27 and no such point. The timing and effect of this cleanup
are not established by these snapshots. The experimental expansion was removed.

The three `player-triple-horde-phase-boundary-*` archives preserve the original
rejection, experimental successor and phase counterexample. The focused check
replays the supported prefix, compares independent observed successors, checks
the precise phase discrepancy, and requires the production model to reject the
boundary. It does not claim this combination is supported or a battle is won.
