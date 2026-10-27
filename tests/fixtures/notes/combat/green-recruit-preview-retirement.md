# Green Recruit preview birth retirement across combat floors

Sample23's release244 default Ring3 run180351-94fd69bb physically wins.
Its full nine EndTurns and retained advice match, but the raw Room/Exchange
stages46/47 and Train combat phase10 retain one temporary preview actor too
long in the model. The original archive preserves all three differences.

Green Recruit22 (card19, original c052c567-1e3c-4f36-a6cd-89b0648b9475) dies
on floor1 and its original death fallback creates a four-stack Fun Guy preview
actor30. This actor retains12 attack/16 HP, no upgrades/triggers, its copied
point364 and its statistics listener at that floor's raw capture. The next
lower floor's real combat yields a frame: native destroys the preview actor,
sets retained room to-1, clears its point/listener, and preserves numeric state.
The primary Fun Guy31 remains on floor1.

Release245 extends the existing calibrated remote preview-birth retirement to
this exact original death fallback. It requires an automatic primary world,
preview-owned cardless unupgraded four-stack Fun Guy with original3/4 troop
definition, a lower destination floor, its copied primary pointer, and the
original dead recruit with a matching retained last-known floor. Primary,
same-floor, changed-stack/upgraded, missing-source and missing-point controls
remain untouched.

All71 native room/exchange stages,17 train phases, nine full EndTurns and RNG
now pass against the unchanged historical source. Four immutable branches and
six scope controls pass. Existing22-input remote-preview and28-input retired
troop-source independent pairs still pass. The original failed33-input/Pyre75
receipt is retained separately. Release245's new default10000ms/two-worker
source182157-136dc51f and independent reward replay182617-be076140 both win:
33 inputs,24 plays/nine EndTurns, Pyre75. Full state, forecast, RNG and all17
train phases match; capture, mismatch, unsupported and pending counters are
all zero. The replay advances58 normal reward/map events to Ring4, save SHA256
8DDF1A2A36B7F3B0E32F1E964711EA3B327AC7BF2747FE49F2952AF9FFED3D7F.
The first three default victories now form one accepted native save chain.
Ring4 checkpoint random23-ring4-default245.zip has57 verified files and SHA256
00C6770361E8B4E10EE593B989885ABC8E928ACF0E73FBA6834BBF092D8ACA94.
The unchanged prior completed profile's21 tracked values remain retained;
metagame continuation is checked without running another battle simulation.
