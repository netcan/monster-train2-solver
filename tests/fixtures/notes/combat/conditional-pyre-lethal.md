# Remote lethal Pyre damage

`full-battle-conditional-pyre-lethal.mt2f` comes from native process
`full-battle-units-spells-and-junk-20261022-123336-c5048d37`, captured on
2026-10-22 with `-ConditionalPyreLethal -SettleDeathDissolves
-SettleCardAnimations`. The controlled scene explicitly sets Pyre HP to five,
replaces an ordinary first-wave slot with the original
`HeavyT3_PyreDamageIfUnblocked`, and deploys one original Steward on floor one.
The enemy's original PostCombat damage remains five.

The native HP signal invokes StopCombat before the damage coroutine reaches
CheckForDeath. At the AfterStopCombatLoop snapshot the remote Pyre has HP zero
and its attacker reference, but HasFinishedDying and IsBeingRemoved remain
false. KillCamActivated remains false, owned cards and the Steward's standby
entry remain populated, and UI focus points to the Pyre floor. The room's
living-unit list excludes the Pyre; the capture retains its original native
object for full lifecycle comparison.

The fresh run exits normally after 28.72 seconds, records DEPTH-PASS, preserves
protected originals and has zero CaptureFailures/Mismatches/Unsupported/Pending.
One play, two EndTurns, thirteen room stages and three train phases match the
model. Eight additional branches compare complete state/RNG without mutating
their shared root. The existing nonlethal open/blocked-floor check also passes.

The binary archive is 24,017 bytes with 2,858 unique nodes; SHA-256:
`4aa2391a9ef92af7b134f1d8065ac267e28ea0456940277a6c9d4e32a8ea0b3f`.

Focused check:

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --conditional-pyre-lethal-only tests/fixtures/combat/resources/full-battle-conditional-pyre-lethal.mt2f
```

This explicitly authored loss validates a terminal damage boundary. It does
not establish a natural Fragile victory or open its player entry.
