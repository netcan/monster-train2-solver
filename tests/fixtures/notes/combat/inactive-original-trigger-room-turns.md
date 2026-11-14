# Natural ascension from original trigger rooms

Two isolated no-cards battles attach unchanged BoilerRoom and
SacrificialChamber to floor 0, each with a cardless first-wave
MageT1_Junker. After a tracking-free change to New moon, the enemy still
understands Full (mask 6). A normal native EndTurn moves it to floor 1 and
removes the original room upgrade and its OnDeath trigger. The mask changes
to 5 before the global phase returns to Full.

Each room was sampled in two separate muted Instant processes. The two
BoilerRoom archives are byte-identical (SHA-256
`9e4286a6e709b2eb614712173f6833ef97f7acb4f5e552ef78bf0e36849b1c4d`);
the two SacrificialChamber archives are byte-identical (SHA-256
`aad6b501f74133d4df39b325b9bf26302ae5db99ad1511bd8c11b6400bc4b041`).
The checker matches each complete settled native/model state and gameplay
RNG, replays the EndTurn through the planner, and checks four immutable
branches. Neither OnDeath effect fires in this no-death witness.

The separate ordinary-combat archives
`full-battle-original-isolated-room-trigger-energy.mt2f` and
`full-battle-original-isolated-room-trigger-gold.mt2f` contain unchanged
original room-card plays and positive OnDeath resource effects. Their focused
checks pass the original-room trigger, complete battle chain and 128-state
search. They do not cover a death after an inactive-moon room move.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture room-moon-inactive-original-boilerroom-turn-calibration.mt2f
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture room-moon-inactive-original-sacrificialchamber-turn-calibration.mt2f
```
