# Inactive moon understanding on outer-boss birth

The original289 Juice/Powerstone cast request reaches seven native plays, then
fails its first EndTurn at input8. Native Fragile11 is30/3000 with mask6;
the historical model retains the unspawned definition's mask4. Tracking is
false, the phase advancesNew-toFull, and neither phase status is present.
Other actors keep their recorded masks; the normal first wave already uses6.

CharacterState.Setup calls UpdateMoonPhaseUnderstanding before starting-status
registration, regardless of enabled tracking. Ordinary enemy creation already
models this; outer-boss creation only initialized the active case.290 updates
captured understanding for inactive known outer-boss births too, preserving
the active signature guards and leaving statuses/callback dispatch unchanged.

The original incomplete archive054740-b80a184f is retained as incomplete,
not promoted to battle acceptance:32556bytes/5448nodes/SHA256
bcfc0281cda83b6569ce17592ef6510ba04a300d7d70353377f05c6690ac9035.
All eight actual native successors/full RNG, exact historical mask-only
difference, four immutable EndTurns and unknown inactive callback rejection
pass. The active Fragile independent
birth pair and all four unknown signature guards still pass. The independent
Juice/Powerstone deployment pair also passes; actual spell cast and the fresh
eight-input boss replay remain pending separate native checks.
