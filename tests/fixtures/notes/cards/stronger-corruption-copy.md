# Original stronger magic and corruption spell copies

Random sample9,Wurmkin/Hellhorned,seed771950956,Covenant0,encounters the
original spell4de04eeb-76d9-4ad0-b5cd-8c2b997395f5 with merchant CopyOnPlay.
Its authored traits are StrongerMagicPower5 and CorruptRestricted2. The
previous generic copy guard rejected the whole starting draw pile.

0.2.74 admits only this exact original non-X-cost spell and its matching
authored/runtime trait parameters. The original and repaired independent
deployment observations match completely; six controls retain rejection
for changed magic/corruption parameters,another card,X-cost and status poison.

The actual native10000ms second battle then loses after seven EndTurns with
zero plays. Preserve that source as a search-quality counterexample. An
offline alternative freezes24 inputs,19 plays and5 EndTurns. Two independent
native full battles reproduce it,win at Pyre80,and progress to Ring3. Both
actually play source14 and generated copy19: the permanent ChaosScepter
damage is100 with clone base20; the original traits survive; the copy loses
the merchant copy upgrade,keeps its temporary copy trait,and is purged after
play. No recursive copy is produced. The original is exhausted.

Component pair:cards/traits/player-stronger-corruption-copy[-replay]-calibration.mt2f.
Full battle pair:campaign/random/full-battle-random-campaign-09-ring2-refined-{search,replay}.mt2f.
The refined pair checker also requires these actual spell effects in both
traces. This is an independently verified frozen-policy victory,not an
actual-default search victory. Other authored trait combinations and
terminal-purge combinations retain their existing guards.
