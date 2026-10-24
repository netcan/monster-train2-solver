# Forsaken trial opening continuation

The live Windows 0.2.13 process (PID 132388, started 2026-10-24 18:29:02)
failed on its first deployment search after nine nodes in 112ms. The reported
"start-of-combat Morsel spawn definition" was misleading: its original
`Enemy_SpawnHordeT1` enemy trial uses the same sealed native effect,
`RelicEffectSpawnUnitStartOfCombat`, to create four Forsaken on floor index 2,
then five on index 1. Both effects run on the first EndTurn. The previous
validator only admitted `SpawnRandomMorsels`; the capture also incorrectly
projected every spawned unit as a player monster and omitted hero birth data.

Capture now preserves native Heroes precedence and raw hero definitions,
Covenant stat adjustments and birth lifecycle fields. The ordered opening
dispatcher uses the existing calibrated `HeroUnitBirthModel` for this trial,
including physical slots, identities, incoming/OnSpawn callbacks and movement.
It does not allocate fallback cards or apply the player-only cardless status.
Original Morsel spawning remains separate and passes its existing native
spawn/feeding witness. Unknown or changed trial payloads still fail explicitly.
The error for another unknown opening owner now includes its actual key/id.

The sealed native `RelicEffectModifyMerchantAllCosts` exposes only
`IMerchantRelicEffect` pricing properties. It has no battle callback. It no
longer blocks battle validation or unrelated spell moon updates. Independently
validated opening spawn effects likewise do not run during moon card refresh.
Additional uncalibrated effects on either owner remain guarded.

## Evidence and focused validation

`full-battle-original-opening-forsaken-covenant10.mt2f` comes from disposable
native worker 2, profile `full-battle-units-and-junk-20261024-183811-a2ec010d`.
The native startup selected cumulative Covenant 10 and the actual unchanged
trial through `SetupTrial`/`SetTrialEnabled`; it also acquired the unchanged
CheaperMerchants relic. Only the sandbox's controlled steward/deck was modified.
The trial owner, both effects, Forsaken definitions and Covenant definitions
were not edited. The run completed in 48.29 seconds with a settled native win,
27 Pyre HP, seven spawn stages and five EndTurns. CaptureFailures, Mismatches,
Unsupported and Pending are zero, including all four opening observer counters.
The runner verified the player's original files unchanged.

The dedicated checker replays both complete opening states and RNG, checks
the exact 4/5 enemy counts and floor order, no cardless/fallback allocation,
two native invalid-room controls and eleven changed owner/payload/team/lifecycle/
Covenant-origin controls. Sixteen parallel replays per effect leave the parent
unchanged. All seven spawn and five EndTurn states match the native witness.

`player-live-forsaken-trial.mt2f` preserves the exact failing 0.2.13 native root
and read-only live enemy definition inputs. Its separate `Root` is a reproducible
projection repair, not a claim that 0.2.14 was loaded in that player process:
`--project-live-forsaken` replaces only this trial's ten repeated opening effect
projections, revalidates every captured relic list, then removes six instances
of the now-obsolete Morsel validation diagnostic. Other external interactions
are retained. Native definition reads preserve RNG and frame; the root read
uses the existing full native state/RNG/frame purity guard. The original
failing root and observed definitions remain unchanged inside the archive.

With the fixed model, this exact deployment root advances through the first
EndTurn and the default two-worker, 256-node, 3-second search returns a complete
six-turn terminal line (ten steps, 48 expanded nodes, about 3.14s) which passes
full-state/RNG replay. Its outcome is PlayerDefeated; a terminal forecast is not
proof of an optimal line or that this battle is unwinnable.

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --original-opening-forsaken-only tests/fixtures/full-battle-original-opening-forsaken-covenant10.mt2f
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --live-forsaken-only tests/fixtures/player-live-forsaken-trial.mt2f
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --original-opening-morsels-only tests/fixtures/full-battle-original-opening-morsels.mt2f
./scripts/Run-FullBattleProbe.ps1 -NativeWorker 2 -Seed 424242 -OriginalOpeningForsakenTrial -StartingCovenantLevel 10 -SkipBuild -TimeoutSeconds 240
./scripts/Check-FixtureCatalog.ps1
```

This repair uses focused fixtures and one native sandbox run; no full regression,
macOS build, player autoplay, save mutation or player process restart is required
for the verification work.

## Windows 0.2.14 installation

Built from `a3e7a3f2d2d999aecb6b903f74719fa7172d31ae` and installed at
2026-10-24 18:47:19 in the current Windows game's
`F:/SteamLibrary/steamapps/common/Monster Train 2/BepInEx/plugins/PojuAdvisor`.
The exact production Model.dll passed the native trial's full spawn/EndTurn
replay, all eleven negative controls, the actual live opening search and both
previous live moon continuation searches in an isolated checker harness.
The live trial still returns a verified six-turn terminal line within the
default budget (45 nodes, 3.045 seconds in the production-model check).

| File | SHA256 |
| --- | --- |
| MonsterTrain2Poju.Advisor.dll | 4b9c1675a3c0fa0664e6f62824c31e4968579f19e5370a679b7a59b15ad712ee |
| Model.dll | 01e0ae587b1938612e76c1dede89d8592d2a1e0394e9b495a7e7b3b8007662fb |
| FixtureArchive.dll | 40ff5d2c8b36da76daa2cab791eb91dcd8fe356531e207a6d6edd56a7e6a4eaf |

All three installed files match the accepted build. Previous and accepted
payloads are retained under
`BepInEx/PojuBackups/ForsakenOpening-20261024-184718/{previous,accepted}`.
The Windows-only ZIP is
`results/mod-dist/PojuAdvisor-0.2.14-Windows-20261024-184718.zip`.
All 28 other loader/plugin/config hashes match, including the other mods and
the existing F10, whole-battle, two-worker, 3000ms settings. The 1359 archive
hash/registration catalog passes without running battle simulations.

PID 132388 was still running the already-loaded 0.2.13 assembly at installation.
Replacing disk files does not reload managed assemblies: a normal complete
game exit and restart is needed to activate 0.2.14. No player turn, card, save,
native effect or process lifecycle was changed; one-shot read-only diagnostic
observers were detached after capture.
