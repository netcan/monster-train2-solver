# Combat victory statistic membership

Fixed random sequence item19, LazarusLeague/LunaCoven seed68586509, reaches a
native first-battle victory using37 default10000ms/two-worker inputs. The final
EndTurn at turn4 trips three duplicate terminal comparisons: the old prediction
retains424 values/24 tracked cards, native has293/15. Preserve the failed trace;
it is not an accepted campaign pair or release validation.

Native UpdateDeckStats falls back to the permanent deck when CardManager owns
no live battle cards, deleting entries for nine generated infusions17..25. The
victory killcam has already cleared every battle pile. Release0.2.195 applies
the existing terminal-card statistic membership refresh to this combat victory
boundary. It retains every permanent statistic and changes no comparison or
normalization rules. Defeat, stalemate and nonterminal turns retain their rules.

All37 actual inputs/full successors/RNG and four detached EndTurns pass against
the preserved native counterexample. Historical nonterminal forecasts still
match; the entire old statistic mismatch is exactly the nine removed entries.
The accepted57-input Ring6 win/replay and26-input Ring7 defeat from sample18
also pass the targeted campaign checks, preserving complete state/forecast/RNG
and original budget classification.
An independent0.2.195 native observation preserves all37 inputs, roots and
actual successors and matches the repaired final forecast. Its curated fixture
is component evidence. A new full-battle attempt still reports stale statistics
at room stage36 and train phase8: fixing only EndTurn had hidden those lower
results.0.2.196 refreshes the shared terminal combat context before the room
result is constructed. The expanded check fails on0.2.195 (424 vs293 values)
and passes on0.2.196: all42 native room stages and9 train phases match, including
their contexts, while the37-input independent observation remains exact.
The0.2.196 default first battle and independent normal progression replay both
win with37 inputs; the same campaign's second battle wins with32 inputs but
exposes the opposite error: a blanket victory refresh incorrectly drops five
generated entries (330/18 predicted vs400/23 native). Preserve that second
counterexample too. The old Smelted Treasure full native chain also rejects
the blanket refresh and retains its74 values.

An opt-in UpdateDeckStats trace on an independent first-battle native replay
identifies the actual caller:
`CardStatistics.IncrementStat > OnCharacterSacrificed > OnCharacterDeath`.
The boss has five `unstable` stacks and dies through a null-source sacrifice.
Native OnCharacterSacrificed calls IncrementStat even for a null attacking
card, refreshing ownership without attributing a per-card statistic. Ordinary
OnCharacterKilled skips IncrementStat for a null responsible card.

Release0.2.197 models that distinction in DispatchDeathStatistics and removes
both blanket victory workarounds from RoomCombat and BattleTurn. The check now
requires both37-input cache removal and32-input cache retention, including
every room/train stage and full successor/RNG; the independent37-input pair
and old Smelted Treasure cache-retention chain remain required controls.
The diagnostic is opt-in and reads native state only. This is not a general
rule that victory, defeat, preview or passive killcam clearing refresh caches.
