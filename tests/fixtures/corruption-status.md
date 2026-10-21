# Persistent corruption damage status

The native Titan Dominion upgrade `TitanDominionCorruption` adds an
OnAttacking trigger to the naturally upgraded Firebrands: apply 20 stacks of
the `corruption` character status to the last attacked character. This status
deals `(native parameter + relic modifier) * stacks` damage during
OnPostCombatPoison, after order-zero status effects and before order-four
Unstable. It retains its stacks. Room corruption remains a separate mechanic.

`corruption-status-calibration.mt2f` imports six exact room/exchange records
and EndTurns 5/7 from the natural seventh-ring rolling run
`.probe-runs/full-battle-units-spells-and-junk-20261021-111407-c72b6e98`.
That full run lost with all four differential counters zero. The component
does not establish a seventh-ring victory. The native status definition was
copied from the same installed game's read-only catalog capture at
`.probe-runs/full-battle-units-spells-and-junk-20261021-111034-6e07d9d7`.
No before/actual states or outcomes were rewritten.

Two separate heavies retain 20 corruption between Pyre combat exchanges.
Their HP drops by exactly 20 after the first exchange, before the next one;
the dedicated checker also compares all six room/exchange states, complete
contexts/RNG and both EndTurns. Before support, the Firebrands were explicitly
rejected for an unmodeled triggered status and could not be deployed.

Run `--corruption-status-only tests/fixtures/corruption-status-calibration.mt2f`.

For strategy diagnosis, `--rolling-forecast-only <rolling fixture> [max nodes]`
replans from every model successor under the three-turn/three-second budget,
then independently replays the complete chain and checks source immutability.
It reports a model outcome only; native timing can select another bounded
path, so this does not replace native victory/replay acceptance.
