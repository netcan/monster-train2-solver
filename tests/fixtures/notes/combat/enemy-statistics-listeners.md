# Frame-completed ordinary enemy statistics listeners

Sample25 Underlegion/Pyreborne seed1860103187 Ring5 release270 restores real
card play but differs after input21, EndTurn at turn3. Both Heavy8 and
Titanskin10 have lethal HP0, finished dying, removal started and completed
destruction; their ordinary statistics listeners remain true in the model
and are false in native. The historical diagnostic request mentions treasure;
the preserved full native witness identifies these two ordinary enemies.

CharacterState.StateInformation.OnDestroy clears all signal listeners for
deaths as well as despawns. TrainCombatModel.CompleteFrameRemovals already
distinguishes raw room results and explicitly completed destruction IDs, but
its final listener cleanup only covered living despawns. Release271 applies
the same cleanup to all frame-completed destroyed actors. Raw combat and
uncompleted destruction keep their listeners until that boundary.

Source014925-2c79fac8 preserves all20 preceding native inputs and the original
incorrect21st prediction. The focused checker compares every complete native
state, forecast and RNG, witnesses exactly the two listener bits, replays
four immutable EndTurns and checks empty/partial completed-ID sets. Existing
three-turn destruction,27-input living-despawn and13-input champion/Titanskin
native pairs pass. Frozen independent replay and actual-budget Ring5 remain
separate acceptance steps; no whole-battle victory is inferred from this fix.

Frozen revisiond48e2bb0 independently repeats the exact request as
015400-551f10f4. Both21-input states/forecasts/RNG match; the source's two
incorrect listener bits and the independently repaired forecast are preserved.
The old22-phase/43-input ascending-frame witness and original owner-death
control also pass. Actual10000ms/two-worker Ring5 resumes the same checkpoint.
