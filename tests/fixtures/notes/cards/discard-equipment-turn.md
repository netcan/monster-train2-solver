# Detached equipment return during end-turn hand discard

Sample24 Banished/Hellhorned1122120878 Ring2 release259 reaches26 valid
inputs, then diverges after EndTurn27 from turn4 to5. Fortify5 remains
in Standby with a captured equipment binding to the old, absent host2.
The original model waits until DrawHand in the next turn to return it.

Native CardManager.DiscardCard calls global CheckStandByConditions after
its hand discard. The orphaned equipment therefore returns before combat
and UpdateStatsForNextTurn. Its TimesExhausted belongs in PreviousTurn,
not ThisTurn. It also enters Exhausted before the two combat deaths:
native IDs4,18,5,2,15 versus old4,18,2,15,5. The Standby free-slot stack is
native7,4,2,6,0 versus old2,7,4,6,0. These allocation differences matter
to later card identities even when every live unit and damage value agrees.

The original request preserves an early diagnostic label about enemy death;
the isolated native comparison establishes equipment discard timing as the
cause. Source213957-c4f191d8 retains the supported but wrong historical
prediction. Release260 independent215231-f63f1af0 matches all27 complete
inputs/forecasts/RNG. The checker constrains the precise historical
statistics/pile-order differences, four immutable parallel branches,
empty-hand next-draw timing and a living host retaining its equipment.

The model runs the existing global equipment return only when actual hand
discard occurred and equipment bindings exist. It preserves normal exhaust
callbacks, death routing and terminal outcomes. Empty, Persistent, Freeze
and Infusion-only hands retain their earlier guards/timing. The existing
original grafted equipment20-input chain/16 immutable branches and54-input
ephemeral equipment native pair remain passing controls.

This component does not establish a completed Ring2 win or campaign clear.
