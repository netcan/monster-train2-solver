# Random28 ring 7 version 437 defeat

After the offline-refined ring 6 victory, the Hellhorned/Awoken campaign with
seed 1632497512 enters ring 7 with 46 Pyre health. Version 437 uses whole-battle
search, two workers, 3000 ms and 256 maximum steps, and loses after 28 inputs
(18 card actions and 10 end turns). An independent native process replays the
frozen inputs and completes defeat settlement. The pair compares all inputs,
complete successor states, forecasts and RNG. This is not a campaign victory.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture player-random28-ring7-437-loss.mt2f
```
