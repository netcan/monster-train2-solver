# Ephemeral equipment attachment and purge callbacks

Sample20 Ring6 first diverges at input54. Fetched Resin Blades card36 has
FetchCostReduction, which installs Ephemeral, and attaches to Steward8.
The equipment upgrades and host reference remain attached. Its standby entry
is then removed by the OnCardPlayed purge, leaving dictionary slot11 free;
OnCardDiscarded purges it again. Purged contains two references to card36,
and standby no longer has an equipment return condition for that card.

The old model only ran these purge callbacks for a Discard-to-Purged route,
leaving this equipment in Standby. Release223 keeps the initial attachment
and standby insertion, then models both purge callbacks. Consume/SelfPurge
combinations on this equipment path remain explicitly guarded until their
native callback order is calibrated.

The original54-input counterexample and the isolated54-input native source
preserve the historical mismatch. Current full successors, forecasts and RNG
match, with four immutable parallel branches and two compound-trait guards.
The seventeen-original-equipment control and existing41-input Ephemeral/Consume
pair also pass. The independent native process matches all54 inputs, complete
states, forecasts and RNG; its repaired forecast and the original mismatch
are both required by the paired checker. This is a component boundary and
does not establish a Ring6 win or a campaign clear.
