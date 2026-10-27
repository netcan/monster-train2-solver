# Generated Dusk Mantle activates moon tracking

Sample25 Ring7 opening exploration on frozen282 reaches an original Fetch
activation by Truffles6 on turn3: card36 creates Dusk Mantle38
(49a88125-2a35-4cfa-b556-3502f0cccb14). Preserve the unchanged17-input prefix
and rejected boundary in native041411-894289d7.

Native PlayerManager.OnCardAdded enables tracking immediately; its
MonsterTurn loop then calls HandleMoonPhaseResetMidBattle before the next
preview. All12 active characters, including the Pyre, acquire understanding
mask Full(2). The phase does not advance, no newmoon/fullmoon status is added,
and phase-change relic callbacks do not run. Unspawned definitions retain
native None(4).

283 admits this captured Dusk Mantle creation during an active player turn,
then completes the reset before previews. Unknown creation signatures,
unobserved timing, retained actors, room masks, character moon
callbacks and spawner moon traits remain guarded. Ability results propagate
the updated tracking flag to the outer battle state.

Player captures now read actual masks from deployment even while tracking
is inactive. The old282 observation omitted these fields; its comparison
compares unspawned definition defaults to native None(4). Omitted live masks
are unknown: the independent replay reads them directly and checks every
previously captured field unchanged. In particular, inactive tracking does
not imply a character has no phase understanding. Actual recorded phase bits,
all other state, inputs, RNG and raw archive bytes remain exact. Development
observation request comparison binds only omitted mask fields to the new
native values without changing the request or capture.

CharacterState.Setup updates understanding even when tracking is inactive;
birth retains the native None bit while adding the current phase bits. The
model now performs that understanding update independently from the tracked
phase status. Failed prefix observations write an explicitly incomplete raw
archive for diagnosis; they cannot satisfy the completed-observation gate.

The preserved17-input prefix043501-d2bcf8dc observes Fight Club29 on floor2
updating Funguy11 from understanding5 to6 during inactive Full Moon. The old
prediction retained5. All other active actors already match; the repaired
model checks every complete native successor/RNG, plus four immutable final
branches. Ordinary attachment character upgrades now update captured
understanding without adding phase statuses or refreshing the global phase.
Unknown moon callbacks/traits remain guarded. This archive is explicitly
incomplete,34700bytes/5804nodes/SHA256
95bdd3ff3f91ac82217485b3f34ff89f12aa2668e2c6b5f062d46dad79478ddd;
it is not counted as a completed component or battle victory.

`--generated-moon-tracking-only` checks all18 native successors/forecasts/RNG,
four immutable boundary branches, four altered creation signatures and two
reset guards. Source36390bytes,5973nodes,SHA256
a43431ab24c38979583ef424684fdffb8c5b42167c7c1f2f54f3aaaabf93f586.
Independent frozen replay and subsequent full-battle acceptance are separate
checks; this component is not a seventh victory.

Frozen28337a22dab independently captures044001-3e252ada in62seconds with
the exact same request and18 native inputs. Every formerly observed native
field, all full successors/forecasts/RNG and four immutable branches match;
newly exposed inactive understanding fields are checked by the model too.
The registered gate always checks the pair and original17-input incomplete
counterexample. Replay36795bytes/6019nodes/SHA256
e1cfa853ed8a20d33fede7f84038a76ec4ee6f640c960116ecaccdd0f97fec68.
Frozen checker SHA256
28d46b26bca9bd895b18d9ab47fe837e93aeb18161fd6088cc3221d25c6b4fbb.

285 accepts the first subsequent tracked transition when each captured living
unit's prior phase bits match the current phase, even when the midbattle reset
has not added a phase status. Contradictory statuses or captured bits still
reject; legacy omitted masks still require their original status evidence.
Native044122-cf660e52 preserves all20 inputs: Full/turn3 with Dusk Mantle38
equipped on Truffles6 advances to New/turn4. All full successors, forecasts and
RNG match, with four immutable EndTurn branches and a conflicting-mask control.
The original capture stored the old validation refusal in ExternalInteractions;
only the checker revalidates and removes that exact historical field. Production
still rejects captured interactions; the raw archive is unchanged.
Source39056bytes/6567nodes/SHA256
1b173e7ce742174d830745e698431303a4daecfb397d6936e78e37c0db90769a.
Independent native replay and seventh-battle victory remain separate gates.

Frozen285e711484e independently repeats the same20 inputs in native
045501-29789c32 (84.56seconds). Full states, forecasts, RNG and unchanged
requests match, with a supported forecast at the original rejected boundary.
The registered gate always checks this pair. Replay39089bytes/6565nodes/SHA256
8f13c6eb668006d4be911eb67ebea1e77f24378fcc0a4bee515efffc75f8f99b.
Frozen checker SHA256
e20bbf7055dee1ffd38494724ecc3e36efd78fe28a1ab71114a7cb5263c6b841.
