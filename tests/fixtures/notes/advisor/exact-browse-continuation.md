# Exact checkpoints take priority over view replay

The champion-preserving fifth random sample's third-battle source records
a13-step winning suffix at decision46. After Mix and an advised cross-floor
spell, the native complete state/RNG exactly matches the verified suffix.
The session nevertheless searched again because recorded floor views replaced
the direct continuation match with a failed selection-history replay.

0.2.54 first matches every observed settled gameplay checkpoint against the
remaining verified prefixes in forward order. When browsing was recorded,
the final checkpoint must also match its strict full-state key, including the
unused preview allocator. Otherwise the existing selection replay rebuilds
the suffix and final snapshot. Intermediate off-route states still force a
fresh search, even if a later state converges to an old prefix.

The regression reproduces the extra search on0.2.52 and passes with one
search after the fix, using the unchanged native winning battle archive.
Native Conduit selection, seven deferred view events, rapid advised inputs,
interleaved browsing and intermediate resource divergence controls pass.
These are archived-data/session checks; fresh native acceptance follows in
the continuing campaign. No battle transition or RNG comparison is removed.

```powershell
pwsh -NoProfile -File scripts/Check-FloorBrowseAdvice.ps1 -SkipBuild
```
