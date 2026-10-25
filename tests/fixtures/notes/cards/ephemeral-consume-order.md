# Ephemeral before later temporary Consume

The Hellhorned/Wurmkin fifth-battle model victory first diverges at input41:
Branding Rite card33 has temporary AddEphemeral followed by the Wurm return
upgrade installing Corrupt/Consume. Native discard traits run in their
composed order. Ephemeral purges first; the later Consume then creates a
new standby entry, whose normal return records the second exhaustion.
The card remains once in Exhausted and twice in Purged.

The old model treated the purge as occurring after every Consume callback,
removed that new standby entry, and recorded only one exhaustion. It now
uses the actual composed trait order. The opposite authored-Consume order
keeps its existing behavior; its original twelve-input native pair passes.
Reactive CardExhausted listeners on this new ordering remain explicitly
guarded until their separate callback timing is calibrated.

Both independent native observations match all41 inputs, full successors,
RNG and immutable parents. The focused pair preserves the old exhaustion
counter mismatch and requires the independently repaired forecast. This
also checks subsequent pile validation: the dual references are accepted
only with the matching composed order and private consumed-card reference;
missing metadata or extra exhausted duplicates remain rejected. All fifty
frozen inputs now replay through a model victory. The complete fifty-input
route also wins in two independent native processes, with matching full
states, forecasts and RNG; see the fourth campaign's refinement acceptance.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -SkipBuild -Fixture player-ephemeral-consume-order-calibration.mt2f
```
