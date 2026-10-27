# Compare partial forecasts at the horizon they actually reached

Release236 fixes a portfolio-merge counterexample from sample22 Ring4's
unchanged native deployment root. The old10000ms/96-node model forecast reaches
five complete turns/Pyre73. A second independently replayed route reaches
seven complete turns/Pyre45 and then wins at Pyre37. The old merge picks the
five-turn forecast because damage from later waves has not happened there.
The curated inputs are explicitly model forecasts, not native wins.

For usable whole-battle continuations, the merge now prioritizes completed
turn depth before the public score, matching the individual planner's completed-
prefix selection. Terminal victories remain first, ordered by surviving Pyre;
known terminal defeats/stalemates remain after usable continuations. Equal-depth
forecasts keep score, material and step tie-breakers. Bounded-horizon searches
keep their score ordering. No budget, worker count, search pruning or capture
comparison is relaxed.

The focused checker fully replays both routes from the identical complete root/
RNG, verifies five/seven-turn recorded states, checks order independence and
retains the terminal win. Existing same-root49/7-step terminal-defeat controls
still select the useful material position and preserve primary-score priority.
An attempted idle extension is explicitly unsupported by another triple-Juice
signature and is not used as a terminal outcome. A fresh native default-budget
battle subsequently completed with release236:53 inputs/45 plays/8 EndTurns,
Pyre5 and16 train stages. Its independent native reward replay matches every
state/forecast/RNG transition, with all four failure counts zero. Normal58
events produce Ring5 save6D2C913B9B0B265D24035AFE2B2F7AFE41B239C6AA684ABE9E24AA4729F4C062.
This establishes a native passing baseline, not an improvement in surviving Pyre.

The continued Ring5 default58-input/Pyre5 pair also passes17 train phases and
complete states/forecasts/RNG. Normal39 events create Ring6 save
8BA7C8C095F0000C8031B34D079CA12B8B53C5111BF055838DDAFD7B13251AE8.
That branch's Ring6 default57-input/8-EndTurn native defeat leaves Cael1748HP;
full state/forecast/RNG and all four zero failure counts pass. Both the victory
pair and actual defeat are curated separately; there is no complete campaign win.
