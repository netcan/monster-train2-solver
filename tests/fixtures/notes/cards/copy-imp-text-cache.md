# Copy and Imp damage text statistics cache

The natural random28 ring 6 decision 41 copies the original Steel Enhancer
with `CreateSpellCopyWithPurge` while Imp-Scaled damage remains in hand. The
native copy 29 enters `CardStatistics` when the Imp damage text queries
`SubtypeInDeck`; the former prediction omitted that cache entry. The decompiled
`CardTraitScalingAddDamage.GetCurrentEffectText` uses `GetStatValue`, whose
subtype query calls `UpdateDeckStats`.

Two independent original and two repaired native observations replay all 41
inputs, compare complete successors and RNG, and preserve the original mismatch.
Four parallel model branches preserve their parent; changing the query to Gold is
a model-only negative control. The implementation is restricted to the observed
card definitions and query, and does not assert behavior for other combinations.
This is a component check, not a native battle victory or campaign completion.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture player-random28-ring6-copy-imp-cache-original.mt2f
```
