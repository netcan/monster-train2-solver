# Sacrifice reference after revival and aura-world preview pruning

Random06 Hellhorned/LazarusLeague, seed1848503146 Covenant0 Ring6,
checkpoint `campaign-ring6-20261025-180952-17b137d4`, input saveSHA256
`50f1c61255e62e195d58abbbf329e4739d79c448aee5b9f5ea937fb85fc60387`.
0.2.65 stops at input26, turn3, after ImpProvisation20 targets assistant15
in room2. This is an incomplete native trace, not a terminal defeat.

The assistant starts at9HP/Undying1/Unstable4. Sacrifice revives it to1HP,
consumes Undying, and its spell callback increases Unstable to8. Native UI
combat preview then sacrifices the unstable unit with a null source card.
`CharacterState.Sacrifice` writes SacrificeCard outside primary state;
preview cancellation restores HP/IsSacrifice while the null source survives.
The live actor and aura-world actor therefore both have SacrificeCardId0.
The old model predicted20 in those two locations, with other state/RNG equal.

The room simulation observed the correct0, but the dead preview actor was
pruned from the aura world. The aura restoration early return skipped its
observed shared-reference writes, and subsequent world synchronization
reintroduced20. 0.2.66 applies those already observed writes even when the
actor is absent from the final preview aura roster. Numeric primary state
and death flags remain restored normally.

Two independent native processes execute the same26 inputs. The historical
source retains the two incorrect20 references; current replay matches both
complete native chains, context/RNG, unchanged input files and immutable
parents. The component observes9->1HP, Unstable4->8 and cleared source in
both live/aura actors. Retained callback/preview controls (including32
immutable branches) and the old31-input Undying/explosion native pair pass.
These component checks do not establish a complete Ring6 victory.

```powershell
./scripts/Check-Models.ps1 -Fixture player-sacrifice-preview-calibration.mt2f -SkipBuild
```
