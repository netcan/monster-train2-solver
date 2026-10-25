# Outer-boss death callback masking

The retained-metagame Banished/Pyreborne Ring3 improved41-input model win
reached its final spell before diverging in cached statistics. Arkion
`Boss_Level3_Arkion_PushAttack` has9 HP. Card13
`fb0683b1-20ac-4dd2-b580-14b502e182f4` kills it; Pyre remains37 in both
processes. The old model incorrectly increments `HeroesKilled` from1 to2
and `AnyHeroKilled` from8 to10, including this-turn counters. Native retains
the earlier values; all other comparable state and RNG agrees.

This is the same native multicast iterator behavior already calibrated for
true-final bosses. `HeroManager.CreateHeroState` registers
`CardStatistics.OnCharacterDeath` once; `DoOuterTrainBossSpawning` appends
`HeroManager.RemoveBossActionQueuedVFX`. `CoroutineSignal<T>.Invoke` advances
only the iterator returned by the last multicast delegate. Registration of
the statistics callback therefore does not mean it executed.

`RoomCombatModel` now skips that dispatch only for captured outer bosses
whose statistics listener is once-only. Ordinary units and permanent/legacy
listeners keep their existing attribution. The observation driver records
the targeted boundary's actual once-callback list as diagnostic metadata;
it does not alter native listeners or combat state.

The preserved original and independent repaired41-input native observations
complete in75.69/79.41s with unchanged checkpoints/protected files. The second
directly captures the two callbacks in that order. The focused checker verifies
every input/root/successor, exact terminal outcome/RNG, immutable parents,
unchanged Pyre37 and only the documented historical kill-counter differences.
Existing terminal death source attribution, detached spawners, clearing,
scaling and32 parallel branches pass. The original terminal-death witness and
both plain damage and paid-spell Dominion terminal components also pass.

This accepts the mechanism boundary. The improved full battle and its
independent native campaign continuation still require separate acceptance.
