# Native cumulative Covenant true final boss

Ranks 2 and 10 use original RunSetup selection and startup callbacks,
including the original 20/22-card decks and rank-nine 80-to-56 Pyre damage.
The probe then uses native Cheat_JumpToTFB: its isolated ForceTFBUnlocked
override changes false to true, distance changes 0 to 8, and original
Level9TrueFinalBoss_V2/character assets and the starting deck remain intact.
All sixteen gameplay RNG streams are preserved (NonDeterministic excluded).
No Covenant is activated after startup. This demonstrates controlled battle
composition, not earned unlocks or a naturally completed cumulative campaign.

| Rank | Native profile suffix | Turns | Room stages | Train phases | Result |
| --- | --- | ---: | ---: | ---: | --- |
| 2 | `120926-0b2a80a8` | 5 | 41 | 10 | Loss, Pyre0 |
| 10 | `121409-9052dff2` | 5 | 39 | 10 | Loss, Pyre0 |

Profiles have prefix `full-battle-no-cards-20261023-`. Both have DEPTH-PASS,
normal exits, unchanged protected files and zero capture/mismatch/unsupported/
pending counters. Every turn, room and train phase matches complete native
state, controller references and RNG, with sixteen immutable full branches.
Five raw boss definitions retain pure original Covenant birth adjustments;
native scheduled placement, queue consumption and global controller state
also match sixty-four immutable concurrent branches. Native cumulative
startup may change the queue permutation, so this acceptance checks all
three original entries rather than imposing the older isolated permutation.

Remote boss previews share enchantment effect objects with other floors.
The model now carries only those shared effects across the preview boundary,
preserving actual remote HP/attack/status state. The earlier broad import
could replace real Pyre damage with stale aura-world HP. Whole-train combat
also publishes shared effects before collecting a previously resolved floor
as a later tower target. Existing high-rank AllyBuffer/Fragile controls pass
the focused regression after this fix.

Failed profiles `120254-6c4df774` (remote-state mismatch),
`120721-66b0944a` (missing startup observation) and `121052-31a0e57e`
(old continuation calibration assumed Pyre80) are diagnostics only and are
not curated. The continuation control now verifies the original saved Pyre
health before detached query controls and records its observed birth value.

Run `scripts/Check-NativeCovenantFinalBoss.ps1 -SkipBuild`; `-Rank 2` or
`-Rank 10` selects one pair.

Rank ten also passes the actual separately loaded player assembly and a
fresh frozen native replay. Both execute twenty inputs (fifteen cards, five
EndTurns), with thirty-nine room stages and nine train phases; every root,
successor, complete forecast, controller reference and RNG matches. Both
lose with Pyre0, without unsupported/unfinished predictions. The source has
twenty-two publications and 253 captures; replay has seventy captures;
UI interactions and purity checks pass, both native processes exit normally,
protected files remain unchanged and all four failure counters are zero.
Runtimes are 353.31/202.37 seconds. Source/replay suffixes are
`121632-6b70a73e` / `122228-e5559588`, prefixed
`full-battle-units-spells-and-junk-20261023-`.
Use `Check-NativeCovenantFinalBoss.ps1 -Player -SkipBuild`; the player default
selects accepted rank ten, while the baseline default selects ranks two/ten.
This is advice/replay correctness for the fresh starting deck at the
disclosed isolated route, not a winning high-rank final-boss deck or earned
unlock. Rank two's individual player pair remains separate.
