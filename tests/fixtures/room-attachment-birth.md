# Room attachment birth ordering · 2026-10-23

The natural Ring7 actual-default experiment
`full-battle-units-spells-and-junk-20261023-220306-98231be1` reaches turn 9 and
resummons Endless/Quick TrainSteward card 5 in room index 2. The room contains
the original StewardsQuarters attachment, and its earlier enemy enchanters
leave a retained enchantment world. The native new unit has attack 38, Quick,
Endless and Multistrike 1. The old prediction retains the attachment's +20
attack and upgrade record but loses its Multistrike status.

The model applied room attachments before starting-status initialization;
that later phase rebuilds the status registry when an enchantment world is
present, erasing the attachment's status. Native `MonsterManager` completes
`CharacterState.Setup` and covenant effects before
`RoomManager.UpdateTrainRoomAttachments`, then enters the regular OnSpawn
sequence. Both direct and queued modeled births now follow that relative
order. The change does not add the attachment's upgrade twice or merge its
status into authored starting statuses.

`player-room-attachment-birth-observed.mt2f` preserves the failed trace exactly:
zero capture failures/unsupported/pending, one mismatch, no terminal result.
Its historical forecasts and failed process are not accepted. The focused
checker independently replays the captured problematic action, requires the
entire native successor/outcome/RNG and exact status insertion order, and
repeats 16 immutable branches. Replaying all 26 actually observed native
inputs also passes; 18 native train phases match.

Existing original StewardsQuarters controls pass seven plays/five EndTurns,
including subtype/floor controls, existing and later births, additive
Multistrike, replacement/restoration and sixteen immutable full chains.
FightClub birth upgrades, room moon callbacks, nested enchantment summons and
pure status callbacks pass their focused checks. Use
`scripts/Check-RoomAttachmentBirth.ps1 -SkipBuild`; no full regression or Mac
packaging is required.

A fresh controlled StewardsQuarters native battle
`full-battle-units-spells-and-junk-20261023-222025-94f5d4eb` completes the same
seven original-room actions and five EndTurns, wins and exits normally in
61.91 seconds. Protected originals are unchanged and all four failure
counters are zero. Its independent full-chain replay and 16 immutable
branches pass. It is registered as
`full-battle-stewards-quarters-birth-order.mt2f`; this controlled scenario
retains its disclosed unit stat modifications and is separate from the
natural Ring7 observation above.

The fresh natural-default source/replay pair `221919-5f432b65` /
`222608-79e8cfd6` subsequently completes 25 inputs and 20 train phases per
process with exact native forecasts/state/RNG, normal exits, protected
originals unchanged and all four counters zero. Both lose. Its path does not
resummon the problematic Steward, so it is a natural execution control rather
than another direct birth-order witness. See
[the continuation pair](player-ordinary-continuation.md).
