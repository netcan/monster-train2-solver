# Deathwish updates retain the aura registry

Random10 Ring6,Banished/Wurmkin,seed1895012796,Covenant0. The unchanged
checkpoint is `campaign-ring6-20261026-002452-44572b0f`,saveSHA256
`d18b35734be6dccb23a5c7ec1aaee74ea52f623b13f91994766bf8cd01bfba1ce`.
Input12 summons upgraded TrainStewardShield3 as TrainStewardBig15 at room0
on turn2. Its Deathwish update previously cleared EnchanterIds permanently.
The following combat preview therefore missed enemy EnchanterT2_Titanskin14
withdrawing its three preview enchantments when dying in preview.

The original 0.2.86 prediction is retained: PreviewTargets11/12/13 remain true
and EnchanterIds becomes empty. Native primary targets remain enchanted and
Titanskin5 remains on all three live enemies. Only the three preview flags
become false; source14 remains registered and its primary health remains10.
The 0.2.87 independent process repeats exactly the original request and
matches all12 full successor states, forecasts, RNG and immutable parents.

The model now restores the original registry and AllowUpdates gate after
the internal Deathwish-only frame completes. A disabled-gate control verifies
that the update neither loses these values nor invokes aura callbacks.
The existing41-transition native enchantment matrix and the persistent-aura
death fixture pass, including21 paid plays and7 EndTurns. The older persistent
summon fixture has an existing retained Enchantment.Bound mismatch at action3;
both unchanged0.2.86 and0.2.87 fail identically. That separate gap remains open.
This component pair does not establish a complete sixth-battle victory.

```powershell
./scripts/Check-Models.ps1 -Fixture player-deathwish-enchantment-registry-calibration.mt2f -SkipBuild
```
