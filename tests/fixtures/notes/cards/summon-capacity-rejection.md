# Full-room summon is illegal before paid-card callbacks

Natural LazarusLeague/LunaCoven seed68586509, Covenant0, Ring5. An offline
candidate tries to summon Spear Steward14 into room2 with5/5 capacity. The
old model reaches an uncalibrated Juice callback before its late capacity
test. Native CardManager.CanPlayHandCard instead rejects with RoomFull.

The development-only observer now records explicitly requested final native
selection rejections, while still requiring every preceding input to be
supported and nonterminal. Ordinary diagnostic errors still fail. This
26-row recording retains all25 complete native successors/RNG and the
original RoomFull selection error. The rejected attempt leaves the complete
native state unchanged. It is not a played card or a battle victory.

Release206 tests ordinary Monster-card capacity before paid callbacks,
using the already resolved size and captured capacity. Four immutable
parallel branches classify the candidate as Illegal; the full-room input
is absent from both legal and unsupported frontiers. The original38-input
native defeat still replays completely, and the calibrated Lazarus Juice
pair/fixed-X abilities pass. Independent release206 rejection replay
matches every full native root/successor, forecast and RNG; both native
processes reject RoomFull without changing the complete state.
The next exported candidate is a direct Lazarus Spike cast, rather than
the invalid summon; that separate boundary still requires native proof.
