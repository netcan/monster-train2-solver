# Skill presentation must not create native cards

Sample16 Ring5,0.2.174, naturally reaches turn6 and a retained winning suffix.
Publishing generation77 calls CharacterState.GetUnitAbilityCardState while
describing a later skill step. That native method calls
UnitOrRoomAbilityCardStateCache.Get, whose cold path constructs a new CardState.
The publication's read-only display therefore changes native state itself.

The curated exact counterexample has NextCardId31->32,registry30->31,and an
initially empty ability cache gaining card31,d1ab03ed-fe63-4eca-b68e-3090524f325e.
Restoring precisely those three fields in every aliased context reproduces the
entire previous state,including RNG. The real allocator/cache change remains
keyed and rejected; it is not a cosmetic difference to ignore.

0.2.175 reads the existing native cache without calling its factory. If the
skill has not been created,the display uses its definition's name and omits an
uncaptured cost. Existing cached card titles/costs retain their ordinary path.
Neither expanding nor collapsing a future plan may allocate native skill cards.
The isolated player driver now renders both modes against full native snapshots
and checks that state/RNG,generation and search count stay unchanged. These
checks are excluded from the actual player assembly. Complete clean native
source/replay acceptance is still pending.
