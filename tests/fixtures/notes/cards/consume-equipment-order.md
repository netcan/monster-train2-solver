# Consume and detached equipment standby ordering

Sample24 Banished/Hellhorned seed1122120878 Ring8 begins with Pyre63 after
the independently accepted refined Ring7. Release267 first differs at input26:
ImpProvisation16 sacrifices Basher3, which still holds Fortify12. Earlier
equipment8/19 remain detached standby entries for that same host.

Death-local checks return the host and attached12 first. The played Consume
card then reuses the freed standby slot9. Native global CheckStandByConditions
snapshots dictionary entry order, so detached8/19 return before Consume16.
The old model returned Consume16 before scanning the equipment entries.
Its only differences are the standby free-list and exhausted-card order.

The preserved source011841-67d97efc contains the complete25-input prefix and
the original incorrect26th prediction. A separate read-only native trace
012552-9720489b observes the death-local, equipment-local and global checks.
It confirms this slot reuse without changing native return behavior.

Release269 returns Consume and equipment entries together in captured standby
order and runs exhaustion callbacks at that position. The focused check
compares all26 complete states, forecasts and RNG, preserves the historical
two-field mismatch and checks four immutable branches. Existing temporary
Consume/Ephemeral, JustCause copies, original grafted equipment and end-turn
equipment pairs pass their affected native transitions. Independent frozen
native replay and the remaining whole battle are separate acceptance steps.

Frozen revisionb96677c4 independently repeats the exact request as
013510-0f6c4409. Both native processes match all26 complete successors,
forecasts and RNG; the original wrong prediction and the repaired matching
prediction remain paired. The focused independent pair passes. The same
Pyre63 checkpoint now continues under actual10000ms/two-worker search.
