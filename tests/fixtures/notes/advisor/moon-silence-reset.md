# Equipment replacement resets moon understanding after Silence loss

Sample16 Ring6,turn3,input19 replaces StoicPlatemail on Sentient(unit2).
The original unit is already silenced with moon mask6. Native removal queues
OnSilenceLost; the fullmoon status resets understanding to zero and rebuilds
the effective mask2. Reapplying Silence does not restore the initial None(4)
bit. The old forecast incorrectly retained mask6.

The original nineteen-input counterexample and an independent exact18-input
prefix plus replacement pair match complete state/forecast/RNG after repair.
Four detached branches preserve their parent. Missing masks,inactive phase
tracking,and authored moon callbacks remain rejected. Other character/card
moon callback payloads require separate native calibration.

The ordinary WithMoonPhaseMask path still preserves the initial None bit;
only the explicit native reset path clears it. Status callback pure checks pass.
This component does not establish a complete Ring6 victory.
