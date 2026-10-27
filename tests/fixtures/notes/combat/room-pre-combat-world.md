# Room damage must preserve the shared physical world

Sample25 seed1860103187 Underlegion/Pyreborne Ring6 release273 fails before
executing any advice input. Both default10000ms workers throw in
EnchantmentWorldModel.Complete during a boss preview. The unchanged native
deployment capture is archived as player-room-pre-combat-world-root.mt2f.

A temporary diagnostic traced the first four-to-one room collapse to
RoomPreCombatModel.Apply: ordinary Inferno Room damage used the single-room
spell wrapper. Its death queue then rebased the shared enchantment world from
that one room. A later preview on another floor could not find its room.

Release274 keeps the complete shared train when running this room effect;
target collection remains restricted to the original room. It does not catch
worker exceptions, manufacture missing rooms or normalize native comparison.
The focused root check reproduces the same two-worker budget, independently
replays its full forecast/state/RNG and checks parent immutability. The native
Inferno control retains same-floor friendly/enemy damage, lethal cleanup,
outside-floor isolation, deployment/replacement/restoration, once-only
relentless damage and sixteen immutable complete chains.

The fixed model finds a58-input/Pyre17 victory. This is a prediction pending
new native replay. A separate273 strong-policy native15-input observation
022847-12488e29 has a NextUnitId18-versus17 difference. Its original request
was prematurely labeled 'room pre-combat shared world'; that preserved label
does not prove it is the same defect and it is not acceptance for274.
