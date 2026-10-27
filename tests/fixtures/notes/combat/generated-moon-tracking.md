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

`--generated-moon-tracking-only` checks all18 native successors/forecasts/RNG,
four immutable boundary branches, four altered creation signatures and two
reset guards. Source36390bytes,5973nodes,SHA256
a43431ab24c38979583ef424684fdffb8c5b42167c7c1f2f54f3aaaabf93f586.
Independent frozen replay and subsequent full-battle acceptance are separate
checks; this component is not a seventh victory.
