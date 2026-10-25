# Small Box: cached copy traits and spell damage ownership

Random09 Wurmkin/Hellhorned Ring8, seed771950956, Covenant0, exposes a
Small Box branch after24 unchanged native inputs. Playing box23 in room1
creates stronger-corruption spell33 with the original Chaos Scepter
single-copy upgrade. The cached card definition has captured copyability,
but the temporary validation card previously lost it and rejected unboxing.

Version0.2.102 carries that copyability into validation, installs the permanent
CopyOnPlay base trait during card creation, and binds created damage-scaling
queries to the spell's own type. Cached definitions remain unchanged. The
existing copy-upgrade identity, single-copy, purge/replacement and missing
copyability guards remain enforced.

The original0.2.101 process20261026-042104-ed7f6475 records the native
transition with no model forecast at the guarded boundary. An independent
0.2.102 process20261026-043127-6f531137 replays the same25 inputs and now
forecasts it. Complete successors, forecasts and RNG match. Eight parallel
branches preserve their parent; removing captured copyability still rejects
the same action. Both processes preserve the protected original files.

```powershell
scripts/Check-Models.ps1 -Fixture player-boxed-copy-trait-calibration.mt2f,player-boxed-copy-trait-replay-calibration.mt2f -SkipBuild
```

Focused Small Box, Conduit, consume/copy, expensive-card relic and spell-shield
target regressions also pass. The current41-input offline Ring8 route still
loses with1344 boss health. This calibration does not establish a campaign win.
