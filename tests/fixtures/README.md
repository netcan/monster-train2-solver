# Native regression fixtures

These fixed inputs reproduce the independently modeled battle and calibration
checks. Run `pwsh -NoProfile -File scripts/Check-Models.ps1` from the repository
root. The script reads the curated fixture list in this directory.

The retained inputs are `.mt2f` binary archives. They store typed, deduplicated
value graphs, not JSON documents. Regression reads those graphs and constructs
model states directly. Every captured value is preserved; identical repeated
states share storage. See `src/FixtureArchive/FORMAT.md` for the versioned layout.

`manifest.tsv` records the original capture length/SHA-256 and each archive's
length/SHA-256. The regression script verifies its complete curated inventory
and archive hashes before running. `SIMULATION.md` records native coverage and
capture versions; historical JSON/gzip paths there describe the original inputs
before the binary migration. Import verification compares all values, exact
number lexemes, property order and duplicate keys; JSON whitespace and escape
spelling are not part of the retained representation.

Direct native archives use a `native:` source label and zero text-source
length/SHA-256, because no source JSON document exists. Their binary payload and
complete archive hashes still protect integrity. The capture records the game
version and module MVID; optional diagnostic JSON is used only for independent
value comparison.

`results/` is ignored and holds local logs, catalogs and benchmark outputs.
Isolated native probes use `.probe-runs/`, which is also ignored. Successful
captures become fixed inputs here after native and independent comparisons pass.

`full-battle-x-cost.mt2f` and `full-battle-x-cost-lethal.mt2f` retain schema 50
native battles with 39 X casts, including 16 zero and 23 positive payments,
96 exact paid-cost damage callbacks and 38 exact subsequent energy contexts.
The lethal fixture finishes with an X spell killing the Boss. Both complete
policies match from initial and mid-battle inputs with 16 parallel branches.

`full-battle-status-callbacks.mt2f` retains the complete schema 46 native battle
and 481 ordered status callback dispatches, including zero additions, actual
removals, silence loss and dying actors. These checks compare captured native
unit/context states independently of the predictions stored in the capture.

`full-battle-status-callback-actions.mt2f` retains a complete schema 48 battle
with nested damage, healing, status, upgrade and source-copy callbacks. It checks
390 status and 419 other character dispatches, 95 nested queue payloads, both
source-card writeback boundaries, and continuous simulation from initial and
mid-battle roots with 16 isolated parallel branches.

`full-battle-energy-effects.mt2f` and `full-battle-energy-effects-lethal.mt2f`
capture current, next-turn and persistent energy changes from spells and unit
triggers. They include phase gates, signed/ranged/zero amounts, native caps,
late end-turn accounting, carried combat gains, cancelled ranges and post-boss
gates. Both complete policies match from initial/mid-battle states in 16 parallel
branches; all 448 observed energy applications also compare complete contexts.
