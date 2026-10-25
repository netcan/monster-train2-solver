# Merchant Consume on Surgical Strike self-copy

Random campaign06, Hellhorned/LazarusLeague, seed1848503146, Covenant0,
Ring5 checkpoint `campaign-ring5-20261025-174215-386cecc9`, saveSHA256
`4bee2b0981053f1c0a4e27f1515372d8d7a872a45ec5b875b4f8d7dc76a945dd`.
SurgicalStrike24 has ChaosScepter (+20 power and Consume) and ReduceCost.
Its effective base traits include the merchant Consume, whereas the authored
card definition contains only slay-fire-count suppression and IgnoreArmor.
The old self-copy guard rejected this difference at the first deployment.

Native `CardEffectAddUpgradedCopy` creates the authored card, copies permanent
and temporary upgrades, then adds SurgicalStrike_Upgrade (Ephemeral). 0.2.64
reconstructs those authored traits before copying upgrades for this exact
SurgicalStrike/ChaosScepter signature. It avoids duplicating Consume or its
private exhaust state. Unknown owners, duplicate Consume traits, authored
starting upgrades, replacements and other unsupported combinations remain
guarded.

The original one-input refusal remains unchanged. Two independent native
observations execute the same18 inputs; at turn3 the final action plays
card24 in room2 against unit15. The lethal24 damage creates child33 with
zero cost, inherited ChaosScepter/ReduceCost, one new Ephemeral upgrade,
one fresh Consume state (trait index2, false/null), and NextCardId+1.
The source card is exhausted. All roots, successors, context and RNG match;
parents and input saves remain unchanged. These are component observations,
not a complete Ring5 victory.

Sources: `full-battle-units-spells-and-junk-20261025-174825-876f0667`
(original refusal), `...-20261025-175023-e5b7e997` and
`...-20261025-175033-9f858b43` (independent positives).
The original self-copy battle also passes:12 plays/5 EndTurns, six original
cards, one generated play, three deterministic self-copies, inherited
Ephemeral, suppression/revival and16 immutable full chains.

```powershell
./scripts/Check-Models.ps1 -Fixture player-consume-kill-copy-calibration.mt2f -SkipBuild
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --original-kill-copy-only tests/fixtures/cards/originals/full-battle-original-kill-copy.mt2f
```
