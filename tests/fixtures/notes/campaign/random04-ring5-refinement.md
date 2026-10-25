# Fourth random campaign, fifth battle refined victory

Hellhorned/Wurmkin seed770862752, Covenant0, natural fifth battle
Level5BattleTitanskinEnchanter. The earlier default10000ms search loses.
A separate60000ms offline local refinement of the strength-policy baseline
finds a fifty-input winning route after changing its first deployment floor.
This is a refined frozen route, not a default-budget search victory.

The first native execution stopped at input41 on Ephemeral followed by
temporary Consume. Version0.2.43 corrects that callback order and validates
the resulting native Exhausted/Purged references for subsequent inputs.

Source full-battle-units-spells-and-junk-20261025-124128-ca475596 executes
all50 frozen inputs:42 plays and8 end turns, a settled native victory and
four zero failure counters. Independent campaign replay
campaign-ring6-20261025-124521-a51f067e wins again with identical inputs,
complete states, forecasts and RNG. Both retain the seed, Covenant and all
four prior native wins; the initial Pyre is32. Normal rewards then reach
the original sixth-battle checkpoint without modifying the real profile.

The dedicated checker explicitly permits two frozen sources. Ordinary
rolling-source pair checks still require the source to originate from a
published search. No default-budget or full-campaign clear is inferred.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -SkipBuild -Fixture full-battle-random-campaign-04-ring5-refined-search.mt2f
```
