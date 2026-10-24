# Natural Corrupter winning player acceptance

`full-battle-player-boss-corrupter-{search,replay}.mt2f` retain actual player
source `full-battle-units-spells-and-junk-20261023-065007-85622feb` and fresh
replay `full-battle-units-spells-and-junk-20261023-065018-06470f33`.

Both restore the hash-verified natural seed42 Ring8 checkpoint in
`campaign-progressed-ring8-corrupter.zip`: seven preceding native wins,
Pyre84/120, an unchanged 31-card Banished/Hellhorned deck and acquired
`DamageOnFloorTransition`. The queued original Corrupter has 2,800 HP and
20 base attack, with its original corruption and status-multiplication
callbacks. No encounter replacement, HP adjustment or deck control is used.

The actual separate player Advisor executes 57 plays and ten EndTurns,
winning at Pyre84. A fresh native process executes the same 67 inputs and
wins at Pyre84. Both record `DEPTH-PASS`, settle terminal effects and exit
normally with original files unchanged. Capture/mismatch/unsupported/pending
counters are all zero. Each captures 19 complete train phases and 73 room
stages, including the natural floor-transition relic, source Duality and
revival/aura interactions.
Both casting and relentless phases participate, with three unit ability
activations in the complete route.

The source passes pointer routing, pause/resume, hide/cancel/show, refresh
and panel interaction checks. It records 69 publications and 708 pure
captures; frozen replay records zero publications and 223 pure captures.
The first consumed publication searches 68 nodes and predicts the complete
victory. All 66 later suggestions retain the exact winning suffix with zero
new search nodes/time. Full published forecasts, actual successors and RNG
must agree under the focused pair gate.

Settings are twelve turns, a 30,000-ms search soft budget, middle deployment
and tactical candidates. The first consumed response is 49.77 seconds,
including 28.43 seconds searching and 20.91 seconds verifying. Retained
response median/nearest-rank P95 is 2.86/4.10 seconds. Source/replay take
1036.26/602.15 seconds with other native work running concurrently. These
measurements establish this configured route; default-budget victory,
arbitrary deviations, other decks and higher Covenant still need separate
evidence. No new Mac build is made.

Focused entry: `scripts/Check-PlayerAdvisor.ps1 -Case Corrupter -SkipBuild`.
It requires actual player UI and purity, two native wins, unchanged seven-win
campaign inputs, the original full-health boss, every complete forecast and
exact retained winning suffix/state/RNG replay. The older bounded-loss pair
retains its loss requirement.

Earlier failed captures remain diagnostics with their original counters.
The fixes cover boss movement timing, source Duality and the living revived
enemy retained by borrowed aura scopes; see
[native comparison notes](../../../../docs/offline-native-planning.md).

The frozen victory also continues through the actual native campaign. Run
`campaign-ring9-20261023-070033-86b0eaef` replays all 67 inputs with zero
failure counters and saves eight wins, but its campaign driver times out on
the native Unlock screen. Its overall failure remains recorded. The driver
now clicks that screen's normal collect button. Native resume
`campaign-ring9-20261023-071349-556c0df1` finishes rewards and records an
unmodified eight-battle Standard victory, normal exit and unchanged original
files, without replaying any battle or loading the player assembly again.

`campaign-completed-corrupter.zip` retains both processes' audit history,
the unchanged inherited final-battle replay and its explicit source/hash,
the native completed save, summary and post-EndGame story observation.
Export requires the immediate parent's exact save hash, eight native wins,
passed replay/protection checks and the settled zero-counter Ring8 replay.
The failed continuation cannot be exported by itself. Restore verifies all
33 payload files. ZIP SHA256:
`0c01672f53b93fe880fe9ef081732bcec1bb8c8cbd5b7b031969fcd7ef0ed117`.
Native post-EndGame observation still reports zero of nine extra Pyre Hearts
and the true-final-boss feature locked; this Standard victory does not claim
a naturally unlocked true-final-boss route.
