# Etch during global equipment standby returns

Version 0.2.90 restores character CardExhausted callbacks when equipment from
an absent original host returns during a global standby check. Native
CardManager.CheckStandByConditions suppresses this callback for a death-local
equipment check, but runs it for the later global check. The callback runs
after exhaustion statistics/pile insertion and before removing the standby
entry. Each returned card drains its character queue separately.

Two unchanged observations preserve the original incorrect forecasts:

- Random10 Ring6, Banished/Wurmkin, seed1895012796: 42 inputs. EndTurn8->9
  returns detached equipment17 and18; FirstOfKin4 on selected room1 gains two
  Plus15Attack upgrades. Base attack45->75, attack87 and health18.
- Random09 Ring8, Wurmkin/Hellhorned, seed771950956: 30 inputs. EndTurn5->6
  returns detached equipment18; FirstOfKin21 on selected room1 gains one
  Plus15Attack upgrade. Base attack59->74 and attack94.

Independent native processes repeat both complete input prefixes. The focused
checker compares every full successor, forecast and RNG, preserves the old
missing-upgrade predictions, and verifies parent immutability. Controls cover
exactly-once returns, a living original host, return to hand and a different
selected room. Existing equipment exhaustion/overflow/trigger scenarios are
checked separately. These observations are component calibration, not complete
random-campaign wins.

```powershell
./scripts/Check-Models.ps1 -Fixture player-standby-etch-paired-boss-calibration.mt2f,player-standby-etch-final-boss-calibration.mt2f -SkipBuild
```
