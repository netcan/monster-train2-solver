# Merchant Corrupt/Consume/Juice on JustCause

Random sample10,Banished/Wurmkin,seed1895012796,Covenant0,Ring2.
Original checkpoint `campaign-ring2-20261025-234946-624a75c1`,saveSHA256
`99c7da04483f790c396e453e38ed883cd4076337082f4c992172e1026bd655ba`.
The native merchant upgrades on JustCause6 are,in order,TraitCorrupt,
ReduceCostAddConsume and TraitJuice. The card has ordinary Corrupt0,
Consume0,Juice2 traits in that order and no status-scaling callbacks.
Version0.2.83 rejects the first deployment input because this owner is in
the draw pile. Its245ms refusal is preserved; it is not a strategy defeat
or evidence that a longer planning budget would help.

The original refused one-input native observation retains its missing
forecast. Two independent11-input native processes start from the identical
deployment and immutable save. On turn2,input11 casts JustCause6 in room1
on champion2: authored Valor2 adds4,existing5 becomes9,and attack10 becomes14.
Room corruption2 becomes3 below maximum4; energy stays3 because the consume
upgrade reduces cost by2. The card enters Exhausted with its base Consume
closure bound to card6. Native TimesExhausted increases by2 and attributed
status stacks by4. Health stays20; the room1 unit order stays13,2,3.
Complete states,forecasts,RNG and immutable parent checks pass for every input,
including reorder behavior,piles and private Consume state. Source/replay
component captures take37.07/36.80seconds and leave original files unchanged.

Version0.2.84 admits only the observed owner,three ordinary ordered traits
and exact ordered permanent upgrade identities. Ten negative controls retain
guards for unknown owners,status scaling,changed multipliers/definitions,
stack mode,Corrupt/Consume parameters,trait order,upgrade identity and upgrade
order. Unknown Juice combinations remain explicit gaps. This component pair
does not prove a complete Ring2 victory or a complete campaign.

The subsequent actual10000ms Ring2 advice and independent replay both win
at Pyre80 with33 inputs. Ring3 also wins both native processes at Pyre62 with
35 inputs. Its isolated callback samples coexist with zero initial active
Juice owners; JustCause23 is observed with the merchant upgrade later on
turn2. The old checker incorrectly required exactly one initial owner and
threw after all full native state/forecast/RNG comparisons had passed.
The checker now validates every initial owner and every later played owner.
All six isolated signed/overflow callback boundaries and unknown-owner guards
remain required. Rechecking the unchanged Ring3 pair passes; a focused Ring2
control retains its one-initial-owner/one-observed-play coverage. No native
battle was repeated for this checker repair.

```powershell
./scripts/Check-Models.ps1 -Fixture player-juice-justcause-calibration.mt2f -SkipBuild
```
