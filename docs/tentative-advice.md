# Tentative next steps

After an accepted manual hand-card play matches the current recommendation's
instance, room, and target or deployment position, the panel immediately
advances to the next step with a pending label. It does not show a confirmed
forecast, hand position, cost, target health, or hand hover until the existing
settled-state and continuation checks publish fresh verified advice. Consecutive
matching plays can advance the tentative view. An off-route accepted play hides
it; reset, pause, and end turn also clear it. Unit/room abilities, extra card
choices, and location-targeted effects wait for normal verification.

The tentative path never changes search, retained proof, or action execution.
It is a hint, not proof that later effects or random outcomes will match.

## Focused evidence

On 2026-10-30 an isolated native 0.2.438 player run used Instant game speed,
one search worker and a 1000 ms search budget. It won with 18 card plays and
seven end turns; the existing presentation, history and native checks passed
with no captured state mismatches. All 18 accepted cards produced pending
steps: the diagnostic timestamps from the native input prefix to writing the
tentative UI text ranged from 1.0 to 4.5 ms, median 2.0 ms. These timestamps
do not include the next rendered frame and are not a player-machine latency
guarantee. Profile: `.probe-runs/full-battle-units-spells-and-junk-20261030-201335-dd569adc`.

The focused input checker covers identity, room, target, deployment, multiple
inputs and conservative rejection. Existing rapid-input tests cover exact
continuation replay and divergence. This isolated run does not establish
timings for every card, user interaction, or actual installed game session.
