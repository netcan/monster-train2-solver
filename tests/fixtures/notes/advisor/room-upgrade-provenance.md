# Retained attachment upgrade provenance

Sample16 Ring8 release188 executes35 matching native inputs, then plays
FightClub25 in room0 at turn5. Dead enemy24 (AttackerT3_Sweep, HP0, destroyed)
still references its EnchantedChamber upgrade after the room replaces that
attachment. The old native projection loses its origin and adds the unsupported
"Upgrade room modifiers" marker, although no game payload changed.

Release190 remembers the original CardUpgradeState object identity in a
per-battle weak table. The complete unchanged36-input prefix and replacement
run in a fresh native process. Every before state and successor equals the
original expected state, including forecasts and RNG. Four detached model
branches match without changing their parent. The historical counterexample
remains unchanged; its only discrepancies are five serialized copies of that
one marker. The checker removes only those historical copies to prove there
are no other differences. Production comparisons do not normalize or ignore
the field. Unknown upgrades continue to report their own interactions.

This is projection evidence, not a Ring8 victory or campaign clear.
