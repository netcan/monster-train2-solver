# Equipment ownership after native preview cleanup

An Instant replay of the retained metagame sixth battle stops at input7:
equipment16,e4ba411f-09b2-4a87-91f6-5f00cd742ba6,is on native owner2's real
equipment list,but the card's EquippedCharacter has become null. The raw
failed capture is preserved with exactly one mismatch,zero capture failures /
unsupported / pending,and no terminal battle result. Only the owner field
in CardInstances and CardRegistry differs from its original forecast.

The native CombatManager.PreviewCardPlay(false,...) unconditionally calls
card.RemoveEquippedCharacterState(). CharacterState.GetNonPreviewEquipment
still returns the real primary equipment list. 0.2.76 uses that list as a
read-only fallback when an equipment card has no back-reference. Destroyed
and preview owners are excluded; canonical decisions also exclude dead owners.
No native equipment relationship is repaired or written back.

Two independent seven-input natural prefixes reach the same settled state.
Each then invokes the game's original PreviewCardPlay(false,...) directly:
the raw owner becomes0,the primary list still owns card16,and the complete
player decision and every RNG remain identical with captured owner2.
The focused checker reproduces every successor and immutable parent,and
cross-checks the original failed raw equipment card and owner. Raw action
and player projections allocate spawner-card references independently; the
owner's spawner reference is mapped only after requiring the same native
card definition. Within each projection,full-state comparisons stay exact.

Curated files:cards/equipment/player-equipment-preview-owner-{failure,calibration,replay-calibration}.mt2f.
The failure archive is diagnostic evidence,not an accepted native battle.
This repair also lets the C3 Ring6 observation pass its old input46 equipment
capture failure and reach input54's separately unsupported Tower callback.
