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
receipt is retained; a new default native victory and independent reward replay
remain pending.
