# Naturally progressed campaign checkpoints

These inputs use seed 424242, covenant 0 and the native Banished/Pyreborne
campaign. The driver selects offered rewards, purchases affordable upgrades,
chooses map branches and plays legal native actions. It does not author deck
contents or unit stats, grant arbitrary rewards, or jump map distances.
Preparation battles use a native heuristic policy; model acceptance runs
separately from each immutable BattleIntro checkpoint.

`campaign-progressed-ring5.zip` retains the exact native save, preferences,
metagame and choice history. Its manifest hashes every member. The save has
four consecutive completed battle records, `wasDevCheatUsed=false`, and no
remaining boss HP in any completed battle. Native preparation outcomes were:

| Ring | Won | Pyre HP after battle |
| --- | --- | ---: |
| 1 | yes | 76 |
| 2 | yes | 76 |
| 3 | yes | 57 |
| 4 | yes | 77 |

The fifth battle has not been accepted by this checkpoint alone. Winning,
native/model comparisons, fresh frozen replay and policy comparison are
separate requirements.

## Reproduce

```powershell
pwsh -NoProfile -File scripts/Run-CampaignProgress.ps1 -StopBeforeRing 5
pwsh -NoProfile -File scripts/Save-CampaignCheckpoint.ps1 -Restore -Archive tests/fixtures/campaign-progressed-ring5.zip
# Use the restored Profile returned above:
pwsh -NoProfile -File scripts/Run-RollingAdvisorBenchmark.ps1 -InputProfile <Profile> -BattleDistance 4
```

The campaign driver saves a separate direct child of `.probe-runs` before
every battle. `-InputProfile` copies and continues a retained native run; it
does not overwrite its input. The battle launcher requires a native
BattleIntro save at the requested distance and records the input save hash.
Export a newly completed checkpoint with `Save-CampaignCheckpoint.ps1
-Profile <Profile> -Archive <new.zip>`.

## Retained preparation evidence

Profiles under `.probe-runs`:

- `campaign-ring5-20261021-095939-bae3b645`: first win and native merchant
  upgrades; interrupted at the map while correcting repeated shop visits.
- `campaign-ring5-20261021-100151-82c2420d`: native continuation, three more
  wins, completed fifth-ring checkpoint; `CAMPAIGN-PASS`, 346 audit events,
  original game files unchanged.
- `campaign-ring5-20261021-100151-82c2420d-before-ring5`: exported immutable
  input, save SHA256
  `3E380C8CCF98146227D97CAE6997BE8F0CBE16995F5A637431657D68A6347E46`.

Checkpoint packaging and restore verified all nine retained members. Focused
validation also checked 840 native campaign draft selections and exact Rewards
RNG. No full regression was run for campaign preparation.
