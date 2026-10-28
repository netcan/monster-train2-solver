# Exhausted turn native sample

Two isolated player-advisor runs at the same fourth-turn boundary compare version
0.2.394 with 0.2.395. Their previous and current roots, and the previous plan's
steps, resulting state and outcome, are identical. Both perform a fresh search
with two workers. Search scheduling and the resulting new plans need not match.

The archived timing data reports verification times of 12,330.0 ms and
7,186.6 ms respectively. The corresponding native logs split these into old
continuation checks of 5,507.7 ms and 461.9 ms, and new plan verification of
6,822.3 ms and 6,724.7 ms. This is one observed sample, not a performance
threshold or a guarantee for other battles. The new plan is still verified.
