# Unboxed hand cards on a Conduit floor

The random Umbra/Stygian Ring3 improved model route diverged at input11:
Ice Spear dealt10 predicted damage instead of20 native damage. The causal
boundary is input10, playing the three-card Small Box (`f0ff6411-5c58-4b69-b025-0f6e4a52fca7`).
Its copied cards23,24,25 enter the already-selected floor0 with a living
10-power Magic Totem. The model omitted their selected-room hand callbacks.

Native `CardManager.TryAddCachedTemporaryCardToHand` enters
`TryAddCardStateImpl`, which dispatches room `Drawn` then `Updated` for each
inserted card. `SmallBoxModel` now applies these callbacks per copied hand
card. It keeps saved templates, shuffle/RNG, upgrade inheritance and capacity
handling unchanged. No generic relic-modified unboxing support is assumed.

Two isolated10-input processes preserve the original missing modifiers and
the repaired forecast. They complete in33.21/33.70s, with unchanged input
profiles and protected files. Every comparable root/successor and RNG agrees.
The focused checker separately checks raw tracker membership and each
new card's10 damage/heal upgrade: normal battle comparison clears selected
room state and would otherwise hide this mismatch. Raw dictionary enumeration
order is not asserted as card identity. Existing Conduit selection, scaling,
discard, death, preview and immutable parallel controls pass. The original
Small Box witness also passes full-hand shuffle, capacity-limited insertion,
starting/temporary upgrades and exact RNG checks.

This repairs a native combination boundary, not the acceptance of the entire
61-input candidate or the default10-second campaign policy. The corrected
route must be generated and independently validated again.
