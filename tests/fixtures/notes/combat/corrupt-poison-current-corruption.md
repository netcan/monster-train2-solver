# Corrupt Poison after attack-time corruption

The ninth deterministic random sample is Wurmkin/Hellhorned,seed771950956,
Covenant0. Its first actual10000ms battle exposes a full-state mismatch at
input8,an EndTurn. The first visible difference is the champion's attacker
identity; the cause is an enemy incorrectly surviving Corrupt Poison.

The native `StatusEffectCorruptPoisonState.OnTriggered` obtains the character's
current room and `GetDamageAmount` reads `GetCurrentCorruption()` at that moment.
The champion's OnAttacking callback has already raised room corruption2->3.
After25+14 attack damage,the45-health enemy has6 health; two poison stacks
with parameter1 deal6 and kill it. The old model used the immutable room's
initial corruption2,dealt4 and retained the enemy at2 health. This also kept
the champion's last attacker5 instead of the native cleared0.

0.2.73 reads the working context's current room corruption at the poison
callback. No damage-source identity is suppressed. Two independent native
eight-input observations preserve the original forecast and the repaired
forecast; every root,successor,complete state and RNG matches. The existing
poison controls also pass: status-stack relic,Pyregel,one trigger after final
relentless exchange,turn-zero suppression and missing-input rejection.

Curated pair:`combat/damage/player-corrupt-poison-combat[-replay]-calibration.mt2f`.
Original source31.13s; repaired independent observation32.42s. The ninth
sample resumes its unchanged natural opening. Full actual10000ms advice and
independent native replay now both win:18 inputs,13 plays,5 EndTurns,Pyre80,
9 train stages,all state/forecast/RNG and four zero failure counters. Native
reward/map continuation enters Ring2. This is one accepted battle,not an
entire campaign victory.

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --corrupt-poison-combat-only tests/fixtures/combat/damage/player-corrupt-poison-combat-calibration.mt2f tests/fixtures/combat/damage/player-corrupt-poison-combat-replay-calibration.mt2f
```
