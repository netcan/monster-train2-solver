# Room status after a lethal self-targeted ability

The unchanged random Pyreborne/Banished Ring5 checkpoint (seed1862997127,
rank0) exposes a legal self-targeted champion ability. The original ability
`c12a58a4-0bae-4120-9921-f93d91b20bcb` deals its source's attack to the selected
actor, then adds four Pyregel to enemies in the room. At the observed boundary,
champion2 has25HP/31attack and targets itself. It dies; enemies10/11 keep their
HP and gain four Pyregel each. This is not retaliation or damage to both enemies.

The old spell model required the damage source to remain present even while
applying the subsequent non-damage status. The source-presence guard now applies
only to actual damage; damage from a missing actor remains unsupported.

Native ability completion also differs from ordinary card discard: removal
returns the champion's current equipment20, but does not perform a global
standby scan. Detached historical equipment12 remains in standby without another
exhaustion event. The ability model no longer performs the extra global scan.
Ordinary card and EndTurn scans remain in their original locations.

Two independent native processes replay the complete29-input prefix plus this
ability. `combat/units/player-dead-caster-status-calibration.mt2f` preserves the
original rejection (`full-battle-units-spells-and-junk-20261025-081414-bcec29e2`);
the `-replay-calibration` archive captures the repaired forecast in
`full-battle-units-spells-and-junk-20261025-082217-69e0782b`. All30 successors,
full comparable state/RNG, immutable roots and original overlay purity pass.
The existing native ability/Incant control also passes. These are component
observations and do not imply a good tactical choice or terminal battle victory.

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --dead-caster-status-pair-only <source.mt2f> <replay.mt2f>
```
