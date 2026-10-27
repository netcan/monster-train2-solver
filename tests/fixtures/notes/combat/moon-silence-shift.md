# Silence does not dispatch a global moon-phase shift

Natural LazarusLeague/LunaCoven seed68586509 Ring6, refined Pyre40 save.
Deployment exploration reaches StoicPlatemail9 on AuroraWeaver19 at turn4,
room1, after33 inputs. Original release211 native observation095704-d7c52cec
retains the guard rejection and all34 inputs/complete native states/RNG.
The old guard rejected any global OnMoonPhaseShift trigger along with the
gained/lost moon callbacks, preventing an otherwise supported equip action.

Native StatusEffectMoonNewState/StatusEffectMoonFullState.OnSilence queue
only the respective moon-lost callbacks. OnSilenceLost calls
CharacterState.ResetMoonPhaseUnderstanding, which resets the mask, queues
gained/lost callbacks and updates card traits. Neither path queues the
global OnMoonPhaseShift event. Release212 therefore permits this unrelated
trigger while retaining gained/lost and card-trait protections.

The full34-input replay, four immutable parallel branches and five guards
pass. Aurora retains mask5/three Conduit stacks/three prior moon upgrades,
gains60Armor and Silence1, and retains its complete trigger state. Four
gained/lost trigger variants and an unknown moon card trait still reject.
Independent repaired native process100338-677db4cd repeats all34 inputs
and matches every complete state/RNG; the final forecast now matches.
Both archives are curated, retaining the original rejection and the repaired
forecast. The old19-input Silence reset counterexample and its independent
observation pair still pass with all three protections. Complete campaign
victory remains a separate acceptance step; this calibration does not imply it.
