# Random campaign 25, final battle, release 0.2.394

The isolated Underlegion/Pyreborne checkpoint used the actual player advisor
with a 10-second budget and two search workers. It played 58 decisions, with
49 card or ability inputs and nine end turns. The native result was defeat:
zero Pyre health and 2722/3000 boss health. Capture failures, mismatches,
unsupported transitions and pending transitions were all zero.

An independent process replayed the frozen player inputs. The focused pair
checker compares all 58 inputs, roots, successors, full planned outcomes and RNG
states, and validates the player capture. Both processes ended in the same
defeat. This is not an eight-battle victory or an optimality claim. The
10-second search budget is the acceptance configuration, not the default
three-second setting.
