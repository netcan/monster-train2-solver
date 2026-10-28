# Random campaign 25, final battle, release 0.2.395

The original-deck checkpoint used the player advisor with a 10-second budget
and two workers. It executed 58 decisions: 49 card or ability inputs and nine
end turns. The native result was defeat with zero Pyre health. Capture failure,
mismatch, unsupported and pending counters were all zero.

An independent player-capture process replayed the frozen inputs. The pair
checker compared every input, root, successor, forecast and RNG state and
validated player capture purity. Both processes ended in defeat with original
files unchanged. This is neither a default-budget campaign clear nor an
optimality guarantee.
