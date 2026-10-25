# Merchant Corrupt with positive Corrupt Restricted

Fourth random Hellhorned/Wurmkin campaign, seed770862752, sixth battle.
An offline alternative first hits the combination on card10, data
ae114dc4-d08f-4909-a2ad-d99215661463, with merchant TraitCorruptwIcon
bdb7a38c-18de-4461-beb8-c27fdbdc5f62 and authored restriction1.
Native base-state trait order includes both the restriction and installed Corrupt.

The original eleven-input observation and independent repaired native process
match all full successors and RNG, retaining immutable parents. The native
CardManager explicitly runs non-restriction pre-play callbacks before restriction
callbacks, regardless of their list order. Existing Corrupt-before-play and
restriction callbacks already model that priority; the compound guard prevented
their use together. The final kill and its authored refill leave corruption3->5.

The repaired native process additionally queries the original hand/play
eligibility methods at corruption0,1,4 and Int32.MaxValue. Presence of Corrupt
credits exactly one, including when the current amount is zero. Native integer
overflow makes the final control ineligible. All full decision/RNG values and
the Unity frame are restored after these isolated queries.

Support is limited to this observed card, one original restriction1, and the
one identified permanent merchant Corrupt upgrade. Source-room eligibility,
replaced traits and unverified combinations remain guarded. In particular,
later WurmReturnEnhanceCorrupt stacking still needs its own native calibration.
The existing twelve original eligibility queries, four-input Corrupt callback
pair and twenty-input source-alignment pair remain accepted. This component
does not turn the sixth-battle default defeat into a native victory or a clear.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -SkipBuild -Fixture player-corrupt-restricted-state-calibration.mt2f
```
