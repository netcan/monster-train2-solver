# Keep the preferred opening in the two-worker portfolio

Sample22 Ring4's unchanged native deployment has a verified fixed upper-floor
policy prediction winning at Pyre37. Release236's actual10000ms/256-node/
two-worker model run instead returns a53-input/Pyre5 win. Its ordinary seed
adapts downward after its short deployment comparison; the tactical worker is
also fixed on the lowest alternative floor. Both discard the preferred floor.
The original default-options forecast is retained in the curated archive.

Release237 keeps the tactical worker on the preferred top floor only for the
ordinary adaptive whole-battle opening, at turn0 with no living placed player
units. All later roots, explicit tactical options, other preferred floors,
bounded horizons and disabled shorter-horizon settings keep their previous
floor diversification. The ordinary worker still considers lower floors;
there are still two private search workspaces sharing256 nodes/10000ms.

The focused unchanged-root test reproduces the old Pyre5 selection, then obtains
a full Pyre37 model win under the same budget/options. Independent full-state/
RNG replay and immutable root pass. Existing single/two-thread whole-battle
wins, shared one-node/one-step limits, expiry and cancellation controls pass.
The existing Fragile control also preserves its safer bottom opening at the
16-node cutoff and replays54 inputs to a Pyre2 model win; this is not a new
native victory or proof of optimality.

Fresh native execution with release237 now wins Ring4 at the unchanged default
10000ms/two-worker budget:57 inputs/49 plays/8 EndTurns, Pyre37 and16 train
stages. The independent native replay matches all states/forecasts/RNG and all
four failure counts remain zero. Normal60 reward/map events produce Ring5 save
8EE2D303385939CB3C1575F811FA6B680DA57B57AEC4909039BFCDC415F1304F.
The original release23653-input/Pyre5 native pair is retained separately. The
new branch continues Ring5; neither pair establishes a complete default-budget
campaign victory.
